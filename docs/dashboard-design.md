# Dashboard Frontend Design Specification

## 1. Purpose

The dashboard is the authenticated landing page of Codeji CMS. It should give employees, HR managers, and administrators a fast overview of the company without forcing them to open multiple modules.

This document defines the visual design, responsive behavior, component structure, data states, and implementation expectations for the frontend dashboard.

Primary implementation files:

```text
src/app/modules/dashboard/Dashboard.tsx
src/app/modules/dashboard/components/
src/app/modules/dashboard/dashboardservices.ts
src/app/modules/dashboard/model.ts
src/_themes/layout/MasterLayout.tsx
src/app/modules/theme/
```

Primary route:

```text
/dashboard
```

---

## 2. Design goals

The dashboard should be: 

- Scannable within five seconds
- Useful even when the company has little or no data
- Responsive from mobile phones to large desktop screens
- Consistent with the Codeji purple/orange visual identity
- Permission-aware
- Accessible by keyboard and screen reader
- Clear in loading, empty, success, and error states
- Modular enough for cards to be added, removed, or reordered

The dashboard should not become a dense reporting screen. Detailed analysis belongs inside the related module.

---

## 3. Information hierarchy

The dashboard has four visual levels:

1. Global navigation and account controls
2. Welcome and date context
3. Primary business metrics
4. Secondary operational information

```text
Application shell
├── Sidebar
├── Top header
└── Dashboard content
    ├── Welcome header
    ├── Primary metrics
    │   ├── Total employees
    │   ├── Departments
    │   ├── Open job roles
    │   └── Applications
    ├── Activity
    │   └── Notice board
    └── Contextual sidebar
        ├── Notification permission
        ├── Employees on leave
        ├── Upcoming holidays and events
        ├── Upcoming celebrations
        └── Help
```

---

## 4. Desktop layout

### Application shell

The existing application shell consists of:

- Fixed or collapsible left sidebar
- Fixed-height top header
- Scrollable content area
- Responsive MUI container

Current sidebar widths:

```text
Expanded: 265px
Mini:      90px
Mobile:    overlay drawer
```

### Dashboard content grid

For desktop screens at `lg` and above:

```text
┌──────────────────────────────────────────────────────────────────────┐
│ Welcome user                                      Weather/date       │
├────────────────────────────────────────────┬─────────────────────────┤
│ Main content — 8 columns                   │ Context — 4 columns     │
│                                            │                         │
│ ┌─────────────────┐ ┌─────────────────┐    │ ┌─────────────────────┐ │
│ │ Employees       │ │ Departments     │    │ │ Notifications       │ │
│ └─────────────────┘ └─────────────────┘    │ └─────────────────────┘ │
│                                            │                         │
│ ┌─────────────────┐ ┌─────────────────┐    │ ┌─────────────────────┐ │
│ │ Open jobs       │ │ Applications    │    │ │ Employees on leave  │ │
│ └─────────────────┘ └─────────────────┘    │ └─────────────────────┘ │
│                                            │                         │
│ ┌───────────────────────────────────────┐  │ ┌─────────────────────┐ │
│ │ Notice board                          │  │ │ Holidays/events      │ │
│ └───────────────────────────────────────┘  │ └─────────────────────┘ │
│                                            │                         │
│                                            │ ┌─────────────────────┐ │
│                                            │ │ Celebrations        │ │
│                                            │ └─────────────────────┘ │
└────────────────────────────────────────────┴─────────────────────────┘
```

Grid ratio:

```text
Primary column:   8/12
Secondary column: 4/12
```

Primary metric cards use a two-column arrangement:

```text
Card width: 6/12 of the primary column
```

### Recommended dimensions

| Element | Recommendation |
|---|---|
| Content maximum width | 1280–1440px |
| Page horizontal padding | 24px desktop |
| Main grid gap | 24px |
| Secondary-column gap | 16–24px |
| Metric card minimum height | 310px |
| Context card minimum height | 240–280px |
| Card padding | 20–24px |
| Card radius | 12–14px |
| Header-to-grid spacing | 24–32px |

