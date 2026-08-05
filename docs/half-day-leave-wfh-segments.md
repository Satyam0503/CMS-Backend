# Half-day Leave and WFH attendance segments

## Purpose

`AttendanceDaySegment` is an additive collection used only when a business day has independently owned halves. It prevents an approved half-day Leave from overwriting an approved half-day WFH request (and the reverse).

## Identity and ownership

The migration `2026-08-04-AddAttendanceDaySegmentIndexes` creates the unique key `CompanyId + UserId + Date + Segment`. `Segment` is `FIRST_HALF` or `SECOND_HALF`; the record stores status, source type/id/version, and optional WFH timing. A conflicting owner is rejected and a blocking exception is created rather than replaced.

## Flow

1. A half-day Leave request persists `HalfDayPeriod` and, after approval, reconciliation creates its `LEAVE` segment.
2. A WFH request with duration `FirstHalf` or `SecondHalf`, after approval, creates the corresponding `WFH_REQUEST` segment.
3. Each source may update or reverse only its own segment. The ordinary `AttendanceModel` remains untouched for that mixed day, preserving legacy calendar and full-day workflows.
4. Payroll and monthly attendance summaries omit the fallback daily row on any date that has segments, then sum the configured paid/unpaid fractions of all segments for that date.

## Rollout requirements

- Deploy API and migration runner together, then run the migration once before approving new half-day combinations.
- Configure the half-day Leave and WFH status codes with fractions totalling `0.5` per half; payroll deliberately uses the company status configuration.
- Test Leave-first and WFH-first approvals, same-half conflicts, reversal, and payroll calculation in an isolated Mongo replica-set environment before production rollout.

## Compatibility

Existing full-day Leave/WFH and daily attendance use `AttendanceModel` unchanged. Legacy half-day Leave without a stored period is interpreted as `FIRST_HALF` during reconciliation to avoid an ambiguous overwrite.
