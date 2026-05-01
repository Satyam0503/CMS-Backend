using System.Threading.Tasks;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, string> _logoDataUrlCache = new();
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



        public async Task<UserModel> GetUserById(string id)
        {
            UserModel userModel = new();
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            EmpUser user = await _userRepository.FirstOrDefault(x => x.UserId == id);
            return _mapper.Map(user, userModel);
        }
        public async Task EmailSendAndSave(EmpEmailLogs emailLog)
        {
            await Common(emailLog);
        }
        public async Task EmailSendAndSave(EmpEmailLogs emailLog, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
        {
            await Common(emailLog, attachments);
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
        public async Task<bool> IsUserNotificationPreferenceEnabled(string userId, NotificationPreferenceType preferenceType)
        {
            var preferenceResult = await _notificationPreferenceRepository.FirstOrDefault(n => n.UserId == userId);
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
            if (_logoDataUrlCache.TryGetValue(companyLogoPath, out var cachedLogoDataUrl))
            {
                return cachedLogoDataUrl;
            }
            byte[] logoByteArray = File.ReadAllBytes(companyLogoPath);
            string logoBase64Format = Convert.ToBase64String(logoByteArray);
            string logoExtension = companyLogoPath.Split('.').Last();
            string logoDataUrl = $"data:image/{logoExtension};base64,{logoBase64Format}";
            _logoDataUrlCache[companyLogoPath] = logoDataUrl;
            return logoDataUrl;
        }
    }
}