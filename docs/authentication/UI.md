# Authentication and Authorization UI Documentation

**Last verified against code:** 2026-09-02

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## 1. Frontend Source Map

### 1.1 `src/app/modules/auth/AuthLayout.tsx`

- Source lines: 185
- Extracted symbols: benefits, AuthLayout, isDark, isDawn
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/auth/AuthPage.tsx`

- Source lines: 23
- Extracted symbols: AuthPage
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/auth/components/CreatePassword.tsx`

- Source lines: 189
- Extracted symbols: validationSchema, initialValues, CreatePassword, navigate, token, handlePasswordChange, payload, response
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/auth/components/ForgotPassword.tsx`

- Source lines: 113
- Extracted symbols: forgotPasswordSchema, ForgotPassword, navigate, location, autoSubmitStarted, navigationState, emailFromLogin, formik, response, type
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/auth/components/Login.tsx`

- Source lines: 260
- Extracted symbols: loginSchema, initialValues, Login, theme, dispatch, fieldSx, handleLogin, handleResendVerification, back
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/auth/components/Registration.tsx`

- Source lines: 276
- Extracted symbols: initialValues, Registration, navigate, handleRegistration, payload
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/auth/core/_models.ts`

- Source lines: 40
- Extracted symbols: AuthModel, CompanyRegisterModel, UserLoginModel, UserAddressModel, UserCommunicationModel, AuthTokens, RefreshTokenPayload, LogInResponseModel
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/auth/core/_requests.ts`

- Source lines: 65
- Extracted symbols: registerCompany, postRequest, login, resendEmailVerification, logoutUser, refreshTokenAsync, getSignedUserDetails, createPassword, resetPassword
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/auth/core/Auth.tsx`

- Source lines: 60
- Extracted symbols: currentUser, dispatch, navigate, requestUserDetails, savedLanguage, defaultLanguage
- React/data hooks found: useState, useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/auth/core/AuthHelpers.ts`

- Source lines: 43
- Extracted symbols: getLocalKey, setLocalKey, lsValue, removeLocalKey
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/auth/index.ts`

- Source lines: 5
- Extracted symbols: 
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/auth/Logout.tsx`

- Source lines: 60
- Extracted symbols: Logout, dispatch, signalRInstance, chatHubInstance, logOutUser, refreshToken
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/routing/AppRoutes.tsx`

