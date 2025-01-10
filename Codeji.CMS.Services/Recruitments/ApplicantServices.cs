using System;
using System.ComponentModel.Design;
using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments
{
    public class ApplicantServices : IApplicantsService
    {
        IMongoDbRepository<Applicant> _applicantRepository;
        public ApplicantServices(IMongoDbRepository<Applicant> applicantDbRepository)
        {
            _applicantRepository = applicantDbRepository;
        }
        /// <summary>
        /// For Annonymous add and update applicants
        /// </summary>
        /// <param name="applicantRegisterModel"></param>
        /// <returns></returns>
        public Task<Result> RegisterApplicants(ApplicantAddEditModel applicantRegisterModel)
        {
            Applicant applicant = new Applicant()
            {
                CompanyId = applicantRegisterModel.CompnyId,
                FirstName = applicantRegisterModel.FirstName,
                LastName = applicantRegisterModel.LastName,
                Exprience = applicantRegisterModel.Exprience,
                VacanyId = applicantRegisterModel.VacanyId,
                Phone = applicantRegisterModel.Phone,
                Email = applicantRegisterModel.Email,
                CreatedBy = "new"

            };
            var result = _applicantRepository.AddOne(applicant);
            return result;
        }

        public async Task<Result> UpdateApplicants(ApplicantAddEditModel model)
        {
            //to do improvement
            Applicant entity = await _applicantRepository.FirstOrDefault(x => model.ApplicantId == x.ApplicantId);
            entity.UpdatedBy = "";
            entity.UpdatedDate = DateTime.Now;
            entity.Exprience = model.Exprience;
            entity.VacanyId = model.VacanyId;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            Expression<Func<Applicant, bool>> whereCondition = x => model.ApplicantId == x.ApplicantId;
            var res = await _applicantRepository.Update(whereCondition, entity);
            return res;

        }
        public async Task<string> GetApplicantsEXistingId(string email, string comapnyId = "")
        {
            var res = await _applicantRepository.FirstOrDefault(x => (string.IsNullOrEmpty(comapnyId) || x.CompanyId == comapnyId) && x.Email == email);
            return res?.ApplicantId ?? string.Empty;
        }

        public async Task<List<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters)
        {
            Expression<Func<Applicant, bool>> whereCondition = x =>
            (!filters.FilterFrom.HasValue || (x.CreatedDate.HasValue && x.CreatedDate > filters.FilterFrom && x.CreatedDate < filters.FilterTo))
            && (!filters.ActivityTypes.Any() || filters.ActivityTypes.Contains(x.ActivityType))
            && (!filters.Status.Any() || filters.Status.Contains(x.Status))
            && (!filters.VacancyIds.Any() || filters.VacancyIds.Contains(x.VacanyId))
            && (string.IsNullOrEmpty(filters.Name) || x.FirstName.Contains(filters.Name));

            ProjectionDefinition<Applicant, Applicant> projection = Builders<Applicant>.Projection.Expression(app => new Applicant
            {
                ApplicantId = app.ApplicantId,
                Email = app.Email,
                Exprience = app.Exprience,
                FirstName = app.FirstName,
                LastName = app.LastName,
                Phone = app.Phone,
                VacanyId = app.VacanyId,
                CreatedDate = app.CreatedDate,
                UpdatedDate = app.UpdatedDate,
                Status = app.Status,
                ActivityType = app.ActivityType

            });

            var applicants = await _applicantRepository.GetAggregateDataAsync(whereCondition, projection);
            var list = (from ap in applicants
                        join s in StaticData.StatusList
                        on ap.Status equals s.Value
                        join ac in StaticData.ActivityTypeList
                        on ap.ActivityType equals ac.Value into acType
                        from act in acType.DefaultIfEmpty(new EnumsBindList())

                        select new ApplicantViewModel
                        {
                            ApplicantId = ap.ApplicantId,
                            ActivityTypeName = act.Name,
                            ApplyDate = ap.CreatedDate,
                            Email = ap.Email,
                            Exprience = ap.Exprience,
                            FirstName = ap.FirstName,
                            LastName = ap.LastName,
                            Phone = ap.Phone,
                            StatusName = s.Name,
                            UpdateDate = ap.UpdatedDate,
                            VacanyName = "to ddo"

                        }).ToList();

            return list;
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
                    VacanyId = entity.VacanyId,
                    Exprience = entity.Exprience,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    UpdateDate = entity.UpdatedDate,
                    ActivityType = entity.ActivityType,
                    ResumeUrl = entity.ResumeUrl
                };
            }
            return result;
        }
    }
}

