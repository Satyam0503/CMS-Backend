# Company Module Workflow

## Scope
The company module manages tenant-level configuration, departments, job titles, module access, policy versions, and company-specific master data that all other modules rely on.

## Architectural role
This module defines the tenant boundary and the shared organizational structure for the product. Most downstream modules depend on company-scoped data, so this module is central to both configuration and data isolation.

## Main entry points
- Controllers: [Codeji.CMS.API/Controllers/CompanyController.cs](../Codeji.CMS.API/Controllers/CompanyController.cs), [Codeji.CMS.API/Controllers/CompanyMasterController.cs](../Codeji.CMS.API/Controllers/CompanyMasterController.cs)
- Service: [Codeji.CMS.Services/Companies/CompanyService.cs](../Codeji.CMS.Services/Companies/CompanyService.cs)

## End-to-end flow
1. A new company is created during registration or through the admin company management flows.
2. Default data such as departments, job titles, and module access settings are seeded.
3. Admins manage business lookup data through master-data endpoints.
4. Company policies and versions are maintained for HR, compliance, and operational workflows.
5. Other modules use this company context to scope employees, roles, attendance, and leave data.

## Detailed behavior
### Tenant creation
- The registration flow creates a company and an initial admin user.
- Default roles and seed data are created for the new tenant.
- The tenant becomes the boundary for future module data.

### Department and job-title management
- Admins can create, update, and delete departments and job titles.
- These values are used throughout employee onboarding and organization setup.

### Custom attribute management
- Custom attributes allow companies to extend employee or company profile data.
- They are used to support organization-specific fields beyond the standard schema.

### Policy and versioning flow
- Company policies can be created, edited, and versioned.
- Policy versions allow change history and review without deleting the prior policy state.

## Dependencies
- Authentication context for company and admin identity.
- Employee module for onboarding and employee organization data.
- Role/permission module for module-access and permission states.
- Notification and other modules may consume company-level settings.

## Core business rules
- Company data is tenant-scoped and must never bleed across companies.
- Master-data edits are admin-level operations.
- Policies and versioning should preserve history while allowing updates to the current active version.

## Integration points with other modules
- Employee onboarding depends on departments and job titles.
- Attendance, leave, and payroll all depend on company-specific configuration and access rules.
- Role and permission data relies on the company module’s module metadata.
- Public-career and recruitment flows also depend on company profile and publishing metadata.

## Request and response shape
- Master-data endpoints typically accept list-based DTO payloads and return result wrappers with success values and the affected data.
- Policy endpoints track versions and are designed to preserve a history of policy changes.
- Module-access endpoints often return lightweight metadata rather than full company objects.

## Failure modes and edge cases
- Invalid or missing company identifiers should be rejected early.
- Department or job-title deletes should be handled carefully if records are still referenced by employees.
- Policy updates should preserve previous versions and avoid breaking active workflows.

## Contributor checklist
- Treat all company changes as tenant-scoped operations.
- Check employee, role, and policy impact whenever changing master-data behavior.
- Keep seeding logic idempotent so new companies can be created safely.

## Operational notes
- New company-level fields or workflows should be designed with tenant isolation in mind.
- Seed data changes can influence all new tenants, so they should be treated as platform-level changes.
