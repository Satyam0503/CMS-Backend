using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public interface IPayrollConfigurationService
{
    Task<List<PayrollPayGroupDto>> GetPayGroups(string companyId);
    Task<Result> SavePayGroup(string companyId, string actorUserId, PayrollPayGroupDto dto);
    Task<List<EmployeePayrollAssignmentDto>> GetAssignments(string companyId);
    Task<Result> SaveAssignment(string companyId, string actorUserId, EmployeePayrollAssignmentDto dto);
    Task<Result> ApproveAssignment(string companyId, string actorUserId, string employeeId, DateTime effectiveFrom);
    Task<Result<PayrollBulkAssignmentPreviewRowDto>> PreviewBulkAssignments(string companyId, PayrollBulkAssignmentRequestDto request);
    Task<Result<PayrollBulkAssignmentResultDto>> SubmitBulkAssignments(string companyId, string actorUserId, PayrollBulkAssignmentRequestDto request);
}

public sealed class PayrollConfigurationService(
    IMongoDbRepository<PayrollPayGroup> payGroups,
    IMongoDbRepository<EmployeePayrollAssignment> assignments, IMongoDbRepository<SalaryTemplate> salaryTemplates, IMongoDbRepository<PayrollPolicyProfile> policyProfiles, IMongoDbRepository<EmpUser>? employees = null) : IPayrollConfigurationService
{
    public async Task<List<PayrollPayGroupDto>> GetPayGroups(string companyId) =>
        (await payGroups.GetAll(x => x.CompanyId == companyId))
        .OrderByDescending(x => x.IsActive).ThenBy(x => x.Name)
        .Select(x => new PayrollPayGroupDto { Id = x.Id, Name = x.Name, Schedule = x.Schedule, LegalEntityId = x.LegalEntityId, PolicyProfileId = x.PolicyProfileId, EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, IsActive = x.IsActive, Version = x.Version }).ToList();

    public async Task<Result> SavePayGroup(string companyId, string actorUserId, PayrollPayGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return new Result { Success = false, StatusCode = 400, Message = "Pay Group name and valid effective dates are required." };
        var duplicate = await payGroups.Exist(x => x.CompanyId == companyId && x.Name == dto.Name.Trim() && x.Id != dto.Id && x.IsActive);
        if (duplicate) return new Result { Success = false, StatusCode = 409, Message = "A Pay Group with this name already exists." };
        if (dto.PolicyProfileId is not null && !await policyProfiles.Exist(x => x.CompanyId == companyId && x.Id == dto.PolicyProfileId && x.IsActive))
            return new Result { Success = false, StatusCode = 404, Message = "Policy profile was not found." };
        var entity = new PayrollPayGroup { Id = dto.Id ?? string.Empty, CompanyId = companyId, Name = dto.Name.Trim(), Schedule = dto.Schedule.Trim().ToUpperInvariant(), LegalEntityId = dto.LegalEntityId?.Trim(), PolicyProfileId = dto.PolicyProfileId, EffectiveFrom = dto.EffectiveFrom.Date, EffectiveTo = dto.EffectiveTo?.Date, IsActive = dto.IsActive, Version = Math.Max(1, dto.Version), };
        if (!string.IsNullOrWhiteSpace(dto.Id)) { entity.Version++; return await payGroups.Update(Builders<PayrollPayGroup>.Filter.Eq(x => x.Id, dto.Id) & Builders<PayrollPayGroup>.Filter.Eq(x => x.CompanyId, companyId) & (dto.Version > 0 ? Builders<PayrollPayGroup>.Filter.Eq(x => x.Version, dto.Version) : Builders<PayrollPayGroup>.Filter.Empty), entity); }
        return await payGroups.AddOne(entity);
    }

    public async Task<List<EmployeePayrollAssignmentDto>> GetAssignments(string companyId) =>
        (await assignments.GetAll(x => x.CompanyId == companyId)).OrderByDescending(x => x.EffectiveFrom).Select(x => new EmployeePayrollAssignmentDto { Id = x.Id, EmployeeId = x.EmployeeId, PayGroupId = x.PayGroupId, SalaryTemplateId = x.SalaryTemplateId, PolicyProfileId = x.PolicyProfileId, EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, IsApproved = x.IsApproved, ApprovedBy = x.ApprovedBy, ApprovedAtUtc = x.ApprovedAtUtc }).ToList();

    public async Task<Result> SaveAssignment(string companyId, string actorUserId, EmployeePayrollAssignmentDto dto)
    {
        var employeeId = dto.EmployeeId?.Trim();
        if (string.IsNullOrWhiteSpace(employeeId) || dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return new Result { Success = false, StatusCode = 400, Message = "Employee and valid effective dates are required." };
        if (employees is not null && !await employees.Exist(x => x.CompanyId == companyId && x.EmployeeId == employeeId))
            return new Result { Success = false, StatusCode = 404, Message = "Employee was not found in this company." };
        if (dto.PayGroupId is not null && !await payGroups.Exist(x => x.CompanyId == companyId && x.Id == dto.PayGroupId && x.IsActive))
            return new Result { Success = false, StatusCode = 404, Message = "Pay Group was not found." };
        if (dto.SalaryTemplateId is not null && !await salaryTemplates.Exist(x => x.CompanyId == companyId && x.Id == dto.SalaryTemplateId && x.IsActive))
            return new Result { Success = false, StatusCode = 404, Message = "Salary template was not found." };
        if (dto.PolicyProfileId is not null && !await policyProfiles.Exist(x => x.CompanyId == companyId && x.Id == dto.PolicyProfileId && x.IsActive))
            return new Result { Success = false, StatusCode = 404, Message = "Policy profile was not found." };
        var overlap = await assignments.Exist(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.EffectiveFrom <= (dto.EffectiveTo ?? DateTime.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo >= dto.EffectiveFrom));
        if (overlap) return new Result { Success = false, StatusCode = 409, Message = "The employee already has an overlapping payroll assignment." };
        return await assignments.AddOne(new EmployeePayrollAssignment { CompanyId = companyId, EmployeeId = employeeId!, PayGroupId = dto.PayGroupId, SalaryTemplateId = dto.SalaryTemplateId, PolicyProfileId = dto.PolicyProfileId, EffectiveFrom = dto.EffectiveFrom.Date, EffectiveTo = dto.EffectiveTo?.Date, IsApproved = false });
    }
    public async Task<Result> ApproveAssignment(string companyId, string actorUserId, string employeeId, DateTime effectiveFrom)
    {
        var assignment = await assignments.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.EffectiveFrom == effectiveFrom.Date);
        if (assignment is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll assignment was not found." };
        if (assignment.IsApproved) return new Result { Success = true, Message = "Payroll assignment is already approved." };
        assignment.IsApproved = true;
        assignment.ApprovedBy = actorUserId;
        assignment.ApprovedAtUtc = DateTime.UtcNow;
        return await assignments.Update(Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.Id, assignment.Id) & Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.CompanyId, companyId) & Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.IsApproved, false), assignment);
    }

    public async Task<Result<PayrollBulkAssignmentPreviewRowDto>> PreviewBulkAssignments(string companyId, PayrollBulkAssignmentRequestDto request)
    {
        var result = new Result<PayrollBulkAssignmentPreviewRowDto>();
        if (request.EffectiveFrom == default || request.EffectiveTo.HasValue && request.EffectiveTo.Value.Date < request.EffectiveFrom.Date)
            return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = 400, Message = "A valid effective date range is required." };
        if (string.IsNullOrWhiteSpace(request.PayGroupId) || !await payGroups.Exist(x => x.CompanyId == companyId && x.Id == request.PayGroupId && x.IsActive))
            return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = 404, Message = "Pay Group was not found." };
        if (request.SalaryTemplateId is not null && !await salaryTemplates.Exist(x => x.CompanyId == companyId && x.Id == request.SalaryTemplateId && x.IsActive))
            return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = 404, Message = "Salary template was not found." };
        if (request.PolicyProfileId is not null && !await policyProfiles.Exist(x => x.CompanyId == companyId && x.Id == request.PolicyProfileId && x.IsActive))
            return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = 404, Message = "Policy profile was not found." };
        if (employees is null) return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = 503, Message = "Employee directory is unavailable." };
        var ids = request.EmployeeIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var employeeList = (await employees.GetAll(x => x.CompanyId == companyId && (ids.Count == 0 || ids.Contains(x.EmployeeId)) && (string.IsNullOrWhiteSpace(request.Department) || x.Department == request.Department) && (string.IsNullOrWhiteSpace(request.JobRole) || x.JobRole == request.JobRole) && (!request.EmploymentType.HasValue || x.EmploymentType == request.EmploymentType.Value))).ToList();
        var existing = (await assignments.GetAll(x => x.CompanyId == companyId)).ToList();
        var rows = employeeList.Select(employee => {
            var overlap = existing.Any(x => x.EmployeeId == employee.EmployeeId && x.EffectiveFrom <= (request.EffectiveTo ?? DateTime.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo >= request.EffectiveFrom));
            return new PayrollBulkAssignmentPreviewRowDto { EmployeeId = employee.EmployeeId, EmployeeName = $"{employee.FirstName} {employee.LastName}".Trim(), Department = employee.Department, JobRole = employee.JobRole, HasExistingAssignment = existing.Any(x => x.EmployeeId == employee.EmployeeId), HasOverlap = overlap, ConflictMessage = overlap ? "An assignment overlaps the selected effective dates." : null, CanSubmit = !overlap };
        }).OrderBy(x => x.EmployeeId).ToList();
        result.MethodResults = rows;
        result.TotalRecords = rows.Count;
        return result;
    }

    public async Task<Result<PayrollBulkAssignmentResultDto>> SubmitBulkAssignments(string companyId, string actorUserId, PayrollBulkAssignmentRequestDto request)
    {
        var preview = await PreviewBulkAssignments(companyId, request);
        if (!preview.Success) return new Result<PayrollBulkAssignmentResultDto> { Success = false, StatusCode = preview.StatusCode, Message = preview.Message };
        var rows = preview.MethodResults.Where(x => x.CanSubmit).ToList();
        if (rows.Count == 0) return new Result<PayrollBulkAssignmentResultDto> { Success = false, StatusCode = 409, Message = "No employees can be submitted because all selected rows have conflicts." };
        var entities = rows.Select(row => new EmployeePayrollAssignment { CompanyId = companyId, EmployeeId = row.EmployeeId, PayGroupId = request.PayGroupId, SalaryTemplateId = request.SalaryTemplateId, PolicyProfileId = request.PolicyProfileId, EffectiveFrom = request.EffectiveFrom.Date, EffectiveTo = request.EffectiveTo?.Date, IsApproved = false }).ToList();
        var saved = await assignments.AddMany(entities);
        if (!saved.Success) return new Result<PayrollBulkAssignmentResultDto> { Success = false, StatusCode = saved.StatusCode, Message = saved.Message };
        return new Result<PayrollBulkAssignmentResultDto> { Success = true, StatusCode = 201, Message = $"{rows.Count} assignments submitted for approval.", MethodResult = new PayrollBulkAssignmentResultDto { SubmittedCount = rows.Count, ConflictCount = preview.MethodResults.Count(x => !x.CanSubmit), Rows = preview.MethodResults } };
    }}