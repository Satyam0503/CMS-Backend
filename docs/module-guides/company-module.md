# Company and Master-Data Module Guide

## Purpose
The company module manages tenant-level company settings, departments, job titles, custom attributes, office schedule settings, and policy versions.

## Core flow
1. A company is created during registration and receives default seeds for departments and job titles.
2. Admins maintain master data through company master endpoints.
3. Company policies and versions are stored and surfaced for HR and compliance workflows.

## Key components
- Controllers: [Codeji.CMS.API/Controllers/CompanyController.cs](../Codeji.CMS.API/Controllers/CompanyController.cs), [Codeji.CMS.API/Controllers/CompanyMasterController.cs](../Codeji.CMS.API/Controllers/CompanyMasterController.cs)
- Service: [Codeji.CMS.Services/Companies/CompanyService.cs](../Codeji.CMS.Services/Companies/CompanyService.cs)

## Main APIs
- GET /api/companymaster/OfficeSchedule
- PUT /api/companymaster/OfficeSchedule
- POST /api/companymaster/UpdateDepartment
- GET /api/companymaster/GetDepartmentList
- DELETE /api/companymaster/DeleteDepartment/{departmentId}
- POST /api/companymaster/AddUpdateJobTitle
- GET /api/companymaster/GetJobTitles
- POST /api/companymaster/CreateCustomAttribute
- GET /api/companymaster/GetAllCustomAttribute
- PUT /api/companymaster/UpdateCustomAttribute
- POST /api/company/CreatePolicy
- PUT /api/company/UpdatePolicy
- GET /api/company/GetAllPolicies
- DELETE /api/company/DeletePolicy/{policyId}

## Notes for contributors
- This module is central to multi-tenant setup and should be kept consistent with company-scoped roles and permissions.
- Any master-data change can affect employee onboarding, leave, and reporting behavior.
