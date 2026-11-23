using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.API.App_Start;

/// <summary>Recognizes resume paths that must never be served by the public upload middleware.</summary>
public static class RecruitmentResumeStaticFileGuard
{
    public static bool IsPrivateResumePath(PathString requestPath)
    {
        string path = requestPath.Value ?? string.Empty;
        bool fullyDecoded = false;
        for (int attempt = 0; attempt < 32; attempt++)
        {
            string decoded = Uri.UnescapeDataString(path);
            if (string.Equals(decoded, path, StringComparison.Ordinal))
            {
                fullyDecoded = true;
                break;
            }
            path = decoded;
        }

        // If nested escaping did not converge within the safety bound, fail closed.
        if (!fullyDecoded) return true;
        path = path.Replace('\\', '/');
        var segments = new List<string>();
        foreach (string segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".") continue;
            if (segment == "..")
            {
                if (segments.Count > 0) segments.RemoveAt(segments.Count - 1);
                continue;
            }
            segments.Add(segment);
        }

        return segments.Count >= 2 &&
               string.Equals(segments[0], "fs", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(segments[1], "Resume", StringComparison.OrdinalIgnoreCase);
    }
}
