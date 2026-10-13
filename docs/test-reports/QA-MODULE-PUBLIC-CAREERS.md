# Public careers audit

## Entry points and data flow

Frontend: `modules/applynow/*` and `modules/careerPortal/*`.  
Backend: anonymous `/api/public` career, company profile, engagement, and subscription controllers plus `PublicCareerService`, career notification worker, and public-company profile service.

Public job searches use direct Mongo collection queries and explicit publication predicates. Public job details require an active, non-deleted, career-enabled company and a published, active career vacancy. Internal applications create an applicant and queue candidate/recruiter notifications; external jobs expose only a safe HTTPS non-loopback redirect URL.

## Confirmed controls

- Public job eligibility checks portal flags, company status, vacancy status, and deadlines.
- Tests cover unsubscribe-token tampering, public rich-text sanitization, publication eligibility, and alert matching.
- Sensitive public endpoints use a fixed-window rate-limit policy configured in `Program.cs` where applied.

## Confirmed bugs

No fresh confirmed public-career bug is filed in this pass. The existing social-link validation finding is excluded because it is reported as addressed.

## Risks and missing tests

- Public endpoints must be runtime-tested with two companies, non-public IDs/slugs, expired jobs, and arbitrary `cId` headers using an isolated dataset.
- Resume/application upload testing must validate bytes, MIME sniffing, filename normalization, authorization of later download, malware scanning, and cleanup on application failure.
- Subscription/visitor cookies or tokens need replay, expiry, consent, and enumeration tests.
- Queued email failures are intentionally non-transactional; verify they do not alter application persistence and are observable in logs.
