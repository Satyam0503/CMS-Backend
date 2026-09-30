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
                EsicMode = "STATUTORY",
                EsicWageCeiling = 21000m,
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
                EsicMode = string.IsNullOrWhiteSpace(item.EsicMode) ? "STATUTORY" : item.EsicMode,
                EsicWageCeiling = item.EsicWageCeiling <= 0 ? 21000m : item.EsicWageCeiling,
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

        // If an active policy starts on the exact same EffectiveFrom date and has no EffectiveTo set,
        // deactivate it so HR can update the current month's policy settings smoothly while preserving version history.
        var sameDayActive = versions.FirstOrDefault(x => x.EffectiveFrom.Date == newStart && !x.EffectiveTo.HasValue);
        if (sameDayActive != null)
        {
            sameDayActive.IsActive = false;
            sameDayActive.UpdatedBy = userId;
            sameDayActive.UpdatedDate = DateTime.UtcNow;
            await repository.Update(Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.Id, sameDayActive.Id) & Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.CompanyId, companyId), sameDayActive);
            versions.Remove(sameDayActive);
        }

        if (versions.Any(x => x.EffectiveFrom.Date == newStart ||
            (x.EffectiveTo.HasValue && x.EffectiveFrom.Date <= newEnd && x.EffectiveTo.Value.Date >= newStart) ||
            (!x.EffectiveTo.HasValue && x.EffectiveFrom.Date > newStart)))
            return new Result { Success = false, Message = "The payroll divisor policy overlaps an existing version." };

        var open = versions.FirstOrDefault(x => !x.EffectiveTo.HasValue && x.EffectiveFrom < dto.EffectiveFrom.Date);
        if (open != null)
        {
            open.EffectiveTo = dto.EffectiveFrom.Date.AddDays(-1);
            open.UpdatedBy = userId;
            open.UpdatedDate = DateTime.UtcNow;
            await repository.Update(Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.Id, open.Id) & Builders<PayrollDivisorPolicy>.Filter.Eq(x => x.CompanyId, companyId), open);
        }

        var allVersions = await repository.GetAll(x => x.CompanyId == companyId);
        var nextVersion = (allVersions.OrderByDescending(x => x.Version).FirstOrDefault()?.Version ?? 0) + 1;

        return await repository.AddOne(new PayrollDivisorPolicy
        {
            CompanyId = companyId,
            PayrollMode = string.IsNullOrWhiteSpace(dto.PayrollMode) ? "Standard" : dto.PayrollMode,
            DivisorPolicy = dto.DivisorPolicy,
            LopCalculationBase = string.IsNullOrWhiteSpace(dto.LopCalculationBase) ? "FIXED_GROSS" : dto.LopCalculationBase,
            EpfMode = string.IsNullOrWhiteSpace(dto.EpfMode) ? "CAPPED_CEILING" : dto.EpfMode,
            EpfWageCeiling = dto.EpfWageCeiling <= 0 ? 15000m : dto.EpfWageCeiling,
            EsicMode = string.IsNullOrWhiteSpace(dto.EsicMode) ? "STATUTORY" : dto.EsicMode,
            EsicWageCeiling = dto.EsicWageCeiling <= 0 ? 21000m : dto.EsicWageCeiling,
            TdsMode = string.IsNullOrWhiteSpace(dto.TdsMode) ? "AUTO_NEW_REGIME" : dto.TdsMode,
            RequireAttendanceLock = dto.RequireAttendanceLock,
            EffectiveFrom = dto.EffectiveFrom.Date,
            EffectiveTo = dto.EffectiveTo?.Date,
            IsActive = dto.IsActive,
            Version = nextVersion,
            CreatedBy = userId
        });
    }

    private static string Normalize(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();
}
