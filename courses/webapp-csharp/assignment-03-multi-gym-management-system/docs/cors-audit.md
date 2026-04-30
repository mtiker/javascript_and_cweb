# CORS Audit

## Policy

Single policy named `ClientApp` registered via `AddAppCors` in
`WebApp/Setup/WebApiExtensions.cs`. Applied in the middleware pipeline between
`UseRouting` and `UseAuthentication`.

```
AllowedOrigins  – see below
AllowAnyHeader  – yes
AllowAnyMethod  – yes
AllowCredentials – no (not called, cookies not used)
```

## Origin resolution

### Development environment

Falls back to `["http://localhost:5173", "https://localhost:5173", "http://127.0.0.1:5173"]`
if `Cors:AllowedOrigins` is not set. Override via config when a non-default Vite
port is needed.

### Production / non-Development environments

`Cors:AllowedOrigins` **must** be configured. `ValidateProductionCorsOrigins` enforces:

1. At least one origin is present – else `InvalidOperationException`.
2. No wildcard (`*`) in any origin – else `InvalidOperationException`.
3. Each entry must be an absolute URI without path, query, or fragment
   (`https://example.com` is valid; `https://example.com/path` is not).
4. `localhost`, `*.localhost`, or a loopback IP (`127.0.0.1`, `::1`, etc.) is
   rejected – else `InvalidOperationException`.

Validation runs at startup (`AddAppCors` is called in `Program.cs`), so a
misconfigured production deployment fails fast before serving any requests.

## Test coverage

| Requirement | Test |
|---|---|
| Missing origins in production → startup failure | `RuntimeConfigurationTests.AddAppCors_ProductionWithoutConfiguredOrigins_FailsFast` |
| Wildcard origin rejected | `RuntimeConfigurationTests.AddAppCors_ProductionRejectsUnsafeOrigins("https://*.example.com")` |
| localhost rejected | `RuntimeConfigurationTests.AddAppCors_ProductionRejectsUnsafeOrigins("http://localhost:5173")` |
| Loopback IP (127.0.0.1) rejected | `RuntimeConfigurationTests.AddAppCors_ProductionRejectsUnsafeOrigins("http://127.0.0.1:5173")` |
| Origin with path rejected | `RuntimeConfigurationTests.AddAppCors_ProductionRejectsUnsafeOrigins("https://example.com/path")` |

## Notes

- `AllowAnyMethod` is broader than strictly necessary but does not weaken security
  for a JSON API that relies on JWT auth rather than cookies.
- The test factory seeds `Cors:AllowedOrigins:0 = https://tests.multi-gym.local`
  so integration tests run under the development environment with an explicit
  (safe) origin rather than the default localhost list.
