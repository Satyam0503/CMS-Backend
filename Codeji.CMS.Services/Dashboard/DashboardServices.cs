using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Dashboard
{
    public class DashboardServices : IDashboardService
    {

        private readonly IMongoDbRepository<Department> _departmentRepository;
        private readonly IMongoDbRepository<EmpUser> _empUserRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        readonly IMongoDbRepository<Applicant> _applicantRepository;
        public DashboardServices(
            IMongoDbRepository<Department> departmentRepository,
            IMongoDbRepository<EmpUser> empUserRepository,
            IHttpContextAccessor httpContextAccessor,
            IMongoDbRepository<Applicant> applicantRepository,
        IMapper mapper)

        {
            _departmentRepository = departmentRepository;
            _empUserRepository = empUserRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _applicantRepository = applicantRepository;
        }

        public async Task<List<AllDepartmentDetailsResponseModel>> GetAllDepartmentsDetails(string companyId)
        {

            string acceptLanguage = CurrentContext.GetLanguage(_httpContextAccessor);

            var allDepartments = await _departmentRepository.GetAll(x => x.CompanyId == companyId && x.IsDeleted == false);
            var allEmpUser = await _empUserRepository.GetAll(x => x.CompanyId == companyId);

            var result = from ad in allDepartments
                         join aeu in allEmpUser on ad.DepartmentId equals aeu.Department into empGroup
                         select new AllDepartmentDetailsResponseModel
                         {
                             Label = ad.Titles.FirstOrDefault(x => x.Language == acceptLanguage)?.Label,
                             DepartmentId = ad.DepartmentId,
                             EmployeeCount = empGroup.Count(),
                         };

            return result.ToList();

        }

        public async Task<List<GenderDetailsResponseModel>> GetAllGenderDetails(string companyId)
        {
            var allEmpUser = await _empUserRepository.GetAll(x => x.CompanyId == companyId);
            var result = (from aeu in allEmpUser
                          group aeu by aeu.Gender into genderGroup
                          select new GenderDetailsResponseModel
                          {
                              Gender = genderGroup.Key,
                              Count = genderGroup.Count()
                          }).ToList();

            return result;

        }
        public async Task<Result<ApplicationDataResponseDto>> GetApplicationStatusData(string? vacancyId)
        {
            Result<ApplicationDataResponseDto> result = new();
            List<Applicant> applicantList = [];
            if (vacancyId != null)
            {
                applicantList = (await _applicantRepository.GetAll(ap => ap.VacancyId == vacancyId)).ToList();
            }
            applicantList = (await _applicantRepository.GetAll()).ToList();
            if (applicantList.Count == 0) return result;
            var groupedApplicantData = applicantList.GroupBy(ap => ap.ActivityType).Select(apg => new ApplicationStatusTypeData()
            {
                ActivityType = apg.Key,
                Count = apg.Count()
            }).ToList();
            result.MethodResult = new ApplicationDataResponseDto()
            {
                TotalApplications = applicantList.Count,
                ApplicationStatusData = groupedApplicantData
            };
            return result;
        }
    }
}
