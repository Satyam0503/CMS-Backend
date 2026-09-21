# Attendance and Work From Home UI Documentation

**Last verified against code:** 2026-09-02

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## Attendance Report roster rule

`EmployeeAttendance.tsx` requests `api/User/GetAllEmployees` with `employmentStatus: "AttendanceRoster"`. The card roster and its pagination include active employees (including legacy blank status) and employees serving notice, but exclude completed `Resigned` and `Terminated` employees. Historical exit records remain available through employee and offboarding reporting; they must not consume an attendance card or page slot.

## 1. Frontend Source Map

### 1.1 `src/app/modules/attendance/AttendanceCalendarLayout.test.ts`

- Source lines: 24
- Extracted symbols: style
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/attendance/attendanceGridLayout.ts`

- Source lines: 80
- Extracted symbols: type, ATTENDANCE_EMPLOYEE_COLUMN_WIDTH, ATTENDANCE_DATE_COLUMN_WIDTH, ATTENDANCE_GRID_MAX_HEIGHT, buildAttendanceGridScrollContainerSx
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/attendance/AttendanceModels.ts`

- Source lines: 72
- Extracted symbols: AttendanceStatus, AttendancePayload, AttendanceModel, CalendarAttendanceRow, GetAllAttendanceData, AdminAttendanceCreateDto
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/attendance/attendancePeriod.ts`

- Source lines: 39
- Extracted symbols: attendancePeriods, AttendancePeriod, AttendanceDateRange, resolveAttendanceDateRange, today, previousMonth
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/attendance/AttendanceServices.ts`

- Source lines: 42
- Extracted symbols: getAllAttendance, postRequest, addEditAttendance, deleteAttendance, deleteRequest, exportMonthlyAttendance, getRequest
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/attendance/attendanceStatusColors.test.ts`

- Source lines: 28
- Extracted symbols: map
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/attendance/attendanceStatusColors.ts`

- Source lines: 44
- Extracted symbols: DEFAULT_ATTENDANCE_STATUS_COLOR, AttendanceStatusSettingColorInput, normalizeAttendanceStatusCode, toAlphaHex, cleaned, alpha, normalizedAlpha, buildAttendanceStatusColorMap, code, color, resolveAttendanceStatusColor, normalized
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/attendance/AttendanceStatusSettings.test.tsx`

- Source lines: 200
- Extracted symbols: StatusPayload, makeStatus, getMockedGetRequest, getMockedPostRequest, renderComponent, container, root, act, flush, response, activeCheckbox, editButton, checkbox, saveButton, payload
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/attendance/AttendanceStatusSettings.tsx`

- Source lines: 210
- Extracted symbols: AttendanceStatusSetting, cloneStatuses, sectionSx, load, result, loaded, addDraft, requestDelete, confirmDelete, item, handleCancelEdit, saveAll, changedItems, original, statusTableGrid, statusTableHeaders
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/attendance/component/AttendanceCalendar.tsx`

- Source lines: 2100
- Extracted symbols: Employee, AttendanceRow, AttendanceData, AttendanceSegmentData, attendanceStatusLabels, statusesWithTimes, formatTimePreset, timeToMinutes, hours, createPunchPresets, start, end, minutes, workflowOwnedStatusCodes, isManualAttendanceStatus, manualAttendanceStatusOptions, Props, theme, isCompactAttendanceGrid, employeeColumnWidth, dateColumnWidth, fitDateColumns, attendanceDatesPaneRef, attendanceHorizontalScrollbarRef, attendanceEmployeesPaneRef, isSynchronizingAttendanceScroll, isSynchronizingAttendanceVerticalScroll, syncAttendanceHorizontalScroll, syncAttendanceVerticalScroll, attendanceStatusColorMap
- React/data hooks found: useMemo, useState, useEffect, useCallback
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx`

