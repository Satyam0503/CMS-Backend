using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text.Json;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public interface IEmployeeSalaryImportService
{
    Task<List<EmployeeSalaryImportPreviewRowDto>> Preview(string companyId, IReadOnlyCollection<EmployeeSalaryImportRowDto> rows);
    Task<Result> Submit(string companyId, string actorUserId, IReadOnlyCollection<EmployeeSalaryImportRowDto> rows, string? batchId = null);
    Task<EmployeeSalaryImportBatchDto?> GetBatch(string companyId, string batchId);
}

public sealed class EmployeeSalaryImportService(IMongoDbRepository<EmpUser> employees, IMongoDbRepository<EmployeeSalaryStructure> salaryStructures, IEmployeeSalaryStructureService salaries, IMongoDbRepository<EmployeeSalaryImportBatch>? batches = null) : IEmployeeSalaryImportService
{
    public async Task<List<EmployeeSalaryImportPreviewRowDto>> Preview(string companyId, IReadOnlyCollection<EmployeeSalaryImportRowDto> rows)
    {
        var employeeIds = rows.Select(x => x.EmployeeId.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var existing = (await employees.GetAll(x => x.CompanyId == companyId && employeeIds.Contains(x.EmployeeId) && !x.IsDeleted)).Select(x => x.EmployeeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingSalaries = (await salaryStructures.GetAll(x => x.CompanyId == companyId && (x.ApprovalStatus == "APPROVED" || x.ApprovalStatus == "SUBMITTED"))).ToList();
        return rows.Select(row => Validate(row, existing, seen, existingSalaries)).ToList();
    }

    public async Task<EmployeeSalaryImportBatchDto?> GetBatch(string companyId, string batchId)
    {
        if (batches is null || string.IsNullOrWhiteSpace(batchId)) return null;
        var item = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.BatchId == batchId.Trim());
        return item is null ? null : new EmployeeSalaryImportBatchDto
        {
            BatchId = item.BatchId, Status = item.Status, TotalRows = item.TotalRows, ProcessedRows = item.ProcessedRows,
            SkippedRows = item.SkippedRows, FailedRowNumber = item.FailedRowNumber, FailureMessage = item.FailureMessage,
            CreatedAtUtc = item.CreatedAtUtc, UpdatedAtUtc = item.UpdatedAtUtc
        };
    }
    public async Task<Result> Submit(string companyId, string actorUserId, IReadOnlyCollection<EmployeeSalaryImportRowDto> rows, string? batchId = null)
    {
        var normalizedBatchId = batchId?.Trim();
        var requestFingerprint = string.IsNullOrWhiteSpace(normalizedBatchId) ? string.Empty : CreateRequestFingerprint(rows);
        EmployeeSalaryImportBatch? batch = null;
        if (!string.IsNullOrWhiteSpace(normalizedBatchId) && batches is not null)
        {
            batch = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.BatchId == normalizedBatchId);
            if (batch is not null)
            {
                if (!string.IsNullOrEmpty(batch.RequestFingerprint) && !string.Equals(batch.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
                    return new Result { Success = false, StatusCode = 409, Message = "This import batch ID is already bound to a different salary payload." };
                if (batch.Status == "COMPLETED")
                {
                    if (string.IsNullOrEmpty(batch.RequestFingerprint))
                        return new Result { Success = false, StatusCode = 409, Message = "The completed legacy batch cannot be safely replayed because its original payload was not recorded." };
                    return new Result { Success = true, StatusCode = 200, Message = "Salary import batch was already completed." };
                }
                if (batch.Status == "PROCESSING")
                    return new Result { Success = false, StatusCode = 409, Message = "This salary import batch is already processing." };
            }
        }

        var preview = await Preview(companyId, rows);
        if (preview.Any(x => !x.IsValid))
            return new Result { Success = false, StatusCode = 400, Message = "Import preview contains validation errors." };

        if (!string.IsNullOrWhiteSpace(normalizedBatchId) && batches is not null)
        {
            if (batch is null)
            {
                batch = new EmployeeSalaryImportBatch
                {
                    CompanyId = companyId, BatchId = normalizedBatchId, RequestFingerprint = requestFingerprint,
                    TotalRows = rows.Count, ActorUserId = actorUserId
                };
                await batches.AddOne(batch);
            }
            else
            {
                if (batch.TotalRows != rows.Count)
                    return new Result { Success = false, StatusCode = 409, Message = "The retry row count does not match the original salary import batch." };
                batch.RequestFingerprint = requestFingerprint;
            }
            batch.Status = "PROCESSING";
            batch.SkippedRows = 0;
            batch.FailedRowNumber = null;
            batch.FailureMessage = null;
            batch.UpdatedAtUtc = DateTime.UtcNow;
            await batches.Update(Builders<EmployeeSalaryImportBatch>.Filter.Eq(x => x.Id, batch.Id) & Builders<EmployeeSalaryImportBatch>.Filter.Eq(x => x.CompanyId, companyId), batch);
        }

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(normalizedBatchId) && await salaryStructures.Exist(x => x.CompanyId == companyId && x.EmployeeId == row.EmployeeId.Trim() && x.Source == $"IMPORT:{normalizedBatchId}"))
            {
                if (batch is not null) batch.SkippedRows++;
                continue;
            }

            var dto = new EmployeeSalaryStructureDto
            {
                EmployeeId = row.EmployeeId, Currency = row.Currency, PaymentFrequency = row.PaymentFrequency,
                AnnualCtc = row.AnnualCtc, MonthlyGross = row.MonthlyGross, EffectiveFrom = row.EffectiveFrom, EffectiveTo = row.EffectiveTo,
                SalaryTemplateId = row.SalaryTemplateId, PayGroupId = row.PayGroupId, EmployeeType = row.EmployeeType,
                Source = string.IsNullOrWhiteSpace(normalizedBatchId) ? "IMPORT" : $"IMPORT:{normalizedBatchId}", Version = 1,
                Components = row.Components
            };
            var saved = await salaries.SaveDraft(companyId, actorUserId, dto);
            if (!saved.Success)
            {
                if (batch is not null)
                {
                    batch.Status = "PARTIAL_FAILURE";
                    batch.FailedRowNumber = row.RowNumber;
                    batch.FailureMessage = saved.Message;
                    batch.UpdatedAtUtc = DateTime.UtcNow;
                    await batches.Update(Builders<EmployeeSalaryImportBatch>.Filter.Eq(x => x.Id, batch.Id) & Builders<EmployeeSalaryImportBatch>.Filter.Eq(x => x.CompanyId, companyId), batch);
                }
                return new Result { Success = false, StatusCode = saved.StatusCode, Message = $"Row {row.RowNumber}: {saved.Message}" };
            }
            if (batch is not null) batch.ProcessedRows++;
        }

        if (batch is not null)
        {
            batch.Status = "COMPLETED";
            batch.UpdatedAtUtc = DateTime.UtcNow;
            await batches.Update(Builders<EmployeeSalaryImportBatch>.Filter.Eq(x => x.Id, batch.Id) & Builders<EmployeeSalaryImportBatch>.Filter.Eq(x => x.CompanyId, companyId), batch);
        }
        return new Result { Success = true, StatusCode = 201, Message = "Salary import drafts saved and require approval." };
    }

