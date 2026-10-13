# Recruitment Module Audit Report

Date: 2026-09-21

## Automated verification

- Backend API build: PASS, 0 errors.
- Services regression: PASS, 274/274.
- API contract/security tests: PASS, 66/66.
- Frontend typecheck: PASS.
- Frontend regression: PASS, 20 files / 40 tests.
- Frontend production build: PASS.

## Findings

### High — duplicate merge does not migrate related records

`Codeji.CMS.Services/Recruitments/RecruitmentEnterpriseService.cs` marks the source Applicant as merged/soft-deleted and writes one log entry, but it does not reassign or preserve ownership of applications, interviews, offers, scorecards, talent-pool records, documents, or related notes. The UI/service message claims those records are preserved. This can make historical records inaccessible through the target candidate and is not production-safe until an atomic merge plan migrates every related collection or provides an explicit linked-history query.

### High — candidate email action can consume a token before the business update succeeds

`RecruitmentCandidateActionService` calls `external.Validate(token, true)` before the offer/interview optimistic update. A database conflict or transient update failure after consumption leaves the candidate with an unusable link and no saved action. Consumption must be coupled to the mutation or support a safe retry/compensation path.

### High — RequestOfferChange token is issued but no candidate endpoint handles it

`RecruitmentExternalActionService` allows `RequestOfferChange`, while `RecruitmentCandidateActionService.RespondToOffer` accepts only `Accepted` or `Declined`. A valid request-change email link therefore returns an invalid decision error. The action must either be implemented end-to-end or removed from the allowed action list.

### Medium — automation outbox is not idempotent

`RecruitmentEnterpriseService.Enqueue` writes every request without checking `(CompanyId, IdempotencyKey)`. Retried commands can create duplicate notifications. Add a tenant-scoped dedupe lookup/unique index and return the original event on replay.

### Medium — relationship linking is not idempotent

`RecruitmentEnterpriseService.Link` stores `LastCommandIdempotencyKey` but does not use it to prevent duplicate InternalMobility/Rehire links. Retries can create duplicate relationships.

### Medium — SLA location matching is whitespace-sensitive

`UpsertSla` searches using the untrimmed command location but stores a trimmed value. The same location with surrounding whitespace can create duplicate policies.

### Medium — offer negotiation lacks complete server validation

`RecruitmentOfferNegotiationService` does not validate allowed offer-version statuses, positive salary/currency formats, expiry ordering, maximum negotiation rounds, or the command expected version. Concurrent version allocation is based on read-max-plus-one and needs a unique index/atomic allocation strategy.

### Medium — action issuance does not verify resource ownership at issuance time

`RecruitmentExternalActionService.Issue` accepts ApplicantId/OfferId/InterviewId values without loading and validating that the referenced records belong to the current tenant and candidate. The later action path protects the main mutation, but invalid or misleading links can still be issued.

## Environment-only gaps

- No isolated Mongo-backed integration run was available in this audit.
- No authenticated two-company browser UAT was available.
- Email/calendar provider delivery was not exercised with captured provider messages.

## Release decision

Automated source gates are green, but the High findings must be fixed before production release. The module should be treated as release-candidate code, not fully production-ready, until merge migration, token mutation atomicity, and RequestOfferChange behavior are resolved.



## Sprint 0 remediation update (2026-09-21)

- High: candidate action token consumption is now performed after the optimistic business update, with compensation on failed consumption; matching idempotency replays are safe.
- High: `RequestOfferChange` is now a supported candidate action with an explicit `ChangeRequested` offer state and sanitized message handling.
- High: applicant merge now reassigns tenant-scoped recruitment history (interviews, offers, offer versions, counter requests, scorecards, talent-pool memberships, handoffs, and logs) and revokes source-linked external action tokens before soft-deleting the source applicant.
- New regression coverage: 2 focused Sprint 0 tests; complete services suite 276/276 passed; API contract suite 66/66 passed.
- Runtime Mongo-backed merge/action race testing remains pending in an isolated environment.
