using System;
using Codeji.CMS.DTO;
using Codeji.CMS.Repository.Entities;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Interface
{
    public interface IMiddlewareService
    {
        Task<UserModel?> GetUserById(string userId);
        Task EmailSendAndSave(EmpEmailLogs emailLog);
        Task EmailSendAndSave(EmpEmailLogs emailLog, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null);
        Task<(bool IsSent, string Error)> EmailSendAndSaveWithResult(EmpEmailLogs emailLog);

        Dictionary<NotificationPreferenceType, bool> GetDefaultNotificationPreferences();
        Task<bool> IsUserNotificationPreferenceEnabled(string userId, NotificationPreferenceType preferenceType);
        string GetCompanyLogoAsDataUrl(string companyLogoPath);
    }
}

