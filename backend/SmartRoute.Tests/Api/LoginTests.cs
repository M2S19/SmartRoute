using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace SmartRoute.Tests.Api;

public sealed class LoginTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private const string LoginUrl = "/api/auth/login";
    private const string ProblemJson = "application/problem+json";

    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokenAndUser()
    {
        // Email avec majuscules et espaces : normalisé côté serveur.
        var response = await _client.PostAsJsonAsync(
            LoginUrl,
            new { email = "  Livreur@SmartRoute.fr ", password = SmartRouteApiFactory.Password },
            Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

        var user = body.GetProperty("user");
        Assert.Equal(SmartRouteApiFactory.DriverEmail, user.GetProperty("email").GetString());
        Assert.Equal("LIVREUR", user.GetProperty("role").GetString());
        Assert.Equal("4092", user.GetProperty("driverCode").GetString());
        Assert.Equal(24, user.GetProperty("id").GetString()!.Length); // ObjectId hexadécimal

        var token = new JsonWebToken(body.GetProperty("token").GetString());
        Assert.Equal(user.GetProperty("id").GetString(), token.Subject);
        Assert.Equal(SmartRouteApiFactory.DriverEmail, token.GetClaim("email").Value);
        Assert.Equal("LIVREUR", token.GetClaim("role").Value);
        Assert.Equal(TimeSpan.FromHours(12), token.ValidTo - token.IssuedAt);
        Assert.Equal(token.ValidTo, body.GetProperty("expiresAt").GetDateTime().ToUniversalTime());
    }

    [Theory]
    [InlineData(SmartRouteApiFactory.DriverEmail, "mauvais-mot-de-passe")]
    [InlineData("inconnu@smartroute.fr", SmartRouteApiFactory.Password)]
    public async Task Login_WithWrongCredentials_Returns401ProblemDetails(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(LoginUrl, new { email, password }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        // Même réponse pour un email inconnu et un mauvais mot de passe.
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(401, problem.GetProperty("status").GetInt32());
        Assert.Equal("INVALID_CREDENTIALS", problem.GetProperty("code").GetString());
        Assert.Equal("Email ou mot de passe incorrect.", problem.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("{}", new[] { "email", "password" })]
    [InlineData("""{"email":"livreur@smartroute.fr"}""", new[] { "password" })]
    [InlineData("""{"password":"x"}""", new[] { "email" })]
    [InlineData("""{"email":"pas-un-email","password":"x"}""", new[] { "email" })]
    public async Task Login_WithMissingOrInvalidFields_Returns400ValidationProblem(string json, string[] invalidFields)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(LoginUrl, content, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("VALIDATION_ERROR", problem.GetProperty("code").GetString());

        var errors = problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Order();
        Assert.Equal(invalidFields.Order(), errors);
    }
}
