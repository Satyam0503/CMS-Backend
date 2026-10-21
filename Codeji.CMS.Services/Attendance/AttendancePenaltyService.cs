using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using MongoDB.Driver;

public interface IAttendancePenaltyService
{
    Task<Result<AttendancePenaltyPolicyDto>> GetEmployeePolicy(string companyId, string employeeUserId, DateTime? effectiveOn = null);
    Task<Result> SaveEmployeePolicy(string companyId, string actorUserId, string employeeUserId, AttendancePenaltyPolicyDto dto);
    Task<Result<AttendancePayrollException>> Recalculate(string companyId, DateTime month);
    Task<Result<AttendancePayrollException>> GetExceptions(string companyId, AttendanceExceptionFilterDto filter);
    Task<Result> Review(string companyId, string reviewerId, string id, AttendanceExceptionReviewDto dto);
    Task<Result<AttendanceExceptionResolutionResultDto>> ResolveAttendanceException(string companyId, string reviewerId, string id, AttendanceExceptionResolutionDto dto);
    Task<Result> OverrideException(string companyId, string reviewerId, string id, AttendanceExceptionOverrideDto dto);
    Task<AttendanceMonthLockStatusDto> GetMonthLockStatus(string companyId, DateTime month);
    Task<Result<AttendanceMonthLockValidationResultDto>> ValidateAndLock(string companyId, string userId, DateTime month);
    Task<Result> UnlockMonth(string companyId, string userId, DateTime month);
}

public class AttendancePenaltyService : IAttendancePenaltyService
{
    private readonly IMongoDbRepository<AttendancePenaltyPolicy> _policies;
    private readonly IMongoDbRepository<AttendancePayrollException> _exceptions;
    private readonly IMongoDbRepository<MonthlyAttendanceSummary> _summaries;
    private readonly IMongoDbRepository<AttendanceModel> _attendance;
    private readonly IMongoDbRepository<AttendanceDaySegment> _segments;
    private readonly IMongoDbRepository<AttendanceStatusSetting> _statusSettings;
    private readonly IMongoDbRepository<EmpUser> _employees;
    private readonly IMongoDbRepository<WeeklyOffSetting> _weeklyOffs;
    private readonly IMongoDbRepository<CalendarEntity> _calendar;
    private readonly IMongoDbRepository<AttendanceNotificationOutbox> _notificationOutbox;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceMutationValidator _mutationValidator;
    private readonly IAttendanceAuditWriter _auditWriter;
    private readonly IMongoDbRepository<EmployeeLifecycleCase>? _cases;
    private readonly ILifecycleAttendanceReconciliationService? _lifecycleAttendanceReconciliation;
    private readonly IMongoDbRepository<EmpPayRoll>? _payrolls;
    public AttendancePenaltyService(IMongoDbRepository<AttendancePenaltyPolicy> policies, IMongoDbRepository<AttendancePayrollException> exceptions, IMongoDbRepository<MonthlyAttendanceSummary> summaries, IMongoDbRepository<AttendanceModel> attendance, IMongoDbRepository<AttendanceDaySegment> segments, IMongoDbRepository<AttendanceStatusSetting> statusSettings, IMongoDbRepository<EmpUser> employees, IMongoDbRepository<WeeklyOffSetting> weeklyOffs, IMongoDbRepository<CalendarEntity> calendar, IMongoDbRepository<AttendanceNotificationOutbox> notificationOutbox, IAttendanceRepository attendanceRepository, IAttendanceMutationValidator mutationValidator, IAttendanceAuditWriter auditWriter, IMongoDbRepository<EmployeeLifecycleCase>? cases = null, ILifecycleAttendanceReconciliationService? lifecycleAttendanceReconciliation = null, IMongoDbRepository<EmpPayRoll>? payrolls = null)
    { _policies=policies; _exceptions=exceptions; _summaries=summaries; _attendance=attendance; _segments=segments; _statusSettings=statusSettings; _employees=employees; _weeklyOffs=weeklyOffs; _calendar=calendar; _notificationOutbox=notificationOutbox; _attendanceRepository=attendanceRepository; _mutationValidator=mutationValidator; _auditWriter=auditWriter; _cases=cases; _lifecycleAttendanceReconciliation=lifecycleAttendanceReconciliation; _payrolls=payrolls; }

