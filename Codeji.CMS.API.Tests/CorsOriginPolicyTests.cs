using Codeji.CMS.API.App_Start;

namespace Codeji.CMS.API.Tests;

public sealed class CorsOriginPolicyTests
{
    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("https://app.codeji.in", true)]
    [InlineData("https://hr.codeji.in", true)]
    [InlineData("null", false)]
    [InlineData("not an origin", false)]
    [InlineData("https://attacker.codeji.in.evil.com", false)]
    [InlineData("https://notcodeji.in", false)]
    [InlineData("https://codeji.in", false)]
    [InlineData("javascript://localhost", false)]
    [InlineData("https://user@localhost", false)]
    [InlineData("https://localhost/path", false)]
    public void Origin_allow_list_rejects_invalid_and_lookalike_origins(string origin, bool expected)
    {
        Assert.Equal(expected, CorsOriginPolicy.IsAllowed(origin));
    }

    [Fact]
    public void Origin_allow_list_rejects_null_input()
    {
        Assert.False(CorsOriginPolicy.IsAllowed(null));
    }
}
