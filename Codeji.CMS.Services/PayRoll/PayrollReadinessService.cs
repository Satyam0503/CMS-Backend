using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public sealed class PayrollReadinessService(
    IMongoDbRepository<Company> companies,
    IMongoDbRepository<PayrollPolicyProfile> policies,
    IMongoDbRepository<SalaryTemplate> templates,
    IMongoDbRepository<PayrollPayGroup> payGroups,
    IMongoDbRepository<PayrollSalaryComponentDefinition>? componentDefinitions = null,
    IMongoDbRepository<EmployeePayrollAssignment>? assignments = null,
    IMongoDbRepository<EmployeeSalaryStructure>? salaryStructures = null,
    IMongoDbRepository<EmpUser>? employees = null,
    IMongoDbRepository<MonthlyAttendanceSummary>? attendanceSummaries = null) : IPayrollReadinessService
{
    public async Task<PayrollReadinessReportDto> ValidateCompanyPayrollReadiness(string companyId, DateTime targetMonth)
    {
        var periodStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        var periodEnd = new DateTime(targetMonth.Year, targetMonth.Month, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month));

        var report = new PayrollReadinessReportDto
        {
            CompanyId = companyId,
            TargetPeriod = periodStart,
            IsReady = true
        };

        // 1. Company Profile & State Registration
        var company = await companies.FirstOrDefault(x => x.CompanyId == companyId);
        if (company is null)
        {
            report.IsReady = false;
            report.BlockingIssues.Add("Company entity does not exist.");
            report.Steps.Add(new PayrollSetupStepStatusDto
            {
                StepKey = "COMPANY_PROFILE",
                Title = "Company Registration & State",
                Status = "BLOCKED",
                Details = "Company entity does not exist.",
                IsBlocking = true
            });
            return report;
        }

        report.CompanyName = company.CompanyName;
        var hasValidState = !string.IsNullOrWhiteSpace(company.State);
        var companyStep = new PayrollSetupStepStatusDto
        {
            StepKey = "COMPANY_PROFILE",
            Title = "Company Registration & State",
            Status = hasValidState ? "READY" : "BLOCKED",
            Details = hasValidState
                ? $"Company '{company.CompanyName}' registered in state '{company.State}'."
                : $"Company '{company.CompanyName}' has no registered State. State is required to resolve statutory professional tax and labor rules.",
            IsBlocking = !hasValidState
        };
        if (!hasValidState)
        {
            report.IsReady = false;
            report.BlockingIssues.Add(companyStep.Details);
        }
        report.Steps.Add(companyStep);

        // 2. Active Approved Payroll Policy Profile
        var allPolicies = (await policies.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsCancelled)).ToList();
        var approvedPolicies = allPolicies.Where(x =>
            string.Equals(x.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
            x.EffectiveFrom.Date <= periodEnd.Date &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= periodStart.Date)).ToList();

        var policyStep = new PayrollSetupStepStatusDto
        {
            StepKey = "PAYROLL_POLICY",
            Title = "Statutory Payroll Policy",
            IsBlocking = true
        };

        PayrollPolicyProfile? activePolicy = null;
        if (approvedPolicies.Count == 0)
        {
            report.IsReady = false;
            policyStep.Status = "BLOCKED";
            policyStep.Details = $"No approved, active payroll policy profile covers period {periodStart:yyyy-MM}. Create and approve a policy profile before running payroll.";
            report.BlockingIssues.Add(policyStep.Details);
        }
        else
        {
            activePolicy = approvedPolicies.OrderByDescending(x => x.EffectiveFrom).First();
            var missingStatutoryMeta = (activePolicy.EpfMode != "EXEMPT" || activePolicy.EsicMode != "EXEMPT" || activePolicy.TdsMode != "MANUAL_ONLY") &&
                (string.IsNullOrWhiteSpace(activePolicy.StatutoryJurisdiction) ||
                 string.IsNullOrWhiteSpace(activePolicy.FinancialYear) ||
                 string.IsNullOrWhiteSpace(activePolicy.StatutoryRuleVersion));

            if (missingStatutoryMeta)
            {
                report.IsReady = false;
                policyStep.Status = "BLOCKED";
                policyStep.Details = $"Active policy '{activePolicy.Name}' is missing required statutory metadata (Jurisdiction, Financial Year, Rule Version).";
                report.BlockingIssues.Add(policyStep.Details);
            }
            else
            {
                policyStep.Status = "READY";
                policyStep.Details = $"Active policy '{activePolicy.Name}' (v{activePolicy.Version}, Divisor: {activePolicy.DivisorPolicy}, EPF: {activePolicy.EpfMode}, ESIC: {activePolicy.EsicMode}, TDS: {activePolicy.TdsMode}).";
            }
        }
        report.Steps.Add(policyStep);

        // 3. Salary Component Catalog
        var compStep = new PayrollSetupStepStatusDto
        {
            StepKey = "COMPONENTS",
            Title = "Salary Components Catalog",
            IsBlocking = true
        };
        List<PayrollSalaryComponentDefinition> componentList = [];
        if (componentDefinitions is not null)
        {
            componentList = (await componentDefinitions.GetAll(x => x.CompanyId == companyId && x.IsActive)).ToList();
        }
        if (componentList.Count == 0)
        {
            report.IsReady = false;
            compStep.Status = "BLOCKED";
            compStep.Details = "No active salary components configured in catalog. Seed or configure standard Indian salary components.";
            report.BlockingIssues.Add(compStep.Details);
        }
        else
        {
            compStep.Status = "READY";
            compStep.Details = $"{componentList.Count} salary component definitions active ({string.Join(", ", componentList.Take(5).Select(c => c.Code))}{(componentList.Count > 5 ? "..." : "")}).";
        }
        report.Steps.Add(compStep);

        // 4. Salary Templates
        var tplStep = new PayrollSetupStepStatusDto
        {
            StepKey = "TEMPLATES",
            Title = "Salary Templates",
            IsBlocking = true
        };
        var allTemplates = (await templates.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsRetired)).ToList();
        var effectiveTemplates = allTemplates.Where(x =>
            x.EffectiveFrom.Date <= periodEnd.Date &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= periodStart.Date)).ToList();

        if (effectiveTemplates.Count == 0)
        {
            report.IsReady = false;
            tplStep.Status = "BLOCKED";
            tplStep.Details = $"No active salary templates configured for period {periodStart:yyyy-MM}. Define at least one salary template.";
            report.BlockingIssues.Add(tplStep.Details);
        }
        else
        {
            tplStep.Status = "READY";
            tplStep.Details = $"{effectiveTemplates.Count} salary template(s) effective for {periodStart:yyyy-MM}.";
        }
        report.Steps.Add(tplStep);

        // 5. Pay Groups
        var pgStep = new PayrollSetupStepStatusDto
        {
            StepKey = "PAY_GROUPS",
            Title = "Pay Groups",
            IsBlocking = false
        };
        var allGroups = (await payGroups.GetAll(x => x.CompanyId == companyId && x.IsActive)).ToList();
        var effectiveGroups = allGroups.Where(x =>
            x.EffectiveFrom.Date <= periodEnd.Date &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= periodStart.Date)).ToList();

        if (effectiveGroups.Count == 0)
        {
            pgStep.Status = "WARNING";
            pgStep.Details = "No specific pay groups configured. Payroll run will use default company scope.";
            report.Warnings.Add(pgStep.Details);
        }
        else
        {
            pgStep.Status = "READY";
            pgStep.Details = $"{effectiveGroups.Count} active pay group(s) effective for {periodStart:yyyy-MM}.";
        }
        report.Steps.Add(pgStep);

        // 6. Active Employees & Approved Salary Structures
        var activeEmployees = employees is not null
            ? (await employees.GetAll(x => x.CompanyId == companyId && x.Status && !x.IsDeleted)).ToList()
            : [];
        report.TotalActiveEmployees = activeEmployees.Count;

        var empStep = new PayrollSetupStepStatusDto
        {
            StepKey = "EMPLOYEE_SALARIES",
            Title = "Employee Salary Assignments & Structures",
            IsBlocking = true
        };

        if (activeEmployees.Count == 0)
        {
            empStep.Status = "WARNING";
            empStep.Details = "Company has no active employees registered.";
            report.Warnings.Add(empStep.Details);
        }
        else
        {
            var empIds = activeEmployees.Select(x => x.EmployeeId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var empAssignments = assignments is not null
                ? (await assignments.GetAll(x => x.CompanyId == companyId && x.IsApproved && !x.IsCancelled && !x.IsRejected)).ToList()
                : [];
            var activeAssignments = empAssignments.Where(x =>
                empIds.Contains(x.EmployeeId) &&
                x.EffectiveFrom.Date <= periodEnd.Date &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= periodStart.Date)).ToList();

            var assignedEmpIds = activeAssignments.Select(x => x.EmployeeId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var empStructures = salaryStructures is not null
                ? (await salaryStructures.GetAll(x => x.CompanyId == companyId && string.Equals(x.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase))).ToList()
                : [];
            var activeStructures = empStructures.Where(x =>
                empIds.Contains(x.EmployeeId) &&
                x.EffectiveFrom.Date <= periodEnd.Date &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= periodStart.Date)).ToList();

            var structuredEmpIds = activeStructures.Select(x => x.EmployeeId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var fullyConfiguredCount = empIds.Count(id => assignedEmpIds.Contains(id) && structuredEmpIds.Contains(id));
            report.ConfiguredEmployeesCount = fullyConfiguredCount;
            report.MissingAssignmentCount = empIds.Count - assignedEmpIds.Count;
            report.MissingSalaryStructureCount = empIds.Count - structuredEmpIds.Count;

            if (fullyConfiguredCount == 0)
            {
                report.IsReady = false;
                empStep.Status = "BLOCKED";
                empStep.Details = $"None of the {activeEmployees.Count} active employees have both an approved payroll assignment and approved salary structure effective for {periodStart:yyyy-MM}.";
                report.BlockingIssues.Add(empStep.Details);
            }
            else if (fullyConfiguredCount < activeEmployees.Count)
            {
                var missingCount = activeEmployees.Count - fullyConfiguredCount;
                empStep.Status = "WARNING";
                empStep.Details = $"{fullyConfiguredCount} of {activeEmployees.Count} employees configured for payroll. {missingCount} employee(s) missing approved assignment or salary structure.";
                report.Warnings.Add(empStep.Details);
            }
            else
            {
                empStep.Status = "READY";
                empStep.Details = $"All {activeEmployees.Count} active employee(s) have approved payroll assignments and salary structures.";
            }
        }
        report.Steps.Add(empStep);

        // 7. Attendance Lock Verification
        var attStep = new PayrollSetupStepStatusDto
        {
            StepKey = "ATTENDANCE_LOCK",
            Title = "Attendance Lock & Approval",
            IsBlocking = activePolicy?.RequireAttendanceLock ?? true
        };

        if (activePolicy is not null && !activePolicy.RequireAttendanceLock)
        {
            attStep.Status = "READY";
            attStep.Details = "Policy does not require attendance lock. Attendance will not block payroll generation.";
            report.IsAttendanceLocked = true;
        }
        else if (activeEmployees.Count > 0 && attendanceSummaries is not null)
        {
            var summaries = (await attendanceSummaries.GetAll(x =>
                x.PayrollMonth == periodStart &&
                activeEmployees.Select(e => e.EmployeeId).Contains(x.EmployeeId))).ToList();

            var lockedCount = summaries.Count(x => x.IsLocked && x.IsApproved);
            report.IsAttendanceLocked = lockedCount == activeEmployees.Count;

            if (lockedCount < activeEmployees.Count)
            {
                var missingCount = activeEmployees.Count - lockedCount;
                attStep.Status = "BLOCKED";
                attStep.Details = $"Monthly attendance for {missingCount} employee(s) is not approved and locked for {periodStart:yyyy-MM}. Lock attendance before generating payroll.";
                report.IsReady = false;
                report.BlockingIssues.Add(attStep.Details);
            }
            else
            {
                attStep.Status = "READY";
                attStep.Details = $"Monthly attendance for all {activeEmployees.Count} employee(s) is approved and locked.";
            }
        }
        else
        {
            attStep.Status = "READY";
            attStep.Details = "No attendance validation required.";
            report.IsAttendanceLocked = true;
        }
        report.Steps.Add(attStep);

        // 8. Bank Account & PAN Identifiers
        var bankStep = new PayrollSetupStepStatusDto
        {
            StepKey = "BANK_DETAILS",
            Title = "Employee Bank & Tax Identifiers",
            IsBlocking = false
        };
        if (activeEmployees.Count > 0)
        {
            var missingBank = activeEmployees.Count(e => e.BankAccountNumber is null || e.BankAccountNumber == 0);
            var missingPan = activeEmployees.Count(e => string.IsNullOrWhiteSpace(e.PanNumber));
            if (missingBank > 0 || missingPan > 0)
            {
                bankStep.Status = "WARNING";
                bankStep.Details = $"{missingBank} employee(s) missing bank account numbers; {missingPan} employee(s) missing PAN.";
                report.Warnings.Add(bankStep.Details);
            }
            else
            {
                bankStep.Status = "READY";
                bankStep.Details = "All active employees have bank accounts and PAN numbers recorded.";
            }
        }
        report.Steps.Add(bankStep);

        return report;
    }
}
