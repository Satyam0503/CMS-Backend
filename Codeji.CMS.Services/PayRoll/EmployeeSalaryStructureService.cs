using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public interface IEmployeeSalaryStructureService
{
    Task<List<EmployeeSalaryStructureDto>> GetHistory(string companyId, string employeeId);
    Task<Result<EmployeeSalaryStructureDto>> SaveDraft(string companyId, string actorUserId, EmployeeSalaryStructureDto dto);
    Task<Result> Submit(string companyId, string actorUserId, string id, int version);
    Task<Result> Approve(string companyId, string actorUserId, string id, int version);
}

public sealed class EmployeeSalaryStructureService(
    IMongoDbRepository<EmployeeSalaryStructure> repository,
    IMongoDbRepository<EmpUser> employees) : IEmployeeSalaryStructureService
{
    public async Task<List<EmployeeSalaryStructureDto>> GetHistory(string companyId, string employeeId) =>
        (await repository.GetAll(x => x.CompanyId == companyId && x.EmployeeId == employeeId.Trim()))
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Version).Select(Map).ToList();

    public async Task<Result<EmployeeSalaryStructureDto>> SaveDraft(string companyId, string actorUserId, EmployeeSalaryStructureDto dto)
    {
        var employeeId = dto.EmployeeId.Trim();
        if (string.IsNullOrWhiteSpace(employeeId) || dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return FailDto(400, "Employee and valid effective dates are required.");
        if (!await employees.Exist(x => x.CompanyId == companyId && x.EmployeeId == employeeId && !x.IsDeleted))
            return FailDto(404, "Employee was not found in this company.");
        EmployeeSalaryStructure? existingRecord = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            existingRecord = await repository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == dto.Id);
            if (existingRecord is null) return FailDto(404, "Salary structure was not found.");
            if (!string.Equals(existingRecord.EmployeeId, employeeId, StringComparison.Ordinal))
                return FailDto(409, "A salary structure cannot be reassigned to another employee.");
            if (existingRecord.Version != dto.Version) return FailDto(409, "Salary structure changed; reload and retry.");
            if (existingRecord.ApprovalStatus != "DRAFT")
                return FailDto(409, "Submitted or approved salary structures are immutable. Create a new effective-dated revision.");
        }
        var existing = (await repository.GetAll(x => x.CompanyId == companyId && x.EmployeeId == employeeId)).ToList();
        var candidateStart = dto.EffectiveFrom.Date;
        var candidateEnd = dto.EffectiveTo?.Date ?? DateTime.MaxValue.Date;
        var overlaps = existing.Where(x => x.Id != dto.Id && !(x.EffectiveFrom.Date < candidateStart && !x.EffectiveTo.HasValue && x.ApprovalStatus == "APPROVED" && x.Status == "ACTIVE") && x.EffectiveFrom.Date <= candidateEnd && (x.EffectiveTo?.Date ?? DateTime.MaxValue.Date) >= candidateStart).ToList();
        if (overlaps.Any(x => x.ApprovalStatus is "APPROVED" or "SUBMITTED"))
            return FailDto(409, "The employee already has an overlapping approved or submitted salary structure.");

        var entity = new EmployeeSalaryStructure
        {
            Id = dto.Id ?? string.Empty, CompanyId = companyId, EmployeeId = employeeId, UserId = dto.UserId,
            OfferId = dto.OfferId, OfferVersion = dto.OfferVersion, SalaryTemplateId = dto.SalaryTemplateId,
            PayGroupId = dto.PayGroupId, EmployeeType = dto.EmployeeType, GradeId = dto.GradeId, DepartmentId = dto.DepartmentId,
            Currency = dto.Currency.Trim().ToUpperInvariant(), PaymentFrequency = dto.PaymentFrequency.Trim().ToUpperInvariant(),
            AnnualCtc = dto.AnnualCtc, MonthlyGross = dto.MonthlyGross, EffectiveFrom = candidateStart,
            EffectiveTo = dto.EffectiveTo?.Date, Status = "DRAFT", ApprovalStatus = "DRAFT", Source = dto.Source.Trim().ToUpperInvariant(),
            Version = existingRecord is null ? Math.Max(1, dto.Version) : existingRecord.Version + 1,
            CreatedBy = existingRecord?.CreatedBy ?? actorUserId, Components = dto.Components.Select(Map).ToList()
        };
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            var updated = await repository.Update(Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, dto.Id) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.CompanyId, companyId) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Version, dto.Version), entity);
            return updated.Success ? Ok(entity) : FailDto(409, updated.Message ?? "Salary structure changed; reload and retry.");
        }
        var added = await repository.AddOne(entity);
        return added.Success ? Ok(entity) : FailDto(added.StatusCode, added.Message ?? "Unable to save salary structure.");
    }

    public async Task<Result> Submit(string companyId, string actorUserId, string id, int version)
    {
        var row = await repository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        if (row is null) return Fail(404, "Salary structure was not found.");
        if (row.Version != version || row.ApprovalStatus != "DRAFT") return Fail(409, "Salary structure changed or is not in draft state.");
        row.ApprovalStatus = "SUBMITTED"; row.Status = "PENDING_REVIEW"; row.CreatedBy ??= actorUserId; row.SubmittedBy = actorUserId;
        return await repository.Update(Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.CompanyId, companyId) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, id) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Version, version), row);
    }

    public async Task<Result> Approve(string companyId, string actorUserId, string id, int version)
    {
        var row = await repository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        if (row is null) return Fail(404, "Salary structure was not found.");
        if (row.Version != version || row.ApprovalStatus != "SUBMITTED") return Fail(409, "Salary structure must be submitted before approval.");
        if ((!string.IsNullOrWhiteSpace(row.CreatedBy) && string.Equals(row.CreatedBy, actorUserId, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(row.SubmittedBy) && string.Equals(row.SubmittedBy, actorUserId, StringComparison.OrdinalIgnoreCase)))
            return Fail(403, "A salary structure must be approved by someone other than its creator or submitter.");
        var priorOpen = (await repository.GetAll(x => x.CompanyId == companyId && x.EmployeeId == row.EmployeeId && x.Id != row.Id && x.ApprovalStatus == "APPROVED" && x.Status == "ACTIVE" && !x.EffectiveTo.HasValue && x.EffectiveFrom < row.EffectiveFrom))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        if (priorOpen is not null)
        {
            priorOpen.EffectiveTo = row.EffectiveFrom.Date.AddDays(-1);
            var closeResult = await repository.Update(Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, priorOpen.Id) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.CompanyId, companyId), priorOpen);
            if (!closeResult.Success) return Fail(409, "Unable to close the previous salary revision; the new revision was not approved.");
        }
        row.ApprovalStatus = "APPROVED"; row.Status = "ACTIVE"; row.ApprovedBy = actorUserId; row.ApprovedAtUtc = DateTime.UtcNow; row.Version++;
        var approval = await repository.Update(Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.CompanyId, companyId) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, id) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Version, version) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.ApprovalStatus, "SUBMITTED"), row);
        if (!approval.Success && priorOpen is not null)
        {
            priorOpen.EffectiveTo = null;
            await repository.Update(Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, priorOpen.Id) & Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.CompanyId, companyId), priorOpen);
        }
        return approval;
    }

    private static EmployeeSalaryStructureDto Map(EmployeeSalaryStructure x) => new()
    {
        Id = x.Id, EmployeeId = x.EmployeeId, UserId = x.UserId, OfferId = x.OfferId, OfferVersion = x.OfferVersion,
        SalaryTemplateId = x.SalaryTemplateId, PayGroupId = x.PayGroupId, EmployeeType = x.EmployeeType, GradeId = x.GradeId,
        DepartmentId = x.DepartmentId, Currency = x.Currency, PaymentFrequency = x.PaymentFrequency, AnnualCtc = x.AnnualCtc,
        MonthlyGross = x.MonthlyGross, EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, Status = x.Status,
        ApprovalStatus = x.ApprovalStatus, Source = x.Source, Version = x.Version, SubmittedBy = x.SubmittedBy, Components = x.Components.Select(Map).ToList()
    };
    private static EmployeeSalaryComponentValueDto Map(EmployeeSalaryComponentValue x) => new() { ComponentCode = x.ComponentCode, ComponentName = x.ComponentName, Kind = x.Kind, Value = x.Value, Percentage = x.Percentage, CalculationType = x.CalculationType, BaseComponentCode = x.BaseComponentCode, IsTaxable = x.IsTaxable, IsRecurring = x.IsRecurring, IsStatutory = x.IsStatutory, IsEmployerContribution = x.IsEmployerContribution };
    private static EmployeeSalaryComponentValue Map(EmployeeSalaryComponentValueDto x) => new() { ComponentCode = x.ComponentCode.Trim().ToUpperInvariant(), ComponentName = x.ComponentName.Trim(), Kind = x.Kind.Trim().ToUpperInvariant(), Value = x.Value, Percentage = x.Percentage, CalculationType = x.CalculationType.Trim().ToUpperInvariant(), BaseComponentCode = x.BaseComponentCode?.Trim().ToUpperInvariant(), IsTaxable = x.IsTaxable, IsRecurring = x.IsRecurring, IsStatutory = x.IsStatutory, IsEmployerContribution = x.IsEmployerContribution };
    private static Result<EmployeeSalaryStructureDto> Ok(EmployeeSalaryStructure x) => new() { Success = true, StatusCode = 201, ResourceId = x.Id, MethodResult = Map(x), Message = "Salary structure saved." };
    private static Result<EmployeeSalaryStructureDto> FailDto(int code, string message) => new() { Success = false, StatusCode = code, Message = message };
    private static Result Fail(int code, string message) => new() { Success = false, StatusCode = code, Message = message };
}




