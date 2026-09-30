using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public sealed class PaymentBatchRequestDto
{
    public string? ProviderReference { get; set; }
    public string? Note { get; set; }
}

public interface IPayrollPaymentBatchService
{
    Task<Result<PayrollPaymentBatch>> GetByRun(string companyId, string runId);
    Task<Result<PayrollPaymentBatch>> Create(string companyId, string runId, string actor);
    Task<Result> Submit(string companyId, string batchId, string actor, PaymentBatchRequestDto request);
    Task<Result> Reconcile(string companyId, string batchId, string actor, PaymentBatchRequestDto request);
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
        if (rows.Count != run.EmployeeCount)
            return Fail<PayrollPaymentBatch>(409, "All payroll employees must be calculated before payment.");

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
        var batch = await batches.FirstOrDefault(x => x.CompanyId == companyId && x.Id == batchId);
        if (batch is null) return Fail(404, "Payment batch was not found.");
        if (batch.Status != "SUBMITTED")
            return Fail(409, "Only a submitted payment batch can be reconciled.");

        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == batch.PayrollRunId);
        if (run is null) return Fail(409, "Payroll run was removed before reconciliation.");
        if (run.Status != "PROCESSED")
            return Fail(409, "The payroll run is no longer awaiting payment reconciliation.");

        batch.Status = "RECONCILED";
        batch.ReconciledBy = actor;
        batch.ReconciledAtUtc = DateTime.UtcNow;
        batch.ReconciliationNote = request?.Note?.Trim();
        foreach (var item in batch.Items) item.Status = "RECONCILED";

        var saved = await batches.Update(
            BatchFilter(companyId, batchId) & Builders<PayrollPaymentBatch>.Filter.Eq(x => x.Status, "SUBMITTED"),
            batch);
        if (!saved.Success) return saved;

        run.Status = "PAID";
        run.Version++;
        run.AuditHistory.Add(new PayrollRunAuditEvent
        {
            Action = "RunPaid",
            ActorId = actor,
            OccurredAtUtc = DateTime.UtcNow,
            Notes = batchId
        });
        return await runs.Update(
            Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollRun>.Filter.Eq(x => x.Id, batch.PayrollRunId) &
            Builders<PayrollRun>.Filter.Eq(x => x.Status, "PROCESSED"),
            run);
    }

    private static FilterDefinition<PayrollPaymentBatch> BatchFilter(string companyId, string batchId) =>
        Builders<PayrollPaymentBatch>.Filter.Eq(x => x.CompanyId, companyId) &
        Builders<PayrollPaymentBatch>.Filter.Eq(x => x.Id, batchId);

    private static Result Fail(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };

    private static Result<T> Fail<T>(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };
}
