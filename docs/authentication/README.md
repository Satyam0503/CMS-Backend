# Authentication and Authorization

`AccountController` provides account/session actions; `RolesController` manages roles and grants. Startup and middleware are in `Codeji.CMS.API/Program.cs`. Controller-level access commonly uses `[Authorize]` and `[ModulePermission(...)]`; domain services add company and ownership checks.

See `../auth-and-permissions.md`, `../authentication-and-authorization-reference.md`, and `../ROLE_PERMISSION_INDEX.md`. For frontend route composition, see `../UI_ROUTE_INDEX.md`.
