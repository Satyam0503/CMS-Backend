using System.Threading.Tasks;
using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services
{
    public class MiddlewareService : IMiddlewareService
    {
        private readonly IMongoDbRepository<EmpUser> _userRepository;
        private readonly IMongoDbRepository<EmpEmailLogs> _emailLogRepository;
        private readonly IMapper _mapper;
        readonly IMongoDbRepository<NotificationPreference> _notificationPreferenceRepository;
        readonly IEmployeeService _employeeService;
        public MiddlewareService(IMongoDbRepository<EmpUser> userRepository,
            IMongoDbRepository<EmpEmailLogs> emailLogRepository,
            IMapper mapper,
            IMongoDbRepository<NotificationPreference> notificationPreferenceRepository
        )
        {
            _userRepository = userRepository;
            _emailLogRepository = emailLogRepository;
            _mapper = mapper;
            _notificationPreferenceRepository = notificationPreferenceRepository;
        }



        public UserModel GetUserById(string id)
        {
            UserModel userModel = new();
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            EmpUser user = _userRepository.FirstOrDefault(x => x.UserId == id).Result;
            return _mapper.Map(user, userModel);
        }
        public void EmailSendAndSave(EmpEmailLogs emailLog)
        {
            Task.Run(() => Common(emailLog));
        }
        public void EmailSendAndSave(EmpEmailLogs emailLog, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
        {
            Task.Run(() => Common(emailLog, attachments));
        }
        private async Task Common(EmpEmailLogs emailLog, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
        {
            try
            {
                if (emailLog != null)
                {
                    string[] ccEmails = null;
                    string[] bccEmails = null;
                    if (emailLog.EmailLogType == Utility.Enums.EnumsHelper.MailType.ApplyNowMailToHR)
                    {
                        //Add email to cc from config file
                        ccEmails = [ConfigManager.EmailSettings.SupportEmail];
                        bccEmails = [ConfigManager.EmailSettings.BccEmail];
                    }
                    var res = await Emailer.SendMail(emailLog.Email, emailLog.Subject, emailLog.Body, ccEmails, bccEmails, attachments);

                    emailLog.Status = res.isSent;
                    if (!string.IsNullOrEmpty(res.log))
                        emailLog.ErrorMessage = res.log;
                    await SaveEmailLog(emailLog);

                }
            }
            catch (Exception ex)
            {
                // Handle exception
                // Log the error or take appropriate action
            }
        }
        private async Task SaveEmailLog(EmpEmailLogs empEmailLogs)
        {
            empEmailLogs.CreatedDate = DateTime.UtcNow;
            empEmailLogs.CreatedBy = empEmailLogs.UserFrom;
            await _emailLogRepository.AddOne(empEmailLogs);
        }


        public Dictionary<NotificationPreferenceType, bool> GetDefaultNotificationPreferences()
        {
            var defaultPreferences = Enum.GetValues(typeof(NotificationPreferenceType)).Cast<NotificationPreferenceType>().ToDictionary(pref => pref, pref => true);
            return defaultPreferences;
        }

        // method to check user notification preference
        public bool IsUserNotificationPreferenceEnabled(string userId, NotificationPreferenceType preferenceType)
        {
            var preferenceResult = _notificationPreferenceRepository.FirstOrDefault(n => n.UserId == userId).Result;
            if (preferenceResult == null)
            {
                var defaultPreferences = GetDefaultNotificationPreferences();
                return defaultPreferences[preferenceType];
            }
            if (preferenceResult != null && preferenceResult.Preferences != null && preferenceResult.Preferences.TryGetValue(preferenceType, out bool value))
            {
                return value;
            }
            return false;
        }
        public string GetCompanyLogoAsDataUrl(string companyLogoPath)
        {
            if (!File.Exists(companyLogoPath)) return string.Empty;
            byte[] logoByteArray = File.ReadAllBytes(companyLogoPath);
            string logoBase64Format = Convert.ToBase64String(logoByteArray);
            string logoExtension = companyLogoPath.Split('.').Last();
            string logoDataUrl = $"data:image/{logoExtension};base64,{logoBase64Format}";
            return logoDataUrl;
        }
    }
}