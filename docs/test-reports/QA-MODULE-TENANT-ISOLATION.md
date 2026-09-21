# Tenant isolation

## Purpose

Company scope is normally derived from the authenticated JWT `company_id` claim by `CurrentContext`, then applied by the generic Mongo repository's default filter. The repository also sets `CompanyId` when inserting entities with that property.

## Entry points and boundaries

- Authenticated requests: `CurrentContext.CompanyId` returns the single `company_id` claim.
- Named public routes: the helper permits `cId` only for public career, login/register/reset/verification, refresh, and static-file paths.
- Direct Mongo collection use exists in public-career services and must explicitly include company/publication predicates.

## Confirmed defects

No new tenant-isolation defect is filed in this pass. The earlier anonymous `cId` concern is documented already and intentionally excluded from the fresh report. The current implementation is narrower than the behavior described in that older report, so it requires isolated endpoint validation rather than repetition.

## Risks and test gaps

- `MongoRepository.Update`, `UpdateMany`, `Delete`, and `DeleteAll` execute the caller-provided `FilterDefinition` without appending a company predicate. Current audited services generally supply a company predicate or first load through the default filter, but the repository abstraction cannot enforce tenant scope for future callers.
- Public routes deliberately accept a tenant selector. Test each public resource with an arbitrary `cId`, a mismatched public identifier, and a valid identifier from another tenant.
- The generic repository's paging aggregate applies tenant filtering independently of its `withDefaultFilter` parameter; callers need regression coverage for consistency with `GetAll`.

## Recommended priorities

Add isolated API tests that create two companies and assert read/update/delete denial across every high-risk ID route. Centralize tenant predicates in write methods so service omissions fail closed.
