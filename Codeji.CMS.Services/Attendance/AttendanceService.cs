using MapsterMapper;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Calendar;
using ClosedXML.Excel;
using Codeji.CMS.Utility.Helpers;


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

    Task<AttendanceMonthlyExportResult> ExportMonthlyAttendanceAsync(string companyId, int year, int month);
}

public sealed record AttendanceMonthlyExportResult(byte[] Content, string FileName);

    public class AdminAttendanceService : IAdminAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IMapper _mapper;
        private readonly IAttendanceMutationValidator _mutationValidator;
        private readonly IAttendanceAuditWriter _auditWriter;
        private readonly IMongoDbRepository<AttendanceDaySegment> _attendanceSegments;
        private readonly IMongoDbRepository<EmpUser> _employees;
        private readonly ICompanyWorkingCalendarService _workingCalendar;
        private readonly IMongoDbRepository<WeeklyOffSetting> _weeklyOffs;
        private readonly IMongoDbRepository<CalendarEntity> _calendar;
        private readonly IMongoDbRepository<AttendanceStatusSetting> _attendanceStatuses;

        public AdminAttendanceService(IAttendanceRepository attendanceRepository, IMapper mapper, IAttendanceMutationValidator mutationValidator, IAttendanceAuditWriter auditWriter, IMongoDbRepository<AttendanceDaySegment> attendanceSegments, IMongoDbRepository<EmpUser> employees, ICompanyWorkingCalendarService workingCalendar, IMongoDbRepository<WeeklyOffSetting> weeklyOffs, IMongoDbRepository<CalendarEntity> calendar, IMongoDbRepository<AttendanceStatusSetting> attendanceStatuses)
        {
            _attendanceRepository = attendanceRepository;
            _mapper = mapper;
            _mutationValidator = mutationValidator;
            _auditWriter = auditWriter;
            _attendanceSegments = attendanceSegments;
            _employees = employees;
            _workingCalendar = workingCalendar;
            _weeklyOffs = weeklyOffs;
            _calendar = calendar;
            _attendanceStatuses = attendanceStatuses;
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
        // Timing is authoritative: an HR/Admin clock-time correction must be
        // reclassified against the effective company schedule on the server.
        PreserveRequestedStatus = false
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

    var dailyRows = _mapper.Map<IEnumerable<AttendanceResponseDto>>(data);
    var segments = await _attendanceSegments.GetAll(x =>
        x.CompanyId == companyId &&
        x.Date >= fromDate &&
        x.Date <= toDateInclusive &&
        (userIds == null || userIds.Length == 0 || userIds.Contains(x.UserId)));

    // Keep daily rows for compatibility and append segments. The calendar
    // intentionally gives a segment precedence for the matching cell.
    var segmentRows = segments.Select(x => new AttendanceResponseDto
    {
        UserId = x.UserId,
        EmployeeId = x.EmployeeId,
        Date = x.Date,
        CheckInTime = x.CheckInTime,
        CheckOutTime = x.CheckOutTime,
        Status = x.Status,
        Remarks = x.Remarks ?? string.Empty,
        SourceType = x.SourceType,
        SourceId = x.SourceId,
        IsDaySegment = true,
        Segment = x.Segment
    });

    return dailyRows.Concat(segmentRows);
}

