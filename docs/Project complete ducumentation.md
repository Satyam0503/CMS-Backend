# Codeji CMS

# Manager-Facing Project Documentation

> A business, technical, operational, and delivery guide for the Codeji CMS platform.
>
> This document explains the product represented by two sibling repositories:
> `CMS-Backend-Core` and `CMS-React`. It is written for managers, product owners,
> technical leads, delivery teams, QA, support, and new contributors.
>
> **Reading rule:** source code and deployed configuration are authoritative. This
> document explains the intended operating model and records the implementation
> facts currently documented in the repositories. Confirm environment-specific
> values before making a production decision.

---

## Document Control

| Field | Value |
|---|---|
| Product | Codeji CMS / HRMS and Recruitment Platform |
| Audience | Managers, product owners, delivery leads, QA, operations, developers |
| Repositories | `CMS-Backend-Core`, `CMS-React` |
| Backend runtime | ASP.NET Core on .NET 10 according to the current API project file |
| Frontend runtime | React 18, TypeScript, Vite |
| Primary database | MongoDB |
| Primary communication | REST API, SignalR, email |
| Document purpose | Explain the product from basic to advanced levels |
| Maintenance rule | Update this guide when a user journey, contract, control, or deployment process changes |

## How to Use This Guide

1. A manager can read Sections 1 through 6 to understand the product and value.
2. A delivery lead can read Sections 7 through 14 to understand scope and dependencies.
3. A technical lead can read Sections 15 through 25 to understand architecture and controls.
4. QA can read Sections 26 through 32 for risk-based verification.
5. Operations can read Sections 33 through 40 for release and support procedures.
6. A developer can read Sections 41 through 48 for extension and governance rules.
7. The final appendices provide checklists, terminology, and questions for reviews.

## Important Current-State Notes

- The API project currently targets `net10.0`; an older architecture note says .NET 8.
- The React project uses Redux Toolkit and also includes TanStack Query dependencies.
- WFH is presented inside Leave Management in the React application.
- MongoDB schema evolution uses a custom migration runner, not Entity Framework migrations.
- Durable notification records are created before realtime delivery is attempted.
- Email success depends on provider configuration and should be verified through email logs.
- A frontend permission check improves usability but never replaces backend authorization.

---

# Part I: Executive Explanation

## 1. What the Product Is

Codeji CMS is a multi-tenant human resources, employee management, attendance,
leave, payroll, recruitment, and company communication platform.

It allows multiple companies to use one application deployment while keeping each
company's business records separated by a company identifier.

The system has two visible faces:

- an authenticated internal application used by employees, HR, managers, and administrators;
- public recruitment pages used by candidates who are not signed in.

The system also has two implementation halves:

- `CMS-Backend-Core` owns business rules, security, persistence, background work, email, and realtime hubs;
- `CMS-React` owns the browser experience, navigation, forms, tables, dashboards, and user feedback.

The platform is not merely a collection of CRUD screens. Attendance, leave, WFH,
and payroll form a controlled chain. A decision in one area changes what is valid
in the next area.

## 2. Why the Product Exists

The platform addresses common operational problems in growing organizations:

- employee information is spread across spreadsheets and email;
- attendance corrections are difficult to audit;
- leave balances do not match actual attendance;
- payroll preparation depends on manual reconciliation;
- recruiting activity is separated from the employee system;
- announcements and notifications are difficult to track;
- access decisions are made informally rather than through explicit permissions;
- managers lack a consistent view of people, work, and workflow status.

The product brings these activities into one tenant-aware platform.

## 3. Business Outcomes

The expected business outcomes are:

1. A single source of truth for employee and company information.
2. More consistent attendance and leave decisions.
3. Better payroll preparation through controlled upstream records.
4. Faster recruitment through public career pages and applicant workflows.
5. Clearer accountability through status history and audit information.
6. Reduced risk from cross-company data exposure.
7. Better employee self-service for profile, leave, attendance, and WFH actions.
8. Faster management reporting through dashboard aggregates.
9. Durable communication through notification history and email logs.
10. A repeatable delivery model for adding new business capabilities.

## 4. Stakeholders

| Stakeholder | Primary interest | Typical decisions |
|---|---|---|
| Executive sponsor | Business value and risk | Funding, priorities, service level |
| Product manager | Scope and user outcomes | Roadmap, acceptance criteria |
| HR administrator | Employee operations | Policies, approvals, corrections |
| Department manager | Team workflow | Approvals, attendance review |
| Employee | Self-service and transparency | Leave, WFH, profile actions |
| Recruiter | Candidate pipeline | Jobs, applicants, process stages |
| Payroll operator | Accurate monthly processing | Locks, exceptions, payslips |
| QA lead | Reliability and regression risk | Test strategy and release sign-off |
| Technical lead | Architecture and maintainability | Design, security, performance |
| Operations | Availability and recoverability | Deployment, monitoring, incident response |
| Support team | Fast issue resolution | Triage, evidence, escalation |

## 5. Product Boundaries

### Included

- tenant company administration;
- authentication and account recovery;
- employees and profiles;
- roles and module permissions;
- attendance and attendance configuration;
- leave policies, balances, and requests;
- work-from-home policy and request workflow;
- salary and payroll processing;
- calendar, holidays, and notices;
- recruitment and public careers;
- dashboards, notifications, chat, and email;
- file uploads and payslip generation;
- migration and test support.

### Not Assumed

- a separate tenant database for every company;
- payroll as the source of truth for attendance;
- browser-only enforcement of authorization;
- email delivery as proof that a business transaction succeeded;
- realtime connectivity as the only notification channel;
- an automatic rollback facility for migrations;
- a generic rule that every role name grants every capability.

## 6. Product Vocabulary

| Term | Meaning |
|---|---|
| Tenant | A company whose records are separated from other companies |
| CompanyId | The primary tenant boundary used by protected workflows |
| UserId | The authenticated application identity |
| EmployeeId | A business-facing employee code, not necessarily globally unique |
| Role | A company-scoped grouping of permissions |
| Module | A product area such as Attendance or Payroll |
| Permission | An action such as View, Create, Edit, or Delete |
| Policy | Configured rules that determine what a workflow permits |
| Source ownership | Metadata identifying the workflow that owns a record |
| Reconciliation | Converting an approved workflow into a related attendance record |
| Lock | A control preventing changes to finalized periods |
| Result | The standard application response containing success and status information |
| Hosted service | A background worker that runs independently of an HTTP request |
| Public route | An endpoint intended for candidate or external access |
| Durable notification | A persisted notification that can be viewed later |

---

# Part II: User and Business Journeys

## 7. Company Registration Journey

1. A company administrator submits registration information.
2. The backend validates company and administrator details.
3. The company record is created in MongoDB.
4. Default roles and company-level settings are prepared.
5. Master data used by later forms is initialized.
6. The account receives a session response when registration is complete.
7. The administrator enters the protected application.

Management meaning: registration is the beginning of a tenant lifecycle, not merely
a user creation screen. A defect here can affect roles, lookup values, invitations,
and every downstream module.

## 8. Login and Session Journey

1. The user submits credentials through the React login form.
2. The API verifies the password hash.
3. The API creates a short-lived JWT and a refresh token.
4. The refresh token is persisted with expiry and revocation state.
5. The browser stores the returned authentication state.
6. The application requests signed-user details.
7. The user profile and module permissions are loaded into application state.
8. The user is routed to the dashboard or an intended protected page.
9. When an access token expires, the axios response interceptor requests a refresh.
10. Concurrent failed requests wait for one refresh operation rather than starting many.

Management meaning: a login test must include both initial login and recovery after
expiry. A successful login alone does not prove session continuity.

## 9. Employee Lifecycle Journey

1. HR creates or invites an employee.
2. The employee is associated with the current company.
3. Department, job title, role, language, and profile values are maintained.
4. The employee may complete self-service profile information.
5. HR may manage education, skills, experience, and attachments.
6. Employment changes affect eligibility in other modules.
7. Deactivation or deletion must be considered against history and payroll.

The employee record is a dependency for attendance, leave, WFH eligibility, salary,
payroll, notifications, and reporting.

## 10. Role and Permission Journey

The authorization chain is:

```text
JWT user and company claims
  -> employee role
  -> company role record
  -> role permission grants
  -> module and action permission
  -> controller gate
  -> service ownership and workflow validation
```

A role label is not sufficient proof of access. A manager review should verify both
the configured grant and the service-level ownership rule.

## 11. Attendance Journey

1. The system identifies the employee inside the authenticated company.
2. The requested business date is normalized.
3. Weekly offs, holidays, and configured statuses are evaluated.
4. Existing source-owned rows are checked.
5. Lock rules are checked.
6. The attendance mutation is validated.
7. The record is inserted or updated using company, user, and date identity.
8. Hours and exception information are calculated where required.
9. The record becomes an input to monthly review and payroll.

Attendance is a controlled record. Direct edits must not silently overwrite a row
owned by approved leave or an approved WFH request.

## 12. Leave Journey

1. HR configures a policy and attendance status mapping.
2. Employee balances are created or updated.
3. An employee submits a request.
4. The API derives the employee from the authenticated identity.
5. Policy, date, notice, calendar, overlap, and balance rules are checked.
6. The request enters a pending state.
7. An authorized reviewer accepts or rejects it.
8. Acceptance changes the balance and reconciles leave-owned attendance.
9. Rejection or withdrawal reverses the applicable downstream state.
10. The request and its related attendance records remain auditable.

## 13. Work From Home Journey

WFH is a governed work-location workflow.

1. The employee opens Leave Management and selects Work from home.
2. The browser asks the server for effective context.
3. The server calculates eligibility, quota, next available date, and safe schedule details.
4. The employee submits a date, duration, and reason.
5. The server validates policy, employee eligibility, calendar, quota, overlap, and locks.
6. The request becomes Pending when approval is required.
7. The request becomes Approved immediately when direct approval is configured.
8. Approved dates create WFH-owned attendance rows.
9. The employee checks in and later checks out through the WFH workflow.
10. Insufficient hours create a reviewable exception instead of being hidden.
11. Notifications are persisted and then delivered through SignalR and email where configured.

The important control is provenance: the attendance row identifies that WFH owns it.

## 14. Payroll Journey

1. HR configures salary structures and payroll settings.
2. Attendance and leave are reviewed for the target month.
3. Exceptions are resolved or explicitly accepted.
4. The month is locked when it is ready for payroll.
5. Payroll is generated from approved upstream inputs.
6. HR reviews the draft payroll.
7. Payroll is processed.
8. Payslips are generated and made visible according to publication rules.
9. Employees download their payslips.

Generate and Process are different business actions. A generated draft should not
be treated as employee-visible payroll until the processing step is complete.

## 15. Recruitment Journey

1. A recruiter creates a job vacancy.
2. The vacancy is configured for internal and/or public visibility.
3. Published jobs appear on the company career page.
4. A candidate views the public job without signing in.
5. The candidate submits an application and permitted files.
6. The application is associated with the selected vacancy and company.
7. Recruiters review applicant data and resume information.
8. Status transitions create process history.
9. Candidate communication may be queued through email.
10. Public endpoints remain filtered to active and published information.

## 16. Notification and Communication Journey

1. A business event occurs, such as a notice, WFH request, or approval.
2. The relevant notification is persisted.
3. Recipient-specific notification records are created.
4. SignalR sends a live event to connected users.
5. Email work is queued when an email is appropriate.
6. Email logs provide evidence of the attempted delivery.
7. Offline users still see durable notification history later.

Realtime is an acceleration layer. Persistence is the reliability layer.

---

# Part III: System Architecture

## 17. Repository Map

```text
CMS-Backend-Core/
  Codeji.CMS.API/             HTTP, middleware, controllers, hubs, templates
  Codeji.CMS.Services/        business workflows and hosted workers
  Codeji.CMS.Repository/      Mongo documents and repositories
  Codeji.CMS.DTO/             request and response contracts
  Codeji.CMS.Utility/         auth, context, enums, mail, sanitization
  Codeji.CMS.Migrations/      ordered Mongo migration executable
  Codeji.CMS.Services.Tests/  service-level tests
  docs/                       backend documentation

CMS-React/
  src/app/modules/            product modules
  src/app/routing/            public and protected routes
  src/app/Redux/              shared client state
  src/server/axios.ts         HTTP client and interceptors
  src/utils/                  constants, types, schemas, helpers
  public/locales/              runtime translation resources
  docs/                       frontend documentation
```

## 18. Layer Responsibilities

### API Layer

The API layer exposes routes, binds DTOs, applies authentication and authorization,
and converts service results into HTTP responses. Controllers should remain thin.

### Service Layer

The service layer owns business rules, cross-module coordination, validation,
workflow transitions, mapping, and side effects.

### Repository Layer

The repository layer owns MongoDB access, common filters, audit stamping, soft
delete behavior, and data access abstractions.

### DTO Layer

DTOs define the contract between HTTP clients and the API. They prevent persistence
entities from becoming accidental public contracts.

### Utility Layer

Utilities provide shared constants, enums, JWT helpers, current-context access,
email formatting, sanitization, and status helpers.

### Frontend Layer

React modules present workflows, call endpoint services, enforce display-level
permissions, manage local form state, and provide responsive user feedback.

## 19. Request Lifecycle

```text
Browser action
  -> React component
  -> feature service
  -> axios helper
  -> request interceptor
  -> CORS and forwarded headers
  -> JWT authentication
  -> policy and module authorization
  -> controller
  -> service validation
  -> repository query or update
  -> MongoDB
  -> Result response
  -> React response handling
  -> table, form, toast, or navigation update
```

