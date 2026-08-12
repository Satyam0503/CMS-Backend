# Attendance Module Guide

## Purpose
The attendance module handles marking and reviewing daily attendance, correction requests, monthly calendars, and the business data that feeds payroll and leave reconciliation.

## Core flow
1. Employees or admins create or update attendance records for a given day.
2. Attendance is read through monthly or range-based queries for UI grids and reporting.
3. Employees can request corrections; admins can review those requests and update the underlying attendance record.
4. Monthly initialization and attendance summaries support payroll and reporting workloads.

## Key components
- Controller: [Codeji.CMS.API/Controllers/AttendanceController.cs](../Codeji.CMS.API/Controllers/AttendanceController.cs)
- Self-service controller: [Codeji.CMS.API/Controllers/MyAttendanceController.cs](../Codeji.CMS.API/Controllers/MyAttendanceController.cs)
- Service: [Codeji.CMS.Services/Attendance/AttendanceService.cs](../Codeji.CMS.Services/Attendance/AttendanceService.cs)

## Main APIs
- POST /api/admin/attendance
- GET /api/admin/attendance/{userId}
- GET /api/admin/attendance/{userId}/date
- PUT /api/admin/attendance/{userId}
- POST /api/admin/attendance/GetAllAttendanceItems
- POST /api/admin/attendance/initialize-month
- GET /api/admin/attendance/correction-requests
- PUT /api/admin/attendance/correction-requests/{requestId}
- GET /api/admin/attendance/my-calendar
- GET /api/attendance/me/grid
- GET /api/attendance/me/calendar
- POST /api/attendance/me/correction-requests
- GET /api/attendance/me/correction-requests

## Notes for contributors
- The module uses separate admin and self-service routes to keep employee access scoped correctly.
- Correction requests create notifications and should be considered part of the broader workflow, not just a simple CRUD operation.
