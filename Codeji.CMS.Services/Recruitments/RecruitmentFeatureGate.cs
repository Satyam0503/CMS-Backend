using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Company-scoped server-side checks for optional recruitment capabilities.</summary>
public static class RecruitmentFeatureGate
{
    public static async Task<Result<T>?> Require<T>(
        IMongoDbRepository<RecruitmentEnterpriseConfig>? configs,
        string companyId,
        Func<RecruitmentEnterpriseConfig, bool> isEnabled,
        string featureName)
    {
        var config = configs is null
            ? null
            : await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);

        // Historical documents may contain only one of the old/current alias fields.
        // Apply the same normalization used by the settings read path before evaluating gates.
        if (config is not null)
            RecruitmentEnterpriseConfigRules.NormalizeHistoricalSwitches(config);

        if (config is not null && isEnabled(config)) return null;

        return new Result<T>
        {
            Success = false,
            StatusCode = 403,
            ErrorCode = RecruitmentErrorCodes.FeatureDisabled,
            Message = $"{featureName} is not enabled for this company. Contact your recruitment administrator."
        };
    }
}
