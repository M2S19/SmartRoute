using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartRoute.Tests.Common;

/// <summary>Connexion des comptes de test de <see cref="SmartRouteApiFactory"/>.</summary>
public static class TestAuth
{
    /// <summary>Se connecte via POST /api/auth/login et ajoute le Bearer au client.</summary>
    public static async Task AuthenticateAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = SmartRouteApiFactory.Password },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
    }
}
