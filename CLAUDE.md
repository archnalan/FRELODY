# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working Approach

- **Confidence before action** — Do not make any changes until you have 95% confidence in what you need to build. Ask follow-up questions until you reach that confidence.
- **Subagents for exploration** — Use subagents for any exploration or research. If a task needs 3+ files or multi-file analysis, spawn a subagent and return only summarized insights. This offloads decision-making from your prompts to your config.

## Solution Structure

FRELODY is a .NET 10 music management platform (chord charts, lyrics, song extraction, AI parsing). The solution file is `FRELODYAPP.sln` with these projects:

| Project | Type | Purpose |
|---|---|---|
| `FRELODYAPIs` | ASP.NET Core Web API | Primary backend — controllers, auth, payments, AI, OCR |
| `FRELODYUI.Web` | Blazor Web App (server host) | Server-side rendering + WebAssembly host |
| `FRELODYUI.Web.Client` | Blazor WebAssembly | Browser-interactive components |
| `FRELODYUI.Shared` | Razor Class Library | Shared UI components + Refit API interfaces |
| `FRELODYLIB` | Class Library | EF Core models, `SongDbContext`, migrations, services |
| `FRELODYSHRD` | Class Library | DTOs, constants, shared models |
| `FRELODYUI` | .NET MAUI | Cross-platform desktop/mobile app |

## Build & Run

```powershell
# Restore + build
dotnet restore FRELODYAPP.sln
dotnet build FRELODYAPP.sln -c Debug

# Run API locally (requires SQL Server)
cd FRELODYAPIs && dotnet run

# Run full stack via Docker Compose
docker compose up -d           # start all services
docker compose up -d --build   # rebuild after code changes
docker compose down            # stop
docker compose logs -f frelody-api
```

Docker services: SQL Server (1433), API (8080), Web UI (5080), Nginx (80), Maildev SMTP (1025) + UI (5000), Jaeger (16686), OTel Collector (4318).

## Database Migrations

Run from `FRELODYLIB/`:
```powershell
dotnet ef migrations add MigrationName --startup-project ../FRELODYAPIs
dotnet ef database update --startup-project ../FRELODYAPIs
```

Migrations auto-apply on app startup via `context.Database.Migrate()`.

## Architecture

### Request Flow
```
Browser → Nginx (80) → /api/* → FRELODYAPIs (8080)
                      → /     → FRELODYUI.Web (5080)
```

- Server-side Blazor calls API via internal Docker hostname `http://frelody-api:8080`
- WASM client calls API via public host address (configured in `ApiSettings:BaseUrl`)
- Both use Refit clients with `AuthHeaderHandler` injecting JWT tokens

### Auth & Authorization
- JWT Bearer (7-day access) + refresh tokens (30-day)
- Custom org-tier RBAC: `OrgRolePolicyProvider` + `OrgRoleAuthorizationHandler`
- Google OAuth for external login
- Email OTP for secondary verification
- SuperAdmin seeded from `SUPERADMIN_SEED_EMAIL` env var

### Refit API Pattern
Interfaces live in `FRELODYUI.Shared` → registered in both `FRELODYUI.Web/Program.cs` and `FRELODYUI.Web.Client/Program.cs` → consumed in Razor components via `@inject`.

### Song Extraction Pipeline
1. **Web scraping** — bradwarden.com, worshiptogether.com, ultimate-guitar.com (server-side, single dispatch handler)
2. **OCR** — Tesseract 5 + SkiaSharp (image → text)
3. **AI parsing** — Nvidia/DeepSeek APIs for chord/lyric structure extraction

### Observability
- OpenTelemetry: AspNetCore + HTTP + EF Core + Runtime instrumentations
- Dev Docker → OTel Collector → Jaeger UI at `http://localhost:16686`
- Excludes `/health`, `/metrics`, `/_` from tracing

### Email
- Dev: `DevEmailService` logs to console (set `EmailSettings:UseDevEmail=true`)
- Dev Docker: Maildev catches all SMTP on localhost:1025; view at `http://localhost:5000`
- Prod: `SmtpSenderService` with configured SMTP credentials

### Mapping
Mapster handles DTO ↔ Entity transformations; global rules registered in `MappingConfig.RegisterMappings()` from the `Profiles/` folder.

## Key Configuration

