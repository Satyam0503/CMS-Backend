namespace Codeji.CMS.Services.Recruitments;

public static class RecruitmentOfferRules
{
    public static bool IsAllowedTransition(string? from, string? to)
    {
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase)) return true;
        return (from, to) switch
        {
            ("Draft", "PendingApproval") => true,
            ("PendingApproval", "Approved") => true,
            ("PendingApproval", "Withdrawn") => true,
            ("Approved", "Sent") => true,
            ("Approved", "Withdrawn") => true,
            ("Sent", "Accepted") => true,
            ("Sent", "Declined") => true,
            ("Sent", "Expired") => true,
            ("Sent", "Withdrawn") => true,
            _ => false
        };
    }

    public static bool IsValidCompensation(decimal salary, string? currency)
        => salary >= 0m && !string.IsNullOrWhiteSpace(currency) && currency.Trim().Length is >= 3 and <= 10;
}
