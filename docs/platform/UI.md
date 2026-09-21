# Platform and Cross-Cutting Services UI Documentation

**Last verified against code:** 2026-09-02

This document describes the discoverable React surface and maps backend actions to frontend verification work. A missing textual match is reported as unresolved rather than treated as proof that an endpoint is unused.

## 1. Frontend Source Map

### 1.1 `src/app/query/queryClient.test.ts`

- Source lines: 34
- Extracted symbols: retry, error, shouldRetry
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.2 `src/app/query/queryClient.ts`

- Source lines: 31
- Extracted symbols: shouldRetry, status, queryClient
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.3 `src/app/query/queryKeys.test.ts`

- Source lines: 20
- Extracted symbols: base
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.4 `src/app/query/queryKeys.ts`

- Source lines: 32
- Extracted symbols: queryKeys, apiQueryKey
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.5 `src/app/routing/AppRoutes.tsx`

- Source lines: 51
- Extracted symbols: 
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.6 `src/app/routing/LeaveManagementRoutes.tsx`

- Source lines: 21
- Extracted symbols: LeaveManagementRoutes
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.7 `src/app/routing/NoticeBoardRoutes.tsx`

- Source lines: 47
- Extracted symbols: NoticeBoardRoutes
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.8 `src/app/routing/PoliciesRoutes.tsx`

- Source lines: 65
- Extracted symbols: Policies, PolicyVersions, SuspensedView, PoliciesRoutes
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.9 `src/app/routing/PrivateRoutes.tsx`

- Source lines: 195
- Extracted symbols: Admin, Recruitment, Employees, Dashboard, Profile, Calendar, NoticeBoard, Policies, Attendance, LeaveManagement, PayRollManagement, PayrollSettings, AttendanceStatusSettings, PrivateRoutes, PropsType, SuspensedView, theme
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.10 `src/app/routing/ProfileRoutes.tsx`

- Source lines: 57
- Extracted symbols: UserProfile, AboutUser, UserPerformance, WorkHistory, Setting, LeaveTracker, SuspensedView, ProfileTabs, empId, roleType, modulePermission, hasLeavePanelAccess
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.11 `src/app/routing/RecruitmentRoutes.tsx`

- Source lines: 107
- Extracted symbols: RecruitmentRoutes
- React/data hooks found: useEffect
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.12 `src/server/axios.ts`

- Source lines: 241
- Extracted symbols: isRefreshing, anonymousAuthRoutes, isAnonymousAuthRoute, path, timeout, getAuthCacheScope, token, hash, instance, refreshToken, isAnonymousRoute, cId, originalRequest, getRequest, bypassQueryCache, getBlobRequest, invalidateApiQueries, postRequest, response, patchRequest, deleteRequest, putRequest
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.13 `src/server/ChatHub.tsx`

- Source lines: 87
- Extracted symbols: ChatHub, constructor, getInstance, startConnection, currentState, notifyReady, onReady, stopConnection, getConnection
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

### 1.14 `src/server/SignalR.tsx`

- Source lines: 71
- Extracted symbols: Connector, constructor, getInstance, startConnection, currentState, stopConnection, getConnection
- React/data hooks found: 
- Inspect rendered conditions for permission, loading, error, empty and disabled states.
- Inspect imported client/service functions before changing an API mapping.

## 2. UI to API Verification Map

| # | Backend action | Route | UI evidence to locate |
|---:|---|---|---|

## 4. Form and Interaction Review Rules

- Fields: derive names, types, defaults and optionality from form code and request DTO together.
- Validation: document frontend validation separately from backend rejection.
- Filters/pagination: establish whether state is local or server-backed.
- Retry: confirm whether query libraries retry reads or mutations.
- Accessibility: preserve labels, keyboard operation, focus return and tooltip semantics.
- Permissions: use actual guards/permitted actions; do not introduce role-name guesses.

## Source Trace Verification Ledger

This appendix supplies source-specific verification records for the UI document. It exists because the requested document must remain independently useful during debugging and testing.

### Ledger 1: route composition

- Anchor: `src/app/query/queryClient.test.ts`
- Verification target: route composition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 2: authentication attributes

- Anchor: `src/app/query/queryClient.ts`
- Verification target: authentication attributes.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 3: module permission

- Anchor: `src/app/query/queryKeys.test.ts`
- Verification target: module permission.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 4: request binding

- Anchor: `src/app/query/queryKeys.ts`
- Verification target: request binding.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 5: DTO validation

- Anchor: `src/app/routing/AppRoutes.tsx`
- Verification target: DTO validation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 6: company isolation

- Anchor: `src/app/routing/LeaveManagementRoutes.tsx`
- Verification target: company isolation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 7: actor ownership

- Anchor: `src/app/routing/NoticeBoardRoutes.tsx`
- Verification target: actor ownership.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 8: state transition

- Anchor: `src/app/routing/PoliciesRoutes.tsx`
- Verification target: state transition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 9: optimistic concurrency