Avoid large empty vertical areas inside cards. A card height may be fixed when chart alignment requires it, but list cards should normally size to content with a sensible minimum.

---

## 5. Tablet layout

At `md` widths:

- Sidebar becomes a drawer or compact sidebar.
- Content uses the full available width.
- Primary and secondary dashboard columns stack.
- Metric cards may remain two per row when enough width exists.
- Secondary cards should form a two-column grid rather than one long column where practical.

Recommended arrangement:

```text
Welcome/date
Employees | Departments
Jobs      | Applications
On leave  | Holidays
Celebrations
Notice board
Help
```

---

## 6. Mobile layout

At `xs` widths:

- All dashboard cards use full width.
- Welcome text and date stack vertically.
- Day/date content aligns left rather than right.
- Charts use reduced height.
- Legends wrap below charts.
- Select controls occupy a touch-friendly width.
- Long lists show a short preview plus a “View all” action.

Recommended order:

```text
Welcome
Date
Notification prompt
Total employees
Departments
Employees on leave
Open jobs
Applications
Holidays/events
Celebrations
Notice board
Help
```

Mobile spacing:

```text
Page padding: 16px
Card gap:     16px
Card padding: 16px
```

No horizontal page scrolling should occur.

---

## 7. Visual design system

### Brand colors

Current primary palette:

```text
Primary main:   #9A56D3
Primary dark:   #7C41A3
Primary light:  #C0A4EB
Primary tint:   #E6D9F5
```

Current secondary palette:

```text
Secondary main: #FEBB68
Secondary dark: #CC8C4A
Secondary tint: #FFF3E0
```

Chart colors:

```text
#9A56D3  Purple
#FFA52F  Orange
#6D6D6D  Grey
#E0468F  Magenta
#F45D48  Coral
#FFC107  Amber
#3949AB  Deep blue
```

### Surface colors

Light mode:

```text
Page background: #F5F5F5 or a subtle blue-grey tint
Header:          #FFFFFF
Cards:           #FFFFFF
Primary text:    theme text.primary
Secondary text:  theme text.secondary
```

Dark mode:

```text
Page background: #000000
Header/cards:    #1E1E1E
```

### Card appearance

Default:

```text
Background: white
Border radius: 12–14px
Border: optional 1px neutral border
Shadow: subtle, low elevation
```

Cards should not use heavy shadows. Hierarchy should come from spacing, typography, contrast, and content.

The project supports multiple application styles:

- Classic
- Cobalt
- Glass
- Gradient
- Minimal

Dashboard cards must remain readable in every style.

---

## 8. Typography

Recommended dashboard typography:

| Usage | Size | Weight |
|---|---:|---:|
| Welcome title | 28–32px | 500–600 |
| Day label | 24–28px | 600 |
| Card title | 14–16px | 600 |
| KPI value | 28–40px | 600–700 |
| Body text | 14px | 400 |
| Supporting text | 12px | 400 |
| Empty-state message | 14–16px | 400–500 |
| Action/link | 12–14px | 600 |

Do not use purple for all empty-state text. Neutral empty states should use secondary text; purple should indicate actions, links, or branded emphasis.

---

## 9. Dashboard header

Component:

```text
components/DashboardHeader.tsx
```

### Content

Left:

```text
Welcome {fullName}
Optional supporting line:
Here is what is happening in your company today.
```

Right:

- Weather/context icon
- Current weekday
- Localized full date

### Behavior

- Use the current application locale.
- The date must update if the page remains open across midnight.
- The decorative weather icon must have useful alt text or empty alt text when purely decorative.
- On mobile, place the date below the welcome title.

### Suggested refinement

```text
Welcome back, Satyam
Thursday, July 16
```

