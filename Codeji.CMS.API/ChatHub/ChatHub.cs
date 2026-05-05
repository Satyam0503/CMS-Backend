using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.ChatHub
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmployeePresenceService _onlineUserService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IHttpContextAccessor httpContextAccessor,
            IEmployeePresenceService onlineUserService,
            ILogger<ChatHub> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _onlineUserService = onlineUserService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            // current connection connectionId and userIdentifier
            var connectionId = Context.ConnectionId;
            var userIdentifier = Context.UserIdentifier;
            if (userIdentifier != null)
            {
                // Get employee's companyId and add to company group
                string companyId = CurrentContext.CompanyId(_httpContextAccessor);
                if (string.IsNullOrEmpty(companyId))
                {
                    _logger.LogWarning("Connection rejected for User {UserId}: CompanyId is missing.", userIdentifier);
                    Context.Abort();
                    await base.OnConnectedAsync();
                    return;
                }
                var groupName = $"company_{companyId}";
                // Add user to the company group using their connectionId
                await Groups.AddToGroupAsync(connectionId, groupName);

                // Track this user as online
                _onlineUserService.AddUserToGroup(groupName, connectionId, userIdentifier);

                // Send newly connected user the list of already online users
                var onlineUsers = _onlineUserService.GetOnlineUsersInGroup(groupName);

                // notify company members that this user is online
                await Clients.Group(groupName).SendAsync("employeeOnline", new { userIds = onlineUsers });
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdentifier = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;
            if (userIdentifier != null)
            {
                // Get employee's company and remove from company group
                string companyId = CurrentContext.CompanyId(_httpContextAccessor);
                if (string.IsNullOrEmpty(companyId))
                {
                    _logger.LogWarning("Disconnection processing skipped for User {UserId}: CompanyId is missing.", userIdentifier);
                    await base.OnDisconnectedAsync(exception);
                    return;
                }
                var groupName = $"company_{companyId}";
                await Groups.RemoveFromGroupAsync(connectionId, groupName);

                // Remove user from online tracking
                _onlineUserService.RemoveUserFromGroup(groupName, connectionId);

                // Notify remaining company members that this user went offline
                await Clients.Group(groupName).SendAsync("employeeOffline", new { userId = userIdentifier });
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}