- Source lines: 815
- Extracted symbols: PayrollException, AttendanceStatusOption, ExistingAttendance, AttendanceResolution, Props, exceptionDescription, name, PenaltyReviewModal, exceptionStatusColor, checkInPresets, checkOutPresets, monthlyExceptionsExpandedStorageKey, isValidTime, formatTimeLabel, load, payrollMonth, postRequest, review, resolveAllFixed, reviewAllPenaltyExceptions, penalties, patchRequest, submitPenaltyReview
- React/data hooks found: useCallback, useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/attendance/EmployeeAttendance.tsx`

- Source lines: 529
- Extracted symbols: useDebounce, handler, EmployeeAttendance, intl, location, theme, userData, isAdministrator, isHrAttendanceRole, hasAnyAttendancePermission, canViewAllAttendance, canManageAttendance, debouncedSearch, fetchEmployees, response, getRequest, loadMyCorrectionRequests, requestId, request, handleSearchChange, dateRange, calendarMonth, calendarOffset, todayOverview, cancelled, postRequest, schedules
- React/data hooks found: useEffect, useState, useCallback, useMemo
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/modules/attendance/WorkFromHome.tsx`

- Source lines: 146
- Extracted symbols: Policy, Request, RequestPage, RequestFilters, Timing, Context, showLegacyRequestForm, canManagePolicy, saving, savePolicy, load, loadReviewRequests, queryString, weekStart, weekEnd, rosterFilters, response, rosterResponse, submit, timing, act, cancelRequest, currentWeekStart, currentWeekEnd, companyBusinessDate, currentWeekWfh
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/app/modules/holidays/Calendar.tsx`

- Source lines: 171
- Extracted symbols: permissions, canImport, handleCategoryChange, handleRefresh, handleSelectedDateChange, readImportFile, file, workbook, sheetRows, normaliseColumn, headerIndex, columns, headers, dateColumn, nameColumn, typeColumn, rows, rawDate, formattedDate, name, sourceType, submitImport, fetchedHolidayData
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.15 `src/app/modules/holidays/calendarServices.ts`

- Source lines: 49
- Extracted symbols: getAllCalendarItem, postRequest, addEditCalendarItem, deleteCalendarItem, deleteRequest, getHolidays, body, importCalendarItems
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.16 `src/app/modules/holidays/components/CalendarDays.tsx`

- Source lines: 365
- Extracted symbols: Props, muiTheme, popoverOpen, isSmallScreen, weekDayShort, open, getDaysInCurrentMonth, activeDate, firstDateOfMonth, daysInCurrentMonth, checkIsSameDay, today, dayjs, findOccasionsForMonth, occassionMonth, handlePopoverOpen, handlePopoverClose, checkForOccasionForDate, holidayDate, showDayOccassion, handleCloseSmlPopover, updatedDate, date, PropType, ToolTipPopOver
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.17 `src/app/modules/holidays/components/CalendarHeader.tsx`

- Source lines: 93
- Extracted symbols: Props, increaseMonth, decreaseMonth
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.18 `src/app/modules/holidays/components/CalendarSidebar.tsx`

- Source lines: 272
- Extracted symbols: Props, CalendarItems, modulePermission, hasAddPermission, fetchedHolidayData, response, handleClose, findUpComingOccassions, today, thisMonthHolidaysFinder, occDate, getMonthSummary, holidayCounter, eventCounter, birthdayCounter, anniversaryCounter, holidayMonth, findHolidaysForDay, occassionDate
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.19 `src/app/modules/holidays/components/HolidayFilter.tsx`

- Source lines: 126
- Extracted symbols: PropsType, dispatch, appliedHolFilters, size, value
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.20 `src/app/modules/holidays/components/HolidayForm.tsx`

- Source lines: 292
- Extracted symbols: PropsTypes, handleFileChange, files, selectedFile, handleAddUpdateHoliday, formData
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.21 `src/app/modules/holidays/components/UpcomingHolidayCard.tsx`

- Source lines: 277
- Extracted symbols: Props, UpcomingHolidayCard, modulePermission, hasEditPermission, hasDeletePermission, handleActionButtonClick, handleActionMenuClose, handleHolidayDeleteClick, handleHolidayEdit, holiday, handleCloseModal, handleDeleteModalClose, handleHolidayDeletion, date
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.22 `src/app/modules/holidays/models.ts`

- Source lines: 58
- Extracted symbols: OccasionModel, OccasionResponseModel, CalendarItemType, GetCalendarItemPayload, CalendarItem, HolidayFilterModel, HolidayPayload, HolidayModel, GetAllCalendarItemData, AddHolidayResponseData, CalendarImportItem
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `Add` | `POST api/admin/attendance` | Search service functions and component event handlers for this route/action. |
| 2 | `GetByUser` | `GET api/admin/attendance/{userId}` | Search service functions and component event handlers for this route/action. |
| 3 | `GetByUserAndDate` | `GET api/admin/attendance/{userId}/date` | Search service functions and component event handlers for this route/action. |
| 4 | `Update` | `PUT api/admin/attendance/{userId}` | Search service functions and component event handlers for this route/action. |
| 5 | `GetAll` | `POST api/admin/attendance/GetAllAttendanceItems` | Search service functions and component event handlers for this route/action. |
| 6 | `GetEffectiveSchedules` | `POST api/admin/attendance/effective-schedules` | Search service functions and component event handlers for this route/action. |
| 7 | `ExportMonthly` | `GET api/admin/attendance/export/monthly` | Search service functions and component event handlers for this route/action. |
| 8 | `InitializeMonth` | `POST api/admin/attendance/initialize-month` | Search service functions and component event handlers for this route/action. |
| 9 | `CorrectionRequests` | `GET api/admin/attendance/correction-requests` | Search service functions and component event handlers for this route/action. |
| 10 | `ReviewCorrection` | `PUT api/admin/attendance/correction-requests/{requestId}` | Search service functions and component event handlers for this route/action. |
| 11 | `GetMyCalendar` | `GET api/admin/attendance/my-calendar` | Search service functions and component event handlers for this route/action. |
| 12 | `GetEmployeePolicy` | `GET api/attendance/penalty/employees/{employeeUserId}/policy` | Search service functions and component event handlers for this route/action. |
| 13 | `SaveEmployeePolicy` | `POST api/attendance/penalty/employees/{employeeUserId}/policy` | Search service functions and component event handlers for this route/action. |
| 14 | `Recalculate` | `POST api/attendance/penalty/exceptions/recalculate` | Search service functions and component event handlers for this route/action. |
| 15 | `Search` | `POST api/attendance/penalty/exceptions/search` | Search service functions and component event handlers for this route/action. |
| 16 | `Review` | `PATCH api/attendance/penalty/exceptions/{id}/review` | Search service functions and component event handlers for this route/action. |
| 17 | `ResolveAttendance` | `POST api/attendance/penalty/exceptions/{id}/resolve-attendance` | Search service functions and component event handlers for this route/action. |
| 18 | `Override` | `PATCH api/attendance/penalty/exceptions/{id}/override` | Search service functions and component event handlers for this route/action. |
| 19 | `GetMonthLockStatus` | `GET api/attendance/penalty/month/lock-status` | Search service functions and component event handlers for this route/action. |
| 20 | `ValidateAndLock` | `POST api/attendance/penalty/month/validate-lock` | Search service functions and component event handlers for this route/action. |
| 21 | `Get` | `GET api/attendance-status-settings` | Search service functions and component event handlers for this route/action. |
| 22 | `Save` | `POST api/attendance-status-settings` | Search service functions and component event handlers for this route/action. |
| 23 | `Delete` | `DELETE api/attendance-status-settings/{statusId}` | Search service functions and component event handlers for this route/action. |
| 24 | `Grid` | `GET api/attendance/me/grid` | Search service functions and component event handlers for this route/action. |
| 25 | `Calendar` | `GET api/attendance/me/calendar` | Search service functions and component event handlers for this route/action. |
| 26 | `RequestCorrection` | `POST api/attendance/me/correction-requests` | Search service functions and component event handlers for this route/action. |
| 27 | `MyCorrectionRequests` | `GET api/attendance/me/correction-requests` | Search service functions and component event handlers for this route/action. |
| 28 | `Get` | `GET api/attendance/weekly-offs` | Search service functions and component event handlers for this route/action. |
| 29 | `Save` | `POST api/attendance/weekly-offs` | Search service functions and component event handlers for this route/action. |
| 30 | `Policy` | `GET api/wfh/policy` | Search service functions and component event handlers for this route/action. |
| 31 | `Context` | `GET api/wfh/context` | Search service functions and component event handlers for this route/action. |
| 32 | `SavePolicy` | `PUT api/wfh/policy` | Search service functions and component event handlers for this route/action. |
| 33 | `SavePolicyPost` | `POST api/wfh/policy` | Search service functions and component event handlers for this route/action. |
| 34 | `Allocations` | `GET api/wfh/allocations` | Search service functions and component event handlers for this route/action. |
| 35 | `SaveAllocation` | `PUT api/wfh/allocations` | Search service functions and component event handlers for this route/action. |
| 36 | `Create` | `POST api/wfh/requests` | Search service functions and component event handlers for this route/action. |
| 37 | `Mine` | `GET api/wfh/requests/my` | Search service functions and component event handlers for this route/action. |
| 38 | `Team` | `GET api/wfh/requests/team` | Search service functions and component event handlers for this route/action. |
| 39 | `All` | `GET api/wfh/requests/all` | Search service functions and component event handlers for this route/action. |
| 40 | `Approve` | `POST api/wfh/requests/{id}/approve` | Search service functions and component event handlers for this route/action. |
| 41 | `Reject` | `POST api/wfh/requests/{id}/reject` | Search service functions and component event handlers for this route/action. |
| 42 | `Return` | `POST api/wfh/requests/{id}/return` | Search service functions and component event handlers for this route/action. |
| 43 | `Cancel` | `POST api/wfh/requests/{id}/cancel` | Search service functions and component event handlers for this route/action. |
| 44 | `CheckIn` | `POST api/wfh/requests/{id}/check-in` | Search service functions and component event handlers for this route/action. |
| 45 | `CheckOut` | `POST api/wfh/requests/{id}/check-out` | Search service functions and component event handlers for this route/action. |
| 46 | `Timing` | `GET api/wfh/requests/{id}/timing` | Search service functions and component event handlers for this route/action. |
| 47 | `CorrectHours` | `PATCH api/wfh/attendance-exceptions/{id}/hours` | Search service functions and component event handlers for this route/action. |
| 48 | `ResolveConflict` | `PATCH api/wfh/attendance-exceptions/{id}/resolve-conflict` | Search service functions and component event handlers for this route/action. |
| 49 | `RemarkOptions` | `GET api/attendance/remark-options` | Search service functions and component event handlers for this route/action. |
| 50 | `SaveRemarkOption` | `POST api/attendance/remark-options` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `Add`

- Backend endpoint: `POST api/admin/attendance`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `[FromBody] AdminAttendanceCreateDto dto`
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
- `src/app/modules/attendance/AttendanceCalendarLayout.test.ts` (matched: attendance)
- `src/app/modules/attendance/attendanceGridLayout.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceModels.ts` (matched: attendance)
- `src/app/modules/attendance/attendancePeriod.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: Add, attendance)
- `src/app/modules/attendance/attendanceStatusColors.test.ts` (matched: attendance)
- `src/app/modules/attendance/attendanceStatusColors.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: attendance)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Add, attendance)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Add, attendance)

## 3.2 UI trace for `GetByUser`

- Backend endpoint: `GET api/admin/attendance/{userId}`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `string userId`
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
- `src/app/modules/attendance/AttendanceCalendarLayout.test.ts` (matched: attendance)
- `src/app/modules/attendance/attendanceGridLayout.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceModels.ts` (matched: attendance)
- `src/app/modules/attendance/attendancePeriod.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: attendance)
- `src/app/modules/attendance/attendanceStatusColors.test.ts` (matched: attendance)
- `src/app/modules/attendance/attendanceStatusColors.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: attendance)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: attendance)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: attendance)

## 3.3 UI trace for `GetByUserAndDate`

- Backend endpoint: `GET api/admin/attendance/{userId}/date`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `string userId, [FromQuery] DateTime date`
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
- `src/app/modules/attendance/attendanceGridLayout.ts` (matched: date)
- `src/app/modules/attendance/AttendanceModels.ts` (matched: date)
- `src/app/modules/attendance/attendancePeriod.ts` (matched: date)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: date)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: date)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: date)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: date)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: date)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: date)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: date)

## 3.4 UI trace for `Update`

- Backend endpoint: `PUT api/admin/attendance/{userId}`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `string userId, [FromQuery] DateTime date, [FromBody] AttendanceUpdateDto dto`
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
- `src/app/modules/attendance/AttendanceCalendarLayout.test.ts` (matched: attendance)
- `src/app/modules/attendance/attendanceGridLayout.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceModels.ts` (matched: attendance)
- `src/app/modules/attendance/attendancePeriod.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: Update, attendance)
- `src/app/modules/attendance/attendanceStatusColors.test.ts` (matched: attendance)
- `src/app/modules/attendance/attendanceStatusColors.ts` (matched: attendance)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Update, attendance)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Update, attendance)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Update, attendance)

