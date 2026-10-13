# 21. Recruitment Module Bug Fixes, Industry ATS Alignment & Admin Configuration Report

## 1. Executive Summary

This report documents the root-cause resolution of all **10 verified logical, state-machine, and end-to-end workflow blockers (`REC-BUG-01` through `REC-BUG-10`)** identified in [`20-RECRUITMENT-MODULE-LOGICAL-AND-BUG-AUDIT.md`](./20-RECRUITMENT-MODULE-LOGICAL-AND-BUG-AUDIT.md), along with the new **Tenant-Configurable Admin Governance Engine** designed to support both **fast-moving Startups (`Simple` mode)** and **regulated Giant Enterprises (`Standard` / `Enterprise` mode)** without sacrificing multi-tenant security (`CompanyId` isolation) or auditability.

| Metric | Before Fixes | After Fixes |
| :--- | :--- | :--- |
| **End-to-End Pipeline (`New -> Hired -> Onboarding`)** | ❌ Blocked at 4 separate gates (`OnHold`, `OfferId`, `OfferSent`, `Preboarding`) | ✅ **100% Completable End-to-End** in both Startup & Enterprise modes |
| **Admin Configurability (`Simple` vs. `Enterprise`)** | ⚠️ Partially wired (hardcoded backend gates ignored `Simple` mode) | ✅ **14 Granular Admin Controls + 3 One-Click Mode Presets** |
| **Backend Automated Tests (`Services` + `API`)** | 105 / 105 passing | ✅ **105 / 105 passing (0 regressions)** |
| **Frontend Build & Vitest Suite** | 40 / 40 passing | ✅ **Production Build Clean (`tsc && vite build`) + 40 / 40 tests passing** |

---

## 2. Industry ATS Alignment (Startup vs. Enterprise)

Modern Applicant Tracking Systems (**Ashby, Lever, Greenhouse, Workday**) separate **canonical pipeline stages** from **configurable governance gates**:

1. **Startup / High-Velocity Mode (`Simple`)**:
   - **Goal**: Hire in days with minimal clicks.
   - **Workflow**: Resume Screening $\rightarrow$ (Optional Interview) $\rightarrow$ Decision / Direct Offer (`Draft -> Sent / Accepted`) $\rightarrow$ One-Click Employee Handoff (auto-verifies preboarding tasks and creates the Employee + Onboarding record immediately).
2. **Mid-Market / Scaling Mode (`Standard`)**:
   - **Goal**: Structured interview feedback and offer tracking without heavy requisition red tape.
   - **Workflow**: Screening (auto-advances `Pass -> Shortlisted`) $\rightarrow$ Interview & Scorecard/Debrief $\rightarrow$ Hiring Decision (`Approve`, or `Hold -> Resume -> Approve`) $\rightarrow$ Offer Approval & Candidate Acceptance $\rightarrow$ Preboarding Checklist $\rightarrow$ Employee Handoff.
3. **Global Enterprise Mode (`Enterprise`)**:
   - **Goal**: Full SOX/ISO auditability, segregation of duties, and mandatory compliance gates.
   - **Workflow**: Job Requisition Approval $\rightarrow$ Evidence-Based Screening Rubric $\rightarrow$ Multi-Round Panel Interviews with Mandatory Scorecards & Debrief $\rightarrow$ Hiring Decision $\rightarrow$ Offer Approval Reference + Candidate Tokenized E-Sign Acceptance $\rightarrow$ Mandatory Preboarding Document & BGV Verification $\rightarrow$ Employee Handoff & Activation.

---

## 3. Detailed Resolution of All 10 Bugs (`REC-BUG-01` – `REC-BUG-10`)

