# Leave Management APIs

## Controller
- Source: [Codeji.CMS.API/Controllers/LeaveManagementController.cs](../Codeji.CMS.API/Controllers/LeaveManagementController.cs)

## Overview
This controller manages leave policies, employee leave balances, leave requests, request approvals, and leave summary reporting.

## Endpoint map

### Leave policy management
- POST /api/leavemanagement/CreateLeavePolicy
  - Purpose: creates a leave policy.
  - Permission: Leave_Management.Create.

- POST /api/leavemanagement/UpdateLeavePolicy
  - Purpose: updates an existing leave policy.
  - Permission: Leave_Management.Edit.

- POST /api/leavemanagement/GetAllLeavePolicies
  - Purpose: lists leave policies for the current company.
  - Permission: Leave_Management.View.

### Leave balances
- GET /api/leavemanagement/GetEmployeeLeaveBalance/{employeeId}
  - Purpose: retrieves the balance for a specific employee.

- GET /api/leavemanagement/me/balances
  - Purpose: returns the current employee’s own leave balance.

- POST /api/leavemanagement/UpdateEmployeeLeaveBalance
  - Purpose: updates employee balances in bulk.

- POST /api/leavemanagement/GetAllEmployeeLeaveBalances
  - Purpose: lists balances for many employees.

### Leave requests
- POST /api/leavemanagement/CreateLeaveRequest
- POST /api/leavemanagement/me/requests
- POST /api/leavemanagement/CreateEmployeeLeaveRequest/{employeeId}
  - Purpose: create leave requests for the current employee or for another employee by HR/admin.

- POST /api/leavemanagement/UpdateLeaveRequest
- PATCH /api/leavemanagement/me/requests
  - Purpose: update leave requests for the current employee or the current user context.

- POST /api/leavemanagement/GetLeaveRequests
- POST /api/leavemanagement/GetMyLeaveRequests
- POST /api/leavemanagement/me/requests/search
  - Purpose: fetch leave requests for HR/admin or the current employee.

- DELETE /api/leavemanagement/DeleteLeaveRequest/{leaveRequestId}
- DELETE /api/leavemanagement/me/requests/{leaveRequestId}
  - Purpose: delete a leave request.

- PATCH /api/leavemanagement/LeaveRequest/{leaveRequestId}/Status
  - Purpose: update the request status (for approval or rejection workflows).

### Leave reporting
- GET /api/leavemanagement/GetLeaveRequestSummary
- GET /api/leavemanagement/GetMonthlyTakenLeaveSummary/{year}
  - Purpose: generate summary views used by reporting and HR dashboards.

## Service dependencies
- ILeaveManagementService

## Implementation notes
- The controller explicitly supports both admin-driven and employee self-service flows.
- Leave validation and balance rules are implemented in the service layer, not in the controller.
