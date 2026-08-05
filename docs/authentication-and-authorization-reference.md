# Authentication, authorization, roles and tenant isolation

> Code-aligned reference for the ASP.NET Core API and its React client. It explains how a person becomes an authenticated user, how the request is bound to one company, and how each module is authorized. It is an implementation guide, not a promise of permissions not present in the database.

## 1. Security model at a glance

```text
Registration / invitation
  -> EmpUser + company-scoped Roles + RolePermission grants
  -> email verification
  -> login validates account, role, access and password
  -> JWT contains user_id, company_id and role_id
  -> middleware validates token, active user/company and module permission
  -> controller/service applies tenant and ownership rules
```

The system has three complementary controls:

1. **Authentication**: a valid JWT identifies the user.
2. **Authorization**: endpoint metadata asks for a module and permission; administrator roles bypass ordinary module checks.
3. **Data ownership and tenancy**: services must use the `CompanyId` and `UserId` derived from the JWT, never a browser-supplied tenant ID.

## 2. Principal, claims and tenant context

`AuthenticationHandler.GenerateJwtToken` creates the JWT after login or refresh. The current context is read through `CurrentContext`:

| Context | JWT claim | Meaning |
|---|---|---|
| User | `user_id` | authenticated employee/user identity |
| Tenant | `company_id` | company that owns the request data |
| Role | `role_id` | company-scoped role assigned to the employee |
| Administrative user | `admin_id` | administrative claim where used |

`CurrentContext` accepts a claim only when the request is authenticated and exactly one matching claim exists. It deliberately returns an empty company ID if a request is anonymous; it does not use a client header as a tenant source.

## 3. Registration and tenant provisioning

`POST /api/account/register` is public. It checks whether the supplied email exists, then `CompanyService.Register` provisions a new tenant.

The intended sequence is:

1. generate a new company ID;
2. clone default roles and their permissions for that company;
3. create the first active administrator employee, initially email-unverified;
4. create the `Company`, including the primary contact and public career identifiers;
5. create default notification preferences;
6. create a one-time email-verification token and send verification mail;
7. seed departments and job titles.

The administrator cannot login until verification succeeds. See the audit report for the current registration atomicity and role-grant persistence findings.

## 4. Login, token refresh and logout

### Login

`POST /api/account/login` calls `AccountServices.VerifyAndGenerateToken`.

It rejects an account unless all are true:

- employee email matches case-insensitively and the employee is active;
- email is verified;
- the assigned role exists and has application access;
- BCrypt password verification succeeds.

On success it returns a short-lived JWT plus a random refresh token. Only the SHA-256 hash of the refresh token is persisted in `RefreshToken`; the browser receives the raw token once.

### Refresh

`POST /api/account/refresh-token` hashes the supplied token, validates persisted state and expiry, then issues a new JWT and refresh token. The previous refresh-token record is marked revoked. The replacement token reflects the employee's current company and role.

### Logout

`POST /api/account/logout` requires the current JWT and refresh token. It verifies token ownership against the JWT user and revokes that token. Access JWTs remain usable until their configured expiry; refresh-token revocation prevents a new session from being minted from that token.

## 5. Verification and password lifecycle

| Flow | Route | Control |
|---|---|---|
| Email verification | `GET /api/account/verify-email?token=...` | hashed, expiring `UserSecurityToken`; verifies the employee email |
| Start reset | `POST /api/account/ResetPassword` | returns a generic accepted response to reduce account enumeration |
| Set initial/reset password | `POST /api/account/CreateNewPassword` | valid unused typed token; password hash replaced; related tokens invalidated by the service flow |
| Change password while authenticated | user/password endpoint | current authenticated identity/ownership is used |

Security tokens are stored as hashes with an expiry, type, used flag and usage timestamp. Raw verification/reset values must never be persisted or written to logs.

## 6. Request pipeline and module permission enforcement

`Program.cs` installs ASP.NET authentication and authorization. The antiforgery/request middleware invokes `CompanyIdMiddleware.AuthenticateUserRequest`, which:

1. allows explicitly public routes to continue without tenant claims;
2. requires `company_id` for normal authenticated API routes;
3. confirms the company remains active;
4. confirms the user remains active;
5. reads `ModulePermissionAttribute` on the selected endpoint;
6. calls `IRoleService.VerifyUserAccess(module, permissions, userId, companyId)`;
7. returns HTTP 403 when the permission is absent.

