# Recruitment and Career Portal UI Documentation

**Last verified against code:** 2026-09-02

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## 1. Frontend Source Map

### 1.1 `src/app/modules/applynow/ApplicationForm.tsx`

- Source lines: 202
- Extracted symbols: Props, intl, type
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/modules/applynow/Career.tsx`

- Source lines: 86
- Extracted symbols: PublicCareerCompany, Career, active, resolveCompany
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/modules/applynow/Component/ApplyJobForm.tsx`

- Source lines: 337
- Extracted symbols: Props, handleFileChange, files, selectedFile, handleApplicationSubmission, formData, uploadResumeResponse, responseData, initialValues, label, check
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/modules/applynow/Component/CareerHeader.tsx`

- Source lines: 125
- Extracted symbols: CareerHeader, theme, isSmallScreen, toggleSmallScreenNavar, navlisnks
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/modules/applynow/Component/CareerHero.tsx`

- Source lines: 54
- Extracted symbols: CareerHero
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/modules/applynow/Component/FindJobsHeader.tsx`

- Source lines: 19
- Extracted symbols: 
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/modules/applynow/Component/Footer.tsx`

- Source lines: 184
- Extracted symbols: intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/modules/applynow/Component/JobInfo.tsx`

- Source lines: 105
- Extracted symbols: Props, handleClose
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/modules/applynow/Component/JobSearchCareer.tsx`

- Source lines: 120
- Extracted symbols: FilterProps, debouncedValue, handleInputChange, handleSelectChange, getActiveVacancy, response
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/modules/applynow/Component/JobsSection.tsx`

- Source lines: 89
- Extracted symbols: Props, theme, isSmallScreen, handleChangePostion, selectedJob, isSelectedJob, isJobSelectedOnSmallScreen
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/modules/applynow/Component/NoVacancy.tsx`

- Source lines: 81
- Extracted symbols: intl, sx
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/app/modules/applynow/Component/WhyJoin.tsx`

- Source lines: 67
- Extracted symbols: WhyJoin
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/app/modules/applynow/MasterCareers.tsx`

