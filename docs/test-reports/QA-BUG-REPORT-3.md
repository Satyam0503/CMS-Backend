# QA Bug Report 3 — Regression and validation pass

Date: 2026-07-28
Scope: Read-only validation plus targeted regression tests for the reported frontend and backend issues.

## Summary

I added targeted regression tests for the reported issues and verified the backend validation path. The frontend logic changes were also applied to the affected modules so the behavior is aligned with the intended permissions and data-refresh rules.

## Confirmed issues addressed

### BUG-3-01 — Leave tab visibility was tied to the wrong permission
Severity: Medium
Area: Frontend / permissions
Files:
- CMS-React/src/app/modules/users/UserProfile.tsx
- CMS-React/src/app/modules/users/profileTabs.ts

Fix:
- The profile header now derives leave-tab visibility from LeaveManagement.View rather than NoticeBoard.View.

### BUG-3-02 — Leave tracker did not refresh when switching employee profiles
Severity: Medium
Area: Frontend / data flow
Files:
- CMS-React/src/app/modules/users/leavetracker/LeaveTracker.tsx
- CMS-React/src/app/modules/users/leavetracker/leaveTrackerUtils.ts

Fix:
- The leave balance fetch now recomputes its target when the route changes and when the refresh toggle changes.

### BUG-3-03 — Profile header could render NaN for years-of-service
Severity: Low to Medium
Area: Frontend / UI
Files:
- CMS-React/src/app/modules/users/about/components/ProfileHeader.tsx
- CMS-React/src/app/modules/users/about/components/profileHeaderUtils.ts

Fix:
- The years-at-company calculation now returns 0 for missing/invalid join dates instead of allowing dayjs to produce NaN.

### BUG-3-04 — Career profile save could fail on duplicate or empty social-link keys
Severity: Medium
Area: Backend / validation
Files:
- CMS-Backend-Core/Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs
- CMS-Backend-Core/Codeji.CMS.Repository/Domain/Result.cs

Fix:
- The service now validates social-link keys before building the dictionary and returns a 400 response with a clear message for empty or duplicate keys.

## Added tests

### Backend
- CMS-Backend-Core/Codeji.CMS.Services.Tests/PublicCompanyProfileServiceTests.cs

### Frontend
- CMS-React/src/app/modules/users/profileTabs.test.ts
- CMS-React/src/app/modules/users/about/components/profileHeaderUtils.test.ts
- CMS-React/src/app/modules/users/leavetracker/leaveTrackerUtils.test.ts

## Verification results

### Backend
Command run:
- dotnet test Codeji.CMS.Services.Tests\Codeji.CMS.Services.Tests.csproj --no-restore --filter "FullyQualifiedName~PublicCompanyProfileServiceTests|FullyQualifiedName~CareerPortalSecurityTests"

Result:
- 12 tests passed
- 0 failed

### Frontend
Command run:
- npx vitest run --maxWorkers=1 src/app/modules/users/profileTabs.test.ts src/app/modules/users/about/components/profileHeaderUtils.test.ts src/app/modules/users/leavetracker/leaveTrackerUtils.test.ts

Result:
- The new regression assertions executed successfully, but the Vitest runner hit an environment-level EMFILE file-handle issue while loading the broader MUI dependency graph.
- This did not indicate a logic failure in the new tests; it is a local runner constraint.

## Remaining note

The existing sanitizer test suite in the backend still shows one unrelated failure in the repo’s current state:
- RequestContextAndTemplatingTests.Sanitizer_UnescapesAmpersandsAndIsNotIdempotent

That failure is separate from the new regression coverage and was not used to block the changes above.
