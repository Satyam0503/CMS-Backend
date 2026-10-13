# QA Bug Report 2 — Fresh findings only

Date: 2026-07-27  
Scope: Read-only frontend and backend review. This report excludes the previously documented tenant-header bug and the earlier sanitizer/templating findings.

## Summary

I reviewed the React frontend and backend module logic for additional issues. The items below are separate from the earlier bug set and focus on new logic / UI problems.

## Confirmed issues

### BUG-2-01 — Leave tab visibility is gated by the wrong permission in the profile header

Severity: Medium  
Area: Frontend UI / permissions  
File:

- `CMS-React/src/app/modules/users/UserProfile.tsx`

Problem:

The `Leave` tab visibility helper is named `isLeaveActive`, but it checks `ModulesName.NoticeBoard` instead of `ModulesName.LeaveManagement`.

Evidence:

- `isLeaveActive` is computed with `hasModuleFeaturePermission(ModulesName.NoticeBoard, Permission.View, userModulePermission)`.
- The same component separately filters the leave tab using `ModulesName.LeaveManagement`.

Why this is a bug:

- A user can have leave access and still not see the leave tab if they lack notice-board permission.
- Conversely, notice-board permission can affect a completely unrelated profile tab.
- That makes the profile navigation inconsistent with route-level authorization.

Expected:

Leave tab visibility should be controlled only by `LeaveManagement.View`.

Actual:

The tab display is tied to `NoticeBoard.View`.

Impact:

- Hidden leave tab for valid users
- Confusing UI behavior
- Mismatch between visible navigation and actual authorization rules

### BUG-2-02 — Leave tracker does not refresh when switching employee profiles

Severity: Medium  
Area: Frontend UI / data flow  
File:

- `CMS-React/src/app/modules/users/leavetracker/LeaveTracker.tsx`

Problem:

The effect that loads leave balance depends only on `currentUserId`, even though the request target is `employeeId ?? currentUserId`.

Evidence:

- The fetch uses `employeeId ?? currentUserId`.
- The effect dependency array is `[currentUserId]`.

Why this is a bug:

- When the route changes from one employee profile to another, `employeeId` changes but the effect does not rerun.
- The UI can therefore keep showing the previous employee’s leave balance and request history.

Expected:

Changing the viewed employee should trigger a reload of the leave data.

Actual:

The component only refreshes when the logged-in user ID changes.

Impact:

- Stale leave data on employee profile pages
- Wrong leave balances / request lists shown after navigation
- Confusing HR/self-service profile experience

### BUG-2-03 — Profile header can render `NaN` for years of service when joining date is missing or invalid

Severity: Low to Medium  
Area: Frontend UI  
File:

- `CMS-React/src/app/modules/users/about/components/ProfileHeader.tsx`

Problem:

`dayjs().diff(userDetails.dateOfJoining, "years")` is called directly without guarding against an empty, null, or malformed joining date.

Why this is a bug:

- If the employee record has no valid joining date, the UI can show `NaN` or an incorrect number instead of a clean fallback.
- This is a user-visible data-quality problem in a core profile widget.

Expected:

Missing joining dates should display a safe fallback such as `0` or `N/A`.

Actual:

The component computes the diff directly.

Impact:

- Broken profile summary card
- Poor data presentation on incomplete employee records

### BUG-2-04 — Career profile save can fail on duplicate or empty social-link keys

Severity: Medium  
Area: Backend logic / validation  
File:

- `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs`

Problem:

The save flow converts social links with:

`request.SocialLinks.ToDictionary(x => CleanText(x.Key) ?? "", x => CleanText(x.Value) ?? "")`

This can throw if two keys sanitize to the same value, or if sanitization collapses a key to an empty string.

Why this is a bug:

- A valid form submission can fail at runtime with an `ArgumentException`.
- The service does not normalize or validate duplicate keys before building the dictionary.

Expected:

The backend should reject invalid social-link input with a clear validation message.

Actual:

The save path may throw during dictionary construction.

Impact:

- Career profile editing can fail unexpectedly
- Poor validation feedback to the UI
- Risk of lost admin changes if the form is resubmitted

## Notes

- I intentionally did not include the earlier tenant-isolation `cId` header issue in this report.
- I also did not repeat the earlier sanitizer / templating findings.

## Recommended follow-up

If you want a third pass, the most useful next targets are:

- recruitment and public-career forms
- attendance exception screens
- payroll month-close UI and error handling
- shared table / modal components for missing empty-state and validation bugs

