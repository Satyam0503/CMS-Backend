# Recruitment Module Sprint Plan

## Sprint objective

Deliver a production-ready recruitment flow covering job creation and publication, public applications, secure resume upload, recruiter processing, audit history, career-portal engagement, and the validation/security gaps identified in `RECRUITMENT_MODULE_COMPLETE_DOCUMENTATION.md`.

**Suggested duration:** 10 working days  
**Team:** 1 backend developer, 1 frontend developer, 1 QA engineer, 1 product/HR reviewer  
**Priority:** P0 = release blocker, P1 = required for sprint acceptance, P2 = follow-up hardening

## Definition of done

- Backend and frontend contracts are aligned and tenant boundaries remain enforced.
- All P0/P1 acceptance tests pass in a clean environment.
- No raw application, session, verification, unsubscribe, or upload token is logged.
- Public and internal flows are verified in the browser, not only by source inspection.
- API, UI, validation, permissions, and known limitations are documented.
- Build, typecheck, lint, targeted tests, and recruitment UAT are green.

## Work breakdown

| ID | Story | Priority | Owner | Estimate |
|---|---|---:|---|---:|
| R-01 | Confirm baseline, contracts, and test fixtures | P0 | QA/BE | 0.5 day |
| R-02 | Harden job vacancy validation and publication rules | P0 | BE | 1.5 days |
| R-03 | Align job DTO, admin UI, and public fields | P1 | FE/BE | 1.5 days |
| R-04 | Harden public application and re-application flow | P0 | BE | 1 day |
| R-05 | Secure and standardize resume upload | P0 | BE/FE | 1 day |
| R-06 | Complete recruiter pipeline, comments, and audit history | P0 | BE/FE | 1 day |
| R-07 | Validate career profile, subscriptions, saves, follows, and alerts | P1 | BE/FE | 1 day |
| R-08 | Responsive UI, permission states, and accessibility pass | P1 | FE | 1 day |
| R-09 | End-to-end regression, security, and UAT | P0 | QA/Product | 1.5 days |

## Detailed stories and acceptance criteria

### R-01 — Baseline and fixtures

- Record current branch, dirty files, API environment, database, storage path, queue/mail configuration, and feature flags.
- Create isolated test data for two companies, two jobs, internal/external modes, applicants, recruiters, and permission variants.
- Verify the documented source map still matches the checked-out backend and frontend.
- Produce a baseline result for build, typecheck, lint, targeted recruitment tests, and browser smoke tests.

**Acceptance:** baseline results include exact pass/fail/skip counts and classify setup, harness, and product failures separately.

### R-02 — Job vacancy validation and publication

- Enforce required title, description, job type, status, and vacancy count on the server.
- Validate non-negative numeric values and ordering for experience, salary, publication, expiry, deadline, and walk-in dates.
- Normalize application mode to `Internal` or `External`.
- Accept external URLs only when absolute HTTPS and non-loopback; reject or clear invalid values consistently.
- Preserve stable public IDs and generate collision-safe slugs.
- Enforce company and job eligibility for career and master portals.

**Acceptance:** JOB-01 through JOB-15 pass; an unauthorized user cannot create, edit, delete, or publish a job.

### R-03 — Contract and UI alignment

- Align `JobVacancyModel`, response DTOs, frontend models, and `AddEditJob.tsx`.
- Decide explicitly whether salary, currency, application mode, walk-in fields, workplace type, notice period, and public flags are editable.
- Support clearing list fields intentionally rather than silently retaining old values.
- Display server validation errors beside the relevant fields.

**Acceptance:** every field documented as editable has a create/edit/display path, and omitted fields are explicitly marked entity-only.

### R-04 — Public application and re-application

- Validate email, phone, names, experience, notice period, and profile URLs on the server.
- Resolve the job by public ID/slug and re-check active, published, company, expiry, deadline, and application-mode rules at write time.
- Keep the six-month same-company re-application rule and define the exact response for waiting, re-apply, and external jobs.
- Create the applicant, initial log, reference, and notification queue item atomically enough to avoid partial records.

**Acceptance:** APPLY-01 through APPLY-15 pass, including cross-company isolation and malformed input cases.

### R-05 — Resume upload

- Use the public application reference plus hashed, expiring, single-use token as the preferred flow.
- Validate extension case-insensitively, exact byte size, PDF signature, token binding, expiry, and one-time consumption.
- Prevent path traversal and ensure stored filenames are generated server-side.
- Decide whether the legacy email-based endpoint is deprecated, restricted, or hardened; document active callers before changing it.
- Preserve safe resume preview/download authorization.

