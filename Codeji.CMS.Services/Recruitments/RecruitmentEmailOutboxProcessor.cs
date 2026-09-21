using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentEmailOutboxProcessor(IMongoDbRepository<RecruitmentEmailOutbox> repository, IMiddlewareService mail) : IRecruitmentEmailOutboxProcessor
{
    private const int MaxAttempts = 5;
    public async Task ProcessNext(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claimable = Builders<RecruitmentEmailOutbox>.Filter.And(
            Builders<RecruitmentEmailOutbox>.Filter.Lte(x => x.AvailableAtUtc, now),
            Builders<RecruitmentEmailOutbox>.Filter.Or(
                Builders<RecruitmentEmailOutbox>.Filter.In(x => x.Status, new[] { "Pending", "Failed" }),
                Builders<RecruitmentEmailOutbox>.Filter.And(Builders<RecruitmentEmailOutbox>.Filter.Eq(x => x.Status, "Processing"), Builders<RecruitmentEmailOutbox>.Filter.Lt(x => x.ProcessingStartedAtUtc, now.AddMinutes(-10)))));
        var item = await repository.GetCollection().FindOneAndUpdateAsync(claimable, Builders<RecruitmentEmailOutbox>.Update.Set(x => x.Status, "Processing").Set(x => x.ProcessingStartedAtUtc, now).Inc(x => x.AttemptCount, 1), new FindOneAndUpdateOptions<RecruitmentEmailOutbox> { Sort = Builders<RecruitmentEmailOutbox>.Sort.Ascending(x => x.AvailableAtUtc), ReturnDocument = ReturnDocument.After }, cancellationToken);
        if (item is null) return;
        try
        {
            var sent = await mail.EmailSendAndSaveWithResult(new EmpEmailLogs { CompanyId = item.CompanyId, UserTo = item.RecipientUserId ?? "recruitment-external", UserFrom = "recruitment", Email = item.RecipientEmail, Subject = item.Subject, Body = item.HtmlBody, EmailLogType = MailType.CareerPortal });
            if (!sent.IsSent) throw new InvalidOperationException(sent.Error);
            await repository.GetCollection().UpdateOneAsync(x => x.OutboxId == item.OutboxId && x.Status == "Processing", Builders<RecruitmentEmailOutbox>.Update.Set(x => x.Status, "Sent").Set(x => x.SentAtUtc, DateTime.UtcNow).Set(x => x.LastError, null).Set(x => x.UpdatedDate, DateTime.UtcNow), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            var dead = item.AttemptCount >= MaxAttempts;
            var delay = TimeSpan.FromMinutes(Math.Pow(2, Math.Min(item.AttemptCount, 6)));
            await repository.GetCollection().UpdateOneAsync(x => x.OutboxId == item.OutboxId && x.Status == "Processing", Builders<RecruitmentEmailOutbox>.Update.Set(x => x.Status, dead ? "DeadLetter" : "Failed").Set(x => x.AvailableAtUtc, now.Add(delay)).Set(x => x.LastError, ex.Message[..Math.Min(1000, ex.Message.Length)]).Set(x => x.UpdatedDate, DateTime.UtcNow), cancellationToken: cancellationToken);
        }
    }
}
