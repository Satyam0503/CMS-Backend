# Current Working Context

## Workspace

- Backend: `CMS-Backend-Core` (.NET, MongoDB, multi-tenant).
- Frontend: sibling `CMS-React` (React, TypeScript, MUI).
- Tenant identity is derived on the server from the authenticated request. Client supplied company IDs are not trusted.

## Current Work: Office Schedules / Shifts

The Company Master Office Schedule screen is being expanded from one company schedule into multiple company-scoped shifts.

- Each shift is a `CompanyOfficeSchedule` MongoDB document.
- One active shift is the company default.
- Effective schedule order is employee assignment, department assignment, then company default.
- New employees may receive a selected shift override. When no override is selected, they resolve to the company default automatically.
- Attendance rows retain their schedule ID and version at the time they are generated; future processing uses the current effective schedule.

## Recent Changes

- Added a company-scoped list endpoint: `GET api/CompanyMaster/OfficeSchedules`.
- The Office Schedule UI shows shifts as expandable rows and reloads the authoritative server list after save.
- The first/default schedule is persisted rather than being a browser-only fallback.
- The known legacy state containing only a default `Shift 2` is repaired by creating `Default shift` and retaining `Shift 2` as a non-default shift.
- Employee invitation includes an optional Shift selector. The selected shift is validated against the authenticated company before an employee assignment is stored.

## Outstanding Work

- Add safe delete/retire-shift functionality. It must reject deletion of the default shift and any shift with active employee or department assignments until those assignments are moved.
- Add focused integration tests for two-company isolation, default switching, and save/reload persistence.

