# Employee Module Guide

## Purpose
The employee module manages the company workforce: employee records, profile information, invitations, skills, notifications, and employee-specific self-service actions.

## Core flow
1. HR or an admin creates or edits employee records through the employee controller.
2. Employee metadata is stored in the employee repository and enriched with profile sections such as summary, skills, certificates, and work history.
3. Self-service endpoints allow employees to update their own profile or read notifications without bypassing the tenant boundary.

## Key components
- Controller: [Codeji.CMS.API/Controllers/UserController.cs](../Codeji.CMS.API/Controllers/UserController.cs)
- Service: [Codeji.CMS.Services/Employees/EmployeeService.cs](../Codeji.CMS.Services/Employees/EmployeeService.cs)
- Supporting entities: employee, summary, education, certification, skills, work history

## Main APIs
- POST /api/user/InviteNewEmployee
- POST /api/user/BulkImportEmployees
- POST /api/user/EditEmployees
- POST /api/user/EditOwnProfile
- POST /api/user/GetAllEmployees
- GET /api/user/GetEmployeeById
- POST /api/user/AddEditEmployeeSummary
- POST /api/user/AddEmployeeEducation
- POST /api/user/EditEmployeeEducation
- POST /api/user/AddEmployeeCertification
- POST /api/user/EditEmployeeCertification
- GET /api/user/GetEmployeeSummary
- GET /api/user/GetEmployeeEducationDetails
- GET /api/user/GetEmployeeCertificationDetails
- POST /api/user/AddEditEmpSkills
- GET /api/user/GetEmployeeSkills
- POST /api/user/AddUpdateWorkHistory
- GET /api/user/GetEmpWorkHistory
- GET /api/user/GetAllNotifications

## Data ownership and security
- The current employee’s own profile can be edited through a dedicated self-service path.
- Admin or HR actions use module permissions and are scoped to the current company.
- The service layer is responsible for enforcing ownership and preventing privilege escalation via the client-supplied user ID.

## Notes for contributors
- Employee profile details are split across several sub-entities, so follow the service layer carefully when adding new profile fields.
- Notification and preferences flows are part of the same employee experience and should be updated together when changing behavior.
