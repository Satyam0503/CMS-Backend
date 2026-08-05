# Calendar / Holidays Module — Current Flow

> Status: Current read-only reference for the calendar module, holiday storage, employee birthday/anniversary enrichment, and its downstream use by attendance and payroll.

## 1. What this module owns

The calendar module is the company-level source of truth for:

- one-off calendar events;
- recurring holidays;
- holiday images / cover media;
- calendar display data used by the attendance UI;
- employee birthdays and work anniversaries, which are computed at read time and merged into the response.

It does not own employee master data or payroll rules, but it feeds both.

## 2. Core files

| Area | File |
|---|---|
| Controller | [`Codeji.CMS.API/Controllers/CalendarController.cs`](../Codeji.CMS.API/Controllers/CalendarController.cs) |
| Service | [`Codeji.CMS.Services/Calendar/CalendarServices.cs`](../Codeji.CMS.Services/Calendar/CalendarServices.cs) |
| Entity | [`Codeji.CMS.Repository/Entities/Calendar/CalendarEntity.cs`](../Codeji.CMS.Repository/Entities/Calendar/CalendarEntity.cs) |
| Holiday entity | [`Codeji.CMS.Repository/Entities/Holiday/Holidays.cs`](../Codeji.CMS.Repository/Entities/Holiday/Holidays.cs) |
| DTOs | [`Codeji.CMS.DTO/Calendar/`](../Codeji.CMS.DTO/Calendar/) |

## 3. Permission model

The controller uses the calendar module permissions from the shared module-action matrix:

- `Calendar.View`
- `Calendar.Create`
- `Calendar.Edit`
- `Calendar.Delete`

The create/update endpoint accepts either create or edit permission because it behaves as an upsert.

## 4. Primary data flow

### 4.1 Read calendar items

`POST /api/calendar/GetAllCalendarItems`

Flow:

1. The caller sends optional filters:
   - `Year`
   - `Type`
2. The service loads calendar items from the company collection.
3. If the item is recurring, the response date is rewritten into the requested year.
4. The service also loads employees with a date of birth or joining date.
5. Birthdays and work anniversaries are projected into the same response model.
6. The final response is a merged list of:
   - holidays
   - events
   - birthdays
   - work anniversaries

### 4.2 Add or update calendar item

`POST /api/calendar/AddUpdateCalendarItem`

Flow:

1. If `Id` is missing, a new calendar item is inserted.
2. If `Image` is present, the file is saved under `Uploads/CalendarItemCoverPictures`.
3. If `Id` is present, the existing record is loaded and replaced.
4. If a new image is uploaded during update, the old image is deleted first.
5. If the update removes the image, the stored file is deleted.

### 4.3 Delete calendar item

`DELETE /api/calendar/DeleteCalendarItem/{itemId}`

Flow:

1. Load the item by ID.
2. Delete the associated image from disk if present.
3. Delete the MongoDB document.

### 4.4 Fetch holidays

`POST /api/calendar/GetHolidays`

Flow:

1. Validate that `FromDate <= ToDate`.
2. Return holiday data in the requested window.

## 5. Important business rules

- Recurring holidays are re-emitted for the requested year rather than stored once per year.
- Employee birthdays and work anniversaries are derived from employee profile fields, not from the calendar collection.
- `DateOfBirth` and `DateOfJoining` must parse in `yyyy-MM-dd` format for those derived entries to appear.
- Invalid date ranges are rejected early in the controller.

## 6. Connections to other modules

### Attendance

Attendance uses calendar holidays when determining:

- expected working days;
- missing-attendance calculations;
- lock validation for monthly summaries;
- leave/holiday context in the attendance calendar.

### Payroll

Payroll month-close logic uses calendar holidays when deciding:

- eligible working days;
- exception counts;
- whether a month can be locked and processed.

### Employees

Birthday and work-anniversary items are derived from employee records, so employee profile completeness directly affects calendar output.

## 7. Risks and watch-outs

- Calendar item images are written to disk, so file cleanup matters on updates and deletes.
- If employee date strings are malformed, birthdays/anniversaries silently disappear from the merged list.
- The module has no separate tenant-neutral cache; it reads directly from the current company context.

## 8. Detailed date semantics and downstream contract

### Tenant and recurrence behaviour

`CalendarEntity` is stored per company through the repository current-company filter. Calendar requests never select a tenant. A recurring item retains its original month/day in storage; reads project that month/day into the requested year. `GetHolidays` expands a recurring holiday once for every year touched by the requested range, then applies the inclusive range filter. A one-off item is returned only when its persisted timestamp is in range.

Use a recurring record only for a fixed annual month/day. Dates that move each year must be separate non-recurring records.

### Calendar versus attendance and leave

The Calendar module declares company holidays; it does not mark an employee present, absent, on leave, or paid.

| Consumer | Uses the holiday for | Does not do |
|---|---|---|
| `CompanyWorkingCalendarService` | Excluding a date from leave duration unless the policy is holiday-inclusive | Create leave/attendance |
| Attendance edit guard | Blocking manual marking on a holiday | Override a leave/WFH source row |
| Leave reconciliation | Including a date only for a holiday-inclusive policy, then creating source-owned attendance | Treat every holiday as leave |
| Monthly attendance/Payroll | Expected-working-day and validation inputs | Use the frontend calendar as a financial source |
| Profile attendance calendar | Context only; it displays persisted attendance | Colour a day merely because it is a holiday |

Weekly offs are company working-calendar configuration, not CalendarEntity records. Leave policies have separate `WeekendInclusive` and `HolidayInclusive` settings. Calendar/weekly-off changes should be controlled carefully around a payroll-active month.

### Birthdays, anniversaries, and files

Birthdays and work anniversaries are response-only projections from `EmpUser`, not saved calendar items. The service parses `DateOfBirth` and `DateOfJoining` as `yyyy-MM-dd`, projects month/day into the requested year, and skips invalid values. Updating a profile date changes the next calendar read; deleting a calendar item does not remove an employee milestone.

Cover images are stored as files under `Uploads/CalendarItemCoverPictures`, with a generated filename in MongoDB. Create saves the file then the item. A replacement deletes the old file, while an image-less update/delete removes the stored file. Multi-instance deployments therefore need shared persistent upload storage or object storage.

### Permission note and verification

Add/update/delete use `Calendar.Create/Edit/Delete`. Read endpoints are authenticated but currently have no explicit `Calendar.View` attribute in `CalendarController`; that is a source fact to review before assuming View-permission-only enforcement.

- Cross-company calendar/holiday reads must never return another company's items or employee milestones.
- A range spanning two years must contain one projected instance of a recurring holiday for each matching year.
- An attendance profile month must remain uncoloured on a holiday until a daily attendance row exists.
- Verify returned image URL and the configured upload-volume lifecycle after replacement/deletion.

## 9. Related documentation

- [Employee profile and attendance calendar](employee-profile-current-flow.md)
- [Leave management](leave-management-current-flow.md)
- [Attendance module](attendance-module-current-flow-and-audit.md)
