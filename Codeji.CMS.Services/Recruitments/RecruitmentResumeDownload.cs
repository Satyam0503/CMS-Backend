namespace Codeji.CMS.Services.Recruitments;

public sealed record RecruitmentResumeDownload(byte[] Content, string FileName);

public static class RecruitmentResumeVerdictRules
{
    public static bool Matches(string? storedSha256, string? actualSha256) =>
        !string.IsNullOrWhiteSpace(storedSha256) &&
        string.Equals(storedSha256, actualSha256, StringComparison.OrdinalIgnoreCase);

    public static bool IsCachedClean(string? verdict, string? storedSha256, string? actualSha256, DateTime? scannedAtUtc, DateTime nowUtc) =>
        string.Equals(verdict, nameof(ResumeScanStatus.Clean), StringComparison.Ordinal) &&
        Matches(storedSha256, actualSha256) && scannedAtUtc is DateTime scannedAt && scannedAt >= nowUtc.AddHours(-24);

    public static bool IsCachedInfected(string? verdict, string? storedSha256, string? actualSha256) =>
        string.Equals(verdict, nameof(ResumeScanStatus.Infected), StringComparison.Ordinal) &&
        Matches(storedSha256, actualSha256);
}
