using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

public interface IPayrollDivisorPolicyService
{
    Task<PayrollDivisorPolicyDto> GetEffective(string companyId, DateTime month);
    Task<Result> Save(string companyId, string userId, PayrollDivisorPolicyDto dto);
}

public sealed class PayrollDivisorPolicyService(IMongoDbRepository<PayrollDivisorPolicy> repository) : IPayrollDivisorPolicyService
{
    public async Task<PayrollDivisorPolicyDto> GetEffective(string companyId, DateTime month)
    {
        var date = new DateTime(month.Year, month.Month, 1);
        var item = (await repository.GetAll(x => x.CompanyId == companyId && x.IsActive && x.EffectiveFrom <= date && (!x.EffectiveTo.HasValue || x.EffectiveTo >= date))).OrderByDescending(x => x.Version).FirstOrDefault();
        return item == null
            ? new PayrollDivisorPolicyDto
            {
                PayrollMode = "Standard",
                DivisorPolicy = "CALENDAR_DAYS",
                LopCalculationBase = "FIXED_GROSS",
                EpfMode = "CAPPED_CEILING",
                EpfWageCeiling = 15000m,
                EpfEmployeeRatePercent = 12m,
                EpfEmployerRatePercent = 12m,
                EsicMode = "STATUTORY",
                EsicWageCeiling = 21000m,
                EsicEmployeeRatePercent = 0.75m,
                EsicEmployerRatePercent = 3.25m,
                TdsMode = "AUTO_NEW_REGIME",
                RequireAttendanceLock = true,
                EffectiveFrom = date,
                IsActive = true,
                Version = 0
            }
            : new PayrollDivisorPolicyDto
            {
                Id = item.Id,
                PayrollMode = string.IsNullOrWhiteSpace(item.PayrollMode) ? "Standard" : item.PayrollMode,
                DivisorPolicy = item.DivisorPolicy,
                LopCalculationBase = string.IsNullOrWhiteSpace(item.LopCalculationBase) ? "FIXED_GROSS" : item.LopCalculationBase,
                EpfMode = string.IsNullOrWhiteSpace(item.EpfMode) ? "CAPPED_CEILING" : item.EpfMode,
                EpfWageCeiling = item.EpfWageCeiling <= 0 ? 15000m : item.EpfWageCeiling,
                EpfEmployeeRatePercent = item.EpfEmployeeRatePercent > 0 ? item.EpfEmployeeRatePercent : 12m,
                EpfEmployerRatePercent = item.EpfEmployerRatePercent > 0 ? item.EpfEmployerRatePercent : 12m,
                EsicMode = string.IsNullOrWhiteSpace(item.EsicMode) ? "STATUTORY" : item.EsicMode,
                EsicWageCeiling = item.EsicWageCeiling <= 0 ? 21000m : item.EsicWageCeiling,
                EsicEmployeeRatePercent = item.EsicEmployeeRatePercent > 0 ? item.EsicEmployeeRatePercent : 0.75m,
                EsicEmployerRatePercent = item.EsicEmployerRatePercent > 0 ? item.EsicEmployerRatePercent : 3.25m,
                TdsMode = string.IsNullOrWhiteSpace(item.TdsMode) ? "AUTO_NEW_REGIME" : item.TdsMode,
                RequireAttendanceLock = item.RequireAttendanceLock,
                EffectiveFrom = item.EffectiveFrom,
                EffectiveTo = item.EffectiveTo,
                IsActive = item.IsActive,
                Version = item.Version
            };
    }

