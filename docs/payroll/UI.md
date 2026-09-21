# Payroll and Salary UI Documentation

**Last verified against code:** 2026-09-02

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## 1. Frontend Source Map

### 1.1 `src/app/modules/payroll/AddUpdateEmployeePayRoll.tsx`

- Source lines: 195
- Extracted symbols: PayRollFormValues, PropsType, FormFieldProps, sections, AddUpdateEmployeePayRoll, handleAddUpdate, paperSx, fields
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/payroll/components/payrollActionButtonSx.ts`

- Source lines: 8
- Extracted symbols: payrollActionButtonSx
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/payroll/components/PayRollTable.tsx`

- Source lines: 336
- Extracted symbols: Column, amountColumns, TableProps, PayRollTable, userModulePermissions, hasEditPermission, handlePreviewPaySlip, response, contentType, errorData, errorJson, handleClosePreview, processedData, grossPay, totalDeductions, netPay, visibleRows, tabIndex, hasPayRoll, isProcessed
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/payroll/components/ProcessPayrollModal.tsx`

- Source lines: 148
- Extracted symbols: Props, ProcessPayrollModal, availableIds, allSelected, partiallySelected, toggleAll, toggleEmployee
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/payroll/components/UploadPayRollData.tsx`

- Source lines: 377
- Extracted symbols: ValidFileExtension, requiredHeaders, moneyHeaders, SAMPLE_FILE_URL, KeyMappingType, PropsType, normalizeHeader, value, readWorkSheetObject, workSheet, HeaderValidationResult, validateHeaderRow, sheetData, row, normalizedCells, missingHeaders, roundSheetMoneyColumns, range, headerCell, cell, rounded, mapExcelRowtoPayLoad, targetKey, UploadPayRollData, inputRef, handleCloseModal, validateWorkSheet, sheet, handleWorkSheetChange, handleFileChange
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/payroll/models.ts`

- Source lines: 52
- Extracted symbols: PayRollDataModel, PayRollRequestModel, PayslipPayload, EmployeePayslipPayload, PayRollResponseModel, AddUpdatePayRollPayload, ProcessPayrollMonthPayload, GetPayRollResponseModel
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/payroll/PayRollManagement.tsx`

- Source lines: 281
- Extracted symbols: PayRollManagement, userModulePermissions, hasCreatePermission, hasEditPermission, payrollRows, processableRows, isMonthProcessed, handleDateChange, handleSearchChange, handleRowClick, handlePageRefresh, handleProcessMonth, handleModalClose, sx, placement
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/payroll/services.ts`

- Source lines: 66
- Extracted symbols: GetPayRollPayload, uploadEmpPayRollData, postRequest, generatePaySlip, generateEmployeePaySlip, getEmployeePayRollData, processPayrollMonth, addUpdateEmployeePayRoll
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/payrollsettings/components/AddUpdateEmployeeSalary.tsx`

- Source lines: 84
- Extracted symbols: PropsType, AddUpdateEmployeeSalary, formRef, policyRef, handleUserUpdate, handleSave
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/payrollsettings/components/SalaryTable.tsx`

- Source lines: 178
- Extracted symbols: Column, amountColumns, TableProps, SalaryTable, userModulePermissions, hasEditPermission, rows, tabIndex
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/payrollsettings/model.ts`

- Source lines: 20
- Extracted symbols: CompanySalaryResponseModel, CompanySalaryFilterPayload, ProcessPayrollPayload
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/payrollsettings/PayrollSettings.tsx`

- Source lines: 311
- Extracted symbols: AttendanceMonthLockStatus, AttendanceLockBlockingIssue, AttendanceMonthLockValidationResult, userModulesPermissions, hasProcessPermission, handleSearchChange, handlePageRefresh, handleRowClick, handleModalClose, handleProcessMonthChange, cancelled, employeeResponse, handleProcessSalary, handleValidateAndLock, selectedMonth, currentMonth, openAttendanceCell, sx
- React/data hooks found: useState, useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/modules/payrollsettings/services.ts`

