# Attendance Status Settings — deep dive

This document explains how attendance status definitions (codes, colors, paid/unpaid fractions, time flags) are stored, seeded, validated and used across the Codeji CMS system. It covers data model details, service logic, tenant scoping (multi-company behavior), UI usage, validation rules, migration considerations, and an admin checklist to safely change or rename codes.

Files referenced
- Model: `Codeji.CMS.Repository/Entities/Attendance/AttendanceStatusSetting.cs`
- Service: `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs`
- Attendance rows: `Codeji.CMS.Repository/Entities/Attendance/AttendanceModel.cs`
- Status serializer: `Codeji.CMS.Repository/Entities/Attendance/AttendanceStatusCodeSerializer.cs`
- Leave mapping & validation: `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs`
- Migration note: `Codeji.CMS.Migrations/Migrations/BackfillAttendanceStatusPresentationAndLeaveEligibility.cs`

---

## 1. Purpose and high-level behavior

Attendance status settings define the set of codes that represent an employee's attendance state on a particular day or shift. Each status has:
- `Code` (string): short canonical identifier used in `AttendanceModel.Status` (e.g. `P`, `CL`, `WFH`).
- `Name` (string): human-readable label.
- `ColorHex` (string): UI display color (6-digit hex string) used by calendars and reports.
- `RequiresTime` (bool): whether the status requires clock-in/clock-out times (e.g. `Present` requires time, `Casual Leave` does not).
- `IsAvailableForLeaveManagement` (bool): whether a leave policy may use this status as the mapped attendance result when a leave is approved (only no-time statuses are eligible).
- `PaidDayFraction` and `UnpaidDayFraction` (decimal): how the status contributes to payroll days.

The system persists these definitions per-company (tenant) and all reads/writes filter by `CompanyId`.

---

## 2. Data model (fields explained)

Open file: `Codeji.CMS.Repository/Entities/Attendance/AttendanceStatusSetting.cs`

Key fields and their semantics:
- `Id` (string): Mongo primary id.
- `CompanyId` (string, required): tenant id — **every row belongs to exactly one company**.
- `Code` (string, required): canonical code stored on `AttendanceModel.Status`.
- `Name` (string, required): display name.
- `IsActive` (bool): whether the status may be used in UI and admit selection.
- `IsSystem` (bool): marks seeded defaults (useful for UI hints but not enforced as immutable).
- `SortOrder` (int): display order in UIs.
- `RequiresTime` (bool): if true, status expects check-in/check-out times. UIs will show time entry controls accordingly.
- `IsAvailableForLeaveManagement` (bool): if true and the status does NOT require time, leave policies may map to this status when approved.
- `ColorHex` (string): hex color for UI presentation (default `#607D8B`).
- `PaidDayFraction`, `UnpaidDayFraction` (decimal): payroll contribution fractions.

All fields are per-company.

---

## 3. Default seeding

When the attendance status service is asked for statuses for a company and none exist, it seeds a default array of statuses for that `CompanyId`:
- The default array (in `AttendanceStatusService`) contains codes such as: `P`, `A`, `SL`, `CL`, `EL`, `WFH`, `HD`, `ED`, `LHD`, `WFH+WFO`, `COMP-OFF`, `CL-HALF`, `SL-HALF`, `WFH-HD`, `UL` with preconfigured colors, time flags and paid/unpaid fractions.
- Each seeded row is stored with `CompanyId = <companyId>` and `IsSystem = true`.

This design means every company owns its copy of the defaults and can afterwards customize them independently.

---

## 4. Service operations (read, save, validation)

Key service: `AttendanceStatusService` (see `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs`).

- `Get(companyId, activeOnly)`
  - Returns the list of `AttendanceStatusSetting` rows for the company, optionally only active ones.
  - If no rows exist for the company, it seeds the default set (see section 3) and persists them.
  - The returned DTO ensures `ColorHex` is set to a fallback if empty.

- `Save(companyId, dto)`
  - Validates numeric bounds on `PaidDayFraction` and `UnpaidDayFraction` (each must be between 0 and 1 and sum <= 1).
  - Enforces `Code` and `Name` non-empty and `ColorHex` is a valid 6-digit hex.
  - Prevents setting `IsAvailableForLeaveManagement && RequiresTime` (a status that requires time cannot be used by leave mapping).
  - Normalizes `Code` to uppercase trimmed value.
  - If an existing row with same `Id` or `Code` exists in the company, it updates that row; otherwise it inserts a new row with `CompanyId`.

All operations filter by `CompanyId` to maintain tenant isolation.

---

## 5. How statuses are used by other modules

