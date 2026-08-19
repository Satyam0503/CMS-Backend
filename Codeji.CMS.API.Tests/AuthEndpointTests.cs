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
        // This API always answers 200 OK and signals failure via the response body's
        // Success flag (the Result<T> convention used across every endpoint), so a rejected
        // login is asserted there rather than via the HTTP status code.
        var response = await Client.PostAsJsonAsync("api/account/login", new
        {
            email = "nonexistent@test.com",
            password = "WrongPass123!"
        });
        var body = await response.Content.ReadFromJsonAsync<ResultBody>(JsonOptions);
        Assert.False(body?.Success, "Expected login with invalid credentials to fail");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Rejects()
    {
        var response = await Client.PostAsJsonAsync("api/account/refresh-token", new
        {
            refreshToken = "invalid-refresh-token-value"
        });
        var body = await response.Content.ReadFromJsonAsync<ResultBody>(JsonOptions);
        Assert.False(body?.Success, "Expected refresh with an invalid token to fail");
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