## 3.5 UI trace for `GetAll`

- Backend endpoint: `POST api/admin/attendance/GetAllAttendanceItems`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `[FromBody] AttendanceCalendarRequestDto dto`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: GetAll)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: GetAll, GetAllAttendanceItems)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: GetAll)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: GetAll)
- `src/app/modules/holidays/Calendar.tsx` (matched: GetAll)
- `src/app/modules/holidays/calendarServices.ts` (matched: GetAll)
- `src/app/modules/holidays/components/CalendarSidebar.tsx` (matched: GetAll)
- `src/app/modules/holidays/models.ts` (matched: GetAll)

## 3.6 UI trace for `GetEffectiveSchedules`

- Backend endpoint: `POST api/admin/attendance/effective-schedules`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `[FromBody] EffectiveOfficeSchedulesRequest request, [FromServices] IEffectiveOfficeScheduleService effectiveSchedules`
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
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: effective-schedules)

## 3.7 UI trace for `ExportMonthly`

- Backend endpoint: `GET api/admin/attendance/export/monthly`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `[FromQuery] int year, [FromQuery] int month`
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
- `src/app/modules/attendance/AttendanceServices.ts` (matched: ExportMonthly, monthly)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: monthly)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: monthly)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: monthly)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: ExportMonthly, monthly)

