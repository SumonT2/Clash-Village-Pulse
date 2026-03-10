using System.Net.Http;

namespace ClashVillagePulse.Infrastructure.StaticData;

public class StaticDataDownloader
{
    private readonly HttpClient _http;

    public StaticDataDownloader(HttpClient http)
    {
        _http = http;
    }

    public async Task<byte[]> DownloadAsync(string url)
    {
        return await _http.GetByteArrayAsync(url);
    }
}