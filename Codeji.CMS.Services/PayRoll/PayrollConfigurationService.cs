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
    Task<Result> RejectAssignment(string companyId, string actorUserId, string employeeId, DateTime effectiveFrom, string reason);
    Task<Result> CancelAssignment(string companyId, string actorUserId, string employeeId, DateTime effectiveFrom);
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
        if (!string.Equals(dto.Schedule?.Trim(), "MONTHLY", StringComparison.OrdinalIgnoreCase))
            return new Result { Success = false, StatusCode = 400, Message = "Only monthly payroll schedules are currently supported." };
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            var existing = await payGroups.FirstOrDefault(x => x.CompanyId == companyId && x.Id == dto.Id);
            if (existing is null)
                return new Result { Success = false, StatusCode = 404, Message = "Pay group was not found in this company." };
            if (dto.Version <= 0 || existing.Version != dto.Version)
                return new Result { Success = false, StatusCode = 409, Message = "The pay group changed. Refresh before updating it." };
        }
        var duplicate = await payGroups.Exist(x => x.CompanyId == companyId && x.Name == dto.Name.Trim() && x.Id != dto.Id && x.IsActive);
        if (duplicate) return new Result { Success = false, StatusCode = 409, Message = "A Pay Group with this name already exists." };
        var policyProfileId = string.IsNullOrWhiteSpace(dto.PolicyProfileId) ? null : dto.PolicyProfileId.Trim();
        if (policyProfileId is not null)
        {
            var profile = await policyProfiles.FirstOrDefault(x => x.CompanyId == companyId && x.Id == policyProfileId && x.IsActive);
            if (profile is null) return new Result { Success = false, StatusCode = 404, Message = "Policy profile was not found in this company or is inactive." };
            if (string.Equals(profile.Scope, "ASSIGNMENT_ONLY", StringComparison.OrdinalIgnoreCase))
                return new Result { Success = false, StatusCode = 400, Message = "An assignment-only policy profile cannot be attached to a pay group." };
            if (!string.Equals(profile.ApprovalStatus, "APPROVED", StringComparison.Ordinal))
                return new Result { Success = false, StatusCode = 409, Message = "The pay group can only use an approved policy profile." };
            if (!Covers(profile.EffectiveFrom, profile.EffectiveTo, dto.EffectiveFrom, dto.EffectiveTo))
                return new Result { Success = false, StatusCode = 400, Message = "The policy profile must be effective for the entire pay group date range." };
        }
        var entity = new PayrollPayGroup { Id = dto.Id ?? string.Empty, CompanyId = companyId, Name = dto.Name.Trim(), Schedule = dto.Schedule.Trim().ToUpperInvariant(), LegalEntityId = dto.LegalEntityId?.Trim(), PolicyProfileId = policyProfileId, EffectiveFrom = dto.EffectiveFrom.Date, EffectiveTo = dto.EffectiveTo?.Date, IsActive = dto.IsActive, Version = Math.Max(1, dto.Version), };
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            entity.Version++;
            var updated = await payGroups.Update(
                Builders<PayrollPayGroup>.Filter.Eq(x => x.Id, dto.Id) &
                Builders<PayrollPayGroup>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<PayrollPayGroup>.Filter.Eq(x => x.Version, dto.Version),
                entity);
            if (!updated.Success && updated.StatusCode == 400)
                return new Result { Success = false, StatusCode = 409, Message = "The pay group changed while you were editing it. Refresh the group and apply your changes again." };
            return updated;
        }
        return await payGroups.AddOne(entity);
    }

    public async Task<List<EmployeePayrollAssignmentDto>> GetAssignments(string companyId) =>
        (await assignments.GetAll(x => x.CompanyId == companyId)).OrderByDescending(x => x.EffectiveFrom).Select(x => new EmployeePayrollAssignmentDto { Id = x.Id, Version = x.Version, EmployeeId = x.EmployeeId, PayGroupId = x.PayGroupId, SalaryTemplateId = x.SalaryTemplateId, PolicyProfileId = x.PolicyProfileId, EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, IsApproved = x.IsApproved, ApprovedBy = x.ApprovedBy, ApprovedAtUtc = x.ApprovedAtUtc, IsRejected = x.IsRejected, RejectedBy = x.RejectedBy, RejectedAtUtc = x.RejectedAtUtc, RejectionReason = x.RejectionReason, IsCancelled = x.IsCancelled, CancelledBy = x.CancelledBy, CancelledAtUtc = x.CancelledAtUtc }).ToList();

    public async Task<Result> SaveAssignment(string companyId, string actorUserId, EmployeePayrollAssignmentDto dto)
    {
        var employeeId = dto.EmployeeId?.Trim();
        if (string.IsNullOrWhiteSpace(employeeId) || dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return new Result { Success = false, StatusCode = 400, Message = "Employee and valid effective dates are required." };
        if (employees is not null && !await employees.Exist(x => x.CompanyId == companyId && x.EmployeeId == employeeId))
            return new Result { Success = false, StatusCode = 404, Message = "Employee was not found in this company." };
        var configuration = await ValidateAssignmentConfiguration(companyId, dto.PayGroupId, dto.SalaryTemplateId, dto.PolicyProfileId, dto.EffectiveFrom, dto.EffectiveTo);
        if (configuration.ErrorCode.HasValue)
            return new Result { Success = false, StatusCode = configuration.ErrorCode.Value, Message = configuration.ErrorMessage };
        var overlap = await assignments.Exist(x => x.CompanyId == companyId && x.EmployeeId == employeeId && !x.IsRejected && !x.IsCancelled && x.EffectiveFrom <= (dto.EffectiveTo ?? DateTime.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo >= dto.EffectiveFrom));
        if (overlap) return new Result { Success = false, StatusCode = 409, Message = "The employee already has an overlapping payroll assignment." };
        return await assignments.AddOne(new EmployeePayrollAssignment { CompanyId = companyId, EmployeeId = employeeId!, PayGroupId = configuration.PayGroupId, SalaryTemplateId = configuration.SalaryTemplateId, PolicyProfileId = configuration.PolicyProfileId, EffectiveFrom = dto.EffectiveFrom.Date, EffectiveTo = dto.EffectiveTo?.Date, CreatedBy = actorUserId, IsApproved = false });
    }
    public async Task<Result> ApproveAssignment(string companyId, string actorUserId, string employeeId, DateTime effectiveFrom)
    {
        var assignment = await assignments.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.EffectiveFrom == effectiveFrom.Date);
        if (assignment is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll assignment was not found." };
        if (assignment.IsApproved) return new Result { Success = true, Message = "Payroll assignment is already approved." };        if (assignment.IsRejected || assignment.IsCancelled) return new Result { Success = false, StatusCode = 409, Message = "A rejected or withdrawn payroll assignment cannot be approved." };
        if (!string.IsNullOrWhiteSpace(assignment.CreatedBy) && string.Equals(assignment.CreatedBy, actorUserId, StringComparison.OrdinalIgnoreCase))
            return new Result { Success = false, StatusCode = 403, Message = "You cannot approve your own payroll assignment." };
        assignment.IsApproved = true;
        assignment.ApprovedBy = actorUserId;
        assignment.ApprovedAtUtc = DateTime.UtcNow;
        var expectedVersion = assignment.Version;
        assignment.Version++;
        var versionFilter = expectedVersion <= 1
            ? Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.Version, expectedVersion) | Builders<EmployeePayrollAssignment>.Filter.Exists(x => x.Version, false)
            : Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.Version, expectedVersion);
        var approved = await assignments.Update(Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.Id, assignment.Id) & Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.CompanyId, companyId) & Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.IsApproved, false) & versionFilter, assignment);
        if (!approved.Success && approved.StatusCode == 400)
            return new Result { Success = false, StatusCode = 409, Message = "The payroll assignment changed while it was being approved. Refresh and review its current status." };
        return approved;
    }

    public async Task<Result> RejectAssignment(string companyId, string actorUserId, string employeeId, DateTime effectiveFrom, string reason)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length == 0 || reason.Length > 500)
            return new Result { Success = false, StatusCode = 400, Message = "A rejection reason of 1 to 500 characters is required." };
        return await TransitionPendingAssignment(companyId, actorUserId, employeeId, effectiveFrom, assignment =>
        {
            assignment.IsRejected = true;
            assignment.RejectedBy = actorUserId;
            assignment.RejectedAtUtc = DateTime.UtcNow;
            assignment.RejectionReason = reason;
        }, "rejected");
    }

    public async Task<Result> CancelAssignment(string companyId, string actorUserId, string employeeId, DateTime effectiveFrom)
    {
        var assignment = await assignments.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.EffectiveFrom == effectiveFrom.Date);
        if (assignment is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll assignment was not found." };


        return await TransitionPendingAssignment(companyId, actorUserId, employeeId, effectiveFrom, item =>
        {
            item.IsCancelled = true;
            item.CancelledBy = actorUserId;
            item.CancelledAtUtc = DateTime.UtcNow;
        }, "cancelled");
    }

    private async Task<Result> TransitionPendingAssignment(string companyId, string actorUserId, string employeeId, DateTime effectiveFrom, Action<EmployeePayrollAssignment> transition, string status)
    {
        var assignment = await assignments.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.EffectiveFrom == effectiveFrom.Date);
        if (assignment is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll assignment was not found." };
        if (assignment.IsApproved || assignment.IsRejected || assignment.IsCancelled)
            return new Result { Success = false, StatusCode = 409, Message = $"A {status} action is allowed only for pending assignments." };
        var expectedVersion = assignment.Version;
        transition(assignment);
        assignment.Version++;
        var versionFilter = expectedVersion <= 1
            ? Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.Version, expectedVersion) | Builders<EmployeePayrollAssignment>.Filter.Exists(x => x.Version, false)
            : Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.Version, expectedVersion);
        var updated = await assignments.Update(Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.Id, assignment.Id) & Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.CompanyId, companyId) & Builders<EmployeePayrollAssignment>.Filter.Eq(x => x.IsApproved, false) & versionFilter, assignment);
        if (!updated.Success && updated.StatusCode == 400)
            return new Result { Success = false, StatusCode = 409, Message = "The payroll assignment changed while this action was being processed. Refresh and review its current status." };
        return updated;
    }
    public async Task<Result<PayrollBulkAssignmentPreviewRowDto>> PreviewBulkAssignments(string companyId, PayrollBulkAssignmentRequestDto request)
    {
        var result = new Result<PayrollBulkAssignmentPreviewRowDto>();
        if (request.EffectiveFrom == default || request.EffectiveTo.HasValue && request.EffectiveTo.Value.Date < request.EffectiveFrom.Date)
            return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = 400, Message = "A valid effective date range is required." };
        if (string.IsNullOrWhiteSpace(request.PayGroupId))
            return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = 400, Message = "A pay group is required for bulk assignment." };
        var configuration = await ValidateAssignmentConfiguration(companyId, request.PayGroupId, request.SalaryTemplateId, request.PolicyProfileId, request.EffectiveFrom, request.EffectiveTo);
        if (configuration.ErrorCode.HasValue)
            return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = configuration.ErrorCode.Value, Message = configuration.ErrorMessage };
        if (employees is null) return new Result<PayrollBulkAssignmentPreviewRowDto> { Success = false, StatusCode = 503, Message = "Employee directory is unavailable." };
        var ids = request.EmployeeIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var employeeList = (await employees.GetAll(x => x.CompanyId == companyId && (ids.Count == 0 || ids.Contains(x.EmployeeId)) && (string.IsNullOrWhiteSpace(request.Department) || x.Department == request.Department) && (string.IsNullOrWhiteSpace(request.JobRole) || x.JobRole == request.JobRole) && (!request.EmploymentType.HasValue || x.EmploymentType == request.EmploymentType.Value))).ToList();
        var existing = (await assignments.GetAll(x => x.CompanyId == companyId)).ToList();
        var rows = employeeList.Select(employee => {
            var overlap = existing.Any(x => x.EmployeeId == employee.EmployeeId && !x.IsRejected && !x.IsCancelled && x.EffectiveFrom <= (request.EffectiveTo ?? DateTime.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo >= request.EffectiveFrom));
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
        var entities = rows.Select(row => new EmployeePayrollAssignment { CompanyId = companyId, EmployeeId = row.EmployeeId, PayGroupId = request.PayGroupId.Trim(), SalaryTemplateId = string.IsNullOrWhiteSpace(request.SalaryTemplateId) ? null : request.SalaryTemplateId.Trim(), PolicyProfileId = string.IsNullOrWhiteSpace(request.PolicyProfileId) ? null : request.PolicyProfileId.Trim(), EffectiveFrom = request.EffectiveFrom.Date, EffectiveTo = request.EffectiveTo?.Date, CreatedBy = actorUserId, IsApproved = false }).ToList();
        var saved = await assignments.AddMany(entities);
        if (!saved.Success) return new Result<PayrollBulkAssignmentResultDto> { Success = false, StatusCode = saved.StatusCode, Message = saved.Message };
        return new Result<PayrollBulkAssignmentResultDto> { Success = true, StatusCode = 201, Message = $"{rows.Count} assignments submitted for approval.", MethodResult = new PayrollBulkAssignmentResultDto { SubmittedCount = rows.Count, ConflictCount = preview.MethodResults.Count(x => !x.CanSubmit), Rows = preview.MethodResults } };
    }

    private async Task<ValidatedAssignmentConfiguration> ValidateAssignmentConfiguration(
        string companyId, string? requestedPayGroupId, string? requestedTemplateId, string? requestedPolicyProfileId, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var payGroupId = string.IsNullOrWhiteSpace(requestedPayGroupId) ? null : requestedPayGroupId.Trim();
        var templateId = string.IsNullOrWhiteSpace(requestedTemplateId) ? null : requestedTemplateId.Trim();
        if (templateId is not null)
        {
            var template = await salaryTemplates.FirstOrDefault(x => x.CompanyId == companyId && x.Id == templateId && x.IsActive && !x.IsRetired);
            if (template is null) return Invalid(404, "Salary template was not found in this company or is inactive.");
            if (!Covers(template.EffectiveFrom, template.EffectiveTo, effectiveFrom, effectiveTo))
                return Invalid(400, "The salary template must be effective for the entire assignment date range.");

            var templatePayGroupId = string.IsNullOrWhiteSpace(template.PayGroupId) ? null : template.PayGroupId.Trim();
            if (payGroupId is not null && templatePayGroupId is not null && !string.Equals(payGroupId, templatePayGroupId, StringComparison.Ordinal))
                return Invalid(400, "The salary template is restricted to a different pay group.");
            payGroupId ??= templatePayGroupId;
        }

        if (payGroupId is not null)
        {
            var payGroup = await payGroups.FirstOrDefault(x => x.CompanyId == companyId && x.Id == payGroupId && x.IsActive);
            if (payGroup is null) return Invalid(404, "Pay group was not found in this company or is inactive.");
            if (!string.Equals(payGroup.Schedule?.Trim(), "MONTHLY", StringComparison.OrdinalIgnoreCase))
                return Invalid(400, "Only monthly pay groups can be assigned at this time.");
            if (!Covers(payGroup.EffectiveFrom, payGroup.EffectiveTo, effectiveFrom, effectiveTo))
                return Invalid(400, "The pay group must be effective for the entire assignment date range.");
        }

        var policyProfileId = string.IsNullOrWhiteSpace(requestedPolicyProfileId) ? null : requestedPolicyProfileId.Trim();
        if (policyProfileId is not null)
        {
            var profile = await policyProfiles.FirstOrDefault(x => x.CompanyId == companyId && x.Id == policyProfileId && x.IsActive);
            if (profile is null) return Invalid(404, "Policy profile was not found in this company or is inactive.");
            if (profile.ApprovalStatus != "APPROVED") return Invalid(409, "The assigned policy profile must be approved before it can be linked to a payroll assignment.");
            if (!Covers(profile.EffectiveFrom, profile.EffectiveTo, effectiveFrom, effectiveTo))
                return Invalid(400, "The policy profile must be effective for the entire assignment date range.");
        }

        return new ValidatedAssignmentConfiguration(payGroupId, templateId, policyProfileId, null, null);
    }

    private static bool Covers(DateTime configuredFrom, DateTime? configuredTo, DateTime requestedFrom, DateTime? requestedTo) =>
        configuredFrom != default && configuredFrom.Date <= requestedFrom.Date &&
        (!configuredTo.HasValue || requestedTo.HasValue && requestedTo.Value.Date <= configuredTo.Value.Date);

    private static ValidatedAssignmentConfiguration Invalid(int statusCode, string message) => new(null, null, null, statusCode, message);

    private sealed record ValidatedAssignmentConfiguration(string? PayGroupId, string? SalaryTemplateId, string? PolicyProfileId, int? ErrorCode, string? ErrorMessage);
}
