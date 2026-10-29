using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Attendance;

public class AttendanceImportService : IAttendanceImportService
{
    private readonly IMongoDbRepository<EmpUser> _employees;
    private readonly IMongoDbRepository<AttendanceModel> _attendance;
    private readonly IMongoDbRepository<LeaveRequest> _leaves;
    private readonly IMongoDbRepository<AttendanceDaySegment> _segments;
    private readonly IMongoDbRepository<MonthlyAttendanceSummary> _summaries;
    private readonly IMongoDbRepository<WeeklyOffSetting> _weeklyOffs;
    private readonly IMongoDbRepository<CalendarEntity> _calendar;
    private readonly IMongoDbRepository<Department> _departments;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceStatusService _statusService;
    private readonly IAttendancePenaltyService _penaltyService;
    private readonly IMongoDbRepository<AttendancePayrollException>? _exceptions;

    public AttendanceImportService(
        IMongoDbRepository<EmpUser> employees,
        IMongoDbRepository<AttendanceModel> attendance,
        IMongoDbRepository<LeaveRequest> leaves,
        IMongoDbRepository<AttendanceDaySegment> segments,
        IMongoDbRepository<MonthlyAttendanceSummary> summaries,
        IMongoDbRepository<WeeklyOffSetting> weeklyOffs,
        IMongoDbRepository<CalendarEntity> calendar,
        IMongoDbRepository<Department> departments,
        IAttendanceRepository attendanceRepository,
        IAttendanceStatusService statusService,
        IAttendancePenaltyService penaltyService,
        IMongoDbRepository<AttendancePayrollException>? exceptions = null
    )
    {
        _employees = employees;
        _attendance = attendance;
        _leaves = leaves;
        _segments = segments;
        _summaries = summaries;
        _weeklyOffs = weeklyOffs;
        _calendar = calendar;
        _departments = departments;
        _attendanceRepository = attendanceRepository;
        _statusService = statusService;
        _penaltyService = penaltyService;
        _exceptions = exceptions;
    }

    private static DateTime UtcMidnight(DateTime date) =>
        DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    public async Task<(byte[] FileBytes, string FileName)> GenerateTemplateAsync(
        string companyId,
        int year,
        int month,
        CancellationToken cancellationToken = default
    )
    {
        if (year is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Year must be between 2000 and 2100."
            );
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = new DateTime(year, month, daysInMonth);

        // Fetch company weekly off configuration
        var weekly = (
            await _weeklyOffs.GetAll(x => x.CompanyId == companyId, withDefaultFilter: false)
        ).FirstOrDefault();
        var offDays = (
            weekly?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday]
        ).ToHashSet();

        // Fetch company holidays
        var holidays = (
            await _calendar.GetAll(
                x =>
                    x.CompanyId == companyId
                    && x.Type == EnumsHelper.CalendarItem.Holiday
                    && (
                        x.Recurring
                        || (x.Date >= monthStart.AddDays(-1) && x.Date <= monthEnd.AddDays(1))
                    ),
                withDefaultFilter: false
            )
        ).ToList();

        // Fetch active attendance statuses configured for this company
        var activeStatuses = await _statusService.Get(companyId, activeOnly: true);