**Acceptance:** RES-01 through RES-15 pass, including reused, expired, cross-applicant, invalid-content, and parallel-upload tests.

### R-06 — Recruiter pipeline and audit history

- Keep server-side `ActivityType` transition rules authoritative.
- Verify terminal-state behavior for `Selected` and `Rejected`, allowed forward transitions, stale writes, and `ReApply` handling.
- Ensure comments and status changes create tenant-scoped process logs with actor, timestamp, applicant, vacancy, category, and previous/new state where applicable.
- Queue status notifications without exposing queue internals to the browser.
- Align frontend enum options with backend behavior.

**Acceptance:** APP-01 through APP-15 pass; two recruiter sessions cannot silently overwrite a newer status.

### R-07 — Career portal engagement

- Validate and sanitize company profile text, rich content, URLs, social links, themes, and list bounds.
- Verify subscriber creation, verification expiry, resend, unsubscribe, session expiry, preferences, alerts, saves, follows, and anonymous-save merge.
- Ensure saved jobs, alerts, and analytics are tenant/job consistent and use allowlisted event types.
- Verify company branding is backend/company-owned and cannot be overridden by browser storage.

**Acceptance:** PROFILE-01 through PROFILE-15 and SUB-01 through SUB-15 pass.

### R-08 — UI, permission, and accessibility pass

- Verify all recruitment routes, loading states, empty states, server errors, expired-job states, and permission-denied states.
- Confirm frontend guards only improve UX; backend permissions remain enforced.
- Test Jobs, Applications, Applicant Details, Process Log, Career Profile, public job detail, Apply Now, and resume upload at desktop and narrow widths.
- Verify keyboard navigation, labels, focus handling, modal containment, table responsiveness, and accessible upload errors.

**Acceptance:** no blocked workflow requires a hidden control, and all critical actions are usable by keyboard and screen-reader labels.

### R-09 — Regression and release gate

- Run backend build, targeted unit/integration tests, frontend typecheck, lint, build, and browser E2E.
- Execute the documented HR job setup, candidate application, recruiter processing, and public engagement UAT scripts.
- Test rate limiting behind the intended proxy configuration, queue/mail observability, resume storage/backup expectations, slug concurrency, and token concurrency.
- Record exact counts, failures, environment blockers, and unresolved P2 items.

**Acceptance:** product owner signs off the UAT matrix; no P0 remains open; P1 exceptions have explicit owner and due date.

## Suggested sequence

```text
R-01 baseline
   ├── R-02 job rules ── R-03 contract/UI alignment
   ├── R-04 public application ── R-05 resume security
   └── R-06 recruiter workflow
          └── R-07 career engagement
R-03 + R-05 + R-06 ── R-08 UI/accessibility ── R-09 release gate
```

## Sprint risks and decisions

| Risk/decision | Required outcome |
|---|---|
| Legacy email resume endpoint is weaker | Identify callers, then deprecate or apply the same binding and file checks |
| Entity has more fields than current UI | Product decides editable vs entity-only before R-03 closes |
| Background email is asynchronous | Queue failure/ retry monitoring is part of release readiness |
| Filesystem resume storage | Confirm backup, retention, malware scanning, and authorized serving policy |
| Public rate limiting behind proxy | Verify real client-IP behavior in deployed topology |
| Status transition semantics | Product confirms allowed transitions, especially `ReApply`, `Selected`, and `Rejected` |

## Sprint deliverables

1. Hardened backend APIs and validations.
2. Aligned internal recruitment UI and public career application flow.
3. Automated recruitment validation/security regression coverage.
4. Browser-verified UAT report with exact pass/fail/skip counts.
5. Updated `RECRUITMENT_MODULE_COMPLETE_DOCUMENTATION.md` showing implemented changes and remaining limitations.
6. This sprint plan with completed story status and evidence links.

## Out of scope unless separately approved

- Replacing the recruitment data model or storage provider.
- Introducing a new ATS workflow beyond the documented status pipeline.
- Changing tenant/permission architecture globally.
- Rewriting unrelated employee, payroll, leave, attendance, or onboarding modules.
- Publishing or deploying without a separate release approval.

## Live implementation status (2026-09-18)

