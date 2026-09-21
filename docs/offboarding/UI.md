# Offboarding UI Documentation

**Last verified against code:** 2026-09-03

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## Operational case queue

The profile-header **Offboarding cases** modal lists active cases available to the signed-in reviewer. Selecting a row opens that employee's existing profile, where the management case card provides the detailed workflow. The list shows only summary fields and progress.

## 1. Frontend Source Map

### 1.1 `src/app/modules/employees/employeeServices.ts`

- Source lines: 222
- Extracted symbols: updateEmployee, postRequest, EditOwnProfilePayloadModel, updateOwnProfile, updateSalary, deleteEmployee, deleteRequest, getAllEmployees, initiateEmployeeExit, completeEmployeeExit, employeeUserId, getDirectoryDepartments, submitOwnResignation, getOwnEmployeeExitCase, getOwnEmployeeExitHistory, withdrawOwnEmployeeExit, getEmployeeExitCaseForManagement, reviewEmployeeExit, respondToEmployeeExitDiscussion, respondToEmployeeExitJointDiscussion, requestEmployeeExitNoticeAdjustment, updateEmployeeExitPostExitContact, acknowledgeEmployeeExit, createEmployeeExitFormerEmployeeRequest, resolveEmployeeExitFormerEmployeeRequest, issueEmployeeExitDocument, createFormerEmployeePortalInvite
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx`

- Routed experience for `/former-employee` (the legacy `FormerEmployeePortalPage.tsx` remains for compatibility).
- Provides OTP access, secure session lifecycle, documents, requests, support, profile, logout, responsive desktop/mobile layouts, Stitch-inspired cards/navigation, and reduced-motion-safe animations.
- Company branding and theme are applied from the server response (`CompanyName`, `CompanyLogo`, `Theme`); browser session storage is only a visual cache and cannot override tenant authority.
- Desktop dashboard includes animated teal welcome hero, verified-access badge, exit journey progress, employment overview, quick actions, recent requests/documents, and profile navigation.
- Mobile layout uses a compact hero and bottom navigation; all actions remain wired to the existing portal service methods.
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx`

- Source lines: 64
- Extracted symbols: sessionKey, FormerEmployeePortalPage, navigate, load, token, submit, requestOtp, verifyOtp, download, response, url
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts`

- Source lines: 32
- Extracted symbols: FormerEmployeePortalCase, sessionConfig, redeemFormerEmployeePortalInvite, verifyFormerEmployeeContact, requestFormerEmployeeOtp, verifyFormerEmployeeOtp, logoutFormerEmployeePortal, getFormerEmployeePortalCase, createFormerEmployeePortalRequest, formerEmployeeDocumentUrl, formerEmployeeDocumentPreviewUrl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/users/components/EmployeeExitCaseCard.tsx`

- Source lines: 294
- Extracted symbols: ReviewJourney, EmployeeExitCaseCard, withdraw, response, respondToDiscussion, refreshed, savePostExitContact, acknowledge, createRequest, uploadEvidence, payload, useEvidence, preview, url, link, refreshCase, requestNoticeAdjustment, submitHandover, decideOffer, activeCase, pendingNotice
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/users/components/EmployeeExitManagementCard.tsx`

- Source lines: 1159
- Extracted symbols: Props, ReviewAction, offboardingErrorMessage, EmployeeExitManagementCard, refresh, response, getAllEmployees, run, review, uploadInterviewFile, payload, saveInterview, transferOwnership, enableFormerEmployeePortal, uploadPortalDocument, openHrPortalDocument
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx`

- Source lines: 98
- Extracted symbols: Props, SectionProps, Section, initial, completionErrorMessage, itemRowSx, actionRowSx, formGridSx, EmployeeExitOperationsPanel, run, field, pendingChecklist, pendingOperationalWork, totalBlockers, blockers, completeExit, useEvidence, preview, response, url, link
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/users/components/EmployeeExitPolicySettings.tsx`

- Source lines: 205
- Extracted symbols: PolicyScope, departmentName, policyDepartmentIds, FieldHelp, ToggleTile, CompletionTile, EmployeeExitPolicySettings, companyPolicy, selectedDepartments, selectedRole, makeScopedPolicy, existing, base, load, chooseScope, chooseDepartments, ids, policyScopeLabel, departmentIds, names, role, policyScopeKind, editPolicy, nextScope, addPolicy, toggle, savePolicy, addTemplate, deleteTemplate
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/users/components/EmployeeOffboardingAction.tsx`

- Source lines: 71
- Extracted symbols: EmployeeOffboardingAction, currentUserId, permissions, isOwnProfile, canManage, status, getEmployeeExitCaseForManagement, exitCase, submit, response
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/users/components/ImageCropper.tsx`

- Source lines: 155
- Extracted symbols: Props, MIN_DIMENSION, MIN_WIDTH_RECTANGLE, MIN_HEIGHT_RECTANGLE, canvasPreview, ctx, scaleX, scaleY, pixelRatio, cropX, cropY, centerX, centerY, imageRef, canvasRef, intl, onImageLoad, cropWidthInPercent, crop, onCropComplete, canvas, base64, aspect
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/users/components/LeaveCalendar.tsx`