Optional side effects branch after business persistence:

```text
Business result
  -> durable notification
  -> SignalR push
  -> queued email
  -> email log
```

## 20. Multi-Tenancy

The platform stores multiple companies in shared MongoDB infrastructure. The key
rule is that every protected business operation resolves company context from the
authenticated request rather than trusting a company identifier from ordinary input.

The main controls are:

- JWT company claim;
- `CurrentContext` accessors;
- repository tenant filters;
- explicit `CompanyId` in sensitive queries and updates;
- service-level ownership checks;
- company-scoped roles and permissions;
- public route filters for published tenant content.

A cross-tenant incident is a critical severity event. Tests must attempt a lookup,
update, notification, and public read using another company's identifiers.

## 21. Authentication and Authorization

Authentication answers “who is this caller?” Authorization answers “what may this
caller do?” Ownership answers “which record may this caller access?” Workflow checks
answer “is this action valid at this point in the lifecycle?”

The platform uses all four concepts.

### Authentication controls

- JWT bearer validation;
- issuer and audience validation;
- expiry validation;
- signing-key validation;
- BCrypt password verification;
- stored refresh-token expiry and revocation;
- SignalR query-token handling restricted to hub routes.

### Authorization controls

- `[Authorize]` for signed-in access;
- policy-based rules such as administrator-only operations;
- `ModulePermissionAttribute` for module actions;
- service-level ownership validation;
- workflow-state validation;
- expected-version checks for mutable decisions.

## 22. Data Model Principles

Most business documents use common lifecycle fields such as company identity, audit
values, soft-delete state, and version information.

Important principles:

1. Tenant identity is part of the data model, not only a UI concern.
2. User identity and employee business code are distinct.
3. Soft deletion preserves history where appropriate.
4. Source metadata protects records created by another workflow.
5. Version fields help prevent conflicting updates.
6. DTOs should expose only the data needed by their audience.
7. Public responses must be smaller and safer than internal responses.

## 23. MongoDB and Repositories

The repository abstraction supports common reads, inserts, updates, paging, filters,
soft deletion, and tenant boundaries. Specialized repositories support complex
attendance or payroll behavior.

Managers should understand the tradeoff: MongoDB provides flexible document storage,
but the application must deliberately enforce indexes, uniqueness, migration safety,
and cross-document consistency.

The database is not expected to replace business validation. The service layer must
validate a workflow before persisting its state.

## 24. Result and Error Model

Services commonly return `Result` or `Result<T>`. A response can be an HTTP success
with an application-level failure flag, so client code must inspect both transport
and business fields.

A useful operational distinction is:

- network or infrastructure failure: request could not be completed;
- authentication failure: the session is invalid or expired;
- authorization failure: the session is valid but the action is not permitted;
- validation failure: the request is not acceptable;
- conflict: the record changed or the workflow state disallows the action;
- dependency failure: a downstream provider such as email failed;
- successful business operation: the requested state was persisted.

Stable business codes are preferable to relying on message text for support and QA.

## 25. Frontend Architecture

The React application is organized by business module. Each module commonly contains:

- entry page;
- route tree;
- API service functions;
- TypeScript models;
- form components;
- validation schemas;
- filters and tables;
- permission checks;
- localized labels.

The application bootstraps routing, authentication, theme, internationalization,
notifications, and realtime connections before protected pages become available.

The axios layer attaches bearer authentication, language, and public company context
when appropriate. A single-flight refresh flow avoids a refresh storm when several
requests expire at once.

## 26. Routing and Navigation

Public surfaces include authentication, errors, career pages, and application entry
points. Protected surfaces are mounted beneath the authenticated layout.

The route and permission model should remain aligned:

```text
sidebar visibility
  -> route visibility
  -> component action visibility
  -> backend authorization
```

Only the final backend authorization is a security boundary. Earlier layers are
primarily usability and navigation controls.

## 27. Internationalization

The frontend loads locale JSON resources from `public/locales`. The active language
comes from user preferences and is also sent to the API through `Accept-Language`.

Translation changes should be reviewed for all supported languages. A feature is
not complete if its default language works but the locale files are incomplete.

## 28. Email Architecture

The email helper builds a branded HTML wrapper when a message is not already a full
HTML document. It sanitizes recipient lists and supports SMTP or SendGrid depending
on configuration.

The operational model is:

```text
business event
  -> queue or service call
  -> provider selection
  -> SMTP or SendGrid
  -> provider result
  -> application email log where applicable
```

Email configuration contains secrets and must be supplied through protected
configuration. Never commit API keys, passwords, or production recipient data.

## 29. Realtime Architecture

SignalR supports notification and chat hubs. The browser may be connected or offline.
Therefore, the application stores notification history before attempting a live push.

A realtime outage should degrade the user experience, not erase business evidence.
Support should compare the notification history with hub connection and provider logs.

## 30. Background Processing

Hosted services handle tasks such as queued work, leave accrual, birthday and
anniversary notifications, career alerts, attendance reminders, and payroll-related
processing where registered.

Background code has no dependable request `HttpContext`. Queue payloads must capture
company, actor, recipient, and target identifiers before the request ends.

Every background operation should be idempotent or guarded by a state check because
process restarts and retries are normal operational events.

---

# Part IV: Functional Module Catalogue

## 31. Company and Company Master

Purpose: configure company identity, departments, job titles, custom attributes,
policy documents, company branding, and public career information.

Dependencies: registration, employees, recruitment, eligibility rules, reporting.

Manager questions:

- Who owns company master data?
- What downstream forms depend on a department or job title?
- Which changes require audit or approval?
- Is the public company profile published intentionally?

## 32. Employees and Profiles

Purpose: maintain the people records used by every HR workflow.

Key concerns: identity, employment state, department, role, salary, profile data,
attachments, notification preferences, and self-service boundaries.

A profile page must distinguish an employee editing their own data from HR viewing
another employee. Sensitive fields must not be returned through broad self-service DTOs.

## 33. Roles and Permissions

Purpose: provide configurable least-privilege access per company.

The permission relationship is:

```text
Module -> ModulePermission -> RolePermission -> Role -> Employee
```

Permission changes should be tested with an allowed user, denied user, self-service
user, another-company user, and a user whose role was recently changed.

## 34. Attendance

Purpose: capture daily work status and time information that becomes a payroll input.

Controls: company and user identity, date normalization, status configuration,
weekly offs, holidays, source ownership, lock state, exception handling, and audit.

Attendance is a downstream record. It should not be used to conceal unresolved leave
or WFH conflicts.

## 35. Leave Management

Purpose: administer leave policies, balances, requests, approvals, withdrawals,
and attendance reconciliation.

Important distinction: request submission does not necessarily deduct a balance;
approval and reconciliation determine the final business impact.

## 36. Work From Home

Purpose: manage policy-controlled remote work rather than allowing arbitrary WFH
attendance codes.

The employee experience belongs to Leave Management. The API remains a dedicated
`api/wfh` contract with policy, ownership, quota, calendar, lock, timing, and audit
controls.

## 37. Calendar and Holidays

Purpose: define working-day exceptions consumed by leave, attendance, WFH, payroll,
and dashboards.

A calendar change can have financial impact. Changes after a period is locked should
be treated as controlled operational changes.

## 38. Payroll and Salary

Purpose: convert validated monthly HR inputs into reviewable payroll and payslips.

Dependencies: employee status, salary structure, attendance, leave reconciliation,
calendar rules, payroll divisor settings, exceptions, and locks.

Payroll should be regenerated only after the upstream defect is understood and fixed.

## 39. Recruitment and Public Careers

Purpose: publish jobs, collect candidate applications, manage applicant status,
retain process history, and present company career information.

Public endpoints need separate scrutiny because they do not have an authenticated
employee context. They must expose only active, published, intentionally public data.

## 40. Dashboard, Notices, Chat, and Notifications

Purpose: provide management summaries, internal announcements, collaboration, and
workflow awareness.

Aggregates must be company-scoped. Notices and notifications need recipient scoping.
Chat and SignalR failures must not become silent authorization bypasses.

---

# Part V: Management Controls and Risks

## 41. Control Objectives

A manager should expect the platform to protect these objectives:

1. Only authorized users can access protected functions.
2. Users cannot cross company boundaries.
3. Employees can perform permitted self-service actions without gaining admin access.
4. Approved workflow records cannot be silently overwritten.
5. Payroll depends on controlled and reviewable upstream inputs.
6. Public users see only public content.
7. Important events remain traceable after realtime or email delivery failures.
8. Releases can be built, tested, migrated, and verified repeatedly.
9. Secrets are supplied securely and are not embedded in source control.
10. Changes have an owner, acceptance criteria, test evidence, and rollback thinking.

## 42. Highest-Risk Areas

| Risk | Why it matters | Main control |
|---|---|---|
| Cross-tenant read | Confidential employee exposure | Current context plus company filters |
| Cross-tenant update | Data corruption and compliance risk | Company and ownership predicates |
| Missing authorization gate | Unauthorized business action | Attribute, policy, and service checks |
| Attendance overwrite | Payroll and audit corruption | Source ownership and mutation guard |
| Stale workflow decision | Conflicting approvals | Expected version checks |
| Unsafe public endpoint | External data exposure | Published filters and rate limiting |
| Migration partial state | Inconsistent deployment | Idempotent migration and backup |
| Secret leakage | Provider and account compromise | Secret store and deployment variables |
| Unsanitized rich text | Script execution or unsafe output | Server and client sanitization |
| Background context loss | Wrong tenant or recipient | Capture context before queueing |
| Email assumption | False completion status | Email logs and provider monitoring |
| Token storage exposure | Session theft after XSS | Sanitization and future storage hardening |

## 43. Security Review Questions

- Does every protected query include the current company boundary?
- Can a caller replace the company, employee, approver, or recipient identifier?
- Does a public endpoint expose internal IDs or private fields?
- Is a rich-text field sanitized before display and persistence?
- Are uploaded files type-checked, size-limited, and stored safely?
- Are refresh tokens revocable and monitored?
- Are SignalR access tokens accepted only on intended hub routes?
- Does every new state-changing endpoint declare an authorization rule?
- Are role names being mistaken for actual grants?
- Can a background task accidentally use request context from another tenant?

## 44. Data Governance Questions

- What is the source of truth for this field?
- Is the field required for payroll or legal reporting?
- How long should the record be retained?
- Is soft deletion preferable to physical deletion?
- Is the value included in audit history?
- Is the field safe for public responses?
- Does the field need a unique or compound index?
- What happens to historical records when a lookup value changes?
- Can two workflows update the same document at once?
- What migration or backfill is required for existing tenants?

## 45. Operational Risks

The system depends on MongoDB availability, correct configuration, worker execution,
email provider access, SignalR connectivity, file storage, and a compatible browser.

Operational readiness means checking the whole dependency chain, not just whether the
API process is listening on a port.

## 46. Performance Considerations

Performance work should begin with measured bottlenecks. Areas that deserve review:

- dashboard aggregation cost;
- employee and attendance grid paging;
- payroll generation and PDF rendering;
- MongoDB indexes for tenant, user, and date queries;
- notification fan-out for large companies;
- background queue depth;
- public recruitment traffic;
- frontend bundle size and lazy-loaded routes;
- duplicate API calls caused by component lifecycle behavior;
- slow email or provider calls accidentally placed in request paths.

## 47. Availability and Recovery

A recovery plan should define:

- MongoDB backup frequency;
- restore test frequency;
- API redeployment procedure;
- migration replay procedure;
- upload-file backup strategy;
- email provider recovery;
- SignalR degradation behavior;
- support communication during an outage;
- data reconciliation after recovery.

A backup that has never been restored is an assumption, not a tested recovery plan.

## 48. Auditability

The strongest audit trail combines:

- record audit fields;
- workflow status history;
- source ownership fields;
- notification records;
- email logs;
- migration execution records;
- application and infrastructure logs;
- deployment identifiers;
- reviewer and expected-version information.

Managers should ask whether an operator can reconstruct what happened, who acted,
which rules applied, and what downstream records changed.

---

# Part VI: Delivery Lifecycle

## 49. Requirement Definition

Every feature request should state:

- business problem;
- intended users;
- tenant scope;
- success measure;
- workflow states;
- permissions;
- affected modules;
- data changes;
- public or private exposure;
- notification requirements;
- reporting and payroll impact;
- acceptance and rejection examples;
- release and rollback considerations.

## 50. Design Review

A design review should trace the feature through:

```text
business outcome
  -> user journey
  -> route and screen
  -> API contract
  -> service rule
  -> persistence model
  -> permission and ownership
  -> side effects
  -> tests
  -> deployment
```

A design is incomplete when it describes only the screen.

## 51. Implementation Sequence

A practical implementation sequence is:

1. agree on the business vocabulary;
2. define state transitions and failure responses;
3. define DTOs and frontend models;
4. define persistence and indexes;
5. add backend constants and permissions;
6. implement service rules;
7. add controller routes;
8. add migration when needed;
9. add frontend service constants;
10. implement the frontend workflow;
11. add localization;
12. add focused tests;
13. run cross-repository smoke tests;
14. update documentation.

## 52. New Backend Feature Checklist

