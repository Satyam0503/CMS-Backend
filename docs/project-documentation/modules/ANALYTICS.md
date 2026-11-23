# Dashboard analytics

**Contract version:** `dashboard-overview.v4` (attendance definition: `attendance-breakdown.v1`)\
**Current date convention:** `Asia/Kolkata`\
**Primary route:** `GET /api/Dashboard/GetDashboardOverview`

## Current product behavior

The overview is assembled from live operational records when the endpoint is called. It is a tenant-scoped summary for users who pass authentication and the `Dashboard.ViewAll` module permission. The dashboard UI displays the company overview to administrators, HR managers and HR executives, or users with the dashboard `ViewAll` permission. Other users see the dashboard's personal/notice-board experience and do not request this overview.

This route does not provide a general report builder, arbitrary date filters, CSV export, saved views, group-company roll-up, payroll cost reporting or a manager-specific team aggregate. Recruiting analytics and payroll reports remain in their respective modules.

The recruitment analytics `GET` is also read-only. When a canonical application is absent, it falls back to the applicant profile's current stage for its counts; repairing that data is a separate write workflow.

## Response metadata

The response includes the fields below so a caller can identify the definition and the time at which the answer was calculated.

| Field | Meaning |
|---|---|
| `definitionVersion` | Version of the current overview metric rules (`dashboard-overview.v4`). |
| `asOfDate` | Local calendar date used for today's workforce, attendance and current-month trend metrics. |
| `timeZone` | The application date convention used by this endpoint (`Asia/Kolkata`). |
| `generatedAtUtc` | UTC time at which the service assembled the response. This is a query time, not a source-system watermark or a guarantee of source completeness. |

The definition version must change when a shipped metric's population, formula, date boundary or interpretation changes. The UI accepts older responses without metadata for backward compatibility.

The React boundary validates scalar metric counts as non-negative integers before presenting them; malformed, fractional, negative, or non-finite counts normalize to zero. Percentage changes may be fractional but must be finite; invalid values normalize to `null`. This is a defensive client contract and does not replace server-side calculation and response validation.

The dashboard labels `generatedAtUtc` as response-generation time and discloses that source update times are not included in the report. No per-source watermark or completeness state is currently returned, so a recent response timestamp must not be read as proof that every source record is current.

### Source module availability

`sourceAvailability` reports whether each company-level source module (Employees, Attendance, Leave Management, Jobs and Offboarding) is enabled. This is a data-availability signal, not a user permission or subscription entitlement; endpoint authentication and per-user authorization still apply separately. The server skips source-domain queries when their module is disabled and returns empty/zero values for dependent analytics. The attendance composition is omitted (`null`) unless Employees, Attendance and Leave Management are all enabled, because those sources are required to interpret its buckets. Clients that receive a legacy response without `sourceAvailability` retain the historical display behavior; a present but malformed availability object fails closed in the React dashboard.

### Query failure behavior

The overview service does not convert repository, calendar, or source-query failures into a successful response with zero metrics. Outside debug mode, exceptions reach the API exception handler, which returns the established error envelope and HTTP status (including `504` for a timeout and a generic `500` message for unexpected failures). Error responses include an `X-Correlation-ID` response header; the same request trace ID is written with the server error log so operators can locate the failure without exposing connection or query details to the client. The client should treat an error response as unavailable analytics and offer its normal retry path, never as an empty workforce result.

## Metric definitions

