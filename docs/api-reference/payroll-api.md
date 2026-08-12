# Payroll and Auto-Payroll APIs

## Controllers
- Source: [Codeji.CMS.API/Controllers/PayRollController.cs](../Codeji.CMS.API/Controllers/PayRollController.cs)
- Source: [Codeji.CMS.API/Controllers/AutoPayrollController.cs](../Codeji.CMS.API/Controllers/AutoPayrollController.cs)

## Overview
These APIs cover payroll data retrieval, pay slip generation, payload upload, payroll updates, and monthly payroll processing.

## Endpoint map

### Payroll APIs
- POST /api/payroll/GetEmpPayRollData
  - Purpose: retrieves payroll data for employees.

- POST /api/payroll/GeneratePaySlip
  - Purpose: generates a payslip.

- POST /api/payroll/GenerateEmployeePaySlip
  - Purpose: generates an employee-specific payslip.

- POST /api/payroll/UploadPayloadData
  - Purpose: uploads payroll payload data.

- POST /api/payroll/AddUpdatePayRoll
  - Purpose: adds or updates a payroll record.

- POST /api/payroll/ProcessPayrollMonth
  - Purpose: processes payroll for a month.

### Auto-payroll APIs
- POST /api/autopayroll/GeneratePayRollMonthly
  - Purpose: runs monthly payroll generation.

## Service dependencies
- IPayRollService
- IAutoPayrollService

## Implementation notes
- Payroll endpoints commonly rely on attendance and leave data as upstream inputs.
- The logic is mostly orchestration-heavy and should be reviewed alongside attendance and leave calculations.
