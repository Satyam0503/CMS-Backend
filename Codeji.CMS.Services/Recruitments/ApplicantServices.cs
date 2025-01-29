using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;

namespace Codeji.CMS.Services.Recruitments
{
    public class ApplicantServices : IApplicantsService
    {
        readonly IMongoDbRepository<Applicant> _applicantRepository;
        readonly IMapper _mapper;
        public ApplicantServices(IMongoDbRepository<Applicant> applicantDbRepository, IMapper mapper)
        {
            _applicantRepository = applicantDbRepository;
            _mapper = mapper;

        }
        /// <summary>
        /// For Annonymous add and update applicants
        /// </summary>
        /// <param name="applicantRegisterModel"></param>
        /// <returns></returns>
        public Task<Result> RegisterApplicants(ApplicantRegisterModel applicantRegisterModel, string companyId)
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
                CreatedBy = "new"

            };
            Task<Result> result = _applicantRepository.AddOne(applicant);
            return result;
        }

        public async Task<Result> UpdateApplicants(ApplicantRegisterModel model, string companyId)
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
            entity.VacancyName = model.VacancyName;
            entity.VacancyId = model.VacancyId;
            Result res = await _applicantRepository.Update(whereCondition, entity);
            return res;

        }
        public async Task<string> GetApplicantsExistingId(string email, string companyId)
        {
            Applicant? res = await _applicantRepository.FirstOrDefault(x => (string.IsNullOrEmpty(companyId) || x.CompanyId == companyId) && x.Email == email);
            return res?.ApplicantId ?? string.Empty;
        }

        public async Task<List<ApplicantViewModel>> GetApplicantsList(string companyId)
        {
            IEnumerable<Applicant> list = await _applicantRepository.GetAll(x => x.CompanyId == companyId);
            return _mapper.Map<List<ApplicantViewModel>>(list);
        }

        //Get Applicant List Using Filter Change this logic in Future
        //public async Task<List<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters)
        //{
        //    Expression<Func<Applicant, bool>> whereCondition = x =>
        //    (!filters.FilterFrom.HasValue || (x.CreatedDate.HasValue && x.CreatedDate > filters.FilterFrom && x.CreatedDate < filters.FilterTo))
        //    && (!filters.ActivityTypes.Any() || filters.ActivityTypes.Contains(x.ActivityType))
        //    && (!filters.Status.Any() || filters.Status.Contains(x.Status))
        //    && (!filters.VacancyIds.Any() || filters.VacancyIds.Contains(x.VacanyId))
        //    && (string.IsNullOrEmpty(filters.Name) || x.FirstName.Contains(filters.Name));

        //    ProjectionDefinition<Applicant, Applicant> projection = Builders<Applicant>.Projection.Expression(app => new Applicant
        //    {
        //        ApplicantId = app.ApplicantId,
        //        Email = app.Email,
        //        Experience = app.Experience,
        //        FirstName = app.FirstName,
        //        LastName = app.LastName,
        //        Phone = app.Phone,
        //        VacanyId = app.VacanyId,
        //        CreatedDate = app.CreatedDate,
        //        UpdatedDate = app.UpdatedDate,
        //        Status = app.Status,
        //        ActivityType = app.ActivityType

        //    });

        //    IEnumerable<Applicant> applicants = await _applicantRepository.GetAggregateDataAsync(whereCondition, projection);
        //    List<ApplicantViewModel> list = (from ap in applicants
        //                                     join s in StaticData.StatusList
        //                                     on ap.Status equals s.Value
        //                                     join ac in StaticData.ActivityTypeList
        //                                     on ap.ActivityType equals ac.Value into acType
        //                                     from act in acType.DefaultIfEmpty(new EnumsBindList())

        //                                     select new ApplicantViewModel
        //                                     {
        //                                         ApplicantId = ap.ApplicantId,
        //                                         ActivityTypeName = act.Name,
        //                                         ApplyDate = ap.CreatedDate,
        //                                         Email = ap.Email,
        //                                         Exprience = ap.Experience,
        //                                         FirstName = ap.FirstName,
        //                                         LastName = ap.LastName,
        //                                         Phone = ap.Phone,
        //                                         StatusName = s.Name,
        //                                         UpdateDate = ap.UpdatedDate,
        //                                         VacanyName = "to ddo"

        //                                     }).ToList();

        //    return list;
        //}


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
    }
}

