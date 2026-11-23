using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using MapsterMapper;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class BiometricAttendanceServiceTests
{
    private const string TenantId = "company-1";

    [Fact]
    public async Task IngestBiometricPunches_CreatesNewRecords_UpdatesCheckout_AndDeduplicates()
    {
        var employee = new EmpUser
        {
            CompanyId = TenantId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            FirstName = "Ananya",
            LastName = "Iyer",
            Email = "ananya@example.com",
            Status = true
        };

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { employee });

        var attendanceList = new List<AttendanceModel>();
        var attendanceRepo = new Mock<IAttendanceRepository>();
        attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TenantId, "user-1", It.IsAny<DateTime>()))
            .ReturnsAsync((string _, string _, DateTime date) => attendanceList.FirstOrDefault(x => x.Date.Date == date.Date));

        attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .ReturnsAsync((AttendanceModel row) =>
            {
                attendanceList.Add(row);
                return row;
            });

        attendanceRepo.Setup(r => r.UpdateAsync(TenantId, "user-1", It.IsAny<DateTime>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync((string _, string _, DateTime _, AttendanceModel row) =>
            {
                var idx = attendanceList.FindIndex(x => x.Date.Date == row.Date.Date);
                if (idx >= 0) attendanceList[idx] = row;
                return true;
            });

        var auditWriter = new Mock<IAttendanceAuditWriter>();
        var segmentsRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        segmentsRepo.Setup(s => s.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());

        var service = new AdminAttendanceService(
            attendanceRepo.Object,
            new Mock<IMapper>().Object,
            new Mock<IAttendanceMutationValidator>().Object,
            auditWriter.Object,
            segmentsRepo.Object,
            empRepo.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            new Mock<IMongoDbRepository<WeeklyOffSetting>>().Object,
            new Mock<IMongoDbRepository<CalendarEntity>>().Object,
            new Mock<IMongoDbRepository<AttendanceStatusSetting>>().Object);

        var punchDate = new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc);

        var request = new BiometricSyncRequestDto
        {
            DeviceId = "BIO-GATE-01",
            Punches =
            [
                // 1. Initial check-in at 09:00 AM
                new BiometricPunchDto
                {
                    DeviceId = "BIO-GATE-01",
                    EmployeeId = "EMP001",
                    TimestampUtc = punchDate,
                    PunchType = "CHECK_IN",
                    VerifyMode = "FINGERPRINT"
                },
                // 2. Duplicate check-in at 09:00:20 AM (within 60s)
                new BiometricPunchDto
                {
                    DeviceId = "BIO-GATE-01",
                    EmployeeId = "EMP001",
                    TimestampUtc = punchDate.AddSeconds(20),
                    PunchType = "CHECK_IN",
                    VerifyMode = "FINGERPRINT"
                },
                // 3. Check-out at 06:00 PM (9 hours later)
                new BiometricPunchDto
                {
                    DeviceId = "BIO-GATE-01",
                    EmployeeId = "EMP001",
                    TimestampUtc = punchDate.AddHours(9),
                    PunchType = "CHECK_OUT",
                    VerifyMode = "FACE"
                },
                // 4. Unknown employee punch
                new BiometricPunchDto
                {
                    DeviceId = "BIO-GATE-01",
                    EmployeeId = "UNKNOWN-99",
                    TimestampUtc = punchDate,
                    PunchType = "CHECK_IN"
                }
            ]
        };

        var result = await service.IngestBiometricPunches(TenantId, "system", request);

        Assert.True(result.Success);
        Assert.Equal(4, result.TotalReceived);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(1, result.UnmatchedEmployeeCount);

        Assert.Single(attendanceList);
        var finalRecord = attendanceList[0];
        Assert.Equal("BIOMETRIC", finalRecord.SourceType);
        Assert.Equal(punchDate, finalRecord.CheckInTime);
        Assert.Equal(punchDate.AddHours(9), finalRecord.CheckOutTime);
        Assert.Equal(9.0m, finalRecord.TotalHours);
        Assert.Equal("P", finalRecord.Status);
    }
}
