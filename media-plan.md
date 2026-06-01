# Implementation Plan: SuperAdmin Docs Media Manager

> Session-agnostic working plan. A SuperAdmin-only page in **FRELODY.Docs** where media
> (screenshots + YouTube videos) is uploaded/linked and **auto-published** to the matching
> documentation sections — no commit, no redeploy.

## Decisions locked
- **Persistence:** Live, API-backed. Files + a `manifest.json` stored on the existing
  **`frelody_media`** Docker volume (`/app/media/docs-media`). No EF entity, no migration,
  **no docker-compose change**, no CORS change (API CORS is already `AllowAll`).
- **Slot model:** Curated registry. One stable slot per existing placeholder, each carrying a
  human "what goes here" context string (harvested from the placeholder `aria-label`/iframe `title`).
- **YouTube field:** accepts a full URL *or* a bare 11-char id; server extracts the id.

## Key facts (verified)
- Docs site is **static nginx** (`Dockerfile.docs`) → cannot accept writes; writes go to the **API**.
- API storage idiom: `IWebHostEnvironment` → write file → serve via `UseStaticFiles`
  (`OgCardService.cs:25`, `Program.cs:476`).
- API CORS = `AllowAll` (`Program.cs:282`); auth is Bearer-header → `AllowAnyOrigin` compatible.
- SuperAdmin gate: `[Authorize(Roles = UserRoles.SuperAdmin)]` on controller + `[AllowAnonymous]`
  on the public GET (mirrors `ProductsController.cs:14`).
- Persistent volume already present: `frelody_media:/app/media` (`docker-compose.yml:113,247`).
  `wwwroot/share-og` is NOT on a volume (regenerable). Uploaded docs media is NOT regenerable →
  must live on `/app/media`.
- Docs `appsettings.json` already has `Api:BaseUrl` stubbed; only `Web:BaseUrl` is wired in `Program.cs`.
- Corpus: ~45 image slots + ~5 video slots across `FRELODY.Docs/wwwroot/content/**`.