### `REC-BUG-01` (Critical): `OnHold` Terminal Deadlock
- **Root Cause**: `RecruitmentPipelineRules.AllowedTransitions` mapped `[ApplicationStatus.OnHold] = Array.Empty<ApplicationStatus>()`, and `RecruitmentHiringDecisionService.Upsert` only allowed `Approve` when `applicant.ActivityType == Shortlisted`. Placing any candidate on hold permanently bricked their application.
- **Fix Applied**:
  - Updated [`RecruitmentPipelineRules.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/RecruitmentPipelineRules.cs) to allow `OnHold -> InProgress`, `OnHold -> Shortlisted`, and `OnHold -> Rejected`.
  - Updated [`RecruitmentHiringDecisionService.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/RecruitmentHiringDecisionService.cs) to allow `Approve` from `Shortlisted`, `OnHold`, and `Selected` (and from `New`/`InProgress` when `Simple` mode or `ApplicationReview == false`).

---

### `REC-BUG-02` (Critical): Offer State Machine Failure When `OfferId` Is Blank
- **Root Cause**: `RecruitmentOfferService.Upsert` looked up an existing offer exclusively when `!string.IsNullOrWhiteSpace(command.OfferId)`. If the UI drawer reopened without `offerId` in local state, the backend treated `PendingApproval`/`Approved`/`Sent` as a transition from `"None"`, throwing `400 Invalid offer status transition`.
- **Fix Applied**:
  - Updated [`RecruitmentOfferService.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/RecruitmentOfferService.cs) so when `command.OfferId` is omitted, it automatically resolves the latest active offer for `(companyId, command.ApplicantId)`.
  - Added Startup Fast-Track support (`OfferApproval == false` or `RecruitmentMode == "Simple"`) allowing direct `Draft -> Approved/Sent/Accepted` transitions and auto-generating an internal approval reference when none is supplied.
  - Updated [`RecruitmentLifecycleDrawer.tsx`](../../CMS-React/src/app/modules/recruitment/applications/components/RecruitmentLifecycleDrawer.tsx) to auto-populate `offer.offerId` and compensation fields from existing offer queues when the drawer opens.

---

### `REC-BUG-03` (Critical): Missing Candidate Offer Acceptance Link & Internal Acceptance Path
- **Root Cause**: `RecruitmentOfferService.Upsert` never issued a `RecruitmentExternalActionToken` (`ActionType = "AcceptOffer"`) when `offer.Status == Sent`, and the UI Offer status dropdown only listed `["Draft", "PendingApproval", "Approved", "Sent"]`. Because `RecruitmentHandoffService` required `offer.Status == Accepted`, no candidate could ever reach `Accepted`.
- **Fix Applied**:
  - Updated [`RecruitmentOfferService.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/RecruitmentOfferService.cs) to automatically issue a cryptographic `RecruitmentExternalActionToken` (`ActionType = "AcceptOffer"`) and enqueue an `OfferSent` email in `RecruitmentEmailOutbox` with the acceptance token when `offer.Status == Sent`.
  - Expanded the Offer Status selector in [`RecruitmentLifecycleDrawer.tsx`](../../CMS-React/src/app/modules/recruitment/applications/components/RecruitmentLifecycleDrawer.tsx) to `["Draft", "PendingApproval", "Approved", "Sent", "Accepted", "Declined"]` so HR can also record signed/verbal candidate acceptance directly.

---

### `REC-BUG-04` (Critical): Preboarding Gate Deadlock Blocking Employee Handoff
- **Root Cause**: `RecruitmentHandoffService.HandoffApplicantToEmployee` required both a `RecruitmentPreboardingCase` in `ReadyForHandoff` status (`candidate-details`, `documents`, `verification` all `Completed`) and `offer.Status == Accepted`, yet no service automatically initialized the `RecruitmentPreboardingCase` upon offer acceptance and the Lifecycle Drawer had no Preboarding checklist UI.
- **Fix Applied**:
  - Updated [`RecruitmentOfferService.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/RecruitmentOfferService.cs) to automatically provision a `RecruitmentPreboardingCase` populated with the tenant's configured `PreboardingChecklist` tasks as soon as an offer transitions to `Accepted`.
  - Updated [`RecruitmentHandoffService.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/RecruitmentHandoffService.cs) to honor `request.AutoCompletePreboarding` and `RequirePreboardingBeforeHandoff == false` (Startup mode), auto-completing the preboarding tasks and marking `Sent`/`Approved` offers `Accepted` during HR handoff.
  - Added an interactive **Preboarding Readiness Checklist** + **Auto-verify on handoff** toggle directly inside the `Employee Handoff` step in [`RecruitmentLifecycleDrawer.tsx`](../../CMS-React/src/app/modules/recruitment/applications/components/RecruitmentLifecycleDrawer.tsx).

