using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Holidays;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
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
        readonly IMongoDbRepository<Holidays> _holidayRepository;
        public DashboardServices(
            IMongoDbRepository<Department> departmentRepository,
            IMongoDbRepository<EmpUser> empUserRepository,
            IHttpContextAccessor httpContextAccessor,
            IMongoDbRepository<Applicant> applicantRepository,
            IMongoDbRepository<Holidays> holidayRepository,
        IMapper mapper)

        {
            _departmentRepository = departmentRepository;
            _empUserRepository = empUserRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _applicantRepository = applicantRepository;
            _holidayRepository = holidayRepository;
        }

        public async Task<List<DepartmentEmpResponseDto>> GetAllDepartmentsDetails(string companyId)
        {
            var allDepartments = await _departmentRepository.GetAll(x => x.CompanyId == companyId && x.IsDeleted == false);
            var allEmpUser = await _empUserRepository.GetAll(x => x.CompanyId == companyId);

            var result = from ad in allDepartments
                         join aeu in allEmpUser on ad.DepartmentId equals aeu.Department into empGroup
                         select new DepartmentEmpResponseDto
                         {
                             Label = ad.Titles.ToDictionary(keySelector: d => d.Language, elementSelector: d => d.Label),
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
            else
            {
                applicantList = (await _applicantRepository.GetAll()).ToList();
            }
            if (applicantList.Count == 0) return result;
            var groupedApplicantData = applicantList.GroupBy(ap => ap.ActivityType).Select(apg => new ApplicationStatusTypeData()
            {
                ActivityType = apg.Key,
                Count = apg.Count()
            }).OrderBy(ap => ap.ActivityType).ToList();
            result.MethodResult = new ApplicationDataResponseDto()
            {
                TotalApplications = applicantList.Count,
                ApplicationStatusData = groupedApplicantData
            };
            return result;
        }

        public async Task<Result<UpComingHolidayResponseDto>> GetUpComingHolidays()
        {
            Result<UpComingHolidayResponseDto> result = new();
            Expression<Func<Holidays, bool>> whereCondition = h => h.Date.Date >= DateTime.UtcNow.Date;
            var data = (await _holidayRepository.GetAggregateDataAsync<Holidays>(whereCondition, isAscending: true, orderedKey: "Date", pageSize: 10)).ToList();
            if (!data.Any()) return result;
            result.MethodResults = data.Select(x =>
                new UpComingHolidayResponseDto()
                {
                    HolidayId = x.HolidayId,
                    HolidayName = x.HolidayName,
                    Date = x.Date,
                    HolidayCoverImageUrl = string.IsNullOrEmpty(x.HolidayImageUrl) ? null : Common.GetHolidayCoverImagePath(x.HolidayImageUrl)
                }
            ).ToList();
            return result;
        }
    }
}