    public async Task<Result> Save(string companyId, string userId, PayrollDivisorPolicyDto dto)
    {
        var allowed = new[] { "CALENDAR_DAYS", "WORKING_DAYS", "FIXED_30_DAYS" };
        if (!allowed.Contains(dto.DivisorPolicy) || (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return new Result { Success = false, Message = "Invalid payroll divisor policy or effective dates." };

        // Statutory settings have a separate maker-checker lifecycle. This endpoint predates that
        // workflow and is still used for divisor/LOP/attendance controls, so reject any statutory
        // change here rather than allowing it to become the payroll fallback before approval.
        var effective = await GetEffective(companyId, dto.EffectiveFrom);
        var epfMode = Normalize(dto.EpfMode, "CAPPED_CEILING");
        var esicMode = Normalize(dto.EsicMode, "STATUTORY");
        var tdsMode = Normalize(dto.TdsMode, "AUTO_NEW_REGIME");
        var statutoryChanged = !string.Equals(epfMode, Normalize(effective.EpfMode, "CAPPED_CEILING"), StringComparison.OrdinalIgnoreCase)
            || (epfMode != "EXEMPT" && dto.EpfWageCeiling > 0 && dto.EpfWageCeiling != effective.EpfWageCeiling)
            || !string.Equals(esicMode, Normalize(effective.EsicMode, "STATUTORY"), StringComparison.OrdinalIgnoreCase)
            || (esicMode != "EXEMPT" && dto.EsicWageCeiling > 0 && dto.EsicWageCeiling != effective.EsicWageCeiling)
            || !string.Equals(tdsMode, Normalize(effective.TdsMode, "AUTO_NEW_REGIME"), StringComparison.OrdinalIgnoreCase);
        statutoryChanged = statutoryChanged || dto.EpfEmployeeRatePercent != effective.EpfEmployeeRatePercent
            || dto.EpfEmployerRatePercent != effective.EpfEmployerRatePercent
            || dto.EsicEmployeeRatePercent != effective.EsicEmployeeRatePercent
            || dto.EsicEmployerRatePercent != effective.EsicEmployerRatePercent;
        if (statutoryChanged)
        {
            return new Result
            {
                Success = false,
                StatusCode = 409,
                Message = "PF, ESIC, and TDS changes must be saved as an effective-dated statutory profile and submitted for authorized approval before payroll can use them."
            };
        }

        var versions = (await repository.GetAll(x => x.CompanyId == companyId && x.IsActive)).OrderByDescending(x => x.Version).ToList();
        var newStart = dto.EffectiveFrom.Date;
        var newEnd = dto.EffectiveTo?.Date ?? DateTime.MaxValue.Date;

        // Preflight the complete scope before changing any existing version. In
        // particular, a same-day replacement must not deactivate the working
        // policy and then discover a conflicting overlapping revision.
        var sameDayActive = versions.FirstOrDefault(x => x.EffectiveFrom.Date == newStart && !x.EffectiveTo.HasValue);
        if (versions.Any(x => x.Id != sameDayActive?.Id &&
            (x.EffectiveFrom.Date == newStart ||
             (x.EffectiveTo.HasValue && x.EffectiveFrom.Date <= newEnd && x.EffectiveTo.Value.Date >= newStart) ||
             (!x.EffectiveTo.HasValue && x.EffectiveFrom.Date > newStart))))
            return new Result { Success = false, Message = "The payroll divisor policy overlaps an existing version." };

        var open = versions.FirstOrDefault(x => x.Id != sameDayActive?.Id && !x.EffectiveTo.HasValue && x.EffectiveFrom < dto.EffectiveFrom.Date);
        var sameDayWasActive = sameDayActive?.IsActive;
        var sameDayUpdatedBy = sameDayActive?.UpdatedBy;
        var sameDayUpdatedDate = sameDayActive?.UpdatedDate;
        var openEffectiveTo = open?.EffectiveTo;
        var openUpdatedBy = open?.UpdatedBy;
        var openUpdatedDate = open?.UpdatedDate;
        var sameDayChangeAttempted = false;
        var openChangeAttempted = false;

        async Task<bool> RestorePriorVersions()
        {
            var restored = true;
            if (sameDayChangeAttempted && sameDayActive is not null)
            {
                sameDayActive.IsActive = sameDayWasActive ?? true;
                sameDayActive.UpdatedBy = sameDayUpdatedBy;
                sameDayActive.UpdatedDate = sameDayUpdatedDate;
                var result = await repository.Update(
                    Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.Id, sameDayActive.Id) & Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.CompanyId, companyId), sameDayActive);
                restored &= result.Success;
            }
            if (openChangeAttempted && open is not null)
            {
                open.EffectiveTo = openEffectiveTo;
                open.UpdatedBy = openUpdatedBy;
                open.UpdatedDate = openUpdatedDate;
                var result = await repository.Update(
                    Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.Id, open.Id) & Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.CompanyId, companyId), open);
                restored &= result.Success;
            }
            return restored;
        }

        // Close the earlier open-ended record first. If a later operation fails,
        // the active same-day version still covers the new start date.
        if (open != null)
        {
            open.EffectiveTo = dto.EffectiveFrom.Date.AddDays(-1);
            open.UpdatedBy = userId;
            open.UpdatedDate = DateTime.UtcNow;
            openChangeAttempted = true;
            var closed = await repository.Update(Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.Id, open.Id) & Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.CompanyId, companyId), open);
            if (!closed.Success)
            {
                var restored = await RestorePriorVersions();
                return restored ? closed : new Result { Success = false, StatusCode = 500, Message = "Policy update failed and the prior policy could not be restored; administrator review is required." };
            }
        }

        // If an active policy starts on the exact same EffectiveFrom date and has no EffectiveTo set,
        // deactivate it so HR can update the current month's policy settings smoothly while preserving version history.
        if (sameDayActive != null)
        {
            sameDayActive.IsActive = false;
            sameDayActive.UpdatedBy = userId;
            sameDayActive.UpdatedDate = DateTime.UtcNow;
            sameDayChangeAttempted = true;
            var deactivated = await repository.Update(Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.Id, sameDayActive.Id) & Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.CompanyId, companyId), sameDayActive);
            if (!deactivated.Success)
            {
                var restored = await RestorePriorVersions();
                return restored ? deactivated : new Result { Success = false, StatusCode = 500, Message = "Policy update failed and the prior policy could not be restored; administrator review is required." };
            }
            versions.Remove(sameDayActive);
        }

        var allVersions = await repository.GetAll(x => x.CompanyId == companyId);
        var nextVersion = (allVersions.OrderByDescending(x => x.Version).FirstOrDefault()?.Version ?? 0) + 1;

        var inserted = await repository.AddOne(new PayrollDivisorPolicy
        {
            CompanyId = companyId,
            PayrollMode = string.IsNullOrWhiteSpace(dto.PayrollMode) ? "Standard" : dto.PayrollMode,
            DivisorPolicy = dto.DivisorPolicy,
            LopCalculationBase = string.IsNullOrWhiteSpace(dto.LopCalculationBase) ? "FIXED_GROSS" : dto.LopCalculationBase,
            EpfMode = string.IsNullOrWhiteSpace(dto.EpfMode) ? "CAPPED_CEILING" : dto.EpfMode,
            EpfWageCeiling = dto.EpfWageCeiling <= 0 ? 15000m : dto.EpfWageCeiling,
            EpfEmployeeRatePercent = dto.EpfEmployeeRatePercent,
            EpfEmployerRatePercent = dto.EpfEmployerRatePercent,
            EsicMode = string.IsNullOrWhiteSpace(dto.EsicMode) ? "STATUTORY" : dto.EsicMode,
            EsicWageCeiling = dto.EsicWageCeiling <= 0 ? 21000m : dto.EsicWageCeiling,
            EsicEmployeeRatePercent = dto.EsicEmployeeRatePercent,
            EsicEmployerRatePercent = dto.EsicEmployerRatePercent,
            TdsMode = string.IsNullOrWhiteSpace(dto.TdsMode) ? "AUTO_NEW_REGIME" : dto.TdsMode,
            RequireAttendanceLock = dto.RequireAttendanceLock,
            EffectiveFrom = dto.EffectiveFrom.Date,
            EffectiveTo = dto.EffectiveTo?.Date,
            IsActive = dto.IsActive,
            Version = nextVersion,
            CreatedBy = userId
        });
        if (inserted.Success) return inserted;

        var priorRestored = await RestorePriorVersions();
        return priorRestored
            ? inserted
            : new Result { Success = false, StatusCode = 500, Message = "Policy creation failed and the prior policy could not be restored; administrator review is required." };
    }

    private static string Normalize(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();
}
