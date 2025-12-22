namespace Codeji.CMS.Services.Employees.Interface;

public interface IEmployeePresenceService
{
    void UserConnected(string userId);
    void UserDisconnected(string userId);

    bool IsUserOnline(string userId);
    IReadOnlyList<string> GetOnlineUser(List<string> userId);
}