    internal static string CreateRequestFingerprint(IReadOnlyCollection<EmployeeSalaryImportRowDto> rows)
    {
        var canonicalRows = rows
            .OrderBy(x => x.EmployeeId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.RowNumber)
            .Select(x => new
            {
                x.RowNumber,
                EmployeeId = x.EmployeeId.Trim().ToUpperInvariant(),
                Currency = x.Currency.Trim().ToUpperInvariant(),
                PaymentFrequency = x.PaymentFrequency.Trim().ToUpperInvariant(),
                x.AnnualCtc,
                x.MonthlyGross,
                EffectiveFrom = x.EffectiveFrom.Date,
                EffectiveTo = x.EffectiveTo?.Date,
                SalaryTemplateId = x.SalaryTemplateId?.Trim(),
                PayGroupId = x.PayGroupId?.Trim(),
                EmployeeType = x.EmployeeType?.Trim(),
                Components = x.Components
                    .OrderBy(c => c.ComponentCode.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(c => new
                    {
                        Code = c.ComponentCode.Trim().ToUpperInvariant(),
                        Name = c.ComponentName.Trim(),
                        Kind = c.Kind.Trim().ToUpperInvariant(),
                        c.Value,
                        c.Percentage,
                        CalculationType = c.CalculationType.Trim().ToUpperInvariant(),
                        BaseComponentCode = c.BaseComponentCode?.Trim().ToUpperInvariant(),
                        c.IsTaxable,
                        c.IsRecurring,
                        c.IsStatutory,
                        c.IsEmployerContribution
                    })
                    .ToArray()
            })
            .ToArray();
        var payload = JsonSerializer.SerializeToUtf8Bytes(canonicalRows);
        return Convert.ToHexString(SHA256.HashData(payload));
    }

    private static EmployeeSalaryImportPreviewRowDto Validate(EmployeeSalaryImportRowDto row, ISet<string> existing, ISet<string> seen, IReadOnlyCollection<EmployeeSalaryStructure> existingSalaries)
    {
        var errors = new List<string>();
        var employeeId = row.EmployeeId.Trim();
        if (string.IsNullOrWhiteSpace(employeeId)) errors.Add("EmployeeId is required.");
        else if (!existing.Contains(employeeId)) errors.Add("Employee does not belong to the current company.");
        else if (!seen.Add(employeeId)) errors.Add("Employee appears more than once in this import.");
        if (row.EffectiveFrom == default) errors.Add("EffectiveFrom is required.");
        if (row.EffectiveTo.HasValue && row.EffectiveTo.Value.Date < row.EffectiveFrom.Date) errors.Add("EffectiveTo cannot precede EffectiveFrom.");
        if (row.AnnualCtc.HasValue && row.AnnualCtc.Value < 0) errors.Add("AnnualCtc cannot be negative.");
        if (row.MonthlyGross.HasValue && row.MonthlyGross.Value < 0) errors.Add("MonthlyGross cannot be negative.");
        if (row.Components.Count == 0) errors.Add("At least one salary component is required.");
        var candidateEnd = row.EffectiveTo?.Date ?? DateTime.MaxValue.Date;
        if (existingSalaries.Any(x => string.Equals(x.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase) && x.EffectiveFrom.Date <= candidateEnd && (x.EffectiveTo?.Date ?? DateTime.MaxValue.Date) >= row.EffectiveFrom.Date))
            errors.Add("Salary effective dates overlap an existing approved or submitted revision.");
        foreach (var component in row.Components)
        {
            if (string.IsNullOrWhiteSpace(component.ComponentCode)) errors.Add("Every component must have a code.");
            if (component.Value < 0) errors.Add($"Component {component.ComponentCode} cannot be negative.");
        }
        return new EmployeeSalaryImportPreviewRowDto { RowNumber = row.RowNumber, EmployeeId = employeeId, IsValid = errors.Count == 0, Errors = errors };
    }
}
