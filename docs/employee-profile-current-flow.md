# Employee Profile and Attendance Calendar: Current Flow

## Purpose and ownership

The employee profile is the authenticated workspace for a person's employment identity, personal information, skills, education, certifications, work history, leave history, notification preferences, and profile image. It is not the public company Career Profile; that separate module is documented in [profile-module-current-flow.md](profile-module-current-flow.md).

The profile attendance calendar is deliberately a **read-only** monthly view. It displays persisted daily attendance from the Attendance module and does not create or edit attendance, leave, WFH, or calendar records.

## Implementation map

| Concern | Backend / frontend location | Responsibility |
|---|---|---|
| Employee APIs | `Codeji.CMS.API/Controllers/UserController.cs` | Own-profile, employee-master, image, detailed-profile and notification endpoints |
| Employee domain logic | `Codeji.CMS.Services/Employees/EmployeeService.cs` | Tenant-scoped employee data and profile-detail persistence |
| Daily attendance read | `AttendanceController.cs`, `MyAttendanceController.cs` | Profile monthly calendar plus self attendance grid/correction request endpoints |
| Attendance data | `AttendanceService`, `AttendanceRepository`, `AttendanceModel` | Company/user/date attendance records and source provenance |
| Profile shell | `CMS-React/src/app/modules/users/UserProfile.tsx` | Provider, header, profile card, calendar and tabbed content |
| Profile calendar | `CMS-React/src/app/modules/users/components/LeaveCalendar.tsx` | Month navigation, status legend, tooltip and read-only rendering |
| Tabs | `CMS-React/src/app/modules/users/profileTabs.ts` | About, Leave Tracker, Performance, Work History and self-only Settings |

## Identity, scope, and access

`UserId` is the authenticated user identity. `EmployeeId` is the organisation-facing employee code. `CompanyId` is the mandatory tenant boundary.

Every profile read or update must resolve the company from the access token/current request context; a client does not select a tenant. Self-service endpoints derive `UserId` from the token. The Employees module permission controls management of other employees. A logged-in employee may edit the deliberately limited own-profile fields without being granted general Employees edit permission.

| Operation | Route / mechanism | Boundary |
|---|---|---|
| Read own profile | `GET api/user/GetEmployeeById` with no `userId` | Controller substitutes caller ID |
| Read selected employee | `GET api/user/GetEmployeeById?userId=...` | Service must resolve that employee in current company |
| Edit own personal fields | `POST api/user/EditOwnProfile` | Controller maps only allowed personal fields and ignores role/job/bank data |
| Edit another employee | `POST api/user/EditEmployees` | `Employees.Edit` required |
| Upload/remove own image | `UploadUserImage`, `RemoveProfileImage` | Caller ID only |
| Read other employee attendance | `POST api/admin/attendance/GetAllAttendanceItems` | `Attendance.View`, target IDs and date range supplied; server scopes by company |

## Profile composition

`UserProfile` opens inside `UserDetailsProvider`. The left column contains `ProfileCard` and `LeaveCalendar`; the main column contains header and tabs. The same component serves the current user and an employee-manager view:

```text
route /profile/*                 -> current authenticated employee
route /employees/{employeeid}/*  -> selected employee profile
          |                                  |
          +---- UserDetailsProvider ----------+
                         |
             ProfileCard + LeaveCalendar + tab outlet
```

The Leave Tracker tab is always available in the signed-in user's own profile, even without Leave Management permission. For a selected employee profile it remains visible only to a viewer with `LeaveManagement.View`. Settings is intentionally hidden when viewing another employee. Tab visibility improves the UI but must not be treated as API authorization.

## Profile Leave Tracker self-service

Profile Leave Tracker is separate from the Leave Management module. It is the employee's own leave workspace, backed by token-derived `me/*` endpoints.

| Action | API behavior | Data boundary |
|---|---|---|
| View balance/policies | `GET api/LeaveManagement/me/balances` | Current employee's assigned current-company balances only |
| Apply | `POST api/LeaveManagement/me/requests` | Server replaces any submitted user ID with caller ID |
| View history | `POST api/LeaveManagement/me/requests/search` | Current employee only |
| Edit/withdraw pending request | `PATCH` / `DELETE api/LeaveManagement/me/requests...` | Current-company pending request owned by caller |

Employees can apply for and monitor their own leave without Leave Management navigation. They cannot configure policies, allocate balances, access another employee's data, or approve/reject requests. Those remain HR/Admin Leave Management responsibilities.

## Attendance calendar in the profile

### Calendar-day handling

