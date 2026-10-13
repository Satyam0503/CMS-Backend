# ATTENDANCE MODULE (`ATTENDANCE.md`)

## 1. Module Overview
The **Attendance Module** is a core operational engine in **Codeji CMS / HRMS** that tracks daily employee check-in/check-out activity, evaluates shift compliance (`P`, `HD`, `LHD`, `ED`, `LHD+ED`), calculates late/early penalties (`AttendancePenaltyService`), enforces offboarding cutoffs (`EmployeeExitAttendanceCutoff`), and produces locked monthly attendance summaries (`MonthlyAttendanceSummary` & `AttendanceMonthLock`) required by `AutoPayrollServices`.

---

## 2. Real-World Business Scenario
1. **Daily Check-In / Check-Out**:
   - Rahul Sharma's effective office schedule (`CompanyOfficeSchedule`) is `09:30` to `18:30` with `CheckInAllowedUntil = 09:45` (`15m` grace) and `CheckOutAllowedFrom = 18:15`.
   - On Tuesday, Rahul checks in at `10:02` and checks out at `18:05`.
   - `AttendanceStatusCalculationService.CalculateStatus` detects `isLate = true` (`10:02 > 09:45`) and `isEarly = true` (`18:05 < 18:15`), assigning daily status **`LHD+ED`** (`Late Half Day + Early Departure`).
2. **Monthly Penalty Evaluation**:
   - Over April 2026, Rahul accumulates 4 occurrences of `LHD` / `ED` / `LHD+ED`.
   - `AttendancePenaltyService.EvaluateMonthlyPenaltiesAsync` compares this against `AttendancePenaltyPolicy.CombinedLhdEdMonthlyLimit = 3`. Because `4 > 3`, it raises an `AttendancePayrollException` (`Status = "PENDING_REVIEW"`, `PenaltyDays = 0.5`).
3. **HR Resolution & Month Lock**:
   - HR Manager reviews the exception via `POST /api/attendance-penalty/resolve` (`APPROVED`, `FinalPenaltyDays = 0.5`).
   - HR then runs `POST /api/attendance/lock-month`, which calls `AttendanceMonthLock.ValidateAndLock()` and locks `MonthlyAttendanceSummary` (`IsApproved = true`, `IsLocked = true`) so April payroll can run.

---

## 3. Actors
| Actor | Capabilities |
|---|---|
| **Employee** | Performs daily check-in/check-out, views personal attendance calendar, status codes (`P`, `LHD`, `ED`, `HD`), and monthly summary. |
| **Reporting Manager** | Views team attendance dashboard, late arrival trends, and attendance exceptions. |
| **HR Admin / Payroll Admin** | Configures `AttendancePenaltyPolicy`, edits/regularizes attendance (`AttendanceAuditLog`), resolves `AttendancePayrollException` items, and locks `AttendanceMonthLock`. |
| **Background Worker (`AttendanceReminderHostedService`)** | Dispatches automated check-in/check-out reminders (`AttendanceReminderDispatchLog`) according to `AttendanceReminderSetting`. |

---

## 4. Frontend Implementation
| File | Responsibility | Calls | Used By |
|---|---|---|---|
| `src/app/modules/attendance/pages/AttendancePage.tsx` | Main attendance calendar, daily punch widget, and status legend (`P`, `HD`, `LHD`, `ED`, `LHD+ED`). | `GET /api/attendance`, `POST /api/attendance/check-in`, `POST /api/attendance/check-out` | Employee / HR |
| `src/app/modules/attendance/EmployeeAttendance.tsx` | Monthly attendance report view with calendar grid, punch modal, and 4-tab Excel export trigger. | `GET /api/admin/attendance/export/monthly` | HR / Admin |
| `src/app/modules/attendance/components/MonthlySummaryTable.tsx` | Displays employee monthly rollup (`PresentDays`, `UnpaidDays`, `LhdCount`, `EdCount`, lock state). | `GET /api/attendance/monthly-summary` | HR / Payroll Admin |
| `src/app/modules/attendance/components/PenaltyExceptionsModal.tsx` | Lists `PENDING_REVIEW` exceptions from `AttendancePayrollException` for HR approval or waiver. | `GET /api/attendance-penalty/exceptions`, `POST /api/attendance-penalty/resolve` | HR Admin |

---

