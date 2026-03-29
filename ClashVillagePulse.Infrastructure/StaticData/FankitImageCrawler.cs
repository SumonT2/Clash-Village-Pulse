using System.Net;
using System.Text.RegularExpressions;

namespace ClashVillagePulse.Infrastructure.StaticData;

public sealed class FankitImageCrawler
{
    private static readonly HashSet<string> DirectExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg", ".avif", ".zip"
    };

    private static readonly string[] DetailHints =
    {
        "/show/",
        "/document/",
        "/collection/",
        "/asset",
        "game-assets",
        "/d/"
    };

    private static readonly string[] SkipLinkHints =
    {
        "mailto:",
        "javascript:",
        "#"
    };

    private readonly HttpClient _httpClient;

    public FankitImageCrawler(HttpClient httpClient)
    {
        _httpClient = httpClient;

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/123.0 Safari/537.36");
        }
    }

    public async Task<FankitCrawlResult> CrawlAsync(
        IEnumerable<string> seedUrls,
        int maxPages,
        double delaySeconds,
        CancellationToken cancellationToken = default)
    {
        var visited = new List<string>();
        var seenPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(seedUrls.Where(x => !string.IsNullOrWhiteSpace(x)));
        var assets = new Dictionary<string, FankitAssetCandidate>(StringComparer.OrdinalIgnoreCase);

        while (queue.Count > 0 && seenPages.Count < maxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = queue.Dequeue();
            if (!seenPages.Add(url))
                continue;

            visited.Add(url);

            string html;
            string currentUrl;
            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                currentUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
                html = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch
            {
                continue;
            }

            var extracted = ExtractLinksAndAssets(html, currentUrl);
            foreach (var asset in extracted.Assets)
            {
                if (!assets.TryGetValue(asset.Url, out var existing) || asset.PageText.Length > existing.PageText.Length)
                {
                    assets[asset.Url] = asset;
                }
            }

            foreach (var link in extracted.DetailLinks)
            {
                if (!seenPages.Contains(link))
                {
                    queue.Enqueue(link);
                }
            }

            if (delaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        return new FankitCrawlResult(assets.Values.ToList(), visited);
    }

    public async Task<FankitDownloadedFile?> DownloadAssetAsync(
        FankitAssetCandidate candidate,
        string destinationDirectory,
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);

        var fileName = BuildLevelAwareFileName(candidate.Url, candidate.MatchedLevel);
        var destinationPath = Path.Combine(destinationDirectory, SanitizeFileName(fileName));

        if (File.Exists(destinationPath) && !overwrite)
        {
            return await CreateDownloadedFileAsync(destinationPath, candidate, cancellationToken);
        }

        try
        {
            using var response = await _httpClient.GetAsync(candidate.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, cancellationToken);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            return await CreateDownloadedFileAsync(destinationPath, candidate, cancellationToken, contentType);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<FankitDownloadedFile> CreateDownloadedFileAsync(
        string destinationPath,
        FankitAssetCandidate candidate,
        CancellationToken cancellationToken,
        string? mimeType = null)
    {
        byte[] bytes = await File.ReadAllBytesAsync(destinationPath, cancellationToken);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();

        return new FankitDownloadedFile(
            destinationPath,
            Path.GetFileName(destinationPath),
            hash,
            mimeType,
            candidate.SourcePage,
            candidate.Url);
    }

    private static FankitExtractResult ExtractLinksAndAssets(string html, string currentUrl)
    {
        var assets = new Dictionary<string, FankitAssetCandidate>(StringComparer.OrdinalIgnoreCase);
        var detailLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pageTitle = ExtractTitle(html);
        var isDetailPage = IsDetailPage(currentUrl);
        var pageContextText = BuildPageContextText(html, currentUrl, pageTitle, isDetailPage);

        void AddCandidate(string rawUrl, string label, string assetType, string? extraText = null)
        {
            var fullUrl = ResolveUrl(currentUrl, rawUrl);
            if (string.IsNullOrWhiteSpace(fullUrl))
                return;

            var fileHint = TryGetFileHint(fullUrl);
            var combinedText = string.Join(' ', new[]
            {
                label,
                extraText,
                fileHint,
                pageContextText
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var candidate = new FankitAssetCandidate(
                fullUrl,
                currentUrl,
                currentUrl,
                string.IsNullOrWhiteSpace(label) ? pageTitle : label,
                assetType,
                combinedText,
                ExtractCandidateLevel(string.Concat(combinedText, " ", fullUrl)));

            if (!assets.TryGetValue(candidate.Url, out var existing) || candidate.PageText.Length > existing.PageText.Length)
            {
                assets[candidate.Url] = candidate;
            }
        }

        foreach (Match match in Regex.Matches(html, "<img[^>]+(?:src|data-src|data-original)=[\"'](?<url>[^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var rawTag = match.Value;
            var src = match.Groups["url"].Value;
            var label = ExtractAttribute(rawTag, "alt") ?? ExtractAttribute(rawTag, "title") ?? pageTitle;
            AddCandidate(src, label, "image", label);

            var srcset = ExtractAttribute(rawTag, "srcset") ?? ExtractAttribute(rawTag, "data-srcset");
            if (!string.IsNullOrWhiteSpace(srcset))
            {
                foreach (var item in srcset.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var piece = item.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(piece))
                    {
                        AddCandidate(piece, label, "image", label);
                    }
                }
            }
        }

        foreach (Match match in Regex.Matches(html, "<meta[^>]+(?:property|name)=[\"'](?<name>[^\"']+)[\"'][^>]+content=[\"'](?<content>[^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var name = match.Groups["name"].Value.Trim().ToLowerInvariant();
            if (name is "og:image" or "twitter:image" or "og:image:url")
            {
                AddCandidate(match.Groups["content"].Value, pageTitle, "meta_image");
            }
        }

        foreach (Match match in Regex.Matches(html, "<a[^>]+href=[\"'](?<href>[^\"']+)[\"'][^>]*>(?<text>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var href = match.Groups["href"].Value;
            var fullUrl = ResolveUrl(currentUrl, href);
            if (string.IsNullOrWhiteSpace(fullUrl))
                continue;

            var label = StripHtml(match.Groups["text"].Value);
            if (LooksLikeDirectAsset(fullUrl))
            {
                AddCandidate(fullUrl, label, "download", label);
            }
            else if (ShouldFollowLink(fullUrl, currentUrl))
            {
                detailLinks.Add(fullUrl);
            }
            else if (fullUrl.Contains("download", StringComparison.OrdinalIgnoreCase) || fullUrl.Contains("asset", StringComparison.OrdinalIgnoreCase))
            {
                AddCandidate(fullUrl, label, "possible_download", label);
            }
        }

        foreach (Match match in Regex.Matches(html, "<script[^>]*>(?<code>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var code = WebUtility.HtmlDecode(match.Groups["code"].Value ?? string.Empty).Replace("\\/", "/");
            foreach (Match urlMatch in Regex.Matches(code, @"https?://[^""'\s<>]+", RegexOptions.IgnoreCase))
            {
                var assetUrl = urlMatch.Value.Trim('"', '\'', ')', ']', '}', '>');
                if (LooksLikeDirectAsset(assetUrl) || assetUrl.Contains("fankit.supercell.com", StringComparison.OrdinalIgnoreCase))
                {
                    AddCandidate(assetUrl, pageTitle, "script_asset");
                }
            }
        }

        return new FankitExtractResult(assets.Values.ToList(), detailLinks);
    }

    private static string BuildPageContextText(string html, string currentUrl, string pageTitle, bool isDetailPage)
    {
        if (!isDetailPage)
        {
            return pageTitle;
        }

        var parts = new List<string>();

        foreach (var text in ExtractHeadingTexts(html))
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                parts.Add(text);
            }
        }

        foreach (var chip in ExtractChipTexts(html))
        {
            if (!string.IsNullOrWhiteSpace(chip))
            {
                parts.Add(chip);
            }
        }

        foreach (var value in ExtractMetadataValues(html))
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add(value);
            }
        }

        var stripped = StripHtml(html);
        if (!string.IsNullOrWhiteSpace(stripped))
        {
            parts.Add(TakeWords(stripped, 120));
        }

        parts.Add(pageTitle);

        return string.Join(' ', parts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeLooseText)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ExtractHeadingTexts(string html)
    {
        foreach (Match match in Regex.Matches(
                     html,
                     "<h[1-3][^>]*>(?<text>.*?)</h[1-3]>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var text = StripHtml(match.Groups["text"].Value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }

    private static IEnumerable<string> ExtractChipTexts(string html)
    {
        foreach (Match match in Regex.Matches(
                     html,
                     "<(?:button|span|div)[^>]*class=[\"'][^\"']*(?:chip|badge|tag|pill)[^\"']*[\"'][^>]*>(?<text>.*?)</(?:button|span|div)>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var text = StripHtml(match.Groups["text"].Value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }

    private static IEnumerable<string> ExtractMetadataValues(string html)
    {
        foreach (Match match in Regex.Matches(
                     html,
                     "<dt[^>]*>(?<key>.*?)</dt>\\s*<dd[^>]*>(?<value>.*?)</dd>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var value = StripHtml(match.Groups["value"].Value);
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }

        foreach (Match match in Regex.Matches(
                     html,
                     @"(?<key>Asset Type|Characters|Heroes|Hero Pets|Home Village|Builder Base)\s*</[^>]+>\s*<[^>]*>(?<value>.*?)</",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var value = StripHtml(match.Groups["value"].Value);
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }

    private static string NormalizeLooseText(string value)
    {
        return Regex.Replace(WebUtility.HtmlDecode(value ?? string.Empty), @"\s+", " ").Trim();
    }

    private static string TakeWords(string value, int maxWords)
    {
        var words = Regex.Split(value ?? string.Empty, @"\s+")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(maxWords);

        return string.Join(' ', words);
    }

    private static bool IsDetailPage(string url)
        => url.Contains("/show/", StringComparison.OrdinalIgnoreCase)
           || url.Contains("/document/", StringComparison.OrdinalIgnoreCase)
           || url.Contains("/collection/", StringComparison.OrdinalIgnoreCase);

    private static string ExtractTitle(string html)
    {
        var match = Regex.Match(html, "<title>(?<title>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success
            ? WebUtility.HtmlDecode(StripHtml(match.Groups["title"].Value)).Trim()
            : string.Empty;
    }

    private static string? ExtractAttribute(string tagHtml, string attributeName)
    {
        var match = Regex.Match(
            tagHtml,
            $"{Regex.Escape(attributeName)}=[\\\"'](?<value>[^\\\"']+)[\\\"']",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success ? WebUtility.HtmlDecode(match.Groups["value"].Value).Trim() : null;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var noTags = Regex.Replace(html, "<[^>]+>", " ", RegexOptions.Singleline);
        var decoded = WebUtility.HtmlDecode(noTags);
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }

    private static string? ResolveUrl(string currentUrl, string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return null;

        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var baseUri))
            return null;

        if (Uri.TryCreate(baseUri, rawUrl, out var resolved))
            return resolved.ToString();

        return null;
    }

    private static bool ShouldFollowLink(string url, string rootUrl)
    {
        if (SkipLinkHints.Any(prefix => url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var target) || !Uri.TryCreate(rootUrl, UriKind.Absolute, out var root))
            return false;

        if (!string.Equals(target.Host, root.Host, StringComparison.OrdinalIgnoreCase))
            return false;

        var lower = url.ToLowerInvariant();
        return DetailHints.Any(lower.Contains);
    }

    private static bool LooksLikeDirectAsset(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var extension = Path.GetExtension(uri.AbsolutePath);
        if (!string.IsNullOrWhiteSpace(extension) && DirectExtensions.Contains(extension))
            return true;

        var lowerPath = uri.AbsolutePath.ToLowerInvariant();
        return lowerPath.Contains("/download/")
            || lowerPath.Contains("/image/")
            || lowerPath.Contains("/images/")
            || lowerPath.Contains("/files/")
            || lowerPath.Contains("/api/asset/");
    }

    private static string? TryGetFileHint(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var fileName = Path.GetFileName(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem))
            return null;

        return Regex.Replace(stem, @"[_\-\.]+", " ").Trim();
    }

    private static int? ExtractCandidateLevel(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        foreach (var pattern in new[]
        {
            @"\blevel\s*(\d{1,2})\b",
            @"\blv\s*(\d{1,2})\b",
            @"\b(\d{1,2})\s*lvl\b",
            @"[_\-\s](\d{1,2})(?:\s|$)"
        })
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static string BuildLevelAwareFileName(string url, int? level)
    {
        var uri = new Uri(url);
        var name = Path.GetFileName(uri.AbsolutePath);
        var stem = Path.GetFileNameWithoutExtension(name);
        var extension = Path.GetExtension(name);

        if (string.IsNullOrWhiteSpace(extension))
        {
            var normalized = name.ToLowerInvariant();
            foreach (var format in new[] { "png", "jpg", "jpeg", "webp", "gif", "svg", "avif", "zip" })
            {
                if (normalized.EndsWith(format, StringComparison.OrdinalIgnoreCase))
                {
                    stem = name[..^format.Length].TrimEnd('.', '_', '-');
                    extension = "." + format;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(url))).ToLowerInvariant()[..12];
        }

        return level.HasValue ? $"{stem}_lv{level.Value}{extension}" : $"{stem}{extension}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new System.Text.StringBuilder(fileName.Length);
        foreach (var ch in fileName)
        {
            builder.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        return builder.ToString();
    }
}

public sealed record FankitExtractResult(
    IReadOnlyList<FankitAssetCandidate> Assets,
    IReadOnlySet<string> DetailLinks);

public sealed record FankitCrawlResult(
    IReadOnlyList<FankitAssetCandidate> Assets,
    IReadOnlyList<string> VisitedPages);

public sealed record FankitAssetCandidate(
    string Url,
    string SourcePage,
    string DetailPage,
    string Label,
    string AssetType,
    string PageText,
    int? CandidateLevel)
{
    public double Score { get; init; }

    public int? MatchedLevel { get; init; }

    public string MatchedSlug { get; init; } = string.Empty;

    public string MatchReason { get; init; } = string.Empty;
}

public sealed record FankitDownloadedFile(
    string AbsolutePath,
    string FileName,
    string ContentHash,
    string? MimeType,
    string SourcePageUrl,
    string SourceUrl);
