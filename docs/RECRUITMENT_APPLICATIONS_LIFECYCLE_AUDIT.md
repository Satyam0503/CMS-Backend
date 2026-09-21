# Recruitment applications, statistics, and lifecycle audit

Date: 2026-09-21

## Executive result

The current screen is an applicant-detail action drawer, not a complete application pipeline. The backend currently treats an `Applicant` document as both the person and the job application. That works for a basic single-job flow, but it creates reporting and lifecycle defects when the same person applies to multiple jobs, is rejected and reapplies, is merged, or reaches offer/preboarding.

A production ATS should separate the durable candidate/person from a job-specific application (job consideration), then attach stage history, interviews, scorecards, offers, communications, and handoff to that application. Ashby explicitly reports on job considerations because one candidate can contribute multiple times to a funnel; Greenhouse models application stages per job application. [Ashby funnel metrics](https://docs.ashbyhq.com/understanding-funnel-metrics), [Greenhouse interview stages](https://harvestdocs.greenhouse.io/reference/get_v3-job-interview-stages)

## Current relationship model

```text
JobVacancy (JobId)
   |
   +-- Applicant (ApplicantId, VacancyId, ActivityType)
          |
          +-- ApplicantLogs (timeline/comments)
          +-- RecruitmentInterview (ApplicantId)
          +-- RecruitmentOffer (ApplicantId)
          +-- RecruitmentScorecard (ApplicantId)
          +-- RecruitmentTalentPoolMember (ApplicantId)
          +-- RecruitmentHandoff (ApplicantId -> employee)
```

There is no first-class `Candidate` plus `Application`/`JobConsideration` relationship in the current runtime path. `Applicant` is the application row and the person record at the same time.

## Expected production flow

1. **Job setup**: create/publish a vacancy with its own location, headcount, pipeline template, interview plan, scorecard, timezone/display policy, and application deadline.
2. **Application intake**: create or match a candidate, then create a job-specific application. Duplicate checks must be scoped to the same job; the same person may apply to another job.
3. **Review**: application starts in `Applied/New`; recruiter screens it and moves it to a configured stage. Every move records actor, timestamp, reason, source stage, target stage, pipeline-version snapshot, and expected version.
4. **Interview**: select active interviewers/panelists for this company, schedule in the project timezone, store UTC instants, send candidate/interviewer notifications, collect scorecards, and prevent progression until required feedback is complete (unless an authorized override is recorded).
5. **Decision**: advance, hold, or reject the application. Rejection is an outcome on the current stage, not a normal stage after the candidate. A rejected application remains in history; the candidate may apply to another job or reapply when policy allows.
6. **Offer**: draft -> approval -> sent -> candidate accepts/declines/requests change. Offer versions and negotiation rounds remain attached to this application and are immutable history.
7. **Hire/preboarding**: acceptance creates a controlled handoff once; employee creation and preboarding status are linked back to the application. `Hired` is the terminal recruitment outcome; onboarding/preboarding belongs to the HR lifecycle rather than being counted as another recruiting stage.
8. **Reporting**: show current pipeline counts, historical stage conversion, time in stage, interview/scorecard completion, offer acceptance, source, and hired outcomes. Reports must define cohort, date range, job, department, location, and permission scope.

This follows established ATS guidance: keep pipelines concise, avoid a separate `Disqualified` stage, do not put stages after `Hired`, and keep interview setup/feedback and offer approval/send as work inside the relevant stage. [Workable pipeline guidance](https://help.workable.com/hc/en-us/articles/4413312707991-Recruiting-pipeline-best-practices)

## Confirmed issues in the current implementation

### Critical

1. **Applicant and application are conflated** (`Applicant.cs`, `ApplicantServices.cs`). `ApplyNowService` looks up an existing applicant by company-wide email rather than `(CompanyId, VacancyId, candidate identity)`. A person applying to a different job can be treated as a reapply to the previous job and be blocked by the six-month wait. This violates multi-job candidate behavior.
2. **ReApply is a terminal state** (`RecruitmentPipelineRules.cs`). The rule rejects all transitions once the current state is `ReApply`, so a newly created reapplication cannot progress to screening/interview. ReApply should be an intake marker or the new application should start at `New` with a reapplication flag.
3. **Application list includes soft-deleted/merged rows** (`ApplicantServices.GetApplicantsList`). The filtered query and null-filter query do not consistently add `!x.IsDeleted`, while analytics does. Merged records can appear in Applications even though analytics excludes them.
4. **Analytics is not wired into the frontend**. `getRecruitmentAnalytics` exists in the service and `RecruitmentEnterpriseController.Analytics` exists, but the Applications page does not load or render it. The user therefore sees no authoritative stats tied to the current filters/job.
5. **Merged count is always effectively zero in analytics** (`RecruitmentEnterpriseService.Analytics`). Analytics filters `!x.IsDeleted` and then counts `MergeStatus == "Merged"`; the merge operation soft-deletes the source, so merged sources cannot be counted.

### High

6. **Stats are applicant counts, not application funnel metrics**. `TotalApplicants`, `ActiveApplicants`, `RejectedApplicants`, and `ByStage` count current Applicant documents. They do not provide stage conversion, cohort dates, time-in-stage, source, offers, hires, or interview completion. A candidate with two job applications cannot be represented correctly.
7. **Lifecycle buttons are not state/permission aware** (`RecruitmentLifecycleDrawer.tsx`). Interview, Offer, Preboarding, Scorecard, Talent Pool, and Merge are shown for every applicant. The UI does not disable interview/offer/preboarding based on current stage, offer state, duplicate status, feature mode, or user permission; server checks are the final authority but the UI should prevent invalid actions.
8. **Interview location and meeting URL are conflated**. The drawer sends the single `Location or meeting URL` field as `Location`; `MeetingUrl` is never populated. A URL can therefore be stored as a physical location and omitted from calendar/link behavior.
9. **Interview times use browser-local parsing, not the project timezone**. The drawer creates a local `YYYY-MM-DDTHH:mm` value and calls `new Date(...).toISOString()`. A recruiter in another browser timezone can schedule a different instant than the intended Asia/Kolkata time. The UI must use the configured timezone explicitly, then convert to UTC.
10. **Required interviewer/feedback semantics are incomplete**. Interviewers are optional, active-user validation exists server-side, but there is no visible interviewer selection directory, no invite/response state, no per-interviewer scorecard completion requirement, and no panel decision/debrief state. Structured hiring systems use configured scorecards and group feedback before a decision. [Greenhouse scorecards](https://support.greenhouse.io/hc/en-us/articles/4414777492891-Scorecard-overview)
11. **Offer UI allows statuses that may not be usable as candidate actions**. The drawer allows Draft/PendingApproval/Approved/Sent but does not show approval/send/expiry/acceptance state or a candidate action link. The offer service creates a draft even when the requested status is not Draft, so the UI and server behavior can diverge.
12. **Preboarding handoff is not visibly tied to offer acceptance**. The server requires `Selected`, but the drawer only asks for employee data and does not show why the action is available, whether the offer was accepted, or whether an employee already exists. The correct behavior is acceptance -> one idempotent handoff -> preboarding status -> employee onboarding.
13. **Timeline is not a complete lifecycle audit**. Applicant logs record status/comments and some feature events, but interviews, scorecard submissions, offer versions/negotiation, notifications, and handoff should all appear in one ordered application timeline with actor, source, and outcome.

### Medium

14. **Workflow snapshot is stored on Applicant but not used as the authoritative stage model**. Current transitions use the enum ordering (`ActivityType`) rather than a job-specific pipeline/stage ID and snapshot version. Different jobs cannot safely have different stage sequences.
15. **No explicit stage-entry/exit timestamps or SLA calculation in the application view**. SLA policy exists, but the screen does not show current stage age, due status, pause reasons, or escalation history.
16. **No cohort definition on analytics**. Funnel statistics must specify whether they represent applications created, applications completed, or activity in a date range. Ashby warns that counting incomplete considerations prematurely skews funnels. [Ashby funnel metrics](https://docs.ashbyhq.com/understanding-funnel-metrics)
17. **Job-level counts and analytics use different sources/filters**. Job vacancy counts group Applicant rows, while enterprise analytics excludes deleted rows and includes different active semantics. These numbers can disagree without explaining why.
18. **External application mode is not a normal internal application path**. The system intentionally rejects internal submission for externally routed jobs; the UI/report must show the external destination and not count an uncreated internal application.

## Required stats contract

The dashboard should expose, with tenant and permission scope:

- Current pipeline: count of applications by configured stage, plus rejected/withdrawn/hold separately.
- Funnel cohort: applied -> screened -> interview -> offer -> accepted -> hired, with passthrough percentages.
- Time metrics: median/average time to first review, interview, offer, acceptance, and hire; current stage aging; SLA breaches.
- Quality/operations: interview scheduled/completed, scorecards submitted/overdue, offer approval cycle, offer acceptance rate, reapplication rate, source and job/location breakdown.
- Definitions: candidate count vs application count, date range, excluded/archived records, and whether counts are current-state or historical events.

## Recommended remediation order

1. Introduce `Candidate` identity and `Application`/job-consideration records while retaining the current Applicant data through a compatibility adapter.
2. Fix tenant/job-scoped duplicate and reapply logic; make ReApply non-terminal.
3. Enforce `!IsDeleted` consistently and repair merged-history reporting.
4. Make pipeline stage IDs/version snapshots authoritative, with stage history and timestamps.
5. Make lifecycle drawer state/permission/mode aware; split meeting URL from physical location and add explicit timezone conversion.
6. Add interviewer directory/panel assignment, scorecard requirements, debrief/decision state, and candidate notification status.
7. Make offer -> acceptance -> handoff -> preboarding a visible state machine.
8. Build analytics from application/stage-event history with documented cohorts and role/scope filters, then wire it into Applications and job detail pages.
9. Add integration tests for multi-job candidates, reapply, rejection, merge visibility, timezone conversion, panel feedback, offer acceptance, and analytics reconciliation.

## Verification scope

This is a source audit plus product-flow comparison. Automated source tests already pass in the current workspace, but no authenticated browser test against two tenants, Mongo-backed migration run, real calendar sync, or email-provider delivery was performed for this report.
