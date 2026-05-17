# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

- **Razor + scoped CSS colocation** — every `Foo.razor` component (page or shared) should be paired with its own `Foo.razor.css` scoped stylesheet sitting in the same folder. Never put a component's styles in a global stylesheet or in another component's `.razor.css`. Blazor's CSS isolation pipeline picks up the sibling file automatically.
- **Theme tokens** — use the FRELODY CSS variables defined in `FRELODYUI.Shared/wwwroot/app.css` (`--text-primary`, `--text-secondary`, `--input-bg`, `--input-border`, `--modal-content-bg`, `--bg-hover`, `--bs-primary`, …). The app theme is driven by `[data-bs-theme="light|dark"]` on a parent element — **don't** branch on `@media (prefers-color-scheme)` because it ignores the user's in-app theme toggle.
- **Refit multipart endpoints** — only call a `[Multipart]` Refit method when you actually have at least one `StreamPart`. Passing all-null parts produces an invalid `Content-Disposition` and ASP.NET Core's form parser will reject the request. Have a JSON-body fallback endpoint for the no-file case.
- **Razor and SVG `<text>`** — Razor reserves the `<text>` tag as a code/markup-mode escape, so it can't carry attributes inside Razor templates. To emit an SVG `<text>` element, use `@((MarkupString)$"<text …>{content}</text>")` and HTML-encode the content. Wrap a `<g @onclick="…">` around it for click handlers.

## Notable Behaviors

- **Data Protection keys** — persisted to `/app/dp-keys` Docker volume; without this, auth cookies break on container restart
- **Share links** — `/shared/{token}` serves crawler-friendly HTML; `/share-og/{token}.png` serves Open Graph card images
- **HTTPS in Docker** — disabled (`ASPNETCORE_HTTPS_PORT=""`); Nginx terminates TLS
- **CI/CD versioning** — commit prefix determines semver bump: `fix:`/`bugfix:` → patch, `feat:`/`feature:` → minor, `breaking:` → major
- **API docs** — Scalar UI available at `/scalar` (dev only)
- **No test projects** in this solution