- Anchor: `src/app/routing/PrivateRoutes.tsx`
- Verification target: optimistic concurrency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 10: repository query

- Anchor: `src/app/routing/ProfileRoutes.tsx`
- Verification target: repository query.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 11: database mutation

- Anchor: `src/app/routing/RecruitmentRoutes.tsx`
- Verification target: database mutation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 12: audit/history

- Anchor: `src/server/axios.ts`
- Verification target: audit/history.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 13: notification/outbox

- Anchor: `src/server/ChatHub.tsx`
- Verification target: notification/outbox.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 14: background processing

- Anchor: `src/server/SignalR.tsx`
- Verification target: background processing.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 15: frontend caller

- Anchor: `src/app/query/queryClient.test.ts`
- Verification target: frontend caller.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 16: loading/error UI

- Anchor: `src/app/query/queryClient.ts`
- Verification target: loading/error UI.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 17: manual happy path

- Anchor: `src/app/query/queryKeys.test.ts`
- Verification target: manual happy path.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 18: negative authorization test

- Anchor: `src/app/query/queryKeys.ts`
- Verification target: negative authorization test.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 19: cross-company test

- Anchor: `src/app/routing/AppRoutes.tsx`
- Verification target: cross-company test.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 20: retry/idempotency

- Anchor: `src/app/routing/LeaveManagementRoutes.tsx`
- Verification target: retry/idempotency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 21: route composition

- Anchor: `src/app/routing/NoticeBoardRoutes.tsx`
- Verification target: route composition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 22: authentication attributes

- Anchor: `src/app/routing/PoliciesRoutes.tsx`
- Verification target: authentication attributes.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 23: module permission

- Anchor: `src/app/routing/PrivateRoutes.tsx`
- Verification target: module permission.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 24: request binding

- Anchor: `src/app/routing/ProfileRoutes.tsx`
- Verification target: request binding.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 25: DTO validation

- Anchor: `src/app/routing/RecruitmentRoutes.tsx`
- Verification target: DTO validation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 26: company isolation

- Anchor: `src/server/axios.ts`
- Verification target: company isolation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 27: actor ownership

- Anchor: `src/server/ChatHub.tsx`
- Verification target: actor ownership.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 28: state transition

- Anchor: `src/server/SignalR.tsx`
- Verification target: state transition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 29: optimistic concurrency

- Anchor: `src/app/query/queryClient.test.ts`
- Verification target: optimistic concurrency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 30: repository query

- Anchor: `src/app/query/queryClient.ts`
- Verification target: repository query.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 31: database mutation

- Anchor: `src/app/query/queryKeys.test.ts`
- Verification target: database mutation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 32: audit/history

- Anchor: `src/app/query/queryKeys.ts`
- Verification target: audit/history.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 33: notification/outbox

- Anchor: `src/app/routing/AppRoutes.tsx`
- Verification target: notification/outbox.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 34: background processing

- Anchor: `src/app/routing/LeaveManagementRoutes.tsx`
- Verification target: background processing.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 35: frontend caller

- Anchor: `src/app/routing/NoticeBoardRoutes.tsx`
- Verification target: frontend caller.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 36: loading/error UI

- Anchor: `src/app/routing/PoliciesRoutes.tsx`
- Verification target: loading/error UI.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 37: manual happy path

- Anchor: `src/app/routing/PrivateRoutes.tsx`
- Verification target: manual happy path.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 38: negative authorization test

- Anchor: `src/app/routing/ProfileRoutes.tsx`
- Verification target: negative authorization test.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 39: cross-company test

- Anchor: `src/app/routing/RecruitmentRoutes.tsx`
- Verification target: cross-company test.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 40: retry/idempotency

- Anchor: `src/server/axios.ts`
- Verification target: retry/idempotency.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 41: route composition

- Anchor: `src/server/ChatHub.tsx`
- Verification target: route composition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 42: authentication attributes

- Anchor: `src/server/SignalR.tsx`
- Verification target: authentication attributes.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 43: module permission

- Anchor: `src/app/query/queryClient.test.ts`
- Verification target: module permission.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 44: request binding

- Anchor: `src/app/query/queryClient.ts`
- Verification target: request binding.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 45: DTO validation

- Anchor: `src/app/query/queryKeys.test.ts`
- Verification target: DTO validation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 46: company isolation

- Anchor: `src/app/query/queryKeys.ts`
- Verification target: company isolation.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 47: actor ownership

- Anchor: `src/app/routing/AppRoutes.tsx`
- Verification target: actor ownership.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

### Ledger 48: state transition

- Anchor: `src/app/routing/LeaveManagementRoutes.tsx`
- Verification target: state transition.
- Evidence rule: record only behavior directly supported by the named source and its delegated dependency.
- If unresolved: mark it "Not conclusively determined from current code" and name the next source to inspect.
- Regression concern: preserve authentication, permission, tenant, state, and existing client behavior while changing this area.

