using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using MongoDB.Driver;

public interface IAttendancePenaltyService
{
    Task<AttendancePenaltyPolicyDto> GetPolicy(string companyId, DateTime? effectiveOn = null);
    Task<Result> SavePolicy(string companyId, string userId, AttendancePenaltyPolicyDto dto);
    Task<Result<AttendancePayrollException>> Recalculate(string companyId, DateTime month);
    Task<Result<AttendancePayrollException>> GetExceptions(string companyId, AttendanceExceptionFilterDto filter);
    Task<Result> Review(string companyId, string reviewerId, string id, AttendanceExceptionReviewDto dto);
    Task<Result<AttendanceExceptionResolutionResultDto>> ResolveAttendanceException(string companyId, string reviewerId, string id, AttendanceExceptionResolutionDto dto);
    Task<AttendanceMonthLockStatusDto> GetMonthLockStatus(string companyId, DateTime month);
    Task<Result<AttendanceMonthLockValidationResultDto>> ValidateAndLock(string companyId, string userId, DateTime month);
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
    public AttendancePenaltyService(IMongoDbRepository<AttendancePenaltyPolicy> policies, IMongoDbRepository<AttendancePayrollException> exceptions, IMongoDbRepository<MonthlyAttendanceSummary> summaries, IMongoDbRepository<AttendanceModel> attendance, IMongoDbRepository<AttendanceDaySegment> segments, IMongoDbRepository<AttendanceStatusSetting> statusSettings, IMongoDbRepository<EmpUser> employees, IMongoDbRepository<WeeklyOffSetting> weeklyOffs, IMongoDbRepository<CalendarEntity> calendar, IMongoDbRepository<AttendanceNotificationOutbox> notificationOutbox, IAttendanceRepository attendanceRepository, IAttendanceMutationValidator mutationValidator, IAttendanceAuditWriter auditWriter)
    { _policies=policies; _exceptions=exceptions; _summaries=summaries; _attendance=attendance; _segments=segments; _statusSettings=statusSettings; _employees=employees; _weeklyOffs=weeklyOffs; _calendar=calendar; _notificationOutbox=notificationOutbox; _attendanceRepository=attendanceRepository; _mutationValidator=mutationValidator; _auditWriter=auditWriter; }

    public async Task<AttendancePenaltyPolicyDto> GetPolicy(string companyId, DateTime? effectiveOn=null)
    {
        var on=(effectiveOn ?? DateTime.UtcNow).Date;
        var policy=(await _policies.GetAll(x=>x.CompanyId==companyId && x.IsEnabled && x.EffectiveFrom<=on && (!x.EffectiveTo.HasValue || x.EffectiveTo>=on))).OrderByDescending(x=>x.Version).FirstOrDefault();
        // Loading the settings screen must not write to the database.  Apart from
        // making a GET unexpectedly fail for a new company, concurrent page loads
        // could race on the unique company/version index.  The returned defaults
        // are persisted only when the administrator saves the policy.
        if(policy==null) policy=new AttendancePenaltyPolicy{CompanyId=companyId,EffectiveFrom=new DateTime(on.Year,on.Month,1)};
        return new AttendancePenaltyPolicyDto{Id=policy.Id,Name=policy.Name,CombinedLhdEdMonthlyLimit=policy.CombinedLhdEdMonthlyLimit,IsEnabled=policy.IsEnabled,RequiresHrApproval=policy.RequiresHrApproval,DefaultDecision=policy.DefaultDecision,EffectiveFrom=policy.EffectiveFrom,EffectiveTo=policy.EffectiveTo,Version=policy.Version};
    }

    public async Task<Result> SavePolicy(string companyId,string userId,AttendancePenaltyPolicyDto dto)
    {
        var result=new Result();
        if(dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date<dto.EffectiveFrom.Date){result.Message="Effective-to date must be on or after effective-from date.";return result;}
        var versions=(await _policies.GetAll(x=>x.CompanyId==companyId)).OrderByDescending(x=>x.Version).ToList();
        var previous=versions.FirstOrDefault(x=>!x.EffectiveTo.HasValue && x.EffectiveFrom<dto.EffectiveFrom.Date);
        if(previous!=null){previous.EffectiveTo=dto.EffectiveFrom.Date.AddDays(-1);previous.UpdatedBy=userId;previous.UpdatedDate=DateTime.UtcNow;await _policies.Update(Builders<AttendancePenaltyPolicy>.Filter.Eq(x=>x.Id,previous.Id),previous);}
        // Automatic financial decisions are intentionally unsupported: every exceeded-limit
        // penalty must remain auditable and require an explicit HR/Admin review.
        var policy=new AttendancePenaltyPolicy{CompanyId=companyId,Name=dto.Name.Trim(),CombinedLhdEdMonthlyLimit=dto.CombinedLhdEdMonthlyLimit,IsEnabled=dto.IsEnabled,RequiresHrApproval=true,DefaultDecision="REVIEW_REQUIRED",EffectiveFrom=dto.EffectiveFrom.Date,EffectiveTo=dto.EffectiveTo?.Date,Version=(versions.FirstOrDefault()?.Version??0)+1,CreatedBy=userId};
        result=await _policies.AddOne(policy);return result;
    }

