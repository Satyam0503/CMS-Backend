# Recruitment and Applicant APIs

## Controllers
- Source: [Codeji.CMS.API/Controllers/ApplicantsController.cs](../Codeji.CMS.API/Controllers/ApplicantsController.cs)
- Source: [Codeji.CMS.API/Controllers/JobVacancyController.cs](../Codeji.CMS.API/Controllers/JobVacancyController.cs)

## Overview
These endpoints manage job vacancies, applicants, comments, resume uploads, and recruitment workflow data.

## Endpoint map

### Job vacancy APIs
- POST /api/jobvacancy/AddJobVacancy
  - Purpose: creates a new vacancy.
  - Permission: Jobs.Create.

- POST /api/jobvacancy/EditJobVacancy
  - Purpose: updates an existing vacancy.
  - Permission: Jobs.Edit.

- POST /api/jobvacancy/GetAllVacancy
  - Purpose: returns vacancies; publicly available.

- POST /api/jobvacancy/GetVacancyById
  - Purpose: returns the title or details of a vacancy.

- DELETE /api/jobvacancy/DeleteJobVacancy
  - Purpose: deletes a vacancy.
  - Permission: Jobs.Delete.

### Applicant APIs
- POST /api/applicants/GetApplicantList
  - Purpose: lists applicants with filters.
  - Permission: Applications.View.

- GET /api/applicants/ApplicantById
  - Purpose: retrieves a single applicant by ID.

- POST /api/applicants/AddApplicant
  - Purpose: creates an applicant entry.
  - Permission: Applications.Create.

- POST /api/applicants/EditApplicant
  - Purpose: updates an applicant entry.
  - Permission: Applications.Edit.

- POST /api/applicants/UploadResume
  - Purpose: uploads a resume for the applicant.
  - Auth: AllowAnonymous.

- POST /api/applicants/AddComment
- GET /api/applicants/GetAllComment/{applicantId}
  - Purpose: add and fetch applicant comments.

- POST /api/applicants/GetProcessLogData
  - Purpose: retrieves process-log data for applicants.

## Service dependencies
- IApplicantsService
- IJobVacancy

## Implementation notes
- Resume upload is public and uses a file validation layer.
- Recruitment routes are strongly tied to the public-career experience and the applicant workflow.