## 3.8 UI trace for `InitializeMonth`

- Backend endpoint: `POST api/admin/attendance/initialize-month`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `[FromBody] AttendanceInitializationRequestDto request, [FromServices] IAttendanceInitializationService initialization`
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

## 3.9 UI trace for `CorrectionRequests`

- Backend endpoint: `GET api/admin/attendance/correction-requests`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `[FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections`
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
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: CorrectionRequests, correction-requests)

## 3.10 UI trace for `ReviewCorrection`

- Backend endpoint: `PUT api/admin/attendance/correction-requests/{requestId}`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `string requestId, [FromBody] AttendanceCorrectionReviewDto model, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.UserNotifications> userNotifications, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Notifications> notifications`
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
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: ReviewCorrection, correction-requests)

## 3.11 UI trace for `GetMyCalendar`

- Backend endpoint: `GET api/admin/attendance/my-calendar`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Declared parameters: `[FromQuery] int year, [FromQuery] int month`
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

## 3.12 UI trace for `GetEmployeePolicy`

- Backend endpoint: `GET api/attendance/penalty/employees/{employeeUserId}/policy`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Declared parameters: `string employeeUserId,[FromQuery]DateTime? effectiveOn=null`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: policy)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: policy)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: policy)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: policy)

## 3.13 UI trace for `SaveEmployeePolicy`

