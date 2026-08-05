# Employee-specific leave allocation

`EmployeeLeaveBalance` is the authoritative per-company, per-user, per-policy entitlement record. Its values are now explicit:

- `TotalAllocated`: entitlement granted in the current cycle.
- `Taken`: approved leave consumed in that cycle.
- `Remaining`: leave available for a request or approval.

The invariant is `TotalAllocated = Taken + Remaining`. Pending and rejected-pending requests do not alter it. Approval increments `Taken` and decrements `Remaining`; an accepted-to-rejected reversal performs the exact inverse. Both use the existing request/version compare-and-set transaction; standalone MongoDB uses the same compare-and-set update and compensation.

## Allocation and accrual

HR sends `TotalAllocated` when creating or editing an assignment. The service rejects totals below `Taken`, negative totals, inactive/cross-company/WFH policies, and stale `ExpectedVersion` values. During the compatibility period the old request `Balance` field is accepted as a remaining-leave input and responses include deprecated `Balance`/`UsedBalance` aliases.

Monthly accrual adds the actual credit to `TotalAllocated` and `Remaining`, respecting `MaxBalance`; zero-credit months still set `LastAccrual`. Yearly accrual resets `Taken`, calculates carry-forward from `Remaining`, and sets `TotalAllocated` and `Remaining` to the new yearly entitlement. Work From Home never receives an `EmployeeLeaveBalance`.

## Migration and deployment

Run `Codeji.CMS.Migrations` before deploying the API. `MigrateEmployeeLeaveBalanceAllocationFields` is idempotent: it maps legacy `Balance`/`UsedBalance` to `Remaining`/`Taken` and sets `TotalAllocated = Balance + UsedBalance`, preserves legacy fields, writes its migration marker, and verifies the unique `CompanyId + UserId + LeavePolicyId` index. Set `LEAVE_BALANCE_MIGRATION_DRY_RUN=true` to log counts without writes.

Deploy order: migration dry-run, migration, API, frontend. Roll back by deploying the prior API while legacy fields remain; do not remove those legacy fields until all clients have moved to the explicit contract and a separate cleanup migration is approved.
