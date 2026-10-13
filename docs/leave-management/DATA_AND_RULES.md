# Leave Management Data and Rules

Entities live under `Codeji.CMS.Repository/Entities/Leave`; policy request contracts include `Codeji.CMS.DTO/LeaveManagement/LeavePolicy/LeavePolicyRequest.cs`.

The service—not the React form—must remain responsible for policy validity, employee/company ownership, balances, request lifecycle and the attendance connection. The older detailed sources `../leave-management-current-flow.md`, `../employee-specific-leave-allocation.md`, and `../half-day-leave-wfh-segments.md` are retained as supporting implementation references.
## Lifecycle interaction

Last verified against code: 2026-09-07.

Approved leave changes availability/attendance expectations, not employment relationship. Maternity, paternity, medical, annual, long-term, and administrative leave must not create an offboarding case or former-employee classification. A notice-period employee may simultaneously have temporary leave, subject to the existing leave policy and last-working-date rules. Only the completed EmployeeExit workflow ends employment.
