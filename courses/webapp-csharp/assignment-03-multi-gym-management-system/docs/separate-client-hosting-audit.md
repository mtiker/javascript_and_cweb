# Separate Client Hosting Audit

**Audited:** 2026-04-28

This audit documents what stands between the current "embedded React" deployment
(client copied into `WebApp/wwwroot/client`) and a deployment where the client
runs on its own web server / container / domain, while still consuming the
existing backend API.

---

## 1. Current state

The React client is embedded in the ASP.NET Core image:

- `Dockerfile` stage 1 builds `client/` with Node 20.
- Stage 3 copies `client/dist` into `wwwroot/client`.
- ASP.NET Core serves it via `app.UseStaticFiles()` and `MapFallbackToFile("/client/{*path:nonfile}", "client/index.html")` (see `src/WebApp/Setup/MiddlewareExtensions.cs:44-47`).
- Vite is configured with `base: "/client/"` (see `client/vite.config.ts`).
- Public URL: `https://mtiker-cweb-4.proxy.itcollege.ee/client`.

Because backend and client share an origin in production, no CORS preflight is
ever triggered against the client UI. `Cors:AllowedOrigins` is set but only
matters when the client is hosted elsewhere (or for local Vite at
`http://localhost:5173`).

---

## 2. What the client assumes about hosting

| Assumption | Current behavior | Impact for separate hosting |
|---|---|---|
| `base: "/client/"` in Vite | All asset URLs are emitted as `/client/...` | Either keep `/client/` path on the new host, or change `base` to `/`. Pick **one** and keep it consistent with nginx `location` config below. |
| `BrowserRouter` (no `basename`) at `client/src/App.tsx:65` | React Router treats `/login`, `/members` etc. as root paths | If the client is served under a sub-path (`/client/`), nginx must serve `index.html` for that whole prefix, OR `BrowserRouter` must be given `basename="/client"`. Today this works only because ASP.NET Core has `MapFallbackToFile("/client/{*path:nonfile}", ...)`. |
| API base URL at `client/src/lib/auth.tsx:23-29` | Reads `VITE_API_BASE_URL`, otherwise `window.location.origin` in production, otherwise `https://localhost:7245` | Already separation-ready. A separate-host build only needs `VITE_API_BASE_URL` set at build time. |
| JWT in `localStorage`, sent as `Authorization: Bearer ...` (`apiClient.ts:454`) | No cookies | Cross-origin works without `AllowCredentials`. CORS only needs `Origin`, `Authorization`, `Content-Type`, `Accept-Language`. |
| Refresh token via JSON POST (`apiClient.ts:494`) | Same | Same — no cookie, no `withCredentials` needed. |

Conclusion: the client itself is already cross-origin-safe. The blockers are
build-time path config and the runtime environment variable for the API URL.

---

## 3. What the backend assumes about the client

- `MapFallbackToFile("/client/{*path:nonfile}", "client/index.html")` and
  `MapGet("/client", ...)` keep the embedded fallback. These do **not** need to
  be removed — they remain useful as a fallback rendering path for users who
  hit the backend domain directly. They can be guarded behind the presence of
  `wwwroot/client/index.html` in the future, but for now removing them is not
  required.
- CORS policy `ClientApp` is permissive on headers and methods (see `src/WebApp/Setup/WebApiExtensions.cs:58-83`). `AllowCredentials` is **not** set — that matches the JWT-in-header design.
- `ValidateProductionCorsOrigins` rejects wildcard, loopback, and origins with
  paths. Multiple comma-separated origins are already supported because the
  reader is `IConfiguration.GetSection("Cors:AllowedOrigins").Get<string[]>()`.

To add a separate client domain, only the **production environment value of
`Cors:AllowedOrigins`** needs to grow. No backend code change is required for
multi-origin support; only documentation and deployment glue.

---

## 4. New components introduced

| Path | Purpose |
|---|---|
| `client/Dockerfile` | Multi-stage Node build → nginx static server |
| `client/nginx.conf` | SPA fallback, gzip, security headers, static cache |
| `client/.env.example` | Documents `VITE_API_BASE_URL` |
| `client/.dockerignore` | Excludes `node_modules`, `dist`, tests from build context |
| `docker-compose.prod.yml` `client` service (optional) | Local end-to-end production simulation with a separate client container |
| `.gitlab-ci.yml` jobs `assignment03_client_*` | Install / test / build / package / deploy the client image |

The client container exposes port `8080` internally (matching backend) and is
expected to be reverse-proxied at a separate hostname (e.g.
`https://mtiker-cweb-4-client.proxy.itcollege.ee`).

---

## 5. Open questions / decisions

1. **Path strategy:** keep Vite `base: "/client/"` (so the same artifact works
   when copied into `wwwroot/client`) or move to root (`base: "/"` for
   dedicated host). Decision: **keep `/client/`** for now — it preserves the
   existing fallback embedded into the backend image. nginx is configured with
   a matching `location /client/` rewrite.
2. **TLS termination:** done by the IT College reverse proxy; the nginx
   container speaks plain HTTP on `:8080`. Same pattern as the backend.
3. **Backwards compatibility:** the embedded `/client` route on the backend
   image stays. We are not removing it in this phase — see deployment doc.
4. **Cookies / credentials:** still none. CORS does not need `AllowCredentials`.

---

## 6. Smoke check after split deployment

```bash
# Client container
curl -I https://<client-host>/client/                  # 200 + text/html
curl -I https://<client-host>/client/assets/index.js   # 200 + application/javascript

# Backend reachable from client browser
# (run in browser console at https://<client-host>/client/)
fetch("https://<api-host>/health").then(r => r.status)

# CORS preflight
curl -i -X OPTIONS https://<api-host>/api/v1/account/login \
  -H "Origin: https://<client-host>" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type, accept-language"
# Expect: 204 + Access-Control-Allow-Origin: https://<client-host>
```
