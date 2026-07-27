# Recruitment: Current Flow and Working Context

> **Implementation snapshot:** 24 July 2026  
> **Scope:** Current backend and React implementation in `CMS-Backend-Core` and `CMS-React`.  
> **Purpose:** Give developers, QA, product, and operations one detailed reference for how recruitment and public careers currently work.

## 1. Executive summary

The recruitment system has two connected experiences:

1. **Authenticated recruitment administration**
   - HR/admin users create and manage vacancies.
   - Authorized users view, add, edit, filter, and comment on applicants.
   - Applicant stages and process logs are managed inside the CMS.

2. **Anonymous public careers**
   - `/careers` lists publishable jobs from all opted-in companies.
   - `/careers/{companyCode}` lists jobs for one company.
   - `/careers/{companyCode}/jobs/{jobSlug}` displays a job and its application action.
   - Internal applications are assigned to the job's company on the server.
   - External jobs redirect to a validated HTTPS application URL.
   - Internal applicants receive a short-lived, single-use token for PDF resume upload.

Every company has a persistent, unique, six-digit `PublicCompanyCode`. Public career URLs use this code rather than the company name, company slug, or internal MongoDB `CompanyId`.

```text
Company registration
  -> generate internal CompanyId
  -> generate unique 6-digit PublicCompanyCode
  -> save both on Company
  -> HR creates/publishes a job
  -> public marketplace returns job + PublicCompanyCode
  -> frontend creates /careers/{PublicCompanyCode}/... URLs
```

## 2. Repository map

### 2.1 Backend

| Concern | Main implementation |
|---|---|
| Company registration and public code | `Codeji.CMS.Services/Companies/CompanyService.cs` |
| Company entity | `Codeji.CMS.Repository/Entities/Company/Company.cs` |
| Authenticated vacancy API | `Codeji.CMS.API/Controllers/JobVacancyController.cs` |
| Vacancy service | `Codeji.CMS.Services/Recruitments/JobVacancyService.cs` |
| Authenticated applicant API | `Codeji.CMS.API/Controllers/ApplicantsController.cs` |
| Applicant service | `Codeji.CMS.Services/Recruitments/ApplicantServices.cs` |
| Public careers API | `Codeji.CMS.API/Controllers/PublicCareerController.cs` |
| Public careers service | `Codeji.CMS.Services/Recruitments/PublicCareerService.cs` |
| Public career DTOs | `Codeji.CMS.DTO/PublicCareers/PublicCareerDtos.cs` |
| Vacancy entity | `Codeji.CMS.Repository/Entities/Recruitments/JobVacancy.cs` |
| Applicant entity | `Codeji.CMS.Repository/Entities/Recruitments/Applicant.cs` |
| Resume token entity | `Codeji.CMS.Repository/Entities/Recruitments/PublicApplicationToken.cs` |
| Applicant logs | `Codeji.CMS.Repository/Entities/Recruitments/ApplicantLogs.cs` |
| Public-field migration | `Codeji.CMS.Migrations/Migrations/BackfillPublicCareerMarketplaceFields.cs` |
| Dependency registration | `Codeji.CMS.Services/Registration/ServicesRegistration.cs` |

### 2.2 Frontend

| Concern | Main implementation |
|---|---|
| Public routes | `CMS-React/src/app/routing/AppRoutes.tsx` |
| Careers marketplace/company listing | `CMS-React/src/app/modules/applynow/MasterCareers.tsx` |
| Job details | `CMS-React/src/app/modules/applynow/PublicJobDetails.tsx` |
| Application modal | `CMS-React/src/app/modules/applynow/PublicJobApplyModal.tsx` |
| Public API client | `CMS-React/src/app/modules/applynow/publicCareerService.ts` |
| Public TypeScript models | `CMS-React/src/app/modules/applynow/publicCareerTypes.ts` |
| Admin company career link | `CMS-React/src/app/modules/admin/companydetails/components/CompanyInfo.tsx` |
| Applications list/toolbar | `CMS-React/src/app/modules/recruitment/applications/Applications.tsx` |
| Applicant details/history | `CMS-React/src/app/modules/recruitment/applications/components/ApplicantDetails.tsx` |
| Applicant editing/return flow | `CMS-React/src/app/modules/recruitment/applications/components/EditApplicant.tsx` |
| Recruitment routes | `CMS-React/src/app/routing/RecruitmentRoutes.tsx` |
| Dynamic breadcrumb resolver | `CMS-React/src/app/shared/components/PageData.tsx` |

## 3. Tenant and identifier model

The implementation uses several identifiers with different responsibilities.

| Identifier | Example | Purpose | Publicly exposed |
|---|---|---|---|
| `CompanyId` | GUID | Internal company/tenant foreign key | Not used in career URLs |
| `PublicCompanyCode` | `483921` | Stable public company routing code | Yes |
| `CareerSlug` | `codeji-infotech` | Older/compatibility company slug | Returned in some DTOs, not used by current frontend routing |
| `JobId` | GUID | Internal vacancy primary key and applicant foreign key | No |
| `PublicJobId` | `JOB-...` | Stable public application identifier | Yes, through API |
| `Slug` | `mern-stack-developer-job-4e03` | Human-readable job URL segment | Yes |
| `ApplicantId` | GUID | Applicant primary key and public application reference | Returned after successful application |