- R-01/R-02/R-04/R-05/R-06: source hardening and compatibility slices are implemented in the current dirty worktree; see `RECRUITMENT_IMPLEMENTATION_AUDIT.md` and `RECRUITMENT_PROGRESS.md` for file-level evidence.
- Lifecycle extension: interview and offer entities, lifecycle rules, tenant-scoped upsert services, optimistic version checks, idempotency keys, REC_* errors, and `Applications.Edit`-protected endpoints are now implemented additively.
- Candidate handoff: `HandoffToPreboarding` reuses `IEmployeeService.InviteNewEmployee` and records a tenant-scoped idempotency handoff record.
- Remaining release work: isolated Mongo-backed integration/browser UAT and later enterprise-layer phases; source-level service/API gates are green and the earlier full-suite fixture blocker is repaired.
- Follow-up implementation completed: scheduled/confirmed interviews call the existing shared calendar service with retryable sync status; offer approval requires a shared approval reference and uses shared audit/notification infrastructure; handoff idempotency has a migration-backed unique index and duplicate-race recovery; typed frontend lifecycle clients and a responsive permission-gated lifecycle drawer are available.

## Sprint 8 — Talent pools (implemented)

Goal: retain viable candidates for future openings without bypassing tenant or application permissions.

Scope delivered: tenant-scoped pool membership, normalized tags (maximum 20), notes, Active/Archived status, idempotent upsert, optimistic version checks, ApplicantLogs timeline entries, Applications.Edit protection, and migration-safe unique/lookup indexes. The existing lifecycle drawer now exposes a responsive Talent pool action.

Acceptance: invalid status/name returns `REC_TALENT_POOL_INVALID`; missing or cross-tenant applicant is rejected; duplicate command replays safely; stale version returns `REC_CONCURRENCY_CONFLICT`; audit event is written through the shared ApplicantLogs stream; frontend typecheck, 20-file/40-test Vitest suite, and production build pass.

Dependencies: existing applicant repository, CurrentContext tenant resolution, shared permission attribute, ApplicantLogs, and generic Mongo repository.

Risks/rollback: additive collection and indexes only; rollback is endpoint/UI removal while retaining historical records. Runtime Mongo and browser UAT still require an isolated environment.

## Sprint 9 — Enterprise completion slice (implemented)

Scope: additive soft-merge, SLA policy storage, enterprise configuration, scoped analytics, automation outbox queue, internal-mobility/rehire relationship links, and lifecycle-drawer merge action.

Acceptance: all enterprise endpoints use existing authorization attributes and tenant context; merge is optimistic-concurrency protected and preserves source history; outbox and relationship records are additive; API contract and service suites remain green.

Rollback: disable new routes/UI actions; retain records for audit and recovery. Runtime Mongo/browser UAT remains a release gate.

## Sprint 10 — Tenant defaults and company overrides (implemented)

Goal: make recruitment configuration safe and useful from first company registration while allowing each tenant to control its own behavior.

Scope delivered: idempotent registration seeding, tenant-safe config read/update/reset, system-default fallback for legacy companies, `ConfigSource` and `OverriddenFields` metadata, normalized location/timezone values, and Jobs View/Edit permission contracts.

Dependencies: existing company registration flow, `CurrentContext` tenant resolution, generic Mongo repository, existing module permissions, and the additive `RecruitmentEnterpriseConfig` entity.

Acceptance criteria: a new company receives exactly one system-default config; a company can update only its own config; cross-tenant context cannot read or mutate another company; reset returns to defaults; legacy companies resolve defaults without a migration; application snapshots and unrelated recruitment data remain unchanged.

Risks and rollback: additive document and endpoint behavior only. Disable the new settings route/UI if required; retain configuration records for audit/recovery. No destructive data rewrite is required.

Test plan: constructor/build validation, recruitment service tests, API permission contract tests, frontend typecheck/Vitest/build, then isolated Mongo/browser UAT before release.

## Sprint 11–16 — Email-first production recruitment

This plan covers the remaining compulsory work identified in `RECRUITMENT_BUILD_REMAINING_RESEARCH.md`. Each sprint is additive and must preserve existing tenant filters, permissions, endpoints, data, and workflow snapshots.

### Sprint 11 — External action tokens and recruitment email outbox

**Goal:** Establish the secure email-action foundation used by every candidate and interviewer link.

**Scope**

