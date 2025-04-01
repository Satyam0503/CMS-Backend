using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Helpers;
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
        public ApplicantServices(IMongoDbRepository<Applicant> applicantDbRepository,
            IMapper mapper,
            IJobVacancy jobVacancyService,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository,
            IMongoDbRepository<JobVacancy> jobVacancyRepository,
            IMongoDbRepository<ApplicantLogs> applicantLogsRepository,
            IMongoDbRepository<EmpUser> employeeRepository
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
        }
        /// <summary>
        /// For Annonymous add and update applicants
        /// </summary>
        /// <param name="applicantRegisterModel"></param>
        /// <returns></returns>
        public async Task<Result> RegisterApplicants(ApplicantAddEditModel applicantRegisterModel)
        {
            Applicant applicant = new Applicant()
            {
                FirstName = applicantRegisterModel.FirstName,
                LastName = applicantRegisterModel.LastName,
                Experience = applicantRegisterModel.Experience,
                VacancyId = applicantRegisterModel.VacancyId,
                //VacancyName = applicantRegisterModel.VacancyName,
                Phone = applicantRegisterModel.Phone,
                Email = applicantRegisterModel.Email,
                Status = applicantRegisterModel.Status,
                State = applicantRegisterModel.State,
                CreatedBy = "new"

            };
            Result result = await _applicantRepository.AddOne(applicant);

            //Acknowledgement Email Logic 

            MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == 4);
            HtmlTemplate htmlTemplate = new HtmlTemplate();
            string replacedBody = htmlTemplate.Render(emailContent?.body ?? string.Empty, new
            {
                CandidateName = applicantRegisterModel.FirstName + " " + applicantRegisterModel.LastName,
                //JobTitle = applicantRegisterModel.VacancyName

            });
            await EmailFunctionality.SendEmailFromAPI(applicantRegisterModel.Email, emailContent.subject, replacedBody);
            return result;
        }

        public async Task<Result> UpdateApplicants(ApplicantAddEditModel model)
        {
            Expression<Func<Applicant, bool>> whereCondition = x => x.ApplicantId == model.ApplicantId && x.Email == model.Email;
            //to do improvement
            Applicant? entity = await _applicantRepository.FirstOrDefault(whereCondition);
            if (entity == null)
            {
                return new Result()
                {
                    Success = false,
                    Message = "Applicant Not Found"
                };
            }
            entity.UpdatedBy = "";
            entity.UpdatedDate = DateTime.Now;
            entity.Experience = model.Experience;
            entity.VacancyId = model.VacancyId;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.Phone = model.Phone;
            //entity.VacancyName = model.VacancyName;
            entity.VacancyId = model.VacancyId;
            entity.ActivityType = model.ActivityType;
            entity.Status = model.Status;
            entity.State = model.State;
            Result res = await _applicantRepository.Update(whereCondition, entity);
            return res;

        }

        public async Task<bool> IsEmailExist(string email)
        {
            bool res = await _applicantRepository.Exist(x => x.Email == email);
            return res;
        }
        public async Task<Result> GetApplicantsExistingId(string email)
        {
            Applicant? res = await _applicantRepository.FirstOrDefault(x => x.Email == email);
            if (res is null)
            {
                return new Result()
                {
                    Success = true,
                };
            }
            else if (res.CreatedDate > DateTime.Now.AddMonths(-6))
            {
                return new Result()
                {
                    Success = false
                };
            }
            else
            {
                return new Result()
                {
                    Success = true,
                    Message = res.ApplicantId
                };
            }

        }

        public async Task<string> GetApplicantExistingResume(string email)
        {
            Applicant? res = await _applicantRepository.FirstOrDefault(x => x.Email == email);
            return res?.ResumeUrl ?? string.Empty;
        }


        //Get Applicant List Using Filter Change this logic in Future
        public async Task<Result<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters, int pageNo, int records)
        {
            pageNo = pageNo == 0 ? 1 : pageNo;
            records = records == 0 ? 10 : records;
            Expression<Func<Applicant, bool>> whereCondition = x =>
            (!filters.FilterFrom.HasValue || (x.CreatedDate.HasValue && x.CreatedDate >= filters.FilterFrom && x.CreatedDate <= filters.FilterTo))
            && (!filters.ActivityTypes.Any() || filters.ActivityTypes.Contains(x.ActivityType))
            && (!filters.Status.Any() || filters.Status.Contains(x.Status))
            && (!filters.VacancyIds.Any() || filters.VacancyIds.Contains(x.VacancyId))
            && (!filters.MinExperience.HasValue || (x.Experience >= filters.MinExperience && x.Experience <= filters.MaxExperience))
            && (string.IsNullOrEmpty(filters.Name)
            || x.FirstName.Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase)
            || x.LastName.Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase)
            || (x.FirstName + " " + x.LastName).Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase));

            var count = _applicantRepository.Count(whereCondition);
            var applicants = await _applicantRepository.GetAggregateDataAsync<Applicant>(whereCondition, pageNo: pageNo, pageSize: records);
            List<JobVacancy> vacancies = (await _jobVacancyRepository.GetAll()).ToList();
            List<ApplicantViewModel> data = (from applicant in applicants
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
                                                 UpdateDate = applicant.UpdatedDate,
                                                 ResumeUrl = applicant.ResumeUrl,
                                             }).ToList();
            return new Result<ApplicantViewModel>
            {
                Success = true,
                TotalRecords = await count,
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
                    UpdateDate = applicant.UpdatedDate,
                    ResumeUrl = applicant.ResumeUrl,

                };
                result.Success = true;
                result.MethodResult = data;
            }
            return result;
        }

        public async Task<Result> AddAppicantResume(string fileName, string email, string filePath)
        {
            Expression<Func<Applicant, bool>> whereCondition = x => x.Email == email;
            Applicant? resume = await _applicantRepository.FirstOrDefault(whereCondition);
            if (resume == null)
            {
                return new Result()
                {
                    Success = false,
                    Message = "Applicant Not Found"
                };
            }
            whereCondition = x => x.ApplicantId == resume.ApplicantId;
            resume.ResumeUrl = fileName;
            return await _applicantRepository.Update(whereCondition, resume);
        }
        //Logic For Addig Comment on Applicant By Employee (Admin And HR Manager )

        public async Task<Result> AddComment(string userId, CommentRequestModel model)
        {
            var applicant = await _applicantRepository.FirstOrDefault(x=>x.ApplicantId == model.ApplicantId);
            ApplicantLogs comments = new()
            {
                UserId = userId,
                ApplicantId = model.ApplicantId,
                ActivityCategory = model.ActivityCategory,
                Description = model.Description,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId,
                JobRole = model.JobTitle,
                ApplicantName = applicant == null ? "" :$"{applicant.FirstName} {applicant.LastName}" 
            };
            await _ApplicantLogsRepository.AddOne(comments);
            Result result = new()
            {
                Success = true,
                Message = "Comment Added Successfully",
                StatusCode = StatusCodes.Status200OK,
            };
            return result;
        }
        public async Task<List<ApplicantLogResponseModel>> GetAllComment(string applicantId)
        {
            var logList = (await _ApplicantLogsRepository.GetAll(x => x.ApplicantId == applicantId)).ToList();
            string [] userIds = logList.Select(x=>x.UserId).Distinct().ToArray();
            var users = (await _employeeRepository.GetAll(x=> userIds.Contains(x.UserId))).ToList();
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
                        }).ToList();
            return data;
        }
        public async Task<Result<ApplicantLogResponseModel>> GetProcessLogData(ApplicantLogFilterModel filters)
        {
            Expression<Func<ApplicantLogs, bool>> whereCondition = x => 
            (!filters.FilterFrom.HasValue  || (x.CreatedDate >= filters.FilterFrom))
            && (!filters.FilterTo.HasValue || (x.CreatedDate <= filters.FilterTo))
            && (filters.ActivityCategory.Length == 0 || filters.ActivityCategory.Contains(x.ActivityCategory))
            && (string.IsNullOrEmpty(filters.JobRole) || x.JobRole.Contains(filters.JobRole,StringComparison.CurrentCultureIgnoreCase) )
            && (string.IsNullOrEmpty(filters.ApplicantName) || x.ApplicantName.Contains(filters.ApplicantName,StringComparison.CurrentCultureIgnoreCase));

            var logCount =await _ApplicantLogsRepository.Count(whereCondition);
            var logList = (await _ApplicantLogsRepository.GetAggregateDataAsync<ApplicantLogs>(whereCondition, pageNo:filters.PageNo,pageSize:filters.PageSize)).ToList();
            string [] empId=logList.Select(x=> x.UserId).Distinct().ToArray();
            var users = await _employeeRepository.GetAll(x=> empId.Contains(x.UserId));
            var data = ( from log in logList join user in users on log.UserId equals user.UserId
            select new ApplicantLogResponseModel {
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
            }).ToList();

            Result<ApplicantLogResponseModel> result = new()
            {
                Success = true,
                MethodResults = data,
                TotalRecords= logCount,
            };
            return result;
        }
    }
}

