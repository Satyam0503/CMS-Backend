using MapsterMapper;


namespace Codeji.CMS.Services.Attendance
{
    
public interface IAdminAttendanceService
{
    Task<AttendanceResponseDto> AddManualAttendance(string companyId, string actorUserId, AdminAttendanceCreateDto dto);

    Task<AttendanceResponseDto?> GetByUserAndDate(string companyId, string userId, DateTime date);

    Task<IEnumerable<AttendanceResponseDto>> GetByUser(string companyId, string userId);

    Task<bool> UpdateAttendance(string companyId, string actorUserId, string userId, DateTime date, AttendanceUpdateDto dto);

    Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByDateRange(
        string companyId,
        DateTime fromDate,
        DateTime toDate,
        string[]? userIds);
}

    public class AdminAttendanceService : IAdminAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IMapper _mapper;
        private readonly IAttendanceMutationValidator _mutationValidator;
        private readonly IAttendanceAuditWriter _auditWriter;

        public AdminAttendanceService(IAttendanceRepository attendanceRepository, IMapper mapper, IAttendanceMutationValidator mutationValidator, IAttendanceAuditWriter auditWriter)
        {
            _attendanceRepository = attendanceRepository;
            _mapper = mapper;
            _mutationValidator = mutationValidator;
            _auditWriter = auditWriter;
        }
  public async Task<AttendanceResponseDto> AddManualAttendance(string companyId, string actorUserId, AdminAttendanceCreateDto dto)
{
    if (dto == null)
        throw new ArgumentNullException(nameof(dto));

    var normalizedDate = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc);
    var prepared = await Prepare(companyId, actorUserId, dto.UserId, normalizedDate, dto.Status, dto.CheckInTime, dto.CheckOutTime, dto.Remarks);
    if (!string.IsNullOrWhiteSpace(dto.EmployeeId) && dto.EmployeeId != prepared.Employee.EmployeeId)
        throw new InvalidOperationException("Employee identity does not match the selected user.");
    var result = prepared.Existing is null
        ? await _attendanceRepository.AddAsync(prepared.Attendance)
        : await PersistUpdate(companyId, dto.UserId, normalizedDate, prepared.Attendance);
    await _auditWriter.WriteMutationAsync(companyId, actorUserId, prepared.Existing is null ? "CREATED" : "UPDATED", result);
    return _mapper.Map<AttendanceResponseDto>(result);
}
public async Task<bool> UpdateAttendance(string companyId, string actorUserId, string userId, DateTime date, AttendanceUpdateDto dto)
{
    if (dto == null)
        throw new ArgumentNullException(nameof(dto));

    var normalizedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    var existing = await _attendanceRepository.GetByUserAndDateAsync(companyId, userId, normalizedDate);
    if (existing is null) return false;
    var prepared = await Prepare(companyId, actorUserId, userId, normalizedDate, dto.Status, dto.CheckInTime, dto.CheckOutTime, dto.Remarks, existing.Version);
    await PersistUpdate(companyId, userId, date.Date, prepared.Attendance);
    await _auditWriter.WriteMutationAsync(companyId, actorUserId, "UPDATED", prepared.Attendance);
    return true;
}

private async Task<PreparedAttendanceMutation> Prepare(string companyId, string actorUserId, string userId, DateTime date, string status, string? checkIn, string? checkOut, string? remarks, long? expectedVersion = null)
{
    var hasTimes = !string.IsNullOrWhiteSpace(checkIn) || !string.IsNullOrWhiteSpace(checkOut);
    TimeOnly? inTime = null, outTime = null;
    if (hasTimes)
    {
        if (!TimeOnly.TryParse(checkIn, out var parsedIn) || !TimeOnly.TryParse(checkOut, out var parsedOut))
            throw new InvalidOperationException("Invalid check-in or check-out time. Expected HH:mm.");
        inTime = parsedIn; outTime = parsedOut;
    }
    var validation = await _mutationValidator.PrepareAsync(new AttendanceMutationRequest
    {
        CompanyId = companyId, ActorUserId = actorUserId, TargetUserId = userId,
        AttendanceDate = DateOnly.FromDateTime(date), StatusCode = status,
        TimingMode = hasTimes ? AttendanceTimingMode.Custom : AttendanceTimingMode.Auto,
        CheckInTime = inTime, CheckOutTime = outTime, OperatorRemark = remarks,
        ExpectedAttendanceVersion = expectedVersion,
        // This is the HR/Admin attendance editor: preserve its explicit selection.
        PreserveRequestedStatus = true
    });
    if (!validation.Success || validation.MethodResult is null) throw new InvalidOperationException(validation.Message ?? "Attendance mutation was rejected.");
    return validation.MethodResult;
}

private async Task<AttendanceModel> PersistUpdate(string companyId, string userId, DateTime date, AttendanceModel row)
{
    var updated = await _attendanceRepository.UpdateAsync(companyId, userId, date, row);
    if (!updated) throw new InvalidOperationException("Attendance record could not be updated.");
    return row;
}


public async Task<AttendanceResponseDto?> GetByUserAndDate(string companyId, string userId, DateTime date)
{
    var normalizedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    var attendance = await _attendanceRepository
        .GetByUserAndDateAsync(companyId, userId, normalizedDate);

    return attendance == null ? null : _mapper.Map<AttendanceResponseDto>(attendance);
}

public async Task<IEnumerable<AttendanceResponseDto>> GetByUser(string companyId, string userId)
{
    var data = await _attendanceRepository
        .GetAllByUserAsync(companyId, userId);

    return _mapper.Map<IEnumerable<AttendanceResponseDto>>(data);
}




public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByDateRange(
    string companyId,
    DateTime from,
    DateTime to,
    string[]? userIds)
{
    if (from.Date > to.Date) throw new ArgumentException("From date cannot be after to date.");
    var fromDate = from.Date;
    var toDateInclusive = to.Date.AddDays(1).AddTicks(-1);

    var data = await _attendanceRepository
        .GetAllByDateRangeAsync(companyId, fromDate, toDateInclusive, userIds);

    return _mapper.Map<IEnumerable<AttendanceResponseDto>>(data);
}

    }
}
