# Public Career APIs

## Controllers
- Source: [Codeji.CMS.API/Controllers/PublicCareerController.cs](../Codeji.CMS.API/Controllers/PublicCareerController.cs)
- Source: [Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs](../Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs)
- Source: [Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs](../Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs)
- Source: [Codeji.CMS.API/Controllers/PublicCompanyProfileController.cs](../Codeji.CMS.API/Controllers/PublicCompanyProfileController.cs)

## Overview
These endpoints expose public-facing career portal capabilities such as listing jobs, applying, saving jobs, managing job alerts, subscribing, and publishing company profiles.

## Endpoint map

### Public jobs and applications
- GET /api/public/jobs
- GET /api/public/jobs/{publicJobId}
- GET /api/public/jobs/by-slug/{jobSlug}
- GET /api/public/companies/{companyCode}/jobs
- GET /api/public/job-locations
- POST /api/public/jobs/{publicJobId}/applications
- POST /api/public/applications/{applicationReference}/resume

### Public career engagement
- POST /api/public/career/jobs/{publicJobId}/save
- DELETE /api/public/career/jobs/{publicJobId}/save
- GET /api/public/career/jobs/saved
- POST /api/public/career/jobs/merge-anonymous-saves
- POST /api/public/career/companies/{companyCode}/follow
- DELETE /api/public/career/companies/{companyCode}/follow
- GET /api/public/career/companies/{companyCode}/follow-status
- POST /api/public/career/job-alerts
- GET /api/public/career/job-alerts
- PUT /api/public/career/job-alerts/{alertId}
- DELETE /api/public/career/job-alerts/{alertId}
- POST /api/public/career/analytics
- PUT /api/public/career/visitor-preferences
- GET /api/public/career/visitor-preferences

### Public subscriptions
- POST /api/public/career/subscribers
- POST /api/public/career/subscribers/verify
- POST /api/public/career/subscribers/resend-verification
- POST /api/public/career/subscribers/unsubscribe
- GET /api/public/career/subscribers/preferences
- PUT /api/public/career/subscribers/preferences

### Public company profiles
- GET /api/public/career/companies
- GET /api/public/career/companies/{companyCode}/profile

## Service dependencies
- IPublicCareerService
- ICareerEngagementService
- ICareerSubscriptionService
- IPublicCompanyProfileService

## Implementation notes
- These controllers are intentionally anonymous and public-facing.
- Several endpoints depend on session tokens or visitor tokens to preserve engagement state.
