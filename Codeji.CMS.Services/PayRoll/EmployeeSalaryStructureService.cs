using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public interface IEmployeeSalaryStructureService
{
    Task<List<EmployeeSalaryStructureDto>> GetHistory(string companyId, string employeeId);
    Task<List<EmployeeSalaryStructureDto>> GetPending(string companyId);
    Task<Result<EmployeeSalaryStructureDto>> SaveDraft(string companyId, string actorUserId, EmployeeSalaryStructureDto dto);
    Task<Result> Submit(string companyId, string actorUserId, string id, int version);
    Task<Result> Approve(string companyId, string actorUserId, string id, int version, bool isCompanyAdministrator = false);
    Task<Result> Reject(string companyId, string actorUserId, string id, int version, string? reason = null);
}

public sealed class EmployeeSalaryStructureService(
    IMongoDbRepository<EmployeeSalaryStructure> repository,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<PayrollSalaryComponentDefinition>? componentDefinitions = null,
    IMongoDbRepository<PayrollPayGroup>? payGroups = null,
    IMongoDbRepository<SalaryTemplate>? salaryTemplates = null,
    IMongoClient? mongoClient = null) : IEmployeeSalaryStructureService
{
    public async Task<List<EmployeeSalaryStructureDto>> GetHistory(string companyId, string employeeId) =>
        (await repository.GetAll(x => x.CompanyId == companyId && x.EmployeeId == employeeId.Trim()))
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Version).Select(Map).ToList();

    public async Task<List<EmployeeSalaryStructureDto>> GetPending(string companyId) =>
        (await repository.GetAll(x => x.CompanyId == companyId && (x.ApprovalStatus == "DRAFT" || x.ApprovalStatus == "SUBMITTED")))
            .OrderBy(x => x.EffectiveFrom).ThenBy(x => x.EmployeeId).Select(Map).ToList();

    public async Task<Result<EmployeeSalaryStructureDto>> SaveDraft(string companyId, string actorUserId, EmployeeSalaryStructureDto dto)
    {
        var employeeId = dto.EmployeeId.Trim();
        if (string.IsNullOrWhiteSpace(employeeId) || dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return FailDto(400, "Employee and valid effective dates are required.");
        var paymentFrequency = dto.PaymentFrequency?.Trim().ToUpperInvariant();
        if (paymentFrequency is not ("MONTHLY" or "MONTH"))
            return FailDto(400, "Only monthly payroll is currently supported. Payment cadence follows the assigned payroll pay group.");
        if (!await employees.Exist(x => x.CompanyId == companyId && x.EmployeeId == employeeId && !x.IsDeleted))
            return FailDto(404, "Employee was not found in this company.");

        var salaryTemplateId = string.IsNullOrWhiteSpace(dto.SalaryTemplateId) ? null : dto.SalaryTemplateId.Trim();
        var payGroupId = string.IsNullOrWhiteSpace(dto.PayGroupId) ? null : dto.PayGroupId.Trim();
        if (salaryTemplateId is not null)
        {
            if (salaryTemplates is null) return FailDto(503, "Salary template validation is unavailable; retry after payroll configuration is ready.");
            var template = await salaryTemplates.FirstOrDefault(x => x.CompanyId == companyId && x.Id == salaryTemplateId && x.IsActive);
            if (template is null) return FailDto(404, "Salary template was not found in this company or is inactive.");
            if (template.IsRetired)
            {
                var linkedDraft = string.IsNullOrWhiteSpace(dto.Id) ? null :
                    await repository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == dto.Id);
                if (linkedDraft?.SalaryTemplateId != salaryTemplateId)
                    return FailDto(409, "This salary template is retired. Select an available template for a new salary structure.");
            }
            if (template.EffectiveFrom.Date > dto.EffectiveFrom.Date || (template.EffectiveTo.HasValue && template.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
                return FailDto(400, "The salary template is not effective on the salary structure start date.");
            var templatePayGroupId = string.IsNullOrWhiteSpace(template.PayGroupId) ? null : template.PayGroupId.Trim();
            if (payGroupId is not null && templatePayGroupId is not null && !string.Equals(payGroupId, templatePayGroupId, StringComparison.Ordinal))
                return FailDto(400, "The selected salary template does not belong to the selected pay group.");
            payGroupId ??= templatePayGroupId;
        }
        if (payGroupId is not null)
        {
            if (payGroups is null) return FailDto(503, "Pay group validation is unavailable; retry after payroll configuration is ready.");
            var payGroup = await payGroups.FirstOrDefault(x => x.CompanyId == companyId && x.Id == payGroupId && x.IsActive);
            if (payGroup is null) return FailDto(404, "Pay group was not found in this company or is inactive.");
            if (!string.Equals(payGroup.Schedule?.Trim(), "MONTHLY", StringComparison.OrdinalIgnoreCase))
                return FailDto(400, "The selected pay group does not use the supported monthly payroll schedule.");
            if (payGroup.EffectiveFrom.Date > dto.EffectiveFrom.Date || (payGroup.EffectiveTo.HasValue && payGroup.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
                return FailDto(400, "The pay group is not effective on the salary structure start date.");
        }
        var candidateStart = dto.EffectiveFrom.Date;
        var candidateEnd = dto.EffectiveTo?.Date ?? DateTime.MaxValue.Date;
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
        else
        {
            var openDraft = await repository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.ApprovalStatus == "DRAFT" && x.EffectiveFrom.Date == candidateStart);
            if (openDraft is not null)
            {
                existingRecord = openDraft;
                dto.Id = openDraft.Id;
                dto.Version = openDraft.Version;
            }
        }
        var existing = (await repository.GetAll(x => x.CompanyId == companyId && x.EmployeeId == employeeId)).ToList();
        var overlaps = existing.Where(x => x.Id != dto.Id && !(x.EffectiveFrom.Date < candidateStart && !x.EffectiveTo.HasValue && x.ApprovalStatus == "APPROVED" && x.Status == "ACTIVE") && x.EffectiveFrom.Date <= candidateEnd && (x.EffectiveTo?.Date ?? DateTime.MaxValue.Date) >= candidateStart).ToList();
        if (overlaps.Any(x => x.ApprovalStatus is "APPROVED" or "SUBMITTED"))
            return FailDto(409, "The employee already has an overlapping approved or submitted salary structure.");

        var mappedComponents = dto.Components.Select(Map).ToList();
        if (mappedComponents.Any(x => string.Equals(x.ComponentCode.Trim(), "TOTAL_DEDUCTIONS", StringComparison.OrdinalIgnoreCase)))
            return FailDto(400, "Total Deductions is an aggregate. Use itemized deduction components in the salary structure.");
        if (mappedComponents.GroupBy(x => x.ComponentCode, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            return FailDto(400, "Salary structure contains duplicate component codes.");

        if (componentDefinitions is not null)
        {
            var companyDefinitions = (await componentDefinitions.GetAll(x => x.CompanyId == companyId)).ToList();
            // Empty catalogs indicate a legacy tenant; preserve its existing
            // fixed-field salary structures until payroll settings are configured.
            if (companyDefinitions.Count > 0)
            {
                var asOf = candidateStart;
                var resolvedComponents = new List<EmployeeSalaryComponentValue>();
                foreach (var component in mappedComponents)
                {
                    var code = component.ComponentCode.Trim().ToUpperInvariant();
                    if (code is "BASIC" or "BASIC_PAY")
                    {
                        resolvedComponents.Add(component);
                        continue;
                    }
                    var definition = PayrollComponentDefinitionResolver.Resolve(companyDefinitions, companyId, code, asOf);
                    if (definition is null)
                    {
                        // Gracefully prune unconfigured components with 0 amount/rate (e.g. blank HRA/LTA)
                        // so employee creation or salary updates are not blocked on unassigned heads.
                        if (component.Value == 0 && (!component.Percentage.HasValue || component.Percentage.Value == 0))
                            continue;

                        return FailDto(400, $"Payroll component '{code}' is not enabled for this company on {asOf:yyyy-MM-dd}.");
                    }
                    if (!string.Equals(component.Kind, definition.Kind, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(component.CalculationType, definition.CalculationType, StringComparison.OrdinalIgnoreCase) ||
                        component.IsTaxable != definition.IsTaxable ||
                        !string.Equals(component.BaseComponentCode ?? string.Empty, definition.BaseComponentCode ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                        return FailDto(400, $"Payroll component '{code}' does not match its approved company definition.");

                    component.ComponentName = definition.Name;
                    component.Kind = definition.Kind.Trim().ToUpperInvariant();
                    component.CalculationType = definition.CalculationType.Trim().ToUpperInvariant();
                    component.BaseComponentCode = definition.BaseComponentCode?.Trim().ToUpperInvariant();
                    component.IsTaxable = definition.IsTaxable;
                    resolvedComponents.Add(component);
                }
                mappedComponents = resolvedComponents;
            }
        }
        var calculatedMonthlyGross = dto.MonthlyGross;
        if (mappedComponents.Count > 0)
        {
            var formulaCheck = PayrollComponentCalculationEngine.Calculate(mappedComponents.Select((x, index) => new PayrollComponentSnapshot
            {
                Code = x.ComponentCode, Name = x.ComponentName, Kind = x.Kind,
                CalculationType = x.CalculationType, BaseComponentCode = x.BaseComponentCode,
                Value = x.Percentage ?? x.Value, IsTaxable = x.IsTaxable, IsRecurring = x.IsRecurring,
                IsStatutory = x.IsStatutory, IsEmployerContribution = x.IsEmployerContribution, SortOrder = index
            }));
            if (!formulaCheck.Success)
                return FailDto(400, formulaCheck.ErrorMessage ?? "Salary component formula is invalid.");
            calculatedMonthlyGross = formulaCheck.Gross;
        }

        var entity = new EmployeeSalaryStructure
        {
            Id = dto.Id ?? string.Empty, CompanyId = companyId, EmployeeId = employeeId, UserId = dto.UserId,
            OfferId = dto.OfferId, OfferVersion = dto.OfferVersion, SalaryTemplateId = salaryTemplateId,
            PayGroupId = payGroupId, EmployeeType = dto.EmployeeType, GradeId = dto.GradeId, DepartmentId = dto.DepartmentId,
            Currency = dto.Currency.Trim().ToUpperInvariant(), PaymentFrequency = "MONTHLY",
            AnnualCtc = dto.AnnualCtc, AnnualCtcSource = NormalizeAnnualCtcSource(dto.AnnualCtcSource), MonthlyGross = calculatedMonthlyGross, EffectiveFrom = candidateStart,
            EffectiveTo = dto.EffectiveTo?.Date, Status = "DRAFT", ApprovalStatus = "DRAFT", Source = dto.Source.Trim().ToUpperInvariant(),
            Version = existingRecord is null ? Math.Max(1, dto.Version) : existingRecord.Version + 1,
            TaxRegime = dto.TaxRegime?.Trim().ToUpperInvariant(),
            EpfModeOverride = dto.EpfModeOverride?.Trim().ToUpperInvariant(),
            EsicModeOverride = dto.EsicModeOverride?.Trim().ToUpperInvariant(),
            AttendanceTrackingMode = dto.AttendanceTrackingMode?.Trim().ToUpperInvariant(),
            CreatedBy = existingRecord?.CreatedBy ?? actorUserId, Components = mappedComponents
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

    public async Task<Result> Approve(string companyId, string actorUserId, string id, int version, bool isCompanyAdministrator = false)
    {
        var row = await repository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        if (row is null) return Fail(404, "Salary structure was not found.");
        if (row.Version != version) return Fail(409, "Salary structure changed; reload and retry.");

        // If a company administrator approves a draft directly, auto-advance it to submitted
        if (row.ApprovalStatus == "DRAFT" && isCompanyAdministrator)
        {
            row.ApprovalStatus = "SUBMITTED";
            row.SubmittedBy = actorUserId;
        }

        if (row.ApprovalStatus != "SUBMITTED") return Fail(409, "Salary structure must be submitted before approval.");

        if (!isCompanyAdministrator &&
            ((!string.IsNullOrWhiteSpace(row.CreatedBy) && string.Equals(row.CreatedBy, actorUserId, StringComparison.OrdinalIgnoreCase)) ||
             (!string.IsNullOrWhiteSpace(row.SubmittedBy) && string.Equals(row.SubmittedBy, actorUserId, StringComparison.OrdinalIgnoreCase))))
            return Fail(403, "A salary structure must be approved by someone other than its creator or submitter.");
        var overlaps = (await repository.GetAll(x => x.CompanyId == companyId && x.EmployeeId == row.EmployeeId && x.Id != row.Id &&
            x.ApprovalStatus == "APPROVED" && x.Status == "ACTIVE" &&
            x.EffectiveFrom.Date <= (row.EffectiveTo ?? DateTime.MaxValue).Date &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= row.EffectiveFrom.Date))).ToList();
        var priorOpenRows = overlaps.Where(x => !x.EffectiveTo.HasValue && x.EffectiveFrom.Date < row.EffectiveFrom.Date)
            .OrderByDescending(x => x.EffectiveFrom.Date).ToList();
        if (priorOpenRows.Count > 1)
            return Fail(409, "More than one open approved salary revision already overlaps this effective date. Reconcile the employee's salary history before approving another revision.");
        var priorOpen = priorOpenRows.FirstOrDefault();
        var conflictingOverlap = overlaps.FirstOrDefault(x => priorOpen is null || x.Id != priorOpen.Id);
        if (conflictingOverlap is not null)
            return Fail(409, $"This salary revision overlaps approved salary revision '{conflictingOverlap.Id}'. Resolve the overlapping effective dates before approval.");
        var allowedApprovalStatuses = isCompanyAdministrator ? new[] { "SUBMITTED", "DRAFT" } : new[] { "SUBMITTED" };
        var approvalFilter = Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.CompanyId, companyId) &
                             Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, id) &
                             Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Version, version) &
                             Builders<EmployeeSalaryStructure>.Filter.In(x => x.ApprovalStatus, allowedApprovalStatuses);
        if (priorOpen is not null)
        {
            if (mongoClient is null)
                return Fail(503, "Salary revision approval requires MongoDB transaction support to safely close the prior revision and activate the new one.");

            var priorVersion = priorOpen.Version;
            var originalApprovalStatus = row.ApprovalStatus;
            var originalStatus = row.Status;
            var originalApprovedBy = row.ApprovedBy;
            var originalApprovedAtUtc = row.ApprovedAtUtc;
            var committed = false;
            var priorEnd = row.EffectiveFrom.Date.AddDays(-1);
            priorOpen.EffectiveTo = priorEnd;
            priorOpen.Version = priorVersion + 1;
            var closeFilter = Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, priorOpen.Id) &
                              Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.CompanyId, companyId) &
                              Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Version, priorVersion) &
                              Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.ApprovalStatus, "APPROVED") &
                              Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Status, "ACTIVE") &
                              Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.EffectiveTo, null);
            row.ApprovalStatus = "APPROVED"; row.Status = "ACTIVE"; row.ApprovedBy = actorUserId; row.ApprovedAtUtc = DateTime.UtcNow; row.Version++;
            try
            {
                using var session = await mongoClient.StartSessionAsync();
                await session.WithTransactionAsync(async (transaction, cancellationToken) =>
                {
                    var close = await repository.GetCollection().ReplaceOneAsync(transaction, closeFilter, priorOpen, cancellationToken: cancellationToken);
                    if (close.MatchedCount != 1) throw new SalaryApprovalConcurrencyException();
                    var approve = await repository.GetCollection().ReplaceOneAsync(transaction, approvalFilter, row, cancellationToken: cancellationToken);
                    if (approve.MatchedCount != 1) throw new SalaryApprovalConcurrencyException();
                    return true;
                });
                committed = true;
                return new Result { Success = true, StatusCode = 200, ResourceId = row.Id, Message = "Salary structure approved." };
            }
            catch (SalaryApprovalConcurrencyException)
            {
                return Fail(409, "A salary revision changed during approval. No salary timeline changes were committed; reload both revisions and retry.");
            }
            catch (Exception ex) when (ex is NotSupportedException || (ex is MongoCommandException exception && exception.Code == 20))
            {
                // Standalone MongoDB deployments do not support multi-document transactions.
                // Fall back to sequential optimistic concurrency updates with compensating rollback on failure.
                var close = await repository.GetCollection().ReplaceOneAsync(closeFilter, priorOpen);
                if (close.MatchedCount != 1)
                {
                    return Fail(409, "A salary revision changed during approval. No salary timeline changes were committed; reload both revisions and retry.");
                }

                var approve = await repository.GetCollection().ReplaceOneAsync(approvalFilter, row);
                if (approve.MatchedCount != 1)
                {
                    // Compensating rollback: revert prior revision back to open/active state
                    priorOpen.EffectiveTo = null;
                    priorOpen.Version = priorVersion;
                    await repository.GetCollection().ReplaceOneAsync(
                        Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, priorOpen.Id) &
                        Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Version, priorVersion + 1),
                        priorOpen);
                    return Fail(409, "A salary revision changed during approval. Prior revision was reverted; reload and retry.");
                }

                committed = true;
                return new Result { Success = true, StatusCode = 200, ResourceId = row.Id, Message = "Salary structure approved." };
            }
            catch (MongoException)
            {
                return Fail(503, "MongoDB could not confirm the salary approval transaction. Reload salary history to verify its outcome before retrying.");
            }
            finally
            {
                if (!committed)
                {
                    priorOpen.EffectiveTo = null;
                    priorOpen.Version = priorVersion;
                    row.ApprovalStatus = originalApprovalStatus;
                    row.Status = originalStatus;
                    row.ApprovedBy = originalApprovedBy;
                    row.ApprovedAtUtc = originalApprovedAtUtc;
                    row.Version = version;
                }
            }
        }
        row.ApprovalStatus = "APPROVED"; row.Status = "ACTIVE"; row.ApprovedBy = actorUserId; row.ApprovedAtUtc = DateTime.UtcNow; row.Version++;
        return await repository.Update(approvalFilter, row);
    }

    public async Task<Result> Reject(string companyId, string actorUserId, string id, int version, string? reason = null)
    {
        var row = await repository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        if (row is null) return Fail(404, "Salary structure was not found.");
        if (row.Version != version) return Fail(409, "Salary structure changed; reload and retry.");
        if (row.ApprovalStatus != "DRAFT" && row.ApprovalStatus != "SUBMITTED")
            return Fail(409, "Only draft or submitted salary structures can be rejected or discarded.");

        row.ApprovalStatus = "REJECTED";
        row.Status = "REJECTED";
        row.Version++;
        return await repository.Update(
            Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Id, id) &
            Builders<EmployeeSalaryStructure>.Filter.Eq(x => x.Version, version),
            row);
    }

    private static EmployeeSalaryStructureDto Map(EmployeeSalaryStructure x) => new()
    {
        Id = x.Id, EmployeeId = x.EmployeeId, UserId = x.UserId, OfferId = x.OfferId, OfferVersion = x.OfferVersion,
        SalaryTemplateId = x.SalaryTemplateId, PayGroupId = x.PayGroupId, EmployeeType = x.EmployeeType, GradeId = x.GradeId,
        DepartmentId = x.DepartmentId, Currency = x.Currency, PaymentFrequency = x.PaymentFrequency, AnnualCtc = x.AnnualCtc, AnnualCtcSource = x.AnnualCtcSource,
        MonthlyGross = x.MonthlyGross, EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, Status = x.Status,
        ApprovalStatus = x.ApprovalStatus, Source = x.Source, Version = x.Version,
        TaxRegime = x.TaxRegime, EpfModeOverride = x.EpfModeOverride, EsicModeOverride = x.EsicModeOverride, AttendanceTrackingMode = x.AttendanceTrackingMode,
        SubmittedBy = x.SubmittedBy, Components = x.Components.Select(Map).ToList()
    };
    private static EmployeeSalaryComponentValueDto Map(EmployeeSalaryComponentValue x) => new() { ComponentCode = x.ComponentCode, ComponentName = x.ComponentName, Kind = x.Kind, Value = x.Value, Percentage = x.Percentage, CalculationType = x.CalculationType, BaseComponentCode = x.BaseComponentCode, IsTaxable = x.IsTaxable, IsRecurring = x.IsRecurring, IsStatutory = x.IsStatutory, IsEmployerContribution = x.IsEmployerContribution };
    private static EmployeeSalaryComponentValue Map(EmployeeSalaryComponentValueDto x) => new() { ComponentCode = x.ComponentCode.Trim().ToUpperInvariant(), ComponentName = x.ComponentName.Trim(), Kind = x.Kind.Trim().ToUpperInvariant(), Value = x.Value, Percentage = x.Percentage, CalculationType = x.CalculationType.Trim().ToUpperInvariant(), BaseComponentCode = x.BaseComponentCode?.Trim().ToUpperInvariant(), IsTaxable = x.IsTaxable, IsRecurring = x.IsRecurring, IsStatutory = x.IsStatutory, IsEmployerContribution = x.IsEmployerContribution };
    private static string NormalizeAnnualCtcSource(string? source) => source?.Trim().ToUpperInvariant() switch
    {
        "OFFER_PDF" => "OFFER_PDF",
        "SPREADSHEET" => "SPREADSHEET",
        "ANNUALIZED_GROSS_ESTIMATE" => "ANNUALIZED_GROSS_ESTIMATE",
        _ => "USER_ENTERED"
    };
    private static Result<EmployeeSalaryStructureDto> Ok(EmployeeSalaryStructure x) => new() { Success = true, StatusCode = 201, ResourceId = x.Id, MethodResult = Map(x), Message = "Salary structure saved." };
    private static Result<EmployeeSalaryStructureDto> FailDto(int code, string message) => new() { Success = false, StatusCode = code, Message = message };
    private static Result Fail(int code, string message) => new() { Success = false, StatusCode = code, Message = message };

    private sealed class SalaryApprovalConcurrencyException : Exception;
}