- [ ] Entity inherits the common base where appropriate.
- [ ] Tenant and lifecycle fields are understood.
- [ ] Create, update, response, and filter DTOs are separated.
- [ ] Mapster mappings are explicit where needed.
- [ ] Service is registered with the correct lifetime.
- [ ] Controller remains thin.
- [ ] Current context is used for user and company identity.
- [ ] Ownership is checked in the service.
- [ ] Module and permission constants are aligned.
- [ ] Authorization is applied to every protected action.
- [ ] Public exposure is intentional and filtered.
- [ ] Version checks protect mutable decisions.
- [ ] Source ownership is preserved for derived records.
- [ ] Background payload captures all required context.
- [ ] Email and notification behavior is non-blocking where appropriate.
- [ ] Migration is idempotent when persistence changes require it.
- [ ] Tests cover allowed, denied, cross-tenant, conflict, and retry cases.

## 53. New Frontend Feature Checklist

- [ ] Route is mounted in the correct public or private tree.
- [ ] Module enum matches the backend constant exactly.
- [ ] Service URL matches the backend route exactly.
- [ ] API request and response types are defined.
- [ ] Axios helpers are used instead of ad hoc clients.
- [ ] Loading, empty, success, business failure, and network failure states exist.
- [ ] Route permission gating is present where required.
- [ ] Action buttons are permission-gated for usability.
- [ ] Backend authorization is still assumed to be authoritative.
- [ ] Forms use existing validation patterns.
- [ ] Rich text is sanitized before rendering.
- [ ] Locale keys exist in supported translation files.
- [ ] Mobile and desktop layouts are reviewed.
- [ ] Tests cover user-visible behavior and failure states.

## 54. Pull Request Expectations

A good pull request explains:

- the user problem;
- the chosen design;
- affected repositories;
- data and migration impact;
- security impact;
- screenshots or API examples where useful;
- test commands and outcomes;
- known limitations;
- deployment order;
- rollback or recovery plan.

## 55. Build and Test Commands

Backend from the solution root:

```text
dotnet restore
dotnet build
dotnet test
```

API development run:

```text
cd Codeji.CMS.API
dotnet run
```

Migration run:

```text
cd Codeji.CMS.Migrations
dotnet run
```

Frontend:

```text
npm install
npm run typecheck
npm run lint
npm run test
npm run build
npm run dev
```

The exact installed SDK, database, secrets, and environment variables must match the
selected environment. A successful compile does not prove a successful integration.

## 56. Release Gates

A release should not be approved until:

- source builds successfully;
- focused tests pass;
- critical authorization cases pass;
- cross-tenant negative tests pass;
- migrations are reviewed and tested on a restored database;
- configuration is present without exposing secrets;
- frontend and backend contracts agree;
- smoke tests pass in the target environment;
- monitoring and support contacts are ready;
- deployment order is documented;
- business owner accepts known limitations.

## 57. Deployment Order

1. Confirm the target environment and database.
2. Back up the database and identify the backup reference.
3. Build backend, migration, and frontend artifacts.
4. Review configuration differences.
5. Run migrations against the intended database.
6. Deploy or restart the API.
7. Deploy the frontend artifact.
8. Verify authentication and a protected endpoint.
9. Verify one critical workflow end to end.
10. Verify notifications, email logs, and SignalR negotiation where applicable.
11. Monitor errors and latency.
12. Record deployment version and result.

## 58. Rollback Thinking

Rollback is not always a binary code revert. A release may include:

- application code;
- frontend bundle;
- database records;
- indexes;
- configuration;
- queued work;
- uploaded files.

Before release, classify each change as reversible, forward-fixable, or dependent
on restore. Mongo migrations have no automatic rollback in the current runner.

---

# Part VII: Testing and Support

## 59. Test Pyramid

### Unit tests

Validate deterministic business rules such as quota calculation, date handling,
permission resolution, mapping, status transitions, and error codes.

### Integration tests

Validate Mongo queries, indexes, tenant filters, migrations, and service wiring.

### API tests

Validate authentication, authorization, DTO binding, response shape, and route behavior.

### Frontend tests

Validate route visibility, form validation, loading states, response handling,
and user actions.

### End-to-end tests

Validate complete journeys across browser, API, database, worker, and provider boundaries.

## 60. Minimum Negative Tests

Every protected module should include:

- unauthenticated request;
- authenticated user without permission;
- authenticated user with permission for another module;
- same-company wrong-owner request;
- different-company identifier request;
- invalid state transition;
- stale expected version;
- locked-period mutation;
- duplicate or overlapping request;
- provider failure after business persistence.

## 61. Attendance Test Matrix

Test employee self-service, HR company view, manual marking, bulk marking, import,
weekly offs, holidays, required-time statuses, locked months, leave-owned rows,
WFH-owned rows, corrections, exceptions, and payroll dependency.

The key assertion is that a protected source row is not silently overwritten.

## 62. Leave Test Matrix

Test policy creation, status mapping, balance creation, monthly accrual, yearly
accrual, carry-over, minimum notice, holidays, weekends, half days, overlap,
balance shortage, acceptance, rejection, withdrawal, reconciliation, and locks.

## 63. WFH Test Matrix

Test disabled policy, eligibility by all employees, department, explicit employee,
exclusion, approval required, direct approval, weekly quota, advance notice,
holiday, weekly off, overlap, locked month, check-in window, check-out window,
minimum hours, cancellation, version conflict, notification, and email logging.

## 64. Payroll Test Matrix

Test salary structure, divisor settings, attendance readiness, unresolved exceptions,
locked months, draft generation, review, processing, employee visibility, payslip
creation, PDF rendering, and repeated generation behavior.

## 65. Recruitment Test Matrix

Test published and unpublished jobs, public company code, anonymous application,
rate limits, invalid files, duplicate applications, applicant status transitions,
process logs, notification behavior, and company isolation.

## 66. Support Triage

When a user reports a defect, capture:

- company;
- user and role;
- browser and version;
- exact route;
- date and business period;
- request identifier if available;
- visible error;
- timestamp and timezone;
- whether the issue affects one user or many;
- whether data was saved;
- notification and email evidence;
- recent deployment or configuration change.

Do not ask users to send passwords, JWTs, provider secrets, or unredacted sensitive
employee data through ordinary support channels.

## 67. Incident Severity

| Severity | Example | Response focus |
|---|---|---|
| Critical | Cross-tenant exposure, payroll corruption, total outage | Contain, preserve evidence, executive notification |
| High | Core workflow unavailable, unauthorized action possible | Mitigate quickly, technical lead ownership |
| Medium | Significant feature degraded with workaround | Prioritized fix and communication |
| Low | Cosmetic defect or narrow inconvenience | Planned correction |

## 68. Incident Questions

- What changed immediately before the incident?
- Is the problem code, data, configuration, provider, or infrastructure?
- Is the issue isolated to one tenant?
- Can further writes cause more damage?
- Is a lock or feature flag available?
- Which records were affected?
- Can a safe read-only query establish scope?
- Is a migration or forward fix safer than rollback?
- What evidence must be preserved?
- What customer communication is required?

---

# Part VIII: Advanced Engineering Guidance

## 69. Workflow State Design

Every workflow should define states, allowed transitions, actors, side effects,
reversal behavior, and audit records before implementation.

Example:

```text
Draft -> Pending -> Approved
                 -> Rejected
                 -> Returned
Approved -> Cancelled
```

A transition is a business operation, not merely a status field assignment.

## 70. Idempotency

Idempotency means repeating an operation does not create an incorrect second effect.
It matters for migrations, queued notifications, accrual, payroll generation,
retries, and webhook-like provider responses.

A manager should ask what happens if the process stops after persistence but before
notification, or after notification but before the worker records completion.

## 71. Optimistic Concurrency

Expected-version checks protect mutable workflows when two users act at nearly the
same time. A stale caller should receive a conflict and reload current state rather
than overwrite another decision.

This is especially important for approvals, cancellations, attendance review, and
payroll processing.

## 72. Source Provenance

Derived data should identify its origin when another workflow owns it. Attendance
rows generated from leave or WFH should retain source type, source ID, and version.

Provenance supports:

- safe edit restrictions;
- audit explanation;
- reconciliation;
- payroll investigation;
- correction design;
- support diagnosis.

## 73. Migration Engineering

The custom migration runner discovers migration classes, sorts by string ID, and
runs them against MongoDB. Each migration should:

1. use an ordered ID;
2. check whether it already ran;
3. handle partial state safely;
4. avoid assuming a fresh database;
5. use UTC timestamps;
6. create or repair indexes deliberately;
7. record completion only after successful work;
8. be tested against a database clone;
9. document tenant-wide backfills;
10. never be run concurrently by multiple deployment agents.

## 74. API Contract Governance

For each endpoint document:

- method and route;
- audience;
- authentication requirement;
- module permission;
- input fields;
- validation rules;
- response shape;
- business error codes;
- side effects;
- paging and sorting behavior;
- tenant and ownership rules;
- version requirements;
- operational logging.

The frontend service constant and backend route must be changed together.

## 75. Observability

Useful signals include:

- request count and latency;
- status-code distribution;
- business failure-code frequency;
- authentication failures;
- authorization denials;
- Mongo query latency;
- queue depth and worker failures;
- SignalR connection failures;
- email provider errors;
- migration execution result;
- frontend error boundary events;
- JavaScript bundle and API performance.

Metrics should be tenant-safe. Never put passwords, tokens, or unnecessary personal
data into logs.

## 76. Capacity Planning

Capacity discussions should include:

- number of companies;
- employees per company;
- attendance rows per month;
- applicant and resume volume;
- notification fan-out;
- payroll PDF volume;
- public career traffic;
- peak login and refresh traffic;
- background queue throughput;
- MongoDB storage and index growth;
- upload storage growth;
- backup size and restore time.

## 77. Maintainability

Maintainability improves when each change has one clear owner per concern:

- controller for transport;
- service for business rules;
- repository for persistence;
- DTO for contract;
- frontend service for API calls;
- component for presentation;
- migration for durable data change;
- documentation for operating knowledge.

Avoid moving business rules into controllers or duplicating them only in the browser.

## 78. Accessibility and Usability

Managers should include accessibility in acceptance criteria:

- keyboard navigation;
- meaningful labels;
- readable validation errors;
- sufficient contrast;
- predictable focus behavior;
- responsive tables and forms;
- localized dates and numbers;
- clear distinction between disabled and unavailable actions;
- confirmation for irreversible actions;
- non-color-only status communication.

## 79. Privacy and Data Minimization

Collect and expose only what a workflow needs. Candidate forms, employee profiles,
notification payloads, emails, logs, and public career pages have different audiences.

Free-text reasons and notes may be sensitive. Broad recipient notifications should
prefer a category or summary rather than forwarding unnecessary private detail.

## 80. Change Communication

A release announcement should explain:

- what changed;
- who benefits;
- what users need to do;
- whether policy or permission settings changed;
- whether existing records are affected;
- expected downtime;
- support contact;
- known limitations;
- verification status.

Technical release notes and manager communication should describe the same reality at
different levels of detail.

---

# Part IX: Manager Review Workbook

## 81. Product Review Prompts

1. What business problem is the product solving this quarter?
2. Which workflows create the most measurable value?
3. Which workflows create the greatest financial or compliance risk?
4. Which user group is currently underserved?
5. Which process still depends on spreadsheet workarounds?
6. What is the definition of success for the next release?
7. Which metric proves adoption rather than merely deployment?
8. Which customer or department will validate the change?
9. What is deliberately out of scope?
10. What decision is needed from management?

## 82. Architecture Review Prompts

1. Which repository owns the change?
2. Does the change cross the frontend/backend boundary?
3. What is the source of truth?
4. Is the operation tenant-scoped?
5. What is the state machine?
6. Which layer owns each rule?
7. What is the failure mode?
8. What happens when the process is retried?
9. What happens when a worker restarts?
10. What evidence remains after a failure?

## 83. Security Review Prompts

1. What is the least privilege required?
2. What prevents another tenant from being selected?
3. What prevents another employee from being selected?
4. What happens if the frontend is bypassed?
5. Is data public, private, or company-internal?
6. Are uploads and rich text protected?
7. How are secrets supplied?
8. What logs are safe to retain?
9. Can access be revoked immediately?
10. Which negative test demonstrates the control?

## 84. Release Review Prompts

1. Did backend and frontend artifacts come from the same change set?
2. Were migrations tested on a restored database?
3. Is the deployment order documented?
4. Is there a backup reference?
5. Were smoke tests run using realistic roles?
6. Were notifications and email logs checked?
7. Were public routes tested anonymously?
8. Were locked-period and conflict cases tested?
9. Is monitoring active after deployment?
10. Who owns the first post-release review?

## 85. Operations Review Prompts

1. What is the expected support symptom?
2. What evidence should support collect first?
3. Which logs and dashboards are authoritative?
4. Can the issue be contained without data mutation?
5. Is the problem isolated to one tenant?
6. Is a provider involved?
7. Is a forward fix safer than a rollback?
8. How will affected users be notified?
9. What data reconciliation is required?
10. What permanent prevention will be added?

## 86. Data Review Prompts

1. What records are created?
2. What records are updated?
3. What records are derived?
4. What records are retained?
5. What index supports the primary query?
6. What is the natural uniqueness rule?
7. Can a partial migration be resumed safely?
8. Can historical records be interpreted after a policy change?
9. What is the deletion or anonymization policy?
10. What report proves data completeness?

## 87. People and Delivery Review Prompts

1. Who owns the business decision?
2. Who owns the technical design?
3. Who writes the acceptance criteria?
4. Who tests the negative cases?
5. Who runs the migration?
6. Who monitors production?
7. Who handles support escalation?
8. Who communicates the change?
9. What knowledge is currently held by one person?
10. What should be documented before the next release?

