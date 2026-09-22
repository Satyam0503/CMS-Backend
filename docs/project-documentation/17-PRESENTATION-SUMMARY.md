# Codeji CMS / HRMS — Presentation Summary & Slide Deck

> **Document Status**: Complete & Authoritative  
> **Target Audience**: Executive Leadership, Client Stakeholders, Technical Buyers, Investors  
> **Slide Count**: 25 Comprehensive Presentation Slides  
> **Format**: Slide Deck with Presenter Notes, Visual Diagrams & Verified Code Evidence  
> **Last Verified**: September 2026

---

## Slide 1: Codeji HRMS Platform Overview

- **Title**: Unified Workforce Operating System
- **Key Points**:
  - All-in-one Human Resource Information System (HRIS) and Applicant Tracking System (ATS).
  - Bridges the entire employee journey: recruitment, daily attendance, leave, automated payroll, separation, and alumni relations.
  - Built on modern, high-performance technology: ASP.NET Core on .NET 10, React 18, and MongoDB.
- **Diagram Suggestion**: Flowchart showing the continuous lifecycle circle connecting Talent Acquisition -> Active Workforce -> Separation -> Alumni.
- **Presenter Notes**: "Welcome everyone. Today we present Codeji HRMS, an enterprise-grade platform designed from the ground up to unify fragmented HR operations into a single, mathematically rigorous operating system."
- **System Evidence**: `Codeji.CMS.API/Program.cs`, `CMS-React/src/App.tsx`.

---

## Slide 2: The Enterprise Problem Being Solved

- **Title**: Eliminating Friction in Workforce Operations
- **Key Points**:
  - Siloed HR systems cause data drift between approved leaves and monthly salary deductions.
  - Security risks emerge when former employees retain active credentials or permissions leak across company borders.
  - Operational overhead from manual calculations of Loss-of-Pay (LOP), late penalties, and notice buyouts.
  - Disputed full-and-final settlements due to undocumented clearance sign-offs.
- **Diagram Suggestion**: Contrast diagram showing "Fragmented HR Apps" (disconnected silos with red warning icons) vs. "Unified Codeji Architecture" (centralized source-of-truth).
- **Presenter Notes**: "Every mid-market company struggles with data drift. A manager approves a leave via email, but payroll runs on a spreadsheet that never captured it, leading to salary errors and employee dissatisfaction."
- **System Evidence**: `AutoPayrollServices.cs:84` (mandatory check for locked attendance summaries).

---

## Slide 3: End-to-End System Architecture

- **Title**: Decoupled, High-Performance Architecture
- **Key Points**:
  - **Frontend**: Single Page Application built with React 18, TypeScript, Vite, Material-UI, Redux, and TanStack Query.
  - **Backend**: Clean Architecture ASP.NET Core Web API targeting .NET 10.
  - **Database**: Document-oriented MongoDB with compound tenant indexing and custom timestamped migrations.
  - **Realtime & Messaging**: SignalR WebSockets paired with transactional outbox workers for guaranteed email delivery.
- **Diagram Suggestion**: 4-tier layer diagram: Browser Clients -> Ingress & API Gateway -> Business Services & Policy Engine -> MongoDB Cluster & Storage.
- **Presenter Notes**: "Our architecture separates client presentation from server governance. All business invariants are enforced on the backend, ensuring that no client-side modification can compromise system integrity."
- **System Evidence**: `Codeji.CMS.API/Program.cs:59-450`, `CMS-React/src/app/routing/AppRoutes.tsx`.

---

## Slide 4: The Complete Employee Lifecycle

- **Title**: Seamless Progression Across 7 Employment Dimensions
- **Key Points**:
  - Tracks 7 orthogonal lifecycle states: Employment, Availability, Account, Payroll, Offboarding, Former Portal, and Alumni.
  - Automated state transitions: Onboarding completion instantly activates employment, availability, and payroll eligibility.
  - Separation completion revokes credentials, cuts off attendance, and triggers final settlement.
