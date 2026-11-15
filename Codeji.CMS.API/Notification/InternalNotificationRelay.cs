using System.Security.Cryptography;
using System.Text;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;

namespace Codeji.CMS.API.Notification;

public sealed record InternalNotificationRequest(string UserId, NotificationViewModel Notification);

/// <summary>
/// Lets the out-of-process Scheduler push real-time notifications through this host's SignalR hub.
/// Branched off before the browser pipeline (antiforgery, app version, tenant checks); guarded by InternalApi:Key.
/// </summary>
public static class InternalNotificationRelay
{
    public const string Path = "/internal/notifications";
    public const string KeyHeader = "X-Internal-Key";

    public static void MapInternalNotificationRelay(this WebApplication app)
    {
        var configuredKey = app.Configuration["InternalApi:Key"];
        if (string.IsNullOrWhiteSpace(configuredKey))
            return;
        if (configuredKey.Length < 32)
            throw new InvalidOperationException("InternalApi:Key must be at least 32 characters.");

        var expected = Encoding.UTF8.GetBytes(configuredKey);
        app.Map(Path, branch => branch.Run(async context =>
        {
            var supplied = Encoding.UTF8.GetBytes(context.Request.Headers[KeyHeader].ToString());
            if (!HttpMethods.IsPost(context.Request.Method) || !CryptographicOperations.FixedTimeEquals(supplied, expected))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var request = await context.Request.ReadFromJsonAsync<InternalNotificationRequest>(context.RequestAborted);
            if (request is null || string.IsNullOrWhiteSpace(request.UserId) || request.Notification is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var notifications = context.RequestServices.GetRequiredService<INotificationService>();
            await notifications.SendNotificationToUser(request.UserId, request.Notification);
            context.Response.StatusCode = StatusCodes.Status204NoContent;
        }));
    }
}
