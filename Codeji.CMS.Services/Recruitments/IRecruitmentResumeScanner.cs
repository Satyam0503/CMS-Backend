namespace Codeji.CMS.Services.Recruitments;

public enum ResumeScanStatus
{
    Clean,
    Infected,
    Unavailable
}

public sealed record ResumeScanResult(ResumeScanStatus Status, string? Sha256 = null);

public interface IRecruitmentResumeScanner
{
    /// <summary>Whether the scanner has enough configuration to attempt a scan.</summary>
    bool IsConfigured => true;

    /// <summary>Returns true/false when reachability was checked, or null when this scanner cannot probe.</summary>
    Task<bool?> ProbeAsync(CancellationToken cancellationToken = default) => Task.FromResult<bool?>(null);

    Task<ResumeScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}
