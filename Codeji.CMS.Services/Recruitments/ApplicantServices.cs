using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments
{
    public class ApplicantServices : IApplicantsService
    {
        readonly IMongoDbRepository<Applicant> _applicantRepository;
        private readonly IMongoDbRepository<ApplicantLogs> _ApplicantLogsRepository;
        private readonly IMongoDbRepository<EmpUser> _employeeRepository;
        readonly IMapper _mapper;
        private readonly IJobVacancy _jobVacancyService;
        readonly IMongoDbRepository<Company> _companyRepository;
        readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
        readonly IMongoDbRepository<JobVacancy> _jobVacancyRepository;
        private readonly IPriorityTaskQueue _priorityTaskQueue;

        private readonly IMiddlewareService _middlewareService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicantServices(IMongoDbRepository<Applicant> applicantDbRepository,
            IMapper mapper,
            IJobVacancy jobVacancyService,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository,
            IMongoDbRepository<JobVacancy> jobVacancyRepository,
            IMongoDbRepository<ApplicantLogs> applicantLogsRepository,
            IMongoDbRepository<EmpUser> employeeRepository,
            IPriorityTaskQueue priorityTaskQueue,
            IMiddlewareService middlewareService,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _applicantRepository = applicantDbRepository;
            _mapper = mapper;
            _jobVacancyService = jobVacancyService;
            _companyRepository = companyRepository;
            _mailTemplateRepository = mailTemplateRepository;
            _jobVacancyRepository = jobVacancyRepository;
            _ApplicantLogsRepository = applicantLogsRepository;
            _employeeRepository = employeeRepository;
            _priorityTaskQueue = priorityTaskQueue;
            _middlewareService = middlewareService;
            _httpContextAccessor = httpContextAccessor;
        }
        /// <summary>
        /// For Annonymous add and update applicants
        /// </summary>
        /// <param name="applicantRegisterModel"></param>
        /// <returns></returns>
        public async Task<Result> RegisterApplicants(ApplicantAddEditModel applicantRegisterModel)
        {
            Result result = new();
            bool isApplicantExist = await _applicantRepository.Exist(ap => ap.Email == applicantRegisterModel.Email);
            if (isApplicantExist)
            {
                result.StatusCode = CustomStatusCode.ApplicantAlreadyExist;
                return result;
            }

            Applicant applicant = new Applicant()
            {
                ApplicantId = Guid.NewGuid().ToString(),
                FirstName = applicantRegisterModel.FirstName,
                LastName = applicantRegisterModel.LastName,
                Experience = applicantRegisterModel.Experience,
                VacancyId = applicantRegisterModel.VacancyId,
                Phone = applicantRegisterModel.Phone,
                Email = applicantRegisterModel.Email,
                Status = applicantRegisterModel.Status,
                State = applicantRegisterModel.State,
            };
            result = await _applicantRepository.AddOne(applicant);
            if (result.Success)
            {
                await SendEmailToApplicant(applicant);
            }
            return result;
        }

        public async Task<Result> UpdateApplicants(ApplicantAddEditModel model)
        {
            Result result = new();
            Expression<Func<Applicant, bool>> whereCondition = x => x.ApplicantId == model.ApplicantId && x.Email == model.Email;
            Applicant? entity = await _applicantRepository.FirstOrDefault(whereCondition);
            if (entity == null) return result;

            entity.Experience = model.Experience;
            entity.VacancyId = model.VacancyId;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.Phone = model.Phone;
            entity.VacancyId = model.VacancyId;
            entity.ActivityType = model.ActivityType;
            entity.Status = model.Status;
            entity.State = model.State;
            result = await _applicantRepository.Update(whereCondition, entity);
            if (result.Success) await SendEmailToApplicant(entity);
            return result;
        }
        public async Task<Result> ApplyNowService(ApplicantAddEditModel model)
        {
            Result result = new();
            Applicant? applicant = await _applicantRepository.FirstOrDefault(a => a.Email == model.Email);
            if (applicant != null)
            {
                var canApplyAgain = DateTime.UtcNow.AddMonths(-6) > applicant.CreatedDate;
                if (!canApplyAgain)
                {
                    result.Success = false;
                    result.StatusCode = CustomStatusCode.ApplyAfterWaitingPeriod;
                    return result;
                }
                else
                {
                    model.ActivityType = EnumsHelper.ActivityType.ReApply;
                    model.Status = EnumsHelper.ActivityStatus.Active;
                }
            }

            Applicant user = new Applicant()
            {
                ApplicantId = Guid.NewGuid().ToString(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Experience = model.Experience,
                VacancyId = model.VacancyId,
                Phone = model.Phone,
                Email = model.Email,
                Status = EnumsHelper.ActivityStatus.Active,
                State = model.State,
                ActivityType = EnumsHelper.ActivityType.New,
            };
            result = await _applicantRepository.AddOne(user);
            if (result.Success)
            {
                await SendEmailToApplicant(user);
            }
            return result;
        }

        public async Task<Result> UploadResume(IFormFile resume, string email)
        {
            Result result = new();
            Applicant? applicant = await _applicantRepository.FirstOrDefault(a => a.Email == email);
            if (applicant is null) return result;

            // upload folder path 
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Resume");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            string fileExtension = Path.GetExtension(resume.FileName);
            string fileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
            string filePath = Path.Combine(uploadFolder, fileName);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                await resume.CopyToAsync(fileStream);
            }

            // check if applicant already has resume uploaded
            if (!string.IsNullOrEmpty(applicant.ResumeUrl))
            {
                string oldPath = Path.Combine(uploadFolder, applicant.ResumeUrl);
                if (File.Exists(oldPath))
                {
                    FileInfo fileInfo = new(oldPath);
                    fileInfo.Delete();
                }
            }
            // upload resume
            Expression<Func<Applicant, bool>> expression = a => a.ApplicantId == applicant.ApplicantId;
            return await _applicantRepository.UpdateMany(expression, Builders<Applicant>.Update.Set(a => a.ResumeUrl, fileName).Set(a => a.UpdatedDate, DateTime.UtcNow));
        }

        //Get Applicant List Using Filter Change this logic in Future
        public async Task<Result<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters? filters)
        {
            IEnumerable<Applicant> applicantList = [];
            int count = 0;
            if (filters is null)
            {
                applicantList = await _applicantRepository.GetAll();
                count = applicantList.Count();
            }
            else
            {
                Expression<Func<Applicant, bool>> whereCondition = x =>
                ((!filters.FilterFrom.HasValue || filters.FilterFrom.Value <= x.CreatedDate) && (!filters.FilterTo.HasValue || filters.FilterTo >= x.CreatedDate))
                && (!filters.ActivityTypes.Any() || filters.ActivityTypes.Contains(x.ActivityType))
                && (!filters.Status.Any() || filters.Status.Contains(x.Status))
                && (!filters.VacancyIds.Any() || filters.VacancyIds.Contains(x.VacancyId))
                && (!filters.MinExperience.HasValue || (x.Experience >= filters.MinExperience && x.Experience <= filters.MaxExperience))
                && (string.IsNullOrEmpty(filters.Name)
                || x.FirstName.Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase)
                || x.LastName.Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase)
                || (x.FirstName + " " + x.LastName).Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase));
                applicantList = await _applicantRepository.GetAggregateDataAsync<Applicant>(whereCondition, pageNo: filters.PageNo, pageSize: filters.PageSize, isAscending: false, orderedKey: "CreatedDate");
                count = await _applicantRepository.Count(whereCondition);
            }
            var vacancyIds = applicantList.Select(a => a.VacancyId).Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
            List<JobVacancy> vacancies = vacancyIds.Count == 0
                ? []
                : (await _jobVacancyRepository.GetAll(v => vacancyIds.Contains(v.JobId))).ToList();
            List<ApplicantViewModel> data = (from applicant in applicantList
                                             join vacancy in vacancies on applicant.VacancyId equals vacancy.JobId
                                             select new ApplicantViewModel
                                             {
                                                 ApplicantId = applicant.ApplicantId,
                                                 FirstName = applicant.FirstName,
                                                 LastName = applicant.LastName,
                                                 Email = applicant.Email,
                                                 Phone = applicant.Phone,
                                                 Status = applicant.Status,
                                                 ActivityType = applicant.ActivityType,
                                                 VacancyName = vacancy.Title,
                                                 VacancyId = vacancy.JobId,
                                                 State = applicant.State,
                                                 Experience = applicant.Experience,
                                                 ApplyDate = applicant.CreatedDate,
                                                 ResumeUrl = Common.GetApplicantResumeFullPath(applicant.ResumeUrl)
                                             }).OrderByDescending(x => x.ApplyDate).ToList();
            return new Result<ApplicantViewModel>
            {
                Success = true,
                TotalRecords = count,
                MethodResults = data
            };
        }

        public async Task<Result<ApplicantViewModel>> ApplicantById(string applicantId)
        {
            Result<ApplicantViewModel> result = new Result<ApplicantViewModel>();
            Applicant? applicant = await _applicantRepository.FirstOrDefault(x => x.ApplicantId == applicantId);
            JobVacancy? vacancy = await _jobVacancyRepository.FirstOrDefault(x => applicant != null && applicant.VacancyId == x.JobId);
            if (applicant is not null)
            {
                ApplicantViewModel data = new ApplicantViewModel
                {
                    ApplicantId = applicant.ApplicantId,
                    FirstName = applicant.FirstName,
                    LastName = applicant.LastName,
                    Email = applicant.Email,
                    Phone = applicant.Phone,
                    Status = applicant.Status,
                    ActivityType = applicant.ActivityType,
                    VacancyName = vacancy?.Title ?? string.Empty,
                    VacancyId = vacancy?.JobId ?? string.Empty,
                    State = applicant.State,
                    Experience = applicant.Experience,
                    ApplyDate = applicant.CreatedDate,
                    ResumeUrl = Common.GetApplicantResumeFullPath(applicant.ResumeUrl)
                };
                result.Success = true;
                result.MethodResult = data;
            }
            return result;
        }

        //Logic For Addig Comment on Applicant By Employee (Admin And HR Manager )

        public async Task<Result> AddComment(string userId, CommentRequestModel model)
        {
            var applicant = await _applicantRepository.FirstOrDefault(x => x.ApplicantId == model.ApplicantId);
            ApplicantLogs comments = new()
            {
                UserId = userId,
                ApplicantId = model.ApplicantId,
                ActivityCategory = model.ActivityCategory,
                Description = model.Description,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId,
                JobRole = model.JobTitle,
                ApplicantName = applicant == null ? "" : $"{applicant.FirstName} {applicant.LastName}"
            };
            await _ApplicantLogsRepository.AddOne(comments);
            Result result = new()
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
            };
            return result;
        }
        public async Task<Result<ApplicantLogResponseModel>> GetAllComment(string applicantId, int pageNo, int pageSize)
        {
            var logCount = await _ApplicantLogsRepository.Count(x => x.ApplicantId == applicantId);
            var logList = (await _ApplicantLogsRepository.GetAggregateDataAsync<ApplicantLogs>(x => x.ApplicantId == applicantId, pageNo: pageNo, pageSize: pageSize)).ToList();
            string[] userIds = logList.Select(x => x.UserId).Distinct().ToArray();
            var users = (await _employeeRepository.GetAll(x => userIds.Contains(x.UserId))).ToList();
            var data = (from log in logList
                        join user in users on log.UserId equals user.UserId
                        select new ApplicantLogResponseModel
                        {
                            Id = log.Id,
                            Description = log.Description,
                            ApplicantId = log.ApplicantId,
                            ActivityCategory = log.ActivityCategory,
                            JobRole = log.JobRole,
                            UserId = log.UserId,
                            UserName = $"{user.FirstName} {user.LastName}",
                            CreatedDate = log.CreatedDate,
                            CreatedBy = log.CreatedBy,
                        }).OrderByDescending(x => x.CreatedDate).ToList();

            Result<ApplicantLogResponseModel> result = new()
            {
                Success = true,
                MethodResults = data,
                TotalRecords = logCount,
            };
            return result;
        }
        public async Task<Result<ApplicantLogResponseModel>> GetProcessLogData(ApplicantLogFilterModel filters)
        {
            Expression<Func<ApplicantLogs, bool>> whereCondition = x =>
            (!filters.FilterFrom.HasValue || (x.CreatedDate >= filters.FilterFrom))
            && (!filters.FilterTo.HasValue || (x.CreatedDate <= filters.FilterTo))
            && (filters.ActivityCategory.Length == 0 || filters.ActivityCategory.Contains(x.ActivityCategory))
            && (string.IsNullOrEmpty(filters.JobRole) || x.JobRole.Contains(filters.JobRole, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrEmpty(filters.ApplicantName) || x.ApplicantName.Contains(filters.ApplicantName, StringComparison.CurrentCultureIgnoreCase));

            var logCount = await _ApplicantLogsRepository.Count(whereCondition);
            var logList = (await _ApplicantLogsRepository.GetAggregateDataAsync<ApplicantLogs>(whereCondition, pageNo: filters.PageNo, pageSize: filters.PageSize)).ToList();
            string[] empId = logList.Select(x => x.UserId).Distinct().ToArray();
            var users = await _employeeRepository.GetAll(x => empId.Contains(x.UserId));
            var data = (from log in logList
                        join user in users on log.UserId equals user.UserId
                        select new ApplicantLogResponseModel
                        {
                            Id = log.Id,
                            Description = log.Description,
                            ApplicantId = log.ApplicantId,
                            ActivityCategory = log.ActivityCategory,
                            JobRole = log.JobRole,
                            UserId = log.UserId,
                            UserName = $"{user.FirstName} {user.LastName}",
                            ApplicantName = log.ApplicantName,
                            CompanyId = log.CompanyId,
                            CreatedBy = log.CreatedBy,
                            CreatedDate = log.CreatedDate,
                        }).OrderByDescending(x => x.CreatedDate).ToList();

            Result<ApplicantLogResponseModel> result = new()
            {
                Success = true,
                MethodResults = data,
                TotalRecords = logCount,
            };
            return result;
        }

        private async Task SendEmailToApplicant(Applicant applicant)
        {
            var vacancy = await _jobVacancyService.GetVacancyById(applicant.VacancyId);
            if (vacancy is null) return;
            var currentUser = await _middlewareService.GetUserById(CurrentContext.UserId(_httpContextAccessor));
            MailTemplate? emailContent = applicant.ActivityType switch
            {
                EnumsHelper.ActivityType.New => await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.ApplyNowMailToApplicant),
                EnumsHelper.ActivityType.Selected => await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.SelectedMail),
                EnumsHelper.ActivityType.Rejected => await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.RejectedMail),
                _ => null
            };
            if (emailContent == null) return;

            string emailBody = HtmlTemplate.Render(emailContent.body, new
            {
                CandidateName = applicant.FirstName + " " + applicant.LastName,
                JobTitle = vacancy.Title,
            });

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                await _middlewareService.EmailSendAndSave(new EmpEmailLogs()
                {
                    UserTo = applicant.ApplicantId,
                    Subject = emailContent.subject ?? "",
                    Body = emailBody,
                    EmailLogType = emailContent.mailType,
                    Email = applicant.Email,
                    UserFrom = currentUser != null ? currentUser.UserId : string.Empty
                });
            }, priority: 1);
        }
    }
}

