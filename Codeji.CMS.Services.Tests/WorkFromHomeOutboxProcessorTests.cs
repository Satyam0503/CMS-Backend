using System.Linq.Expressions;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class WorkFromHomeOutboxProcessorTests
{
    [Fact]
    public async Task ProcessNext_AuditEvent_PersistsTenantScopedRequestLogAndMarksOutboxSent()
    {
        var evt = new WorkFromHomeOutboxEvent
        {
            EventId = "event-1", CompanyId = "company-a", RequestId = "request-a", DedupeKey = "audit:request-a:event-1",
            EventType = "AUDIT", ActorUserId = "hr-a", PreviousStatus = "Pending", NewStatus = "Approved",
            Action = "Approved", RequestVersion = 2, CreatedBy = "hr-a", CreatedDate = DateTime.UtcNow
        };
        var outboxRepo = new Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>>();
        var outboxCollection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        outboxRepo.Setup(x => x.GetCollection()).Returns(outboxCollection.Object);
        outboxCollection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<FindOneAndUpdateOptions<WorkFromHomeOutboxEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        outboxCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var logs = new Mock<IMongoDbRepository<WorkFromHomeRequestLog>>();
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequestLog, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((WorkFromHomeRequestLog?)null);
        WorkFromHomeRequestLog? savedLog = null;
        logs.Setup(x => x.AddOne(It.IsAny<WorkFromHomeRequestLog>()))
            .Callback<WorkFromHomeRequestLog>(item => savedLog = item)
            .ReturnsAsync(new Result { Success = true });

        var processor = new WorkFromHomeOutboxProcessor(
            outboxRepo.Object, logs.Object,
            Mock.Of<IMongoDbRepository<Notifications>>(), Mock.Of<IMongoDbRepository<UserNotifications>>(),
            Mock.Of<INotificationService>(), Mock.Of<IMiddlewareService>(), Mock.Of<ILogger<WorkFromHomeOutboxProcessor>>());

        Assert.True(await processor.ProcessNextAsync());
        Assert.NotNull(savedLog);
        Assert.Equal("company-a", savedLog.CompanyId);
        Assert.Equal("request-a", savedLog.RequestId);
        Assert.Equal("Approved", savedLog.NewStatus);
        Assert.Equal(evt.EventId, savedLog.LogId);
        outboxCollection.Verify(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
            It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessNext_NotificationEvent_PersistsCompanyAndRecipientBeforeLivePush()
    {
        var evt = new WorkFromHomeOutboxEvent
        {
            EventId = "event-2", CompanyId = "company-b", RequestId = "request-b", DedupeKey = "notify:request-b:1:Approved:user-b",
            EventType = "NOTIFICATION", ActorUserId = "hr-b", RecipientUserId = "user-b", Title = "Approved",
            Body = "WFH dates", NotificationType = (int)EnumsHelper.NotificationTypes.WorkFromHomeApproved,
            CreatedBy = "hr-b", CreatedDate = DateTime.UtcNow
        };
        var collection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        var outbox = new Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>>();
        outbox.Setup(x => x.GetCollection()).Returns(collection.Object);
        collection.Setup(x => x.FindOneAndUpdateAsync(It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(), It.IsAny<FindOneAndUpdateOptions<WorkFromHomeOutboxEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        collection.Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        Notifications? notification = null;
        UserNotifications? recipient = null;
        var notificationRepo = new Mock<IMongoDbRepository<Notifications>>();
        notificationRepo.Setup(x => x.AddOne(It.IsAny<Notifications>())).Callback<Notifications>(x => notification = x).ReturnsAsync(new Result { Success = true });
        var recipientRepo = new Mock<IMongoDbRepository<UserNotifications>>();
        recipientRepo.Setup(x => x.AddOne(It.IsAny<UserNotifications>())).Callback<UserNotifications>(x => recipient = x).ReturnsAsync(new Result { Success = true });
        var push = new Mock<INotificationService>();
        push.Setup(x => x.SendNotificationToUser(It.IsAny<string>(), It.IsAny<NotificationViewModel>())).Returns(Task.CompletedTask);
        var processor = new WorkFromHomeOutboxProcessor(outbox.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeRequestLog>>(), notificationRepo.Object, recipientRepo.Object,
            push.Object, Mock.Of<IMiddlewareService>(), Mock.Of<ILogger<WorkFromHomeOutboxProcessor>>());

        Assert.True(await processor.ProcessNextAsync());
        Assert.NotNull(notification);
        Assert.Equal("company-b", notification.CompanyId);
        Assert.Equal(evt.RequestId, notification.TargetId);
        Assert.NotNull(recipient);
        Assert.Equal("company-b", recipient.CompanyId);
        Assert.Equal("user-b", recipient.UserId);
        Assert.Equal(notification.NotificationId, recipient.NotificationId);
        push.Verify(x => x.SendNotificationToUser("user-b", It.Is<NotificationViewModel>(message => message.UserNotificationId == recipient.UserNotificationId)), Times.Once);
    }

    [Fact]
    public async Task ProcessNext_MissingRequestId_RecoversFromDedupeKey()
    {
        var evt = new WorkFromHomeOutboxEvent
        {
            EventId = "event-recover-1", CompanyId = "company-c", RequestId = "", DedupeKey = "audit:recovered-req-1:event-recover-1",
            EventType = "AUDIT", ActorUserId = "hr-c", PreviousStatus = "Pending", NewStatus = "Approved",
            Action = "Approved", RequestVersion = 1, CreatedBy = "hr-c", CreatedDate = DateTime.UtcNow
        };
        var outboxRepo = new Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>>();
        var outboxCollection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        outboxRepo.Setup(x => x.GetCollection()).Returns(outboxCollection.Object);
        outboxCollection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<FindOneAndUpdateOptions<WorkFromHomeOutboxEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        outboxCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var logs = new Mock<IMongoDbRepository<WorkFromHomeRequestLog>>();
        WorkFromHomeRequestLog? savedLog = null;
        logs.Setup(x => x.AddOne(It.IsAny<WorkFromHomeRequestLog>()))
            .Callback<WorkFromHomeRequestLog>(item => savedLog = item)
            .ReturnsAsync(new Result { Success = true });

        var processor = new WorkFromHomeOutboxProcessor(
            outboxRepo.Object, logs.Object,
            Mock.Of<IMongoDbRepository<Notifications>>(), Mock.Of<IMongoDbRepository<UserNotifications>>(),
            Mock.Of<INotificationService>(), Mock.Of<IMiddlewareService>(), Mock.Of<ILogger<WorkFromHomeOutboxProcessor>>());

        Assert.True(await processor.ProcessNextAsync());
        Assert.NotNull(savedLog);
        Assert.Equal("recovered-req-1", savedLog.RequestId);
        Assert.Equal("recovered-req-1", evt.RequestId);
    }

    [Fact]
    public async Task ProcessNext_MissingRequestId_RecoversFromRequestRepository()
    {
        var now = DateTime.UtcNow;
        var evt = new WorkFromHomeOutboxEvent
        {
            EventId = "event-recover-2", CompanyId = "company-d", RequestId = "", DedupeKey = "audit::event-recover-2",
            EventType = "AUDIT", ActorUserId = "user-d", PreviousStatus = "Pending", NewStatus = "Approved",
            Action = "Approved", RequestVersion = 1, CreatedBy = "user-d", CreatedDate = now
        };
        var outboxRepo = new Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>>();
        var outboxCollection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        outboxRepo.Setup(x => x.GetCollection()).Returns(outboxCollection.Object);
        outboxCollection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<FindOneAndUpdateOptions<WorkFromHomeOutboxEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        outboxCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var reqRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        reqRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new WorkFromHomeRequest { CompanyId = "company-d", RequestId = "req-from-repo", UserId = "user-d", CreatedDate = now });

        var logs = new Mock<IMongoDbRepository<WorkFromHomeRequestLog>>();
        WorkFromHomeRequestLog? savedLog = null;
        logs.Setup(x => x.AddOne(It.IsAny<WorkFromHomeRequestLog>()))
            .Callback<WorkFromHomeRequestLog>(item => savedLog = item)
            .ReturnsAsync(new Result { Success = true });

        var processor = new WorkFromHomeOutboxProcessor(
            outboxRepo.Object, logs.Object,
            Mock.Of<IMongoDbRepository<Notifications>>(), Mock.Of<IMongoDbRepository<UserNotifications>>(),
            Mock.Of<INotificationService>(), Mock.Of<IMiddlewareService>(), Mock.Of<ILogger<WorkFromHomeOutboxProcessor>>(),
            reqRepo.Object);

        Assert.True(await processor.ProcessNextAsync());
        Assert.NotNull(savedLog);
        Assert.Equal("req-from-repo", savedLog.RequestId);
        Assert.Equal("req-from-repo", evt.RequestId);
    }

    [Fact]
    public async Task ProcessNext_UnrecoverableMissingIdentity_MarksDeadLetterWithoutThrowing()
    {
        var evt = new WorkFromHomeOutboxEvent
        {
            EventId = "event-deadletter", CompanyId = "company-e", RequestId = "", DedupeKey = "corrupt",
            EventType = "AUDIT", ActorUserId = "user-e", CreatedBy = "user-e", CreatedDate = null
        };
        var outboxRepo = new Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>>();
        var outboxCollection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        outboxRepo.Setup(x => x.GetCollection()).Returns(outboxCollection.Object);
        outboxCollection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<FindOneAndUpdateOptions<WorkFromHomeOutboxEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        outboxCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var processor = new WorkFromHomeOutboxProcessor(
            outboxRepo.Object, Mock.Of<IMongoDbRepository<WorkFromHomeRequestLog>>(),
            Mock.Of<IMongoDbRepository<Notifications>>(), Mock.Of<IMongoDbRepository<UserNotifications>>(),
            Mock.Of<INotificationService>(), Mock.Of<IMiddlewareService>(), Mock.Of<ILogger<WorkFromHomeOutboxProcessor>>());

        var result = await processor.ProcessNextAsync();
        Assert.True(result);
        outboxCollection.Verify(x => x.UpdateOneAsync(
            It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
            It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