- Source lines: 51
- Extracted symbols: 
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/app/routing/LeaveManagementRoutes.tsx`

- Source lines: 21
- Extracted symbols: LeaveManagementRoutes
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.15 `src/app/routing/NoticeBoardRoutes.tsx`

- Source lines: 47
- Extracted symbols: NoticeBoardRoutes
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.16 `src/app/routing/PoliciesRoutes.tsx`

- Source lines: 65
- Extracted symbols: Policies, PolicyVersions, SuspensedView, PoliciesRoutes
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.17 `src/app/routing/PrivateRoutes.tsx`

- Source lines: 195
- Extracted symbols: Admin, Recruitment, Employees, Dashboard, Profile, Calendar, NoticeBoard, Policies, Attendance, LeaveManagement, PayRollManagement, PayrollSettings, AttendanceStatusSettings, PrivateRoutes, PropsType, SuspensedView, theme
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.18 `src/app/routing/ProfileRoutes.tsx`

- Source lines: 57
- Extracted symbols: UserProfile, AboutUser, UserPerformance, WorkHistory, Setting, LeaveTracker, SuspensedView, ProfileTabs, empId, roleType, modulePermission, hasLeavePanelAccess
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.19 `src/app/routing/RecruitmentRoutes.tsx`

- Source lines: 107
- Extracted symbols: RecruitmentRoutes
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `GetAntiForgeryToken` | `GET api/account/antiforgerytoken/{appKey}` | Search service functions and component event handlers for this route/action. |
| 2 | `CheckAppVersion` | `GET api/app/checkAppVersion` | Search service functions and component event handlers for this route/action. |
| 3 | `Register` | `POST api/account/register` | Search service functions and component event handlers for this route/action. |
| 4 | `Login` | `POST api/account/login` | Search service functions and component event handlers for this route/action. |
| 5 | `RefreshToken` | `POST api/account/refresh-token` | Search service functions and component event handlers for this route/action. |
| 6 | `LogOut` | `POST api/account/logout` | Search service functions and component event handlers for this route/action. |
| 7 | `GetUserByToken` | `POST api/account/getSignedUserDetails` | Search service functions and component event handlers for this route/action. |
| 8 | `CreateNewPassword` | `POST api/CreateNewPassword` | Search service functions and component event handlers for this route/action. |
| 9 | `VerifyEmail` | `GET api/account/verify-email` | Search service functions and component event handlers for this route/action. |
| 10 | `ResendEmailVerification` | `POST api/account/resend-email-verification` | Search service functions and component event handlers for this route/action. |
| 11 | `ResetPassword` | `POST api/account/ResetPassword` | Search service functions and component event handlers for this route/action. |
| 12 | `RegisterApplicants` | `POST api/applicant/applyJob` | Search service functions and component event handlers for this route/action. |
| 13 | `GetRoles` | `GET api/Roles/GetRoles` | Search service functions and component event handlers for this route/action. |
| 14 | `GetRolePermission` | `GET api/Roles/GetRolePermission` | Search service functions and component event handlers for this route/action. |
| 15 | `AddRole` | `POST api/Roles/AddEditRole` | Search service functions and component event handlers for this route/action. |
| 16 | `GetModulePermission` | `GET api/Roles/GetAllRolesWithPermission` | Search service functions and component event handlers for this route/action. |
| 17 | `GetModulePermissionyId` | `GET api/Roles/GetRoleWithPermissionById` | Search service functions and component event handlers for this route/action. |
| 18 | `GetRoleById` | `GET api/Roles/GetRoleById/{roleId}` | Search service functions and component event handlers for this route/action. |
| 19 | `UpdateAppAccessForRole` | `PATCH api/Roles/UpdateAppAccessForRole/{roleId}` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `GetAntiForgeryToken`

- Backend endpoint: `GET api/account/antiforgerytoken/{appKey}`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `string appKey`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.2 UI trace for `CheckAppVersion`

- Backend endpoint: `GET api/app/checkAppVersion`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `string appVersion`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.3 UI trace for `Register`

- Backend endpoint: `POST api/account/register`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `[FromBody] CompanyRequestModel companyModel`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

Source files containing the action name or a static route fragment:
- `src/app/modules/auth/components/Login.tsx` (matched: Register, register)
- `src/app/modules/auth/components/Registration.tsx` (matched: Register, register)
- `src/app/modules/auth/core/_models.ts` (matched: Register, register)
- `src/app/modules/auth/core/_requests.ts` (matched: Register, register)

## 3.4 UI trace for `Login`

- Backend endpoint: `POST api/account/login`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `[FromBody] LoginModel model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

Source files containing the action name or a static route fragment:
- `src/app/modules/auth/AuthPage.tsx` (matched: Login, login)
- `src/app/modules/auth/components/CreatePassword.tsx` (matched: Login, login)
- `src/app/modules/auth/components/ForgotPassword.tsx` (matched: Login, login)
- `src/app/modules/auth/components/Login.tsx` (matched: Login, login)
- `src/app/modules/auth/components/Registration.tsx` (matched: Login, login)
- `src/app/modules/auth/core/_models.ts` (matched: Login, login)
- `src/app/modules/auth/core/_requests.ts` (matched: Login, login)
- `src/app/modules/auth/Logout.tsx` (matched: Login, login)

## 3.5 UI trace for `RefreshToken`

- Backend endpoint: `POST api/account/refresh-token`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `[FromBody] RefreshTokenRequestDto model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

Source files containing the action name or a static route fragment:
- `src/app/modules/auth/core/_models.ts` (matched: RefreshToken)
- `src/app/modules/auth/core/_requests.ts` (matched: RefreshToken)
- `src/app/modules/auth/Logout.tsx` (matched: RefreshToken)

## 3.6 UI trace for `LogOut`

- Backend endpoint: `POST api/account/logout`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `[FromBody] RefreshTokenRequestDto model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

Source files containing the action name or a static route fragment:
- `src/app/modules/auth/core/_requests.ts` (matched: LogOut, logout)
- `src/app/modules/auth/core/Auth.tsx` (matched: LogOut, logout)
- `src/app/modules/auth/index.ts` (matched: LogOut, logout)
- `src/app/modules/auth/Logout.tsx` (matched: LogOut, logout)
- `src/app/routing/AppRoutes.tsx` (matched: LogOut, logout)

## 3.7 UI trace for `GetUserByToken`

- Backend endpoint: `POST api/account/getSignedUserDetails`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: ``
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

