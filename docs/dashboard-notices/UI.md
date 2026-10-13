# Dashboard and Notices UI Documentation

**Last verified against code:** 2026-09-02

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## 1. Frontend Source Map

### 1.1 `src/app/modules/dashboard/components/ApplicationsChart.tsx`

- Source lines: 158
- Extracted symbols: ApplicationsChart, theme, getActiveJobs, response, getAllApplicationsData, key, i
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/dashboard/components/CardSkeleton.tsx`

- Source lines: 55
- Extracted symbols: CardSkeleton
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/dashboard/components/DashboardHeader.tsx`

- Source lines: 57
- Extracted symbols: DashboardHeader, intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/dashboard/components/DepartmentChart.tsx`

- Source lines: 70
- Extracted symbols: defaultLanguage, hasAssignedEmployees, chartSeries
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/dashboard/components/EmployeeChart.tsx`

- Source lines: 79
- Extracted symbols: createChartData, countTotalEmployee, totalEmployee, intl, empData
- React/data hooks found: useMemo
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/dashboard/components/EmployeePerformance.tsx`

- Source lines: 71
- Extracted symbols: createData, rows, intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/dashboard/components/EmpOfMonth.tsx`

- Source lines: 94
- Extracted symbols: intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/dashboard/components/EmpOnLeave.tsx`

- Source lines: 125
- Extracted symbols: defaultLanguage, fetchLeaveRequest
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/dashboard/components/Help.tsx`

- Source lines: 65
- Extracted symbols: intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/dashboard/components/NoticeBoard.tsx`

- Source lines: 155
- Extracted symbols: updateNoticeViewedStatus
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/dashboard/components/OpenPositions.tsx`

- Source lines: 100
- Extracted symbols: getActiveJobs, response
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/dashboard/components/Punctual.tsx`

- Source lines: 95
- Extracted symbols: intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/modules/dashboard/components/PushNotification.tsx`

