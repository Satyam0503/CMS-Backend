# Public Careers: Current Flow and Implementation Details

> **Implementation snapshot:** 29 July 2026
> **Status:** Current Career Portal marketplace, company profile, engagement, and durable-notification implementation.  
> **Important:** The earlier slug + `localStorage.clientId` + `cId` public flow is legacy behavior and is not the canonical architecture documented here.

## 1. Purpose

The public careers feature allows anonymous visitors to browse eligible jobs across all companies, browse one company's jobs using a stable public code, view job details, apply internally, securely upload a PDF resume, or follow a validated external application URL.

HR continues to create vacancies and manage applicants through the authenticated CMS. Public applications enter that same recruitment workflow.

## 2. Canonical public URLs

| Route | Meaning |
|---|---|
| `/careers` | Master marketplace containing eligible jobs from all companies |
| `/careers/{sixDigitCompanyCode}` | Career portal for one company |
| `/careers/{sixDigitCompanyCode}/jobs/{jobSlug}` | Public job details |
| `/career` | Compatibility redirect to `/careers` |
| `/career/{legacyValue}` | Compatibility redirect to `/careers` |

Examples:

```text
http://localhost:5173/careers
http://localhost:5173/careers/483921
http://localhost:5173/careers/483921/jobs/mern-stack-developer-job-4e03
```

Company names, internal company IDs, and company slugs are not canonical URL keys.

## 3. Identifier and ownership model

| Identifier | Responsibility |
|---|---|
| `CompanyId` | Internal tenant key and foreign key; never selected by a public candidate |
| `PublicCompanyCode` | Unique persistent six-digit public routing code |
| `CareerSlug` | Retained compatibility data; not used by current frontend routes |
| `JobId` | Internal vacancy identifier and `Applicant.VacancyId` target |
| `PublicJobId` | Public-safe stable identifier used by application APIs |
| `Slug` | Human-readable public job URL segment |
| `ApplicantId` | Applicant key and returned application reference |

```text
PublicCompanyCode -> Company.CompanyId
Job slug/public ID -> JobVacancy.CompanyId + JobVacancy.JobId
Public application -> Applicant.CompanyId copied from JobVacancy.CompanyId
                   -> Applicant.VacancyId copied from JobVacancy.JobId
```

The browser never provides authoritative tenant ownership.

## 4. Company-code lifecycle

During `CompanyService.Register`, the backend:

1. creates the internal `CompanyId`;
2. generates a cryptographically random number between `100000` and `999999`;
3. checks that the value is unused;
4. persists it as `Company.PublicCompanyCode`;
5. enables the career portal and initializes publishing settings.

The `ux_company_public_code` database index enforces uniqueness. `EnsureCareerPortal` and the public-career migration repair missing, malformed, alphanumeric, or duplicate codes.

The authenticated Company Details screen builds:

```text
{frontendOrigin}/careers/{PublicCompanyCode}
```

## 5. Publishing rules

Company controls:

- `CareerPortalEnabled`
- `PublishJobsToMasterPortal`
- `Status`
- `IsDeleted`
- `PublicCompanyCode`

Job controls:

- `PublishToCareerPortal`
- `PublishToMasterPortal`
- `Status`
- `IsDeleted`
- `ExpiresAt`
- `ApplicationDeadline`
- `ApplicationMode`
- `ExternalApplicationUrl`

| Condition | Company portal | Master marketplace |
|---|---:|---:|
| Company inactive, deleted, or portal disabled | Hidden | Hidden |
| Job inactive, deleted, expired, or past deadline | Hidden | Hidden |
| Job career publishing disabled | Hidden | Hidden |
| Job career publishing enabled | Visible | Depends on master flags |
| Company or job master publishing disabled | Visible | Hidden |
| Both master flags enabled | Visible | Visible |

## 6. Implementation map

### Backend

