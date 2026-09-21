# Company, Calendar and Schedules UI Documentation

**Last verified against code:** 2026-09-02

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## 1. Frontend Source Map

### 1.1 `src/app/modules/admin/companydetails/CompanyDetails.tsx`

- Source lines: 151
- Extracted symbols: CompanyDetailsTab, CompanyDetails, location
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/admin/companydetails/components/ApplicationModules.tsx`

- Source lines: 322
- Extracted symbols: ActionType, dispatch, user, friendlyModuleLabel, translated, closeConfirmationModal, updateModuleAccessApi, updateModuleAccess, updateSingleModule, response, updateBulkModules, requestAction, handleConfirmation, getAllModules, payrollValues, recruitmentValues, payroll, recruitment, remaining, moduleDesc, isDisabling
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/admin/companydetails/components/CompanyInfo.tsx`

- Source lines: 649
- Extracted symbols: SupportedImageFormat, MAX_FILE_SIZE, CompanyInfo, applyEmployeeIdSequence, userData, dispatch, defaultLanguage, applicationLanguages, handleImageCropDiscard, handleImageCropSuccess, handleUpdateCompanyDetails, handleFileChange, files, selectedFile, fileSizeInMB, fileFormat, reader, formData, base64Image, blob, Uint8Array, response, refreshEmployeeIdSequence, handleEditCancel, getEmployeeIdSequence, toggleFormerEmployeePortal, confirmSkip
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/admin/companydetails/components/CompanyInfoSkeleton.tsx`

- Source lines: 50
- Extracted symbols: CompanyInfoSkeleton
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/admin/companydetails/components/ModuleCard.tsx`

- Source lines: 180
- Extracted symbols: ModuleCardProps, installedModule, isInstalled, isHardDisabled, isDisabled
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/admin/companymaster/CompanyMaster.tsx`

- Source lines: 193
- Extracted symbols: getCustomAttributeTitle, CompanyMasterTab, CompanyMaster, navigate, location, AddNewCustomAttribute, existingDraft, UpdateCustomAttributeValue, exists, RemoveCustomAttribute, namedCustomAttributes, active, path
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/admin/companymaster/components/AddNewAttributeItem.tsx`

- Source lines: 165
- Extracted symbols: Props, AddNewAttributeItem, defaultCompanyLanguage, currentLang, translationLanguageTitle, currentLanguageTitle, handleChange, titles, validateAndUpdateNewItemData, index, currentLanguageName, hasDefaultLangTitle, handleSwitchChange, updatedItem, handleSelectChange
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/admin/companymaster/components/AttributeItemRow.tsx`

- Source lines: 181
- Extracted symbols: Props, ReqProps, AttributeItemRow, defaultCompanyLanguage, currentLanguage, translatedTitle, currentTitle, handleChange, updatedLanguage, updatedTitles, updatedItem, handleItemDelete, handleDeleteAttribute, handleSwitchChange, updateItem, hasDefaultLangTitle, handleSelectChange, validateAndUpdateNewItemData, currentLanguageName
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/admin/companymaster/components/AttributeTable.tsx`

- Source lines: 332
- Extracted symbols: Props, AttributeTable, defaultCompanyLanguage, currentLanguage, itemsRef, newItemsRef, initialAttributeTitle, handleReset, UpdateExistingItemData, updated, UpdateNewItemsList, handleUpdate, allItems, shouldUpdate, handleAttributeUpdate, savedItems, updateAttributeTitle, persistedItems, id, value, selectedLanguage, language, uuid
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/admin/companymaster/components/AttributeTitleRow.tsx`

- Source lines: 105
- Extracted symbols: AttributeTitleProps, AttributeTitleRow, defaultCompanyLanguage, defaultLanguage, translationLanguageTitle, currentLanguageTitle, handleChange, updatedTitle, validateAndUpdateNewItemData, hasDefaultLangTitle
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/admin/companymaster/components/CustomAttribute.tsx`

- Source lines: 175
- Extracted symbols: CustomAttribute, navigate, param, customAttributeId, customAttributeName, attributeValues, fetchCustomAttribute, handleCustomAttributeUpdate, message, handleCustomAttributeItemDelete, response, handleCustomAttributeDelete
- React/data hooks found: useCallback, useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/admin/companymaster/components/CustomAttributeWrapper.tsx`

- Source lines: 11
- Extracted symbols: CustomAttributeWrapper, customAttributeId
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/modules/admin/companymaster/components/Department.tsx`

