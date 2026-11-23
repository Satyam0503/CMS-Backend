namespace Codeji.CMS.API.App_Start;

/// <summary>Validates browser origins against the application's explicit cross-origin allow-list.</summary>
public static class CorsOriginPolicy
{
    public static bool IsAllowed(string? origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
            return false;

        return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.EndsWith(".codeji.in", StringComparison.OrdinalIgnoreCase);
    }
}
