# Codeji CMS / HRMS — Cross-Module Workflows

> **Document Status**: Complete & Authoritative  
> **Source-of-Truth**: Verified from Repository Implementation  
> **Workflows Traced**: Joining, Daily Operations, Resignation/Offboarding, Rejoining, Multi-Tenant Isolation  
> **Last Verified**: September 2026

---

## 1. Master Workflow A: Recruitment to Active Employment

This workflow traces a job applicant from external application on the career portal to fully activated employment:

```mermaid
sequenceDiagram
    autonumber
    actor Candidate as Job Candidate
    participant Portal as Career Marketplace (/careers)
    participant ATS as RecruitmentApplicationService
    actor HR as HR Manager
    participant OfferSvc as RecruitmentOfferService
    participant PreSvc as RecruitmentPreboardingService
    participant OnbSvc as RecruitmentOnboardingService
    participant EmpRepo as IMongoDbRepository<EmpUser>
    participant SignalR as NotificationHub

    Candidate->>Portal: Apply to Vacancy (Resume + Details)
    Portal->>ATS: POST /api/PublicCareer/ApplyJob
    ATS->>ATS: Create Applicant & RecruitmentJobApplication records
    ATS->>SignalR: Notify HR of new applicant
    HR->>ATS: Screen candidate & record Interview Scorecards
    HR->>OfferSvc: Generate Offer (CTC, JoiningDate, Designation)
    OfferSvc->>Candidate: Dispatched via RecruitmentEmailOutbox
    Candidate->>Portal: Review & Accept Offer (/candidate/offer)
    Portal->>OfferSvc: POST /api/PublicCareerEngagement/AcceptOffer
    OfferSvc->>PreSvc: Initialize RecruitmentPreboardingCase
    Candidate->>PreSvc: Upload Educational & Identity Documents
    HR->>PreSvc: Verify Documents & Sign Off Preboarding
    PreSvc->>OnbSvc: Initialize RecruitmentOnboardingCase (Task Checklist)
    HR->>OnbSvc: Complete Onboarding Tasks (IT Setup, Badge, Agreement)
    HR->>OnbSvc: PUT /api/RecruitmentOnboarding/Update (Status: "Completed")
    OnbSvc->>OnbSvc: Validate all tasks are "Completed" or "Skipped"
    OnbSvc->>EmpRepo: ActivateEmployeeLifecycle (UserId)
    Note over OnbSvc,EmpRepo: Sets Status=true, EmploymentStatus=Active, AvailabilityStatus=Working, AccountStatus=Active, PayrollStatus=Active
    EmpRepo-->>OnbSvc: Updated with Optimistic Concurrency Check (Version + 1)
    OnbSvc->>SignalR: Broadcast "Employee Activated"
```

---

## 2. Master Workflow B: Daily Workforce Operations & Payroll Synthesis

This workflow traces how daily employee activity (punching in, requesting remote work, or taking leave) flows into the monthly payroll calculation:

```mermaid
sequenceDiagram
    autonumber
    actor Emp as Employee
    participant AttSvc as AttendanceService
    participant Calc as AttendanceStatusCalculationService
    participant LeaveSvc as LeaveManagementService
    participant Recon as LeaveAttendanceReconciliationService
    actor HR as HR Manager
    participant LockSvc as AttendancePenaltyService
    participant AutoPay as AutoPayrollServices
    participant Pdf as PdfService

    Emp->>AttSvc: Mark Daily Punch (CheckIn: 09:45, CheckOut: 18:30)
    AttSvc->>Calc: CalculateOfficeStatus(Schedule, CheckIn, CheckOut)
    Calc-->>AttSvc: CheckIn exceeds GraceMinutes -> Assign "LHD" (Late Half Day)
    AttSvc->>AttSvc: Persist AttendanceModel & AttendanceDaySegment

    Note over Emp,LeaveSvc: Next Day: Employee requests Full Day Medical Leave
    Emp->>LeaveSvc: Submit LeaveRequest (1 Day Sick Leave)
    LeaveSvc-->>LeaveSvc: Check EmployeeLeaveBalance -> Balance Sufficient
    HR->>LeaveSvc: Approve Leave Request
    Recon->>Recon: Periodic Background Run: Reconcile Attendance
    Recon->>AttSvc: Update Attendance record for Leave Date -> Set Status to "L"

    Note over HR,AutoPay: Month-End: HR prepares Monthly Payroll
    HR->>LockSvc: ValidateAndLock(CompanyId, Month)
    LockSvc->>LockSvc: Check unreviewed exceptions. If zero -> Set IsLocked=true
    HR->>AutoPay: POST /api/AutoPayroll/GeneratePayrollForMonth
    AutoPay->>AutoPay: Verify Attendance is Approved & Locked
    AutoPay->>AutoPay: Resolve Divisor (e.g. 30 Days)
    AutoPay->>AutoPay: Calculate LOP Days = Unpaid Leave Days + Exceeded Penalty Days
    AutoPay->>AutoPay: Compute Gross, LOP Deduction, Tax, EPF, Net Salary
    AutoPay->>Pdf: Generate SalarySlip PDF
    AutoPay-->>HR: Payroll Successfully Processed
```

---

## 3. Master Workflow C: Resignation, Clearances & Final Settlement

This workflow traces an employee separation from resignation submission through departmental clearance to full and final financial closure:

```mermaid
sequenceDiagram
    autonumber
    actor Emp as Employee
    participant EmpSvc as EmployeeService
    participant Resolver as OffboardingPolicyResolver
    actor Mgr as Reporting Manager
    actor HR as HR Manager
    participant Outbox as EmployeeExitOutbox
    participant FnfSvc as EmployeeFinalSettlement
    participant FormerPortal as FormerEmployeePortalService

    Emp->>EmpSvc: POST /api/User/SubmitResignation (Reason, Attachment)
    EmpSvc->>Resolver: Resolve active EmployeeExitPolicy
    EmpSvc->>EmpSvc: Snapshot policy into EmployeeExit document
    EmpSvc->>EmpSvc: Status = "PendingManagerApproval"
    EmpSvc->>Outbox: Queue manager notification
    Mgr->>EmpSvc: Review & Recommend Last Working Date (LWD)
    HR->>EmpSvc: Approve Resignation -> Status = "NoticePeriod"
    Note over EmpSvc,Outbox: Notice Period Begins. Reminders fired every N days.
    
    HR->>EmpSvc: Initialize Clearance Checklists
    Note over HR,EmpSvc: IT revokes credentials; Asset Manager inspects hardware; Finance reviews advances
    HR->>EmpSvc: Sign off Clearances -> Status = "PendingSettlement"
    
    HR->>FnfSvc: Calculate Final Settlement
    FnfSvc->>FnfSvc: Pull last active processed payroll + unpaid days + leave encashment - notice recovery
    HR->>FnfSvc: Approve Settlement & Record Payment Voucher Reference
    
    HR->>EmpSvc: POST /api/User/CompleteEmployeeExit (ExitId)
    EmpSvc->>EmpSvc: Validate all clearances complete & settlement approved
    EmpSvc->>EmpSvc: Set EmpUser.Status = false, EmploymentStatus = "Resigned", AccountStatus = "Closed"
    EmpSvc->>EmpSvc: Revoke all active RefreshTokens
    EmpSvc->>EmpSvc: Insert AttendanceCutoff (Prohibits future punches)
    EmpSvc->>FormerPortal: Provision FormerEmployeeAccessToken
    FormerPortal-->>Emp: Email secure link to /former-employee
```

---

## 4. Master Workflow D: Employee Rejoin & Status Restoration

When a separated employee is re-hired by the organization:
1. **Rejoin Eligibility Verification**:
   - The system inspects the former employee's separation record (`EmployeeExit`).
   - Verifies `PolicySnapshot.AllowRejoin == true`. If the exit was classified as `Terminated` for misconduct with rejoin barred, rehire is blocked.
2. **Account Reactivation**:
   - `POST /api/User/RejoinEmployee`: Admin provides new Date of Joining, designation, reporting manager, and compensation structure.
   - `EmpUser.Status` is flipped from `false` to `true`.
   - `EmpUser.Lifecycle.EmploymentStatus` transitions from `Resigned`/`Terminated` to `Active`.
   - `EmpUser.Lifecycle.AvailabilityStatus` transitions to `Working`.
   - `EmpUser.Lifecycle.AccountStatus` transitions to `Active`.
   - `EmpUser.Lifecycle.PayrollStatus` transitions to `Active`.
   - `AlumniStatus` is updated to `NotApplicable`.
3. **Attendance & Shift Re-enrollment**:
   - Removes or archives the prior `AttendanceCutoff` record, allowing punch registration from the new Date of Joining forward.
   - Assigns a current `CompanyOfficeSchedule`.
4. **Historical Continuity**:
   - Historical attendance records, prior payslips, and previous separation records remain linked to the permanent `UserId` for statutory audit continuity.

---

## 5. Master Workflow E: Multi-Tenant Boundary Enforcement

The diagram below demonstrates how the backend systematically prevents cross-tenant data access:

```mermaid
sequenceDiagram
    autonumber
    actor Attacker as Malicious Actor (Company A Token)
    participant Gateway as Reverse Proxy / Kestrel
    participant AuthMid as JWT Authentication Handler
    participant TenantMid as CompanyIdMiddleware
    participant RoleSvc as RoleServices
    participant Mongo as MongoRepository<EmpUser>

    Attacker->>Gateway: POST /api/User/GetEmployeeDetails\nHeader: Authorization: Bearer <Token_Company_A>\nBody: { "UserId": "Target_User_Company_B" }
    Gateway->>AuthMid: Terminate TLS & Parse JWT
    AuthMid->>AuthMid: Validate HMAC signature & expiration
    AuthMid->>AuthMid: Extract claim company_id = "COMP_A", user_id = "USER_A"
    AuthMid->>TenantMid: Invoke Middleware Pipeline
    TenantMid->>TenantMid: CurrentContext.CompanyId returns "COMP_A"
    TenantMid->>RoleSvc: VerifyUserAccess(Module: Employees, Action: View)
    RoleSvc-->>TenantMid: Granted (User possesses Employee.View in COMP_A)
    TenantMid->>Mongo: Query EmpUser with Filter: x.CompanyId == "COMP_A" && x.UserId == "Target_User_Company_B"
    Mongo-->>TenantMid: Document Not Found (Filter evaluates false because target belongs to COMP_B)
    TenantMid-->>Attacker: HTTP 404 Not Found (Target user invisible across tenant boundary)
```
Even if an authenticated user attempts to request an entity by a valid foreign ID, database queries enforce `x.CompanyId == currentCompanyId`, ensuring that foreign tenant resources evaluate as non-existent.
