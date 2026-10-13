# UI Route Index

Last verified against source: 2026-09-02. React route composition is split across `../CMS-React/src/app/routing/AppRoutes.tsx`, `PrivateRoutes.tsx`, `RecruitmentRoutes.tsx`, `ProfileRoutes.tsx`, `PoliciesRoutes.tsx`, `LeaveManagementRoutes.tsx`, `NoticeBoardRoutes.tsx`, and `src/app/modules/admin/adminRoutes.tsx`. Access is evaluated by route guards and feature-level permitted actions; this index does not substitute for those checks.

| Area | Main route source / page | API documentation |
|---|---|---|
| Authentication | `modules/auth/AuthPage.tsx` | `API_INDEX.md` |
| Profile and employee exit | `modules/users/UserProfile.tsx`; `CompanyDetails.tsx` child route `offboarding` | `offboarding/UI.md` |
| Leave | `routing/LeaveManagementRoutes.tsx`; `modules/leaves/LeaveManagement.tsx` | `leave-management/README.md` |
| Recruitment | `routing/RecruitmentRoutes.tsx` | `recruitment/README.md` |
| Admin/company | `modules/admin/adminRoutes.tsx`; `CompanyDetails.tsx` | `company/README.md` |

> ⚠ Not conclusively determined from current code: a single static route table. Routes are composed from the files above, so inspect the relevant composition file for an exact browser URL before a UI change.