- Source lines: 22
- Extracted symbols: getCompanySalaries, postRequest, processPayroll
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `GeneratePayRollForMonthly` | `POST api/AutoPayroll/GeneratePayRollMonthly` | Search service functions and component event handlers for this route/action. |
| 2 | `GetEmpPayRollData` | `POST api/PayRoll/GetEmpPayRollData` | Search service functions and component event handlers for this route/action. |
| 3 | `GetSalarySlip` | `POST api/PayRoll/GeneratePaySlip` | Search service functions and component event handlers for this route/action. |
| 4 | `GetEmployeeSalarySlip` | `POST api/PayRoll/GenerateEmployeePaySlip` | Search service functions and component event handlers for this route/action. |
| 5 | `UploadPayloadData` | `POST api/PayRoll/UploadPayloadData` | Search service functions and component event handlers for this route/action. |
| 6 | `AddUpdatePayRoll` | `POST api/PayRoll/AddUpdatePayRoll` | Search service functions and component event handlers for this route/action. |
| 7 | `ProcessPayrollMonth` | `POST api/PayRoll/ProcessPayrollMonth` | Search service functions and component event handlers for this route/action. |
| 8 | `Get` | `GET api/payroll/divisor-policy` | Search service functions and component event handlers for this route/action. |
| 9 | `Save` | `POST api/payroll/divisor-policy` | Search service functions and component event handlers for this route/action. |
| 10 | `CreateSalary` | `POST api/Salary/UpdateSalary` | Search service functions and component event handlers for this route/action. |
| 11 | `GetActiveSalary` | `GET api/Salary/active/{userId}` | Search service functions and component event handlers for this route/action. |
| 12 | `GetSalaryHistory` | `GET api/Salary/history/{userId}` | Search service functions and component event handlers for this route/action. |
| 13 | `GetCompanySalaries` | `POST api/Salary/GetCompanySalaries` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `GeneratePayRollForMonthly`

- Backend endpoint: `POST api/AutoPayroll/GeneratePayRollMonthly`
- Controller source: `Codeji.CMS.API/Controllers/AutoPayrollController.cs`
- Declared parameters: `[FromBody] ProcessPayrollRequestDto model`
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

## 3.2 UI trace for `GetEmpPayRollData`

- Backend endpoint: `POST api/PayRoll/GetEmpPayRollData`
- Controller source: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Declared parameters: `[FromBody] GetEmpPayRollRequestDto payload`
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

## 3.3 UI trace for `GetSalarySlip`

- Backend endpoint: `POST api/PayRoll/GeneratePaySlip`
- Controller source: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Declared parameters: `PayslipRequestDto model`
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
- `src/app/modules/payroll/services.ts` (matched: GeneratePaySlip)

## 3.4 UI trace for `GetEmployeeSalarySlip`

- Backend endpoint: `POST api/PayRoll/GenerateEmployeePaySlip`
- Controller source: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Declared parameters: `EmployeePayslipRequestDto model`
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
- `src/app/modules/payroll/components/PayRollTable.tsx` (matched: GenerateEmployeePaySlip)
- `src/app/modules/payroll/services.ts` (matched: GenerateEmployeePaySlip)

## 3.5 UI trace for `UploadPayloadData`

- Backend endpoint: `POST api/PayRoll/UploadPayloadData`
- Controller source: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Declared parameters: `[FromBody] EmplyeePayRollRequestDto model`
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

## 3.6 UI trace for `AddUpdatePayRoll`

- Backend endpoint: `POST api/PayRoll/AddUpdatePayRoll`
- Controller source: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Declared parameters: `[FromBody] AddUpdatePayRollRequestDto model`
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
- `src/app/modules/payroll/models.ts` (matched: AddUpdatePayRoll)
- `src/app/modules/payroll/services.ts` (matched: AddUpdatePayRoll)

## 3.7 UI trace for `ProcessPayrollMonth`

- Backend endpoint: `POST api/PayRoll/ProcessPayrollMonth`
- Controller source: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Declared parameters: `[FromBody] ProcessPayrollRequestDto model`
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
- `src/app/modules/payroll/models.ts` (matched: ProcessPayrollMonth)
- `src/app/modules/payroll/PayRollManagement.tsx` (matched: ProcessPayrollMonth)
- `src/app/modules/payroll/services.ts` (matched: ProcessPayrollMonth)

## 3.8 UI trace for `Get`

- Backend endpoint: `GET api/payroll/divisor-policy`
- Controller source: `Codeji.CMS.API/Controllers/PayrollDivisorPolicyController.cs`
- Declared parameters: `[FromQuery]DateTime month`
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
- `src/app/modules/payroll/AddUpdateEmployeePayRoll.tsx` (matched: Get)
- `src/app/modules/payroll/components/PayRollTable.tsx` (matched: Get)
- `src/app/modules/payroll/components/ProcessPayrollModal.tsx` (matched: Get)
- `src/app/modules/payroll/components/UploadPayRollData.tsx` (matched: Get)
- `src/app/modules/payroll/models.ts` (matched: Get)
- `src/app/modules/payroll/PayRollManagement.tsx` (matched: Get)
- `src/app/modules/payroll/services.ts` (matched: Get)
- `src/app/modules/payrollsettings/components/SalaryTable.tsx` (matched: Get)
- `src/app/modules/payrollsettings/PayrollSettings.tsx` (matched: Get)
- `src/app/modules/payrollsettings/services.ts` (matched: Get)

## 3.9 UI trace for `Save`