- Source lines: 87
- Extracted symbols: Departments, handleRefresh, handleDepartmentUpdate, handleDeleteDepartment, response, getAllDeps
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/app/modules/admin/companymaster/components/JobTitles.tsx`

- Source lines: 107
- Extracted symbols: JobTitles, handleRefresh, handleJobTitleUpdate, resp, handleDeleteJobTitle, response, getAllJobTitles
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.15 `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx`

- Source lines: 573
- Extracted symbols: OfficeScheduleForm, DepartmentOption, DepartmentShiftAssignment, toMinutes, formatTime, suffix, twelveHour, formatMinutes, safeMinutes, hours, remainder, ScheduleMetric, OfficeSchedule, visibleSchedules, nextShiftNumber, set, loadSchedules, items, selected, saveWeeklyOffs, schedule, start, end, span, available, required, assignedDepartmentIds, duplicateTimingSchedule, save, saved
- React/data hooks found: useCallback, useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.16 `src/app/modules/holidays/Calendar.tsx`

- Source lines: 171
- Extracted symbols: permissions, canImport, handleCategoryChange, handleRefresh, handleSelectedDateChange, readImportFile, file, workbook, sheetRows, normaliseColumn, headerIndex, columns, headers, dateColumn, nameColumn, typeColumn, rows, rawDate, formattedDate, name, sourceType, submitImport, fetchedHolidayData
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.17 `src/app/modules/holidays/calendarServices.ts`

- Source lines: 49
- Extracted symbols: getAllCalendarItem, postRequest, addEditCalendarItem, deleteCalendarItem, deleteRequest, getHolidays, body, importCalendarItems
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.18 `src/app/modules/holidays/components/CalendarDays.tsx`

- Source lines: 365
- Extracted symbols: Props, muiTheme, popoverOpen, isSmallScreen, weekDayShort, open, getDaysInCurrentMonth, activeDate, firstDateOfMonth, daysInCurrentMonth, checkIsSameDay, today, dayjs, findOccasionsForMonth, occassionMonth, handlePopoverOpen, handlePopoverClose, checkForOccasionForDate, holidayDate, showDayOccassion, handleCloseSmlPopover, updatedDate, date, PropType, ToolTipPopOver
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.19 `src/app/modules/holidays/components/CalendarHeader.tsx`

- Source lines: 93
- Extracted symbols: Props, increaseMonth, decreaseMonth
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.20 `src/app/modules/holidays/components/CalendarSidebar.tsx`

- Source lines: 272
- Extracted symbols: Props, CalendarItems, modulePermission, hasAddPermission, fetchedHolidayData, response, handleClose, findUpComingOccassions, today, thisMonthHolidaysFinder, occDate, getMonthSummary, holidayCounter, eventCounter, birthdayCounter, anniversaryCounter, holidayMonth, findHolidaysForDay, occassionDate
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.21 `src/app/modules/holidays/components/HolidayFilter.tsx`

- Source lines: 126
- Extracted symbols: PropsType, dispatch, appliedHolFilters, size, value
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.22 `src/app/modules/holidays/components/HolidayForm.tsx`

- Source lines: 292
- Extracted symbols: PropsTypes, handleFileChange, files, selectedFile, handleAddUpdateHoliday, formData
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.23 `src/app/modules/holidays/components/UpcomingHolidayCard.tsx`

- Source lines: 277
- Extracted symbols: Props, UpcomingHolidayCard, modulePermission, hasEditPermission, hasDeletePermission, handleActionButtonClick, handleActionMenuClose, handleHolidayDeleteClick, handleHolidayEdit, holiday, handleCloseModal, handleDeleteModalClose, handleHolidayDeletion, date
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.24 `src/app/modules/holidays/models.ts`

- Source lines: 58
- Extracted symbols: OccasionModel, OccasionResponseModel, CalendarItemType, GetCalendarItemPayload, CalendarItem, HolidayFilterModel, HolidayPayload, HolidayModel, GetAllCalendarItemData, AddHolidayResponseData, CalendarImportItem
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `GetAllCalendarItems` | `POST api/Calendar/GetAllCalendarItems` | Search service functions and component event handlers for this route/action. |
| 2 | `AddUpdateCalendarItem` | `POST api/Calendar/AddUpdateCalendarItem` | Search service functions and component event handlers for this route/action. |
| 3 | `ImportCalendarItems` | `POST api/Calendar/ImportCalendarItems` | Search service functions and component event handlers for this route/action. |
| 4 | `DeleteCalendarItem` | `DELETE api/Calendar/DeleteCalendarItem/{itemId}` | Search service functions and component event handlers for this route/action. |
| 5 | `GetHolidays` | `POST api/Calendar/GetHolidays` | Search service functions and component event handlers for this route/action. |
| 6 | `GetAllCompanyList` | `GET api/Company/GetAllCompanyList` | Search service functions and component event handlers for this route/action. |
| 7 | `GetCompanyDetails` | `GET api/Company/GetCompanyDetails` | Search service functions and component event handlers for this route/action. |
| 8 | `GetPublicCareerCompany` | `GET api/Company/career` | Search service functions and component event handlers for this route/action. |
| 9 | `GetPublicCareerCompany` | `GET api/Company/career/{publicCompanyCode}` | Search service functions and component event handlers for this route/action. |
| 10 | `GetCurrentCompanyTheme` | `GET api/Company/theme` | Search service functions and component event handlers for this route/action. |
| 11 | `UpdateCompanyDetails` | `POST api/Company/UpdateCompanyDetails` | Search service functions and component event handlers for this route/action. |
| 12 | `AddPolicy` | `POST api/Company/AddPolicy` | Search service functions and component event handlers for this route/action. |
| 13 | `UpdatePolicy` | `POST api/Company/UpdatePolicy` | Search service functions and component event handlers for this route/action. |
| 14 | `GetAllPolicies` | `GET api/Company/GetAllPolicies` | Search service functions and component event handlers for this route/action. |
| 15 | `DeletePolicy` | `DELETE api/Company/DeletePolicy/{policyId}` | Search service functions and component event handlers for this route/action. |
| 16 | `AddPolicyVersion` | `POST api/Company/CreatePolicyVersion` | Search service functions and component event handlers for this route/action. |
| 17 | `EditPolicyVersion` | `POST api/Company/EditPolicyVersion` | Search service functions and component event handlers for this route/action. |
| 18 | `SetCurrentPolicyVersion` | `POST api/Company/SetCurrentPolicyVersion` | Search service functions and component event handlers for this route/action. |
| 19 | `GetAllPolicyVersion` | `GET api/Company/GetAllPolicyVersion/{policyId}` | Search service functions and component event handlers for this route/action. |
| 20 | `DeletePolicyVersion` | `DELETE api/Company/DeletePolicyVersion/{policyVersionId}` | Search service functions and component event handlers for this route/action. |
| 21 | `GetPolicyDocument` | `GET api/Company/GetPolicyDocument/{policyVersionId}` | Search service functions and component event handlers for this route/action. |
| 22 | `GetOfficeSchedule` | `GET api/CompanyMaster/OfficeSchedule` | Search service functions and component event handlers for this route/action. |
| 23 | `GetOfficeSchedules` | `GET api/CompanyMaster/OfficeSchedules` | Search service functions and component event handlers for this route/action. |
| 24 | `SaveOfficeSchedule` | `PUT api/CompanyMaster/OfficeSchedule` | Search service functions and component event handlers for this route/action. |
| 25 | `DeleteOfficeSchedule` | `DELETE api/CompanyMaster/OfficeSchedule/{scheduleId}` | Search service functions and component event handlers for this route/action. |
| 26 | `GetDepartmentOfficeScheduleAssignments` | `GET api/CompanyMaster/DepartmentOfficeScheduleAssignments` | Search service functions and component event handlers for this route/action. |
| 27 | `SetDepartmentOfficeScheduleAssignment` | `PUT api/CompanyMaster/DepartmentOfficeScheduleAssignment/{departmentId}` | Search service functions and component event handlers for this route/action. |
| 28 | `AddEditDepartment` | `POST api/CompanyMaster/UpdateDepartment` | Search service functions and component event handlers for this route/action. |
| 29 | `GetDepartmentList` | `GET api/CompanyMaster/GetDepartmentList` | Search service functions and component event handlers for this route/action. |
| 30 | `DeleteDepartment` | `DELETE api/CompanyMaster/DeleteDepartment/{departmentId}` | Search service functions and component event handlers for this route/action. |
| 31 | `UpdateModuleAccess` | `PATCH api/CompanyMaster/UpdateModuleAccess/{moduleId}` | Search service functions and component event handlers for this route/action. |
| 32 | `GetAllModuleDetails` | `GET api/CompanyMaster/GetAllModuleDetails` | Search service functions and component event handlers for this route/action. |
| 33 | `AddUpdateJobTitle` | `POST api/CompanyMaster/AddUpdateJobTitle` | Search service functions and component event handlers for this route/action. |
| 34 | `GetAllJobTitles` | `GET api/CompanyMaster/GetJobTitles` | Search service functions and component event handlers for this route/action. |
| 35 | `DeleteJobTitle` | `DELETE api/CompanyMaster/DeleteJobTitle/{jobTitleId}` | Search service functions and component event handlers for this route/action. |
| 36 | `CreateCustomAttribute` | `POST api/CompanyMaster/CreateCustomAttribute` | Search service functions and component event handlers for this route/action. |
| 37 | `GetAllCustomAttributes` | `GET api/CompanyMaster/GetAllCustomAttribute` | Search service functions and component event handlers for this route/action. |
| 38 | `GetCustomAttributeById` | `GET api/CompanyMaster/GetCustomAttributeById/{customAttributeId}` | Search service functions and component event handlers for this route/action. |
| 39 | `UpdateCustomAttribute` | `PUT api/CompanyMaster/UpdateCustomAttribute` | Search service functions and component event handlers for this route/action. |
| 40 | `DeleteCustomAttribute` | `DELETE api/CompanyMaster/DeleteCustomAttribute/{customAttributeId}` | Search service functions and component event handlers for this route/action. |
| 41 | `DeleteCustomAttributeValue` | `DELETE api/CompanyMaster/DeleteCustomAttributeValue/{customAttributeValueId}` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `GetAllCalendarItems`

- Backend endpoint: `POST api/Calendar/GetAllCalendarItems`
- Controller source: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Declared parameters: `[FromBody] CalendarFilters? filters`
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

## 3.2 UI trace for `AddUpdateCalendarItem`

- Backend endpoint: `POST api/Calendar/AddUpdateCalendarItem`
- Controller source: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Declared parameters: `[FromForm] CalendarRequestDto model`
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

## 3.3 UI trace for `ImportCalendarItems`

- Backend endpoint: `POST api/Calendar/ImportCalendarItems`
- Controller source: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Declared parameters: `[FromBody] CalendarBulkImportRequest model`
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
- `src/app/modules/holidays/Calendar.tsx` (matched: ImportCalendarItems)
- `src/app/modules/holidays/calendarServices.ts` (matched: ImportCalendarItems)

## 3.4 UI trace for `DeleteCalendarItem`

- Backend endpoint: `DELETE api/Calendar/DeleteCalendarItem/{itemId}`
- Controller source: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Declared parameters: `string itemId`
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
- `src/app/modules/holidays/calendarServices.ts` (matched: DeleteCalendarItem)
- `src/app/modules/holidays/components/UpcomingHolidayCard.tsx` (matched: DeleteCalendarItem)

## 3.5 UI trace for `GetHolidays`

- Backend endpoint: `POST api/Calendar/GetHolidays`
- Controller source: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Declared parameters: `[FromBody] HolidayFilter filter`
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
- `src/app/modules/holidays/calendarServices.ts` (matched: GetHolidays)

## 3.6 UI trace for `GetAllCompanyList`

- Backend endpoint: `GET api/Company/GetAllCompanyList`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
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

## 3.7 UI trace for `GetCompanyDetails`

- Backend endpoint: `GET api/Company/GetCompanyDetails`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
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
- `src/app/modules/admin/companydetails/components/CompanyInfo.tsx` (matched: GetCompanyDetails)

## 3.8 UI trace for `GetPublicCareerCompany`

- Backend endpoint: `GET api/Company/career`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
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
- `src/app/modules/admin/companydetails/components/CompanyInfo.tsx` (matched: career)

## 3.9 UI trace for `GetPublicCareerCompany`

- Backend endpoint: `GET api/Company/career/{publicCompanyCode}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `string publicCompanyCode`
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
- `src/app/modules/admin/companydetails/components/CompanyInfo.tsx` (matched: career)

