using System.Net;
using System.Net.Http.Json;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Integration")]
public class AuthEndpointTests : ApiTestBase
{
    public AuthEndpointTests(TestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_WithMissingFields_Returns400()
    {
        var response = await Client.PostAsJsonAsync("api/account/login", new { });
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400/422, got {response.StatusCode}");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var response = await Client.PostAsJsonAsync("api/account/login", new
        {
            email = "nonexistent@test.com",
            password = "WrongPass123!"
        });
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 401/400, got {response.StatusCode}");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Rejects()
    {
        var response = await Client.PostAsJsonAsync("api/account/refresh-token", new
        {
            refreshToken = "invalid-refresh-token-value"
        });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await UnauthenticatedGetAsync("api/User/GetAllEmployees");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "expired.jwt.token");
        var response = await Client.GetAsync("api/User/GetAllEmployees");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_Returns401()
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");
        var response = await Client.GetAsync("api/User/GetAllEmployees");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
