using MapsterMapper;


namespace Codeji.CMS.Services.Attendance
{
    
    public interface IAdminAttendanceService
{
    Task<AttendanceResponseDto> AddManualAttendance(AdminAttendanceCreateDto dto);

    Task<AttendanceResponseDto?> GetByUserAndDate(string userId, DateTime date);

    Task<IEnumerable<AttendanceResponseDto>> GetByUser(string userId);

    Task<bool> UpdateAttendance(string userId, DateTime date, AttendanceUpdateDto dto);

    Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByDateRange(
        DateTime fromDate,
        DateTime toDate,
        string[]? userIds);
}

    public class AdminAttendanceService : IAdminAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IMapper _mapper;
        private readonly IAttendanceEditGuard _editGuard;

        public AdminAttendanceService(IAttendanceRepository attendanceRepository, IMapper mapper, IAttendanceEditGuard editGuard)
        {
            _attendanceRepository = attendanceRepository;
            _mapper = mapper;
            _editGuard = editGuard;
        }
            private static readonly TimeSpan OFFICE_START = TimeSpan.FromHours(9);
            private static readonly TimeSpan OFFICE_END = TimeSpan.FromHours(18);
            private const decimal REQUIRED_WORKING_HOURS = 8m;
            private static readonly HashSet<string> NO_TIME_STATUSES =
            [
                "A", "L", "SL", "CL", "EL", "COMP-OFF", "CL-HALF", "SL-HALF"
            ];


  public async Task<AttendanceResponseDto> AddManualAttendance(AdminAttendanceCreateDto dto)
{
    if (dto == null)
        throw new ArgumentNullException(nameof(dto));

    var attendanceDate = dto.Date.Date;
    await _editGuard.EnsureEditableWorkingDayAsync(dto.UserId, attendanceDate);

    DateTime? checkIn = null;
    DateTime? checkOut = null;

    if (!string.IsNullOrWhiteSpace(dto.CheckInTime) &&
        TimeSpan.TryParse(dto.CheckInTime, out var checkInTs))
    {
        checkIn = attendanceDate.Add(checkInTs);
    }

    if (!string.IsNullOrWhiteSpace(dto.CheckOutTime) &&
        TimeSpan.TryParse(dto.CheckOutTime, out var checkOutTs))
    {
        checkOut = attendanceDate.Add(checkOutTs);
    }

    var existing = await _attendanceRepository
        .GetByUserAndDateAsync(dto.UserId, attendanceDate);
           

    if (existing != null)
    {
        existing.Status = dto.Status;
        existing.Remarks = dto.Remarks;
        existing.CheckInTime = checkIn;
        existing.CheckOutTime = checkOut;
        existing.EmployeeId = dto.EmployeeId;


        CalculateTotalHours(existing);

        await _attendanceRepository
            .UpdateAsync(dto.UserId, attendanceDate, existing);

        return _mapper.Map<AttendanceResponseDto>(existing);
    }
var entity = new AttendanceModel
{
    UserId = dto.UserId,
    EmployeeId = dto.EmployeeId,   
    Date = attendanceDate,
    Status = dto.Status,
    Remarks = dto.Remarks,
    CheckInTime = checkIn,
    CheckOutTime = checkOut
};


    CalculateTotalHours(entity);

    var result = await _attendanceRepository.AddAsync(entity);

    return _mapper.Map<AttendanceResponseDto>(result);
}
public async Task<bool> UpdateAttendance(string userId, DateTime date, AttendanceUpdateDto dto)
{
    if (dto == null)
        throw new ArgumentNullException(nameof(dto));

    var attendanceDate = date.Date;
    await _editGuard.EnsureEditableWorkingDayAsync(userId, attendanceDate);

    var existing = await _attendanceRepository
        .GetByUserAndDateAsync(userId, attendanceDate);

    if (existing == null)
        return false;

    DateTime? checkIn = null;
    DateTime? checkOut = null;

    if (!string.IsNullOrWhiteSpace(dto.CheckInTime) &&
        TimeSpan.TryParse(dto.CheckInTime, out var checkInTs))
    {
        checkIn = attendanceDate.Add(checkInTs);
    }

    if (!string.IsNullOrWhiteSpace(dto.CheckOutTime) &&
        TimeSpan.TryParse(dto.CheckOutTime, out var checkOutTs))
    {
        checkOut = attendanceDate.Add(checkOutTs);
    }

    existing.Status = dto.Status;
    existing.Remarks = dto.Remarks;

    if (NO_TIME_STATUSES.Contains(dto.Status))
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
        .UpdateAsync(userId, attendanceDate, existing);
}


public async Task<AttendanceResponseDto?> GetByUserAndDate(string userId, DateTime date)
{
    var attendance = await _attendanceRepository
        .GetByUserAndDateAsync(userId, date.Date);

    return attendance == null ? null : _mapper.Map<AttendanceResponseDto>(attendance);
}

public async Task<IEnumerable<AttendanceResponseDto>> GetByUser(string userId)
{
    var data = await _attendanceRepository
        .GetAllByUserAsync(userId);

    return _mapper.Map<IEnumerable<AttendanceResponseDto>>(data);
}




public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByDateRange(
    DateTime from,
    DateTime to,
    string[]? userIds)
{
    var fromDate = from.Date;
    var toDateInclusive = to.Date.AddDays(1).AddTicks(-1);

    var data = await _attendanceRepository
        .GetAllByDateRangeAsync(fromDate, toDateInclusive, userIds);

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