- Source lines: 208
- Extracted symbols: AttendanceStatusSetting, CalendarDayAttendance, LeaveCalendar, userData, currentUserId, targetUserId, canViewOtherAttendance, mayViewCalendar, load, fromDate, toDate, response, attendanceByDay, days, date, entry, statusByCode, visibleStatuses, renderDay, dateKey, holiday, holidayName, dayAttendance, records, colors, color, background, statusLines
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/users/components/ProfileCard.tsx`

- Source lines: 251
- Extracted symbols: theme, profileImage, employeeId, defaultLanguage, paySlipControlColor, handleClose, handleDateChange, viewPaySlip, response, fileURL, errorData, errorJson
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/modules/users/components/ProfileUpdateModal.tsx`

- Source lines: 206
- Extracted symbols: Props, SupportedImageFormat, MAX_IMAGE_SIZE, dispatch, userData, handleFileChange, files, selectedFile, fileSizeInMB, fileFormat, reader, handleUpdateAvatar, formData, base64Image, blob, Uint8Array, response, handlePictureRemove, handleImageCropDiscard, handleImageCropSuccess
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/app/modules/users/components/ResignationAction.tsx`

- Source lines: 61
- Extracted symbols: ResignationAction, currentUserId, isCurrent, submit, response
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.15 `src/app/modules/users/components/WorkShiftCard.tsx`

- Source lines: 114
- Extracted symbols: OfficeSchedule, OfficeScheduleAssignment, formatTime, hour, formatWorkingDays, labels, WorkShiftCard, employeeId, permissions, canManage, load, save, timing, workingDays
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

## 2.1 Current visual/runtime status (2026-09-03)

- The active route is `AppRoutes.tsx -> FormerEmployeePortalExperience`.
- The Vite development server was restarted from `C:\Users\User 2\CMS\CMS-React` to ensure the current source is served on `localhost:5173`.
- Frontend `npm run typecheck` passes.
- Animations include portal entry, hero orbit/float, feature reveal, OTP transitions, verification pulse, and decorative wave motion; `prefers-reduced-motion` disables them.
- Styling is responsive through MUI breakpoints and preserves the existing portal API, tenant scoping, server authorization, and action handlers.
- Browser/UAT pixel comparison against supplied Stitch images remains a visual acceptance step; the supplied images are design references, not executable product screens.

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `Redeem` | `POST api/public/former-employee/redeem` | Search service functions and component event handlers for this route/action. |
| 2 | `RequestOtp` | `POST api/public/former-employee/otp/request` | Search service functions and component event handlers for this route/action. |
| 3 | `VerifyOtp` | `POST api/public/former-employee/otp/verify` | Search service functions and component event handlers for this route/action. |
| 4 | `Logout` | `POST api/public/former-employee/logout` | Search service functions and component event handlers for this route/action. |
| 5 | `VerifyContact` | `POST api/public/former-employee/verify-contact` | Search service functions and component event handlers for this route/action. |
| 6 | `GetCase` | `GET api/public/former-employee/case` | Search service functions and component event handlers for this route/action. |
| 7 | `CreateRequest` | `POST api/public/former-employee/requests` | Search service functions and component event handlers for this route/action. |
| 8 | `DownloadDocument` | `GET api/public/former-employee/documents/{evidenceId}` | Search service functions and component event handlers for this route/action. |
| 9 | `PreviewDocument` | `GET api/public/former-employee/documents/{evidenceId}/preview` | Search service functions and component event handlers for this route/action. |
| 10 | `InitiateEmployeeExit` | `POST api/User/InitiateEmployeeExit` | Search service functions and component event handlers for this route/action. |
| 11 | `SubmitOwnResignation` | `POST api/User/SubmitOwnResignation` | Search service functions and component event handlers for this route/action. |
| 12 | `GetEmployeeExitCase` | `GET api/User/EmployeeExit/{employeeUserId}` | Search service functions and component event handlers for this route/action. |
| 13 | `GetOwnEmployeeExitHistory` | `GET api/User/EmployeeExitHistory/{employeeUserId}` | Search service functions and component event handlers for this route/action. |
| 14 | `GetEmployeeExitCaseForManagement` | `GET api/User/EmployeeExitManagement/{employeeUserId}` | Search service functions and component event handlers for this route/action. |
| 15 | `CompleteEmployeeExit` | `POST api/User/CompleteEmployeeExit` | Search service functions and component event handlers for this route/action. |
| 16 | `ReviewEmployeeExit` | `POST api/User/ReviewEmployeeExit` | Search service functions and component event handlers for this route/action. |
| 17 | `RespondToEmployeeExitDiscussion` | `POST api/User/RespondToEmployeeExitDiscussion` | Search service functions and component event handlers for this route/action. |
| 18 | `RespondToEmployeeExitJointDiscussion` | `POST api/User/RespondToEmployeeExitJointDiscussion` | Search service functions and component event handlers for this route/action. |
| 19 | `WithdrawOwnEmployeeExit` | `POST api/User/WithdrawOwnEmployeeExit` | Search service functions and component event handlers for this route/action. |
| 20 | `DecideEmployeeExitWithdrawal` | `POST api/User/DecideEmployeeExitWithdrawal` | Search service functions and component event handlers for this route/action. |
| 21 | `CancelEmployeeExit` | `POST api/User/CancelEmployeeExit` | Search service functions and component event handlers for this route/action. |
| 22 | `UpdateEmployeeExitDate` | `POST api/User/UpdateEmployeeExitDate` | Search service functions and component event handlers for this route/action. |
| 23 | `RequestEmployeeExitNoticeAdjustment` | `POST api/User/RequestEmployeeExitNoticeAdjustment` | Search service functions and component event handlers for this route/action. |
| 24 | `DecideEmployeeExitNoticeAdjustment` | `POST api/User/DecideEmployeeExitNoticeAdjustment` | Search service functions and component event handlers for this route/action. |
| 25 | `DecideOwnEmployeeExitNoticeOffer` | `POST api/User/DecideOwnEmployeeExitNoticeOffer` | Search service functions and component event handlers for this route/action. |
| 26 | `UpsertEmployeeExitPolicy` | `POST api/User/EmployeeExitPolicy` | Search service functions and component event handlers for this route/action. |
| 27 | `GetEmployeeExitPolicy` | `GET api/User/EmployeeExitPolicy/{policyId}` | Search service functions and component event handlers for this route/action. |
| 28 | `GetEmployeeExitClearanceTemplate` | `GET api/User/EmployeeExitClearanceTemplate/{templateId}` | Search service functions and component event handlers for this route/action. |
| 29 | `GetEmployeeExitPolicies` | `GET api/User/EmployeeExitPolicies` | Search service functions and component event handlers for this route/action. |
| 30 | `GetEmployeeExitClearanceTemplates` | `GET api/User/EmployeeExitClearanceTemplates` | Search service functions and component event handlers for this route/action. |
| 31 | `UpsertEmployeeExitClearanceTemplate` | `POST api/User/EmployeeExitClearanceTemplate` | Search service functions and component event handlers for this route/action. |
| 32 | `DeleteEmployeeExitClearanceTemplate` | `DELETE api/User/EmployeeExitClearanceTemplate/{templateId}` | Search service functions and component event handlers for this route/action. |
| 33 | `ApproveEmployeeFinalSettlement` | `POST api/User/ApproveEmployeeFinalSettlement` | Search service functions and component event handlers for this route/action. |
| 34 | `UpdateEmployeeFinalSettlementPayment` | `POST api/User/UpdateEmployeeFinalSettlementPayment` | Search service functions and component event handlers for this route/action. |
| 35 | `CalculateEmployeeFinalSettlement` | `POST api/User/CalculateEmployeeFinalSettlement` | Search service functions and component event handlers for this route/action. |
| 36 | `MarkEmployeeFinalSettlementPaid` | `POST api/User/MarkEmployeeFinalSettlementPaid` | Search service functions and component event handlers for this route/action. |
| 37 | `RecordEmployeeExitInterview` | `POST api/User/RecordEmployeeExitInterview` | Search service functions and component event handlers for this route/action. |
| 38 | `UpsertEmployeeExitReassignment` | `POST api/User/EmployeeExitReassignment` | Search service functions and component event handlers for this route/action. |
| 39 | `GenerateEmployeeExitDocument` | `POST api/User/GenerateEmployeeExitDocument` | Search service functions and component event handlers for this route/action. |
| 40 | `IssueEmployeeExitDocument` | `POST api/User/IssueEmployeeExitDocument` | Search service functions and component event handlers for this route/action. |
| 41 | `CompleteEmployeeExitClearance` | `POST api/User/CompleteEmployeeExitClearance` | Search service functions and component event handlers for this route/action. |
| 42 | `UpdateEmployeeExitClearanceStatus` | `POST api/User/UpdateEmployeeExitClearanceStatus` | Search service functions and component event handlers for this route/action. |
| 43 | `WaiveEmployeeExitClearance` | `POST api/User/WaiveEmployeeExitClearance` | Search service functions and component event handlers for this route/action. |
| 44 | `UpsertEmployeeExitHandoverTask` | `POST api/User/EmployeeExitHandoverTask` | Search service functions and component event handlers for this route/action. |
| 45 | `ReviewEmployeeExitHandoverTask` | `POST api/User/ReviewEmployeeExitHandoverTask` | Search service functions and component event handlers for this route/action. |
| 46 | `UpsertEmployeeExitAsset` | `POST api/User/EmployeeExitAsset` | Search service functions and component event handlers for this route/action. |
| 47 | `UpsertEmployeeExitAccessItem` | `POST api/User/EmployeeExitAccessItem` | Search service functions and component event handlers for this route/action. |
| 48 | `UpsertEmployeeExitFinanceItem` | `POST api/User/EmployeeExitFinanceItem` | Search service functions and component event handlers for this route/action. |
| 49 | `UpsertEmployeeExitBenefitItem` | `POST api/User/EmployeeExitBenefitItem` | Search service functions and component event handlers for this route/action. |
| 50 | `AddEmployeeExitEvidence` | `POST api/User/EmployeeExitEvidence` | Search service functions and component event handlers for this route/action. |
| 51 | `UploadEmployeeExitEvidence` | `POST api/User/EmployeeExitEvidenceUpload` | Search service functions and component event handlers for this route/action. |
| 52 | `DownloadEmployeeExitEvidence` | `GET api/User/EmployeeExitEvidence/{employeeExitId}/{evidenceId}/download` | Search service functions and component event handlers for this route/action. |
| 53 | `VerifyEmployeeExitEvidence` | `POST api/User/VerifyEmployeeExitEvidence` | Search service functions and component event handlers for this route/action. |
| 54 | `SupersedeEmployeeExitEvidence` | `POST api/User/SupersedeEmployeeExitEvidence` | Search service functions and component event handlers for this route/action. |
| 55 | `UpdateEmployeeExitPostExitContact` | `POST api/User/EmployeeExitPostExitContact` | Search service functions and component event handlers for this route/action. |
| 56 | `AcknowledgeEmployeeExit` | `POST api/User/AcknowledgeEmployeeExit` | Search service functions and component event handlers for this route/action. |
| 57 | `CreateEmployeeExitFormerEmployeeRequest` | `POST api/User/EmployeeExitFormerEmployeeRequest` | Search service functions and component event handlers for this route/action. |
| 58 | `ResolveEmployeeExitFormerEmployeeRequest` | `POST api/User/EmployeeExitFormerEmployeeRequestResolve` | Search service functions and component event handlers for this route/action. |
| 59 | `CreateFormerEmployeePortalInvite` | `POST api/User/EmployeeExitFormerEmployeePortalInvite` | Search service functions and component event handlers for this route/action. |
| 60 | `RevokeFormerEmployeePortalAccess` | `POST api/User/EmployeeExitFormerEmployeePortalRevoke` | Search service functions and component event handlers for this route/action. |
| 61 | `SetFormerEmployeePortalAccess` | `POST api/User/EmployeeExitFormerEmployeePortalAccess` | Search service functions and component event handlers for this route/action. |
| 62 | `SetCompanyFormerEmployeePortalAccess` | `POST api/User/CompanyFormerEmployeePortalAccess` | Search service functions and component event handlers for this route/action. |
| 63 | `RejoinEmployee` | `POST api/User/RejoinEmployee` | Search service functions and component event handlers for this route/action. |
| 64 | `ExceptionalCompleteEmployeeExit` | `POST api/User/ExceptionalCompleteEmployeeExit` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `Redeem`

- Backend endpoint: `POST api/public/former-employee/redeem`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Declared parameters: `[FromBody] FormerEmployeePortalTokenRequest request`
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
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: Redeem, redeem)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx` (matched: Redeem, redeem)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: Redeem, redeem)