---

# Part X: Appendices

## 88. Environment Preparation

Backend prerequisites normally include the approved .NET SDK, MongoDB access,
configuration files, email settings when needed, and any browser or PDF runtime
required by the target feature.

Frontend prerequisites include Node.js, npm, repository dependencies, environment
variables prefixed with `VITE_`, and access to the backend URL.

Never copy production secrets into a local configuration file that can be committed.

## 89. Configuration Categories

- MongoDB connection and database name;
- API and application URLs;
- JWT signing and expiry settings;
- SMTP or SendGrid configuration;
- CAPTCHA configuration;
- upload and public file settings;
- frontend API base URL;
- frontend public keys;
- supported locale configuration;
- deployment and proxy settings.

## 90. Definition of Done

A feature is done when:

- the business rule is agreed;
- the user journey works;
- the API contract is implemented;
- tenant and authorization controls are verified;
- frontend states are complete;
- localized text is present;
- tests pass;
- migration and deployment needs are known;
- documentation is updated;
- support knows how to identify failure;
- the business owner accepts the result.

## 91. Final Manager Summary

Codeji CMS is a connected HR and recruitment platform. Its most important design
principle is controlled flow: identity enables access, policies govern requests,
attendance records feed payroll, and workflow history explains decisions.

The most important management responsibilities are to protect scope, require clear
ownership, insist on negative testing, verify tenant isolation, treat migrations as
production changes, and ensure that operational evidence exists after deployment.

The product is strongest when the business process and technical implementation are
reviewed together. A screen can look correct while an ownership rule is missing; an
API can pass a unit test while a migration is unsafe; an email can be sent while the
business record remains incomplete. The review process must connect those layers.

## 92. Source Documentation Index

- `docs/architecture.md`
- `docs/auth-and-permissions.md`
- `docs/roles-and-permissions-complete-reference.md`
- `docs/permissions-current-flow.md`
- `docs/data-layer.md`
- `docs/migrations.md`
- `docs/work-from-home-module.md`
- `docs/attendance-module-current-flow-and-audit.md`
- `docs/leave-attendance-payroll-end-to-end-flow.md`
- `../CMS-React/docs/architecture.md`
- `../CMS-React/docs/modules.md`
- `../CMS-React/docs/routing-and-auth.md`
- `../CMS-React/docs/data-layer.md`
- `../CMS-React/docs/ui-patterns.md`
- `../CMS-React/docs/conventions.md`

## 93. Maintenance Record

When this document changes, record:

- date;
- author;
- affected release;
- sections changed;
- source files reviewed;
- business owner;
- technical reviewer;
- unresolved assumptions;
- follow-up work.

This guide should be treated as a maintained product asset, not a one-time report.

---

# Part XI: Operational Control Register

The register below is intended for release meetings, quarterly reviews, audit
preparation, and incident follow-up. Each numbered line is a concrete prompt that
can be assigned to an owner and supported by evidence. “Pass” means the evidence
exists for the target environment, not merely that the source code appears correct.

## 94. Product and Scope Controls

1. Confirm the release has a named business owner.
2. Confirm the release has a named technical owner.
3. Confirm the release has a named QA owner.
4. Confirm the user problem is written in business language.
5. Confirm the affected user groups are listed.
6. Confirm the intended tenant scope is listed.
7. Confirm success criteria are measurable.
8. Confirm non-goals are documented.
9. Confirm the feature has acceptance examples.
10. Confirm the feature has rejection examples.
11. Confirm the feature has a support contact.
12. Confirm the feature has a release communication owner.
13. Confirm the feature has a known dependency list.
14. Confirm the feature has a data owner.
15. Confirm the feature has a security reviewer.
16. Confirm the feature has a rollback decision.
17. Confirm the feature has a migration decision.
18. Confirm the feature has a notification decision.
19. Confirm the feature has a reporting decision.
20. Confirm the feature has a retention decision.
21. Confirm the feature has a privacy classification.
22. Confirm the feature has a browser support decision.
23. Confirm the feature has a localization decision.
24. Confirm the feature has an accessibility decision.
25. Confirm the feature has a performance expectation.
26. Confirm the feature has an availability expectation.
27. Confirm the feature has a support runbook.
28. Confirm the feature has a test plan.
29. Confirm the feature has a deployment order.
30. Confirm the feature has a post-release review date.

## 95. Company and Tenant Controls

31. Confirm every company record has a stable identifier.
32. Confirm company data is isolated from other companies.
33. Confirm company registration creates expected defaults.
34. Confirm registration cannot create duplicate identities unexpectedly.
35. Confirm registration failures do not leave unsafe partial access.
36. Confirm the administrator receives the expected session response.
37. Confirm the default role set is present.
38. Confirm default permissions are reviewed.
39. Confirm default departments are intentional.
40. Confirm default job titles are intentional.
41. Confirm company master edits are tenant-scoped.
42. Confirm lookup changes identify downstream consumers.
43. Confirm company policy versions remain traceable.
44. Confirm company branding is safe for public display.
45. Confirm public company codes are not confused with internal IDs.
46. Confirm unpublished profile data is not public.
47. Confirm inactive company content is filtered.
48. Confirm tenant filters exist on reads.
49. Confirm tenant filters exist on updates.
50. Confirm tenant filters exist on deletes.
51. Confirm tenant filters exist on notification recipients.
52. Confirm tenant filters exist on dashboard aggregates.
53. Confirm tenant filters exist on background jobs.
54. Confirm tenant filters exist on public handoffs.
55. Confirm cross-tenant negative tests pass.
56. Confirm company deletion policy is documented.
57. Confirm company suspension behavior is documented.
58. Confirm company data export ownership is documented.
59. Confirm company data retention ownership is documented.
60. Confirm company support escalation is documented.

## 96. Identity and Session Controls

61. Confirm password input is never logged.
62. Confirm password storage uses a one-way hash.
63. Confirm password verification uses the stored hash.
64. Confirm login rejects invalid credentials consistently.
65. Confirm login does not reveal which credential failed.
66. Confirm JWT issuer validation is enabled.
67. Confirm JWT audience validation is enabled.
68. Confirm JWT lifetime validation is enabled.
69. Confirm JWT signing-key validation is enabled.
70. Confirm signing secrets are external configuration.
71. Confirm access-token expiry is understood by support.
72. Confirm refresh-token expiry is understood by support.
73. Confirm refresh tokens are stored with expiry.
74. Confirm refresh tokens can be revoked.
75. Confirm logout revokes the intended session state.
76. Confirm expired refresh tokens are rejected.
77. Confirm malformed tokens return an authentication failure.
78. Confirm token claims identify the expected user.
79. Confirm token claims identify the expected company.
80. Confirm token claims identify the expected role.
81. Confirm services use the current-context helper.
82. Confirm services do not trust client company IDs.
83. Confirm services do not trust client owner IDs.
84. Confirm SignalR token handling is path-restricted.
85. Confirm public endpoints do not require private session claims.
86. Confirm private endpoints reject anonymous requests.
87. Confirm frontend logout clears local session state.
88. Confirm refresh failure returns the user to authentication.
89. Confirm concurrent expiry creates one refresh request.
90. Confirm a failed refresh does not replay requests forever.

## 97. Role and Permission Controls

91. Confirm every protected action has an access decision.
92. Confirm module constants match across repositories.
93. Confirm permission constants match across repositories.
94. Confirm permission data exists in the target database.
95. Confirm role grants are company-scoped.
96. Confirm administrator access is still data-backed.
97. Confirm HR access is still data-backed.
98. Confirm employee access is self-service scoped.
99. Confirm manager access requires assignment where applicable.
100. Confirm route gates are not the only control.
101. Confirm button hiding is not the only control.
102. Confirm controller attributes are reviewed.
103. Confirm policy handlers are reviewed.
104. Confirm service ownership checks are reviewed.
105. Confirm denied requests return a consistent result.
106. Confirm a user cannot select another company.
107. Confirm a user cannot select another owner.
108. Confirm a requester cannot approve their own request.
109. Confirm permission changes are audited.
110. Confirm permission changes have a support procedure.
111. Confirm new modules receive migration support.
112. Confirm existing roles receive intended grants.
113. Confirm disabled modules are unavailable.
114. Confirm stale frontend permissions are refreshed.
115. Confirm role changes apply on the next authorization check.
116. Confirm permission tests cover positive cases.
117. Confirm permission tests cover negative cases.
118. Confirm permission tests cover another tenant.
119. Confirm permission tests cover an expired session.
120. Confirm permission documentation is updated.

## 98. Employee Controls

121. Confirm employee creation is company-scoped.
122. Confirm employee invitation is company-scoped.
123. Confirm employee identifiers are validated.
124. Confirm employee business codes are not assumed global.
125. Confirm employee user identity is stable.
126. Confirm inactive employees cannot perform active workflows.
127. Confirm self-service derives the current user server-side.
128. Confirm self-service cannot edit protected employment fields.
129. Confirm HR can view permitted employee fields.
130. Confirm HR cannot view another tenant employee.
131. Confirm profile attachments follow access rules.
132. Confirm profile rich text is sanitized.
133. Confirm profile imports validate every row.
134. Confirm profile imports report rejected rows.
135. Confirm bulk imports do not partially misassign tenants.
136. Confirm employee department changes are traceable.
137. Confirm employee job-title changes are traceable.
138. Confirm employee role changes are traceable.
139. Confirm employee salary changes are controlled.
140. Confirm employee notification preferences are scoped.
141. Confirm employee deletion impact is reviewed.
142. Confirm employee history remains available when required.
143. Confirm employee list paging is bounded.
144. Confirm employee search is tenant-scoped.
145. Confirm employee filters have empty states.
146. Confirm employee forms display validation errors.
147. Confirm employee forms display server errors.
148. Confirm employee forms support localization.
149. Confirm employee forms support keyboard use.
150. Confirm employee documentation is current.

## 99. Attendance Controls

151. Confirm attendance identity includes company.
152. Confirm attendance identity includes user.
153. Confirm attendance identity includes business date.
154. Confirm business dates are normalized.
155. Confirm status codes belong to the company.
156. Confirm inactive status codes cannot be selected.
157. Confirm required-time statuses require times.
158. Confirm leave statuses do not require inappropriate times.
159. Confirm weekly offs are evaluated.
160. Confirm holidays are evaluated.
161. Confirm locked months reject mutations.
162. Confirm source-owned rows are identified.
163. Confirm leave-owned rows are protected.
164. Confirm WFH-owned rows are protected.
165. Confirm manual edits do not silently overwrite source rows.
166. Confirm bulk operations prevalidate before writing.
167. Confirm bulk operations report protected skips.
168. Confirm attendance imports report invalid rows.
169. Confirm attendance imports preserve tenant identity.
170. Confirm employee self-service uses token identity.
171. Confirm company grid access has a permission gate.
172. Confirm company grid paging is bounded.
173. Confirm attendance summaries use persisted records.
174. Confirm exception calculations are repeatable.
175. Confirm exception review records the decision.
176. Confirm correction requests identify the owner.
177. Confirm correction requests have a status.
178. Confirm total hours are derived consistently.
179. Confirm server time rules are documented.
180. Confirm timezone behavior is documented.
181. Confirm late and early rules are testable.
182. Confirm attendance updates use expected versions where needed.
183. Confirm attendance indexes support company and date queries.
184. Confirm attendance negative tests pass.
185. Confirm payroll reads controlled attendance.
186. Confirm attendance UI displays persisted source metadata.
187. Confirm attendance UI supports read-only mode.
188. Confirm attendance UI supports empty state.
189. Confirm attendance UI supports loading state.
190. Confirm attendance documentation is current.

## 100. Leave Controls

191. Confirm every leave policy belongs to a company.
192. Confirm policy names and codes are validated.
193. Confirm policy status is respected.
194. Confirm policy accrual settings are coherent.
195. Confirm carry-over settings are coherent.
196. Confirm advance notice is enforced.
197. Confirm weekend inclusion is enforced.
198. Confirm holiday inclusion is enforced.
199. Confirm attendance mapping exists.
200. Confirm attendance mapping is active.
201. Confirm mapped leave status is appropriate.
202. Confirm balances are created for eligible employees.
203. Confirm balances are company-scoped.
204. Confirm balances cannot become negative unexpectedly.
205. Confirm monthly accrual is idempotent.
206. Confirm yearly accrual is idempotent.
207. Confirm maximum balances are applied.
208. Confirm carry-over limits are applied.
209. Confirm request dates are normalized.
210. Confirm end date cannot precede start date.
211. Confirm overlap is rejected.
212. Confirm zero-working-day requests are rejected.
213. Confirm pending requests reserve intended capacity.
214. Confirm approval rechecks the balance.
215. Confirm approval identifies the reviewer.
216. Confirm rejection does not deduct incorrectly.
217. Confirm withdrawal reverses applicable effects.
218. Confirm accepted leave reconciles attendance.
219. Confirm rejected leave does not create active attendance.
220. Confirm reconciliation is tenant-scoped.
221. Confirm reconciliation is version-aware.
222. Confirm locked months reject invalid changes.
223. Confirm leave UI separates policy and request roles.
224. Confirm leave UI shows clear status labels.
225. Confirm leave UI handles server business failures.
226. Confirm leave UI supports localization.
227. Confirm leave tests cover another employee.
228. Confirm leave tests cover another company.
229. Confirm leave tests cover concurrency.
230. Confirm leave documentation is current.

## 101. WFH Controls