The greeting may vary by time:

```text
Good morning
Good afternoon
Good evening
```

This is optional; localization must be supported if added.

---

## 10. Dashboard card anatomy

Every standard dashboard card should follow:

```text
┌───────────────────────────────────────────┐
│ Title                     value/action    │
│ Optional subtitle                         │
│                                           │
│ Main visualization or list                │
│                                           │
│ Optional footer or “View all” action       │
└───────────────────────────────────────────┘
```

Required characteristics:

- Consistent padding
- Clear card title
- Optional KPI aligned opposite title
- Loading state with stable dimensions
- Empty state with explanation and optional action
- Error state with retry
- Keyboard-accessible interactions

Shared wrapper:

```text
src/app/shared/components/CustomCard.tsx
```

Prefer extending the shared card wrapper over repeating header and spacing styles.

---

## 11. Total employees card

Component:

```text
components/EmployeeChart.tsx
```

### Purpose

Show:

- Total active employees
- Employee distribution by gender

### Layout

Header:

```text
Total Employees                           42
```

Body:

- Donut chart
- Legend below or beside chart depending on width
- Tooltip showing label, count, and percentage

### Empty state

If no employees exist:

```text
No employees have been added yet.
[Invite employee]
```

Only show the action if the user has `Employees/Create`.

### Accessibility

Add a textual summary for screen readers:

```text
42 employees: 20 women, 21 men, 1 other.
```

Do not communicate gender categories by color alone.

---

## 12. Departments card

Component:

```text
components/DepartmentChart.tsx
```

### Purpose

Show employee distribution across departments.

### Current issue

The screenshot shows the total department count in the center while the legend may include departments with zero employees. This can be confusing.

### Recommended design

Header:

```text
Departments                               8
```

Body:

- Donut chart should represent employee headcount by department.
- Center value should say `8 departments` or show total employees, depending on the chart's meaning.
- Legend should show only departments represented in the chart by default.
- A “View all departments” link can expose all configured departments.

Example legend:

```text
Engineering       18
Human Resources    4
Finance             6
```

### Empty state

```text
No departments are configured.
[Manage departments]
```

The action is administrator-only.

---

## 13. Open job roles card

Component:

```text
components/OpenPositions.tsx
```

### Purpose

Show currently active vacancies and provide a fast route to recruitment.

### Populated state

Each item should display:

- Job title
- Work type: onsite, hybrid, or remote
- Department
- Number of applicants
- Posted or closing date

Limit the dashboard preview to three or four jobs.

Footer:

```text
View all jobs →
```

### Empty state

```text
No active vacancies
Create a job opening when your team is ready to hire.
[Create vacancy]
```

Only show creation actions when the user has `Jobs/Create`.

---

## 14. Applications card

Component:

```text
components/ApplicationsChart.tsx
```

### Purpose

Show applicant counts by recruitment status.

### Header

```text
Applications                    [All jobs ▼]
```

The filter must have an accessible label such as:

```text
Filter applications by vacancy
```

### Visualization

Recommended status order:

```text
New
In progress
On hold
Shortlisted
Selected
Rejected
```

Use a bar chart for direct comparison. Tooltips should show exact counts.

### Empty state

```text
No applications yet
Applications will appear here when candidates apply.
```

If a specific vacancy is selected:

```text
No applications for this vacancy.
```

---

## 15. Notification permission card

Component:

```text
components/PushNotification.tsx
```

### Visibility

Display only when:

```text
Notification.permission === "default"
```

Do not display after permission is granted or denied.

### Design

Use a soft primary tint rather than a full saturated background.

Content:

```text
Stay updated
Get alerts for new messages, task updates, and important events.
[Enable notifications]
```

The browser permission prompt must only open after a direct user action.

If notifications are unsupported, hide the card.

---

## 16. Employees on leave card

Component:

```text
components/EmpOnLeave.tsx
```

### Purpose

