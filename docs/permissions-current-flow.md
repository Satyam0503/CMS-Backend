# Permissions / Access Control — Current Flow

> Status: Current reference for authentication, JWT claims, module permissions, and tenant-scoped access enforcement.

## 1. The access-control stack

Access is enforced in layers:

1. Authentication — is the caller signed in?
2. Tenant context — which company does the request belong to?
3. Module permission — does the user’s role have the action on the module?
4. Endpoint policy — does the endpoint require an extra policy such as `AdminOnly`?

This layered approach is why the permission doc matters to almost every module in the repo.

## 2. Core files

| Area | File |
|---|---|
| Auth docs | [`docs/auth-and-permissions.md`](./auth-and-permissions.md) |
| Current context helper | [`Codeji.CMS.Utility/middlewares/CurrentContext.cs`](../Codeji.CMS.Utility/middlewares/CurrentContext.cs) |
| Module-permission attribute | [`Codeji.CMS.API/App_Start/ModulePermissionAttribute.cs`](../Codeji.CMS.API/App_Start/ModulePermissionAttribute.cs) |
| Permission middleware | [`Codeji.CMS.API/App_Start/AuthenticateUserRequest.cs`](../Codeji.CMS.API/App_Start/AuthenticateUserRequest.cs) |
| Custom policy | [`Codeji.CMS.API/App_Start/CustomAuthorizationPolicy.cs`](../Codeji.CMS.API/App_Start/CustomAuthorizationPolicy.cs) |
| Role service | [`Codeji.CMS.Services/Employees/RoleServices.cs`](../Codeji.CMS.Services/Employees/RoleServices.cs) |

## 3. Claim model

The current request context reads these important claims:

- `user_id`
- `role_id`
- `company_id`
- `admin_id`

These claims are the bridge between the JWT and the rest of the request pipeline.

## 4. Module-permission flow

### 4.1 How an endpoint declares access

Controllers mark actions with:

```csharp
[ModulePermission(AppModule.SomeModule, Permission.View)]
```

or a permission list such as:

```csharp
[ModulePermission(AppModule.Calendar, [Permission.Create, Permission.Edit])]
```

### 4.2 How the middleware enforces it

1. The request reaches the authorization pipeline.
2. The endpoint metadata is inspected.
3. The module and permission list are resolved.
4. `IRoleService.VerifyUserAccess(...)` is called.
5. A 403 is returned if access is denied.

## 5. Permission data model

The database model is:

`Module -> ModulePermission -> RolePermission -> Roles`

That means access is not granted by role name alone. A role must be linked to the specific module action.

## 6. Default role behavior

During company registration, the system seeds default roles and their permissions so that:

- Administrator can manage the tenant;
- HR gets the expected employee/recruitment permissions;
- Employee gets the limited self-service surface area.

## 7. Current endpoint patterns

### Strict module gating

Most business endpoints use `[ModulePermission]` and are tenant-scoped.

### Admin-only exceptions

Some master-data endpoints still use `AdminOnly` policy on top of the module model.

### Public exceptions

Some endpoints are intentionally anonymous:

- login/register
- public careers
- application submission
- antiforgery token issuance

Those endpoints still need tenant-safe logic even without authentication.

## 8. Connections to other modules

Permissions are the switchboard for:

- attendance
- leave
- payroll
- calendar
- jobs
- applicants
- career profile
- employee CRUD
- company master data

If a module gets a new endpoint, the first question should be: “What permission does it need?”

## 9. Risks and watch-outs

- Missing `[ModulePermission]` on a new controller action can accidentally expose functionality to any authenticated user.
- Some public endpoints still rely on request headers for tenant context, so auth-free does not mean context-free.
- Role names and permission grants are separate concepts; do not assume `HR` automatically means access.

