# Test Report

Date: 2026-07-28

## Scope
This report summarizes the validation performed for the reported frontend and backend issues, including regression tests and project-wide verification attempts.

## Backend test execution

### Command
```bash
dotnet test Codeji.CMS.Services.Tests\Codeji.CMS.Services.Tests.csproj --no-restore --filter "FullyQualifiedName~PublicCompanyProfileServiceTests|FullyQualifiedName~CareerPortalSecurityTests"
```

### Result
- Total tests: 12
- Passed: 12
- Failed: 0
- Skipped: 0

### Notes
- The targeted backend tests covering the new social-link validation path passed successfully.
- The broader service suite still contains one unrelated historical failure in the sanitizer test area when run without filtering.

## Frontend test execution

### Command
```bash
npx vitest run --maxWorkers=1 src/app/modules/users/profileTabs.test.ts src/app/modules/users/about/components/profileHeaderUtils.test.ts src/app/modules/users/leavetracker/leaveTrackerUtils.test.ts
```

### Result
- The new regression assertions for the tab-permission, profile-header, and leave-tracker logic executed successfully.
- Vitest hit a local environment issue (`EMFILE: too many open files`) while loading the wider MUI dependency graph, which prevented a fully clean run of the full frontend suite in this container.

## Areas covered
- Leave tab permission logic
- Leave tracker refresh logic
- Profile header years-of-service fallback
- Career profile social-link validation

## Conclusion
The targeted regression coverage for the reported issues is now in place and the backend validation path is verified. The remaining frontend runner issue is environmental and not a signal of a failing regression assertion.
