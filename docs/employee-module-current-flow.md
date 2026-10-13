# Employee Module Workflow

## Scope
The employee module governs the lifecycle of people inside a company: onboarding, profile updates, summary details, education, certifications, skills, work history, notifications, and employee-related reporting.

## Architectural role
This module is the primary place where the human identity layer intersects with the company and permission structure. It is responsible for keeping employee records accurate while respecting who is allowed to edit which fields.

## Main entry points
- Controller: [Codeji.CMS.API/Controllers/UserController.cs](../Codeji.CMS.API/Controllers/UserController.cs)
- Service: [Codeji.CMS.Services/Employees/EmployeeService.cs](../Codeji.CMS.Services/Employees/EmployeeService.cs)

## End-to-end flow
1. HR or admin creates or invites an employee through the invitation or bulk import APIs.
2. The request is validated, the current user is resolved from the auth context, and the service creates the employee record.
3. Additional profile sections such as summary, education, certifications, skills, and work history are added through separate endpoints.
4. Employees can also update their own profile through the self-service path without gaining admin-level privileges.
5. Notification and preference APIs complete the employee experience around profile activity and communication.

## Detailed behavior
### Invitation and onboarding
- The invite endpoint creates a new employee invitation and triggers welcome-email behavior.
- Bulk import accepts a list of employees and performs a batch onboarding flow.
- New employees become part of the current company’s tenant data and inherit the company’s configuration.

### Profile data management
- Summary endpoints store core data like biography and profile details.
- Education and certification endpoints manage linked records for the employee.
- Skills endpoints manage normalized skill data and suggestions.
- Work history captures prior roles and employment context.

### Self-service experience
- The self-edit path allows an employee to change their own basic profile details.
- These updates are intentionally routed to a restricted mapping to avoid accidental privilege escalation.

## Dependencies
- Authentication context for the current user and company.
- Account service for password and invite-related flows.
- Company module data such as departments, job titles, and custom attributes.
- Notification infrastructure for invitations and other employee events.

## Core business rules
- Admin and HR actions are protected by employee-module permissions.
- Self-edit operations are limited to the caller’s own identity.
- Employee records are scoped to the active company to keep data isolated by tenant.

## Integration points with other modules
- Attendance and leave modules often reference employee identities.
- Company master data supplies departments, job titles, and custom attributes used by employee records.
- Payroll and HR reporting consume employee data and profile-related context.
- Notifications and preferences created here can be consumed by other modules when they need to alert employees or reviewers.

## Request and response shape
- Employee create/edit endpoints commonly accept a strongly typed request model and return a result wrapper with either a success flag or a method result payload.
- Profile sub-entities like education and certification are usually stored as separate records linked to the employee identity.
- Notification reads and writes are handled through result-based payloads rather than direct exceptions.

## Failure modes and edge cases
- Editing another employee requires the right module permission; self-editing is handled via a separate restricted path.
- Missing or malformed employee IDs should return a failed result rather than causing partial writes.
- Bulk import operations should be validated for size and data quality to avoid inconsistent employee state.

## Contributor checklist
- Keep role-sensitive changes behind the permission-rich admin path unless the operation is explicitly self-service.
- When adding employee properties, consider whether they need to be exposed in the profile summary, self-edit flow, or admin edit flow.
- Update notifications and preferences consistently when changing employee communication behavior.

## Operational notes
- Employee profile data is fragmented across multiple sub-entities, so new profile features should be added carefully and consistently.
- Notification and preferences should be updated together when changing communication behavior.
