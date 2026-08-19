# Project Design and UI Guide

This reference defines the shared user-interface structure and design rules for Codeji CMS. The implementation is in the sibling frontend checkout, `CMS-React`; backend endpoints must preserve the contracts that these screens depend on.

## Design intent

The product is an operational HRMS: information should be easy to scan, actions should be obvious, and dense data should remain usable on smaller screens.

- Use a clear hierarchy: page title/context, controls, summary, then detailed data.
- Prefer reusable MUI components and theme tokens over page-specific colours and controls.
- Keep administrative actions explicit and reversible where the workflow supports it.
- Show useful loading, empty, disabled, permission, and error states.
- Do not use a frontend visibility rule as the only authorization or tenant-isolation control.

## Application shell

`CMS-React/src/_themes/layout/MasterLayout.tsx` owns the authenticated application shell.

```text
Application shell
├── Header: search, appearance, language, notifications and user menu
├── Sidebar: module navigation; expanded, collapsed or mobile drawer
└── Scrollable page surface
    ├── Breadcrumb/page context
    ├── Routed feature page
    └── Footer in normal document flow
```

Desktop sidebar widths are 265px expanded and 100px collapsed. At the `md` breakpoint (960px), the sidebar becomes a temporary drawer and content uses the full viewport width. The page surface, not the browser shell, owns vertical scrolling; the footer follows content instead of being fixed over it.

## Theme and visual system

The frontend uses Material UI with theme palettes, CSS variables, light/dark schemes, and selectable surface treatments. Theme creation is in `src/app/modules/theme/MuiTheme.ts`; palette definitions and application-style choices live alongside it.

Use semantic theme values in `sx` or component props:

```tsx
bgcolor: "background.paper"
color: "text.secondary"
borderColor: "divider"
color: "primary.main"
```

Do not introduce arbitrary fixed colours when a semantic token or configured status colour is available. Existing surface styles are default, glass, minimal, and aurora; new components must remain readable in every enabled style and colour mode.

### Surfaces and spacing

| Element | Default treatment |
|---|---|
| Page | Responsive horizontal padding: 16px mobile, 24px desktop |
| Card | Theme `background.paper`, divider border where needed, modest elevation |
| Section | `Stack`/`Grid` spacing of 16–24px |
| Form controls | Outlined, full width by default, consistent label/helper text |
| Buttons | MUI theme sizing, sentence-case labels, no unnecessary elevation |
| Icons | Accompany familiar actions; provide tooltip or accessible label when icon-only |

## Shared component rules

Use the product primitives from `src/app/shared/components/ui/MuiPrimitives.tsx`:

| Use | Component |
|---|---|
| Primary/secondary actions | `AppButton` |
| Text and select fields | `AppTextField`, `AppSelect` |
| Toggle inputs | `AppCheckbox`, `AppRadio`, `AppSwitch` |
| Icon-only interaction | `AppIconButton` |
| Inline status/feedback | `AppAlert`, MUI `Chip`, MUI `Alert` |

Do not recreate default button, input, or switch styling within a feature. A feature-specific style is appropriate only when it communicates domain state—for example, an attendance status colour or a destructive delete action.

## Page composition

Most authenticated pages should use this order:

```text
Page context/title
→ Filter/search/date controls
→ Summary cards or primary actions
→ Main data area (table, cards, calendar, or form)
→ Detail, exception, or history section
```

### Data-heavy screens

- Keep filters close to the data they affect.
- Preserve selected period, page, and search context after an allowed mutation when practical.
- Make the empty state explain whether data is unavailable, filtered out, or not yet recorded.
- Prefer pagination over an unbounded rendered list.
- On narrow screens, retain readable column/cell widths and provide horizontal scrolling instead of shrinking text into unreadability.

### Mutations and feedback

- Disable a submit action while its request is in progress.
- Display the server-provided error when it is safe and actionable.
- Refresh or invalidate the affected view only after a successful mutation.
- Require confirmation for destructive or broad actions.
- Keep workflow-owned records visibly read-only and explain where the user must make the correction.

## Attendance interface

Primary files:

```text
src/app/modules/attendance/EmployeeAttendance.tsx
src/app/modules/attendance/attendancePeriod.ts
src/app/modules/attendance/attendanceGridLayout.ts
src/app/modules/attendance/component/AttendanceCalendar.tsx
```

### Date range and overview