## Architecture
```
docs.frelody.com/admin/media (SuperAdmin, holds bridged JWT)
   | multipart POST image / PUT {videoId,caption}   [Bearer token]
   v
FRELODYAPIs DocMediaController -> /app/media/docs-media/<slot>.<ext>  (frelody_media volume)
                              `-> /app/media/docs-media/manifest.json
   ^ GET manifest (anonymous)         served via PhysicalFileProvider at <Api>/docs-media/*
   |
docs DocumentService -> injects <img>/<iframe> into <figure data-media-slot="..."> before MarkupString
```

## Phases & status
- [x] **Phase 1 — DTOs** (`FRELODYSHRD/Dtos/DocsDtos/DocMediaDto.cs`): `DocMediaEntryDto`,
      `DocMediaManifestDto`, `DocMediaTextUpdateDto`. **Do NOT add `FRELODYSHRD` ProjectReference to
      `FRELODY.Docs.csproj`** — FRELODYSHRD pulls itext7/OpenXml/PdfPig (heavy, WASM-unfit). The docs
      site defines its own tiny local manifest model (mirrors the decoupled `AuthService` approach).
- [x] **Phase 2 — API storage service + controller** ✅ builds clean. Files: `Services/DocsMedia/IDocMediaService.cs`,
      `Services/DocsMedia/DocMediaService.cs` (registered **Singleton** in Program.cs:113), `Controllers/DocMediaController.cs`,
      Program.cs static `/docs-media` mapping after `UseStaticFiles()`. Slot validated by regex (not registry list) since
      endpoint is SuperAdmin-only; format `^[a-z0-9]+(?:-+[a-z0-9]+)*$` blocks traversal.
  - `FRELODYAPIs/Services/DocsMedia/IDocMediaService.cs` + `DocMediaService.cs`
    - Root = `Path.Combine(ContentRootPath, cfg "DocsMedia:Root" ?? "media/docs-media")` → `/app/media/docs-media` in Docker.
    - `GetManifestAsync`, `SaveImageAsync(slot, IFormFile)`, `SetTextAsync(slot, dto)`, `ClearAsync(slot, kind)`.
    - Validate slot against registry allow-list; content-type in {png,jpeg,webp}; size <= ~5 MB.
    - Manifest read-modify-write guarded by `SemaphoreSlim`; write-temp-then-rename.
    - YouTube id extraction regex (watch?v=, youtu.be/, /embed/, bare id).
  - `FRELODYAPIs/Controllers/DocMediaController.cs` — `[Authorize(Roles=SuperAdmin)]`, route `api/[controller]/[action]`:
    - `GET GetManifest` `[AllowAnonymous]`
    - `POST UploadImage([FromForm] string slot, IFormFile file)` `[Consumes multipart]`
    - `PUT SetText([FromQuery] slot, [FromBody] DocMediaTextUpdateDto)`
    - `DELETE Clear([FromQuery] slot, [FromQuery] kind)`
  - `FRELODYAPIs/Program.cs`: register `IDocMediaService`; after `UseStaticFiles()` add a
    `PhysicalFileProvider` mapping `docsMediaRoot` -> `/docs-media` with `Cache-Control: public,max-age=300`;
    `Directory.CreateDirectory(docsMediaRoot)` at startup.
- [x] **Phase 3 — Registry + markdown annotation** ✅ 49 placeholders annotated (46 image + 3 video) with
      `data-media-slot` via scripted pass; `FRELODY.Docs/Services/MediaRegistry.cs` + `Models/MediaSlot.cs`
      generated; `_README.md` writer brief updated to document the slot attribute + `/admin/media` flow.
  - Key scheme: `"<slug-dashes>--<ordinal>"` (e.g. `discover-overview--1`, `compose-overview--vid1`).
  - One-time scripted pass: add `data-media-slot="<key>"` to every `<figure class="img-frame">`
    and `<div class="video-embed">` in `content/**`; harvest `aria-label`/`title` as context.
  - `FRELODY.Docs/Services/MediaRegistry.cs`: static `MediaSlot(Key, PageSlug, PageTitle, Kind, AspectRatio, Context)` list.
- [x] **Phase 4 — Docs API client + manifest fetch + injection** ✅ `Models/DocMediaManifest.cs` (local model,
      no FRELODYSHRD ref), `Services/DocMediaService.cs` (manifest fetch + Bearer-authed writes + cache-busted
      absolute image URLs), `Program.cs` registers it with its own client at `Api:BaseUrl`. `DocumentService`
      caches pre-injection HTML and runs `InjectMedia` per load (only this page's slots; unset → placeholder).
  - `FRELODY.Docs/Program.cs`: named `HttpClient "api"` from `Api:BaseUrl`; register docs-side `DocMediaService`.
  - `DocumentService.cs`: after `Markdown.ToHtml`, `InjectMedia(html)` regex-replaces matching
    `data-media-slot` figures/video blocks with real `<img>`(`?v={updatedAt}`)/`<iframe>`; unset slots keep placeholder.
    Cache key includes manifest version.
- [x] **Phase 5 — SuperAdmin page** ✅ `FRELODY.Docs/Pages/Admin/MediaManager.razor` (+ `.razor.css`) `@page "/admin/media"`.
  Gated on `Auth.IsSuperAdmin` (new helper; narrower than `IsAdmin`). Slots grouped by page, each card has context +
  aspect/kind badges + live-page link, `InputFile` image picker, YouTube URL/id field, caption, clear buttons, per-card
  save state + thumbnails. SuperAdmin-only **Media** link added to `MainLayout` topbar.
- [x] **Phase 6 — Config & deploy** ✅ **ROLLED OUT to prod 2026-06-01**
  - Docs `wwwroot/appsettings.Production.json` created: `Api`/`Web` BaseUrl = `https://frelody.com` (mirrors
    `FRELODYUI.Web.Client` prod; gateway proxies `/api`).
  - `nginx.conf`: added `location /docs-media/` → `frelody-api:8080` on the `frelody.com` server block (mirrors `/share-og/`).
  - API default `DocsMedia:Root = media/docs-media` (on existing `frelody_media` volume).
  - **Rollout executed:** rebuilt+recreated `frelody-api` and `frelody-docs`, then **force-recreated** the `nginx`
    gateway. Force-recreate (not just `nginx -s reload`) was required: `nginx.conf` is bind-mounted and had been
    atomically replaced, so the live container held a stale inode (no `/docs-media/` block); recreate also
    re-resolves the static `frelody-api` upstream IP. CI/CD skipped this deploy because the commit message lacked a
    `feat:`/`fix:` prefix — manual rollout on the box.
  - **Bug found & fixed during rollout:** the docs-side `DocMediaService` called `api/docmedia/*` but the API uses a
    global kebab-case URL transformer, so the real routes are `api/doc-media/get-manifest|upload-image|set-text|clear`.
    All four client URLs corrected; without this the manifest fetch + every admin write 404'd (silent → placeholders
    only). A clean build did not catch it (hardcoded URL strings).
  - **Verified live:** anon `GET /api/doc-media/get-manifest` → 200 `{"slots":{}}`; unauth writes → 401;
    `docs.frelody.com` → 200; fixed URL present in the published WASM bundle.

## Build status
`dotnet build FRELODYAPP.sln -c Debug` → **0 errors** (API, docs, full solution all green). Runtime verification
(checklist below) still needs the live stack (SQL + API + docs) — not run in this environment.

## Risks / resolutions
- Redeploy data loss -> on `frelody_media` volume, not wwwroot.
- Stale manifest -> `max-age=300` + `?v={updatedAt}` cache-bust.
- Unknown/abusive slots -> registry allow-list + type/size limits.
- Manifest write races -> `SemaphoreSlim` + temp-then-rename.
- Owner/Admin != SuperAdmin -> gate on SuperAdmin role string both sides.
- Multipart-with-no-file -> image upload requires `IFormFile`; video/caption is a separate JSON PUT.

## Verify checklist
1. `dotnet build FRELODYAPP.sln -c Debug`.
2. `GET /api/docmedia/getmanifest` -> `{}` anonymously; upload/PUT -> 401/403 without SuperAdmin token.
3. Docs: sign in as SuperAdmin -> `/admin/media` lists ~50 slots -> upload image + set video -> reload
   `/docs/<slug>` shows real `<img>`/`<iframe>`; unset slots show placeholder.
4. PDF export includes image, omits video (existing print rule).

## Open items to confirm
- Production `Api:BaseUrl` for the docs site (gateway path vs `api.` subdomain).