| Concern | File |
|---|---|
| Public controller | `Codeji.CMS.API/Controllers/PublicCareerController.cs` |
| Public service | `Codeji.CMS.Services/Recruitments/PublicCareerService.cs` |
| Service interface | `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs` |
| Public DTOs | `Codeji.CMS.DTO/PublicCareers/` |
| Company code generation | `Codeji.CMS.Services/Companies/CompanyService.cs` |
| Vacancy fields | `Codeji.CMS.Repository/Entities/Recruitments/JobVacancy.cs` |
| Company fields | `Codeji.CMS.Repository/Entities/Company/Company.cs` |
| Resume token | `Codeji.CMS.Repository/Entities/Recruitments/PublicApplicationToken.cs` |
| Backfill/indexes | `Codeji.CMS.Migrations/Migrations/BackfillPublicCareerMarketplaceFields.cs` |

### Frontend

| Concern | File |
|---|---|
| Public routes | `CMS-React/src/app/routing/AppRoutes.tsx` |
| Marketplace/company listing | `CMS-React/src/app/modules/applynow/MasterCareers.tsx` |
| Job details | `CMS-React/src/app/modules/applynow/PublicJobDetails.tsx` |
| Application modal | `CMS-React/src/app/modules/applynow/PublicJobApplyModal.tsx` |
| API client | `CMS-React/src/app/modules/applynow/publicCareerService.ts` |
| Public models | `CMS-React/src/app/modules/applynow/publicCareerTypes.ts` |

The public UI is responsive and uses MUI theme tokens for light/dark and palette variants.

## 7. Public API map

Base route: `/api/public`.

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/jobs` | Master marketplace |
| `GET` | `/jobs/{publicJobId}` | Job details by public ID |
| `GET` | `/jobs/by-slug/{jobSlug}` | Job details by slug |
| `GET` | `/companies/{companyCode}/jobs` | One company's eligible jobs |
| `POST` | `/jobs/{publicJobId}/applications` | Create internal application |
| `POST` | `/applications/{applicationReference}/resume` | Token-authorized PDF upload |

These routes are anonymous. Ownership, publishing, deadline, and status rules remain server-enforced.

## 8. Listing and details flow

Master marketplace:

```text
GET /api/public/jobs
  -> active, non-deleted, career-enabled companies
  -> company master publishing enabled
  -> active, non-deleted, career-published jobs
  -> job master publishing enabled
  -> remove expired/deadline-passed jobs
  -> apply filters, sort, and paging
  -> attach company public code/name/logo 
```
 
Company portal:

```text
GET /api/public/companies/{sixDigitCode}/jobs
  -> resolve active career-enabled company
  -> filter by internal CompanyId
  -> require job career publishing
  -> do not require master publishing
```

Job details:

1. Frontend loads the job by slug.
2. Backend validates job/company availability and publishing.
3. Frontend verifies the returned company code matches the URL.
4. DOMPurify sanitizes rich job HTML.
5. Internal jobs show the application modal.
6. External jobs show an external apply action.

External URLs must be absolute, HTTPS, non-loopback, and parseable.

## 9. Internal application flow

The public modal collects first name, last name, email, phone, state, experience, and an optional PDF resume.

State uses the same searchable `indianStates` selector as authenticated applicant forms. It supports typing, keyboard selection, clearing, theme-aware styling, and responsive layout. A selected state is required.

```http
POST /api/public/jobs/{publicJobId}/applications
Content-Type: application/json
```

```json
{
  "firstName": "Aman",
  "lastName": "Singh",
  "email": "aman@example.com",
  "phone": "9876543211",
  "state": "Uttarakhand",
  "experience": 2
}
```

The request contains no `CompanyId`, `JobId`, `clientId`, or `cId`.

```text
PublicJobId
  -> load eligible job
  -> load company from job.CompanyId
  -> reject external/expired/unavailable job
  -> normalize email
  -> enforce same-company six-month application rule
  -> create Applicant using job CompanyId and JobId
  -> create initial ApplicantLogs record
  -> create two-hour resume token
  -> queue acknowledgement email
  -> return application reference + raw token
