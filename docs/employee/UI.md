# Employee UI Documentation

**Last verified against code:** 2026-09-07

## Lifecycle management UI

The employee grid displays separate employment and availability badges. Authorized HR/admin users can open the responsive lifecycle dialog to manage temporary availability, expected return date, reason, and Resume Work. The dialog lazy-loads the protected lifecycle history timeline and shows loading, empty, error-safe, and saving states. It does not provide a direct exit shortcut; completed exits remain in the existing EmployeeExit/offboarding workflow.

The employee filter now supports both Employment status and Availability status. They are combined with department, gender, and name filters; an empty availability selection means all availability values.

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## 1. Frontend Source Map

### 1.1 `src/app/modules/employees/components/AddEmployeeStepper.tsx`

- Source lines: 443
- Extracted symbols: AddEmployeeSteps, PropsTypes, steps, AddEmployeeStepper, salaryUpdateRef, inviteFormRef, updateFormRef, leaveAllocateFormRef, incrementStep, handleInviteUser, handleSkip, handleButtonLoading, handleClick, ColorlibConnector, ColorlibStepIconRoot, ColorlibStepIcon
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/employees/components/BulkImportEmployeesModal.tsx`

- Source lines: 321
- Extracted symbols: Props, validImportFileExtensions, normalizeHeader, getMappedHeader, mapRowsToBulkEmployees, headers, headerMap, BulkImportEmployeesModal, bulkImportInputRef, handleBulkImportClick, handleDownloadSample, csv, blob, url, anchor, handleBulkImportFileChange, file, fileExt, reader, data, workbook, firstSheetName, sheet, rawRows, parsedEmployees, handleSave, methodResult, createdCount, failedCount, total
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/employees/components/EmployeeCardSkeleton.tsx`

- Source lines: 28
- Extracted symbols: EmpCardSkeleton
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/employees/components/EmployeeFilter.tsx`

- Source lines: 225
- Extracted symbols: employmentStatusOptions, dispatch, appliedEmpFilters, defaultLanguage, canFilterEmploymentStatus, getAllDepartments, response, size, value
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/employees/components/EmployeeGridView.tsx`

- Source lines: 109
- Extracted symbols: Props, currentEmpId, navigate, defaultLang, handleClick
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/employees/components/EmployeePayrollPenaltyPolicy.tsx`

- Source lines: 134
- Extracted symbols: EmployeePenaltyPolicy, initialPolicy, EmployeePayrollPenaltyPolicyHandle, EmployeePayrollPenaltyPolicy, load, item, save
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/employees/components/InviteNewEmployee.tsx`

- Source lines: 165
- Extracted symbols: PropsTypes, InviteNewEmployee, message, fetchNextEmployeeId, getRequest, getAllEmployees, validationSchema
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/employees/components/LeaveAllocateForm.tsx`

- Source lines: 210
- Extracted symbols: PropsTypes, LeaveAllocateForm, AllocateEmployeeLeaveBalance, initialValues, handleCheck, isSelected
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/employees/components/OnlineUsersListener.tsx`

- Source lines: 44
- Extracted symbols: OnlineUsersListener, dispatch, chatHub, setupListener, chatHubConnection, handleUserOnlineStatus, handleUserOfflineStatus
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/employees/components/SetDepartmentName.tsx`

- Source lines: 15
- Extracted symbols: SetDepartmentName, defaultLang
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/employees/components/UpdateEmployeeData.tsx`

- Source lines: 523
- Extracted symbols: PropsTypes, UpdateEmployeeData, defaultLang, UpdateEmployeeDetails, payload, response, result
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/employees/components/UpdateEmployeeSalaryData.tsx`

- Source lines: 229
- Extracted symbols: PropsTypes, UpdateEmployeeSalaryData, UpadteSalaryData, payload, response, onUserUpdate, name, label
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/modules/employees/Employees.tsx`

