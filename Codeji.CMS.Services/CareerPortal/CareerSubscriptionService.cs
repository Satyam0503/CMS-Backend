using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.CareerPortal;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.CareerPortal;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.CareerPortal.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.CareerPortal;

public sealed class CareerSubscriptionService : ICareerSubscriptionService
{
    private static readonly HashSet<string> Frequencies = new(StringComparer.OrdinalIgnoreCase)
        { "Instant", "DailyDigest", "WeeklyDigest", "None" };
    private readonly IMongoCollection<CareerSubscriber> _subscribers;
    private readonly IPriorityTaskQueue _queue;
    private readonly IMiddlewareService _mail;
    private readonly IMongoCollection<CareerAnalyticsEvent> _analytics;

    public CareerSubscriptionService(
        IMongoDbRepository<CareerSubscriber> subscriberRepository,
        IMongoDbRepository<CareerAnalyticsEvent> analyticsRepository,
        IPriorityTaskQueue queue,
        IMiddlewareService mail)
    {
        _subscribers = subscriberRepository.GetCollection();
        _analytics = analyticsRepository.GetCollection();
        _queue = queue;
        _mail = mail;
    }

    public async Task<Result> Subscribe(CareerSubscribeRequest request)
    {
        var generic = Accepted();
        if (request.Email.Length > 320 || request.PreferredLanguage.Length > 10 ||
            (request.ConsentSource?.Length ?? 0) > 100 ||
            (request.PrivacyPolicyVersion?.Length ?? 0) > 50) return generic;
        var email = NormalizeEmail(request.Email);
        if (email is null || !Frequencies.Contains(request.NotificationFrequency)) return generic;

        var now = DateTime.UtcNow;
        var rawVerification = NewToken();
        var rawUnsubscribe = NewToken();
        var existing = await _subscribers.Find(s => s.EmailNormalized == email).FirstOrDefaultAsync();
        if (existing is null)
        {
            existing = new CareerSubscriber
            {
                EmailNormalized = email,
                Email = email,
                IsActive = true,
                VerificationTokenHash = Hash(rawVerification),
                VerificationExpiresAt = now.AddHours(24),
                UnsubscribeTokenHash = Hash(rawUnsubscribe),
                PreferredLanguage = CleanLanguage(request.PreferredLanguage),
                NotificationFrequency = CanonicalFrequency(request.NotificationFrequency),
                ConsentSource = request.ConsentSource,
                ConsentTimestamp = now,
                PrivacyPolicyVersion = request.PrivacyPolicyVersion,
                CreatedAt = now
            };
            await _subscribers.InsertOneAsync(existing);
        }
        else
        {
            if (existing.IsEmailVerified && existing.IsActive) return generic;
            existing.IsActive = true;
            existing.IsEmailVerified = false;
            existing.UnsubscribedAt = null;
            existing.SessionTokenHash = null;
            existing.SessionExpiresAt = null;
            existing.VerificationTokenHash = Hash(rawVerification);
            existing.VerificationExpiresAt = now.AddHours(24);
            existing.UnsubscribeTokenHash = string.IsNullOrEmpty(existing.UnsubscribeTokenHash)
                ? Hash(rawUnsubscribe) : existing.UnsubscribeTokenHash;
            existing.NotificationFrequency = CanonicalFrequency(request.NotificationFrequency);
            existing.PreferredLanguage = CleanLanguage(request.PreferredLanguage);
            existing.ConsentTimestamp = now;
            await _subscribers.ReplaceOneAsync(s => s.SubscriberId == existing.SubscriberId, existing);
        }

        QueueVerification(existing, rawVerification);
        await TryTrack("SubscriptionStarted", existing.SubscriberId);
        return generic;
    }

    public async Task<Result<CareerSessionResponse>> Verify(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 500) return TokenError<CareerSessionResponse>();
        var now = DateTime.UtcNow;
        var subscriber = await _subscribers.Find(s =>
            s.VerificationTokenHash == Hash(token) && s.VerificationExpiresAt > now).FirstOrDefaultAsync();
        if (subscriber is null) return TokenError<CareerSessionResponse>();

        var session = NewToken();
        subscriber.IsEmailVerified = true;
        subscriber.IsActive = true;
        subscriber.VerifiedAt ??= now;
        subscriber.VerificationTokenHash = null;
        subscriber.VerificationExpiresAt = null;
        subscriber.SessionTokenHash = Hash(session);
        subscriber.SessionExpiresAt = now.AddDays(30);
        await _subscribers.ReplaceOneAsync(s => s.SubscriberId == subscriber.SubscriberId, subscriber);
        await TryTrack("SubscriptionVerified", subscriber.SubscriberId);

