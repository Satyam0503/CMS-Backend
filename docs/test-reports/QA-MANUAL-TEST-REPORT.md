# Manual QA test report

Date: 2026-07-28  
Run scope: current database, restricted to one newly created QA company if the application becomes available.  
Result: **Blocked before test-data creation.**

## Safety scope

The configured MongoDB target resolved to host `192.168.0.172`, database `codejiCmsv2`. Per the user's selected option, the intended mutation boundary was one new QA Testing Company and only run-created synthetic records. No company or other record was created because the API could not be started.

## Execution status

| Area | Status | Reason |
| --- | --- | --- |
| Employee/profile authorization | BLOCKED | API unavailable |
| Leave create/approve/reject/sync | BLOCKED | API unavailable |
| Attendance scenarios | BLOCKED | API unavailable |
| Recruitment/public careers | BLOCKED | API unavailable |
| Authentication | BLOCKED | API unavailable |
| Roles/permissions | BLOCKED | API unavailable |
| Company profile | BLOCKED | API unavailable |
| UI responsive/state checks | BLOCKED | no browser/API session |
| Payroll, processing, month locks/reopen | BLOCKED by scope | expressly unsafe in shared database |

No blocked item is classified as pass, fail, or product bug.

## Environment blocker

`dotnet run --no-launch-profile --urls http://127.0.0.1:5036` could not build because API output DLLs were locked by existing `dotnet` processes. No listener was present on port 5036. The run also emitted NU1900 package-vulnerability metadata warnings due unavailable NuGet access; those warnings did not cause the launch block.

## Required next step

Stop or identify the process holding `Codeji.CMS.API/bin/Debug/net10.0` outside this QA run, then confirm a single API instance can listen locally. I will then create the synthetic QA tenant and proceed without opening or modifying another tenant.
