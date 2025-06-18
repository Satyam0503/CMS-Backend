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
    }
}