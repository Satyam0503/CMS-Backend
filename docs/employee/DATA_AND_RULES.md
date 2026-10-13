# Employee Data and Rules

Employee entities are under `Codeji.CMS.Repository/Entities/Employees`, particularly `EmpUser.cs`. Common dependencies include company master data, role/permission data, attendance, leave, payroll and offboarding. Existing detailed references: `../employee-module-current-flow.md` and `../employee-profile-current-flow.md`.

> ⚠ Keep reporting-manager relationship semantics separate from role/permission grants: it is a scoped business relationship, not a generic module permission.