Source files containing the action name or a static route fragment:
- `src/app/modules/auth/core/_requests.ts` (matched: getSignedUserDetails)
- `src/app/modules/auth/core/Auth.tsx` (matched: getSignedUserDetails)

## 3.8 UI trace for `CreateNewPassword`

- Backend endpoint: `POST api/CreateNewPassword`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `CreateNewPasswordRequest model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.9 UI trace for `VerifyEmail`

- Backend endpoint: `GET api/account/verify-email`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `[FromQuery] string token`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.10 UI trace for `ResendEmailVerification`

- Backend endpoint: `POST api/account/resend-email-verification`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `[FromBody] EmailVerificationRequest model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

Source files containing the action name or a static route fragment:
- `src/app/modules/auth/components/Login.tsx` (matched: ResendEmailVerification)
- `src/app/modules/auth/core/_requests.ts` (matched: ResendEmailVerification, resend-email-verification)

## 3.11 UI trace for `ResetPassword`

- Backend endpoint: `POST api/account/ResetPassword`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `[FromBody] JsonElement body`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

Source files containing the action name or a static route fragment:
- `src/app/modules/auth/components/ForgotPassword.tsx` (matched: ResetPassword)
- `src/app/modules/auth/core/_requests.ts` (matched: ResetPassword)

## 3.12 UI trace for `RegisterApplicants`

- Backend endpoint: `POST api/applicant/applyJob`
- Controller source: `Codeji.CMS.API/Controllers/AccountController.cs`
- Declared parameters: `[FromBody] ApplicantAddEditModel applicantRegisterModel`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.13 UI trace for `GetRoles`

- Backend endpoint: `GET api/Roles/GetRoles`
- Controller source: `Codeji.CMS.API/Controllers/RolesController.cs`
- Declared parameters: `[FromQuery] bool? excludeAdmin`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.14 UI trace for `GetRolePermission`

- Backend endpoint: `GET api/Roles/GetRolePermission`
- Controller source: `Codeji.CMS.API/Controllers/RolesController.cs`
- Declared parameters: ``
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.15 UI trace for `AddRole`

- Backend endpoint: `POST api/Roles/AddEditRole`
- Controller source: `Codeji.CMS.API/Controllers/RolesController.cs`
- Declared parameters: `RoleWithModuleAndPermissions model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.16 UI trace for `GetModulePermission`

- Backend endpoint: `GET api/Roles/GetAllRolesWithPermission`
- Controller source: `Codeji.CMS.API/Controllers/RolesController.cs`
- Declared parameters: ``
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.17 UI trace for `GetModulePermissionyId`

- Backend endpoint: `GET api/Roles/GetRoleWithPermissionById`
- Controller source: `Codeji.CMS.API/Controllers/RolesController.cs`
- Declared parameters: `[FromQuery] string roleId`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.18 UI trace for `GetRoleById`

- Backend endpoint: `GET api/Roles/GetRoleById/{roleId}`
- Controller source: `Codeji.CMS.API/Controllers/RolesController.cs`
- Declared parameters: `string roleId`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.19 UI trace for `UpdateAppAccessForRole`

- Backend endpoint: `PATCH api/Roles/UpdateAppAccessForRole/{roleId}`
- Controller source: `Codeji.CMS.API/Controllers/RolesController.cs`
- Declared parameters: `string roleId, [FromBody] bool hasAppAccess`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
- Enabled state: verify status, ownership, form-validity and in-flight guards.
- Submission: trace the event handler to the frontend client and exact endpoint.
- Loading: identify query/mutation pending rendering.
- Success: identify cache invalidation, refetch, navigation, modal close and user feedback.
- Error: identify server-message handling and fallback rendering.
- Empty state: verify behavior when the read returns no records.
- Responsive behavior: inspect MUI breakpoints, grids, overflow and modal props in the owning component.
- Security: UI hiding is not authorization; backend checks remain mandatory.
- Current mapping status: not conclusively determined solely from controller extraction; use the frontend source map above.

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 4. Form and Interaction Review Rules

- Fields: derive names, types, defaults and optionality from form code and request DTO together.
- Validation: document frontend validation separately from backend rejection.
- Filters/pagination: establish whether state is local or server-backed.
- Retry: confirm whether query libraries retry reads or mutations.
- Accessibility: preserve labels, keyboard operation, focus return and tooltip semantics.
- Permissions: use actual guards/permitted actions; do not introduce role-name guesses.

