# Recruitment Module: Current Flow and Gap Audit

> Snapshot: 2026-07-22. This document describes the backend implementation as it exists in this repository. It is an implementation audit, not a statement of intended product behavior.

## 1. Scope

The recruitment module provides:

- job-vacancy creation, editing, listing, lookup, and deletion;
- public candidate application intake;
- applicant creation and editing by authorized employees;
- PDF resume upload;
- applicant search and filtering;
- comments and process-log reporting;
- applicant acknowledgement, selection, and rejection emails;
- dashboard counts grouped by recruitment stage.

There is no implemented interview scheduling, interviewer assignment, scorecard, offer-letter, approval, onboarding handoff, or conversion-to-employee workflow.

## 2. Main components

| Layer | Implementation |
|---|---|
| Vacancy API | `Codeji.CMS.API/Controllers/JobVacancyController.cs` |
| Applicant API | `Codeji.CMS.API/Controllers/ApplicantsController.cs` |
| Public application API | `Codeji.CMS.API/Controllers/AccountController.cs` |
| Dashboard API | `Codeji.CMS.API/Controllers/DashboardController.cs` |
| Vacancy service | `Codeji.CMS.Services/Recruitments/JobVacancyService.cs` |
| Applicant service | `Codeji.CMS.Services/Recruitments/ApplicantServices.cs` |
| Service contracts | `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs`, `IApplicantsService.cs` |
| Entities | `JobVacancy`, `Applicant`, `ApplicantLogs`, `MailTemplate` |
| DTOs | `JobVacancyModel`, `JobRequestModel`, `ApplicantAddEditModel`, `ApplicantResultFilters`, `ApplicantViewModel`, `ApplicantLogFilterModel` |
| Persistence | Generic `IMongoDbRepository<T>` over MongoDB |
| Permissions | `Jobs`, `Applications`, and `ProcessLog` modules |
| Email delivery | Seeded `MailTemplate` records, `HtmlTemplate.Render`, priority background queue, and `IMiddlewareService.EmailSendAndSave` |
| Resume storage | Local server directory: `Uploads/Resume` |

Services are registered as scoped dependencies in `ServicesRegistration.cs`.

## 3. Data model

### 3.1 JobVacancy

`JobVacancy` inherits `BaseClass`, so it includes `CompanyId`, created/updated audit fields, and `IsDeleted` in addition to:

- `JobId`
- `Title`
- `Vacancies`
- `JobType` (integer; no recruitment-specific enum is used here)
- `Status` (`true`/`false`)
- `Description`

### 3.2 Applicant

`Applicant` also inherits `BaseClass` and stores:

- identity: `ApplicantId`, first name, last name, email, and phone;
- application link: one `VacancyId`;
- profile: state, years of experience, and `ResumeUrl`;
- pipeline stage: `ActivityType`;
- active/inactive state: `ActivityStatus`.

Pipeline values are:

```text
New -> InProgress -> OnHold -> Shortlisted -> Selected / Rejected
                                               |
                                            ReApply
```

This diagram is descriptive only. The backend does not enforce these transition paths; an authorized edit can assign any enum value directly.

### 3.3 ApplicantLogs

An applicant log contains applicant/user IDs, applicant name, job role, description, activity category, and inherited audit/company fields. It is used for the per-applicant comments view and the global process log.

Despite its name, it is not currently a complete audit history. It records:

- a new-application event only through `RegisterApplicants` (the authenticated HR create path); and
- comments explicitly added through `AddComment`.

It does not automatically record public `ApplyNowService` submissions or changes made through `UpdateApplicants`.

### 3.4 MailTemplate

Templates are global records keyed by `MailType`; they do not inherit `BaseClass` and therefore have no company scope. The migration seeds templates for applicant acknowledgement, selection, rejection, and HR notification, among other system emails.

The recruitment service currently consumes acknowledgement, selection, and rejection templates. Although `ApplyNowMailToHR` is seeded, the application flow does not send it.

## 4. API and authorization map

### 4.1 Job vacancies

Base route: `api/JobVacancy`; the controller requires authentication unless an action overrides it.

