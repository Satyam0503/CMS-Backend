namespace Codeji.CMS.Services.Recruitments;

public static class RecruitmentCandidateOfferLink
{
    public static string? Build(string? portalRoot, string? rawActionToken)
    {
        if (string.IsNullOrWhiteSpace(rawActionToken) ||
            !Uri.TryCreate(portalRoot?.Trim(), UriKind.Absolute, out var portalUri) ||
            !string.IsNullOrEmpty(portalUri.UserInfo) || !string.IsNullOrEmpty(portalUri.Query) || !string.IsNullOrEmpty(portalUri.Fragment) ||
            (portalUri.Scheme != Uri.UriSchemeHttps && !(portalUri.IsLoopback && portalUri.Scheme == Uri.UriSchemeHttp)))
            return null;

        return $"{portalRoot!.Trim().TrimEnd('/')}/candidate/offer?token={Uri.EscapeDataString(rawActionToken.Trim())}";
    }
}