| Metric | Definition in `dashboard-overview.v4` | Date and population rules |
|---|---|---|
| `totalEmployees` | Count of company employee records that satisfy the current-employee rule and `EmploymentPeriod.WasEmployedOn` on the as-of date. | `Status` must be true. A missing lifecycle or lifecycle status, `Active`, `NoticePeriod` and `Suspended` are included. Future joiners are excluded. |
| `totalEmployeesChangePercent` | `(current as-of headcount - previous month-end headcount) / previous month-end headcount × 100`, rounded to one decimal place. | `null` when previous month-end headcount is zero. |
| `presentToday` | Distinct current-employee IDs with a same-day attendance record that has a check-in time or a recognized worked status (`P`, `WFH`, `WFH+WFO`, `WFH-HD`, `LHD`, `LHD+ED`, `ED`, `WFH+SL`, `WFH+CL`). Plain `HD` is not proof of work because the default status does not require time and is leave-eligible. | Attendance date is bounded by local midnight and the next local midnight. |
| `onLeaveToday` | Distinct current-employee IDs with an accepted leave request overlapping the as-of date. | The leave interval uses inclusive `StartDate`/`EndDate` date comparisons. |
| `absentToday` | Current employees expected to attend who have no recognized presence and no accepted overlapping leave. This legacy KPI includes employees with no attendance row and should be read as a missing-presence count, not an explicit `A` status count. | Zero on a non-working day. Attendance expectation uses employment dates and existing `EmploymentPeriod` rules. |
| `attendanceBreakdown` | Mutually exclusive buckets for expected employee-days: `presentOnly`, `leaveOnly`, `presentAndOnLeave`, `explicitlyAbsent`, `otherStatus`, and `unreported`. | Versioned independently as `attendance-breakdown.v1`; all buckets sum to `expectedEmployees`. A company-calendar non-working date has an expected denominator of zero. Employee-specific shift/weekly-off overrides are not incorporated yet. Only `A` is classified as explicitly absent; an unknown/custom status remains `otherStatus`. |
| `isWorkingDay` | Result from the tenant working-calendar service for the as-of date. | Calendar service owns weekend, holiday and company schedule rules. |
| `openPositions` | Count of non-deleted company vacancies whose `Status` is true. | Soft-deleted vacancies are excluded; this remains an active vacancy count and does not inspect applicant deadline/expiry dates. |
| `headcountTrend` | Twelve monthly values, oldest first: eleven closed month-end snapshots and the current partial month at the as-of date. Closed months use `EmploymentPeriod.WasEmployedOn`; the current month uses the as-of date plus the `totalEmployees` current-employee rule. | The current month value reconciles to `totalEmployees`. The dashboard labels it partial through the response `asOfDate`; it must not be compared as a completed month. |
| `workforceMovementTrend` | Twelve monthly event counts, oldest first, for initial hires, explicit rejoin events, and completed departures. `netChange` is hires minus exits. | Initial hires use a parseable `DateOfJoining` (records without it are omitted; account creation is not treated as a hire), rehires use lifecycle history with `ReasonCode=REJOIN`, and exits use completed `EmployeeExit` records on `LastWorkingDate`. The current month is partial through `asOfDate`. Historical events outside the retained employee/lifecycle/exit records cannot be recovered. |
| `leaveRequestCohortTrend` | Twelve submission-month cohorts with total request count and current status counts (`pending`, `approved`, `rejected`, `withdrawn`, `otherStatus`). | Cohort month is the India-local month of `CreatedDate`. Status is the record's current status at query time, not an immutable decision event; changing a request later moves it between status buckets in the original cohort. It is request workload, not approved leave taken, leave days, or utilization. Requests without `CreatedDate` are omitted. |
| `recentActivities` | Up to eight events, newest first; the UI renders up to five. | Event dates are limited to the last 30 calendar days including the as-of date. Future-dated events and older events are excluded. |

### Attendance interpretation limit

The legacy KPI fields remain employee counts, not scheduled-hour equivalents. `presentToday` and `onLeaveToday` can overlap, and `absentToday` still combines explicit absence with employees who have no attendance row. The new breakdown exists specifically for composition charts: it exposes overlap and separates explicitly absent, other-status, and unreported employees, while its categories reconcile to the expected employee count. It does not claim a scheduled-hours denominator and does not fractionally allocate half-day leave.

## Activity feed sources