| Endpoint | Access | Behavior |
|---|---|---|
| `POST AddJobVacancy` | `Jobs/Create` | Inserts a vacancy. |
| `POST EditJobVacancy?jobId=...` | `Jobs/Edit` | Replaces/updates the matching vacancy from the supplied DTO. |
| `POST GetAllVacancy` | Anonymous | Filters by job type, status, and title; paginates; adds application counts. |
| `POST GetVacancyById?vacancyId=...` | Authenticated only; no module permission | Returns only the vacancy title as a string. |
| `DELETE DeleteJobVacancy?vacancyId=...` | `Jobs/Delete` | Marks the record deleted and then calls repository delete. |

### 4.2 Applicants

Base route: `api/Applicants`; the controller requires authentication except resume upload.

| Endpoint | Access | Behavior |
|---|---|---|
| `POST GetApplicantList` | `Applications/View` | Filters and pages applicants and joins vacancy names. |
| `GET ApplicantById?id=...` | `Applications/View` | Returns applicant details and vacancy name. |
| `POST AddApplicant` | `Applications/Create` | HR creates an applicant, adds an initial log, and queues acknowledgement email. |
| `POST EditApplicant` | `Applications/Edit` | Updates profile, vacancy, stage, and status; may queue stage email. |
| `POST UploadResume?email=...` | Anonymous | Stores/replaces a PDF resume for the applicant found by email. |
| `POST AddComment` | `Applications/Edit` | Adds an applicant log/comment using the current user ID. |
| `GET GetAllComment/{applicantId}` | `Applications/View` | Returns paged applicant logs/comments. |
| `POST GetProcessLogData` | `ProcessLog/View` | Filters and pages logs across applicants. |

Public application endpoint:

| Endpoint | Access | Behavior |
|---|---|---|
| `POST api/Account/applicant/applyJob` | Anonymous | Enforces an email-based six-month waiting period, inserts an applicant, and queues acknowledgement email. |

Tenant selection for anonymous career endpoints depends on the request middleware populating `HttpContext.Items["CompanyId"]` (typically from request context such as the company header). This is a required integration contract even though the action signature does not show it.

## 5. End-to-end flow

### 5.1 HR publishes a vacancy

```text
Authenticated HR/admin
  -> Jobs/Create permission
  -> AddJobVacancy
  -> JobVacancyService.AddJobVacancy
  -> generic repository sets company/audit context
  -> JobVacancy document
```

The vacancy can later be edited, enabled/disabled using `Status`, listed, or deleted. There is no validation preventing zero/negative vacancy counts, blank titles, invalid job types, or modification of a vacancy that already has applications.

### 5.2 Public career page lists jobs

```text
Career page + company context
  -> anonymous GetAllVacancy request
  -> filters: JobTypes, Status, Search
  -> tenant/soft-delete repository filters
  -> newest vacancies first
  -> count applicants for each returned JobId
  -> paged vacancy response
```

The action allows callers to request inactive vacancies unless the client explicitly supplies `Status = true`.

### 5.3 Candidate submits an application

```text
Candidate
  -> POST api/Account/applicant/applyJob
  -> model validation
  -> lookup applicant by email
  -> if latest matching applicant is less than six months old: reject
  -> insert Applicant as Active + New
  -> resolve vacancy and acknowledgement template
  -> queue acknowledgement email
```

Important implementation details:

- duplicate/reapply detection uses email only, not `(CompanyId, VacancyId, Email)`;
- the six-month comparison uses the one applicant returned by `FirstOrDefault`;
- although the method temporarily sets the request model to `ReApply`, the inserted entity is always assigned `ActivityType.New`;
- no application log is created in this public path;
- vacancy existence, active status, and capacity are not validated before insert;
- resume is uploaded separately, after application creation.

### 5.4 Resume upload

```text
Candidate/client with applicant email
  -> anonymous UploadResume
  -> extension and size checks in controller
  -> find applicant by email
  -> save GUID-named PDF under Uploads/Resume
  -> delete old local file, if present
  -> update Applicant.ResumeUrl
```

The stated size limit is 5 MB. The code calculates whole megabytes using integer division, so a file slightly larger than 5 MB may pass until it reaches 6 MB. Extension checking is case-sensitive and verifies neither MIME type nor PDF content.

### 5.5 HR reviews and processes applicants

