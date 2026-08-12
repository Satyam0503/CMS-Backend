# Company and Master-Data APIs

## Controllers
- Source: [Codeji.CMS.API/Controllers/CompanyController.cs](../Codeji.CMS.API/Controllers/CompanyController.cs)
- Source: [Codeji.CMS.API/Controllers/CompanyMasterController.cs](../Codeji.CMS.API/Controllers/CompanyMasterController.cs)

## Overview
These controllers cover company configuration, departments, job titles, custom attributes, and leave/policy management that belongs to the tenant/company context.

## Endpoint map

### Company master data
- GET /api/companymaster/OfficeSchedule
- PUT /api/companymaster/OfficeSchedule
  - Purpose: read and save office schedule configuration.
  - Permission: AdminOnly.

- POST /api/companymaster/UpdateDepartment
- GET /api/companymaster/GetDepartmentList
- DELETE /api/companymaster/DeleteDepartment/{departmentId}
  - Purpose: manage company departments.

- PATCH /api/companymaster/UpdateModuleAccess/{moduleId}
  - Purpose: updates module access state for a module.

- GET /api/companymaster/GetAllModuleDetails
  - Purpose: returns module metadata for the current company.

- POST /api/companymaster/AddUpdateJobTitle
- GET /api/companymaster/GetJobTitles
- DELETE /api/companymaster/DeleteJobTitle/{jobTitleId}
  - Purpose: manage job titles.

- POST /api/companymaster/CreateCustomAttribute
- GET /api/companymaster/GetAllCustomAttribute
- GET /api/companymaster/GetCustomAttributeById/{customAttributeId}
- PUT /api/companymaster/UpdateCustomAttribute
- DELETE /api/companymaster/DeleteCustomAttributeValue/{customAttributeValueId}
  - Purpose: manage custom employee/company attributes.

### Company policy and settings
- POST /api/company/CreatePolicy
- PUT /api/company/UpdatePolicy
- GET /api/company/GetAllPolicies
- DELETE /api/company/DeletePolicy/{policyId}
- POST /api/company/CreatePolicyVersion
- PUT /api/company/EditPolicyVersion
- GET /api/company/GetAllPolicyVersion/{policyId}
- DELETE /api/company/DeletePolicyVersion/{policyVersionId}
  - Purpose: manage company policy versions and lifecycle.

- POST /api/company/GetAllCompanyList
- GET /api/company/GetCompanyDetails
- GET /api/company/career/{publicCompanyCode}
- POST /api/company/UpdateCompanyDetails
  - Purpose: read and update company profile and listing details.

## Service dependencies
- ICompanyService
- ICompanyMasterService
- IOfficeScheduleSettingsService

## Implementation notes
- Company registration and initial tenant seeding run through the company services, not directly from these controllers.
- Master-data endpoints are heavily admin-gated and rely on the current company ID from the auth context.
