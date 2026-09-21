# Leave Management

`LeaveManagementController` delegates to leave-management services and uses DTOs under `Codeji.CMS.DTO/LeaveManagement`. It covers policy, balances, employee/self-service requests, approval/status operations and summaries. Read `../leave-management-current-flow.md` and `../employee-specific-leave-allocation.md` for verified implementation detail; cross-check the controller action before API testing because the module uses a mixture of legacy and `me/*` routes.

Lifecycle integration verified 2026-09-07: approved leave affects availability and attendance expectations only. It never creates an offboarding case or former-employee classification, including maternity, paternity, medical, long-term, annual, and administrative leave. Notice-period employees may carry temporary leave subject to existing policy and last-working-date rules.
