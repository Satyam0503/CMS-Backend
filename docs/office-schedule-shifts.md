# Office Schedule and Shift Management

> **Authoritative scope:** Company Master shift configuration, its assignment
> precedence, and the attendance rules that consume it. This document does not
> authorize the browser to decide a company or employee scope; those are always
> resolved by the API from the authenticated request.

## Purpose

Company Master manages attendance shifts. A shift defines office timing and the rules used by automatic Present, attendance timing validation, late/early classification, and Work From Home schedule checks.

## Shift Data

Each `CompanyOfficeSchedule` stores:

- name and timezone;
- standard start and end time;
- permitted check-in and check-out windows;
- grace, break, and required working minutes;
- effective dates, active state, default state, and version.

All records include `CompanyId`. Reads and writes use the authenticated company
ID and validate selected shift IDs in that company. A schedule ID received from
the browser is therefore an identifier to validate, never proof of access.

## Admin Flow

1. The Shift list loads from `GET api/CompanyMaster/OfficeSchedules` for the
   authenticated company only.
2. Each persisted shift is a compact, expandable row. Its name, default badge,
   time range, and actions remain visible when the details are collapsed.
3. Expanding a row renders that row's editor immediately beneath its own header;
   it must not render at the bottom of the list or reuse another shift's state.
4. **Add Shift** inserts one unsaved row with the same editor and safe default
   values. It does not create a database document until Save succeeds.
5. Save creates or updates only the selected row. The UI reloads the canonical
   server list after a successful mutation, so every saved row survives refresh.
6. The default checkbox / **Make default** action is exclusive: saving a new
   default clears the default flag on the other schedules in that company.
7. Duplicate schedule creation is rejected by the server for the same tenant
   according to its normalized business identity; a double click or stale client
   state must not create a second schedule document.

## Employee Allocation

The employee-invite form and the employee profile both offer an optional Shift
selector. Department administration can also allocate an eligible shift to a
department.

- If HR selects a shift for an employee, an
  `EmployeeScheduleAssignment` is created or updated for that employee.
- If HR selects a shift for a department, the department assignment is used for
  eligible members that have no individual override.
- If allocation is skipped, no individual override is stored and the effective
  schedule is resolved automatically.
- A selected shift must be active, effective for the requested date, and belong
  to the authenticated company. The service verifies the employee and
  department are also in that same company.
- An individual employee assignment overrides a department assignment. Removing
  that override restores department/default inheritance rather than copying a
  schedule into the employee record.

## Attendance Behavior

The effective schedule is resolved as:

1. active employee assignment;
2. active department assignment;
3. active company default shift.

When attendance is generated or validated, the applied `ScheduleId` and
`ScheduleVersion` are retained on the attendance record. Changing a shift or
default affects future processing; it does not rewrite historical attendance.

The effective shift also supplies the permitted punch-in and punch-out windows.
The attendance editor can offer convenient time selections inside those windows,
but the backend is the authority: it rejects a time outside the resolved shift
window, including when a request bypasses the React UI. Stored API fields remain
`CheckInTime` and `CheckOutTime` for compatibility; user-facing labels use
**Punch in** and **Punch out**.

## Legacy Recovery

Earlier UI behavior displayed a default schedule without persisting it. If the only stored schedule is the legacy default `Shift 2`, the list endpoint creates a persisted `Default shift` and keeps `Shift 2` as a separate non-default shift.

## Deletion Policy

An eligible non-default shift can be deleted from its row action. The UI presents
the destructive action at the end of the row, with a confirmation and a brief
removal transition; it must not be positioned beneath the row or rely on a
border-heavy control to communicate the action.

The server blocks deletion when the schedule is the company default or has an
active employee/department assignment. A caller cannot bypass this by hiding a
button, submitting another tenant's schedule ID, or using a stale page. HR must
first select a replacement default or reassign dependent employees/departments.
Historical attendance continues to retain its stored schedule snapshot.

## Frontend layout and responsive contract

The Company Master Office Schedule page is a single shift-list container. It is
not a collection of disconnected forms.

- The heading and **Add Shift** action share a row on wide screens and stack with
  a full-width, reachable action on narrow screens.
- Each shift header uses a stable three-part layout: identity/default state,
  time range, then row actions/expand control. Actions do not push the time
  range to another line unnecessarily.
- Expanded fields use responsive grids: summary metrics may be four columns on
  wide screens, two columns on medium screens, and one column on small screens.
  Check-in and check-out panels remain readable before their fields stack.
- Form labels, helper text, buttons, switches, and date/time controls use the
  shared frontend primitives. A compact action is not allowed to reduce touch
  target accessibility or obscure its meaning.
- Save state, validation errors, disabled state, empty state, and delete errors
  are shown beside the affected shift. A failed save leaves the unsaved row
  editable and does not make the client list appear persisted.

## Verification checklist

- Create two shifts in one company; reload and confirm both rows are returned.
- Attempt the same normalized shift identity twice in the same company and
  confirm the second request is rejected without creating another document.
- Create the same business identity in a second company and confirm no data or
  default state is shared.
- Set a new default; confirm exactly one default exists and new employees without
  an explicit assignment resolve to it.
- Assign a department shift, then an employee override; verify the employee
  takes the override and returns to the department/default shift when removed.
- Attempt to assign or delete a schedule from another company, delete the
  default, and delete an assigned shift; all must fail server-side.
- Verify desktop, tablet, and narrow mobile widths: row actions are aligned,
  expanded details appear below the matching row, and no text or control is
  clipped.
