# Recruitment modernization implementation TODO

Status legend: [x] implemented in current worktree, [~] in progress, [ ] pending validation or implementation.

## Sprint 0 — Security stabilization
- [x] Tenant/company ownership checks on recruitment commands.
- [x] Permission checks on recruitment controllers.
- [x] Idempotency and optimistic-concurrency protections for lifecycle commands.
- [x] Secure external action token hashing, expiry, revocation, and replay protection.
- [x] Resume PDF extension/signature/size validation.
- [ ] Application-bound resume token migration and legacy email-upload retirement.
- [ ] Isolated Mongo and authenticated browser security validation.

## Sprint 1 — Unified workflow foundation
- [x] Company recruitment modes and capability matrix persisted server-side.
- [x] Company settings UI for workflow, interview, offer, SLA, and enterprise capabilities.
- [x] Stage-aware lifecycle UI gating for scorecard, offer, and preboarding.
- [x] Add server-driven current-stage and allowed-actions contract.
- [x] Add durable job-application identity separate from candidate/applicant.
- [x] Add application stage history and tenant-scoped stage transition endpoint; configurable stage catalog remains.
- [x] Snapshot effective pipeline configuration when an application starts.

## Sprint 2 — Structured screening
- [x] Screening form and criterion template entry point per company/job (first criterion; template catalog remains).
- [x] Screening review record with reviewer, resume version, score, decision, reason, evidence, idempotency, and version checks.
- [x] Recruiter screening queue with configurable overdue threshold, SLA indicators, and durable rejection reasons.
- [x] Optional advisory resume extraction with human approval and audit trail.

## Sprint 3 — Multi-round interviews
- [x] Tenant-scoped interview plan and ordered stage definitions per default/job (API, effective-plan retrieval, and settings UI).
- [x] Multiple interview records per application and round metadata (round number/type; plan catalog remains).
- [~] Interviewer assignment validation is enforced when saving a scorecard; role-scoped visibility and quorum gates remain.
- [x] Per-interviewer scorecards now carry round number, recommendation, and optional interview assignment validation; question-kit catalog remains.
- [~] Server-side interview quorum, feedback completion, debrief decisions, and automatic durable application-stage synchronization are implemented; reminders remain.

## Sprint 4 — Hiring decision and offers
- [x] Hiring-decision entity and approval gate after required interview debrief feedback.
- [~] Tenant/vacancy offer approval policy now supports legal entity, department, and grade dimensions plus currency threshold and approval-reference enforcement; runtime matching now resolves exact vacancy, then vacancy legal-entity/department/grade dimensions, then company default.
- [x] Offer versioning, negotiation, audit, and candidate response flow.
- [x] Prevent offer send before required approval (server transition rules allow Sent only from Approved; covered by recruitment lifecycle rule tests).

## Sprint 5 — Preboarding and onboarding integration
- [x] Offer acceptance creates a durable preboarding case/checklist and emits an idempotent `OfferAccepted` automation event.
- [x] Preboarding case, starter checklist, status, and ownership fields added; document/verification UI remains.
- [x] Durable OfferAccepted event and preboarding case are created before employee handoff; existing handoff advances the case to ReadyForOnboarding.
- [x] Durable onboarding case/checklist is created at employee handoff with PendingActivation status and can be read/updated through the lifecycle UI with guarded task completion.
- [ ] Payroll, attendance, leave, and access activation integration points.

## Sprint 6 — Enterprise configuration
- [ ] Business-unit, department, legal-entity, country, and job overrides.
- [ ] Multi-currency and localized templates.
- [~] Delegated approvals enforce configured approver role; agency/vendor access remains.
- [ ] SSO/SCIM and enterprise permission review.

## Sprint 7 — Reporting, privacy, and operations
- [~] Feedback SLA report/API and recruiter card now show open/overdue interview rounds and queue idempotent reminder events; full source/recruiter/hiring-manager dashboards remain.
- [x] Candidate consent, retention, revocation, and auditable anonymization workflow.
- [~] Tenant-safe recruitment audit CSV export with Process Log download and row/date bounds; operational alerting remains.
- [ ] AI/automation governance and adverse-impact monitoring controls.

## Validation and release gates
- [ ] Backend build and focused service/API tests after each sprint.
- [ ] Frontend typecheck, Vitest, lint, and production build after each UI slice.
- [ ] Authenticated two-tenant browser UAT at desktop/tablet/mobile widths.
- [ ] Isolated Mongo integration tests for tenant, transition, concurrency, and idempotency behavior.
- [ ] Update this TODO with exact evidence before release.

## Latest execution evidence (2026-09-21)
- Server workflow query: GET api/Applicants/Workflow/{id} with tenant and Applications.View protection.
- Frontend settings and lifecycle UI consume the capability/stage contract.
- Backend API build after screening/round metadata: passed, 0 errors (existing nullable warnings only).
- Focused backend API recruitment tests after screening/round metadata: 18 passed, 0 failed.
- Focused backend service recruitment tests after screening implementation: 28 passed, 0 failed.
- Focused backend recruitment tests: 28 passed, 0 failed.
- Frontend typecheck: passed.
- Frontend Vitest after screening/round UI: 20 files / 40 tests passed.
- Frontend production build after screening/round UI: passed; existing SignalR Rollup annotation warnings only.
- Frontend Vitest: 20 files / 40 tests passed.
- Frontend production build: passed; existing SignalR Rollup annotation warnings only.
- Mongo-backed integration and authenticated browser UAT remain pending.