## 3.2 UI trace for `RequestOtp`

- Backend endpoint: `POST api/public/former-employee/otp/request`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Declared parameters: `[FromBody] FormerEmployeePortalOtpRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: request)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: request)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx` (matched: RequestOtp, request)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: request)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: request)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: request)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: request)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: request)
- `src/app/modules/users/components/LeaveCalendar.tsx` (matched: request)
- `src/app/modules/users/components/ResignationAction.tsx` (matched: request)

## 3.3 UI trace for `VerifyOtp`

- Backend endpoint: `POST api/public/former-employee/otp/verify`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Declared parameters: `[FromBody] FormerEmployeePortalOtpVerifyRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: verify)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: VerifyOtp, verify)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx` (matched: VerifyOtp, verify)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: verify)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: verify)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: verify)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: verify)

## 3.4 UI trace for `Logout`

- Backend endpoint: `POST api/public/former-employee/logout`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
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
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: Logout, logout)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: Logout, logout)

## 3.5 UI trace for `VerifyContact`

- Backend endpoint: `POST api/public/former-employee/verify-contact`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Declared parameters: `[FromBody] FormerEmployeePortalTokenRequest request`
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
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: verify-contact)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx` (matched: verify-contact)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: verify-contact)

## 3.6 UI trace for `GetCase`

- Backend endpoint: `GET api/public/former-employee/case`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
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
- `src/app/modules/employees/employeeServices.ts` (matched: case)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: case)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx` (matched: case)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: case)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: case)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: case)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: case)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: case)
- `src/app/modules/users/components/EmployeeOffboardingAction.tsx` (matched: case)
- `src/app/modules/users/components/ProfileUpdateModal.tsx` (matched: case)

