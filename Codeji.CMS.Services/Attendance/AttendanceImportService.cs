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
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceStatusService _statusService;
    private readonly IAttendancePenaltyService _penaltyService;

    public AttendanceImportService(
        IMongoDbRepository<EmpUser> employees,
        IMongoDbRepository<AttendanceModel> attendance,
        IMongoDbRepository<LeaveRequest> leaves,
        IMongoDbRepository<AttendanceDaySegment> segments,
        IMongoDbRepository<MonthlyAttendanceSummary> summaries,
        IMongoDbRepository<WeeklyOffSetting> weeklyOffs,
        IMongoDbRepository<CalendarEntity> calendar,
        IAttendanceRepository attendanceRepository,
        IAttendanceStatusService statusService,
        IAttendancePenaltyService penaltyService)
    {
        _employees = employees;
        _attendance = attendance;
        _leaves = leaves;
        _segments = segments;
        _summaries = summaries;
        _weeklyOffs = weeklyOffs;
        _calendar = calendar;
        _attendanceRepository = attendanceRepository;
        _statusService = statusService;
        _penaltyService = penaltyService;
    }

    private static DateTime UtcMidnight(DateTime date) => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    public async Task<(byte[] FileBytes, string FileName)> GenerateTemplateAsync(
        string companyId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (year is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2100.");
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = new DateTime(year, month, daysInMonth);

        // Fetch company weekly off configuration
        var weekly = (await _weeklyOffs.GetAll(x => x.CompanyId == companyId, withDefaultFilter: false)).FirstOrDefault();
        var offDays = (weekly?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday]).ToHashSet();

        // Fetch company holidays
        var holidays = (await _calendar.GetAll(x => x.CompanyId == companyId &&
            x.Type == EnumsHelper.CalendarItem.Holiday &&
            (x.Recurring || (x.Date >= monthStart.AddDays(-1) && x.Date <= monthEnd.AddDays(1))),
            withDefaultFilter: false)).ToList();

        // Fetch active attendance statuses configured for this company
        var activeStatuses = await _statusService.Get(companyId, activeOnly: true);

        // Fetch active employees eligible for this month
        var allEmployees = (await _employees.GetAll(x => x.CompanyId == companyId && x.Status && !x.IsDeleted)).ToList();
        var eligibleEmployees = allEmployees
            .Where(e => EmploymentPeriod.IsEligibleForMonth(e, monthStart))
            .OrderBy(e => e.EmployeeId ?? e.FirstName ?? string.Empty)
            .ToList();

        using var workbook = new XLWorkbook();

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
            wsCodes.Cell(codeRow, 3).Value = status.IsAvailableForLeaveManagement ? "Leave" : "Working Status";
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
            ("N/A", "Not Applicable", "Employment Tenure", 0.0m, 0.0m, "Day prior to employee joining date or after exit date")
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

        // ==========================================
        // SHEET 1: Attendance Muster
        // ==========================================
        var wsMuster = workbook.Worksheets.Add("Attendance Muster");

        // Static metadata headers
        wsMuster.Cell(1, 1).Value = "Employee ID";
        wsMuster.Cell(1, 2).Value = "Employee Name";
        wsMuster.Cell(1, 3).Value = "Date of Joining";
        wsMuster.Cell(1, 4).Value = "Department";

        var metaHeaders = wsMuster.Range(1, 1, 1, 4);
        metaHeaders.Style.Font.Bold = true;
        metaHeaders.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        metaHeaders.Style.Font.FontColor = XLColor.White;
        metaHeaders.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Day columns
        for (var day = 1; day <= daysInMonth; day++)
        {
            var colIndex = 4 + day;
            var cellDate = new DateTime(year, month, day);
            var isWeekend = offDays.Contains((int)cellDate.DayOfWeek);
            var isHoliday = holidays.Any(h => CalendarDateHelpers.MatchesDate(h, DateOnly.FromDateTime(cellDate)));

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

        // Fill employee rows
        var musterRow = 2;
        foreach (var emp in eligibleEmployees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            wsMuster.Cell(musterRow, 1).Value = emp.EmployeeId ?? emp.UserId;
            wsMuster.Cell(musterRow, 2).Value = $"{emp.FirstName} {emp.LastName}".Trim();
            
            var hasDoj = EmploymentPeriod.TryGetJoiningDate(emp, out var doj);
            wsMuster.Cell(musterRow, 3).Value = hasDoj ? doj.ToString("yyyy-MM-dd") : (emp.DateOfJoining ?? string.Empty);
            wsMuster.Cell(musterRow, 4).Value = emp.Department ?? string.Empty;

            var lwd = EmploymentPeriod.GetLastWorkingDate(emp);

            for (var day = 1; day <= daysInMonth; day++)
            {
                var colIndex = 4 + day;
                var cellDate = new DateTime(year, month, day);
                var cell = wsMuster.Cell(musterRow, colIndex);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (hasDoj && cellDate.Date < doj.Date)
                {
                    cell.Value = "N/A";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                }
                else if (lwd.HasValue && cellDate.Date > lwd.Value.Date)
                {
                    cell.Value = "N/A";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                }
                else if (holidays.Any(h => CalendarDateHelpers.MatchesDate(h, DateOnly.FromDateTime(cellDate))))
                {
                    cell.Value = "H";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7"); // light amber
                    cell.Style.Font.FontColor = XLColor.FromHtml("#92400E");
                }
                else if (offDays.Contains((int)cellDate.DayOfWeek))
                {
                    cell.Value = "WO";
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC"); // light slate
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

        // Add dropdown validation to all attendance cells
        var totalRows = Math.Max(2, musterRow - 1);
        if (totalRows >= 2 && daysInMonth > 0)
        {
            var dataRange = wsMuster.Range(2, 5, totalRows, 4 + daysInMonth);
            var dv = dataRange.CreateDataValidation();
            dv.List($"'Allowed Status Codes'!$A$2:$A${allowedCodesList.Count + 1}", true);
            dv.InputTitle = "Attendance Status";
            dv.InputMessage = "Select a valid status code (e.g., P, A, HD, WFH, WO, H, N/A).";
            dv.ErrorTitle = "Invalid Attendance Code";
            dv.ErrorMessage = "Please enter an allowed attendance status code from the list.";
            dv.ShowErrorMessage = true;
            dv.ShowInputMessage = true;

            var fullGrid = wsMuster.Range(1, 1, totalRows, 4 + daysInMonth);
            fullGrid.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            fullGrid.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            fullGrid.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            fullGrid.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
        }

        wsMuster.SheetView.FreezeRows(1);
        wsMuster.SheetView.FreezeColumns(4);
        wsMuster.Columns().AdjustToContents();

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
        CancellationToken cancellationToken = default)
    {
        var result = new AttendanceImportValidationResultDto
        {
            IsValid = true,
            TotalDaysInMonth = DateTime.DaysInMonth(year, month)
        };

        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            result.IsValid = false;
            result.ErrorCount = 1;
            result.Issues.Add(new AttendanceImportIssueDto
            {
                RowNumber = 0,
                Severity = "Error",
                Message = $"Invalid calendar period: Year {year}, Month {month}."
            });
            return result;
        }

        var daysInMonth = result.TotalDaysInMonth;
        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // 1. Guard check: Is payroll month locked?
        var isLocked = await _summaries.Exist(x => x.CompanyId == companyId && x.PayrollMonth == startOfMonth && x.IsLocked);
        if (isLocked)
        {
            result.IsValid = false;
            result.ErrorCount = 1;
            result.Issues.Add(new AttendanceImportIssueDto
            {
                RowNumber = 0,
                Severity = "Error",
                Message = $"Payroll month {year}-{month:D2} is locked. Attendance records cannot be imported."
            });
            return result;
        }

        // 2. Load Excel workbook
        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheet("Attendance Muster") ?? workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            result.IsValid = false;
            result.ErrorCount = 1;
            result.Issues.Add(new AttendanceImportIssueDto
            {
                RowNumber = 0,
                Severity = "Error",
                Message = "No worksheets found in the Excel workbook."
            });
            return result;
        }

        // 3. Map column headers
        var headerRow = sheet.Row(1);
        var empIdCol = 1;
        var dayColMap = new Dictionary<int, int>(); // day (1..31) -> colIndex

        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? (4 + daysInMonth);
        for (var col = 1; col <= lastCol; col++)
        {
            var headerText = headerRow.Cell(col).GetString()?.Trim() ?? string.Empty;
            if (headerText.Equals("Employee ID", StringComparison.OrdinalIgnoreCase) ||
                headerText.Equals("EmployeeId", StringComparison.OrdinalIgnoreCase) ||
                headerText.Equals("Emp ID", StringComparison.OrdinalIgnoreCase))
            {
                empIdCol = col;
            }
            else if (TryExtractDayNumber(headerText, year, month, daysInMonth, col - 4, out var parsedDay))
            {
                if (!dayColMap.ContainsKey(parsedDay))
                {
                    dayColMap[parsedDay] = col;
                }
            }
        }

        if (dayColMap.Count == 0)
        {
            // Fallback: columns 5 to 4 + daysInMonth
            for (var d = 1; d <= daysInMonth; d++)
            {
                dayColMap[d] = 4 + d;
            }
        }

        // 4. Fetch company employees
        var allEmployees = (await _employees.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)).ToList();
        var empByEmployeeId = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);
        var empByUserId = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var emp in allEmployees)
        {
            if (!string.IsNullOrWhiteSpace(emp.EmployeeId))
                empByEmployeeId[emp.EmployeeId.Trim()] = emp;
            if (!string.IsNullOrWhiteSpace(emp.UserId))
                empByUserId[emp.UserId.Trim()] = emp;
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
            x.CompanyId == companyId &&
            x.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            x.StartDate <= endOfMonth &&
            x.EndDate >= startOfMonth);

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

            var empIdVal = sheet.Cell(row, empIdCol).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(empIdVal))
                continue; // Skip empty rows

            EmpUser? employee = null;
            if (empByEmployeeId.TryGetValue(empIdVal, out var matchByEmpId))
                employee = matchByEmpId;
            else if (empByUserId.TryGetValue(empIdVal, out var matchByUserId))
                employee = matchByUserId;

            if (employee == null)
            {
                result.Issues.Add(new AttendanceImportIssueDto
                {
                    RowNumber = row,
                    ColumnHeader = "Employee ID",
                    EmployeeId = empIdVal,
                    Severity = "Error",
                    Message = $"Employee '{empIdVal}' was not found in company directory."
                });
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
                var colHeader = sheet.Cell(1, colIdx).GetString()?.Trim() ?? cellDate.ToString("dd MMM");
                var rawValue = sheet.Cell(row, colIdx).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    result.Issues.Add(new AttendanceImportIssueDto
                    {
                        RowNumber = row,
                        ColumnHeader = colHeader,
                        EmployeeId = employee.EmployeeId,
                        EmployeeName = empName,
                        Date = cellDateStr,
                        Severity = "Warning",
                        Message = $"Empty attendance status for {cellDateStr}. It will be skipped."
                    });
                    result.WarningCount++;
                    continue;
                }

                var normalizedCode = NormalizeStatusCode(rawValue);

                // Tenure check: Prior to joining date
                if (hasDoj && cellDate.Date < doj.Date)
                {
                    if (normalizedCode is not ("N/A" or "NA"))
                    {
                        result.Issues.Add(new AttendanceImportIssueDto
                        {
                            RowNumber = row,
                            ColumnHeader = colHeader,
                            EmployeeId = employee.EmployeeId,
                            EmployeeName = empName,
                            Date = cellDateStr,
                            Severity = "Error",
                            Message = $"Cannot mark attendance for {cellDateStr} prior to joining date ({doj:yyyy-MM-dd}). Cell must be 'N/A'."
                        });
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
                        result.Issues.Add(new AttendanceImportIssueDto
                        {
                            RowNumber = row,
                            ColumnHeader = colHeader,
                            EmployeeId = employee.EmployeeId,
                            EmployeeName = empName,
                            Date = cellDateStr,
                            Severity = "Error",
                            Message = $"Cannot mark attendance for {cellDateStr} after exit date ({lwd.Value:yyyy-MM-dd}). Cell must be 'N/A'."
                        });
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
                    result.Issues.Add(new AttendanceImportIssueDto
                    {
                        RowNumber = row,
                        ColumnHeader = colHeader,
                        EmployeeId = employee.EmployeeId,
                        EmployeeName = empName,
                        Date = cellDateStr,
                        Severity = "Error",
                        Message = $"Cannot mark attendance '{normalizedCode}' for future date {cellDateStr}."
                    });
                    result.ErrorCount++;
                    continue;
                }

                // Status code validity check
                if (!allowedCodes.Contains(normalizedCode))
                {
                    result.Issues.Add(new AttendanceImportIssueDto
                    {
                        RowNumber = row,
                        ColumnHeader = colHeader,
                        EmployeeId = employee.EmployeeId,
                        EmployeeName = empName,
                        Date = cellDateStr,
                        Severity = "Error",
                        Message = $"Invalid status code '{rawValue}' for date {cellDateStr}. Allowed codes: {string.Join(", ", allowedCodes.Take(12))}..."
                    });
                    result.ErrorCount++;
                    continue;
                }

                // Check approved leave conflict
                if (leaveByEmployeeDate.Contains((employee.UserId, cellDate.Date)))
                {
                    if (preserveApprovedLeaves)
                    {
                        result.Issues.Add(new AttendanceImportIssueDto
                        {
                            RowNumber = row,
                            ColumnHeader = colHeader,
                            EmployeeId = employee.EmployeeId,
                            EmployeeName = empName,
                            Date = cellDateStr,
                            Severity = "Warning",
                            Message = $"Employee has approved leave on {cellDateStr}. Existing leave will be preserved."
                        });
                        result.PreservedLeavesCount++;
                        result.WarningCount++;
                        result.ValidEntriesCount++;
                        continue;
                    }
                    else
                    {
                        result.Issues.Add(new AttendanceImportIssueDto
                        {
                            RowNumber = row,
                            ColumnHeader = colHeader,
                            EmployeeId = employee.EmployeeId,
                            EmployeeName = empName,
                            Date = cellDateStr,
                            Severity = "Warning",
                            Message = $"Approved leave on {cellDateStr} will be overwritten by imported status '{normalizedCode}'."
                        });
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
        CancellationToken cancellationToken = default)
    {
        // 1. Run Pre-flight Validation
        // We copy to MemoryStream first so we can reuse stream for both validation and reading
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var validation = await ValidateAsync(companyId, year, month, memoryStream, preserveApprovedLeaves, cancellationToken);
        if (!validation.IsValid)
        {
            return new AttendanceImportCommitResultDto
            {
                Success = false,
                Message = $"Validation failed with {validation.ErrorCount} error(s). Please correct them and re-upload.",
                Issues = validation.Issues
            };
        }

        memoryStream.Position = 0;
        using var workbook = new XLWorkbook(memoryStream);
        var sheet = workbook.Worksheet("Attendance Muster") ?? workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            return new AttendanceImportCommitResultDto
            {
                Success = false,
                Message = "No worksheets found in the Excel workbook."
            };
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // Map column headers
        var headerRow = sheet.Row(1);
        var empIdCol = 1;
        var dayColMap = new Dictionary<int, int>();

        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? (4 + daysInMonth);
        for (var col = 1; col <= lastCol; col++)
        {
            var headerText = headerRow.Cell(col).GetString()?.Trim() ?? string.Empty;
            if (headerText.Equals("Employee ID", StringComparison.OrdinalIgnoreCase) ||
                headerText.Equals("EmployeeId", StringComparison.OrdinalIgnoreCase) ||
                headerText.Equals("Emp ID", StringComparison.OrdinalIgnoreCase))
            {
                empIdCol = col;
            }
            else if (TryExtractDayNumber(headerText, year, month, daysInMonth, col - 4, out var parsedDay))
            {
                if (!dayColMap.ContainsKey(parsedDay))
                {
                    dayColMap[parsedDay] = col;
                }
            }
        }

        if (dayColMap.Count == 0)
        {
            for (var d = 1; d <= daysInMonth; d++)
                dayColMap[d] = 4 + d;
        }

        // Fetch company employees
        var allEmployees = (await _employees.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)).ToList();
        var empByEmployeeId = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);
        var empByUserId = new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var emp in allEmployees)
        {
            if (!string.IsNullOrWhiteSpace(emp.EmployeeId))
                empByEmployeeId[emp.EmployeeId.Trim()] = emp;
            if (!string.IsNullOrWhiteSpace(emp.UserId))
                empByUserId[emp.UserId.Trim()] = emp;
        }

        // Fetch existing attendance records for the month
        var existingAttendance = await _attendance.GetAll(x =>
            x.CompanyId == companyId &&
            x.Date >= startOfMonth &&
            x.Date <= endOfMonth.AddDays(1));

        var existingMap = existingAttendance
            .GroupBy(x => (x.UserId ?? string.Empty, x.Date.Date))
            .ToDictionary(g => g.Key, g => g.First());

        // Fetch approved leaves
        var acceptedLeaves = await _leaves.GetAll(x =>
            x.CompanyId == companyId &&
            x.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            x.StartDate <= endOfMonth &&
            x.EndDate >= startOfMonth);

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
        var recordsToUpsert = new List<AttendanceModel>();
        var affectedEmployees = new HashSet<EmpUser>();
        var preservedLeaves = 0;

        for (var row = 2; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var empIdVal = sheet.Cell(row, empIdCol).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(empIdVal))
                continue;

            EmpUser? employee = null;
            if (empByEmployeeId.TryGetValue(empIdVal, out var matchByEmpId))
                employee = matchByEmpId;
            else if (empByUserId.TryGetValue(empIdVal, out var matchByUserId))
                employee = matchByUserId;

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
                var normalizedCode = NormalizeStatusCode(rawValue);

                // Ignore N/A
                if (normalizedCode is "N/A" or "NA")
                    continue;

                // Out of employment period guard
                if (hasDoj && cellDate.Date < doj.Date)
                    continue;
                if (lwd.HasValue && cellDate.Date > lwd.Value.Date)
                    continue;

                // Future date guard
                if (cellDate.Date > today && normalizedCode is not ("WO" or "H"))
                    continue;

                // Leave preservation guard
                var hasExistingLeave = leaveByEmployeeDate.Contains((employee.UserId, cellDate.Date));
                if (existingMap.TryGetValue((employee.UserId, cellDate.Date), out var existingRow))
                {
                    if (preserveApprovedLeaves && (hasExistingLeave || existingRow.SourceType is "LEAVE" or "LIFECYCLE_CASE"))
                    {
                        preservedLeaves++;
                        continue;
                    }
                }
                else if (preserveApprovedLeaves && hasExistingLeave)
                {
                    preservedLeaves++;
                    continue;
                }

                var attendanceRow = new AttendanceModel
                {
                    AttendanceId = existingRow?.AttendanceId,
                    CompanyId = companyId,
                    UserId = employee.UserId,
                    EmployeeId = employee.EmployeeId,
                    Date = UtcMidnight(cellDate),
                    Status = normalizedCode,
                    SourceType = "BULK_ATTENDANCE_IMPORT",
                    SourceId = $"IMPORT_{year}_{month:D2}",
                    Remarks = "Bulk Attendance Import",
                    SystemRemark = $"Bulk imported by user {actorUserId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    MarkedByUserId = actorUserId,
                    MarkedAtUtc = DateTime.UtcNow,
                    Version = (existingRow?.Version ?? 0) + 1
                };

                recordsToUpsert.Add(attendanceRow);
            }
        }

        // 2. Perform atomic bulk write into MongoDB
        var upsertedCount = await _attendanceRepository.BulkUpsertAsync(recordsToUpsert, cancellationToken);

        // 3. Clean up any pre-joining or post-exit default rows for affected employees
        foreach (var emp in affectedEmployees)
        {
            if (EmploymentPeriod.TryGetJoiningDate(emp, out var doj))
            {
                var lwd = EmploymentPeriod.GetLastWorkingDate(emp);
                await _attendanceRepository.DeleteDefaultPresentOutsideEmploymentPeriodAsync(
                    companyId, emp.UserId, doj, lwd);
            }
        }

        // 4. Invalidate unlocked summaries for this month so muster/payroll recalculates cleanly
        await _summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x =>
            x.CompanyId == companyId && x.PayrollMonth == startOfMonth && !x.IsLocked));

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
            Message = $"Successfully imported {recordsToUpsert.Count} attendance records for {affectedEmployees.Count} employee(s).",
            EmployeesAffected = affectedEmployees.Count,
            RecordsUpserted = recordsToUpsert.Count,
            PreservedLeavesCount = preservedLeaves,
            Issues = validation.Issues.Where(x => x.Severity == "Warning").ToList()
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
            _ => trimmed
        };
    }

    private static bool TryExtractDayNumber(
        string header,
        int year,
        int month,
        int daysInMonth,
        int defaultDayIndex,
        out int dayNumber)
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
        if (DateTime.TryParse(header, out var parsedDate) && parsedDate.Year == year && parsedDate.Month == month)
        {
            dayNumber = parsedDate.Day;
            return true;
        }

        // 3. Starts with day digits followed by separator/month: "01 Oct (Thu)", "1-Oct"
        var match = Regex.Match(header, @"^(\d{1,2})(?:\s|[\/\-_]|[a-zA-Z])");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var leadingNum) && leadingNum >= 1 && leadingNum <= daysInMonth)
        {
            dayNumber = leadingNum;
            return true;
        }

        // 4. Default position fallback
        if (defaultDayIndex >= 1 && defaultDayIndex <= daysInMonth)
        {
            dayNumber = defaultDayIndex;
            return true;
        }

        return false;
    }
}