- Backend endpoint: `POST api/attendance/penalty/employees/{employeeUserId}/policy`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Declared parameters: `string employeeUserId,AttendancePenaltyPolicyDto dto`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: policy)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: policy)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: policy)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: policy)

## 3.14 UI trace for `Recalculate`

- Backend endpoint: `POST api/attendance/penalty/exceptions/recalculate`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Declared parameters: `AttendanceMonthRequestDto dto`
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
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Recalculate, recalculate)

## 3.15 UI trace for `Search`

- Backend endpoint: `POST api/attendance/penalty/exceptions/search`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Declared parameters: `AttendanceExceptionFilterDto dto`
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
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Search, search)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Search, search)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Search, search)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Search, search)

## 3.16 UI trace for `Review`

- Backend endpoint: `PATCH api/attendance/penalty/exceptions/{id}/review`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Declared parameters: `string id,AttendanceExceptionReviewDto dto`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Review, review)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Review, review)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Review, review)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Review, review)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Review, review)

## 3.17 UI trace for `ResolveAttendance`

- Backend endpoint: `POST api/attendance/penalty/exceptions/{id}/resolve-attendance`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Declared parameters: `string id, AttendanceExceptionResolutionDto dto`
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
- `src/app/modules/attendance/attendancePeriod.ts` (matched: ResolveAttendance)
- `src/app/modules/attendance/attendanceStatusColors.test.ts` (matched: ResolveAttendance)
- `src/app/modules/attendance/attendanceStatusColors.ts` (matched: ResolveAttendance)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: ResolveAttendance)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: resolve-attendance)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: ResolveAttendance)

