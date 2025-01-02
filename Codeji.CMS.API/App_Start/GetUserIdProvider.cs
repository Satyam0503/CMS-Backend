using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.App_Start
{
    public class GetUserIdProvider : IUserIdProvider
    {
        public virtual string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst("user_id")?.Value!;
        }
    }
}
