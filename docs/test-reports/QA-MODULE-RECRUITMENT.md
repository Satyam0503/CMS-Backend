# Recruitment and applicants audit

## Entry points

Frontend: recruitment routes, jobs, applications, applicant detail/edit, and process logs.  
Backend: `JobVacancyController`, `ApplicantsController`, `JobVacancyService`, and `ApplicantServices`.

## Permissions and flow

Jobs, applications, and process logs use module permission attributes for their main list/create/edit actions. A vacancy is tenant-scoped by normal repository filters. Applicant updates create logs and can send queued status emails when activity changes.

## Confirmed bug

`BUG-4-02` in `QA-BUG-REPORT-4.md`: `POST /api/JobVacancy/GetVacancyById` is authenticated but lacks Jobs.View permission, unlike other internal recruitment reads.

## Risks and missing tests

- Applicant stage values have no audited transition matrix; validate prohibited reversals, duplicate status emails, and concurrent edits.
- The anonymous legacy resume upload route needs isolated tests for ownership binding, duplicate email handling, MIME/content validation, and path traversal defense.
- Test applicant/comment/process-log IDOR across two companies and deletion/soft-delete visibility.
