using System.Net;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public class SecurityHeaderTests : ApiTestBase
{
    public SecurityHeaderTests(TestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Response_HasStrictTransportSecurity()
    {
        var response = await Client.GetAsync("api/account/login");
        Assert.True(
            response.Headers.Contains("Strict-Transport-Security"),
            "Missing Strict-Transport-Security header");
    }

    [Fact]
    public async Task Response_HasXContentTypeOptions()
    {
        var response = await Client.GetAsync("api/account/login");
        Assert.True(
            response.Headers.Contains("x-content-type-options"),
            "Missing X-Content-Type-Options header");
        var value = response.Headers.GetValues("x-content-type-options").First();
        Assert.Equal("nosniff", value);
    }

    [Fact]
    public async Task Response_HasXFrameOptions()
    {
        var response = await Client.GetAsync("api/account/login");
        Assert.True(
            response.Headers.Contains("x-frame-options"),
            "Missing X-Frame-Options header");
        var value = response.Headers.GetValues("x-frame-options").First();
        Assert.Equal("DENY", value);
    }

    [Fact]
    public async Task Response_HasContentSecurityPolicy()
    {
        var response = await Client.GetAsync("api/account/login");
        Assert.True(
            response.Headers.Contains("Content-Security-Policy") ||
            response.Content.Headers.Contains("Content-Security-Policy"),
            "Missing Content-Security-Policy header");
    }

    [Fact]
    public async Task Response_HasReferrerPolicy()
    {
        var response = await Client.GetAsync("api/account/login");
        Assert.True(
            response.Headers.Contains("referrer-policy"),
            "Missing Referrer-Policy header");
    }

    [Fact]
    public async Task Response_DoesNotExposeServerHeader()
    {
        var response = await Client.GetAsync("api/account/login");
        Assert.False(
            response.Headers.Contains("Server"),
            "Server header should be removed");
        Assert.False(
            response.Headers.Contains("X-Powered-By"),
            "X-Powered-By header should be removed");
    }

    [Fact]
    public async Task Response_HasXPermittedCrossDomainPolicies()
    {
        var response = await Client.GetAsync("api/account/login");
        Assert.True(
            response.Headers.Contains("X-Permitted-Cross-Domain-Policies"),
            "Missing X-Permitted-Cross-Domain-Policies header");
        var value = response.Headers.GetValues("X-Permitted-Cross-Domain-Policies").First();
        Assert.Equal("none", value);
    }
}