The profile calendar renders persisted attendance business dates. Leave and leave-generated attendance are stored at UTC midnight; the UI must not use a legacy timestamp's UTC prefix as the employee calendar day. A single-day leave-generated cell resolves its displayed day from the matching approved leave request, while the database migration repairs legacy rows. The calendar remains read-only and cannot repair or overwrite attendance itself.

### Data-load sequence

For each displayed month, `LeaveCalendar.tsx` does the following:

1. Calculates the first and last date of that month in the browser.
2. Loads active company status definitions from `GET api/attendance-status-settings?activeOnly=true`.
3. If the profile belongs to the current user, calls `GET api/admin/attendance/my-calendar?year=YYYY&month=MM`. The API validates year `2000..2100`, month `1..12`, derives both `CompanyId` and `UserId` from the authenticated request, and queries only that user.
4. If an authorised viewer opened another employee profile, calls `GetAllAttendanceItems` with the visible date range and that single target `UserId`. This endpoint is protected by `Attendance.View` and passes the current `CompanyId` to the service.
5. Builds an in-memory map keyed as `YYYY-MM-DD`, then renders only dates with a persisted attendance row.

No client-provided `CompanyId` is accepted in either path. No attendance is inferred from a leave badge or from a calendar holiday; the calendar reflects persisted `AttendanceModel` rows.

### Rendering and meaning

Each recorded day is rendered using the active status setting's `ColorHex`; a fallback palette is used only when the setting cannot be found. The status code is not replaced with a generic present/absent symbol. A tooltip contains code, configured status name, date, check-in time and, when present, check-out time. The legend is built from the distinct codes actually returned for the month.

The component uses `DateCalendar` in `readOnly` mode. Month navigation changes the query; selecting a day changes the displayed month but does not open an editor or write data.

| Persisted provenance | Profile-calendar result |
|---|---|
| Manual attendance | Shows the configured status and recorded times |
| `SourceType = LEAVE` | Shows the leave policy's mapped attendance status after approval reconciliation |
| `SourceType = WFH_REQUEST` | Shows WFH status/times generated by the WFH workflow |
| No row | Normal uncoloured date; this is not automatically an absence |

### Why it is separated from leave and calendar

The profile widget is a convenient view, not a source of truth. Leave approval writes a `LeaveRequest`, updates balance, and then creates source-owned attendance only through reconciliation. Company holidays and weekly offs influence whether dates are eligible, but they do not themselves create daily attendance. Payroll consumes locked attendance summaries, not the coloured profile UI.

## Profile mutation flows

### Self edit

`EditOwnProfile` accepts an `EmployeeSelfEditDto` and maps first/last name, gender, birth date, phone, blood group, personal email, emergency contact and address. It intentionally does not accept role, organisation job fields, salary/bank data, tenant identity, or another user ID. The service resolves and updates the employee document in the current company.

### HR employee edit

Employee administrators use `EditEmployees`, employee invitations and bulk import. These operations use Employees module permissions and may update employment-facing information. A caller must not gain this scope merely by editing their profile.

### Detail sections

Profile sections use dedicated entities/requests for summary, education, certification, skills and work history. The controller defaults a missing user ID to the current user for self-service use; any endpoint that accepts a target must still apply company ownership in the service. Profile image files are stored under `Uploads/ProfileImage`, while the employee document stores the generated filename/path reference.

## Notifications in profile

The profile notification area reads `UserNotifications` for the current `UserId`, joins the associated `Notifications` records, and supports mark-one/mark-all-read operations. Leave application sends HR/Admin notifications; leave approval/rejection sends the employee a notification. Notification delivery is company-scoped at creation time; a real-time SignalR event is an addition to, not a replacement for, the persisted notification row.

Notification preferences are read and saved through `GetNotificationPreferences` and `UpdateNotificationPreferences`. `LeaveStatusUpdate` controls approval/rejection delivery to the employee. Email is delivered only to verified email addresses and logged through `EmpEmailLogs`.

## Operational checks

- A self profile calendar request must never return another user's records even if the browser changes a query string.
- An HR profile calendar request must return only the selected employee inside the authenticated company.
- Changing an attendance status colour/name should update the next profile calendar load without changing historical attendance codes.
- A leave-generated or WFH-generated row must be corrected through its owning workflow, not through the profile calendar.
- A missing/disabled email verification means an in-app notification may exist while no email is sent; inspect `EmpEmailLogs` for delivery status/error.

## Related references

- [Attendance module](attendance-module-current-flow-and-audit.md)
- [Leave management](leave-management-current-flow.md)
- [Calendar module](calendar-module-current-flow.md)
- [Career Profile module](profile-module-current-flow.md)