- Add tenant-scoped external-action token entity and repository.
- Support action types: interview scheduling, rescheduling, cancellation, offer review, accept, decline, counter-offer, and preboarding.
- Generate cryptographically secure random tokens; store only hashes.
- Add expiry, revocation, consumption, attempt count, and audit metadata.
- Reuse shared email middleware and add recruitment outbox events/templates.
- Add delivery status, retry/backoff, dedupe key, provider message ID, bounce/failure state, and dead-letter handling.
- Add non-enumerating public responses and rate limits.

**Dependencies:** existing email middleware, career notification worker, Mongo repository, CurrentContext for internal commands.

**Acceptance criteria**

- A token is scoped to one company, application, candidate, and action/version.
- Raw tokens never appear in database records, logs, analytics, or error messages.
- Expired, consumed, revoked, or cross-tenant tokens cannot perform actions.
- Repeating an email event with the same dedupe key sends at most one logical message.
- Internal commands retain existing permission checks.

**Tests:** token entropy/hash verification, expiry, replay, revocation, rate limits, tenant mismatch, enumeration resistance, outbox retry, dedupe, and email-template rendering.

**Risks/rollback:** provider outages and duplicate delivery. Disable new recruitment email triggers while retaining outbox records; do not delete token or audit history.

### Sprint 12 — Interviewer assignment and calendar availability

**Goal:** Allow recruiters to select one interviewer or a panel and safely calculate available time slots.

**Scope**

- Add interviewer/panel selector with company and job-scope filtering.
- Validate active user, recruitment permission, location/time-zone eligibility, and calendar connection.
- Add interview duration, buffers, availability window, required interviewer count, and candidate self-scheduling switch.
- Add provider availability adapters through the existing shared calendar service.
- Add atomic slot claim and double-booking protection.
- Preserve existing fixed-time interview commands for small companies.

**Dependencies:** Sprint 11 tokens/outbox, shared calendar integration, user/role/scope queries.

**Acceptance criteria**

- A recruiter can select one interviewer for simple mode or a panel for advanced mode.
- Candidate slots represent the intersection of required interviewer availability.
- A slot is rechecked and atomically claimed at confirmation.
- Time zones are explicit and stored in UTC plus display zone.
- Existing fixed-time interviews remain backward compatible.

**Tests:** eligibility, cross-tenant user rejection, calendar conflicts, concurrent slot claims, buffers, DST/time zones, panel quorum, provider failure, and simple-mode fallback.

**Risks/rollback:** calendar provider limits or stale availability. Fall back to recruiter-entered fixed time without deleting interview records.

### Sprint 13 — Candidate scheduling and interviewer response

**Goal:** Complete the email-driven interview journey.

**Scope**

- Add public secure scheduling page reached from candidate email.
- Show available slots, candidate time zone, duration, location/meeting information, and optional note.
- Add confirm, reschedule, cancel, and request-more-slots actions.
- Send candidate and interviewer confirmations, calendar invitations, reminders, and failure messages.
- Add interviewer accept/decline/replacement action with reason.
- Add scorecard reminder after interview completion.

**Dependencies:** Sprints 11–12, frontend public route, calendar provider, email templates.

**Acceptance criteria**

- Candidate completes scheduling without an HR portal account.
- Candidate cannot view private notes, compensation, other candidates, or internal scorecards.
- Interviewer action rechecks authentication/permission and tenant scope.
- Rescheduling revokes the old slot and creates a new auditable event.
- Calendar and email failures are visible and retryable.

**Tests:** public-token security, mobile/desktop rendering, slot race, reschedule/cancel, interviewer decline, reminder dedupe, email delivery, and no-private-data leakage.

**Risks/rollback:** public endpoint abuse or email-link forwarding. Revoke affected tokens, disable self-scheduling, and retain recruiter-managed fixed scheduling.

### Sprint 14 — Versioned offers and candidate actions

**Goal:** Send approved offers by email and make candidate responses safe and auditable.

**Scope**

- Add immutable offer-version entity linked to the current offer.
- Add offer document/template rendering.
- Add approved-offer email with secure review link.
- Add candidate review, accept, decline, and request-change actions.
- Add expiry, withdrawal, resend, and revocation behavior.
- Lock an accepted offer atomically and trigger preboarding only once.
- Keep existing offer endpoint behavior compatible by creating the first version automatically.

**Dependencies:** Sprint 11 token/outbox, shared approval infrastructure, preboarding handoff, document storage/rendering policy.

**Acceptance criteria**