### 3.1 Six-digit company-code lifecycle

During company registration, `CompanyService.Register`:

1. Creates a new internal `CompanyId`.
2. Creates the default administrator role and user.
3. Calls `BuildUniquePublicCompanyCode`.
4. Generates a cryptographically random integer from `100000` through `999999`.
5. Checks the company collection for an existing matching code.
6. Retries until it finds an unused value.
7. Saves the code in `Company.PublicCompanyCode`.

The MongoDB unique index `ux_company_public_code` provides database-level uniqueness.

`EnsureCareerPortal` repairs a company when its code is missing or is not exactly six digits. The backfill migration repairs all existing companies and intentionally has a new migration ID so databases that previously received alphanumeric codes are processed again.

### 3.2 Company ownership of recruitment data

`JobVacancy`, `Applicant`, `ApplicantLogs`, and `PublicApplicationToken` inherit `BaseClass`. They therefore carry `CompanyId` along with audit and soft-delete fields.

The core ownership chain is:

```text
Company.CompanyId
   |
   +-- JobVacancy.CompanyId
   |      |
   |      +-- Applicant.VacancyId -> JobVacancy.JobId
   |
   +-- Applicant.CompanyId
   +-- ApplicantLogs.CompanyId
   +-- PublicApplicationToken.CompanyId
```

Public application requests do not accept `CompanyId`. The backend loads the selected job and copies `job.CompanyId` to the applicant, log, and token. This prevents a candidate from assigning an application to another tenant through request data.

## 4. Data model

### 4.1 Company public-career fields 

| Field | Meaning |
|---|---|
| `PublicCompanyCode` | Unique six-digit public identifier |
| `CareerSlug` | Legacy/compatibility slug |
| `CareerPortalEnabled` | Enables the company's public portal |
| `PublishJobsToMasterPortal` | Allows eligible jobs to appear on `/careers` |
| `ExternalCareerUrl` | Reserved external company career URL |
| `CompanyLogo` | Logo returned with public job/company data |
| `Status` | Company active state |
| `IsDeleted` | Company soft-delete state |

### 4.2 JobVacancy

`JobVacancy` contains the original internal vacancy fields and public publishing metadata.

**Core fields**

- `JobId`
- `CompanyId`
- `Title`
- `Description`
- `Vacancies`
- `JobType`
- `Status`
- audit and soft-delete fields from `BaseClass`

**Public identity**

- `PublicJobId`
- `Slug`
- `ReferenceCode`

**Publishing**

- `PublishToCareerPortal`
- `PublishToMasterPortal`
- `PublishedAt`
- `ExpiresAt`
- `ApplicationDeadline`

**Application behavior**

- `ApplicationMode`: normalized to `Internal` or `External`
- `ExternalApplicationUrl`: accepted only for an external job and only when it is a non-loopback HTTPS URL

**Candidate-facing metadata**

- `Location`
- `WorkplaceType`
- `EmploymentType`
- `ExperienceMin`
- `ExperienceMax`
- `SalaryMin`
- `SalaryMax`
- `Currency`
- `Skills`

### 4.3 Applicant

An applicant contains:

- `ApplicantId`
- `CompanyId`
- `VacancyId`
- `FirstName`
- `LastName`
- `Email`
- `Phone`
- `State`
- `Experience`
- `ResumeUrl`
- `ActivityType`
- `Status`
- audit and soft-delete fields

The current stage enum includes values such as:

```text
New
InProgress
OnHold
Shortlisted
Selected
Rejected
ReApply
```

The public service creates a first-time applicant as `New` and an allowed returning applicant as `ReApply`.

### 4.4 PublicApplicationToken

This record authorizes one resume upload:

- `TokenId`
- `TokenHash`
- `ApplicationReference`
- `ApplicantId`
- `JobId`
- `CompanyId`
- `Purpose`
- `ExpiresAt`
- `IsUsed`
- `UsedAt`

The raw token is returned once to the browser. Only its SHA-256 hash is stored.

### 4.5 ApplicantLogs

The modern public application path creates an initial log:

```text
Description: New application submitted from public career portal
JobRole: selected job title
ApplicantName: candidate full name
CompanyId: job company
```

Authenticated HR creation and explicit comments also create logs. Existing edit/stage behavior should still be reviewed before treating the collection as a complete immutable audit trail.

## 5. Company registration flow

```text
POST company/account registration
  -> AccountController
  -> CompanyService.Register
  -> create internal CompanyId
  -> create default company roles
  -> create administrator EmpUser
  -> generate unique CareerSlug
  -> generate unique 6-digit PublicCompanyCode
  -> enable CareerPortalEnabled
  -> enable PublishJobsToMasterPortal
  -> insert Company
  -> insert administrator
  -> insert notification preferences
  -> send verification email
```

The public company code is server-generated. The registration request does not supply it and the frontend must not invent or override it.

