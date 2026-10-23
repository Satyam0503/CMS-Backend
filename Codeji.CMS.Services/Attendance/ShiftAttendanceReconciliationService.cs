using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Utility.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codeji.CMS.Services.Attendance;

public interface IShiftAttendanceReconciliationService
{
    Task<int> ReconcileTodayShiftAttendanceAsync(string companyId, CancellationToken cancellationToken = default);
}

public sealed class ShiftAttendanceReconciliationService(
    IMongoDbRepository<AttendanceModel> attendanceRepository,
    IMongoDbRepository<EmpUser> employeesRepository,
    IEffectiveOfficeScheduleService effectiveSchedules,
    ILogger<ShiftAttendanceReconciliationService> logger) : IShiftAttendanceReconciliationService
{
    public async Task<int> ReconcileTodayShiftAttendanceAsync(string companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(companyId)) return 0;

        try
        {
            var today = IndiaTime.Now.Date;
            var dayStart = DateTime.SpecifyKind(today, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            // Fetch today's replaceable system present records for this tenant
            var replaceableSources = new[] { AttendanceSourceTransitionPolicy.DefaultPresent, "SYSTEM_OFFICE_START_PRESENT" };
            var existingAttendance = (await attendanceRepository.GetAll(
                x => x.CompanyId == companyId &&
                     x.Date >= dayStart && x.Date < dayEnd &&
                     x.Status == "P" &&
                     replaceableSources.Contains(x.SourceType),
                WithDeletedObjects: false,
                withDefaultFilter: false)).ToList();

            if (existingAttendance.Count == 0) return 0;

            var userIds = existingAttendance.Select(x => x.UserId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var employees = (await employeesRepository.GetAll(
                x => x.CompanyId == companyId && userIds.Contains(x.UserId) && !x.IsDeleted,
                WithDeletedObjects: false,
                withDefaultFilter: false))
                .Where(e => EmploymentPeriod.WasEmployedOn(e, today))
                .ToDictionary(x => x.UserId, StringComparer.Ordinal);

            var updatedCount = 0;
            foreach (var record in existingAttendance)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(record.UserId) || !employees.TryGetValue(record.UserId, out var employee))
                    continue;

                var schedule = await effectiveSchedules.ResolveAsync(companyId, employee, today, cancellationToken);
                if (schedule is null) continue;

                var tz = schedule.TimeZoneId ?? "Asia/Kolkata";
                var effIn = AttendanceMutationValidator.ToScheduleUtc(today, schedule.StartTime, tz);
                var effOut = AttendanceMutationValidator.ToScheduleUtc(today, schedule.EndTime, tz);
                if (schedule.EndTime <= schedule.StartTime)
                {
                    effOut = effOut.AddDays(1);
                }

                // If timings and schedule already match, no update needed
                if (record.CheckInTime == effIn && record.CheckOutTime == effOut && record.ScheduleId == schedule.ScheduleId)
                    continue;

                record.CheckInTime = effIn;
                record.CheckOutTime = effOut;
                record.BreakMinutes = schedule.BreakMinutes;
                record.TotalHours = Math.Max((decimal)(effOut - effIn).TotalHours, 0);
                record.ScheduleId = schedule.ScheduleId;
                record.ScheduleVersion = schedule.Version;
                var remark = $"System Present aligned to effective shift ({schedule.Name} · {schedule.StartTime:hh\\:mm}–{schedule.EndTime:hh\\:mm}).";
                record.Remarks = remark;
                record.RemarkCode = "SYSTEM_OFFICE_START_PRESENT";
                record.RemarkText = remark;
                record.SystemRemark = remark;
                record.LateCount = 0;
                record.EarlyExitCount = 0;
                record.ModifiedAtUtc = DateTime.UtcNow;
                record.ModifiedByUserId = "system";
                record.Version++;

                var updateFilter = Builders<AttendanceModel>.Filter.And(
                    Builders<AttendanceModel>.Filter.Eq(x => x.CompanyId, companyId),
                    Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, record.AttendanceId));

                var updateResult = await attendanceRepository.Update(updateFilter, record);
                if (updateResult.Success)
                {
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                logger.LogInformation("Reconciled {UpdatedCount} system-present attendance records for company {CompanyId} to effective shift.", updatedCount, companyId);
            }

            return updatedCount;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to reconcile shift attendance for company {CompanyId}", companyId);
            return 0;
        }
    }
}

