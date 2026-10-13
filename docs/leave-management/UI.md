# Leave Management UI Documentation

**Last verified against code:** 2026-09-02

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## 1. Frontend Source Map

### 1.1 `src/app/modules/leaves/components/AddLeavePolicy.tsx`

- Source lines: 733
- Extracted symbols: AttendanceStatusOption, Props, renderInfoIcon, handleCreateNewPolicy, handleUpdateLeavePolicy, message, handleListOpen, getAllEmployeesList, handleClose, paperSx, halfDayWfhEnabled, applicableTo, policy, policyType, label, limitTags, ids, Leave, Active, id, selectedOptionId, optionValues, sx
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/leaves/components/AddLeaveTypes.tsx`

- Source lines: 165
- Extracted symbols: Props, handleLeaveTypeSubmit
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/leaves/components/EditEmployeePoliciesForm.tsx`

- Source lines: 341
- Extracted symbols: Props, InitialValueType, GetLeaveBalanceValue, allocation, policyLimit, limit, eligible, handleLeaveBalanceSubmission, message, statusMessage, changedLeaveBalances, original, selectedLeave, handleLeavePolicyChange, existingIndex, exist, handleLeaveBalanceChange, label
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/leaves/components/EmployeeLeaveTypeMonthlyChart.tsx`

- Source lines: 88
- Extracted symbols: Props, EmployeeLeaveTypeMonthlyChart, theme, targetYear, start, end, res, items, map, policy, m, lbls, seriesData
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/leaves/components/EmployeesLeaveBalance.tsx`

- Source lines: 351
- Extracted symbols: EmployeesLeaveBalance, defaultLang, debouncedValue, open, handleClickOptions, handleFilterClick, handleClose, setRowPerPage, setPage, handleRefresh, handleSelectingPolicies, getLeaveBalanceFunc, byEmployeeId, allocation
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/leaves/components/FilterPolicySearch.tsx`

- Source lines: 129
- Extracted symbols: Props, size, value
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/leaves/components/LeaveManagementTabs.tsx`

- Source lines: 81
- Extracted symbols: LeaveManagementTabs, navigate
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/leaves/components/LeavePolicies.tsx`

- Source lines: 127
- Extracted symbols: LeavePolicies, handleClose, handleRefresh, handleEdit
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/leaves/components/LeaveRequest.tsx`

- Source lines: 516
- Extracted symbols: LeaveRequest, modulePermissions, canCreateForEmployee, appliedLeaveRequestFilter, fetchLeaveRequest, handlePageChange, handleRowPerPageChange, handleRefresh, CreateEmployeeLeaveModalProps, CreateEmployeeLeaveModal, loadEmployees, continueToApplication, initialValues, handleClose, PropsType, LeaveApprovalModal, isRejection, friendlyMessage, handleLeaveStatusUpdate, responseMessage, error, handleChange, type
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/leaves/components/LeaveRequestFilter.tsx`

- Source lines: 132
- Extracted symbols: LeaveRequestFilter, dispatch, appliedLeaveRequestFilter, debouncedValues, statusValue, handleResetFilter
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/leaves/components/LeaveRequestSummaryChart.tsx`

- Source lines: 97
- Extracted symbols: CreateChartDataType, LeaveRequestSummaryChart, intl
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/leaves/components/MonthlyLeaveTakenLineChart.tsx`

- Source lines: 67
- Extracted symbols: MonthlyLeaveTakenLineChart, theme, monthShort
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/modules/leaves/components/Summary.tsx`

