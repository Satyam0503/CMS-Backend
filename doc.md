# HRMS Leave Management, Attendance, and Payroll Flow

## 1. Purpose

This document describes how Leave Management, Attendance, and Payroll work independently and together in the HRMS. It covers configuration, daily operations, monthly closing, payroll calculation, exception handling, roles, audit requirements, and recommended production controls.

The three modules form one controlled flow:

```text
Employee master and company policies
                 |
                 v
        Leave requests/approvals
                 |
                 v
 Daily attendance and approved leave reconciliation
                 |
                 v
 Attendance validation -> exception resolution -> month lock
                 |
                 v
 Salary structure + locked attendance + payroll policy
                 |
                 v
 Payroll calculation -> review -> approval -> payslip/payment
```

Payroll must not independently guess attendance or leave. It should consume an approved and locked monthly attendance summary.

---

## 2. Shared Foundation

### 2.1 Employee master

Every employee requires a stable internal user ID and a unique employee ID. The employee master should contain at least:

- Employee ID and user ID
- Full name and work email
- Company, department, job role, and reporting manager
- Joining date
- Exit date, when applicable
- Employment status
- Employment type
- Shift or attendance policy assignment
- Leave policy assignment
- Salary structure

The joining and exit dates define payroll eligibility. An employee must not receive attendance before joining or after exiting.

### 2.2 Company calendar

The calendar determines working and non-working dates:

- Weekly offs, such as Saturday and Sunday or Sunday only
- Public holidays
- Company holidays
- Emergency closures
- Optional branch/location-specific holidays

Weekly offs and holidays must not be treated as missing attendance. HR/Admin configures weekly offs and manages special holidays through the Calendar module.

### 2.3 Role responsibilities

| Role | Main responsibilities |
|---|---|
| Employee | Check in/out, view attendance, submit leave and correction requests |
| Manager | Review leave and attendance corrections for direct reports |
| HR | Configure policies, correct attendance, resolve exceptions, close attendance |
| Payroll officer | Configure salary, process payroll, verify deductions and payslips |
| Admin | Configure company-wide settings, roles, permissions, calendars, and status definitions |
| Finance approver | Approve payroll totals and authorize payment |

Sensitive actions must be permission-controlled and audited.

---

## 3. Leave Management

### 3.1 Leave policy configuration

HR/Admin defines leave types such as:

- Casual Leave (`CL`)
- Sick Leave (`SL`)
- Earned Leave (`EL`)
- Leave Without Pay (`LOP`), if supported
- Compensatory Off (`COMP-OFF`)
- Half-day leave variants

Each leave policy should define:

- Paid or unpaid classification
- Annual/monthly entitlement
- Accrual frequency
- Carry-forward rules
- Maximum balance
- Expiry rules
- Probation restrictions
- Minimum and maximum request duration
- Advance notice requirement
- Backdated request permission
- Attachment requirement
- Approval workflow
- Holiday/weekend sandwich rules
- Half-day support

### 3.2 Leave balance lifecycle

```text
Opening balance
   + periodic accrual
   + manual adjustment
   + approved carry-forward
   - approved leave usage
   - expired balance
   = available balance
```

Pending leave may be reserved so employees cannot submit overlapping requests beyond their balance. Rejected or cancelled requests release the reserved balance.

### 3.3 Leave request flow

1. Employee selects leave type, dates, half/full day, and reason.
2. System validates employment dates, overlap, balance, holidays, weekly offs, and policy rules.
3. Request enters `PENDING` status.
4. Assigned manager/HR receives a notification.
5. Approver selects `APPROVED`, `REJECTED`, or requests clarification.
6. On approval, the balance is deducted/reserved permanently.
7. Approved dates become inputs to Attendance.
8. Employee receives the result notification.

Recommended request states:

```text
DRAFT -> PENDING -> APPROVED
                 -> REJECTED
APPROVED -> CANCEL_REQUESTED -> CANCELLED
```

### 3.4 Leave and attendance reconciliation

Approved leave is authoritative for the approved period but should not silently overwrite physical attendance without a rule. Examples:

- Approved full-day `CL` with no attendance: mark `CL`.
- Approved half-day leave with half-day work: mark `CL-HALF` or `SL-HALF` with valid work time.
- Approved leave but employee works a full day: flag a conflict for HR review.
- Attendance marked absent before leave approval: update/reconcile after approval.
- Leave is cancelled after attendance close: require attendance unlock/reopen approval.

Payroll must use only approved leave. Pending or rejected leave must not create paid leave days.

---

## 4. Attendance Management