- **Diagram Suggestion**: Multi-state workflow diagram illustrating status transitions from `PreJoining` to `Active`, `NoticePeriod`, `Resigned`, and `Alumni`.
- **Presenter Notes**: "Employee status in Codeji is multi-dimensional. An employee can be 'Active' in employment while simultaneously 'On Leave' in availability, 'Active' in payroll, and 'None' in offboarding."
- **System Evidence**: `EmployeeLifecycle.cs:5-108`, `RecruitmentOnboardingService.cs:140-181`.

---

## Slide 5: Major Functional Modules

- **Title**: Comprehensive Suite of Integrated HR Modules
- **Key Points**:
  - **Talent Acquisition**: Public career marketplace, job vacancies, ATS candidate evaluation, interview scorecards.
  - **Workforce Management**: Multi-shift scheduling, punch classifications (P/HD/LHD/ED), leave accruals, WFH tracking.
  - **Compensation**: Standardized salary structures, CTC breakdowns, AutoPayroll generation, PDF payslips.
  - **Separation**: Multi-department clearances, notice countdown reminders, full-and-final settlement, alumni portal.
  - **Governance**: Enterprise policy engine, company master administration, dynamic RBAC.
- **Diagram Suggestion**: Modular grid display highlighting the 15 core functional modules with feature icons.
- **Presenter Notes**: "Codeji delivers an exhaustive HR functional suite. Every module is purpose-built to eliminate manual spreadsheets and human error."
- **System Evidence**: `ConstraintHelper.cs:4-21` (`AppModule` constants).

---

## Slide 6: Recruitment & Talent Acquisition (ATS)

- **Title**: Modern, Transparent Hiring Pipeline
- **Key Points**:
  - Branded Public Career Marketplace (`/careers/:companyCode`) with vacancy search and direct resume upload.
  - Structured applicant tracking across 7 recruitment stages (`New`, `InProgress`, `OnHold`, `Shortlisted`, `Selected`, `Rejected`, `ReApply`).
  - Interviewer scorecards, structured debriefs, and multi-tier offer approval policies.
  - Public candidate offer review portal (`/candidate/offer`) supporting digital acceptance and counter-negotiations.
- **Diagram Suggestion**: Linear recruitment funnel: Job Post -> Application -> Screening -> Interview -> Scorecard -> Offer -> Acceptance.
- **Presenter Notes**: "Our recruitment module empowers hiring teams to attract, evaluate, and issue offers seamlessly. The candidate experiences a polished, branded portal from initial application to offer sign-off."
- **System Evidence**: `ApplicantsController.cs`, `RecruitmentLifecycleDrawer.tsx`, `CandidateOfferResponse.tsx`.

---

## Slide 7: Preboarding & Automated Onboarding

- **Title**: Zero-Friction Transition from Candidate to Colleague
- **Key Points**:
  - Preboarding case tracking for pre-hire document submission (educational proofs, identity cards).
  - Customizable onboarding checklists (IT equipment allocation, security badge issuance, contract sign-offs).
  - **Automated Lifecycle Activation**: Closing all checklist items triggers automated employee activation without manual data re-entry.
- **Diagram Suggestion**: Handoff flowchart showing Accepted Offer -> Preboarding Documents -> Onboarding Tasks -> Employee Activation.
- **Presenter Notes**: "The dangerous gap between offer acceptance and the first day of work is bridged automatically. Once onboarding tasks are confirmed, the candidate is automatically promoted into an active employee."
- **System Evidence**: `RecruitmentOnboardingService.cs:74-125`, `RecruitmentPreboardingCase.cs`.

---

## Slide 8: Employee Directory & Profiles

- **Title**: Unified, Auditable Employee Records
- **Key Points**:
  - Comprehensive employee profiles covering personal info, work history, skills, certifications, and bank/PAN records.
  - Dynamic user custom attributes (`UserCustomAttribute`) allowing organizations to capture bespoke metadata.
  - Strict input validation: Regex validation for tax identifiers (PAN) and bank account numbers.
  - Bulk employee Excel import and export capabilities.