        return new Result<CareerSessionResponse>
        {
            MethodResult = new CareerSessionResponse { SessionToken = session, ExpiresAt = subscriber.SessionExpiresAt.Value },
            TotalRecords = 1,
            Message = "Email verified."
        };
    }

    public Task<Result> Resend(string email) =>
        Subscribe(new CareerSubscribeRequest { Email = email, ConsentSource = "verification-resend" });

    public async Task<Result> Unsubscribe(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 1000)
            return new Result { Success = true, Message = "Your subscription preferences have been updated." };
        var subscriberId = CareerTokenProtector.ReadUnsubscribeToken(token);
        var subscriber = subscriberId is null
            ? await _subscribers.Find(s => s.UnsubscribeTokenHash == Hash(token)).FirstOrDefaultAsync()
            : await _subscribers.Find(s => s.SubscriberId == subscriberId).FirstOrDefaultAsync();
        if (subscriber is not null)
        {
            subscriber.IsActive = false;
            subscriber.NotificationFrequency = "None";
            subscriber.UnsubscribedAt = DateTime.UtcNow;
            subscriber.SessionTokenHash = null;
            subscriber.SessionExpiresAt = null;
            await _subscribers.ReplaceOneAsync(s => s.SubscriberId == subscriber.SubscriberId, subscriber);
            QueueConfirmation(subscriber, "Career alerts unsubscribed",
                "<p>Your career email subscription has been disabled.</p><p>You can subscribe again from the careers portal at any time.</p>");
        }
        return new Result { Success = true, Message = "Your subscription preferences have been updated." };
    }

    public async Task<Result<CareerPreferencesDto>> GetPreferences(string sessionToken)
    {
        var subscriber = await Authorize(sessionToken);
        return subscriber is null
            ? Unauthorized<CareerPreferencesDto>()
            : Preferences(subscriber);
    }

    public async Task<Result<CareerPreferencesDto>> UpdatePreferences(
        string sessionToken, UpdateCareerPreferencesRequest request)
    {
        var subscriber = await Authorize(sessionToken);
        if (subscriber is null) return Unauthorized<CareerPreferencesDto>();
        if (!Frequencies.Contains(request.NotificationFrequency))
            return new Result<CareerPreferencesDto> { Success = false, StatusCode = 400, Message = "Invalid notification frequency." };

        subscriber.NotificationFrequency = CanonicalFrequency(request.NotificationFrequency);
        subscriber.PreferredLanguage = CleanLanguage(request.PreferredLanguage);
        subscriber.IsActive = request.IsActive;
        if (!request.IsActive) subscriber.UnsubscribedAt = DateTime.UtcNow;
        await _subscribers.ReplaceOneAsync(s => s.SubscriberId == subscriber.SubscriberId, subscriber);
        QueueConfirmation(subscriber, "Career alert preferences updated",
            $"<p>Your career alert preference is now <strong>{WebUtility.HtmlEncode(subscriber.NotificationFrequency)}</strong>.</p>");
        return Preferences(subscriber);
    }

    public Task<CareerSubscriber?> Authorize(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken)) return Task.FromResult<CareerSubscriber?>(null);
        var now = DateTime.UtcNow;
        return _subscribers.Find(s =>
            s.SessionTokenHash == Hash(sessionToken) && s.SessionExpiresAt > now &&
            s.IsEmailVerified && s.IsActive).FirstOrDefaultAsync();
    }

    private void QueueVerification(CareerSubscriber subscriber, string rawToken)
    {
        var baseUrl = (ConfigManager.AppSettings.AppUrl ?? "").TrimEnd('/');
        var url = $"{baseUrl}/careers/preferences?verify={Uri.EscapeDataString(rawToken)}";
        var body = $"<p>Confirm your career email subscription.</p><p><a href=\"{WebUtility.HtmlEncode(url)}\">Verify email</a></p><p>This link expires in 24 hours.</p>";
        _queue.QueueBackgroundWorkItem(_ => _mail.EmailSendAndSave(new EmpEmailLogs
        {
            CompanyId = string.Empty,
            UserTo = subscriber.SubscriberId,
            UserFrom = "career-portal",
            Email = subscriber.Email,
            Subject = "Verify your career alerts subscription",
            Body = body,
            EmailLogType = MailType.CareerPortal
        }), 1);
    }

    private async Task TryTrack(string eventType, string subscriberId)
    {
        try
        {
            await _analytics.InsertOneAsync(new CareerAnalyticsEvent
            {
                EventType = eventType, SubscriberId = subscriberId, CreatedAt = DateTime.UtcNow
            });
        }
        catch { }
    }

    private void QueueConfirmation(CareerSubscriber subscriber, string subject, string body) =>
        _queue.QueueBackgroundWorkItem(_ => _mail.EmailSendAndSave(new EmpEmailLogs
        {
            CompanyId = string.Empty, UserTo = subscriber.SubscriberId, UserFrom = "career-portal",
            Email = subscriber.Email, Subject = subject, Body = body, EmailLogType = MailType.CareerPortal
        }), 0);

    private static Result Accepted() => new()
    {
        Success = true, StatusCode = StatusCodes.Status202Accepted,
        Message = "If the address can be subscribed, a verification email will be sent."
    };
    private static Result<CareerPreferencesDto> Preferences(CareerSubscriber s) => new()
    {
        MethodResult = new CareerPreferencesDto
        {
            NotificationFrequency = s.NotificationFrequency, PreferredLanguage = s.PreferredLanguage, IsActive = s.IsActive
        },
        TotalRecords = 1
    };
    private static Result<T> Unauthorized<T>() => new() { Success = false, StatusCode = 401, Message = "Career session is missing or expired." };
    private static Result<T> TokenError<T>() => new() { Success = false, StatusCode = 400, Message = "Verification token is invalid or expired." };
    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? "")));
    private static string CanonicalFrequency(string value) => Frequencies.First(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
    private static string CleanLanguage(string value) => string.IsNullOrWhiteSpace(value) ? "en" : value.Trim().ToLowerInvariant()[..Math.Min(10, value.Trim().Length)];
    private static string? NormalizeEmail(string value)
    {
        try { return new MailAddress(value.Trim()).Address.ToLowerInvariant(); }
        catch { return null; }
    }
}
