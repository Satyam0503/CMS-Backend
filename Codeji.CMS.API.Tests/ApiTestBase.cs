using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Codeji.CMS.API.Tests;

public abstract class ApiTestBase : IClassFixture<TestWebAppFactory>
{
    protected readonly HttpClient Client;
    protected readonly TestWebAppFactory Factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected ApiTestBase(TestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<string> GetAuthTokenAsync(
        string email = "admin@test.codeji.in",
        string password = "Test@1234")
    {
        var response = await Client.PostAsJsonAsync("api/account/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return body?.MethodResult?.Token ?? throw new Exception("Login failed — no token returned");
    }

    protected void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<HttpResponseMessage> AuthenticatedGetAsync(string url)
    {
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        return await Client.GetAsync(url);
    }

    protected async Task<HttpResponseMessage> AuthenticatedPostAsync<T>(string url, T payload)
    {
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);
        return await Client.PostAsJsonAsync(url, payload);
    }

    protected async Task<HttpResponseMessage> UnauthenticatedGetAsync(string url)
    {
        Client.DefaultRequestHeaders.Authorization = null;
        return await Client.GetAsync(url);
    }

    private record LoginResponse(bool Success, LoginMethodResult? MethodResult);
    private record LoginMethodResult(string Token, string RefreshToken);
}
