using Codeji.CMS.Services.CareerPortal;
using System.Collections.Generic;

namespace Codeji.CMS.Services.Tests;

public sealed class PublicCompanyProfileServiceTests
{
    [Fact]
    public void NormalizeSocialLinks_RejectsEmptyOrDuplicateSanitizedKeys()
    {
        var links = PublicCompanyProfileService.NormalizeSocialLinks(new[]
        {
            new KeyValuePair<string, string>("LinkedIn", "https://example.com"),
            new KeyValuePair<string, string>("  ", "https://example.com/2"),
            new KeyValuePair<string, string>("linkedin", "https://example.com/3")
        });

        Assert.False(links.Success);
        Assert.Equal("Social links contain invalid or duplicate keys.", links.Message);
    }

    [Fact]
    public void NormalizeSocialLinks_AcceptsUniqueKeysAndTrimsValues()
    {
        var links = PublicCompanyProfileService.NormalizeSocialLinks(new[]
        {
            new KeyValuePair<string, string>("LinkedIn", "https://example.com"),
            new KeyValuePair<string, string>("GitHub", "https://github.com")
        });

        Assert.True(links.Success);
        Assert.Equal("https://example.com", links.Data!["LinkedIn"]);
        Assert.Equal("https://github.com", links.Data["GitHub"]);
    }

    [Fact]
    public void NormalizeCode_StripsNonDigitsAndHandlesEmptyValues()
    {
        Assert.Equal("123456", PublicCompanyProfileService.NormalizeCode("AB-12-34-56"));
        Assert.Equal(string.Empty, PublicCompanyProfileService.NormalizeCode(null));
    }
}