```

A recent same-company application blocks another public application. An older application is created with the `ReApply` stage. The same email can apply to another company.

## 10. Resume upload

```http
POST /api/public/applications/{applicationReference}/resume
Content-Type: multipart/form-data
```

Inputs are the raw token and resume file. Validation requires:

- a non-empty file;
- maximum `5 * 1024 * 1024` bytes;
- `.pdf` extension;
- `%PDF` signature;
- matching token hash and application reference;
- unexpired and unused token;
- matching applicant and company.

On success, a GUID-named PDF is written under `Uploads/Resume`, the applicant is updated, and the token is marked used. Only the SHA-256 token hash is stored.

## 11. Authenticated handoff

```text
/recruitment/applications
  -> select candidate
  -> /recruitment/applicant/{applicantId}
  -> edit
  -> /recruitment/applicant/edit/{applicantId}
```

The Applicant breadcrumb, Cancel, and successful Update all return directly to the same applicant ID. Return navigation does not depend on browser history and must never emit literal `{id}` or `{applicantid}` segments.

## 12. Migration

`BackfillPublicCareerMarketplaceFields`:

- assigns/repairs six-digit company codes;
- initializes company career and publishing fields;
- creates missing public job IDs and unique slugs;
- initializes publishing and application-mode defaults;
- creates public-career indexes;
- is idempotent through migration tracking.

Run against the intended database:

```powershell
dotnet run --project Codeji.CMS.Migrations
```

## 13. Security boundaries

- Public users cannot select company ownership.
- Public DTOs avoid exposing internal entities.
- External jobs reject internal application submissions.
- External links are validated.
- Job HTML is sanitized.
- Resume uploads require expiring single-use tokens.
- Resume extension and signature are validated.
- Public codes are routing identifiers, not authorization secrets.
- Authenticated recruitment remains tenant- and permission-scoped.

## 14. Failure diagnosis

### Company route returns 404

- code is not six digits;
- company is inactive/deleted;
- portal is disabled;
- code backfill has not run.

### Jobs are missing

- career publishing is disabled;
- company/job is inactive or deleted;
- deadline/expiry passed;
- master flags are missing when testing `/careers`.

### Application fails

- job is unavailable, expired, or external;
- company portal is unavailable;
- same email applied to that company within six months;
- state was not selected.

### Resume fails

- file is missing, oversized, or not a real PDF;
- token/reference mismatch;
- token expired or was already used.

## 15. Local validation

```powershell
# Backend
dotnet run --project Codeji.CMS.API