- Source lines: 139
- Extracted symbols: MasterCareers, navigate, validCompanyCode, active, request, companyName, value
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/app/modules/applynow/publicCareerService.ts`

- Source lines: 50
- Extracted symbols: getPublicJobs, getCompanyPublicJobs, getPublicJobBySlug, applyForPublicJob, uploadPublicResume, formData, postRequest
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.15 `src/app/modules/applynow/publicCareerTypes.ts`

- Source lines: 77
- Extracted symbols: PublicJobSummary, PublicJobDetails, PublicJobApplicationPayload, PublicJobApplicationResponse
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.16 `src/app/modules/applynow/PublicJobApplyModal.tsx`

- Source lines: 189
- Extracted symbols: Props, initialForm, PublicJobApplyModal, update, submit, uploadPublicResume, fieldSx, maxWidth, options, renderInput, inputProps
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.17 `src/app/modules/applynow/PublicJobDetails.tsx`

- Source lines: 79
- Extracted symbols: PublicJobDetails, description, active
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.18 `src/app/modules/applynow/services.ts`

- Source lines: 9
- Extracted symbols: applyNow, postRequest
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.19 `src/app/modules/careerPortal/components/CareerHeader.tsx`

- Source lines: 62
- Extracted symbols: CareerHeader
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.20 `src/app/modules/careerPortal/components/CompanyCareerTheme.tsx`

- Source lines: 26
- Extracted symbols: Props, CompanyCareerTheme, companyTheme, muiTheme
- React/data hooks found: useMemo
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.21 `src/app/modules/careerPortal/components/CompanyLogo.tsx`

- Source lines: 47
- Extracted symbols: CompanyLogoProps, CompanyLogo, showImage
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.22 `src/app/modules/careerPortal/components/JobCard.tsx`

- Source lines: 73
- Extracted symbols: JobCard, navigate
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.23 `src/app/modules/careerPortal/components/SaveJobButton.tsx`

- Source lines: 47
- Extracted symbols: Props, SaveJobButton, saved, toggle
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.24 `src/app/modules/careerPortal/components/SubscribeModal.tsx`

- Source lines: 102
- Extracted symbols: Props, SubscribeModal, close, doNotShowAgain, submit, subscribe
- React/data hooks found: useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.25 `src/app/modules/careerPortal/context/SavedJobsContext.tsx`

- Source lines: 51
- Extracted symbols: SavedJobsContextValue, SavedJobsContext, SavedJobsProvider, refreshSavedJobs, setJobSaved, next, value, useSavedJobs, context
- React/data hooks found: useCallback, useContext, useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.26 `src/app/modules/careerPortal/hooks/useCareerVisitor.ts`

- Source lines: 28
- Extracted symbols: VISITOR_KEY, SESSION_KEY, createToken, getCareerVisitorToken, value, getCareerSession, setCareerSession, clearCareerSession, useCareerVisitor
- React/data hooks found: useMemo
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.27 `src/app/modules/careerPortal/pages/CareerJobDetails.tsx`

- Source lines: 289
- Extracted symbols: DetailRow, ListSection, CareerJobDetails, description, active, session, value, recent, toggleFollow, nextFollowing
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.28 `src/app/modules/careerPortal/pages/CareerMarketplace.tsx`

- Source lines: 356
- Extracted symbols: Filters, FilterContent, options, CareerMarketplace, theme, desktop, validCode, timer, active, params, activeFilters, companyName
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.29 `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx`

- Source lines: 152
- Extracted symbols: CareerPreferencesPage, load, verify, unsubscribeToken, start, subscribe, savePreferences, updatePreferences, addAlert, value, createAlert, removeAlert, deleteAlert
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.30 `src/app/modules/careerPortal/pages/CareerProfileAdminPage.tsx`

- Source lines: 316
- Extracted symbols: type, parseCommaSeparatedValues, ProfileFieldProps, ProfileField, help, label, HtmlEditor, editorRef, safeValue, updateValue, command, insertLink, url, minRows, role, CsvField, parsedDraft, matchesValue, nextDraft, CareerProfileAdminPage, userData, canEdit, pageHeaderActionsTarget, set, save, saved, publicPageUrl
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.31 `src/app/modules/careerPortal/pages/SavedJobsPage.tsx`

- Source lines: 80
- Extracted symbols: SavedJobsPage, load, remove, unsaveJob
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.32 `src/app/modules/careerPortal/services/careerPortalService.ts`

- Source lines: 156
- Extracted symbols: sessionConfig, discoverJobs, getCompanyProfile, getCompanies, getJobLocations, getManagedCompanyProfile, saveManagedCompanyProfile, saveJob, unsaveJob, getSavedJobs, subscribe, verifySubscription, unsubscribe, getPreferences, updatePreferences, followCompany, unfollowCompany, getFollowStatus, getAlerts, createAlert, deleteAlert, dismissSubscribePopup, getVisitorPreferences, trackCareerEvent
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.33 `src/app/modules/careerPortal/types/careerPortalTypes.ts`

- Source lines: 69
- Extracted symbols: PublicCompanyProfile, SavedCareerJob, CareerPreferences, CompanyFollowStatus, JobAlert
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.34 `src/app/modules/careerPortal/utils/careerSearch.test.ts`

- Source lines: 56
- Extracted symbols: first, params
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.35 `src/app/modules/careerPortal/utils/careerSearch.ts`

- Source lines: 27
- Extracted symbols: CareerSearchFilters, buildCareerSearchParams
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.36 `src/app/modules/recruitment/applications/Applications.tsx`

- Source lines: 406
- Extracted symbols: CustomDataGrid, useApplicantTableColumnList, values, experience, ActionsCell, closePreview, Applications, dispatch, intl, columnList, compactView, responsiveColumns, navigate, applicationFilters, open, id, totalAppliedFilter, userModulesPermissions, jobId, userPermissionsForModule, handleFilterClick, handleSearchChange, rowClickHandler, handlePaginationModelChange, payload, getApplicantList
- React/data hooks found: useEffect, useMemo, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.37 `src/app/modules/recruitment/applications/components/AddApplicant.tsx`

- Source lines: 311
- Extracted symbols: PropsTypes, handleClose, handleFileChange, files, selectedFile, handleAddApplicant, formData, payload, uploadResumeResponse, responseData, getAllVacancy, rowSpacing
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.38 `src/app/modules/recruitment/applications/components/AddComment.tsx`

- Source lines: 59
- Extracted symbols: PropTypes, intl
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.39 `src/app/modules/recruitment/applications/components/ApplicantDetails.tsx`

- Source lines: 526
- Extracted symbols: ApplicantDetails, navigate, currentUser, param, userPermissions, canEditApplicant, payload, getAllComments, response, handleCommentChange, applicantId, handleEditApplicant, handleAddComment, hasStatusChange, commentPayload, commentResponse, handleClose, handleStatusChange, getStatusLabel, configuredStatus, isStatusChangePending, isTerminalStatus, handlePageChange, handleRowPerPageChange, spacing
- React/data hooks found: useCallback, useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.40 `src/app/modules/recruitment/applications/components/ApplicationFilter.tsx`

- Source lines: 364
- Extracted symbols: dispatch, applicationFilters, isTimeOptionOpen, getActiveJobs, response, handleTimeOptionMenuClose, handleClick, handleTimePeriodChange, currentFilters, selectedTimePeriod, updatedFromDate, updatedToDate, size, value, error, type
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.41 `src/app/modules/recruitment/applications/components/ApplicationPreview.tsx`

- Source lines: 132
- Extracted symbols: PropsType, experience, direction
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.42 `src/app/modules/recruitment/applications/components/EditApplicant.tsx`

- Source lines: 510
- Extracted symbols: EditApplicant, navigate, user, param, applicantId, getAllVacancy, response, handleCommentChange, handleAddComment, actionCategory, payload, handleFileChange, files, selectedFile, handleEditApplicant, formData, vacancy, totalExperienceInMonths, resumeUploadResponse, handleModalClose, direction
- React/data hooks found: useCallback, useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.43 `src/app/modules/recruitment/jobs/components/AddEditJob.tsx`

- Source lines: 433
- Extracted symbols: PropsTypes, commaSeparatedJobFields, CommaSeparatedJobField, serializeCommaSeparatedList, parseCommaSeparatedList, FormSwitch, defaultLanguage, handleJobDescriptionChange, setListValue, departmentName, placeholder, error, jobType, name, timeSteps
- React/data hooks found: useCallback, useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.44 `src/app/modules/recruitment/jobs/components/JobFilter.tsx`

- Source lines: 121
- Extracted symbols: jobFilters, dispatch, value, size
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.45 `src/app/modules/recruitment/jobs/components/ViewJobDetails.tsx`

- Source lines: 146
- Extracted symbols: PropsTypes, handleClose
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.46 `src/app/modules/recruitment/jobs/Jobs.tsx`

- Source lines: 488
- Extracted symbols: CustomDataGrid, useJobTableColumnList, values, ActionsCell, closePreview, handleJobDetailView, open, id, userModulesPermissions, jobFilters, columnList, compactView, responsiveColumns, hasAddPermission, hasEditPermission, handleSearchChange, handleClose, handleEditJob, closeConfirmationModal, handleFilterClick, handledeleteJob, response, handleAddEditJob, payload, jobId, handlePaginationModelChange
- React/data hooks found: useEffect, useState, useMemo
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.47 `src/app/modules/recruitment/models.ts`

- Source lines: 117
- Extracted symbols: ApplicantModel, EditApplicantModel, Applicant, JobsFilterModel, JobModal, CommentModel, GetAllVacancyPayload, CommentResponseModel
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.48 `src/app/modules/recruitment/processlog/components/ProcessLogFilter.tsx`

- Source lines: 182
- Extracted symbols: processLogFilters, dispatch, getActiveJobs, response, size, value
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.49 `src/app/modules/recruitment/processlog/ProcessLog.tsx`

- Source lines: 157
- Extracted symbols: ProcessLogs, intl, open, processLogFilters, totalAppliedFilters, handleSearchChange, handlePageChange, handleRowPerPageChange, handleFilterClick, handleCloseFilter
- React/data hooks found: useEffect, useState
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.50 `src/app/modules/recruitment/services.ts`

- Source lines: 99
- Extracted symbols: addJobVacancy, postRequest, editJobVacancy, getAllJobVacancies, deleteJobVacancy, deleteRequest, getAllApplicantList, addApplicant, editApplicant, getApplicantById, getRequest, uploadResume, getProcessLogData, addComment, getApplicantComments
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.51 `src/app/routing/RecruitmentRoutes.tsx`

- Source lines: 107
- Extracted symbols: RecruitmentRoutes
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|
| 1 | `GetApplicantList` | `POST api/Applicants/GetApplicantList` | Search service functions and component event handlers for this route/action. |
| 2 | `ApplicantById` | `GET api/Applicants/ApplicantById` | Search service functions and component event handlers for this route/action. |
| 3 | `AddApplicant` | `POST api/Applicants/AddApplicant` | Search service functions and component event handlers for this route/action. |
| 4 | `EditApplicants` | `POST api/Applicants/EditApplicant` | Search service functions and component event handlers for this route/action. |
| 5 | `UploadResume` | `POST api/Applicants/UploadResume` | Search service functions and component event handlers for this route/action. |
| 6 | `AddComment` | `POST api/Applicants/AddComment` | Search service functions and component event handlers for this route/action. |
| 7 | `GetAllComment` | `GET api/Applicants/GetAllComment/{applicantId}` | Search service functions and component event handlers for this route/action. |
| 8 | `GetProcessLogData` | `POST api/Applicants/GetProcessLogData` | Search service functions and component event handlers for this route/action. |
| 9 | `Get` | `GET api/CareerProfile` | Search service functions and component event handlers for this route/action. |
| 10 | `Save` | `PUT api/CareerProfile` | Search service functions and component event handlers for this route/action. |
| 11 | `UpdateTheme` | `PUT api/CareerProfile/theme` | Search service functions and component event handlers for this route/action. |
| 12 | `AddJobVacancy` | `POST api/JobVacancy/AddJobVacancy` | Search service functions and component event handlers for this route/action. |
| 13 | `EditJobVacancy` | `POST api/JobVacancy/EditJobVacancy` | Search service functions and component event handlers for this route/action. |
| 14 | `GetAllVacancy` | `POST api/JobVacancy/GetAllVacancy` | Search service functions and component event handlers for this route/action. |
| 15 | `GetVacancyById` | `POST api/JobVacancy/GetVacancyById` | Search service functions and component event handlers for this route/action. |
| 16 | `DeleteJobVacancy` | `DELETE api/JobVacancy/DeleteJobVacancy` | Search service functions and component event handlers for this route/action. |
| 17 | `GetJobs` | `GET api/public/jobs` | Search service functions and component event handlers for this route/action. |
| 18 | `GetJobByPublicId` | `GET api/public/jobs/{publicJobId}` | Search service functions and component event handlers for this route/action. |
| 19 | `GetJobBySlug` | `GET api/public/jobs/by-slug/{jobSlug}` | Search service functions and component event handlers for this route/action. |
| 20 | `GetCompanyJobs` | `GET api/public/companies/{companyCode}/jobs` | Search service functions and component event handlers for this route/action. |
| 21 | `GetLocations` | `GET api/public/job-locations` | Search service functions and component event handlers for this route/action. |
| 22 | `Apply` | `POST api/public/jobs/{publicJobId}/applications` | Search service functions and component event handlers for this route/action. |
| 23 | `UploadResume` | `POST api/public/applications/{applicationReference}/resume` | Search service functions and component event handlers for this route/action. |
| 24 | `Save` | `POST api/public/career/jobs/{publicJobId}/save` | Search service functions and component event handlers for this route/action. |
| 25 | `Unsave` | `DELETE api/public/career/jobs/{publicJobId}/save` | Search service functions and component event handlers for this route/action. |
| 26 | `Saved` | `GET api/public/career/jobs/saved` | Search service functions and component event handlers for this route/action. |
| 27 | `Merge` | `POST api/public/career/jobs/merge-anonymous-saves` | Search service functions and component event handlers for this route/action. |
| 28 | `Follow` | `POST api/public/career/companies/{companyCode}/follow` | Search service functions and component event handlers for this route/action. |
| 29 | `Unfollow` | `DELETE api/public/career/companies/{companyCode}/follow` | Search service functions and component event handlers for this route/action. |
| 30 | `FollowStatus` | `GET api/public/career/companies/{companyCode}/follow-status` | Search service functions and component event handlers for this route/action. |
| 31 | `CreateAlert` | `POST api/public/career/job-alerts` | Search service functions and component event handlers for this route/action. |
| 32 | `GetAlerts` | `GET api/public/career/job-alerts` | Search service functions and component event handlers for this route/action. |
| 33 | `UpdateAlert` | `PUT api/public/career/job-alerts/{alertId}` | Search service functions and component event handlers for this route/action. |
| 34 | `DeleteAlert` | `DELETE api/public/career/job-alerts/{alertId}` | Search service functions and component event handlers for this route/action. |
| 35 | `Track` | `POST api/public/career/analytics` | Search service functions and component event handlers for this route/action. |
| 36 | `VisitorPreferences` | `PUT api/public/career/visitor-preferences` | Search service functions and component event handlers for this route/action. |
| 37 | `VisitorPreferences` | `GET api/public/career/visitor-preferences` | Search service functions and component event handlers for this route/action. |
| 38 | `Subscribe` | `POST api/public/career/subscribers` | Search service functions and component event handlers for this route/action. |
| 39 | `Verify` | `POST api/public/career/subscribers/verify` | Search service functions and component event handlers for this route/action. |
| 40 | `Resend` | `POST api/public/career/subscribers/resend-verification` | Search service functions and component event handlers for this route/action. |
| 41 | `Unsubscribe` | `POST api/public/career/subscribers/unsubscribe` | Search service functions and component event handlers for this route/action. |
| 42 | `GetPreferences` | `GET api/public/career/subscribers/preferences` | Search service functions and component event handlers for this route/action. |
| 43 | `UpdatePreferences` | `PUT api/public/career/subscribers/preferences` | Search service functions and component event handlers for this route/action. |
| 44 | `GetCompanies` | `GET api/public/career/companies` | Search service functions and component event handlers for this route/action. |
| 45 | `GetProfile` | `GET api/public/career/companies/{companyCode}/profile` | Search service functions and component event handlers for this route/action. |

## 3.1 UI trace for `GetApplicantList`

- Backend endpoint: `POST api/Applicants/GetApplicantList`
- Controller source: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Declared parameters: `ApplicantResultFilters? filters`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/recruitment/applications/Applications.tsx` (matched: GetApplicantList)