## 3.7 UI trace for `CreateRequest`

- Backend endpoint: `POST api/public/former-employee/requests`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Declared parameters: `[FromBody] FormerEmployeePortalRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: requests)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: requests)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx` (matched: requests)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: requests)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: CreateRequest, requests)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: requests)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: requests)

## 3.8 UI trace for `DownloadDocument`

- Backend endpoint: `GET api/public/former-employee/documents/{evidenceId}`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Declared parameters: `string evidenceId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: documents)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: documents)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx` (matched: documents)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: documents)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: documents)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: documents)

## 3.9 UI trace for `PreviewDocument`

- Backend endpoint: `GET api/public/former-employee/documents/{evidenceId}/preview`
- Controller source: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Declared parameters: `string evidenceId`
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
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: preview)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: preview)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: preview)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: preview)
- `src/app/modules/users/components/ImageCropper.tsx` (matched: preview)

## 3.10 UI trace for `InitiateEmployeeExit`

- Backend endpoint: `POST api/User/InitiateEmployeeExit`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] InitiateEmployeeExitRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: InitiateEmployeeExit)
- `src/app/modules/users/components/EmployeeOffboardingAction.tsx` (matched: InitiateEmployeeExit)

## 3.11 UI trace for `SubmitOwnResignation`

- Backend endpoint: `POST api/User/SubmitOwnResignation`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] InitiateEmployeeExitRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: SubmitOwnResignation)
- `src/app/modules/users/components/ResignationAction.tsx` (matched: SubmitOwnResignation)

