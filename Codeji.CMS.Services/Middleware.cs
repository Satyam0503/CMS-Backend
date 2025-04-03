using System.Threading.Tasks;
using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services
{
    public class MiddlewareService : IMiddlewareService
    {
        private readonly IMongoDbRepository<EmpUser> _userRepository;
        private readonly IMongoDbRepository<EmpEmailLogs> _emailLogRepository;
        private readonly IMapper _mapper;
        public MiddlewareService(IMongoDbRepository<EmpUser> userRepository,
            IMongoDbRepository<EmpEmailLogs> emailLogRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _emailLogRepository = emailLogRepository;
            _mapper = mapper;
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
    }
}

