using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public sealed class FankitImagesProcessor : StaticDataTargetProcessorBase
{
    private static readonly string[] AliasFields =
    {
        "TID",
        "InfoTID",
        "ExportName",
        "IconExportName",
        "BigPicture",
        "ArmyTrainingPicture",
        "IconSWF",
        "BigPictureSWF",
        "ArmyTrainingPictureSWF",
        "Name"
    };

    private static readonly string[] StopWords =
    {
        "hero", "pet", "unit", "icon", "big", "picture", "army", "training", "info",
        "default", "placeholder", "ability", "character", "level", "lv", "asset", "assets",
        "clash", "of", "coc", "supercell", "tid", "game", "show", "document", "collection",
        "png", "jpg", "jpeg", "webp", "avif", "svg"
    };

    private static readonly HashSet<string> StopWordSet = new(StopWords, StringComparer.OrdinalIgnoreCase);

    private static readonly string[] StrongWords =
    {
        "lassi", "barksy", "yak", "bulldozer", "owl", "electro", "electrowl", "unicorn",
        "phoenix", "egg", "tiny", "stork", "lizard", "poison", "diggy", "frosty", "fox",
        "spirit", "fennec", "phase", "jelly", "rage", "sneezy", "raven", "crow", "snail",
        "haste", "turtle", "greedy", "unipony"
    };

    private static readonly HashSet<string> StrongWordSet = new(StrongWords, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string[]> IdentifierRewrites = new(StringComparer.OrdinalIgnoreCase)
    {
        ["electrowl"] = new[] { "electro owl" },
        ["unipony"] = new[] { "uni pony unicorn" },
        ["phasefennec"] = new[] { "phase fennec fox" },
        ["ragejelly"] = new[] { "rage jelly angry jelly" },
        ["poisoniguana"] = new[] { "poison iguana poison lizard" },
        ["spiritjellyfish"] = new[] { "spirit jellyfish turtle" },
        ["hastespirit"] = new[] { "haste spirit speedup pet" },
        ["slimesnail"] = new[] { "slime snail jump aura pet" },
        ["bulldozer"] = new[] { "bulldozer mighty yak" },
        ["barksy"] = new[] { "barksy lassi" },
        ["meleejumper"] = new[] { "melee jumper" },
        ["rangedattacker"] = new[] { "ranged attacker" },
        ["wallbuster"] = new[] { "wall buster" }
    };

    private static readonly Dictionary<string, string[]> PetSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lassi"] = new[] { "barksy", "barky", "pet lassi" },
        ["mighty yak"] = new[] { "yak", "bulldozer", "mightyyak" },
        ["electro owl"] = new[] { "owl", "eowl", "electrowl" },
        ["spirit fox"] = new[] { "fox", "phase fennec", "phasefennec" },
        ["angry jelly"] = new[] { "rage jelly", "ragejelly", "jelly" },
        ["poison lizard"] = new[] { "poison iguana", "poisoniguana", "lizard", "iguana" },
        ["crow"] = new[] { "raven", "greedy raven" },
        ["phoenix egg"] = new[] { "phoenix", "egg" },
        ["stork"] = new[] { "tiny" },
        ["jumpaurapet"] = new[] { "jump aura pet", "slime snail", "slimesnail" },
        ["speeduppet"] = new[] { "speedup pet", "haste spirit", "hastespirit" },
        ["airsplitpetspawnremoved"] = new[] { "air split pet", "airsplit pet spawn removed" }
    };

    private static readonly Dictionary<string, (int GlobalId, string DisplayName)> HelperMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BuilderApprentice"] = (93000000, "Builder's Apprentice"),
        ["ResearchApprentice"] = (93000001, "Lab Assistant"),
        ["Alchemist"] = (93000002, "Alchemist"),
        ["Prospector"] = (93000003, "Prospector")
    };

    private static readonly IReadOnlyList<FankitCsvSource> CsvSources =
    [
        new("logic/buildings.csv", new[] { "Level" }, ResolveGenericId, ResolveSectionFromVillageType, _ => ItemType.Building),
        new("logic/characters.csv", new[] { "VisualLevel", "Level" }, ResolveGenericId, ResolveSectionFromVillageType, ResolveCharacterItemType),
        new("logic/heroes.csv", new[] { "VisualLevel", "Level" }, ResolveGenericId, ResolveSectionFromVillageType, _ => ItemType.Hero),
        new("logic/spells.csv", new[] { "Level" }, ResolveGenericId, ResolveSectionFromVillageType, _ => ItemType.Spell),
        new("logic/pets.csv", new[] { "TroopLevel", "Level" }, ResolveGenericId, ResolveSectionFromVillageType, _ => ItemType.Pet),
        new("logic/character_items.csv", new[] { "Level" }, ResolveGenericId, _ => VillageSection.HomeVillage, _ => ItemType.Equipment),
        new("logic/traps.csv", new[] { "Level" }, ResolveGenericId, ResolveSectionFromVillageType, _ => ItemType.Trap),
        new("logic/villager_apprentices.csv", new[] { "Level" }, ResolveHelperId, _ => VillageSection.HomeVillage, _ => ItemType.Helper)
    ];

    private static readonly string[] DefaultSeedUrls =
    {
        "https://fankit.supercell.com/d/vkEdmkUCngKw/game-assets",
        "https://fankit.supercell.com/d/vkEdmkUCngKw/game-assets?asset-type97=Hero+Pets"
    };

    private readonly FankitImageCrawler _crawler;

    public FankitImagesProcessor(FankitImageCrawler crawler)
    {
        _crawler = crawler;
    }

    public override string TargetKey => "fankit-images";

    public override async Task ProcessAsync(StaticDataProcessorContext context, CancellationToken cancellationToken = default)
    {
        var outputRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "static", "fankit");
        Directory.CreateDirectory(outputRoot);

        var aliasGroups = await TrackParseAsync(
            context,
            async () => await BuildAliasGroupsAsync(context, cancellationToken),
            "Built Fan Kit alias groups from static-data CSV sources.",
            cancellationToken);

        var crawlResult = await TrackParseAsync(
            context,
            async () => await _crawler.CrawlAsync(DefaultSeedUrls, maxPages: 30, delaySeconds: 0.6d, cancellationToken),
            "Discovered public Fan Kit asset candidates.",
            cancellationToken);

        var directOverrideCandidates = LoadOverrideCandidates(outputRoot).ToList();
        var allCandidates = DedupeCandidates(crawlResult.Assets.Concat(directOverrideCandidates)).ToList();
        var autoMatches = MatchGroups(aliasGroups, allCandidates, threshold: 16.0d, preferLatest: false).ToList();

        await TrackSaveAsync(
            context,
            async () => await PersistMatchesAsync(context, aliasGroups, autoMatches, crawlResult.VisitedPages, outputRoot, cancellationToken),
            $"Saved {autoMatches.Count} Fan Kit image matches.",
            cancellationToken);
    }


    private async Task<List<FankitAliasGroup>> BuildAliasGroupsAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken)
    {
        var groups = new Dictionary<string, FankitAliasGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in CsvSources)
        {
            var rawBytes = await DownloadAsync(context, source.AssetPath, cancellationToken);
            var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);
            var rows = ReadSparseCsvRows(decompressedBytes);

            foreach (var row in rows)
            {
                if (!source.TryResolveItemDataId(row, out var itemDataId) || itemDataId <= 0)
                    continue;

                var name = (row.GetValueOrDefault("Name") ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var section = source.ResolveSection(row);
                var itemType = source.ResolveItemType(row);
                var level = ResolveLevel(row, source.LevelColumns);
                if (level is null || level.Value <= 0)
                    continue;

                var aliases = BuildAliases(row, name, itemType);
                var slug = Slugify(name);
                var key = $"{section}|{itemType}|{itemDataId}";

                if (!groups.TryGetValue(key, out var group))
                {
                    group = new FankitAliasGroup(
                        itemDataId,
                        itemType,
                        section,
                        slug,
                        name,
                        aliases,
                        ExtractAnchorWords(name, aliases));
                    groups[key] = group;
                }
                else
                {
                    var mergedAliases = group.Aliases.Union(aliases, StringComparer.OrdinalIgnoreCase).ToList();
                    group = group with
                    {
                        Aliases = mergedAliases,
                        AnchorWords = ExtractAnchorWords(group.Name, mergedAliases)
                    };
                    groups[key] = group;
                }

                var rowAliases = new HashSet<string>(groups[key].Aliases, StringComparer.OrdinalIgnoreCase)
                {
                    name,
                    $"{name} level {level.Value}",
                    $"{name} lv {level.Value}"
                };

                foreach (var alias in groups[key].Aliases)
                {
                    rowAliases.Add($"{alias} level {level.Value}");
                    rowAliases.Add($"{alias} lv {level.Value}");
                }

                groups[key].Levels.Add(new FankitAliasRow(level.Value, name, rowAliases.OrderBy(x => x).ToList()));
            }
        }

        return groups.Values
            .Select(x => x with
            {
                Levels = x.Levels
                    .GroupBy(l => l.Level)
                    .Select(g => g.First())
                    .OrderBy(l => l.Level)
                    .ToList(),
                Aliases = x.Aliases.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                AnchorWords = x.AnchorWords.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            })
            .ToList();
    }

    private async Task PersistMatchesAsync(
        StaticDataProcessorContext context,
        IReadOnlyList<FankitAliasGroup> groups,
        IReadOnlyList<FankitImageMatch> matches,
        IReadOnlyList<string> visitedPages,
        string outputRoot,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var staticItems = await db.StaticItems
            .Include(x => x.Levels)
            .Include(x => x.Images)
            .ThenInclude(x => x.StaticImageAsset)
            .ToListAsync(cancellationToken);

        var itemMap = staticItems.ToDictionary(
            x => BuildItemKey(x.Section, x.ItemType, x.ItemDataId),
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var matchedItemIds = new HashSet<Guid>();
        foreach (var match in matches)
        {
            var itemKey = BuildItemKey(match.Group.Section, match.Group.ItemType, match.Group.ItemDataId);
            if (itemMap.TryGetValue(itemKey, out var staticItem))
            {
                matchedItemIds.Add(staticItem.Id);
            }
        }

        if (matchedItemIds.Count > 0)
        {
            var existingLinks = await db.StaticItemImages
                .Include(x => x.StaticImageAsset)
                .Where(x => matchedItemIds.Contains(x.StaticItemId) && x.StaticImageAsset.SourceType == StaticImageSourceType.Fankit)
                .ToListAsync(cancellationToken);

            db.StaticItemImages.RemoveRange(existingLinks);
            await db.SaveChangesAsync(cancellationToken);
        }

        var createdAssets = new Dictionary<string, StaticImageAsset>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches.OrderByDescending(x => x.Candidate.Score))
        {
            var itemKey = BuildItemKey(match.Group.Section, match.Group.ItemType, match.Group.ItemDataId);
            if (!itemMap.TryGetValue(itemKey, out var staticItem))
                continue;

            var categoryPath = BuildCategoryPath(outputRoot, staticItem);
            var download = await _crawler.DownloadAssetAsync(match.Candidate, categoryPath, overwrite: false, cancellationToken);
            if (download is null)
                continue;

            if (!createdAssets.TryGetValue(match.Candidate.Url, out var imageAsset))
            {
                imageAsset = await db.StaticImageAssets
                    .FirstOrDefaultAsync(
                        x => x.SourceType == StaticImageSourceType.Fankit && x.SourceUrl == match.Candidate.Url,
                        cancellationToken)
                    ?? new StaticImageAsset
                    {
                        Id = Guid.NewGuid(),
                        SourceType = StaticImageSourceType.Fankit,
                        SourceUrl = match.Candidate.Url
                    };

                imageAsset.SourcePageUrl = download.SourcePageUrl;
                imageAsset.LocalPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), download.AbsolutePath).Replace('\\', '/');
                imageAsset.FileName = download.FileName;
                imageAsset.ContentHash = download.ContentHash;
                imageAsset.MimeType = download.MimeType ?? InferMimeType(download.FileName);
                imageAsset.DownloadedAtUtc = DateTime.UtcNow;
                imageAsset.MatchScore = match.Candidate.Score;
                imageAsset.MatchReason = match.Candidate.MatchReason;
                imageAsset.IsPrimary = true;

                if (imageAsset.Id == Guid.Empty)
                {
                    imageAsset.Id = Guid.NewGuid();
                }

                if (db.Entry(imageAsset).State == EntityState.Detached)
                {
                    db.StaticImageAssets.Add(imageAsset);
                }

                createdAssets[match.Candidate.Url] = imageAsset;
            }

            var staticLevel = staticItem.Levels.FirstOrDefault(x => x.Level == match.Level);
            var assetKind = InferAssetKind(match.Candidate);

            db.StaticItemImages.Add(new StaticItemImage
            {
                Id = Guid.NewGuid(),
                StaticItemId = staticItem.Id,
                StaticItemLevelId = staticLevel?.Id,
                StaticImageAssetId = imageAsset.Id,
                AssetKind = assetKind,
                MatchedLevel = match.Level,
                IsPreferred = true
            });
        }

        var groupedPreferred = matches
            .GroupBy(x => BuildItemKey(x.Group.Section, x.Group.ItemType, x.Group.ItemDataId), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.Candidate.Score).ThenByDescending(x => x.Level).First())
            .ToList();

        foreach (var preferred in groupedPreferred)
        {
            var itemKey = BuildItemKey(preferred.Group.Section, preferred.Group.ItemType, preferred.Group.ItemDataId);
            if (!itemMap.TryGetValue(itemKey, out var staticItem))
                continue;

            if (!createdAssets.TryGetValue(preferred.Candidate.Url, out var imageAsset))
                continue;

            db.StaticItemImages.Add(new StaticItemImage
            {
                Id = Guid.NewGuid(),
                StaticItemId = staticItem.Id,
                StaticItemLevelId = null,
                StaticImageAssetId = imageAsset.Id,
                AssetKind = InferAssetKind(preferred.Candidate),
                MatchedLevel = preferred.Level,
                IsPreferred = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        await WriteReportAsync(outputRoot, groups, matches, visitedPages, cancellationToken);
    }

    private static async Task WriteReportAsync(
        string outputRoot,
        IReadOnlyList<FankitAliasGroup> groups,
        IReadOnlyList<FankitImageMatch> matches,
        IReadOnlyList<string> visitedPages,
        CancellationToken cancellationToken)
    {
        var reportDir = Path.Combine(outputRoot, "_reports");
        Directory.CreateDirectory(reportDir);

        var matchedKeys = matches
            .Select(x => BuildItemKey(x.Group.Section, x.Group.ItemType, x.Group.ItemDataId) + $":{x.Level}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unresolvedRows = groups
            .SelectMany(g => g.Levels.Select(l => new { Group = g, Level = l.Level }))
            .Where(x => !matchedKeys.Contains(BuildItemKey(x.Group.Section, x.Group.ItemType, x.Group.ItemDataId) + $":{x.Level}"))
            .ToList();

        var matchesCsv = new StringBuilder();
        matchesCsv.AppendLine("section,item_type,item_data_id,name,level,score,reason,url,asset_kind");
        foreach (var match in matches.OrderBy(x => x.Group.Name).ThenBy(x => x.Level))
        {
            matchesCsv.AppendLine(string.Join(',', new[]
            {
                Csv(match.Group.Section.ToString()),
                Csv(match.Group.ItemType.ToString()),
                Csv(match.Group.ItemDataId.ToString(CultureInfo.InvariantCulture)),
                Csv(match.Group.Name),
                Csv(match.Level.ToString(CultureInfo.InvariantCulture)),
                Csv(match.Candidate.Score.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(match.Candidate.MatchReason),
                Csv(match.Candidate.Url),
                Csv(InferAssetKind(match.Candidate))
            }));
        }

        var unresolvedCsv = new StringBuilder();
        unresolvedCsv.AppendLine("section,item_type,item_data_id,name,level,aliases,anchor_words");
        foreach (var unresolved in unresolvedRows.OrderBy(x => x.Group.Name).ThenBy(x => x.Level))
        {
            unresolvedCsv.AppendLine(string.Join(',', new[]
            {
                Csv(unresolved.Group.Section.ToString()),
                Csv(unresolved.Group.ItemType.ToString()),
                Csv(unresolved.Group.ItemDataId.ToString(CultureInfo.InvariantCulture)),
                Csv(unresolved.Group.Name),
                Csv(unresolved.Level.ToString(CultureInfo.InvariantCulture)),
                Csv(string.Join(" | ", unresolved.Group.Aliases)),
                Csv(string.Join(" | ", unresolved.Group.AnchorWords))
            }));
        }

        var manifest = new
        {
            visited_pages = visitedPages,
            group_count = groups.Count,
            row_count = groups.Sum(x => x.Levels.Count),
            matched_count = matches.Count,
            unresolved_count = unresolvedRows.Count
        };

        await File.WriteAllTextAsync(Path.Combine(reportDir, "matches_auto.csv"), matchesCsv.ToString(), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(reportDir, "unresolved_items.csv"), unresolvedCsv.ToString(), cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(reportDir, "manifest.json"),
            System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);
    }

    private static IReadOnlyList<FankitImageMatch> MatchGroups(
        IReadOnlyList<FankitAliasGroup> groups,
        IReadOnlyList<FankitAssetCandidate> candidates,
        double threshold,
        bool preferLatest)
    {
        var matches = new List<FankitImageMatch>();

        foreach (var group in groups)
        {
            var groupCandidates = candidates
                .Select(candidate => new { Candidate = candidate, Score = ScoreCandidateForGroup(group, candidate) })
                .Where(x => x.Score.Score >= Math.Max(4d, threshold - 6d))
                .Select(x => x.Candidate with { Score = x.Score.Score, MatchReason = string.Join("; ", x.Score.Reasons) })
                .ToList();

            if (groupCandidates.Count == 0)
                continue;

            foreach (var row in group.Levels)
            {
                var chosen = PickCandidateForLevel(group, groupCandidates, row.Level, preferLatest);
                if (chosen is null)
                    continue;

                if (chosen.Score >= threshold)
                {
                    matches.Add(new FankitImageMatch(group, row.Level, chosen));
                }
            }
        }

        var duplicateUrlThreshold = 3;
        var noisyUrls = matches
            .GroupBy(x => x.Candidate.Url, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(x => BuildItemKey(x.Group.Section, x.Group.ItemType, x.Group.ItemDataId)).Distinct(StringComparer.OrdinalIgnoreCase).Count() > duplicateUrlThreshold)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return matches
            .Where(x => !noisyUrls.Contains(x.Candidate.Url) || string.Equals(x.Candidate.AssetType, "override", StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => new { x.Group.ItemDataId, x.Group.ItemType, x.Group.Section, x.Level, x.Candidate.Url })
            .Select(g => g.OrderByDescending(x => x.Candidate.Score).First())
            .OrderBy(x => x.Group.Name)
            .ThenBy(x => x.Level)
            .ToList();
    }


    private static FankitAssetCandidate? PickCandidateForLevel(
        FankitAliasGroup group,
        IReadOnlyList<FankitAssetCandidate> candidates,
        int level,
        bool preferLatest)
    {
        FankitAssetCandidate? best = null;
        (int Score, int ExactLevel, int DownloadPriority, int CandidateLevel) bestRank = default;
        var hasRank = false;

        foreach (var candidate in candidates)
        {
            var reasons = new List<string>();
            if (!string.IsNullOrWhiteSpace(candidate.MatchReason))
            {
                reasons.Add(candidate.MatchReason);
            }

            var score = candidate.Score;
            var exactLevel = 0;
            var candidateLevel = candidate.CandidateLevel ?? 0;

            if (candidate.CandidateLevel.HasValue)
            {
                if (candidate.CandidateLevel.Value == level)
                {
                    score += 7d;
                    exactLevel = 1;
                    reasons.Add($"level={level}");
                }
                else
                {
                    var distance = Math.Abs(candidate.CandidateLevel.Value - level);
                    score -= Math.Min(4d, distance * 0.75d);
                    reasons.Add($"level_miss={candidate.CandidateLevel.Value}");
                }
            }

            if (preferLatest && candidate.CandidateLevel.HasValue)
            {
                score += candidate.CandidateLevel.Value / 20d;
            }

            var downloadPriority = candidate.AssetType is "download" or "script_asset" or "image" ? 1 : 0;
            var rank = ((int)Math.Round(score * 100d), exactLevel, downloadPriority, candidateLevel);

            if (!hasRank || rank.CompareTo(bestRank) > 0)
            {
                hasRank = true;
                bestRank = rank;
                best = candidate with
                {
                    Score = score,
                    MatchReason = string.Join("; ", reasons.Where(x => !string.IsNullOrWhiteSpace(x))),
                    MatchedLevel = level,
                    MatchedSlug = group.Slug
                };
            }
        }

        return best;
    }


    private static (double Score, List<string> Reasons) ScoreCandidateForGroup(FankitAliasGroup group, FankitAssetCandidate candidate)
    {
        var text = CandidateText(candidate);
        var normalized = NormalizeText(text);
        var tokens = Tokenize(normalized);

        if (tokens.Count == 0)
            return (0d, new List<string>());

        var score = 0d;
        var reasons = new List<string>();
        var strongEvidence = false;

        foreach (var alias in group.Aliases)
        {
            if (!IsUsefulAlias(alias))
                continue;

            var aliasNormalized = NormalizeText(alias);
            if (string.IsNullOrWhiteSpace(aliasNormalized))
                continue;

            var aliasTokens = Tokenize(aliasNormalized);
            if (aliasTokens.Count == 0)
                continue;

            var exactPhrase = normalized.Contains(aliasNormalized, StringComparison.OrdinalIgnoreCase);
            if (exactPhrase)
            {
                score += 12d + Math.Min(aliasTokens.Count * 1.5d, 6d);
                reasons.Add($"exact:{aliasNormalized}");
                strongEvidence = true;
                continue;
            }

            var overlapTokens = aliasTokens.Intersect(tokens, StringComparer.OrdinalIgnoreCase).ToList();
            var overlap = overlapTokens.Count;
            if (overlap >= Math.Min(aliasTokens.Count, 2))
            {
                score += overlap * 4.5d + (overlap / Math.Max(1d, aliasTokens.Count)) * 3d;
                reasons.Add($"tokens:{aliasNormalized}:{overlap}");
                if (overlap >= 2 || aliasTokens.Count == 1 && aliasNormalized.Length >= 5)
                {
                    strongEvidence = true;
                }
            }
        }

        var slugText = group.Slug.Replace('-', ' ');
        if (!string.IsNullOrWhiteSpace(slugText) && normalized.Contains(slugText, StringComparison.OrdinalIgnoreCase))
        {
            score += 4d;
            reasons.Add("slug");
            strongEvidence = true;
        }

        var anchorHits = group.AnchorWords.Intersect(tokens, StringComparer.OrdinalIgnoreCase).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (anchorHits.Count > 0)
        {
            score += 5d * anchorHits.Count;
            reasons.Add("anchor:" + string.Join(',', anchorHits));
            strongEvidence = true;
        }

        switch (candidate.AssetType)
        {
            case "override":
                score += 100d;
                reasons.Add("override");
                strongEvidence = true;
                break;
            case "download":
            case "script_asset":
            case "image":
                score += 1.5d;
                reasons.Add(candidate.AssetType);
                break;
            case "possible_download":
                score += 0.5d;
                reasons.Add(candidate.AssetType);
                break;
            case "meta_image":
                score -= 12d;
                reasons.Add("meta_penalty");
                break;
        }

        if (IsGenericPreviewCandidate(candidate, normalized))
        {
            score -= 8d;
            reasons.Add("generic_preview_penalty");
        }

        if (!strongEvidence)
        {
            score -= 8d;
            reasons.Add("weak_match_penalty");
        }

        return (score, reasons);
    }


    private static string CandidateText(FankitAssetCandidate candidate)
        => string.Join(' ', new[]
        {
            candidate.Label,
            candidate.PageText,
            candidate.Url,
            candidate.DetailPage
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static List<FankitAssetCandidate> DedupeCandidates(IEnumerable<FankitAssetCandidate> candidates)
    {
        var result = new Dictionary<string, FankitAssetCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (!result.TryGetValue(candidate.Url, out var existing) || candidate.PageText.Length > existing.PageText.Length)
            {
                result[candidate.Url] = candidate;
            }
        }

        return result.Values.ToList();
    }

    private static IEnumerable<FankitAssetCandidate> LoadOverrideCandidates(string outputRoot)
    {
        var overridePath = Path.Combine(outputRoot, "_reports", "fankit-image-overrides.csv");
        if (!File.Exists(overridePath))
            yield break;

        using var reader = new StreamReader(overridePath);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            yield break;

        var headers = ParseCsvLine(headerLine);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseCsvLine(line);
            var row = headers.Zip(values, (h, v) => new { h, v }).ToDictionary(x => x.h, x => x.v, StringComparer.OrdinalIgnoreCase);
            var directUrl = row.GetValueOrDefault("direct_image_url")?.Trim();
            if (string.IsNullOrWhiteSpace(directUrl))
                continue;

            var slug = row.GetValueOrDefault("slug")?.Trim() ?? string.Empty;
            var pageUrl = row.GetValueOrDefault("fankit_page_url")?.Trim() ?? directUrl;
            var label = row.GetValueOrDefault("name")?.Trim();
            var aliases = row.GetValueOrDefault("aliases")?.Trim();
            var notes = row.GetValueOrDefault("notes")?.Trim();

            yield return new FankitAssetCandidate(
                directUrl,
                pageUrl,
                pageUrl,
                string.IsNullOrWhiteSpace(label) ? slug : label,
                "override",
                string.Join(' ', new[] { aliases, notes }.Where(x => !string.IsNullOrWhiteSpace(x))),
                ExtractCandidateLevel(string.Join(' ', new[] { label, aliases, notes, directUrl })));
        }
    }

    private static string BuildCategoryPath(string outputRoot, StaticItem item)
        => Path.Combine(outputRoot, item.Section.ToString().ToLowerInvariant(), item.ItemType.ToString().ToLowerInvariant(), Slugify(item.Name));

    private static string InferAssetKind(FankitAssetCandidate candidate)
    {
        var text = CandidateText(candidate).ToLowerInvariant();
        if (text.Contains("icon"))
            return "icon";
        if (text.Contains("portrait"))
            return "portrait";
        if (text.Contains("render") || text.Contains("art"))
            return "render";
        return "preview";
    }

    private static string InferMimeType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".avif" => "image/avif",
            _ => "application/octet-stream"
        };
    }

    private static string BuildItemKey(VillageSection section, ItemType itemType, int itemDataId)
        => $"{section}|{itemType}|{itemDataId}";

    private static List<Dictionary<string, string>> ReadSparseCsvRows(byte[] rawBytes)
    {
        var text = Encoding.UTF8.GetString(rawBytes);
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 3)
            return new List<Dictionary<string, string>>();

        var header = ParseCsvLine(lines[0]);
        var rows = new List<Dictionary<string, string>>();
        var carry = new string[header.Length];

        foreach (var line in lines.Skip(2))
        {
            var values = ParseCsvLine(line);
            if (values.Length < header.Length)
            {
                Array.Resize(ref values, header.Length);
            }

            if (values.Length > header.Length)
            {
                values = values.Take(header.Length).ToArray();
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < header.Length; i++)
            {
                var value = values[i]?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    carry[i] = value;
                }

                row[header[i]] = carry[i] ?? string.Empty;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }

    private static List<string> BuildAliases(Dictionary<string, string> row, string name, ItemType itemType)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            name,
            name.Replace("_", " ", StringComparison.Ordinal),
            name.Replace("-", " ", StringComparison.Ordinal)
        };

        foreach (var field in AliasFields)
        {
            var value = row.GetValueOrDefault(field) ?? string.Empty;
            if (field is "TID" or "InfoTID")
            {
                foreach (var alias in ParseTidAliases(value))
                {
                    aliases.Add(alias);
                }
            }
            else
            {
                foreach (var alias in SplitExportAliases(value))
                {
                    aliases.Add(alias);
                }
            }
        }

        if (itemType == ItemType.Pet)
        {
            var normalizedName = NormalizeText(name);
            if (PetSynonyms.TryGetValue(normalizedName, out var synonyms))
            {
                foreach (var synonym in synonyms)
                {
                    aliases.Add(synonym);
                    aliases.Add(NormalizeText(synonym));
                }
            }
        }

        return NormalizeAliases(aliases);
    }


    private static List<string> ParseTidAliases(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        var cleaned = value.Trim();
        if (cleaned.StartsWith("TID_", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[4..];
        }

        var parts = cleaned.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var filtered = parts.Where(x => x is not ("pet" or "info" or "tid" or "unit" or "hero")).ToArray();
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            value
        };

        if (filtered.Length > 0)
        {
            var joined = string.Join(' ', filtered).ToLowerInvariant();
            aliases.Add(joined);
            aliases.Add(Slugify(joined).Replace('-', ' '));
            aliases.Add(filtered[^1].ToLowerInvariant());
            if (filtered.Length >= 2)
            {
                aliases.Add(string.Join(' ', filtered.Skip(filtered.Length - 2)).ToLowerInvariant());
            }

            aliases.Add(NormalizeText(joined));
        }

        return NormalizeAliases(aliases);
    }


    private static List<string> SplitExportAliases(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            value,
            NormalizeText(value)
        };

        var joined = NormalizeText(value);
        if (!string.IsNullOrWhiteSpace(joined))
        {
            aliases.Add(joined);
            foreach (var prefix in new[] { "icon unit pet ", "unit pet ", "hero art pet ", "info pet ", "tid pet " })
            {
                if (joined.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    aliases.Add(joined[prefix.Length..]);
                }
            }

            foreach (var suffix in new[] { " big", " info", " icon", " swf" })
            {
                if (joined.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    aliases.Add(joined[..^suffix.Length]);
                }
            }
        }

        foreach (var piece in value.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            aliases.Add(piece);
            aliases.Add(NormalizeText(piece));
        }

        return NormalizeAliases(aliases);
    }


    private static List<string> NormalizeAliases(IEnumerable<string> aliases)
    {
        return aliases
            .Where(IsUsefulAlias)
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static bool IsUsefulAlias(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return false;

        var normalized = NormalizeText(alias);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return false;

        if (tokens.Length == 1)
        {
            var token = tokens[0];
            if (token.Length < 3)
                return false;

            if (StopWordSet.Contains(token))
                return false;
        }

        return tokens.Any(x => x.Length >= 3 && !StopWordSet.Contains(x));
    }

    private static bool IsGenericPreviewCandidate(FankitAssetCandidate candidate, string normalizedCandidateText)
    {
        if (!string.Equals(candidate.AssetType, "meta_image", StringComparison.OrdinalIgnoreCase))
            return false;

        var genericTokens = new[] { "game assets", "clash of clans", "supercell", "frontify" };
        return genericTokens.Count(normalizedCandidateText.Contains) >= 2;
    }

    private static List<string> ExtractAnchorWords(string name, IReadOnlyList<string> aliases)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in new[] { name }.Concat(aliases))
        {
            foreach (var token in Tokenize(value).Where(x => x.Length >= 4 && !StopWordSet.Contains(x)))
            {
                if (StrongWordSet.Contains(token) || token.Length >= 5)
                {
                    words.Add(token);
                }
            }
        }

        foreach (var weak in new[] { "spirit", "speedup", "jump", "melee", "ranged", "attacker", "wall", "buster", "healer" })
        {
            words.Remove(weak);
        }

        return words.OrderBy(x => x).ToList();
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return NormalizeText(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !StopWordSet.Contains(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = WebUtility.HtmlDecode(value).Trim();
        text = Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
        text = Regex.Replace(text, "([A-Z])([A-Z][a-z])", "$1 $2");
        text = text.ToLowerInvariant();

        foreach (var rewrite in IdentifierRewrites)
        {
            text = text.Replace(rewrite.Key.ToLowerInvariant(), $" {rewrite.Value[0].ToLowerInvariant()} ", StringComparison.OrdinalIgnoreCase);
        }

        text = text.Replace("&", " and ", StringComparison.Ordinal);
        text = Regex.Replace(text, "[_/\\\\-]+", " ");
        text = Regex.Replace(text, "(?<!\\d)(\\d+)(?!\\d)", " $1 ");
        text = Regex.Replace(text, "[^a-z0-9]+", " ");
        return Regex.Replace(text, "\\s+", " ").Trim();
    }

    private static int? ResolveLevel(Dictionary<string, string> row, IReadOnlyList<string> levelColumns)
    {
        foreach (var column in levelColumns)
        {
            if (TryGetInt(row, column, out var value))
                return value;
        }

        return null;
    }

    private static bool ResolveGenericId(Dictionary<string, string> row, out int itemDataId)
    {
        foreach (var column in new[] { "GlobalId", "GlobalID", "ID" })
        {
            if (TryGetInt(row, column, out itemDataId) && itemDataId > 0)
                return true;
        }

        itemDataId = 0;
        return false;
    }

    private static bool ResolveHelperId(Dictionary<string, string> row, out int itemDataId)
    {
        if (ResolveGenericId(row, out itemDataId))
            return true;

        var rawName = (row.GetValueOrDefault("Name") ?? string.Empty).Trim();
        if (HelperMap.TryGetValue(rawName, out var helper))
        {
            row["Name"] = helper.DisplayName;
            itemDataId = helper.GlobalId;
            return true;
        }

        itemDataId = 0;
        return false;
    }

    private static VillageSection ResolveSectionFromVillageType(Dictionary<string, string> row)
        => VillageTypeResolver.ResolveSection(row.GetValueOrDefault("VillageType"));

    private static ItemType ResolveCharacterItemType(Dictionary<string, string> row)
    {
        var name = (row.GetValueOrDefault("Name") ?? string.Empty).Trim();
        var tid = (row.GetValueOrDefault("TID") ?? string.Empty).Trim();
        var productionBuilding = (row.GetValueOrDefault("ProductionBuilding") ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(tid) && tid.Contains("GUARDIAN", StringComparison.OrdinalIgnoreCase))
            return ItemType.Guardian;

        if (name.StartsWith("Guardian ", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Longshot", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Smasher", StringComparison.OrdinalIgnoreCase))
            return ItemType.Guardian;

        if (!string.IsNullOrWhiteSpace(productionBuilding)
            && productionBuilding.Contains("Workshop", StringComparison.OrdinalIgnoreCase))
            return ItemType.SiegeMachine;

        return ItemType.Troop;
    }

    private static bool TryGetInt(Dictionary<string, string> row, string column, out int value)
        => int.TryParse(row.GetValueOrDefault(column), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static string Slugify(string value)
    {
        var text = WebUtility.HtmlDecode(value).Trim().ToLowerInvariant();
        text = Regex.Replace(text, "[^a-z0-9]+", "-");
        text = Regex.Replace(text, "-+", "-");
        return text.Trim('-');
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
            if (match.Success && int.TryParse(match.Groups[1].Value, out var level))
                return level;
        }

        return null;
    }

    private static string Csv(string? value)
    {
        var safe = value ?? string.Empty;
        if (!safe.Contains(',') && !safe.Contains('"') && !safe.Contains('\n'))
            return safe;

        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private sealed record FankitCsvSource(
        string AssetPath,
        IReadOnlyList<string> LevelColumns,
        TryResolveItemDataIdDelegate TryResolveItemDataId,
        ResolveSectionDelegate ResolveSection,
        ResolveItemTypeDelegate ResolveItemType);

    private sealed record FankitAliasGroup(
        int ItemDataId,
        ItemType ItemType,
        VillageSection Section,
        string Slug,
        string Name,
        List<string> Aliases,
        List<string> AnchorWords)
    {
        public List<FankitAliasRow> Levels { get; init; } = new();
    }

    private sealed record FankitAliasRow(int Level, string Name, List<string> Aliases);

    private sealed record FankitImageMatch(FankitAliasGroup Group, int Level, FankitAssetCandidate Candidate);

    private delegate bool TryResolveItemDataIdDelegate(Dictionary<string, string> row, out int itemDataId);

    private delegate VillageSection ResolveSectionDelegate(Dictionary<string, string> row);

    private delegate ItemType ResolveItemTypeDelegate(Dictionary<string, string> row);
}