Show employees whose approved leave includes the current date or begins soon.

### Populated item

```text
[Avatar] Employee name
         Leave type
         Jul 16–18 · 3 days
```

Recommended maximum preview:

```text
4 employees
```

Footer:

```text
View leave calendar →
```

### Empty state

```text
Everyone is available today.
```

This is more positive and concise than:

```text
No employees are currently on leave!
```

---

## 17. Upcoming holidays and events card

Component:

```text
components/UpComingHolidayAndEvent.tsx
```

### Purpose

Show the next company holidays and calendar events.

### Populated item

```text
[Date badge] Holiday/event title
             Type
             Relative date
```

Example:

```text
15 AUG    Independence Day
          National holiday · In 30 days
```

Use different icons for holidays and events, but maintain one visual system.

### Empty state

```text
No upcoming holidays or events.
[Open calendar]
```

---

## 18. Upcoming celebrations card

Component:

```text
components/UpcomingCelebration.tsx
```

### Purpose

Show upcoming:

- Birthdays
- Work anniversaries

### Item content

```text
[Avatar] Employee name
         Birthday tomorrow
```

or:

```text
[Avatar] Employee name
         3rd work anniversary · Jul 20
```

Use subtle birthday and anniversary icons. Avoid excessive decorative emoji when it affects consistency.

### Empty state

```text
No celebrations coming up.
```

---

## 19. Notice board card

Component:

```text
components/NoticeBoard.tsx
```

### Purpose

Show recent company announcements.

### Item structure

```text
[Avatar/icon] Notice title                    2h ago
              Short preview text
              Posted by employee
```

Notice priority should use:

- Text label
- Icon
- Color

Do not use color alone.

Recommended labels:

```text
General
Important
Urgent
```

Show up to four items and provide:

```text
View notice board →
```

Clicking an item may open the existing notice viewer.

### Empty state

```text
No notices have been posted.
```

If permitted:

```text
[Post notice]
```

---

## 20. Help card

Component:

```text
components/Help.tsx
```

The help card should be visually secondary. It should not consume more space than operational information.

Content:

```text
Need help?
Contact the support team if you have questions about Codeji CMS.
[Contact support]
```

The action should open a working support channel:

- `mailto:` link
- Internal support page
- Support chat

Decorative artwork must not overlap text on narrow screens.

---

## 21. Permissions and role personalization

Dashboard cards must follow module permissions.

Current examples:

| Card | Permission |
|---|---|
| Total employees | `Employees/View` |
| Open jobs | `Jobs/View` |
| Applications | `Jobs/View` or applications permission as appropriate |
| Notice board | `Notice_Board/View` |

Recommended improvements:

- Employees should see personal information first.
- HR managers should see leave, recruitment, and employee metrics.
- Administrators should see company setup and permission warnings.

Potential employee-focused cards:

- My attendance today
- My leave balance
- My upcoming holidays
- Recent company notices

Potential administrator alerts:

- Departments with no employees
- Incomplete payroll setup
- Roles with missing permissions
- Company profile incomplete

Do not show an empty gap where a permission-restricted card was removed. The grid must reflow automatically.

---

## 22. Loading states

Current shared component:

```text
components/CardSkeleton.tsx
```

Skeletons should approximate the final component:

- Chart card skeleton for charts
- List skeleton for lists
- Compact banner skeleton for notification cards

Avoid using one identical 310px skeleton for every card because it creates visible layout jumps.

Loading rules:

- Show skeleton immediately.
- Keep the card dimensions stable.
- Do not show “No data” before loading finishes.
- Avoid full-page loaders when cards load independently.

---

## 23. Empty states

Every card should have a deliberate empty state.

An empty state may contain:

1. Short title
2. One explanatory sentence
3. Optional permission-aware action

Example:

```text
No active vacancies
Create a vacancy when your company is ready to hire.
[Create vacancy]
```

Avoid:

- Large areas of blank white space
- Only saying “No data”
- Exclamation marks for normal empty states
- Error colors for valid zero-data conditions

---

## 24. Error states

API errors must not silently become empty states.

Recommended card error:

```text
Couldn’t load employee data.
[Try again]
```

Rules:

- Keep the rest of the dashboard usable.
- Log technical details for diagnostics.
- Show a user-readable message.
- Provide a retry action.
- Distinguish network failure from legitimate empty data.

The frontend currently logs several dashboard errors only to the console. Those cards should gain visible error states.

---

## 25. Refresh behavior

Recommended behavior:

- Fetch card data on dashboard mount.
- Add a page-level refresh action if users need current operational data.
- Show a non-blocking refresh indicator.
- Avoid resetting all cards to full skeletons during background refresh.
- Optionally refresh selected real-time cards after SignalR events.

Useful refresh triggers:

- New notice
- Leave request approved
- Employee added
- Job application received

---

## 26. Accessibility

Requirements:

- All interactive cards and links must be keyboard accessible.
- Use semantic headings in order.
- Charts need text summaries.
- Tooltips must not be the only way to access data.
- Color contrast should meet WCAG AA.
- Focus states must remain visible.
- Icon-only buttons need `aria-label`.
- Selects need visible or programmatic labels.
- Empty-state illustrations should have empty alt text when decorative.
- Do not rely only on red/green or chart colors.
- Respect reduced-motion preferences.

Suggested heading order:

```text
h1: Dashboard or welcome heading
h2: Major dashboard sections if introduced
h3: Individual card headings
```

The current welcome heading uses the MUI `h2` visual variant but does not necessarily render a semantic `h1`. Set the `component` prop explicitly where needed.

---

## 27. Internationalization

All visible text must use React Intl.

This includes:

- Card titles
- Empty states
- Error messages
- Button labels
- Chart labels
- Tooltip text
- Accessibility labels

Department names already support multilingual labels. Use:

```text
current language
→ company default language
→ translated “missing translation” fallback
```

Layouts must support longer translated text without clipping.

---

## 28. Suggested component structure

```text
dashboard/
├── Dashboard.tsx
├── dashboardservices.ts
├── model.ts
├── components/
│   ├── DashboardHeader.tsx
│   ├── DashboardSection.tsx
│   ├── DashboardCard.tsx
│   ├── DashboardCardSkeleton.tsx
│   ├── DashboardCardError.tsx
│   ├── DashboardEmptyState.tsx
│   ├── EmployeeChart.tsx
│   ├── DepartmentChart.tsx
│   ├── OpenPositions.tsx
│   ├── ApplicationsChart.tsx
│   ├── EmpOnLeave.tsx
│   ├── NoticeBoard.tsx
│   ├── PushNotification.tsx
│   ├── UpComingHolidayAndEvent.tsx
│   ├── UpcomingCelebration.tsx
│   └── Help.tsx
└── hooks/
    ├── useDashboardPermissions.ts
    └── useDashboardRefresh.ts
```

This is a recommended evolution, not a requirement to rename all existing files immediately.

---

## 29. Recommended dashboard data contract

The current design makes multiple API calls, one from each card. This allows independent loading but increases network requests.

Two valid approaches exist.

### Independent card requests

Advantages:

- Cards fail independently.
- Existing services can be reused.
- Permission-restricted cards do not fetch unnecessary data.

Disadvantages:

- Many requests on page load.
- Harder to show one “last updated” time.

### Aggregated dashboard endpoint

Example:

```json
{
  "employees": {
    "total": 42,
    "gender": []
  },
  "departments": [],
  "openJobs": [],
  "applications": {},
  "employeesOnLeave": [],
  "holidays": [],
  "events": [],
  "celebrations": [],
  "notices": []
}
```

Advantages:

- Fewer round trips
- Consistent snapshot
- Easier caching

Disadvantages:

- Larger endpoint
- One failure can affect more cards
- Permission-aware response shaping becomes important

For the current project, independent requests are acceptable. Consider aggregation only after measuring dashboard latency.

---

## 30. Performance expectations

Targets:

```text
Initial shell visible:       < 1 second on local/fast connection
Useful dashboard content:    < 2 seconds under normal conditions
No major layout shift:       CLS kept low
Interaction response:        < 100ms for local UI actions
```

Recommendations:

- Lazy-load chart libraries if bundle analysis justifies it.
- Avoid fetching permission-hidden cards.
- Limit dashboard list results.
- Debounce or avoid unnecessary filter requests.
- Memoize chart transformations.
- Use stable effect dependencies.
- Cancel stale requests when filters change rapidly.

---

## 31. Current-screen review

The current dashboard screenshot already has a strong base:

- Clear left navigation
- Recognizable primary branding
- Simple card layout
- Useful module overview
- Permission-based card rendering
- Consistent rounded surfaces

Priority design improvements:

1. Reduce unused white space in empty cards.
2. Make empty states descriptive and actionable.
3. Clarify what the department chart center value represents.
4. Add visible error and retry states.
5. Use consistent card heights by content category rather than one fixed height.
6. Improve mobile ordering and card density.
7. Add “View all” actions to preview cards.
8. Add text summaries for charts.
9. Use neutral text for empty states rather than primary purple everywhere.
10. Make the right column visually balanced when the notification prompt is absent.

---

## 32. Implementation phases

### Phase 1 — Consistency

- Standardize card padding, titles, heights, and gaps.
- Introduce shared empty and error states.
- Improve card skeleton variants.
- Add “View all” links.
- Fix semantic headings and labels.

### Phase 2 — Responsive design

- Review all breakpoints.
- Stack header content on mobile.
- Reduce chart dimensions on small screens.
- Ensure legends and selects wrap.
- Reorder cards for mobile priority.

### Phase 3 — User experience

- Add retry actions.
- Add page refresh.
- Add personal dashboard cards for employees.
- Add admin setup warnings.
- Improve notification prompt behavior.

### Phase 4 — Performance and realtime

- Measure request and bundle performance.
- Refresh relevant cards from SignalR events.
- Consider dashboard endpoint aggregation if needed.

---

## 33. Acceptance checklist

### Layout

- [ ] Desktop uses an 8/4 primary/context grid.
- [ ] Tablet and mobile layouts stack without horizontal overflow.
- [ ] Cards reflow when permission-restricted cards are hidden.
- [ ] Card spacing is consistent.

### States

- [ ] Every card has loading, empty, success, and error states.
- [ ] Empty states are not confused with errors.
- [ ] Retry works without refreshing the whole page.

### Accessibility

- [ ] Correct semantic headings are used.
- [ ] Keyboard navigation works.
- [ ] Icon buttons and filters have labels.
- [ ] Charts have text equivalents.
- [ ] Color contrast meets WCAG AA.

### Internationalization

- [ ] All dashboard strings use translation keys.
- [ ] Long translated strings do not overflow.
- [ ] Dates use the active locale.

### Permissions

- [ ] Restricted cards are not rendered or fetched.
- [ ] Card actions check the relevant create/edit permission.
- [ ] The backend remains the authoritative permission layer.

### Performance

- [ ] Dashboard shell renders quickly.
- [ ] Lists request only preview-size result sets.
- [ ] Chart transformation is memoized where appropriate.
- [ ] No duplicate API calls occur in development or production unexpectedly.

---

## 34. Design handoff summary

The intended dashboard experience is:

```text
Welcoming
→ immediately understandable
→ operationally useful
→ permission-aware
→ calm when there is no data
→ resilient when one API fails
→ responsive on every supported screen
```

The current implementation should be evolved incrementally by improving shared card primitives and states first. This produces a consistent dashboard without requiring a full rewrite of every card.