## 3.2 UI trace for `ApplicantById`

- Backend endpoint: `GET api/Applicants/ApplicantById`
- Controller source: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
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
- `src/app/modules/recruitment/applications/components/ApplicantDetails.tsx` (matched: ApplicantById)
- `src/app/modules/recruitment/applications/components/EditApplicant.tsx` (matched: ApplicantById)
- `src/app/modules/recruitment/services.ts` (matched: ApplicantById)

## 3.3 UI trace for `AddApplicant`

- Backend endpoint: `POST api/Applicants/AddApplicant`
- Controller source: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
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

Source files containing the action name or a static route fragment:
- `src/app/modules/recruitment/applications/Applications.tsx` (matched: AddApplicant)
- `src/app/modules/recruitment/applications/components/AddApplicant.tsx` (matched: AddApplicant)
- `src/app/modules/recruitment/services.ts` (matched: AddApplicant)

## 3.4 UI trace for `EditApplicants`

- Backend endpoint: `POST api/Applicants/EditApplicant`
- Controller source: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Declared parameters: `[FromBody] ApplicantAddEditModel model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/recruitment/applications/components/ApplicantDetails.tsx` (matched: EditApplicant)
- `src/app/modules/recruitment/applications/components/EditApplicant.tsx` (matched: EditApplicant)
- `src/app/modules/recruitment/models.ts` (matched: EditApplicant)
- `src/app/modules/recruitment/services.ts` (matched: EditApplicant)
- `src/app/routing/RecruitmentRoutes.tsx` (matched: EditApplicant)