- Source lines: 551
- Extracted symbols: CustomDataGrid, ViewMode, useEmployeeTableColumnList, modulePermission, canInviteEmployee, onlineUsers, canViewEmployeePhoneNumbers, InvitationMailButton, sentInvitationMail, Employees, navigate, handleCloseCkeckModal, open, id, userModulesPermissions, userPermissionForModule, currentEmpId, userRoleType, employeeFilters, employmentStatus, totalAppliedFilter, columns, handlePaginationModelChange, rowClickHandler, empId, handleViewChange, handleFilterClick, handleReresh, handleBulkExport, response
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/app/modules/employees/employeeServices.ts`

- Source lines: 222
- Extracted symbols: updateEmployee, postRequest, EditOwnProfilePayloadModel, updateOwnProfile, updateSalary, deleteEmployee, deleteRequest, getAllEmployees, initiateEmployeeExit, completeEmployeeExit, employeeUserId, getDirectoryDepartments, submitOwnResignation, getOwnEmployeeExitCase, getOwnEmployeeExitHistory, withdrawOwnEmployeeExit, getEmployeeExitCaseForManagement, reviewEmployeeExit, respondToEmployeeExitDiscussion, respondToEmployeeExitJointDiscussion, requestEmployeeExitNoticeAdjustment, updateEmployeeExitPostExitContact, acknowledgeEmployeeExit, createEmployeeExitFormerEmployeeRequest, resolveEmployeeExitFormerEmployeeRequest, issueEmployeeExitDocument, createFormerEmployeePortalInvite
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.15 `src/app/modules/employees/models.ts`

- Source lines: 163
- Extracted symbols: GetAllEmployeeResponseModel, EmployeeFilterModel, InitiateEmployeeExitRequest, GetAllEmployeePayloadModel, EmployeeModel, InviteNewEmployeeModel, InviteEmployeeResponse, EmployeeLeaveBalance, EditEmployeeResponseData, CreateEmployeeSalaryResponseData, GetAllEmployeeResponseData, SalaryModel, CreateEmployeeSalary, BulkImportEmployeeModel, BulkImportEmployeesRequest, BulkImportEmployeeResult, BulkImportEmployeesResponse
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.16 `src/app/modules/users/about/AboutUser.tsx`

- Source lines: 32
- Extracted symbols: 
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.17 `src/app/modules/users/about/components/AddUpdateCertification.tsx`

- Source lines: 168
- Extracted symbols: PropTypes
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.18 `src/app/modules/users/about/components/AddUpdateEducation.tsx`

- Source lines: 268
- Extracted symbols: PropTypes, debouncedValue, employeeId, handleAddUpdateEducation, response, handleSearchChange, handlePopUpClose
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.19 `src/app/modules/users/about/components/AddUpdateSkills.tsx`

- Source lines: 197
- Extracted symbols: PropsType, employeeId, debouncedSearch, handleClose, handleListClose, handleSelectedSkillsChange, exist, handleSkillUpdate, selectedSkillsId, response, sanitizedValue, active, size, options
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.20 `src/app/modules/users/about/components/ListSkeleton.tsx`

- Source lines: 29
- Extracted symbols: ListSkeleton
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.21 `src/app/modules/users/about/components/PersonalDetails.tsx`

- Source lines: 328
- Extracted symbols: PersonalDetails, employeeId, userPermissions, hasEmployeeEditPermission, defaultLanguage, canAssignEmployeeId, openEmployeeIdAssignment, requestEmployeeIdAssignment, assignEmployeeId
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.22 `src/app/modules/users/about/components/ProfileHeader.tsx`

- Source lines: 52
- Extracted symbols: ProfileHeader, companyName, theme, experience, totalYearAtCurrentCompany
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.23 `src/app/modules/users/about/components/profileHeaderUtils.test.ts`

- Source lines: 16
- Extracted symbols: joinDate
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.24 `src/app/modules/users/about/components/profileHeaderUtils.ts`

- Source lines: 11
- Extracted symbols: getYearsAtCurrentCompany, doj, diff
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.25 `src/app/modules/users/about/components/ProtectedRoute.tsx`

- Source lines: 34
- Extracted symbols: PropsTypes, userRoleType, modulePermissions, hasAccess
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.26 `src/app/modules/users/about/components/UpdateUserInfo.tsx`

- Source lines: 711
- Extracted symbols: Props, transformCustomAttributeData, user, dispatch, employeeId, userPermissions, hasEditPermission, isEditingOwnProfile, canEditEmploymentDetails, defaultLanguage, handleClose, handleUpdateUserDetail, employeeName, response, result, label, PropsType, UserCustomAttribute, defaultLang, handleListOpen, handleValueChange, index, getCurrentValue, data
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.27 `src/app/modules/users/about/components/UserCertification.tsx`

- Source lines: 263
- Extracted symbols: UserCertification, intl, employeeId, userPermissions, hasEmployeeEditPermission, handleAddUpdateCertification, response, handleCertificationEdit, handleClose, closeConfirmationModal, deleteCertification
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.28 `src/app/modules/users/about/components/UserEducation.tsx`

- Source lines: 236
- Extracted symbols: UserEducation, intl, employeeId, userPermissions, hasEmployeeEditPermission, handleClose, handleCloseConfirmation, handleRefresh, handleEducationEdit, handleDeleteEducation, response
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.29 `src/app/modules/users/about/components/UserSkills.tsx`

- Source lines: 84
- Extracted symbols: UserSkills, intl, employeeId, userPermissions, hasEmployeeEditPermission
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.30 `src/app/modules/users/about/components/UserSummary.tsx`

- Source lines: 127
- Extracted symbols: UserSummary, currentUser, userPermissions, hasEmployeeEditPermission, handleClose, handleUpdateSummary, payload, response, id
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.31 `src/app/modules/users/components/EmployeeExitCaseCard.tsx`

- Source lines: 294
- Extracted symbols: ReviewJourney, EmployeeExitCaseCard, withdraw, response, respondToDiscussion, refreshed, savePostExitContact, acknowledge, createRequest, uploadEvidence, payload, useEvidence, preview, url, link, refreshCase, requestNoticeAdjustment, submitHandover, decideOffer, activeCase, pendingNotice
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.32 `src/app/modules/users/components/EmployeeExitManagementCard.tsx`

- Source lines: 1159
- Extracted symbols: Props, ReviewAction, offboardingErrorMessage, EmployeeExitManagementCard, refresh, response, getAllEmployees, run, review, uploadInterviewFile, payload, saveInterview, transferOwnership, enableFormerEmployeePortal, uploadPortalDocument, openHrPortalDocument
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.33 `src/app/modules/users/components/EmployeeExitOperationsPanel.tsx`

- Source lines: 98
- Extracted symbols: Props, SectionProps, Section, initial, completionErrorMessage, itemRowSx, actionRowSx, formGridSx, EmployeeExitOperationsPanel, run, field, pendingChecklist, pendingOperationalWork, totalBlockers, blockers, completeExit, useEvidence, preview, response, url, link
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.34 `src/app/modules/users/components/EmployeeExitPolicySettings.tsx`

- Source lines: 205
- Extracted symbols: PolicyScope, departmentName, policyDepartmentIds, FieldHelp, ToggleTile, CompletionTile, EmployeeExitPolicySettings, companyPolicy, selectedDepartments, selectedRole, makeScopedPolicy, existing, base, load, chooseScope, chooseDepartments, ids, policyScopeLabel, departmentIds, names, role, policyScopeKind, editPolicy, nextScope, addPolicy, toggle, savePolicy, addTemplate, deleteTemplate
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.35 `src/app/modules/users/components/EmployeeOffboardingAction.tsx`

- Source lines: 71
- Extracted symbols: EmployeeOffboardingAction, currentUserId, permissions, isOwnProfile, canManage, status, getEmployeeExitCaseForManagement, exitCase, submit, response
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.36 `src/app/modules/users/components/ImageCropper.tsx`

- Source lines: 155
- Extracted symbols: Props, MIN_DIMENSION, MIN_WIDTH_RECTANGLE, MIN_HEIGHT_RECTANGLE, canvasPreview, ctx, scaleX, scaleY, pixelRatio, cropX, cropY, centerX, centerY, imageRef, canvasRef, intl, onImageLoad, cropWidthInPercent, crop, onCropComplete, canvas, base64, aspect
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.37 `src/app/modules/users/components/LeaveCalendar.tsx`

- Source lines: 208
- Extracted symbols: AttendanceStatusSetting, CalendarDayAttendance, LeaveCalendar, userData, currentUserId, targetUserId, canViewOtherAttendance, mayViewCalendar, load, fromDate, toDate, response, attendanceByDay, days, date, entry, statusByCode, visibleStatuses, renderDay, dateKey, holiday, holidayName, dayAttendance, records, colors, color, background, statusLines
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.38 `src/app/modules/users/components/ProfileCard.tsx`

- Source lines: 251
- Extracted symbols: theme, profileImage, employeeId, defaultLanguage, paySlipControlColor, handleClose, handleDateChange, viewPaySlip, response, fileURL, errorData, errorJson
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.39 `src/app/modules/users/components/ProfileUpdateModal.tsx`

- Source lines: 206
- Extracted symbols: Props, SupportedImageFormat, MAX_IMAGE_SIZE, dispatch, userData, handleFileChange, files, selectedFile, fileSizeInMB, fileFormat, reader, handleUpdateAvatar, formData, base64Image, blob, Uint8Array, response, handlePictureRemove, handleImageCropDiscard, handleImageCropSuccess
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.40 `src/app/modules/users/components/ResignationAction.tsx`

- Source lines: 61
- Extracted symbols: ResignationAction, currentUserId, isCurrent, submit, response
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.41 `src/app/modules/users/components/WorkShiftCard.tsx`

- Source lines: 114
- Extracted symbols: OfficeSchedule, OfficeScheduleAssignment, formatTime, hour, formatWorkingDays, labels, WorkShiftCard, employeeId, permissions, canManage, load, save, timing, workingDays
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.42 `src/app/modules/users/leavetracker/components/LeaveApplication.tsx`

- Source lines: 759
- Extracted symbols: SelectableLeaveType, Props, getWorkFromHomeErrorMessage, code, details, getLeaveRequestErrorMessage, WorkFromHomeContext, formatOfficeStartTime, hour, minute, suffix, displayHour, LeaveApplication, routeEmployeeId, employeeId, currentUserId, loadWfhEligibility, fetchedHolidayData, response, loadNonBalancePolicies, policies, targetUserId, appliesToEmployee, getDatesBetween, current, checkIfHoliday, checkIfWeekend
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.43 `src/app/modules/users/leavetracker/components/LeaveTypesChart.tsx`

- Source lines: 132
- Extracted symbols: PropsType, EmployeeLeaveTypesCharts, theme, isCompact, leaveTypeLabels, series, chartScale, largestEntitlement, max, step, ticks, balanceSummary
- React/data hooks found: useMemo
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.44 `src/app/modules/users/leavetracker/components/MyLeaveRequest.tsx`

- Source lines: 297
- Extracted symbols: PropsType, MyLeaveRequests, withdrawalInFlight, employeeId, handlePageChange, handleRowPerPageChange, handleLeaveRequestDeletion, handleClose, fetchLeaveRequest, canWithdraw, canEdit
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.45 `src/app/modules/users/leavetracker/LeaveTracker.tsx`

- Source lines: 336
- Extracted symbols: WorkFromHomeContext, WorkFromHomeRequest, WorkFromHomeTiming, employeeId, currentUserId, modulePermission, handleClose, handleOpen, handleRefresh, fetchEmployeeLeaveTypeBalance, wfhContext, wfhRequests, businessDate, todayRequest, getRequest, withdrawWfhRequest, message, canWithdrawWfhRequest, requestDate, toggleWfhAttendance, action
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.46 `src/app/modules/users/leavetracker/leaveTrackerUtils.test.ts`

- Source lines: 12
- Extracted symbols: 
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.47 `src/app/modules/users/leavetracker/leaveTrackerUtils.ts`

- Source lines: 4
- Extracted symbols: resolveLeaveBalanceTargetUserId
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.48 `src/app/modules/users/models.ts`

- Source lines: 152
- Extracted symbols: UploadProfileResponseModel, ProfileProps, SkillModel, UserSkillsModel, EditEmployeePayloadModel, EmployeeCustomAttribute, EmployeeByIdResponse, EmployeeSalaryByIdResponse, NotificationModel, NotificationRequestType, NotificationRequestPayload, GetNotificationResponseModel, WorkHistoryModel, UserCertificationModel, UserEducationModel, UserSummaryModel, ChangePasswordPayload, NotificationPreferences, WorkHistoryResponseModel, GetCollegeListResponseModel, EmployeeSearchResponseModel, CertificationResponseData, UserEducationResponseData, UserSummaryResponseData
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.49 `src/app/modules/users/performance/components/Achievements.tsx`

- Source lines: 284
- Extracted symbols: intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.50 `src/app/modules/users/performance/components/Feedback.tsx`

- Source lines: 123
- Extracted symbols: intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.51 `src/app/modules/users/performance/UserPerformance.tsx`

- Source lines: 61
- Extracted symbols: DonutApexChart, Feedback, Achievements, intl, sx
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.52 `src/app/modules/users/profileTabs.test.ts`

- Source lines: 32
- Extracted symbols: tabs, leaveTab
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.53 `src/app/modules/users/profileTabs.ts`

- Source lines: 70
- Extracted symbols: isLeaveTabEnabled, createTabs, isLeaveEnabled, canAccessHrManagement, getProfileTabs
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.54 `src/app/modules/users/UserDataProvider.tsx`

- Source lines: 82
- Extracted symbols: UserDataContextType, PropTypes, UserDetailsContext, employeeId, updateUserDetails, updateTotalSkills, handleRefresh, useUserDetails, context
- React/data hooks found: useContext, useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.55 `src/app/modules/users/UserProfile.tsx`

- Source lines: 171
- Extracted symbols: UserProfile, navigate, empId, modulePermission, roleType, userModulePermission, tabsRoutes, filteredTabs, spacing, tabTo
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.56 `src/app/modules/users/userservices.ts`

- Source lines: 279
- Extracted symbols: getEmployeeById, getRequest, getEmployeeCertifications, getEmployeeEducations, getEmployeeSummary, getEmployeeSkills, udpateEmployeeSkills, postRequest, addEmployeeEducation, updateEmployeeSummary, addEmployeeCertification, updateEmployeeCertification, updateEmployeeEducation, updateProfilePicture, removeProfilePicture
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.57 `src/app/modules/users/usersetting/components/NotificationSetting.tsx`

- Source lines: 104
- Extracted symbols: NotificationSetting, handleToggle, handleUpdateNotificationPreferences
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.58 `src/app/modules/users/usersetting/components/PasswordResetModal.tsx`

- Source lines: 146
- Extracted symbols: PropsType, PasswordResetModel, handleClickShowPassword, handleUpdate
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.59 `src/app/modules/users/usersetting/Setting.tsx`

- Source lines: 44
- Extracted symbols: intl, handleCloseModal, handleOpenModal
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.60 `src/app/modules/users/workhistory/components/AddUpdateWorkHistory.tsx`

- Source lines: 174
- Extracted symbols: Props, employeeId, handleAddUpdateWorkHistory
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.61 `src/app/modules/users/workhistory/WorkHistory.tsx`

- Source lines: 234
- Extracted symbols: countTotalExperience, totalMonths, years, months, WorkHistory, intl, employeeId, userModulesPermissions, hasEditPermission, handleClose, handleConfirmationClose, handleRefresh, handleWorkHistoryEdit, handleWorkHistoryDelete
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.62 `src/app/routing/ProfileRoutes.tsx`

- Source lines: 57
- Extracted symbols: UserProfile, AboutUser, UserPerformance, WorkHistory, Setting, LeaveTracker, SuspensedView, ProfileTabs, empId, roleType, modulePermission, hasLeavePanelAccess
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `InviteNewEmployee` | `POST api/User/InviteNewEmployee` | Search service functions and component event handlers for this route/action. |
| 2 | `BulkImportEmployees` | `POST api/User/BulkImportEmployees` | Search service functions and component event handlers for this route/action. |
| 3 | `GetNextEmployeeId` | `GET api/User/GetNextEmployeeId` | Search service functions and component event handlers for this route/action. |
| 4 | `EditEmployees` | `POST api/User/EditEmployees` | Search service functions and component event handlers for this route/action. |
| 5 | `EditOwnProfile` | `POST api/User/EditOwnProfile` | Search service functions and component event handlers for this route/action. |
| 6 | `GetOfficeScheduleAssignment` | `GET api/User/OfficeScheduleAssignment/{userId}` | Search service functions and component event handlers for this route/action. |
| 7 | `GetMyOfficeScheduleAssignment` | `GET api/User/MyOfficeScheduleAssignment` | Search service functions and component event handlers for this route/action. |
| 8 | `GetAvailableOfficeSchedules` | `GET api/User/AvailableOfficeSchedules` | Search service functions and component event handlers for this route/action. |
| 9 | `SetOfficeScheduleAssignment` | `PUT api/User/OfficeScheduleAssignment/{userId}` | Search service functions and component event handlers for this route/action. |
| 10 | `GetEmployeeIdSequence` | `GET api/User/EmployeeIdSequence` | Search service functions and component event handlers for this route/action. |
| 11 | `SkipEmployeeIds` | `POST api/User/EmployeeIdSequence/Skip` | Search service functions and component event handlers for this route/action. |
| 12 | `StartEmployeeIdSequence` | `POST api/User/EmployeeIdSequence/StartFrom` | Search service functions and component event handlers for this route/action. |
| 13 | `AssignMissingEmployeeId` | `POST api/User/Employees/{userId}/AssignEmployeeId` | Search service functions and component event handlers for this route/action. |
| 14 | `GetAllEmployees` | `POST api/User/GetAllEmployees` | Search service functions and component event handlers for this route/action. |
| 15 | `GetExEmployees` | `POST api/User/GetExEmployees` | Search service functions and component event handlers for this route/action. |
| 16 | `GetDirectoryDepartments` | `GET api/User/GetDirectoryDepartments` | Search service functions and component event handlers for this route/action. |
| 17 | `ChangePassword` | `POST api/User/ChangePassword` | Search service functions and component event handlers for this route/action. |
| 18 | `GetEmployeeById` | `GET api/User/GetEmployeeById` | Search service functions and component event handlers for this route/action. |
| 19 | `SearchEmployeeByName` | `GET api/User/Search` | Search service functions and component event handlers for this route/action. |
| 20 | `AddEditEmployeeSummary` | `POST api/User/AddEditEmployeeSummary` | Search service functions and component event handlers for this route/action. |
| 21 | `AddEmployeeEducation` | `POST api/User/AddEmployeeEducation` | Search service functions and component event handlers for this route/action. |
| 22 | `EditEmployeeEducation` | `POST api/User/EditEmployeeEducation` | Search service functions and component event handlers for this route/action. |
| 23 | `AddEmployeeCertification` | `POST api/User/AddEmployeeCertification` | Search service functions and component event handlers for this route/action. |
| 24 | `EditEmployeeCertification` | `POST api/User/EditEmployeeCertification` | Search service functions and component event handlers for this route/action. |
| 25 | `GetEmployeeSummary` | `GET api/User/GetEmployeeSummary` | Search service functions and component event handlers for this route/action. |
| 26 | `GetEmployeeEducationDetails` | `GET api/User/GetEmployeeEducationDetails` | Search service functions and component event handlers for this route/action. |
| 27 | `GetEmployeeCertificationDetails` | `GET api/User/GetEmployeeCertificationDetails` | Search service functions and component event handlers for this route/action. |
| 28 | `UploadUserImage` | `POST api/User/UploadUserImage` | Search service functions and component event handlers for this route/action. |
| 29 | `AddEditSkills` | `POST api/User/AddEditEmpSkills` | Search service functions and component event handlers for this route/action. |
| 30 | `GetEmployeeSkills` | `GET api/User/GetEmployeeSkills` | Search service functions and component event handlers for this route/action. |
| 31 | `DeleteEducationDetails` | `DELETE api/User/DeleteEducationDetails/{educationId}` | Search service functions and component event handlers for this route/action. |
| 32 | `DeleteCertificationDetails` | `DELETE api/User/DeleteCertificationDetails/{certificationId}` | Search service functions and component event handlers for this route/action. |
| 33 | `DeleteEmployee` | `DELETE api/User/DeleteEmployee` | Search service functions and component event handlers for this route/action. |
| 34 | `GetSuggestedSkills` | `GET api/User/GetSuggestedSkills` | Search service functions and component event handlers for this route/action. |
| 35 | `AddSkill` | `POST api/User/AddSkill` | Search service functions and component event handlers for this route/action. |
| 36 | `GetAllNotifications` | `GET api/User/GetAllNotifications` | Search service functions and component event handlers for this route/action. |
| 37 | `MarkNotificationAsRead` | `PATCH api/User/Notification/{userNotificationId}/MarkAsRead` | Search service functions and component event handlers for this route/action. |
| 38 | `AddWorkHistory` | `POST api/User/AddUpdateWorkHistory` | Search service functions and component event handlers for this route/action. |
| 39 | `GetEmpWorkHistory` | `GET api/User/GetEmpWorkHistory` | Search service functions and component event handlers for this route/action. |
| 40 | `DeleteWorkHistory` | `DELETE api/User/DeleteWorkHistory/{workId}` | Search service functions and component event handlers for this route/action. |
| 41 | `MarkAllNotificationAsRead` | `POST api/User/MarkAllNotificationAsRead` | Search service functions and component event handlers for this route/action. |
| 42 | `RemoveProfileImage` | `DELETE api/User/RemoveProfileImage` | Search service functions and component event handlers for this route/action. |
| 43 | `GetCollegeList` | `GET api/User/GetCollegeList` | Search service functions and component event handlers for this route/action. |
| 44 | `GetNotificationPreferences` | `GET api/User/GetNotificationPreferences` | Search service functions and component event handlers for this route/action. |
| 45 | `UpdateNotificationPreferences` | `POST api/User/UpdateNotificationPreferences` | Search service functions and component event handlers for this route/action. |
| 46 | `ResendInviteLink` | `POST api/User/ResendInvite/{userId}` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `InviteNewEmployee`

- Backend endpoint: `POST api/User/InviteNewEmployee`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] InviteEmployeeDto model`
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
- `src/app/modules/employees/components/AddEmployeeStepper.tsx` (matched: InviteNewEmployee)
- `src/app/modules/employees/components/InviteNewEmployee.tsx` (matched: InviteNewEmployee)
- `src/app/modules/employees/components/UpdateEmployeeData.tsx` (matched: InviteNewEmployee)
- `src/app/modules/employees/employeeServices.ts` (matched: InviteNewEmployee)
- `src/app/modules/employees/models.ts` (matched: InviteNewEmployee)