231. Confirm WFH policy enabled state is respected.
232. Confirm WFH policy ownership is company-scoped.
233. Confirm policy editing requires permission.
234. Confirm employee context is server-calculated.
235. Confirm employee eligibility is server-calculated.
236. Confirm explicit exclusions are enforced.
237. Confirm explicit inclusions are enforced.
238. Confirm department eligibility is enforced.
239. Confirm employment-type eligibility is enforced.
240. Confirm all-employee eligibility is intentional.
241. Confirm effective dates are enforced.
242. Confirm company timezone is validated.
243. Confirm invalid timezone fallback is known.
244. Confirm advance notice is enforced.
245. Confirm weekly quota is enforced.
246. Confirm pending requests count as designed.
247. Confirm approved requests count as designed.
248. Confirm rejected requests do not consume quota.
249. Confirm cancelled requests do not consume quota.
250. Confirm overlap is rejected.
251. Confirm weekly offs are handled.
252. Confirm holidays are handled.
253. Confirm locked dates are rejected.
254. Confirm reason requirements are enforced.
255. Confirm direct approval creates approved state.
256. Confirm approval-required policy creates pending state.
257. Confirm only assigned reviewers can decide.
258. Confirm self-approval is rejected.
259. Confirm decisions require expected version.
260. Confirm decision history is append-only.
261. Confirm approval reconciles attendance.
262. Confirm WFH attendance includes source type.
263. Confirm WFH attendance includes source ID.
264. Confirm WFH attendance includes source version.
265. Confirm clock-in uses server time.
266. Confirm clock-out requires prior clock-in.
267. Confirm check-in window is enforced.
268. Confirm check-out window is enforced.
269. Confirm minimum hours are enforced.
270. Confirm insufficient hours create an exception.
271. Confirm employee sees only own requests.
272. Confirm team reviewer sees assigned team.
273. Confirm all-reviewer sees current company.
274. Confirm WFH notifications are durable.
275. Confirm WFH SignalR delivery is additional.
276. Confirm WFH email logging is available.
277. Confirm free-text reason is minimized in broad notices.
278. Confirm WFH route is under Leave Management in React.
279. Confirm WFH UI exposes no policy editor to employees.
280. Confirm WFH documentation is current.

## 102. Payroll Controls

281. Confirm salary structures are company-scoped.
282. Confirm salary effective dates are understood.
283. Confirm locked periods cannot be changed casually.
284. Confirm payroll divisor policy is configured.
285. Confirm attendance is ready before generation.
286. Confirm leave reconciliation is ready before generation.
287. Confirm unresolved exceptions are visible.
288. Confirm payroll generation has a defined owner.
289. Confirm generation creates a reviewable draft.
290. Confirm processing is a separate action.
291. Confirm unprocessed payroll is not employee-visible.
292. Confirm processed payroll is employee-visible as intended.
293. Confirm management payroll access is permission-controlled.
294. Confirm employee payroll access is ownership-controlled.
295. Confirm payroll generation is repeatable or guarded.
296. Confirm duplicate payroll rows are prevented.
297. Confirm payroll edits are auditable.
298. Confirm payroll exceptions identify the source.
299. Confirm payslip PDF rendering is tested.
300. Confirm headless browser dependencies are available.
301. Confirm payslip files have safe access.
302. Confirm payslip downloads are tenant-safe.
303. Confirm payroll month and timezone conventions are clear.
304. Confirm payroll errors are supportable.
305. Confirm payroll documentation is current.

## 103. Recruitment Controls

306. Confirm a vacancy has a business owner.
307. Confirm a vacancy belongs to one company.
308. Confirm vacancy title is validated.
309. Confirm vacancy description is sanitized.
310. Confirm vacancy status is explicit.
311. Confirm publish state is explicit.
312. Confirm closing date behavior is documented.
313. Confirm public jobs show only published records.
314. Confirm inactive companies do not show jobs.
315. Confirm public company code is stable.
316. Confirm public code lookup is tenant-safe.
317. Confirm internal job views require access.
318. Confirm recruiter access is permission-controlled.
319. Confirm applicants inherit company from vacancy.
320. Confirm applicant company cannot be selected by input.
321. Confirm applicant email is validated.
322. Confirm resume file size is limited.
323. Confirm resume file type is limited.
324. Confirm resume storage path is controlled.
325. Confirm resume download is authorized.
326. Confirm anonymous application fields are minimal.
327. Confirm CAPTCHA or rate limiting is configured.
328. Confirm sensitive public routes use rate limiting.
329. Confirm duplicate application behavior is defined.
330. Confirm application status vocabulary is documented.
331. Confirm status transition rules are documented.
332. Confirm status changes identify the actor.
333. Confirm process logs are append-only.
334. Confirm process logs are company-scoped.
335. Confirm applicant comments are access-controlled.
336. Confirm candidate emails are queued safely.
337. Confirm candidate email failures are visible.
338. Confirm external URLs reject unsafe schemes.
339. Confirm external URLs reject loopback targets.
340. Confirm public responses omit internal notes.
341. Confirm public responses omit reviewer data.
342. Confirm public responses omit tenant internals.
343. Confirm job filters are bounded.
344. Confirm applicant filters are bounded.
345. Confirm applicant lists paginate.
346. Confirm applicant search is tenant-scoped.
347. Confirm recruiter screens have loading states.
348. Confirm recruiter screens have empty states.
349. Confirm recruiter screens have failure states.
350. Confirm recruitment translations exist.
351. Confirm recruitment accessibility is reviewed.
352. Confirm candidate form errors are understandable.
353. Confirm candidate upload errors are actionable.
354. Confirm process history can support an investigation.
355. Confirm job publishing is included in release tests.
356. Confirm anonymous routes are included in release tests.
357. Confirm rate-limit responses are supportable.
358. Confirm recruitment documentation is current.

## 104. Company Communication Controls

359. Confirm notices have an author.
360. Confirm notices have a company.
361. Confirm notice audience is defined.
362. Confirm notice type is validated.
363. Confirm notice publication state is explicit.
364. Confirm notice content is sanitized.
365. Confirm notice edits are controlled.
366. Confirm notice deletion behavior is understood.
367. Confirm notice recipients are company-scoped.
368. Confirm recipient records identify the user.
369. Confirm recipient records support unread state.
370. Confirm notification history survives offline use.
371. Confirm SignalR is not the only delivery path.
372. Confirm duplicate notifications are controlled.
373. Confirm notification retry behavior is understood.
374. Confirm notification queue failures are visible.
375. Confirm email is not treated as transaction proof.
376. Confirm email logs identify delivery attempts.
377. Confirm invalid recipients are filtered.
378. Confirm duplicate recipients are removed.
379. Confirm CC recipients are controlled.
380. Confirm BCC recipients are controlled.
381. Confirm provider credentials are protected.
382. Confirm SMTP configuration is tested.
383. Confirm SendGrid configuration is tested.
384. Confirm provider fallback behavior is documented.
385. Confirm branded email output is reviewed.
386. Confirm email HTML is safe.
387. Confirm email attachments are limited.
388. Confirm email attachments are authorized.
389. Confirm notification payloads minimize personal data.
390. Confirm free-text notes are not broadcast unnecessarily.
391. Confirm hub connections authenticate correctly.
392. Confirm hub routes are path-restricted.
393. Confirm hub disconnects are recoverable.
394. Confirm online presence is non-authoritative.
395. Confirm chat messages have an owner.
396. Confirm chat access is company-scoped.
397. Confirm notice lists are pageable.
398. Confirm notification lists are pageable.
399. Confirm notification UI has an empty state.
400. Confirm notification UI has a failure state.
401. Confirm bell counts refresh correctly.
402. Confirm locale text exists for notifications.
403. Confirm email subjects are recognizable.
404. Confirm support can query email logs.
405. Confirm support can query notification history.
406. Confirm communication documentation is current.

## 105. Calendar and Dashboard Controls

407. Confirm calendar records have a company.
408. Confirm holiday dates are normalized.
409. Confirm holiday type is validated.
410. Confirm duplicate holidays are handled.
411. Confirm holiday edits are permission-controlled.
412. Confirm calendar events have an owner.
413. Confirm calendar events are tenant-scoped.
414. Confirm calendar visibility is intentional.
415. Confirm calendar changes affect leave rules.
416. Confirm calendar changes affect WFH rules.
417. Confirm calendar changes affect attendance rules.
418. Confirm calendar changes affect payroll assumptions.
419. Confirm locked-period calendar changes are reviewed.
420. Confirm calendar imports validate rows.
421. Confirm calendar filters are bounded.
422. Confirm calendar UI supports locale dates.
423. Confirm calendar UI supports timezone display.
424. Confirm calendar UI has empty state.
425. Confirm calendar UI has loading state.
426. Confirm calendar UI has failure state.
427. Confirm dashboard aggregates use current company.
428. Confirm dashboard counts exclude deleted records.
429. Confirm dashboard counts use defined status rules.
430. Confirm dashboard date windows are documented.
431. Confirm dashboard payroll metrics are labeled.
432. Confirm dashboard recruitment metrics are labeled.
433. Confirm dashboard leave metrics are labeled.
434. Confirm dashboard attendance metrics are labeled.
435. Confirm dashboard data is not treated as ledger truth.
436. Confirm dashboard queries are bounded.
437. Confirm dashboard queries have indexes.
438. Confirm dashboard loading skeletons work.
439. Confirm dashboard chart empty states work.
440. Confirm dashboard chart errors are visible.
441. Confirm dashboard is accessible without color alone.
442. Confirm dashboard is usable on supported screens.
443. Confirm dashboard metrics have an owner.
444. Confirm dashboard metric definitions are documented.
445. Confirm calendar and dashboard documentation is current.

## 106. Frontend Delivery Controls

446. Confirm the route is in the correct route tree.
447. Confirm protected routes use the established guard.
448. Confirm public routes are intentionally public.
449. Confirm lazy loading is used for large modules.
450. Confirm the page has a loading state.
451. Confirm the page has an empty state.
452. Confirm the page has a success state.
453. Confirm the page has an application failure state.
454. Confirm the page has a network failure state.
455. Confirm the page has a retry path where useful.
456. Confirm endpoint paths use ServiceUrl constants.
457. Confirm service functions use axios helpers.
458. Confirm service functions have request types.
459. Confirm service functions have response types.
460. Confirm service functions do not swallow errors.
461. Confirm components inspect success flags.
462. Confirm components inspect business status codes.
463. Confirm components show useful error messages.
464. Confirm forms use existing validation conventions.
465. Confirm forms prevent duplicate submission.
466. Confirm forms show server validation errors.
467. Confirm destructive actions require confirmation.
468. Confirm action buttons use permission checks.
469. Confirm route permissions match backend names.
470. Confirm permission checks are not security claims.
471. Confirm local state is used for local concerns.
472. Confirm shared state is used only when needed.
473. Confirm Redux selectors are typed.
474. Confirm filters persist only when intended.
475. Confirm API calls are not duplicated unnecessarily.
476. Confirm query invalidation is understood.
477. Confirm stale data behavior is acceptable.
478. Confirm token refresh retries once.
479. Confirm token refresh cannot loop.
480. Confirm auth failure redirects cleanly.
481. Confirm locale files contain new keys.
482. Confirm locale fallback is understood.
483. Confirm date formatting uses the project convention.
484. Confirm timezone display is intentional.
485. Confirm rich text uses sanitization.
486. Confirm file inputs have size feedback.
487. Confirm file inputs have type feedback.
488. Confirm tables remain usable on narrow screens.
489. Confirm dialogs have keyboard focus behavior.
490. Confirm buttons have accessible labels.
491. Confirm status indicators do not rely only on color.
492. Confirm images have meaningful alternatives.
493. Confirm browser console has no new errors.
494. Confirm network requests have no unexpected loops.
495. Confirm frontend build passes.
496. Confirm frontend typecheck passes.
497. Confirm frontend lint passes.
498. Confirm frontend focused tests pass.
499. Confirm frontend documentation is current.

## 107. Data and Migration Controls

500. Confirm a data change has a named owner.
501. Confirm the affected collection is identified.
502. Confirm the affected tenants are identified.
503. Confirm the natural key is identified.
504. Confirm the new field default is understood.
505. Confirm missing fields are safe for old documents.
506. Confirm a backfill is required or ruled out.
507. Confirm an index is required or ruled out.
508. Confirm uniqueness is required or ruled out.
509. Confirm the migration ID sorts correctly.
510. Confirm the migration checks its own history.
511. Confirm the migration handles partial state.
512. Confirm the migration uses UTC timestamps.
513. Confirm the migration is safe to rerun.
514. Confirm the migration does not assume a fresh database.
515. Confirm the migration does not use HTTP context.
516. Confirm the migration does not send unnecessary email.
517. Confirm the migration does not run concurrently.
518. Confirm the migration has a target database.
519. Confirm the target database has a backup.
520. Confirm the backup has a restore reference.
521. Confirm the migration has been tested on a clone.
522. Confirm the migration output is recorded.
523. Confirm partial failure behavior is known.
524. Confirm forward-fix behavior is known.
525. Confirm no rollback is being assumed.
526. Confirm tenant backfill volume is estimated.
527. Confirm tenant backfill duration is estimated.
528. Confirm index build impact is estimated.
529. Confirm production execution has an owner.
530. Confirm production execution has a maintenance window.
531. Confirm migration logging avoids secrets.
532. Confirm migration logging avoids personal data.
533. Confirm migration queries use correct collection names.
534. Confirm migration seed ordering is correct.
535. Confirm permission seed ordering is correct.
536. Confirm module backfill ordering is correct.
537. Confirm default data conflicts are handled.
538. Confirm duplicate lookup rows are handled.
539. Confirm migration records are queryable.
540. Confirm migration completion is verified.
541. Confirm application restart follows migration.
542. Confirm health checks follow restart.
543. Confirm one protected workflow is smoke-tested.
544. Confirm one public workflow is smoke-tested.
545. Confirm migration documentation is current.