- **Diagram Suggestion**: Profile layout card showing tab navigation: About, Experience, Education, Certifications, Bank & Tax, Settings.
- **Presenter Notes**: "Employee profiles serve as the digital dossier for every team member, capturing verified personal data, banking details for payroll, and skills for internal talent mobility."
- **System Evidence**: `userModel.cs:6-56`, `UserProfile.tsx`, `AboutUser.tsx`.

---

## Slide 9: Attendance & Shift Management

- **Title**: Accurate Time Tracking with Automated Shift Governance
- **Key Points**:
  - Shift schedules define start/end times, tolerance windows, and grace periods.
  - Pure calculation engine classifies daily punches into `P` (Present), `HD` (Half Day), `LHD` (Late Arrival), `ED` (Early Departure), or `LHD+ED`.
  - Attendance Penalty Engine: Monitors monthly late limits and raises exception reviews.
  - Attendance Month Lock: Freezes monthly punch data prior to payroll execution.
- **Diagram Suggestion**: Punch evaluation timeline illustrating Shift Start, Grace Window, Late Half-Day Boundary, and Status Assignment.
- **Presenter Notes**: "Our attendance system eliminates subjective attendance disputes. Daily punch times are evaluated against the employee's assigned shift schedule with mathematical certainty."
- **System Evidence**: `AttendanceStatusCalculationService.cs:15-40`, `AttendancePenaltyService.cs`.

---

## Slide 10: Leave Management & Work From Home (WFH)

- **Title**: Flexible Time-Off Administration with Real-Time Balance Tracking
- **Key Points**:
  - Supports diverse leave types: Sick, Casual, Earned, Maternity, Paternity, and Sabbatical.
  - Automated monthly accrual processing via `LeaveAccrualHostedService`.
  - Integrated Work From Home (WFH) requests supporting full-day and half-day segments.
  - Asynchronous reconciliation worker retroactively marks approved leave dates on the attendance calendar.
- **Diagram Suggestion**: Balance ledger diagram showing Monthly Accrual -> Employee Leave Request -> Manager Approval -> Attendance Reconciled to 'L'.
- **Presenter Notes**: "Leave management operates with full transparency. Employees inspect real-time balances, managers approve requests in a single click, and background workers automatically reconcile attendance."
- **System Evidence**: `LeaveManagementService.cs`, `WorkFromHomeService.cs`, `LeaveAccrualHostedService.cs`.

---

## Slide 11: Compensation & AutoPayroll

- **Title**: Automated, Audit-Proof Payroll Generation
- **Key Points**:
  - Standardized salary structures defining CTC, basic pay, allowances (HRA, LTA, other), and statutory deductions (EPF, ESIC, Tax).
  - AutoPayroll engine strictly enforces pre-conditions: Monthly attendance must be approved and locked, with zero unreviewed exceptions.
  - Automatic Loss-of-Pay (LOP) deduction: Unpaid leave days plus approved policy penalty days.
  - Divisor policy options: Fixed 30 Days, Actual Working Days, or Calendar Days.
  - In-memory PDF payslip generation with numbers rendered into English words.
- **Diagram Suggestion**: Calculation formula flowchart: Gross Pay - Statutory Deductions - LOP Deductions = Net Pay -> PDF Payslip.
- **Presenter Notes**: "AutoPayroll bridges attendance and compensation. The system mathematically computes exact Loss-of-Pay deductions based on locked attendance and renders digital payslips in seconds."
- **System Evidence**: `AutoPayrollServices.cs:55-120`, `PayrollCalculationRules.cs:1-15`, `PdfService.cs`.

---

## Slide 12: Company Master & Administration

- **Title**: Centralized Tenant Configuration Hub
- **Key Points**:
  - Master data administration: Departments, Job Roles/Designations, Office Locations, Working Days.
  - Employee ID Sequence generator supporting customized enterprise prefix formats.
  - Company branding customization: Logo upload, official address, and SMTP email settings.
