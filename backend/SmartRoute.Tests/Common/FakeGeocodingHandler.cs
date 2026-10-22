using System.Net;
using System.Net.Http.Json;

namespace SmartRoute.Tests.Common;

/// <summary>
/// Remplace l'appel HTTP de GeocodingService à la Géoplateforme (data.geopf.fr) :
/// réponses déterministes, sans Internet. GeocodingService lui-même n'est pas modifié.
/// - adresse connue (<see cref="KnownAddresses"/>) → une « feature » avec ses coordonnées ;
/// - adresse inconnue → aucune « feature » (le service lève « Adresse introuvable ») ;
/// - adresse contenant <see cref="OutageKeyword"/> → HTTP 503 (géocodage indisponible).
/// </summary>
public sealed class FakeGeocodingHandler : HttpMessageHandler
{
    public const string OutageKeyword = "panne-geocodage";

    /// <summary>Points alignés d'ouest en est (latitude fixe), longitudes en degrés.</summary>
    public static readonly IReadOnlyDictionary<string, (double Latitude, double Longitude)> KnownAddresses =
        new Dictionary<string, (double, double)>
        {
            ["Depot Mulhouse"] = (47.75, 7.30),
            ["Arret A"] = (47.75, 7.31),
            ["Arret B"] = (47.75, 7.33),
            ["Arret C"] = (47.75, 7.32),
            // Affectation (US2) : dépôt au sud, hors de la ligne A–C–B, pour qu'une seule insertion soit la meilleure.
            ["Depot Sud"] = (47.74, 7.30),
            // Sur le segment A → C : insertion attendue en 2e position dans la tournée Dépôt Sud → A → C → B.
            ["Nouvelle livraison"] = (47.75, 7.315),
        };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var query = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;
        var address = query
            .Split('&')
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2 && pair[0] == "q")
            .Select(pair => Uri.UnescapeDataString(pair[1]))
            .FirstOrDefault() ?? string.Empty;

        if (address.Contains(OutageKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        // Même forme que la réponse GeoJSON de la Géoplateforme : coordinates = [longitude, latitude].
        object[] features = KnownAddresses.TryGetValue(address, out var point)
            ? [new { type = "Feature", geometry = new { type = "Point", coordinates = new[] { point.Longitude, point.Latitude } } }]
            : [];

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { type = "FeatureCollection", features }),
        });
    }
}