## 108. API and Contract Controls

546. Confirm method and route are documented.
547. Confirm the audience is documented.
548. Confirm authentication is documented.
549. Confirm authorization is documented.
550. Confirm ownership is documented.
551. Confirm tenant scope is documented.
552. Confirm required fields are documented.
553. Confirm optional fields are documented.
554. Confirm enum values are documented.
555. Confirm date format is documented.
556. Confirm timezone assumptions are documented.
557. Confirm paging is documented.
558. Confirm sorting is documented.
559. Confirm success response is documented.
560. Confirm failure response is documented.
561. Confirm stable error codes are documented.
562. Confirm conflict responses are documented.
563. Confirm version fields are documented.
564. Confirm side effects are documented.
565. Confirm notification side effects are documented.
566. Confirm email side effects are documented.
567. Confirm file side effects are documented.
568. Confirm public fields are minimized.
569. Confirm internal fields are excluded publicly.
570. Confirm request IDs are available where needed.
571. Confirm logs identify the operation.
572. Confirm logs identify the company safely.
573. Confirm logs avoid tokens.
574. Confirm logs avoid passwords.
575. Confirm logs avoid unnecessary personal data.
576. Confirm controller binding is validated.
577. Confirm service validation remains authoritative.
578. Confirm repository filters remain authoritative.
579. Confirm frontend path matches backend path.
580. Confirm frontend request type matches DTO.
581. Confirm frontend response type matches result.
582. Confirm error handling matches actual transport behavior.
583. Confirm anonymous endpoint behavior is tested.
584. Confirm authenticated endpoint behavior is tested.
585. Confirm invalid input behavior is tested.
586. Confirm stale version behavior is tested.
587. Confirm duplicate request behavior is tested.
588. Confirm another-tenant behavior is tested.
589. Confirm rate-limit behavior is tested.
590. Confirm contract documentation is current.

## 109. Observability Controls

591. Confirm API availability is monitored.
592. Confirm API latency is monitored.
593. Confirm error rates are monitored.
594. Confirm authentication failures are monitored.
595. Confirm authorization denials are monitored.
596. Confirm business failure codes are counted.
597. Confirm Mongo connectivity is monitored.
598. Confirm Mongo query latency is monitored.
599. Confirm Mongo storage is monitored.
600. Confirm index health is reviewed.
601. Confirm worker health is monitored.
602. Confirm queue depth is monitored.
603. Confirm queue failure count is monitored.
604. Confirm accrual completion is monitored.
605. Confirm notification processing is monitored.
606. Confirm email provider failures are monitored.
607. Confirm email log growth is monitored.
608. Confirm SignalR connections are monitored.
609. Confirm SignalR failures are monitored.
610. Confirm upload storage is monitored.
611. Confirm PDF generation failures are monitored.
612. Confirm public recruitment traffic is monitored.
613. Confirm rate-limit events are monitored.
614. Confirm frontend runtime errors are monitored.
615. Confirm frontend performance is monitored.
616. Confirm bundle size is reviewed.
617. Confirm deployment version is visible.
618. Confirm migration version is visible.
619. Confirm environment identity is visible safely.
620. Confirm dashboards have alert owners.
621. Confirm alerts have severity.
622. Confirm alerts have runbooks.
623. Confirm alerts have escalation contacts.
624. Confirm alert thresholds are reviewed.
625. Confirm noisy alerts are corrected.
626. Confirm logs have retention rules.
627. Confirm logs have access controls.
628. Confirm logs are searchable by time.
629. Confirm logs are searchable by operation.
630. Confirm logs are searchable by tenant safely.
631. Confirm sensitive fields are redacted.
632. Confirm correlation identifiers are preserved.
633. Confirm provider response details are safe.
634. Confirm support can access required evidence.
635. Confirm observability documentation is current.

## 110. Reliability and Recovery Controls

636. Confirm backup frequency is defined.
637. Confirm backup ownership is defined.
638. Confirm backup retention is defined.
639. Confirm restore testing is scheduled.
640. Confirm restore duration is measured.
641. Confirm restore data completeness is checked.
642. Confirm upload files are backed up.
643. Confirm configuration recovery is possible.
644. Confirm secret recovery is possible.
645. Confirm migration recovery is possible.
646. Confirm queued-work recovery is possible.
647. Confirm duplicate-work behavior is known.
648. Confirm provider outage behavior is known.
649. Confirm email outage does not erase business state.
650. Confirm SignalR outage does not erase notices.
651. Confirm browser refresh recovers state.
652. Confirm expired session recovery works.
653. Confirm API restart recovery works.
654. Confirm worker restart recovery works.
655. Confirm Mongo reconnect behavior is tested.
656. Confirm partial deployment behavior is understood.
657. Confirm frontend and API version compatibility is understood.
658. Confirm maintenance mode behavior is documented.
659. Confirm support communication is templated.
660. Confirm incident severity is assigned.
661. Confirm incident commander is assigned.
662. Confirm evidence preservation is assigned.
663. Confirm customer communication is assigned.
664. Confirm data scope is established.
665. Confirm containment is considered first.
666. Confirm read-only investigation is preferred initially.
667. Confirm emergency writes are reviewed.
668. Confirm recovery validation is defined.
669. Confirm reconciliation after recovery is defined.
670. Confirm lessons learned are recorded.
671. Confirm permanent prevention is assigned.
672. Confirm recovery drills are scheduled.
673. Confirm recovery documents are accessible.
674. Confirm recovery documentation is current.

## 111. QA Evidence Controls

675. Confirm every acceptance criterion has a test.
676. Confirm every rejection criterion has a test.
677. Confirm unit tests are deterministic.
678. Confirm service tests isolate external providers.
679. Confirm API tests include authentication.
680. Confirm API tests include authorization.
681. Confirm API tests include tenant isolation.
682. Confirm API tests include ownership isolation.
683. Confirm API tests include concurrency.
684. Confirm API tests include locked periods.
685. Confirm API tests include duplicate requests.
686. Confirm API tests include provider failure.
687. Confirm frontend tests include loading.
688. Confirm frontend tests include empty data.
689. Confirm frontend tests include business errors.
690. Confirm frontend tests include network errors.
691. Confirm frontend tests include permissions.
692. Confirm frontend tests include form validation.
693. Confirm frontend tests include localization.
694. Confirm frontend tests include responsive behavior.
695. Confirm smoke tests use realistic roles.
696. Confirm smoke tests use realistic dates.
697. Confirm smoke tests use realistic tenant data.
698. Confirm smoke tests avoid production mutation.
699. Confirm test data is disposable.
700. Confirm test data does not contain secrets.
701. Confirm test data does not contain unnecessary personal data.
702. Confirm migration tests use a database clone.
703. Confirm index tests use realistic query shape.
704. Confirm payroll tests include PDF output.
705. Confirm recruitment tests include anonymous access.
706. Confirm SignalR tests include disconnected clients.
707. Confirm notification tests inspect durable records.
708. Confirm email tests inspect logs.
709. Confirm retry tests do not create duplicates.
710. Confirm regression tests cover changed modules.
711. Confirm critical tests run in CI.
712. Confirm test failures block release when required.
713. Confirm flaky tests have owners.
714. Confirm skipped tests have reasons.
715. Confirm test reports are archived.
716. Confirm test evidence identifies the build.
717. Confirm test evidence identifies the environment.
718. Confirm test evidence identifies the data set.
719. Confirm QA signs the release recommendation.

## 112. Support Runbook Controls

720. Confirm support captures company.
721. Confirm support captures user role.
722. Confirm support captures route.
723. Confirm support captures timestamp.
724. Confirm support captures timezone.
725. Confirm support captures browser.
726. Confirm support captures visible message.
727. Confirm support captures business date.
728. Confirm support captures affected record type.
729. Confirm support captures whether data saved.
730. Confirm support avoids collecting passwords.
731. Confirm support avoids collecting tokens.
732. Confirm support avoids collecting secrets.
733. Confirm support redacts personal data.
734. Confirm support checks recent deployments.
735. Confirm support checks configuration changes.
736. Confirm support checks provider status.
737. Confirm support checks worker status.
738. Confirm support checks migration status.
739. Confirm support checks notification history.
740. Confirm support checks email logs.
741. Confirm support checks application logs.
742. Confirm support checks tenant scope.
743. Confirm support checks reproducibility.
744. Confirm support distinguishes UI and API failure.
745. Confirm support distinguishes auth and permission failure.
746. Confirm support distinguishes validation and conflict.
747. Confirm support distinguishes provider failure.
748. Confirm support escalates security concerns immediately.
749. Confirm support escalates payroll concerns immediately.
750. Confirm support escalates cross-tenant concerns immediately.
751. Confirm support uses severity definitions.
752. Confirm support records containment.
753. Confirm support records user communication.
754. Confirm support records resolution.
755. Confirm support records follow-up.
756. Confirm support articles match current routes.
757. Confirm support articles match current permissions.
758. Confirm support articles match current error codes.
759. Confirm support documentation is current.

## 113. Governance and Maintenance Controls

760. Confirm architecture documentation has an owner.
761. Confirm module documentation has an owner.
762. Confirm permission documentation has an owner.
763. Confirm migration documentation has an owner.
764. Confirm release notes are published.
765. Confirm operational runbooks are published.
766. Confirm stale documentation is marked.
767. Confirm source paths are checked after moves.
768. Confirm current runtime versions are recorded.
769. Confirm package changes are reviewed.
770. Confirm security-sensitive packages are reviewed.
771. Confirm dependency updates have test evidence.
772. Confirm configuration templates are maintained.
773. Confirm environment differences are documented.
774. Confirm feature owners are recorded.
775. Confirm deprecation decisions are recorded.
776. Confirm legacy routes are identified.
777. Confirm legacy fields are identified.
778. Confirm legacy permissions are identified.
779. Confirm legacy workers are identified.
780. Confirm unused configuration is reviewed.
781. Confirm unused endpoints are reviewed.
782. Confirm unused frontend modules are reviewed.
783. Confirm data retention is reviewed.
784. Confirm access reviews are scheduled.
785. Confirm administrator access is reviewed.
786. Confirm provider access is reviewed.
787. Confirm database access is reviewed.
788. Confirm production access is logged.
789. Confirm emergency access expires.
790. Confirm ownership changes are communicated.
791. Confirm knowledge transfer is planned.
792. Confirm single-person dependencies are reduced.
793. Confirm roadmap dependencies are visible.
794. Confirm technical debt has an owner.
795. Confirm technical debt has a reason.
796. Confirm technical debt has a review date.
797. Confirm documentation changes are reviewed.
798. Confirm this guide is updated after major releases.
799. Confirm the maintenance record is completed.

## 114. Advanced Decision Cards

800. Decision: add a field only; verify default behavior first.
801. Decision: add a collection; define tenant and lifecycle fields first.
802. Decision: add an endpoint; define owner and permission first.
803. Decision: add a public endpoint; define exposure first.
804. Decision: add a worker; define retry and idempotency first.
805. Decision: add an email; define durable audit first.
806. Decision: add a SignalR event; define offline behavior first.
807. Decision: add a report; define source-of-truth fields first.
808. Decision: add a payroll input; define lock behavior first.
809. Decision: add a leave rule; define calendar interaction first.
810. Decision: add an attendance status; define time semantics first.
811. Decision: add a WFH rule; define quota and ownership first.
812. Decision: add a permission; define least privilege first.
813. Decision: add a role grant; define tenant impact first.
814. Decision: add an upload; define access and retention first.
815. Decision: add rich text; define sanitization first.
816. Decision: add a migration; define partial-state behavior first.
817. Decision: add an index; measure query shape first.
818. Decision: add a cache; define invalidation first.
819. Decision: add a dashboard metric; define business meaning first.
820. Decision: add a locale key; update supported locales first.
821. Decision: add a route; define public versus private first.
822. Decision: add a form; define validation ownership first.
823. Decision: add a retry; define duplicate effects first.
824. Decision: add a background task; capture context first.
825. Decision: add an audit event; define actor and timestamp first.
826. Decision: change a status; define all transitions first.
827. Decision: change a policy; define historical interpretation first.
828. Decision: change a lookup; find all consumers first.
829. Decision: change a salary field; define effective period first.
830. Decision: change a calendar; assess payroll impact first.
831. Decision: change a public field; assess privacy first.
832. Decision: change a token setting; assess active sessions first.
833. Decision: change a provider; test failure behavior first.
834. Decision: change a route; coordinate frontend and backend first.
835. Decision: change a response; assess client compatibility first.
836. Decision: change a repository filter; run cross-tenant tests first.
837. Decision: change a source rule; review reconciliation first.
838. Decision: change a lock rule; review payroll impact first.
839. Decision: change a worker schedule; review duplicate work first.
840. Decision: change a document; update manager guide first.

## 115. Acceptance Evidence Cards

