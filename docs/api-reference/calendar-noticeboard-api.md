# Calendar and Notice Board APIs

## Controllers
- Source: [Codeji.CMS.API/Controllers/CalendarController.cs](../Codeji.CMS.API/Controllers/CalendarController.cs)
- Source: [Codeji.CMS.API/Controllers/NoticeBoardController.cs](../Codeji.CMS.API/Controllers/NoticeBoardController.cs)

## Overview
These endpoints support company calendar items, holidays, and internal notices.

## Endpoint map

### Calendar and holiday APIs
- POST /api/calendar/GetAllCalendarItems
  - Purpose: retrieves the company calendar.

- POST /api/calendar/AddUpdateCalendarItem
  - Purpose: creates or updates a calendar item.
  - Permission: Calendar.Create/Edit.

- DELETE /api/calendar/DeleteCalendarItem/{itemId}
  - Purpose: removes a calendar item.
  - Permission: Calendar.Delete.

- POST /api/calendar/GetHolidays
  - Purpose: returns holiday data for the requested range.

### Notice board APIs
- POST /api/noticeboard/PostNotice
  - Purpose: publishes a notice.
  - Permission: Notice_Board.Create.

- POST /api/noticeboard/GetAllNotice
  - Purpose: returns paged or filtered notice data.
  - Permission: Notice_Board.View.

- GET /api/noticeboard/GetNoticeById/{noticeId}
  - Purpose: gets a single notice.

- GET /api/noticeboard/GetMyNotices
  - Purpose: lists notices visible to the current user.

- PUT /api/noticeboard/UpdateMyNotice
  - Purpose: updates a notice.
  - Permission: Notice_Board.Edit.

- DELETE /api/noticeboard/DeleteNotice/{noticeId}
  - Purpose: deletes a notice.
  - Permission: Notice_Board.Delete.

## Service dependencies
- ICalendarServices
- INoticeBoardService

## Implementation notes
- Notice bodies are handled as HTML and sanitized before persistence.
- Calendar records include holiday and event behavior that is often used in attendance and payroll contexts.
