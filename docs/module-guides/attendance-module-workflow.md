# Attendance Module Workflow

## Scope
The attendance module tracks daily presence, attendance edits, correction requests, monthly calendars, and attendance data used downstream by leave and payroll processes.

## Architectural role
Attendance is one of the most operationally important modules because it anchors daily presence information and feeds several downstream systems. It also has a self-service branch so employees can interact with their own attendance without broad admin privileges.

## Main entry points
- Admin controller: [Codeji.CMS.API/Controllers/AttendanceController.cs](../Codeji.CMS.API/Controllers/AttendanceController.cs)
- Employee self-service controller: [Codeji.CMS.API/Controllers/MyAttendanceController.cs](../Codeji.CMS.API/Controllers/MyAttendanceController.cs)
- Service: [Codeji.CMS.Services/Attendance/AttendanceService.cs](../Codeji.CMS.Services/Attendance/AttendanceService.cs)

## End-to-end flow
1. An employee or admin creates or updates attendance for a specific date.
2. The controller validates the request and resolves the current company and user from auth context.
3. Attendance is stored and can be queried by date range or month.
4. Employees can request corrections for attendance entries.
5. Admins review those correction requests and either approve or reject them, updating the attendance record as needed.
6. Attendance data is consumed by payroll and leave-related scenarios.

## Detailed behavior
### Admin attendance flow
- Admin routes allow manual attendance creation and updates.
- They support viewing records by employee, by date, and by date range.
- The initialization endpoint can prepare a monthly attendance structure for a company.

### Employee self-service flow
- Employees can retrieve their own calendar or grid view for a month.
- They can submit correction requests without needing broad attendance permissions.
- Their self-service endpoints derive the user identity from the JWT rather than accepting a client-supplied ID.

### Correction request lifecycle
- A correction request is created for a specific attendance date.
- Duplicate pending requests are blocked.
- An admin review updates the request state and sends notifications to the involved employee.

## Dependencies
- Authentication context for the current user and company.
- Notification infrastructure to inform employees and reviewers.
- Employee repository for role-based recipients.
- Attendance services and repository-backed queries.

## Core business rules
- Admin operations require higher attendance permissions.
- Self-service endpoints are scoped to the authenticated employee.
- Correction request flow is designed to prevent duplicate submissions and keep audit data available.

## Integration points with other modules
- Leave Management uses attendance data when evaluating balances or presence-related behavior.
- Payroll depends on attendance records for paid-time calculations and summaries.
- Calendar and holiday features can influence attendance visibility and expected workdays.
- Notice and notification flows are important because correction requests and attendance updates often need to notify the right reviewers.

## Request and response shape
- Admin attendance endpoints often accept a date or date-range payload and return either an object with items or a direct success/failure result.
- Self-service endpoints use the current authenticated employee identity and typically return a calendar or list of attendance items.
- Correction requests are modeled as workflow objects with status, resolution, and reviewer metadata.

## Failure modes and edge cases
- Invalid date ranges should return a bad-request response.
- Missing attendance records for a requested date should return a not-found or validation response.
- Duplicate correction requests should be prevented until the prior one is resolved.
- Month initialization should be checked carefully when the company has date-based work rules or holidays.

## Contributor checklist
- Preserve the distinction between admin and self-service routes.
- Avoid changing attendance semantics without checking payroll and leave impact.
- Keep correction request and notification behavior consistent when modifying review flows.

## Operational notes
- Monthly attendance initialization and range queries should be verified when adding seasonal or shift-based logic.
- Any attendance change should be reviewed for downstream payroll and leave side effects.