## 3.12 UI trace for `GetEmployeeExitCase`

- Backend endpoint: `GET api/User/EmployeeExit/{employeeUserId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string employeeUserId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: GetEmployeeExitCase, EmployeeExit)
- `src/app/modules/formerEmployeePortal/formerEmployeePortalService.ts` (matched: EmployeeExit)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: EmployeeExit)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: GetEmployeeExitCase, EmployeeExit)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: EmployeeExit)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: EmployeeExit)
- `src/app/modules/users/components/EmployeeOffboardingAction.tsx` (matched: GetEmployeeExitCase, EmployeeExit)
- `src/app/modules/users/components/ResignationAction.tsx` (matched: EmployeeExit)

## 3.13 UI trace for `GetOwnEmployeeExitHistory`

- Backend endpoint: `GET api/User/EmployeeExitHistory/{employeeUserId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string employeeUserId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: GetOwnEmployeeExitHistory, EmployeeExitHistory)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: GetOwnEmployeeExitHistory, EmployeeExitHistory)

## 3.14 UI trace for `GetEmployeeExitCaseForManagement`

- Backend endpoint: `GET api/User/EmployeeExitManagement/{employeeUserId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string employeeUserId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: GetEmployeeExitCaseForManagement, EmployeeExitManagement)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: GetEmployeeExitCaseForManagement, EmployeeExitManagement)
- `src/app/modules/users/components/EmployeeOffboardingAction.tsx` (matched: GetEmployeeExitCaseForManagement)

## 3.15 UI trace for `CompleteEmployeeExit`

- Backend endpoint: `POST api/User/CompleteEmployeeExit`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] CompleteEmployeeExitRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: CompleteEmployeeExit)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: CompleteEmployeeExit)

## 3.16 UI trace for `ReviewEmployeeExit`

- Backend endpoint: `POST api/User/ReviewEmployeeExit`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] ReviewEmployeeExitRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: ReviewEmployeeExit)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: ReviewEmployeeExit)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: ReviewEmployeeExit)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: ReviewEmployeeExit)

## 3.17 UI trace for `RespondToEmployeeExitDiscussion`

- Backend endpoint: `POST api/User/RespondToEmployeeExitDiscussion`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] RespondToEmployeeExitDiscussionRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: RespondToEmployeeExitDiscussion)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: RespondToEmployeeExitDiscussion)

## 3.18 UI trace for `RespondToEmployeeExitJointDiscussion`

- Backend endpoint: `POST api/User/RespondToEmployeeExitJointDiscussion`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] RespondToEmployeeExitJointDiscussionRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: RespondToEmployeeExitJointDiscussion)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: RespondToEmployeeExitJointDiscussion)

## 3.19 UI trace for `WithdrawOwnEmployeeExit`

- Backend endpoint: `POST api/User/WithdrawOwnEmployeeExit`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] CancelEmployeeExitRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: WithdrawOwnEmployeeExit)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: WithdrawOwnEmployeeExit)

## 3.20 UI trace for `DecideEmployeeExitWithdrawal`

- Backend endpoint: `POST api/User/DecideEmployeeExitWithdrawal`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] DecideEmployeeExitWithdrawalRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: DecideEmployeeExitWithdrawal)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: DecideEmployeeExitWithdrawal)

## 3.21 UI trace for `CancelEmployeeExit`

- Backend endpoint: `POST api/User/CancelEmployeeExit`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] CancelEmployeeExitRequest request`
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

