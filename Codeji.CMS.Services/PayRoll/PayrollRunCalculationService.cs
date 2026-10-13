using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll.Interface;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public interface IPayrollRunCalculationService
{
    Task<Result> SyncCalculatedRows(string companyId, string runId, string actorId);
}

public sealed class PayrollRunCalculationService(
    IMongoDbRepository<PayrollRun> runs,
    IMongoDbRepository<PayrollRunEmployee> runEmployees,
    IMongoDbRepository<EmpPayRoll> payrolls,
    IMongoDbRepository<MonthlyAttendanceSummary>? attendanceSummaries = null,
    IAutoPayRollServices? autoPayRollServices = null) : IPayrollRunCalculationService
{
    public async Task<Result> SyncCalculatedRows(string companyId, string runId, string actorId)
    {
        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == runId);
        if (run is null) return Fail(404, "Payroll run was not found.");
        var calculationStartStatus = run.Status;
        var calculationStartVersion = run.Version;
        if (calculationStartStatus is not ("DRAFT" or "BLOCKED"))
            return Fail(409, "Only a draft or blocked payroll run can be calculated.");

        var members = (await runEmployees.GetAll(x => x.CompanyId == companyId && x.PayrollRunId == runId)).ToList();
        if (members.Count == 0) return Fail(409, "Payroll run has no employees.");

        var month = new DateTime(run.PeriodStart.Year, run.PeriodStart.Month, 1);
        var attendanceRequiredEmployees = members.Where(member => member.Status != "SKIPPED" &&
            (member.InputSnapshot.RequireAttendanceLock ?? run.ConfigurationSnapshot.RequireAttendanceLock))
            .Select(member => member.EmployeeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, MonthlyAttendanceSummary> lockedAttendance = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> ambiguousAttendance = new(StringComparer.OrdinalIgnoreCase);
        if (attendanceRequiredEmployees.Count > 0)
        {
            if (attendanceSummaries is null)
                return Fail(503, "Attendance-lock verification is unavailable; payroll calculation is blocked.");
            var attendanceByEmployee = (await attendanceSummaries.GetAll(x => x.CompanyId == companyId &&
                x.PayrollMonth == month && x.IsApproved && x.IsLocked))
                .Where(x => attendanceRequiredEmployees.Contains(x.EmployeeId))
                .GroupBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase).ToArray();
            ambiguousAttendance = attendanceByEmployee.Where(group => group.Count() > 1)
                .Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            lockedAttendance = attendanceByEmployee.Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);
        }
        var rows = (await payrolls.GetAll(x => x.CompanyId == companyId && x.PayMonth == month))
            .GroupBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);

        if (autoPayRollServices is not null && members.Any(m => m.Status != "SKIPPED" && !rows.ContainsKey(m.EmployeeId)))
        {
            var genResult = await autoPayRollServices.GeneratePayrollForMonthAsync(companyId, month);
            if (genResult.Success)
            {
                rows = (await payrolls.GetAll(x => x.CompanyId == companyId && x.PayMonth == month))
                    .GroupBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);
            }
        }
        var missing = 0;
        decimal gross = 0;
        decimal deductions = 0;
        decimal net = 0;

        foreach (var member in members)
        {
            // Exclusion is part of the captured run population. A source payroll row
            // appearing later must not turn an excluded employee into a payment.
            if (member.Status == "SKIPPED") continue;
            if (attendanceRequiredEmployees.Contains(member.EmployeeId) &&
                !lockedAttendance.ContainsKey(member.EmployeeId))
            {
                var ambiguous = ambiguousAttendance.Contains(member.EmployeeId);
                Block(member, ambiguous ? "PAYROLL_ATTENDANCE_SUMMARY_AMBIGUOUS" : "PAYROLL_ATTENDANCE_NOT_LOCKED",
                    ambiguous
                        ? "Multiple approved and locked attendance summaries exist for this employee and payroll month. Resolve the duplicate summaries before retrying payroll."
                        : "Approved attendance is not locked for this employee and payroll month. Validate and lock attendance, regenerate payroll, then retry the run.");
                missing++;
                if (await SaveLine(companyId, member) is { } attendanceSaveError) return attendanceSaveError;
                continue;
            }

            if (!rows.TryGetValue(member.EmployeeId, out var employeeRows))
            {
                var hasEmployeeSalarySnapshot = !string.IsNullOrWhiteSpace(member.InputSnapshot.SalaryStructureId) || member.InputSnapshot.SalaryStructures.Count > 0;
                Block(member,
                    hasEmployeeSalarySnapshot ? "PAYROLL_ROW_MISSING" : "PAYROLL_EMPLOYEE_SALARY_MISSING",
                    hasEmployeeSalarySnapshot
                        ? "Calculated employee payroll row was not found. Verify the monthly payroll generation completed for this employee, then retry."
                        : "No approved employee-specific salary revision is frozen for this employee. A pay group or salary template alone is not payable; create and approve the employee salary before retrying.");
                missing++;
                if (await SaveLine(companyId, member) is { } saveError) return saveError;
                continue;
            }

            if (employeeRows.Length != 1)
            {
                Block(member, "PAYROLL_ROW_DUPLICATE", "Multiple employee payroll rows exist for this month. Resolve the duplicate source records before retrying calculation.");
                missing++;
                if (await SaveLine(companyId, member) is { } saveError) return saveError;
                continue;
            }
            var row = employeeRows[0];
            if (attendanceRequiredEmployees.Contains(member.EmployeeId) &&
                row.CalculationSnapshot?.AttendanceSummaryVersion != lockedAttendance[member.EmployeeId].Version)
            {
                Block(member, "PAYROLL_ATTENDANCE_SNAPSHOT_MISMATCH",
                    "The monthly payroll row was not calculated from the currently approved and locked attendance version. Regenerate payroll after attendance is locked, then retry.");
                missing++;
                if (await SaveLine(companyId, member) is { } attendanceSnapshotSaveError) return attendanceSnapshotSaveError;
                continue;
            }

            PayrollComponentCalculationOutcome? componentResult = null;
            string? componentError = null;
            var componentSets = new[] { member.InputSnapshot.Components }
                .Concat(member.InputSnapshot.SalaryStructures.Select(x => x.Components))
                .Where(x => x.Count > 0);
            foreach (var components in componentSets)
            {
                var outcome = PayrollComponentCalculationEngine.Calculate(components);
                componentResult ??= outcome;
                if (!outcome.Success)
                {
                    componentError = outcome.ErrorMessage ?? "Payroll component formula is invalid.";
                    member.ErrorCode = outcome.ErrorCode ?? "PAYROLL_COMPONENT_FORMULA_INVALID";
                    break;
                }
            }
            if (componentError is not null)
            {
                Block(member, member.ErrorCode ?? "PAYROLL_COMPONENT_FORMULA_INVALID", componentError);
                missing++;
                if (await SaveLine(companyId, member) is { } saveError) return saveError;
                continue;
            }

            if (member.InputSnapshot.SalaryStructures.Count > 0)
            {
                var expectedRevisions = member.InputSnapshot.SalaryStructures
                    .Where(x => x.Version > 0)
                    .Select(x => (x.SalaryStructureId, x.Version))
                    .OrderBy(x => x.SalaryStructureId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Version)
                    .ToArray();
                var actualRevisions = row.CalculationSnapshot?.SalaryRevisions
                    .Select(x => (x.SalaryStructureId, x.Version))
                    .OrderBy(x => x.SalaryStructureId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Version)
                    .ToArray() ?? [];
                if (!expectedRevisions.SequenceEqual(actualRevisions))
                {
                    Block(member, "PAYROLL_SALARY_SNAPSHOT_MISMATCH", "Calculated payroll row does not match the salary revisions frozen into this payroll run.");
                    missing++;
                    if (await SaveLine(companyId, member) is { } saveError) return saveError;
                    continue;
                }

                if (!SalaryStructuresMatch(member.InputSnapshot.SalaryStructures, row.CalculationSnapshot?.SalaryStructures ?? []))
                {
                    Block(member, "PAYROLL_SALARY_COMPONENT_SNAPSHOT_MISMATCH", "Calculated payroll row does not preserve the salary component values frozen into this payroll run.");
                    missing++;
                    if (await SaveLine(companyId, member) is { } saveError) return saveError;
                    continue;
                }
            }

            var calculatedSnapshot = row.CalculationSnapshot;
            var hasAssignmentSnapshot = !string.IsNullOrWhiteSpace(member.InputSnapshot.AssignmentId) ||
                                        !string.IsNullOrWhiteSpace(member.InputSnapshot.PayGroupId) ||
                                        !string.IsNullOrWhiteSpace(member.InputSnapshot.SalaryTemplateId) ||
                                        member.InputSnapshot.AssignmentVersion != 0 || member.InputSnapshot.PayGroupVersion != 0 || member.InputSnapshot.SalaryTemplateVersion != 0;
            if (hasAssignmentSnapshot && (calculatedSnapshot is null ||
                !string.Equals(member.InputSnapshot.AssignmentId, calculatedSnapshot.PayrollAssignmentId, StringComparison.Ordinal) ||
                member.InputSnapshot.AssignmentVersion != calculatedSnapshot.PayrollAssignmentVersion ||
                !string.Equals(member.InputSnapshot.PayGroupId, calculatedSnapshot.PayGroupId, StringComparison.Ordinal) ||
                member.InputSnapshot.PayGroupVersion != calculatedSnapshot.PayGroupVersion ||
                !string.Equals(member.InputSnapshot.SalaryTemplateId, calculatedSnapshot.SalaryTemplateId, StringComparison.Ordinal) ||
                member.InputSnapshot.SalaryTemplateVersion != calculatedSnapshot.SalaryTemplateVersion))
            {
                Block(member, "PAYROLL_ASSIGNMENT_SNAPSHOT_MISMATCH", "Calculated payroll row does not match the assignment, pay group, or salary template frozen into this payroll run.");
                missing++;
                if (await SaveLine(companyId, member) is { } saveError) return saveError;
                continue;
            }

            var frozenPolicyProfileId = member.InputSnapshot.PolicyProfileId;
            var frozenPolicyProfileVersion = member.InputSnapshot.PolicyProfileVersion;
            var calculatedPolicyProfileId = row.CalculationSnapshot?.ResolvedPolicyProfileId;
            var calculatedPolicyProfileVersion = row.CalculationSnapshot?.ResolvedPolicyVersion ?? 0;
            var hasPolicySnapshot = !string.IsNullOrWhiteSpace(frozenPolicyProfileId) ||
                                    frozenPolicyProfileVersion != 0 ||
                                    !string.IsNullOrWhiteSpace(calculatedPolicyProfileId) ||
                                    calculatedPolicyProfileVersion != 0;
            if (hasPolicySnapshot &&
                (!string.Equals(frozenPolicyProfileId, calculatedPolicyProfileId, StringComparison.Ordinal) ||
                 frozenPolicyProfileVersion != calculatedPolicyProfileVersion))
            {
                Block(member, "PAYROLL_POLICY_SNAPSHOT_MISMATCH", "Calculated payroll row does not match the statutory policy profile and version frozen into this payroll run.");
                missing++;
                if (await SaveLine(companyId, member) is { } saveError) return saveError;
                continue;
            }

            var hasFrozenPolicySettings = !string.IsNullOrWhiteSpace(member.InputSnapshot.EpfMode) ||
                                          !string.IsNullOrWhiteSpace(member.InputSnapshot.EsicMode) ||
                                          !string.IsNullOrWhiteSpace(member.InputSnapshot.TdsMode);
            if (hasFrozenPolicySettings && (calculatedSnapshot is null ||
                !string.Equals(member.InputSnapshot.EpfMode, calculatedSnapshot.EpfMode, StringComparison.OrdinalIgnoreCase) ||
                member.InputSnapshot.EpfWageCeiling != calculatedSnapshot.EpfWageCeiling ||
                member.InputSnapshot.EpfEmployeeRatePercent != calculatedSnapshot.EpfEmployeeRatePercent ||
                member.InputSnapshot.EpfEmployerRatePercent != calculatedSnapshot.EpfEmployerRatePercent ||
                !string.Equals(member.InputSnapshot.EsicMode, calculatedSnapshot.EsicMode, StringComparison.OrdinalIgnoreCase) ||
                member.InputSnapshot.EsicWageCeiling != calculatedSnapshot.EsicWageCeiling ||
                member.InputSnapshot.EsicEmployeeRatePercent != calculatedSnapshot.EsicEmployeeRatePercent ||
                member.InputSnapshot.EsicEmployerRatePercent != calculatedSnapshot.EsicEmployerRatePercent ||
                !string.Equals(member.InputSnapshot.TdsMode, calculatedSnapshot.TdsMode, StringComparison.OrdinalIgnoreCase)))
            {
                Block(member, "PAYROLL_POLICY_SETTINGS_SNAPSHOT_MISMATCH", "Calculated payroll row does not use the statutory modes, rates, or ceilings frozen into this payroll run.");
                missing++;
                if (await SaveLine(companyId, member) is { } saveError) return saveError;
                continue;
            }

            if (HasInvalidSourceAmounts(row))
            {
                Block(member, "PAYROLL_SOURCE_ROW_INVALID", "The employee payroll row contains a negative or malformed earning/deduction amount. Correct or regenerate the source row before retrying.");
                missing++;
                if (await SaveLine(companyId, member) is { } invalidRowSaveError) return invalidRowSaveError;
                continue;
            }

            // Validate the saved itemized earnings before using this payroll row.
            if (row.EarningLines is { Count: > 0 } earningLines &&
                PayrollCalculationRules.RoundMoney(earningLines.Sum(line => line.Amount)) !=
                PayrollCalculationRules.RoundMoney(row.Allowance.OtherAllowance))
            {
                Block(member, "PAYROLL_EARNING_LINES_MISMATCH",
                    "Itemized earning lines do not match the payroll row's Other Allowance total. Correct or regenerate the employee payroll row before retrying.");
                missing++;
                if (await SaveLine(companyId, member) is { } earningLineSaveError) return earningLineSaveError;
                continue;
            }

            // Payroll rows already include attendance proration, LOP, and statutory deductions.
            // Frozen component snapshots validate inputs; they do not replace payroll row totals.
            var earning = row.BasicPay + row.Bonus + row.Allowance.HRA + row.Allowance.LTA + row.Allowance.OtherAllowance;
            var deduction = row.Deduction.LossOfPay + row.Deduction.IncomeTax + row.Deduction.HealthInsurance + row.Deduction.EPF + row.Deduction.ESIC + PayrollDeductionLineTotals.AdditionalAmount(row.DeductionLines);
            member.GrossAmount = PayrollCalculationRules.RoundMoney(Math.Max(0m, earning));
            member.DeductionAmount = PayrollCalculationRules.RoundMoney(Math.Max(0m, deduction));
            if (member.DeductionAmount > member.GrossAmount)
            {
                Block(member, "PAYROLL_NET_NEGATIVE", "Employee deductions exceed gross pay. Review salary, attendance, and deduction inputs before retrying this payroll run.");
                missing++;
                if (await SaveLine(companyId, member) is { } saveError) return saveError;
                continue;
            }
            member.Status = "CALCULATED";
            member.ErrorCode = null;
            member.ErrorMessage = null;
            member.NetAmount = PayrollCalculationRules.RoundMoney(member.GrossAmount - member.DeductionAmount);

            member.StatutorySnapshot = BuildStatutorySnapshot(member, componentResult, row);
            if (await SaveLine(companyId, member) is { } calculatedSaveError) return calculatedSaveError;
            gross += member.GrossAmount;
            deductions += member.DeductionAmount;
            net += member.NetAmount;
        }

        run.EmployeeCount = members.Count;
        run.ReadyCount = members.Count(x => x.Status == "CALCULATED");
        run.BlockedCount = missing;
        run.SkippedCount = members.Count(x => x.Status == "SKIPPED");
        run.GrossTotal = PayrollCalculationRules.RoundMoney(gross);
        run.DeductionTotal = PayrollCalculationRules.RoundMoney(deductions);
        run.NetTotal = PayrollCalculationRules.RoundMoney(net);

        if (missing > 0)
        {
            run.Status = "BLOCKED";
            run.Version++;
            run.AuditHistory.Add(new PayrollRunAuditEvent
            {
                Action = "RunCalculationBlocked",
                ActorId = actorId,
                OccurredAtUtc = DateTime.UtcNow,
                Notes = calculationStartStatus == "BLOCKED"
                    ? "Retry remains blocked; resolve missing or duplicate payroll rows, salary or statutory policy snapshot mismatches, invalid frozen salary formulas, or deductions exceeding gross pay."
                    : "Initial calculation is blocked by missing or duplicate payroll rows, salary or statutory policy snapshot mismatches, invalid frozen salary formulas, or deductions exceeding gross pay."
            });
            var blockedSave = await runs.Update(
                TenantRunFilter(companyId, runId) &
                Builders<PayrollRun>.Filter.Eq(x => x.Status, calculationStartStatus) &
                Builders<PayrollRun>.Filter.Eq(x => x.Version, calculationStartVersion),
                run);
            if (!blockedSave.Success) return Fail(409, "Payroll run changed during calculation; reload before retrying.");
            return Fail(409, $"{missing} employees are blocked by missing or duplicate payroll data, frozen salary/policy snapshot mismatches, invalid component formulas, or deductions exceeding gross pay.");
        }

        run.Status = "CALCULATED";
        run.Version++;
        run.AuditHistory.Add(new PayrollRunAuditEvent
        {
            Action = "RunCalculated",
            ActorId = actorId,
            OccurredAtUtc = DateTime.UtcNow,
            Notes = calculationStartStatus == "BLOCKED"
                ? "Retried a blocked run using its preserved employee, salary and policy snapshots."
                : "Calculated from employee payroll rows and immutable component snapshots."
        });
        var saved = await runs.Update(
            TenantRunFilter(companyId, runId) &
            Builders<PayrollRun>.Filter.Eq(x => x.Status, calculationStartStatus) &
            Builders<PayrollRun>.Filter.Eq(x => x.Version, calculationStartVersion),
            run);
        return saved.Success
            ? new Result { Success = true, ResourceId = runId, Message = "Payroll run calculated from employee payroll rows." }
            : Fail(409, "Payroll run changed by another request; reload and retry.");
    }

    private static PayrollStatutorySnapshot BuildStatutorySnapshot(PayrollRunEmployee member, PayrollComponentCalculationOutcome? componentResult, EmpPayRoll legacyRow)
    {
        var snapshot = new PayrollStatutorySnapshot
        {
            PolicyProfileId = member.InputSnapshot.PolicyProfileId,
            PolicyProfileVersion = member.InputSnapshot.PolicyProfileVersion
        };
        var contractualBasic = Math.Max(0m, legacyRow.BasicPay);
        var contractualGross = Math.Max(0m, legacyRow.BasicPay + legacyRow.Bonus + legacyRow.Allowance.HRA + legacyRow.Allowance.LTA + legacyRow.Allowance.OtherAllowance);
        var earnedGross = Math.Max(0m, contractualGross - legacyRow.Deduction.LossOfPay);
        var earnedBasic = Math.Max(0m, contractualBasic - Math.Min(contractualBasic, legacyRow.Deduction.LossOfPay));
        snapshot.EmployeeEpf = PayrollCalculationRules.RoundMoney(Math.Max(0m, legacyRow.Deduction.EPF));
        snapshot.EmployerEpf = PayrollCalculationRules.CalculateMonthlyEpf(
            legacyRow.CalculationSnapshot?.EpfMode, earnedBasic,
            legacyRow.CalculationSnapshot?.EpfWageCeiling ?? 15000m,
            legacyRow.CalculationSnapshot?.EpfEmployerRatePercent ?? 12m);
        snapshot.EmployeeEsic = PayrollCalculationRules.RoundMoney(Math.Max(0m, legacyRow.Deduction.ESIC));
        snapshot.EmployerEsic = PayrollCalculationRules.CalculateEmployerMonthlyEsic(
            legacyRow.CalculationSnapshot?.EsicMode, contractualGross, earnedGross,
            legacyRow.CalculationSnapshot?.EsicWageCeiling ?? 21000m,
            legacyRow.CalculationSnapshot?.EsicEmployerRatePercent ?? 3.25m);
        snapshot.IncomeTax = PayrollCalculationRules.RoundMoney(Math.Max(0m, legacyRow.Deduction.IncomeTax));
        snapshot.ProfessionalTax = componentResult is null ? 0m : StatutoryComponent(componentResult.Values, "PT", "PROFESSIONAL_TAX");
        return snapshot;    }

    private static decimal StatutoryComponent(IReadOnlyDictionary<string, decimal> values, params string[] codes)
    {
        foreach (var code in codes)
            if (values.TryGetValue(code, out var amount)) return PayrollCalculationRules.RoundMoney(Math.Max(0m, amount));
        return 0m;
    }

    private static bool SalaryStructuresMatch(
        IReadOnlyCollection<PayrollSalaryStructureSnapshot> expected,
        IReadOnlyCollection<PayrollSalaryStructureSnapshot> actual)
    {
        if (expected.Count != actual.Count) return false;
        var expectedStructures = expected.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.SalaryStructureId, StringComparer.OrdinalIgnoreCase).ToArray();
        var actualStructures = actual.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.SalaryStructureId, StringComparer.OrdinalIgnoreCase).ToArray();
        for (var index = 0; index < expectedStructures.Length; index++)
        {
            var left = expectedStructures[index];
            var right = actualStructures[index];
            if (!string.Equals(left.SalaryStructureId, right.SalaryStructureId, StringComparison.Ordinal) ||
                left.Version != right.Version || !string.Equals(left.PayGroupId, right.PayGroupId, StringComparison.Ordinal) ||
                left.EffectiveFrom.Date != right.EffectiveFrom.Date || left.EffectiveTo?.Date != right.EffectiveTo?.Date)
                return false;

            var leftComponents = left.Components.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase).ToArray();
            var rightComponents = right.Components.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase).ToArray();
            if (leftComponents.Length != rightComponents.Length) return false;
            for (var componentIndex = 0; componentIndex < leftComponents.Length; componentIndex++)
            {
                var leftComponent = leftComponents[componentIndex];
                var rightComponent = rightComponents[componentIndex];
                if (!string.Equals(leftComponent.Code, rightComponent.Code, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(leftComponent.Name, rightComponent.Name, StringComparison.Ordinal) ||
                    !string.Equals(leftComponent.Kind, rightComponent.Kind, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(leftComponent.CalculationType, rightComponent.CalculationType, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(leftComponent.BaseComponentCode, rightComponent.BaseComponentCode, StringComparison.OrdinalIgnoreCase) ||
                    leftComponent.Value != rightComponent.Value || leftComponent.IsTaxable != rightComponent.IsTaxable ||
                    leftComponent.IsRecurring != rightComponent.IsRecurring || leftComponent.IsStatutory != rightComponent.IsStatutory ||
                    leftComponent.IsEmployerContribution != rightComponent.IsEmployerContribution || leftComponent.SortOrder != rightComponent.SortOrder)
                    return false;
            }
        }
        return true;
    }

    private async Task<Result?> SaveLine(string companyId, PayrollRunEmployee member)
    {
        var saved = await runEmployees.Update(
            Builders<PayrollRunEmployee>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollRunEmployee>.Filter.Eq(x => x.PayrollRunId, member.PayrollRunId) &
            Builders<PayrollRunEmployee>.Filter.Eq(x => x.Id, member.Id),
            member);
        return saved.Success ? null : new Result
        {
            Success = false,
            StatusCode = saved.StatusCode >= 400 ? saved.StatusCode : 409,
            ErrorCode = "PAYROLL_CALCULATION_WRITE_FAILED",
            Message = $"Calculation result for employee {member.EmployeeId} could not be saved. The run was not marked calculated. Reload and retry."
        };
    }

    private static bool HasInvalidSourceAmounts(EmpPayRoll row)
    {
        if (row.Allowance is null || row.Deduction is null) return true;
        return row.BasicPay < 0 || row.Bonus < 0 || row.PaidDays < 0 ||
               row.Allowance.HRA < 0 || row.Allowance.LTA < 0 || row.Allowance.OtherAllowance < 0 ||
               row.Deduction.LossOfPayDays < 0 || row.Deduction.LossOfPay < 0 || row.Deduction.IncomeTax < 0 ||
               row.Deduction.HealthInsurance < 0 || row.Deduction.EPF < 0 || row.Deduction.ESIC < 0 ||
               (row.EarningLines ?? []).Any(line => line is null || string.IsNullOrWhiteSpace(line.Code) || string.IsNullOrWhiteSpace(line.Description) || line.Amount < 0) ||
               (row.DeductionLines ?? []).Any(line => line is null || string.IsNullOrWhiteSpace(line.Code) || string.IsNullOrWhiteSpace(line.Description) || line.Amount < 0 || line.DayFraction < 0);
    }

    private static void Block(PayrollRunEmployee member, string code, string message)
    {
        member.Status = "BLOCKED";
        member.ErrorCode = code;
        member.ErrorMessage = message;
        member.GrossAmount = 0;
        member.DeductionAmount = 0;
        member.NetAmount = 0;
        member.StatutorySnapshot = new PayrollStatutorySnapshot
        {
            PolicyProfileId = member.InputSnapshot.PolicyProfileId,
            PolicyProfileVersion = member.InputSnapshot.PolicyProfileVersion
        };
    }

    private static FilterDefinition<PayrollRun> TenantRunFilter(string companyId, string runId) =>
        Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, companyId) &
        Builders<PayrollRun>.Filter.Eq(x => x.Id, runId);

    private static Result Fail(int code, string message) => new() { Success = false, StatusCode = code, Message = message };
}