### 4.1 Attendance status configuration

Default statuses supported by the current flow include:

| Code | Meaning | Typical paid fraction | Typical unpaid fraction | Time required |
|---|---|---:|---:|---|
| `P` | Present | 1 | 0 | Yes |
| `A` | Absent | 0 | 1 | No |
| `SL` | Sick Leave | 1 | 0 | No |
| `CL` | Casual Leave | 1 | 0 | No |
| `EL` | Earned Leave | 1 | 0 | No |
| `WFH` | Work From Home | 1 | 0 | Yes |
| `HD` | Half Day | 0.5 | 0.5 | Yes |
| `ED` | Early Departure | Policy-based | Policy-based | Yes |
| `LHD` | Late Arrival–Half Day | 0.5 | 0.5 | Yes |
| `WFH+WFO` | Half WFH and half office | 1 | 0 | Yes |
| `COMP-OFF` | Compensatory Off | 1 | 0 | No |
| `CL-HALF` | Casual Leave Half Day | 0.5 | Policy-based | No/work segment optional |
| `SL-HALF` | Sick Leave Half Day | 0.5 | Policy-based | No/work segment optional |
| `WFH-HD` | WFH with Half Day | 0.5 | 0.5 | Yes |

Admin can activate/deactivate statuses and add company-specific statuses. A status used by historical locked attendance must not be physically deleted; it should be made inactive.

### 4.2 Attendance capture methods

Production attendance may come from:

- Employee web/mobile check-in and checkout
- Biometric device integration
- Access-control integration
- HR bulk import
- HR manual marking
- Approved leave synchronization
- Approved attendance correction

Every employee/date must have at most one effective attendance record. The database and service must enforce uniqueness and idempotent updates.

### 4.3 Daily attendance flow

1. Determine whether the date is an eligible working day.
2. Employee checks in or the device sends check-in.
3. Store date, employee, source, time, location/device metadata, and audit data.
4. Employee checks out or the device sends checkout.
5. Calculate gross duration, break deduction, and net hours.
6. Compare time with assigned shift and grace rules.
7. Determine late arrival, early departure, half-day, or overtime indicators.
8. Reconcile approved leave and company calendar.
9. Save the effective attendance status.
10. Show unresolved problems to employee/manager/HR.

### 4.4 Check-in/check-out rules

For time-required statuses, missing time is an incomplete record:

- Missing check-in: create a missing-check-in exception when supported.
- Missing checkout: create `MISSING_CHECKOUT`.
- Checkout before check-in: create `INVALID_TIME_ORDER`.
- Unknown/inactive status: create `UNKNOWN_OR_INACTIVE_STATUS`.

The system must not silently assume a checkout time in production. The employee should submit a correction request, and a manager/HR should approve it with a reason. Test-data scripts may use fixed times, but that behavior must remain isolated from production.

### 4.5 Manual and bulk attendance

HR may:

- Click an employee/date cell for individual marking.
- Select employees and apply a status for one date.
- Apply a status to all eligible employees for one date.

The system must reject marking on:

- Weekly offs, unless an approved working-weekend flow exists
- Holidays, unless holiday work is explicitly supported
- Dates before joining
- Dates after exit
- Locked months
- Future dates, except planned statuses where explicitly allowed

Bulk operations must be atomic or return clear row-level results. Repeating the same operation must update the existing record, not create a duplicate.

### 4.6 Attendance correction flow

```text
Exception detected
      |
      v
Employee/HR opens exact employee and affected date
      |
      v
Correct status or check-in/out + provide reason
      |
      v
Manager/HR approval when required
      |
      v
Recheck exception
      |
      v
RESOLVED or remains PENDING_REVIEW
```

The audit log should capture old values, new values, reason, actor, approver, date, and source.

### 4.7 Monthly exceptions

Structural blocking exceptions include:

- `MISSING_ATTENDANCE`
- `MISSING_CHECKOUT`
- `DUPLICATE_ATTENDANCE`
- `INVALID_TIME_ORDER`
- `UNKNOWN_OR_INACTIVE_STATUS`

Policy exceptions include limits such as combined `LHD + ED` occurrences.

Structural exceptions cannot be waived merely by changing their status. The underlying attendance must be corrected. Policy exceptions may support audited HR decisions:

- Waive
- Warning only
- Half-day LOP
- Full-day LOP

The Attendance page provides month selection, status filtering, affected dates, direct Edit navigation, Recheck, resolution of all corrected issues, and bulk LHD/ED decisions.

### 4.8 Current-month behavior

The current month remains open for daily attendance. It must not be locked before month-end. The UI should show a concise notice such as:

> July 2026 is still in progress. Lock attendance after July 31.

Future dates must not be reported as missing attendance. Payroll locking is for completed months only.

### 4.9 Attendance validation and lock

After month-end, HR performs:

1. Select the completed payroll month.
2. Recalculate exceptions.
3. Correct structural exceptions.
4. Review penalty exceptions.
5. Confirm approved leaves are synchronized.
6. Validate expected working days against recorded days.
7. Generate monthly attendance summaries.
8. Lock the month.

A summary should contain:

- Eligible period
- Expected/eligible working days
- Present days
- WFH days
- Paid and unpaid leave days
- Half days
- Absent days
- Missing attendance/checkout counts
- LHD and ED counts
- Paid days and unpaid days
- Lock and approval audit fields

Once locked, ordinary users and HR cannot edit the month. Reopening requires elevated permission, reason, audit history, downstream payroll impact checks, and payroll recalculation.

The lock is company- and month-specific. It is successful only when every active employee who is eligible during the selected month has an approved and locked monthly attendance summary. A partial lock must not enable payroll.

The current implementation exposes the persisted state through:

```text
GET /api/attendance/penalty/month/lock-status?month=YYYY-MM-01
```

The response includes the selected payroll month, whether it is fully locked, the eligible employee count, and the locked employee count. Current and future months always return an unlocked state because they are still open for attendance entry.

---

## 5. Payroll Management

### 5.1 Salary structure

Each eligible employee needs an effective-dated salary structure. Current UI components include:

- Basic Pay
- HRA (House Rent Allowance)
- LTA (Leave Travel Allowance)
- Other Allowance
- Bonus
- Health Insurance
- Gross Salary

Production payroll may additionally include special allowance, overtime, incentives, provident fund, ESI, professional tax, income tax/TDS, loans, reimbursements, arrears, and other deductions.

Salary structures must be versioned by effective date. Payroll for June must use the structure effective during June, not a later edited value.

### 5.2 Payroll eligibility

An employee is eligible when:

- Active/employed during at least one day in the payroll month
- Joining date is on or before month-end
- Exit date is absent or on/after month-start
- A valid salary structure exists
- Attendance for the eligible period is locked

The employee may appear in Payroll Settings because a salary exists while still being ineligible for a selected historical month due to joining date.

### 5.3 Payroll divisor

Payroll currently uses the actual number of calendar days in the selected month automatically. HR does not manually select or save a divisor in Payroll Settings.

Examples:

- February 2026: 28 calendar days
- June 2026: 30 calendar days
- July 2026: 31 calendar days

Weekly offs and company holidays remain paid/non-working attendance days according to company policy, but they do not change the calendar-day divisor.

Example daily rate:

```text
Daily gross rate = Monthly gross salary / Payroll divisor
LOP deduction = Daily gross rate x approved unpaid-day fraction
Payable gross = Prorated gross - LOP deduction + eligible additions
Net pay = Payable gross + reimbursements - statutory and other deductions
```

Joining and exit dates require proration for the eligible portion of the month.

### 5.4 Payroll generation flow

1. Select a completed payroll month.
2. Verify salary structures for all eligible employees.
3. Recalculate and resolve all attendance exceptions.
4. Validate and lock attendance.
5. Confirm the persisted lock status covers every eligible employee.
6. Enable Process Salary only after the complete lock is confirmed.
7. Load locked monthly attendance summaries.
8. Calculate eligible and paid/unpaid days using the month's calendar-day divisor.
9. Apply approved LOP decisions.
10. Prorate salary for joiners/exits.
11. Calculate earnings and deductions.
12. Generate payroll records.
13. Run variance and compliance checks.
14. HR/Payroll reviews exceptions.
15. Finance approves payroll.
16. Generate payslips and bank/payment output.
17. Publish payslips and mark payment status.

Payroll processing must be idempotent. Repeated processing should update/recalculate a draft payroll or reject an already finalized payroll, not create duplicates.

### 5.5 Payroll statuses

Recommended states:

```text
DRAFT -> CALCULATED -> REVIEWED -> APPROVED -> PAID
   |          |
   +------> RECALCULATION_REQUIRED
APPROVED/PAID -> REVERSAL_REQUIRED (controlled workflow only)
```

### 5.6 Payroll validation

Block processing when:

- Attendance month is not locked
- Structural attendance exceptions remain
- Required LHD/ED decisions remain pending
- Salary structure is missing or invalid
- Duplicate payroll already exists
- Employee eligibility dates are invalid
- Paid/unpaid totals exceed eligible days
- Negative or inconsistent payroll values occur