    public async Task<Result<AttendancePenaltyPolicyDto>> GetEmployeePolicy(string companyId, string employeeUserId, DateTime? effectiveOn=null)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(employeeUserId))
            return new Result<AttendancePenaltyPolicyDto> { Success = false, Message = "Company and employee are required." };

        var employee = await _employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employeeUserId && !x.IsDeleted);
        if (employee is null)
            return new Result<AttendancePenaltyPolicyDto> { Success = false, Message = "Employee was not found in the current company." };

        var on=(effectiveOn ?? DateTime.UtcNow).Date;
        var policy = await GetEffectiveEmployeePolicy(companyId, employeeUserId, on);
        var dto = policy is null
            ? new AttendancePenaltyPolicyDto { UserId = employee.UserId, EmployeeId = employee.EmployeeId, EffectiveFrom = new DateTime(on.Year, on.Month, 1) }
            : ToDto(policy);
        return new Result<AttendancePenaltyPolicyDto> { Success = true, MethodResult = dto };
    }

    public async Task<Result> SaveEmployeePolicy(string companyId,string actorUserId,string employeeUserId,AttendancePenaltyPolicyDto dto)
    {
        var result=new Result();
        if (dto is null) { result.Message = "Employee policy is required."; return result; }
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId) || string.IsNullOrWhiteSpace(employeeUserId))
        {
            result.Message = "Company, actor, and employee are required.";
            return result;
        }
        var employee = await _employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employeeUserId && !x.IsDeleted);
        if (employee is null) { result.Message = "Employee was not found in the current company."; return result; }
        if (dto.CombinedLhdEdMonthlyLimit < 0 || dto.CombinedLhdEdMonthlyLimit > 31) { result.Message = "Monthly limit must be between 0 and 31."; return result; }
        if(dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date<dto.EffectiveFrom.Date){result.Message="Effective-to date must be on or after effective-from date.";return result;}
        var versions=(await _policies.GetAll(x=>x.CompanyId==companyId && x.UserId==employeeUserId && !x.IsDeleted, withDefaultFilter:false)).OrderByDescending(x=>x.Version).ToList();
        // An update is identified by its server-issued policy id, not by a value
        // the client can change such as EffectiveFrom. This lets HR update every
        // field while retaining optimistic-concurrency protection.
        var existing = !string.IsNullOrWhiteSpace(dto.Id)
            ? versions.FirstOrDefault(x => x.Id == dto.Id)
            : versions.FirstOrDefault(x => x.EffectiveFrom.Date == dto.EffectiveFrom.Date);
        if (!string.IsNullOrWhiteSpace(dto.Id) && existing is null)
        {
            result.Message = "Employee policy was not found in the current company.";
            return result;
        }
        var requestedEnd = dto.EffectiveTo?.Date ?? DateTime.MaxValue.Date;
        if (versions.Where(x => existing is null || x.Id != existing.Id)
            .Any(x => x.EffectiveFrom.Date <= requestedEnd && (x.EffectiveTo?.Date ?? DateTime.MaxValue.Date) >= dto.EffectiveFrom.Date))
        {
            result.Message = "The effective dates overlap an existing policy for this employee.";
            return result;
        }
        var policy = existing ?? new AttendancePenaltyPolicy
        {
            CompanyId=companyId, UserId=employee.UserId, EmployeeId=employee.EmployeeId,
            EffectiveFrom=dto.EffectiveFrom.Date, Version=(versions.FirstOrDefault()?.Version??0)+1,
            CreatedBy=actorUserId, CreatedDate=DateTime.UtcNow
        };
        if (existing is not null && (dto.Version <= 0 || existing.Version != dto.Version))
        {
            result.Message = "This employee policy was changed by another user. Refresh and try again.";
            return result;
        }
        policy.Name=string.IsNullOrWhiteSpace(dto.Name) ? "Combined LHD + ED monthly allowance" : dto.Name.Trim();
        policy.CombinedLhdEdMonthlyLimit=dto.CombinedLhdEdMonthlyLimit; policy.IsEnabled=dto.IsEnabled;
        // Automatic payroll deductions remain prohibited. Every exceeded limit is
        // reviewed by HR/Admin and captured as an auditable exception.
        policy.RequiresHrApproval=true; policy.DefaultDecision="REVIEW_REQUIRED";
        policy.EffectiveFrom=dto.EffectiveFrom.Date; policy.EffectiveTo=dto.EffectiveTo?.Date; policy.UpdatedBy=actorUserId; policy.UpdatedDate=DateTime.UtcNow;
        if (existing is null) return await _policies.AddOne(policy);
        policy.Version++;
        var tenantScoped = Builders<AttendancePenaltyPolicy>.Filter.And(
            Builders<AttendancePenaltyPolicy>.Filter.Eq(x => x.CompanyId, companyId),
            Builders<AttendancePenaltyPolicy>.Filter.Eq(x => x.UserId, employeeUserId),
            Builders<AttendancePenaltyPolicy>.Filter.Eq(x => x.Id, policy.Id),
            Builders<AttendancePenaltyPolicy>.Filter.Eq(x => x.Version, dto.Version));
        return await _policies.Update(tenantScoped, policy);
    }

    public async Task<Result<AttendancePayrollException>> Recalculate(string companyId,DateTime month)
    {
        var start=new DateTime(month.Year,month.Month,1);var end=start.AddMonths(1).AddDays(-1);
        var lastCompletedDate=CalendarDateHelpers.GetLastCompletedBusinessDate(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
        var employees=(await _employees.GetAll(x=>x.CompanyId==companyId)).ToList();var output=new List<AttendancePayrollException>();
        foreach(var emp in employees)
        {
            var policy=await GetEffectiveEmployeePolicy(companyId,emp.UserId,end);
            if(policy is null){await CancelPendingLhdEdException(companyId,emp,start,"No active employee payroll penalty policy applies to this month.");continue;}
            if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to)){await CancelPendingLhdEdException(companyId,emp,start,"Employee is not eligible for this attendance month.");continue;}
            var reviewTo=to.Date<lastCompletedDate?to.Date:lastCompletedDate;
            var records=reviewTo<from.Date?new List<AttendanceModel>():(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<reviewTo.AddDays(1))).Where(a=>a.Status is "LHD" or "ED" or "LHD+ED").ToList();
            var daySegments=reviewTo<from.Date?new List<AttendanceDaySegment>():(await _segments.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<reviewTo.AddDays(1))).Where(a=>a.Status is "LHD" or "ED" or "LHD+ED").ToList();
            var segmentDates=daySegments.Select(x=>x.Date.Date).ToHashSet();
            var effectiveRecords=records.Where(x=>!segmentDates.Contains(x.Date.Date)).ToList();
            var lhd=effectiveRecords.Count(x=>x.Status is "LHD" or "LHD+ED")+daySegments.Count(x=>x.Status is "LHD" or "LHD+ED");
            var ed=effectiveRecords.Count(x=>x.Status is "ED" or "LHD+ED")+daySegments.Count(x=>x.Status is "ED" or "LHD+ED");
            var combined=lhd+ed;
            var exceeded=AttendancePayrollRules.ExceededOccurrences(combined,policy.CombinedLhdEdMonthlyLimit);
            var existing=await _exceptions.FirstOrDefault(x=>x.CompanyId==companyId&&x.UserId==emp.UserId&&x.EmployeeId==emp.EmployeeId&&x.PayrollMonth==start&&x.ExceptionType=="LHD_ED_LIMIT_EXCEEDED");
            if(exceeded==0)
            {
                if(existing!=null&&existing.Status=="PENDING_REVIEW")
                {
                    existing.Status="CANCELLED";
                    existing.UpdatedDate=DateTime.UtcNow;
                    await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,existing.Id),existing);
                }
                continue;
            }
            var item=existing??new AttendancePayrollException{CompanyId=companyId,UserId=emp.UserId,EmployeeId=emp.EmployeeId,PayrollMonth=start,PolicyId=policy.Id!,PolicyVersion=policy.Version};
            item.LhdCount=lhd;
            item.EdCount=ed;
            item.CombinedOccurrenceCount=combined;
            item.AllowedOccurrenceCount=policy.CombinedLhdEdMonthlyLimit;
            item.ExceededOccurrenceCount=exceeded;
            item.AffectedAttendanceRecordIds=effectiveRecords.Select(x=>x.AttendanceId!).Concat(daySegments.Select(x=>x.AttendanceDaySegmentId!)).Where(x=>x!=null).ToList();
            item.AffectedDates=effectiveRecords.Select(x=>x.Date.Date).Concat(daySegments.Select(x=>x.Date.Date)).Distinct().ToList();
            if(existing==null)
            {
                await _exceptions.AddOne(item);
                await QueueMonthlyLhdEdWarning(companyId, emp.UserId, start, policy.CombinedLhdEdMonthlyLimit, combined);
            }
            else if(existing.Status=="PENDING_REVIEW")
            {
                item.Version++;
                await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id),item);
            }
            else if(exceeded > existing.ExceededOccurrenceCount)
            {
                item.Status = "PENDING_REVIEW";
                item.Decision = null;
                item.Reason = (existing.Reason != null ? existing.Reason + "; " : "") +
                    $"Reopened: Exceeded occurrences increased from {existing.ExceededOccurrenceCount} to {exceeded}.";
                item.UpdatedDate = DateTime.UtcNow;
                item.Version++;
                await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id),item);
            }
            output.Add(item);
        }
        // Recalculate is the single refresh operation used by the Attendance UI.
        // Keep structural validation exceptions in sync as well as LHD/ED penalties,
        // otherwise repaired duplicates/checkouts remain incorrectly pending.
        await RefreshValidationExceptions(companyId, start, end);
        return new Result<AttendancePayrollException>{MethodResults=output,TotalRecords=output.Count};
    }

    private async Task<AttendancePenaltyPolicy?> GetEffectiveEmployeePolicy(string companyId, string employeeUserId, DateTime effectiveOn)
    {
        var on = effectiveOn.Date;
        return (await _policies.GetAll(x => x.CompanyId == companyId && x.UserId == employeeUserId && x.IsEnabled && !x.IsDeleted && x.EffectiveFrom <= on && (!x.EffectiveTo.HasValue || x.EffectiveTo >= on), withDefaultFilter: false))
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Version).FirstOrDefault();
    }

    private async Task CancelPendingLhdEdException(string companyId, EmpUser employee, DateTime payrollMonth, string reason)
    {
        var stale = await _exceptions.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == payrollMonth && x.ExceptionType == "LHD_ED_LIMIT_EXCEEDED" && x.Status == "PENDING_REVIEW");
        if (stale is null) return;
        stale.Status = "CANCELLED"; stale.Resolution = reason; stale.UpdatedDate = DateTime.UtcNow; stale.Version++;
        await _exceptions.Update(Builders<AttendancePayrollException>.Filter.And(
            Builders<AttendancePayrollException>.Filter.Eq(x => x.CompanyId, companyId),
            Builders<AttendancePayrollException>.Filter.Eq(x => x.Id, stale.Id),
            Builders<AttendancePayrollException>.Filter.Eq(x => x.Version, stale.Version - 1)), stale);
    }

    private static AttendancePenaltyPolicyDto ToDto(AttendancePenaltyPolicy policy) => new()
    {
        Id = policy.Id, UserId = policy.UserId, EmployeeId = policy.EmployeeId, Name = policy.Name,
        CombinedLhdEdMonthlyLimit = policy.CombinedLhdEdMonthlyLimit, IsEnabled = policy.IsEnabled,
        RequiresHrApproval = policy.RequiresHrApproval, DefaultDecision = policy.DefaultDecision,
        EffectiveFrom = policy.EffectiveFrom, EffectiveTo = policy.EffectiveTo, Version = policy.Version
    };

    private async Task QueueMonthlyLhdEdWarning(string companyId, string userId, DateTime monthStart, int limit, int occurrences)
    {
        var aggregateId = $"LHD_ED_LIMIT:{userId}:{monthStart:yyyy-MM}";
        if (await _notificationOutbox.Exist(x => x.CompanyId == companyId && x.EventType == "LHD_ED_MONTHLY_LIMIT_EXCEEDED" && x.AggregateId == aggregateId && x.RecipientUserId == userId)) return;
        await _notificationOutbox.AddOne(new AttendanceNotificationOutbox
        {
            CompanyId = companyId, EventType = "LHD_ED_MONTHLY_LIMIT_EXCEEDED", AggregateId = aggregateId, RecipientUserId = userId,
            Title = "Attendance punctuality warning", Body = $"You have recorded {occurrences} Late Half Day/Early Departure occurrences in {monthStart:MMMM yyyy}, exceeding the monthly limit of {limit}. Please avoid being late or leaving early for the rest of this month.",
            Status = "Pending", AvailableAtUtc = DateTime.UtcNow
        });
    }

    public async Task<Result<AttendancePayrollException>> GetExceptions(string companyId,AttendanceExceptionFilterDto filter)
    {
        var month = new DateTime(filter.PayrollMonth.Year, filter.PayrollMonth.Month, 1);
        var end = month.AddMonths(1).AddDays(-1);
        var employees = (await _employees.GetAll(x => x.CompanyId == companyId))
            .ToDictionary(x => x.UserId, StringComparer.Ordinal);
        var lastCompletedDate = CalendarDateHelpers.GetLastCompletedBusinessDate(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
        var all = (await _exceptions.GetAll(x => x.CompanyId == companyId && x.PayrollMonth == month))
            // Resolved records are retained for audit, but are no longer active exceptions.
            .Where(x => x.Status != "RESOLVED")
            .Where(x => (string.IsNullOrEmpty(filter.EmployeeId) || x.EmployeeId.Contains(filter.EmployeeId, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrEmpty(filter.Status) || x.Status == filter.Status) &&
                        (string.IsNullOrEmpty(filter.Decision) || x.Decision == filter.Decision))
            .Where(x => employees.TryGetValue(x.UserId, out var employee) &&
                        KeepEligibleExceptionDates(x, employee, month, end, lastCompletedDate))
            .OrderByDescending(x => x.CreatedDate)
            .ToList();

        foreach (var exception in all)
            if (employees.TryGetValue(exception.UserId, out var employee))
                exception.EmployeeName = $"{employee.FirstName} {employee.LastName}".Trim();

        return new Result<AttendancePayrollException>
        {
            MethodResults = all.Skip((filter.PageNo - 1) * filter.PageSize).Take(filter.PageSize).ToList(),
            TotalRecords = all.Count
        };
    }

    private static bool KeepEligibleExceptionDates(AttendancePayrollException exception, EmpUser employee, DateTime monthStart, DateTime monthEnd, DateTime lastCompletedDate)
    {
        if (!AttendancePayrollRules.TryGetEligiblePeriod(employee, monthStart, monthEnd, out var eligibleFrom, out var eligibleTo))
            return false;

        var reviewTo = eligibleTo.Date < lastCompletedDate ? eligibleTo.Date : lastCompletedDate;
        if (reviewTo < eligibleFrom.Date)
            return false;

        if (exception.AffectedDates.Count > 0)
        {
            exception.AffectedDates = exception.AffectedDates
                .Where(date => date.Date >= eligibleFrom.Date && date.Date <= reviewTo)
                .Distinct()
                .OrderBy(date => date)
                .ToList();
            return exception.AffectedDates.Count > 0;
        }

        return !exception.AttendanceDate.HasValue ||
               (exception.AttendanceDate.Value.Date >= eligibleFrom.Date && exception.AttendanceDate.Value.Date <= reviewTo);
    }

    public async Task<Result> Review(string companyId,string reviewerId,string id,AttendanceExceptionReviewDto dto)
    {var result=new Result();var item=await _exceptions.FirstOrDefault(x=>x.Id==id&&x.CompanyId==companyId);if(item==null){result.Message="Exception not found.";return result;}if(item.ExceptionType!="LHD_ED_LIMIT_EXCEEDED"){result.Message="Correct the underlying attendance record to resolve this exception.";return result;}if(item.Status!="PENDING_REVIEW"||item.Version!=dto.Version){result.Message="Exception was already reviewed or changed. Refresh and try again.";return result;}var allowed=new[]{"WAIVE","WARNING_ONLY","HALF_DAY_LOP","FULL_DAY_LOP","CUSTOM"};if(!allowed.Contains(dto.Decision)){result.Message="Invalid decision.";return result;}var fraction=dto.Decision switch{"HALF_DAY_LOP"=>.5m,"FULL_DAY_LOP"=>1m,"CUSTOM"=>dto.DeductionDayFraction??-1m,_=>0m};if(fraction<0||fraction>31){result.Message="Invalid deduction day fraction.";return result;}item.Decision=dto.Decision;item.DeductionDayFraction=fraction;item.Reason=dto.Reason.Trim();item.ReviewedBy=reviewerId;item.ReviewedAt=DateTime.UtcNow;item.Status=fraction>0?"DEDUCTION_APPROVED":"WAIVED";item.Version++;item.UpdatedBy=reviewerId;item.UpdatedDate=DateTime.UtcNow;var filter=Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id)&Builders<AttendancePayrollException>.Filter.Eq(x=>x.Version,dto.Version);return await _exceptions.Update(filter,item);}

    public async Task<Result<AttendanceMonthLockValidationResultDto>> ValidateAndLock(string companyId,string userId,DateTime month)
    {
        var start=new DateTime(month.Year,month.Month,1);var end=start.AddMonths(1).AddDays(-1);
        var result=new Result<AttendanceMonthLockValidationResultDto>{Success=false};
        var todayIst = IndiaTime.Today;
        var currentMonthStart = new DateTime(todayIst.Year, todayIst.Month, 1);
        if(start>currentMonthStart)
        {
            result.Message=$"{start:MMMM yyyy} is a future payroll month and cannot be locked.";
            return result;
        }
        var effectiveEnd = (start == currentMonthStart && todayIst < end) ? todayIst : end;
        await Recalculate(companyId,start);
        var rules=(await _statusSettings.GetAll(x=>x.CompanyId==companyId)).ToDictionary(x=>x.Code,StringComparer.OrdinalIgnoreCase);
        var weekly=await _weeklyOffs.FirstOrDefault(x=>x.CompanyId==companyId);
        var offDays=(weekly?.OffDays??[(int)DayOfWeek.Saturday,(int)DayOfWeek.Sunday]).ToHashSet();
        var endExclusive=end.Date.AddDays(1);
        // A ±1 day window tolerates a legacy row stored as an IST-midnight instant
        // converted to UTC; CalendarDateHelpers.MatchesDate makes the exact call.
        var holidays=(await _calendar.GetAll(x=>x.CompanyId==companyId&&x.Type==EnumsHelper.CalendarItem.Holiday&&(x.Recurring||(x.Date>=start.AddDays(-1)&&x.Date<endExclusive.AddDays(1))))).ToList();
        bool IsHoliday(DateTime d)=>holidays.Any(h => CalendarDateHelpers.MatchesDate(h, d));
        var summaries=new List<MonthlyAttendanceSummary>();
        var monthExceptions=(await _exceptions.GetAll(x=>x.CompanyId==companyId&&x.PayrollMonth==start)).ToList();
        foreach(var emp in (await _employees.GetAll(x=>x.CompanyId==companyId&&!x.IsDeleted))
            .Where(employee => AttendancePayrollRules.IsEligibleForPayrollMonth(employee, start)))
        {
            if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to))continue;
            var toExclusive=to.Date.AddDays(1);
            var records=(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).ToList();
            var daySegments=(await _segments.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).ToList();
            var segmentDates=daySegments.Select(x=>x.Date.Date).ToHashSet();
            var effectiveRecords=records.Where(x=>!segmentDates.Contains(x.Date.Date)).ToList();
            var reviewTo=to>effectiveEnd?effectiveEnd:to;
            var expectedDates=reviewTo<from?new List<DateTime>():Enumerable.Range(0,(reviewTo-from).Days+1).Select(i=>from.AddDays(i).Date).Where(d=>!offDays.Contains((int)d.DayOfWeek)&&!IsHoliday(d)&&EmploymentPeriod.IsAttendanceExpectedOn(emp,d)).ToList();
            var recordedDates=records.Select(x=>x.Date.Date).Concat(daySegments.Select(x=>x.Date.Date)).Distinct().ToHashSet();
            var missingDates=expectedDates.Where(x=>!recordedDates.Contains(x)).ToList();
            var missingCheckoutDates=records.Where(x=>rules.TryGetValue(x.Status,out var rule)&&rule.RequiresTime&&!x.CheckOutTime.HasValue).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>rules.TryGetValue(x.Status,out var rule)&&rule.RequiresTime&&!x.CheckOutTime.HasValue).Select(x=>x.Date.Date)).Distinct().ToList();
            var duplicateDates=records.GroupBy(x=>x.Date.Date).Where(x=>x.Count()>1).Select(x=>x.Key).ToList();
            var invalidTimeDates=records.Where(x=>x.CheckInTime.HasValue&&x.CheckOutTime.HasValue&&x.CheckOutTime<x.CheckInTime).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>x.CheckInTime.HasValue&&x.CheckOutTime.HasValue&&x.CheckOutTime<x.CheckInTime).Select(x=>x.Date.Date)).Distinct().ToList();
            var invalidStatusDates=records.Where(x=>!rules.TryGetValue(x.Status,out var rule)||!rule.IsActive).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>!rules.TryGetValue(x.Status,out var rule)||!rule.IsActive).Select(x=>x.Date.Date)).Distinct().ToList();
            var existing=await _summaries.FirstOrDefault(x=>x.CompanyId==companyId&&x.EmployeeId==emp.EmployeeId&&x.PayrollMonth==start);
            var summary=existing??new MonthlyAttendanceSummary{CompanyId=companyId,UserId=emp.UserId,EmployeeId=emp.EmployeeId,PayrollMonth=start};
            summary.EligibleFrom=from;summary.EligibleTo=to;summary.ExpectedWorkingDays=expectedDates.Count;summary.EligibleWorkingDays=expectedDates.Count;
            summary.MissingAttendanceDays=missingDates.Count;summary.MissingCheckoutDays=missingCheckoutDates.Count;
            var policy = await GetEffectiveEmployeePolicy(companyId, emp.UserId, end);
            var allowedLimit = policy?.CombinedLhdEdMonthlyLimit ?? 2;
            var infractions = effectiveRecords.Where(x => AttendancePayrollRules.IsPunctualityInfraction(x.Status))
                .Select(x => (x.Date.Date, x.Status))
                .Concat(daySegments.Where(x => AttendancePayrollRules.IsPunctualityInfraction(x.Status)).Select(x => (x.Date.Date, x.Status)));
            var graceDates = AttendancePayrollRules.ResolveGraceExemptDates(infractions, allowedLimit);

            summary.UnpaidDays = effectiveRecords.Sum(x =>
            {
                var rawUnpaid = rules.TryGetValue(x.Status, out var r) ? r.UnpaidDayFraction : 0m;
                return AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction(x.Status, graceDates.Contains(x.Date.Date), rawUnpaid);
            }) + daySegments.Sum(x =>
            {
                var rawUnpaid = rules.TryGetValue(x.Status, out var r) ? r.UnpaidDayFraction : 0m;
                return AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction(x.Status, graceDates.Contains(x.Date.Date), rawUnpaid);
            });
            var eligibleCalendarDays = (decimal)((to.Date - from.Date).Days + 1);
            var empException = monthExceptions.FirstOrDefault(x => x.EmployeeId == emp.EmployeeId);
            decimal approvedPenaltyDays = empException?.Status is "DEDUCTION_APPROVED" or "APPLIED_TO_PAYROLL" ? empException.DeductionDayFraction : 0m;
            summary.PaidDays = Math.Max(0m, eligibleCalendarDays - summary.UnpaidDays - approvedPenaltyDays);
            summary.PresentDays = effectiveRecords.Count(x => x.Status == "P" || ((x.Status is "LHD" or "LHD+ED") && graceDates.Contains(x.Date.Date)));
            summary.WfhDays = effectiveRecords.Count(x => x.Status is "WFH" or "WFH+WFO") + daySegments.Count(x => x.SourceType == "WFH_REQUEST") / 2m;
            summary.PaidLeaveDays = effectiveRecords.Where(x => rules.TryGetValue(x.Status, out var r) && r.PaidDayFraction > 0 && x.Status is not "P" and not "WFH" and not "WFH+WFO").Sum(x => rules.TryGetValue(x.Status, out var r) ? r.PaidDayFraction : 0) + daySegments.Where(x => x.SourceType is "LEAVE" or "LIFECYCLE_CASE").Sum(x => rules.TryGetValue(x.Status, out var r) ? r.PaidDayFraction : 0);
            summary.UnpaidLeaveDays = effectiveRecords.Where(x => rules.TryGetValue(x.Status, out var r) && r.UnpaidDayFraction > 0).Sum(x => rules.TryGetValue(x.Status, out var r) ? r.UnpaidDayFraction : 0) + daySegments.Where(x => x.SourceType is "LEAVE" or "LIFECYCLE_CASE").Sum(x => rules.TryGetValue(x.Status, out var r) ? r.UnpaidDayFraction : 0);
            summary.HalfDays = effectiveRecords.Count(x => (x.Status is "HD" or "CL-HALF" or "SL-HALF" or "WFH-HD") || ((x.Status is "LHD" or "LHD+ED") && !graceDates.Contains(x.Date.Date))) * .5m + daySegments.Count(x => !graceDates.Contains(x.Date.Date)) * .5m;
            summary.AbsentDays = effectiveRecords.Count(x => x.Status == "A");
            summary.LhdCount = records.Count(x => x.Status is "LHD" or "LHD+ED");
            summary.EdCount = records.Count(x => x.Status is "ED" or "LHD+ED");
            summary.CombinedLhdEdCount = summary.LhdCount + summary.EdCount;
            summary.BlockingExceptionCount=missingDates.Count+missingCheckoutDates.Count+duplicateDates.Count+invalidTimeDates.Count+invalidStatusDates.Count;
            if(existing==null)await _summaries.AddOne(summary);else if(!existing.IsLocked)await _summaries.Update(Builders<MonthlyAttendanceSummary>.Filter.Eq(x=>x.Id,existing.Id),summary);
            summaries.Add(summary);
        }
        // Only exceptions belonging to employees who were employed during this payroll
        // month may prevent the month from being locked. Historical/future-joiner rows
        // remain available for audit but must not block a payroll that cannot include them.
        var eligibleUserIds=summaries.Select(x=>x.UserId).Where(x=>!string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);
        var unresolved=(await _exceptions.GetAll(x=>x.CompanyId==companyId&&x.PayrollMonth==start&&x.Status=="PENDING_REVIEW"&&x.Severity=="BLOCKING"))
            .Where(x=>eligibleUserIds.Contains(x.UserId)).ToList();
        var blockingIssues = await GetBlockingIssues(companyId, start, end, unresolved);
        var blockedUserIds = unresolved.Select(x => x.UserId).ToHashSet(StringComparer.Ordinal);
        var lockedCount = 0;
        foreach(var s in summaries)
        {
            if (blockedUserIds.Contains(s.UserId)) continue;
            if (s.IsLocked) { lockedCount++; continue; }
            s.IsApproved = true;
            s.IsLocked = true;
            s.ApprovedBy = userId;
            s.ApprovedAt = DateTime.UtcNow;
            s.LockedBy = userId;
            s.LockedAt = DateTime.UtcNow;
            s.Version++;
            await _summaries.Update(Builders<MonthlyAttendanceSummary>.Filter.Eq(x => x.Id, s.Id), s);
            lockedCount++;
        }
        if(blockingIssues.Count>0)
        {
            result.Success = lockedCount > 0;
            result.Message = lockedCount > 0
                ? $"Locked attendance for {lockedCount} employee(s). {blockedUserIds.Count} employee(s) have {blockingIssues.Count} blocking issue(s) and remain unlocked."
                : $"Attendance cannot be locked because {blockingIssues.Count} blocking issue(s) need to be resolved.";
            result.MethodResult=new AttendanceMonthLockValidationResultDto { BlockingIssues=blockingIssues };
            return result;
        }
        result.Success=true;result.Message=$"Locked attendance for {summaries.Count} employee(s).";result.MethodResult=new AttendanceMonthLockValidationResultDto();return result;
    }

    public async Task<Result> UnlockMonth(string companyId, string userId, DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var result = new Result { Success = false };

        if (_payrolls is not null)
        {
            var finalized = await _payrolls.FirstOrDefault(x => x.CompanyId == companyId && x.PayMonth == start &&
                (x.PaymentStatus == "PAID" || x.WorkflowStatus == "CLOSED" || x.ClosedAt != null));
            if (finalized is not null)
            {
                result.Message = $"Cannot unlock attendance for {start:MMMM yyyy}: payroll has already been finalized or paid.";
                return result;
            }
        }

        var lockedSummaries = (await _summaries.GetAll(x => x.CompanyId == companyId && x.PayrollMonth == start && x.IsLocked)).ToList();
        if (lockedSummaries.Count == 0)
        {
            result.Message = $"{start:MMMM yyyy} attendance is not currently locked.";
            result.Success = true;
            return result;
        }

        foreach (var s in lockedSummaries)
        {
            s.IsLocked = false;
            s.IsApproved = false;
            s.Version++;
            s.UpdatedBy = userId;
            s.UpdatedDate = DateTime.UtcNow;
            await _summaries.Update(Builders<MonthlyAttendanceSummary>.Filter.Eq(x => x.Id, s.Id), s);
        }

        result.Success = true;
        result.Message = $"Successfully unlocked attendance for {start:MMMM yyyy} ({lockedSummaries.Count} employee records unlocked).";
        return result;
    }

    private async Task<List<AttendanceLockBlockingIssueDto>> GetBlockingIssues(string companyId, DateTime start, DateTime end, List<AttendancePayrollException> exceptions)
    {
        if (exceptions.Count == 0) return [];
        var userIds=exceptions.Select(x=>x.UserId).Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var employees=(await _employees.GetAll(x=>x.CompanyId==companyId&&userIds.Contains(x.UserId))).ToDictionary(x=>x.UserId,StringComparer.Ordinal);
        var endExclusive=end.Date.AddDays(1);
        var records=(await _attendance.GetAll(x=>x.CompanyId==companyId&&userIds.Contains(x.UserId)&&x.Date>=start&&x.Date<endExclusive)).ToList();
        var segments=(await _segments.GetAll(x=>x.CompanyId==companyId&&userIds.Contains(x.UserId)&&x.Date>=start&&x.Date<endExclusive)).ToList();
        var output=new List<AttendanceLockBlockingIssueDto>();
        foreach(var exception in exceptions)
        {
            if(!employees.TryGetValue(exception.UserId,out var employee)) continue;
            var dates=(exception.AffectedDates.Count>0?exception.AffectedDates:exception.AttendanceDate.HasValue?[exception.AttendanceDate.Value]:[])
                .Select(x=>x.Date).Where(x=>x>=start&&x<=end).Distinct().ToList();
            foreach(var date in dates)
            {
                var record=records.FirstOrDefault(x=>x.UserId==exception.UserId&&x.Date.Date==date);
                var segment=segments.FirstOrDefault(x=>x.UserId==exception.UserId&&x.Date.Date==date);
                var status=record?.Status??segment?.Status;
                output.Add(new AttendanceLockBlockingIssueDto
                {
                    EmployeeId=employee.EmployeeId, EmployeeCode=employee.EmployeeId, EmployeeName=$"{employee.FirstName} {employee.LastName}".Trim(),
                    AttendanceDate=date, ExceptionType=exception.ExceptionType, CurrentAttendanceStatus=status,
                    CheckInTime=record?.CheckInTime??segment?.CheckInTime, CheckOutTime=record?.CheckOutTime??segment?.CheckOutTime,
                    Message=GetBlockingIssueMessage(exception)
                });
            }
        }
        return output.OrderBy(x=>x.EmployeeName).ThenBy(x=>x.AttendanceDate).ThenBy(x=>x.ExceptionType).ToList();
    }

    private static string GetBlockingIssueMessage(AttendancePayrollException exception) => exception.ExceptionType switch
    {
        "MISSING_ATTENDANCE" => "Attendance has not been marked.",
        "MISSING_CHECKOUT" => "Check-out time is missing.",
        "DUPLICATE_ATTENDANCE" => "Multiple attendance records exist for this date.",
        "INVALID_TIME_ORDER" => "Check-out time must be later than check-in time.",
        "UNKNOWN_OR_INACTIVE_STATUS" => "Attendance status is invalid or inactive.",
        "LHD_ED_LIMIT_EXCEEDED" => $"Late Half Day and Early Departure occurrences exceed the allowed monthly limit of {exception.AllowedOccurrenceCount}.",
        _ => "Attendance requires review before this month can be locked."
    };

    public async Task<Result<AttendanceExceptionResolutionResultDto>> ResolveAttendanceException(string companyId, string reviewerId, string id, AttendanceExceptionResolutionDto dto)
    {
        var result = new Result<AttendanceExceptionResolutionResultDto> { Success = false };
        var exception = await _exceptions.FirstOrDefault(x => x.Id == id && x.CompanyId == companyId);
        if (exception is null) { result.Message = "Attendance exception was not found."; return result; }
        if (exception.Status != "PENDING_REVIEW" || exception.Version != dto.ExceptionVersion) { result.Message = "This exception was already changed. Refresh and try again."; return result; }

        var supportedTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "MISSING_ATTENDANCE", "MISSING_CHECKOUT", "INVALID_TIME_ORDER", "UNKNOWN_OR_INACTIVE_STATUS"
        };
        if (!supportedTypes.Contains(exception.ExceptionType))
        {
            result.Message = exception.ExceptionType == "DUPLICATE_ATTENDANCE"
                ? "Duplicate attendance requires the dedicated duplicate-resolution workflow."
                : "This exception must be resolved through its source workflow.";
            return result;
        }

        var date = DateTime.SpecifyKind(dto.AttendanceDate.Date, DateTimeKind.Utc);
        if (!exception.AffectedDates.Any(x => x.Date == date.Date)) { result.Message = "The selected date is not affected by this exception."; return result; }
        if (!TimeOnly.TryParse(dto.CheckInTime, out var checkIn) && !string.IsNullOrWhiteSpace(dto.CheckInTime)) { result.Message = "Check-in time must use HH:mm format."; return result; }
        if (!TimeOnly.TryParse(dto.CheckOutTime, out var checkOut) && !string.IsNullOrWhiteSpace(dto.CheckOutTime)) { result.Message = "Check-out time must use HH:mm format."; return result; }

        var existing = await _attendanceRepository.GetByUserAndDateAsync(companyId, exception.UserId, date);
        var hasTimes = !string.IsNullOrWhiteSpace(dto.CheckInTime) || !string.IsNullOrWhiteSpace(dto.CheckOutTime);
        var prepared = await _mutationValidator.PrepareAsync(new Codeji.CMS.Services.Attendance.AttendanceMutationRequest
        {
            CompanyId = companyId,
            ActorUserId = reviewerId,
            TargetUserId = exception.UserId,
            AttendanceDate = DateOnly.FromDateTime(date),
            StatusCode = dto.Status,
            TimingMode = hasTimes ? Codeji.CMS.Services.Attendance.AttendanceTimingMode.Custom : Codeji.CMS.Services.Attendance.AttendanceTimingMode.Auto,
            CheckInTime = string.IsNullOrWhiteSpace(dto.CheckInTime) ? null : checkIn,
            CheckOutTime = string.IsNullOrWhiteSpace(dto.CheckOutTime) ? null : checkOut,
            OperatorRemark = $"Monthly exception {exception.Id}: {dto.Reason.Trim()}",
            RemarkCode = "MONTHLY_EXCEPTION_RESOLUTION",
            SourceType = "ADMIN_MANUAL",
            PreserveRequestedStatus = true,
            ExpectedAttendanceVersion = existing is null ? null : dto.AttendanceVersion ?? existing.Version
        });
        if (!prepared.Success || prepared.MethodResult is null) { result.Message = prepared.Message ?? "Attendance correction was rejected."; return result; }

        var persisted = prepared.MethodResult.Existing is null
            ? await _attendanceRepository.AddAsync(prepared.MethodResult.Attendance)
            : await PersistExceptionCorrection(companyId, exception.UserId, date, prepared.MethodResult.Attendance);
        var action = prepared.MethodResult.Existing is null ? "CREATED_FROM_MONTHLY_EXCEPTION" : "CORRECTED_FROM_MONTHLY_EXCEPTION";
        await _auditWriter.WriteMutationAsync(companyId, reviewerId, action, persisted);

        // The stored exception is only resolved when the same detector no longer finds it.
        await Recalculate(companyId, exception.PayrollMonth);
        var refreshed = await _exceptions.FirstOrDefault(x => x.Id == exception.Id && x.CompanyId == companyId);
        var isResolved = refreshed?.Status == "RESOLVED";
        result.Success = true;
        result.MethodResult = new AttendanceExceptionResolutionResultDto
        {
            ExceptionId = exception.Id,
            AttendanceAction = action,
            IsResolved = isResolved,
            RemainingIssue = isResolved ? null : "Attendance was saved, but this exception still has unresolved affected date(s)."
        };
        result.Message = isResolved ? "Attendance corrected and the exception is resolved." : result.MethodResult.RemainingIssue;
        return result;
    }

    private async Task<AttendanceModel> PersistExceptionCorrection(string companyId, string userId, DateTime date, AttendanceModel attendance)
    {
        if (!await _attendanceRepository.UpdateAsync(companyId, userId, date, attendance))
            throw new InvalidOperationException("Attendance record could not be updated.");
        return attendance;
    }

    public async Task<AttendanceMonthLockStatusDto> GetMonthLockStatus(string companyId,DateTime month)
    {
        var start=new DateTime(month.Year,month.Month,1);
        var end=start.AddMonths(1).AddDays(-1);
        var currentMonthStart=new DateTime(DateTime.UtcNow.Year,DateTime.UtcNow.Month,1);
        var eligibleEmployeeIds=(await _employees.GetAll(x=>x.CompanyId==companyId&&x.Status&&!x.IsDeleted))
            .Where(x=>AttendancePayrollRules.TryGetEligiblePeriod(x,start,end,out _,out _))
            .Select(x=>x.EmployeeId)
            .Where(x=>!string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lockedEmployeeCount=0;
        if(start<currentMonthStart&&eligibleEmployeeIds.Count>0)
        {
            lockedEmployeeCount=(await _summaries.GetAll(x=>x.CompanyId==companyId&&x.PayrollMonth==start&&x.IsApproved&&x.IsLocked))
                .Select(x=>x.EmployeeId)
                .Where(eligibleEmployeeIds.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }
        return new AttendanceMonthLockStatusDto
        {
            PayrollMonth=start,
            EligibleEmployeeCount=eligibleEmployeeIds.Count,
            LockedEmployeeCount=lockedEmployeeCount,
            IsLocked=eligibleEmployeeIds.Count>0&&lockedEmployeeCount==eligibleEmployeeIds.Count
        };
    }

    private async Task RefreshValidationExceptions(string companyId, DateTime start, DateTime end)
    {
        var rules=(await _statusSettings.GetAll(x=>x.CompanyId==companyId)).ToDictionary(x=>x.Code,StringComparer.OrdinalIgnoreCase);
        var weekly=await _weeklyOffs.FirstOrDefault(x=>x.CompanyId==companyId);
        var offDays=(weekly?.OffDays??[(int)DayOfWeek.Saturday,(int)DayOfWeek.Sunday]).ToHashSet();
        var endExclusive=end.Date.AddDays(1);
        // A ±1 day window tolerates a legacy row stored as an IST-midnight instant
        // converted to UTC; CalendarDateHelpers.MatchesDate makes the exact call.
        var holidays=(await _calendar.GetAll(x=>x.CompanyId==companyId&&x.Type==EnumsHelper.CalendarItem.Holiday&&(x.Recurring||(x.Date>=start.AddDays(-1)&&x.Date<endExclusive.AddDays(1))))).ToList();
        bool IsHoliday(DateTime d)=>holidays.Any(h => CalendarDateHelpers.MatchesDate(h, d));

        var lastCompletedDate=CalendarDateHelpers.GetLastCompletedBusinessDate(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
        foreach(var emp in await _employees.GetAll(x=>x.CompanyId==companyId&&x.Status&&!x.IsDeleted))
        {
            if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to))
            {
                await ResolveIneligibleValidationExceptions(companyId, emp, start);
                continue;
            }
            if (_lifecycleAttendanceReconciliation is not null && _cases is not null)
            {
                var activeCases = (await _cases.GetAll(c => c.CompanyId == companyId && c.EmployeeUserId == emp.UserId && c.Status != "Cancelled" && c.EffectiveFromUtc <= end && (c.ExpectedReturnDate == null || c.ExpectedReturnDate >= start))).ToList();
                foreach (var c in activeCases.Where(c => c.LeaveReconciliationStatus != "Completed"))
                {
                    await _lifecycleAttendanceReconciliation.ReconcileCaseAttendanceAsync(companyId, c.CaseId, "system");
                }
            }
            var reviewTo=to.Date<lastCompletedDate?to.Date:lastCompletedDate;
            var records=reviewTo<from.Date?new List<AttendanceModel>():(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<reviewTo.AddDays(1))).ToList();
            var daySegments=reviewTo<from.Date?new List<AttendanceDaySegment>():(await _segments.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<reviewTo.AddDays(1))).ToList();
            var expectedDates=reviewTo<from.Date?new List<DateTime>():Enumerable.Range(0,(reviewTo-from.Date).Days+1).Select(i=>from.AddDays(i).Date).Where(d=>!offDays.Contains((int)d.DayOfWeek)&&!IsHoliday(d)&&EmploymentPeriod.IsAttendanceExpectedOn(emp,d)).ToList();
            var recordedDates=records.Select(x=>x.Date.Date).Concat(daySegments.Select(x=>x.Date.Date)).Distinct().ToHashSet();

            await UpsertValidationException(companyId,emp,start,"MISSING_ATTENDANCE",expectedDates.Where(x=>!recordedDates.Contains(x)).ToList());
            // WFH has its own correction workflow. A WFH row is not exceptional
            // when it is created on approval; it becomes one only after the
            // business day is complete and a required check-out is still absent.
            // Keep this source-linked so it can be corrected against the exact
            // WFH request/attendance row rather than a month-wide generic issue.
            var wfhMissingCheckout = records.Where(x => x.SourceType == "WFH_REQUEST" && rules.TryGetValue(x.Status, out var rule) && rule.RequiresTime && !x.CheckOutTime.HasValue).ToList();
            await UpsertWfhInsufficientHoursExceptions(companyId, emp, start, wfhMissingCheckout);
            var wfhSegmentMissingCheckout = daySegments.Where(x => x.SourceType == "WFH_REQUEST" && rules.TryGetValue(x.Status, out var rule) && rule.RequiresTime && !x.CheckOutTime.HasValue).ToList();
            await UpsertWfhInsufficientHoursExceptions(companyId, emp, start, wfhSegmentMissingCheckout);
            // WFH half-days are stored as source-owned segments.  They already have a
            // dedicated WFH_INSUFFICIENT_HOURS exception above, which sends the user
            // to the guarded WFH-hours correction flow.  Including them here creates
            // a second generic MISSING_CHECKOUT issue; its generic editor correctly
            // refuses to overwrite the WFH segment, but leaves HR with no actionable
            // path.  Only non-WFH records/segments belong to generic correction.
            await UpsertValidationException(companyId,emp,start,"MISSING_CHECKOUT",records.Where(x=>x.SourceType != "WFH_REQUEST"&&rules.TryGetValue(x.Status,out var rule)&&rule.RequiresTime&&!x.CheckOutTime.HasValue).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>x.SourceType != "WFH_REQUEST"&&rules.TryGetValue(x.Status,out var rule)&&rule.RequiresTime&&!x.CheckOutTime.HasValue).Select(x=>x.Date.Date)).Distinct().ToList());
            await UpsertValidationException(companyId,emp,start,"DUPLICATE_ATTENDANCE",records.GroupBy(x=>x.Date.Date).Where(x=>x.Count()>1).Select(x=>x.Key).ToList());
            await UpsertValidationException(companyId,emp,start,"INVALID_TIME_ORDER",records.Where(x=>x.CheckInTime.HasValue&&x.CheckOutTime.HasValue&&x.CheckOutTime<x.CheckInTime).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>x.CheckInTime.HasValue&&x.CheckOutTime.HasValue&&x.CheckOutTime<x.CheckInTime).Select(x=>x.Date.Date)).Distinct().ToList());
            await UpsertValidationException(companyId,emp,start,"UNKNOWN_OR_INACTIVE_STATUS",records.Where(x=>x.Status is not ("WO" or "H" or "N/A" or "NA") && (!rules.TryGetValue(x.Status,out var rule)||!rule.IsActive)).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>x.Status is not ("WO" or "H" or "N/A" or "NA") && (!rules.TryGetValue(x.Status,out var rule)||!rule.IsActive)).Select(x=>x.Date.Date)).Distinct().ToList());
        }
    }

    private async Task UpsertValidationException(string companyId,EmpUser employee,DateTime month,string type,List<DateTime> dates)
    {
        var item=await _exceptions.FirstOrDefault(x=>x.CompanyId==companyId&&x.UserId==employee.UserId&&x.EmployeeId==employee.EmployeeId&&x.PayrollMonth==month&&x.ExceptionType==type);
        if(dates.Count==0){if(item!=null&&item.Status=="PENDING_REVIEW"){item.Status="RESOLVED";item.Resolution="Underlying attendance issue corrected.";item.UpdatedDate=DateTime.UtcNow;item.Version++;await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id),item);}return;}
        if(item==null){item=new AttendancePayrollException{CompanyId=companyId,UserId=employee.UserId,EmployeeId=employee.EmployeeId,PayrollMonth=month,ExceptionType=type,Severity="BLOCKING",AffectedDates=dates};await _exceptions.AddOne(item);return;}
        if (item.Status is "WAIVED" || item.Decision is "HR_ADMIN_OVERRIDE")
        {
            item.AffectedDates = dates;
            item.UpdatedDate = DateTime.UtcNow;
            await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x => x.Id, item.Id), item);
            return;
        }
        item.AffectedDates=dates;item.Status="PENDING_REVIEW";item.Resolution=null;item.UpdatedDate=DateTime.UtcNow;item.Version++;await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id),item);
    }

    public async Task<Result> OverrideException(string companyId, string reviewerId, string id, AttendanceExceptionOverrideDto dto)
    {
        var item = await _exceptions.FirstOrDefault(x => x.Id == id && x.CompanyId == companyId);
        if (item is null) return new Result { Success = false, Message = "Exception not found in the current company." };
        if (item.Status != "PENDING_REVIEW" || item.Version != dto.ExceptionVersion)
            return new Result { Success = false, Message = "This exception has changed. Refresh the exception list and review the latest details." };

        item.Status = "WAIVED";
        item.Decision = "HR_ADMIN_OVERRIDE";
        item.Resolution = $"HR/Admin override: {dto.Reason.Trim()}";
        item.Reason = dto.Reason.Trim();
        item.ReviewedBy = reviewerId;
        item.ReviewedAt = DateTime.UtcNow;
        item.UpdatedBy = reviewerId;
        item.UpdatedDate = DateTime.UtcNow;
        item.Version++;
        var saved = await _exceptions.Update(Builders<AttendancePayrollException>.Filter.And(
            Builders<AttendancePayrollException>.Filter.Eq(x => x.Id, id),
            Builders<AttendancePayrollException>.Filter.Eq(x => x.CompanyId, companyId),
            Builders<AttendancePayrollException>.Filter.Eq(x => x.Status, "PENDING_REVIEW"),
            Builders<AttendancePayrollException>.Filter.Eq(x => x.Version, dto.ExceptionVersion)), item);
        return saved.Success ? new Result { Success = true, Message = "Exception overridden by HR/Admin and no longer blocks payroll." } : new Result { Success = false, Message = "The exception changed before the override could be saved." };
    }

    private async Task UpsertWfhInsufficientHoursExceptions(string companyId, EmpUser employee, DateTime month, IEnumerable<AttendanceModel> rows)
    {
        foreach (var row in rows)
        {
            var existing = await _exceptions.FirstOrDefault(x =>
                x.CompanyId == companyId && x.UserId == employee.UserId && x.EmployeeId == employee.EmployeeId &&
                x.PayrollMonth == month && x.ExceptionType == "WFH_INSUFFICIENT_HOURS" &&
                x.AttendanceDate == row.Date.Date && x.LeaveRequestId == row.SourceId);

            if (existing is not null)
                continue;

            await _exceptions.AddOne(new AttendancePayrollException
            {
                CompanyId = companyId,
                UserId = employee.UserId,
                EmployeeId = employee.EmployeeId,
                PayrollMonth = month,
                AttendanceDate = row.Date.Date,
                AffectedDates = [row.Date.Date],
                ExceptionType = "WFH_INSUFFICIENT_HOURS",
                Severity = "BLOCKING",
                Status = "PENDING_REVIEW",
                LeaveRequestId = row.SourceId,
                ExistingAttendanceId = row.AttendanceId,
                ExistingStatus = row.Status,
                SourceVersion = row.SourceVersion,
                Reason = "Approved WFH attendance is missing a required check-out after the attendance day completed."
            });
        }
    }

    // Half-day WFH is represented by AttendanceDaySegment rather than a daily
    // AttendanceModel.  Create the same source-linked exception so HR/Admin
    // can correct the exact WFH half instead of receiving a generic checkout
    // error that has no actionable WFH request attached.
    private async Task UpsertWfhInsufficientHoursExceptions(string companyId, EmpUser employee, DateTime month, IEnumerable<AttendanceDaySegment> segments)
    {
        foreach (var segment in segments)
        {
            var existing = await _exceptions.FirstOrDefault(x =>
                x.CompanyId == companyId && x.UserId == employee.UserId && x.EmployeeId == employee.EmployeeId &&
                x.PayrollMonth == month && x.ExceptionType == "WFH_INSUFFICIENT_HOURS" &&
                x.AttendanceDate == segment.Date.Date && x.LeaveRequestId == segment.SourceId &&
                x.ExistingAttendanceId == segment.AttendanceDaySegmentId);
            if (existing is not null) continue;

            await _exceptions.AddOne(new AttendancePayrollException
            {
                CompanyId = companyId,
                UserId = employee.UserId,
                EmployeeId = employee.EmployeeId,
                PayrollMonth = month,
                AttendanceDate = segment.Date.Date,
                AffectedDates = [segment.Date.Date],
                ExceptionType = "WFH_INSUFFICIENT_HOURS",
                Severity = "BLOCKING",
                Status = "PENDING_REVIEW",
                LeaveRequestId = segment.SourceId,
                ExistingAttendanceId = segment.AttendanceDaySegmentId,
                ExistingStatus = segment.Status,
                SourceVersion = segment.SourceVersion,
                Reason = $"Approved {segment.Segment.Replace('_', ' ').ToLowerInvariant()} WFH attendance is missing a required check-out after the attendance day completed."
            });
        }
    }

    private async Task ResolveIneligibleValidationExceptions(string companyId, EmpUser employee, DateTime month)
    {
        var validationTypes = new[] { "MISSING_ATTENDANCE", "MISSING_CHECKOUT", "DUPLICATE_ATTENDANCE", "INVALID_TIME_ORDER", "UNKNOWN_OR_INACTIVE_STATUS" };
        var stale = await _exceptions.GetAll(x =>
            x.CompanyId == companyId &&
            x.UserId == employee.UserId &&
            x.EmployeeId == employee.EmployeeId &&
            x.PayrollMonth == month &&
            x.Status == "PENDING_REVIEW");

        foreach (var item in stale.Where(x => validationTypes.Contains(x.ExceptionType, StringComparer.Ordinal)))
        {
            item.Status = "RESOLVED";
            item.Resolution = "Employee is not eligible for this attendance month.";
            item.UpdatedDate = DateTime.UtcNow;
            item.Version++;
            await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x => x.Id, item.Id), item);
        }
    }

}
