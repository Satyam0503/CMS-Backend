using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
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
        private readonly IMongoDbRepository<PublicApplicationToken>? _applicationTokenRepository;
        private readonly IMongoDbRepository<RecruitmentJobApplication>? _jobApplicationRepository;
        private readonly IMongoDbRepository<RecruitmentEnterpriseConfig>? _configRepository;
        private readonly IMongoDbRepository<RecruitmentHandoff>? _handoffRepository;
        private readonly IMongoDbRepository<RecruitmentScorecard>? _scorecardRepository;
        private readonly IMongoDbRepository<RecruitmentInterview>? _interviewRepository;

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
            IHttpContextAccessor httpContextAccessor,
            IMongoDbRepository<PublicApplicationToken>? applicationTokenRepository = null,
            IMongoDbRepository<RecruitmentJobApplication>? jobApplicationRepository = null,
            IMongoDbRepository<RecruitmentEnterpriseConfig>? configRepository = null,
            IMongoDbRepository<RecruitmentHandoff>? handoffRepository = null,
            IMongoDbRepository<RecruitmentScorecard>? scorecardRepository = null,
            IMongoDbRepository<RecruitmentInterview>? interviewRepository = null
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
            _applicationTokenRepository = applicationTokenRepository;
            _jobApplicationRepository = jobApplicationRepository;
            _configRepository = configRepository;
            _handoffRepository = handoffRepository;
            _scorecardRepository = scorecardRepository;
            _interviewRepository = interviewRepository;
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
            var config = _configRepository is null ? null : await _configRepository.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);
            var allowMultiVacancy = config?.AllowMultiVacancyApplications ?? true;

            // REC-BUG-07 Fix: Scope duplicate check to !ap.IsDeleted and to the specific VacancyId when AllowMultiVacancyApplications is enabled.
            bool isApplicantExist = allowMultiVacancy && !string.IsNullOrWhiteSpace(applicantRegisterModel.VacancyId)
                ? await _applicantRepository.Exist(ap => ap.CompanyId == companyId && ap.VacancyId == applicantRegisterModel.VacancyId && ap.Email == applicantRegisterModel.Email && !ap.IsDeleted)
                : await _applicantRepository.Exist(ap => ap.CompanyId == companyId && ap.Email == applicantRegisterModel.Email && !ap.IsDeleted);
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
                PipelineVersion = 1,
                WorkflowSnapshot = RecruitmentWorkflowSnapshot.LegacyV1,
            };
            result = await _applicantRepository.AddOne(applicant);
            if (result.Success)
            {
                result.ResourceId = applicant.ApplicantId;
                // REC-BUG-09 Fix: Synchronize canonical RecruitmentJobApplication immediately on registration
                if (_jobApplicationRepository is not null)
                {
                    await _jobApplicationRepository.AddOne(new RecruitmentJobApplication
                    {
                        CompanyId = companyId,
                        ApplicantId = applicant.ApplicantId,
                        VacancyId = applicant.VacancyId ?? string.Empty,
                        CurrentStage = applicant.ActivityType.ToString(),
                        StageHistory =
                        [
                            new RecruitmentApplicationStageEvent
                            {
                                FromStage = "None",
                                ToStage = applicant.ActivityType.ToString(),
                                ActorUserId = applicant.ApplicantId,
                                Reason = "Initial application submitted"
                            }
                        ],
                        CreatedBy = applicant.ApplicantId,
                        UpdatedBy = applicant.ApplicantId
                    });
                }
                await LogNewApplication(applicant);
                await SendApplicationAlertToRecruitmentTeam(applicant);
                await SendEmailToApplicant(applicant);
            }
            return result;
        }

        private async Task LogNewApplication(Applicant applicant)
        {
            var vacancy = await _jobVacancyService.GetVacancyById(applicant.VacancyId, applicant.CompanyId);
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

            if (!string.IsNullOrWhiteSpace(model.IdempotencyKey) && string.Equals(entity.LastCommandIdempotencyKey, model.IdempotencyKey, StringComparison.Ordinal))
                return new Result { Success = true, StatusCode = StatusCodes.Status200OK };
            var currentVersion = entity.Version > 0 ? entity.Version : 1;
            if (model.ExpectedVersion.HasValue && model.ExpectedVersion.Value != currentVersion)
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, ErrorCode = RecruitmentErrorCodes.ConcurrencyConflict, Message = "This application was updated by another user. Refresh it and try again." };

            var previousActivityType = entity.ActivityType;
            if (RecruitmentPipelineRules.RequiresCanonicalStageCommand(previousActivityType, model.ActivityType))
            {
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, ErrorCode = RecruitmentErrorCodes.InvalidTransition, Message = "Recruitment stage changes must be completed through the recruitment lifecycle workflow." };
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
            entity.LastCommandIdempotencyKey = string.IsNullOrWhiteSpace(model.IdempotencyKey) ? entity.LastCommandIdempotencyKey : model.IdempotencyKey;
            entity.Version = currentVersion + 1;
            // Compare the persisted status while writing. A second HR user working from a
            // stale screen cannot overwrite a newer workflow stage.
            Expression<Func<Applicant, bool>> updateCondition = x =>
                x.CompanyId == companyId &&
                x.ApplicantId == model.ApplicantId &&
                x.Email == model.Email &&
                x.ActivityType == previousActivityType &&
                (x.Version == currentVersion || x.Version == 0);
            result = await _applicantRepository.Update(updateCondition, entity);
            if (!result.Success)
            {
                result.StatusCode = StatusCodes.Status409Conflict;
                result.ErrorCode = RecruitmentErrorCodes.ConcurrencyConflict;
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
                EventType = "StageChanged",
                PreviousStage = previousActivityType.ToString(),
                CurrentStage = applicant.ActivityType.ToString(),
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
            => RecruitmentPipelineRules.IsAllowedTransition(current, target, out error);
        public async Task<Result> ApplyNowService(ApplicantAddEditModel model)
        {
            Result result = new();
            if (model is null || string.IsNullOrWhiteSpace(model.VacancyId) || string.IsNullOrWhiteSpace(model.Email) ||
                !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(model.Email) ||
                model.Experience < 0 || string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Valid applicant and vacancy details are required.", ErrorCode = RecruitmentErrorCodes.InvalidRequest };
            }

            var vacancy = await _jobVacancyRepository.FirstOrDefault(v => v.JobId == model.VacancyId && !v.IsDeleted && v.Status);
            if (vacancy is null) return new Result { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "The job is no longer available.", ErrorCode = RecruitmentErrorCodes.JobNotFound };
            var company = await _companyRepository.FirstOrDefault(c => c.CompanyId == vacancy.CompanyId && c.Status && !c.IsDeleted && c.CareerPortalEnabled);
            if (company is null) return new Result { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "The job is no longer available.", ErrorCode = RecruitmentErrorCodes.JobNotFound };
            if (!string.Equals(vacancy.ApplicationMode, "Internal", StringComparison.OrdinalIgnoreCase))
                return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "This job accepts applications through an external link.", ErrorCode = RecruitmentErrorCodes.ExternalApplication };
            if (vacancy.ExpiresAt.HasValue && vacancy.ExpiresAt.Value <= DateTime.UtcNow || vacancy.ApplicationDeadline.HasValue && vacancy.ApplicationDeadline.Value <= DateTime.UtcNow)
                return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Applications for this job are closed.", ErrorCode = RecruitmentErrorCodes.JobClosed };

            model.Email = model.Email.Trim().ToLowerInvariant();
            Applicant? applicant = await _applicantRepository.FirstOrDefault(a => a.CompanyId == vacancy.CompanyId && a.VacancyId == model.VacancyId && a.Email == model.Email && !a.IsDeleted);
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
                CompanyId = vacancy.CompanyId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Experience = model.Experience,
                VacancyId = model.VacancyId,
                Phone = model.Phone,
                Email = model.Email,
                Status = EnumsHelper.ActivityStatus.Active,
                State = model.State,
                ActivityType = applicant is null ? EnumsHelper.ActivityType.New : EnumsHelper.ActivityType.ReApply,
                PipelineVersion = applicant?.PipelineVersion > 0 ? applicant.PipelineVersion : 1,
                WorkflowSnapshot = string.IsNullOrWhiteSpace(applicant?.WorkflowSnapshot) ? RecruitmentWorkflowSnapshot.LegacyV1 : applicant.WorkflowSnapshot,
            };
            result = await _applicantRepository.AddOne(user);
            if (result.Success)
            {
                result.ResourceId = user.ApplicantId;
                if (_jobApplicationRepository is not null)
                {
                    await _jobApplicationRepository.AddOne(new RecruitmentJobApplication
                    {
                        CompanyId = user.CompanyId,
                        ApplicantId = user.ApplicantId,
                        VacancyId = user.VacancyId,
                        CurrentStage = user.ActivityType.ToString(),
                        StageHistory =
                        [
                            new RecruitmentApplicationStageEvent
                            {
                                FromStage = "None",
                                ToStage = user.ActivityType.ToString(),
                                ActorUserId = user.ApplicantId,
                                Reason = "Initial application submitted via career portal"
                            }
                        ],
                        CreatedBy = user.ApplicantId,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = user.ApplicantId,
                        UpdatedDate = DateTime.UtcNow
                    });
                }
                if (_applicationTokenRepository is not null)
                {
                    string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                    DateTime tokenExpiry = DateTime.UtcNow.AddHours(2);
                    await _applicationTokenRepository.AddOne(new PublicApplicationToken { CompanyId = user.CompanyId, CreatedBy = user.ApplicantId, CreatedDate = DateTime.UtcNow, TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))), ApplicationReference = user.ApplicantId, ApplicantId = user.ApplicantId, JobId = user.VacancyId, ExpiresAt = tokenExpiry });
                    result.ActionToken = rawToken;
                }
                await LogNewApplication(user);
                await SendApplicationAlertToRecruitmentTeam(user);
                await SendEmailToApplicant(user);
            }
            return result;
        }

        public async Task<Result> UploadResume(IFormFile resume, string email)
        {
            Result result = new();
            if (resume is null || resume.Length == 0 || resume.Length > 5 * 1024 * 1024 || !string.Equals(Path.GetExtension(resume.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                return new Result { Success = false, StatusCode = CustomStatusCode.InvalidFileFormat, Message = "A PDF resume up to 5 MB is required.", ErrorCode = RecruitmentErrorCodes.InvalidResume };
            Applicant? applicant = await _applicantRepository.FirstOrDefault(a => a.Email == email);
            if (applicant is null) return result;

            // upload folder path 
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Resume");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            string fileName = $"{Guid.NewGuid():N}.pdf";
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

        public async Task<Result> UploadResumeForApplicant(string applicantId, IFormFile resume, string actorUserId)
        {
            if (string.IsNullOrWhiteSpace(applicantId)) return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Applicant ID is required.", ErrorCode = RecruitmentErrorCodes.InvalidResume };
            if (resume is null || resume.Length == 0 || resume.Length > 5 * 1024 * 1024 || !string.Equals(Path.GetExtension(resume.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                return new Result { Success = false, StatusCode = CustomStatusCode.InvalidFileFormat, Message = "A PDF resume up to 5 MB is required.", ErrorCode = RecruitmentErrorCodes.InvalidResume };
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            Applicant? applicant = await _applicantRepository.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted);
            if (applicant is null) return new Result { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "Applicant was not found in the current company." };
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Resume");
            Directory.CreateDirectory(uploadFolder);
            string fileName = $"{Guid.NewGuid():N}.pdf";
            string filePath = Path.Combine(uploadFolder, fileName);
            using (FileStream fileStream = new(filePath, FileMode.CreateNew)) await resume.CopyToAsync(fileStream);
            if (!string.IsNullOrEmpty(applicant.ResumeUrl))
            {
                string oldPath = Path.Combine(uploadFolder, Path.GetFileName(applicant.ResumeUrl));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }
            var updated = await _applicantRepository.UpdateMany(
                Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicantId),
                Builders<Applicant>.Update.Set(x => x.ResumeUrl, fileName).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, actorUserId));
            if (!updated.Success) return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Resume could not be saved." };
            return updated;
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
                : (await _jobVacancyRepository.GetAll(v => vacancyIds.Contains(v.JobId) && v.CompanyId == companyId)).ToList();
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
                                                 ResumeUrl = Common.GetApplicantResumeFullPath(applicant.ResumeUrl),
                                                 FinalInterviewScore = applicant.FinalInterviewScore,
                                                 FinalInterviewRating = applicant.FinalInterviewRating,
                                                 FinalInterviewRecommendation = applicant.FinalInterviewRecommendation,
                                                 EvaluatedRoundsCount = applicant.EvaluatedRoundsCount
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
                // Dynamic self-healing of interview benchmark if scorecards exist but were not yet persisted
                if ((applicant.FinalInterviewScore == null || applicant.FinalInterviewScore == 0) && _scorecardRepository is not null)
                {
                    try
                    {
                        var allScorecards = (await _scorecardRepository.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted)).ToList();
                        if (allScorecards.Count > 0)
                        {
                            var allInterviews = _interviewRepository is not null
                                ? (await _interviewRepository.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted)).ToList()
                                : null;
                            var computed = RecruitmentInterviewScoreCalculator.Compute(allScorecards, allInterviews);
                            if (computed.FinalInterviewScore > 0)
                            {
                                applicant.FinalInterviewScore = computed.FinalInterviewScore;
                                applicant.FinalInterviewRating = computed.FinalInterviewRating;
                                applicant.FinalInterviewRecommendation = computed.FinalInterviewRecommendation;
                                applicant.EvaluatedRoundsCount = computed.EvaluatedRoundsCount;
                                applicant.FinalScoreCalculatedAtUtc = DateTime.UtcNow;
                                await _applicantRepository.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId), applicant);
                            }
                        }
                    }
                    catch
                    {
                        // Non-blocking telemetry
                    }
                }

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
                    ResumeUrl = Common.GetApplicantResumeFullPath(applicant.ResumeUrl),
                    FinalInterviewScore = applicant.FinalInterviewScore,
                    FinalInterviewRating = applicant.FinalInterviewRating,
                    FinalInterviewRecommendation = applicant.FinalInterviewRecommendation,
                    EvaluatedRoundsCount = applicant.EvaluatedRoundsCount
                };

                if (_handoffRepository is not null)
                {
                    var handoff = await _handoffRepository.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted);
                    if (handoff is not null)
                    {
                        data.IsHired = true;
                        data.EmployeeUserId = handoff.EmployeeUserId;
                    }
                }

                if (!data.IsHired && _employeeRepository is not null && !string.IsNullOrWhiteSpace(applicant.Email))
                {
                    var emp = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.Email.ToLower() == applicant.Email.ToLower() && !x.IsDeleted);
                    if (emp is not null)
                    {
                        data.IsHired = true;
                        data.EmployeeUserId = emp.UserId;
                        data.EmployeeId = emp.EmployeeId;
                    }
                }
                else if (data.IsHired && !string.IsNullOrWhiteSpace(data.EmployeeUserId) && _employeeRepository is not null)
                {
                    var emp = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == data.EmployeeUserId && !x.IsDeleted);
                    if (emp is not null)
                    {
                        data.EmployeeId = emp.EmployeeId;
                    }
                }

                result.Success = true;
                result.StatusCode = StatusCodes.Status200OK;
                result.MethodResult = data;
            }
            else
            {
                result.Success = false;
                result.StatusCode = StatusCodes.Status404NotFound;
                result.Message = "Applicant was not found in the current company.";
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
        public async Task<Result<RecruitmentWorkflowViewModel>> GetWorkflow(string applicantId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(applicantId))
                return new Result<RecruitmentWorkflowViewModel> { Success = false, StatusCode = 400, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Company and applicant are required." };

            var applicant = await _applicantRepository.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted);
            if (applicant is null)
                return new Result<RecruitmentWorkflowViewModel> { Success = false, StatusCode = 404, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Applicant was not found in the current company." };

            var stage = applicant.ActivityType;
            var terminal = stage is EnumsHelper.ActivityType.Rejected;
            var selected = stage is EnumsHelper.ActivityType.Selected;
            var scorecard = stage is EnumsHelper.ActivityType.Shortlisted or EnumsHelper.ActivityType.Selected;
            var actions = new List<RecruitmentWorkflowActionViewModel>
            {
                Action("ScheduleInterview", !terminal, terminal ? "Rejected applications cannot be scheduled." : null),
                Action("SubmitScreening", !terminal && !selected, selected ? "Screening is complete for selected applicants." : terminal ? "Rejected applications cannot be screened." : null),
                Action("SubmitScorecard", scorecard, scorecard ? null : "Scorecards become available after shortlisting."),
                Action("CreateOffer", selected, selected ? null : "An offer requires a selected application."),
                Action("StartPreboarding", selected, selected ? null : "Preboarding requires a selected application."),
                Action("AddToTalentPool", !terminal, terminal ? "Rejected applications cannot be added to the talent pool." : null),
                Action("MergeApplicant", true, null),
            };
            var workflow = new RecruitmentWorkflowViewModel
            {
                ApplicantId = applicant.ApplicantId,
                CurrentStage = stage,
                PipelineVersion = applicant.PipelineVersion <= 0 ? 1 : applicant.PipelineVersion,
                Actions = actions,
            };
            return new Result<RecruitmentWorkflowViewModel> { Success = true, StatusCode = 200, MethodResult = workflow, Data = workflow, Message = "Recruitment workflow loaded." };

            static RecruitmentWorkflowActionViewModel Action(string code, bool enabled, string? reason) => new() { Code = code, Enabled = enabled, DisabledReason = reason };
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
                            EventType = log.EventType,
                            PreviousStage = log.PreviousStage,
                            CurrentStage = log.CurrentStage,
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
            var vacancy = await _jobVacancyService.GetVacancyById(applicant.VacancyId, applicant.CompanyId);
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
            JobVacancy? vacancy = await _jobVacancyRepository.FirstOrDefault(x => x.JobId == applicant.VacancyId && x.CompanyId == applicant.CompanyId);
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









