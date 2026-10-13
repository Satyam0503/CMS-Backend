using System.Text.Json;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;

namespace Codeji.CMS.Services.Attendance;

public interface IAttendanceAuditWriter
{
    Task WriteMutationAsync(string companyId, string actorUserId, string action, AttendanceModel attendance, CancellationToken cancellationToken = default);
}

public sealed class AttendanceAuditWriter(
    IMongoDbRepository<AttendanceAuditEvent> audits,
    IMongoDbRepository<AttendanceNotificationOutbox> outbox) : IAttendanceAuditWriter
{
    public async Task WriteMutationAsync(string companyId, string actorUserId, string action, AttendanceModel attendance, CancellationToken cancellationToken = default)
    {
        var snapshot = JsonSerializer.Serialize(new { attendance.UserId, attendance.EmployeeId, attendance.Date, attendance.Status, attendance.CheckInTime, attendance.CheckOutTime, attendance.TotalHours, attendance.SourceType, attendance.Version, attendance.RemarkCode, attendance.OperatorRemark });
        await audits.AddOne(new AttendanceAuditEvent { CompanyId = companyId, AttendanceUserId = attendance.UserId, AttendanceDate = attendance.Date.Date, Action = action, ActorUserId = actorUserId, AttendanceVersion = attendance.Version, SnapshotJson = snapshot, OccurredAtUtc = DateTime.UtcNow });
        if (!string.Equals(actorUserId, attendance.UserId, StringComparison.Ordinal))
            await outbox.AddOne(new AttendanceNotificationOutbox { CompanyId = companyId, EventType = "ATTENDANCE_MUTATED", AggregateId = $"{attendance.UserId}:{attendance.Date:yyyy-MM-dd}:{attendance.Version}", RecipientUserId = attendance.UserId, Title = "Attendance updated", Body = $"Your attendance for {attendance.Date:dd MMM yyyy} was updated.", PayloadJson = snapshot, Status = "Pending", AvailableAtUtc = DateTime.UtcNow });
    }
}
