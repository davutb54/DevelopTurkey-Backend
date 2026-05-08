using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Core.Entities.Constants;
using Entities.DTOs.Geo;

namespace WebAPI.Services;

public class GeoLocationService : IGeoLocationService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GeoLocationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ReverseGeocodeCityDto?> ReverseGeocodeCityAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Nominatim usage: identify the application via User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DevelopTurkey/1.0 (reverse-geocode)");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en-US;q=0.7,en;q=0.6");

        var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&zoom=10&addressdetails=1";

        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!doc.RootElement.TryGetProperty("address", out var addressEl))
        {
            return null;
        }

        // Turkey province typically comes as 'state'. Fallbacks are included.
        string? state = TryGetString(addressEl, "state")
                        ?? TryGetString(addressEl, "province")
                        ?? TryGetString(addressEl, "region")
                        ?? TryGetString(addressEl, "city");

        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        var (cityCode, cityName) = MapCityNameToCityCode(state);
        if (cityCode <= 0)
        {
            return null;
        }

        string? displayName = TryGetString(doc.RootElement, "display_name");

        return new ReverseGeocodeCityDto
        {
            CityCode = cityCode,
            CityName = cityName,
            ResolvedAddress = displayName
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static (int CityCode, string CityName) MapCityNameToCityCode(string raw)
    {
        var normalized = NormalizeCityName(raw);

        // Common alias fixes
        if (normalized == "afyonkarahisar") normalized = "afyon";

        // ConstantData has a trailing space for Mersin; NormalizeCityName trims so it's OK.

        var match = ConstantData.Cities
            .Where(c => c.Value > 0)
            .Select(c => new
            {
                c.Value,
                c.Text,
                Normalized = NormalizeCityName(c.Text)
            })
            .FirstOrDefault(c => c.Normalized == normalized);

        if (match != null)
        {
            return (match.Value, match.Text.Trim());
        }

        // Fallback: try prefix match (e.g., if provider adds suffix)
        match = ConstantData.Cities
            .Where(c => c.Value > 0)
            .Select(c => new
            {
                c.Value,
                c.Text,
                Normalized = NormalizeCityName(c.Text)
            })
            .FirstOrDefault(c => normalized.StartsWith(c.Normalized) || c.Normalized.StartsWith(normalized));

        return match != null ? (match.Value, match.Text.Trim()) : (0, string.Empty);
    }

    private static string NormalizeCityName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var value = input.Trim();

        // Normalize casing (Turkish)
        value = value.ToLower(new CultureInfo("tr-TR"));

        // Replace Turkish characters for stable comparisons
        value = value
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');

        // Remove punctuation and normalize spaces
        var chars = value.Where(ch => char.IsLetter(ch) || char.IsWhiteSpace(ch)).ToArray();
        value = new string(chars);
        value = string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return value;
    }
}
