# Career, Salary, and Dashboard APIs

## Controllers
- Source: [Codeji.CMS.API/Controllers/CareerProfileController.cs](../Codeji.CMS.API/Controllers/CareerProfileController.cs)
- Source: [Codeji.CMS.API/Controllers/SalaryController.cs](../Codeji.CMS.API/Controllers/SalaryController.cs)
- Source: [Codeji.CMS.API/Controllers/DashboardController.cs](../Codeji.CMS.API/Controllers/DashboardController.cs)

## Overview
These endpoints manage career profile information, salary data, and dashboard summaries used by the administration and employee experience.

## Endpoint map

### Career profile APIs
- Purpose: supports career profile management and related profile data.

### Salary APIs
- POST /api/salary/UpdateSalary
- POST /api/salary/GetCompanySalaries
  - Purpose: manages salary records and company salary lists.

### Dashboard APIs
- POST /api/dashboard/GetAllGenderDetails
- POST /api/dashboard/GetApplicationStatusData
- POST /api/dashboard/GetAllDepartmentDetails
- POST /api/dashboard/GetUpComingHolidayAndEvents
- POST /api/dashboard/GetUpComingCelebrations
  - Purpose: returns dashboard metrics and upcoming company events.

## Service dependencies
- ICareerProfileService
- ISalaryService
- IDashboardService

## Implementation notes
- These endpoints are mostly read-oriented and provide aggregated views for UI dashboards and profile screens.