- Sprint 2/3 implementation slice: screening API/UI and interview round metadata compiled successfully; frontend typecheck passed. 



- Durable application slice: RecruitmentJobApplication persistence, stage history, idempotency/concurrency, and React screening-stage handoff compiled; service tests 29/29 and frontend tests 40/40 passed.


- Scorecard increment: backend build passed; focused API recruitment tests 18/18, service recruitment tests 28/28; frontend typecheck, Vitest 20/40, and production build passed.



- Offer integration increment: exact API build passed with 0 errors after releasing Codeji.CMS.API PID 5604; recruitment service tests 29/29 passed.


- Preboarding increment: API build passed with 0 errors; recruitment service tests 29/29 passed.


- Interview debrief increment: API build 0 errors; service tests 31/31; API tests 18/18; frontend typecheck, Vitest 20/40, and production build passed.


- Debrief synchronization increment: API build 0 errors and recruitment service tests 31/31 passed.


- Interview-plan increment: API build 0 errors; service tests 31/31; frontend typecheck, Vitest 20/40, and production build passed.



- Effective-plan increment: API build 0 errors; service tests 31/31; frontend typecheck, Vitest 20/40, and production build passed.


- Feedback-SLA increment: API build 0 errors; service tests 31/31; API tests 18/18; frontend typecheck, Vitest 20/40, and production build passed.


- Reminder automation increment: API build 0 errors; service tests 31/31; API tests 18/18; frontend typecheck, Vitest 20/40, and production build passed.


- Offer-policy increment: API build 0 errors; service tests 31/31; API tests 18/18; frontend typecheck, Vitest 20/40, and production build passed.


- Approval-dimension increment: API build 0 errors; service tests 31/31; frontend typecheck passed.


- Approval resolution increment: vacancy organizational fields and runtime precedence added; API build 0 errors and service tests 31/31 passed.


- Onboarding bridge increment: API build 0 errors; recruitment service tests 31/31 passed.


- Onboarding operations increment: added tenant-scoped GET-by-applicant and idempotent/versioned task-status update API, registered service, and typed React client methods; backend build passed and recruitment service tests 31/31, frontend typecheck passed. The lifecycle drawer editor was completed in the following UI increment.

- Screening queue increment: added tenant-scoped queue/report API and applications-page SLA card; backend build passed, service tests 33/33, API tests 18/18, frontend typecheck, Vitest 20/40, and production build passed.

- Candidate offer-response increment: added anonymous /candidate/offer?token=... React page for accept/decline/change requests, wired to the existing one-time token backend endpoint; frontend typecheck and Vitest 20/40 passed.

- Offer negotiation UI increment: wired existing version/counter APIs into the lifecycle drawer with typed commands, validation, and tenant-authorized calls; frontend typecheck and Vitest 20/40 passed.

- Candidate privacy increment: added consent grant/revoke, retention date, reason-gated anonymization, PII/resume scrubbing, audit event, API, and lifecycle UI mode; backend build passed, service tests 35/35, API tests 18/18, frontend typecheck and Vitest 20/40 passed.

- Hiring-decision increment: added durable approve/hold/reject entity, server-side debrief gate, applicant-stage synchronization, lifecycle UI mode, and focused test; API build passed, service tests 34/34, API tests 18/18, frontend typecheck and Vitest 20/40 passed. Production build reached transformation but hit the machine's Node/esbuild out-of-memory condition on retry.

- Onboarding UI increment: added an Onboarding mode to the applicant lifecycle drawer with case/task status editing, guarded by preboarding handoff state and the tenant-safe onboarding API. Backend API tests 18/18, service tests 32/32, frontend typecheck passed, Vitest 20/40, and production build passed.









- Pipeline snapshot increment: application creation now reads the current tenant recruitment config version (with legacy fallback), preserves the stage snapshot immutably, and has focused coverage; backend build passed and recruitment service tests 36/36.


- Resume extraction increment: added tenant-scoped proposal/review entity, pending/approved/rejected API, audit event, lifecycle UI editor, and focused test; backend build passed, service tests 37/37, API tests 18/18, frontend typecheck and Vitest 20/40 passed.


- Delegated approval increment: offer approval now enforces the configured approver role claim server-side and returns 403 before mutation; backend build passed and recruitment service tests 38/38.


- Audit export increment (2026-09-21): RecruitmentAuditExportService filters by current tenant, applicant/event/date, caps exports at 10,000 rows, escapes CSV values, and is protected by ProcessLog.View. Backend API build: 0 errors (25 existing warnings); focused export tests: 2 passed; complete services regression: 288 passed, 0 failed; frontend typecheck passed; Vitest 20 files / 40 tests passed.



- Frontend lint audit: command completed with 181 existing findings (172 warnings, 9 errors), all errors in unrelated EmployeeLifecyclePolicyConfig.tsx no-unexpected-multiline lines 54,63,65,73,88,100,105,107,109; no export-file lint error.