## 3.2 UI trace for `BulkImportEmployees`

- Backend endpoint: `POST api/User/BulkImportEmployees`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] BulkImportEmployeesRequestDto model`
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
- `src/app/modules/employees/components/BulkImportEmployeesModal.tsx` (matched: BulkImportEmployees)
- `src/app/modules/employees/Employees.tsx` (matched: BulkImportEmployees)
- `src/app/modules/employees/employeeServices.ts` (matched: BulkImportEmployees)
- `src/app/modules/employees/models.ts` (matched: BulkImportEmployees)

## 3.3 UI trace for `GetNextEmployeeId`

- Backend endpoint: `GET api/User/GetNextEmployeeId`
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
- `src/app/modules/employees/components/InviteNewEmployee.tsx` (matched: GetNextEmployeeId)
- `src/app/modules/users/about/components/PersonalDetails.tsx` (matched: GetNextEmployeeId)
- `src/app/modules/users/userservices.ts` (matched: GetNextEmployeeId)

## 3.4 UI trace for `EditEmployees`

- Backend endpoint: `POST api/User/EditEmployees`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `EmployeePersonalInfo user, string userId`
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

## 3.5 UI trace for `EditOwnProfile`

- Backend endpoint: `POST api/User/EditOwnProfile`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `EmployeeSelfEditDto user`
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
- `src/app/modules/employees/employeeServices.ts` (matched: EditOwnProfile)

## 3.6 UI trace for `GetOfficeScheduleAssignment`

- Backend endpoint: `GET api/User/OfficeScheduleAssignment/{userId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string userId, CancellationToken cancellationToken`
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
- `src/app/modules/users/components/WorkShiftCard.tsx` (matched: OfficeScheduleAssignment)

## 3.7 UI trace for `GetMyOfficeScheduleAssignment`

- Backend endpoint: `GET api/User/MyOfficeScheduleAssignment`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
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
- `src/app/modules/users/components/WorkShiftCard.tsx` (matched: MyOfficeScheduleAssignment)

## 3.8 UI trace for `GetAvailableOfficeSchedules`

- Backend endpoint: `GET api/User/AvailableOfficeSchedules`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
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
- `src/app/modules/users/components/WorkShiftCard.tsx` (matched: AvailableOfficeSchedules)

## 3.9 UI trace for `SetOfficeScheduleAssignment`

- Backend endpoint: `PUT api/User/OfficeScheduleAssignment/{userId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string userId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken`
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
- `src/app/modules/users/components/WorkShiftCard.tsx` (matched: OfficeScheduleAssignment)

## 3.10 UI trace for `GetEmployeeIdSequence`

- Backend endpoint: `GET api/User/EmployeeIdSequence`
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

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.11 UI trace for `SkipEmployeeIds`

- Backend endpoint: `POST api/User/EmployeeIdSequence/Skip`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] SkipEmployeeIdsRequestDto model`
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
- `src/app/modules/employees/components/AddEmployeeStepper.tsx` (matched: Skip)
- `src/app/modules/employees/components/InviteNewEmployee.tsx` (matched: Skip)
- `src/app/modules/users/components/ProfileUpdateModal.tsx` (matched: Skip)

## 3.12 UI trace for `StartEmployeeIdSequence`

- Backend endpoint: `POST api/User/EmployeeIdSequence/StartFrom`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] StartEmployeeIdSequenceRequestDto model`
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

## 3.13 UI trace for `AssignMissingEmployeeId`

- Backend endpoint: `POST api/User/Employees/{userId}/AssignEmployeeId`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string userId, [FromBody] AssignMissingEmployeeIdRequestDto model`
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
- `src/app/modules/users/about/components/PersonalDetails.tsx` (matched: AssignMissingEmployeeId, AssignEmployeeId)
- `src/app/modules/users/userservices.ts` (matched: AssignMissingEmployeeId, AssignEmployeeId)

## 3.14 UI trace for `GetAllEmployees`

- Backend endpoint: `POST api/User/GetAllEmployees`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `GetAllEmployeeRequestModel? filters`
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
- `src/app/modules/employees/components/InviteNewEmployee.tsx` (matched: GetAllEmployees)
- `src/app/modules/employees/components/UpdateEmployeeData.tsx` (matched: GetAllEmployees)
- `src/app/modules/employees/Employees.tsx` (matched: GetAllEmployees)
- `src/app/modules/employees/employeeServices.ts` (matched: GetAllEmployees)
- `src/app/modules/users/about/components/UpdateUserInfo.tsx` (matched: GetAllEmployees)
- `src/app/modules/users/components/EmployeeExitManagementCard.tsx` (matched: GetAllEmployees)

## 3.15 UI trace for `GetExEmployees`

- Backend endpoint: `POST api/User/GetExEmployees`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `GetAllEmployeeRequestModel? filters`
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

## 3.16 UI trace for `GetDirectoryDepartments`

- Backend endpoint: `GET api/User/GetDirectoryDepartments`
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
- `src/app/modules/employees/components/EmployeeFilter.tsx` (matched: GetDirectoryDepartments)
- `src/app/modules/employees/employeeServices.ts` (matched: GetDirectoryDepartments)

## 3.17 UI trace for `ChangePassword`

- Backend endpoint: `POST api/User/ChangePassword`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `ChangePasswordRequest passwordModel`
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
- `src/app/modules/users/models.ts` (matched: ChangePassword)
- `src/app/modules/users/userservices.ts` (matched: ChangePassword)
- `src/app/modules/users/usersetting/components/PasswordResetModal.tsx` (matched: ChangePassword)

## 3.18 UI trace for `GetEmployeeById`

- Backend endpoint: `GET api/User/GetEmployeeById`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string userId = null`
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
- `src/app/modules/users/UserDataProvider.tsx` (matched: GetEmployeeById)
- `src/app/modules/users/userservices.ts` (matched: GetEmployeeById)

## 3.19 UI trace for `SearchEmployeeByName`

- Backend endpoint: `GET api/User/Search`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string name`
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
- `src/app/modules/users/about/components/AddUpdateEducation.tsx` (matched: Search)
- `src/app/modules/users/about/components/AddUpdateSkills.tsx` (matched: Search)
- `src/app/modules/users/models.ts` (matched: Search)
- `src/app/modules/users/userservices.ts` (matched: Search)

## 3.20 UI trace for `AddEditEmployeeSummary`

- Backend endpoint: `POST api/User/AddEditEmployeeSummary`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `EmployeeSummaryRequestModel userSummary`
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

## 3.21 UI trace for `AddEmployeeEducation`

- Backend endpoint: `POST api/User/AddEmployeeEducation`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `EmployeeEducationRequestModel educationDetails`
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
- `src/app/modules/users/about/components/AddUpdateEducation.tsx` (matched: AddEmployeeEducation)
- `src/app/modules/users/userservices.ts` (matched: AddEmployeeEducation)

## 3.22 UI trace for `EditEmployeeEducation`

- Backend endpoint: `POST api/User/EditEmployeeEducation`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `EmployeeEducationRequestModel educationDetails`
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

## 3.23 UI trace for `AddEmployeeCertification`

- Backend endpoint: `POST api/User/AddEmployeeCertification`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `EmployeeCertificationRequestModel certificationDetails`
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
- `src/app/modules/users/about/components/UserCertification.tsx` (matched: AddEmployeeCertification)
- `src/app/modules/users/userservices.ts` (matched: AddEmployeeCertification)

## 3.24 UI trace for `EditEmployeeCertification`

- Backend endpoint: `POST api/User/EditEmployeeCertification`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `EmployeeCertificationRequestModel certificationDetails`
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

## 3.25 UI trace for `GetEmployeeSummary`

- Backend endpoint: `GET api/User/GetEmployeeSummary`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string userId = null`
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
- `src/app/modules/users/about/components/UserSummary.tsx` (matched: GetEmployeeSummary)
- `src/app/modules/users/userservices.ts` (matched: GetEmployeeSummary)

## 3.26 UI trace for `GetEmployeeEducationDetails`

- Backend endpoint: `GET api/User/GetEmployeeEducationDetails`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string userId = null`
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

## 3.27 UI trace for `GetEmployeeCertificationDetails`

- Backend endpoint: `GET api/User/GetEmployeeCertificationDetails`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string userId = null`
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

## 3.28 UI trace for `UploadUserImage`

- Backend endpoint: `POST api/User/UploadUserImage`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `IFormFile profilePicture`
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

## 3.29 UI trace for `AddEditSkills`

- Backend endpoint: `POST api/User/AddEditEmpSkills`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `SkillsRequestModel skillsModel`
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

## 3.30 UI trace for `GetEmployeeSkills`

- Backend endpoint: `GET api/User/GetEmployeeSkills`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string userId = null`
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
- `src/app/modules/users/about/components/UserSkills.tsx` (matched: GetEmployeeSkills)
- `src/app/modules/users/userservices.ts` (matched: GetEmployeeSkills)

## 3.31 UI trace for `DeleteEducationDetails`

- Backend endpoint: `DELETE api/User/DeleteEducationDetails/{educationId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string educationId, [FromBody] string userId`
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

## 3.32 UI trace for `DeleteCertificationDetails`

- Backend endpoint: `DELETE api/User/DeleteCertificationDetails/{certificationId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string certificationId, [FromBody] string userId`
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

## 3.33 UI trace for `DeleteEmployee`

- Backend endpoint: `DELETE api/User/DeleteEmployee`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string employeeId`
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
- `src/app/modules/employees/employeeServices.ts` (matched: DeleteEmployee)
- `src/app/modules/users/about/components/UserCertification.tsx` (matched: DeleteEmployee)
- `src/app/modules/users/about/components/UserEducation.tsx` (matched: DeleteEmployee)
- `src/app/modules/users/components/EmployeeExitPolicySettings.tsx` (matched: DeleteEmployee)
- `src/app/modules/users/userservices.ts` (matched: DeleteEmployee)
- `src/app/modules/users/workhistory/WorkHistory.tsx` (matched: DeleteEmployee)

## 3.34 UI trace for `GetSuggestedSkills`

- Backend endpoint: `GET api/User/GetSuggestedSkills`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string query`
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

## 3.35 UI trace for `AddSkill`

- Backend endpoint: `POST api/User/AddSkill`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] string skill`
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
- `src/app/modules/users/userservices.ts` (matched: AddSkill)

## 3.36 UI trace for `GetAllNotifications`

- Backend endpoint: `GET api/User/GetAllNotifications`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] NotificationRequestDTO model`
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

