# Project Layout

This repository is the backend half of Codeji CMS. The React application is a sibling checkout, not a folder inside this solution:

```text
CMS/
├── CMS-Backend-Core/                 # This repository
└── CMS-React/                        # Vite + React frontend repository
```

## Backend solution

`CodejiCMSCore.sln` is the backend solution entry point.

```text
CMS-Backend-Core/
├── Codeji.CMS.API/                   # ASP.NET Core host and HTTP boundary
│   ├── App_Start/                     # Auth, permission, exception, antiforgery middleware
│   ├── Controllers/                   # API endpoints; keep controllers thin
│   ├── Notification/ and ChatHub/     # SignalR hubs and delivery services
│   ├── Templates/                     # Email templates copied to build output
│   └── Program.cs                     # DI, middleware pipeline, hosted-service registration
├── Codeji.CMS.Services/               # Business rules and workflow orchestration
│   ├── Attendance/                    # Attendance, WFH, schedule, calendar and reminder services
│   ├── Employees/, Companies/          # Employee and company-domain workflows
│   ├── LeaveManagement/, PayRoll/     # Leave and payroll workflows
│   ├── Registration/                  # Service DI registration and Mapster configuration
│   └── BackgroundTasks/               # Queue and background-work primitives
├── Codeji.CMS.Repository/             # MongoDB persistence layer
│   ├── Entities/                      # MongoDB document models
│   ├── Repositories/                  # Generic and specialised repositories
│   ├── Interfaces/                    # Repository contracts
│   └── Registration/                  # Repository DI registration
├── Codeji.CMS.DTO/                    # Request/response contracts grouped by domain
├── Codeji.CMS.Utility/                # Shared enums, helpers, sanitization and current-context helpers
├── Codeji.CMS.Migrations/             # MongoDB migration runner and ordered migrations
├── Codeji.CMS.Services.Tests/         # Unit and focused regression tests
├── docs/                              # Authoritative implementation documentation
├── tools/                             # Developer and maintenance tooling
└── CodejiCMSCore.sln                  # Backend solution file
```

### Backend dependency direction

```text
API → Services → Repository → DTO → Utility
          ↑            ↑
      Migrations ──────┘
```

Dependencies must remain one-way. A DTO must not depend on a service or repository. Controllers obtain the authenticated user and company context, services enforce domain rules, and repositories persist data.

## Frontend solution

`CMS-React` is a Vite/TypeScript React application.

```text
CMS-React/
├── src/
│   ├── app/
│   │   ├── modules/                   # Feature modules: attendance, employees, leave, payroll, etc.
│   │   ├── shared/                    # Reusable UI components and application helpers
│   │   └── Redux/                     # Client application state
│   ├── server/                        # Axios client and API request helpers
│   ├── utils/                         # Shared date, formatting and permission utilities
│   ├── _themes/                       # Theme and application shell layout
│   └── main.tsx                       # React application entry point
├── public/                            # Static browser assets
├── package.json                       # Frontend scripts and dependencies
└── vite.config.ts                     # Development/build configuration
```

### Attendance UI locations

```text
CMS-React/src/app/modules/attendance/
├── EmployeeAttendance.tsx             # Page controls, date range, employees and summary cards
├── attendancePeriod.ts                 # Shared attendance period/date-range resolution
├── attendanceGridLayout.ts             # Shared grid dimensions and scroll styles
├── component/
│   ├── AttendanceCalendar.tsx          # Employee/date attendance grid and cell interaction
│   └── MonthlyAttendanceExceptions.tsx # Exception review area
└── AttendanceCalendarLayout.test.ts    # Focused grid-layout regression test
```

The attendance grid uses two synchronized panes: the employee list and the date grid. Their vertical scroll positions must remain synchronized; the date grid separately owns horizontal scrolling so employee names remain visible.

## Request and data flow

```text
React page
  → server/axios request helper
  → Codeji.CMS.API controller
  → Codeji.CMS.Services domain service
  → Codeji.CMS.Repository MongoDB repository
  → MongoDB
```

For company-owned data, `CompanyId` must come from authenticated server context. Frontend-supplied tenant identifiers are not authoritative. Background services do not have an HTTP context, so their repository reads must explicitly include the company predicate and intentionally use the repository's background-safe access path.

## Where to make common changes

| Change | Primary location |
|---|---|
| Add or change an API endpoint | `Codeji.CMS.API/Controllers/` |
| Add domain logic | `Codeji.CMS.Services/<Domain>/` |
| Add a MongoDB entity/index | `Codeji.CMS.Repository/Entities/` and migrations |
| Change API request/response shape | `Codeji.CMS.DTO/` |
| Register a service | `Codeji.CMS.Services/Registration/ServicesRegistration.cs` |
| Register a hosted worker | `Codeji.CMS.API/Program.cs` |
| Change a React feature screen | `CMS-React/src/app/modules/<feature>/` |
| Change shared React UI | `CMS-React/src/app/shared/` |
| Change API client behaviour | `CMS-React/src/server/` |

## Verification entry points

```powershell
# Backend
dotnet build Codeji.CMS.Services/Codeji.CMS.Services.csproj --no-restore
dotnet test Codeji.CMS.Services.Tests/Codeji.CMS.Services.Tests.csproj --no-restore

# Frontend (run from CMS-React)
npm run typecheck
npm test -- --run src/app/modules/attendance/AttendanceCalendarLayout.test.ts
```

Build and unit-test results do not replace browser, authorization, tenant-isolation, or isolated database validation.
