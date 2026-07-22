using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;

public interface IAttendancePenaltyService
{
    Task<AttendancePenaltyPolicyDto> GetPolicy(string companyId, DateTime? effectiveOn = null);
    Task<Result> SavePolicy(string companyId, string userId, AttendancePenaltyPolicyDto dto);
    Task<Result<AttendancePayrollException>> Recalculate(string companyId, DateTime month);
    Task<Result<AttendancePayrollException>> GetExceptions(string companyId, AttendanceExceptionFilterDto filter);
    Task<Result> Review(string companyId, string reviewerId, string id, AttendanceExceptionReviewDto dto);
    Task<AttendanceMonthLockStatusDto> GetMonthLockStatus(string companyId, DateTime month);
    Task<Result<MonthlyAttendanceSummary>> ValidateAndLock(string companyId, string userId, DateTime month);
}

public class AttendancePenaltyService : IAttendancePenaltyService
{
    private readonly IMongoDbRepository<AttendancePenaltyPolicy> _policies;
    private readonly IMongoDbRepository<AttendancePayrollException> _exceptions;
    private readonly IMongoDbRepository<MonthlyAttendanceSummary> _summaries;
    private readonly IMongoDbRepository<AttendanceModel> _attendance;
    private readonly IMongoDbRepository<AttendanceStatusSetting> _statusSettings;
    private readonly IMongoDbRepository<EmpUser> _employees;
    private readonly IMongoDbRepository<WeeklyOffSetting> _weeklyOffs;
    private readonly IMongoDbRepository<CalendarEntity> _calendar;
    public AttendancePenaltyService(IMongoDbRepository<AttendancePenaltyPolicy> policies, IMongoDbRepository<AttendancePayrollException> exceptions, IMongoDbRepository<MonthlyAttendanceSummary> summaries, IMongoDbRepository<AttendanceModel> attendance, IMongoDbRepository<AttendanceStatusSetting> statusSettings, IMongoDbRepository<EmpUser> employees, IMongoDbRepository<WeeklyOffSetting> weeklyOffs, IMongoDbRepository<CalendarEntity> calendar)
    { _policies=policies; _exceptions=exceptions; _summaries=summaries; _attendance=attendance; _statusSettings=statusSettings; _employees=employees; _weeklyOffs=weeklyOffs; _calendar=calendar; }