- **Diagram Suggestion**: Organization hierarchy chart showing Company Root branching into Departments, Designations, Locations, and Working Calendars.
- **Presenter Notes**: "Company Master serves as the administrative cockpit, allowing HR operations to configure organizational hierarchies and business operating parameters without engineering assistance."
- **System Evidence**: `CompanyMasterController.cs`, `CompanyService.cs`, `CompanyMaster.tsx`.

---

## Slide 13: Role & Permission Security (RBAC)

- **Title**: Dynamic, Multi-Tenant Role-Based Access Control
- **Key Points**:
  - Fine-grained permission model based on Cartesian intersections of Modules and Actions.
  - Standard system roles: Company Administrator, HR Manager, HR Executive, and Employee.
  - Declarative endpoint protection using `[ModulePermission(AppModule.X, Permission.Y)]`.
  - Zero-trust server evaluation: Client-side UI hiding is backed by authoritative middleware verification.
- **Diagram Suggestion**: Security gate diagram tracing a request through Token Parsing -> Claim Verification -> Role Lookup -> Access Granted/Denied.
- **Presenter Notes**: "Security is non-negotiable. Our dynamic RBAC system verifies every API call against company-scoped role permissions, ensuring complete protection across all endpoints."
- **System Evidence**: `ModulePermissionAttribute.cs`, `AuthenticateUserRequest.cs:83-111`, `RoleServices.cs:587-634`.

---

## Slide 14: Enterprise Policy Resolution Engine

- **Title**: Deterministic Multi-Tier Policy Engine
- **Key Points**:
  - Resolves business rules using strict precedence: Department -> Role -> Company Baseline.
  - Pure, deterministic resolution logic guarantees query order cannot alter outcomes.
  - **Immutable Policy Snapshotting**: When an employee initiates a separation, the active policy is frozen into the exit document, preventing retroactive rule alterations.
- **Diagram Suggestion**: Hierarchical funnel diagram showing Department match checked first, Role match second, Company default as fallback.
- **Presenter Notes**: "The policy engine provides organizational flexibility without complexity. Rules can be customized per department or role, while immutable snapshots ensure point-in-time legal compliance."
- **System Evidence**: `OffboardingPolicyResolver.cs:25-82`, `EmployeeExitPolicy.cs`.

---

## Slide 15: Notice Period Governance

- **Title**: Structured Resignation & Notice Management
- **Key Points**:
  - Calculates required notice duration from snapshotted exit policies.
  - Supports notice adjustment tracking: Early releases, buyouts, and managerial waivers.
  - Automated notice countdown reminders dispatched via transactional outbox workers.
  - Sets authoritative Last Working Date (LWD) governing system access cutoff.
- **Diagram Suggestion**: Timeline diagram showing Resignation Date, Notice Period Countdown, Early Buyout Adjustment, and Last Working Date.
- **Presenter Notes**: "Notice periods are managed with complete transparency. Countdown reminders keep managers and employees aligned, while buyout and waiver tracking feeds directly into final settlement."
- **System Evidence**: `EmployeeExitDtos.cs`, `EmployeeExitOutboxProcessor.cs`.

---

## Slide 16: Separation & Multi-Department Clearances

- **Title**: Flawless Employee Offboarding & Asset Return
- **Key Points**:
  - Multi-department clearance templates covering IT, Facilities, Finance, and HR.
  - Asset return tracking: Evaluates condition, serial numbers, and damage deductions.
  - IT access revocation: Coordinates single sign-on, email, and hardware access closure.
  - Exit interviews capturing categorized feedback on compensation, culture, and management.
- **Diagram Suggestion**: Multi-track clearance checklist showing IT Sign-off, Asset Sign-off, Finance Sign-off, and HR Final Sign-off.
- **Presenter Notes**: "Offboarding ensures organizational security and asset recovery. Every department must complete its clearance checklist before an exit can be finalized."
- **System Evidence**: `EmployeeExitCaseCard.tsx`, `EmployeeExitOperationsPanel.tsx`, `EmployeeExitClearanceTemplate.cs`.

---

## Slide 17: Full & Final Settlement (FnF)

