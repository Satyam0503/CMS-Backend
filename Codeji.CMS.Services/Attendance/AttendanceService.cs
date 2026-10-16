using ClosedXML.Excel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Utility.Helpers;
using MapsterMapper;

namespace Codeji.CMS.Services.Attendance
{
    public interface IAdminAttendanceService
    {
        Task<AttendanceResponseDto> AddManualAttendance(
            string companyId,
            string actorUserId,
            AdminAttendanceCreateDto dto
        );

        Task<AttendanceResponseDto?> GetByUserAndDate(
            string companyId,
            string userId,
            DateTime date
        );

        Task<IEnumerable<AttendanceResponseDto>> GetByUser(string companyId, string userId);

        Task<bool> UpdateAttendance(
            string companyId,
            string actorUserId,
            string userId,
            DateTime date,
            AttendanceUpdateDto dto
        );

        Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByDateRange(
            string companyId,
            DateTime fromDate,
            DateTime toDate,
            string[]? userIds
        );

        Task<AttendanceMonthlyExportResult> ExportMonthlyAttendanceAsync(
            string companyId,
            int year,
            int month,
            string? departmentId = null,
            string? search = null,
            string? employmentStatus = null,
            bool onlyEmployed = true
        );
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
        private readonly IMongoDbRepository<Department>? _departments;
        private readonly IAttendanceInitializationService? _initializationService;

        public AdminAttendanceService(
            IAttendanceRepository attendanceRepository,
            IMapper mapper,
            IAttendanceMutationValidator mutationValidator,
            IAttendanceAuditWriter auditWriter,
            IMongoDbRepository<AttendanceDaySegment> attendanceSegments,
            IMongoDbRepository<EmpUser> employees,
            ICompanyWorkingCalendarService workingCalendar,
            IMongoDbRepository<WeeklyOffSetting> weeklyOffs,
            IMongoDbRepository<CalendarEntity> calendar,
            IMongoDbRepository<AttendanceStatusSetting> attendanceStatuses,
            IMongoDbRepository<Department>? departments = null,
            IAttendanceInitializationService? initializationService = null
        )
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
            _departments = departments;
            _initializationService = initializationService;
        }