## 3.37 UI trace for `MarkNotificationAsRead`

- Backend endpoint: `PATCH api/User/Notification/{userNotificationId}/MarkAsRead`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string userNotificationId`
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
- `src/app/modules/users/userservices.ts` (matched: MarkNotificationAsRead, MarkAsRead)

## 3.38 UI trace for `AddWorkHistory`

- Backend endpoint: `POST api/User/AddUpdateWorkHistory`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `EmployeeWorkHistoryModel model`
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
- `src/app/modules/users/userservices.ts` (matched: AddUpdateWorkHistory)
- `src/app/modules/users/workhistory/components/AddUpdateWorkHistory.tsx` (matched: AddUpdateWorkHistory)
- `src/app/modules/users/workhistory/WorkHistory.tsx` (matched: AddUpdateWorkHistory)

## 3.39 UI trace for `GetEmpWorkHistory`

- Backend endpoint: `GET api/User/GetEmpWorkHistory`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string? userId`
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

## 3.40 UI trace for `DeleteWorkHistory`

- Backend endpoint: `DELETE api/User/DeleteWorkHistory/{workId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `string workId, [FromBody] string? userId`
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

## 3.41 UI trace for `MarkAllNotificationAsRead`

- Backend endpoint: `POST api/User/MarkAllNotificationAsRead`
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
- `src/app/modules/users/userservices.ts` (matched: MarkAllNotificationAsRead)

## 3.42 UI trace for `RemoveProfileImage`

- Backend endpoint: `DELETE api/User/RemoveProfileImage`
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

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 3.43 UI trace for `GetCollegeList`

- Backend endpoint: `GET api/User/GetCollegeList`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromQuery] string searchValue`
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
- `src/app/modules/users/about/components/AddUpdateEducation.tsx` (matched: GetCollegeList)
- `src/app/modules/users/models.ts` (matched: GetCollegeList)
- `src/app/modules/users/userservices.ts` (matched: GetCollegeList)