    public async Task<Result<AttendancePayrollException>> Recalculate(string companyId,DateTime month)
    {
        var start=new DateTime(month.Year,month.Month,1);var end=start.AddMonths(1).AddDays(-1);var policy=await GetPolicy(companyId,end);
        var employees=(await _employees.GetAll(x=>x.CompanyId==companyId)).ToList();var output=new List<AttendancePayrollException>();
        foreach(var emp in employees){if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to))continue;var toExclusive=to.Date.AddDays(1);var records=(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).Where(a=>a.Status is "LHD" or "ED" or "LHD+ED").ToList();var lhd=records.Count(x=>x.Status is "LHD" or "LHD+ED");var ed=records.Count(x=>x.Status is "ED" or "LHD+ED");var combined=lhd+ed;var exceeded=AttendancePayrollRules.ExceededOccurrences(lhd,ed,policy.CombinedLhdEdMonthlyLimit);var existing=await _exceptions.FirstOrDefault(x=>x.CompanyId==companyId&&x.UserId==emp.UserId&&x.EmployeeId==emp.EmployeeId&&x.PayrollMonth==start&&x.ExceptionType=="LHD_ED_LIMIT_EXCEEDED");
            if(exceeded==0){if(existing!=null&&existing.Status=="PENDING_REVIEW"){existing.Status="CANCELLED";existing.UpdatedDate=DateTime.UtcNow;await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,existing.Id),existing);}continue;}
            var item=existing??new AttendancePayrollException{CompanyId=companyId,UserId=emp.UserId,EmployeeId=emp.EmployeeId,PayrollMonth=start,PolicyId=policy.Id!,PolicyVersion=policy.Version};item.LhdCount=lhd;item.EdCount=ed;item.CombinedOccurrenceCount=combined;item.AllowedOccurrenceCount=policy.CombinedLhdEdMonthlyLimit;item.ExceededOccurrenceCount=exceeded;item.AffectedAttendanceRecordIds=records.Select(x=>x.AttendanceId!).Where(x=>x!=null).ToList();item.AffectedDates=records.Select(x=>x.Date.Date).Distinct().ToList();
            if(existing==null)
            {
                await _exceptions.AddOne(item);
                await QueueMonthlyLhdEdWarning(companyId, emp.UserId, start, policy.CombinedLhdEdMonthlyLimit, combined);
            }
            else if(existing.Status=="PENDING_REVIEW"){item.Version++;await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id),item);}output.Add(item);
        }
        // Recalculate is the single refresh operation used by the Attendance UI.
        // Keep structural validation exceptions in sync as well as LHD/ED penalties,
        // otherwise repaired duplicates/checkouts remain incorrectly pending.
        await RefreshValidationExceptions(companyId, start, end);
        return new Result<AttendancePayrollException>{MethodResults=output,TotalRecords=output.Count};
    }

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
    {var month=new DateTime(filter.PayrollMonth.Year,filter.PayrollMonth.Month,1);var all=(await _exceptions.GetAll(x=>x.CompanyId==companyId&&x.PayrollMonth==month)).Where(x=>(string.IsNullOrEmpty(filter.EmployeeId)||x.EmployeeId.Contains(filter.EmployeeId,StringComparison.OrdinalIgnoreCase))&&(string.IsNullOrEmpty(filter.Status)||x.Status==filter.Status)&&(string.IsNullOrEmpty(filter.Decision)||x.Decision==filter.Decision)).OrderByDescending(x=>x.CreatedDate).ToList();return new Result<AttendancePayrollException>{MethodResults=all.Skip((filter.PageNo-1)*filter.PageSize).Take(filter.PageSize).ToList(),TotalRecords=all.Count};}

    public async Task<Result> Review(string companyId,string reviewerId,string id,AttendanceExceptionReviewDto dto)
    {var result=new Result();var item=await _exceptions.FirstOrDefault(x=>x.Id==id&&x.CompanyId==companyId);if(item==null){result.Message="Exception not found.";return result;}if(item.ExceptionType!="LHD_ED_LIMIT_EXCEEDED"){result.Message="Correct the underlying attendance record to resolve this exception.";return result;}if(item.Status!="PENDING_REVIEW"||item.Version!=dto.Version){result.Message="Exception was already reviewed or changed. Refresh and try again.";return result;}var allowed=new[]{"WAIVE","WARNING_ONLY","HALF_DAY_LOP","FULL_DAY_LOP","CUSTOM"};if(!allowed.Contains(dto.Decision)){result.Message="Invalid decision.";return result;}var fraction=dto.Decision switch{"HALF_DAY_LOP"=>.5m,"FULL_DAY_LOP"=>1m,"CUSTOM"=>dto.DeductionDayFraction??-1m,_=>0m};if(fraction<0||fraction>31){result.Message="Invalid deduction day fraction.";return result;}item.Decision=dto.Decision;item.DeductionDayFraction=fraction;item.Reason=dto.Reason.Trim();item.ReviewedBy=reviewerId;item.ReviewedAt=DateTime.UtcNow;item.Status=fraction>0?"DEDUCTION_APPROVED":"WAIVED";item.Version++;item.UpdatedBy=reviewerId;item.UpdatedDate=DateTime.UtcNow;var filter=Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id)&Builders<AttendancePayrollException>.Filter.Eq(x=>x.Version,dto.Version);return await _exceptions.Update(filter,item);}

    public async Task<Result<AttendanceMonthLockValidationResultDto>> ValidateAndLock(string companyId,string userId,DateTime month)
    {
        var start=new DateTime(month.Year,month.Month,1);var end=start.AddMonths(1).AddDays(-1);
        var result=new Result<AttendanceMonthLockValidationResultDto>{Success=false};
        var currentMonthStart=new DateTime(DateTime.UtcNow.Year,DateTime.UtcNow.Month,1);
        if(start>=currentMonthStart)
        {
            result.Message=start==currentMonthStart
                ? $"{start:MMMM yyyy} is still in progress. Lock attendance after {end:MMMM d, yyyy}."
                : $"{start:MMMM yyyy} is a future payroll month and cannot be locked.";
            return result;
        }
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
        foreach(var emp in await _employees.GetAll(x=>x.CompanyId==companyId&&x.Status&&!x.IsDeleted))
        {
            if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to))continue;
            var toExclusive=to.Date.AddDays(1);
            var records=(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).ToList();
            var daySegments=(await _segments.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).ToList();
            var segmentDates=daySegments.Select(x=>x.Date.Date).ToHashSet();
            var effectiveRecords=records.Where(x=>!segmentDates.Contains(x.Date.Date)).ToList();
            var expectedDates=Enumerable.Range(0,(to-from).Days+1).Select(i=>from.AddDays(i).Date).Where(d=>!offDays.Contains((int)d.DayOfWeek)&&!IsHoliday(d)).ToList();
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
            summary.PaidDays=effectiveRecords.Sum(x=>rules.TryGetValue(x.Status,out var r)?r.PaidDayFraction:0)+daySegments.Sum(x=>rules.TryGetValue(x.Status,out var r)?r.PaidDayFraction:0);summary.UnpaidDays=effectiveRecords.Sum(x=>rules.TryGetValue(x.Status,out var r)?r.UnpaidDayFraction:0)+daySegments.Sum(x=>rules.TryGetValue(x.Status,out var r)?r.UnpaidDayFraction:0);
            summary.PresentDays=effectiveRecords.Count(x=>x.Status=="P");summary.WfhDays=effectiveRecords.Count(x=>x.Status is "WFH" or "WFH+WFO")+daySegments.Count(x=>x.SourceType=="WFH_REQUEST")/2m;
            summary.PaidLeaveDays=effectiveRecords.Where(x=>x.Status is "SL" or "CL" or "EL" or "COMP-OFF").Sum(x=>rules.TryGetValue(x.Status,out var r)?r.PaidDayFraction:0)+daySegments.Where(x=>x.SourceType=="LEAVE").Sum(x=>rules.TryGetValue(x.Status,out var r)?r.PaidDayFraction:0);
            summary.UnpaidLeaveDays=effectiveRecords.Where(x=>x.Status=="A").Sum(x=>rules.TryGetValue(x.Status,out var r)?r.UnpaidDayFraction:1)+daySegments.Where(x=>x.SourceType=="LEAVE").Sum(x=>rules.TryGetValue(x.Status,out var r)?r.UnpaidDayFraction:0);
            summary.HalfDays=effectiveRecords.Count(x=>x.Status is "HD" or "LHD" or "LHD+ED" or "CL-HALF" or "SL-HALF" or "WFH-HD")*.5m+daySegments.Count*.5m;summary.AbsentDays=effectiveRecords.Count(x=>x.Status=="A");
            summary.LhdCount=records.Count(x=>x.Status is "LHD" or "LHD+ED");summary.EdCount=records.Count(x=>x.Status is "ED" or "LHD+ED");summary.CombinedLhdEdCount=summary.LhdCount+summary.EdCount;
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
        if(blockingIssues.Count>0)
        {
            result.Message=$"Attendance cannot be locked because {blockingIssues.Count} blocking issue(s) need to be resolved.";
            result.MethodResult=new AttendanceMonthLockValidationResultDto { BlockingIssues=blockingIssues };
            return result;
        }
        foreach(var s in summaries){if(s.IsLocked)continue;s.IsApproved=true;s.IsLocked=true;s.ApprovedBy=userId;s.ApprovedAt=DateTime.UtcNow;s.LockedBy=userId;s.LockedAt=DateTime.UtcNow;s.Version++;await _summaries.Update(Builders<MonthlyAttendanceSummary>.Filter.Eq(x=>x.Id,s.Id),s);}
        result.Success=true;result.Message=$"Locked attendance for {summaries.Count} employee(s).";result.MethodResult=new AttendanceMonthLockValidationResultDto();return result;
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

        foreach(var emp in await _employees.GetAll(x=>x.CompanyId==companyId&&x.Status&&!x.IsDeleted))
        {
            if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to))continue;
            var toExclusive=to.Date.AddDays(1);
            var records=(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).ToList();
            var daySegments=(await _segments.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).ToList();
            var expectedDates=Enumerable.Range(0,(to-from).Days+1).Select(i=>from.AddDays(i).Date).Where(d=>!offDays.Contains((int)d.DayOfWeek)&&!IsHoliday(d)).ToList();
            var recordedDates=records.Select(x=>x.Date.Date).Concat(daySegments.Select(x=>x.Date.Date)).Distinct().ToHashSet();

            await UpsertValidationException(companyId,emp,start,"MISSING_ATTENDANCE",expectedDates.Where(x=>!recordedDates.Contains(x)).ToList());
            await UpsertValidationException(companyId,emp,start,"MISSING_CHECKOUT",records.Where(x=>rules.TryGetValue(x.Status,out var rule)&&rule.RequiresTime&&!x.CheckOutTime.HasValue).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>rules.TryGetValue(x.Status,out var rule)&&rule.RequiresTime&&!x.CheckOutTime.HasValue).Select(x=>x.Date.Date)).Distinct().ToList());
            await UpsertValidationException(companyId,emp,start,"DUPLICATE_ATTENDANCE",records.GroupBy(x=>x.Date.Date).Where(x=>x.Count()>1).Select(x=>x.Key).ToList());
            await UpsertValidationException(companyId,emp,start,"INVALID_TIME_ORDER",records.Where(x=>x.CheckInTime.HasValue&&x.CheckOutTime.HasValue&&x.CheckOutTime<x.CheckInTime).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>x.CheckInTime.HasValue&&x.CheckOutTime.HasValue&&x.CheckOutTime<x.CheckInTime).Select(x=>x.Date.Date)).Distinct().ToList());
            await UpsertValidationException(companyId,emp,start,"UNKNOWN_OR_INACTIVE_STATUS",records.Where(x=>!rules.TryGetValue(x.Status,out var rule)||!rule.IsActive).Select(x=>x.Date.Date).Concat(daySegments.Where(x=>!rules.TryGetValue(x.Status,out var rule)||!rule.IsActive).Select(x=>x.Date.Date)).Distinct().ToList());
        }
    }

    private async Task UpsertValidationException(string companyId,EmpUser employee,DateTime month,string type,List<DateTime> dates)
    {
        var item=await _exceptions.FirstOrDefault(x=>x.CompanyId==companyId&&x.UserId==employee.UserId&&x.EmployeeId==employee.EmployeeId&&x.PayrollMonth==month&&x.ExceptionType==type);
        if(dates.Count==0){if(item!=null&&item.Status=="PENDING_REVIEW"){item.Status="RESOLVED";item.Resolution="Underlying attendance issue corrected.";item.UpdatedDate=DateTime.UtcNow;item.Version++;await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id),item);}return;}
        if(item==null){item=new AttendancePayrollException{CompanyId=companyId,UserId=employee.UserId,EmployeeId=employee.EmployeeId,PayrollMonth=month,ExceptionType=type,Severity="BLOCKING",AffectedDates=dates};await _exceptions.AddOne(item);return;}
        item.AffectedDates=dates;item.Status="PENDING_REVIEW";item.Resolution=null;item.UpdatedDate=DateTime.UtcNow;item.Version++;await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id),item);
    }

}
