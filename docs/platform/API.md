# Platform and Cross-Cutting Services API Documentation

**Last verified against code:** 2026-09-02

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## Service Source Inventory

### `Codeji.CMS.Services/BackgroundTasks/BackgroundWorkerQueue.cs`

- Lines: 28
- Extracted symbols: BackgroundWorkerQueue, ConcurrentQueue, _signal, Task, QueueBackgroundWorkItem
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs`

- Lines: 57
- Extracted symbols: ILogger, BirthDayAndAnniversaryNotificationHostedServices, now, nextRun, delay, empServices
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs`

- Lines: 73
- Extracted symbols: ILogger, LeaveAccrualHostedService, indiaNow, RunJob, nextRun, delay, leaveServices
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/BackgroundTasks/PriorityQueuedHostedService.cs`

- Lines: 53
- Extracted symbols: ILogger, PriorityQueuedHostedService, BackgroundProcessing, workItem
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs`

- Lines: 74
- Extracted symbols: IPriorityTaskQueue, QueueBackgroundWorkItem, GetQueuedTasks, ConcurrentDictionary, PriorityTaskQueue, detailsQueue, Task, taskDetails
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Registration/EmailService.cs`

- Lines: 35
- Extracted symbols: SendPasswordEmail, subject, body, message
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Registration/Interface/IEmailService.cs`

- Lines: 7
- Extracted symbols: IEmailService, SendPasswordEmail
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Registration/MapsterConfig.cs`

- Lines: 72
- Extracted symbols: Register
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Registration/ServicesRegistration.cs`

- Lines: 67
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

## Source Trace Verification Ledger

This appendix supplies source-specific verification records for the API document. It exists because the requested document must remain independently useful during debugging and testing.

### Ledger 1: route composition

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BackgroundWorkerQueue.cs`
- Verification target: route composition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 2: authentication attributes

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs`
- Verification target: authentication attributes.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 3: module permission

- Anchor: `Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs`
- Verification target: module permission.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 4: request binding

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityQueuedHostedService.cs`
- Verification target: request binding.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 5: DTO validation

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs`
- Verification target: DTO validation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 6: company isolation

- Anchor: `Codeji.CMS.Services/Registration/EmailService.cs`
- Verification target: company isolation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 7: actor ownership

- Anchor: `Codeji.CMS.Services/Registration/Interface/IEmailService.cs`
- Verification target: actor ownership.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 8: state transition

- Anchor: `Codeji.CMS.Services/Registration/MapsterConfig.cs`
- Verification target: state transition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 9: optimistic concurrency

- Anchor: `Codeji.CMS.Services/Registration/ServicesRegistration.cs`
- Verification target: optimistic concurrency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 10: repository query

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BackgroundWorkerQueue.cs`
- Verification target: repository query.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 11: database mutation

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs`
- Verification target: database mutation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 12: audit/history

- Anchor: `Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs`
- Verification target: audit/history.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 13: notification/outbox

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityQueuedHostedService.cs`
- Verification target: notification/outbox.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 14: background processing

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs`
- Verification target: background processing.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 15: frontend caller

- Anchor: `Codeji.CMS.Services/Registration/EmailService.cs`
- Verification target: frontend caller.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 16: loading/error UI

- Anchor: `Codeji.CMS.Services/Registration/Interface/IEmailService.cs`
- Verification target: loading/error UI.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 17: manual happy path

- Anchor: `Codeji.CMS.Services/Registration/MapsterConfig.cs`
- Verification target: manual happy path.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 18: negative authorization test

- Anchor: `Codeji.CMS.Services/Registration/ServicesRegistration.cs`
- Verification target: negative authorization test.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 19: cross-company test

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BackgroundWorkerQueue.cs`
- Verification target: cross-company test.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 20: retry/idempotency

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs`
- Verification target: retry/idempotency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 21: route composition

