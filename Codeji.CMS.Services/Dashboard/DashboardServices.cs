using System.Globalization;
using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Holidays;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.middlewares;
using LinqKit;
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
        readonly IMongoDbRepository<CalendarEntity> _calendarRepository;
        readonly IMongoDbRepository<JobTitles> _jobTitleRepository;
        public DashboardServices(
            IMongoDbRepository<Department> departmentRepository,
            IMongoDbRepository<EmpUser> empUserRepository,
            IHttpContextAccessor httpContextAccessor,
            IMongoDbRepository<Applicant> applicantRepository,
            IMongoDbRepository<CalendarEntity> calendarRepository,
            IMongoDbRepository<JobTitles> jobRepository,
        IMapper mapper)

        {
            _departmentRepository = departmentRepository;
            _empUserRepository = empUserRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _applicantRepository = applicantRepository;
            _calendarRepository = calendarRepository;
            _jobTitleRepository = jobRepository;
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

        public async Task<Result<UpComingHolidayEventResponseDto>> GetUpComingHolidayAndEvents()
        {
            Result<UpComingHolidayEventResponseDto> result = new();
            var recurringItems = await _calendarRepository.GetAll(ci => ci.Recurring);
            var upcomingEventOrHolidays = recurringItems.Select(ci =>
            {
                var date = ci.Date;
                var adjustedDate = new DateTime(DateTime.UtcNow.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
                if (adjustedDate < DateTime.UtcNow.Date)
                {
                    adjustedDate = adjustedDate.AddYears(1);
                }
                return new CalendarEntity()
                {
                    Id = ci.Id,
                    Name = ci.Name,
                    Date = adjustedDate,
                    Recurring = ci.Recurring,
                    Description = ci.Description,
                    Type = ci.Type,
                    ImageUrl = ci.ImageUrl
                };
            }).Where(ci => ci.Date > DateTime.UtcNow).OrderBy(ci => ci.Date);

            Expression<Func<CalendarEntity, bool>> holidayExpressiojn = ci => ci.Date.Date >= DateTime.UtcNow.Date && ci.Recurring == false && ci.Type == EnumsHelper.CalendarItem.Holiday;
            var upcomingHoliday = (await _calendarRepository.GetAggregateDataAsync<CalendarEntity>(holidayExpressiojn, isAscending: true, orderedKey: "Date", pageSize: 5)).ToList();

            Expression<Func<CalendarEntity, bool>> eventExpression = ci => ci.Date.Date >= DateTime.UtcNow.Date && ci.Recurring == false && ci.Type == EnumsHelper.CalendarItem.Event;
            var upcomingEvent = (await _calendarRepository.GetAggregateDataAsync<CalendarEntity>(eventExpression, isAscending: true, orderedKey: "Date", pageSize: 5)).ToList();

            var combinedItems = upcomingHoliday.Concat(upcomingEvent).Concat(upcomingEventOrHolidays).OrderBy(ci => ci.Date).Select(x =>
                new CalendarItemDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Date = x.Date,
                    Type = x.Type,
                    Description = x.Description,
                    ImageUrl = string.IsNullOrEmpty(x.ImageUrl) ? null : Common.GetCalendarItemCoverImagePath(x.ImageUrl)
                }
            ).ToList();
            result.MethodResult = new UpComingHolidayEventResponseDto
            {
                Holiday = combinedItems.Where(x => x.Type == EnumsHelper.CalendarItem.Holiday).Take(5).ToList(),
                Event = combinedItems.Where(x => x.Type == EnumsHelper.CalendarItem.Event).Take(5).ToList()
            };
            return result;
        }

        public async Task<Result<UpcomingCelebrations>> GetUpComingCelebrations()
        {
            Result<UpcomingCelebrations> result = new();
            IEnumerable<EmpUser> employeeList = await _empUserRepository.GetAll();
            List<string> empJobIds = employeeList.Select(e => e.JobRole).ToList();
            IEnumerable<JobTitles> jobTitles = await _jobTitleRepository.GetAll(jt => empJobIds.Contains(jt.JobTitleId));

            var today = DateTime.Today;
            string dateFormat = "yyyy-MM-dd";

            var empJobTitleJoin = (from emp in employeeList
                                   join jobTitle in jobTitles
                                   on emp.JobRole equals jobTitle.JobTitleId into empJobTitleGroup
                                   from jobRole in empJobTitleGroup.DefaultIfEmpty()
                                   select new
                                   {
                                       EmployeeId = emp.EmployeeId,
                                       EmployeeName = $"{emp.FirstName} {emp.LastName}",
                                       JobRole = jobRole?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
                                       DateOfBirth = emp.DateOfBirth,
                                       DateOfJoining = emp.DateOfJoining,
                                       ProfileUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                                   }).ToList();

            var upcomingBirthdays = empJobTitleJoin
                .Select(emp =>
                {
                    if (!DateTime.TryParseExact(emp.DateOfBirth, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dob))
                        return null; // Skip if invalid format

                    var thisYearBirthday = new DateTime(today.Year, dob.Month, dob.Day);
                    var nextBirthday = thisYearBirthday < today ? thisYearBirthday.AddYears(1) : thisYearBirthday;

                    return new CalebrationItemDto
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = emp.EmployeeName,
                        ProfileUrl = emp.ProfileUrl,
                        JobRole = emp.JobRole,
                        Date = nextBirthday,
                        Ordinal = nextBirthday.Year - dob.Year
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x.Date)
                .Take(5)
                .ToList();

            var upcomingAnniversaries = empJobTitleJoin
                .Select(emp =>
                {
                    if (!DateTime.TryParseExact(emp.DateOfJoining, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime doj))
                        return null; // Skip if invalid format

                    var thisYearAnniv = new DateTime(today.Year, doj.Month, doj.Day);
                    var nextAnniv = thisYearAnniv < today ? thisYearAnniv.AddYears(1) : thisYearAnniv;

                    return new CalebrationItemDto
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = emp.EmployeeName,
                        ProfileUrl = emp.ProfileUrl,
                        JobRole = emp.JobRole,
                        Date = nextAnniv,
                        Ordinal = nextAnniv.Year - doj.Year
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x.Date)
                .Take(5)
                .ToList();

            result.MethodResult = new UpcomingCelebrations
            {
                Birthday = upcomingBirthdays,
                WorkAnniversary = upcomingAnniversaries
            };
            result.Success = true;
            return result;
        }

    }

}

