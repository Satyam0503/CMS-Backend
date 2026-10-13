# Employee management audit

## Purpose and entry points

Frontend: `modules/employees/Employees.tsx`, employee services, profile routes, import/invite dialogs.  
Backend: `UserController` covers invitation, bulk import, employee updates, self-profile editing, lookup, profile sections, images, skills, work history, and notifications. Controller authentication is required; most administrative operations declare module permissions.

## Data flow and tenant boundary

`EmployeeService` uses the generic repository, whose normal reads apply the resolved company filter. Employee profile data is linked by `UserId`; invitation/password onboarding then uses account security tokens.

## Confirmed bugs

No new confirmed employee-specific defect was identified in this source-only pass. Previously documented leave-profile UI defects are excluded because they are recorded as addressed in `QA-BUG-REPORT-3.md`.

## Risks and missing tests

- Validate that every profile sub-resource verifies the target `UserId` belongs to the caller's company and that self-service methods reject another employee ID.
- Bulk import needs isolated tests for duplicates within a file, duplicate existing emails, partial failure reporting, and rollback expectations.
- Upload/image paths need isolated content-type, size, filename, access-control, and stale-file cleanup tests.
- Test authorization independently of frontend `ProtectedRoute`; route hiding is not backend enforcement.

## Priority

Build two-tenant API tests for employee lookup/update/delete and focused tests for every ID-bearing profile endpoint.
