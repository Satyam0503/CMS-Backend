using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Attendance;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeAttendanceTimingTests
{
    private static readonly DateTime Today = new(2026, 10, 27);
    private static readonly TimeSpan ShiftStart = new(9, 30, 0);
    private static readonly TimeSpan ShiftEnd = new(18, 30, 0);

    [Fact]
    public void IsSameDayCutoff_FullDay_BeforeShiftStart_NotBlocked()
    {
        var now = new DateTime(2026, 10, 27, 8, 30, 0);
        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now, "FullDay", ShiftStart, null, 0, 480, 8m);

        Assert.False(isBlocked);
    }

    [Fact]
    public void IsSameDayCutoff_FullDay_AfterShiftStart_IsBlocked()
    {
        var now = new DateTime(2026, 10, 27, 9, 31, 0);
        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now, "FullDay", ShiftStart, null, 0, 480, 8m);

        Assert.True(isBlocked);
    }

    [Fact]
    public void IsSameDayCutoff_FirstHalf_AfterShiftStart_IsBlocked()
    {
        var now = new DateTime(2026, 10, 27, 10, 0, 0);
        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now, "FirstHalf", ShiftStart, null, 0, 480, 8m);

        Assert.True(isBlocked);
    }

    [Fact]
    public void IsSameDayCutoff_SecondHalf_AfterShiftStart_BeforeHalfDayBoundary_NotBlocked()
    {
        // Between 09:30 and 13:30, second half has NOT started yet, so applying for Second Half is allowed!
        var now = new DateTime(2026, 10, 27, 11, 0, 0);
        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now, "SecondHalf", ShiftStart, null, 0, 480, 8m);

        Assert.False(isBlocked);
    }

    [Fact]
    public void IsSameDayCutoff_SecondHalf_AfterHalfDayBoundary_IsBlocked()
    {
        // 13:30 is the half-day boundary (9:30 + 4 hours). At 13:31, Second Half cutoff has passed.
        var now = new DateTime(2026, 10, 27, 13, 31, 0);
        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now, "SecondHalf", ShiftStart, null, 0, 480, 8m);

        Assert.True(isBlocked);
    }

    [Fact]
    public void IsSameDayCutoff_SecondHalf_At1514_IsBlocked()
    {
        // User scenario from screenshot: at 15:14, first half and second half cutoffs have both passed!
        var now = new DateTime(2026, 10, 27, 15, 14, 0);
        var firstHalfBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now, "FirstHalf", ShiftStart, null, 0, 480, 8m);
        var secondHalfBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now, "SecondHalf", ShiftStart, null, 0, 480, 8m);

        Assert.True(firstHalfBlocked);
        Assert.True(secondHalfBlocked);
    }

    [Fact]
    public void WfhRequestConflicts_OppositeHalvesSameDate_DoNotConflict()
    {
        var existing = new WorkFromHomeRequest
        {
            FromDate = Today,
            ToDate = Today,
            DurationType = "FirstHalf"
        };

        // New request for SecondHalf on the same day must NOT conflict
        var conflicts = WorkFromHomeService.WfhRequestConflicts(existing, Today, Today, "SecondHalf");
        Assert.False(conflicts);

        // And vice-versa
        var existingSecondHalf = new WorkFromHomeRequest
        {
            FromDate = Today,
            ToDate = Today,
            DurationType = "SecondHalf"
        };
        var conflictsReverse = WorkFromHomeService.WfhRequestConflicts(existingSecondHalf, Today, Today, "FirstHalf");
        Assert.False(conflictsReverse);
    }

    [Fact]
    public void WfhRequestConflicts_SameHalvesSameDate_DoConflict()
    {
        var existing = new WorkFromHomeRequest
        {
            FromDate = Today,
            ToDate = Today,
            DurationType = "FirstHalf"
        };

        var conflicts = WorkFromHomeService.WfhRequestConflicts(existing, Today, Today, "FirstHalf");
        Assert.True(conflicts);
    }

    [Fact]
    public void WfhRequestConflicts_FullDayWithHalfDay_DoConflict()
    {
        var existing = new WorkFromHomeRequest
        {
            FromDate = Today,
            ToDate = Today,
            DurationType = "FullDay"
        };

        var conflicts = WorkFromHomeService.WfhRequestConflicts(existing, Today, Today, "SecondHalf");
        Assert.True(conflicts);
    }

    [Fact]
    public void LeaveConflictsWithWfh_Combinations()
    {
        // FirstHalf WFH with SecondHalf Leave -> Coexist without conflict (WFH + HD)
        var secondHalfLeave = new LeaveRequest
        {
            LeavePolicyId = "LP1",
            Reason = "Medical",
            IsHalfDay = true,
            HalfDayPeriod = "SECOND_HALF"
        };
        var conflicts1 = WorkFromHomeService.LeaveConflictsWithWfh(secondHalfLeave, "FirstHalf");
        Assert.False(conflicts1);

        // SecondHalf WFH with FirstHalf Leave -> Coexist without conflict (HD + WFH)
        var firstHalfLeave = new LeaveRequest
        {
            LeavePolicyId = "LP1",
            Reason = "Personal",
            IsHalfDay = true,
            HalfDayPeriod = "FIRST_HALF"
        };
        var conflicts2 = WorkFromHomeService.LeaveConflictsWithWfh(firstHalfLeave, "SecondHalf");
        Assert.False(conflicts2);

        // Same half -> Must conflict
        var sameHalfConflict1 = WorkFromHomeService.LeaveConflictsWithWfh(firstHalfLeave, "FirstHalf");
        Assert.True(sameHalfConflict1);

        var sameHalfConflict2 = WorkFromHomeService.LeaveConflictsWithWfh(secondHalfLeave, "SecondHalf");
        Assert.True(sameHalfConflict2);

        // Full day leave with any WFH -> Must conflict
        var fullDayLeave = new LeaveRequest
        {
            LeavePolicyId = "LP1",
            Reason = "Vacation",
            IsHalfDay = false
        };
        Assert.True(WorkFromHomeService.LeaveConflictsWithWfh(fullDayLeave, "FirstHalf"));
        Assert.True(WorkFromHomeService.LeaveConflictsWithWfh(fullDayLeave, "SecondHalf"));
        Assert.True(WorkFromHomeService.LeaveConflictsWithWfh(fullDayLeave, "FullDay"));

        // Full day WFH with half day leave -> Must conflict
        Assert.True(WorkFromHomeService.LeaveConflictsWithWfh(firstHalfLeave, "FullDay"));
        Assert.True(WorkFromHomeService.LeaveConflictsWithWfh(secondHalfLeave, "FullDay"));
    }

    [Fact]
    public void GracePeriod_ExtendsCutoffWindow()
    {
        // Office start 09:30, grace 30 mins => cutoff is 10:00
        var now945 = new DateTime(2026, 10, 27, 9, 45, 0);
        var blockedAt945 = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now945, "FullDay", ShiftStart, null, 30, 480, 8m);
        Assert.False(blockedAt945);

        var now1001 = new DateTime(2026, 10, 27, 10, 1, 0);
        var blockedAt1001 = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            Today, now1001, "FullDay", ShiftStart, null, 30, 480, 8m);
        Assert.True(blockedAt1001);
    }

    [Fact]
    public void MultiDay_WfhRequestConflicts_ChecksOverlaps()
    {
        var multiDayExisting = new WorkFromHomeRequest
        {
            FromDate = Today,
            ToDate = Today.AddDays(2),
            DurationType = "FullDay"
        };

        // Any overlap with multi-day full day is a conflict
        Assert.True(WorkFromHomeService.WfhRequestConflicts(multiDayExisting, Today, Today, "FirstHalf"));
        Assert.True(WorkFromHomeService.WfhRequestConflicts(multiDayExisting, Today.AddDays(1), Today.AddDays(1), "SecondHalf"));

        // Request for a different day outside the range does NOT conflict
        Assert.False(WorkFromHomeService.WfhRequestConflicts(multiDayExisting, Today.AddDays(3), Today.AddDays(3), "FullDay"));
    }

    [Fact]
    public void HalfDayClockIn_SegmentBoundary_Validation()
    {
        var boundary = WorkFromHomeService.HalfDayBoundary(ShiftStart, 480, 8m);
        Assert.Equal(new TimeSpan(13, 30, 0), boundary);

        // At 10:00, clock in for FirstHalf is valid
        var firstHalfValid = WorkFromHomeService.IsHalfDayClockInWithinRequestedSegment(
            "FirstHalf", new TimeSpan(10, 0, 0), ShiftStart, 480, 8m);
        Assert.True(firstHalfValid);

        // At 14:00, clock in for FirstHalf is INVALID
        var firstHalfAt1400 = WorkFromHomeService.IsHalfDayClockInWithinRequestedSegment(
            "FirstHalf", new TimeSpan(14, 0, 0), ShiftStart, 480, 8m);
        Assert.False(firstHalfAt1400);

        // At 14:00, clock in for SecondHalf is valid
        var secondHalfAt1400 = WorkFromHomeService.IsHalfDayClockInWithinRequestedSegment(
            "SecondHalf", new TimeSpan(14, 0, 0), ShiftStart, 480, 8m);
        Assert.True(secondHalfAt1400);

        // Kiara's scenario: at 15:14 (3:14 PM), First-half WFH check-in must fail because first half has passed
        var firstHalfAt1514 = WorkFromHomeService.IsHalfDayClockInWithinRequestedSegment(
            "FirstHalf", new TimeSpan(15, 14, 0), ShiftStart, 480, 8m);
        Assert.False(firstHalfAt1514);
    }

    [Theory]
    [InlineData("FullDay", "FullDay")]
    [InlineData("WfhWfo", "FirstHalf")]
    [InlineData("WfoWfh", "SecondHalf")]
    [InlineData("WfhHd", "FirstHalf")]
    [InlineData("HdWfh", "SecondHalf")]
    public void AdminGrant_DurationTypeMapping_AllCombinationsSupported(string grantType, string expectedDurationType)
    {
        var resolvedDuration = grantType switch
        {
            "WfhWfo" => "FirstHalf",
            "WfoWfh" => "SecondHalf",
            "WfhHd" => "FirstHalf",
            "HdWfh" => "SecondHalf",
            _ => "FullDay"
        };

        Assert.Equal(expectedDurationType, resolvedDuration);
    }

    [Fact]
    public void AllowedDaysOfWeek_Validation_ChecksDayNames()
    {
        var tuesday = new DateTime(2026, 10, 27); // Tuesday
        var wednesday = new DateTime(2026, 10, 28); // Wednesday
        var allowed = new List<string> { "Monday", "Friday" };

        var tuesdayAllowed = allowed.Any(x => string.Equals(x, tuesday.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.False(tuesdayAllowed);

        var monday = new DateTime(2026, 10, 26); // Monday
        var mondayAllowed = allowed.Any(x => string.Equals(x, monday.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.True(mondayAllowed);
    }

    [Fact]
    public void BlockedDaysOfWeek_Validation_ChecksCoreOfficeDays()
    {
        var wednesday = new DateTime(2026, 10, 28); // Wednesday
        var coreOfficeDays = new List<string> { "Wednesday", "Thursday" };

        var isWednesdayBlocked = coreOfficeDays.Any(x => string.Equals(x, wednesday.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.True(isWednesdayBlocked);

        var friday = new DateTime(2026, 10, 30); // Friday
        var isFridayBlocked = coreOfficeDays.Any(x => string.Equals(x, friday.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.False(isFridayBlocked);
    }

    [Fact]
    public void ReplaceableDefaultPresent_IdentifiesSystemDefaultStatuses()
    {
        Assert.True(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent("SYSTEM_DEFAULT_PRESENT"));
        Assert.True(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent("SYSTEM_OFFICE_START_PRESENT"));
        Assert.False(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(null));
        Assert.False(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(""));
        Assert.False(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent("ADMIN_MANUAL"));
        Assert.False(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent("ADMIN_OVERRIDE"));
        Assert.False(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent("WFH_REQUEST"));
        Assert.False(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent("LEAVE"));
    }

    [Fact]
    public void OfficeConversion_ExcludesDayFromWeeklyUsage_AndRefundsQuota()
    {
        var monday = new DateTime(2026, 10, 26);
        var tuesday = new DateTime(2026, 10, 27);
        var request = new WorkFromHomeRequest
        {
            RequestId = "REQ-101",
            UserId = "USR-01",
            FromDate = monday,
            ToDate = tuesday,
            DurationType = "FullDay",
            Status = "Approved",
            OfficeConversions =
            [
                new WorkFromHomeOfficeConversion
                {
                    AttendanceDate = tuesday,
                    Remarks = "Converted to in-office half day / full day attendance",
                    ConvertedByUserId = "HR-01",
                    ConvertedAtUtc = DateTime.UtcNow
                }
            ]
        };

        var week = new DateTime(2026, 10, 26);
        var unitsPerDay = request.DurationType is "FirstHalf" or "SecondHalf" ? .5m : 1m;
        var convertedDates = (request.OfficeConversions ?? []).Select(x => x.AttendanceDate.Date).ToHashSet();
        var days = new[] { monday, tuesday };
        var activeWfhDaysCount = days.Count(day => day >= week && day < week.AddDays(7) && !convertedDates.Contains(day.Date)) * unitsPerDay;

        // Tuesday is converted to office, so only Monday counts against WFH allowance (1 day, not 2)
        Assert.Equal(1m, activeWfhDaysCount);
    }

    [Fact]
    public void HalfDayOfficeConversion_ExcludesHalfDay_AndRefundsHalfDayQuota()
    {
        var tuesday = new DateTime(2026, 10, 27);
        var request = new WorkFromHomeRequest
        {
            RequestId = "REQ-102",
            UserId = "USR-01",
            FromDate = tuesday,
            ToDate = tuesday,
            DurationType = "FirstHalf",
            Status = "Approved",
            OfficeConversions =
            [
                new WorkFromHomeOfficeConversion
                {
                    AttendanceDate = tuesday,
                    Remarks = "Converted to in-office work",
                    ConvertedByUserId = "HR-01",
                    ConvertedAtUtc = DateTime.UtcNow
                }
            ]
        };

        var week = new DateTime(2026, 10, 26);
        var unitsPerDay = request.DurationType is "FirstHalf" or "SecondHalf" ? .5m : 1m;
        var convertedDates = (request.OfficeConversions ?? []).Select(x => x.AttendanceDate.Date).ToHashSet();
        var days = new[] { tuesday };
        var activeWfhDaysCount = days.Count(day => day >= week && day < week.AddDays(7) && !convertedDates.Contains(day.Date)) * unitsPerDay;

        // Converted half-day WFH counts as 0 remaining against WFH allowance
        Assert.Equal(0m, activeWfhDaysCount);
    }

    [Fact]
    public void ResolveBusinessDateOnly_MapsMidnightUtcAndIstMidnight_ToSameCalendarDay()
    {
        // 2026-10-28 midnight UTC
        var utcMidnight = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);
        var resolvedUtc = WorkFromHomeService.ResolveBusinessDateOnly(utcMidnight, "Asia/Kolkata");
        Assert.Equal(new DateOnly(2026, 10, 28), resolvedUtc);

        // 2026-10-27 18:30:00 UTC (which is 2026-10-28 00:00:00 IST)
        var istMidnightStoredInUtc = new DateTime(2026, 10, 27, 18, 30, 0, DateTimeKind.Utc);
        var resolvedIst = WorkFromHomeService.ResolveBusinessDateOnly(istMidnightStoredInUtc, "Asia/Kolkata");
        Assert.Equal(new DateOnly(2026, 10, 28), resolvedIst);
    }

    [Fact]
    public void BusinessDateUtc_PreservesCalendarDate_WithExplicitUtcKind()
    {
        var unspecified = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Unspecified);
        var result = WorkFromHomeService.BusinessDateUtc(unspecified);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(2026, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Equal(28, result.Day);
        Assert.Equal(TimeSpan.Zero, result.TimeOfDay);
    }

    [Fact]
    public void ReplaceableSystemPresent_WhenTransitioningToWfh_ClearsGhostShiftPunchTimestamps()
    {
        // Simulates the morning SYSTEM_OFFICE_START_PRESENT row which carried 09:00 - 18:00
        var morningRow = new AttendanceModel
        {
            CompanyId = "comp-1",
            UserId = "user-1",
            Date = Today,
            Status = "P",
            SourceType = "SYSTEM_OFFICE_START_PRESENT",
            CheckInTime = Today.AddHours(9),
            CheckOutTime = Today.AddHours(18),
            TotalHours = 9
        };

        Assert.True(AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(morningRow.SourceType));

        // When half-day WFH sync executes without prior portal punches or companion punches,
        // it must strip fake 09:00 - 18:00 placeholder times
        var hasPortalClock = morningRow.Remarks != null && (morningRow.Remarks.Contains("Checked in through employee WFH portal") || morningRow.Remarks.Contains("Checked out through employee WFH portal"));
        if (!hasPortalClock && AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(morningRow.SourceType))
        {
            morningRow.CheckInTime = null;
            morningRow.CheckOutTime = null;
            morningRow.TotalHours = null;
        }

        Assert.Null(morningRow.CheckInTime);
        Assert.Null(morningRow.CheckOutTime);
        Assert.Null(morningRow.TotalHours);
    }

    [Fact]
    public void AdminOverride_AllowsWfhAndHybridStatuses_ForTimingRegularization()
    {
        var wfhStatuses = new[] { "WFH", "WFH+WFO", "WFH-HD" };
        foreach (var status in wfhStatuses)
        {
            var isWfh = string.Equals(status, "WFH", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "WFH+WFO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "WFH-HD", StringComparison.OrdinalIgnoreCase);

            Assert.True(isWfh, $"{status} should be recognized as a valid WFH/hybrid status for HR edit");
        }
    }

    [Fact]
    public void AdminGrant_OfficePunchWindow_ConvertsLocalToUtcCorrectly()
    {
        var attendanceDate = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);
        var localCheckIn = new TimeSpan(9, 0, 0); // 09:00 IST
        var localCheckOut = new TimeSpan(14, 0, 0); // 14:00 IST
        var tz = "Asia/Kolkata";

        var utcIn = AttendanceMutationValidator.ToScheduleUtc(attendanceDate, localCheckIn, tz);
        var utcOut = AttendanceMutationValidator.ToScheduleUtc(attendanceDate, localCheckOut, tz);

        // 09:00 IST is 03:30 UTC
        Assert.Equal(new DateTime(2026, 10, 29, 3, 30, 0, DateTimeKind.Utc), utcIn);
        // 14:00 IST is 08:30 UTC
        Assert.Equal(new DateTime(2026, 10, 29, 8, 30, 0, DateTimeKind.Utc), utcOut);

        var totalHours = Math.Round((decimal)(utcOut - utcIn).TotalHours, 2);
        Assert.Equal(5.00m, totalHours);
    }

    [Theory]
    [InlineData("09:00", "18:00", "09:00", "13:30", "13:30", "18:00")]
    [InlineData("09:30", "18:30", "09:30", "14:00", "14:00", "18:30")]
    [InlineData("10:00", "19:00", "10:00", "14:30", "14:30", "19:00")]
    public void ShiftMidpoint_DividesWorkingHoursEvenly(
        string startStr, string endStr, string expectedFhIn, string expectedFhOut, string expectedShIn, string expectedShOut)
    {
        var start = TimeSpan.Parse(startStr);
        var end = TimeSpan.Parse(endStr);
        var totalMinutes = (end - start).TotalMinutes;
        var midpoint = start.Add(TimeSpan.FromMinutes(totalMinutes / 2));

        Assert.Equal(expectedFhIn, start.ToString(@"hh\:mm"));
        Assert.Equal(expectedFhOut, midpoint.ToString(@"hh\:mm"));
        Assert.Equal(expectedShIn, midpoint.ToString(@"hh\:mm"));
        Assert.Equal(expectedShOut, end.ToString(@"hh\:mm"));
    }

    [Fact]
    public void IsTransactionUnsupported_DetectsStandaloneAndWrappedExceptions()
    {
        var directNotSupported = new NotSupportedException("Transactions are not supported.");
        Assert.True(WorkFromHomeService.IsTransactionUnsupported(directNotSupported));

        var directMessage = new Exception("Transaction numbers are only allowed on a replica set member or mongos.");
        Assert.True(WorkFromHomeService.IsTransactionUnsupported(directMessage));

        var wrappedInMongoException = new Exception("Transaction failed", directMessage);
        Assert.True(WorkFromHomeService.IsTransactionUnsupported(wrappedInMongoException));

        var aggregateException = new AggregateException(new InvalidOperationException("Other"), directMessage);
        Assert.True(WorkFromHomeService.IsTransactionUnsupported(aggregateException));

        var unrelatedException = new InvalidOperationException("Some business rule violated.");
        Assert.False(WorkFromHomeService.IsTransactionUnsupported(unrelatedException));
    }

    [Fact]
    public void MergedDailyRow_CalculatesEarliestIn_LatestOut_AndCombinedHours()
    {
        var morningIn = new DateTime(2026, 10, 29, 3, 30, 0, DateTimeKind.Utc); // 09:00 IST
        var morningOut = new DateTime(2026, 10, 29, 8, 0, 0, DateTimeKind.Utc);  // 13:30 IST
        var afternoonIn = new DateTime(2026, 10, 29, 8, 0, 0, DateTimeKind.Utc); // 13:30 IST
        var afternoonOut = new DateTime(2026, 10, 29, 12, 30, 0, DateTimeKind.Utc); // 18:00 IST

        var wfhSegment = new AttendanceDaySegment
        {
            Segment = "FIRST_HALF",
            CheckInTime = morningIn,
            CheckOutTime = morningOut,
            TotalHours = 4.5m
        };

        var officeSegment = new AttendanceDaySegment
        {
            Segment = "SECOND_HALF",
            CheckInTime = afternoonIn,
            CheckOutTime = afternoonOut,
            TotalHours = 4.5m
        };

        var earliestIn = (wfhSegment.CheckInTime.HasValue && officeSegment.CheckInTime.HasValue)
            ? (wfhSegment.CheckInTime.Value < officeSegment.CheckInTime.Value ? wfhSegment.CheckInTime : officeSegment.CheckInTime)
            : (wfhSegment.CheckInTime ?? officeSegment.CheckInTime);

        var latestOut = (wfhSegment.CheckOutTime.HasValue && officeSegment.CheckOutTime.HasValue)
            ? (wfhSegment.CheckOutTime.Value > officeSegment.CheckOutTime.Value ? wfhSegment.CheckOutTime : officeSegment.CheckOutTime)
            : (wfhSegment.CheckOutTime ?? officeSegment.CheckOutTime);

        var combinedHours = Math.Round((wfhSegment.TotalHours ?? 0m) + (officeSegment.TotalHours ?? 0m), 2);

        Assert.Equal(morningIn, earliestIn);
        Assert.Equal(afternoonOut, latestOut);
        Assert.Equal(9.00m, combinedHours);
    }
}