**Local dev** — `appsettings.Development.json` or user secrets  
**Docker/prod** — `.env` file or environment variables:

| Variable | Purpose |
|---|---|
| `SA_PASSWORD` | SQL Server SA password |
| `JWT_KEY` | JWT signing key |
| `GOOGLE_CLIENT_ID/SECRET/REDIRECT_URI` | OAuth |
| `PESAPAL_CONSUMER_KEY/SECRET` | Payment gateway |
| `NVIDIA_API_KEY`, `DEEPSEEK_API_KEY` | AI APIs |
| `SMTP_HOST/PORT/EMAIL/PASSWORD` | Email |
| `OTEL_ENDPOINT`, `OTEL_HEADERS` | Observability |
| `SERVER_HOST` | Public domain for CORS and share-link URLs |

## Adding a New Feature

**New API endpoint:**
1. Add `IMyFeatureApi` Refit interface in `FRELODYUI.Shared/RefitApis/`
2. Implement controller in `FRELODYAPIs/Controllers/`
3. Add service logic in `FRELODYLIB/Services/` or `FRELODYAPIs/Services/`
4. Register Refit client in both `FRELODYUI.Web/Program.cs` and `FRELODYUI.Web.Client/Program.cs`

**New database entity:**
1. Add model in `FRELODYLIB/Models/` (inherit `BaseEntity` for Id/timestamps)
2. Configure relationships in `SongDbContext.OnModelCreating()`
3. Create migration from `FRELODYLIB/`

## Styling Conventions

- **UI design rule** — any UI work (new components, refactors, polish passes) must be **modern, mobile-first, minimalist, intuitive, crisp, and theme-responsive**. Design layouts at the small-viewport breakpoint first and progressively enhance for desktop. Prefer the `--k-*` admin/dashboard token layer in `FRELODYUI.Shared/wwwroot/app.css` (radii, shadows, surfaces, accent-soft/success-soft/danger-soft overlays) for new surfaces — they already cover both `[data-bs-theme]` modes.
- **Token consolidation rule** — repeated design literals that span more than one component (spacing/gaps, radii, font sizes, transition durations, shimmer/loading motions, status-chip tints, aspect ratios for music surfaces) **must** be expressed via tokens in `FRELODYUI.Shared/wwwroot/app.css`, not hard-coded in scoped `.razor.css` files. The current vocabulary covers it: `--k-space-1…6` (4-pt rhythm), `--k-radius-xs/sm/md/lg/pill`, `--k-shadow-sm/-/hover/lg`, `--k-transition-fast/-/slow`, `--k-aspect-chord`, `--k-font-size-xs/sm`, `--k-skill-easy/medium/advanced/expert-{bg,text}`, plus the shared `@keyframes k-shimmer` and `.k-shimmer` helper. If a value is genuinely one-off (`0.3rem` carousel indicator gap, sub-rem nav offsets, per-component nuance), keep it local — token sprawl is worse than a clean literal. When in doubt, search for the literal across `Pages/**/*.razor.css`; if it appears 3+ times in unrelated components, lift it.
- **Adding a new `--k-*` token** — add it to **both** the `:root, [data-bs-theme="light"]` block and (if it needs a different value in dark mode) the `[data-bs-theme="dark"]` block. Spacing/radii/motion are theme-agnostic and only need the light block. Always namespace new tokens with `--k-` to keep them inside the harmonized system; never extend the legacy globals (`--text-primary`, `--input-bg`, …) for new work.
- **Placeholder skeletons** — `isLoading` markup must live in its own component under `Pages/PlaceHolders/` (or a sibling `*Placeholder.razor` next to the page) using Bootstrap's `placeholder-glow` structure. Don't inline placeholder markup in the page itself, and don't hard-code `background-color` — let the `.placeholder` class pick up theme colors.
- **Razor + scoped CSS colocation** — every `Foo.razor` component (page or shared) should be paired with its own `Foo.razor.css` scoped stylesheet sitting in the same folder. Never put a component's styles in a global stylesheet or in another component's `.razor.css`. Blazor's CSS isolation pipeline picks up the sibling file automatically.
- **Theme tokens** — use the FRELODY CSS variables defined in `FRELODYUI.Shared/wwwroot/app.css` (`--text-primary`, `--text-secondary`, `--input-bg`, `--input-border`, `--modal-content-bg`, `--bg-hover`, `--bs-primary`, …). The app theme is driven by `[data-bs-theme="light|dark"]` on a parent element — **don't** branch on `@media (prefers-color-scheme)` because it ignores the user's in-app theme toggle.
- **Refit multipart endpoints** — only call a `[Multipart]` Refit method when you actually have at least one `StreamPart`. Passing all-null parts produces an invalid `Content-Disposition` and ASP.NET Core's form parser will reject the request. Have a JSON-body fallback endpoint for the no-file case.
- **Razor and SVG `<text>`** — Razor reserves the `<text>` tag as a code/markup-mode escape, so it can't carry attributes inside Razor templates. To emit an SVG `<text>` element, use `@((MarkupString)$"<text …>{content}</text>")` and HTML-encode the content. Wrap a `<g @onclick="…">` around it for click handlers.