## 3.22 UI trace for `UpdateEmployeeExitDate`

- Backend endpoint: `POST api/User/UpdateEmployeeExitDate`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpdateEmployeeExitDateRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: UpdateEmployeeExitDate)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: UpdateEmployeeExitDate)

## 3.23 UI trace for `RequestEmployeeExitNoticeAdjustment`

- Backend endpoint: `POST api/User/RequestEmployeeExitNoticeAdjustment`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] RequestEmployeeExitNoticeAdjustmentRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: RequestEmployeeExitNoticeAdjustment)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: RequestEmployeeExitNoticeAdjustment)

## 3.24 UI trace for `DecideEmployeeExitNoticeAdjustment`

- Backend endpoint: `POST api/User/DecideEmployeeExitNoticeAdjustment`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] DecideEmployeeExitNoticeAdjustmentRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: DecideEmployeeExitNoticeAdjustment)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: DecideEmployeeExitNoticeAdjustment)

## 3.25 UI trace for `DecideOwnEmployeeExitNoticeOffer`

- Backend endpoint: `POST api/User/DecideOwnEmployeeExitNoticeOffer`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] DecideOwnEmployeeExitNoticeOfferRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: DecideOwnEmployeeExitNoticeOffer)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: DecideOwnEmployeeExitNoticeOffer)

## 3.26 UI trace for `UpsertEmployeeExitPolicy`

- Backend endpoint: `POST api/User/EmployeeExitPolicy`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpsertEmployeeExitPolicyRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EmployeeExitPolicy)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: EmployeeExitPolicy)

## 3.27 UI trace for `GetEmployeeExitPolicy`

- Backend endpoint: `GET api/User/EmployeeExitPolicy/{policyId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string policyId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EmployeeExitPolicy)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: EmployeeExitPolicy)

## 3.28 UI trace for `GetEmployeeExitClearanceTemplate`

- Backend endpoint: `GET api/User/EmployeeExitClearanceTemplate/{templateId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string templateId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: GetEmployeeExitClearanceTemplate, EmployeeExitClearanceTemplate)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: GetEmployeeExitClearanceTemplate, EmployeeExitClearanceTemplate)

## 3.29 UI trace for `GetEmployeeExitPolicies`

- Backend endpoint: `GET api/User/EmployeeExitPolicies`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
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
- `src/app/modules/employees/employeeServices.ts` (matched: GetEmployeeExitPolicies, EmployeeExitPolicies)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: GetEmployeeExitPolicies, EmployeeExitPolicies)

## 3.30 UI trace for `GetEmployeeExitClearanceTemplates`

- Backend endpoint: `GET api/User/EmployeeExitClearanceTemplates`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
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
- `src/app/modules/employees/employeeServices.ts` (matched: GetEmployeeExitClearanceTemplates, EmployeeExitClearanceTemplates)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: GetEmployeeExitClearanceTemplates, EmployeeExitClearanceTemplates)

## 3.31 UI trace for `UpsertEmployeeExitClearanceTemplate`

- Backend endpoint: `POST api/User/EmployeeExitClearanceTemplate`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpsertEmployeeExitClearanceTemplateRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EmployeeExitClearanceTemplate)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: EmployeeExitClearanceTemplate)

## 3.32 UI trace for `DeleteEmployeeExitClearanceTemplate`