When an authenticated administrator loads company details, the returned `Company` includes `PublicCompanyCode`. The admin UI builds the link:

```text
{frontend-origin}/careers/{publicCompanyCode}
```

## 6. Authenticated vacancy flow

Base controller route: `api/JobVacancy`

| Endpoint | Access | Current behavior |
|---|---|---|
| `POST AddJobVacancy` | Authenticated + `Jobs/Create` | Creates a tenant-scoped vacancy and public identity |
| `POST EditJobVacancy?jobId=...` | Authenticated + `Jobs/Edit` | Updates the vacancy while preserving stable public ID and slug |
| `POST GetAllVacancy` | Anonymous legacy action | Lists repository-scoped vacancies with filters and application counts |
| `POST GetVacancyById?vacancyId=...` | Authenticated | Returns vacancy title through controller |
| `DELETE DeleteJobVacancy?vacancyId=...` | Authenticated + `Jobs/Delete` | Marks/deletes the vacancy through repository operations |

### 6.1 Vacancy creation

`JobVacancyService.AddJobVacancy`:

1. Maps the internal and publishing fields from `JobVacancyModel`.
2. Generates `PublicJobId` as `JOB-{GUID}` if not supplied.
3. Generates a globally unique normalized job slug.
4. Defaults `PublishToCareerPortal` to `true`.
5. Defaults `PublishToMasterPortal` to `false`.
6. Normalizes application mode.
7. Keeps an external URL only when it passes the HTTPS safety check.
8. Persists through the tenant-aware repository.

### 6.2 Vacancy edit

Editing preserves an existing `PublicJobId` and `Slug`. Missing newer public fields fall back to their existing stored values. This keeps already published URLs stable when HR edits a job title or description.

### 6.3 Publishing matrix

| Company/job state | Company page | Master `/careers` |
|---|---:|---:|
| Company inactive/deleted/portal disabled | Hidden | Hidden |
| Job inactive/deleted | Hidden | Hidden |
| `PublishToCareerPortal = false` | Hidden | Hidden |
| `PublishToCareerPortal = true` | Visible | Depends on master flags |
| Company `PublishJobsToMasterPortal = false` | Visible | Hidden |
| Job `PublishToMasterPortal = false` | Visible | Hidden |
| Both master flags enabled | Visible | Visible |
| Deadline/expiry passed | Hidden from lists | Hidden from lists |

## 7. Public frontend routes

Current routes are:

| URL | Component | Purpose |
|---|---|---|
| `/careers` | `MasterCareers` | All eligible jobs |
| `/careers/{companyCode}` | `MasterCareers` | One company's eligible jobs |
| `/careers/{companyCode}/jobs/{jobSlug}` | `PublicJobDetails` | Job details and apply action |
| `/career` | Redirect | Redirects to `/careers` |
| `/career/{legacyValue}` | Redirect | Redirects to `/careers` |

The frontend validates company codes using `^\d{6}$`. Job links use the `companyPublicCode` returned by the API.

The careers UI uses the application's MUI theme tokens:

- `background.default`
- `background.paper`
- `text.primary`
- `text.secondary`
- `divider`
- the configured primary palette

It therefore follows light/dark mode and the configured application surface style.

## 8. Public API map

Base route: `api/public`. All actions are anonymous.

| Method and route | Purpose |
|---|---|
| `GET /jobs` | Master job marketplace |
| `GET /jobs/{publicJobId}` | One job by public job ID |
| `GET /jobs/by-slug/{jobSlug}` | One job by normalized slug |
| `GET /companies/{companyCode}/jobs` | Jobs belonging to one six-digit company code |
| `POST /jobs/{publicJobId}/applications` | Submit an internal application |
| `POST /applications/{applicationReference}/resume` | Upload a resume using the returned token |

### 8.1 Public search input

`PublicJobSearchRequest` supports:

- `PageNo`
- `PageSize` (clamped from 1 through 50)
- `Search`
- `Company` (currently present in DTO but not applied by the service)
- `Location`
- `EmploymentType`
- `WorkplaceType`
- `JobType`
- `ExperienceMin`
- `ExperienceMax`
- `Sort`: `newest`, `oldest`, or `deadline`

### 8.2 Master marketplace query

```text
GET /api/public/jobs
  -> load active, non-deleted companies
  -> require CareerPortalEnabled
  -> require company PublishJobsToMasterPortal
  -> load jobs for those companies
  -> require job Status
  -> exclude soft-deleted jobs
  -> require PublishToCareerPortal
  -> require PublishToMasterPortal
  -> apply filters
  -> exclude expired/deadline-passed jobs
  -> sort and paginate
  -> attach company public code/name/logo
```

### 8.3 Company-specific query

```text
GET /api/public/companies/{sixDigitCode}/jobs
  -> normalize input to digits
  -> resolve active company by PublicCompanyCode
  -> require CareerPortalEnabled
  -> return 404 if not found
  -> search jobs for only that CompanyId
  -> require PublishToCareerPortal
  -> do not require master-publishing flags
```

The company-specific response may therefore include a job that is intentionally absent from the master marketplace.

