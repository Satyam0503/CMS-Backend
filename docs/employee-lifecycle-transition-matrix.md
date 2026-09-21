# Employee lifecycle transition matrix

| Operation | Employment | Availability | EmployeeExit | Former employee | Rejoin |
|---|---|---|---|---|---|
| Start normal work | Active | Working | None | No | No |
| Approved annual/sick/medical leave | Active or NoticePeriod | Leave-specific | Unchanged | No | No |
| Sabbatical | Active | Sabbatical | Unchanged | No | No; use Resume Work |
| Suspension | Suspended | Suspended | Unchanged | No | No |
| Unauthorized absence review | Active or NoticePeriod | UnauthorizedAbsence / AbscondingUnderReview | Not created yet | No | No |
| HR decides absconding exit | Terminated | Working or policy-defined | Created/completed with Absconding reason | Yes after completion | Yes only if eligible |
| Resignation approved | NoticePeriod | Working or temporary leave | Active EmployeeExit | No until completion/LWD | No |
| Exit completed | Resigned or Terminated | N/A | Authoritative completed exit | Yes | Yes only through completed exit |
| Resume work | Active | Working | Unchanged | No | No |
| Rejoin | Active | Working | Prior exit closed/rejoined | No | N/A |

## Invariants

- Availability changes never create an exit.
- Temporary leave, sabbatical, suspension, and absconding review never enter former-employee results.
- Completed exits remain authoritative for former portal access and rejoin eligibility.
- Account lock/disablement does not change employment classification.
- Payroll may include the exit month, but lifecycle-closed employees are excluded from future months.
- All lifecycle mutations are tenant-scoped, permission-checked, version-checked, and audited.