public async Task<AttendanceMonthlyExportResult> ExportMonthlyAttendanceAsync(string companyId, int year, int month)
{
    if (year is < 2000 or > 2100 || month is < 1 or > 12)
        throw new ArgumentOutOfRangeException(nameof(month), "A valid report month is required.");

    var monthStart = DateTime.SpecifyKind(new DateTime(year, month, 1), DateTimeKind.Utc);
    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
    var employees = (await _employees.GetAll(x => x.CompanyId == companyId && !x.IsDeleted))
        .OrderBy(x => x.FirstName)
        .ThenBy(x => x.LastName)
        .ToList();
    var userIds = employees.Select(x => x.UserId).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    var attendance = (await _attendanceRepository.GetAllByDateRangeAsync(companyId, monthStart, monthEnd, userIds))
        .ToDictionary(x => (x.UserId ?? string.Empty, x.Date.Date));
    var segments = (await _attendanceSegments.GetAll(x =>
            x.CompanyId == companyId && x.Date >= monthStart && x.Date <= monthEnd))
        .GroupBy(x => (x.UserId, x.Date.Date))
        .ToDictionary(x => x.Key, x => x.OrderBy(segment => segment.Segment).ToList());
    var workingDates = (await _workingCalendar.GetWorkingDatesAsync(companyId, DateOnly.FromDateTime(monthStart), DateOnly.FromDateTime(monthEnd)))
        .ToHashSet();
    var weeklyOffs = (await _weeklyOffs.FirstOrDefault(x => x.CompanyId == companyId))?.OffDays
        ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday];
    var holidays = (await _calendar.GetAll(x => x.CompanyId == companyId &&
            x.Type == Codeji.CMS.Utility.Enums.EnumsHelper.CalendarItem.Holiday &&
            (x.Recurring || (x.Date >= monthStart && x.Date <= monthEnd))))
        .ToList();
    var statusColors = (await _attendanceStatuses.GetAll(x => x.CompanyId == companyId && x.IsActive))
        .Where(x => !string.IsNullOrWhiteSpace(x.Code) && IsHexColor(x.ColorHex))
        .GroupBy(x => x.Code.Trim().ToUpperInvariant())
        .ToDictionary(x => x.Key, x => x.First().ColorHex.Trim());

    using var workbook = new XLWorkbook();
    var sheet = workbook.Worksheets.Add("Attendance report");
    sheet.Cell(1, 1).Value = "Employee ID";
    sheet.Cell(1, 2).Value = "Employee Name";
    for (var day = 0; day < DateTime.DaysInMonth(year, month); day++)
    {
        var date = monthStart.AddDays(day);
        sheet.Cell(1, day + 3).Value = date.ToString("dd MMM\nddd");
    }

    var lastColumn = DateTime.DaysInMonth(year, month) + 2;
    var headerRange = sheet.Range(1, 1, 1, lastColumn);
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("3F2A63");
    headerRange.Style.Font.FontColor = XLColor.White;
    sheet.SheetView.FreezeRows(1);
    sheet.SheetView.FreezeColumns(2);

    var row = 2;
    foreach (var employee in employees)
    {
        sheet.Cell(row, 1).Value = employee.EmployeeId ?? string.Empty;
        sheet.Cell(row, 2).Value = $"{employee.FirstName} {employee.LastName}".Trim();
        for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
        {
            attendance.TryGetValue((employee.UserId, date.Date), out var daily);
            segments.TryGetValue((employee.UserId, date.Date), out var daySegments);
            var cellValue = FormatAttendanceCell(daily, daySegments, employee, date, workingDates, weeklyOffs, holidays);
            var cell = sheet.Cell(row, date.Day + 2);
            cell.Value = cellValue;
            ApplyAttendanceCellStyle(cell, cellValue, statusColors, daily?.Status ?? daySegments?.FirstOrDefault()?.Status);
        }
        sheet.Row(row).Height = 58;
        row++;
    }

    var lastRow = Math.Max(row - 1, 1);
    sheet.Range(1, 1, lastRow, lastColumn).SetAutoFilter();
    sheet.Column(1).Width = 18;
    sheet.Column(2).Width = 26;
    for (var column = 3; column <= lastColumn; column++) sheet.Column(column).Width = 25;
    sheet.Range(1, 1, lastRow, lastColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    sheet.Range(1, 3, lastRow, lastColumn).Style.Alignment.WrapText = true;
    sheet.Range(1, 3, 1, lastColumn).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return new AttendanceMonthlyExportResult(stream.ToArray(), $"attendance-report-{year:D4}-{month:D2}.xlsx");
}

private static string FormatAttendanceCell(AttendanceModel? daily, IReadOnlyCollection<AttendanceDaySegment>? segments,
    EmpUser employee, DateTime date, IReadOnlySet<DateOnly> workingDates, IReadOnlyCollection<int> weeklyOffs,
    IReadOnlyCollection<CalendarEntity> holidays)
{
    var lines = new List<string>();
    if (daily is not null)
    {
        lines.Add(daily.Status);
        if (daily.CheckInTime.HasValue || daily.CheckOutTime.HasValue)
            lines.Add($"In {FormatAttendanceTime(daily.CheckInTime)} · Out {FormatAttendanceTime(daily.CheckOutTime)}");
        if (!string.IsNullOrWhiteSpace(daily.Remarks)) lines.Add(daily.Remarks);
    }

    if (segments is not null)
    {
        foreach (var segment in segments)
        {
            var label = segment.Segment == "FIRST_HALF" ? "First half" : segment.Segment == "SECOND_HALF" ? "Second half" : segment.Segment;
            lines.Add($"{label}: {segment.Status}");
            if (segment.CheckInTime.HasValue || segment.CheckOutTime.HasValue)
                lines.Add($"In {FormatAttendanceTime(segment.CheckInTime)} · Out {FormatAttendanceTime(segment.CheckOutTime)}");
            if (!string.IsNullOrWhiteSpace(segment.Remarks)) lines.Add(segment.Remarks);
        }
    }

    if (lines.Count > 0) return string.Join(Environment.NewLine, lines);

    if (DateOnly.FromDateTime(date) > DateOnly.FromDateTime(IndiaTime.Today)) return "Future date";
    if (DateTime.TryParse(employee.DateOfJoining, out var joiningDate) && date.Date < joiningDate.Date) return "Not eligible";
    if (!workingDates.Contains(DateOnly.FromDateTime(date)))
    {
        var isHoliday = holidays.Any(holiday => CalendarDateHelpers.MatchesDate(holiday, DateOnly.FromDateTime(date)));
        return isHoliday ? "Holiday" : weeklyOffs.Contains((int)date.DayOfWeek) ? "Weekend" : "Holiday";
    }

    return "Absent";
}

private static void ApplyAttendanceCellStyle(IXLCell cell, string value, IReadOnlyDictionary<string, string> statusColors, string? attendanceStatus)
{
    var status = value.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    var normalizedStatus = attendanceStatus?.Trim().ToUpperInvariant() ?? string.Empty;
    var color = statusColors.TryGetValue(normalizedStatus, out var configuredColor)
        ? configuredColor
        : status switch
    {
        "A" or "Absent" => "FCE5E5",
        "Weekend" or "Holiday" => "EEF1F4",
        "Future date" or "Not eligible" => "F4F4F5",
        _ => "FFFFFF"
    };
    cell.Style.Fill.BackgroundColor = XLColor.FromHtml(color);
    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("D9DEE7");
}

private static bool IsHexColor(string? value) => !string.IsNullOrWhiteSpace(value) &&
    System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$");

private static string FormatAttendanceTime(DateTime? time) => time?.ToLocalTime().ToString("HH:mm") ?? "–";
    }
}