## 3.10 UI trace for `GetCurrentCompanyTheme`

- Backend endpoint: `GET api/Company/theme`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
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
- `src/app/modules/admin/companydetails/CompanyDetails.tsx` (matched: theme)
- `src/app/modules/admin/companydetails/components/CompanyInfo.tsx` (matched: theme)
- `src/app/modules/admin/companymaster/CompanyMaster.tsx` (matched: theme)
- `src/app/modules/admin/companymaster/components/AddNewAttributeItem.tsx` (matched: theme)
- `src/app/modules/admin/companymaster/components/AttributeItemRow.tsx` (matched: theme)
- `src/app/modules/admin/companymaster/components/AttributeTable.tsx` (matched: theme)
- `src/app/modules/admin/companymaster/components/AttributeTitleRow.tsx` (matched: theme)
- `src/app/modules/admin/companymaster/components/CustomAttribute.tsx` (matched: theme)
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` (matched: theme)
- `src/app/modules/holidays/components/CalendarDays.tsx` (matched: theme)

## 3.11 UI trace for `UpdateCompanyDetails`

- Backend endpoint: `POST api/Company/UpdateCompanyDetails`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `[FromForm] UpdateCompanyInfoRequestModel model`
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
- `src/app/modules/admin/companydetails/components/CompanyInfo.tsx` (matched: UpdateCompanyDetails)

## 3.12 UI trace for `AddPolicy`

- Backend endpoint: `POST api/Company/AddPolicy`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `[FromBody] CreatePolicyRequestModel model`
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

## 3.13 UI trace for `UpdatePolicy`

- Backend endpoint: `POST api/Company/UpdatePolicy`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `[FromBody] UpdatePolicyRequestModel model`
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

## 3.14 UI trace for `GetAllPolicies`

- Backend endpoint: `GET api/Company/GetAllPolicies`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
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

## 3.15 UI trace for `DeletePolicy`

- Backend endpoint: `DELETE api/Company/DeletePolicy/{policyId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
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

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.16 UI trace for `AddPolicyVersion`

- Backend endpoint: `POST api/Company/CreatePolicyVersion`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `[FromForm] PolicyVersionRequestModel model`
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

## 3.17 UI trace for `EditPolicyVersion`

- Backend endpoint: `POST api/Company/EditPolicyVersion`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `[FromForm] PolicyVersionUpdateModel model`
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

## 3.18 UI trace for `SetCurrentPolicyVersion`

- Backend endpoint: `POST api/Company/SetCurrentPolicyVersion`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `[FromBody] SetCurrentPolicyVersionRequestModel model`
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

## 3.19 UI trace for `GetAllPolicyVersion`

- Backend endpoint: `GET api/Company/GetAllPolicyVersion/{policyId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
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

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.20 UI trace for `DeletePolicyVersion`

- Backend endpoint: `DELETE api/Company/DeletePolicyVersion/{policyVersionId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `string policyVersionId`
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

## 3.21 UI trace for `GetPolicyDocument`

- Backend endpoint: `GET api/Company/GetPolicyDocument/{policyVersionId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Declared parameters: `string policyVersionId`
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

## 3.22 UI trace for `GetOfficeSchedule`

- Backend endpoint: `GET api/CompanyMaster/OfficeSchedule`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `CancellationToken cancellationToken`
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
- `src/app/modules/admin/companydetails/CompanyDetails.tsx` (matched: OfficeSchedule)
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` (matched: OfficeSchedule)

## 3.23 UI trace for `GetOfficeSchedules`

- Backend endpoint: `GET api/CompanyMaster/OfficeSchedules`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `CancellationToken cancellationToken`
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
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` (matched: OfficeSchedules)

## 3.24 UI trace for `SaveOfficeSchedule`

- Backend endpoint: `PUT api/CompanyMaster/OfficeSchedule`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `OfficeScheduleSettingsDto request, CancellationToken cancellationToken`
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
- `src/app/modules/admin/companydetails/CompanyDetails.tsx` (matched: OfficeSchedule)
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` (matched: OfficeSchedule)

## 3.25 UI trace for `DeleteOfficeSchedule`

- Backend endpoint: `DELETE api/CompanyMaster/OfficeSchedule/{scheduleId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `string scheduleId, CancellationToken cancellationToken`
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
- `src/app/modules/admin/companydetails/CompanyDetails.tsx` (matched: OfficeSchedule)
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` (matched: OfficeSchedule)

## 3.26 UI trace for `GetDepartmentOfficeScheduleAssignments`

- Backend endpoint: `GET api/CompanyMaster/DepartmentOfficeScheduleAssignments`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `CancellationToken cancellationToken`
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
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` (matched: DepartmentOfficeScheduleAssignments)

## 3.27 UI trace for `SetDepartmentOfficeScheduleAssignment`

- Backend endpoint: `PUT api/CompanyMaster/DepartmentOfficeScheduleAssignment/{departmentId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `string departmentId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken`
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
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` (matched: DepartmentOfficeScheduleAssignment)

## 3.28 UI trace for `AddEditDepartment`

- Backend endpoint: `POST api/CompanyMaster/UpdateDepartment`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `List<DepartmentRequestDto> data`
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
- `src/app/modules/admin/companymaster/components/Department.tsx` (matched: UpdateDepartment)

## 3.29 UI trace for `GetDepartmentList`

- Backend endpoint: `GET api/CompanyMaster/GetDepartmentList`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `[FromQuery] bool? isActive`
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
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` (matched: GetDepartmentList)

## 3.30 UI trace for `DeleteDepartment`

- Backend endpoint: `DELETE api/CompanyMaster/DeleteDepartment/{departmentId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `string departmentId`
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
- `src/app/modules/admin/companymaster/components/Department.tsx` (matched: DeleteDepartment)

## 3.31 UI trace for `UpdateModuleAccess`

- Backend endpoint: `PATCH api/CompanyMaster/UpdateModuleAccess/{moduleId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `string moduleId`
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
- `src/app/modules/admin/companydetails/components/ApplicationModules.tsx` (matched: UpdateModuleAccess)

## 3.32 UI trace for `GetAllModuleDetails`

- Backend endpoint: `GET api/CompanyMaster/GetAllModuleDetails`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
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

## 3.33 UI trace for `AddUpdateJobTitle`

- Backend endpoint: `POST api/CompanyMaster/AddUpdateJobTitle`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `List<JobTitleRequestDto> data`
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
- `src/app/modules/admin/companymaster/components/JobTitles.tsx` (matched: AddUpdateJobTitle)

## 3.34 UI trace for `GetAllJobTitles`

- Backend endpoint: `GET api/CompanyMaster/GetJobTitles`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `[FromQuery] bool? isActive, [FromQuery] string? departmentId`
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
- `src/app/modules/admin/companymaster/components/JobTitles.tsx` (matched: GetAllJobTitles, GetJobTitles)

## 3.35 UI trace for `DeleteJobTitle`

- Backend endpoint: `DELETE api/CompanyMaster/DeleteJobTitle/{jobTitleId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `string jobTitleId`
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
- `src/app/modules/admin/companymaster/components/JobTitles.tsx` (matched: DeleteJobTitle)

## 3.36 UI trace for `CreateCustomAttribute`

- Backend endpoint: `POST api/CompanyMaster/CreateCustomAttribute`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
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

## 3.37 UI trace for `GetAllCustomAttributes`

- Backend endpoint: `GET api/CompanyMaster/GetAllCustomAttribute`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
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
- `src/app/modules/admin/companymaster/CompanyMaster.tsx` (matched: GetAllCustomAttributes, GetAllCustomAttribute)

## 3.38 UI trace for `GetCustomAttributeById`

- Backend endpoint: `GET api/CompanyMaster/GetCustomAttributeById/{customAttributeId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `string customAttributeId, [FromQuery] bool? active`
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
- `src/app/modules/admin/companymaster/components/CustomAttribute.tsx` (matched: GetCustomAttributeById)

## 3.39 UI trace for `UpdateCustomAttribute`

- Backend endpoint: `PUT api/CompanyMaster/UpdateCustomAttribute`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `[FromBody] CustomAttributeRequestDto data`
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
- `src/app/modules/admin/companymaster/CompanyMaster.tsx` (matched: UpdateCustomAttribute)
- `src/app/modules/admin/companymaster/components/CustomAttribute.tsx` (matched: UpdateCustomAttribute)

## 3.40 UI trace for `DeleteCustomAttribute`

- Backend endpoint: `DELETE api/CompanyMaster/DeleteCustomAttribute/{customAttributeId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `string customAttributeId`
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
- `src/app/modules/admin/companymaster/components/AttributeTable.tsx` (matched: DeleteCustomAttribute)
- `src/app/modules/admin/companymaster/components/CustomAttribute.tsx` (matched: DeleteCustomAttribute)

## 3.41 UI trace for `DeleteCustomAttributeValue`

- Backend endpoint: `DELETE api/CompanyMaster/DeleteCustomAttributeValue/{customAttributeValueId}`
- Controller source: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Declared parameters: `string customAttributeValueId`
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