- Attendance storage: `AttendanceModel.Status` stores only the **code** string (serialized via `AttendanceStatusCodeSerializer` which supports legacy numeric values). See `Codeji.CMS.Repository/Entities/Attendance/AttendanceModel.cs` and `AttendanceStatusCodeSerializer.cs`.
- UI rendering: frontend queries the company’s `AttendanceStatusSetting` to map `Status` codes to `Name` and `ColorHex` for calendar and grid displays.
- Leave policy mapping: `LeavePolicy` may include an `AttendanceStatusCode`. The leave service validates that the referenced code exists in the **same company**, is active, is not `RequiresTime`, and `IsAvailableForLeaveManagement == true` before it accepts and uses the mapping. This validation avoids cross-tenant or invalid mappings.
- Payroll: `PaidDayFraction` and `UnpaidDayFraction` are used by payroll services to compute day fractions during payroll generation.

---

## 6. Tenant isolation: multi-company behavior explained

- Each `AttendanceStatusSetting` row includes `CompanyId`. All service queries include `CompanyId == <current company>`.
- When company X updates a status (code/name/color), that update is applied only to rows where `CompanyId == X`.
- Company Y's settings are a separate set (even the initially seeded defaults are duplicated per company), so edits do not cross companies.
- When code lookup occurs (for leave mapping or UI), the service always searches within the tenant's `CompanyId`.

Implication: administrators of company X can freely edit and rename status codes/colors without affecting company Y.

---

## 7. Editing status codes: consequences & safe patterns

A status `Code` is the canonical value stored in `AttendanceModel.Status`. Renaming a code requires care:

Risks when changing `Code` for a company:
- Historical `AttendanceModel` rows for that company still store the old code; UI mapping will not find a display name/color unless you migrate historical rows or leave the old code present.
- Leave policies that reference the old code must be updated to point to the new code or validation will fail.

Safe procedure to rename a code for a company (recommended admin checklist):
1. Create the new `AttendanceStatusSetting` row with the new `Code` and desired metadata (color, paid fraction).
2. Update all `LeavePolicy` rows for the company that referenced the old code to the new code (if applicable).
3. Option A (preferred): Migrate historical `AttendanceModel` rows to the new code in a single atomic background job:
   - Use a batched update: `UpdateMany({ CompanyId: X, Status: oldCode }, { $set: { Status: newCode } })`.
   - Ensure you run this under the company tenant filter and take a snapshot backup.
4. Option B: keep the old code row as an inactive/display-only entry so older attendance rows still render correctly. Add a deprecation note in UI.
5. Re-run any reconciliation or payroll preview to detect unexpected side-effects.

If you need, I can draft a safe migration script for step 3 (batched Mongo update with progress logging and rollback plan).

---

## 8. Migrations and compatibility

- The project contains a migration `BackfillAttendanceStatusPresentationAndLeaveEligibility` that added `ColorHex` and `IsAvailableForLeaveManagement` fields while preserving existing company customizations. Migration files are located under `Codeji.CMS.Migrations/Migrations`.
- `AttendanceStatusCodeSerializer` allows older integer-coded attendance rows to deserialize into the string codes for compatibility.

---

## 9. Admin checklist for common operations

- Add a new code
  - Use the `AttendanceStatusService.Save(companyId, dto)` API or admin UI.
  - Ensure `ColorHex` is a valid `#RRGGBB` hex.
  - Decide `RequiresTime` and `IsAvailableForLeaveManagement` flags.

- Update display color
  - Update `ColorHex` for the status; UI will pick up the new color for both existing and new attendance rows.

- Rename a code
  - Create the new code, update leave policies, migrate attendance rows (or keep legacy code as inactive), and notify payroll/HR.

- Remove a code
  - Prefer to mark it `IsActive=false` rather than deleting. Deleting leaves historical attendance rows referencing the code without a mapping.

---

## 10. Where to look in the code when things go wrong

- Are company rows present? `AttendanceStatusSetting` collection for `CompanyId` should have rows. If empty, requesting `Get` will cause seeding.
- Lookup failures for leave mapping: check `LeaveManagementService.ValidateAttendanceStatusMapping`.
- UI coloring issues: check the UI code that fetches the attendance statuses (frontend) — it reads `ColorHex` and maps codes to colors.
- Legacy numeric statuses: `AttendanceStatusCodeSerializer.Deserialize` supports numeric legacy data.

---

## 11. Optional next steps I can perform for you
- Draft a reversible MongoDB migration script to rename a code across a single company (with progress logging and safety checks).
- Add an admin UI checklist page in the repo (markdown) with step-by-step commands and rollback steps.
- Add automated tests that ensure `ValidateAttendanceStatusMapping` logic prevents invalid mappings.

Tell me which of the above you want next and I'll implement it (I can start with the migration script if you plan to rename codes).