- **Title**: Accurate, Audit-Ready Financial Closure
- **Key Points**:
  - Automatically reconciles unpaid salary days from processed payroll.
  - Computes additions (reimbursements, encashable leave balances, bonuses).
  - Computes deductions (notice period buyout recovery, unreturned asset damages, loan dues).
  - Generates clear financial direction: `PayableToEmployee`, `ReceivableFromEmployee`, or `ZeroSettlement`.
- **Diagram Suggestion**: Financial reconciliation balance sheet showing Gross Earnings + Leave Encashment - Notice Recovery - Dues = Net Payable.
- **Presenter Notes**: "Full and final settlements are a major source of labor disputes. Codeji eliminates disputes by auto-calculating settlements against real payroll and leave data."
- **System Evidence**: `EmployeeFinalSettlement.cs:5-36`, `UserController.cs:GetEmployeeFinalSettlement`.

---

## Slide 18: Former Employee & Alumni Portal

- **Title**: Post-Separation Engagement & Document Access
- **Key Points**:
  - Dedicated, unauthenticated-app portal (`/former-employee`) accessed via secure one-time tokens or OTP.
  - Separated employees download historical payslips, tax forms (Form 16), and experience certificates.
  - Rejoin eligibility tracking: Preserves historical records while facilitating rapid re-hiring.
- **Diagram Suggestion**: Portal experience mockup showing separated employee logging in via secure token to download their Experience Letter and Payslips.
- **Presenter Notes**: "Our relationship with employees doesn't end on their last day. The Former Employee Portal provides alumni with self-service access to their documents without burdening the HR team."
- **System Evidence**: `FormerEmployeePortalExperience.tsx`, `FormerEmployeePortalService.cs`, `FormerEmployeeAccessToken.cs`.

---

## 19. Cross-Module Automation

- **Title**: The Power of an Integrated System
- **Key Points**:
  - Zero manual re-keying: Candidate data flows directly into employee profile.
  - Daily punches instantly classify attendance and feed monthly lock readiness.
  - Locked attendance automatically calculates salary deductions in AutoPayroll.
  - Separation completion instantly revokes system access and triggers final settlement.
- **Diagram Suggestion**: Interconnected gear diagram showing ATS, Attendance, Leave, Payroll, and Offboarding moving in synchrony.
- **Presenter Notes**: "The real power of Codeji is automation across module boundaries. When one event occurs, all downstream dependencies update automatically without human intervention."
- **System Evidence**: Master workflows traced in `09-CROSS-MODULE-WORKFLOWS.md`.

---

## Slide 20: Multi-Tenant Boundary Security

- **Title**: Zero-Trust Tenant Data Isolation
- **Key Points**:
  - Absolute server-side tenant scoping: Tenancy derived strictly from signed JWT claims (`company_id`).
  - Client-supplied tenant headers or query overrides are completely ignored.
  - Every database query enforces `x.CompanyId == authenticatedCompanyId`.
  - Database-level compound unique indexes prevent cross-tenant collisions.
- **Diagram Suggestion**: Fortress diagram showing Company A and Company B completely isolated by cryptographic walls, sharing common compute securely.
- **Presenter Notes**: "Multi-tenancy in Codeji is built on zero-trust principles. A user from Company A can never, under any circumstances, view or modify records belonging to Company B."
- **System Evidence**: `CurrentContext.cs:36-49`, `CompanyIdMiddleware.cs:48-65`.

---

## Slide 21: Realtime Communication & Outbox Resilience

- **Title**: Guaranteed Delivery with Asynchronous Outbox Patterns
- **Key Points**:
  - In-app push notifications delivered via SignalR WebSockets (`/notificationhub`).
  - Transactional Outbox Pattern: Candidate emails, exit notices, and punch alerts are saved atomically to MongoDB before external transmission.
  - 4 dedicated background workers poll outbox queues with exponential backoff retries.
  - Delivery failures cannot cause database rollbacks or user UI lag.
- **Diagram Suggestion**: Outbox flowchart: Service -> Database Outbox Table -> Background Worker -> SMTP Gateway -> Recipient.
- **Presenter Notes**: "We never call an email server directly inside a user request. Outbox workers ensure that external network hiccups never affect the responsiveness or reliability of our application."
- **System Evidence**: `RecruitmentEmailOutboxWorker.cs`, `EmployeeExitOutboxWorker.cs`, `AttendanceOutboxWorker.cs`.

