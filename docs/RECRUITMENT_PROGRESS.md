# Recruitment implementation progress

Last updated: 2026-09-18

Loop: inspect -> implement smallest safe slice -> build -> test -> fix -> repeat

## Definition of done

- [x] Jobs, candidates, applications, snapshot-based pipeline, timeline, interviews, offers, and candidate-to-preboarding handoff are implemented in the backend core.
- [x] Tenant isolation and existing module permissions remain enforced by the existing scoped services/controllers.
- [x] Legacy endpoints/data remain backward-compatible through additive fields/endpoints and non-destructive migration indexes.
- [x] Critical commands have stable REC_* errors, concurrency protection, and idempotency.
- [x] Backend production build, frontend typecheck/tests/build, and focused recruitment tests pass.
- [x] Full backend and frontend suite blockers are resolved; remaining runtime UAT is explicitly environment-scoped.

## Live TODO

- [x] Audit current backend/frontend/database/API/permission/configuration flow.
- [x] Create phase-wise sprint plan and audit document.
- [x] Add backward-compatible REC_* error-code contract.
- [x] Add additive applicant workflow snapshot metadata.
- [x] Add compatibility pipeline snapshot and reusable transition validation.
- [x] Internal stage movement accepts ExpectedVersion and returns REC_CONCURRENCY_CONFLICT on stale writes.
- [x] Add optional idempotency key handling for internal applicant update commands.
- [x] Additive timeline before/after stage metadata is recorded and returned by existing log APIs.
- [x] Add additive interview and offer lifecycle entities plus pure transition/validation rules.
- [x] Add tenant-scoped interview and offer upsert commands/endpoints with optimistic concurrency, idempotency, and REC_* errors.
- [x] Add tenant-scoped candidate-to-preboarding handoff endpoint using existing Applications.Edit permission and IEmployeeService.InviteNewEmployee.
- [x] Connect interview scheduling to the existing shared calendar service through a recruitment-owned command/service (calendar sync status is retryable).
- [x] Require a shared approval reference before an offer can transition to Approved.
- [x] Connect offer lifecycle updates to the existing ApplicantLogs audit stream and shared notification service (delivery is non-critical).
- [x] Add durable unique handoff/interview/offer idempotency indexes and duplicate-race recovery.
- [x] Add handoff timeline/audit event through the existing ApplicantLogs stream.
- [x] Add and execute focused recruitment lifecycle rule tests (5/5 passed).
- [x] Add tenant-scoped service regression coverage for permission context, cross-tenant lookup, idempotency replay, and stale-version conflicts.
- [x] Add tenant-safe duplicate-candidate detection by normalized email, phone, and LinkedIn with Applications.View protection and typed frontend client.
- [ ] Run Mongo-backed integration tests for the full tenant/permission/concurrency/idempotency/snapshot/interview/offer/handoff/requisition path in an isolated environment.
- [x] Add typed frontend lifecycle API clients for interview, offer, handoff, requisition, and scorecard commands.
- [x] Add minimal responsive frontend lifecycle drawers for interview, offer, candidate-to-preboarding, job headcount requisition, scorecard and talent-pool commands with permission gates and REC_* display.
- [x] Add tenant-scoped talent-pool membership with tag normalization, lifecycle status, idempotency, optimistic concurrency, audit timeline, and durable indexes.
- [x] Run backend/frontend build, test, and typecheck validation; browser/Mongo UAT remains pending an isolated safe environment.
- [x] Update this report after every loop and record exact verification counts.

## Current loop

Status: IN PROGRESS — production core plus requisition/headcount and talent-pool slices with responsive UI are implemented and green; scorecard and talent-pool UI slices are implemented and green; duplicate detection is implemented; remaining enterprise layers and runtime/Mongo/browser UAT are in progress.

Last verification (2026-09-18):