### 8.4 Public job details

Job detail lookup:

1. Finds the job by public ID or normalized slug.
2. Rejects missing, inactive, deleted, or non-career-published jobs.
3. Loads the active portal-enabled company from `job.CompanyId`.
4. Returns the job with its `CompanyPublicCode`.
5. The frontend verifies the code in the URL equals the code returned for the job.
6. Rich job HTML is sanitized with DOMPurify before rendering.

## 9. Public internal-application flow

### 9.1 Browser request

```http
POST /api/public/jobs/{publicJobId}/applications
Content-Type: application/json
```

Conceptual request:

```json
{
  "firstName": "Candidate",
  "lastName": "Name",
  "email": "candidate@example.com",
  "phone": "9999999999",
  "state": "Maharashtra",
  "experience": 3.5
}
```

The request intentionally contains no company ID or internal job ID.

### 9.2 Server processing

```text
PublicJobId
  -> load active, non-deleted, career-published JobVacancy
  -> load active, non-deleted, portal-enabled Company by job.CompanyId
  -> reject expired/deadline-passed job
  -> reject internal submission when ApplicationMode is External
  -> normalize email
  -> search latest non-deleted applicant in the same company
  -> apply six-month waiting rule
  -> create Applicant under job.CompanyId and job.JobId
  -> create initial ApplicantLogs record
  -> create two-hour resume token
  -> queue acknowledgement email
  -> return application reference and raw resume token
```

### 9.3 Duplicate/reapplication rule

The public service checks the most recent applicant with the same email **within the selected company**.

- If it was created during the previous six months, the request is rejected.
- If none exists, stage is `New`.
- If an older application exists, stage is `ReApply`.

This rule is company-scoped but not vacancy-scoped. A recent application to any job in the same company blocks another public application using the same email.

### 9.4 Successful response

The response includes:

- `ApplicationReference`
- `ResumeUploadToken`
- `TokenExpiresAt`
- `JobTitle`
- `CompanyName`

The raw resume token should be treated as a secret and is not stored raw in MongoDB.

## 10. Resume upload flow

```http
POST /api/public/applications/{applicationReference}/resume
Content-Type: multipart/form-data

token={raw token}
resume={PDF file}
```

The service validates:

1. A non-empty file exists.
2. Size is at most exactly `5 * 1024 * 1024` bytes.
3. Extension equals `.pdf`, case-insensitively.
4. Token hash and application reference match an unused token.
5. Token has not expired.
6. File begins with the PDF signature `%PDF`.
7. Applicant ID and company ID match the token record.

On success:

1. A GUID-named `.pdf` is written under `Uploads/Resume`.
2. `Applicant.ResumeUrl` is updated.
3. Applicant audit fields are updated.
4. The token is marked used with `UsedAt`.

The token expires two hours after application and is single-use.

## 11. External application flow

For an external job:

1. HR sets `ApplicationMode = External`.
2. HR supplies a valid external HTTPS URL.
3. The public DTO returns the URL only when it remains safe.
4. The frontend displays “Apply on company site”.
5. The backend rejects attempts to use the internal application endpoint for that job.

Invalid, non-HTTPS, loopback, or unparsable external URLs are discarded.

## 12. Email flow

After a public internal application is inserted:

1. The service loads `ApplyNowMailToApplicant`.
2. It renders the subject/body using candidate, job, and company values.
3. If the template is absent, it uses a built-in acknowledgement.
4. It also loads `ApplyNowMailToHR` for internal recruitment notifications.
5. It resolves recipients from `JobVacancy.RecruiterContactEmail` plus active verified company users whose role type is `Administrator` or `HR`.
6. It queues work through `IPriorityTaskQueue`.
7. The background worker uses `IMiddlewareService.EmailSendAndSave`.
8. Email delivery is recorded in `EmpEmailLogs`.

Application persistence does not wait for SMTP delivery.

Authenticated applicant edits may also trigger applicant stage-update emails in `ApplicantServices`. Candidate emails are sent when the activity type changes and currently cover `New`, `InProgress`, `OnHold`, `Shortlisted`, `Selected`, `Rejected`, and `ReApply`, using `SelectedMail` and `RejectedMail` where available and fallback bodies for the other stages.

## 13. Authenticated applicant management

Base route: `api/Applicants`

| Endpoint | Permission | Behavior |
|---|---|---|
| `POST GetApplicantList` | `Applications/View` | Filtered/paged tenant applicant list |
| `GET ApplicantById?id=...` | `Applications/View` | Applicant details |
| `POST AddApplicant` | `Applications/Create` | HR-created applicant |
| `POST EditApplicant` | `Applications/Edit` | Profile/stage/status update |
| `POST AddComment` | `Applications/Edit` | Applicant comment/log |
| `GET GetAllComment/{applicantId}` | `Applications/View` | Applicant comments/logs |
| `POST GetProcessLogData` | `ProcessLog/View` | Cross-applicant process log |
| `POST UploadResume?email=...` | Anonymous legacy endpoint | Older email-authorized upload path |

### 13.1 HR-created applicant

