namespace Codeji.CMS.Services.Recruitments;

public static class RecruitmentInterviewRules
{
    public static bool IsValidSchedule(DateTime startsAtUtc, DateTime endsAtUtc)
        => startsAtUtc.Kind == DateTimeKind.Utc && endsAtUtc.Kind == DateTimeKind.Utc && endsAtUtc > startsAtUtc;

    public static bool IsAllowedTransition(string? from, string? to)
    {
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase)) return true;
        return (from, to) switch
        {
            ("Draft", "Scheduled") => true,
            ("Scheduled", "Confirmed") => true,
            ("Scheduled", "Cancelled") => true,
            ("Confirmed", "Cancelled") => true,
            ("Confirmed", "Completed") => true,
            ("Scheduled", "Completed") => true,
            _ => false
        };
    }
}