## 3.5 UI trace for `UploadResume`

- Backend endpoint: `POST api/Applicants/UploadResume`
- Controller source: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Declared parameters: `[FromForm] ResumeApplicantModel model, string email`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Component/ApplyJobForm.tsx` (matched: UploadResume)
- `src/app/modules/recruitment/applications/components/AddApplicant.tsx` (matched: UploadResume)
- `src/app/modules/recruitment/applications/components/EditApplicant.tsx` (matched: UploadResume)
- `src/app/modules/recruitment/services.ts` (matched: UploadResume)

## 3.6 UI trace for `AddComment`

- Backend endpoint: `POST api/Applicants/AddComment`
- Controller source: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Declared parameters: `CommentRequestModel model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/recruitment/applications/components/AddComment.tsx` (matched: AddComment)
- `src/app/modules/recruitment/applications/components/ApplicantDetails.tsx` (matched: AddComment)
- `src/app/modules/recruitment/applications/components/EditApplicant.tsx` (matched: AddComment)
- `src/app/modules/recruitment/services.ts` (matched: AddComment)

## 3.7 UI trace for `GetAllComment`

- Backend endpoint: `GET api/Applicants/GetAllComment/{applicantId}`
- Controller source: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Declared parameters: `string applicantId, [FromQuery] int pageNo, [FromQuery] int pageSize`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/recruitment/applications/components/ApplicantDetails.tsx` (matched: GetAllComment)

