# Recruitment Module Guide

## Purpose
The recruitment module supports internal vacancy management, applicant tracking, comments, resume upload, and public-facing recruitment flows.

## Core flow
1. Internal administrators create or edit job vacancies.
2. Applicants or public users apply through the applicant or public-career endpoints.
3. The platform stores applicant data, comments, and process logs for recruiting teams.
4. Public-facing endpoints expose jobs and allow job applications without requiring a signed-in account.

## Key components
- Controller: [Codeji.CMS.API/Controllers/JobVacancyController.cs](../Codeji.CMS.API/Controllers/JobVacancyController.cs)
- Applicants controller: [Codeji.CMS.API/Controllers/ApplicantsController.cs](../Codeji.CMS.API/Controllers/ApplicantsController.cs)
- Public controllers: [Codeji.CMS.API/Controllers/PublicCareerController.cs](../Codeji.CMS.API/Controllers/PublicCareerController.cs)

## Main APIs
- POST /api/jobvacancy/AddJobVacancy
- POST /api/jobvacancy/EditJobVacancy
- POST /api/jobvacancy/GetAllVacancy
- POST /api/jobvacancy/GetVacancyById
- DELETE /api/jobvacancy/DeleteJobVacancy
- POST /api/applicants/GetApplicantList
- GET /api/applicants/ApplicantById
- POST /api/applicants/AddApplicant
- POST /api/applicants/EditApplicant
- POST /api/applicants/UploadResume
- POST /api/applicants/AddComment
- GET /api/applicants/GetAllComment/{applicantId}
- POST /api/public/jobs
- POST /api/public/jobs/{publicJobId}/applications

## Notes for contributors
- Public recruitment flows and internal applicant management share the same underlying domain services.
- Resume upload and public application submissions should be validated carefully for file type, size, and token handling.