When a route has no module attribute, `[Authorize]`, service ownership rules, or both may still protect it. Never interpret absence of `ModulePermissionAttribute` as anonymous access.

## 7. Role and permission data model

```text
Company
  └─ Roles (company scoped)
       └─ RolePermission (company + role + ModulePermission decision)
            └─ ModulePermission (global module/permission pairing)
                 ├─ Module (global feature catalogue)
                 └─ Permission (global action catalogue)
```

The ordinary action set is `View`, `Create`, `Edit`, and `Delete`. WFH also uses explicit ownership/team/reviewer actions such as `ViewOwn`, `CreateOwn`, `CancelOwn`, `ViewTeam`, `ApproveTeam`, `ViewAll`, `PolicyView`, and `PolicyEdit`.

Administrator is a role type, not a different tenant. `VerifyUserAccess` grants an active administrator role access before evaluating ordinary role-permission entries. Every other role needs a matching accessible role-permission grant.

## 8. Module access reference

| Module constant | Main purpose | Typical access rule |
|---|---|---|
| `Employees` | employee master, invitations, managed profiles | View/Create/Edit/Delete module permissions; self-profile routes use ownership |
| `Attendance` | manual attendance, calendar, status settings | Attendance permissions; own profile calendar has server-derived identity |
| `Leave_Management` | policies, balances, requests and review | employee request ownership plus HR reviewer actions |
| `Work_From_Home` | policy, scheduling, approval, clocking | employee ownership for own requests; policy/team/all/approval permissions for reviewers |
| `PayRoll` | salary execution and payslips | payroll module permissions plus employee ownership for own output |
| `Payroll_Settings` | payroll-period/divisor configuration | privileged configuration access |
| `Calendar` | holiday/event calendar | calendar module permissions |
| `Notice_Board` | notices and read state | notice module permissions, recipient ownership |
| `Policy` | company policy documents and versions | policy module permissions plus company scope |
| `Jobs`, `Applications`, `Process_Log` | recruitment operations | each feature has its own module constant; public careers use separate anonymous contracts |
| `Career_Profile` | public company career content | tenant-scoped authenticated edit/view permission |

The frontend may hide navigation based on the permission strings supplied at login, but that is usability only. The API remains authoritative.

## 9. Ownership rules by functional area

- **Employee profile:** a user can read/update their own profile paths; management of another employee needs employee-module authority.
- **Leave:** employee-facing routes set/filter the employee ID from `CurrentContext.UserId`; an approver does not become the requester.
- **WFH:** own requests, timing and clock events are service ownership checked. Review/policy endpoints are module-permission protected. Attendance rows created by WFH retain source provenance and cannot be manually overwritten.
- **Attendance:** HR/Admin calendar calls are tenant-scoped. `my-calendar` derives the user from the JWT and never accepts a user ID from the browser.
- **Payroll:** employee output is restricted to the caller unless the caller has payroll-management access.
- **Recruitment public surface:** anonymous routes must resolve company from a trusted public route/resource context; they must not accept a tenant header.

## 10. Rules for new endpoints and modules

1. Begin with `[Authorize]` unless the route is explicitly public.
2. For module actions, add `[ModulePermission(AppModule.X, Permission.Y)]`.
3. Derive `CompanyId` and acting `UserId` from `CurrentContext`.
4. If a request accepts another user/resource ID, load it tenant-scoped and verify ownership/reviewer authority.
5. Add a migration for a new module/permission and grants for existing tenants.
6. Update the React permission model and navigation only after server authorization exists.
7. Add two-tenant tests: user A must not read/mutate user B's data.

## 11. Operational checklist

- Verify email before first login.
- Re-login or refresh after a role/grant change so the UI receives updated permissions; the API still evaluates current database grants.
- Deactivate an employee or company to block subsequent middleware checks.
- Audit `RolePermission` rows by both `CompanyId` and `RoleId`.
- Do not run registration/tenant-isolation stress tests against the business database. Use a new isolated Testing MongoDB database.

## Related documents

- [Role/permission and tenant audit](./test-reports/ROLE-PERMISSION-TENANT-AUDIT-2026-07-29.md)
- [Complete project documentation](./COMPLETE_PROJECT_DOCUMENTATION.md)
- [Roles and permissions reference](./roles-and-permissions-complete-reference.md)
- [WFH module](./work-from-home-module.md)
