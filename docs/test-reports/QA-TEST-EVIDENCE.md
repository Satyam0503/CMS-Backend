# Manual QA evidence log

## Pre-run evidence

- Database host resolved without disclosing credentials: `192.168.0.172`
- Database name: `codejiCmsv2`
- Authorized data boundary: one new QA Testing Company only
- Existing launch profiles: Development
- API local listener at port 5036: not present

## Launch evidence

Attempted command (local-only listener):

```powershell
dotnet run --no-launch-profile --urls http://127.0.0.1:5036
```

Observed result: build failed after repeated `MSB3026` retries and `MSB3027`/`MSB3021` errors because existing processes held API output DLLs. No HTTP request, browser session, YOPmail address, synthetic user, or database mutation was made.

## Evidence limitations

There are no screenshots, network captures, API responses, test account IDs, or database before/after records because test execution did not begin.