## 5. Backend Implementation
| Backend File | Responsibility | Dependencies |
|---|---|---|
| `Controllers/AttendanceController.cs` | Handles check-in, check-out, regularization, monthly summary generation, month locking, and monthly 4-tab Excel workbook export. | `IAttendanceService`, `IAttendancePenaltyService`, `IAdminAttendanceService` |
| `Services/AttendanceService.cs` | `ExportMonthlyAttendanceAsync` generates closedXML 4-tab workbook (`Attendance Muster`, `Daily Punch Logs`, `Monthly Summary`, `Legend & Policy`) with sanitized remarks and pastel styling. | `IMongoDbRepository<EmpUser>`, `IAttendanceRepository`, `IMongoDbRepository<AttendanceDaySegment>`, `ICompanyWorkingCalendarService` |
| `Services/AttendanceStatusCalculationService.cs` | Pure deterministic calculation of daily status (`P`, `HD`, `LHD`, `ED`, `LHD+ED`) from punch times and `CompanyOfficeSchedule`. | `EffectiveOfficeScheduleService` |
| `Services/AttendancePenaltyService.cs` | Evaluates monthly `LHD`/`ED` counts against `AttendancePenaltyPolicy.CombinedLhdEdMonthlyLimit`, creates `AttendancePayrollException`, and enforces `AttendanceMonthLock.ValidateAndLock()`. | `IMongoRepository<AttendancePenaltyPolicy>`, `IMongoRepository<AttendancePayrollException>`, `IMongoRepository<AttendanceMonthLock>` |
| `Services/HostedServices/AttendanceReminderHostedService.cs` | Periodic `BackgroundService` that checks `AttendanceReminderSetting` and sends check-in/out notifications via `NotificationService`. | `IMongoRepository<AttendanceReminderDispatchLog>` |

---

