namespace Codeji.CMS.DTO.Attendance;

public sealed class BiometricPunchDto
{
    public required string DeviceId { get; set; }
    public required string EmployeeId { get; set; }
    public required DateTime TimestampUtc { get; set; }
    public string PunchType { get; set; } = "CHECK_IN"; // "CHECK_IN", "CHECK_OUT", "AUTO"
    public string? VerifyMode { get; set; } // "FINGERPRINT", "FACE", "CARD", "PASSWORD"
    public string? TerminalIp { get; set; }
}

public sealed class BiometricSyncRequestDto
{
    public required string DeviceId { get; set; }
    public string? DeviceApiKey { get; set; }
    public List<BiometricPunchDto> Punches { get; set; } = [];
}

public sealed class BiometricSyncResultDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public int TotalReceived { get; set; }
    public int ProcessedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int UnmatchedEmployeeCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
