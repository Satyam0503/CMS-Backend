using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Employees;

public static class EmployeeContractLifecycleRules
{
    public static string? Validate(string action, EmployeeContractLifecycle contract, DateTime? startDate, DateTime? endDate, bool hasActiveExit)
    {
        if (string.Equals(action, "Extend", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(contract.Status, "Ended", StringComparison.OrdinalIgnoreCase)) return "CONTRACT_ALREADY_ENDED";
            if (!endDate.HasValue) return "CONTRACT_EXTENSION_DATE_INVALID";
            if (contract.StartDate.HasValue && endDate.Value.Date < contract.StartDate.Value.Date) return "CONTRACT_EXTENSION_DATE_INVALID";
            if (contract.EndDate.HasValue && endDate.Value.Date <= contract.EndDate.Value.Date) return "CONTRACT_EXTENSION_DATE_INVALID";
            return null;
        }

        if (string.Equals(action, "End", StringComparison.OrdinalIgnoreCase))
        {
            if (endDate.HasValue && contract.StartDate.HasValue && endDate.Value.Date < contract.StartDate.Value.Date) return "CONTRACT_END_DATE_INVALID";
            if (hasActiveExit) return "ACTIVE_EXIT_ALREADY_EXISTS";
            return null;
        }

        return "CONTRACT_ACTION_INVALID";
    }
}