## 3.8 UI trace for `GetProcessLogData`

- Backend endpoint: `POST api/Applicants/GetProcessLogData`
- Controller source: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Declared parameters: `ApplicantLogFilterModel model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/recruitment/processlog/ProcessLog.tsx` (matched: GetProcessLogData)
- `src/app/modules/recruitment/services.ts` (matched: GetProcessLogData)

## 3.9 UI trace for `Get`

- Backend endpoint: `GET api/CareerProfile`
- Controller source: `Codeji.CMS.API/Controllers/CareerProfileController.cs`
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
- `src/app/modules/applynow/ApplicationForm.tsx` (matched: Get)
- `src/app/modules/applynow/Career.tsx` (matched: Get)
- `src/app/modules/applynow/Component/ApplyJobForm.tsx` (matched: Get)
- `src/app/modules/applynow/Component/CareerHero.tsx` (matched: Get)
- `src/app/modules/applynow/Component/FindJobsHeader.tsx` (matched: Get)
- `src/app/modules/applynow/Component/JobSearchCareer.tsx` (matched: Get)
- `src/app/modules/applynow/Component/WhyJoin.tsx` (matched: Get)
- `src/app/modules/applynow/MasterCareers.tsx` (matched: Get)
- `src/app/modules/applynow/publicCareerService.ts` (matched: Get)
- `src/app/modules/applynow/PublicJobApplyModal.tsx` (matched: Get)

## 3.10 UI trace for `Save`

- Backend endpoint: `PUT api/CareerProfile`
- Controller source: `Codeji.CMS.API/Controllers/CareerProfileController.cs`
- Declared parameters: `UpsertPublicCompanyProfileRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: Save)
- `src/app/modules/careerPortal/components/JobCard.tsx` (matched: Save)
- `src/app/modules/careerPortal/components/SaveJobButton.tsx` (matched: Save)
- `src/app/modules/careerPortal/context/SavedJobsContext.tsx` (matched: Save)
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: Save)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: Save)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: Save)
- `src/app/modules/careerPortal/pages/CareerProfileAdminPage.tsx` (matched: Save, CareerProfile)
- `src/app/modules/careerPortal/pages/SavedJobsPage.tsx` (matched: Save)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Save, CareerProfile)

## 3.11 UI trace for `UpdateTheme`

- Backend endpoint: `PUT api/CareerProfile/theme`
- Controller source: `Codeji.CMS.API/Controllers/CareerProfileController.cs`
- Declared parameters: `UpdateCompanyThemeRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Component/CareerHeader.tsx` (matched: theme)
- `src/app/modules/applynow/Component/JobsSection.tsx` (matched: theme)
- `src/app/modules/applynow/Component/NoVacancy.tsx` (matched: theme)
- `src/app/modules/careerPortal/components/CompanyCareerTheme.tsx` (matched: theme)
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: theme)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: theme)
- `src/app/modules/careerPortal/pages/CareerProfileAdminPage.tsx` (matched: theme)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: theme)
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` (matched: theme)
- `src/app/modules/recruitment/jobs/components/AddEditJob.tsx` (matched: theme)

