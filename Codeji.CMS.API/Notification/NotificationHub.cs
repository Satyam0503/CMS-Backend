using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Notification;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(IHttpContextAccessor httpContextAccessor, ILogger<NotificationHub> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdentifier = Context.UserIdentifier;
        if (userIdentifier is not null)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning(
                    "Notification hub connection rejected for User {UserId}: CompanyId is missing.",
                    userIdentifier);
                Context.Abort();
                await base.OnConnectedAsync();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdentifier = Context.UserIdentifier;
        if (userIdentifier is not null)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            if (!string.IsNullOrEmpty(companyId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company_{companyId}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