- Backend endpoint: `DELETE api/User/EmployeeExitClearanceTemplate/{templateId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string templateId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: DeleteEmployeeExitClearanceTemplate, EmployeeExitClearanceTemplate)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: DeleteEmployeeExitClearanceTemplate, EmployeeExitClearanceTemplate)

## 3.33 UI trace for `ApproveEmployeeFinalSettlement`

- Backend endpoint: `POST api/User/ApproveEmployeeFinalSettlement`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] ApproveEmployeeFinalSettlementRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: ApproveEmployeeFinalSettlement)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: ApproveEmployeeFinalSettlement)

## 3.34 UI trace for `UpdateEmployeeFinalSettlementPayment`

- Backend endpoint: `POST api/User/UpdateEmployeeFinalSettlementPayment`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpdateEmployeeFinalSettlementPaymentRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: UpdateEmployeeFinalSettlementPayment)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: UpdateEmployeeFinalSettlementPayment)

## 3.35 UI trace for `CalculateEmployeeFinalSettlement`

- Backend endpoint: `POST api/User/CalculateEmployeeFinalSettlement`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] CalculateEmployeeFinalSettlementRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: CalculateEmployeeFinalSettlement)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: CalculateEmployeeFinalSettlement)

## 3.36 UI trace for `MarkEmployeeFinalSettlementPaid`

- Backend endpoint: `POST api/User/MarkEmployeeFinalSettlementPaid`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] MarkEmployeeFinalSettlementPaidRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: MarkEmployeeFinalSettlementPaid)

## 3.37 UI trace for `RecordEmployeeExitInterview`

- Backend endpoint: `POST api/User/RecordEmployeeExitInterview`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] RecordEmployeeExitInterviewRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: RecordEmployeeExitInterview)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: RecordEmployeeExitInterview)

## 3.38 UI trace for `UpsertEmployeeExitReassignment`

- Backend endpoint: `POST api/User/EmployeeExitReassignment`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpsertEmployeeExitReassignmentRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: UpsertEmployeeExitReassignment, EmployeeExitReassignment)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: UpsertEmployeeExitReassignment, EmployeeExitReassignment)

## 3.39 UI trace for `GenerateEmployeeExitDocument`

- Backend endpoint: `POST api/User/GenerateEmployeeExitDocument`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] GenerateEmployeeExitDocumentRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: GenerateEmployeeExitDocument)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: GenerateEmployeeExitDocument)

## 3.40 UI trace for `IssueEmployeeExitDocument`

- Backend endpoint: `POST api/User/IssueEmployeeExitDocument`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] IssueEmployeeExitDocumentRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: IssueEmployeeExitDocument)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: IssueEmployeeExitDocument)

## 3.41 UI trace for `CompleteEmployeeExitClearance`

- Backend endpoint: `POST api/User/CompleteEmployeeExitClearance`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] CompleteEmployeeExitClearanceRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: CompleteEmployeeExitClearance)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: CompleteEmployeeExitClearance)

## 3.42 UI trace for `UpdateEmployeeExitClearanceStatus`

- Backend endpoint: `POST api/User/UpdateEmployeeExitClearanceStatus`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpdateEmployeeExitClearanceStatusRequest request`
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

## 3.43 UI trace for `WaiveEmployeeExitClearance`

- Backend endpoint: `POST api/User/WaiveEmployeeExitClearance`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] CompleteEmployeeExitClearanceRequest request`
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

## 3.44 UI trace for `UpsertEmployeeExitHandoverTask`

- Backend endpoint: `POST api/User/EmployeeExitHandoverTask`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpsertEmployeeExitHandoverTaskRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EmployeeExitHandoverTask)

## 3.45 UI trace for `ReviewEmployeeExitHandoverTask`

- Backend endpoint: `POST api/User/ReviewEmployeeExitHandoverTask`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] ReviewEmployeeExitHandoverTaskRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: ReviewEmployeeExitHandoverTask)

## 3.46 UI trace for `UpsertEmployeeExitAsset`

- Backend endpoint: `POST api/User/EmployeeExitAsset`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpsertEmployeeExitAssetRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: UpsertEmployeeExitAsset, EmployeeExitAsset)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: UpsertEmployeeExitAsset, EmployeeExitAsset)

## 3.47 UI trace for `UpsertEmployeeExitAccessItem`

- Backend endpoint: `POST api/User/EmployeeExitAccessItem`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpsertEmployeeExitAccessItemRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EmployeeExitAccessItem)

## 3.48 UI trace for `UpsertEmployeeExitFinanceItem`

- Backend endpoint: `POST api/User/EmployeeExitFinanceItem`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpsertEmployeeExitFinanceItemRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EmployeeExitFinanceItem)

## 3.49 UI trace for `UpsertEmployeeExitBenefitItem`

- Backend endpoint: `POST api/User/EmployeeExitBenefitItem`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpsertEmployeeExitBenefitItemRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EmployeeExitBenefitItem)

## 3.50 UI trace for `AddEmployeeExitEvidence`

- Backend endpoint: `POST api/User/EmployeeExitEvidence`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] AddEmployeeExitEvidenceRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EmployeeExitEvidence)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: EmployeeExitEvidence)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: EmployeeExitEvidence)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: EmployeeExitEvidence)

## 3.51 UI trace for `UploadEmployeeExitEvidence`

- Backend endpoint: `POST api/User/EmployeeExitEvidenceUpload`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromForm] string employeeExitId, [FromForm] string category, [FromForm] string visibility, [FromForm] IFormFile file`
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
- `src/app/modules/employees/employeeServices.ts` (matched: UploadEmployeeExitEvidence, EmployeeExitEvidenceUpload)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: UploadEmployeeExitEvidence)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: UploadEmployeeExitEvidence)

## 3.52 UI trace for `DownloadEmployeeExitEvidence`