        // Fetch company departments to resolve department names
        var departments = (
            await _departments.GetAll(
                x => x.CompanyId == companyId && !x.IsDeleted,
                withDefaultFilter: false
            )
        ).ToList();
        var departmentNames = departments.ToDictionary(
            x => x.DepartmentId,
            x =>
                x.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Label))?.Label?.Trim()
                ?? x.DepartmentId,
            StringComparer.OrdinalIgnoreCase
        );
        using var workbook = new XLWorkbook();

        // ==========================================
        // SHEET 1: Attendance Muster (Primary Sheet)
        // ==========================================
        var wsMuster = workbook.Worksheets.Add("Attendance Muster");

        // Static metadata headers
        wsMuster.Cell(1, 1).Value = "Employee ID";
        wsMuster.Cell(1, 2).Value = "Employee Name";
        wsMuster.Cell(1, 3).Value = "Email";
        wsMuster.Cell(1, 4).Value = "Date of Joining";
        wsMuster.Cell(1, 5).Value = "Department";

        var metaHeaders = wsMuster.Range(1, 1, 1, 5);
        metaHeaders.Style.Font.Bold = true;
        metaHeaders.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        metaHeaders.Style.Font.FontColor = XLColor.White;
        metaHeaders.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Day columns
        for (var day = 1; day <= daysInMonth; day++)
        {
            var colIndex = 5 + day;
            var cellDate = new DateTime(year, month, day);
            var isWeekend = offDays.Contains((int)cellDate.DayOfWeek);
            var isHoliday = holidays.Any(h =>
                CalendarDateHelpers.MatchesDate(h, DateOnly.FromDateTime(cellDate))
            );

            var cell = wsMuster.Cell(1, colIndex);
            cell.Value = cellDate.ToString("dd MMM (ddd)");
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            if (isHoliday)
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#B45309"); // Amber/brown
                cell.Style.Font.FontColor = XLColor.White;
            }
            else if (isWeekend)
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#475569"); // Slate grey
                cell.Style.Font.FontColor = XLColor.White;
            }
            else
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A"); // Dark slate
                cell.Style.Font.FontColor = XLColor.White;
            }
        }

        var firstDept =
            departmentNames.Values.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))
            ?? "Engineering";
        var secondDept =
            departmentNames.Values.Skip(1).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))
            ?? "Operations";
        var musterRow = 2;

        // Generate 2 sample guide rows demonstrating how company should structure data
        var sampleRows = new[]
        {
            new
            {
                Id = "SAMPLE-001",
                Name = "Sample Employee 1 (Full Month Present)",
                Email = "sample.emp1@example.com",
                Doj = new DateTime(year, 1, 1),
                Dept = firstDept,
                MidJoinDay = 0,
            },
            new
            {
                Id = "SAMPLE-002",
                Name = "Sample Employee 2 (Mid-Month Joiner)",
                Email = "sample.emp2@example.com",
                Doj = new DateTime(year, month, Math.Min(6, daysInMonth)),
                Dept = secondDept,
                MidJoinDay = Math.Min(6, daysInMonth),
            },
        };

        foreach (var sample in sampleRows)
        {
            wsMuster.Cell(musterRow, 1).Value = sample.Id;
            wsMuster.Cell(musterRow, 2).Value = sample.Name;
            wsMuster.Cell(musterRow, 3).Value = sample.Email;
            wsMuster.Cell(musterRow, 4).Value = sample.Doj.ToString("yyyy-MM-dd");
            wsMuster.Cell(musterRow, 5).Value = sample.Dept;

            for (var day = 1; day <= daysInMonth; day++)
            {
                var colIndex = 5 + day;
                var cellDate = new DateTime(year, month, day);
                var cell = wsMuster.Cell(musterRow, colIndex);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (sample.MidJoinDay > 0 && day < sample.MidJoinDay)
                {
                    cell.Value = "N/A";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                }
                else if (
                    holidays.Any(h =>
                        CalendarDateHelpers.MatchesDate(h, DateOnly.FromDateTime(cellDate))
                    )
                )
                {
                    cell.Value = "H";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#92400E");
                }
                else if (offDays.Contains((int)cellDate.DayOfWeek))
                {
                    cell.Value = "WO";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
                }
                else
                {
                    cell.Value = "P";
                    cell.Style.Font.FontColor = XLColor.FromHtml("#1E293B");
                }
            }

            musterRow++;
        }

        // ==========================================
        // SHEET 2: Allowed Status Codes Reference
        // ==========================================
        var wsCodes = workbook.Worksheets.Add("Allowed Status Codes");
        wsCodes.Cell(1, 1).Value = "Code";
        wsCodes.Cell(1, 2).Value = "Name";
        wsCodes.Cell(1, 3).Value = "Category";
        wsCodes.Cell(1, 4).Value = "Paid Fraction";
        wsCodes.Cell(1, 5).Value = "Unpaid Fraction";
        wsCodes.Cell(1, 6).Value = "Description";

        var headerRangeCodes = wsCodes.Range(1, 1, 1, 6);
        headerRangeCodes.Style.Font.Bold = true;
        headerRangeCodes.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        headerRangeCodes.Style.Font.FontColor = XLColor.White;

        var allowedCodesList = new List<string>();
        var codeRow = 2;

        foreach (var status in activeStatuses)
        {
            var code = status.Code.Trim().ToUpperInvariant();
            if (!allowedCodesList.Contains(code))
                allowedCodesList.Add(code);

            wsCodes.Cell(codeRow, 1).Value = code;
            wsCodes.Cell(codeRow, 2).Value = status.Name;
            wsCodes.Cell(codeRow, 3).Value = status.IsAvailableForLeaveManagement
                ? "Leave"
                : "Working Status";
            wsCodes.Cell(codeRow, 4).Value = status.PaidDayFraction;
            wsCodes.Cell(codeRow, 5).Value = status.UnpaidDayFraction;
            wsCodes.Cell(codeRow, 6).Value = $"Company status: {status.Name}";
            codeRow++;
        }

        // Add special system calendar & tenure codes
        var systemCodes = new[]
        {
            ("WO", "Weekly Off", "Calendar", 1.0m, 0.0m, "Configured weekly off day"),
            ("H", "Holiday", "Calendar", 1.0m, 0.0m, "Configured company holiday"),
            (
                "N/A",
                "Not Applicable",
                "Employment Tenure",
                0.0m,
                0.0m,
                "Day prior to employee joining date or after exit date"
            ),
        };

        foreach (var (code, name, cat, paid, unpaid, desc) in systemCodes)
        {
            if (!allowedCodesList.Contains(code))
                allowedCodesList.Add(code);

            wsCodes.Cell(codeRow, 1).Value = code;
            wsCodes.Cell(codeRow, 2).Value = name;
            wsCodes.Cell(codeRow, 3).Value = cat;
            wsCodes.Cell(codeRow, 4).Value = paid;
            wsCodes.Cell(codeRow, 5).Value = unpaid;
            wsCodes.Cell(codeRow, 6).Value = desc;
            codeRow++;
        }

        wsCodes.Columns().AdjustToContents();

        // Add dropdown validation to all active employee rows (plus 50 buffer rows)
        var totalValidationRows = Math.Max(200, musterRow + 50);
        if (daysInMonth > 0)
        {
            var dataRange = wsMuster.Range(2, 6, totalValidationRows, 5 + daysInMonth);
            var dv = dataRange.CreateDataValidation();
            dv.List($"'Allowed Status Codes'!$A$2:$A${allowedCodesList.Count + 1}", true);
            dv.InputTitle = "Attendance Status & Punch Times";
            dv.InputMessage =
                "Select a status code (e.g. P, A, HD, ED, LHD, WFH, WO, H) or enter status with punch times like ED (09:00-14:00), LHD (13:30-18:00), P (09:00-18:00), or 09:00-18:00.";
            dv.ErrorTitle = "Invalid Attendance Code";
            dv.ErrorMessage =
                "Please enter an allowed attendance status code or status with punch times.";
            dv.ShowErrorMessage = false;
            dv.ShowInputMessage = true;

            var sampleGrid = wsMuster.Range(1, 1, Math.Max(2, musterRow - 1), 5 + daysInMonth);
            sampleGrid.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            sampleGrid.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            sampleGrid.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            sampleGrid.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
        }

        wsMuster.SheetView.FreezeRows(1);
        wsMuster.SheetView.FreezeColumns(5);
        wsMuster.Columns().AdjustToContents();
        wsMuster.SetTabActive();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileBytes = stream.ToArray();
        var fileName = $"Attendance_Import_Template_{year}_{month:D2}.xlsx";
        return (fileBytes, fileName);
    }

    public async Task<AttendanceImportValidationResultDto> ValidateAsync(
        string companyId,
        int year,
        int month,
        Stream fileStream,
        bool preserveApprovedLeaves = true,
        CancellationToken cancellationToken = default
    )
    {
        var result = new AttendanceImportValidationResultDto
        {
            IsValid = true,
            TotalDaysInMonth = DateTime.DaysInMonth(year, month),
        };

        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            result.IsValid = false;
            result.ErrorCount = 1;
            result.Issues.Add(
                new AttendanceImportIssueDto
                {
                    RowNumber = 0,
                    Severity = "Error",
                    Message = $"Invalid calendar period: Year {year}, Month {month}.",
                }
            );
            return result;
        }

        var daysInMonth = result.TotalDaysInMonth;
        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // 1. Guard check: Is payroll month locked?
        var monthWindowStart = startOfMonth.AddDays(-1);
        var monthWindowEnd = startOfMonth.AddDays(1);
        var isLocked = await _summaries.Exist(x =>
            x.CompanyId == companyId
            && x.PayrollMonth >= monthWindowStart
            && x.PayrollMonth <= monthWindowEnd
            && x.IsLocked
        );
        if (isLocked)
        {
            result.IsValid = false;
            result.ErrorCount = 1;
            result.Issues.Add(
                new AttendanceImportIssueDto
                {
                    RowNumber = 0,
                    Severity = "Error",
                    Message =
                        $"Payroll month {year}-{month:D2} is locked. Attendance records cannot be imported.",
                }
            );
            return result;
        }

        // 2. Load Excel workbook
        using var workbook = new XLWorkbook(fileStream);
        var sheet =
            workbook.Worksheets.FirstOrDefault(w => w.Name == "Attendance Muster")
            ?? workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            result.IsValid = false;
            result.ErrorCount = 1;
            result.Issues.Add(
                new AttendanceImportIssueDto
                {
                    RowNumber = 0,
                    Severity = "Error",
                    Message = "No worksheets found in the Excel workbook.",
                }
            );
            return result;
        }

        // 3. Map column headers
        var headerRow = sheet.Row(1);
        var empIdCol = -1;
        var emailCol = -1;
        var dayColMap = new Dictionary<int, int>(); // day (1..31) -> colIndex

        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? (5 + daysInMonth);
        var allHeaders = new List<string>();
        for (var col = 1; col <= lastCol; col++)
        {
            var headerText = headerRow.Cell(col).GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(headerText))
                continue;

            allHeaders.Add(headerText);

            if (
                headerText.Equals("Employee ID", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("EmployeeId", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Emp ID", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("EmpID", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("User ID", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("UserId", StringComparison.OrdinalIgnoreCase)
            )
            {
                empIdCol = col;
            }
            else if (
                headerText.Equals("Email", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Official Email", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Email Address", StringComparison.OrdinalIgnoreCase)
            )
            {
                emailCol = col;
            }
            else if (
                headerText.Equals("Employee Name", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Name", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Date of Joining", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Joining Date", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("DOJ", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Department", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Dept", StringComparison.OrdinalIgnoreCase)
            )
            {
                // Recognized metadata column, do not match as day column
            }
            else if (TryExtractDayNumber(headerText, year, month, daysInMonth, out var parsedDay))
            {
                if (!dayColMap.ContainsKey(parsedDay))
                {
                    dayColMap[parsedDay] = col;
                }
            }
        }

        // Cross-template confusion guard: Monthly Payroll Register uploaded into Attendance Muster import
        var looksLikePayrollRegister = allHeaders.Any(h =>
            h.Equals("Basic Pay", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Net Pay", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Gross Earnings", StringComparison.OrdinalIgnoreCase)
        );

        if (looksLikePayrollRegister)
        {
            result.IsValid = false;
            result.ErrorCount = 1;
            result.Issues.Add(
                new AttendanceImportIssueDto
                {
                    RowNumber = 1,
                    Severity = "Error",
                    Message =
                        "The uploaded file appears to be a Monthly Payroll Register. To upload monthly payroll data, please go to Payroll -> Upload Monthly Attendance & Pay Data. This screen only accepts the Daily Attendance Muster template.",
                }
            );
            return result;
        }

        if (dayColMap.Count == 0)
        {
            result.IsValid = false;
            result.ErrorCount = 1;
            result.Issues.Add(
                new AttendanceImportIssueDto
                {
                    RowNumber = 1,
                    Severity = "Error",
                    Message =
                        "No valid daily attendance date columns (e.g., '01 Oct', '1', '2026-10-01') were found in the header row. Please download and use the official Daily Attendance Muster template.",
                }
            );
            return result;
        }

        if (empIdCol == -1)
            empIdCol = 1;

        // 4. Fetch company employees
        var allEmployees = (
            await _employees.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)
        ).ToList();
        var empByEmployeeId = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);
        var empByUserId = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);
        var empByEmail = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var emp in allEmployees)
        {
            if (!string.IsNullOrWhiteSpace(emp.EmployeeId))
                empByEmployeeId[emp.EmployeeId.Trim()] = emp;
            if (!string.IsNullOrWhiteSpace(emp.UserId))
                empByUserId[emp.UserId.Trim()] = emp;
            if (!string.IsNullOrWhiteSpace(emp.Email))
                empByEmail[emp.Email.Trim()] = emp;
        }

        // 5. Fetch allowed status codes
        var configuredStatuses = await _statusService.Get(companyId, activeOnly: true);
        var allowedCodes = configuredStatuses
            .Select(s => s.Code.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        allowedCodes.Add("WO");
        allowedCodes.Add("H");
        allowedCodes.Add("N/A");
        allowedCodes.Add("NA");

        // 6. Fetch accepted leave requests overlapping this month
        var acceptedLeaves = await _leaves.GetAll(x =>
            x.CompanyId == companyId
            && x.Status == EnumsHelper.LeaveRequestStatus.Accepted
            && x.StartDate <= endOfMonth
            && x.EndDate >= startOfMonth
        );

        var leaveByEmployeeDate = new HashSet<(string UserId, DateTime Date)>();
        foreach (var l in acceptedLeaves)
        {
            var cur = l.StartDate.Date < startOfMonth.Date ? startOfMonth.Date : l.StartDate.Date;
            var end = l.EndDate.Date > endOfMonth.Date ? endOfMonth.Date : l.EndDate.Date;
            for (var d = cur; d <= end; d = d.AddDays(1))
            {
                leaveByEmployeeDate.Add((l.EmployeeId, d));
            }
        }

        var today = IndiaTime.Today.Date;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var processedEmployees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 7. Process rows
        for (var row = 2; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var empIdVal = empIdCol > 0 ? sheet.Cell(row, empIdCol).GetString()?.Trim() : null;
            var emailVal = emailCol > 0 ? sheet.Cell(row, emailCol).GetString()?.Trim() : null;

            if (string.IsNullOrWhiteSpace(empIdVal) && string.IsNullOrWhiteSpace(emailVal))
                continue; // Skip empty rows

            // Automatically skip sample guide rows if left in template
            if (
                (
                    !string.IsNullOrWhiteSpace(empIdVal)
                    && empIdVal.StartsWith("SAMPLE", StringComparison.OrdinalIgnoreCase)
                )
                || (
                    !string.IsNullOrWhiteSpace(emailVal)
                    && emailVal.Contains("@example.com", StringComparison.OrdinalIgnoreCase)
                )
            )
                continue;

            EmpUser? employee = null;
            if (!string.IsNullOrWhiteSpace(empIdVal))
            {
                if (empByEmployeeId.TryGetValue(empIdVal, out var matchByEmpId))
                    employee = matchByEmpId;
                else if (empByUserId.TryGetValue(empIdVal, out var matchByUserId))
                    employee = matchByUserId;
                else if (empByEmail.TryGetValue(empIdVal, out var matchByEmailInId))
                    employee = matchByEmailInId;
            }

            if (employee == null && !string.IsNullOrWhiteSpace(emailVal))
            {
                if (empByEmail.TryGetValue(emailVal, out var matchByEmail))
                    employee = matchByEmail;
            }

            if (employee == null)
            {
                var identifier = !string.IsNullOrWhiteSpace(empIdVal) ? empIdVal : emailVal;
                result.Issues.Add(
                    new AttendanceImportIssueDto
                    {
                        RowNumber = row,
                        ColumnHeader = "Employee ID",
                        EmployeeId = identifier,
                        Severity = "Error",
                        Message = $"Employee '{identifier}' was not found in company directory.",
                    }
                );
                result.ErrorCount++;
                continue;
            }

            processedEmployees.Add(employee.UserId);
            var empName = $"{employee.FirstName} {employee.LastName}".Trim();
            var hasDoj = EmploymentPeriod.TryGetJoiningDate(employee, out var doj);
            var lwd = EmploymentPeriod.GetLastWorkingDate(employee);

            for (var day = 1; day <= daysInMonth; day++)
            {
                result.TotalCellsProcessed++;

                if (!dayColMap.TryGetValue(day, out var colIdx))
                    continue;

                var cellDate = new DateTime(year, month, day);
                var cellDateStr = cellDate.ToString("yyyy-MM-dd");
                var colHeader =
                    sheet.Cell(1, colIdx).GetString()?.Trim() ?? cellDate.ToString("dd MMM");
                var rawValue = sheet.Cell(row, colIdx).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    result.Issues.Add(
                        new AttendanceImportIssueDto
                        {
                            RowNumber = row,
                            ColumnHeader = colHeader,
                            EmployeeId = employee.EmployeeId,
                            EmployeeName = empName,
                            Date = cellDateStr,
                            Severity = "Warning",
                            Message =
                                $"Empty attendance status for {cellDateStr}. It will be skipped.",
                        }
                    );
                    result.WarningCount++;
                    continue;
                }

                var parsed = ParseMusterCell(rawValue);
                var normalizedCode = parsed.StatusCode;

                if (!string.IsNullOrEmpty(parsed.ParseError))
                {
                    result.Issues.Add(
                        new AttendanceImportIssueDto
                        {
                            RowNumber = row,
                            ColumnHeader = colHeader,
                            EmployeeId = employee.EmployeeId,
                            EmployeeName = empName,
                            Date = cellDateStr,
                            Severity = "Error",
                            Message =
                                $"Invalid punch times in cell '{rawValue}' for {cellDateStr}: {parsed.ParseError}",
                        }
                    );
                    result.ErrorCount++;
                    continue;
                }

                // Tenure check: Prior to joining date
                if (hasDoj && cellDate.Date < doj.Date)
                {
                    if (normalizedCode is not ("N/A" or "NA"))
                    {
                        result.Issues.Add(
                            new AttendanceImportIssueDto
                            {
                                RowNumber = row,
                                ColumnHeader = colHeader,
                                EmployeeId = employee.EmployeeId,
                                EmployeeName = empName,
                                Date = cellDateStr,
                                Severity = "Error",
                                Message =
                                    $"Cannot mark attendance for {cellDateStr} prior to joining date ({doj:yyyy-MM-dd}). Cell must be 'N/A'.",
                            }
                        );
                        result.ErrorCount++;
                        continue;
                    }

                    result.ValidEntriesCount++;
                    continue;
                }

                // Tenure check: Post exit date
                if (lwd.HasValue && cellDate.Date > lwd.Value.Date)
                {
                    if (normalizedCode is not ("N/A" or "NA"))
                    {
                        result.Issues.Add(
                            new AttendanceImportIssueDto
                            {
                                RowNumber = row,
                                ColumnHeader = colHeader,
                                EmployeeId = employee.EmployeeId,
                                EmployeeName = empName,
                                Date = cellDateStr,
                                Severity = "Error",
                                Message =
                                    $"Cannot mark attendance for {cellDateStr} after exit date ({lwd.Value:yyyy-MM-dd}). Cell must be 'N/A'.",
                            }
                        );
                        result.ErrorCount++;
                        continue;
                    }

                    result.ValidEntriesCount++;
                    continue;
                }

                // If code is N/A during employment, report warning or allow
                if (normalizedCode is "N/A" or "NA")
                {
                    result.ValidEntriesCount++;
                    continue;
                }

                // Future date check
                if (cellDate.Date > today && normalizedCode is not ("WO" or "H"))
                {
                    result.Issues.Add(
                        new AttendanceImportIssueDto
                        {
                            RowNumber = row,
                            ColumnHeader = colHeader,
                            EmployeeId = employee.EmployeeId,
                            EmployeeName = empName,
                            Date = cellDateStr,
                            Severity = "Error",
                            Message =
                                $"Cannot mark attendance '{normalizedCode}' for future date {cellDateStr}.",
                        }
                    );
                    result.ErrorCount++;
                    continue;
                }

                // Status code validity check
                if (!allowedCodes.Contains(normalizedCode))
                {
                    result.Issues.Add(
                        new AttendanceImportIssueDto
                        {
                            RowNumber = row,
                            ColumnHeader = colHeader,
                            EmployeeId = employee.EmployeeId,
                            EmployeeName = empName,
                            Date = cellDateStr,
                            Severity = "Error",
                            Message =
                                $"Invalid status code '{rawValue}' for date {cellDateStr}. Allowed codes: {string.Join(", ", allowedCodes.Take(12))}...",
                        }
                    );
                    result.ErrorCount++;
                    continue;
                }

                // Check approved leave conflict
                if (leaveByEmployeeDate.Contains((employee.UserId, cellDate.Date)))
                {
                    if (preserveApprovedLeaves)
                    {
                        result.Issues.Add(
                            new AttendanceImportIssueDto
                            {
                                RowNumber = row,
                                ColumnHeader = colHeader,
                                EmployeeId = employee.EmployeeId,
                                EmployeeName = empName,
                                Date = cellDateStr,
                                Severity = "Warning",
                                Message =
                                    $"Employee has approved leave on {cellDateStr}. Existing leave will be preserved.",
                            }
                        );
                        result.PreservedLeavesCount++;
                        result.WarningCount++;
                        result.ValidEntriesCount++;
                        continue;
                    }
                    else
                    {
                        result.Issues.Add(
                            new AttendanceImportIssueDto
                            {
                                RowNumber = row,
                                ColumnHeader = colHeader,
                                EmployeeId = employee.EmployeeId,
                                EmployeeName = empName,
                                Date = cellDateStr,
                                Severity = "Warning",
                                Message =
                                    $"Approved leave on {cellDateStr} will be overwritten by imported status '{normalizedCode}'.",
                            }
                        );
                        result.WarningCount++;
                    }
                }

                result.ValidEntriesCount++;
            }
        }

        result.TotalEmployees = processedEmployees.Count;
        result.IsValid = result.ErrorCount == 0;
        return result;
    }

    public async Task<AttendanceImportCommitResultDto> CommitAsync(
        string companyId,
        string actorUserId,
        int year,
        int month,
        Stream fileStream,
        bool preserveApprovedLeaves = true,
        CancellationToken cancellationToken = default
    )
    {
        // 1. Run Pre-flight Validation
        // We copy to MemoryStream first so we can reuse stream for both validation and reading
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var validation = await ValidateAsync(
            companyId,
            year,
            month,
            memoryStream,
            preserveApprovedLeaves,
            cancellationToken
        );
        if (!validation.IsValid)
        {
            return new AttendanceImportCommitResultDto
            {
                Success = false,
                Message =
                    $"Validation failed with {validation.ErrorCount} error(s). Please correct them and re-upload.",
                Issues = validation.Issues,
            };
        }

        memoryStream.Position = 0;
        using var workbook = new XLWorkbook(memoryStream);
        var sheet =
            workbook.Worksheets.FirstOrDefault(w => w.Name == "Attendance Muster")
            ?? workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            return new AttendanceImportCommitResultDto
            {
                Success = false,
                Message = "No worksheets found in the Excel workbook.",
            };
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // Map column headers
        var headerRow = sheet.Row(1);
        var empIdCol = -1;
        var emailCol = -1;
        var dayColMap = new Dictionary<int, int>();

        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? (5 + daysInMonth);
        for (var col = 1; col <= lastCol; col++)
        {
            var headerText = headerRow.Cell(col).GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(headerText))
                continue;

            if (
                headerText.Equals("Employee ID", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("EmployeeId", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Emp ID", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("EmpID", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("User ID", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("UserId", StringComparison.OrdinalIgnoreCase)
            )
            {
                empIdCol = col;
            }
            else if (
                headerText.Equals("Email", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Official Email", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Email Address", StringComparison.OrdinalIgnoreCase)
            )
            {
                emailCol = col;
            }
            else if (
                headerText.Equals("Employee Name", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Name", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Date of Joining", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Joining Date", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("DOJ", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Department", StringComparison.OrdinalIgnoreCase)
                || headerText.Equals("Dept", StringComparison.OrdinalIgnoreCase)
            )
            {
                // Recognized metadata column, do not match as day column
            }
            else if (TryExtractDayNumber(headerText, year, month, daysInMonth, out var parsedDay))
            {
                if (!dayColMap.ContainsKey(parsedDay))
                {
                    dayColMap[parsedDay] = col;
                }
            }
        }

        if (empIdCol == -1)
            empIdCol = 1;

        if (dayColMap.Count == 0)
        {
            var startCol = (emailCol > 0 ? 6 : 5);
            for (var d = 1; d <= daysInMonth; d++)
                dayColMap[d] = startCol + d - 1;
        }

        // Fetch company employees
        var allEmployees = (
            await _employees.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)
        ).ToList();
        var empByEmployeeId = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);
        var empByUserId = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);
        var empByEmail = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var emp in allEmployees)
        {
            if (!string.IsNullOrWhiteSpace(emp.EmployeeId))
                empByEmployeeId[emp.EmployeeId.Trim()] = emp;
            if (!string.IsNullOrWhiteSpace(emp.UserId))
                empByUserId[emp.UserId.Trim()] = emp;
            if (!string.IsNullOrWhiteSpace(emp.Email))
                empByEmail[emp.Email.Trim()] = emp;
        }

        // Fetch existing attendance records for the month
        var existingAttendance = await _attendance.GetAll(x =>
            x.CompanyId == companyId && x.Date >= startOfMonth && x.Date <= endOfMonth.AddDays(1)
        );

        var existingMap = existingAttendance
            .GroupBy(x => (x.UserId ?? string.Empty, x.Date.Date))
            .ToDictionary(g => g.Key, g => g.First());

        // Fetch approved leaves
        var acceptedLeaves = await _leaves.GetAll(x =>
            x.CompanyId == companyId
            && x.Status == EnumsHelper.LeaveRequestStatus.Accepted
            && x.StartDate <= endOfMonth
            && x.EndDate >= startOfMonth
        );

        var leaveByEmployeeDate = new HashSet<(string UserId, DateTime Date)>();
        foreach (var l in acceptedLeaves)
        {
            var cur = l.StartDate.Date < startOfMonth.Date ? startOfMonth.Date : l.StartDate.Date;
            var end = l.EndDate.Date > endOfMonth.Date ? endOfMonth.Date : l.EndDate.Date;
            for (var d = cur; d <= end; d = d.AddDays(1))
            {
                leaveByEmployeeDate.Add((l.EmployeeId, d));
            }
        }

        var configuredStatuses = await _statusService.Get(companyId, activeOnly: true);
        var statusByCode = configuredStatuses.ToDictionary(
            s => s.Code.Trim().ToUpperInvariant(),
            s => s,
            StringComparer.OrdinalIgnoreCase
        );

        var today = IndiaTime.Today.Date;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var recordsToUpsert = new List<AttendanceModel>();
        var affectedEmployees = new HashSet<EmpUser>();
        var preservedLeaves = 0;

        for (var row = 2; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var empIdVal = empIdCol > 0 ? sheet.Cell(row, empIdCol).GetString()?.Trim() : null;
            var emailVal = emailCol > 0 ? sheet.Cell(row, emailCol).GetString()?.Trim() : null;

            if (string.IsNullOrWhiteSpace(empIdVal) && string.IsNullOrWhiteSpace(emailVal))
                continue;

            // Automatically skip sample guide rows if left in template
            if (
                (
                    !string.IsNullOrWhiteSpace(empIdVal)
                    && empIdVal.StartsWith("SAMPLE", StringComparison.OrdinalIgnoreCase)
                )
                || (
                    !string.IsNullOrWhiteSpace(emailVal)
                    && emailVal.Contains("@example.com", StringComparison.OrdinalIgnoreCase)
                )
            )
                continue;

            EmpUser? employee = null;
            if (!string.IsNullOrWhiteSpace(empIdVal))
            {
                if (empByEmployeeId.TryGetValue(empIdVal, out var matchByEmpId))
                    employee = matchByEmpId;
                else if (empByUserId.TryGetValue(empIdVal, out var matchByUserId))
                    employee = matchByUserId;
                else if (empByEmail.TryGetValue(empIdVal, out var matchByEmailInId))
                    employee = matchByEmailInId;
            }

            if (employee == null && !string.IsNullOrWhiteSpace(emailVal))
            {
                if (empByEmail.TryGetValue(emailVal, out var matchByEmail))
                    employee = matchByEmail;
            }

            if (employee == null)
                continue;

            affectedEmployees.Add(employee);
            var hasDoj = EmploymentPeriod.TryGetJoiningDate(employee, out var doj);
            var lwd = EmploymentPeriod.GetLastWorkingDate(employee);

            for (var day = 1; day <= daysInMonth; day++)
            {
                if (!dayColMap.TryGetValue(day, out var colIdx))
                    continue;

                var rawValue = sheet.Cell(row, colIdx).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(rawValue))
                    continue;

                var cellDate = new DateTime(year, month, day);
                var parsed = ParseMusterCell(rawValue);
                var normalizedCode = parsed.StatusCode;

                // Leave preservation guard
                var hasExistingLeave = leaveByEmployeeDate.Contains(
                    (employee.UserId, cellDate.Date)
                );
                if (existingMap.TryGetValue((employee.UserId, cellDate.Date), out var existingRow))
                {
                    if (
                        preserveApprovedLeaves
                        && (
                            hasExistingLeave
                            || existingRow.SourceType is "LEAVE" or "LIFECYCLE_CASE"
                        )
                    )
                    {
                        if (_exceptions != null && parsed.HasExplicitTimes)
                        {
                            var payrollMonth = new DateTime(cellDate.Year, cellDate.Month, 1);
                            var dateUtc = UtcMidnight(cellDate);
                            var existingException = await _exceptions.FirstOrDefault(x =>
                                x.CompanyId == companyId && x.UserId == employee.UserId &&
                                x.PayrollMonth == payrollMonth && x.AttendanceDate == dateUtc &&
                                x.ExceptionType == "PUNCH_ON_APPROVED_LEAVE");
                            if (existingException == null)
                            {
                                await _exceptions.AddOne(new AttendancePayrollException
                                {
                                    CompanyId = companyId,
                                    UserId = employee.UserId,
                                    EmployeeId = employee.EmployeeId,
                                    PayrollMonth = payrollMonth,
                                    AttendanceDate = dateUtc,
                                    AffectedDates = [dateUtc],
                                    ExceptionType = "PUNCH_ON_APPROVED_LEAVE",
                                    Severity = "WARNING",
                                    Status = "PENDING_REVIEW",
                                    LeaveRequestId = existingRow?.SourceId,
                                    ExistingAttendanceId = existingRow?.AttendanceId,
                                    ExistingStatus = existingRow?.Status ?? "CL",
                                    RequestedLeaveStatus = "P",
                                    Reason = $"Employee has approved leave but import record contains punches ({parsed.CheckInTime:HH:mm} - {parsed.CheckOutTime:HH:mm}). Review to convert to office present or keep leave.",
                                    CreatedBy = actorUserId
                                });
                            }
                        }
                        preservedLeaves++;
                        continue;
                    }
                }
                else if (preserveApprovedLeaves && hasExistingLeave)
                {
                    if (_exceptions != null && parsed.HasExplicitTimes)
                    {
                        var payrollMonth = new DateTime(cellDate.Year, cellDate.Month, 1);
                        var dateUtc = UtcMidnight(cellDate);
                        var existingException = await _exceptions.FirstOrDefault(x =>
                            x.CompanyId == companyId && x.UserId == employee.UserId &&
                            x.PayrollMonth == payrollMonth && x.AttendanceDate == dateUtc &&
                            x.ExceptionType == "PUNCH_ON_APPROVED_LEAVE");
                        if (existingException == null)
                        {
                            await _exceptions.AddOne(new AttendancePayrollException
                            {
                                CompanyId = companyId,
                                UserId = employee.UserId,
                                EmployeeId = employee.EmployeeId,
                                PayrollMonth = payrollMonth,
                                AttendanceDate = dateUtc,
                                AffectedDates = [dateUtc],
                                ExceptionType = "PUNCH_ON_APPROVED_LEAVE",
                                Severity = "WARNING",
                                Status = "PENDING_REVIEW",
                                ExistingStatus = "CL",
                                RequestedLeaveStatus = "P",
                                Reason = $"Employee has approved leave but import record contains punches ({parsed.CheckInTime:HH:mm} - {parsed.CheckOutTime:HH:mm}). Review to convert to office present or keep leave.",
                                CreatedBy = actorUserId
                            });
                        }
                    }
                    preservedLeaves++;
                    continue;
                }

                // Virtual calendar states (WO, H, N/A) MUST NOT be persisted as physical attendance records.
                // If an authoritative import specifies an off-day (WO or H), any prior non-leave record
                // on this date (e.g. pre-initialized SYSTEM_DEFAULT_PRESENT, manual, or bulk import) is obsolete and must be purged.
                if (normalizedCode is "WO" or "H" or "N/A" or "NA")
                {
                    if (
                        existingRow != null
                        && (
                            !preserveApprovedLeaves
                            || existingRow.SourceType is not ("LEAVE" or "LIFECYCLE_CASE")
                        )
                    )
                    {
                        await _attendance.DeleteAll(
                            Builders<AttendanceModel>.Filter.Eq(
                                x => x.AttendanceId,
                                existingRow.AttendanceId
                            )
                        );
                        existingMap.Remove((employee.UserId, cellDate.Date));
                    }
                    continue;
                }

                // Out of employment period guard
                if (hasDoj && cellDate.Date < doj.Date)
                    continue;
                if (lwd.HasValue && cellDate.Date > lwd.Value.Date)
                    continue;

                // Future date guard
                if (cellDate.Date > today)
                    continue;

                // Resolve punch in/out times and calculated hours
                DateTime? checkIn = null;
                DateTime? checkOut = null;
                var isAbsentOrOff = normalizedCode is "A" or "WO" or "H" or "N/A" or "NA";
                var requiresTime =
                    !isAbsentOrOff
                    && (
                        (
                            statusByCode.TryGetValue(normalizedCode, out var statusSetting)
                            && statusSetting.RequiresTime
                        )
                        || normalizedCode
                            is "P"
                                or "WFH"
                                or "ED"
                                or "LHD"
                                or "HD"
                                or "LC"
                                or "EHD"
                                or "LHD+ED"
                                or "WFH+WFO"
                                or "WFH-HD"
                        || parsed.HasExplicitTimes
                    );

                if (requiresTime)
                {
                    if (parsed.CheckInTime.HasValue)
                    {
                        checkIn = ToUtcFromIst(cellDate, parsed.CheckInTime.Value);
                    }
                    else if (normalizedCode == "LHD")
                    {
                        // LHD is late arrival (half day): defaults to 13:30 IST (08:00 UTC)
                        checkIn = ToUtcFromIst(cellDate, new TimeOnly(13, 30));
                    }
                    else
                    {
                        // Default start time: 09:00 IST (03:30 UTC)
                        checkIn = ToUtcFromIst(cellDate, new TimeOnly(9, 0));
                    }

                    if (parsed.CheckOutTime.HasValue)
                    {
                        checkOut = ToUtcFromIst(cellDate, parsed.CheckOutTime.Value);
                    }
                    else if (normalizedCode == "ED")
                    {
                        // ED is early departure: defaults to 14:00 IST (08:30 UTC)
                        checkOut = ToUtcFromIst(cellDate, new TimeOnly(14, 0));
                    }
                    else if (normalizedCode == "HD")
                    {
                        // HD is half day: defaults to 13:30 IST (08:00 UTC)
                        checkOut = ToUtcFromIst(cellDate, new TimeOnly(13, 30));
                    }
                    else
                    {
                        // Default end time: 18:00 IST (12:30 UTC)
                        checkOut = ToUtcFromIst(cellDate, new TimeOnly(18, 0));
                    }

                    if (checkOut <= checkIn)
                    {
                        checkOut = checkOut.Value.AddDays(1);
                    }
                }

                decimal? totalHours = null;
                if (checkIn.HasValue && checkOut.HasValue)
                {
                    var rawDuration = (decimal)(checkOut.Value - checkIn.Value).TotalHours;
                    var breakDeduction = (rawDuration >= 6.0m) ? 1.0m : 0m;
                    totalHours = Math.Max(Math.Round(rawDuration - breakDeduction, 2), 0m);
                }

                var lateCount = (normalizedCode is "LHD" or "LC" or "LHD+ED") ? 1 : 0;
                var earlyExitCount = (normalizedCode is "ED" or "LHD+ED") ? 1 : 0;

                var remark = parsed.HasExplicitTimes
                    ? $"Bulk Imported ({parsed.CheckInTime?.ToString("HH:mm") ?? "09:00"} - {parsed.CheckOutTime?.ToString("HH:mm") ?? "18:00"})"
                    : "Bulk Attendance Import";

                var attendanceRow = new AttendanceModel
                {
                    AttendanceId = existingRow?.AttendanceId,
                    CompanyId = companyId,
                    UserId = employee.UserId,
                    EmployeeId = employee.EmployeeId,
                    Date = UtcMidnight(cellDate),
                    Status = normalizedCode,
                    CheckInTime = checkIn,
                    CheckOutTime = checkOut,
                    TotalHours = totalHours,
                    LateCount = lateCount,
                    EarlyExitCount = earlyExitCount,
                    SourceType = "BULK_ATTENDANCE_IMPORT",
                    SourceId = $"IMPORT_{year}_{month:D2}",
                    Remarks = remark,
                    SystemRemark =
                        $"Bulk imported by user {actorUserId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    MarkedByUserId = actorUserId,
                    MarkedAtUtc = DateTime.UtcNow,
                    Version = (existingRow?.Version ?? 0) + 1,
                };

                recordsToUpsert.Add(attendanceRow);
            }
        }

        // 2. Perform atomic bulk write into MongoDB
        var upsertedCount = await _attendanceRepository.BulkUpsertAsync(
            recordsToUpsert,
            cancellationToken
        );

        // 3. Clean up any pre-joining or post-exit default rows for affected employees
        foreach (var emp in affectedEmployees)
        {
            if (EmploymentPeriod.TryGetJoiningDate(emp, out var doj))
            {
                var lwd = EmploymentPeriod.GetLastWorkingDate(emp);
                await _attendanceRepository.DeleteDefaultPresentOutsideEmploymentPeriodAsync(
                    companyId,
                    emp.UserId,
                    doj,
                    lwd
                );
            }
        }

        // 4. Invalidate unlocked summaries for this month so muster/payroll recalculates cleanly
        await _summaries.DeleteAll(
            Builders<MonthlyAttendanceSummary>.Filter.Where(x =>
                x.CompanyId == companyId && x.PayrollMonth == startOfMonth && !x.IsLocked
            )
        );

        // 5. Trigger penalty and payroll exceptions recalculation
        try
        {
            await _penaltyService.Recalculate(companyId, startOfMonth);
        }
        catch
        {
            // Do not abort commit if penalty recalculation encounters minor edge case
        }

        return new AttendanceImportCommitResultDto
        {
            Success = true,
            Message =
                $"Successfully imported {recordsToUpsert.Count} attendance records for {affectedEmployees.Count} employee(s).",
            EmployeesAffected = affectedEmployees.Count,
            RecordsUpserted = recordsToUpsert.Count,
            PreservedLeavesCount = preservedLeaves,
            Issues = validation.Issues.Where(x => x.Severity == "Warning").ToList(),
        };
    }

    private static string NormalizeStatusCode(string value)
    {
        var trimmed = value.Trim().ToUpperInvariant();
        return trimmed switch
        {
            "PRESENT" => "P",
            "ABSENT" => "A",
            "HALF DAY" or "HALF-DAY" => "HD",
            "WORK FROM HOME" or "WFH" => "WFH",
            "WEEKLY OFF" or "WEEKEND" or "OFF" => "WO",
            "HOLIDAY" => "H",
            "NOT APPLICABLE" or "NOT EMPLOYED" or "NOT ELIGIBLE" or "-" => "N/A",
            _ => trimmed,
        };
    }

    private static bool TryExtractDayNumber(
        string header,
        int year,
        int month,
        int daysInMonth,
        out int dayNumber
    )
    {
        dayNumber = 0;
        if (string.IsNullOrWhiteSpace(header))
            return false;

        header = header.Trim();

        // 1. Direct number: "1", "01"
        if (int.TryParse(header, out var directNum) && directNum >= 1 && directNum <= daysInMonth)
        {
            dayNumber = directNum;
            return true;
        }

        // 2. Full date format: "2026-10-01" or "01-10-2026"
        if (
            DateTime.TryParse(header, out var parsedDate)
            && parsedDate.Year == year
            && parsedDate.Month == month
        )
        {
            dayNumber = parsedDate.Day;
            return true;
        }

        // 3. Starts with day digits followed by separator/month: "01 Oct (Thu)", "1-Oct", "01/10/2026"
        var match = Regex.Match(header, @"^(\d{1,2})(?:\s|[\/\-_]|[a-zA-Z])");
        if (
            match.Success
            && int.TryParse(match.Groups[1].Value, out var leadingNum)
            && leadingNum >= 1
            && leadingNum <= daysInMonth
        )
        {
            dayNumber = leadingNum;
            return true;
        }

        return false;
    }

    public sealed class ParsedMusterEntry
    {
        public string RawValue { get; init; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public TimeOnly? CheckInTime { get; set; }
        public TimeOnly? CheckOutTime { get; set; }
        public bool HasExplicitTimes => CheckInTime.HasValue || CheckOutTime.HasValue;
        public string? ParseError { get; set; }
    }

    public static ParsedMusterEntry ParseMusterCell(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return new ParsedMusterEntry
            {
                RawValue = rawValue ?? string.Empty,
                StatusCode = string.Empty,
            };

        var text = rawValue.Trim();

        // 1. Time range: e.g. "09:00 - 18:00", "09:00-14:00", "9:00 - 18:00", "ED (09:00 - 14:00)", "ED 09:00-14:00"
        var rangeMatch = Regex.Match(
            text,
            @"(?<in>\d{1,2}:\d{2})\s*(?:-|to|\–|\—)\s*(?<out>\d{1,2}:\d{2})",
            RegexOptions.IgnoreCase
        );
        if (rangeMatch.Success)
        {
            if (
                TimeOnly.TryParse(rangeMatch.Groups["in"].Value, out var inTime)
                && TimeOnly.TryParse(rangeMatch.Groups["out"].Value, out var outTime)
            )
            {
                var remaining = text.Remove(rangeMatch.Index, rangeMatch.Length);
                remaining = Regex.Replace(remaining, @"[\(\)\[\]\{\}\|:]", " ").Trim();
                var explicitCode = NormalizeStatusCode(remaining);
                var code = explicitCode;

                if (string.IsNullOrWhiteSpace(code))
                {
                    // No code explicitly given: derive status from duration
                    // In .NET TimeOnly subtraction (outTime - inTime) already accounts for midnight wrap-around.
                    var duration = (outTime - inTime).TotalHours;

                    if (duration >= 7.5)
                        code = "P";
                    else if (inTime >= new TimeOnly(12, 0))
                        code = "LHD";
                    else if (outTime <= new TimeOnly(16, 30) && duration >= 4.0)
                        code = "ED";
                    else if (duration >= 4.0)
                        code = "HD";
                    else
                        code = "A";
                }

                string? parseError = null;
                if (outTime == inTime && outTime != TimeOnly.MinValue)
                {
                    parseError = "Check-out time cannot equal check-in time.";
                }
                else if (outTime < inTime && explicitCode is "ED" or "LC" or "HD" or "LHD")
                {
                    parseError = "Check-out time must be later than check-in time.";
                }

                return new ParsedMusterEntry
                {
                    RawValue = text,
                    StatusCode = code,
                    CheckInTime = inTime,
                    CheckOutTime = outTime,
                    ParseError = parseError,
                };
            }
        }

        // 2. Single time: e.g. "ED 14:00", "ED (14:00)", "LHD 13:30", "LC 10:30"
        var singleMatch = Regex.Match(text, @"(?<time>\d{1,2}:\d{2})");
        if (singleMatch.Success)
        {
            if (TimeOnly.TryParse(singleMatch.Groups["time"].Value, out var singleTime))
            {
                var remaining = text.Remove(singleMatch.Index, singleMatch.Length);
                remaining = Regex.Replace(remaining, @"[\(\)\[\]\{\}\|:]", " ").Trim();
                var code = NormalizeStatusCode(remaining);

                if (!string.IsNullOrWhiteSpace(code))
                {
                    if (code == "ED")
                    {
                        return new ParsedMusterEntry
                        {
                            RawValue = text,
                            StatusCode = code,
                            CheckInTime = null,
                            CheckOutTime = singleTime,
                        };
                    }

                    if (code is "LHD" or "LC")
                    {
                        return new ParsedMusterEntry
                        {
                            RawValue = text,
                            StatusCode = code,
                            CheckInTime = singleTime,
                            CheckOutTime = null,
                        };
                    }

                    if (code == "HD")
                    {
                        return new ParsedMusterEntry
                        {
                            RawValue = text,
                            StatusCode = code,
                            CheckInTime =
                                singleTime < new TimeOnly(12, 0) ? singleTime : new TimeOnly(9, 0),
                            CheckOutTime =
                                singleTime >= new TimeOnly(12, 0)
                                    ? singleTime
                                    : new TimeOnly(18, 0),
                        };
                    }

                    return new ParsedMusterEntry
                    {
                        RawValue = text,
                        StatusCode = code,
                        CheckInTime = singleTime < new TimeOnly(12, 0) ? singleTime : null,
                        CheckOutTime = singleTime >= new TimeOnly(12, 0) ? singleTime : null,
                    };
                }
            }
        }

        // 3. Simple code
        return new ParsedMusterEntry { RawValue = text, StatusCode = NormalizeStatusCode(text) };
    }

    private static DateTime ToUtcFromIst(DateTime date, TimeOnly time)
    {
        var localDateTime = new DateTime(
            date.Year,
            date.Month,
            date.Day,
            time.Hour,
            time.Minute,
            time.Second,
            DateTimeKind.Unspecified
        );
        try
        {
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, istZone);
        }
        catch
        {
            return DateTime.SpecifyKind(localDateTime.AddHours(-5.5), DateTimeKind.Utc);
        }
    }
}
