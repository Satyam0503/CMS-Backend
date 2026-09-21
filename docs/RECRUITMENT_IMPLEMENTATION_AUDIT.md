# Recruitment implementation audit and delivery plan

Audit date: 2026-09-18

Repositories audited: `C:\Users\User 2\CMS\CMS-Backend-Core`, `C:\Users\User 2\CMS\CMS-React`

## Scope and safety boundary

The repository checkouts contain unrelated dirty changes. No destructive reset, broad formatting, migration execution, data mutation, publish, or deployment was performed. Existing Recruitment diffs were treated as user-owned work and validated in place.

## Current implementation classification

| Area | Classification | Evidence / next increment |
|---|---|---|
| Jobs | KEEP + EXTEND | JobVacancy/controller/service and React Jobs UI exist. Add REC codes, optimistic version, unique/index review. |
| Candidates/applications | KEEP + EXTEND | Applicant/services/controller/forms exist. Separate candidate identity from application while preserving legacy DTOs/endpoints. |
| Timeline/audit | KEEP + EXTEND | ApplicantLogs, comments, status history, process-log UI exist. Add append-only before/after stage, actor, correlation key. |
| Pipeline | KEEP + REFACTOR SAFELY | ActivityType and transition checks exist. Add versioned definitions and immutable workflow snapshot. |
| Interviews | EXTEND | Additive interview aggregate, tenant-scoped scheduling, optimistic concurrency, idempotency, shared calendar sync, and retryable sync status are implemented. |
| Offers | EXTEND | Additive offer state machine, compensation validation, shared approval-reference requirement, audit event, notification reuse, optimistic concurrency, and idempotency are implemented. |
| Preboarding handoff | EXTEND | Idempotent tenant-scoped handoff reuses IEmployeeService.InviteNewEmployee and records the existing ApplicantLogs audit event. |
| Tenant isolation | EXTEND | Current dirty diff binds Recruitment mutations to authenticated CompanyId. Add two-tenant tests for every path. |
| Permissions | KEEP + EXTEND | ModulePermission protects Jobs/Applications/ProcessLog; add scope-aware recruiter rules through shared authorization. |
| Notifications/audit | KEEP | Reuse priority queue, mail templates, and base audit fields. |
| Concurrency/idempotency | EXTEND | Public upload token claim is atomic; add request key + expected version to critical commands. |
| REC_* errors | EXTEND | `ErrorCode` is now optional on shared results; stable Recruitment codes are wired into current validation/not-found paths. |
| Database migrations | EXTEND | Additive nullable fields/indexes and resumable, audited backfill only after design review. |
| Responsive UI | KEEP + EXTEND | Existing responsive jobs/applications/public portal; progressively replace modal duplication with drawers. |

## Existing Recruitment changes validated in the dirty checkout

- Job create/edit/delete/read paths use authenticated tenant context.
- Public/internal application input has stronger validation and active/expiry/deadline checks.
- Internal applicant creation records a first timeline event.
- Resume checks are case-insensitive, PDF-only, and bounded to 5 MB; generated filenames no longer use user input.
- Public one-use resume tokens are claimed with an atomic conditional update before file write.
- Frontend schemas validate integer vacancy counts, non-negative numeric ranges, range ordering, and case-insensitive PDF extensions.

These changes are not committed or published by this audit.

## Phase-wise sprint plan

### Phase 0 — Baseline and release guardrails (1 sprint)

Goal: freeze contracts and establish evidence. Scope: branch/dirty inventory, API/UI route map, permission matrix, tenant fixtures, baseline validation, error envelope proposal. Acceptance: two-company fixture plan, exact baseline counts, no unapproved endpoint changes. Rollback: source revert; no data mutation.

Risks: dirty worktree and unavailable isolated Mongo. Mitigation: source/read-only tests first; never use the configured production-like database.

### Phase 1 — Production core: jobs, candidates, applications (2 sprints)

Goal: reliable lifecycle while preserving legacy endpoints. Scope: tenant-safe commands, candidate/application DTO split, duplicate-active-application rule, idempotent create, REC codes, expected-version conflicts. Acceptance: duplicate submits converge; cross-tenant access fails; old endpoints remain compatible; invalid input returns stable codes. Migration: additive nullable PipelineDefinitionId, PipelineVersion, WorkflowSnapshot, Version, IdempotencyKey and indexes. Rollback: feature flag the new path; retain legacy projection/data.

### Phase 2 — Versioned pipeline and timeline (1–2 sprints)

Goal: stage history remains correct after configuration changes. Scope: company pipeline definitions/versions, application snapshot, transition matrix, append-only timeline, actor/scope checks. Acceptance: pipeline edits do not alter existing applications; invalid transition returns `REC_INVALID_TRANSITION`; stale updates return 409/`REC_CONCURRENCY_CONFLICT`; events include actor/time/before/after.

### Phase 3 — Interviews and offers (2 sprints)

Goal: recruiter execution using shared calendar/approval/notification infrastructure. Acceptance: timezone-safe scheduling, panel authorization, no confirmed double-booking, idempotent/auditable offer approval, withdrawal and acceptance.

### Phase 4 — Candidate-to-preboarding handoff (1 sprint)

Goal: eliminate re-entry after acceptance. Scope: idempotent handoff, candidate/application snapshot, existing employee/preboarding services, retry state. Acceptance: one accepted application creates one employee/preboarding case; retries return same result; tenant and permission checks apply; source data is preserved.

### Phase 5 — Configurable enterprise layers (2–3 sprints)

Scope: requisitions/headcount/approvals, scorecards/panels, duplicate detection, talent pools, SLA/stage aging, outbox notifications, role/scope analytics, multi-location, internal mobility, former employee/rehire links. Each feature must be independently enabled, permissioned, observable, and backward-compatible.

## Required test matrix

Backend: tenant isolation, permission/scope denial, invalid transitions, snapshot stability, optimistic concurrency, duplicate/idempotency, legacy compatibility, token race, candidate-to-preboarding retry.

Frontend: route permissions, loading/empty/error states, drawer containment, keyboard access, narrow/mobile tables, stable error-code rendering.

UAT: create job → publish → apply → upload resume → review → move stage → interview → offer → accept → preboarding; repeat with two tenants and revoked permission.

## Verification result and remaining TODO

Backend API and migration builds pass with 0 errors; services tests pass 261/261 and API tests pass 51/51. The earlier fixture compile blocker was repaired with test-only fixture data, without changing production behavior.

Remaining TODO: run Mongo-backed integration and browser UAT in an isolated environment; add later configurable enterprise layers (requisitions/headcount, scorecards, duplicate detection, talent pools, SLA aging, outbox automation, analytics, multi-location, internal mobility, and rehire links) as separate phases; commit/publish only after explicit release approval.
