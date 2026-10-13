using System;
using System.Text.Json;
namespace SmartRoute.Api.Services;

public sealed class GeocodingService
{
    private readonly HttpClient _httpClient;

    public GeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(double Latitude, double Longitude)> GeocodeAsync(string address)
    {
        string encodedAddress = Uri.EscapeDataString(address);

        string url =
            $"https://data.geopf.fr/geocodage/search?q={encodedAddress}";

        HttpResponseMessage response =
            await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                "Erreur lors du géocodage de l'adresse.");
        }

        string json =
            await response.Content.ReadAsStringAsync();

        using JsonDocument document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;
        JsonElement features = root.GetProperty("features");
        if (features.GetArrayLength() == 0)
        {
            throw new Exception("Adresse introuvable.");
        }
        JsonElement firstFeature = features[0];
        JsonElement geometry = firstFeature.GetProperty("geometry");
        JsonElement coordinates = geometry.GetProperty("coordinates");

        double longitude = coordinates[0].GetDouble();
        double latitude = coordinates[1].GetDouble();

        return (latitude, longitude);
    }
}