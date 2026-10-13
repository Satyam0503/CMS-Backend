# Codeji Recruitment: Built, Remaining, and Production Readiness

**Research and audit date:** 21 September 2026  
**Scope:** Codeji CMS/HRIS recruitment module, email-first candidate experience, interviewer scheduling, offers/negotiation, tenant isolation, data integrity, and company-sized configuration.

## Executive conclusion

Codeji already has a strong internal recruitment foundation: tenant-scoped jobs, applicants, applications, workflow snapshots, timeline/audit records, interviews, offers, scorecards, preboarding handoff, requisitions, talent pools, duplicate detection, enterprise configuration, idempotency, optimistic concurrency, and permission contracts.

It is **not yet a complete production ATS** for the requested email-first experience. The compulsory remaining layer is the secure external-action workflow: candidate email links, interviewer/panel assignment and response, calendar availability, offer delivery, offer-version negotiation, candidate accept/decline, expiry/revocation, and reliable email/outbox processing.

The product should support two operating profiles without forking the data model:

- **Simple mode for small companies:** recruiter selects one interviewer, enters a fixed time, sends an email, creates a simple offer, and records acceptance manually or through a secure link.
- **Advanced mode for larger companies:** interview plans, panels, shared calendar availability, scorecards, approval chains, versioned offers, negotiation, SLA automation, analytics, audit, and role/scope controls.

The same tenant-safe commands and entities should be used in both modes. Company configuration should turn optional capabilities on or off; it must never weaken server-side tenant or permission checks.

## Research basis

The following current product and security references were reviewed:

