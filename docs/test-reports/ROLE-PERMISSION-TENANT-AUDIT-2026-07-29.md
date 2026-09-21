# Role, permission, authentication, registration and tenant-isolation audit

> Source audit performed 29 July 2026. Scope: account registration/login, JWT context, module permissions, role grants and tenant boundaries. No database records, roles or permissions were changed. Findings marked **confirmed** are directly evidenced in source; risks marked **runtime check required** need controlled integration testing and/or index inspection.

## Confirmed findings

### RP-01 — Default role-permission grants are not awaited during registration

**Severity: High**

`RoleServices.AddDefaultRole` assigns `_rolePermissionRepository.AddOne(...)` to a local `Task<Result>` but does not await or collect it. It then saves the roles and returns. `CompanyService.Register` calls this method before it assigns the new administrator role to the first user.

**Impact:** a newly registered company can receive its roles while some or all role-permission rows have not completed or have failed silently. The first administrator may be able to authenticate but receive incomplete module permissions and repeated 403 responses.

**Evidence:** `Codeji.CMS.Services/Employees/RoleServices.cs`, `AddDefaultRole`, permission-copy loop; `Codeji.CMS.Services/Companies/CompanyService.cs`, `Register`.

**Fix:** build a list of the permission insert tasks and `await Task.WhenAll(...)`; fail registration or compensate before creating the tenant user if any grant cannot be persisted. Add a registration integration test asserting every default role has the expected grants.

### RP-02 — Registration is a multi-write workflow without transactional or compensating protection

**Severity: High**

Registration creates default roles, adds a company, adds the admin user, adds notification preferences, creates an email-verification token, queues mail and seeds company master data in separate operations. Failures after the company write are not rolled back or marked for repair.

**Impact:** a transient MongoDB or mail/seed failure can leave an incomplete tenant: company without a usable administrator, missing default data, or role/grant inconsistencies. Retrying registration can then fail due to the partial state.

**Evidence:** `CompanyService.Register` executes these writes sequentially and returns after the company write path; no session/transaction or compensation is present.

**Fix:** use a MongoDB transaction where the deployment supports it, or persist a tenant-provisioning state plus idempotent retry/compensation. Mail must be queued only after the durable tenant core is complete.

### RP-03 — Role-permission lookup omits `CompanyId`

**Severity: Medium**

`GetRolePermissionOfuser(roleId)` gets role permissions with `RoleId`, `IsAccessible` and `HasAccess` only. The same method is used by `VerifyUserAccess` after it has confirmed that the role belongs to the request company.

**Impact:** role IDs are currently GUIDs, so accidental cross-tenant collision is unlikely, but the authorization read does not enforce the same tenant predicate as the role lookup. A corrupted/misassigned `RolePermission` row can affect a role outside its own company.

**Evidence:** `Codeji.CMS.Services/Employees/RoleServices.cs`, `GetRolePermissionOfuser` and `VerifyUserAccess`.

**Fix:** change the method signature to accept `companyId` and filter `RolePermission` by both `RoleId` and `CompanyId`; add a two-tenant regression test with intentionally conflicting role-permission documents.

### RP-04 — Custom-attribute update audit actor is recorded as the company ID

**Severity: Medium (audit integrity)**

`CompanyMasterController.UpdateCustomAttribute` assigns `userId` using `CurrentContext.CompanyId(...)`, then passes it to the service as the acting user.

**Impact:** audit metadata cannot identify the person who changed a custom attribute. It will incorrectly contain the tenant ID.

**Evidence:** `Codeji.CMS.API/Controllers/CompanyMasterController.cs`, `UpdateCustomAttribute`.

**Fix:** use `CurrentContext.UserId(_httpContextAccessor)` and add a controller/service test for audit values.

## Positive controls confirmed

- Login retrieves an active user, rejects unverified email, checks application access and verifies the BCrypt password before it issues a JWT (`AccountServices.VerifyAndGenerateToken`).
- The JWT carries company/user/role claims; `CurrentContext.CompanyId` accepts only one authenticated `company_id` claim and no longer trusts a caller-controlled tenant header.
- The middleware verifies active company and active user before it evaluates endpoint module permission metadata (`AuthenticateUserRequest`).
- The middleware resolves endpoint metadata and asks `VerifyUserAccess` for the request user/company; administrator bypass is explicit.
- The employee self-profile endpoints are deliberately ownership-based, while employee-management endpoints use `Employees` module permissions. WFH employee endpoints use service ownership checks for the same reason.

## Runtime/index checks required

These are not reported as confirmed bugs because source inspection alone cannot establish them.

1. **Registration email uniqueness:** registration performs an `IsEmailExist` check before insert. Inspect MongoDB unique indexes and run two parallel registrations in an isolated Testing database to confirm duplicates cannot be created by a race.
2. **Role-permission resolver semantics:** test a new tenant immediately after registration, including an admin login and one endpoint for every expected default module permission.
3. **Tenant isolation:** run two-test-tenant cases against each high-risk route that accepts a resource/user ID, verifying that a valid user from tenant A cannot read, edit or delete tenant B data.
4. **Anonymous allow-list matching:** `AuthenticateUserRequest` uses substring matching for anonymous paths. Replace with route-aware exact/prefix matching and test look-alike routes before treating this as a confirmed exploit.
5. **Refresh-token isolation/revocation:** verify refresh, logout, password-reset and email-verification concurrency using an isolated test database; these flows depend on persisted tokens and cannot be safely proven from static review alone.

## Recommended order

1. Fix RP-01 and RP-02 before onboarding more tenants.
2. Fix RP-03 and add cross-tenant authorization tests.
3. Fix RP-04 in the next small audit-integrity change.
4. Add an isolated end-to-end registration/login/permission/tenant test suite; do not run it against the current business database.