## 3.12 UI trace for `AddJobVacancy`

- Backend endpoint: `POST api/JobVacancy/AddJobVacancy`
- Controller source: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Declared parameters: `JobVacancyModel jobVacancy`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/recruitment/jobs/Jobs.tsx` (matched: AddJobVacancy)
- `src/app/modules/recruitment/services.ts` (matched: AddJobVacancy)

## 3.13 UI trace for `EditJobVacancy`

- Backend endpoint: `POST api/JobVacancy/EditJobVacancy`
- Controller source: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Declared parameters: `JobVacancyModel jobVacancy, string jobId`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/recruitment/jobs/Jobs.tsx` (matched: EditJobVacancy)
- `src/app/modules/recruitment/services.ts` (matched: EditJobVacancy)

## 3.14 UI trace for `GetAllVacancy`

- Backend endpoint: `POST api/JobVacancy/GetAllVacancy`
- Controller source: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Declared parameters: `JobRequestModel model`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Component/JobSearchCareer.tsx` (matched: GetAllVacancy)
- `src/app/modules/recruitment/applications/components/AddApplicant.tsx` (matched: GetAllVacancy)
- `src/app/modules/recruitment/applications/components/EditApplicant.tsx` (matched: GetAllVacancy)
- `src/app/modules/recruitment/jobs/Jobs.tsx` (matched: GetAllVacancy)
- `src/app/modules/recruitment/models.ts` (matched: GetAllVacancy)
- `src/app/modules/recruitment/services.ts` (matched: GetAllVacancy)

## 3.15 UI trace for `GetVacancyById`

- Backend endpoint: `POST api/JobVacancy/GetVacancyById`
- Controller source: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Declared parameters: `string vacancyId`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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

## 3.16 UI trace for `DeleteJobVacancy`

- Backend endpoint: `DELETE api/JobVacancy/DeleteJobVacancy`
- Controller source: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Declared parameters: `[FromQuery] string vacancyId`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/recruitment/jobs/Jobs.tsx` (matched: DeleteJobVacancy)
- `src/app/modules/recruitment/services.ts` (matched: DeleteJobVacancy)

## 3.17 UI trace for `GetJobs`

- Backend endpoint: `GET api/public/jobs`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Declared parameters: `[FromQuery] PublicJobSearchRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Career.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/CareerHero.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/FindJobsHeader.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/JobSearchCareer.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/JobsSection.tsx` (matched: jobs)
- `src/app/modules/applynow/MasterCareers.tsx` (matched: jobs)
- `src/app/modules/applynow/publicCareerService.ts` (matched: jobs)
- `src/app/modules/applynow/publicCareerTypes.ts` (matched: jobs)
- `src/app/modules/applynow/PublicJobDetails.tsx` (matched: jobs)
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: jobs)

## 3.18 UI trace for `GetJobByPublicId`

- Backend endpoint: `GET api/public/jobs/{publicJobId}`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Declared parameters: `string publicJobId`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Career.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/CareerHero.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/FindJobsHeader.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/JobSearchCareer.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/JobsSection.tsx` (matched: jobs)
- `src/app/modules/applynow/MasterCareers.tsx` (matched: jobs)
- `src/app/modules/applynow/publicCareerService.ts` (matched: jobs)
- `src/app/modules/applynow/publicCareerTypes.ts` (matched: jobs)
- `src/app/modules/applynow/PublicJobDetails.tsx` (matched: jobs)
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: jobs)

## 3.19 UI trace for `GetJobBySlug`

- Backend endpoint: `GET api/public/jobs/by-slug/{jobSlug}`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Declared parameters: `string jobSlug`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/publicCareerService.ts` (matched: by-slug)

## 3.20 UI trace for `GetCompanyJobs`

- Backend endpoint: `GET api/public/companies/{companyCode}/jobs`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Declared parameters: `string companyCode, [FromQuery] PublicJobSearchRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Career.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/CareerHero.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/FindJobsHeader.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/JobSearchCareer.tsx` (matched: jobs)
- `src/app/modules/applynow/Component/JobsSection.tsx` (matched: jobs)
- `src/app/modules/applynow/MasterCareers.tsx` (matched: jobs)
- `src/app/modules/applynow/publicCareerService.ts` (matched: jobs)
- `src/app/modules/applynow/publicCareerTypes.ts` (matched: jobs)
- `src/app/modules/applynow/PublicJobDetails.tsx` (matched: jobs)
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: jobs)

## 3.21 UI trace for `GetLocations`

- Backend endpoint: `GET api/public/job-locations`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Declared parameters: `[FromQuery] string? companyCode, [FromQuery] string? search`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: job-locations)

## 3.22 UI trace for `Apply`