        public async Task<AttendanceResponseDto> AddManualAttendance(
            string companyId,
            string actorUserId,
            AdminAttendanceCreateDto dto
        )
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var normalizedDate = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc);
            var prepared = await Prepare(
                companyId,
                actorUserId,
                dto.UserId,
                normalizedDate,
                dto.Status,
                dto.CheckInTime,
                dto.CheckOutTime,
                dto.Remarks
            );
            if (
                !string.IsNullOrWhiteSpace(dto.EmployeeId)
                && dto.EmployeeId != prepared.Employee.EmployeeId
            )
                throw new InvalidOperationException(
                    "Employee identity does not match the selected user."
                );
            var result = prepared.Existing is null
                ? await _attendanceRepository.AddAsync(prepared.Attendance)
                : await PersistUpdate(companyId, dto.UserId, normalizedDate, prepared.Attendance);
            await _auditWriter.WriteMutationAsync(
                companyId,
                actorUserId,
                prepared.Existing is null ? "CREATED" : "UPDATED",
                result
            );
            return _mapper.Map<AttendanceResponseDto>(result);
        }

        public async Task<bool> UpdateAttendance(
            string companyId,
            string actorUserId,
            string userId,
            DateTime date,
            AttendanceUpdateDto dto
        )
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var normalizedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var existing = await _attendanceRepository.GetByUserAndDateAsync(
                companyId,
                userId,
                normalizedDate
            );
            if (existing is null)
                return false;
            var prepared = await Prepare(
                companyId,
                actorUserId,
                userId,
                normalizedDate,
                dto.Status,
                dto.CheckInTime,
                dto.CheckOutTime,
                dto.Remarks,
                existing.Version
            );
            await PersistUpdate(companyId, userId, date.Date, prepared.Attendance);
            await _auditWriter.WriteMutationAsync(
                companyId,
                actorUserId,
                "UPDATED",
                prepared.Attendance
            );
            return true;
        }

        private async Task<PreparedAttendanceMutation> Prepare(
            string companyId,
            string actorUserId,
            string userId,
            DateTime date,
            string status,
            string? checkIn,
            string? checkOut,
            string? remarks,
            long? expectedVersion = null
        )
        {
            var hasTimes =
                !string.IsNullOrWhiteSpace(checkIn) || !string.IsNullOrWhiteSpace(checkOut);
            TimeOnly? inTime = null,
                outTime = null;
            if (hasTimes)
            {
                if (!string.IsNullOrWhiteSpace(checkIn))
                {
                    if (!TimeOnly.TryParse(checkIn, out var parsedIn))
                        throw new InvalidOperationException(
                            "Invalid check-in time. Expected HH:mm."
                        );
                    inTime = parsedIn;
                }

                if (!string.IsNullOrWhiteSpace(checkOut))
                {
                    if (!TimeOnly.TryParse(checkOut, out var parsedOut))
                        throw new InvalidOperationException(
                            "Invalid check-out time. Expected HH:mm."
                        );
                    outTime = parsedOut;
                }

                // Sensible pairings if only one punch time was provided
                if (inTime == null && outTime != null)
                    inTime = string.Equals(status, "LHD", StringComparison.OrdinalIgnoreCase)
                        ? new TimeOnly(13, 30)
                        : new TimeOnly(9, 0);
                if (outTime == null && inTime != null)
                    outTime = string.Equals(status, "ED", StringComparison.OrdinalIgnoreCase)
                        ? new TimeOnly(14, 0)
                        : new TimeOnly(18, 0);
            }
            var validation = await _mutationValidator.PrepareAsync(
                new AttendanceMutationRequest
                {
                    CompanyId = companyId,
                    ActorUserId = actorUserId,
                    TargetUserId = userId,
                    AttendanceDate = DateOnly.FromDateTime(date),
                    StatusCode = status,
                    TimingMode = hasTimes ? AttendanceTimingMode.Custom : AttendanceTimingMode.Auto,
                    CheckInTime = inTime,
                    CheckOutTime = outTime,
                    OperatorRemark = remarks,
                    ExpectedAttendanceVersion = expectedVersion,
                    PreserveRequestedStatus = true,
                }
            );
            if (!validation.Success || validation.MethodResult is null)
                throw new InvalidOperationException(
                    validation.Message ?? "Attendance mutation was rejected."
                );
            return validation.MethodResult;
        }

        private async Task<AttendanceModel> PersistUpdate(
            string companyId,
            string userId,
            DateTime date,
            AttendanceModel row
        )
        {
            var updated = await _attendanceRepository.UpdateAsync(companyId, userId, date, row);
            if (!updated)
                throw new InvalidOperationException("Attendance record could not be updated.");
            return row;
        }

        public async Task<AttendanceResponseDto?> GetByUserAndDate(
            string companyId,
            string userId,
            DateTime date
        )
        {
            var normalizedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var attendance = await _attendanceRepository.GetByUserAndDateAsync(
                companyId,
                userId,
                normalizedDate
            );

            return attendance == null ? null : _mapper.Map<AttendanceResponseDto>(attendance);
        }

        public async Task<IEnumerable<AttendanceResponseDto>> GetByUser(
            string companyId,
            string userId
        )
        {
            var data = await _attendanceRepository.GetAllByUserAsync(companyId, userId);

            return _mapper.Map<IEnumerable<AttendanceResponseDto>>(data);
        }

        public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceByDateRange(
            string companyId,
            DateTime from,
            DateTime to,
            string[]? userIds
        )
        {
            if (from.Date > to.Date)
                throw new ArgumentException("From date cannot be after to date.");
            var fromDate = from.Date;
            var toDateInclusive = to.Date.AddDays(1).AddTicks(-1);

            if (
                _initializationService != null
                && userIds != null
                && userIds.Length > 0
                && userIds.Length <= 20
            )
            {
                foreach (var uid in userIds)
                {
                    if (!string.IsNullOrWhiteSpace(uid))
                    {
                        try
                        {
                            await _initializationService.InitializeEmployeeAttendanceAsync(
                                companyId,
                                "system",
                                uid,
                                from,
                                CancellationToken.None
                            );
                        }
                        catch
                        {
                            // Non-blocking: calendar query still succeeds if initialization encounters an exception
                        }
                    }
                }
            }

            var data = await _attendanceRepository.GetAllByDateRangeAsync(
                companyId,
                fromDate,
                toDateInclusive,
                userIds
            );

            var dailyRows = _mapper.Map<IEnumerable<AttendanceResponseDto>>(data);
            if (userIds != null && userIds.Length > 0 && userIds.Length <= 20)
            {
                var emps = (
                    await _employees.GetAll(x =>
                        x.CompanyId == companyId && userIds.Contains(x.UserId)
                    )
                ).ToDictionary(x => x.UserId, x => x);

                dailyRows = dailyRows
                    .Where(r =>
                    {
                        if (r.UserId != null && emps.TryGetValue(r.UserId, out var emp))
                        {
                            if (
                                EmploymentPeriod.TryGetJoiningDate(emp, out var j)
                                && r.Date.Date < j.Date
                            )
                                return false;
                            var l = EmploymentPeriod.GetLastWorkingDate(emp);
                            if (l.HasValue && r.Date.Date > l.Value.Date)
                                return false;
                        }
                        return true;
                    })
                    .ToList();
            }

            var segments = await _attendanceSegments.GetAll(x =>
                x.CompanyId == companyId
                && x.Date >= fromDate
                && x.Date <= toDateInclusive
                && (userIds == null || userIds.Length == 0 || userIds.Contains(x.UserId))
            );

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
                Segment = x.Segment,
            });

            return dailyRows.Concat(segmentRows);
        }

        public async Task<AttendanceMonthlyExportResult> ExportMonthlyAttendanceAsync(
            string companyId,
            int year,
            int month,
            string? departmentId = null,
            string? search = null,
            string? employmentStatus = null,
            bool onlyEmployed = true
        )
        {
            if (year is < 2000 or > 2100 || month is < 1 or > 12)
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "A valid report month is required."
                );

            var monthStart = DateTime.SpecifyKind(new DateTime(year, month, 1), DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var allEmployees = (
                await _employees.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)
            )
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToList();

            var employees = allEmployees
                .Where(emp =>
                {
                    // 1. Department filter
                    if (
                        !string.IsNullOrWhiteSpace(departmentId)
                        && !string.Equals(
                            emp.Department,
                            departmentId.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return false;
                    }

                    // 2. Search filter (ID, name, email)
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var term = search.Trim();
                        var fullName = $"{emp.FirstName} {emp.LastName}".Trim();
                        var match =
                            (
                                emp.EmployeeId != null
                                && emp.EmployeeId.Contains(term, StringComparison.OrdinalIgnoreCase)
                            )
                            || fullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                            || (
                                emp.Email != null
                                && emp.Email.Contains(term, StringComparison.OrdinalIgnoreCase)
                            );
                        if (!match)
                            return false;
                    }

                    // 3. Employment status filter
                    if (!string.IsNullOrWhiteSpace(employmentStatus))
                    {
                        var normalizedFilter = employmentStatus.Trim();
                        if (
                            string.Equals(
                                normalizedFilter,
                                "All",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                            return true;

                        if (
                            string.Equals(
                                normalizedFilter,
                                "Employed",
                                StringComparison.OrdinalIgnoreCase
                            )
                            || string.Equals(
                                normalizedFilter,
                                "Active",
                                StringComparison.OrdinalIgnoreCase
                            )
                            || string.Equals(
                                normalizedFilter,
                                "AttendanceRoster",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            return emp.Status
                                && !EmploymentExitRules.IsFormerEmployee(emp)
                                && EmploymentPeriod.IsEligibleForMonth(emp, monthStart);
                        }

                        var empStatus = EmploymentExitRules.GetEffectiveEmploymentStatus(emp);
                        return string.Equals(
                            empStatus,
                            normalizedFilter,
                            StringComparison.OrdinalIgnoreCase
                        );
                    }

                    // 4. Default: only employed employees who were active/eligible in the requested month
                    if (onlyEmployed)
                    {
                        if (!emp.Status || EmploymentExitRules.IsFormerEmployee(emp))
                            return false;

                        if (!EmploymentPeriod.IsEligibleForMonth(emp, monthStart))
                            return false;
                    }

                    return true;
                })
                .ToList();

            var userIds = employees
                .Select(x => x.UserId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
            var attendance = (
                await _attendanceRepository.GetAllByDateRangeAsync(
                    companyId,
                    monthStart,
                    monthEnd,
                    userIds
                )
            ).ToDictionary(x => (x.UserId ?? string.Empty, x.Date.Date));
            var segments = (
                await _attendanceSegments.GetAll(x =>
                    x.CompanyId == companyId && x.Date >= monthStart && x.Date <= monthEnd
                )
            )
                .GroupBy(x => (x.UserId, x.Date.Date))
                .ToDictionary(x => x.Key, x => x.OrderBy(segment => segment.Segment).ToList());
            var workingDates = (
                await _workingCalendar.GetWorkingDatesAsync(
                    companyId,
                    DateOnly.FromDateTime(monthStart),
                    DateOnly.FromDateTime(monthEnd)
                )
            ).ToHashSet();
            var weeklyOffs =
                (await _weeklyOffs.FirstOrDefault(x => x.CompanyId == companyId))?.OffDays
                ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday];
            var holidays = (
                await _calendar.GetAll(x =>
                    x.CompanyId == companyId
                    && x.Type == Codeji.CMS.Utility.Enums.EnumsHelper.CalendarItem.Holiday
                    && (
                        x.Recurring
                        || (x.Date >= monthStart.AddDays(-1) && x.Date <= monthEnd.AddDays(1))
                    )
                )
            ).ToList();
            var allStatuses = (
                await _attendanceStatuses.GetAll(x => x.CompanyId == companyId && x.IsActive)
            ).ToList();
            var statusColors = allStatuses
                .Where(x => !string.IsNullOrWhiteSpace(x.Code) && IsHexColor(x.ColorHex))
                .GroupBy(x => x.Code.Trim().ToUpperInvariant())
                .ToDictionary(x => x.Key, x => x.First().ColorHex.Trim());
            var statusSettingsMap = allStatuses
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code.Trim().ToUpperInvariant())
                .ToDictionary(x => x.Key, x => x.First());

            Dictionary<string, string> departmentMap = [];
            if (_departments != null)
            {
                try
                {
                    var depts = await _departments.GetAll(x =>
                        x.CompanyId == companyId && !x.IsDeleted
                    );
                    departmentMap = depts
                        .Where(d => !string.IsNullOrWhiteSpace(d.DepartmentId))
                        .ToDictionary(
                            d => d.DepartmentId,
                            d =>
                                d.Titles?.FirstOrDefault(t => t.Language == "en")?.Label
                                ?? d.DepartmentId
                        );
                }
                catch
                {
                    // Graceful fallback if department repository fails
                }
            }

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Attendance report");
            sheet.Cell(1, 1).Value = "Employee ID";
            sheet.Cell(1, 2).Value = "Employee Name";
            sheet.Cell(1, 3).Value = "Department";
            sheet.Cell(1, 4).Value = "Designation";

            var daysInMonth = DateTime.DaysInMonth(year, month);
            for (var day = 0; day < daysInMonth; day++)
            {
                var date = monthStart.AddDays(day);
                sheet.Cell(1, day + 5).Value = date.ToString("dd MMM\nddd");
            }

            var summaryStartCol = daysInMonth + 5;
            sheet.Cell(1, summaryStartCol).Value = "Present\n(P)";
            sheet.Cell(1, summaryStartCol + 1).Value = "Half Day\n(HD)";
            sheet.Cell(1, summaryStartCol + 2).Value = "Absent\n(A)";
            sheet.Cell(1, summaryStartCol + 3).Value = "Leave\n(L)";
            sheet.Cell(1, summaryStartCol + 4).Value = "Weekly Off\n(WO)";
            sheet.Cell(1, summaryStartCol + 5).Value = "Holiday\n(H)";
            sheet.Cell(1, summaryStartCol + 6).Value = "Total Paid\nDays";

            var lastColumn = daysInMonth + 11;
            var headerRange = sheet.Range(1, 1, 1, lastColumn);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("3F2A63");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Alignment.WrapText = true;
            sheet.Row(1).Height = 32;

            var summaryHeaderRange = sheet.Range(1, summaryStartCol, 1, lastColumn);
            summaryHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("2D1E47");

            sheet.SheetView.FreezeRows(1);
            sheet.SheetView.FreezeColumns(4);

            var row = 2;
            foreach (var employee in employees)
            {
                sheet.Cell(row, 1).Value = employee.EmployeeId ?? string.Empty;
                sheet.Cell(row, 2).Value = $"{employee.FirstName} {employee.LastName}".Trim();
                var deptName =
                    !string.IsNullOrWhiteSpace(employee.Department)
                    && departmentMap.TryGetValue(employee.Department, out var dName)
                        ? dName
                        : employee.Department ?? string.Empty;
                sheet.Cell(row, 3).Value = deptName;
                sheet.Cell(row, 4).Value = employee.JobRole ?? string.Empty;

                sheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                sheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                sheet.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                sheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                decimal presentCount = 0;
                decimal halfDayCount = 0;
                decimal absentCount = 0;
                decimal leaveCount = 0;
                decimal weeklyOffCount = 0;
                decimal holidayCount = 0;
                decimal paidDays = 0;

                for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
                {
                    attendance.TryGetValue((employee.UserId, date.Date), out var daily);
                    segments.TryGetValue((employee.UserId, date.Date), out var daySegments);
                    var (cellValue, statusCode) = FormatAttendanceCell(
                        daily,
                        daySegments,
                        employee,
                        date,
                        workingDates,
                        weeklyOffs,
                        holidays
                    );
                    var cell = sheet.Cell(row, date.Day + 4);
                    cell.Value = cellValue;
                    ApplyAttendanceCellStyle(
                        cell,
                        cellValue,
                        statusColors,
                        daily?.Status ?? daySegments?.FirstOrDefault()?.Status
                    );

                    TallyAttendanceDay(
                        daily,
                        daySegments,
                        statusCode,
                        statusSettingsMap,
                        ref presentCount,
                        ref halfDayCount,
                        ref absentCount,
                        ref leaveCount,
                        ref weeklyOffCount,
                        ref holidayCount,
                        ref paidDays
                    );
                }

                // Summary columns
                var pCell = sheet.Cell(row, summaryStartCol);
                pCell.Value = presentCount;
                pCell.Style.NumberFormat.Format = "0.#";

                var hdCell = sheet.Cell(row, summaryStartCol + 1);
                hdCell.Value = halfDayCount;
                hdCell.Style.NumberFormat.Format = "0.#";

                var aCell = sheet.Cell(row, summaryStartCol + 2);
                aCell.Value = absentCount;
                aCell.Style.NumberFormat.Format = "0.#";

                var lCell = sheet.Cell(row, summaryStartCol + 3);
                lCell.Value = leaveCount;
                lCell.Style.NumberFormat.Format = "0.#";

                var woCell = sheet.Cell(row, summaryStartCol + 4);
                woCell.Value = weeklyOffCount;
                woCell.Style.NumberFormat.Format = "0.#";

                var hCell = sheet.Cell(row, summaryStartCol + 5);
                hCell.Value = holidayCount;
                hCell.Style.NumberFormat.Format = "0.#";

                var pdCell = sheet.Cell(row, summaryStartCol + 6);
                pdCell.Value = paidDays;
                pdCell.Style.NumberFormat.Format = "0.#";
                pdCell.Style.Font.Bold = true;
                pdCell.Style.Fill.BackgroundColor = XLColor.FromHtml("E8F0FE");

                for (var sc = summaryStartCol; sc <= lastColumn; sc++)
                {
                    sheet.Cell(row, sc).Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;
                    sheet.Cell(row, sc).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    sheet.Cell(row, sc).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    sheet.Cell(row, sc).Style.Border.OutsideBorderColor = XLColor.FromHtml(
                        "D9DEE7"
                    );
                }

                sheet.Row(row).Height = 44;
                row++;
            }

            var lastRow = Math.Max(row - 1, 1);
            sheet.Range(1, 1, lastRow, lastColumn).SetAutoFilter();
            sheet.Column(1).Width = 16;
            sheet.Column(2).Width = 24;
            sheet.Column(3).Width = 20;
            sheet.Column(4).Width = 22;
            for (var column = 5; column < summaryStartCol; column++)
                sheet.Column(column).Width = 18;
            for (var column = summaryStartCol; column <= lastColumn; column++)
                sheet.Column(column).Width = 13;

            sheet.Range(1, 1, lastRow, lastColumn).Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;
            sheet.Range(1, 5, lastRow, summaryStartCol - 1).Style.Alignment.WrapText = true;
            sheet.Range(1, 5, lastRow, summaryStartCol - 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new AttendanceMonthlyExportResult(
                stream.ToArray(),
                $"attendance-report-{year:D4}-{month:D2}.xlsx"
            );
        }

        private static (string Value, string StatusCode) FormatAttendanceCell(
            AttendanceModel? daily,
            IReadOnlyCollection<AttendanceDaySegment>? segments,
            EmpUser employee,
            DateTime date,
            IReadOnlySet<DateOnly> workingDates,
            IReadOnlyCollection<int> weeklyOffs,
            IReadOnlyCollection<CalendarEntity> holidays
        )
        {
            var lines = new List<string>();
            if (daily is not null)
            {
                lines.Add(daily.Status);
                if (daily.CheckInTime.HasValue || daily.CheckOutTime.HasValue)
                    lines.Add(
                        $"In {FormatAttendanceTime(daily.CheckInTime)} · Out {FormatAttendanceTime(daily.CheckOutTime)}"
                    );
                return (
                    string.Join(Environment.NewLine, lines),
                    daily.Status?.Trim().ToUpperInvariant() ?? "P"
                );
            }

            if (segments is not null && segments.Count > 0)
            {
                foreach (var segment in segments)
                {
                    var label =
                        segment.Segment == "FIRST_HALF" ? "First half"
                        : segment.Segment == "SECOND_HALF" ? "Second half"
                        : segment.Segment;
                    lines.Add($"{label}: {segment.Status}");
                    if (segment.CheckInTime.HasValue || segment.CheckOutTime.HasValue)
                        lines.Add(
                            $"In {FormatAttendanceTime(segment.CheckInTime)} · Out {FormatAttendanceTime(segment.CheckOutTime)}"
                        );
                }
                return (
                    string.Join(Environment.NewLine, lines),
                    segments.FirstOrDefault()?.Status?.Trim().ToUpperInvariant() ?? "HD"
                );
            }

            if (DateOnly.FromDateTime(date) > DateOnly.FromDateTime(IndiaTime.Today))
                return ("Future", "FUTURE");

            if (
                EmploymentPeriod.TryGetJoiningDate(employee, out var joiningDate)
                && date.Date < joiningDate.Date
            )
                return ("—", "NOT_ELIGIBLE");

            var lastWorkingDate = EmploymentPeriod.GetLastWorkingDate(employee);
            if (lastWorkingDate.HasValue && date.Date > lastWorkingDate.Value)
                return ("—", "NOT_EMPLOYED");

            if (!workingDates.Contains(DateOnly.FromDateTime(date)))
            {
                var holidayMatch = holidays.FirstOrDefault(holiday =>
                    CalendarDateHelpers.MatchesDate(holiday, DateOnly.FromDateTime(date))
                );
                if (holidayMatch != null)
                    return (
                        !string.IsNullOrWhiteSpace(holidayMatch.Name)
                            ? $"Holiday\n({holidayMatch.Name})"
                            : "Holiday",
                        "H"
                    );

                return ("Weekly Off", "WO");
            }

            return ("A", "A");
        }

        private static void TallyAttendanceDay(
            AttendanceModel? daily,
            IReadOnlyCollection<AttendanceDaySegment>? segments,
            string statusCode,
            IReadOnlyDictionary<string, AttendanceStatusSetting> statusSettingsMap,
            ref decimal presentCount,
            ref decimal halfDayCount,
            ref decimal absentCount,
            ref decimal leaveCount,
            ref decimal weeklyOffCount,
            ref decimal holidayCount,
            ref decimal paidDays
        )
        {
            if (statusCode is "FUTURE" or "NOT_ELIGIBLE" or "NOT_EMPLOYED")
                return;

            if (segments is not null && segments.Count > 0)
            {
                foreach (var segment in segments)
                {
                    var code = segment.Status?.Trim().ToUpperInvariant() ?? string.Empty;
                    statusSettingsMap.TryGetValue(code, out var setting);
                    var paidFraction =
                        setting?.PaidDayFraction
                        ?? (
                            code switch
                            {
                                "P"
                                or "PRESENT"
                                or "WFH"
                                or "WORK_FROM_HOME"
                                or "WO"
                                or "WEEKEND"
                                or "WEEKLY_OFF"
                                or "H"
                                or "HOLIDAY" => 1m,
                                "HD" or "HALF_DAY" => 0.5m,
                                "A" or "ABSENT" => 0m,
                                _ when IsLeaveCode(code) => 1m,
                                _ => 0m,
                            }
                        );

                    paidDays += 0.5m * paidFraction;

                    if (code is "P" or "PRESENT" or "WFH" or "WORK_FROM_HOME")
                        presentCount += 0.5m;
                    else if (code is "HD" or "HALF_DAY")
                        halfDayCount += 0.5m;
                    else if (code is "A" or "ABSENT")
                        absentCount += 0.5m;
                    else if (code is "WO" or "WEEKEND" or "WEEKLY_OFF")
                        weeklyOffCount += 0.5m;
                    else if (code is "H" or "HOLIDAY")
                        holidayCount += 0.5m;
                    else if (IsLeaveCode(code))
                        leaveCount += 0.5m;
                    else if (setting != null)
                    {
                        if (setting.PaidDayFraction >= 1m)
                            presentCount += 0.5m;
                        else if (setting.UnpaidDayFraction >= 1m)
                            absentCount += 0.5m;
                        else if (setting.PaidDayFraction > 0)
                            halfDayCount += 0.5m;
                    }
                }
                return;
            }

            if (daily is not null)
            {
                var code = daily.Status?.Trim().ToUpperInvariant() ?? string.Empty;
                statusSettingsMap.TryGetValue(code, out var setting);
                var paidFraction =
                    setting?.PaidDayFraction
                    ?? (
                        code switch
                        {
                            "P"
                            or "PRESENT"
                            or "WFH"
                            or "WORK_FROM_HOME"
                            or "WO"
                            or "WEEKEND"
                            or "WEEKLY_OFF"
                            or "H"
                            or "HOLIDAY" => 1m,
                            "HD" or "HALF_DAY" => 0.5m,
                            "A" or "ABSENT" => 0m,
                            _ when IsLeaveCode(code) => 1m,
                            _ => 0m,
                        }
                    );

                paidDays += paidFraction;

                if (code is "P" or "PRESENT" or "WFH" or "WORK_FROM_HOME")
                    presentCount += 1m;
                else if (code is "HD" or "HALF_DAY")
                {
                    halfDayCount += 1m;
                    presentCount += 0.5m;
                }
                else if (code is "A" or "ABSENT")
                    absentCount += 1m;
                else if (code is "WO" or "WEEKEND" or "WEEKLY_OFF")
                    weeklyOffCount += 1m;
                else if (code is "H" or "HOLIDAY")
                    holidayCount += 1m;
                else if (IsLeaveCode(code))
                    leaveCount += 1m;
                else if (setting != null)
                {
                    if (setting.PaidDayFraction >= 1m)
                        presentCount += 1m;
                    else if (setting.UnpaidDayFraction >= 1m)
                        absentCount += 1m;
                    else if (setting.PaidDayFraction > 0)
                    {
                        halfDayCount += 1m;
                        presentCount += setting.PaidDayFraction;
                    }
                }
                return;
            }

            if (statusCode is "WO")
            {
                weeklyOffCount += 1m;
                paidDays += 1m;
            }
            else if (statusCode is "H")
            {
                holidayCount += 1m;
                paidDays += 1m;
            }
            else if (statusCode is "A")
            {
                absentCount += 1m;
            }
        }

        private static bool IsLeaveCode(string code) =>
            code is "L" or "LEAVE" or "CL" or "SL" or "PL" or "EL" or "ML" or "PAT" or "MAT"
            || code.StartsWith("LEAVE", StringComparison.OrdinalIgnoreCase);

        private static void ApplyAttendanceCellStyle(
            IXLCell cell,
            string value,
            IReadOnlyDictionary<string, string> statusColors,
            string? attendanceStatus
        )
        {
            var status =
                value
                    .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()
                ?? string.Empty;
            var normalizedStatus = attendanceStatus?.Trim().ToUpperInvariant() ?? string.Empty;
            var color = statusColors.TryGetValue(normalizedStatus, out var configuredColor)
                ? configuredColor
                : status switch
                {
                    "P" or "Present" or "WFH" => "E8F5E9",
                    "HD" or "Half Day" or "First half: P" or "First half: HD" => "FFF3E0",
                    "A" or "Absent" => "FFEBEE",
                    "Weekly Off" or "Weekend" => "F5F5F5",
                    string s when s.StartsWith("Holiday", StringComparison.OrdinalIgnoreCase) =>
                        "EDE7F6",
                    "—" or "Future" or "Not eligible" or "Not employed" => "FAFAFA",
                    _ when IsLeaveCode(normalizedStatus) => "E1F5FE",
                    _ => "FFFFFF",
                };
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(color);
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("E0E0E0");
        }

        private static bool IsHexColor(string? value) =>
            !string.IsNullOrWhiteSpace(value)
            && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$");

        private static string FormatAttendanceTime(DateTime? time) =>
            time.HasValue ? IndiaTime.ToIndiaTime(time.Value).ToString("HH:mm") : "–";
    }
}
