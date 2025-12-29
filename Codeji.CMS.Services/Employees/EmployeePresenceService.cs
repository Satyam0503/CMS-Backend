using System.Collections.Concurrent;
using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;

namespace Codeji.CMS.Services.Employees;

public class EmployeePresenceService : IEmployeePresenceService
{
    // using concurrentDictionary for thread safety
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _onlineUsers = new();
    public void AddUserToGroup(string groupName, string connectionId, string userId)
    {
        var group = _onlineUsers.GetOrAdd(
            groupName,
            _ => new ConcurrentDictionary<string, string>()
        );

        // Track by connectionId (supports multiple devices)
        group[connectionId] = userId;
    }

    public void RemoveUserFromGroup(string groupName, string connectionId)
    {
        if (_onlineUsers.TryGetValue(groupName, out var group))
        {
            group.TryRemove(connectionId, out _);

            // Remove group if empty
            if (group.IsEmpty)
            {
                _onlineUsers.TryRemove(groupName, out _);
            }
        }
    }

    public List<string> GetOnlineUsersInGroup(string groupName, string excludeUserId = null)
    {
        if (_onlineUsers.TryGetValue(groupName, out var group))
        {
            return group.Values
                .Distinct()
                .Where(u => excludeUserId == null || u != excludeUserId)
                .ToList();
        }

        return new List<string>();
    }
}
