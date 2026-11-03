# Leave Management Data and Rules

Entities live under `Codeji.CMS.Repository/Entities/Leave`; policy request contracts include `Codeji.CMS.DTO/LeaveManagement/LeavePolicy/LeavePolicyRequest.cs`.

The service—not the React form—must remain responsible for policy validity, employee/company ownership, balances, request lifecycle and the attendance connection. The older detailed sources `../leave-management-current-flow.md`, `../employee-specific-leave-allocation.md`, and `../half-day-leave-wfh-segments.md` are retained as supporting implementation references.
## Lifecycle interaction

Last verified against code: 2026-09-07.

Approved leave changes availability/attendance expectations, not employment relationship. Maternity, paternity, medical, annual, long-term, and administrative leave must not create an offboarding case or former-employee classification. A notice-period employee may simultaneously have temporary leave, subject to the existing leave policy and last-working-date rules. Only the completed EmployeeExit workflow ends employment.

## Module Enablement and Fail-Closed Boundary

1. **Tenant Module Access Validation**:
   - Every Leave Management endpoint — including self-service endpoints (`me/balances`, `me/requests`, `me/policies`, `GetMyLeaveRequests`, `DeleteLeaveRequest`) — enforces `IsModuleAccessibleForCompany(AppModule.LeaveManagement, companyId)`.
   - If the company has uninstalled or disabled the Leave Management module, all endpoints reject requests fail-closed with `MODULE_NOT_ACCESSIBLE` or HTTP 403 Forbidden.
   - Profile self-service cannot bypass tenant-level module disablement.

2. **Decoupling from Payroll**:
   - Leave Management is completely decoupled from Payroll.
   - Disabling Payroll only disables Payroll and Payroll Settings; it does **not** alter Leave Management policies, balances, or request workflows.
   - Tenants can run Leave Management and Attendance tracking as standalone workforce management capabilities without activating Payroll.