## 3.18 UI trace for `Override`

- Backend endpoint: `PATCH api/attendance/penalty/exceptions/{id}/override`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Declared parameters: `string id, AttendanceExceptionOverrideDto dto`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Override, override)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Override, override)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Override, override)

## 3.19 UI trace for `GetMonthLockStatus`

- Backend endpoint: `GET api/attendance/penalty/month/lock-status`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
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

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.20 UI trace for `ValidateAndLock`

- Backend endpoint: `POST api/attendance/penalty/month/validate-lock`
- Controller source: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Declared parameters: `AttendanceMonthRequestDto dto`
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

## 3.21 UI trace for `Get`

- Backend endpoint: `GET api/attendance-status-settings`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs`
- Declared parameters: `[FromQuery] bool activeOnly=true`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: Get)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: Get)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Get, attendance-status-settings)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Get, attendance-status-settings)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Get, attendance-status-settings)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Get, attendance-status-settings)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Get)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Get)
- `src/app/modules/holidays/Calendar.tsx` (matched: Get)
- `src/app/modules/holidays/calendarServices.ts` (matched: Get)

## 3.22 UI trace for `Save`

- Backend endpoint: `POST api/attendance-status-settings`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs`
- Declared parameters: `AttendanceStatusSettingDto dto`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Save, attendance-status-settings)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Save, attendance-status-settings)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Save, attendance-status-settings)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Save, attendance-status-settings)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Save)

## 3.23 UI trace for `Delete`

- Backend endpoint: `DELETE api/attendance-status-settings/{statusId}`
- Controller source: `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs`
- Declared parameters: `string statusId`
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
- `src/app/modules/attendance/AttendanceServices.ts` (matched: Delete)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Delete, attendance-status-settings)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Delete, attendance-status-settings)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: attendance-status-settings)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: attendance-status-settings)
- `src/app/modules/holidays/calendarServices.ts` (matched: Delete)
- `src/app/modules/holidays/components/UpcomingHolidayCard.tsx` (matched: Delete)

## 3.24 UI trace for `Grid`

- Backend endpoint: `GET api/attendance/me/grid`
- Controller source: `Codeji.CMS.API/Controllers/MyAttendanceController.cs`
- Declared parameters: `[FromQuery] int year, [FromQuery] int month`
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
- `src/app/modules/attendance/AttendanceCalendarLayout.test.ts` (matched: Grid, grid)
- `src/app/modules/attendance/attendanceGridLayout.ts` (matched: Grid, grid)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Grid, grid)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Grid, grid)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Grid, grid)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Grid, grid)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Grid, grid)
- `src/app/modules/holidays/components/CalendarDays.tsx` (matched: Grid, grid)
- `src/app/modules/holidays/components/CalendarSidebar.tsx` (matched: Grid, grid)
- `src/app/modules/holidays/components/HolidayForm.tsx` (matched: Grid, grid)

## 3.25 UI trace for `Calendar`