# Frontend from CMS-React
npm run dev
```

Validate:

1. new companies receive unique six-digit codes;
2. `/careers` works anonymously without `cId`;
3. `/careers/{code}` only shows that company's eligible jobs;
4. job details reject a company-code mismatch;
5. applications inherit company and vacancy from the job;
6. a valid PDF uploads once;
7. the applicant appears internally;
8. Applicant → Edit → Applicant works via breadcrumb, Cancel, and Update;
9. frontend and backend builds pass.

## 16. Career Portal module added in the July 2026 upgrade

The public browsing experience is now a separate Career Portal module alongside the
existing Recruitment module. Recruitment still owns vacancies, publication settings,
applications, resume intake, applicant stages, comments, and logs. Career Portal owns
candidate-facing discovery, company presentation, subscriptions, saved jobs, follows,
alerts, analytics, and job-notification delivery.

No canonical identifier was replaced:

```text
PublicCompanyCode -> Company.CompanyId
PublicJobId or Slug -> JobVacancy.JobId
JobVacancy.CompanyId -> tenant owner
JobVacancy.JobId -> Applicant.VacancyId
Applicant.ApplicantId -> application reference
```

Public requests still cannot authoritatively supply `CompanyId`, `JobId`, `clientId`, or
`cId`.

### 16.1 Backend implementation map

| Area | Current files |
|---|---|
| Career entities | `Codeji.CMS.Repository/Entities/CareerPortal/CareerPortalEntities.cs` |
| Career DTOs | `Codeji.CMS.DTO/CareerPortal/CareerPortalDtos.cs` |
| Profile service | `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs` |
| Subscriber service | `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs` |
| Save/follow/alert service | `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs` |
| Durable notification service | `Codeji.CMS.Services/CareerPortal/CareerNotificationService.cs` |
| Outbox worker | `Codeji.CMS.Services/CareerPortal/CareerNotificationWorker.cs` |
| Public profile API | `Codeji.CMS.API/Controllers/PublicCompanyProfileController.cs` |
| Public subscriber API | `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs` |
| Public engagement API | `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs` |
| Authenticated profile API | `Codeji.CMS.API/Controllers/CareerProfileController.cs` |
| Discovery/application API | `Codeji.CMS.API/Controllers/PublicCareerController.cs` |
| Schema/index migration | `Codeji.CMS.Migrations/Migrations/AddCareerPortalModule.cs` |
| Permission migration | `Codeji.CMS.Migrations/Migrations/AddCareerProfilePermissions.cs` |

Services are registered through `ServicesRegistration.AddBusinessServices`. The durable
worker is registered in `Codeji.CMS.API/Program.cs`.

### 16.2 Frontend implementation map

The existing `applynow` application modal and application service remain in use.
New browsing and engagement behavior lives under:

```text
CMS-React/src/app/modules/careerPortal/
  components/
    CareerHeader.tsx
    CompanyLogo.tsx
    JobCard.tsx
    SaveJobButton.tsx
    SubscribeModal.tsx
  context/
    SavedJobsContext.tsx
  hooks/
    useCareerVisitor.ts
  pages/
    CareerMarketplace.tsx
    CareerJobDetails.tsx
    SavedJobsPage.tsx
    CareerPreferencesPage.tsx
    CareerProfileAdminPage.tsx
  services/
    careerPortalService.ts
  types/
    careerPortalTypes.ts