- Unapproved offers cannot be sent.
- Every sent offer has an immutable version and expiry.
- Candidate actions work without a portal login but require a valid secure token.
- Duplicate clicks are idempotent.
- Acceptance prevents later mutation and starts preboarding exactly once.
- Decline, expiry, and withdrawal produce audit events and notifications.

**Tests:** approval gating, stale/revoked links, duplicate acceptance, concurrent acceptance, version immutability, document access, expiry worker, tenant mismatch, and preboarding idempotency.

**Risks/rollback:** legal/compensation document errors. Disable sending new offers while preserving existing drafts and sent versions for audit.

### Sprint 15 — Negotiation and compensation controls

**Goal:** Support real-world offer negotiation without overwriting history or bypassing approval.

**Scope**

- Add candidate counter-offer request entity.
- Add recruiter response and proposed revision UI.
- Re-enter shared approval when salary, benefits, or start date changes.
- Add versioned states: `ChangeRequested`, `PendingApproval`, `RevisedSent`.
- Add configurable negotiation limits, expiry, maximum rounds, and authorized negotiators.
- Add candidate and recruiter email notifications for each state.
- Lock final accepted version and prevent further counters.

**Dependencies:** Sprint 14 offer versions, shared approvals, compensation/company configuration, Sprint 11 outbox.

**Acceptance criteria**

- Candidate cannot edit the company offer directly.
- Every counter creates a new immutable request/version.
- Compensation changes require the configured approval path.
- Unauthorized recruiters cannot negotiate outside their scope.
- Maximum rounds and expiry are enforced server-side.
- Final acceptance preserves the entire negotiation timeline.

**Tests:** counter validation, approval re-entry, stale version, concurrent counter requests, maximum rounds, expiry, unauthorized role, audit completeness, and final lock.

**Risks/rollback:** inconsistent compensation or legal terms. Disable counter-offers and continue sending approved fixed offers; retain all history.

### Sprint 16 — Company-size presets, operations, and release hardening

**Goal:** Make the complete feature flexible for small and large companies and prove production readiness.

**Scope**

- Add Simple, Standard, and Enterprise recruitment presets.
- Add company overrides for interview panels, self-scheduling, approvals, scorecards, negotiation, SLA, automation, locations, and analytics.
- Add SLA/stage-aging worker and breach reminders.
- Complete recruitment outbox monitoring, dead-letter replay, and operations dashboard.
- Add role/scope-aware analytics, multi-location rules, internal mobility, and rehire UX.
- Add migration/default fallback for existing companies.
- Run isolated two-tenant Mongo integration and authenticated browser UAT.

**Dependencies:** Sprints 11–15, existing tenant configuration, shared notifications/approvals/audit, deployment observability.

**Acceptance criteria**

- A small company can use a short fixed-time flow with advanced fields hidden.
- An enterprise can enable panels, self-scheduling, approvals, negotiation, SLA, automation, and analytics independently.
- Presets change configuration/UI only; backend authorization and tenant filters always remain active.
- Two companies cannot cross-read, cross-write, or receive each other’s emails/events.
- Outbox failures are observable, retryable, and recoverable.
- Full unit, API, integration, regression, security, and browser suites pass.

**Tests:** preset compatibility, legacy-data fallback, tenant isolation, scope analytics, outbox replay, SLA aging, responsive UAT, accessibility, provider integrations, and rollback rehearsal.

**Risks/rollback:** configuration complexity and provider variance. Disable optional presets/features per company, keep Simple mode available, and preserve additive records for recovery.

## Release gates for the complete sequence

A sprint is not accepted by compilation alone. Before production release:

1. Backend build has zero errors.
2. Unit/service/API tests pass.
3. Integration tests run against an isolated Mongo database.
4. Email provider test messages are captured and verified.
5. Calendar provider booking/reschedule/cancel is verified.
6. Browser UAT covers desktop, tablet, and mobile.
7. Two-company tenant isolation tests pass.
8. Token replay, expiry, revocation, rate-limit, and enumeration tests pass.
9. Offer approval, versioning, negotiation, and acceptance-lock tests pass.
10. Rollback of each optional feature is rehearsed without deleting audit or business records.

## Recommended implementation order

Do not start with analytics or visual configuration. The critical path is:

```text
Secure token/outbox
  → interviewer/panel availability
  → candidate scheduling
  → offer versions and email actions
  → negotiation
  → company presets and enterprise operations
```

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

