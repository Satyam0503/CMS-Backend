using System.Globalization;
using System.Linq.Expressions;
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
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using LinqKit;
using MapsterMapper;
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
            IMapper mapper
        )
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
            var allDepartments = await _departmentRepository.GetAll(x =>
                x.CompanyId == companyId && x.IsDeleted == false
            );
            var allEmpUser = await _empUserRepository.GetAll(x =>
                x.CompanyId == companyId
                && x.Status
                && (
                    x.Lifecycle == null
                    || x.Lifecycle.EmploymentStatus == null
                    || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active
                    || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod
                    || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended
                )
            );
            var result =
                from ad in allDepartments
                join aeu in allEmpUser on ad.DepartmentId equals aeu.Department into empGroup
                select new DepartmentEmpResponseDto
                {
                    Label = ad.Titles.ToDictionary(
                        keySelector: d => d.Language,
                        elementSelector: d => d.Label
                    ),
                    DepartmentId = ad.DepartmentId,
                    EmployeeCount = empGroup.Count(),
                };
            return result.ToList();
        }

        public async Task<List<GenderDetailsResponseModel>> GetAllGenderDetails(string companyId)
        {
            var allEmpUser = await _empUserRepository.GetAll(x =>
                x.CompanyId == companyId
                && x.Status
                && (
                    x.Lifecycle == null
                    || x.Lifecycle.EmploymentStatus == null
                    || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active
                    || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod
                    || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended
                )
            );
            var result = (
                from aeu in allEmpUser
                group aeu by aeu.Gender into genderGroup
                select new GenderDetailsResponseModel
                {
                    Gender = genderGroup.Key,
                    Count = genderGroup.Count(),
                }
            ).ToList();

            return result;
        }

        public async Task<Result<ApplicationDataResponseDto>> GetApplicationStatusData(
            string? vacancyId
        )
        {
            Result<ApplicationDataResponseDto> result = new();
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            List<Applicant> applicantList = [];
            if (vacancyId != null)
            {
                applicantList = (
                    await _applicantRepository.GetAll(ap =>
                        ap.CompanyId == companyId && ap.VacancyId == vacancyId
                    )
                ).ToList();
            }
            else
            {
                applicantList = (
                    await _applicantRepository.GetAll(ap => ap.CompanyId == companyId)
                ).ToList();
            }
            if (applicantList.Count == 0)
                return result;
            var groupedApplicantData = applicantList
                .GroupBy(ap => ap.ActivityType)
                .Select(apg => new ApplicationStatusTypeData()
                {
                    ActivityType = apg.Key,
                    Count = apg.Count(),
                })
                .OrderBy(ap => ap.ActivityType)
                .ToList();
            result.MethodResult = new ApplicationDataResponseDto()
            {
                TotalApplications = applicantList.Count,
                ApplicationStatusData = groupedApplicantData,
            };
            return result;
        }

        public async Task<Result<UpComingHolidayEventResponseDto>> GetUpComingHolidayAndEvents()
        {
            Result<UpComingHolidayEventResponseDto> result = new();
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var today = IndiaTime.Today;
            var targetMonth = today.AddMonths(2);
            var windowEnd = new DateTime(targetMonth.Year, targetMonth.Month, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month), 23, 59, 59, DateTimeKind.Utc);
            if (today.AddMonths(2) > windowEnd)
            {
                windowEnd = today.AddMonths(2);
            }

            var recurringItems = await _calendarRepository.GetAll(ci =>
                ci.CompanyId == companyId && ci.Recurring
            );
            var upcomingEventOrHolidays = recurringItems
                .Select(ci =>
                {
                    var date = ci.Date;
                    var day = date.Day;
                    if (date.Month == 2 && day == 29 && !DateTime.IsLeapYear(today.Year))
                    {
                        day = 28;
                    }
                    var adjustedDate = new DateTime(
                        today.Year,
                        date.Month,
                        day,
                        date.Hour,
                        date.Minute,
                        date.Second,
                        date.Kind
                    );
                    if (adjustedDate.Date < today.Date)
                    {
                        var nextYear = today.Year + 1;
                        var nextDay = date.Day;
                        if (date.Month == 2 && nextDay == 29 && !DateTime.IsLeapYear(nextYear))
                        {
                            nextDay = 28;
                        }
                        adjustedDate = new DateTime(nextYear, date.Month, nextDay, date.Hour, date.Minute, date.Second, date.Kind);
                    }
                    return new CalendarEntity()
                    {
                        Id = ci.Id,
                        Name = ci.Name,
                        Date = adjustedDate,
                        Recurring = ci.Recurring,
                        Description = ci.Description,
                        Type = ci.Type,
                        ImageUrl = ci.ImageUrl,
                    };
                })
                .Where(ci => ci.Date.Date >= today.Date && ci.Date.Date <= windowEnd.Date)
                .OrderBy(ci => ci.Date);

            Expression<Func<CalendarEntity, bool>> holidayExpression = ci =>
                ci.CompanyId == companyId
                && ci.Date >= today.Date
                && ci.Date <= windowEnd
                && ci.Recurring == false
                && ci.Type == EnumsHelper.CalendarItem.Holiday;
            var upcomingHoliday = (
                await _calendarRepository.GetAll(holidayExpression)
            ).ToList();

            Expression<Func<CalendarEntity, bool>> eventExpression = ci =>
                ci.CompanyId == companyId
                && ci.Date >= today.Date
                && ci.Date <= windowEnd
                && ci.Recurring == false
                && ci.Type == EnumsHelper.CalendarItem.Event;
            var upcomingEvent = (
                await _calendarRepository.GetAll(eventExpression)
            ).ToList();

            var combinedItems = upcomingHoliday
                .Concat(upcomingEvent)
                .Concat(upcomingEventOrHolidays)
                .OrderBy(ci => ci.Date)
                .Select(x => new CalendarItemDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Date = x.Date,
                    Type = x.Type,
                    Description = x.Description,
                    ImageUrl = Common.GetCalendarItemCoverImagePath(x.ImageUrl),
                })
                .ToList();
            result.MethodResult = new UpComingHolidayEventResponseDto
            {
                Holiday = combinedItems
                    .Where(x => x.Type == EnumsHelper.CalendarItem.Holiday)
                    .ToList(),
                Event = combinedItems
                    .Where(x => x.Type == EnumsHelper.CalendarItem.Event)
                    .ToList(),
            };
            return result;
        }

        public async Task<Result<UpcomingCelebrations>> GetUpComingCelebrations()
        {
            Result<UpcomingCelebrations> result = new();
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            IEnumerable<EmpUser> employeeList = await _empUserRepository.GetAll(employee =>
                employee.CompanyId == companyId
                && employee.Status
                && (
                    employee.Lifecycle == null
                    || employee.Lifecycle.EmploymentStatus == null
                    || employee.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active
                    || employee.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod
                    || employee.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended
                )
            );
            List<string> empJobIds = employeeList.Select(e => e.JobRole).ToList();
            IEnumerable<JobTitles> jobTitles = await _jobTitleRepository.GetAll(jt =>
                jt.CompanyId == companyId && empJobIds.Contains(jt.JobTitleId)
            );

            var today = IndiaTime.Today;
            var targetMonth = today.AddMonths(2);
            var windowEnd = new DateTime(targetMonth.Year, targetMonth.Month, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month), 23, 59, 59, DateTimeKind.Utc);
            if (today.AddMonths(2) > windowEnd)
            {
                windowEnd = today.AddMonths(2);
            }
            string dateFormat = "yyyy-MM-dd";

            var empJobTitleJoin = (
                from emp in employeeList
                join jobTitle in jobTitles
                    on emp.JobRole equals jobTitle.JobTitleId
                    into empJobTitleGroup
                from jobRole in empJobTitleGroup.DefaultIfEmpty()
                select new
                {
                    EmployeeId = emp.EmployeeId,
                    EmployeeName = $"{emp.FirstName} {emp.LastName}",
                    JobRole = jobRole?.Titles.ToDictionary(
                        keySelector: jt => jt.Language,
                        elementSelector: jt => jt.Label
                    ),
                    DateOfBirth = emp.DateOfBirth,
                    DateOfJoining = emp.DateOfJoining,
                    ProfileUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                }
            ).ToList();

            var upcomingBirthdays = empJobTitleJoin
                .Select(emp =>
                {
                    if (
                        !DateTime.TryParseExact(
                            emp.DateOfBirth,
                            dateFormat,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime dob
                        )
                    )
                        return null; // Skip if invalid format

                    var day = dob.Day;
                    if (dob.Month == 2 && day == 29 && !DateTime.IsLeapYear(today.Year))
                    {
                        day = 28;
                    }
                    var thisYearBirthday = new DateTime(today.Year, dob.Month, day);
                    var nextYear = thisYearBirthday < today ? today.Year + 1 : today.Year;
                    var nextDay = dob.Day;
                    if (dob.Month == 2 && nextDay == 29 && !DateTime.IsLeapYear(nextYear))
                    {
                        nextDay = 28;
                    }
                    var nextBirthday = new DateTime(nextYear, dob.Month, nextDay);

                    return new CalebrationItemDto
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = emp.EmployeeName,
                        ProfileUrl = emp.ProfileUrl,
                        JobRole = emp.JobRole,
                        Date = nextBirthday,
                        Ordinal = nextBirthday.Year - dob.Year,
                    };
                })
                .Where(x => x != null && x.Date.Date >= today.Date && x.Date.Date <= windowEnd.Date)
                .OrderBy(x => x.Date)
                .ToList();

            var upcomingAnniversaries = empJobTitleJoin
                .Select(emp =>
                {
                    if (
                        !DateTime.TryParseExact(
                            emp.DateOfJoining,
                            dateFormat,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime doj
                        )
                    )
                        return null; // Skip if invalid format

                    var day = doj.Day;
                    if (doj.Month == 2 && day == 29 && !DateTime.IsLeapYear(today.Year))
                    {
                        day = 28;
                    }
                    var thisYearAnniv = new DateTime(today.Year, doj.Month, day);
                    var nextYear = thisYearAnniv < today ? today.Year + 1 : today.Year;
                    var nextDay = doj.Day;
                    if (doj.Month == 2 && nextDay == 29 && !DateTime.IsLeapYear(nextYear))
                    {
                        nextDay = 28;
                    }
                    var nextAnniv = new DateTime(nextYear, doj.Month, nextDay);

                    return new CalebrationItemDto
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = emp.EmployeeName,
                        ProfileUrl = emp.ProfileUrl,
                        JobRole = emp.JobRole,
                        Date = nextAnniv,
                        Ordinal = nextAnniv.Year - doj.Year,
                    };
                })
                .Where(x => x != null && x.Date.Date >= today.Date && x.Date.Date <= windowEnd.Date)
                .OrderBy(x => x.Date)
                .ToList();

            result.MethodResult = new UpcomingCelebrations
            {
                Birthday = upcomingBirthdays,
                WorkAnniversary = upcomingAnniversaries,
            };
            result.Success = true;
            return result;
        }
    }
}
