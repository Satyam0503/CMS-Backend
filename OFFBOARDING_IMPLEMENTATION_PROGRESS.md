# Production Offboarding Implementation Progress

This checklist tracks code present on `feature/production-offboarding`. It is not a deployment record.

## Completed

- [x] Tenant-scoped exit cases with a version field and atomic update filters.
- [x] Configurable offboarding policy snapshots, permission checks, manager review, HR review, discussions, withdrawal, cancellation, notice/LWD adjustment, and rejoin.
- [x] Handover, asset, IT-access, finance, benefit, clearance, payroll, and final-settlement completion gates.
- [x] Private evidence storage, upload validation, evidence status transitions, email/reference evidence metadata, and permission-aware download checks.
- [x] Reminder outbox claiming and audit history.
- [x] Former-employee portal: one-time invite redemption, short-lived sessions, portal revocation, document access, and requests.
- [x] Former-employee request fulfilment lifecycle: authorized HR users can mark a request in progress, issued, or closed; every action uses tenant scope, a version check, and an audit event.
- [x] React employee/management/portal surfaces for the implemented endpoints, including responsive policy settings, notice adjustment, handover, clearances, assets, IT access, finance, benefits, settlement, evidence, portal access, and completion controls.

## Before a production release

- [ ] Apply and verify the Mongo migrations against a production-like database, including indexes and existing company permission assignments.
- [x] Connect former-employee portal invitations to the existing audited SMTP mail provider. A mail-delivery failure is logged and returned to the authorized HR user while preserving the newly generated one-time link for secure manual handling.
- [ ] Choose and configure the production document store if the current private local-disk evidence storage is not acceptable for the target deployment topology.
- [ ] Add end-to-end browser/API tests against a real authenticated environment, including both an authorized HR user and an unauthorized cross-company user.
- [x] Run the full backend suite and frontend type/lint/build checks. Latest result: 154 service tests and 45 API tests passed; frontend typecheck and production build passed; lint completed with 0 errors.
- [ ] Apply and verify migrations against a production-like database, then commit and deploy the reviewed feature branch.

## Safety notes

- There are no role-name checks for HR offboarding actions; authorization is evaluated through the tenant's Offboarding permissions (with the application's existing administrator bypass).
- “Issued” records fulfilment state. The related document must also be uploaded and published with employee visibility before it can be downloaded from the former-employee portal.
- Portal invitations are sent through the existing SMTP path to the employee's recorded personal email. Sender credentials and deliverability must be verified in the target environment before release.