841. Evidence: successful login response.
842. Evidence: rejected invalid login response.
843. Evidence: successful token refresh.
844. Evidence: rejected expired refresh token.
845. Evidence: protected route without token.
846. Evidence: protected route without permission.
847. Evidence: same-company owner access.
848. Evidence: same-company wrong-owner rejection.
849. Evidence: different-company rejection.
850. Evidence: employee invitation.
851. Evidence: employee self-profile update.
852. Evidence: protected employment-field rejection.
853. Evidence: attendance company grid.
854. Evidence: attendance own grid.
855. Evidence: locked attendance rejection.
856. Evidence: leave-owned attendance protection.
857. Evidence: WFH-owned attendance protection.
858. Evidence: leave request submission.
859. Evidence: leave approval.
860. Evidence: leave rejection.
861. Evidence: leave attendance reconciliation.
862. Evidence: WFH context response.
863. Evidence: WFH quota rejection.
864. Evidence: WFH pending state.
865. Evidence: WFH direct approval.
866. Evidence: WFH version conflict.
867. Evidence: WFH check-in.
868. Evidence: WFH check-out.
869. Evidence: WFH insufficient-hours exception.
870. Evidence: payroll draft.
871. Evidence: payroll process action.
872. Evidence: employee payroll visibility.
873. Evidence: payslip download.
874. Evidence: public published job.
875. Evidence: public unpublished job rejection.
876. Evidence: public application.
877. Evidence: invalid resume rejection.
878. Evidence: applicant status change.
879. Evidence: applicant process log.
880. Evidence: company notice.
881. Evidence: durable notification.
882. Evidence: SignalR live notification.
883. Evidence: email log entry.
884. Evidence: calendar holiday.
885. Evidence: dashboard aggregate.
886. Evidence: migration execution record.
887. Evidence: migration rerun behavior.
888. Evidence: API build output.
889. Evidence: service test output.
890. Evidence: frontend typecheck output.
891. Evidence: frontend lint output.
892. Evidence: frontend test output.
893. Evidence: frontend build output.
894. Evidence: release smoke test.
895. Evidence: backup reference.
896. Evidence: post-release monitoring.

## 116. Manager Sign-Off Questions

897. Is the business owner satisfied with the journey?
898. Is the technical owner satisfied with the design?
899. Is QA satisfied with the evidence?
900. Is security satisfied with the boundaries?
901. Is operations satisfied with deployment?
902. Is support satisfied with the runbook?
903. Is payroll satisfied with upstream controls?
904. Is HR satisfied with policy behavior?
905. Is recruitment satisfied with public behavior?
906. Is the employee experience understandable?
907. Are failure messages actionable?
908. Are public and private data separated?
909. Are role grants least-privileged?
910. Are tenant filters tested?
911. Are workflow conflicts handled?
912. Are migrations reversible or forward-fixable?
913. Are backups verified?
914. Are providers monitored?
915. Are workers monitored?
916. Are notifications durable?
917. Are email results auditable?
918. Are files protected?
919. Is rich text sanitized?
920. Are locale files complete?
921. Are accessibility concerns addressed?
922. Is mobile behavior acceptable?
923. Is performance acceptable?
924. Is the release communication ready?
925. Is the support communication ready?
926. Is the release owner clear?
927. Is the post-release review scheduled?

## 117. Closing Operating Principle

928. A manager-approved feature must be understandable to users.
929. A user-visible feature must be enforced by the backend.
930. A backend-enforced feature must be tenant-scoped.
931. A tenant-scoped feature must have negative tests.
932. A persisted feature must have lifecycle ownership.
933. A workflow feature must have explicit transitions.
934. A derived record must retain provenance.
935. A background feature must tolerate retries.
936. A migration must tolerate partial state.
937. A provider feature must expose failure evidence.
938. A release must have a recovery decision.
939. A supportable feature must have a runbook.
940. A maintained feature must have current documentation.
941. A measurable feature must have an owner.
942. A secure feature must minimize data exposure.
943. A reliable feature must be tested across boundaries.
944. A complete feature must be reviewed from business to database.

---

# Part XII: Extended Review Questions

The following questions provide an additional meeting-ready review surface. They
are intentionally specific so a manager can assign them to product, engineering,
QA, security, operations, HR, payroll, or support and receive evidence in return.

## 118. Business Readiness Questions

945. Which user starts this workflow?
946. Which user finishes this workflow?
947. What business state exists before the workflow?
948. What business state exists after the workflow?
949. What is the normal successful path?
950. What is the expected waiting path?
951. What is the expected rejection path?
952. What is the expected cancellation path?
953. What is the expected correction path?
954. What is the expected retry path?
955. What evidence proves completion?
956. What evidence proves rejection?
957. What evidence proves cancellation?
958. What evidence proves correction?
959. What evidence proves notification?
960. What user decision is irreversible?
961. What user decision is reversible?
962. What user decision needs confirmation?
963. What user decision needs a reviewer?
964. What user decision needs a second person?
965. What user decision affects payroll?
966. What user decision affects compliance?
967. What user decision affects a candidate?
968. What user decision affects another employee?
969. What user decision affects a public page?
970. Which business term must remain consistent?
971. Which business term is ambiguous?
972. Which business term needs a glossary entry?
973. Which workflow state is externally visible?
974. Which workflow state is internal only?
975. Which workflow state is reportable?
976. Which workflow state is payroll-relevant?
977. Which workflow state is legally relevant?
978. Which workflow state requires history?
979. Which workflow state requires a notification?
980. Which workflow state requires an email?
981. Which workflow state requires a lock?
982. Which workflow state requires a version?
983. Which workflow state can expire?
984. Which workflow state can be reopened?
985. What happens if the user closes the browser?
986. What happens if the network fails?
987. What happens if the API restarts?
988. What happens if the database is slow?
989. What happens if the provider is unavailable?
990. What happens if the worker retries?
991. What happens if two reviewers act together?
992. What happens if policy changes mid-workflow?
993. What happens if the employee becomes inactive?
994. What happens if the company is suspended?

## 119. Product Ownership Questions

995. Who approves the business vocabulary?
996. Who approves the user journey?
997. Who approves the permission model?
998. Who approves the data model?
999. Who approves the public exposure?
1000. Who approves the notification content?
1001. Who approves the email content?
1002. Who approves the payroll impact?
1003. Who approves the migration?
1004. Who approves the release?
1005. Who owns the acceptance criteria?
1006. Who owns the rejection criteria?
1007. Who owns the support article?
1008. Who owns the dashboard metric?
1009. Who owns the policy configuration?
1010. Who owns the role configuration?
1011. Who owns the public career content?
1012. Who owns the employee data definition?
1013. Who owns the retention decision?
1014. Who owns the incident response?
1015. Who owns the backup decision?
1016. Who owns the restore drill?
1017. Who owns provider escalation?
1018. Who owns the worker schedule?
1019. Who owns migration execution?
1020. Who owns post-release monitoring?
1021. Who owns the product metric?
1022. Who owns customer communication?
1023. Who owns the known limitation?
1024. Who owns the technical debt?
1025. Who owns documentation freshness?
1026. Who owns access review?
1027. Who owns the security exception?
1028. Who owns the performance budget?
1029. Who owns browser compatibility?
1030. Who owns localization quality?
1031. Who owns accessibility quality?
1032. Who owns test data?
1033. Who owns test environment readiness?
1034. Who owns staging verification?
1035. Who owns production verification?
1036. Who owns rollback communication?
1037. Who owns reconciliation after an incident?
1038. Who owns the lessons-learned meeting?
1039. Who owns the follow-up actions?
1040. Who reports completion to management?
1041. Who confirms the release is safe?
1042. Who confirms the release is useful?
1043. Who confirms the release is supportable?
1044. Who confirms the release is measurable?

## 120. Employee Experience Questions

1045. Can an employee find the correct page?
1046. Can an employee understand the page title?
1047. Can an employee understand the current status?
1048. Can an employee understand the next action?
1049. Can an employee understand why an action is disabled?
1050. Can an employee understand a validation error?
1051. Can an employee recover from a network error?
1052. Can an employee recover from an expired session?
1053. Can an employee identify their own records?
1054. Can an employee avoid seeing another employee?
1055. Can an employee cancel an eligible request?
1056. Can an employee correct an incorrect request?
1057. Can an employee see reviewer feedback?
1058. Can an employee see notification history?
1059. Can an employee see email-related status safely?
1060. Can an employee see their own payroll?
1061. Can an employee distinguish draft payroll?
1062. Can an employee understand a locked period?
1063. Can an employee understand a calendar exclusion?
1064. Can an employee understand a quota failure?
1065. Can an employee understand a policy restriction?
1066. Can an employee understand a time requirement?
1067. Can an employee see current working hours?
1068. Can an employee see required working hours?
1069. Can an employee understand insufficient hours?
1070. Can an employee use the workflow on mobile?
1071. Can an employee use the workflow with a keyboard?
1072. Can an employee use the workflow with zoom?
1073. Can an employee read the status without color?
1074. Can an employee select a localized date?
1075. Can an employee understand the timezone?
1076. Can an employee upload a supported file?
1077. Can an employee recover from a rejected file?
1078. Can an employee submit only once?
1079. Can an employee identify duplicate submission?
1080. Can an employee return later and continue?
1081. Can an employee find help?
1082. Can an employee contact support safely?
1083. Can an employee understand privacy expectations?
1084. Can an employee see only permitted profile fields?
1085. Can an employee update allowed profile fields?
1086. Can an employee understand why protected fields cannot change?
1087. Can an employee see an empty state?
1088. Can an employee see a loading state?
1089. Can an employee see a retry action?
1090. Can an employee see a successful completion?
1091. Can an employee trust that the record was saved?
1092. Can an employee distinguish a saved record from a queued email?
1093. Can an employee distinguish a live notification from history?
1094. Can an employee understand the next business step?

## 121. HR Operations Questions

1095. Can HR configure the intended policy?
1096. Can HR identify policy ownership?
1097. Can HR identify affected employees?
1098. Can HR identify exceptions?
1099. Can HR identify pending approvals?
1100. Can HR identify overdue approvals?
1101. Can HR identify locked dates?
1102. Can HR identify source-owned attendance?
1103. Can HR identify leave reconciliation?
1104. Can HR identify WFH reconciliation?
1105. Can HR identify balance changes?
1106. Can HR identify accrual completion?
1107. Can HR identify inactive employees?
1108. Can HR identify incorrect role grants?
1109. Can HR identify missing department setup?
1110. Can HR identify missing job-title setup?
1111. Can HR identify missing attendance statuses?
1112. Can HR identify missing calendar entries?
1113. Can HR identify missing salary structure?
1114. Can HR identify unresolved payroll exceptions?
1115. Can HR identify payroll drafts?
1116. Can HR identify processed payroll?
1117. Can HR identify payslip generation failures?
1118. Can HR identify notification history?
1119. Can HR identify email log entries?
1120. Can HR identify applicant process history?
1121. Can HR identify public vacancy state?
1122. Can HR identify company profile publication?
1123. Can HR identify employee data changes?
1124. Can HR identify reviewer decisions?
1125. Can HR identify stale version conflicts?
1126. Can HR identify duplicate requests?
1127. Can HR identify overlapping requests?
1128. Can HR identify quota consumption?
1129. Can HR identify calendar exclusions?
1130. Can HR identify manual attendance changes?
1131. Can HR identify bulk-operation results?
1132. Can HR identify import rejection reasons?
1133. Can HR identify policy effective dates?
1134. Can HR identify employment-type eligibility?
1135. Can HR identify explicit employee exclusions?
1136. Can HR identify company timezone?
1137. Can HR identify configured work hours?
1138. Can HR identify minimum required hours?
1139. Can HR identify outstanding corrections?
1140. Can HR identify support evidence?
1141. Can HR perform a safe correction?
1142. Can HR reverse an eligible workflow?
1143. Can HR explain a payroll number?
1144. Can HR explain a status history?

## 122. Payroll Operations Questions

1145. Is the payroll period defined?
1146. Is the payroll period timezone understood?
1147. Is attendance complete?
1148. Is leave reconciliation complete?
1149. Is WFH reconciliation complete?
1150. Are exceptions reviewed?
1151. Are exceptions resolved?
1152. Are exceptions intentionally accepted?
1153. Is the period locked?
1154. Is salary effective data available?
1155. Is divisor policy available?
1156. Are inactive employees handled?
1157. Are new employees handled?
1158. Are departed employees handled?
1159. Are partial periods handled?
1160. Are half-day statuses handled?
1161. Are unpaid statuses handled?
1162. Are overtime assumptions documented?
1163. Are late penalties documented?
1164. Are early-exit penalties documented?
1165. Are calendar exclusions documented?
1166. Are locked-period overrides controlled?
1167. Is payroll generation authorized?
1168. Is payroll draft review assigned?
1169. Is payroll processing authorized?
1170. Is employee visibility checked?
1171. Are duplicate generation attempts controlled?
1172. Are draft changes logged?
1173. Are process changes logged?
1174. Are payslip files generated?
1175. Are payslip files accessible only to owners?
1176. Are payslip errors visible?
1177. Are PDF dependencies available?
1178. Is storage capacity sufficient?
1179. Is generation duration acceptable?
1180. Is provider communication separate from payroll completion?
1181. Are payroll support records retained?
1182. Are payroll corrections approved?
1183. Are payroll reruns documented?
1184. Are payroll exports reconciled?
1185. Are payroll metrics defined?
1186. Is payroll data protected in logs?
1187. Is payroll data protected in support tickets?
1188. Is payroll data protected in downloads?
1189. Are payroll permissions reviewed?
1190. Are payroll release gates passed?
1191. Is the payroll owner available?
1192. Is the backup payroll owner available?
1193. Is the payroll communication ready?
1194. Is the final payroll sign-off recorded?