- Source lines: 43
- Extracted symbols: createData, rows
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/app/modules/leaves/LeaveManagement.tsx`

- Source lines: 46
- Extracted symbols: intl
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.15 `src/app/modules/leaves/leaveServices.ts`

- Source lines: 248
- Extracted symbols: createUpdateLeaveType, postRequest, getLeavePolicies, getLeaveTypes, getRequest, createLeavePolicy, updateLeavePolicy, getLeaveBalance, serializeLeaveRequestPayload, createLeaveRequest, createEmployeeLeaveRequest, createMyProfileLeaveRequest, WorkFromHomeRequestPayload, createWorkFromHomeRequest, WorkFromHomeEmployeeAllocation, WorkFromHomeEmployeeAllocationsResponse, getMySelfServiceLeavePolicies, getWorkFromHomeEmployeeAllocations, saveWorkFromHomeEmployeeAllocation
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.16 `src/app/modules/leaves/model.ts`

- Source lines: 214
- Extracted symbols: LeaveType, RequestLeaveModel, MyLeaveRequestModel, LeaveRequestModel, LeaveRequestsFilterModel, GetLeaveRequestPayLoad, LeaveBalanceFilter, LeaveTypeBalance, LeaveBalanceType, LeaveBalance, EmployeeLeaveTypeBalance, LeaveRequestApprovalPayload, LeaveRequestSummary, MonthlyLeaveTakenSummary, LeavePolicyModel, EmployeeLeaveBalanceFilter, EmployeeLeaveInfo, EmployeeLeaveBalanceData, GetLeaveTypeResponse, GetLeaveBalanceResponse, GetLeaveRequestsResponse, GetMyLeaveRequestsResponse, GetLeaveRequestsSummary, GetEmployeeLeaveBalanceResponse
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.17 `src/app/routing/LeaveManagementRoutes.tsx`

- Source lines: 21
- Extracted symbols: LeaveManagementRoutes
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `CreateNewLeavePolicy` | `POST api/LeaveManagement/CreateLeavePolicy` | Search service functions and component event handlers for this route/action. |
| 2 | `UpdateLeavePolicy` | `POST api/LeaveManagement/UpdateLeavePolicy` | Search service functions and component event handlers for this route/action. |
| 3 | `GetAllLeavePolicies` | `POST api/LeaveManagement/GetAllLeavePolicies` | Search service functions and component event handlers for this route/action. |
| 4 | `GetMySelfServicePolicies` | `GET api/LeaveManagement/me/policies` | Search service functions and component event handlers for this route/action. |
| 5 | `GetEmployeeLeaveBalance` | `GET api/LeaveManagement/GetEmployeeLeaveBalance/{employeeId}` | Search service functions and component event handlers for this route/action. |
| 6 | `GetMyLeaveBalance` | `GET api/LeaveManagement/me/balances` | Search service functions and component event handlers for this route/action. |
| 7 | `CreateLeaveRequest` | `POST api/LeaveManagement/CreateLeaveRequest` | Search service functions and component event handlers for this route/action. |
| 8 | `CreateMyLeaveRequest` | `POST api/LeaveManagement/me/requests` | Search service functions and component event handlers for this route/action. |
| 9 | `CreateEmployeeLeaveRequest` | `POST api/LeaveManagement/CreateEmployeeLeaveRequest/{employeeId}` | Search service functions and component event handlers for this route/action. |
| 10 | `UpdateLeaveRequest` | `POST api/LeaveManagement/UpdateLeaveRequest` | Search service functions and component event handlers for this route/action. |
| 11 | `UpdateMyLeaveRequest` | `PATCH api/LeaveManagement/me/requests` | Search service functions and component event handlers for this route/action. |
| 12 | `GetLeaveRequest` | `POST api/LeaveManagement/GetLeaveRequests` | Search service functions and component event handlers for this route/action. |
| 13 | `GetMyLeaveRequests` | `POST api/LeaveManagement/GetMyLeaveRequests` | Search service functions and component event handlers for this route/action. |
| 14 | `GetMyProfileLeaveRequests` | `POST api/LeaveManagement/me/requests/search` | Search service functions and component event handlers for this route/action. |
| 15 | `DeleteLeaveRequest` | `DELETE api/LeaveManagement/DeleteLeaveRequest/{leaveRequestId}` | Search service functions and component event handlers for this route/action. |
| 16 | `DeleteMyLeaveRequest` | `DELETE api/LeaveManagement/me/requests/{leaveRequestId}` | Search service functions and component event handlers for this route/action. |
| 17 | `UpdateLeaveRequestStatus` | `PATCH api/LeaveManagement/LeaveRequest/{leaveRequestId}/Status` | Search service functions and component event handlers for this route/action. |
| 18 | `GetLeaveRequestSummary` | `GET api/LeaveManagement/GetLeaveRequestSummary` | Search service functions and component event handlers for this route/action. |
| 19 | `GetMonthlyTakenLeaveSummary` | `GET api/LeaveManagement/GetMonthlyTakenLeaveSummary/{year}` | Search service functions and component event handlers for this route/action. |
| 20 | `UpdateEmpLeaveBalance` | `POST api/LeaveManagement/UpdateEmployeeLeaveBalance` | Search service functions and component event handlers for this route/action. |
| 21 | `GetAllEmployeeLeaveBalances` | `POST api/LeaveManagement/GetAllEmployeeLeaveBalances` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `CreateNewLeavePolicy`

- Backend endpoint: `POST api/LeaveManagement/CreateLeavePolicy`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] LeavePolicyRequest model`
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
- `src/app/modules/leaves/components/AddLeavePolicy.tsx` (matched: CreateLeavePolicy)
- `src/app/modules/leaves/leaveServices.ts` (matched: CreateLeavePolicy)

