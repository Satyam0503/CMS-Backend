namespace PayrollReadOnlyValidation;

// Explicit audited routes only. GET is not globally safe: legacy payroll generation uses GET.
public static class ReadOnlyRequestPolicy
{
    private static readonly HashSet<string> ReadPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/payroll/configuration/components",
        "/api/payroll/configuration/pay-groups",
        "/api/payroll/configuration/assignments",
        "/api/payroll/configuration/policies",
        "/api/payroll/configuration/salary-templates",
        "/api/payroll/divisor-policy"
    };

    public static bool IsAllowed(string method, string path)
    {
        path = path.TrimEnd('/');
        if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)) return ReadPaths.Contains(path);
        // Normal sign-in was explicitly approved. Signed-user lookup is a read-only POST.
        return string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(path, "/api/account/login", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(path, "/api/account/getSignedUserDetails", StringComparison.OrdinalIgnoreCase));
    }
}