## 6. API Documentation
| Method | URL | Purpose | Auth / Permission | Request / Response | Side Effects |
|---|---|---|---|---|---|
| `GET` | `/api/admin/attendance/export/monthly?year={y}&month={m}` | Exports production-ready 4-tab Excel workbook (`attendance-report-YYYY-MM.xlsx`). | JWT (`attendance:read`) | Stream `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | Generates 4 worksheets: Muster, Punch Logs, Payroll Summary, Legend |
| `POST` | `/api/attendance/check-in` | Records employee daily check-in timestamp. | JWT (`attendance:create`) | `CheckInRequestDto` -> `AttendanceDto` | Checks `EmployeeExitAttendanceCutoff`; calculates initial status |
| `POST` | `/api/attendance/check-out` | Records employee daily check-out and computes worked hours & `Status`. | JWT (`attendance:update`) | `CheckOutRequestDto` -> `AttendanceDto` | Runs `AttendanceStatusCalculationService.CalculateStatus` |
| `PUT` | `/api/attendance/{id}/regularize` | HR/Manager manual override of punch times or status. | JWT (`attendance:update`) | `RegularizeAttendanceDto` | Writes immutable `AttendanceAuditLog` (`PreviousValue`, `NewValue`, `Reason`) |
| `POST` | `/api/attendance-penalty/evaluate` | Evaluates monthly `LHD`/`ED` limits and creates `AttendancePayrollException` records. | JWT (`attendance:update`) | `{ Year, Month }` | Creates/updates `AttendancePayrollException` |
| `POST` | `/api/attendance-penalty/resolve` | Approves or waives a `PENDING_REVIEW` penalty exception. | JWT (`attendance:approve`) | `ResolveExceptionDto` (`Status`, `FinalPenaltyDays`) | Unblocks month lock when zero `PENDING_REVIEW` remain |
| `POST` | `/api/attendance/lock-month` | Validates all summaries and locks the month for payroll. | JWT (`attendance:approve`) | `{ Year, Month }` | Calls `AttendanceMonthLock.ValidateAndLock()` |

---

## 7. Database Model
```mermaid
erDiagram
    Attendance ||--o{ AttendanceAuditLog : "audits"
    MonthlyAttendanceSummary ||--o{ AttendancePayrollException : "triggers"
    AttendanceMonthLock ||--|{ MonthlyAttendanceSummary : "locks month"

    Attendance {
        string Id PK
        string CompanyId FK
        string EmployeeId FK
        DateTime Date
        DateTime CheckInTime
        DateTime CheckOutTime
        string Status "P | HD | LHD | ED | LHD+ED | A | L"
        double TotalHours
    }
    MonthlyAttendanceSummary {
        string Id PK
        string CompanyId FK
        string EmployeeId FK
        int Year
        int Month
        double PresentDays
        double UnpaidDays
        int LhdCount
        int EdCount
        bool IsApproved
        bool IsLocked
    }
    AttendancePayrollException {
        string Id PK
        string CompanyId FK
        string EmployeeId FK
        int Year
        int Month
        string Status "PENDING_REVIEW | APPROVED | WAIVED"
        double CalculatedPenaltyDays
        double FinalPenaltyDays
    }
    AttendanceMonthLock {
        string Id PK
        string CompanyId FK
        int Year
        int Month
        bool IsLocked
        DateTime LockedAtUtc
        string LockedByUserId
    }
```

---

## 8. Business Rules
- **Daily Status Rule (`AttendanceStatusCalculationService`)**:
  - `HalfDayThresholdHours`: If total worked hours `< HalfDayThresholdHours` (or missing check-out), status is `HD` (Half Day).
  - `isLate`: `CheckInTime > (StartTime + GraceMinutes)` (`CheckInAllowedUntil`).
  - `isEarly`: `CheckOutTime < CheckOutAllowedFrom`.
  - Status matrix:
    - `!isLate && !isEarly` $\rightarrow$ **`P`** (Present)
    - `isLate && !isEarly` $\rightarrow$ **`LHD`** (Late Half Day / Late Arrival)
    - `!isLate && isEarly` $\rightarrow$ **`ED`** (Early Departure)
    - `isLate && isEarly` $\rightarrow$ **`LHD+ED`** (Combined Late + Early)
- **Offboarding Cutoff Rule**: If an `EmployeeExitAttendanceCutoff` exists for the employee and `Date > LastWorkingDayUtc`, check-in is blocked.
- **Penalty Rule (`AttendancePenaltyService`)**: If `(LhdCount + EdCount + LhdEdCount) > AttendancePenaltyPolicy.CombinedLhdEdMonthlyLimit`, an `AttendancePayrollException` (`PENDING_REVIEW`) is created.
- **Month Lock Gate (`AttendanceMonthLock.ValidateAndLock`)**: A month cannot be locked if any `AttendancePayrollException` in that `(CompanyId, Year, Month)` has `Status == "PENDING_REVIEW"`.

---

## 9. Complete Workflow
```mermaid
flowchart TD
    Punch["Employee Check-In & Check-Out"] --> Cutoff{"Check EmployeeExitAttendanceCutoff"}
    Cutoff -->|After LWD| Reject["403 Attendance Blocked Post-Exit"]
    Cutoff -->|Allowed| Schedule["Resolve EffectiveOfficeSchedule\n(StartTime, EndTime, GraceMinutes)"]
    Schedule --> Calc["AttendanceStatusCalculationService.CalculateStatus()\n-> P | HD | LHD | ED | LHD+ED"]
    Calc --> SaveDaily["Persist Attendance Entity"]
    SaveDaily --> MonthEnd["Month-End Aggregation -> MonthlyAttendanceSummary"]
    MonthEnd --> Penalty["AttendancePenaltyService.EvaluateMonthlyPenaltiesAsync()"]
    Penalty --> CheckLimit{"(LHD + ED) > CombinedLhdEdMonthlyLimit?"}
    CheckLimit -->|Yes| CreateEx["Create AttendancePayrollException (PENDING_REVIEW)"]
    CheckLimit -->|No| ReadyLock["Ready for Month Lock"]
    CreateEx --> HRResolve["HR Resolves Exception (APPROVED / WAIVED)"]
    HRResolve --> ReadyLock
    ReadyLock --> Lock["AttendanceMonthLock.ValidateAndLock()\nIsLocked = true, IsApproved = true"]
```

### 9.1 Enterprise 4-Tab Attendance Export Architecture
The monthly export (`GET /api/admin/attendance/export/monthly`) generates a comprehensive OpenXML Excel workbook (`attendance-report-YYYY-MM.xlsx`) structured across 4 dedicated worksheets:

1. **Tab 1 — Attendance Muster (`Attendance Muster`)**:
   - **Demographic Columns**: `Employee ID`, `Employee Name`, `Department`, `Designation` (frozen horizontally across scrolling).
   - **Daily Matrix (Days 1 to 28/30/31)**: Compact status code badge (`P`, `A`, `HD`, `WFH`, `PL`, `WO`, `H`) with IST punch timestamps (`09:15 - 18:30`).
   - **Sanitization Engine**: Strips internal GUIDs and system audit notes; replaces raw database strings with clean human tags (e.g. `"Approved WFH"`, `"Approved Leave"`).
   - **Pastel Styling**: Soft sage (`#E8F5E9` fill / `#166534` text) for Present, soft rose (`#FEE2E2` / `#991B1B`) for Absent, warm amber (`#FEF3C7` / `#92400E`) for Half Day, soft sky (`#E0F2FE` / `#0369A1`) for WFH, and slate tint (`#F8FAFC` / `#64748B`) for Weekends.
   - **Monthly Rollup**: Right-side sticky summary columns (`Present`, `Half Days`, `WFH`, `Paid Leaves`, `Absent / LOP`, `Weekly Offs`, `Holidays`, `Working Days`, `Total Paid Days`, `Late In`, `Early Exit`).
   - **Bottom Summaries**: Daily aggregate rows for `TOTAL STAFF PRESENT` and `TOTAL STAFF ABSENT`.
2. **Tab 2 — Daily Punch Logs (`Daily Punch Logs`)**:
   - Flat tabular audit table: `Date`, `Day`, `Employee ID`, `Employee Name`, `Department`, `Designation`, `Status`, `Check-In (IST)`, `Check-Out (IST)`, `Hours`, `Late?`, `Early Exit?`, `Day Type`, `Remarks`.
   - Alternating zebra striping with Excel auto-filters; ideal for compliance audits and PivotTables.
3. **Tab 3 — Monthly Summary (`Monthly Summary`)**:
   - Payroll-ready reconciliation: `Employee ID`, `Name`, `Department`, `Designation`, `Date of Joining`, `Expected Work Days`, `Eligible Work Days`, `Present Days`, `Half Days`, `WFH Days`, `Paid Leaves`, `Absent / LOP`, `Weekly Offs`, `Holidays`, `Total Paid Days`, `Total Unpaid Days`, `Work Hours`, `Late Count`, `Early Exit Count`, `Payroll Status`.
   - Native `=SUM(...)` Grand Totals row. Highlighting LOP > 0 in soft red.
4. **Tab 4 — Legend & Policy (`Legend & Policy`)**:
   - Organization metadata, status code matrix with preview badges, paid factor multipliers (`1.0x`, `0.5x`, `0.0x`), and calculation formulas (Payable Days, Eligible Days, LOP rules).

---

## 10. State Machine
```mermaid
stateDiagram-v2
    [*] --> OpenMonth : Daily Punches Recorded
    OpenMonth --> SummaryGenerated : Generate MonthlyAttendanceSummary
    SummaryGenerated --> ExceptionPending : CombinedLhdEdMonthlyLimit Exceeded (PENDING_REVIEW)
    SummaryGenerated --> ReadyToLock : Zero Exceptions
    ExceptionPending --> ReadyToLock : HR Resolves All (APPROVED / WAIVED)
    ReadyToLock --> Locked : AttendanceMonthLock.ValidateAndLock()
    Locked --> PayrollProcessed : Consumed by AutoPayrollServices
```

---

## 11. Permissions
- `attendance:read` — View personal or team attendance records.
- `attendance:create` / `attendance:update` — Check-in, check-out, and manual regularization.
- `attendance:approve` — Resolve `AttendancePayrollException` and execute `AttendanceMonthLock`.

---

## 12. Dependencies
- **Upstream**: `WORK-SHIFT` (`EffectiveOfficeScheduleService`), `CALENDAR` (`CompanyWorkingCalendarService`), `LEAVE` & `WFH` (approved leaves/WFH overlay daily status), `OFFBOARDING` (`EmployeeExitAttendanceCutoff`).
- **Downstream**: `PAYROLL` (`AutoPayrollServices` requires locked `MonthlyAttendanceSummary` and reads approved `FinalPenaltyDays`).

---

## 13. Cross-Module Events
- **Manual Regularization**: Appends an immutable record to `AttendanceAuditLog`.
- **Month Lock**: Unblocks `AutoPayrollServices.GeneratePayrollForMonthAsync` for that `(CompanyId, Year, Month)`.

---

## 14. Failure Scenarios
- **Attempting to Lock Month with Unresolved Exceptions**: `AttendanceMonthLock.ValidateAndLock()` throws a domain validation error listing the unresolved `PENDING_REVIEW` exceptions.
- **Modifying Locked Month Attendance**: Regularization calls against a date in a locked `AttendanceMonthLock` are rejected (`409 Conflict`).

---

## 15. Edge Cases
- **Half-Day Leave + Punch**: When an employee has an approved half-day leave (`FirstHalf` or `SecondHalf`) or half-day WFH, `EffectiveOfficeScheduleService` adjusts the expected check-in/check-out window accordingly so they are not falsely marked `LHD` or `ED`.

---

## 16. Current Strengths
- Deterministic, policy-driven daily status classification (`P`, `HD`, `LHD`, `ED`, `LHD+ED`).
- Hard pre-payroll gate (`AttendanceMonthLock` + `AttendancePayrollException` review) prevents unverified attendance from leaking into salary calculations.

---

## 17. Current Gaps
- Biometric hardware webhook ingestion is not natively wired; punches currently originate from web/mobile API requests.

---

## 18. Recommended Improvements
- Add an idempotent batch punch ingestion endpoint (`POST /api/attendance/biometric-sync`) for external biometric devices.

---

## 19. Future Scope
- Geofenced / IP-whitelisted check-in rules per `CompanyOfficeSchedule`.