## 3.2 UI trace for `UpdateLeavePolicy`

- Backend endpoint: `POST api/LeaveManagement/UpdateLeavePolicy`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] UpdateLeavePolicyRequest model`
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
- `src/app/modules/leaves/components/AddLeavePolicy.tsx` (matched: UpdateLeavePolicy)
- `src/app/modules/leaves/leaveServices.ts` (matched: UpdateLeavePolicy)

## 3.3 UI trace for `GetAllLeavePolicies`

- Backend endpoint: `POST api/LeaveManagement/GetAllLeavePolicies`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromQuery] bool? status`
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

## 3.4 UI trace for `GetMySelfServicePolicies`

- Backend endpoint: `GET api/LeaveManagement/me/policies`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
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
- `src/app/modules/leaves/components/AddLeavePolicy.tsx` (matched: policies)
- `src/app/modules/leaves/components/EditEmployeePoliciesForm.tsx` (matched: policies)
- `src/app/modules/leaves/components/EmployeesLeaveBalance.tsx` (matched: policies)
- `src/app/modules/leaves/components/FilterPolicySearch.tsx` (matched: policies)
- `src/app/modules/leaves/components/LeaveManagementTabs.tsx` (matched: policies)
- `src/app/modules/leaves/components/LeavePolicies.tsx` (matched: policies)
- `src/app/modules/leaves/leaveServices.ts` (matched: policies)
- `src/app/modules/leaves/model.ts` (matched: policies)
- `src/app/routing/LeaveManagementRoutes.tsx` (matched: policies)

## 3.5 UI trace for `GetEmployeeLeaveBalance`

- Backend endpoint: `GET api/LeaveManagement/GetEmployeeLeaveBalance/{employeeId}`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `string employeeId`
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
- `src/app/modules/leaves/components/LeaveRequest.tsx` (matched: GetEmployeeLeaveBalance)
- `src/app/modules/leaves/leaveServices.ts` (matched: GetEmployeeLeaveBalance)
- `src/app/modules/leaves/model.ts` (matched: GetEmployeeLeaveBalance)

## 3.6 UI trace for `GetMyLeaveBalance`

- Backend endpoint: `GET api/LeaveManagement/me/balances`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
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
- `src/app/modules/leaves/components/EditEmployeePoliciesForm.tsx` (matched: balances)
- `src/app/modules/leaves/components/EmployeesLeaveBalance.tsx` (matched: balances)
- `src/app/modules/leaves/components/LeaveRequest.tsx` (matched: balances)
- `src/app/modules/leaves/leaveServices.ts` (matched: balances)
- `src/app/modules/leaves/model.ts` (matched: balances)