- Backend isolated API build: PASS, 0 errors. Migration build: PASS, 0 errors, 19 existing warnings in the direct build.
- Frontend typecheck: PASS (`tsc --noEmit`). Full frontend tests: PASS, 20 files / 40 tests. Production build: PASS (`tsc && vite build`, 14,372 modules transformed) after requisition, scorecard, and talent-pool UI refinement.
- Focused frontend career regression: PASS, 1 file and 3 tests.
- Focused recruitment lifecycle tests: PASS, rules 5/5 and service 15/15. API permission contract: PASS, 6/6 recruitment lifecycle/planning endpoints plus duplicate detection.
- Backend services tests: PASS, 270/270.
- Backend API tests: PASS, 54/54.
- Full backend solution test gate: PASS (270 services + 54 API tests).
- Runtime Mongo/browser UAT: NOT RUN against the existing application database; requires an isolated safe test environment.

## Changed files in the latest loop

- `Codeji.CMS.Repository/Entities/Recruitments/RecruitmentRequisition.cs`
- `Codeji.CMS.Repository/Entities/Recruitments/RecruitmentScorecard.cs`
- `Codeji.CMS.Repository/Entities/Recruitments/RecruitmentTalentPoolMember.cs`
- `Codeji.CMS.Repository/Entities/Recruitments/RecruitmentInterview.cs`
- `Codeji.CMS.Repository/Entities/Recruitments/RecruitmentOffer.cs`
- `Codeji.CMS.Repository/Entities/Recruitments/RecruitmentHandoff.cs`
- `Codeji.CMS.Services/Recruitments/RecruitmentInterviewRules.cs`
- `Codeji.CMS.Services/Recruitments/RecruitmentOfferRules.cs`
- `Codeji.CMS.Services/Recruitments/RecruitmentHandoffService.cs` (includes timeline/audit event)
- `Codeji.CMS.Services/Recruitments/Interface/IRecruitmentHandoffService.cs`
- `Codeji.CMS.Services/Registration/ServicesRegistration.cs`
- `Codeji.CMS.DTO/Recruitments/CandidateHandoffRequest.cs`
- `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- `Codeji.CMS.DTO/Recruitments/RecruitmentRequisitionCommands.cs`
- `Codeji.CMS.DTO/Recruitments/RecruitmentScorecardCommand.cs`
- `Codeji.CMS.DTO/Recruitments/RecruitmentTalentPoolCommand.cs`
- `Codeji.CMS.DTO/Recruitments/RecruitmentLifecycleCommands.cs`
- `Codeji.CMS.Services/Recruitments/Interface/IRecruitmentLifecycleServices.cs`
- `Codeji.CMS.Services/Recruitments/RecruitmentRequisitionService.cs`
- `Codeji.CMS.Services/Recruitments/RecruitmentScorecardService.cs`
- `Codeji.CMS.Services/Recruitments/RecruitmentTalentPoolService.cs`
- `Codeji.CMS.Services/Recruitments/Interface/IRecruitmentTalentPoolService.cs`
- `Codeji.CMS.Services/Recruitments/RecruitmentInterviewService.cs`
- `Codeji.CMS.Services/Recruitments/RecruitmentOfferService.cs`
- `Codeji.CMS.Migrations/Migrations/AddRecruitmentCoreIndexes.cs`
- `Codeji.CMS.Services/Calendar/CalendarServices.cs`
- `Codeji.CMS.DTO/Calendar/CalendarRequestDto.cs`
- `Codeji.CMS.DTO/Calendar/CalendarResponseDto.cs`
- `Codeji.CMS.Repository/Entities/Calendar/CalendarEntity.cs`
- `Codeji.CMS.Repository/Domain/Result.cs`
- `Codeji.CMS.Services.Tests/RecruitmentLifecycleRulesTests.cs`
- `Codeji.CMS.Services.Tests/RecruitmentLifecycleServiceTests.cs` (tenant, idempotency, concurrency, handoff, calendar, and requisition coverage)
- `Codeji.CMS.API.Tests/RecruitmentControllerContractTests.cs` (authentication, Applications.Edit, and Jobs.Edit contracts)
- `Codeji.CMS.API/Controllers/RecruitmentPlanningController.cs`
- `Codeji.CMS.Services.Tests/RecruitmentLifecycleServiceTests.cs` (scorecard validation and tenant coverage)
- `CMS-React/src/app/modules/recruitment/jobs/components/RecruitmentRequisitionDrawer.tsx`
- `CMS-React/src/app/modules/recruitment/applications/components/RecruitmentLifecycleDrawer.tsx` (scorecard mode)
- `Codeji.CMS.Services.Tests/AttendanceReminderEvaluatorTests.cs` (required fixture data only)
- `Codeji.CMS.Services.Tests/EmployeeLifecycleTransitionTests.cs` (required fixture data only)
- `Codeji.CMS.Services.Tests/UnitTest1.cs` (required fixture data/mock only)
- `CMS-React/src/app/modules/employees/employeeExitPolicyServices.test.ts` (Vitest hoisted mock)
- `CMS-React/src/utils/constant/data.ts`
- `CMS-React/src/app/modules/recruitment/models.ts`
- `CMS-React/src/app/modules/recruitment/applications/components/RecruitmentLifecycleDrawer.tsx`
- `CMS-React/src/app/modules/recruitment/applications/components/ApplicantDetails.tsx`
- `CMS-React/src/app/modules/recruitment/services.ts`

## Safety and blockers

No destructive source rewrite, migration, commit, push, or deployment was performed. Existing tenant filters, Applications.Edit permission, and employee invitation validation are reused. The full-suite fixture issue has been resolved and covered by the green service/API gates. The remaining verification gap is the lack of an isolated runtime database/browser environment; Mongo-backed runtime integration remains explicitly pending and is not silently treated as complete.




















## Enterprise completion slice (2026-09-21)

Implemented additive enterprise recruitment capabilities:

- Permission-protected applicant soft-merge workflow. Source records are retained as soft-deleted records with `MergedIntoApplicantId`, `MergeStatus`, and ApplicantLogs audit history.
- SLA policy storage and upsert endpoint keyed by stage/location.
- Recruitment enterprise configuration for locations, timezone, SLA, and automation flags.
- Role/permission-protected recruitment analytics endpoint with optional vacancy scope.
- Automation outbox event queue with idempotency key payload storage.
- Internal mobility and rehire relationship links.
- Frontend typed clients and a merge action in the lifecycle drawer.

Verification: backend API contract suite 61/61; backend services suite 270/270; frontend typecheck passed; frontend tests 20 files/40 tests passed; frontend production build passed with 14,372 modules.

Remaining release gates: run these endpoints against an isolated Mongo database and execute authenticated browser UAT across desktop/tablet/mobile. The current environment does not provide a safe running database/browser session for that final verification.

## Tenant defaults and company overrides (2026-09-21)

Implemented production-safe default resolution for recruitment enterprise settings:

- During company registration, `RecruitmentEnterpriseConfig` is created with the new company ID, `ConfigSource=SystemDefault`, version `1`, UTC timezone, empty locations, SLA enabled, and automation enabled. Seeding is idempotent, so retrying registration/setup cannot create a second active config.
- Every read, update, and reset resolves the company from the authenticated `CurrentContext`; a client-supplied company ID is not trusted. `Jobs.View` protects reads and `Jobs.Edit` protects writes.
- Existing companies without a config receive the same immutable system-default response until an authorized company update creates their tenant-owned configuration.
- Company updates are stored only in that company’s document and are marked `ConfigSource=CompanyOverride` with `OverriddenFields` metadata. Values are normalized (trimmed, de-duplicated locations and UTC fallback timezone).
- Resetting with `ResetToDefaults=true` restores the system defaults and clears override metadata without touching jobs, applications, workflow snapshots, audit history, or other tenants.
- Historical application workflow snapshots remain unchanged when a company changes its current recruitment settings.

Verification after this slice:

- Backend API build: passed, 0 errors (104 pre-existing warnings).
- Recruitment service tests: 22/22 passed.
- Recruitment API contract tests: 13/13 passed.
- Frontend typecheck: passed.
- Frontend tests: 20 files / 40 tests passed.
- Frontend production build: passed (`RecruitmentRoutes` emitted successfully).

Remaining release gate: authenticated Mongo-backed runtime and browser UAT in an isolated environment; source-level gates above are green.


## Validation update (2026-09-21)

- Backend solution tests: 331/331 passed (270 services + 61 API contract tests).
- Frontend unit/regression: 20 files and 40 tests passed.
- Frontend production build: passed.
- Browser smoke integration: 15 authentication smoke tests passed; 33 authenticated module tests were blocked by missing `QA_EMAIL`/`QA_PASSWORD` fixture credentials. These are environment/setup failures, not product assertion failures. No recruitment-specific browser test exists in the current smoke catalog.
- Mongo-backed integration remains pending until an isolated running API/database and authorized test account are available.

## Sprint execution update (2026-09-21)

### Completed in this loop

- Sprint 11 secure external action links: cryptographically random one-time tokens, SHA-256-at-rest hashes, expiry/revocation/consumption, issuance-key replay protection, tenant ownership, and recruitment email outbox with deduplication/retry worker.
- Sprint 12 interviewer panel validation: optional active interviewer IDs are normalized, deduplicated, limited to 12, and checked against active users in the same company. The responsive lifecycle drawer now exposes the shared interviewer field.
- Sprint 14 offer versioning: immutable offer versions, idempotent version creation, currency/status normalization, and approval-reference/start-date/expiry capture.
- Sprint 15 candidate negotiation: tenant-scoped counter requests, idempotent counter submission and decision keys, approved counter revision creation, conflict-safe status updates, and explicit invalid-request/concurrency errors.
- Candidate email-link actions: anonymous offer accept/decline and interview confirm/reschedule endpoints validate action type, applicant/offer/interview ownership, token expiry and one-time consumption, expected resource version, and queue a deduplicated confirmation email.
- Sprint 16 company-size flexibility: tenant-owned `Simple`, `Standard`, and `Enterprise` recruitment modes with server-side validation; missing legacy configuration still resolves to safe Simple defaults. Mode changes are configuration-only and do not weaken authorization or tenant filters.

### Verification loop

- Backend API production build: passed, 0 errors (warnings are existing nullable/legacy warnings).
- Services tests: 273/273 passed.
- API contract tests: 66/66 passed.
- Frontend typecheck: passed.
- Frontend Vitest regression: 20 files / 40 tests passed.
- Frontend production build: passed.

### Remaining release gates (not source defects)

- Run authenticated endpoints against an isolated Mongo database and real email provider sandbox.
- Verify calendar provider booking/reschedule/cancel and outbox delivery/retry with captured messages.
- Execute two-tenant authenticated browser UAT at desktop/tablet/mobile widths, including candidate email links.
- Add production observability dashboards/alerts and rehearse rollback of optional modes.

No destructive rewrite, commit, push, or deployment was performed. Existing unrelated dirty files were preserved.


## HRMS roadmap vertical slice — capability matrix (2026-09-21)

Added an additive company capability matrix to the existing recruitment configuration. The backend persists explicit capability flags while preserving Simple/Standard/Enterprise preset defaults and tenant-scoped read/write permissions. The frontend config model and save payload now carry the same flags without changing existing routes or historical application snapshots.

Capabilities covered: manual application review, hiring-manager review, interview rounds, parallel panels, scorecards, offer approval, offer negotiation, candidate email actions, requisitions, SLA tracking, automation, internal mobility, and rehire links.

Validation: services build passed with 0 errors; focused recruitment tests 28/28 passed; frontend typecheck passed. Full UI toggle rendering and hierarchical country/location/department/job overrides remain follow-up slices.