The authenticated creation path:

- validates the model;
- checks its existing duplicate rules;
- creates a tenant applicant;
- adds an initial log;
- queues acknowledgement email.

It is separate from `PublicCareerService.Apply`.

### 13.2 HR review and stage updates

```text
Recruitment user
  -> Applications/View
  -> filter and open applicant
  -> inspect job/application/resume
  -> Applications/Edit
  -> change applicant/profile/stage/status
  -> optional stage email
  -> optional comment/process log
```

Current backend behavior does not provide interview rounds, interview scheduling, scorecards, offer approvals, offer documents, or automatic employee conversion.

## 14. Modern public flow versus retained legacy flow

Two public mechanisms currently exist.

### Preferred modern flow

- `api/public/...`
- Resolves tenant from the selected job or six-digit company code.
- Does not trust a browser-provided `CompanyId`.
- Uses public job IDs.
- Uses signed-like random upload tokens stored as hashes.
- Checks exact size, extension, PDF signature, expiration, use state, applicant, and company.
- Creates the initial public application log.

### Retained legacy flow

- Anonymous `JobVacancy/GetAllVacancy`
- `Account/applicant/applyJob`
- Anonymous `Applicants/UploadResume?email=...`
- Historically depends on browser company context/header behavior.
- Resume ownership is based on knowing an email address.

The current React careers pages use the modern flow. The legacy actions remain callable for backward compatibility and should not be used by new public UI.

## 15. Security and trust boundaries

### Enforced in the modern flow

- Company ownership is derived from `JobVacancy.CompanyId`.
- Public company lookup uses a non-internal six-digit identifier.
- Jobs must be active, non-deleted, and public-career enabled.
- Company must be active, non-deleted, and career enabled.
- Master visibility requires both company and job master flags.
- Closed jobs cannot receive an internal application.
- External jobs cannot use internal apply.
- Resume token is random, hashed, expiring, single-use, applicant-bound, and company-bound.
- Resume size uses exact bytes.
- Extension check is case-insensitive.
- Basic PDF magic-byte verification is present.
- External URLs require HTTPS and cannot be loopback.
- Job HTML is sanitized in the browser before rendering.

### Remaining considerations

- PDF magic bytes are only a basic file check; antivirus/content scanning is not implemented.
- Resume storage is local filesystem storage, not object storage.
- Application insertion, log insertion, token insertion, and email queueing are not one MongoDB transaction.
- Job search filtering occurs in memory after loading eligible jobs for selected companies.
- Job slug lookup is not scoped by company in the API; the frontend cross-checks returned company code.
- Six-digit space is finite; random retry is adequate for the current scale but should be monitored at very large tenant counts.
- The legacy anonymous resume endpoint remains weaker than the modern tokenized route.
- Stage transitions are not documented as an enforced state machine.

## 16. Migrations and indexes

`BackfillPublicCareerMarketplaceFields`:

- assigns/repairs company career slugs;
- replaces missing, invalid, duplicate, or alphanumeric public company codes with six-digit codes;
- initializes company publishing flags;
- creates job public IDs and slugs;
- initializes job publishing/application fields;
- creates supporting indexes.

Important indexes:

| Index | Purpose |
|---|---|
| `ux_company_public_code` | Unique company public code |
| `ux_job_public_id` | Unique public job ID |
| `ix_job_public_slug` | Job slug lookup |
| `ix_public_career_jobs` | Company and public publishing query support |

The migration is idempotent through the `Migration` collection. Its July 24 migration ID intentionally reruns the public-code backfill after the earlier alphanumeric implementation.

## 17. Common operational scenarios

### Company URL returns 404

Check:

- URL contains exactly six digits.
- Company `PublicCompanyCode` matches.
- `Status = true`.
- `IsDeleted = false`.
- `CareerPortalEnabled = true`.
- migrations have run.

### Company page has no jobs

Check:

- jobs belong to that company's internal `CompanyId`;
- job `Status = true`;
- job `IsDeleted = false`;
- `PublishToCareerPortal = true`;
- expiry and deadline have not passed.

`PublishToMasterPortal` is not required on a company-specific page.

### Job missing from `/careers`

In addition to the company-page checks:

- company `PublishJobsToMasterPortal = true`;
- job `PublishToMasterPortal = true`.

### Application rejected

Possible reasons:

- job is missing/inactive/deleted/not published;
- company portal is unavailable;
- expiry or deadline passed;
- job uses external application mode;
- same email applied to the same company during the last six months.

### Resume rejected

Possible reasons:

- file is absent or empty;
- file exceeds 5 MiB;
- filename does not end with `.pdf`;
- file does not begin with `%PDF`;
- token/reference mismatch;
- token expired;
- token was already used.

## 18. Local development

Backend:

```powershell
dotnet run --project Codeji.CMS.API
```

Frontend:

```powershell
cd "..\CMS-React"
npm run dev
```

Migrations:

```powershell
dotnet run --project Codeji.CMS.Migrations
```

Common local URLs:

```text
http://localhost:5173/careers
http://localhost:5173/careers/{sixDigitCompanyCode}
http://localhost:5173/careers/{sixDigitCompanyCode}/jobs/{jobSlug}
http://localhost:5036/swagger/index.html
```

The migration runner uses its configured MongoDB connection. Confirm the target environment before running it.

## 19. Suggested validation matrix

### Company codes

- New registration always saves a six-digit code.
- Codes are unique under concurrent registrations.
- Existing blank/alphanumeric/duplicate codes are repaired by migration.
- Admin company details returns the code.
- Admin-generated career link uses the code.

### Public listing

- Master page returns only companies and jobs with both master flags.
- Company page returns its portal jobs without requiring master flags.
- Invalid/nonexistent company code returns 404.
- Inactive/deleted/expired jobs are absent.
- Search, filters, sorting, page size, and paging behave as documented.

### Job details

- Public ID and slug lookup return the expected job.
- Inactive/deleted/private job returns 404.
- URL company code mismatch is rejected by the frontend.
- HTML description is sanitized and formatted.

### Application

- Candidate cannot submit `CompanyId` or choose tenant ownership.
- Applicant inherits company and vacancy from the selected job.
- Recent same-company email is rejected.
- Older same-company email creates `ReApply`.
- Different-company application is not blocked by the same email.
- External jobs reject internal application requests.
- Initial log, applicant acknowledgement, and HR/admin notification emails are created/queued.

### Resume

- Valid token and PDF succeed once.
- Reused, expired, incorrect, or cross-reference token fails.
- Oversized, empty, renamed non-PDF, and invalid-signature files fail.
- Applicant resume path is updated only within the token company.

### Authenticated recruitment

- Vacancy CRUD respects tenant and module permissions.
- Applicant list/details respect tenant and permissions.
- Comments and process logs are company-scoped.
- Stage emails use the expected template.
- Deleted vacancies and orphan applicant references are handled consistently.

## 20. Current product boundaries

The current implementation covers vacancy publishing, candidate intake, resume attachment, applicant review, stages, comments/log reporting, and basic recruitment email communication.

It does not currently implement:

- vacancy approval workflows;
- recruiter/hiring-manager assignment;
- interview rounds or scheduling;
- interviewer feedback and scorecards;
- candidate portal/login;
- candidate withdrawal;
- offer approvals, generation, or e-signature;
- background verification;
- document collection;
- automatic onboarding or employee conversion;
- consent, retention, anonymization, or deletion workflows;
- durable transactional outbox across all recruitment writes.

These are future capabilities and should not be assumed by clients integrating with the current API.

## 21. Latest frontend flow and navigation changes

### 21.1 Public application location

The public application form no longer accepts arbitrary state text. It uses the shared
`indianStates` autocomplete used by authenticated Add/Edit Applicant screens.

- candidates can search and select with mouse or keyboard;
- selection is required before submission;
- clearing resets the stored state value;
- the dropdown follows the current application theme;
- state and experience remain side-by-side where space permits and stack on mobile;
- the backend continues to receive the selected state as a string.

### 21.2 Applications list layout

The authenticated Applications toolbar now uses one responsive row at tablet and
desktop widths:

```text
[ expanding search field ] [ filter ] [ add applicant ]
```

On narrow phones the action group and search field stack to avoid clipped or compressed
controls. Filter count, permissions, search debounce, grid loading, paging, and row
navigation behavior are unchanged.

### 21.3 Applicant details presentation

Applicant identity, contact, job, experience, applied date, application stage, and
active status are presented in responsive theme-aware information surfaces. Comment
history is contained in a bordered region with horizontal overflow on narrow screens.
CV preview, stage updates, status changes, permissions, comments, and pagination retain
their existing service behavior.

### 21.4 Applicant edit and return navigation

Canonical internal routes:

```text
/recruitment/applications
/recruitment/applicant/{applicantId}
/recruitment/applicant/edit/{applicantId}
```

React Router and recruitment breadcrumb definitions use the parameter name
`applicantid`. The shared `PageData` component resolves placeholder values from the
current URL. Edit Applicant also receives an explicit Applicant parent target because
the parent and edit routes place their dynamic value at different segment positions.

All exits from Edit Applicant use deterministic URLs:

- **Applicant breadcrumb** → `/recruitment/applicant/{actualApplicantId}`;
- **Cancel** → `/recruitment/applicant/{actualApplicantId}`;
- **successful Update** → replaces the edit URL with the same applicant detail URL.

The flow does not use `navigate(-1)` or `window.history.back()`. It therefore works
after direct navigation, refresh, or opening the edit page in a new tab. Generated URLs
must never contain literal `%7Bid%7D`, `{id}`, or `{applicantid}` values.

### 21.5 Current responsive recruitment UI

- Public careers, public job details, and the application modal follow MUI theme tokens.
- The application modal has responsive fields/actions, an explicit close control, PDF
  upload feedback, and clear error/loading states.
- Applications search/actions remain compact and responsive.
- Applicant details and comments use modern card hierarchy and readable spacing.
- The main layout and sidebar own independent bounded scroll regions where required.

### 21.6 Regression checklist for the latest changes