- Backend endpoint: `GET api/User/EmployeeExitEvidence/{employeeExitId}/{evidenceId}/download`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string employeeExitId, string evidenceId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: DownloadEmployeeExitEvidence, download)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalExperience.tsx` (matched: download)
- `src/app/modules/formerEmployeePortal/FormerEmployeePortalPage.tsx` (matched: download)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: DownloadEmployeeExitEvidence, download)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: DownloadEmployeeExitEvidence, download)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: DownloadEmployeeExitEvidence, download)
- `src/app/modules/users/components/ProfileCard.tsx` (matched: download)

## 3.53 UI trace for `VerifyEmployeeExitEvidence`

- Backend endpoint: `POST api/User/VerifyEmployeeExitEvidence`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] VerifyEmployeeExitEvidenceRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: VerifyEmployeeExitEvidence)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: VerifyEmployeeExitEvidence)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: VerifyEmployeeExitEvidence)

## 3.54 UI trace for `SupersedeEmployeeExitEvidence`

- Backend endpoint: `POST api/User/SupersedeEmployeeExitEvidence`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] SupersedeEmployeeExitEvidenceRequest request`
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

## 3.55 UI trace for `UpdateEmployeeExitPostExitContact`

- Backend endpoint: `POST api/User/EmployeeExitPostExitContact`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] UpdateEmployeeExitPostExitContactRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: UpdateEmployeeExitPostExitContact, EmployeeExitPostExitContact)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: UpdateEmployeeExitPostExitContact, EmployeeExitPostExitContact)

## 3.56 UI trace for `AcknowledgeEmployeeExit`

- Backend endpoint: `POST api/User/AcknowledgeEmployeeExit`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] AcknowledgeEmployeeExitRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: AcknowledgeEmployeeExit)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: AcknowledgeEmployeeExit)

## 3.57 UI trace for `CreateEmployeeExitFormerEmployeeRequest`

- Backend endpoint: `POST api/User/EmployeeExitFormerEmployeeRequest`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] CreateEmployeeExitFormerEmployeeRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: CreateEmployeeExitFormerEmployeeRequest, EmployeeExitFormerEmployeeRequest)
- `src/app/modules/users/components/EmployeeExitCaseCard.tsx` (matched: CreateEmployeeExitFormerEmployeeRequest, EmployeeExitFormerEmployeeRequest)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: EmployeeExitFormerEmployeeRequest)

## 3.58 UI trace for `ResolveEmployeeExitFormerEmployeeRequest`

- Backend endpoint: `POST api/User/EmployeeExitFormerEmployeeRequestResolve`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] ResolveEmployeeExitFormerEmployeeRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: ResolveEmployeeExitFormerEmployeeRequest, EmployeeExitFormerEmployeeRequestResolve)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: ResolveEmployeeExitFormerEmployeeRequest)

## 3.59 UI trace for `CreateFormerEmployeePortalInvite`

- Backend endpoint: `POST api/User/EmployeeExitFormerEmployeePortalInvite`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] CreateFormerEmployeePortalInviteRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: CreateFormerEmployeePortalInvite, EmployeeExitFormerEmployeePortalInvite)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: CreateFormerEmployeePortalInvite)

## 3.60 UI trace for `RevokeFormerEmployeePortalAccess`

- Backend endpoint: `POST api/User/EmployeeExitFormerEmployeePortalRevoke`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] RevokeFormerEmployeePortalAccessRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: RevokeFormerEmployeePortalAccess, EmployeeExitFormerEmployeePortalRevoke)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: RevokeFormerEmployeePortalAccess)

## 3.61 UI trace for `SetFormerEmployeePortalAccess`

- Backend endpoint: `POST api/User/EmployeeExitFormerEmployeePortalAccess`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] SetFormerEmployeePortalAccessRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: SetFormerEmployeePortalAccess, EmployeeExitFormerEmployeePortalAccess)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: SetFormerEmployeePortalAccess)

## 3.62 UI trace for `SetCompanyFormerEmployeePortalAccess`

- Backend endpoint: `POST api/User/CompanyFormerEmployeePortalAccess`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] SetCompanyFormerEmployeePortalRequest request`
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

## 3.63 UI trace for `RejoinEmployee`

- Backend endpoint: `POST api/User/RejoinEmployee`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] RejoinEmployeeRequest request`
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

## 3.64 UI trace for `ExceptionalCompleteEmployeeExit`

- Backend endpoint: `POST api/User/ExceptionalCompleteEmployeeExit`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] ExceptionalCompleteEmployeeExitRequest request`
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
- `src/app/modules/employees/employeeServices.ts` (matched: ExceptionalCompleteEmployeeExit)
- `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx` (matched: ExceptionalCompleteEmployeeExit)

## 4. Form and Interaction Review Rules

- Fields: derive names, types, defaults and optionality from form code and request DTO together.
- Validation: document frontend validation separately from backend rejection.
- Filters/pagination: establish whether state is local or server-backed.
- Retry: confirm whether query libraries retry reads or mutations.
- Accessibility: preserve labels, keyboard operation, focus return and tooltip semantics.
- Permissions: use actual guards/permitted actions; do not introduce role-name guesses.