```text
Applications/View
  -> list/filter candidates
  -> view applicant and resume

Applications/Edit
  -> edit candidate/profile/stage/status
  -> save Applicant
  -> if stage is New, Selected, or Rejected, queue matching email
  -> optionally add a comment/process-log entry
```

Filtering supports date range, stage, active status, vacancy IDs, experience, name, and pagination. Applicant results use an inner join to vacancies, so applicants whose vacancy is missing/deleted are omitted from returned data while still contributing to `TotalRecords`.

### 5.6 Comments and process logs

`AddComment` stores the actor, applicant, job-title text supplied by the request, category, description, and applicant name. `GetAllComment` returns logs for one applicant and attempts to resolve employee names. `GetProcessLogData` provides a cross-applicant report filtered by date, category, job role, and applicant name.

### 5.7 Dashboard reporting

`DashboardServices.GetApplicationStatusData` loads applicants, optionally for one vacancy, groups them by `ActivityType`, and returns total and per-stage counts. It is a current-state aggregation, not a funnel/history report.

## 6. Business rules currently enforced

- Vacancy create/edit/delete requires the corresponding `Jobs` permission.
- Applicant read/create/edit requires the corresponding `Applications` permission.
- Process-log read requires `ProcessLog/View`.
- Public applications are active and start at `New`.
- A matching email cannot reapply within six months in the public flow.
- HR `RegisterApplicants` rejects any existing applicant with the same email.
- Resume upload accepts `.pdf` names and applies an approximate 5 MB limit.
- Applicant acknowledgement is sent for `New`; selection/rejection emails are sent after edits to those stages.
- Repository defaults are intended to apply company and soft-delete filters to `BaseClass` entities.

## 7. Gaps and risks

### 7.1 Critical/high priority

1. **Anonymous email lookup and resume replacement can cross tenant boundaries when company context is missing.** Generic repository filtering only adds `CompanyId` for some query paths when it is non-empty. Both public apply and anonymous resume upload query by email, making correct middleware/header enforcement security-critical.
2. **Resume upload is unauthenticated and uses email as the only ownership proof.** Anyone who knows an applicant email may replace that applicant's resume. Use a short-lived application token or authenticated applicant session and bind it to applicant/company.
3. **Vacancy list tenant behavior is inconsistent across generic repository paths.** Aggregate queries explicitly compare `CompanyId` with the current context, while normal queries may omit the tenant condition when context is empty. Anonymous behavior must be standardized and fail closed when company context is absent.
4. **Applicant history is incomplete.** Public submission and stage/status edits do not create `ApplicantLogs`; therefore the process log cannot prove who changed a stage, when it changed, or the previous/new values.
5. **No enforced recruitment state machine.** Any edit can jump between any stages, reopen selected/rejected candidates, or combine contradictory stage/status values without validation.
6. **Vacancy validity is not checked during application.** Applications can reference missing, inactive, deleted, or cross-company vacancies and can exceed the advertised vacancy count.
7. **File validation is insufficient.** Extension-only checks, case sensitivity, integer-rounded sizing, no content signature/MIME validation, local public-path storage, and delete-before-database-update behavior create security and consistency risks.

### 7.2 Medium priority

8. **Reapply logic is internally inconsistent.** The method sets the DTO to `ReApply` but always persists the new record as `New`; email lookup is not explicitly ordered and is not scoped to vacancy.
9. **Different create paths enforce different duplicate rules.** HR creation blocks an email forever, while public creation allows another record after six months. Neither rule is scoped to a vacancy, and database uniqueness is not evident.
10. **Applicant edits overwrite all mutable fields.** The update endpoint combines profile editing, vacancy reassignment, stage transition, and activation in one broad DTO, increasing accidental changes and authorization ambiguity.
11. **Vacancy deletion is ambiguous.** The service first sets `IsDeleted = true` and then calls `Delete`; this is redundant at best and may become a hard delete depending on repository behavior. Applicant references and reporting consequences are not handled.
12. **List counts can disagree with returned rows.** The applicant list counts matching applicant records before inner-joining vacancies; orphaned/deleted vacancy references disappear from results.
13. **Public listing does not force active jobs.** A caller can omit the status filter or explicitly request inactive vacancies.
14. **Application counts are current document counts, not qualified/active counts.** They do not distinguish withdrawn, inactive, rejected, duplicate, or reapplication records.
15. **Email workflow is partial.** `ApplyNowMailToHR` exists but is unused; email queueing occurs after persistence without durable outbox/retry linkage; intermediate-stage communication is absent.
16. **Email templates are global.** There is no company-level branding, language selection, or tenant-specific content for recruitment messages.
17. **Comments trust request data for job role.** `AddComment` stores `model.JobTitle` rather than resolving the applicant's actual vacancy, so logs can contain inaccurate role data.
18. **Input validation is incomplete.** Required validation is inconsistent for names/vacancy/state, experience can be negative, job counts can be negative, paging can be invalid, and filter arrays may be null even though the service calls `.Any()`/`.Length`.

