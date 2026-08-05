namespace Codeji.CMS.Services.Attendance;

/// <summary>Single compatibility rule for replaceable system-created Present placeholders.</summary>
public static class AttendanceSourceTransitionPolicy
{
    public const string DefaultPresent = "SYSTEM_DEFAULT_PRESENT";
    public static bool IsReplaceableDefaultPresent(string? sourceType) =>
        string.Equals(sourceType, DefaultPresent, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sourceType, "SYSTEM_OFFICE_START_PRESENT", StringComparison.OrdinalIgnoreCase);
}