- Backend endpoint: `GET api/attendance/me/calendar`
- Controller source: `Codeji.CMS.API/Controllers/MyAttendanceController.cs`
- Declared parameters: `[FromQuery] int year, [FromQuery] int month`
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
- `src/app/modules/attendance/AttendanceCalendarLayout.test.ts` (matched: Calendar, calendar)
- `src/app/modules/attendance/AttendanceModels.ts` (matched: Calendar, calendar)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Calendar, calendar)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Calendar, calendar)
- `src/app/modules/holidays/Calendar.tsx` (matched: Calendar, calendar)
- `src/app/modules/holidays/calendarServices.ts` (matched: Calendar, calendar)
- `src/app/modules/holidays/components/CalendarDays.tsx` (matched: Calendar, calendar)
- `src/app/modules/holidays/components/CalendarHeader.tsx` (matched: Calendar, calendar)
- `src/app/modules/holidays/components/CalendarSidebar.tsx` (matched: Calendar, calendar)
- `src/app/modules/holidays/components/HolidayForm.tsx` (matched: Calendar, calendar)

## 3.26 UI trace for `RequestCorrection`

- Backend endpoint: `POST api/attendance/me/correction-requests`
- Controller source: `Codeji.CMS.API/Controllers/MyAttendanceController.cs`
- Declared parameters: `[FromBody] AttendanceCorrectionRequestDto model`
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
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: correction-requests)

## 3.27 UI trace for `MyCorrectionRequests`

- Backend endpoint: `GET api/attendance/me/correction-requests`
- Controller source: `Codeji.CMS.API/Controllers/MyAttendanceController.cs`
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
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: MyCorrectionRequests, correction-requests)

## 3.28 UI trace for `Get`

- Backend endpoint: `GET api/attendance/weekly-offs`
- Controller source: `Codeji.CMS.API/Controllers/WeeklyOffSettingsController.cs`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: Get)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: Get)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Get, weekly-offs)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Get)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Get, weekly-offs)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Get)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Get)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Get)
- `src/app/modules/holidays/Calendar.tsx` (matched: Get)
- `src/app/modules/holidays/calendarServices.ts` (matched: Get)

## 3.29 UI trace for `Save`

- Backend endpoint: `POST api/attendance/weekly-offs`
- Controller source: `Codeji.CMS.API/Controllers/WeeklyOffSettingsController.cs`
- Declared parameters: `WeeklyOffSettingDto dto`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Save, weekly-offs)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Save)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Save, weekly-offs)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Save)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Save)

## 3.30 UI trace for `Policy`

- Backend endpoint: `GET api/wfh/policy`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Policy, policy)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Policy, policy)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Policy, policy)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Policy, policy)

## 3.31 UI trace for `Context`

- Backend endpoint: `GET api/wfh/context`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Context, context)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Context, context)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Context, context)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Context, context)
- `src/app/modules/holidays/Calendar.tsx` (matched: Context, context)

## 3.32 UI trace for `SavePolicy`

- Backend endpoint: `PUT api/wfh/policy`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `WorkFromHomePolicyDto dto`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: policy)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: policy)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: policy)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: SavePolicy, policy)

## 3.33 UI trace for `SavePolicyPost`

- Backend endpoint: `POST api/wfh/policy`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `WorkFromHomePolicyDto dto`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: policy)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: policy)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: policy)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: policy)

## 3.34 UI trace for `Allocations`

- Backend endpoint: `GET api/wfh/allocations`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
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

## 3.35 UI trace for `SaveAllocation`

- Backend endpoint: `PUT api/wfh/allocations`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `WorkFromHomeEmployeeAllocationDto dto`
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

## 3.36 UI trace for `Create`

- Backend endpoint: `POST api/wfh/requests`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `WorkFromHomeRequestDto dto`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: Create)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: Create)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Create)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Create, requests)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Create, requests)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: requests)
- `src/app/modules/holidays/Calendar.tsx` (matched: Create)
- `src/app/modules/holidays/components/CalendarSidebar.tsx` (matched: Create)
- `src/app/modules/holidays/components/HolidayForm.tsx` (matched: Create)