- Apply modal cannot submit without a selected state.
- Selected state is persisted on the applicant and shown during edit.
- Applications search, filter count, create permission, paging, and row click work.
- Applicant detail loads with the real ID from the route.
- Edit opens with that same ID.
- Applicant breadcrumb, Cancel, and Update all return to the same applicant.
- No route contains an encoded or literal placeholder.
- Frontend `npm run build` succeeds.
- Backend `dotnet build CodejiCMSCore.sln --no-restore` succeeds when the API process is
  not locking build output DLLs.

## 22. Career Portal integration (July 2026)

Recruitment and Career Portal are now explicitly separate but connected modules.

```text
Authenticated Recruitment
  creates/edits JobVacancy
  owns publication flags
  owns Applicant and ApplicantLogs
  owns application and resume workflow
          |
          | eligible public projection and meaningful transition
          v
Career Portal
  public discovery and company profiles
  saved jobs and anonymous visitor preferences
  verified subscribers, follows, and search alerts
  analytics
  durable notification outbox and delivery history
```

The integration is additive. Existing recruitment endpoints, DTO properties, IDs,
applicant stages, comments, resume validation, email acknowledgement, and navigation
are not renamed or replaced.

### 22.1 Vacancy fields added for candidate presentation

`JobVacancy` and `JobVacancyModel` now carry nullable/initialized structured public
fields:

```text
Summary
Department
FunctionalArea
Industry
RoleCategory
EducationRequirement
Responsibilities
RequiredSkills
PreferredSkills
Benefits
Keywords
ShiftType
WorkingDays
TravelRequirement
IsFeatured
IsUrgentHiring
IsWalkIn
WalkInStartAt
WalkInEndAt
WalkInAddress
RecruiterContactEmail
NoticePeriodMaxDays
ShowSalary
```

`JobVacancyService.AddJobVacancy` maps them on creation.
`EditJobVacancy` preserves the stored value when an older client omits a new field,
which keeps the authenticated API backward-compatible.

### 22.2 Publication transition handoff

After a successful vacancy insert or edit, `JobVacancyService` calls
`ICareerNotificationService.HandleJobSaved(previous, current)`.

This call does not change the save result or applicant flow. The notification service
resolves the persisted tenant company and computes:

- previous company-page eligibility;
- current company-page eligibility;
- previous master-marketplace eligibility;
- current master-marketplace eligibility;
- whether a selected candidate-facing field meaningfully changed.

`NewJob` is queued only for a false-to-true visibility transition. `JobUpdated` is
separate. Repeated edits to an already public job do not become repeated new-job
messages.

### 22.3 Career-profile administration

Authenticated route:

```text
/recruitment/career-profile
```

API:

```http
GET /api/CareerProfile
PUT /api/CareerProfile
```

The controller reads `CompanyId` and `UserId` from `CurrentContext`; neither value is
accepted from the request body. The `Career_Profile` module has `View` and `Edit`
permissions. The migration grants them to active administrator and HR roles and the
normal role-creation path can use the seeded module for later companies.

The profile is a separate document, so editing it cannot overwrite operational
`Company` fields used by payroll, attendance, authentication, or recruitment.

### 22.4 Public entry into recruitment remains unchanged

The new `CareerJobDetails` page still calls the existing public application endpoints
and renders `PublicJobApplyModal` from `modules/applynow`.

Internal mode:

```text
CareerJobDetails
 -> PublicJobApplyModal
 -> POST /api/public/jobs/{publicJobId}/applications
 -> existing six-month check
 -> Applicant(New or ReApply)
 -> initial ApplicantLogs entry
 -> existing acknowledgement email
 -> two-hour single-use resume token
 -> POST /api/public/applications/{reference}/resume
```

External mode:

```text
CareerJobDetails
 -> server-provided validated HTTPS ExternalApplicationUrl
 -> new browser tab with noopener/noreferrer
```

The browser still cannot choose the applicant tenant or internal vacancy ID.

### 22.5 New public routes

Existing canonical routes:

```text
/careers
/careers/{sixDigitCompanyCode}
/careers/{sixDigitCompanyCode}/jobs/{jobSlug}
```

New additive routes:

```text
/careers/saved
/careers/preferences
```

Compatibility redirects remain:

```text
/career
/career/{legacyValue}
```

### 22.6 New public APIs

Company presentation:

```http
GET /api/public/career/companies
GET /api/public/career/companies/{companyCode}/profile
```

Double-opt-in subscriber lifecycle:

```http
POST /api/public/career/subscribers
POST /api/public/career/subscribers/verify
POST /api/public/career/subscribers/resend-verification
POST /api/public/career/subscribers/unsubscribe
GET  /api/public/career/subscribers/preferences
PUT  /api/public/career/subscribers/preferences
```

Engagement:

```http
POST   /api/public/career/jobs/{publicJobId}/save
DELETE /api/public/career/jobs/{publicJobId}/save
GET    /api/public/career/jobs/saved
POST   /api/public/career/jobs/merge-anonymous-saves

POST   /api/public/career/companies/{companyCode}/follow
DELETE /api/public/career/companies/{companyCode}/follow
GET    /api/public/career/companies/{companyCode}/follow-status

POST   /api/public/career/job-alerts
GET    /api/public/career/job-alerts
PUT    /api/public/career/job-alerts/{alertId}
DELETE /api/public/career/job-alerts/{alertId}

PUT  /api/public/career/visitor-preferences
POST /api/public/career/analytics
```

Subscriber-owned APIs authorize an opaque career session from `X-Career-Session`.
Email alone is never authorization.

Subscribe, resend, verify, save, and follow actions are additionally protected by a
fixed-window per-address rate limit. Oversubscribed callers receive HTTP 429 rather
than entering the service or mail queue.

### 22.7 New persistence relations

| Collection | Relation and role |
|---|---|
| `PublicCompanyProfile` | one active profile per `CompanyId` |
| `CareerSubscriber` | normalized email, verification/session/unsubscribe state |
| `JobAlertSubscription` | many saved searches per subscriber |
| `CompanyFollower` | unique subscriber/company relationship |
| `SavedJob` | subscriber or anonymous visitor hash related to a server-resolved job |
| `CareerVisitorPreference` | non-sensitive server-side popup/visit state |
| `CareerNotificationOutbox` | durable delivery intent and retry state |
| `CareerNotificationDelivery` | unique subscriber/job/reason delivery history |
| `CareerAnalyticsEvent` | allow-listed public interaction events |

Anonymous visitor, verification, session, and unsubscribe secrets are never stored as
raw reusable values. Visitor/verification/session values use hashes; marketing
unsubscribe links use an expiring HMAC signature.

### 22.8 Durable notification operations

`CareerNotificationWorker` is a hosted service independent of the pre-existing
in-memory priority queue. The in-memory queue continues to handle the existing
application acknowledgement and immediate verification email. Job publication
notifications use MongoDB durability:

```text
Pending/Failed and AvailableAt <= now
 -> atomic claim as Processing
 -> existing email sender and EmpEmailLogs
 -> CareerNotificationDelivery
 -> Sent

send failure
 -> exponential AvailableAt backoff
 -> Failed
 -> retry
 -> DeadLetter after attempt 5
```

Processing rows older than ten minutes are reclaimable, making API restarts safe.
Delivery uniqueness prevents a company follow and matching search alert from producing
two messages for the same subscriber/job/reason.

Daily and weekly due rows are claimed per subscriber and delivered as one grouped
digest. The worker also performs an hourly saved-job deadline scan and queues
`SavedJobDeadline` reminders through the same durable, retryable path.

### 22.9 Search and UI behavior

Public filters are built into the Mongo `JobVacancy` query before count, sort, skip,
and limit. Page size remains capped at 50. Company name filtering is applied before the
job query. Company size is resolved through published profile relationships.

The React marketplace:

- uses current MUI light/dark theme tokens;
- has a desktop sticky filter panel and a mobile bottom drawer;
- has responsive job cards and active filter feedback;
- displays featured, urgent, and walk-in labels;
- supports save/unsave without navigating away;
- exposes rich structured job sections and similar jobs;
- provides company story, values, benefits, and culture;
- displays published company discovery cards and locally remembered recently viewed
  jobs;
- offers a delayed, dismissible, double-opt-in subscription prompt;
- reads server-side visitor suppression before showing the prompt (`Not now`: 7 days,
  `Do not show again`: 180 days, verified session: suppressed);
- preserves the existing apply modal and location selector.

### 22.10 Current regression result

The Recruitment vacancy editor remains protected by the existing Jobs module
permissions. An authorized HR/admin user posting or editing the vacancy owns the
candidate-facing job description and the expanded career metadata: summary,
location, work/employment type, experience, department, industry, functional area,
role category, education, responsibilities, skills, benefits, keywords, shift,
working days, recruiter email, deadline, publication scope, featured, and urgent
hiring.

Saved-job ObjectId compatibility, bookmark restoration after refresh, company-name
search, and API-backed location suggestions were added on 24 July 2026. Empty public
company-story sections are suppressed instead of displaying a blank panel.

The vacancy editor now uses the tenant's active Department master for department
selection. Existing Job Type values are the single source for public work mode
(`On-site`, `Hybrid`, or `Remote`), while employment type remains the separate
contract relationship. Shift and working-days inputs use controlled option lists,
and the deadline uses the MUI date-time calendar/clock with 15-minute steps. Inline
help explains every candidate-facing field.

Publication controls enforce these rules in the UI and payload:

- company careers publishes to the company's six-digit career page;
- all-company careers requires company-career publication and the company-level
  master-portal setting;
- featured and urgent only add badges/filter eligibility and never publish an
  otherwise inactive/private/expired job;
- turning off company-career publication also turns off master publication.

Validation performed after the integration:

```text
Backend solution build: passed
Backend tests: 48 passed, 0 failed
Frontend tests: 10 passed, 0 failed
Frontend production build: passed
```

New tests cover signed unsubscribe token round-trip, tamper rejection, and removal of
executable rich-profile content. Existing recruitment tests continue to pass.
