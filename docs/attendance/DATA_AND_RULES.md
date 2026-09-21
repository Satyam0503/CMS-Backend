# Attendance and WFH Data and Rules

Primary persistence sources are `Codeji.CMS.Repository/Entities/Attendance`, `WorkFromHomeRequest.cs`, and related attendance payroll exception records. Service sources are `Codeji.CMS.Services/Attendance`.

| Concern | Source-backed rule |
|---|---|
| Tenant boundary | Service operations resolve/filter current company; IDs alone are not sufficient authority. |
| Derived attendance | Leave/WFH reconciliation and initialization services create/adjust derived outcomes. |
| Source ownership | WFH/leave source-owned attendance needs its specific correction path. |
| Payroll impact | Attendance payroll rules are evaluated by backend services, not calculated from a UI total. |
| Schedules | Office schedule and department assignment are resolved server-side. |

> ⚠ Not conclusively determined from current code in this pass: the complete collection index catalogue. Inspect repository/migration definitions before an index or retention change.
## Lifecycle-aware attendance

Last verified against code: 2026-09-07.

Attendance eligibility is date-aware and no longer derives only from `EmpUser.Status`. The resolver considers joining date, last working date, approved leave, holidays, weekoffs, schedule, and lifecycle availability. Working and unauthorized absence remain attendance-expected; sabbatical, maternity/paternity/medical/long leave, administrative leave, suspension, and other temporary non-working states do not create normal attendance rows. Notice-period employees remain eligible through their applicable last working date.
