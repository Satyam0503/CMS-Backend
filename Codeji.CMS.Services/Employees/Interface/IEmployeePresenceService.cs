namespace Codeji.CMS.Services.Employees.Interface;

public interface IEmployeePresenceService
{
    void AddUserToGroup(string groupName, string connectionId, string userId);
    void RemoveUserFromGroup(string groupName, string connectionId);
    List<string> GetOnlineUsersInGroup(string groupName, string excludeUserId = null);
}
