using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services.Tests;

public class EmailVerificationUrlBuilderTests
{
    [Theory]
    [InlineData("https://depapp.codeji.in", "https://depapp.codeji.in/api/account/verify-email?token=a%2Bb%2Fc%3D")]
    [InlineData("https://depapp.codeji.in/", "https://depapp.codeji.in/api/account/verify-email?token=a%2Bb%2Fc%3D")]
    [InlineData("http://localhost:5036/", "http://localhost:5036/api/account/verify-email?token=a%2Bb%2Fc%3D")]
    [InlineData("https://depapp.codeji.in/api/", "https://depapp.codeji.in/api/account/verify-email?token=a%2Bb%2Fc%3D")]
    public void Build_NormalizesTheEndpointAndEncodesTheToken(string baseUrl, string expected)
    {
        Assert.Equal(expected, EmailVerificationUrlBuilder.Build(baseUrl, "a+b/c="));
    }
}