Warnings may be used for unusual but permitted changes, such as a large month-over-month variance.

### 5.7 Payroll Settings button behavior

For each selected process month:

1. The page resets the local lock state and requests the persisted month lock status.
2. Process Salary remains disabled while that request is loading.
3. Process Salary remains disabled when attendance is unlocked, partially locked, or validation failed.
4. Validate & Lock Attendance performs the complete server-side validation and lock operation.
5. A successful lock immediately enables Process Salary.
6. A failed lock keeps Process Salary disabled and directs HR/Admin to correct attendance exceptions.
7. Changing the month repeats the server check; a lock from one month never enables another month.
8. Salary processing also performs a defensive lock check so the rule cannot be bypassed through the UI.

For the current or a future month, attendance validation and locking are rejected and Process Salary remains disabled. For example, while July 2026 is in progress, June 2026 may be processed after it is fully locked, but July 2026 cannot yet be processed.

---

## 6. End-to-End Example

Assume an employee has a monthly gross salary of ₹60,000 and June uses 30 calendar days.

1. The employee is eligible for all 30 calendar days.
2. Saturdays and Sundays are configured weekly offs.
3. The employee records normal attendance on working days.
4. One approved paid sick-leave day becomes `SL`.
5. One unapproved absence becomes `A` with one unpaid day.
6. The employee has three combined LHD/ED occurrences while the limit is two.
7. HR reviews the exceeded occurrence and approves half-day LOP.
8. Attendance validation produces paid/unpaid fractions and locks June.
9. Daily gross rate is ₹60,000 / 30 = ₹2,000.
10. Total LOP is 1 absent day + 0.5 approved penalty day = 1.5 days.
11. LOP deduction is ₹2,000 x 1.5 = ₹3,000.
12. Payable gross before other adjustments is ₹57,000.
13. Payroll applies statutory and other deductions, then produces net pay.

Every amount should reference the salary version, attendance summary, exception decision, and divisor policy used.

---

## 7. Notifications

Notifications should be concise and actionable:

- Leave request submitted, approved, rejected, or cancelled
- Missing checkout reminder after shift end
- Attendance correction approved/rejected
- Manager pending approval reminder
- Month-end unresolved exception count for HR
- Current month cannot yet be locked
- Attendance month successfully locked
- Payroll ready for review/approval
- Payslip published

Large employee-level error lists should not appear in toast notifications. A toast should show a summary and link the user to the detailed exception page.

---

## 8. Audit, Security, and Data Integrity

Production requirements include:

- Role-based permissions for marking, approving, locking, reopening, and payroll processing
- Unique attendance per effective employee/date
- Unique payroll per company/employee/month
- Effective-dated policies and salary structures
- Immutable audit history for attendance and financial decisions
- Optimistic concurrency/version checks on reviews
- No hard deletion of locked financial records
- Tenant/company isolation on every query
- Secure exports and payslip access
- Time-zone-consistent date handling
- Background job idempotency
- Database backup and recovery testing

---

## 9. Recommended Production Checklist

### Before daily use

- Configure company time zone, shifts, weekly offs, and holidays.
- Configure attendance statuses and time requirements.
- Configure leave policies and balances.
- Assign joining dates, managers, policies, and salary structures.

### Before attendance lock

- Approve/reject all relevant leave requests.
- Resolve missing attendance and time exceptions.
- Remove/consolidate duplicates.
- Review LHD/ED penalties.
- Confirm joiners and exits.
- Verify paid plus unpaid days do not exceed eligible days.

### Before payroll approval

- Confirm attendance is locked.
- Confirm salary versions and the automatically selected calendar-day divisor.
- Review joiner/exit proration.
- Review LOP, bonuses, arrears, and deductions.
- Compare payroll totals with the prior month.
- Obtain HR and Finance approval.

### After payroll

- Generate payslips and bank output.
- Record approval and payment references.
- Restrict edits to controlled reversal/recalculation workflows.
- Retain audit evidence and reports.

---

## 10. Final Operating Principle

Leave explains approved absence. Attendance records what happened on each eligible day. The locked attendance summary converts daily outcomes into paid and unpaid fractions. Payroll applies those fractions to an effective salary structure under an effective payroll policy.

The safe production sequence is:

```text
Configure -> Capture -> Approve -> Reconcile -> Validate -> Resolve -> Lock -> Calculate -> Review -> Approve -> Pay
```

Skipping reconciliation, exception resolution, or locking creates incorrect salary risk and must be prevented by both the UI and backend.
