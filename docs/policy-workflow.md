## Policy Feature - Workflow and Implementation Details

### Purpose
This document describes the current end-to-end implementation of the Policy feature: data models, backend endpoints and rules, frontend flows, common error cases, and recommended improvements.

### Data model
- `Policy` (Repository Entities/Company/Policy.cs)
  - Fields: `PolicyId`, `PolicyName`, `Description`, `Departments`, `Roles`, `IsActive`, `IsDeleted`
  - A company-level record that groups policy versions and controls visibility/scope.
- `PolicyVersion` (Repository Entities/Company/PolicyVersion.cs)
  - Fields: `Id`, `PolicyId`, `DocUrl`, `VersionName`, `IsCurrent`, `IsDeleted`
  - `IsCurrent` indicates which version should be visible to non-editor users. A policy can intentionally have no current version; in that state regular users do not see a published version.

### Backend endpoints and behavior
Controller: `CompanyController` delegates to `CompanyService`.

- `POST api/Company/AddPolicy` - create a new policy (basic metadata).
- `POST api/Company/CreatePolicyVersion` - upload a document and create a version.
  - If `IsCurrent = true` in request, server clears `IsCurrent` on other versions for that policy.
  - Stores uploaded file under `Uploads/Policy` and returns the created version info.
- `POST api/Company/EditPolicyVersion` - update version metadata / document.
  - If request sets `IsCurrent = true`, service clears `IsCurrent` on other versions.
  - If request sets `IsCurrent = false`, the policy can be left with no current/published version.
- `POST api/Company/SetCurrentPolicyVersion` - makes the selected version current.
  - Clears current status from other versions first, then marks the selected version current.
- `DELETE api/Company/DeletePolicyVersion/{id}` - soft-delete a version.
  - Current versions can be deleted. If no current version remains, regular users do not see a published version.
- `DELETE api/Company/DeletePolicy/{policyId}` - soft-delete policy.
  - On success: mark policy deleted and set all versions `IsDeleted=true` and `IsCurrent=false`.
- `GET api/Company/GetAllPolicyVersion/{policyId}` - returns versions.
  - Admin/editor: returns all versions.
  - Regular user: returns only current version.
- `GET api/Company/GetPolicyDocument/{versionId}` - returns file bytes.
  - Admin/editor: can fetch any version.
  - Regular user: only allowed to fetch if the requested version is `IsCurrent`.

See implementation: `Codeji.CMS.Services/Companies/CompanyService.cs` (methods: `AddPolicyVersion`, `EditPolicyVersion`, `SetCurrentPolicyVersion`, `GetAllPolicyVersion`, `DeletePolicyVersion`, `DeletePolicy`).

### Frontend flow and key files
- API client: `src/app/modules/policies/policyServices.ts` - wrappers for create/edit/delete/list/download/current.
- Versions list: `src/app/modules/policies/components/PolicyVersions.tsx` - loads versions and renders cards; offers Edit, Make Current, Make Inactive, and Delete actions for permitted users.
- Add/Edit form: `src/app/modules/policies/components/PolilcyVersionForm.tsx`
  - Builds `FormData` and calls `CreatePolicyVersion` / `EditPolicyVersion`.
  - Editors can set or unset `isCurrent`.

### Current / inactive behavior
- Editors can make a previous version current.
- Editors can make the current version inactive, leaving the policy without a published version.
- Editors can delete policy versions, including the current version.
- Editors can delete a policy even when it has a current version; the policy and its versions are soft-deleted together.
- Regular users only see active policies with a current, non-deleted version.

### Common error codes/messages
- `PolicyVersionNotFound` / `PolicyNotFound` - version or parent policy does not exist.

### Recommendations
1. Improve user messaging: map custom status codes to user-friendly toasts/dialogs on the frontend.
2. Optional behavior change: implement automatic promotion if the current version is deleted/inactivated and the product should always publish a replacement version.

### Quick dev pointers
- Files to edit for current/inactive actions: `src/app/modules/policies/components/PolicyVersions.tsx` and `Codeji.CMS.Services/Companies/CompanyService.cs`.

---
Generated on: 2026-08-12