## 3.7 UI trace for `CreateLeaveRequest`

- Backend endpoint: `POST api/LeaveManagement/CreateLeaveRequest`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] LeaveRequestDto leaveRequest`
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
- `src/app/modules/leaves/leaveServices.ts` (matched: CreateLeaveRequest)

## 3.8 UI trace for `CreateMyLeaveRequest`

- Backend endpoint: `POST api/LeaveManagement/me/requests`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] LeaveRequestDto leaveRequest`
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
- `src/app/modules/leaves/components/AddLeavePolicy.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveManagementTabs.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequest.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequestFilter.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequestSummaryChart.tsx` (matched: requests)
- `src/app/modules/leaves/LeaveManagement.tsx` (matched: requests)
- `src/app/modules/leaves/leaveServices.ts` (matched: requests)
- `src/app/modules/leaves/model.ts` (matched: requests)

## 3.9 UI trace for `CreateEmployeeLeaveRequest`

- Backend endpoint: `POST api/LeaveManagement/CreateEmployeeLeaveRequest/{employeeId}`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `string employeeId, [FromBody] LeaveRequestDto leaveRequest`
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
- `src/app/modules/leaves/leaveServices.ts` (matched: CreateEmployeeLeaveRequest)

## 3.10 UI trace for `UpdateLeaveRequest`

- Backend endpoint: `POST api/LeaveManagement/UpdateLeaveRequest`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] UpdateLeaveRequestDto leaveRequest`
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
- `src/app/modules/leaves/components/LeaveRequest.tsx` (matched: UpdateLeaveRequest)
- `src/app/modules/leaves/leaveServices.ts` (matched: UpdateLeaveRequest)

## 3.11 UI trace for `UpdateMyLeaveRequest`

- Backend endpoint: `PATCH api/LeaveManagement/me/requests`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] UpdateLeaveRequestDto leaveRequest`
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
- `src/app/modules/leaves/components/AddLeavePolicy.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveManagementTabs.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequest.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequestFilter.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequestSummaryChart.tsx` (matched: requests)
- `src/app/modules/leaves/LeaveManagement.tsx` (matched: requests)
- `src/app/modules/leaves/leaveServices.ts` (matched: requests)
- `src/app/modules/leaves/model.ts` (matched: requests)

## 3.12 UI trace for `GetLeaveRequest`

- Backend endpoint: `POST api/LeaveManagement/GetLeaveRequests`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `LeaveRequestFilter? leaveFilter`
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
- `src/app/modules/leaves/components/EmployeeLeaveTypeMonthlyChart.tsx` (matched: GetLeaveRequest)
- `src/app/modules/leaves/components/LeaveRequest.tsx` (matched: GetLeaveRequest)
- `src/app/modules/leaves/components/LeaveRequestSummaryChart.tsx` (matched: GetLeaveRequest, GetLeaveRequests)
- `src/app/modules/leaves/leaveServices.ts` (matched: GetLeaveRequest, GetLeaveRequests)
- `src/app/modules/leaves/model.ts` (matched: GetLeaveRequest, GetLeaveRequests)

## 3.13 UI trace for `GetMyLeaveRequests`

- Backend endpoint: `POST api/LeaveManagement/GetMyLeaveRequests`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] EmpLeaveRequestFilter leaveFilter`
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
- `src/app/modules/leaves/leaveServices.ts` (matched: GetMyLeaveRequests)
- `src/app/modules/leaves/model.ts` (matched: GetMyLeaveRequests)

## 3.14 UI trace for `GetMyProfileLeaveRequests`

- Backend endpoint: `POST api/LeaveManagement/me/requests/search`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] EmpLeaveRequestFilter leaveFilter`
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
- `src/app/modules/leaves/components/EmployeesLeaveBalance.tsx` (matched: search)
- `src/app/modules/leaves/components/FilterPolicySearch.tsx` (matched: search)
- `src/app/modules/leaves/leaveServices.ts` (matched: search)

