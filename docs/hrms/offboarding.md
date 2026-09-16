# Offboarding Module

## Purpose

Offboarding manages the controlled end of employment. Resignation is the employee/HR request; offboarding is the broader review, notice, handover, clearance, assets, access, finance, benefits, evidence, settlement, documents, interview, account closure, reminder, former-portal, and rejoin workflow.

`EmployeeExit` is the authoritative aggregate. `EmpUser.Lifecycle` and legacy employee status/date fields are synchronized projections for other modules.

## Entry paths

- **Employee resignation:** `ResignationAction` submits the authenticated employee's own resignation without granting Employees Edit.
- **HR-initiated exit:** `EmployeeOffboardingAction` initiates a permitted exit for a same-company employee.
- **Management:** `EmployeeExitManagementCard` and `EmployeeExitOperationsPanel` load the active case and available operations.
- **Queue:** `EmployeeExitQueueAction` exposes operational paging/search/status and reminder work.
- **History:** a dedicated button/modal shows previous attempts, including withdrawn/cancelled records. Manage Offboarding is hidden when there is no actionable case.

## Policy snapshot and feature controls

`EmployeeExitPolicy` controls notice period, manager/HR review, discussion, employee acknowledgement, reminders, handover, clearance, evidence, settlement, documents, interview, and former portal. A snapshot is copied into each case so policy edits do not rewrite an active workflow.

Feature configuration covers SelfResignation, HrInitiatedExit, ManagerApproval, HrApproval, Discussion, NoticePeriod, NoticeAdjustments, Withdrawal, Handover, Clearance, Assets, ItAccess, Finance, Benefits, Evidence, FinalSettlement, ExitInterview, Documents, and FormerEmployeePortal. Features can be enabled, required, completion-blocking, permission-owned, waivable, and reason-required.

## End-to-end flow

```text
Employee resignation / HR exit
        |
Validate tenant, employee, permission, duplicate case and policy
        |
Create EmployeeExit + policy snapshot + version + history
        |
Manager/HR review and optional discussion
        |
Notice period, date and adjustment decisions
        |
Handover + clearance + assets + access + finance + benefits + evidence
        |
Settlement + interview + required documents
        |
Completion readiness validation
        |
Complete exit and synchronize lifecycle/account/payroll/attendance cutoff
        |
Optional former portal and later rejoin
```

## Decisions and states

The aggregate owns its status and event history. Terminal/non-actionable states include Withdrawn, Cancelled, Completed, and Rejoined.

- Review follows configured manager/HR requirements.
- Employee withdrawal can require an authorized decision; accepted withdrawal closes the resignation without completing offboarding.
- HR cancellation is distinct and requires authorized action/reason.
- Notice-date and notice-adjustment changes are explicit versioned request/decision flows.
- Exceptional completion is an elevated override and must retain reason/audit evidence.
- Rejoin restores approved lifecycle dimensions without deleting exit history.

## Workstreams

### Handover and reassignment

Responsibility reassignment stores the responsibility, new owner, and notes. Handover tasks have explicit review. Required incomplete items can block completion.

### Clearances, assets, and access

Company clearance templates seed case work. Clearance items retain owner, status, evidence, and notes. Asset items track return/recovery. Access items track system/account revocation. Required unresolved items block completion according to the policy snapshot.

### Finance, benefits, and settlement

Finance items track dues/recoveries/payables; benefit items track continuation/closure. Final settlement records salary payable, deductions, notice recovery, net payable, source payroll ID/month, calculation, approval, payment, and status. Calculation, approval, and paid marking are separate actions.

### Evidence and documents

Evidence is uploaded privately, verified or rejected explicitly, superseded rather than silently replaced, and downloaded through a tenant/case/permission-checked endpoint. Supported generated documents include Relieving Letter, Experience Letter, and Final Settlement Statement.

### Exit interview and former portal

Interview data can hold employee feedback, manager feedback, HR notes, rehire eligibility, and conducted time. Company former-portal enablement and per-employee access are separate controls; completing an exit does not automatically grant access.

## Completion effects

`EmployeeLifecycleTransitionService.ApplyCompletedExit` maps exit type to final employment status, uses Working as the neutral availability value, closes the account, sets payroll status to FinalSettlement, marks offboarding Completed, defaults former portal conservatively, applies alumni eligibility where appropriate, records history, and increments version.

Completion also applies employee exit/last-working projections, attendance cutoff after the last working date, settlement/document progression, and outbox events while preserving historical attendance, payroll, evidence, and audit data.

## Reminders and outbox

`EmployeeExitOutboxWorker` invokes `EmployeeExitOutboxProcessor`:

- Due reminders are atomically claimed.
- Dedupe keys prevent duplicate recipient events.
- Policy controls employee, manager, HR, administrator, in-app, and email recipients.
- Delivery channels record timestamps independently.
- Failure records keep attempts, error, and next-attempt time.
- HR digests target same-company users with Offboarding CaseManage.
- Global reminder rendering and the manual Offboarding cases button remain separate UI concerns.