The available attendance periods are Today, This Week, This Month, 2 Weeks, and Previous Month. Resolve one date range and pass it to the grid, overview cards, exceptions, and export behaviour. Month navigation deliberately shows the complete selected month.

### Grid rules

```text
Attendance grid
├── Sticky employee pane
│   └── Vertical employee list
└── Date pane
    ├── Sticky date header
    ├── Vertically synchronized attendance rows
    └── Independent horizontal date scrolling
```

- Employee and date panes must synchronize vertical scroll position.
- The horizontal date scrollbar must remain reachable below the grid.
- Use configured attendance-status colours by actual status code.
- Use neutral visual states for weekends, holidays, future dates, and employees not yet eligible.
- A workflow-owned Leave/WFH cell cannot be edited through ordinary attendance marking; provide the reason and route users to the owning workflow.

## Accessibility and responsive checklist

- Every interactive icon has an accessible name or a nearby visible label.
- Keyboard users can reach and operate interactive cards/cells; focus is visible.
- Text, icons, borders, and status colours meet contrast needs in light and dark modes.
- Mobile layouts stack controls, use drawers for navigation, and avoid clipped content.
- Tables and calendars preserve their structural relationship while horizontally scrollable.
- Honour reduced-motion preferences for non-essential animation.

## Backend/UI contract rules

The UI consumes API data but does not own business authority.

- The API derives `CompanyId` from the authenticated request; the client must not supply or decide tenant ownership.
- Permission-gated UI is helpful for usability, but controllers/services must enforce the same permission server-side.
- API errors for row-level imports or validation should identify the affected item, field, and message so the UI can display a repairable error.
- Persisted status codes, dates, and source ownership determine UI rendering; do not infer them from labels or colours.

## Verification

For a UI change, run the closest checks and then verify the affected screen in a browser:

```powershell
cd CMS-React
npm run typecheck
npm test -- --run src/app/modules/attendance/AttendanceCalendarLayout.test.ts
```

Also manually verify desktop, a narrow viewport, light/dark mode, loading/empty/error states, keyboard navigation, and the API authorization result. Typechecking alone cannot prove browser layout or backend access behaviour.

## Cross-module form and control contract

All modules use the same visual grammar for input fields, labels, switches,
checkboxes, date pickers, tabs, and actions. This is a product requirement, not
an optional styling preference.

| Control | Required layout behaviour |
|---|---|
| Text/select/date/time field | visible label, full available column width, aligned label/notch and helper text, no overlapping adornment |
| Boolean setting | descriptive label on the left; checkbox/switch and state text aligned in one compact action group on the right |
| Filter bar | controls share a row on wide screens; wrap as complete controls on smaller screens without orphaning an icon button |
| Primary mutation | one clear save/submit action per form scope; disable while pending and preserve entered values on failure |
| Destructive mutation | compact icon or text action with accessible name, confirmation when appropriate, and an error beside the affected item |
| Tabs | clear selected state, deliberate gap/divider, keyboard navigation, no clipped labels |

Avoid explanatory paragraphs when a concise label, helper text, tooltip, or
inline status will communicate the rule. Long operational guidance belongs in a
collapsible help area or documentation, not between every field.

### Responsive composition rules

Use CSS Grid/Stack breakpoints rather than hard-coded screen widths:

- Large screens may use multiple columns only when each field remains readable.
- Medium screens reduce the number of columns before labels or controls collide.
- Small screens use one-column forms, full-width primary actions where useful,
  and compact secondary/icon actions with adequate touch targets.
- Preserve data-table/calendar structure with an intentional inner horizontal
  scroll; do not let the entire page acquire an inaccessible horizontal overflow.
- An expanded accordion/card must appear directly after its own trigger row and
  retain focus/scroll context for keyboard users.

### Domain UI vocabulary

Use **Punch in** and **Punch out** for attendance times visible to users. Keep
the existing `CheckInTime`/`CheckOutTime` API field names unless a versioned
contract change is made. Display the employee's effective shift and assignment
source in attendance views where it explains validation or filtering; do not
expose IDs, tenant keys, or another employee's assignment data.

### Frontend implementation checklist

1. Start from the shared primitives and theme tokens.
2. Verify 320px-class mobile, tablet, and desktop layouts for the modified flow.
3. Test loading, empty, denied, validation, and server-error states.
4. Keep request ownership and permission enforcement in the API; client gating
   is only a usability aid.
5. Re-test keyboard focus, screen-reader names, and reduced-motion behaviour
   after adding an animation or icon-only action.