- Source lines: 29
- Extracted symbols: PushNotification, isAuthenticated, requestUserPermission
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/app/modules/dashboard/components/pushNotificationUtils.ts`

- Source lines: 75
- Extracted symbols: getNotificationOption, hostName, createNotifications, option, title, notification
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.15 `src/app/modules/dashboard/components/UpcomingCelebration.tsx`

- Source lines: 134
- Extracted symbols: UpComingCelebration
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.16 `src/app/modules/dashboard/components/UpComingHolidayAndEvent.tsx`

- Source lines: 146
- Extracted symbols: UpComingHolidayAndEvents
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.17 `src/app/modules/dashboard/Dashboard.tsx`

- Source lines: 171
- Extracted symbols: user, userModulesPermissions, hasAccessToEmpModule, canViewCompanyDashboard, hasAccessToJob, hasAccessToNoticeBoard
- React/data hooks found: useEffect, useMemo
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.18 `src/app/modules/dashboard/dashboardQueries.test.ts`

- Source lines: 70
- Extracted symbols: createTestClient, QueryClient, response, data, client, options, keys
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.19 `src/app/modules/dashboard/dashboardQueries.ts`

- Source lines: 40
- Extracted symbols: dashboardQueryOptions, useGenderQuery, useDepartmentsQuery, useUpcomingEventsQuery, useUpcomingCelebrationsQuery
- React/data hooks found: useQuery
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.20 `src/app/modules/dashboard/dashboardservices.ts`

- Source lines: 44
- Extracted symbols: getAllGenderInfo, getRequest, getAllDepartmentInfo, getAllApplicationStatus, getUpcomingHolidayAndEvents, getUpcomingCelebration
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.21 `src/app/modules/dashboard/model.ts`

- Source lines: 54
- Extracted symbols: GenderModel, DepartmentResponseModel, ApplicationStatusModel, UpComingHolidayAndEventModel, CalendarModel, UpcomingCelebration, CelebrationModule, GetAllGenderDetailsResponseData, GetAllDepartmentDetailsResponseData, GetAllApplicationStatusData, GetUpComingHolidayEvent, GetUpComingCelebration
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.22 `src/app/modules/noticeboard/components/AddNotice.tsx`

- Source lines: 199
- Extracted symbols: PropsTypes, defaultLang, handleAddUpdateNotice
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.23 `src/app/modules/noticeboard/components/MyNotices.tsx`

- Source lines: 169
- Extracted symbols: MyNotices, userModulesPermissions, navigate, hasAddPermission, handleClose, handleRefresh, handlePageChange, handleRowPerPageChange, handleConfirmationOpen, handleRowClick, handleNoticeDelete
- React/data hooks found: useCallback, useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.24 `src/app/modules/noticeboard/components/NoticeBoardFilter.tsx`

- Source lines: 200
- Extracted symbols: isTimeOptionOpen, noticeBoardFilter, dispatch, handleTimeOptionMenuClose, handleClick, handleTimePeriodChange, currentFilters, selectedTimePeriod, updatedFromDate, updatedToDate, size, value
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.25 `src/app/modules/noticeboard/components/NoticeViewModel.tsx`

- Source lines: 130
- Extracted symbols: Props, defaultLanguage, designation, designationLabel
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.26 `src/app/modules/noticeboard/models.ts`

- Source lines: 49
- Extracted symbols: NoticeModel, NoticeBoardFilterModel, NoticeResponseModel, GetAllNoticePayload, ViewModel, MyNoticeModel, GetAllNoticeResponse, GetMyNoticesResponse
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.27 `src/app/modules/noticeboard/NoticeBoard.tsx`

- Source lines: 372
- Extracted symbols: NoticeBoard, userModulesPermissions, navigate, noticeBoardFilter, noticeIdFromRedirect, hasAddPermission, handleClose, handleRefresh, handleRowClick, handleFilterOpen, handleSearchChange, handlePageChange, handleRowPerPageChange, handleNoticeViewClose, updateNoticeViewedStatus, noticeType, tone, designation
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.28 `src/app/modules/noticeboard/services.ts`

- Source lines: 50
- Extracted symbols: postNotice, postRequest, updateNotice, putRequest, getAllNotices, getMyPostedNotices, getRequest, deleteMyNotice, deleteRequest, getNoticeById
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.29 `src/app/routing/NoticeBoardRoutes.tsx`

- Source lines: 47
- Extracted symbols: NoticeBoardRoutes
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `GetAllGenderDetails` | `GET api/Dashboard/GetAllGenderDetails` | Search service functions and component event handlers for this route/action. |
| 2 | `GetApplicationStatusData` | `GET api/Dashboard/GetApplicationStatusData` | Search service functions and component event handlers for this route/action. |
| 3 | `GetAllDepartmentDetails` | `GET api/Dashboard/GetAllDepartmentDetails` | Search service functions and component event handlers for this route/action. |
| 4 | `GetUpComingHolidays` | `GET api/Dashboard/GetUpComingHolidayAndEvents` | Search service functions and component event handlers for this route/action. |
| 5 | `GetUpComingCelebrations` | `GET api/Dashboard/GetUpComingCelebrations` | Search service functions and component event handlers for this route/action. |
| 6 | `PostNotice` | `POST api/NoticeBoard/PostNotice` | Search service functions and component event handlers for this route/action. |
| 7 | `GetAllNotices` | `POST api/NoticeBoard/GetAllNotice` | Search service functions and component event handlers for this route/action. |
| 8 | `GetNoticeById` | `GET api/NoticeBoard/GetNoticeById/{noticeId}` | Search service functions and component event handlers for this route/action. |
| 9 | `GetMyNotices` | `GET api/NoticeBoard/GetMyNotices` | Search service functions and component event handlers for this route/action. |
| 10 | `UpdateMyNotice` | `PUT api/NoticeBoard/UpdateMyNotice` | Search service functions and component event handlers for this route/action. |
| 11 | `DeleteNotice` | `DELETE api/NoticeBoard/DeleteNotice/{noticeId}` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `GetAllGenderDetails`

- Backend endpoint: `GET api/Dashboard/GetAllGenderDetails`
- Controller source: `Codeji.CMS.API/Controllers/DashboardController.cs`
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
- `src/app/modules/dashboard/dashboardservices.ts` (matched: GetAllGenderDetails)
- `src/app/modules/dashboard/model.ts` (matched: GetAllGenderDetails)

## 3.2 UI trace for `GetApplicationStatusData`

- Backend endpoint: `GET api/Dashboard/GetApplicationStatusData`
- Controller source: `Codeji.CMS.API/Controllers/DashboardController.cs`
- Declared parameters: `[FromQuery] string? vacancyId`
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

## 3.3 UI trace for `GetAllDepartmentDetails`

- Backend endpoint: `GET api/Dashboard/GetAllDepartmentDetails`
- Controller source: `Codeji.CMS.API/Controllers/DashboardController.cs`
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
- `src/app/modules/dashboard/dashboardservices.ts` (matched: GetAllDepartmentDetails)
- `src/app/modules/dashboard/model.ts` (matched: GetAllDepartmentDetails)

## 3.4 UI trace for `GetUpComingHolidays`

- Backend endpoint: `GET api/Dashboard/GetUpComingHolidayAndEvents`
- Controller source: `Codeji.CMS.API/Controllers/DashboardController.cs`
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
- `src/app/modules/dashboard/dashboardQueries.test.ts` (matched: GetUpComingHolidayAndEvents)
- `src/app/modules/dashboard/dashboardQueries.ts` (matched: GetUpComingHolidayAndEvents)
- `src/app/modules/dashboard/dashboardservices.ts` (matched: GetUpComingHolidayAndEvents)

## 3.5 UI trace for `GetUpComingCelebrations`

- Backend endpoint: `GET api/Dashboard/GetUpComingCelebrations`
- Controller source: `Codeji.CMS.API/Controllers/DashboardController.cs`
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

## 3.6 UI trace for `PostNotice`

- Backend endpoint: `POST api/NoticeBoard/PostNotice`
- Controller source: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Declared parameters: `AddNoticeRequestModel notice`
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
- `src/app/modules/noticeboard/components/AddNotice.tsx` (matched: PostNotice)
- `src/app/modules/noticeboard/services.ts` (matched: PostNotice)

## 3.7 UI trace for `GetAllNotices`

- Backend endpoint: `POST api/NoticeBoard/GetAllNotice`
- Controller source: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Declared parameters: `[FromBody] GetNoticeRequest filter`
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
- `src/app/modules/dashboard/components/NoticeBoard.tsx` (matched: GetAllNotices, GetAllNotice)
- `src/app/modules/noticeboard/models.ts` (matched: GetAllNotice)
- `src/app/modules/noticeboard/NoticeBoard.tsx` (matched: GetAllNotices, GetAllNotice)
- `src/app/modules/noticeboard/services.ts` (matched: GetAllNotices, GetAllNotice)

## 3.8 UI trace for `GetNoticeById`

- Backend endpoint: `GET api/NoticeBoard/GetNoticeById/{noticeId}`
- Controller source: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Declared parameters: `string noticeId`
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
- `src/app/modules/noticeboard/components/NoticeViewModel.tsx` (matched: GetNoticeById)
- `src/app/modules/noticeboard/services.ts` (matched: GetNoticeById)

## 3.9 UI trace for `GetMyNotices`

- Backend endpoint: `GET api/NoticeBoard/GetMyNotices`
- Controller source: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Declared parameters: `[FromQuery] int pageNo, [FromQuery] int records`
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
- `src/app/modules/noticeboard/models.ts` (matched: GetMyNotices)
- `src/app/modules/noticeboard/services.ts` (matched: GetMyNotices)

## 3.10 UI trace for `UpdateMyNotice`

- Backend endpoint: `PUT api/NoticeBoard/UpdateMyNotice`
- Controller source: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Declared parameters: `[FromBody] UpdateNoticeDto notice`
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

## 3.11 UI trace for `DeleteNotice`

- Backend endpoint: `DELETE api/NoticeBoard/DeleteNotice/{noticeId}`
- Controller source: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Declared parameters: `string noticeId`
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

## Source Trace Verification Ledger

This appendix supplies source-specific verification records for the UI document. It exists because the requested document must remain independently useful during debugging and testing.

### Ledger 1: route composition

- Anchor: `GET api/Dashboard/GetAllGenderDetails / DashboardController.GetAllGenderDetails()`
- Verification target: route composition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 2: authentication attributes

- Anchor: `GET api/Dashboard/GetApplicationStatusData / DashboardController.GetApplicationStatusData()`
- Verification target: authentication attributes.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 3: module permission

- Anchor: `GET api/Dashboard/GetAllDepartmentDetails / DashboardController.GetAllDepartmentDetails()`
- Verification target: module permission.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 4: request binding

- Anchor: `GET api/Dashboard/GetUpComingHolidayAndEvents / DashboardController.GetUpComingHolidays()`
- Verification target: request binding.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

