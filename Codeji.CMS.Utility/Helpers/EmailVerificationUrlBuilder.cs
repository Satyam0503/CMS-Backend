namespace Codeji.CMS.Utility.Helpers;

/// <summary>Builds the public verification endpoint without relying on deployment URL formatting.</summary>
public static class EmailVerificationUrlBuilder
{
    public static string Build(string apiBaseUrl, string token)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl)) throw new ArgumentException("An API base URL is required.", nameof(apiBaseUrl));
        if (string.IsNullOrEmpty(token)) throw new ArgumentException("A verification token is required.", nameof(token));

        var baseUrl = apiBaseUrl.Trim().TrimEnd('/');
        var path = baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase)
            ? "/account/verify-email"
            : "/api/account/verify-email";

        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }
}
