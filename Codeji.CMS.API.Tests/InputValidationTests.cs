using System.Net;
using System.Net.Http.Json;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Integration")]
public class InputValidationTests : ApiTestBase
{
    public InputValidationTests(TestWebAppFactory factory) : base(factory) { }

    [Theory]
    [InlineData("api/account/login")]
    [InlineData("api/account/register")]
    public async Task Endpoint_RejectsEmptyBody(string endpoint)
    {
        var response = await Client.PostAsJsonAsync(endpoint, new { });
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.Unauthorized,
            $"{endpoint} accepted empty body with status {response.StatusCode}");
    }

    [Fact]
    public async Task Login_RejectsOversizedPayload()
    {
        // Login always answers 200 OK and signals failure via the response body's Success
        // flag (the Result<T> convention used across every endpoint), so a rejected login
        // is asserted there rather than via the HTTP status code.
        var oversizedEmail = new string('a', 10_000) + "@test.com";
        var response = await Client.PostAsJsonAsync("api/account/login", new
        {
            email = oversizedEmail,
            password = "x"
        });
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.UnprocessableEntity or HttpStatusCode.Unauthorized)
        {
            return;
        }
        var body = await response.Content.ReadFromJsonAsync<ResultBody>(JsonOptions);
        Assert.False(body?.Success, $"Oversized payload accepted with {response.StatusCode}");
    }

    [Theory]
    [InlineData("{'$gt': ''}")]
    [InlineData("{\"$ne\": null}")]
    [InlineData("admin'; db.users.drop();--")]
    public async Task Login_RejectsNoSQLInjectionPayloads(string maliciousInput)
    {
        var response = await Client.PostAsJsonAsync("api/account/login", new
        {
            email = maliciousInput,
            password = maliciousInput
        });
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.UnprocessableEntity,
            $"NoSQL injection payload accepted with {response.StatusCode}");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("\" onclick=\"alert(1)")]
    public async Task Login_DoesNotReflectXSSPayloads(string xssPayload)
    {
        var response = await Client.PostAsJsonAsync("api/account/login", new
        {
            email = xssPayload,
            password = "test"
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script>", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick=", body, StringComparison.OrdinalIgnoreCase);
    }
}
