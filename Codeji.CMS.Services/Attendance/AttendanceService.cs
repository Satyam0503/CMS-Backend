using MapsterMapper;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;


namespace Codeji.CMS.Services.Attendance
{
    
public interface IAdminAttendanceService
{
    Task<AttendanceResponseDto> AddManualAttendance(string companyId, AdminAttendanceCreateDto dto);

    Task<AttendanceResponseDto?> GetByUserAndDate(string companyId, string userId, DateTime date);

    Task<IEnumerable<AttendanceResponseDto>> GetByUser(string companyId, string userId);

    Task<bool> UpdateAttendance(string companyId, string userId, DateTime date, AttendanceUpdateDto dto);

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
        private readonly IAttendanceEditGuard _editGuard;
        private readonly IMongoDbRepository<EmpUser> _employees;
        private readonly IMongoDbRepository<AttendanceStatusSetting> _statusSettings;

        public AdminAttendanceService(IAttendanceRepository attendanceRepository, IMapper mapper, IAttendanceEditGuard editGuard,
            IMongoDbRepository<EmpUser> employees, IMongoDbRepository<AttendanceStatusSetting> statusSettings)
        {
            _attendanceRepository = attendanceRepository;
            _mapper = mapper;
            _editGuard = editGuard;
            _employees = employees;
            _statusSettings = statusSettings;
        }
            private static readonly TimeSpan OFFICE_START = TimeSpan.FromHours(9);
            private static readonly TimeSpan OFFICE_END = TimeSpan.FromHours(18);
            private const decimal REQUIRED_WORKING_HOURS = 8m;
            private static readonly HashSet<string> NO_TIME_STATUSES =
            [
                "A", "L", "SL", "CL", "EL", "COMP-OFF", "CL-HALF", "SL-HALF"
            ];


