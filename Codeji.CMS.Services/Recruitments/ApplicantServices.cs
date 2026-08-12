using System.Linq.Expressions;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
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
        readonly IMongoDbRepository<Roles> _rolesRepository;
        private readonly IPriorityTaskQueue _priorityTaskQueue;

        private readonly IMiddlewareService _middlewareService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicantServices(IMongoDbRepository<Applicant> applicantDbRepository,
            IMapper mapper,
            IJobVacancy jobVacancyService,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository,
            IMongoDbRepository<JobVacancy> jobVacancyRepository,
            IMongoDbRepository<Roles> rolesRepository,
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
            _rolesRepository = rolesRepository;
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
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            bool isApplicantExist = await _applicantRepository.Exist(ap => ap.CompanyId == companyId && ap.Email == applicantRegisterModel.Email);
            if (isApplicantExist)
            {
                result.StatusCode = CustomStatusCode.ApplicantAlreadyExist;
                return result;
            }

            Applicant applicant = new Applicant()
            {
                ApplicantId = Guid.NewGuid().ToString(),
                CompanyId = companyId,
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
                await LogNewApplication(applicant);
                await SendApplicationAlertToRecruitmentTeam(applicant);
                await SendEmailToApplicant(applicant);
            }
            return result;
        }

        private async Task LogNewApplication(Applicant applicant)
        {
            var vacancy = await _jobVacancyService.GetVacancyById(applicant.VacancyId);
            ApplicantLogs log = new()
            {
                CompanyId = applicant.CompanyId,
                ApplicantId = applicant.ApplicantId,
                // This is a candidate-submission event, not a staff action.
                UserId = applicant.ApplicantId,
                ActivityCategory = 0, // CommentAction.Process — initial application submission
                Description = "New application submitted",
                JobRole = vacancy?.Title ?? string.Empty,
                ApplicantName = $"{applicant.FirstName} {applicant.LastName}",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = applicant.ApplicantId,
            };
            await _ApplicantLogsRepository.AddOne(log);
        }

        public async Task<Result> UpdateApplicants(ApplicantAddEditModel model, string actorUserId)
        {
            Result result = new();
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                result.Message = "An authenticated user is required to update an applicant.";
                return result;
            }

            // The actor and tenant always come from authentication, never from the request payload.
            var actor = await _employeeRepository.FirstOrDefault(x => x.UserId == actorUserId && x.CompanyId == companyId && !x.IsDeleted);
            if (actor is null)
            {
                result.Message = "The authenticated user is not available in this company.";
                return result;
            }

            Expression<Func<Applicant, bool>> whereCondition = x =>
                x.CompanyId == companyId && x.ApplicantId == model.ApplicantId && x.Email == model.Email;
            Applicant? entity = await _applicantRepository.FirstOrDefault(whereCondition);
            if (entity == null) return result;

            var previousActivityType = entity.ActivityType;
            if (!IsAllowedActivityTransition(previousActivityType, model.ActivityType, out var transitionError))
            {
                result.Message = transitionError;
                return result;
            }

            entity.Experience = model.Experience;
            entity.VacancyId = model.VacancyId;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.Phone = model.Phone;
            entity.VacancyId = model.VacancyId;
            entity.ActivityType = model.ActivityType;
            entity.Status = model.Status;
            entity.State = model.State;
            entity.UpdatedBy = actorUserId;
            entity.UpdatedDate = DateTime.UtcNow;
            // Compare the persisted status while writing. A second HR user working from a
            // stale screen cannot overwrite a newer workflow stage.
            Expression<Func<Applicant, bool>> updateCondition = x =>
                x.CompanyId == companyId &&
                x.ApplicantId == model.ApplicantId &&
                x.Email == model.Email &&
                x.ActivityType == previousActivityType;
            result = await _applicantRepository.Update(updateCondition, entity);
            if (!result.Success)
            {
                result.Message = "This application was updated by another user. Refresh it and try again.";
                return result;
            }
            if (result.Success && previousActivityType != entity.ActivityType)
            {
                await AddStatusHistory(entity, previousActivityType, actorUserId);
            }
            // A candidate receives one email when HR moves the application to a new stage.
            // Ordinary edits (phone, state, etc.) must not send duplicate emails.
            if (result.Success && previousActivityType != entity.ActivityType) await SendEmailToApplicant(entity);
            return result;
        }

        private async Task<Result> AddStatusHistory(Applicant applicant, EnumsHelper.ActivityType previousActivityType, string actorUserId)
        {
            var vacancy = await _jobVacancyRepository.FirstOrDefault(x =>
                x.CompanyId == applicant.CompanyId && x.JobId == applicant.VacancyId);
            return await _ApplicantLogsRepository.AddOne(new ApplicantLogs
            {
                CompanyId = applicant.CompanyId,
                ApplicantId = applicant.ApplicantId,
                UserId = actorUserId,
                CreatedBy = actorUserId,
                CreatedDate = DateTime.UtcNow,
                ActivityCategory = 0,
                Description = $"{FormatApplicantName(applicant)} status changed from {previousActivityType} to {applicant.ActivityType}.",
                JobRole = vacancy?.Title ?? string.Empty,
                ApplicantName = $"{applicant.FirstName} {applicant.LastName}"
            });
        }

        private static string FormatApplicantName(Applicant applicant)
        {
            var name = $"{applicant.FirstName} {applicant.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? "Applicant" : name;
        }

        private static bool IsAllowedActivityTransition(
            EnumsHelper.ActivityType current,
            EnumsHelper.ActivityType target,
            out string error)
        {
            error = string.Empty;
            if (current == target) return true;

            if (current is EnumsHelper.ActivityType.Selected or EnumsHelper.ActivityType.Rejected or EnumsHelper.ActivityType.ReApply)
            {
                error = $"Applicant status cannot be changed after it is {current}.";
                return false;
            }

            // Rejection is the existing terminal side path and remains available from
            // any non-terminal hiring stage. ReApply is a public-application flow only.
            if (target == EnumsHelper.ActivityType.Rejected) return true;
            if (target is EnumsHelper.ActivityType.New or EnumsHelper.ActivityType.ReApply)
            {
                error = $"Applicant status cannot be changed from {current} to {target}.";
                return false;
            }

            if ((int)target > (int)current) return true; // preserve existing forward skipping

            error = $"Applicant status cannot be changed from {current} back to {target}.";
            return false;
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
                await SendApplicationAlertToRecruitmentTeam(user);
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
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            List<Applicant> applicantList = [];
            int count = 0;
            if (filters is null)
            {
                applicantList = (await _applicantRepository.GetAll(x => x.CompanyId == companyId)).ToList();
                count = applicantList.Count;
            }
            else
            {
                Expression<Func<Applicant, bool>> whereCondition = x =>
                x.CompanyId == companyId &&
                ((!filters.FilterFrom.HasValue || filters.FilterFrom.Value <= x.CreatedDate) && (!filters.FilterTo.HasValue || filters.FilterTo >= x.CreatedDate))
                && (!filters.ActivityTypes.Any() || filters.ActivityTypes.Contains(x.ActivityType))
                && (!filters.Status.Any() || filters.Status.Contains(x.Status))
                && (!filters.VacancyIds.Any() || filters.VacancyIds.Contains(x.VacancyId))
                && (!filters.MinExperience.HasValue || (x.Experience >= filters.MinExperience && x.Experience <= filters.MaxExperience))
                && (string.IsNullOrEmpty(filters.Name)
                || x.FirstName.Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase)
                || x.LastName.Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase)
                || (x.FirstName + " " + x.LastName).Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase));
                applicantList = (await _applicantRepository.GetAggregateDataAsync<Applicant>(whereCondition, pageNo: filters.PageNo, pageSize: filters.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();
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
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            Applicant? applicant = await _applicantRepository.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicantId);
            JobVacancy? vacancy = await _jobVacancyRepository.FirstOrDefault(x => applicant != null && x.CompanyId == companyId && applicant.VacancyId == x.JobId);
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
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var applicant = await _applicantRepository.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == model.ApplicantId);
            if (applicant is null) return new Result { Message = "Applicant was not found." };

            ApplicantLogs comments = new()
            {
                CompanyId = companyId,
                UserId = userId,
                ApplicantId = model.ApplicantId,
                ActivityCategory = model.ActivityCategory,
                Description = model.Description,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId,
                JobRole = model.JobTitle,
                ApplicantName = $"{applicant.FirstName} {applicant.LastName}"
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
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var applicantExists = await _applicantRepository.Exist(x => x.CompanyId == companyId && x.ApplicantId == applicantId);
            if (!applicantExists) return new Result<ApplicantLogResponseModel> { Success = false, Message = "Applicant was not found." };

            var logCount = await _ApplicantLogsRepository.Count(x => x.CompanyId == companyId && x.ApplicantId == applicantId);
            var logList = (await _ApplicantLogsRepository.GetAggregateDataAsync<ApplicantLogs>(x => x.CompanyId == companyId && x.ApplicantId == applicantId, pageNo: pageNo, pageSize: pageSize)).ToList();
            string[] userIds = logList.Select(x => x.UserId).Distinct().ToArray();
            var users = (await _employeeRepository.GetAll(x => x.CompanyId == companyId && userIds.Contains(x.UserId))).ToList();
            var data = (from log in logList
                        join user in users on log.UserId equals user.UserId into joined
                        from user in joined.DefaultIfEmpty()
                        select new ApplicantLogResponseModel
                        {
                            Id = log.Id,
                            Description = log.Description,
                            ApplicantId = log.ApplicantId,
                            ActivityCategory = log.ActivityCategory,
                            JobRole = log.JobRole,
                            UserId = log.UserId,
                            // Applicant-name fallback is only legitimate for the original candidate submission.
                            // Staff status/comment events persist the authenticated user id and cannot become the applicant.
                            UserName = user != null ? $"{user.FirstName} {user.LastName}" : log.UserId == log.ApplicantId ? log.ApplicantName : "System",
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
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            Expression<Func<ApplicantLogs, bool>> whereCondition = x =>
            x.CompanyId == companyId &&
            (!filters.FilterFrom.HasValue || (x.CreatedDate >= filters.FilterFrom))
            && (!filters.FilterTo.HasValue || (x.CreatedDate <= filters.FilterTo))
            && (filters.ActivityCategory.Length == 0 || filters.ActivityCategory.Contains(x.ActivityCategory))
            && (string.IsNullOrEmpty(filters.JobRole) || x.JobRole.Contains(filters.JobRole, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrEmpty(filters.ApplicantName) || x.ApplicantName.Contains(filters.ApplicantName, StringComparison.CurrentCultureIgnoreCase));

            var logCount = await _ApplicantLogsRepository.Count(whereCondition);
            var logList = (await _ApplicantLogsRepository.GetAggregateDataAsync<ApplicantLogs>(whereCondition, pageNo: filters.PageNo, pageSize: filters.PageSize)).ToList();
            string[] empId = logList.Select(x => x.UserId).Distinct().ToArray();
            var users = await _employeeRepository.GetAll(x => x.CompanyId == companyId && empId.Contains(x.UserId));
            var data = (from log in logList
                        join user in users on log.UserId equals user.UserId into joined
                        from user in joined.DefaultIfEmpty()
                        select new ApplicantLogResponseModel
                        {
                            Id = log.Id,
                            Description = log.Description,
                            ApplicantId = log.ApplicantId,
                            ActivityCategory = log.ActivityCategory,
                            JobRole = log.JobRole,
                            UserId = log.UserId,
                            UserName = user != null ? $"{user.FirstName} {user.LastName}" : log.UserId == log.ApplicantId ? log.ApplicantName : "System",
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
            EnumsHelper.MailType? templateType = applicant.ActivityType switch
            {
                EnumsHelper.ActivityType.New => EnumsHelper.MailType.ApplyNowMailToApplicant,
                EnumsHelper.ActivityType.Selected => EnumsHelper.MailType.SelectedMail,
                EnumsHelper.ActivityType.Rejected => EnumsHelper.MailType.RejectedMail,
                _ => null
            };
            string templateSubject = string.Empty;
            string templateBody = string.Empty;
            if (templateType.HasValue) RepositoryEmailTemplate.TryGet(templateType.Value, out templateSubject, out templateBody);

            string candidateName = applicant.FirstName + " " + applicant.LastName;
            string subject = string.IsNullOrWhiteSpace(templateSubject) ? $"Update on your application for {vacancy.Title}" : templateSubject;
            string fallbackBody = applicant.ActivityType switch
            {
                EnumsHelper.ActivityType.New => "<p>Hi [CandidateName],</p><p>We received your application for <strong>[JobTitle]</strong>. Our team will review it and get back to you soon.</p>",
                EnumsHelper.ActivityType.InProgress => "<p>Hi [CandidateName],</p><p>Your application for <strong>[JobTitle]</strong> is now under review. Our hiring team will contact you when there is an update.</p>",
                EnumsHelper.ActivityType.OnHold => "<p>Hi [CandidateName],</p><p>Your application for <strong>[JobTitle]</strong> is currently on hold. We will let you know as soon as there is an update.</p>",
                EnumsHelper.ActivityType.Shortlisted => "<p>Hi [CandidateName],</p><p>Good news — you have been shortlisted for the <strong>[JobTitle]</strong> position. Our hiring team will contact you soon about the next steps.</p>",
                EnumsHelper.ActivityType.Selected => "<p>Hi [CandidateName],</p><p>Congratulations — you have been selected for the <strong>[JobTitle]</strong> position. Our team will contact you shortly with the next steps.</p>",
                EnumsHelper.ActivityType.Rejected => "<p>Hi [CandidateName],</p><p>Thank you for applying for <strong>[JobTitle]</strong>. After careful consideration, we will not be moving forward at this time. We wish you the best in your job search.</p>",
                EnumsHelper.ActivityType.ReApply => "<p>Hi [CandidateName],</p><p>We received your new application for <strong>[JobTitle]</strong>. Our team will review it and share updates with you.</p>",
                _ => "<p>Hi [CandidateName],</p><p>There is an update on your application for <strong>[JobTitle]</strong>. Our team will contact you if any action is needed.</p>"
            };

            string emailBody = HtmlTemplate.Render(string.IsNullOrWhiteSpace(templateBody) ? fallbackBody : templateBody, new
            {
                CandidateName = candidateName,
                JobTitle = vacancy.Title,
            });

            subject = HtmlTemplate.Render(subject, new { CandidateName = candidateName, JobTitle = vacancy.Title });

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                await _middlewareService.EmailSendAndSave(new EmpEmailLogs()
                {
                    UserTo = applicant.ApplicantId,
                    Subject = subject,
                    Body = emailBody,
                    EmailLogType = templateType ?? EnumsHelper.MailType.ApplyNowMailToApplicant,
                    Email = applicant.Email,
                    UserFrom = currentUser != null ? currentUser.UserId : string.Empty
                });
            }, priority: 1);
        }

        private async Task SendApplicationAlertToRecruitmentTeam(Applicant applicant)
        {
            JobVacancy? vacancy = await _jobVacancyRepository.FirstOrDefault(x => x.JobId == applicant.VacancyId);
            if (vacancy == null || string.IsNullOrWhiteSpace(vacancy.CompanyId)) return;

            Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == vacancy.CompanyId);
            string candidateName = $"{applicant.FirstName} {applicant.LastName}".Trim();
            RepositoryEmailTemplate.TryGet(EnumsHelper.MailType.ApplyNowMailToHR, out var templateSubject, out var templateBody);
            string subject = HtmlTemplate.Render(
                string.IsNullOrWhiteSpace(templateSubject) ? $"New application received for {vacancy.Title}" : templateSubject,
                new { CandidateName = candidateName, CandidateEmail = applicant.Email, CandidatePhone = applicant.Phone, JobTitle = vacancy.Title, CompanyName = company?.CompanyName ?? string.Empty });
            string body = HtmlTemplate.Render(
                string.IsNullOrWhiteSpace(templateBody) ?
                "<p>A new application has been submitted.</p><ul><li><strong>Name:</strong> [CandidateName]</li><li><strong>Email:</strong> [CandidateEmail]</li><li><strong>Phone:</strong> [CandidatePhone]</li><li><strong>Job Title:</strong> [JobTitle]</li><li><strong>Company:</strong> [CompanyName]</li></ul>" : templateBody,
                new { CandidateName = candidateName, CandidateEmail = applicant.Email, CandidatePhone = applicant.Phone, JobTitle = vacancy.Title, CompanyName = company?.CompanyName ?? string.Empty });

            List<(string UserId, string Email)> recipients = await GetRecruitmentRecipients(vacancy.CompanyId, vacancy.RecruiterContactEmail);
            if (recipients.Count == 0) return;

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                foreach (var recipient in recipients)
                {
                    await _middlewareService.EmailSendAndSave(new EmpEmailLogs
                    {
                        CompanyId = vacancy.CompanyId,
                        UserTo = recipient.UserId,
                        UserFrom = CurrentContext.UserId(_httpContextAccessor) ?? "system",
                        Email = recipient.Email,
                        Subject = subject,
                        Body = body,
                        EmailLogType = EnumsHelper.MailType.ApplyNowMailToHR
                    });
                }
            }, priority: 1);
        }

        private async Task<List<(string UserId, string Email)>> GetRecruitmentRecipients(string companyId, string? recruiterContactEmail)
        {
            Dictionary<string, (string UserId, string Email)> recipientMap = new(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(recruiterContactEmail))
            {
                string email = recruiterContactEmail.Trim();
                recipientMap[email] = ("recruiter-contact", email);
            }

            var roleIds = (await _rolesRepository.GetAll(r => r.CompanyId == companyId && !r.IsDeleted &&
                (r.RoleType == (int)EnumsHelper.Roles.Administrator || r.RoleType == (int)EnumsHelper.Roles.HR || r.RoleType == (int)EnumsHelper.Roles.HRExecutive)))
                .Select(r => r.RolesId)
                .Distinct()
                .ToList();
            if (roleIds.Count == 0) return recipientMap.Values.ToList();

            var users = (await _employeeRepository.GetAll(u => u.CompanyId == companyId && u.Status && u.IsEmailVerified &&
                roleIds.Contains(u.RoleId) && !string.IsNullOrWhiteSpace(u.Email))).ToList();
            foreach (var user in users)
            {
                string email = user.Email.Trim();
                recipientMap[email] = (user.UserId, email);
            }

            return recipientMap.Values.ToList();
        }
    }
}

