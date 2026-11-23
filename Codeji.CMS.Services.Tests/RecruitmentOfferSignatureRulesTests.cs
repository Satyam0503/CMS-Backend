using Codeji.CMS.Services.Recruitments;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentOfferSignatureRulesTests
{
    [Theory]
    [InlineData("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4////fwAJ+wP9KobjigAAAABJRU5ErkJggg==", true)]
    [InlineData("data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=", false)]
    [InlineData("data:image/png;base64,not-base64", false)]
    [InlineData("data:image/png;base64,AAAA", false)]
    [InlineData("", false)]
    public void Only_bounded_png_signature_data_is_accepted(string dataUrl, bool expected)
    {
        Assert.Equal(expected, RecruitmentOfferSignatureRules.IsValidPngDataUrl(dataUrl));
    }
}
