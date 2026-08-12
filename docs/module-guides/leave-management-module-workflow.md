# Leave Management Module Workflow

## Scope
The leave-management module handles leave policies, employee balances, leave requests, approval workflows, and reporting summaries used by HR and employees.

## Architectural role
This module provides the policy-and-workflow layer for absence management. It sits at the intersection of employee records, attendance data, and reporting systems, making it one of the most important workflow modules in the platform.

## Main entry points
- Controller: [Codeji.CMS.API/Controllers/LeaveManagementController.cs](../Codeji.CMS.API/Controllers/LeaveManagementController.cs)
- Service: [Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs](../Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs)

## End-to-end flow
1. HR or admin creates or updates leave policies for the company.
2. Employees request leave through the request endpoints.
3. The service validates the request against policy and balance rules.
4. HR or admin updates the request status, which changes the leave workflow state.
5. Leave balances and summaries are generated for reporting and employee self-service views.

## Detailed behavior
### Policy management
- Leave policies define the type, accrual period, carry-over behavior, and validity of leave rules.
- Policies are validated before they are saved to avoid inconsistent accrual values.

### Request lifecycle
- An employee creates a leave request with dates, leave type, and related metadata.
- The controller resolves the current user and passes the request into the service layer.
- The service validates the request against the policy and balance rules.

### Approval flow
- Admin or HR changes the request status to approved, rejected, or withdrawn.
- The update may trigger notifications and update reporting state.
- Leave balances are recalculated or checked by the service to reflect the new state.

### Employee self-service
- Employees can view their own balances and requests through dedicated routes.
- These routes avoid requiring the employee to supply a foreign user ID for their own data.

## Dependencies
- Authentication context for current user and company.
- Employee identity data for the target employee.
- Notification infrastructure for approval and update events.
- Attendance and payroll modules, which may depend on leave state and balance information.

## Core business rules
- Leave requests must align with the active policy and current balance.
- Approval workflows are the main transition point between pending and finalized leave records.
- Self-service routes are separate from admin/HR routes to keep ownership clear.

## Integration points with other modules
- Attendance can affect presence-based or absence-based leave workflows.
- Payroll may rely on leave usage information for month-end reconciliation.
- Employee and company modules provide the employee identifiers and policy context.
- Notification and user-preference flows are often triggered when a request is created or reviewed.

## Request and response shape
- Policy endpoints typically accept a policy DTO and return a result wrapper.
- Request endpoints carry leave dates, type, and other business metadata.
- Summary endpoints expose aggregated leave data for reporting and dashboards.

## Failure modes and edge cases
- Invalid accrual amounts and carry-over settings should be rejected during policy creation.
- Requests for employees without a valid context should fail safely.
- Approval or update operations should preserve the current request state and avoid inconsistent transitions.

## Contributor checklist
- Keep validation logic aligned with policy rules, not only with DTO shape.
- Verify leave balances whenever changing request approval or status transitions.
- Make sure any new leave type or policy setting is reflected across create, update, query, and summary flows.

## Operational notes
- Any change to leave balance logic should be tested against creation, approval, and reporting flows.
- Leave policies need careful review when changing accrual behavior or carry-over settings.