## 3.15 UI trace for `DeleteLeaveRequest`

- Backend endpoint: `DELETE api/LeaveManagement/DeleteLeaveRequest/{leaveRequestId}`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `string leaveRequestId`
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
- `src/app/modules/leaves/leaveServices.ts` (matched: DeleteLeaveRequest)

## 3.16 UI trace for `DeleteMyLeaveRequest`

- Backend endpoint: `DELETE api/LeaveManagement/me/requests/{leaveRequestId}`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `string leaveRequestId`
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
- `src/app/modules/leaves/components/AddLeavePolicy.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveManagementTabs.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequest.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequestFilter.tsx` (matched: requests)
- `src/app/modules/leaves/components/LeaveRequestSummaryChart.tsx` (matched: requests)
- `src/app/modules/leaves/LeaveManagement.tsx` (matched: requests)
- `src/app/modules/leaves/leaveServices.ts` (matched: requests)
- `src/app/modules/leaves/model.ts` (matched: requests)

## 3.17 UI trace for `UpdateLeaveRequestStatus`

- Backend endpoint: `PATCH api/LeaveManagement/LeaveRequest/{leaveRequestId}/Status`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `string leaveRequestId, [FromBody] LeaveRequestUpdateDto model`
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
- `src/app/modules/leaves/components/AddLeavePolicy.tsx` (matched: Status)
- `src/app/modules/leaves/components/AddLeaveTypes.tsx` (matched: Status)
- `src/app/modules/leaves/components/EditEmployeePoliciesForm.tsx` (matched: Status)
- `src/app/modules/leaves/components/EmployeeLeaveTypeMonthlyChart.tsx` (matched: Status)
- `src/app/modules/leaves/components/LeavePolicies.tsx` (matched: Status)
- `src/app/modules/leaves/components/LeaveRequest.tsx` (matched: UpdateLeaveRequestStatus, Status)
- `src/app/modules/leaves/components/LeaveRequestFilter.tsx` (matched: Status)
- `src/app/modules/leaves/components/LeaveRequestSummaryChart.tsx` (matched: Status)
- `src/app/modules/leaves/leaveServices.ts` (matched: UpdateLeaveRequestStatus, Status)
- `src/app/modules/leaves/model.ts` (matched: Status)

## 3.18 UI trace for `GetLeaveRequestSummary`

- Backend endpoint: `GET api/LeaveManagement/GetLeaveRequestSummary`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
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
- `src/app/modules/leaves/components/LeaveRequestSummaryChart.tsx` (matched: GetLeaveRequestSummary)
- `src/app/modules/leaves/leaveServices.ts` (matched: GetLeaveRequestSummary)

## 3.19 UI trace for `GetMonthlyTakenLeaveSummary`

- Backend endpoint: `GET api/LeaveManagement/GetMonthlyTakenLeaveSummary/{year}`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `int? year`
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

## 3.20 UI trace for `UpdateEmpLeaveBalance`

- Backend endpoint: `POST api/LeaveManagement/UpdateEmployeeLeaveBalance`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `List<LeaveBalanceRequestDto> LeaveBalanceRequestDto`
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
- `src/app/modules/leaves/components/EditEmployeePoliciesForm.tsx` (matched: UpdateEmployeeLeaveBalance)
- `src/app/modules/leaves/leaveServices.ts` (matched: UpdateEmployeeLeaveBalance)

## 3.21 UI trace for `GetAllEmployeeLeaveBalances`

- Backend endpoint: `POST api/LeaveManagement/GetAllEmployeeLeaveBalances`
- Controller source: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Declared parameters: `[FromBody] LeaveBalanceFilter filter`
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

