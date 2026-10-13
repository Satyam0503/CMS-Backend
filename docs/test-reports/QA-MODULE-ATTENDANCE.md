# Attendance audit

## Purpose and entry points

Frontend: `modules/attendance/EmployeeAttendance.tsx`, calendar, status settings, and monthly exceptions.  
Backend: `AdminAttendanceController`, attendance/status/weekly-off/penalty services, and leave reconciliation.

## Authorization and tenant checks

All admin attendance endpoints require authentication plus Attendance module permissions. Controller methods pass the JWT-derived company ID into services; services look up employee and attendance records with company/user/date predicates.

## State and controls

Manual add/update is protected by an attendance edit guard. The audited services account for weekly offs, holidays, status settings, monthly summaries, exception records, and payroll locking.

## Confirmed bugs

No fresh confirmed attendance defect was identified by source-only review.

## Risks and missing tests

- Validate time-zone/date-boundary behavior using the company’s intended locale, especially midnight and DST-style offset changes.
- Test simultaneous manual attendance edits and leave reconciliation for the same employee/date.
- Test locked/reopened month behavior with a payroll record already processed.
- Validate that bulk/range endpoints reject cross-tenant user IDs and very large date ranges predictably.