---

## Slide 22: Reporting, Analytics & Auditability

- **Title**: Actionable Workforce Insights & Statutory Non-Repudiation
- **Key Points**:
  - Operational analytics: Recruitment funnel conversion rates, interview feedback SLAs, leave consumption trends.
  - Pervasive audit trails: Creation, mutation, and soft-delete actor timestamps on every record.
  - Dedicated audit collections: `AttendanceAuditLog`, `ApplicantLogs`, and `EmployeeExit.AuditHistory`.
  - Comprehensive Excel and CSV export capabilities for executive reviews.
- **Diagram Suggestion**: Analytics dashboard mockup showing Recruitment Funnel, Attendance Heatmap, and Audit Log Export table.
- **Presenter Notes**: "Data-driven HR teams need real-time metrics. Codeji provides deep operational visibility alongside tamper-evident audit trails for labor law compliance."
- **System Evidence**: `RecruitmentAuditExportService.cs`, `AttendanceAuditWriter.cs`, `ApplicantLogs.cs`.

---

## Slide 23: Production Readiness & Quality Assurance

- **Title**: Rigorous Technical Assessment & Hardening Roadmap
- **Key Points**:
  - Core business workflows are verified, operational, and backed by automated tests.
  - 65 backend unit/contract test suites and 16 frontend component test suites.
  - Identified remediation priorities: MongoDB multi-document transactions (P0), OpenTelemetry distributed tracing (P1), and service modularization (P1).
  - Clear, risk-prioritized delivery path ensuring enterprise general availability.
- **Diagram Suggestion**: Readiness scorecard showing green checkmarks across Core Features, RBAC, and Tenant Security, with targeted P0/P1 items in progress.
- **Presenter Notes**: "We maintain an honest, evidence-based engineering culture. The system is functional and secure today, with a prioritized hardening roadmap for high-scale enterprise deployment."
- **System Evidence**: Detailed findings in `15-PRODUCTION-READINESS.md`.

---

## Slide 24: Technical & Product Roadmap

- **Title**: Strategic Evolution & Enterprise Scale
- **Key Points**:
  - **Phase 1**: Native MongoDB replica set transactions and micro-modular service decomposition.
  - **Phase 2**: Biometric IoT hardware punch gateway and Slack/Microsoft Teams bots.
  - **Phase 3**: AI-assisted resume parsing, attrition risk modeling, and shift optimization.
  - **Phase 4**: Global multi-jurisdiction statutory tax engines (US, UK, Middle East) and multi-currency payroll.
- **Diagram Suggestion**: Horizontal 4-phase milestone timeline highlighting key architectural deliverables.
- **Presenter Notes**: "Our roadmap extends Codeji from an internal operating system to a global enterprise SaaS platform, integrating IoT hardware, artificial intelligence, and international compliance."
- **System Evidence**: Full roadmap detailed in `16-FUTURE-ROADMAP.md`.

---

## Slide 25: The Codeji HRMS Value Proposition

- **Title**: The Unified Platform for Modern Enterprises
- **Key Points**:
  - **Single Source of Truth**: Completely eliminates spreadsheets, duplicate data entry, and payroll errors.
  - **Enterprise Security**: Cryptographically enforced tenant isolation and fine-grained RBAC.
  - **Total Compliance**: Tamper-proof audit logs, policy snapshots, and automated statutory calculations.
  - **Rapid ROI**: Drastically reduces HR administrative overhead while elevating employee experience.
- **Diagram Suggestion**: Value summary graphic highlighting: Security, Automation, Flexibility, Scalability, and Compliance.
- **Presenter Notes**: "In summary, Codeji HRMS delivers the agility of modern web software backed by enterprise-grade governance. Thank you, and we welcome your questions."
- **System Evidence**: Verified implementation across `CMS-Backend-Core` and `CMS-React`.