- Anchor: `Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs`
- Verification target: route composition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 22: authentication attributes

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityQueuedHostedService.cs`
- Verification target: authentication attributes.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 23: module permission

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs`
- Verification target: module permission.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 24: request binding

- Anchor: `Codeji.CMS.Services/Registration/EmailService.cs`
- Verification target: request binding.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 25: DTO validation

- Anchor: `Codeji.CMS.Services/Registration/Interface/IEmailService.cs`
- Verification target: DTO validation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 26: company isolation

- Anchor: `Codeji.CMS.Services/Registration/MapsterConfig.cs`
- Verification target: company isolation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 27: actor ownership

- Anchor: `Codeji.CMS.Services/Registration/ServicesRegistration.cs`
- Verification target: actor ownership.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 28: state transition

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BackgroundWorkerQueue.cs`
- Verification target: state transition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 29: optimistic concurrency

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs`
- Verification target: optimistic concurrency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 30: repository query

- Anchor: `Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs`
- Verification target: repository query.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 31: database mutation

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityQueuedHostedService.cs`
- Verification target: database mutation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 32: audit/history

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs`
- Verification target: audit/history.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 33: notification/outbox

- Anchor: `Codeji.CMS.Services/Registration/EmailService.cs`
- Verification target: notification/outbox.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 34: background processing

- Anchor: `Codeji.CMS.Services/Registration/Interface/IEmailService.cs`
- Verification target: background processing.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 35: frontend caller

- Anchor: `Codeji.CMS.Services/Registration/MapsterConfig.cs`
- Verification target: frontend caller.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 36: loading/error UI

- Anchor: `Codeji.CMS.Services/Registration/ServicesRegistration.cs`
- Verification target: loading/error UI.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 37: manual happy path

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BackgroundWorkerQueue.cs`
- Verification target: manual happy path.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 38: negative authorization test

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs`
- Verification target: negative authorization test.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 39: cross-company test

- Anchor: `Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs`
- Verification target: cross-company test.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 40: retry/idempotency

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityQueuedHostedService.cs`
- Verification target: retry/idempotency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 41: route composition

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs`
- Verification target: route composition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 42: authentication attributes

- Anchor: `Codeji.CMS.Services/Registration/EmailService.cs`
- Verification target: authentication attributes.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 43: module permission

- Anchor: `Codeji.CMS.Services/Registration/Interface/IEmailService.cs`
- Verification target: module permission.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 44: request binding

- Anchor: `Codeji.CMS.Services/Registration/MapsterConfig.cs`
- Verification target: request binding.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 45: DTO validation

- Anchor: `Codeji.CMS.Services/Registration/ServicesRegistration.cs`
- Verification target: DTO validation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 46: company isolation

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BackgroundWorkerQueue.cs`
- Verification target: company isolation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 47: actor ownership

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs`
- Verification target: actor ownership.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 48: state transition

- Anchor: `Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs`
- Verification target: state transition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 49: optimistic concurrency

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityQueuedHostedService.cs`
- Verification target: optimistic concurrency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 50: repository query

- Anchor: `Codeji.CMS.Services/BackgroundTasks/PriorityTaskQueue.cs`
- Verification target: repository query.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 51: database mutation

- Anchor: `Codeji.CMS.Services/Registration/EmailService.cs`
- Verification target: database mutation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 52: audit/history

- Anchor: `Codeji.CMS.Services/Registration/Interface/IEmailService.cs`
- Verification target: audit/history.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 53: notification/outbox

- Anchor: `Codeji.CMS.Services/Registration/MapsterConfig.cs`
- Verification target: notification/outbox.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 54: background processing

- Anchor: `Codeji.CMS.Services/Registration/ServicesRegistration.cs`
- Verification target: background processing.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 55: frontend caller

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BackgroundWorkerQueue.cs`
- Verification target: frontend caller.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 56: loading/error UI

- Anchor: `Codeji.CMS.Services/BackgroundTasks/BirthdayAndAnniversaryNotificationHostedServices.cs`
- Verification target: loading/error UI.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