- [Workable self-scheduled events](https://help.workable.com/hc/en-us/articles/360007483594-Self-scheduled-events): email scheduling links, calendar availability, interviewer selection, time zones, candidate confirmation, and calendar invitations.
- [Workable recruiting pipeline practices](https://help.workable.com/hc/en-us/articles/4413312707991-Recruiting-pipeline-best-practices): pipelines should be reusable by role and interview feedback should stay connected to the interview stage.
- [Greenhouse offer approval](https://support.greenhouse.io/hc/en-us/articles/201165300-Request-approval-for-a-new-offer) and [approval overview](https://support.greenhouse.io/hc/en-us/articles/17679895458587-Approvals-overview): offers require configured approval before they can be sent.
- [Ashby offer-stage configuration](https://docs.ashbyhq.com/configuring-offer-stage-admin-settings): offer acceptance can advance a candidate, with optional post-offer stages such as references and background checks.
- [OWASP Email Validation and Verification](https://cheatsheetseries.owasp.org/cheatsheets/Email_Validation_and_Verification_Cheat_Sheet.html), [OWASP Input Validation](https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html), and [OWASP Session Management](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html): external email links need cryptographically random, hashed, time-limited, single-use or revocable tokens; HTTPS, rate limits, safe logging, and server-side authorization.

These references support an email-first model. A candidate does not need an HR portal account for the core journey; the email link is only an action surface, not a replacement for internal authorization.

## Capability matrix

Legend: **Built** means source-level functionality exists and is covered by current automated gates. **Partial** means a foundation exists but the production flow is incomplete. **Remaining** means it is required for the target feature but is not implemented.

| Capability | Current state | Classification | Production note |
|---|---|---:|---|
| Tenant-scoped jobs/applicants/applications | Company filters and `CurrentContext` checks exist | Built | Keep server-side filters on every query and command |
| Existing permission model | `ModulePermission` attributes are used | Built | Do not trust frontend visibility as authorization |
| Candidate application and confirmation email | Public career application and application email flow exist | Partial | Add delivery status, retry, bounce handling, and template configuration |
| Recruitment pipeline and workflow snapshot | Snapshot and stage transition rules exist | Built | Snapshot must remain immutable for an existing application |
| Candidate timeline/audit | Applicant logs and enterprise audit events exist | Built | Never log raw external tokens or full action URLs |
| Interview record | Interview entity, UTC dates, status, meeting/location, calendar IDs, panelist ID array exist | Partial | Current UI does not fully assign panels or expose availability rules |
| Shared calendar sync | Existing shared calendar service is reused by interview service | Partial | Add provider availability lookup, booking race protection, reschedule/cancel sync, and delivery evidence |
| Interviewer selection | `PanelistUserIds` is present in the command/entity | Partial | Add eligible-user search, scope checks, panel rules, accept/decline, replacement, and workload/availability logic |
| Candidate interview email link | Not present as a candidate action-token workflow | Remaining/Compulsory | Add public token endpoint, slot selection, confirmation, reschedule, cancellation, and email templates |
| Interviewer email/action link | Not present as a dedicated workflow | Remaining/Compulsory | Add internal authenticated link or deep link with permission re-check |
| Scorecard model and manual ratings | Scorecard entity/service and manual UI rating exist | Built | Add multiple criteria/templates/panel completion rules for advanced mode |
| Offer entity and approval reference | Offer statuses and shared approval reference exist | Partial | Approval integration is foundational; email delivery and candidate response are missing |
| Candidate offer email/link | Not implemented | Remaining/Compulsory | Add secure offer review link and document rendering |
| Offer acceptance/decline | Status values exist | Partial | Add candidate action endpoints, token validation, locking, and audit |
| Offer expiry/revocation | Status values exist | Partial | Add scheduled expiry, revoke endpoint, and candidate-facing messages |
| Offer negotiation | No offer-version/counter-offer model | Remaining/Compulsory for full production | Add immutable versions, counter requests, approval loop, and final lock |
| Preboarding handoff | Existing handoff reuses employee invitation flow | Built | Trigger only from accepted/authorized offer state |
| Duplicate candidate detection | Lookup and soft-merge foundation exists | Partial | Add review/merge UI, authorized merge, relationship preservation verification |
| Requisition/headcount/approval | Additive service and approval reference exist | Built/Partial | Add company opt-in and complete approval UX |
| SLA/stage aging | SLA policy storage exists | Partial | Add aging computation, breach jobs, reminders, and dashboards |
| Automation/outbox | Recruitment automation event storage exists | Partial | Add worker, retry/backoff, dead-letter state, provider delivery status, and dedupe |
| Analytics | Basic tenant-scoped analytics endpoint exists | Partial | Add role/scope filtering, time windows, stage aging, and privacy thresholds |
| Multi-location configuration | Tenant config and override metadata exist | Partial | Add UI and location-specific rules/panels/time zones |
| Internal mobility/rehire links | Relationship link service exists | Partial | Add candidate/employee lookup, authorization, and duplicate relationship rules |
| Small-company simple mode | Configuration foundation exists | Remaining | Add a simple preset that hides advanced fields without bypassing rules |
| Large-company advanced mode | Most internal foundations exist | Partial | Complete panel, approval, email, negotiation, automation, and analytics layers |

## What is compulsory before production

### A. Secure email action infrastructure

Create a reusable `RecruitmentExternalActionToken` or equivalent with:

- Tenant/company ID
- Application ID
- Candidate ID
- Action type (`ScheduleInterview`, `RescheduleInterview`, `AcceptOffer`, `DeclineOffer`, `RequestOfferChange`, `Preboarding`)
- Random token generated by a cryptographically secure source
- Only a hash stored in MongoDB
- Issued time, expiry time, consumed time, and revoked time
- Intended offer/interview version
- Attempt count and last-used metadata

Tokens must be HTTPS-only, time-limited, revocable, rate-limited, and invalidated after use where the action is consequential. OWASP recommends random, sufficiently long, single-use, time-limited tokens and warns against logging full URLs or secrets.

### B. Candidate email/outbox workflow

Reuse the existing email/middleware infrastructure and add recruitment-specific templates/events:

```text
ApplicationReceived
InterviewInvitation
InterviewBooked
InterviewRescheduled
InterviewReminder
OfferReady
OfferChangeRequested
OfferUpdated
OfferAccepted
OfferDeclined
OfferExpired
PreboardingStarted
```

Each email must have a durable outbox record, idempotency/dedupe key, retry policy, provider message ID, delivery status, bounce/complaint status, and audit timeline entry.

### C. Interviewer and panel workflow

Add the following UI and server behavior:

1. Recruiter selects one interviewer or a panel from tenant users.
2. Server verifies company, job scope, active status, interview permission, and calendar integration.
3. The system finds common available slots using interviewer calendars, working hours, leave, buffers, and time zones.
4. Candidate receives only valid slots through an email link.
5. At confirmation, the server rechecks availability and atomically claims the slot.
6. Interviewers receive calendar invitations and action links.
7. Interviewers can accept, decline with a reason, or be replaced.
8. Each interviewer submits an independent scorecard.
9. The hiring manager sees completion state, not private data outside their scope.

### D. Versioned offers and negotiation

Do not mutate a sent offer in place. Add:

```text
Offer
  └─ OfferVersion 1 (sent)
  └─ CandidateCounterRequest 1
  └─ OfferVersion 2 (approved)
  └─ CandidateAcceptance
```

Every version must retain salary, currency, benefits, start date, expiry, approval reference, author, timestamps, and source version. A candidate counter-request must create a pending review state; it must never directly change compensation.

Required transitions:

```text
Draft
→ PendingApproval
→ Approved
→ Sent
→ ChangeRequested
→ PendingApproval
→ RevisedSent
→ Accepted / Declined / Expired / Withdrawn
```

Acceptance must atomically lock the final version and prevent further negotiation.

### E. Data integrity and concurrency

All consequential commands need:

- Tenant filter
- Permission check
- Entity ownership check
- Allowed transition validation
- Expected version check
- Idempotency key
- Audit event
- Retry-safe response

Calendar booking, offer acceptance, merge, and preboarding handoff require race-safe updates. A stale command must return a conflict rather than overwrite another user’s change.

## Flexible company profiles

### Simple profile: 1–25 employees

Recommended defaults:

- One pipeline
- One recruiter/owner
- One interviewer per round
- Fixed interview time or manual scheduling
- No requisition approval
- Basic scorecard optional
- Simple offer approval by company admin
- Email notifications enabled
- Candidate accept/decline link enabled
- No SLA dashboards unless enabled

The UI should show only the fields needed for the selected flow. Hiding advanced fields is a presentation choice; the backend still validates every command.

### Standard profile: 26–250 employees

Recommended capabilities:

- Multiple pipelines by job family
- Interview panels
- Shared calendar availability
- Scorecard templates
- Requisition approval
- Offer approval
- Candidate self-scheduling
- Basic SLA reminders
- Talent pools
- Duplicate review
- Hiring analytics

### Enterprise profile: 250+ employees

Recommended capabilities:

- Multi-location and time-zone rules
- Department/location permission scopes
- Interview panels and coordinator pools
- Approval chains and delegation
- Compensation bands and offer versioning
- Negotiation controls
- Background/reference check stages
- SLA breach automation
- Outbox monitoring and dead-letter handling
- Role/scope-aware analytics
- Internal mobility and rehire links
- Retention, legal hold, export, and audit reporting

The company may enable advanced features individually. A preset must never create a separate code path that bypasses tenant, permission, validation, or audit rules.

## Tenant and security rules

1. Resolve company ID from authenticated server context, never from a client-provided company ID.
2. Include `CompanyId` in every recruitment entity and every repository filter.
3. For nested records, validate both the child ID and its parent company/application ownership.
4. Restrict interviewer, approver, recruiter, and analytics visibility by existing permission and scope infrastructure.
5. Do not expose private notes, scorecards, compensation, approval details, or other candidates in candidate links.
6. Use HTTPS for all external links.
7. Store only token hashes; never log raw tokens or full URLs.
8. Use short expiry and explicit revocation for candidate links.
9. Rate-limit public token endpoints and return non-enumerating errors.
10. Sanitize candidate-provided notes, counter-offer text, meeting URLs, and email template fields.
11. Keep immutable history for sent offers, accepted offers, scorecards, stage changes, merges, and handoffs.
12. Use idempotency and optimistic concurrency on email-triggering and state-changing commands.
13. Mask personal data in logs and restrict audit access.
14. Apply retention/deletion policy without breaking legal/audit holds.

## Required implementation sequence

### Phase 1 — Email and token foundation

- External action token entity/repository
- Token hashing, expiry, revoke, consume, rate limit
- Recruitment email templates and outbox events
- Delivery/retry/bounce tracking
- Candidate-safe public endpoints
- Tests for replay, expiry, tenant mismatch, enumeration, and rate limits

### Phase 2 — Interviewer and scheduling

- Interviewer/panel selector
- Eligibility and scope checks
- Calendar availability service
- Candidate slot-selection link
- Atomic booking and rescheduling
- Interviewer accept/decline/replacement
- Reminder automation
- Tests for double booking, time zones, panel completion, and calendar failure

### Phase 3 — Offers and negotiation

- Offer document model
- Immutable offer versions
- Candidate review/accept/decline link
- Counter-offer request
- Approval re-entry for changed compensation
- Expiry/revocation
- Acceptance lock and preboarding trigger
- Tests for stale links, duplicate clicks, changed offer versions, and acceptance races

### Phase 4 — Flexible configuration

- Simple/standard/enterprise presets
- Company override UI
- Per-job pipeline and interview plan selection
- Optional feature flags
- Multi-location/time-zone settings
- Migration/default behavior for existing companies

### Phase 5 — Enterprise operations

- SLA aging worker
- Automation/outbox worker and dead-letter queue
- Role/scope-aware analytics
- Internal mobility and rehire UX
- Merge review and relationship preservation
- Operational monitoring and support tools

## Definition of production-ready

The feature is production-ready only when all of the following are true:

- Small and large company presets work using the same tenant-safe backend.
- Candidate can complete scheduling, offer review, negotiation, acceptance, decline, and preboarding entirely from email links.
- Interviewers are explicitly selected, authorized, calendar-checked, notified, and able to submit feedback.
- Every external token is secure, expiring, revocable, rate-limited, and audited.
- Every state change is tenant-scoped, permission-checked, validated, idempotent, and concurrency-safe.
- Offer versions and negotiation history are immutable and recoverable.
- Email delivery is observable and retryable.
- Unit, API contract, integration, regression, and authenticated browser tests pass.
- Isolated Mongo-backed UAT verifies two companies cannot cross-read or cross-write data.
- Existing recruitment records remain compatible and workflow snapshots remain stable.

## Current verification evidence

The current source-level gates are green:

- Backend services: 270/270 tests passed.
- Backend API contracts: 61/61 tests passed.
- Frontend unit/regression: 20 files and 40 tests passed.
- Frontend typecheck and production build passed.

The following are still release gates rather than completed evidence:

- Authenticated Mongo-backed integration tests for external links, calendar booking, email delivery, offers, and negotiation.
- Browser UAT across desktop, tablet, and mobile using real test accounts.
- Provider-specific calendar and email delivery verification.
- Security testing of token replay, enumeration, rate limits, and cross-tenant access.

## Final product decision

Build the compulsory email/token, interviewer scheduling, and versioned-offer layers first. Keep scorecards, requisitions, SLAs, automation, analytics, multi-location, internal mobility, and rehire as configurable layers.

This gives a ten-person company a short, understandable recruitment flow while giving a large enterprise the controls it needs without creating a separate product or weakening security.
