using System.Net;
using System.Net.Http.Json;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public sealed class ForwardedRateLimitTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    [Fact]
    public async Task AccountLimiter_UsesDistinctTrustedForwardedClientIps()
    {
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();
        const string firstIp = "198.51.100.41";
        const string secondIp = "198.51.100.42";

        for (var attempt = 0; attempt < 8; attempt++)
        {
            using var request = LoginRequest(firstIp);
            await firstClient.SendAsync(request);
        }

        using var throttledRequest = LoginRequest(firstIp);
        var throttled = await firstClient.SendAsync(throttledRequest);
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

        using var independentRequest = LoginRequest(secondIp);
        var independent = await secondClient.SendAsync(independentRequest);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, independent.StatusCode);
    }

    private static HttpRequestMessage LoginRequest(string forwardedClientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/account/login")
        {
            Content = JsonContent.Create(new { email = "rate-limit@test.invalid", password = "invalid" })
        };
        request.Headers.Add("X-Forwarded-For", forwardedClientIp);
        return request;
    }
}