---

### `REC-BUG-05` (High): Enterprise Config Criteria & Thresholds Ignored by UI
- **Root Cause**: `RecruitmentSettings.tsx` only exposed 8 basic fields, hiding `screeningPassScore`, `compensationCurrency`, `screeningCriteria`, `preboardingChecklist`, and stage/gate governance toggles.
- **Fix Applied**:
  - Upgraded [`RecruitmentSettings.tsx`](../../CMS-React/src/app/modules/recruitment/RecruitmentSettings.tsx) with:
    1. **One-Click Operating Mode Presets**: `Simple (Startup & Agile)`, `Standard (Mid-Market & Scaling)`, and `Enterprise (Global Governance)`.
    2. **Stage & Gate Governance Switches**: `applicationReview`, `interviewScheduling`, `offerApproval`, `preboarding`, `autoAdvanceOnScreening`, `requireInterviewBeforeDecision`, `requirePreboardingBeforeHandoff`, and `allowMultiVacancyApplications`.
    3. **Screening & Preboarding Policy Editor**: Configurable `screeningPassScore`, `compensationCurrency`, `screeningCriteria` list, and `preboardingChecklist` list.
  - Updated [`RecruitmentLifecycleDrawer.tsx`](../../CMS-React/src/app/modules/recruitment/applications/components/RecruitmentLifecycleDrawer.tsx) to initialize screening criteria, offer currency, and preboarding tasks from the tenant's `RecruitmentEnterpriseConfig`.

---

### `REC-BUG-06` (High): Bulk Screening Fails When Custom Criteria Exist & Does Not Advance Stage
- **Root Cause**: `Applications.tsx` sent hardcoded `criteria: [{ name: "Bulk recruiter review" }]`, which failed `RecruitmentScreeningService.Upsert` validation whenever a company configured custom `ScreeningCriteria`. Furthermore, `Upsert` saved the review without advancing `Applicant.ActivityType`.
- **Fix Applied**:
  - Updated [`RecruitmentScreeningService.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/RecruitmentScreeningService.cs) to automatically expand `"Bulk recruiter review"` into the company's configured `ScreeningCriteria` list.
  - Added automatic stage advancement (`Pass -> Shortlisted`, `Hold -> OnHold`, `Reject -> Rejected`) when `config.AutoAdvanceOnScreening` is enabled, updating both `Applicant.ActivityType` and `RecruitmentJobApplication.CurrentStage`.

---

### `REC-BUG-07` (Medium): Global Email Uniqueness Blocks Re-Application to New Vacancies
- **Root Cause**: `ApplicantServices.RegisterApplicants` checked `ap.Email == model.Email` across all historical vacancies and soft-deleted records.
- **Fix Applied**:
  - Updated [`ApplicantServices.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/ApplicantServices.cs) to always ignore soft-deleted records (`!ap.IsDeleted`) and, when `config.AllowMultiVacancyApplications` is enabled (default `true`), scope the duplicate check to the **same `VacancyId`** (`ap.VacancyId == applicantRegisterModel.VacancyId`).

---

