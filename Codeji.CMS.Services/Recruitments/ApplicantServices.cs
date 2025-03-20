using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
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
        readonly IMongoDbRepository<JobVacancy> _jobVacancyRepository;
        public ApplicantServices(IMongoDbRepository<Applicant> applicantDbRepository,
            IMapper mapper,
            IMongoDbRepository<Resume> resumeRepository,
            IJobVacancy jobVacancyService,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository,
            IMongoDbRepository<JobVacancy> jobVacancyRepository)
        {
            _applicantRepository = applicantDbRepository;
            _resumeRepository = resumeRepository;
            _mapper = mapper;
            _jobVacancyService = jobVacancyService;
            _companyRepository = companyRepository;
            _mailTemplateRepository = mailTemplateRepository;
            _jobVacancyRepository = jobVacancyRepository;
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
            string replacedBody = htmlTemplate.Render(emailContent.body, new
            {
                CandidateName = applicantRegisterModel.FirstName + " " + applicantRegisterModel.LastName,
                JobTitle = applicantRegisterModel.VacancyName

            });
            await EmailFunctionality.SendEmailFromAPI(applicantRegisterModel.Email, emailContent.subject, replacedBody);
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
        public async Task<Result<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters, string companyId, int pageNo, int records)
        {
            Expression<Func<Applicant, bool>> whereCondition = x => x.CompanyId == companyId
            && (!filters.FilterFrom.HasValue || (x.CreatedDate.HasValue && x.CreatedDate >= filters.FilterFrom && x.CreatedDate <= filters.FilterTo))
            && (!filters.ActivityTypes.Any() || filters.ActivityTypes.Contains(x.ActivityType))
            && (!filters.Status.Any() || filters.Status.Contains(x.Status))
            && (!filters.VacancyIds.Any() || filters.VacancyIds.Contains(x.VacancyId))
            && (!filters.MinExperience.HasValue || (x.Experience >= filters.MinExperience && x.Experience <= filters.MaxExperience))
            && (string.IsNullOrEmpty(filters.Name)
            || x.FirstName.Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase)
            || x.LastName.Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase)
            || (x.FirstName + " " + x.LastName).Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase));

            List<Applicant> applicants = (await _applicantRepository.GetAll(whereCondition)).ToList();
            List<JobVacancy> vacancies = (await _jobVacancyRepository.GetAll(x => x.CompanyId == companyId)).ToList();

            List<ApplicantViewModel> applicantData = _mapper.Map<List<ApplicantViewModel>>(applicants);
            List<ApplicantViewModel> data = (from applicant in applicantData
                                             join vacancy in vacancies on applicant.VacancyId equals vacancy.JobId
                                             select new ApplicantViewModel
                                             {
                                                 ApplicantId = applicant.ApplicantId,
                                                 FirstName = applicant.FirstName,
                                                 LastName = applicant.LastName,
                                                 Email = applicant.Email,
                                                 Phone = applicant.Phone,
                                                 StatusName = applicant.StatusName,
                                                 Status = applicant.Status,
                                                 ActivityTypeName = applicant.ActivityTypeName,
                                                 VacancyName = vacancy.Title,
                                                 VacancyId = vacancy.JobId,
                                                 State = applicant.State,
                                                 Experience = applicant.Experience,
                                                 ApplyDate = applicant.ApplyDate,
                                                 UpdateDate = applicant.UpdateDate,
                                                 ResumeUrl = applicant.ResumeUrl,

                                             }).ToList();
            List<ApplicantViewModel> pagedList = data;
            if (pageNo != 0 && records != 0)
            {
                pagedList = data.Skip((pageNo - 1) * records).Take(records).ToList();
            }

            Result<ApplicantViewModel> result = new Result<ApplicantViewModel>
            {
                Success = true,
                TotalRecords = applicants.Count,
                MethodResults = pagedList
            };
            return result;


        }

        public async Task<Result<ApplicantViewModel>> ApplicantById(string applicantId, string companyId)
        {
            Result<ApplicantViewModel> result = new Result<ApplicantViewModel>();
            IEnumerable<Applicant>? entity = await _applicantRepository.GetAll(x => x.ApplicantId == applicantId);
            List<JobVacancy> vacancies = (await _jobVacancyRepository.GetAll(x => x.CompanyId == companyId)).ToList();
            IEnumerable<ApplicantViewModel> data = from e in entity
                                                   join vacancy in vacancies on e.CompanyId equals vacancy.CompanyId
                                                   where e.VacancyId == vacancy.JobId
                                                   select new ApplicantViewModel
                                                   {
                                                       ApplicantId = e.ApplicantId,
                                                       FirstName = e.FirstName,
                                                       LastName = e.LastName,
                                                       Email = e.Email,
                                                       Phone = e.Phone,
                                                       Status = e.Status,
                                                       ActivityType = e.ActivityType,
                                                       VacancyName = vacancy.Title,
                                                       VacancyId = vacancy.JobId,
                                                       State = e.State,
                                                       Experience = e.Experience,
                                                       ApplyDate = e.CreatedDate,
                                                       UpdateDate = e.UpdatedDate,
                                                       ResumeUrl = e.ResumeUrl,

                                                   };
            if (entity is not null)
            {
                result.Success = true;
                result.MethodResult = data.FirstOrDefault();
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

