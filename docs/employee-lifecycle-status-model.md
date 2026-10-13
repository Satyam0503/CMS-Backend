# Employee lifecycle status model

The lifecycle is additive. `EmployeeExit` remains the authoritative record for a completed resignation or termination; `EmpUser.Lifecycle` is the read model used to expose independent dimensions without changing legacy `Status`, `EmploymentStatus`, or exit endpoints.

## Dimensions

| Dimension | Examples | Meaning |
|---|---|---|
| Employment | PreJoining, Active, NoticePeriod, Suspended, Resigned, Terminated, Retired, ContractEnded, Deceased | Employment relationship |
| Availability | Working, AnnualLeave, SickLeave, MedicalLeave, MaternityLeave, PaternityLeave, Sabbatical, LongTermLeave, AdministrativeLeave, UnauthorizedAbsence, Suspended | Whether work is expected now |
| Account | Active, Locked, Limited, TemporarilyDisabled, Suspended, Closed | Login/security access |
| Payroll | Active, Hold, Paused, FinalSettlement, Closed | Payroll processing eligibility |
| Offboarding | None, PendingManagerApproval, PendingHrApproval, NoticePeriod, Discussion, WithdrawalPending, Completed, Withdrawn | Exit process progress |
| Former portal | NotEligible, Eligible, Invited, Active, Revoked, Expired | Post-exit portal access |
| Alumni | NotApplicable, Eligible, Invited, Member, OptedOut, Blocked | Optional post-employment relationship |

Temporary availability states never imply former-employee classification. Sabbatical keeps employment `Active` and resumes through a Resume Work operation. Absconding starts as `UnauthorizedAbsence` or `AbscondingUnderReview`; only the HR decision that creates/completes an `EmployeeExit` changes employment to a former state.

## Compatibility rules

- Existing `EmployeeExit` completion, final settlement, former portal, and rejoin APIs remain unchanged.
- `Status` remains a legacy account/activity field and is not used as the lifecycle authority.
- Directory responses expose lifecycle dimensions while older clients can continue reading `EmploymentStatus`.
- Notice-period employees remain current employees through their last working date, including combinations such as NoticePeriod plus medical leave.
- Rejoin is valid only for a completed former-employee exit. Resume Work is the operation for sabbatical and other temporary availability states.
- Lifecycle mutations must be tenant-scoped, permission-checked, effective-date validated, and version-checked; every accepted change appends an audit history record.
