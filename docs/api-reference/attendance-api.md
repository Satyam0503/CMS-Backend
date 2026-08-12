# Attendance APIs

## Controllers
- Source: [Codeji.CMS.API/Controllers/AttendanceController.cs](../Codeji.CMS.API/Controllers/AttendanceController.cs)
- Source: [Codeji.CMS.API/Controllers/MyAttendanceController.cs](../Codeji.CMS.API/Controllers/MyAttendanceController.cs)

## Overview
These endpoints cover attendance entry, review, correction requests, monthly calendar views, and employee self-service attendance access.

## Endpoint map

### Admin attendance management
- POST /api/admin/attendance
  - Purpose: add manual attendance for an employee.
  - Permission: Attendance.CreateForEmployee.

- GET /api/admin/attendance/{userId}
  - Purpose: get attendance records by employee.
  - Permission: Attendance.ViewAll.

- GET /api/admin/attendance/{userId}/date
  - Purpose: fetch attendance for one employee on one date.

- PUT /api/admin/attendance/{userId}
  - Purpose: update attendance for a given employee/date.
  - Permission: Attendance.Override.

- POST /api/admin/attendance/GetAllAttendanceItems
  - Purpose: query attendance records for a date range and optional employee list.

- POST /api/admin/attendance/initialize-month
  - Purpose: initialize attendance month data.

- GET /api/admin/attendance/correction-requests
- PUT /api/admin/attendance/correction-requests/{requestId}
  - Purpose: review employee attendance corrections.

- GET /api/admin/attendance/my-calendar
  - Purpose: employee-facing monthly calendar under the admin route for backward compatibility.

### Employee self-service attendance
- GET /api/attendance/me/grid
- GET /api/attendance/me/calendar
  - Purpose: get the current employee’s monthly attendance grid.

- POST /api/attendance/me/correction-requests
  - Purpose: submit a correction request for one attendance record.

- GET /api/attendance/me/correction-requests
  - Purpose: list the current employee’s pending/completed correction requests.

## Service dependencies
- IAdminAttendanceService
- IAttendanceInitializationService
- IMongoDbRepository<AttendanceCorrectionRequest>

## Implementation notes
- The admin and self-service endpoints are intentionally split so employees can access their own attendance without needing broad attendance permissions.
- Attendance correction requests flow through repository-backed records and notifications.
