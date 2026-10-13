# QA Bug Report

Date: 2026-07-27  
Scope: Read-only testing and code inspection. No source, database, or configuration changes were made.

## Test execution summary

Command run:

```bash
dotnet test Codeji.CMS.Services.Tests\Codeji.CMS.Services.Tests.csproj --no-restore
```

Result:

- 65 tests passed
- 1 test failed
- The failing test was `RequestContextAndTemplatingTests.Sanitizer_UnescapesAmpersandsAndIsNotIdempotent`

Important note: that failing test does not prove a product defect by itself. The underlying sanitizer behavior looks stable from source inspection, so I have not filed it as a confirmed bug.

## Confirmed bugs

### BUG-01 — Anonymous requests can choose the tenant by setting the `cId` header

Severity: High  
Area: Auth / tenant isolation  
Files:

- `Codeji.CMS.Utility\middlewares\CurrentContext.cs`
- `Codeji.CMS.API\App_Start\AntyForgeryMiddleware.cs`
- `Codeji.CMS.API\App_Start\CompanyIdMiddleware.cs`
- `Codeji.CMS.Repository\Repositories\MongoRepository.cs`

Problem:

`CurrentContext.CompanyId(...)` returns the caller-supplied `cId` request header whenever the request is not authenticated. That value is then copied into `HttpContext.Items["CompanyId"]`, and the repository layer uses it to build the tenant filter.

Why this is a bug:

- Public endpoints or unauthenticated flows can be pointed at an arbitrary tenant context by the client.
- That means tenant scope is derived from user input instead of trusted server state.
- In a multi-tenant system, that is a classic cross-tenant isolation risk.

Expected:

Anonymous or unauthenticated routes should not be able to select an arbitrary tenant just by sending a header.

Actual:

The tenant ID comes from `Request.Headers["cId"]` when the user is not authenticated.

Evidence:

- `CurrentContext.CompanyId(...)` falls back to the request header for anonymous requests.
- `AntiforgeryMiddleware` writes that value into `HttpContext.Items["CompanyId"]`.
- `MongoRepository.GetCompanyId()` reads from `HttpContext.Items["CompanyId"]`.

Impact:

Potential tenant data exposure or incorrect tenant routing on public / unauthenticated flows.

### BUG-02 — Duplicate claims can crash context resolution instead of failing closed

Severity: Medium  
Area: Auth / request context  
File:

- `Codeji.CMS.Utility\middlewares\CurrentContext.cs`

Problem:

`UserId`, `UserRoleId`, `CompanyId` and `AdminUserId` all use `SingleOrDefault()` over claims with the same type.

Why this is a bug:

- A malformed principal with duplicate claims throws `InvalidOperationException`.
- That turns a token-validation/data-quality problem into an unhandled 500-style failure path instead of a controlled auth rejection.

Expected:

Duplicate or malformed claims should be rejected cleanly and consistently.

Actual:

The helper throws when more than one matching claim exists.

Impact:

- Fragile auth flow
- Possible denial-of-service style failure on malformed or tampered identities
- Harder troubleshooting because the failure happens deep in request processing

### BUG-03 — Mail template rendering is raw string replacement with no HTML escaping

Severity: High  
Area: Notifications / security  
Files:

- `Codeji.CMS.Utility\Helpers\HtmlTemplate.cs`
- Any service that passes user-controlled values into templates

Problem:

`HtmlTemplate.Render(...)` performs direct `string.Replace(...)` on template tokens and inserts values without HTML encoding or output sanitization.

Why this is a bug:

- If any template variable contains user input, the rendered output can include active HTML or script content.
- That is especially risky for public-career / applicant flows where names, emails, phone numbers, or free-text fields may originate from external users.

Expected:

Untrusted values should be encoded before they are inserted into HTML output.

Actual:

The renderer inserts raw values directly into HTML.

Impact:

- HTML injection risk
- Potential XSS in mail previews, HTML bodies, or any HTML rendering path that reuses the helper

### BUG-04 — Template placeholders are expanded recursively across values

Severity: Medium  
Area: Notifications / templating  
File:

- `Codeji.CMS.Utility\Helpers\HtmlTemplate.cs`

Problem:

The renderer loops over properties sequentially and keeps mutating the same output string. If one value contains another placeholder token, the later loop pass will expand it too.

Why this is a bug:

- Template output becomes dependent on property ordering.
- A value that should be treated as plain text can accidentally become another dynamic field.

Expected:

Values should be treated as literal text, not as additional template syntax.

Actual:

Placeholder text found inside a value gets re-interpreted on the next replacement pass.

Impact:

- Data leakage across fields
- Unstable / surprising template output
- Hard-to-debug notification content

### BUG-05 — Sanitizer silently swallows exceptions

Severity: Medium  
Area: Cross-cutting sanitization  
File:

- `Codeji.CMS.Utility\Sanitizer.cs`

Problem:

`SanitizeProperties(...)` catches all exceptions and ignores them.

Why this is a bug:

- If sanitization fails for any reason, the app continues as though the input was processed.
- That makes security failures invisible.

Expected:

Sanitization failures should be logged or surfaced so the caller can reject the input.

Actual:

Exceptions are swallowed.

Impact:

- Silent security bypass risk
- Debugging blind spot
- User content may be persisted unsanitized if a failure occurs

## Non-confirmed issue

### Failed test: `Sanitizer_UnescapesAmpersandsAndIsNotIdempotent`

I did not file this as a confirmed bug.

Reason:

- The failing assertion claims the sanitizer should transform `&lt;b&gt;not bold&lt;/b&gt;` into live `<b>` HTML after repeated passes.
- Source inspection does not support that behavior as a reliable defect.
- The sanitizer test appears to be asserting an interpretation that the current implementation does not actually exhibit.

## Risk note from package scan during test run

The test run also emitted NuGet vulnerability warnings for:

- `AngleSharp` 0.17.1
- `SharpCompress` 0.30.1
- `Snappier` 1.0.0

I did not treat these as code bugs in this report because they are dependency-risk items, not logic or flow defects. They should still be reviewed separately.

## Recommended next validation pass

If you want me to continue, the next safest step is a targeted read-only audit of:

- public career / applicant flows
- auth and tenant isolation paths
- mail rendering and sanitization call sites

That would let me turn more of the above into endpoint-level repro steps without touching the database or changing any feature behavior.

