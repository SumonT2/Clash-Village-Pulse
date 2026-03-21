using System.Net.Http.Headers;
using System.Text.Json;
using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClashVillagePulse.Infrastructure.Services;

public sealed class ClashApiService : IClashApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _token;

    public ClashApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        _baseUrl = configuration["ClashApi:BaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Missing ClashApi:BaseUrl configuration.");

        _token = configuration["ClashApi:Token"]
            ?? throw new InvalidOperationException("Missing ClashApi:Token configuration.");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task<PlayerProfileDto> GetPlayerProfileAsync(
        string playerTag,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playerTag))
            throw new ArgumentException("Player tag is required.", nameof(playerTag));

        Console.WriteLine($"[ClashApi] Start GetPlayerProfileAsync");
        Console.WriteLine($"[ClashApi] Raw playerTag: {playerTag}");

        string normalizedTag = NormalizeTag(playerTag);
        string encodedTag = Uri.EscapeDataString(normalizedTag);

        Console.WriteLine($"[ClashApi] Normalized tag: {normalizedTag}");
        Console.WriteLine($"[ClashApi] Encoded tag: {encodedTag}");
        Console.WriteLine($"[ClashApi] Request URL: {_baseUrl}/players/{encodedTag}");

        using var response = await _httpClient.GetAsync(
            $"{_baseUrl}/players/{encodedTag}",
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        Console.WriteLine($"[ClashApi] Status Code: {(int)response.StatusCode}");
        Console.WriteLine($"[ClashApi] Response Body: {responseBody}");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to fetch player profile for {normalizedTag}. " +
                $"Status: {(int)response.StatusCode}. Response: {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        string? clanTag = null;
        string? clanName = null;

        if (root.TryGetProperty("clan", out var clanElement) &&
            clanElement.ValueKind == JsonValueKind.Object)
        {
            clanTag = TryGetString(clanElement, "tag");
            clanName = TryGetString(clanElement, "name");
        }

        return new PlayerProfileDto
        {
            PlayerTag = TryGetString(root, "tag") ?? normalizedTag,
            PlayerName = TryGetString(root, "name"),
            ClanTag = clanTag,
            ClanName = clanName,
            TownHallLevel = TryGetInt(root, "townHallLevel"),
            BuilderHallLevel = TryGetInt(root, "builderHallLevel")
        };
    }

    private static string NormalizeTag(string tag)
    {
        string value = tag.Trim().ToUpperInvariant();
        if (!value.StartsWith("#"))
            value = "#" + value;
        return value;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim()
            : null;
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : null;
    }
}