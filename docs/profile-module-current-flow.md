# Career Profile Module — Current Flow

> Status: Current reference for the company career profile feature that feeds the public careers experience.

## 1. What this module owns

This module stores the public-facing company profile content that appears on the careers site:

- display name;
- short description;
- about page HTML;
- logo / cover image URLs;
- website and social links;
- industry / company type / size;
- mission / vision / values;
- benefits and technologies;
- culture / hiring-process / diversity content.

It is intentionally separate from the operational `Company` record.

## 2. Core files

| Area | File |
|---|---|
| Controller | [`Codeji.CMS.API/Controllers/CareerProfileController.cs`](../Codeji.CMS.API/Controllers/CareerProfileController.cs) |
| Service | [`Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs`](../Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs) |
| DTOs | [`Codeji.CMS.DTO/CareerPortal/`](../Codeji.CMS.DTO/CareerPortal/) |
| Entity | [`Codeji.CMS.Repository/Entities/CareerPortal/CareerPortalEntities.cs`](../Codeji.CMS.Repository/Entities/CareerPortal/CareerPortalEntities.cs) |
| Related company entity | [`Codeji.CMS.Repository/Entities/Company/Company.cs`](../Codeji.CMS.Repository/Entities/Company/Company.cs) |

## 3. Permission model

The controller uses:

- `Career_Profile.View`
- `Career_Profile.Edit`

## 4. Primary data flow

### 4.1 Read the current company profile

`GET /api/careerprofile`

Flow:

1. Resolve the current tenant from the authenticated context.
2. Load the operational company record.
3. Load the profile document for that company.
4. Merge company defaults with profile overrides.

### 4.2 Save the company profile

`PUT /api/careerprofile`

Flow:

1. Resolve current company and user.
2. Validate length limits.
3. Validate all external URLs:
   - must be absolute HTTPS;
   - must not be loopback;
   - must not include user-info.
4. Sanitize HTML and text fields.
5. Insert or replace the profile document.
6. Update audit fields.

### 4.3 Public profile retrieval

The service also exposes published-profile reads for public-career use.

Flow:

1. Find the company by public company code.
2. Require:
   - company active;
   - company not deleted;
   - career portal enabled.
3. Load the published profile document.
4. Return the public-facing DTO.

## 5. Important business rules

- Profile text is sanitized before persistence.
- Profile URLs must be safe HTTPS URLs.
- List fields are deduplicated and trimmed.
- Published profiles are optional; if no profile exists, company defaults still surface.

## 6. Connections to other modules

### Jobs

Jobs and profiles meet on the public careers site:

- the profile provides branding and descriptive content;
- jobs provide the actual openings;
- both are scoped to the same company/public code.

### Applicants

Applicants see the company branding when they arrive from a public vacancy page.

### Company

The underlying company record provides the stable company identity and public company code. The profile enriches that identity without overwriting payroll/auth/company master data.

## 7. Risks and watch-outs

- Rich HTML content must stay sanitized.
- External links are intentionally restricted to safe HTTPS URLs.
- The profile is a separate document, so edits should not mutate operational company settings.