### 7.3 Product/completeness gaps

19. No interview rounds, scheduling, participants, feedback, scorecards, or decision approvals.
20. No source/channel, recruiter owner, tags, skills, education, notice period, expected compensation, location preference, attachments, or consent/retention fields.
21. No offer generation, negotiation, acceptance/rejection, document collection, background verification, or onboarding/employee conversion.
22. No candidate withdrawal, deletion/anonymization, privacy consent, retention schedule, or data-subject workflow.
23. No vacancy approval, department/location/hiring-manager ownership, publish/close dates, or automatic closing when filled.
24. No concurrency/version check; simultaneous HR edits can overwrite each other.
25. No recruitment-specific service tests were found for vacancy CRUD, public application, tenant isolation, transitions, logging, resume security, email selection, or dashboard aggregation.

## 8. Recommended target flow

```text
Draft vacancy
  -> approval
  -> published vacancy with company-safe public identifier
  -> candidate application + resume in one authorized/atomic workflow
  -> immutable application event
  -> screening
  -> interview rounds + feedback
  -> decision approval
  -> offer workflow
  -> accepted offer
  -> controlled conversion to employee/onboarding
  -> vacancy fill/close update
```

Each transition should validate the current state, requested next state, actor permission, tenant, vacancy status, and required data. Applicant updates and event creation should be atomic or use a durable workflow/outbox.

## 9. Recommended remediation order

1. Require and validate company context for every public recruitment request; fail closed when missing.
2. Replace email-authorized resume upload with a signed, expiring application token and strengthen PDF validation/storage.
3. Validate vacancy existence, company, published/active status, and application eligibility before inserting.
4. Split profile update from stage transition and implement an explicit transition matrix.
5. Write an immutable event for every application, transition, comment, email decision, and actor.
6. Define a consistent reapplication/duplicate key and enforce it with tenant-aware MongoDB indexes.
7. Correct list joins/counts, deletion semantics, null-safe filters, paging, and validation.
8. Send HR notifications through a durable outbox and add company-specific templates.
9. Add interview, offer, and employee-conversion workflows based on product priority.
10. Add unit/integration tests, especially tenant-isolation and anonymous-endpoint security tests.

## 10. Minimum test matrix

- Vacancy CRUD respects company and soft-delete boundaries.
- Anonymous vacancy list fails without company context and returns only active jobs for the selected company.
- Apply rejects missing/inactive/cross-company vacancy IDs.
- Duplicate and reapply rules are deterministic for same/different companies and vacancies.
- Resume upload requires applicant proof and cannot replace another tenant's file.
- PDF validation rejects uppercase/invalid extensions as appropriate, spoofed content, empty files, and files over the exact byte limit.
- Every allowed transition succeeds; every disallowed transition fails.
- Every application/transition produces one correct immutable log with actor and old/new values.
- Selection/rejection/acknowledgement emails use the correct tenant template and do not block the API response.
- Deleted/missing vacancies do not cause applicant count/list mismatch.
- Null/empty filters and invalid pagination return stable validation errors rather than exceptions.
- Dashboard totals match tenant-scoped applicants and defined inclusion rules.

## 11. Documentation correction note

The recruitment section in `docs/modules.md` currently lists endpoint names and behaviors that do not match the controller implementation and states that every status change is logged. Treat this audit and the source code as authoritative until that catalog section is corrected.
