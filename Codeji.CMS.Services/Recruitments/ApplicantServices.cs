using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Helpers;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments
{
    public class ApplicantServices : IApplicantsService
    {
        readonly IMongoDbRepository<Applicant> _applicantRepository;
        readonly IMongoDbRepository<Resume> _resumeRepository;
        readonly IMapper _mapper;
        private readonly IJobVacancy _jobVacancyService;
        readonly IMongoDbRepository<Company> _companyRepository;
        readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
        public ApplicantServices(IMongoDbRepository<Applicant> applicantDbRepository,
            IMapper mapper,
            IMongoDbRepository<Resume> resumeRepository,
            IJobVacancy jobVacancyService,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository)
        {
            _applicantRepository = applicantDbRepository;
            _resumeRepository = resumeRepository;
            _mapper = mapper;
            _jobVacancyService = jobVacancyService;
            _companyRepository = companyRepository;
            _mailTemplateRepository = mailTemplateRepository;
        }
        /// <summary>
        /// For Annonymous add and update applicants
        /// </summary>
        /// <param name="applicantRegisterModel"></param>
        /// <returns></returns>
        public async Task<Result> RegisterApplicants(ApplicantAddEditModel applicantRegisterModel, string companyId)
        {
            Applicant applicant = new Applicant()
            {
                CompanyId = companyId,
                FirstName = applicantRegisterModel.FirstName,
                LastName = applicantRegisterModel.LastName,
                Experience = applicantRegisterModel.Experience,
                VacancyId = applicantRegisterModel.VacancyId,
                VacancyName = applicantRegisterModel.VacancyName,
                Phone = applicantRegisterModel.Phone,
                Email = applicantRegisterModel.Email,
                Status = applicantRegisterModel.Status,
                CreatedBy = "new"

            };
            Result result = await _applicantRepository.AddOne(applicant);

            //Acknowledgement Email Logic 

            List<JobVacancyModel> vacancies = await _jobVacancyService.GetAllVacancy(applicant.CompanyId);
            List<JobVacancyModel> vacancyName = vacancies.FindAll(x => x.JobId == applicant.VacancyId);
            string jobTitle = vacancyName[0].Title;
            Company? companyName = await _companyRepository.FirstOrDefault(x => x.CompanyId == applicant.CompanyId);
            MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == 4);
            await EmailFunctionality.SendEmailFromAPI(applicant.Email, emailContent.subject, emailContent.body);
            return result;
        }

        public async Task<Result> UpdateApplicants(ApplicantAddEditModel model, string companyId)
        {
            Expression<Func<Applicant, bool>> whereCondition = x => x.ApplicantId == model.ApplicantId && x.CompanyId == companyId && x.Email == model.Email;
            //to do improvement
            Applicant entity = await _applicantRepository.FirstOrDefault(whereCondition);
            entity.UpdatedBy = "";
            entity.UpdatedDate = DateTime.Now;
            entity.Experience = model.Experience;
            entity.VacancyId = model.VacancyId;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.Phone = model.Phone;
            entity.VacancyName = model.VacancyName;
            entity.VacancyId = model.VacancyId;
            entity.ActivityType = model.ActivityType;
            entity.Status = model.Status;
            Result res = await _applicantRepository.Update(whereCondition, entity);
            return res;

        }

        public async Task<bool> IsEmailExist(string email)
        {
            Applicant? res = await _applicantRepository.FirstOrDefault(x => x.Email == email);
            if (string.IsNullOrEmpty(res?.Email))
            {
                return false;
            }
            return true;
        }
        public async Task<string> GetApplicantsExistingId(string email, string companyId)
        {
            Applicant? res = await _applicantRepository.FirstOrDefault(x => (string.IsNullOrEmpty(companyId) || x.CompanyId == companyId) && x.Email == email);
            if (string.IsNullOrEmpty(res?.Email))
            {
                return string.Empty;
            }
            else
            {
                DateTime createdDate = (DateTime)res.CreatedDate;
                long createdTimeStamp = new DateTimeOffset(createdDate).ToUnixTimeSeconds();
                DateTimeOffset sixMonthAgo = DateTimeOffset.Now.AddMonths(-6);
                if (createdDate <= sixMonthAgo)

                {
                    return string.Empty;
                }
                return res?.ApplicantId;

            }

        }

        public async Task<string> GetApplicantExistingResume(string email)
        {
            Applicant? res = await _applicantRepository.FirstOrDefault(x => x.Email == email);





            return res?.ResumeUrl ?? string.Empty;
        }

        //public async Task<List<ApplicantViewModel>> GetApplicantsList(string companyId)
        //{
        //    IEnumerable<Applicant> list = await _applicantRepository.GetAll(x => x.CompanyId == companyId);
        //    return _mapper.Map<List<ApplicantViewModel>>(list);
        //}

        //Get Applicant List Using Filter Change this logic in Future
        public async Task<List<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters, string companyId)
        {
            Expression<Func<Applicant, bool>> whereCondition = x => x.CompanyId == companyId
            && (!filters.FilterFrom.HasValue || (x.CreatedDate.HasValue && x.CreatedDate > filters.FilterFrom && x.CreatedDate < filters.FilterTo))
            && (!filters.ActivityTypes.Any() || filters.ActivityTypes.Contains(x.ActivityType))
            && (!filters.Status.Any() || filters.Status.Contains(x.Status))
            && (!filters.VacancyIds.Any() || filters.VacancyIds.Contains(x.VacancyId))
            && (string.IsNullOrEmpty(filters.Name) || x.FirstName.Contains(filters.Name))
            && ((!filters.MinExperience.HasValue && filters.MaxExperience.HasValue) || (x.Experience >= filters.MinExperience))
            && ((!filters.MaxExperience.HasValue && filters.MinExperience.HasValue) || (x.Experience <= filters.MaxExperience));
            List<Applicant> applicants = (await _applicantRepository.GetAll(whereCondition)).ToList();
            return _mapper.Map<List<ApplicantViewModel>>(applicants);

        }

        public async Task<Result<ApplicantViewModel>> ApplicantById(string applicantId)
        {
            Result<ApplicantViewModel> result = new Result<ApplicantViewModel>();
            Applicant entity = await _applicantRepository.FirstOrDefault(x => x.ApplicantId == applicantId);
            if (entity is not null)
            {
                result.Success = true;
                result.MethodResult = new ApplicantViewModel()
                {
                    ApplicantId = entity.ApplicantId,
                    ApplyDate = entity.CreatedDate,
                    Email = entity.Email,
                    Phone = entity.Phone,
                    Status = entity.Status,
                    VacancyId = entity.VacancyId,
                    VacancyName = entity.VacancyName,
                    Experience = entity.Experience,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    UpdateDate = entity.UpdatedDate,
                    ActivityType = entity.ActivityType,
                    ResumeUrl = entity.ResumeUrl,

                };
            }
            return result;
        }

        public async Task<Result> AddAppicantResume(string fileName, string email, string filePath)
        {
            Expression<Func<Applicant, bool>> whereCondition = x => x.Email == email;
            Applicant resume = await _applicantRepository.FirstOrDefault(whereCondition);
            if (resume == null)
            {
                return new Result()
                {
                    Success = false,
                    Message = "Applicant Not Found"
                };
            }
            resume.ResumeUrl = fileName;

            Result res = await _applicantRepository.Update(whereCondition, resume);
            return res;
        }
    }
}

