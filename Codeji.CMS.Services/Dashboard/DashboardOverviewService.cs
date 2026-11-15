using System.Globalization;
using System.Linq.Expressions;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services.Dashboard;

public sealed class DashboardOverviewService(
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceModel> attendance,
    IMongoDbRepository<LeaveRequest> leaveRequests,
    IMongoDbRepository<LeavePolicy> leavePolicies,
    IMongoDbRepository<JobVacancy> vacancies,
    IMongoDbRepository<JobTitles> jobTitles,
    IMongoDbRepository<EmployeeExit> exits,
    IMongoDbRepository<EmployeeSalaryStructure> salaryStructures,
    ICompanyWorkingCalendarService workingCalendar)
{
    private const int TrendMonths = 6;
    private const int ActivityLimit = 8;

    // Statuses that mean the person worked today; WFH is presence, not leave.
    private static readonly HashSet<string> PresentStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "P", "WFH", "WFH+WFO", "HD", "WFH-HD", "LHD", "LHD+ED" };

    /// <summary>Same "current employee" rule as DashboardServices.GetAllGenderDetails; keep them in sync.</summary>
    public static readonly Expression<Func<EmpUser, bool>> IsCurrentEmployee = x =>
        x.Status
        && (
            x.Lifecycle == null
            || x.Lifecycle.EmploymentStatus == null
            || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active
            || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod
            || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended
        );

    private static readonly Func<EmpUser, bool> IsCurrentEmployeeCompiled = IsCurrentEmployee.Compile();

    public Task<DashboardOverviewDto> GetOverviewAsync(string companyId) =>
        GetOverviewAsync(companyId, IndiaTime.Today);

    public async Task<DashboardOverviewDto> GetOverviewAsync(string companyId, DateTime today)
    {
        today = today.Date;
        var tomorrow = today.AddDays(1);

        // ponytail: loads the tenant's employee documents once (as the existing dashboard widgets do);
        // switch to a projection if tenants grow past a few thousand employees.
        var allEmployees = (await employees.GetAll(x => x.CompanyId == companyId)).ToList();
        var current = allEmployees.Where(IsCurrentEmployeeCompiled).ToList();
        var currentIds = current.Select(x => x.UserId).ToHashSet();

        var trend = BuildHeadcountTrend(allEmployees, today);
        var lastMonthCount = trend[^2].Count;

        var presentIds = (await attendance.GetAll(x => x.CompanyId == companyId && x.Date >= today && x.Date < tomorrow))
            .Where(x => x.UserId != null && currentIds.Contains(x.UserId)
                        && (x.CheckInTime.HasValue || PresentStatuses.Contains(x.Status)))
            .Select(x => x.UserId!)
            .ToHashSet();

        var leaveIds = (await leaveRequests.GetAll(x =>
                x.CompanyId == companyId
                && x.Status == EnumsHelper.LeaveRequestStatus.Accepted
                && x.StartDate < tomorrow
                && x.EndDate >= today))
            .Where(x => currentIds.Contains(x.EmployeeId))
            .Select(x => x.EmployeeId)
            .ToHashSet();

        var isWorkingDay = await workingCalendar.IsWorkingDayAsync(companyId, DateOnly.FromDateTime(today));

        return new DashboardOverviewDto
        {
            TotalEmployees = current.Count,
            TotalEmployeesChangePercent = lastMonthCount == 0
                ? null
                : Math.Round((current.Count - lastMonthCount) * 100m / lastMonthCount, 1, MidpointRounding.AwayFromZero),
            PresentToday = presentIds.Count,
            OnLeaveToday = leaveIds.Count,
            IsWorkingDay = isWorkingDay,
            AbsentToday = CalculateAbsent(current, presentIds, leaveIds, today, isWorkingDay),
            OpenPositions = await vacancies.Count(x => x.CompanyId == companyId && x.Status),
            HeadcountTrend = trend,
            RecentActivities = await GetRecentActivitiesAsync(companyId, allEmployees, today),
        };
    }

    /// <summary>Headcount at each month end (today for the current month), oldest first.</summary>
    public static List<HeadcountPointDto> BuildHeadcountTrend(IReadOnlyCollection<EmpUser> employeesOfCompany, DateTime today)
    {
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        return Enumerable.Range(0, TrendMonths)
            .Select(i => currentMonth.AddMonths(i - (TrendMonths - 1)))
            .Select(month =>
            {
                var point = month == currentMonth ? today.Date : month.AddMonths(1).AddDays(-1);
                return new HeadcountPointDto
                {
                    Month = month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Count = employeesOfCompany.Count(e => EmploymentPeriod.WasEmployedOn(e, point)),
                };
            })
            .ToList();
    }

    /// <summary>Employees expected to work today who are neither present nor on approved leave.</summary>
    public static int CalculateAbsent(
        IEnumerable<EmpUser> currentEmployees, ISet<string> presentIds, ISet<string> leaveIds, DateTime today, bool isWorkingDay)
    {
        if (!isWorkingDay)
            return 0;
        return currentEmployees.Count(e =>
            EmploymentPeriod.IsAttendanceExpectedOn(e, today)
            && !presentIds.Contains(e.UserId)
            && !leaveIds.Contains(e.UserId));
    }

    private async Task<List<DashboardActivityDto>> GetRecentActivitiesAsync(
        string companyId, IReadOnlyCollection<EmpUser> allEmployees, DateTime today)
    {
        var byUserId = allEmployees.Where(x => x.UserId != null).GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.First());
        var byEmployeeCode = allEmployees.Where(x => !string.IsNullOrEmpty(x.EmployeeId))
            .GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.First());
        EmpUser? Find(string? id) =>
            id == null ? null : byUserId.TryGetValue(id, out var u) ? u : byEmployeeCode.GetValueOrDefault(id);
        static string NameOf(EmpUser? e) => e == null ? string.Empty : $"{e.FirstName} {e.LastName}".Trim();

        var activities = new List<DashboardActivityDto>();

        var joined = allEmployees
            .Select(e => (Employee: e, Joined: EmploymentPeriod.TryGetJoiningDate(e, out var d) ? d : (DateTime?)null))
            .Where(x => x.Joined.HasValue && x.Joined.Value.Date <= today)
            .OrderByDescending(x => x.Joined)
            .Take(ActivityLimit)
            .ToList();
        var titleIds = joined.Select(x => x.Employee.JobRole).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var titles = titleIds.Count == 0
            ? new Dictionary<string, string>()
            : (await jobTitles.GetAll(x => x.CompanyId == companyId && titleIds.Contains(x.JobTitleId)))
                .ToDictionary(x => x.JobTitleId, x => (x.Titles?.FirstOrDefault(t => t.Language == "en") ?? x.Titles?.FirstOrDefault())?.Label ?? string.Empty);
        activities.AddRange(joined.Select(x => new DashboardActivityDto
        {
            Type = "joined",
            EmployeeId = x.Employee.UserId,
            EmployeeName = NameOf(x.Employee),
            Detail = titles.GetValueOrDefault(x.Employee.JobRole ?? string.Empty),
            OccurredAt = DateTime.SpecifyKind(x.Joined!.Value.Date, DateTimeKind.Utc),
        }));

        var approvedLeaves = (await leaveRequests.GetAggregateDataAsync<LeaveRequest>(
                x => x.CompanyId == companyId && x.Status == EnumsHelper.LeaveRequestStatus.Accepted && x.ReviewedAt != null,
                isAscending: false, orderedKey: nameof(LeaveRequest.ReviewedAt), pageNo: 1, pageSize: ActivityLimit))
            .ToList();
        var policyIds = approvedLeaves.Select(x => x.LeavePolicyId).Distinct().ToList();
        var policyNames = policyIds.Count == 0
            ? new Dictionary<string, string>()
            : (await leavePolicies.GetAll(x => x.CompanyId == companyId && policyIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name);
        activities.AddRange(approvedLeaves.Select(x => new DashboardActivityDto
        {
            Type = "leaveApproved",
            EmployeeId = Find(x.EmployeeId)?.UserId ?? x.EmployeeId,
            EmployeeName = NameOf(Find(x.EmployeeId)),
            Detail = policyNames.GetValueOrDefault(x.LeavePolicyId),
            OccurredAt = x.ReviewedAt!.Value,
        }));

        // A revision is an approved structure that supersedes an earlier approved one for the same employee.
        var approvedStructures = (await salaryStructures.GetAggregateDataAsync<EmployeeSalaryStructure>(
                x => x.CompanyId == companyId && x.ApprovalStatus == "APPROVED" && x.ApprovedAtUtc != null,
                isAscending: false, orderedKey: nameof(EmployeeSalaryStructure.ApprovedAtUtc), pageNo: 1, pageSize: ActivityLimit))
            .ToList();
        var structureEmployeeIds = approvedStructures.Select(x => x.EmployeeId).Distinct().ToList();
        var priorStructures = structureEmployeeIds.Count == 0
            ? []
            : (await salaryStructures.GetAll(x =>
                x.CompanyId == companyId && x.ApprovalStatus == "APPROVED" && structureEmployeeIds.Contains(x.EmployeeId))).ToList();
        activities.AddRange(approvedStructures
            .Where(s => priorStructures.Any(p => p.EmployeeId == s.EmployeeId && p.Id != s.Id && p.EffectiveFrom < s.EffectiveFrom))
            .Select(s =>
            {
                var employee = Find(s.UserId) ?? Find(s.EmployeeId);
                return new DashboardActivityDto
                {
                    Type = "salaryRevised",
                    EmployeeId = employee?.UserId ?? s.UserId,
                    EmployeeName = NameOf(employee),
                    OccurredAt = s.ApprovedAtUtc!.Value,
                };
            }));

        var recentExits = await exits.GetAggregateDataAsync<EmployeeExit>(
            x => x.CompanyId == companyId,
            isAscending: false, orderedKey: nameof(EmployeeExit.InitiatedAtUtc), pageNo: 1, pageSize: ActivityLimit);
        activities.AddRange(recentExits.Select(x => new DashboardActivityDto
        {
            Type = "exitInitiated",
            EmployeeId = x.EmployeeUserId,
            EmployeeName = NameOf(Find(x.EmployeeUserId)),
            Detail = x.ExitType,
            OccurredAt = x.InitiatedAtUtc,
        }));

        var recentJobs = await vacancies.GetAggregateDataAsync<JobVacancy>(
            x => x.CompanyId == companyId && x.CreatedDate != null,
            isAscending: false, orderedKey: nameof(JobVacancy.CreatedDate), pageNo: 1, pageSize: ActivityLimit);
        activities.AddRange(recentJobs.Select(x => new DashboardActivityDto
        {
            Type = "jobPosted",
            EmployeeId = Find(x.CreatedBy)?.UserId,
            EmployeeName = NameOf(Find(x.CreatedBy)),
            Detail = x.Title,
            OccurredAt = x.CreatedDate!.Value,
        }));

        return activities.OrderByDescending(x => x.OccurredAt).Take(ActivityLimit).ToList();
    }
}
