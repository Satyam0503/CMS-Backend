using System.Collections.Concurrent;
using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;

namespace Codeji.CMS.Services.Employees;

public class EmployeePresenceService : IEmployeePresenceService
{
    // using concurrentDictionary for thread safety
    private readonly ConcurrentDictionary<string, int> onlineUser = new();

    public EmployeePresenceService()
    {
    }

    public void UserConnected(string userId)
    {
        onlineUser.AddOrUpdate(userId, 1, (_, count) => count + 1);
    }

    public void UserDisconnected(string userId)
    {
        if (onlineUser.TryGetValue(userId, out var count))
        {
            if (count <= 1)
            {
                onlineUser.TryRemove(userId, out _);
            }
            else
            {
                onlineUser[userId] = count - 1;
            }
        }
    }
    public bool IsUserOnline(string userId)
    {
        return onlineUser.ContainsKey(userId);
    }

    public IReadOnlyList<string> GetOnlineUser(List<string> userIds)
    {
        ICollection<string> onLineUsers = onlineUser.Keys;
        return onLineUsers.Where(u => userIds.Contains(u)).ToList();
    }

}