The feed combines recent employee joining dates, approved leave reviews, initiated exits and created vacancies. The API and client normalization allow only the documented `joined`, `leaveApproved`, `exitInitiated` and `jobPosted` event types; unknown types are omitted. Compensation changes are omitted because this feed is protected by the broader `Dashboard.ViewAll` permission; salary history remains in payroll-protected workflows. Joining dates are synthesized as UTC midnight from the stored local date. Other event timestamps use their stored timestamps. A missing name/title/policy is shown as an empty detail rather than failing the entire overview.

## Data access and performance

The service applies the current company ID to each repository query and cross-checks attendance and leave employee IDs against that company's employee set. Dashboard authorization is enforced on the API, not only by the React visibility check. Tests cover tenant isolation and dashboard permission cases.

The service now requests explicit employee, attendance, leave-request, recent-exit, vacancy activity, workforce-movement exit and activity-lookup projections. Employee fields are limited to headcount, attendance eligibility, movement events and recent-activity labels; credentials, contact, tax, banking and custom profile fields are excluded, and rejoin history is reduced to required event fields. The current attendance query selects only employee ID, check-in time and status; remarks, source data and operator/audit metadata are excluded. Leave queries select only employee/policy IDs, current status, relevant period dates, cohort date and review timestamp; request reasons, comments, evidence, assigned reviewer and reconciliation details are excluded. Exit activity selects only employee ID, exit type and initiation time; case reasons, notes, discussion, evidence, clearances, finance, and exit documents are excluded. Recent job activity selects only creator ID, vacancy title and creation date; descriptions, salary ranges, recruiter contact details, screening criteria and other recruiting details are excluded. Workforce movement exit reads select only status, effective last-working date, completion timestamp and deletion state; employee identity and case data are excluded. Activity labels use only job-title IDs/localized labels and leave-policy IDs/names, excluding unused job metadata and policy configuration. Calculations and joins still run in memory. Rendered Mongo projection contract tests validate projection documents but are not Mongo integration or query-plan evidence. No query-plan, transfer-volume, 10,000-employee or concurrency benchmark has been completed, so do not treat the endpoint as scale-validated or as an enterprise analytics warehouse.

The separate recruitment analytics endpoint also uses narrow reads: applicant ID, activity stage, creation date and merge/deletion state; canonical application ID, current stage and update date; and accepted offer applicant ID and response time. Candidate contact/resume fields, application history/snapshots, and offer compensation/approval data are excluded from its funnel and time-to-hire calculations. This reduces fields loaded but does not validate database query plans or establish a supported recruitment-report scale.

## Implementation references

- API: `Codeji.CMS.API/Controllers/DashboardController.cs`
- Calculation: `Codeji.CMS.Services/Dashboard/DashboardOverviewService.cs`
- Contract: `Codeji.CMS.DTO/Dashboard/DashboardOverviewDto.cs`
- React normalization and view: `CMS-React/src/app/modules/dashboard/`
- Test coverage: `Codeji.CMS.Services.Tests/DashboardOverviewServiceTests.cs`, `Codeji.CMS.Services.Tests/DashboardOverviewProjectionTests.cs`, `Codeji.CMS.Services.Tests/DashboardSecurityAndTenantIsolationTests.cs`, API contract tests, and dashboard Vitest tests.

## Next production gates

1. Agree a scheduled-hours attendance definition before reporting fractional present/leave/absence shares; the current composition chart is an employee-count view with explicit overlap.
2. Move expensive overview calculation to bounded projections/aggregation after measuring query plans and indexes.
3. Make date/timezone configuration tenant-specific before supporting tenants outside the current date convention.
4. Add documented freshness/completeness states based on source watermarks and failure behavior.
5. Deliver standard workforce, attendance, leave and recruiting reports behind a server-side scope/entitlement matrix, with export tests.
6. Complete desktop, tablet, mobile, accessibility, performance, backup/recovery and staged-rollout evidence before declaring the analytics program production-ready.