- Backend endpoint: `POST api/payroll/divisor-policy`
- Controller source: `Codeji.CMS.API/Controllers/PayrollDivisorPolicyController.cs`
- Declared parameters: `PayrollDivisorPolicyDto dto`
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
- `src/app/modules/payroll/components/UploadPayRollData.tsx` (matched: Save)
- `src/app/modules/payrollsettings/components/AddUpdateEmployeeSalary.tsx` (matched: Save)

## 3.10 UI trace for `CreateSalary`

- Backend endpoint: `POST api/Salary/UpdateSalary`
- Controller source: `Codeji.CMS.API/Controllers/SalaryController.cs`
- Declared parameters: `[FromBody] CreateSalaryDto dto`
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

## 3.11 UI trace for `GetActiveSalary`

- Backend endpoint: `GET api/Salary/active/{userId}`
- Controller source: `Codeji.CMS.API/Controllers/SalaryController.cs`
- Declared parameters: `Guid userId`
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
- `src/app/modules/payrollsettings/components/SalaryTable.tsx` (matched: active)
- `src/app/modules/payrollsettings/model.ts` (matched: active)
- `src/app/modules/payrollsettings/PayrollSettings.tsx` (matched: active)

## 3.12 UI trace for `GetSalaryHistory`

- Backend endpoint: `GET api/Salary/history/{userId}`
- Controller source: `Codeji.CMS.API/Controllers/SalaryController.cs`
- Declared parameters: `Guid userId`
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

## 3.13 UI trace for `GetCompanySalaries`

- Backend endpoint: `POST api/Salary/GetCompanySalaries`
- Controller source: `Codeji.CMS.API/Controllers/SalaryController.cs`
- Declared parameters: `[FromBody] CompanySalaryFilterDto filter`
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
- `src/app/modules/payrollsettings/PayrollSettings.tsx` (matched: GetCompanySalaries)
- `src/app/modules/payrollsettings/services.ts` (matched: GetCompanySalaries)

## 4. Form and Interaction Review Rules

- Fields: derive names, types, defaults and optionality from form code and request DTO together.
- Validation: document frontend validation separately from backend rejection.
- Filters/pagination: establish whether state is local or server-backed.
- Retry: confirm whether query libraries retry reads or mutations.
- Accessibility: preserve labels, keyboard operation, focus return and tooltip semantics.
- Permissions: use actual guards/permitted actions; do not introduce role-name guesses.

## Source Trace Verification Ledger

This appendix supplies source-specific verification records for the UI document. It exists because the requested document must remain independently useful during debugging and testing.

### Ledger 1: route composition

- Anchor: `POST api/AutoPayroll/GeneratePayRollMonthly / AutoPayrollController.GeneratePayRollForMonthly()`
- Verification target: route composition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 2: authentication attributes

- Anchor: `POST api/PayRoll/GetEmpPayRollData / PayRollController.GetEmpPayRollData()`
- Verification target: authentication attributes.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 3: module permission

- Anchor: `POST api/PayRoll/GeneratePaySlip / PayRollController.GetSalarySlip()`
- Verification target: module permission.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 4: request binding

- Anchor: `POST api/PayRoll/GenerateEmployeePaySlip / PayRollController.GetEmployeeSalarySlip()`
- Verification target: request binding.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 5: DTO validation

- Anchor: `POST api/PayRoll/UploadPayloadData / PayRollController.UploadPayloadData()`
- Verification target: DTO validation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 6: company isolation

- Anchor: `POST api/PayRoll/AddUpdatePayRoll / PayRollController.AddUpdatePayRoll()`
- Verification target: company isolation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 7: actor ownership

- Anchor: `POST api/PayRoll/ProcessPayrollMonth / PayRollController.ProcessPayrollMonth()`
- Verification target: actor ownership.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 8: state transition

- Anchor: `GET api/payroll/divisor-policy / PayrollDivisorPolicyController.Get()`
- Verification target: state transition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 9: optimistic concurrency

- Anchor: `POST api/payroll/divisor-policy / PayrollDivisorPolicyController.Save()`
- Verification target: optimistic concurrency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 10: repository query

- Anchor: `POST api/Salary/UpdateSalary / SalaryController.CreateSalary()`
- Verification target: repository query.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 11: database mutation

- Anchor: `GET api/Salary/active/{userId} / SalaryController.GetActiveSalary()`
- Verification target: database mutation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 12: audit/history

- Anchor: `GET api/Salary/history/{userId} / SalaryController.GetSalaryHistory()`
- Verification target: audit/history.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 13: notification/outbox

- Anchor: `POST api/Salary/GetCompanySalaries / SalaryController.GetCompanySalaries()`
- Verification target: notification/outbox.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 14: background processing

- Anchor: `POST api/AutoPayroll/GeneratePayRollMonthly / AutoPayrollController.GeneratePayRollForMonthly()`
- Verification target: background processing.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

