# Platform and Cross-Cutting Services

This module documents behavior that is intentionally not owned by one business feature: application startup/DI, Mongo repository behavior, tenant context, notifications, background processing, migrations and shared result/error conventions.

Main sources: `Codeji.CMS.API/Program.cs`, `Codeji.CMS.Services/Registration`, `Codeji.CMS.Repository/Repositories/MongoRepository.cs`, `Codeji.CMS.Migrations/Migrations`, `Codeji.CMS.Services/BackgroundTasks`, and feature-local hosted services/outbox processors.
