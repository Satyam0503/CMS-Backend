# Platform Data and Rules

| Concern | Primary source |
|---|---|
| Startup/auth/middleware | `Codeji.CMS.API/Program.cs` |
| Service registration | `Codeji.CMS.Services/Registration` |
| Repository behavior | `Codeji.CMS.Repository/Repositories/MongoRepository.cs` |
| Entities | `Codeji.CMS.Repository/Entities` |
| Migrations | `Codeji.CMS.Migrations/Migrations` |
| Shared constraints | `Codeji.CMS.Utility/Constraints` |
| Background processing | `Codeji.CMS.Services/BackgroundTasks` and feature folders |

Tenant isolation is a cross-cutting invariant: request DTOs should not establish company authority. Each business service must filter reads/writes by the authenticated company and additionally apply feature ownership/permission rules.