```

`AppRoutes.tsx` maps the established public URLs to the new pages. Recruitment routing
adds `/recruitment/career-profile` for authorized company administrators.

## 17. Public company profiles

`PublicCompanyProfile` is a separate, one-per-company document. `CompanyId` is the
authoritative relation; `PublicCompanyCode` is copied only for public lookup and
routing. Profile content includes:

- display name, short description, logo, cover, website;
- industry, company type/size, founded year, headquarters, office locations;
- mission, vision, values, benefits, and technologies;
- work-culture, hiring-process, diversity, and about-company rich content;
- social links and draft/published status.

Public behavior:

1. Resolve an active, non-deleted, career-enabled company by its six-digit code.
2. Load its active published profile.
3. If the profile is missing or is still a draft, return a safe fallback using the
   existing company name and logo. The company career page therefore never breaks
   merely because HR has not completed a profile.
4. The company page combines the profile hero/story/culture with that company's
   eligible jobs. The backend-managed Company Details upload is expanded to the
   configured static CompanyLogo URL and is the only source for the public logo.
5. The profile lookup normalizes the incoming code before the MongoDB query and then
   uses an equality-only database predicate. Custom .NET helpers are never embedded
   in a Mongo LINQ expression.

Administration behavior:

- `GET /api/CareerProfile` reads only the authenticated user's tenant profile.
- `PUT /api/CareerProfile` writes only that tenant profile.
- `Career_Profile/View` and `Career_Profile/Edit` permissions are created for
  administrator and HR roles by migration.
- the React editor supports preview, draft, and publish states;
- rich HTML is sanitized on the server and again before React renders it;
- the company logo is read-only in the Career Profile editor and always comes from the
  authenticated Company Details upload; it cannot be overridden by a profile URL;
- the company name is read-only in the Career Profile editor and always comes from
  Company Details; profile saves cannot rename the company;
- cover, website, and social URLs must be valid HTTP/HTTPS URLs.

## 18. Expanded job presentation and search

`JobVacancy` keeps every existing property and adds nullable/safely initialized
candidate-facing fields:

```text
Summary, Department, FunctionalArea, Industry, RoleCategory,
EducationRequirement, Responsibilities, RequiredSkills, PreferredSkills,
Benefits, Keywords, ShiftType, WorkingDays, TravelRequirement,
IsFeatured, IsUrgentHiring, IsWalkIn, WalkInStartAt, WalkInEndAt,
WalkInAddress, RecruiterContactEmail, NoticePeriodMaxDays, ShowSalary
```

Vacancy creation/editing maps these fields without renaming existing fields. Public
summary and detail DTOs expose them additively.

Search filtering is performed in MongoDB before paging. Supported inputs are:

```text
search, company, location, workplace type, employment type, job type,
experience range, salary range, currency, department, industry,
role category, education, skills, date posted, company size,
featured, urgent hiring, walk-in, application mode
```

Supported sort values are:

```text
relevance, newest, oldest, deadline, salary-high, salary-low,
most-viewed, most-saved
```

Page numbers are one-based and page size remains clamped to 1–50. Most-viewed and
most-saved sorting reads only eligible job keys and engagement counters before loading
the requested page; it does not load all job documents for application-side filtering.
Search text is regex-escaped before constructing Mongo filters.

## 19. Subscriber verification and public authorization

### 19.1 Subscribe

```http
POST /api/public/career/subscribers
```

The service normalizes and validates the email, validates the frequency
(`Instant`, `DailyDigest`, `WeeklyDigest`, or `None`), and creates or safely reactivates
the subscriber. The response is deliberately generic, regardless of whether the
address already exists.

A cryptographically random verification token is emailed through the existing
priority email infrastructure. Only its SHA-256 hash is stored. The verification link
expires after 24 hours and alerts are not sent before verification.

### 19.2 Verify and session

```http
POST /api/public/career/subscribers/verify
```

Successful verification clears the verification token, marks the subscriber verified,
and returns an opaque 30-day career session token. Only the token hash and expiry are
stored. Subscriber-authorized requests send the raw value in:

```text
X-Career-Session: <opaque token>
```

An email address by itself never authorizes preferences, saves, follows, or alerts.

### 19.3 Preferences and unsubscribe

```http
POST /api/public/career/subscribers/resend-verification
POST /api/public/career/subscribers/unsubscribe
GET  /api/public/career/subscribers/preferences
PUT  /api/public/career/subscribers/preferences
```

Marketing/job-alert messages include a signed, expiring unsubscribe link. Signing uses
the configured server secret. Tampered tokens fail constant-time signature validation.
Unsubscribe is idempotent and clears the active public session.

## 20. Saved jobs and anonymous visitors

The browser creates a first-party opaque visitor token and stores it under
`career_portal_visitor`. The server stores only its SHA-256 hash.

```http
POST   /api/public/career/jobs/{publicJobId}/save
DELETE /api/public/career/jobs/{publicJobId}/save
GET    /api/public/career/jobs/saved
POST   /api/public/career/jobs/merge-anonymous-saves
```

Save flow:

1. Resolve `PublicJobId` to an eligible job.
2. Resolve either a verified career session or a valid anonymous visitor token.
3. Upsert the subscriber/job or visitor-hash/job relationship.
4. Store server-resolved `JobId`, `CompanyId`, and `PublicJobId`.
5. Repeated saves are idempotent.

### 20.1 Cross-page saved state

`SavedJobsProvider` is mounted around the public routes. It reads the server-owned
saved-job list once for the current visitor/session and keeps one in-memory set of
`PublicJobId` values. Every bookmark control reads and writes that shared set only
after the save/remove API succeeds. As a result, a bookmark changed in a listing,
job detail, or the Saved Jobs page updates every mounted career view immediately and
is restored after navigation or browser refresh from `GET /jobs/saved`.

The browser never treats the visual bookmark as the source of truth; anonymous saves
remain keyed by the first-party visitor token and verified-subscriber saves by the
career session, as described above.

Company logos use the shared `CompanyLogo` component. Uploaded brand marks are shown
inside a squared, padded tile with `object-fit: contain`, so a wide or circular logo is
not cropped into an avatar. If the image cannot load, the tile safely falls back to the
company initial.

When a visitor verifies an email, the merge endpoint copies active anonymous saves to
the subscriber using an idempotent upsert, then deactivates the anonymous rows.

`CareerVisitorPreference` stores only server-useful state such as popup dismissal,
`DoNotShowUntil`, display count, and last company visit. The current subscribe prompt
waits 12 seconds, can be explicitly declined, and checks the hashed server-side visitor
preference before opening. `Not now` suppresses it for 7 days, `Do not show again` for
180 days, and an existing verified career session suppresses it entirely.

## 21. Company follows and job alerts

```http
POST   /api/public/career/companies/{companyCode}/follow
DELETE /api/public/career/companies/{companyCode}/follow
GET    /api/public/career/companies/{companyCode}/follow-status

