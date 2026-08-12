# Employee and Profile APIs

## Controller
- Source: [Codeji.CMS.API/Controllers/UserController.cs](../Codeji.CMS.API/Controllers/UserController.cs)

## Overview
This controller manages the employee domain: invitations, employee CRUD, profile updates, education/certification details, skills, notifications, work history, and self-service profile edits.

## Endpoint map

### Employee lifecycle
- POST /api/user/InviteNewEmployee
  - Purpose: invites a new employee and sends the welcome email.
  - Permission: Employees.Create.

- POST /api/user/BulkImportEmployees
  - Purpose: imports employees in bulk from a DTO payload.
  - Permission: Employees.Create.

- GET /api/user/GetNextEmployeeId
  - Purpose: returns the next employee identifier for the current company.
  - Permission: Employees.View.

### Employee management
- POST /api/user/EditEmployees
  - Purpose: edits another employee’s profile.
  - Permission: Employees.Edit.
  - Notes: the controller prevents self-elevation by routing self-edit requests through a restricted path.

- POST /api/user/EditOwnProfile
  - Purpose: edits the currently authenticated user’s profile.
  - Auth: any authenticated user.

- POST /api/user/GetAllEmployees
  - Purpose: retrieves a paginated and filterable list of employees.
  - Permission: Employees.View.

- DELETE /api/user/DeleteEmployee
  - Purpose: removes an employee record.
  - Permission: Employees.Delete.

### Profile details and self-service
- GET /api/user/GetEmployeeById
  - Purpose: gets a single employee by ID, or the current user if no ID is supplied.

- GET /api/user/Search
  - Purpose: searches employees by name.

- POST /api/user/AddEditEmployeeSummary
- POST /api/user/AddEmployeeEducation
- POST /api/user/EditEmployeeEducation
- POST /api/user/AddEmployeeCertification
- POST /api/user/EditEmployeeCertification
- GET /api/user/GetEmployeeSummary
- GET /api/user/GetEmployeeEducationDetails
- GET /api/user/GetEmployeeCertificationDetails
  - Purpose: manage employee summary, education, and certification records.

- POST /api/user/UploadUserImage
  - Purpose: uploads and stores a profile picture.

- POST /api/user/AddEditEmpSkills
- GET /api/user/GetEmployeeSkills
  - Purpose: manage employee skills.

- DELETE /api/user/DeleteEducationDetails/{educationId}
- DELETE /api/user/DeleteCertificationDetails/{certificationId}
  - Purpose: remove employee education/certification entries.

### Work history and notifications
- POST /api/user/AddUpdateWorkHistory
- GET /api/user/GetEmpWorkHistory
- DELETE /api/user/DeleteWorkHistory/{workId}
  - Purpose: manage the employee’s work history.

- GET /api/user/GetAllNotifications
- PATCH /api/user/Notification/{userNotificationId}/MarkAsRead
- POST /api/user/MarkAllNotificationAsRead
  - Purpose: read and manage user notifications.

- GET /api/user/GetNotificationPreferences
- POST /api/user/UpdateNotificationPreferences
  - Purpose: expose preference-based notification settings.

### Miscellaneous helpers
- POST /api/user/ChangePassword
  - Purpose: changes the current user’s password.

- GET /api/user/GetSuggestedSkills
- POST /api/user/AddSkill
  - Purpose: suggestions and creation of skills.

- POST /api/user/ResendInvite/{userId}
  - Purpose: resends an employee invitation.

## Service dependencies
- IEmployeeService
- IAccountServices

## Implementation notes
- The controller uses the current authentication context heavily to avoid trusting client-supplied user IDs for self-service requests.
- Employee self-edit is a deliberate exception to the normal module-permission pattern.
