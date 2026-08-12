# Leave Management Module Guide

## Purpose
The leave-management module handles leave policies, leave balances, leave requests, approvals, monthly summaries, and employee self-service request flows.

## Core flow
1. HR creates or updates leave policies for the tenant.
2. Employees create leave requests and view their own balances and requests.
3. Approvers review requests and update the status, which then affects the leave balance and notifications.
4. HR and reporting endpoints summarize request history and monthly usage.

## Key components
- Controller: [Codeji.CMS.API/Controllers/LeaveManagementController.cs](../Codeji.CMS.API/Controllers/LeaveManagementController.cs)
- Service: [Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs](../Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs)
- Supporting entities: leave policy, leave request, employee leave balance

## Main APIs
- POST /api/leavemanagement/CreateLeavePolicy
- POST /api/leavemanagement/UpdateLeavePolicy
- POST /api/leavemanagement/GetAllLeavePolicies
- GET /api/leavemanagement/GetEmployeeLeaveBalance/{employeeId}
- GET /api/leavemanagement/me/balances
- POST /api/leavemanagement/CreateLeaveRequest
- POST /api/leavemanagement/me/requests
- POST /api/leavemanagement/CreateEmployeeLeaveRequest/{employeeId}
- POST /api/leavemanagement/UpdateLeaveRequest
- PATCH /api/leavemanagement/me/requests
- POST /api/leavemanagement/GetLeaveRequests
- POST /api/leavemanagement/GetMyLeaveRequests
- PATCH /api/leavemanagement/LeaveRequest/{leaveRequestId}/Status
- GET /api/leavemanagement/GetLeaveRequestSummary
- GET /api/leavemanagement/GetMonthlyTakenLeaveSummary/{year}

## Business rules
- Leave policy creation validates accrual and carry-over requirements.
- Employee self-service routes are intentionally separate from admin and HR routes.
- Approval and status updates are the main workflow transition point.

## Notes for contributors
- Leave processing is shared with attendance and payroll data flows; audit and summary changes should be checked across all three modules.
