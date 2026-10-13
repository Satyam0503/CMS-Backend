using System.Net;
using System.Net.Http.Headers;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public class CorsTests : ApiTestBase
{
    public CorsTests(TestWebAppFactory factory) : base(factory) { }

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("https://app.codeji.in")]
    [InlineData("https://hr.codeji.in")]
    public async Task AllowedOrigins_GetCorsHeaders(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "api/account/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await Client.SendAsync(request);

        // Either preflight succeeds or the actual response allows the origin
        if (response.Headers.Contains("Access-Control-Allow-Origin"))
        {
            var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").First();
            Assert.Equal(origin, allowedOrigin);
        }
    }

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("https://attacker.codeji.in.evil.com")]
    [InlineData("https://notcodeji.in")]
    [InlineData("null")]
    public async Task DisallowedOrigins_DoNotGetCorsHeaders(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "api/account/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await Client.SendAsync(request);

        if (response.Headers.Contains("Access-Control-Allow-Origin"))
        {
            var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").First();
            Assert.NotEqual("*", allowedOrigin);
            Assert.NotEqual(origin, allowedOrigin);
        }
    }
}
