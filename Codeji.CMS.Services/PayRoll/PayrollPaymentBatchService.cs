using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public sealed class PaymentBatchRequestDto
{
    public string? ProviderReference { get; set; }
    public string? Note { get; set; }
    public bool PaymentConfirmed { get; set; }
}

public interface IPayrollPaymentBatchService
{
    Task<Result<PayrollPaymentBatch>> GetByRun(string companyId, string runId);
    Task<Result<PayrollPaymentBatch>> Create(string companyId, string runId, string actor);
    Task<Result> Submit(string companyId, string batchId, string actor, PaymentBatchRequestDto request);
    Task<Result> Reconcile(string companyId, string batchId, string actor, PaymentBatchRequestDto request);
    Task<byte[]?> ExportCsv(string companyId, string batchId);
}

public sealed class PayrollPaymentBatchService(
    IMongoDbRepository<PayrollPaymentBatch> batches,
    IMongoDbRepository<PayrollRun> runs,
    IMongoDbRepository<PayrollRunEmployee> employees) : IPayrollPaymentBatchService
{
    public async Task<Result<PayrollPaymentBatch>> GetByRun(string companyId, string runId)
    {
        var batch = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.PayrollRunId == runId);
        return batch is null
            ? Fail<PayrollPaymentBatch>(404, "Payment batch was not found.")
            : new Result<PayrollPaymentBatch> { Success = true, MethodResult = batch, ResourceId = batch.Id };
    }

    public async Task<Result<PayrollPaymentBatch>> Create(string companyId, string runId, string actor)
    {
        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == runId);
        if (run is null) return Fail<PayrollPaymentBatch>(404, "Payroll run was not found.");
        if (run.Status != "PROCESSED")
            return Fail<PayrollPaymentBatch>(409, "Only a processed payroll run can create a payment batch.");
        if (await batches.Exist(x => x.CompanyId == companyId && x.PayrollRunId == runId))
            return Fail<PayrollPaymentBatch>(409, "A payment batch already exists for this run.");

        var rows = (await employees.GetAll(x =>
            x.CompanyId == companyId && x.PayrollRunId == runId && x.Status == "CALCULATED")).ToList();
        if (run.BlockedCount != 0 || run.ReadyCount + run.SkippedCount != run.EmployeeCount)
            return Fail<PayrollPaymentBatch>(409, "The payroll run contains unresolved employees and cannot be paid.");
        if (rows.Count != run.ReadyCount)
            return Fail<PayrollPaymentBatch>(409, "Calculated payroll rows do not match the run's ready employee count.");
        if (rows.Any(x => string.IsNullOrWhiteSpace(x.EmployeeId) || x.GrossAmount < 0m || x.DeductionAmount < 0m || x.NetAmount < 0m ||
                PayrollCalculationRules.RoundMoney(x.GrossAmount - x.DeductionAmount) != PayrollCalculationRules.RoundMoney(x.NetAmount)) ||
            rows.Select(x => x.EmployeeId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != rows.Count)
            return Fail<PayrollPaymentBatch>(409, "Payroll employee payment rows contain invalid amounts or duplicate employee identities.");

        var grossTotal = PayrollCalculationRules.RoundMoney(rows.Sum(x => x.GrossAmount));
        var deductionTotal = PayrollCalculationRules.RoundMoney(rows.Sum(x => x.DeductionAmount));
        var netTotal = PayrollCalculationRules.RoundMoney(rows.Sum(x => x.NetAmount));
        if (grossTotal != run.GrossTotal || deductionTotal != run.DeductionTotal || netTotal != run.NetTotal)
            return Fail<PayrollPaymentBatch>(409, "Calculated employee payment rows no longer reconcile with the approved payroll run totals.");

        var batch = new PayrollPaymentBatch
        {
            CompanyId = companyId,
            PayrollRunId = runId,
            CreatedBy = actor,
            CreatedAtUtc = DateTime.UtcNow,
            EmployeeCount = rows.Count,
            TotalNetAmount = PayrollCalculationRules.RoundMoney(rows.Sum(x => x.NetAmount)),
            Items = rows.Select(x => new PayrollPaymentBatchItem
            {
                EmployeeId = x.EmployeeId,
                NetAmount = PayrollCalculationRules.RoundMoney(x.NetAmount)
            }).ToList()
        };
        var saved = await batches.AddOne(batch);
        if (!saved.Success && IsPaymentBatchUniqueConflict(saved.Message))
        {
            var winningBatch = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.PayrollRunId == runId);
            return winningBatch is not null
                ? new Result<PayrollPaymentBatch> { Success = true, MethodResult = winningBatch, ResourceId = winningBatch.Id, Message = "Existing payment batch returned." }
                : Fail<PayrollPaymentBatch>(409, "A payment batch was created concurrently for this payroll run. Refresh before trying again.");
        }
        return saved.Success
            ? new Result<PayrollPaymentBatch> { Success = true, MethodResult = batch, ResourceId = batch.Id }
            : Fail<PayrollPaymentBatch>(500, "Payment batch could not be created.");
    }

    public async Task<Result> Submit(string companyId, string batchId, string actor, PaymentBatchRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ProviderReference))
            return Fail(400, "A provider reference is required before payment submission.");

        var batch = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.Id == batchId);
        if (batch is null) return Fail(404, "Payment batch was not found.");
        if (batch.Status != "DRAFT")
            return Fail(409, "Only a draft payment batch can be submitted.");

        batch.ProviderReference = request.ProviderReference.Trim();
        batch.Status = "SUBMITTED";
        batch.SubmittedBy = actor;
        batch.SubmittedAtUtc = DateTime.UtcNow;
        return await batches.Update(
            BatchFilter(companyId, batchId) & Builders<PayrollPaymentBatch>.Filter.Eq(x => x.Status, "DRAFT"),
            batch);
    }

    public async Task<Result> Reconcile(string companyId, string batchId, string actor, PaymentBatchRequestDto request)
    {
        if (request is null || !request.PaymentConfirmed || string.IsNullOrWhiteSpace(request.Note))
            return Fail(400, "Confirm that the provider payment was verified and include a reconciliation note.");

        var batch = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.Id == batchId);
        if (batch is null) return Fail(404, "Payment batch was not found.");
        if (batch.Status is not ("SUBMITTED" or "RECONCILING" or "RECONCILED"))
            return Fail(409, "Only a submitted or interrupted payment reconciliation can be completed.");

        if (batch.Status == "SUBMITTED")
        {
            var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == batch.PayrollRunId);
            if (run is null) return Fail(409, "Payroll run was removed before reconciliation.");
            if (run.Status != "PROCESSED") return Fail(409, "The payroll run is no longer awaiting payment reconciliation.");

            batch.Status = "RECONCILING";
            batch.ReconciliationStartedBy = actor;
            batch.ReconciliationStartedAtUtc = DateTime.UtcNow;
            batch.ReconciliationNote = request.Note.Trim();
            var claimed = await batches.Update(
                BatchFilter(companyId, batchId) & Builders<PayrollPaymentBatch>.Filter.Eq(x => x.Status, "SUBMITTED"), batch);
            if (!claimed.Success)
            {
                batch = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.Id == batchId);
                if (batch is null || batch.Status is not ("RECONCILING" or "RECONCILED"))
                    return Fail(409, "Another reconciliation update is in progress. Refresh and retry to resume it.");
            }
        }

        return await CompleteReconciliation(companyId, batch, actor);
    }

    private async Task<Result> CompleteReconciliation(string companyId, PayrollPaymentBatch batch, string retryActor)
    {
        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == batch.PayrollRunId);
        if (run is null) return Fail(409, "Payroll run was removed before reconciliation could finish.");

        var paidEvent = run.AuditHistory.FirstOrDefault(x => x.Action == "RunPaid" && x.Notes == batch.Id);
        if (run.Status == "PROCESSED")
        {
            var previousVersion = run.Version;
            run.Status = "PAID";
            run.Version++;
            run.AuditHistory.Add(new PayrollRunAuditEvent
            {
                Action = "RunPaid",
                ActorId = batch.ReconciliationStartedBy ?? batch.ReconciledBy ?? retryActor,
                OccurredAtUtc = DateTime.UtcNow,
                Notes = batch.Id
            });
            var savedRun = await runs.Update(
                Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<PayrollRun>.Filter.Eq(x => x.Id, batch.PayrollRunId) &
                Builders<PayrollRun>.Filter.Eq(x => x.Status, "PROCESSED") &
                Builders<PayrollRun>.Filter.Eq(x => x.Version, previousVersion), run);
            if (!savedRun.Success) return Fail(409, "Payment confirmation is recorded, but the payroll run update did not finish. Retry reconciliation to resume safely.");
        }
        else if (run.Status is not ("PAID" or "CLOSED") || paidEvent is null)
        {
            return Fail(409, "The payroll run state does not match this payment confirmation. Keep the reconciliation pending and request payroll review.");
        }

        if (batch.Status == "RECONCILED") return new Result { Success = true, StatusCode = 200, Message = "Payment reconciliation is complete." };

        batch.Status = "RECONCILED";
        batch.ReconciledBy = batch.ReconciliationStartedBy ?? retryActor;
        batch.ReconciledAtUtc = DateTime.UtcNow;
        foreach (var item in batch.Items) item.Status = "RECONCILED";
        var savedBatch = await batches.Update(
            BatchFilter(companyId, batch.Id) & Builders<PayrollPaymentBatch>.Filter.Eq(x => x.Status, "RECONCILING"), batch);
        if (!savedBatch.Success) return Fail(409, "The payroll run is paid, but payment-batch finalization did not finish. Retry reconciliation to resume safely.");
        return new Result { Success = true, StatusCode = 200, Message = "Payment reconciliation is complete." };
    }

    public async Task<byte[]?> ExportCsv(string companyId, string batchId)
    {
        var batch = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.Id == batchId);
        if (batch is null) return null;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("EmployeeId,NetAmount,Status,BatchId,ProviderReference");
        foreach (var item in batch.Items)
        {
            sb.AppendLine($"{item.EmployeeId},{item.NetAmount:F2},{item.Status},{batch.Id},{item.ProviderReference ?? batch.ProviderReference ?? string.Empty}");
        }
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static FilterDefinition<PayrollPaymentBatch> BatchFilter(string companyId, string batchId) =>
        Builders<PayrollPaymentBatch>.Filter.Eq(x => x.CompanyId, companyId) &
        Builders<PayrollPaymentBatch>.Filter.Eq(x => x.Id, batchId);

    private static Result Fail(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };

    private static Result<T> Fail<T>(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };

    private static bool IsPaymentBatchUniqueConflict(string? message) =>
        !string.IsNullOrEmpty(message) && message.Contains("E11000", StringComparison.Ordinal) &&
        message.Contains("ux_payrollpaymentbatch_company_run", StringComparison.Ordinal);
}