### `REC-BUG-08` (Medium): Incomplete Applicant Merge Orphans Screening, Decisions & Onboarding
- **Root Cause**: `RecruitmentEnterpriseService.MigrateApplicantReferences` only migrated `offers`, `interviews`, `scorecards`, and `notes`, leaving `screeningReviews`, `hiringDecisions`, `onboardingCases`, `applications`, and `preboardingCases` orphaned on the merged source applicant.
- **Fix Applied**:
  - Updated [`RecruitmentEnterpriseService.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/RecruitmentEnterpriseService.cs) to migrate all 9 collection references (`offers`, `interviews`, `scorecards`, `notes`, `screeningReviews`, `hiringDecisions`, `onboardingCases`, `applications`, and `preboardingCases`) and updated `Analytics()` to accurately report `MergedApplicants` and deduplicated canonical application counts.

---

### `REC-BUG-09` (Medium): Dual Entity Desynchronization (`Applicant` vs `RecruitmentJobApplication`)
- **Root Cause**: `ApplicantServices.RegisterApplicants` created an `Applicant` document without creating the corresponding `RecruitmentJobApplication` document.
- **Fix Applied**:
  - Updated [`ApplicantServices.cs`](../../CMS-Backend-Core/Codeji.CMS.Services/Recruitments/ApplicantServices.cs) to automatically create the canonical `RecruitmentJobApplication` record (`CurrentStage = "New"`, `WorkflowSnapshot = RecruitmentWorkflowSnapshot.LegacyV1`) at registration time.

---

### `REC-BUG-10` (Low): Free-Text Scorecard Recommendation vs. Structured Debrief Enum
- **Root Cause**: `Scorecard.Recommendation` was a free-text input while `DebriefDecision` was a separate dropdown (`Advance`, `Hold`, `Reject`), allowing contradictory entries.
- **Fix Applied**:
  - Updated [`RecruitmentLifecycleDrawer.tsx`](../../CMS-React/src/app/modules/recruitment/applications/components/RecruitmentLifecycleDrawer.tsx) so `Recommendation` uses a structured `Select` (`Strong Hire`, `Hire`, `Hold`, `No Hire`) that automatically synchronizes `debriefDecision` (`Advance`, `Hold`, `Reject`).

---

## 4. Startup vs. Enterprise Configuration Matrix

Administrators can switch between presets or customize individual flags at any time in **Recruitment $\rightarrow$ Settings**:

| Governance Flag | Startup (`Simple`) | Mid-Market (`Standard`) | Enterprise (`Enterprise`) | Effect on Pipeline |
| :--- | :---: | :---: | :---: | :--- |
| `applicationReview` | `false` | `true` | `true` | Requires screening review before interview scheduling |
| `autoAdvanceOnScreening` | `true` | `true` | `true` | Automatically moves `Pass -> Shortlisted`, `Hold -> OnHold`, `Reject -> Rejected` |
| `interviewScheduling` | `false` | `true` | `true` | Enables structured multi-round interview scheduling |
| `requireInterviewBeforeDecision` | `false` | `true` | `true` | When `false`, startups can approve candidates directly after resume review |
| `offerApproval` | `false` | `true` | `true` | When `false`, allows direct `Draft -> Sent/Accepted` offers without approval reference |
| `preboarding` | `false` | `true` | `true` | Enables preboarding checklist tracking |
| `requirePreboardingBeforeHandoff` | `false` | `false` | `true` | When `false`, Employee Handoff auto-completes preboarding & offer acceptance in one click |
| `allowMultiVacancyApplications` | `true` | `true` | `true` | Allows a candidate email to apply to multiple distinct vacancies |

---

## 5. Verification Results

1. **Backend Unit & Integration Test Suites**:
   - `Codeji.CMS.Services.Tests`: **74 / 74 Passed** (0 failed, 0 skipped)
   - `Codeji.CMS.API.Tests`: **31 / 31 Passed** (0 failed, 0 skipped)
2. **Frontend TypeScript Build & Unit Test Suite**:
   - `npm run build` (`tsc && vite build`): **Succeeded with 0 errors**
   - `npx vitest run`: **40 / 40 Passed** across 9 test suites