  public async Task<AttendanceResponseDto> AddManualAttendance(string companyId, AdminAttendanceCreateDto dto)
{
    if (dto == null)
        throw new ArgumentNullException(nameof(dto));

    var attendanceDate = dto.Date.Date;
    var employee = await ResolveEmployee(companyId, dto.UserId);
    if (!string.IsNullOrWhiteSpace(dto.EmployeeId) && dto.EmployeeId != employee.EmployeeId)
        throw new InvalidOperationException("Employee identity does not match the selected user.");
    await _editGuard.EnsureEditableWorkingDayAsync(dto.UserId, attendanceDate);
    var (checkIn, checkOut, requiresTime) = await Normalize(companyId, dto.Status, attendanceDate, dto.CheckInTime, dto.CheckOutTime);

    var existing = await _attendanceRepository
        .GetByUserAndDateAsync(companyId, dto.UserId, attendanceDate);
           

    if (existing != null)
    {
        existing.Status = dto.Status.Trim().ToUpperInvariant();
        existing.Remarks = dto.Remarks;
        existing.CheckInTime = checkIn;
        existing.CheckOutTime = checkOut;
        existing.EmployeeId = employee.EmployeeId;
        existing.CompanyId = companyId;


        CalculateTotalHours(existing);

        await _attendanceRepository
            .UpdateAsync(companyId, dto.UserId, attendanceDate, existing);

        return _mapper.Map<AttendanceResponseDto>(existing);
    }
var entity = new AttendanceModel
{
    UserId = dto.UserId,
    EmployeeId = employee.EmployeeId,
    CompanyId = companyId,
    Date = attendanceDate,
    Status = dto.Status.Trim().ToUpperInvariant(),
    Remarks = dto.Remarks,
    CheckInTime = checkIn,
    CheckOutTime = checkOut
};


    CalculateTotalHours(entity);

    var result = await _attendanceRepository.AddAsync(entity);

    return _mapper.Map<AttendanceResponseDto>(result);
}
public async Task<bool> UpdateAttendance(string companyId, string userId, DateTime date, AttendanceUpdateDto dto)
{
    if (dto == null)
        throw new ArgumentNullException(nameof(dto));

    var attendanceDate = date.Date;
    await ResolveEmployee(companyId, userId);
    await _editGuard.EnsureEditableWorkingDayAsync(userId, attendanceDate);

    var existing = await _attendanceRepository
        .GetByUserAndDateAsync(companyId, userId, attendanceDate);

    if (existing == null)
        return false;

    var (checkIn, checkOut, requiresTime) = await Normalize(companyId, dto.Status, attendanceDate, dto.CheckInTime, dto.CheckOutTime);

    existing.Status = dto.Status.Trim().ToUpperInvariant();
    existing.Remarks = dto.Remarks;

    if (!requiresTime)
    {
        existing.CheckInTime = null;
        existing.CheckOutTime = null;
        existing.TotalHours = 0;
        existing.LateCount = 0;
        existing.EarlyExitCount = 0;
    }
    else
    {
        existing.CheckInTime = checkIn;
        existing.CheckOutTime = checkOut;

        CalculateTotalHours(existing);
    }

    return await _attendanceRepository
        .UpdateAsync(companyId, userId, attendanceDate, existing);
}


public async Task<AttendanceResponseDto?> GetByUserAndDate(string companyId, string userId, DateTime date)
{
    var attendance = await _attendanceRepository
        .GetByUserAndDateAsync(companyId, userId, date.Date);

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


private void ApplyHalfDayOrManualTimes(
    AttendanceModel entity,
    string status,
    DateTime date,
    DateTime? checkIn,
    DateTime? checkOut)
        {
            if ((status == "HD" || status == "LHD" || status == "WFH-HD")
                && !checkIn.HasValue && !checkOut.HasValue)
            {
                var defaultCheckIn = date.Date.AddHours(9);
                entity.CheckInTime = defaultCheckIn;
                entity.CheckOutTime = defaultCheckIn.AddHours(4);
            }
            else
            {
                if (checkIn.HasValue) entity.CheckInTime = checkIn;
                if (checkOut.HasValue) entity.CheckOutTime = checkOut;
            }
            
}
private void CalculateTotalHours(AttendanceModel entity)
{
    if (!entity.CheckInTime.HasValue || !entity.CheckOutTime.HasValue)
    {
        entity.TotalHours = null;
        entity.LateCount = 0;
        entity.EarlyExitCount = 0;
        return;
    }

    if (entity.CheckOutTime <= entity.CheckInTime)
    {
        entity.TotalHours = 0;
        return;
    }

    var rawHours = (entity.CheckOutTime.Value - entity.CheckInTime.Value).TotalHours;

    entity.TotalHours = (decimal)Math.Max(rawHours - 1, 0);

    UpdateLateEarlyStatus(entity);
}

private async Task<EmpUser> ResolveEmployee(string companyId, string userId) =>
    await _employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == userId && x.Status && !x.IsDeleted)
    ?? throw new InvalidOperationException("Employee does not belong to the authenticated company.");

private async Task<(DateTime? CheckIn, DateTime? CheckOut, bool RequiresTime)> Normalize(
    string companyId, string statusCode, DateTime date, string? checkInText, string? checkOutText)
{
    var code = statusCode.Trim().ToUpperInvariant();
    var status = await _statusSettings.FirstOrDefault(x => x.CompanyId == companyId && x.Code == code && x.IsActive);
    if (status == null) throw new InvalidOperationException("Attendance status is unknown or inactive. Initialize company attendance settings before entering attendance.");
    if (!status.RequiresTime) return (null, null, false);
    if (!TimeSpan.TryParseExact(checkInText, ["hh\\:mm", "h\\:mm"], null, out var checkIn))
        throw new InvalidOperationException("Invalid check-in time. Expected HH:mm.");
    if (!TimeSpan.TryParseExact(checkOutText, ["hh\\:mm", "h\\:mm"], null, out var checkOut))
        throw new InvalidOperationException("Invalid check-out time. Expected HH:mm.");
    if (checkIn >= TimeSpan.FromDays(1) || checkOut >= TimeSpan.FromDays(1) || checkOut <= checkIn)
        throw new InvalidOperationException("Check-out time must be later than check-in time on the same day.");
    return (date.Add(checkIn), date.Add(checkOut), true);
}


private void UpdateLateEarlyStatus(AttendanceModel entity)
{
    entity.LateCount = 0;
    entity.EarlyExitCount = 0;

    if (!entity.CheckInTime.HasValue || !entity.CheckOutTime.HasValue)
        return;

    if (!entity.TotalHours.HasValue)
        return;

    var checkInTime = entity.CheckInTime.Value.TimeOfDay;

    if (checkInTime > OFFICE_START)
        entity.LateCount = 1;

    if (entity.TotalHours < REQUIRED_WORKING_HOURS)
        entity.EarlyExitCount = 1;
}




    }
}