## Docker Maintenance & Troubleshooting

- **502 Bad Gateway after container changes** — Adding new services or modifying the network can cause Docker to reassign internal IP addresses. Nginx resolves these hostnames at startup/reload and may hold stale IPs, leading to 502 errors. Fix by reloading Nginx:
  ```bash
  docker exec frelody-gateway nginx -s reload
  ```
- **IP Address Shifts** — Nginx (open source) does not automatically re-resolve upstream hostnames unless variables are used in `proxy_pass`. Since `frelody-gateway` is often long-running, always reload it after any `docker-compose.yml` modifications that might shift the IP pool.
- **AI Analysis Timeouts** — AI processing (chord recognition, YouTube extraction) can take several minutes. Ensure timeouts are aligned across the stack:
    - **Nginx**: `proxy_read_timeout 600s` in `location /api/`.
    - **ASP.NET Core**: `HttpClient` timeout set to `TimeSpan.FromMinutes(10)`.
    - **Python (Gunicorn)**: `--timeout 600`.
- **Application Error Mapping** — Do not use 502/503 for expected application-level failures (e.g., YouTube "bot wall"). Use **422 Unprocessable Entity** with a JSON error body. This prevents Nginx/Cloudflare from intercepting the response with a generic "Bad Gateway" HTML page, allowing the Blazor UI to show the friendly error message to the user.
- **YouTube "Sign in to confirm you're not a bot"** — Caused by **IP reputation**, not config. The prod box exits through a Contabo datacenter IP, which YouTube bot-walls *even with a valid bgutil PO token*; a residential IP (e.g. local dev) is trusted, which is why it works locally. PO tokens are necessary but **not sufficient** on datacenter IPs ([yt-dlp #15865](https://github.com/yt-dlp/yt-dlp/issues/15865)). Escalation path (already wired): logged-in **cookies**.
    - Export a **throwaway** YouTube account's cookies in Netscape format (Firefox export is the only reliably-working method in 2026; Chrome 127+ app-bound encryption breaks the others). Save as `docker/cookies/youtube.txt` (git-ignored — it's account credentials).
    - `docker compose up -d chordmini-backend` to remount; verify via `docker exec frelody-chordmini curl -s localhost:8081/api/ytdlp/health` → `"cookies": true`.
    - The sidecar (`docker/ytdlp_server.py`) copies the jar to a private temp file **per request** — yt-dlp rewrites the cookie jar on exit and concurrent workers sharing one file corrupts it ([#12045](https://github.com/yt-dlp/yt-dlp/issues/12045)), so the mount is `:ro` and the source stays pristine.
    - Cookies expire/get rotated; refresh `youtube.txt` when the bot-wall returns. Keep request rate low to reduce ban risk on the cookie account. Alternative for scale: a residential `--proxy`.

## Notable Behaviors

- **Data Protection keys** — persisted to `/app/dp-keys` Docker volume; without this, auth cookies break on container restart
- **Share links** — `/shared/{token}` serves crawler-friendly HTML; `/share-og/{token}.png` serves Open Graph card images
- **HTTPS in Docker** — disabled (`ASPNETCORE_HTTPS_PORT=""`); Nginx terminates TLS
- **CI/CD versioning** — commit prefix determines semver bump: `fix:`/`bugfix:` → patch, `feat:`/`feature:` → minor, `breaking:` → major
- **API docs** — Scalar UI available at `/scalar` (dev only)
- **No test projects** in this solution
