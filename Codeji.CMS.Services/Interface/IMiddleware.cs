using System;
using Codeji.CMS.DTO;
using Codeji.CMS.Repository.Entities;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Interface
{
    public interface IMiddlewareService
    {
        UserModel GetUserById(string usierId);
        void EmailSendAndSave(EmpEmailLogs emailLog);
        void EmailSendAndSave(EmpEmailLogs emailLog, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null);

        Dictionary<NotificationPreferenceType, bool> GetDefaultNotificationPreferences();
        bool IsUserNotificationPreferenceEnabled(string userId, NotificationPreferenceType preferenceType);
    }
}

