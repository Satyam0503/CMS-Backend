# Attendance and Work From Home

Controller ownership: `AttendanceController`, `MyAttendanceController`, `AttendancePenaltyController`, `AttendanceStatusSettingsController`, `WeeklyOffSettingsController`, and `WorkFromHomeController`. Services are under `Codeji.CMS.Services/Attendance`; DTOs/entities are under `Codeji.CMS.DTO/Attendance` and `Codeji.CMS.Repository/Entities/Attendance`.

Lifecycle integration verified 2026-09-07: attendance initialization and mutation checks use date-aware employment and availability rules. Leave, sabbatical, suspension, and other temporary non-working states do not create normal attendance; notice-period employees remain current through the applicable last working date.

Current detailed references: `../attendance-module-current-flow-and-audit.md`, `../attendance-bulk-marking-flow.md`, `../wfh-end-to-end-flow.md`, and `../office-schedule-shifts.md`. WFH team/all routes carry explicit `ModulePermission` guards; attendance exception mutation in `WorkFromHomeController` is protected by Attendance edit permission.