## UI behavior

The employee profile shows resignation/offboarding actions only when relevant and permitted. Resignation history opens in a modal. Withdrawn history remains visible but does not show Manage Offboarding. The work schedule and offboarding cards are independent.

The management UI groups summary, reviews/discussion, notice, handover, clearance, assets, access, finance, benefits, evidence, settlement, interview, documents, and portal controls. Actions appear only for valid state/permission combinations and surface loading, errors, stale versions, disabled reasons, and completion blockers.

The queue supports page, page size, search, and status. Terminal items are excluded from active reminders where appropriate but remain available as history.

## API inventory

Routes are under `api/User`.

| Area | Routes |
|---|---|
| Initiation | `InitiateEmployeeExit`, `SubmitOwnResignation` |
| Read/queue/history | `EmployeeExit/{employeeUserId}`, `EmployeeExitHistory/{employeeUserId}`, `EmployeeExitManagement/{employeeUserId}`, `EmployeeExitQueue` |
| Review/discussion | `ReviewEmployeeExit`, `RespondToEmployeeExitDiscussion`, `RespondToEmployeeExitJointDiscussion` |
| Withdrawal/cancel | `WithdrawOwnEmployeeExit`, `DecideEmployeeExitWithdrawal`, `CancelEmployeeExit` |
| Notice | `UpdateEmployeeExitDate`, `RequestEmployeeExitNoticeAdjustment`, `DecideEmployeeExitNoticeAdjustment`, `DecideOwnEmployeeExitNoticeOffer` |
| Policy/templates | `EmployeeExitPolicy...`, `EmployeeExitPolicies`, `EmployeeExitClearanceTemplate...`, `EmployeeExitClearanceTemplates` |
| Operations | `CompleteEmployeeExitClearance`, `ReviewEmployeeExitHandoverTask`, `EmployeeExitReassignment`, asset/access/finance/benefit item routes |
| Evidence | `EmployeeExitEvidenceUpload`, `VerifyEmployeeExitEvidence`, `SupersedeEmployeeExitEvidence`, evidence download |
| Settlement/documents | `CalculateEmployeeFinalSettlement`, `ApproveEmployeeFinalSettlement`, `MarkEmployeeFinalSettlementPaid`, `GenerateEmployeeExitDocument` |
| Completion/post-exit | `CompleteEmployeeExit`, `ExceptionalCompleteEmployeeExit`, `RejoinEmployee`, former-portal access routes |

## Permissions and data

Offboarding has granular CaseManage, Edit, Override, Download, and feature-owner permissions. Employee self-service is restricted to the authenticated employee and valid case state. Company-wide former-portal enablement requires administrator authorization. Services remain authoritative even when controller attributes and UI gating exist.

Primary records are `EmployeeExit`, `EmployeeExitPolicy`, `EmployeeExitClearanceTemplate`, `EmployeeFinalSettlement`, `EmployeeExitAttendanceCutoff`, `EmployeeExitOutbox`, and private evidence metadata/files. Mutations use the current version; stale clients cannot overwrite newer work.

## Implementation map

Backend: `UserController.cs`, `EmployeeService.cs`, `EmployeeExitOutboxProcessor.cs`, `EmployeeExitOutboxWorker.cs`, `PrivateEmployeeExitEvidenceStorage.cs`, `EmployeeExitDtos.cs`, `EmployeeExit.cs`, `EmployeeExitPolicy.cs`, and `EmployeeFinalSettlement.cs`.

Frontend: `ResignationAction.tsx`, `EmployeeOffboardingAction.tsx`, `EmployeeExitCaseCard.tsx`, `EmployeeExitManagementCard.tsx`, `EmployeeExitOperationsPanel.tsx`, `EmployeeExitQueueAction.tsx`, `EmployeeExitPolicySettings.tsx`, and employee `employeeServices.ts`.

## Validation scenarios

- Employee submits only their own resignation.
- HR actions require permission and same-company ownership.
- Duplicate active exits are rejected.
- Review order follows the policy snapshot.
- Withdrawal, cancellation, notice adjustment, and operational updates enforce state/version.
- Normal completion is blocked by every required incomplete workstream.
- Exceptional completion requires elevated permission and reason.
- Evidence cannot cross tenant/case boundaries.
- Settlement calculation, approval, payment, and document issuance remain separate.
- Completion closes account, applies attendance cutoff, sets FinalSettlement, and preserves history.
- Rejoin preserves historical exits and restores only approved dimensions.
- Reminder retries are deduplicated and observable.

## Limitations

Offboarding spans multiple repository writes. Versioning and outbox processing improve safety but do not make the entire workflow one MongoDB transaction. External notification/email proof depends on runtime configuration and recipients. Former-portal access must be explicitly authorized. Sensitive exports require a defined audience and retention policy. Operations need repair procedures for partial settlement, completion, and delivery failures.