POST   /api/public/career/job-alerts
GET    /api/public/career/job-alerts
PUT    /api/public/career/job-alerts/{alertId}
DELETE /api/public/career/job-alerts/{alertId}
```

All endpoints require a verified career session. Company ownership is resolved from
the public code. `SubscriberId + CompanyId` is unique and follow/unfollow is
idempotent. Alert update/delete queries always include `SubscriberId`, preventing one
subscriber from modifying another subscriber's alert.

Each subscriber may maintain multiple alerts using search text, location, work and
employment types, job types, industries, departments, skills, experience, salary,
currency, and frequency.

## 22. Notification transition and delivery flow

Vacancy saves do not automatically send mail. `JobVacancyService` supplies the prior
and saved job to `CareerNotificationService`, which evaluates the exact publication
transition.

Company-page visibility requires:

```text
active/non-deleted company
CareerPortalEnabled = true
active/non-deleted job
PublishToCareerPortal = true
expiry and application deadline not passed
```

Master visibility additionally requires:

```text
Company.PublishJobsToMasterPortal = true
JobVacancy.PublishToMasterPortal = true
```

Only a transition from non-visible to visible creates `NewJob`. A job that remains
visible creates `JobUpdated` only when selected candidate-facing properties change.
Draft creation, internal edits, private/inactive/deleted/expired jobs, and repeated
visible saves do not create another new-job event.

Recipient rules:

- active company followers can receive company-page publication;
- general verified subscribers and matching alerts receive master publication;
- update notifications respect follower update preferences and matching alerts;
- the subscriber set is unioned before outbox insertion, so a follow and alert match
  do not intentionally send duplicate notifications.

Durable delivery:

1. Insert safe notification metadata and JSON payload into
   `CareerNotificationOutbox`.
2. Schedule `AvailableAt` immediately, for the next daily window, or for the next
   weekly window according to subscriber frequency.
3. The hosted worker atomically changes a due row from `Pending`/`Failed` to
   `Processing`.
4. A row stuck in `Processing` for ten minutes becomes claimable after restart/failure.
5. Send through `IMiddlewareService.EmailSendAndSaveWithResult`, preserving the
   existing email log.
6. For daily/weekly subscribers, claim all due rows for that subscriber and send one
   grouped digest; immediate subscribers continue to receive one-job messages.
7. Insert `CareerNotificationDelivery` for every included job/reason.
8. Mark every included outbox row `Sent`.
9. On failure, use exponential backoff. After five attempts, mark `DeadLetter`.

Once per hour the worker also scans active saved jobs approaching their application
deadline and queues a `SavedJobDeadline` reminder. The same outbox and delivery
uniqueness rules prevent repeat reminders.

The unique delivery key is `SubscriberId + JobId + NotificationReason`. The worker also
checks delivery history before sending, and duplicate-key races are handled as already
delivered.

### 22.1 Application notifications

Creating an internal application first persists the applicant, its audit log, and the
two-hour resume-upload token. It then queues non-blocking email work through
`IPriorityTaskQueue`; the HTTP response is not held open for SMTP delivery. The queued
work sends the candidate acknowledgement and sends the recruiter alert to the optional
vacancy recruiter-contact address plus active, verified Administrator/HR users in the
same company, de-duplicated by email. `IMiddlewareService.EmailSendAndSave` delivers
each message and writes the existing email-log record.

Later applicant-stage emails are separate from the initial acknowledgement:
`ApplicantServices` sends them only after a successful `ActivityType` change. A mail
delivery failure is logged and does not undo an already persisted application or stage
update.

## 23. Collections and indexes

New collections:

```text
PublicCompanyProfile
CareerSubscriber
JobAlertSubscription
CompanyFollower
SavedJob
CareerVisitorPreference
CareerNotificationOutbox
CareerNotificationDelivery
CareerAnalyticsEvent
```

Important indexes created by `AddCareerPortalModule`:

- unique subscriber normalized email;
- verification, unsubscribe, and session token lookup;
- unique profile company and public code;
- profile industry/publication;
- unique subscriber/company follow;
- subscriber and frequency alert lookup;
- partial unique subscriber/job and anonymous-hash/job saves;
- outbox status/availability and subscriber/status;
- unique notification delivery relation;
- analytics job/event/time;
- public job discovery fields.

The migration initializes new job fields only on documents missing the new schema
marker and does not rewrite public IDs, slugs, publication flags, applicants, or
application tokens.

## 24. Analytics

```http
POST /api/public/career/analytics
```

Only allow-listed event types are accepted:

```text
JobViewed, CompanyViewed, JobSaved, JobUnsaved, CompanyFollowed
CompanyUnfollowed, SearchPerformed, FilterApplied, ApplyClicked
ExternalApplyClicked, SubscriptionStarted, SubscriptionVerified
```

Visitor IDs are hashed. Analytics failure is non-blocking in the React client and
never blocks browsing or applying. Job-view counts feed the `most-viewed` sort.

Subscribe, resend, verify, save, and follow endpoints use the `career-sensitive`
fixed-window rate-limit policy (12 requests per remote address per minute, no queue).
Rejected requests return HTTP 429. Subscriber/profile/search inputs also have explicit
length/count limits in addition to normalization, sanitization, and escaped query
construction.

## 25. Validation status and remaining product boundaries

### 24 July 2026 saved-job and discovery correction

- `SavedJob._id` accepts MongoDB ObjectId values created by upsert, so existing
  anonymous saves can be deserialized and remain available after refresh.
- The marketplace retrieves saved IDs on load and restores each bookmark state.
- Public text search matches company names as well as job content.
- `GET /api/public/job-locations` supplies tenant/publication-scoped location
  suggestions for the searchable location autocomplete.
- Empty company-story content no longer renders a blank “team behind the roles”
  panel.
- The permission-protected Recruitment job add/edit form now lets the authorized
  HR/admin user maintain summary, location, work/employment type, experience,
  department, industry, functional area, role category, education, responsibilities,
  skills, benefits, keywords, shift, working days, recruiter contact, deadline,
  publication scope, featured status, and urgent-hiring status.
- Department options come from the current company's active Department master.
  Job Type is mapped to the canonical On-site/Hybrid/Remote public work mode.
  Deadline selection uses the built-in MUI calendar and clock with 15-minute steps,
  and publication toggles explain and enforce their dependencies.

Validated on 24 July 2026:

```powershell
dotnet build CodejiCMSCore.sln --no-restore
dotnet test Codeji.CMS.Services.Tests/Codeji.CMS.Services.Tests.csproj --no-restore
npm run build  # CMS-React
```

Results:

- backend solution: build passed;
- backend tests: 48 passed, 0 failed;
- frontend tests: 10 passed, 0 failed;
- frontend production build: passed;
- new security/logic coverage: signed unsubscribe round-trip, tamper rejection, rich
  HTML executable-content removal, publication eligibility, closed-job exclusion,
  candidate-facing update detection, alert matching, stable opaque visitor state,
  separate career-session state, and public search parameter construction.

Package audit warnings already present in the solution remain visible for AngleSharp,
SharpCompress, and Snappier; they are not suppressed by this feature.

Current recruitment boundaries still exclude interview scheduling, scorecards,
recruiter assignment, offer generation, candidate login/withdrawal, background
verification, onboarding conversion, and retention/anonymization automation.