- Backend endpoint: `POST api/public/jobs/{publicJobId}/applications`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Declared parameters: `string publicJobId, [FromBody] PublicJobApplicationRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Component/ApplyJobForm.tsx` (matched: Apply, applications)
- `src/app/modules/applynow/Component/Footer.tsx` (matched: Apply)
- `src/app/modules/applynow/Component/JobInfo.tsx` (matched: Apply)
- `src/app/modules/applynow/Component/JobsSection.tsx` (matched: applications)
- `src/app/modules/applynow/publicCareerService.ts` (matched: Apply, applications)
- `src/app/modules/applynow/publicCareerTypes.ts` (matched: Apply)
- `src/app/modules/applynow/PublicJobApplyModal.tsx` (matched: Apply)
- `src/app/modules/applynow/PublicJobDetails.tsx` (matched: Apply)
- `src/app/modules/applynow/services.ts` (matched: Apply)
- `src/app/modules/careerPortal/components/JobCard.tsx` (matched: Apply)

## 3.23 UI trace for `UploadResume`

- Backend endpoint: `POST api/public/applications/{applicationReference}/resume`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Declared parameters: `string applicationReference, [FromForm] string token, [FromForm] IFormFile resume`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Component/ApplyJobForm.tsx` (matched: UploadResume, resume)
- `src/app/modules/applynow/Component/JobsSection.tsx` (matched: resume)
- `src/app/modules/applynow/publicCareerService.ts` (matched: resume)
- `src/app/modules/applynow/publicCareerTypes.ts` (matched: resume)
- `src/app/modules/applynow/PublicJobApplyModal.tsx` (matched: resume)
- `src/app/modules/recruitment/applications/components/AddApplicant.tsx` (matched: UploadResume, resume)
- `src/app/modules/recruitment/applications/components/ApplicantDetails.tsx` (matched: resume)
- `src/app/modules/recruitment/applications/components/ApplicationPreview.tsx` (matched: resume)
- `src/app/modules/recruitment/applications/components/EditApplicant.tsx` (matched: UploadResume, resume)
- `src/app/modules/recruitment/models.ts` (matched: resume)

## 3.24 UI trace for `Save`

- Backend endpoint: `POST api/public/career/jobs/{publicJobId}/save`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `string publicJobId, SaveJobRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/components/JobCard.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/components/SaveJobButton.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/context/SavedJobsContext.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/pages/CareerProfileAdminPage.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/pages/SavedJobsPage.tsx` (matched: Save, save)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Save, save)

## 3.25 UI trace for `Unsave`

- Backend endpoint: `DELETE api/public/career/jobs/{publicJobId}/save`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `string publicJobId, [FromQuery] string? visitorToken`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: save)
- `src/app/modules/careerPortal/components/JobCard.tsx` (matched: save)
- `src/app/modules/careerPortal/components/SaveJobButton.tsx` (matched: Unsave, save)
- `src/app/modules/careerPortal/context/SavedJobsContext.tsx` (matched: save)
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: save)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: save)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: save)
- `src/app/modules/careerPortal/pages/CareerProfileAdminPage.tsx` (matched: save)
- `src/app/modules/careerPortal/pages/SavedJobsPage.tsx` (matched: Unsave, save)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Unsave, save)

## 3.26 UI trace for `Saved`

- Backend endpoint: `GET api/public/career/jobs/saved`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `[FromQuery] string? visitorToken`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: Saved, saved)
- `src/app/modules/careerPortal/components/JobCard.tsx` (matched: Saved, saved)
- `src/app/modules/careerPortal/components/SaveJobButton.tsx` (matched: Saved, saved)
- `src/app/modules/careerPortal/context/SavedJobsContext.tsx` (matched: Saved, saved)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: Saved, saved)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: Saved, saved)
- `src/app/modules/careerPortal/pages/CareerProfileAdminPage.tsx` (matched: Saved, saved)
- `src/app/modules/careerPortal/pages/SavedJobsPage.tsx` (matched: Saved, saved)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Saved, saved)
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` (matched: Saved, saved)

## 3.27 UI trace for `Merge`

- Backend endpoint: `POST api/public/career/jobs/merge-anonymous-saves`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `MergeAnonymousSavesRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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

## 3.28 UI trace for `Follow`

- Backend endpoint: `POST api/public/career/companies/{companyCode}/follow`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `string companyCode, FollowCompanyRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: Follow, follow)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: Follow, follow)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Follow, follow)
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` (matched: Follow, follow)

## 3.29 UI trace for `Unfollow`

- Backend endpoint: `DELETE api/public/career/companies/{companyCode}/follow`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `string companyCode`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: Unfollow, follow)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: follow)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Unfollow, follow)
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` (matched: follow)

## 3.30 UI trace for `FollowStatus`

- Backend endpoint: `GET api/public/career/companies/{companyCode}/follow-status`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `string companyCode`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: FollowStatus)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: FollowStatus, follow-status)
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` (matched: FollowStatus)

## 3.31 UI trace for `CreateAlert`

- Backend endpoint: `POST api/public/career/job-alerts`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `JobAlertRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: CreateAlert)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: CreateAlert, job-alerts)

