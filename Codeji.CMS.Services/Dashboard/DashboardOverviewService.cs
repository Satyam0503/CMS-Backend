using System.Globalization;
using System.Linq.Expressions;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Dashboard;

public sealed class DashboardOverviewService(
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceModel> attendance,
    IMongoDbRepository<LeaveRequest> leaveRequests,
    IMongoDbRepository<LeavePolicy> leavePolicies,
    IMongoDbRepository<JobVacancy> vacancies,
    IMongoDbRepository<JobTitles> jobTitles,
    IMongoDbRepository<EmployeeExit> exits,
    ICompanyWorkingCalendarService workingCalendar,
    IRoleService roleService)
{
    private const int TrendMonths = 12;
    private const int ActivityLimit = 8;
    private const int RecentActivityWindowDays = 30;
    private static readonly HashSet<string> DashboardActivityTypes =
        new(StringComparer.OrdinalIgnoreCase) { "joined", "leaveApproved", "exitInitiated", "jobPosted" };

    // Statuses that mean the person worked today; WFH is presence, not leave.
    private static readonly HashSet<string> PresentStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "P", "WFH", "WFH+WFO", "WFH-HD", "LHD", "LHD+ED", "WFH+SL", "WFH+CL", "ED" };

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

    /// <summary>
    /// The overview needs lifecycle, date, and display fields only. Keep credentials,
    /// contact, tax, banking, and custom profile fields out of this analytics read.
    /// Rejoin history is filtered and reduced to the three fields used by movement.
    /// </summary>
    internal static readonly ProjectionDefinition<EmpUser, EmpUser> EmployeeAnalyticsProjection =
        Builders<EmpUser>.Projection.Expression(employee => new EmpUser
        {
            UserId = employee.UserId,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = string.Empty,
            EmployeeId = employee.EmployeeId,
            JobRole = employee.JobRole,
            DateOfJoining = employee.DateOfJoining,
            ExitDate = employee.ExitDate,
            EmploymentStatus = employee.EmploymentStatus,
            LastWorkingDate = employee.LastWorkingDate,
            Status = employee.Status,
            Lifecycle = employee.Lifecycle == null
                ? null!
                : new EmployeeLifecycleState
                {
                    EmploymentStatus = employee.Lifecycle.EmploymentStatus,
                    AvailabilityStatus = employee.Lifecycle.AvailabilityStatus,
                    AvailabilityEffectiveFrom = employee.Lifecycle.AvailabilityEffectiveFrom,
                    AvailabilityExpectedReturnDate = employee.Lifecycle.AvailabilityExpectedReturnDate,
                    History = employee.Lifecycle.History
                        .Where(history => history.Dimension == nameof(EmployeeLifecycleState.EmploymentStatus)
                            && history.ReasonCode == "REJOIN")
                        .Select(history => new EmployeeLifecycleStatusHistory
                        {
                            Dimension = history.Dimension,
                            ReasonCode = history.ReasonCode,
                            EffectiveFromUtc = history.EffectiveFromUtc,
                        })
                        .ToList(),
                },
        });

    /// <summary>Attendance composition uses no notes, audit actors or reconciliation metadata.</summary>
    internal static readonly ProjectionDefinition<AttendanceModel, AttendanceModel> AttendanceAnalyticsProjection =
        Builders<AttendanceModel>.Projection.Expression(row => new AttendanceModel
        {
            UserId = row.UserId,
            CheckInTime = row.CheckInTime,
            Status = row.Status,
        });

    /// <summary>
    /// Leave overview metrics use dates, current status and employee/policy IDs only.
    /// Request reasons, comments, evidence and reconciliation details are not analytics inputs.
    /// </summary>
    internal static readonly ProjectionDefinition<LeaveRequest, LeaveRequest> LeaveAnalyticsProjection =
        Builders<LeaveRequest>.Projection.Expression(request => new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            LeavePolicyId = request.LeavePolicyId,
            Reason = string.Empty,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedDate = request.CreatedDate,
            ReviewedAt = request.ReviewedAt,
        });

    /// <summary>Recent exit activity is an event label, not an offboarding case-detail view.</summary>
    internal static readonly ProjectionDefinition<EmployeeExit, EmployeeExit> ExitActivityProjection =
        Builders<EmployeeExit>.Projection.Expression(exit => new EmployeeExit
        {
            EmployeeUserId = exit.EmployeeUserId,
            ExitType = exit.ExitType,
            InitiatedAtUtc = exit.InitiatedAtUtc,
        });

    /// <summary>Movement analytics needs completion, status, effective date and deletion state only.</summary>
    internal static readonly ProjectionDefinition<EmployeeExit, EmployeeExit> ExitMovementProjection =
        Builders<EmployeeExit>.Projection.Expression(exit => new EmployeeExit
        {
            Status = exit.Status,
            LastWorkingDate = exit.LastWorkingDate,
            CompletedAtUtc = exit.CompletedAtUtc,
            IsDeleted = exit.IsDeleted,
        });

    /// <summary>Recent job activity needs only the event author, title and date.</summary>
    internal static readonly ProjectionDefinition<JobVacancy, JobVacancy> VacancyActivityProjection =
        Builders<JobVacancy>.Projection.Expression(job => new JobVacancy
        {
            CreatedBy = job.CreatedBy,
            Title = job.Title,
            CreatedDate = job.CreatedDate,
        });

    /// <summary>Joined activity labels need only the job title ID and localized labels.</summary>
    internal static readonly ProjectionDefinition<JobTitles, JobTitles> JobTitleLabelProjection =
        Builders<JobTitles>.Projection.Expression(jobTitle => new JobTitles
        {
            JobTitleId = jobTitle.JobTitleId,
            Titles = jobTitle.Titles.Select(title => new MultilingualModel
            {
                Language = title.Language,
                Label = title.Label,
            }).ToList(),
        });

    /// <summary>Approved-leave activity displays only the leave policy name.</summary>
    internal static readonly ProjectionDefinition<LeavePolicy, LeavePolicy> LeavePolicyNameProjection =
        Builders<LeavePolicy>.Projection.Expression(policy => new LeavePolicy
        {
            Id = policy.Id,
            Name = policy.Name,
            Code = string.Empty,
        });

    public Task<DashboardOverviewDto> GetOverviewAsync(string companyId) =>
        GetOverviewAsync(companyId, IndiaTime.Today);

    public async Task<DashboardOverviewDto> GetOverviewAsync(string companyId, DateTime today)
    {
        today = today.Date;
        var tomorrow = today.AddDays(1);

        var moduleAvailability = await roleService.GetModuleAvailabilityForCompany(companyId,
        [
            AppModule.Employees,
            AppModule.Attendance,
            AppModule.LeaveManagement,
            AppModule.Jobs,
            AppModule.Offboarding,
        ]);
        var employeesAvailable = moduleAvailability.GetValueOrDefault(AppModule.Employees);
        var attendanceAvailable = moduleAvailability.GetValueOrDefault(AppModule.Attendance);
        var leaveAvailable = moduleAvailability.GetValueOrDefault(AppModule.LeaveManagement);
        var jobsAvailable = moduleAvailability.GetValueOrDefault(AppModule.Jobs);
        var offboardingAvailable = moduleAvailability.GetValueOrDefault(AppModule.Offboarding);

        // Load only fields used by the dashboard's workforce, attendance and activity metrics.
        var allEmployees = employeesAvailable
            ? (await employees.GetAggregateDataAsync<EmpUser>(
                x => x.CompanyId == companyId,
                EmployeeAnalyticsProjection)).ToList()
            : [];
        var current = allEmployees
            .Where(IsCurrentEmployeeCompiled)
            .Where(e => EmploymentPeriod.WasEmployedOn(e, today))
            .ToList();
        var currentIds = current.Select(x => x.UserId).ToHashSet();

        var trend = employeesAvailable ? BuildHeadcountTrend(allEmployees, today) : [];
        var lastMonthCount = trend.Count >= 2 ? trend[^2].Count : 0;
        var movementStart = new DateTime(today.Year, today.Month, 1).AddMonths(-(TrendMonths - 1));
        var storedStart = IndiaTime.ToStoredBusinessDate(DateOnly.FromDateTime(movementStart.AddDays(-1)));
        var storedEnd = IndiaTime.ToStoredBusinessDate(DateOnly.FromDateTime(today.AddDays(2)));
        var movementAvailable = employeesAvailable && offboardingAvailable;
        var completedExits = movementAvailable
            ? (await exits.GetAggregateDataAsync<EmployeeExit>(x =>
                x.CompanyId == companyId
                && x.LastWorkingDate >= storedStart
                && x.LastWorkingDate < storedEnd
                && x.CompletedAtUtc.HasValue
                && (EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status) || x.Status == "Rejoined"),
                ExitMovementProjection))
                .ToList()
            : [];
        var movementTrend = movementAvailable ? BuildWorkforceMovementTrend(allEmployees, completedExits, today) : [];
        var storedCohortStart = IndiaTime.ToStoredBusinessDate(DateOnly.FromDateTime(movementStart.AddDays(-1)));
        var storedCohortEnd = IndiaTime.ToStoredBusinessDate(DateOnly.FromDateTime(today.AddDays(2)));
        var leaveCohorts = leaveAvailable
            ? (await leaveRequests.GetAggregateDataAsync<LeaveRequest>(
                x => x.CompanyId == companyId
                    && x.CreatedDate.HasValue
                    && x.CreatedDate >= storedCohortStart
                    && x.CreatedDate < storedCohortEnd,
                LeaveAnalyticsProjection))
                .ToList()
            : [];
        var leaveRequestCohortTrend = BuildLeaveRequestCohortTrend(leaveCohorts, today);

        var attendanceToday = attendanceAvailable && employeesAvailable
            ? (await attendance.GetAggregateDataAsync<AttendanceModel>(
                x => x.CompanyId == companyId && x.Date >= today && x.Date < tomorrow,
                AttendanceAnalyticsProjection))
                .Where(x => x.UserId != null && currentIds.Contains(x.UserId))
                .ToList()
            : [];
        var presentIds = attendanceToday
            .Where(x => x.UserId != null && currentIds.Contains(x.UserId)
                        && (x.CheckInTime.HasValue || PresentStatuses.Contains(x.Status)))
            .Select(x => x.UserId!)
            .ToHashSet();

        var leaveIds = leaveAvailable && employeesAvailable
            ? (await leaveRequests.GetAggregateDataAsync<LeaveRequest>(
                x => x.CompanyId == companyId
                    && x.Status == EnumsHelper.LeaveRequestStatus.Accepted
                    && x.StartDate < tomorrow
                    && x.EndDate >= today,
                LeaveAnalyticsProjection))
                .Where(x => currentIds.Contains(x.EmployeeId))
                .Select(x => x.EmployeeId)
                .ToHashSet()
            : new HashSet<string>();

        var isWorkingDay = await workingCalendar.IsWorkingDayAsync(companyId, DateOnly.FromDateTime(today));
        var attendanceBreakdownAvailable = employeesAvailable && attendanceAvailable && leaveAvailable;
        var attendanceBreakdown = attendanceBreakdownAvailable
            ? BuildAttendanceBreakdown(current, attendanceToday, leaveIds, today, isWorkingDay)
            : null;

        return new DashboardOverviewDto
        {
            DefinitionVersion = DashboardOverviewDto.CurrentDefinitionVersion,
            AsOfDate = DateOnly.FromDateTime(today),
            TimeZone = DashboardOverviewDto.TimeZoneId,
            GeneratedAtUtc = DateTime.UtcNow,
            SourceAvailability = new DashboardSourceAvailabilityDto
            {
                Employees = employeesAvailable,
                Attendance = attendanceAvailable,
                LeaveManagement = leaveAvailable,
                Jobs = jobsAvailable,
                Offboarding = offboardingAvailable,
            },
            TotalEmployees = current.Count,
            TotalEmployeesChangePercent = !employeesAvailable || lastMonthCount == 0
                ? null
                : Math.Round((current.Count - lastMonthCount) * 100m / lastMonthCount, 1, MidpointRounding.AwayFromZero),
            PresentToday = attendanceAvailable && employeesAvailable ? presentIds.Count : 0,
            OnLeaveToday = leaveAvailable && employeesAvailable ? leaveIds.Count : 0,
            IsWorkingDay = isWorkingDay,
            AbsentToday = attendanceBreakdownAvailable ? CalculateAbsent(current, presentIds, leaveIds, today, isWorkingDay) : 0,
            AttendanceBreakdown = attendanceBreakdown,
            OpenPositions = jobsAvailable ? await vacancies.Count(x => x.CompanyId == companyId && x.Status && !x.IsDeleted) : 0,
            HeadcountTrend = trend,
            WorkforceMovementTrend = movementTrend,
            LeaveRequestCohortTrend = leaveAvailable ? leaveRequestCohortTrend : [],
            RecentActivities = employeesAvailable
                ? await GetRecentActivitiesAsync(companyId, allEmployees, today, leaveAvailable, jobsAvailable, offboardingAvailable)
                : [],
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
                    Count = month == currentMonth
                        ? employeesOfCompany.Count(e => IsCurrentEmployeeCompiled(e) && EmploymentPeriod.WasEmployedOn(e, point))
                        : employeesOfCompany.Count(e => EmploymentPeriod.WasEmployedOn(e, point)),
                };
            })
            .ToList();
    }

    /// <summary>
    /// Counts original join dates and explicit rejoin lifecycle events as hires; completed
    /// EmployeeExit records count as exits on their effective last-working business date.
    /// The current month is partial through today, matching the headcount trend window.
    /// </summary>
    public static List<WorkforceMovementPointDto> BuildWorkforceMovementTrend(
        IReadOnlyCollection<EmpUser> employeesOfCompany,
        IReadOnlyCollection<EmployeeExit> completedExits,
        DateTime today)
    {
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var months = Enumerable.Range(0, TrendMonths)
            .Select(i => currentMonth.AddMonths(i - (TrendMonths - 1)))
            .ToList();
        var windowStart = months[0];
        var monthKeys = months.Select(x => x.ToString("yyyy-MM", CultureInfo.InvariantCulture)).ToHashSet(StringComparer.Ordinal);
        var hiresByMonth = new Dictionary<string, int>(StringComparer.Ordinal);

        void AddHire(DateTime effectiveDate)
        {
            var date = effectiveDate.Date;
            if (date > today.Date) return;
            var key = date.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            if (monthKeys.Contains(key)) hiresByMonth[key] = hiresByMonth.GetValueOrDefault(key) + 1;
        }

        foreach (var employee in employeesOfCompany)
        {
            if (!string.IsNullOrWhiteSpace(employee.DateOfJoining)
                && DateTime.TryParse(employee.DateOfJoining, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var joiningDate))
                AddHire(joiningDate);
            foreach (var rejoin in employee.Lifecycle?.History ?? [])
            {
                if (rejoin.Dimension == nameof(EmployeeLifecycleState.EmploymentStatus)
                    && string.Equals(rejoin.ReasonCode, "REJOIN", StringComparison.OrdinalIgnoreCase))
                    AddHire(IndiaTime.ToBusinessDate(rejoin.EffectiveFromUtc).ToDateTime(TimeOnly.MinValue));
            }
        }

        var exitsByMonth = completedExits
            .Where(exit => exit.CompletedAtUtc.HasValue && !exit.IsDeleted
                && (EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(exit.Status) || exit.Status == "Rejoined"))
            .Select(exit => IndiaTime.ToBusinessDate(exit.LastWorkingDate))
            .Where(date => date <= DateOnly.FromDateTime(today) && date >= DateOnly.FromDateTime(windowStart))
            .GroupBy(date => date.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return months.Select(month =>
        {
            var key = month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            return new WorkforceMovementPointDto
            {
                Month = key,
                Hires = hiresByMonth.GetValueOrDefault(key),
                Exits = exitsByMonth.GetValueOrDefault(key),
            };
        }).ToList();
    }

    /// <summary>
    /// Groups requests by submission month and their current status. Status buckets can
    /// change after a reviewer updates a request; this is not a historical approval-event
    /// ledger and must not be described as approvals made in that month.
    /// </summary>
    public static List<LeaveRequestCohortPointDto> BuildLeaveRequestCohortTrend(
        IReadOnlyCollection<LeaveRequest> requests,
        DateTime today)
    {
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var months = Enumerable.Range(0, TrendMonths)
            .Select(i => currentMonth.AddMonths(i - (TrendMonths - 1)))
            .ToList();
        var start = DateOnly.FromDateTime(months[0]);
        var end = DateOnly.FromDateTime(today);
        var cohorts = requests
            .Where(request => request.CreatedDate.HasValue && !request.IsDeleted)
            .Select(request => (Request: request, Date: IndiaTime.ToBusinessDate(request.CreatedDate!.Value)))
            .Where(item => item.Date >= start && item.Date <= end)
            .GroupBy(item => item.Date.ToString("yyyy-MM", CultureInfo.InvariantCulture), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Request).ToList(), StringComparer.Ordinal);

        return months.Select(month =>
        {
            var key = month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            var cohort = cohorts.GetValueOrDefault(key) ?? [];
            return new LeaveRequestCohortPointDto
            {
                Month = key,
                TotalRequests = cohort.Count,
                Pending = cohort.Count(request => request.Status == EnumsHelper.LeaveRequestStatus.Pending),
                Approved = cohort.Count(request => request.Status == EnumsHelper.LeaveRequestStatus.Accepted),
                Rejected = cohort.Count(request => request.Status == EnumsHelper.LeaveRequestStatus.Rejected),
                Withdrawn = cohort.Count(request => request.Status == EnumsHelper.LeaveRequestStatus.WithDrawn),
                OtherStatus = cohort.Count(request => request.Status is not (
                    EnumsHelper.LeaveRequestStatus.Pending or EnumsHelper.LeaveRequestStatus.Accepted or
                    EnumsHelper.LeaveRequestStatus.Rejected or EnumsHelper.LeaveRequestStatus.WithDrawn)),
            };
        }).ToList();
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

    /// <summary>
    /// Partitions the employees expected to attend into disjoint buckets. Approved leave
    /// and a present mark can coexist for half-day requests; that overlap is kept visible.
    /// A missing row is reported as unreported rather than silently asserted to be absence.
    /// </summary>
    public static AttendanceBreakdownDto BuildAttendanceBreakdown(
        IEnumerable<EmpUser> employeesOfCompany,
        IEnumerable<AttendanceModel> attendanceRows,
        ISet<string> approvedLeaveIds,
        DateTime date,
        bool isWorkingDay)
    {
        var expected = isWorkingDay
            ? employeesOfCompany.Where(e => EmploymentPeriod.IsAttendanceExpectedOn(e, date)).ToList()
            : [];
        var expectedIds = expected.Select(e => e.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet(StringComparer.Ordinal);
        var rowsByUser = attendanceRows
            .Where(row => !string.IsNullOrWhiteSpace(row.UserId) && expectedIds.Contains(row.UserId!))
            .GroupBy(row => row.UserId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var present = rowsByUser.Where(pair => pair.Value.Any(row => row.CheckInTime.HasValue || PresentStatuses.Contains(row.Status)))
            .Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);
        var leave = approvedLeaveIds.Where(expectedIds.Contains).ToHashSet(StringComparer.Ordinal);
        var explicitAbsent = rowsByUser.Where(pair => pair.Value.Any(row => string.Equals(row.Status, "A", StringComparison.OrdinalIgnoreCase)))
            .Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);

        var presentOnly = 0;
        var leaveOnly = 0;
        var overlap = 0;
        var absent = 0;
        var otherStatus = 0;
        var unreported = 0;
        foreach (var employeeId in expectedIds)
        {
            var hasPresent = present.Contains(employeeId);
            var hasLeave = leave.Contains(employeeId);
            if (hasPresent && hasLeave) overlap++;
            else if (hasPresent) presentOnly++;
            else if (hasLeave) leaveOnly++;
            else if (explicitAbsent.Contains(employeeId)) absent++;
            else if (rowsByUser.ContainsKey(employeeId)) otherStatus++;
            else unreported++;
        }

        return new AttendanceBreakdownDto
        {
            ExpectedEmployees = expectedIds.Count,
            PresentOnly = presentOnly,
            LeaveOnly = leaveOnly,
            PresentAndOnLeave = overlap,
            ExplicitlyAbsent = absent,
            OtherStatus = otherStatus,
            Unreported = unreported,
        };
    }

    private async Task<List<DashboardActivityDto>> GetRecentActivitiesAsync(
        string companyId,
        IReadOnlyCollection<EmpUser> allEmployees,
        DateTime today,
        bool leaveAvailable,
        bool jobsAvailable,
        bool offboardingAvailable)
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
        var titleIds = jobsAvailable
            ? joined.Select(x => x.Employee.JobRole).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList()
            : [];
        var titles = titleIds.Count == 0
            ? new Dictionary<string, string>()
            : (await jobTitles.GetAggregateDataAsync<JobTitles>(
                x => x.CompanyId == companyId && titleIds.Contains(x.JobTitleId),
                JobTitleLabelProjection))
                .ToDictionary(x => x.JobTitleId, x => (x.Titles?.FirstOrDefault(t => t.Language == "en") ?? x.Titles?.FirstOrDefault())?.Label ?? string.Empty);
        activities.AddRange(joined.Select(x => new DashboardActivityDto
        {
            Type = "joined",
            EmployeeId = x.Employee.UserId,
            EmployeeName = NameOf(x.Employee),
            Detail = titles.GetValueOrDefault(x.Employee.JobRole ?? string.Empty),
            OccurredAt = DateTime.SpecifyKind(x.Joined!.Value.Date, DateTimeKind.Utc),
        }));

        var approvedLeaves = leaveAvailable
            ? (await leaveRequests.GetAggregateDataAsync<LeaveRequest>(
                x => x.CompanyId == companyId && x.Status == EnumsHelper.LeaveRequestStatus.Accepted && x.ReviewedAt != null,
                LeaveAnalyticsProjection,
                isAscending: false, orderedKey: nameof(LeaveRequest.ReviewedAt), pageNo: 1, pageSize: ActivityLimit))
                .ToList()
            : [];
        var policyIds = approvedLeaves.Select(x => x.LeavePolicyId).Distinct().ToList();
        var policyNames = policyIds.Count == 0
            ? new Dictionary<string, string>()
            : (await leavePolicies.GetAggregateDataAsync<LeavePolicy>(
                x => x.CompanyId == companyId && policyIds.Contains(x.Id),
                LeavePolicyNameProjection)).ToDictionary(x => x.Id, x => x.Name);
        activities.AddRange(approvedLeaves.Select(x => new DashboardActivityDto
        {
            Type = "leaveApproved",
            EmployeeId = Find(x.EmployeeId)?.UserId ?? x.EmployeeId,
            EmployeeName = NameOf(Find(x.EmployeeId)),
            Detail = policyNames.GetValueOrDefault(x.LeavePolicyId),
            OccurredAt = x.ReviewedAt!.Value,
        }));

        var recentExits = offboardingAvailable
            ? await exits.GetAggregateDataAsync<EmployeeExit>(
                x => x.CompanyId == companyId,
                ExitActivityProjection,
                isAscending: false, orderedKey: nameof(EmployeeExit.InitiatedAtUtc), pageNo: 1, pageSize: ActivityLimit)
            : [];
        activities.AddRange(recentExits.Select(x => new DashboardActivityDto
        {
            Type = "exitInitiated",
            EmployeeId = x.EmployeeUserId,
            EmployeeName = NameOf(Find(x.EmployeeUserId)),
            Detail = x.ExitType,
            OccurredAt = x.InitiatedAtUtc,
        }));

        var recentJobs = jobsAvailable
            ? await vacancies.GetAggregateDataAsync<JobVacancy>(
                x => x.CompanyId == companyId && x.CreatedDate != null,
                VacancyActivityProjection,
                isAscending: false, orderedKey: nameof(JobVacancy.CreatedDate), pageNo: 1, pageSize: ActivityLimit)
            : [];
        activities.AddRange(recentJobs.Select(x => new DashboardActivityDto
        {
            Type = "jobPosted",
            EmployeeId = Find(x.CreatedBy)?.UserId,
            EmployeeName = NameOf(Find(x.CreatedBy)),
            Detail = x.Title,
            OccurredAt = x.CreatedDate!.Value,
        }));

        return FilterRecentActivities(activities, today);
    }

    /// <summary>Returns at most eight activities from the last 30 calendar days, including today.</summary>
    internal static List<DashboardActivityDto> FilterRecentActivities(
        IEnumerable<DashboardActivityDto> activities, DateTime today)
    {
        var endExclusive = today.Date.AddDays(1);
        var startInclusive = today.Date.AddDays(-(RecentActivityWindowDays - 1));

        return activities
            // The general Dashboard.ViewAll feed has a deliberately narrow allow-list.
            // Compensation and any future unreviewed event types stay in their own workflows.
            .Where(activity => DashboardActivityTypes.Contains(activity.Type))
            .Where(activity => activity.OccurredAt >= startInclusive && activity.OccurredAt < endExclusive)
            .OrderByDescending(activity => activity.OccurredAt)
            .Take(ActivityLimit)
            .ToList();
    }
}
