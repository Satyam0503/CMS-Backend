using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Notification
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public NotificationHub(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public override async Task OnConnectedAsync()
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            await Groups.AddToGroupAsync(Context.ConnectionId, companyId);
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, companyId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}