## 3.32 UI trace for `GetAlerts`

- Backend endpoint: `GET api/public/career/job-alerts`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
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
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: GetAlerts)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: GetAlerts, job-alerts)

## 3.33 UI trace for `UpdateAlert`

- Backend endpoint: `PUT api/public/career/job-alerts/{alertId}`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `string alertId, JobAlertRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: job-alerts)

## 3.34 UI trace for `DeleteAlert`

- Backend endpoint: `DELETE api/public/career/job-alerts/{alertId}`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `string alertId`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: DeleteAlert)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: DeleteAlert, job-alerts)

## 3.35 UI trace for `Track`

- Backend endpoint: `POST api/public/career/analytics`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `CareerAnalyticsRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: Track)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: Track)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Track, analytics)

## 3.36 UI trace for `VisitorPreferences`

- Backend endpoint: `PUT api/public/career/visitor-preferences`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `VisitorPreferenceRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: VisitorPreferences)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: VisitorPreferences, visitor-preferences)

## 3.37 UI trace for `VisitorPreferences`

- Backend endpoint: `GET api/public/career/visitor-preferences`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Declared parameters: `[FromQuery] string visitorToken`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: VisitorPreferences)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: VisitorPreferences, visitor-preferences)

## 3.38 UI trace for `Subscribe`

- Backend endpoint: `POST api/public/career/subscribers`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Declared parameters: `CareerSubscribeRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/components/SubscribeModal.tsx` (matched: Subscribe)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: Subscribe)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: Subscribe)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Subscribe, subscribers)

## 3.39 UI trace for `Verify`

- Backend endpoint: `POST api/public/career/subscribers/verify`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Declared parameters: `CareerTokenRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: Verify, verify)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: Verify, verify)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Verify, verify)

## 3.40 UI trace for `Resend`

- Backend endpoint: `POST api/public/career/subscribers/resend-verification`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Declared parameters: `CareerSubscribeRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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

## 3.41 UI trace for `Unsubscribe`

- Backend endpoint: `POST api/public/career/subscribers/unsubscribe`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Declared parameters: `CareerTokenRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: Unsubscribe, unsubscribe)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: Unsubscribe, unsubscribe)

## 3.42 UI trace for `GetPreferences`

- Backend endpoint: `GET api/public/career/subscribers/preferences`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
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
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: preferences)
- `src/app/modules/careerPortal/components/SubscribeModal.tsx` (matched: preferences)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: preferences)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: GetPreferences, preferences)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: GetPreferences, preferences)
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` (matched: preferences)

## 3.43 UI trace for `UpdatePreferences`

- Backend endpoint: `PUT api/public/career/subscribers/preferences`
- Controller source: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Declared parameters: `UpdateCareerPreferencesRequest request`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/careerPortal/components/CareerHeader.tsx` (matched: preferences)
- `src/app/modules/careerPortal/components/SubscribeModal.tsx` (matched: preferences)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: preferences)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: UpdatePreferences, preferences)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: UpdatePreferences, preferences)
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` (matched: preferences)

## 3.44 UI trace for `GetCompanies`

- Backend endpoint: `GET api/public/career/companies`
- Controller source: `Codeji.CMS.API/Controllers/PublicCompanyProfileController.cs`
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
- `src/app/modules/applynow/publicCareerService.ts` (matched: companies)
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: companies)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: GetCompanies, companies)
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` (matched: companies)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: GetCompanies, companies)

## 3.45 UI trace for `GetProfile`

- Backend endpoint: `GET api/public/career/companies/{companyCode}/profile`
- Controller source: `Codeji.CMS.API/Controllers/PublicCompanyProfileController.cs`
- Declared parameters: `string companyCode`
- Entry state: confirm the actual route/page/component importing the client function.
- Visibility: verify permission or permitted-action condition in rendered code.
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
- `src/app/modules/applynow/Component/ApplyJobForm.tsx` (matched: profile)
- `src/app/modules/careerPortal/components/CompanyCareerTheme.tsx` (matched: profile)
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` (matched: profile)
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` (matched: profile)
- `src/app/modules/careerPortal/pages/CareerProfileAdminPage.tsx` (matched: profile)
- `src/app/modules/careerPortal/services/careerPortalService.ts` (matched: profile)
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` (matched: profile)
- `src/app/modules/recruitment/applications/components/AddApplicant.tsx` (matched: profile)
- `src/app/routing/RecruitmentRoutes.tsx` (matched: profile)

## 4. Form and Interaction Review Rules

- Fields: derive names, types, defaults and optionality from form code and request DTO together.
- Validation: document frontend validation separately from backend rejection.
- Filters/pagination: establish whether state is local or server-backed.
- Retry: confirm whether query libraries retry reads or mutations.
- Accessibility: preserve labels, keyboard operation, focus return and tooltip semantics.
- Permissions: use actual guards/permitted actions; do not introduce role-name guesses.

