# Role and Permission Index

Last verified against code: 2026-09-02.

`Role`, `Job Title`, `Department`, and `Permission` are separate concepts. Roles and their grants are managed through `Codeji.CMS.API/Controllers/RolesController.cs`; job titles and departments are company-master data in `CompanyMasterController.cs`. `ModulePermissionAttribute` is the controller-level declarative guard; services must still enforce ownership and workflow conditions.

| Concept | Source | Meaning |
|---|---|---|
| Application role | `RolesController`, role entities/services | A named set of application access grants. |
| Permission | `Codeji.CMS.Utility/Constraints/ConstraintHelper.cs` and controller `ModulePermission` attributes | Operation access within an application module. |
| Job title / role | `CompanyMasterController` job-title endpoints | Employee organisational data; not an authorization grant by itself. |
| Department | `CompanyMasterController` department endpoints | Company organisational scope; may drive policy resolution. |
| Reporting manager | `EmpUser` relationship, service checks | Relationship-based authority for assigned requests/cases; not a broad role. |

Important permissions visible in current controller code include Attendance `View`/`Edit`, WorkFromHome `PolicyEdit`/`ViewTeam`/`ViewAll`/`ApproveTeam`, and Offboarding `PolicyManage`, `CaseManage`, `AssetManage`, `AccessManage`, `FinanceManage`, `BenefitsManage`, `EvidenceManage`, `PortalManage`, and `Download`. Exact grants are company-configured; do not infer a grant from a page being visible.

Source: `Codeji.CMS.API/Controllers/*.cs`, `Codeji.CMS.Utility/Constraints/ConstraintHelper.cs`, and `Codeji.CMS.API/Filters/ModulePermissionAttribute.cs`.
