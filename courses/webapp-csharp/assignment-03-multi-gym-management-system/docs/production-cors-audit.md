# Production CORS Audit (Phase 8 — separate client origin)

**Audited:** 2026-04-28

This audit complements `cors-audit.md` and focuses on what production CORS
must look like once the React client runs on a different origin from the API.

---

## 1. Current production CORS state

`AddAppCors` (`src/WebApp/Setup/WebApiExtensions.cs:58-83`) reads
`Cors:AllowedOrigins` as a `string[]` and enforces, in non-Development:

- at least one origin
- no `*`
- absolute URI with no path/query/fragment
- not `localhost`, `*.localhost`, or a loopback IP

Policy: `AllowAnyHeader`, `AllowAnyMethod`, **no** `AllowCredentials`.
Applied between `UseRouting` and `UseAuthentication`.

`docker-compose.prod.yml` currently passes a single origin:

```yaml
Cors__AllowedOrigins__0: ${CORS_ALLOWED_ORIGIN:-https://mtiker-cweb-4.proxy.itcollege.ee}
```

Because Compose passes the env var as a single index, the existing layout
already supports a list — we just need to widen it.

---

## 2. What changes for Phase 8

| Item | Change | Why |
|---|---|---|
| `CORS_ALLOWED_ORIGIN` | Becomes a **comma-separated list** in deployment glue | One origin per host: backend public origin (legacy `/client`) + new client origin |
| `docker-compose.prod.yml` | Splits `CORS_ALLOWED_ORIGIN` on commas into `Cors__AllowedOrigins__0..N` indexed env vars | ASP.NET Core configuration array binding requires distinct indexed keys; we cannot use a comma-separated single value |
| `WebApiExtensions.cs` | No code change | Validation already iterates the configured array |
| `RuntimeConfigurationTests` | No code change | Existing test cases already cover multi-origin via `Cors:AllowedOrigins:0..N` |

The simplest way to support a list while keeping a single secret variable is to
have `docker-compose.prod.yml` use an entrypoint shim, but that adds moving
parts. We instead expose two named variables and let `appsettings`
configuration handle the array:

```yaml
Cors__AllowedOrigins__0: ${CORS_ALLOWED_ORIGIN:-https://mtiker-cweb-4.proxy.itcollege.ee}
Cors__AllowedOrigins__1: ${CORS_ALLOWED_ORIGIN_CLIENT:-}
```

If `CORS_ALLOWED_ORIGIN_CLIENT` is empty, ASP.NET Core ignores the empty entry
because `WebApiExtensions.cs:60-64` already filters `IsNullOrWhiteSpace`. So
operators who do not run a separate client host can leave it unset and the
existing single-origin behavior is preserved.

For three or more origins (e.g. preview environments, multiple client builds),
add `Cors__AllowedOrigins__2`, `__3`, … via GitLab CI/CD variables — no code
change needed.

---

## 3. Headers and methods

`AllowAnyHeader` and `AllowAnyMethod` already cover what the client sends:

- `Authorization: Bearer <jwt>` (auth)
- `Content-Type: application/json` (POST / PUT / DELETE bodies)
- `Accept-Language: et-EE | en` (localization)

No `Access-Control-Expose-Headers` is required — the client only reads the
JSON response body and `WWW-Authenticate` (which the browser surfaces
automatically for `fetch`).

`AllowCredentials` stays **off**. JWTs travel in the `Authorization` header,
not cookies, so the cross-origin call does not carry credentials and the
browser does not require `Access-Control-Allow-Credentials: true`.

---

## 4. Reverse proxy considerations

The IT College proxy already terminates TLS in front of the backend at
`https://mtiker-cweb-4.proxy.itcollege.ee`. For the client there will be a
second proxy entry at e.g. `https://mtiker-cweb-4-client.proxy.itcollege.ee`.

Notes:

- `UseForwardedHeaders` is wired up (`AddAppForwardedHeaders` +
  `app.UseForwardedHeaders()`), so `Origin` arriving from the client is
  preserved end to end.
- The backend currently runs on host port `83`; the client container is added
  with `CLIENT_PORT` (default `8081`) so they can coexist behind separate
  proxy hostnames.
- HSTS is enabled in production (`UseHsts`). Both hosts must serve HTTPS via
  the upstream proxy, otherwise the browser will block the cross-origin call.

---

## 5. Smoke checks

```bash
# 1. Preflight succeeds and returns the client origin
curl -i -X OPTIONS https://<api-host>/api/v1/account/login \
  -H "Origin: https://<client-host>" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type, authorization, accept-language"
# Expect 204
# Expect Access-Control-Allow-Origin: https://<client-host>
# Expect Access-Control-Allow-Methods to include POST

# 2. Real login fetch from the client (run in browser console at https://<client-host>)
fetch("https://<api-host>/api/v1/account/login", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email: "admin@peakforge.local", password: "GymStrong123!" })
}).then(r => r.json())

# 3. An unlisted origin is rejected
curl -i -X OPTIONS https://<api-host>/api/v1/account/login \
  -H "Origin: https://evil.example.com" \
  -H "Access-Control-Request-Method: POST"
# Expect: no Access-Control-Allow-Origin header, fetch from evil origin will fail
```

---

## 6. Failure modes the validator already catches

| Misconfiguration | Outcome at startup |
|---|---|
| `CORS_ALLOWED_ORIGIN` and `CORS_ALLOWED_ORIGIN_CLIENT` both empty in production | `InvalidOperationException: Cors:AllowedOrigins must be configured outside Development.` (process exits before serving requests) |
| Client origin set to `https://*.proxy.itcollege.ee` | Wildcard rejected at startup |
| Client origin accidentally set to `http://localhost:8081` | Loopback rejected at startup |
| Client origin includes path, e.g. `https://client-host/client` | Rejected — must be authority only |

These are exactly the cases asserted in
`RuntimeConfigurationTests.AddAppCors_ProductionRejectsUnsafeOrigins`.