## 123. Technical Review Questions

1195. Is the owning layer clear?
1196. Is the public contract clear?
1197. Is the persistence contract clear?
1198. Is the tenant boundary explicit?
1199. Is the ownership boundary explicit?
1200. Is the workflow state machine explicit?
1201. Is the concurrency strategy explicit?
1202. Is the retry strategy explicit?
1203. Is the idempotency strategy explicit?
1204. Is the migration strategy explicit?
1205. Is the index strategy explicit?
1206. Is the error-code strategy explicit?
1207. Is the logging strategy explicit?
1208. Is the metric strategy explicit?
1209. Is the alert strategy explicit?
1210. Is the test strategy explicit?
1211. Is the release strategy explicit?
1212. Is the recovery strategy explicit?
1213. Is the privacy strategy explicit?
1214. Is the retention strategy explicit?
1215. Is the file strategy explicit?
1216. Is the email strategy explicit?
1217. Is the realtime strategy explicit?
1218. Is the worker strategy explicit?
1219. Is the frontend state strategy explicit?
1220. Is the cache strategy explicit?
1221. Is the invalidation strategy explicit?
1222. Is the localization strategy explicit?
1223. Is the accessibility strategy explicit?
1224. Is the performance strategy explicit?
1225. Is the compatibility strategy explicit?
1226. Is the dependency strategy explicit?
1227. Is the configuration strategy explicit?
1228. Is the secret strategy explicit?
1229. Is the deployment order explicit?
1230. Is the smoke-test order explicit?
1231. Is the rollback threshold explicit?
1232. Is the forward-fix threshold explicit?
1233. Is the support escalation explicit?
1234. Is the incident severity explicit?
1235. Is the evidence owner explicit?
1236. Is the decision owner explicit?
1237. Is the documentation owner explicit?
1238. Is the code review owner explicit?
1239. Is the architecture review owner explicit?
1240. Is the database review owner explicit?
1241. Is the security review owner explicit?
1242. Is the QA review owner explicit?
1243. Is the operations review owner explicit?
1244. Is the business sign-off owner explicit?

## 124. Security Assurance Questions

1245. Can an anonymous caller access private data?
1246. Can a signed-in caller change the company ID?
1247. Can a signed-in caller change the owner ID?
1248. Can a signed-in caller impersonate an approver?
1249. Can a signed-in caller read another company?
1250. Can a signed-in caller update another company?
1251. Can a signed-in caller delete another company?
1252. Can a signed-in caller read hidden public content?
1253. Can a signed-in caller submit duplicate work?
1254. Can a signed-in caller bypass a lock?
1255. Can a signed-in caller overwrite source ownership?
1256. Can a signed-in caller approve their own request?
1257. Can a signed-in caller replay a stale decision?
1258. Can a signed-in caller expose free text broadly?
1259. Can an upload escape its intended directory?
1260. Can an upload execute as a script?
1261. Can rich text execute unsafe markup?
1262. Can a public URL target a private service?
1263. Can a public URL target localhost?
1264. Can an email recipient list include invalid addresses?
1265. Can an email recipient list leak BCC data?
1266. Can a log contain a password?
1267. Can a log contain a token?
1268. Can a log contain a provider key?
1269. Can a support ticket contain unnecessary personal data?
1270. Can a refresh token live after revocation?
1271. Can a hub accept a token on an unrelated path?
1272. Can a role name bypass a permission grant?
1273. Can a hidden button be mistaken for authorization?
1274. Can a background job lose its tenant context?
1275. Can a notification cross a company boundary?
1276. Can a dashboard aggregate cross a company boundary?
1277. Can a public career code reveal private identifiers?
1278. Can a candidate see reviewer notes?
1279. Can an employee see HR-only fields?
1280. Can HR see another company?
1281. Can an inactive user continue a workflow?
1282. Can a deleted record remain publicly visible?
1283. Can an exception be silently dismissed?
1284. Can a migration create duplicate grants?
1285. Can a migration run concurrently?
1286. Can a retry duplicate a notification?
1287. Can a retry duplicate payroll?
1288. Can a retry duplicate accrual?
1289. Can a provider response expose personal data?
1290. Can a browser cache private data incorrectly?
1291. Can an error message disclose internal details?
1292. Can a file download bypass ownership?
1293. Can an audit event be edited silently?
1294. Can a security exception expire?

## 125. Final Meeting Record

1295. Record the meeting date.
1296. Record the release identifier.
1297. Record the environment reviewed.
1298. Record the database reviewed.
1299. Record the frontend artifact reviewed.
1300. Record the backend artifact reviewed.
1301. Record the migration reviewed.
1302. Record the business owner.
1303. Record the technical owner.
1304. Record the QA owner.
1305. Record the security owner.
1306. Record the operations owner.
1307. Record the support owner.
1308. Record the approval decision.
1309. Record the rejected items.
1310. Record the accepted risks.
1311. Record the unresolved assumptions.
1312. Record the required follow-up.
1313. Record the follow-up owner.
1314. Record the follow-up due date.
1315. Record the evidence location.
1316. Record the backup reference.
1317. Record the migration result.
1318. Record the smoke-test result.
1319. Record the monitoring result.
1320. Record the support-readiness result.
1321. Record the security-readiness result.
1322. Record the data-readiness result.
1323. Record the accessibility result.
1324. Record the localization result.
1325. Record the performance result.
1326. Record the recovery result.
1327. Record the notification result.
1328. Record the email result.
1329. Record the realtime result.
1330. Record the worker result.
1331. Record the public-route result.
1332. Record the protected-route result.
1333. Record the cross-tenant test result.
1334. Record the wrong-owner test result.
1335. Record the stale-version test result.
1336. Record the locked-period test result.
1337. Record the duplicate-request test result.
1338. Record the provider-failure test result.
1339. Record the partial-migration test result.
1340. Record the rollback decision.
1341. Record the forward-fix decision.
1342. Record the communication decision.
1343. Record the customer-impact decision.
1344. Record the next review date.

## 126. Post-Release Review Prompts

1345. Was the deployment completed in the planned window?
1346. Was the deployed version recorded?
1347. Was the migration result recorded?
1348. Was the API restarted successfully?
1349. Was the frontend artifact served successfully?
1350. Did authentication work after deployment?
1351. Did token refresh work after deployment?
1352. Did a protected read work after deployment?
1353. Did a protected write work after deployment?
1354. Did an anonymous read work after deployment?
1355. Did an anonymous submission work after deployment?
1356. Did the main dashboard load after deployment?
1357. Did the employee list load after deployment?
1358. Did attendance load after deployment?
1359. Did leave load after deployment?
1360. Did WFH context load after deployment?
1361. Did payroll load after deployment?
1362. Did recruitment load after deployment?
1363. Did notifications load after deployment?
1364. Did SignalR negotiate after deployment?
1365. Did an email log appear after deployment?
1366. Did a background worker complete after deployment?
1367. Did a migration rerun skip safely?
1368. Did the database remain healthy?
1369. Did indexes remain available?
1370. Did error rates remain within threshold?
1371. Did latency remain within threshold?
1372. Did queue depth remain within threshold?
1373. Did provider errors remain within threshold?
1374. Did frontend runtime errors remain within threshold?
1375. Did browser console errors remain within threshold?
1376. Did public pages remain published correctly?
1377. Did private pages remain protected correctly?
1378. Did role permissions remain correct?
1379. Did cross-tenant tests remain negative?
1380. Did wrong-owner tests remain negative?
1381. Did stale-version tests remain negative?
1382. Did locked-period tests remain negative?
1383. Did duplicate-request tests remain negative?
1384. Did source-owned attendance remain protected?
1385. Did payroll visibility remain correct?
1386. Did employee self-service remain scoped?
1387. Did manager review remain scoped?
1388. Did public applicant data remain minimized?
1389. Did file access remain protected?
1390. Did rich text remain sanitized?
1391. Did localization remain complete?
1392. Did dates render in the expected timezone?
1393. Did accessibility checks remain acceptable?
1394. Did mobile layout remain usable?
1395. Did support receive the release notes?
1396. Did support receive the runbook?
1397. Did support receive known limitations?
1398. Did operations receive the monitoring links?
1399. Did operations receive the backup reference?
1400. Did security receive the change summary?
1401. Did QA archive the test evidence?
1402. Did the business owner confirm the outcome?
1403. Did users understand the change?
1404. Did users encounter unexpected errors?
1405. Did users encounter unexpected permission changes?
1406. Did users encounter unexpected data changes?
1407. Did users encounter unexpected notification volume?
1408. Did users encounter unexpected email volume?
1409. Did users encounter unexpected performance changes?
1410. Did users encounter unexpected browser behavior?
1411. Did any tenant require manual correction?
1412. Did any payroll period require review?
1413. Did any public job require republishing?
1414. Did any applicant require support?
1415. Did any worker require restart?
1416. Did any provider require escalation?
1417. Did any alert fire unexpectedly?
1418. Did any alert fail to fire when expected?
1419. Did any log expose sensitive information?
1420. Did any audit trail become incomplete?
1421. Did any migration warning require follow-up?
1422. Did any data reconciliation remain open?
1423. Did any known risk become an incident?
1424. Did any incident require customer communication?
1425. Did any emergency access occur?
1426. Was emergency access closed?
1427. Was the release accepted as successful?
1428. Was the release accepted with conditions?
1429. Was the release rolled back?
1430. Was a forward fix required?
1431. Was a follow-up ticket created?
1432. Was a technical-debt item created?
1433. Was documentation corrected?
1434. Was the maintenance record updated?
1435. Was the next review scheduled?
1436. Was the final manager summary circulated?
1437. Was the release learning shared with the team?
1438. Was the control register updated for new risks?
1439. Was the product roadmap updated?
1440. Was the support roadmap updated?
1441. Was the security roadmap updated?
1442. Was the reliability roadmap updated?
1443. Was the testing roadmap updated?
1444. Was the ownership register updated?

## 127. Current Attendance, Leave, Shift, WFH, and Frontend Experience Model

This section connects the operational modules that are most often changed
together. It is a manager-facing summary; the linked module guides remain the
implementation references.

### 127.1 Attendance and shift ownership

Attendance is evaluated against an effective shift for each employee and
business date. The order is individual employee override, department assignment,
then the active company default shift. This supports a standard department
schedule while allowing an HR-authorized individual exception without copying
department data into every employee record.

Each shift is tenant-owned and contains its punch windows, standard times,
break/grace/required-work rules, effective dates, activity/default state, and
version. HR can add, expand, save, make default, and—only when safe—delete
non-default shifts. A deletion is blocked if a current employee or department
assignment depends on it. The server validates every schedule, employee, and
department ID within the authenticated company.

Attendance records retain the applied schedule ID/version so a later change to
a shift does not silently rewrite historical attendance. The main report may be
filtered by shifts available to the current company. User-facing screens say
**Punch in** and **Punch out**; existing service fields remain compatible with
`CheckInTime` and `CheckOutTime`.

### 127.2 Leave and WFH lifecycle

Leave policies, balances, requests, and decisions are company-scoped. Inactive
policies remain auditable but are excluded from new allocations and applications.
Approval creates a source-owned attendance row through reconciliation; it is not
only a visual leave-status change. Manual attendance cannot overwrite rows owned
by Leave or WFH.

Approved WFH is a policy-controlled workflow. For an approved request covering
today, the employee receives exactly one progressive action: Clock in, then
Clock out, then a completed state. The API derives employee and company from the
session and updates only the matching WFH-owned record. This prevents an
employee from clocking a different person's request or turning WFH into a
general manual attendance path.

### 127.3 Frontend design and layout standards

The React application uses a common shell, theme tokens, and shared UI
primitives. Every module should follow the same visual rhythm:

```text
Page context -> local filters/actions -> summary -> primary data/form -> history or exceptions
```

Forms use aligned outlined fields, visible labels, consistent helper text, and
one clear save action per form scope. Switches and checkboxes are compact action
groups with adequate spacing. On wide screens controls share rows; on smaller
screens they stack before collision. Dense tables and calendars use intentional
inner horizontal scrolling rather than unreadable compressed columns. Expandable
shift content appears directly below the selected shift row.

The browser improves discoverability through disabled states, permission-gated
actions, success/error feedback, and responsive layout. It never supplies
tenant authority or replaces authorization, workflow validation, concurrency
controls, or source ownership in the API.

### 127.4 Delivery and acceptance checklist

1. Verify a second company cannot read, assign, update, delete, or filter by
   another company's shift, employee, policy, balance, request, WFH record, or
   attendance row.
2. Verify an employee override, department assignment, and company default in
   that exact precedence order.
3. Verify duplicate schedule prevention and exactly one default schedule per
   company.
4. Verify an inactive leave policy is visible only where history requires it.
5. Verify leave/WFH reconciliation, source protection, payroll exceptions, and
   month-lock behavior.
6. Verify the affected pages at mobile, tablet, and desktop widths with loading,
   empty, error, keyboard, and permission-denied states.
7. Record API authorization, cross-tenant negative tests, database/index
   behavior, and browser verification separately; a successful build alone is
   not production acceptance.

### 127.5 Primary reading path

- [Project layout](project-layout.md)
- [Project design and UI guide](project-design-and-ui.md)
- [Office Schedule and Shift Management](office-schedule-shifts.md)
- [Attendance Module](attendance-module-current-flow-and-audit.md)
- [Leave Management](leave-management-current-flow.md)
- [WFH End-to-End Flow](wfh-end-to-end-flow.md)