## 3.44 UI trace for `GetNotificationPreferences`

- Backend endpoint: `GET api/User/GetNotificationPreferences`
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
- `src/app/modules/users/userservices.ts` (matched: GetNotificationPreferences)
- `src/app/modules/users/usersetting/components/NotificationSetting.tsx` (matched: GetNotificationPreferences)

## 3.45 UI trace for `UpdateNotificationPreferences`

- Backend endpoint: `POST api/User/UpdateNotificationPreferences`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
- Declared parameters: `[FromBody] Dictionary<EnumsHelper.NotificationPreferenceType, bool> preferences`
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
- `src/app/modules/users/userservices.ts` (matched: UpdateNotificationPreferences)
- `src/app/modules/users/usersetting/components/NotificationSetting.tsx` (matched: UpdateNotificationPreferences)

## 3.46 UI trace for `ResendInviteLink`

- Backend endpoint: `POST api/User/ResendInvite/{userId}`
- Controller source: `Codeji.CMS.API/Controllers/UserController.cs`
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

- No textual caller match was found in this module frontend source set. This is not proof that the endpoint is unused; search shared clients and repository-wide callers.

## 4. Form and Interaction Review Rules

- Fields: derive names, types, defaults and optionality from form code and request DTO together.
- Validation: document frontend validation separately from backend rejection.
- Filters/pagination: establish whether state is local or server-backed.
- Retry: confirm whether query libraries retry reads or mutations.
- Accessibility: preserve labels, keyboard operation, focus return and tooltip semantics.
- Permissions: use actual guards/permitted actions; do not introduce role-name guesses.

