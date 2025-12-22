using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.ChatHub
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmployeePresenceService _employeePresenceService;
        private readonly IMongoDbRepository<EmpUser> _employeeRepository;

        public ChatHub(IHttpContextAccessor httpContextAccessor, IEmployeePresenceService employeePresenceService, IMongoDbRepository<EmpUser> employeeRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _employeePresenceService = employeePresenceService;
            _employeeRepository = employeeRepository;
        }

        public async Task NotifyOnlineUserPresence()
        {
            var userId = Context.UserIdentifier;
            Console.WriteLine($"User Connected: {userId}");
            // get Connected user company's employee
            List<string> allUser = [];
            var employee = await _employeeRepository.FirstOrDefault(e => e.UserId == userId && e.Status && e.IsEmailVerified);
            if (employee != null)
            {
                allUser = (await _employeeRepository.GetAll(e => e.CompanyId == employee.CompanyId && e.Status && e.IsEmailVerified)).Select(e => e.UserId).ToList();
            }
            var onlineUser = _employeePresenceService.GetOnlineUser(allUser);
            foreach (var item in onlineUser)
            {
                Console.WriteLine($"{item} ,");
            }

            await Clients.Users(onlineUser).SendAsync("employeeOnlineStatus", onlineUser);
        }

        public async override Task<Task> OnConnectedAsync()
        {
            // current connection connectionId and userIdentifier
            var connectionId = Context.ConnectionId;
            var userIdentifier = Context.UserIdentifier;
            if (userIdentifier != null)
            {
                _employeePresenceService.UserConnected(userIdentifier);
                await NotifyOnlineUserPresence();
            }
            return base.OnConnectedAsync();
        }
        public override async Task<Task> OnDisconnectedAsync(Exception? exception)
        {
            var userIdentifier = Context.UserIdentifier;
            if (userIdentifier != null)
            {
                _employeePresenceService.UserDisconnected(userIdentifier);
                await NotifyOnlineUserPresence();
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}