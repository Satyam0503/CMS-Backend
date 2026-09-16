using System.Threading.Tasks;
using System.Collections.Concurrent;
using MapsterMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Company;
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
        readonly IMongoDbRepository<Company> _companyRepository;
        readonly IEmployeeService _employeeService;
        public MiddlewareService(IMongoDbRepository<EmpUser> userRepository,
            IMongoDbRepository<EmpEmailLogs> emailLogRepository,
            IMapper mapper,
            IMongoDbRepository<NotificationPreference> notificationPreferenceRepository,
            IMongoDbRepository<Company> companyRepository
        )
        {
            _userRepository = userRepository;
            _emailLogRepository = emailLogRepository;
            _mapper = mapper;
            _notificationPreferenceRepository = notificationPreferenceRepository;
            _companyRepository = companyRepository;
        }



        public async Task<UserModel?> GetUserById(string id)
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
        public async Task<(bool IsSent, string Error)> EmailSendAndSaveWithResult(EmpEmailLogs emailLog)
        {
            return await Common(emailLog);
        }
        public async Task EmailSendAndSave(EmpEmailLogs emailLog, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
        {
            await Common(emailLog, attachments);
        }
        private async Task<(bool IsSent, string Error)> Common(EmpEmailLogs emailLog, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
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
                    return (res.isSent, res.log);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            return (false, "Email details are missing.");
        }
        private async Task SaveEmailLog(EmpEmailLogs empEmailLogs)
        {
            empEmailLogs.CreatedDate = DateTime.UtcNow;
            empEmailLogs.CreatedBy = empEmailLogs.UserFrom;
            await _emailLogRepository.AddOne(empEmailLogs);
        }


        public Dictionary<NotificationPreferenceType, bool> GetDefaultNotificationPreferences()
        {
            return Enum.GetValues(typeof(NotificationPreferenceType))
                .Cast<NotificationPreferenceType>()
                .ToDictionary(preference => preference, _ => true);
        }

        // method to check user notification preference
        public async Task<bool> IsUserNotificationPreferenceEnabled(string userId, NotificationPreferenceType preferenceType)
        {
            var user = await _userRepository.FirstOrDefault(x => x.UserId == userId);
           if(user?.CompanyId is null)
            {
                return true;
            }

            var company=await _companyRepository.FirstOrDefault(x => x.CompanyId == user.CompanyId && !x.IsDeleted);
            if(company?.NotificationPreferences is {Count: >0} companyPreferences &&
                companyPreferences.TryGetValue(preferenceType, out var companyEnabled) &&
                !companyEnabled)
            {
                // A company-disabled category cannot be enabled by an employee.
                return false;
            }

            var preferenceResult = await _notificationPreferenceRepository.FirstOrDefault(
                n => n.UserId == userId && n.CompanyId == user.CompanyId);
            if (preferenceResult?.Preferences is not null &&
                preferenceResult.Preferences.TryGetValue(preferenceType, out var employeeEnabled))
            {
                return employeeEnabled;
            }

            // Company enabled (or unset) and no employee override: enabled by default.
            return true;
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