## 3.37 UI trace for `Mine`

- Backend endpoint: `GET api/wfh/requests/my`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
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
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: my)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: my)

## 3.38 UI trace for `Team`

- Backend endpoint: `GET api/wfh/requests/team`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `[FromQuery] WorkFromHomeRequestFilterDto filter`
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
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Team, team)

## 3.39 UI trace for `All`

- Backend endpoint: `GET api/wfh/requests/all`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `[FromQuery] WorkFromHomeRequestFilterDto filter`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: All, all)
- `src/app/modules/attendance/attendancePeriod.ts` (matched: All, all)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: All, all)
- `src/app/modules/attendance/attendanceStatusColors.test.ts` (matched: All, all)
- `src/app/modules/attendance/attendanceStatusColors.ts` (matched: All, all)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: All, all)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: All, all)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: All, all)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: All, all)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: All, all)

## 3.40 UI trace for `Approve`

- Backend endpoint: `POST api/wfh/requests/{id}/approve`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id, WorkFromHomeDecisionDto dto`
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
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Approve, approve)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Approve, approve)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Approve, approve)

## 3.41 UI trace for `Reject`

- Backend endpoint: `POST api/wfh/requests/{id}/reject`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id, WorkFromHomeDecisionDto dto`
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
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Reject, reject)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Reject, reject)

## 3.42 UI trace for `Return`

- Backend endpoint: `POST api/wfh/requests/{id}/return`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id, WorkFromHomeDecisionDto dto`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: Return, return)
- `src/app/modules/attendance/attendancePeriod.ts` (matched: Return, return)
- `src/app/modules/attendance/AttendanceServices.ts` (matched: Return, return)
- `src/app/modules/attendance/attendanceStatusColors.ts` (matched: Return, return)
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Return, return)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Return, return)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Return, return)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Return, return)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Return, return)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Return, return)

## 3.43 UI trace for `Cancel`

- Backend endpoint: `POST api/wfh/requests/{id}/cancel`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id, WorkFromHomeCancelDto dto`
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
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` (matched: Cancel, cancel)
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` (matched: Cancel, cancel)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: Cancel, cancel)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: Cancel, cancel)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: Cancel, cancel)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Cancel, cancel)
- `src/app/modules/holidays/Calendar.tsx` (matched: Cancel, cancel)
- `src/app/modules/holidays/components/HolidayForm.tsx` (matched: Cancel, cancel)

## 3.44 UI trace for `CheckIn`

- Backend endpoint: `POST api/wfh/requests/{id}/check-in`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: CheckIn)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: CheckIn)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: CheckIn)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: check-in)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: CheckIn, check-in)

## 3.45 UI trace for `CheckOut`

- Backend endpoint: `POST api/wfh/requests/{id}/check-out`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: CheckOut)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: CheckOut)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: CheckOut)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: check-out)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: CheckOut, check-out)

## 3.46 UI trace for `Timing`

- Backend endpoint: `GET api/wfh/requests/{id}/timing`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id`
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
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: Timing, timing)

## 3.47 UI trace for `CorrectHours`

- Backend endpoint: `PATCH api/wfh/attendance-exceptions/{id}/hours`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id, WorkFromHomeHoursCorrectionDto dto`
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
- `src/app/modules/attendance/AttendanceModels.ts` (matched: hours)
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` (matched: hours)
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: hours)
- `src/app/modules/attendance/EmployeeAttendance.tsx` (matched: hours)
- `src/app/modules/attendance/WorkFromHome.tsx` (matched: hours)

## 3.48 UI trace for `ResolveConflict`

- Backend endpoint: `PATCH api/wfh/attendance-exceptions/{id}/resolve-conflict`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `string id, WorkFromHomeConflictResolutionDto dto`
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
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` (matched: resolve-conflict)

## 3.49 UI trace for `RemarkOptions`

- Backend endpoint: `GET api/attendance/remark-options`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
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

## 3.50 UI trace for `SaveRemarkOption`

- Backend endpoint: `POST api/attendance/remark-options`
- Controller source: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Declared parameters: `AttendanceRemarkOptionDto dto`
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