    public async Task<AttendancePenaltyPolicyDto> GetPolicy(string companyId, DateTime? effectiveOn=null)
    {
        var on=(effectiveOn ?? DateTime.UtcNow).Date;
        var policy=(await _policies.GetAll(x=>x.CompanyId==companyId && x.IsEnabled && x.EffectiveFrom<=on && (!x.EffectiveTo.HasValue || x.EffectiveTo>=on))).OrderByDescending(x=>x.Version).FirstOrDefault();
        if(policy==null){ policy=new AttendancePenaltyPolicy{CompanyId=companyId,EffectiveFrom=new DateTime(on.Year,on.Month,1)}; await _policies.AddOne(policy); }
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
        foreach(var emp in employees){if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to))continue;var toExclusive=to.Date.AddDays(1);var records=(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).Where(a=>a.Status is "LHD" or "ED").ToList();var lhd=records.Count(x=>x.Status=="LHD");var ed=records.Count(x=>x.Status=="ED");var combined=lhd+ed;var exceeded=AttendancePayrollRules.ExceededOccurrences(lhd,ed,policy.CombinedLhdEdMonthlyLimit);var existing=await _exceptions.FirstOrDefault(x=>x.CompanyId==companyId&&x.UserId==emp.UserId&&x.EmployeeId==emp.EmployeeId&&x.PayrollMonth==start&&x.ExceptionType=="LHD_ED_LIMIT_EXCEEDED");
            if(exceeded==0){if(existing!=null&&existing.Status=="PENDING_REVIEW"){existing.Status="CANCELLED";existing.UpdatedDate=DateTime.UtcNow;await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,existing.Id),existing);}continue;}
            var item=existing??new AttendancePayrollException{CompanyId=companyId,UserId=emp.UserId,EmployeeId=emp.EmployeeId,PayrollMonth=start,PolicyId=policy.Id!,PolicyVersion=policy.Version};item.LhdCount=lhd;item.EdCount=ed;item.CombinedOccurrenceCount=combined;item.AllowedOccurrenceCount=policy.CombinedLhdEdMonthlyLimit;item.ExceededOccurrenceCount=exceeded;item.AffectedAttendanceRecordIds=records.Select(x=>x.AttendanceId!).Where(x=>x!=null).ToList();
            if(existing==null)await _exceptions.AddOne(item);else if(existing.Status=="PENDING_REVIEW"){item.Version++;await _exceptions.Update(Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id),item);}output.Add(item);
        }
        // Recalculate is the single refresh operation used by the Attendance UI.
        // Keep structural validation exceptions in sync as well as LHD/ED penalties,
        // otherwise repaired duplicates/checkouts remain incorrectly pending.
        await RefreshValidationExceptions(companyId, start, end);
        return new Result<AttendancePayrollException>{MethodResults=output,TotalRecords=output.Count};
    }

    public async Task<Result<AttendancePayrollException>> GetExceptions(string companyId,AttendanceExceptionFilterDto filter)
    {var month=new DateTime(filter.PayrollMonth.Year,filter.PayrollMonth.Month,1);var all=(await _exceptions.GetAll(x=>x.CompanyId==companyId&&x.PayrollMonth==month)).Where(x=>(string.IsNullOrEmpty(filter.EmployeeId)||x.EmployeeId.Contains(filter.EmployeeId,StringComparison.OrdinalIgnoreCase))&&(string.IsNullOrEmpty(filter.Status)||x.Status==filter.Status)&&(string.IsNullOrEmpty(filter.Decision)||x.Decision==filter.Decision)).OrderByDescending(x=>x.CreatedDate).ToList();return new Result<AttendancePayrollException>{MethodResults=all.Skip((filter.PageNo-1)*filter.PageSize).Take(filter.PageSize).ToList(),TotalRecords=all.Count};}

    public async Task<Result> Review(string companyId,string reviewerId,string id,AttendanceExceptionReviewDto dto)
    {var result=new Result();var item=await _exceptions.FirstOrDefault(x=>x.Id==id&&x.CompanyId==companyId);if(item==null){result.Message="Exception not found.";return result;}if(item.ExceptionType!="LHD_ED_LIMIT_EXCEEDED"){result.Message="Correct the underlying attendance record to resolve this exception.";return result;}if(item.Status!="PENDING_REVIEW"||item.Version!=dto.Version){result.Message="Exception was already reviewed or changed. Refresh and try again.";return result;}var allowed=new[]{"WAIVE","WARNING_ONLY","HALF_DAY_LOP","FULL_DAY_LOP","CUSTOM"};if(!allowed.Contains(dto.Decision)){result.Message="Invalid decision.";return result;}var fraction=dto.Decision switch{"HALF_DAY_LOP"=>.5m,"FULL_DAY_LOP"=>1m,"CUSTOM"=>dto.DeductionDayFraction??-1m,_=>0m};if(fraction<0||fraction>31){result.Message="Invalid deduction day fraction.";return result;}item.Decision=dto.Decision;item.DeductionDayFraction=fraction;item.Reason=dto.Reason.Trim();item.ReviewedBy=reviewerId;item.ReviewedAt=DateTime.UtcNow;item.Status=fraction>0?"DEDUCTION_APPROVED":"WAIVED";item.Version++;item.UpdatedBy=reviewerId;item.UpdatedDate=DateTime.UtcNow;var filter=Builders<AttendancePayrollException>.Filter.Eq(x=>x.Id,item.Id)&Builders<AttendancePayrollException>.Filter.Eq(x=>x.Version,dto.Version);return await _exceptions.Update(filter,item);}

    public async Task<Result<MonthlyAttendanceSummary>> ValidateAndLock(string companyId,string userId,DateTime month)
    {
        var start=new DateTime(month.Year,month.Month,1);var end=start.AddMonths(1).AddDays(-1);
        var result=new Result<MonthlyAttendanceSummary>{Success=false};
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
        var holidays=(await _calendar.GetAll(x=>x.CompanyId==companyId&&x.Type==EnumsHelper.CalendarItem.Holiday&&(x.Recurring||(x.Date>=start&&x.Date<endExclusive)))).ToList();
        bool IsHoliday(DateTime d)=>holidays.Any(h=>h.Recurring?h.Date.Month==d.Month&&h.Date.Day==d.Day:h.Date.Date==d.Date);
        var summaries=new List<MonthlyAttendanceSummary>();var errors=new List<string>();
        foreach(var emp in await _employees.GetAll(x=>x.CompanyId==companyId&&x.Status&&!x.IsDeleted))
        {
            if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to))continue;
            var toExclusive=to.Date.AddDays(1);
            var records=(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).ToList();
            var expectedDates=Enumerable.Range(0,(to-from).Days+1).Select(i=>from.AddDays(i).Date).Where(d=>!offDays.Contains((int)d.DayOfWeek)&&!IsHoliday(d)).ToList();
            var recordedDates=records.Select(x=>x.Date.Date).Distinct().ToHashSet();
            var missingDates=expectedDates.Where(x=>!recordedDates.Contains(x)).ToList();
            var missingCheckoutDates=records.Where(x=>rules.TryGetValue(x.Status,out var rule)&&rule.RequiresTime&&!x.CheckOutTime.HasValue).Select(x=>x.Date.Date).Distinct().ToList();
            var duplicateDates=records.GroupBy(x=>x.Date.Date).Where(x=>x.Count()>1).Select(x=>x.Key).ToList();
            var invalidTimeDates=records.Where(x=>x.CheckInTime.HasValue&&x.CheckOutTime.HasValue&&x.CheckOutTime<x.CheckInTime).Select(x=>x.Date.Date).ToList();
            var invalidStatusDates=records.Where(x=>!rules.TryGetValue(x.Status,out var rule)||!rule.IsActive).Select(x=>x.Date.Date).ToList();
            if(missingDates.Count+missingCheckoutDates.Count+duplicateDates.Count+invalidTimeDates.Count+invalidStatusDates.Count>0)
                errors.Add($"{emp.EmployeeId}: missing attendance {missingDates.Count}, missing checkout {missingCheckoutDates.Count}, duplicates {duplicateDates.Count}, invalid time {invalidTimeDates.Count}, invalid status {invalidStatusDates.Count}");
            var existing=await _summaries.FirstOrDefault(x=>x.CompanyId==companyId&&x.EmployeeId==emp.EmployeeId&&x.PayrollMonth==start);
            var summary=existing??new MonthlyAttendanceSummary{CompanyId=companyId,UserId=emp.UserId,EmployeeId=emp.EmployeeId,PayrollMonth=start};
            summary.EligibleFrom=from;summary.EligibleTo=to;summary.ExpectedWorkingDays=expectedDates.Count;summary.EligibleWorkingDays=expectedDates.Count;
            summary.MissingAttendanceDays=missingDates.Count;summary.MissingCheckoutDays=missingCheckoutDates.Count;
            summary.PaidDays=records.Sum(x=>rules.TryGetValue(x.Status,out var r)?r.PaidDayFraction:0);summary.UnpaidDays=records.Sum(x=>rules.TryGetValue(x.Status,out var r)?r.UnpaidDayFraction:0);
            summary.PresentDays=records.Count(x=>x.Status=="P");summary.WfhDays=records.Count(x=>x.Status is "WFH" or "WFH+WFO");
            summary.PaidLeaveDays=records.Where(x=>x.Status is "SL" or "CL" or "EL" or "COMP-OFF").Sum(x=>rules.TryGetValue(x.Status,out var r)?r.PaidDayFraction:0);
            summary.UnpaidLeaveDays=records.Where(x=>x.Status=="A").Sum(x=>rules.TryGetValue(x.Status,out var r)?r.UnpaidDayFraction:1);
            summary.HalfDays=records.Count(x=>x.Status is "HD" or "LHD" or "CL-HALF" or "SL-HALF" or "WFH-HD")*.5m;summary.AbsentDays=records.Count(x=>x.Status=="A");
            summary.LhdCount=records.Count(x=>x.Status=="LHD");summary.EdCount=records.Count(x=>x.Status=="ED");summary.CombinedLhdEdCount=summary.LhdCount+summary.EdCount;
            summary.BlockingExceptionCount=missingDates.Count+missingCheckoutDates.Count+duplicateDates.Count+invalidTimeDates.Count+invalidStatusDates.Count;
            if(existing==null)await _summaries.AddOne(summary);else if(!existing.IsLocked)await _summaries.Update(Builders<MonthlyAttendanceSummary>.Filter.Eq(x=>x.Id,existing.Id),summary);
            summaries.Add(summary);
        }
        var unresolved=(await _exceptions.GetAll(x=>x.CompanyId==companyId&&x.PayrollMonth==start&&x.Status=="PENDING_REVIEW"&&x.Severity=="BLOCKING")).ToList();
        if(errors.Count>0||unresolved.Count>0)
        {
            var messageParts=new List<string>{"Attendance month cannot be locked."};
            if(errors.Count>0)
            {
                messageParts.Add("Correct attendance data: "+string.Join("; ",errors.Take(10))+(errors.Count>10?$"; and {errors.Count-10} more":"")+".");
            }
            if(unresolved.Count>0)
            {
                var exceptionDetails=unresolved.Take(10).Select(x=>x.ExceptionType=="LHD_ED_LIMIT_EXCEEDED"
                    ? $"{x.EmployeeId}: LHD {x.LhdCount} + ED {x.EdCount}, allowed {x.AllowedOccurrenceCount}, exceeded {x.ExceededOccurrenceCount}"
                    : $"{x.EmployeeId}: {x.ExceptionType} ({x.AffectedDates.Count} affected date(s))");
                messageParts.Add($"Review {unresolved.Count} blocking exception(s) in Attendance > Monthly Exceptions for {start:MMMM yyyy}: "+string.Join("; ",exceptionDetails)+(unresolved.Count>10?$"; and {unresolved.Count-10} more":"")+".");
            }
            result.Message=string.Join(" ",messageParts);result.MethodResults=summaries;return result;
        }
        foreach(var s in summaries){if(s.IsLocked)continue;s.IsApproved=true;s.IsLocked=true;s.ApprovedBy=userId;s.ApprovedAt=DateTime.UtcNow;s.LockedBy=userId;s.LockedAt=DateTime.UtcNow;s.Version++;await _summaries.Update(Builders<MonthlyAttendanceSummary>.Filter.Eq(x=>x.Id,s.Id),s);}
        result.Success=true;result.Message=$"Locked attendance for {summaries.Count} employee(s).";result.MethodResults=summaries;return result;
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
        var holidays=(await _calendar.GetAll(x=>x.CompanyId==companyId&&x.Type==EnumsHelper.CalendarItem.Holiday&&(x.Recurring||(x.Date>=start&&x.Date<endExclusive)))).ToList();
        bool IsHoliday(DateTime d)=>holidays.Any(h=>h.Recurring?h.Date.Month==d.Month&&h.Date.Day==d.Day:h.Date.Date==d.Date);

        foreach(var emp in await _employees.GetAll(x=>x.CompanyId==companyId&&x.Status&&!x.IsDeleted))
        {
            if(!AttendancePayrollRules.TryGetEligiblePeriod(emp,start,end,out var from,out var to))continue;
            var toExclusive=to.Date.AddDays(1);
            var records=(await _attendance.GetAll(a=>a.CompanyId==companyId&&a.UserId==emp.UserId&&a.EmployeeId==emp.EmployeeId&&a.Date>=from&&a.Date<toExclusive)).ToList();
            var expectedDates=Enumerable.Range(0,(to-from).Days+1).Select(i=>from.AddDays(i).Date).Where(d=>!offDays.Contains((int)d.DayOfWeek)&&!IsHoliday(d)).ToList();
            var recordedDates=records.Select(x=>x.Date.Date).Distinct().ToHashSet();

            await UpsertValidationException(companyId,emp,start,"MISSING_ATTENDANCE",expectedDates.Where(x=>!recordedDates.Contains(x)).ToList());
            await UpsertValidationException(companyId,emp,start,"MISSING_CHECKOUT",records.Where(x=>rules.TryGetValue(x.Status,out var rule)&&rule.RequiresTime&&!x.CheckOutTime.HasValue).Select(x=>x.Date.Date).Distinct().ToList());
            await UpsertValidationException(companyId,emp,start,"DUPLICATE_ATTENDANCE",records.GroupBy(x=>x.Date.Date).Where(x=>x.Count()>1).Select(x=>x.Key).ToList());
            await UpsertValidationException(companyId,emp,start,"INVALID_TIME_ORDER",records.Where(x=>x.CheckInTime.HasValue&&x.CheckOutTime.HasValue&&x.CheckOutTime<x.CheckInTime).Select(x=>x.Date.Date).Distinct().ToList());
            await UpsertValidationException(companyId,emp,start,"UNKNOWN_OR_INACTIVE_STATUS",records.Where(x=>!rules.TryGetValue(x.Status,out var rule)||!rule.IsActive).Select(x=>x.Date.Date).Distinct().ToList());
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
