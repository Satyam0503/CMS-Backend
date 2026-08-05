# Jobs Module — Current Flow

> Status: Current reference for job vacancy administration and how jobs feed the public careers portal and applicant workflow.

## 1. What this module owns

The jobs module owns the internal CRUD lifecycle of vacancies:

- create a vacancy
- edit a vacancy
- list vacancies
- delete a vacancy
- expose vacancies to public-career discovery when eligible

This module is the upstream source for both:

- public careers browsing;
- applicant intake.

## 2. Core files

| Area | File |
|---|---|
| Controller | [`Codeji.CMS.API/Controllers/JobVacancyController.cs`](../Codeji.CMS.API/Controllers/JobVacancyController.cs) |
| Service | [`Codeji.CMS.Services/Recruitments/JobVacancyService.cs`](../Codeji.CMS.Services/Recruitments/JobVacancyService.cs) |
| Entity | [`Codeji.CMS.Repository/Entities/Recruitments/JobVacancy.cs`](../Codeji.CMS.Repository/Entities/Recruitments/JobVacancy.cs) |
| DTOs | [`Codeji.CMS.DTO/RequestModels/JobVacancyModel.cs`](../Codeji.CMS.DTO/RequestModels/JobVacancyModel.cs), [`GetJobVacancyModel.cs`](../Codeji.CMS.DTO/RequestModels/GetJobVacancyModel.cs), [`JobSearchModel.cs`](../Codeji.CMS.DTO/RequestModels/JobSearchModel.cs) |

## 3. Permission model

The authenticated vacancy endpoints use:

- `Jobs.Create`
- `Jobs.Edit`
- `Jobs.Delete`
- `Jobs.View`

The public vacancy list endpoint is anonymous, because it powers the career portal.

## 4. Primary data flow

### 4.1 Add vacancy

`POST /api/jobvacancy/AddJobVacancy`

Flow:

1. HR/admin submits the vacancy model.
2. Service stamps tenant-specific values.
3. Public-facing job fields are prepared for career portal use.
4. The vacancy is stored under the current company.

### 4.2 Edit vacancy

`POST /api/jobvacancy/EditJobVacancy`

Flow:

1. Load the current vacancy by `jobId`.
2. Apply edits without breaking stable public identifiers where possible.
3. Preserve career publishing fields so the public route remains stable.

### 4.3 List vacancies

`POST /api/jobvacancy/GetAllVacancy`

Flow:

1. The authenticated caller receives company-scoped vacancy data.
2. Search/filter logic is applied by the service.
3. Results are returned in the shared `Result<T>` shape.

### 4.4 Public vacancy list

`POST /api/jobvacancy/GetVacancyForApplyNow` and related public-career reads

Flow:

1. The public careers layer asks for eligible jobs.
2. The service filters by company and publishing flags.
3. Only career-visible records are returned.

### 4.5 Delete vacancy

`DELETE /api/jobvacancy/DeleteJobVacancy`

Flow:

1. The vacancy is loaded by ID.
2. A delete operation is executed.
3. The controller returns a success/failure response.

## 5. Important business rules

- A vacancy belongs to one company.
- Public visibility is separate from internal existence.
- External application mode is allowed for some jobs, but the internal application endpoint must reject them.
- Slug / public ID stability matters because the public careers site links directly to vacancy routes.

## 6. Connections to other modules

### Applicants

The applicants module consumes job vacancy data to:

- attach applications to a vacancy;
- resolve the company for the applicant;
- decide whether internal or external apply flow is allowed.

### Career Profile / Public Careers

Vacancies are displayed alongside company profile content in the public portal. Publishing decisions in this module directly affect what the public user sees.

### Company

Vacancy fields often depend on company master data such as departments, job titles, recruiter contacts, and the company’s public-career flags.

## 7. Risks and watch-outs

- Public and authenticated routes share the same underlying vacancy data.
- A bad edit can invalidate public URLs if stable identifiers are not preserved.
- Vacancy publishing is part of the public surface area, so tenant filtering must remain strict.

