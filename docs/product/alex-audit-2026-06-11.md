# Alex E2E Audit — 2026-06-11

> Multi-session audit of FRELODY against `ALEX.md` (v0.2). Each session: pick the next open
> item, test live, record PASS/FAIL + evidence, fix, retest.
> Stack under test: prod containers on this box (frelody.com via Cloudflare tunnel → nginx gateway).
> **Run compose with BOTH files** — `docker compose -f docker-compose.yml -f docker-compose.prod.yml …`.
> Bare base-file recreate injects dev SA creds + drifts the network (caused a DB outage this session).

## Status board

| # | Area | Status | Evidence |
|---|------|--------|----------|
| 1 | Anonymous browse + public song play | PASS | gateway `/` 200; frelody.com 200 |
| 1b | `/health` + `/health/ready` endpoints | PASS (new) | liveness 200; readiness pings DB, 503 when down |
| 2 | YouTube analyze + anon→login redirect | PASS* | anon `/api/you-tube/analyze` → **402 `unauthenticated`**; returnUrl now family-preserving. *Fresh analysis still bot-walled (cookies:false) |
| 3 | TikTok analyze + login redirect | PASS (fixed) | anon → **402** (was 502 after a wasted resolve); resolve/analyze failures now **422** not 502 |
| 4 | Daily limit / Today's songs / paywall / payments | PASS (gate) | `limits` → `freeAnalyzedSongsPerDay:2`; `unlock`/`quota-status` anon → 401; 402 payload carries `dailyLimit/usedToday/remaining/reason`. Pesapal sandbox run still pending |
| 5 | Layout/breadcrumb consistency | PASS (fixed) | 2 cross-family redirects to `/discover/*` corrected to stay in landing family |
| 6 | Console logs gated to dev | PASS | WASM `SetMinimumLevel(Warning)` outside Dev; remaining `console.*` are legit `console.warn` on failures |
| 7 | Logs + audit trail accessibility | PASS (documented) | Jaeger + DB tables; per-user admin UI is the top remaining gap (see below) |
| 8 | ALEX.md rewrite | DONE | edits applied for the bot-wall caveat + status-code convention |
| 9 | Architecture/algorithm improvements + verdict | DONE | see Verdict section |

## Findings log

- 2026-06-11: `GET /health` → **404** — endpoint anticipated by the OTel trace filter but never
  mapped, and the API container had **no Docker healthcheck** (only SQL Server did). **FIXED**:
  added `/health` (liveness) + `/health/ready` (DB ping) minimal endpoints; added a busybox-`wget`
  healthcheck to `frelody-api`; made `frelody-web` `depends_on` api `service_healthy`.
- 2026-06-11: **`/health/ready` immediately caught a real DB outage** — after a mis-issued
  recreate the API was "Up (healthy)" on liveness but could not log into SQL Server. The readiness
  probe surfaced it (503) where liveness + container status hid it. (Root cause: bare-base compose
  recreate → dev SA creds + a stranded, never-started `frelody-sqlserver`. Restored with a full
  prod-overlay `up -d`. Lesson recorded in agent memory.)
- 2026-06-11: **Layout-family bugs** — `Discover.razor` deep-link `?v=` redirect and
  `YoutubePlaybackView.SaveToLibraryAsync` login-returnUrl both hardcoded `/discover/{id}`
  (MainLayout) regardless of whether Alex entered via the `/play/*` landing family (LandingLayout).
  **FIXED**: deep-link now honors `IsLanding`; save-login returnUrl now uses the current relative
  URL. The primary paywall/sign-in sheet was already correct (`ReturnUrl="@CurrentRelativeUrl"`).
- 2026-06-11: **TikTok status-code + ordering bug** — `TikTokController` returned **502** for
  app-level extraction failures (region/copyright/bot-wall) in 3 places, contradicting the
  CLAUDE.md rule (use **422** so Cloudflare/nginx don't swap in a generic Bad Gateway page that
  hides the friendly message). It also resolved the video (a costly, bot-wall-prone network call)
  **before** the access gate, so an anonymous paste produced a scary error instead of the
  "sign in to play" 402. **FIXED**: 502→422 in all 3 spots; added an anonymous short-circuit that
  returns the 402 nudge before any resolve. Verified live: anon analyze → 402, resolve fail → 422.
- 2026-06-11: `frelody-chordmini` health: yt-dlp **2026.06.09** (current), bgutil PO-token
  provider **1.3.1** wired, player_client `default,tv`, but **`cookies:false`** and no proxy.
  **Live bot-wall measurement (this session):** with the PO token alone, fresh extraction is
  *intermittent* — `dQw4w9WgXcQ` extracted a 7 MB mp3 in 10 s, but `jNQXAC9IVRw` / `9bZkp7q19f0`
  / `kJQP7kiw5Fk` all returned **"Sign in to confirm you're not a bot"** (≈1/4 success). So the
  earlier "always fails" framing was too strong — it's *unreliable roulette*, which is just as
  unusable for Alex. **Confirmed:** PO token is **necessary but not sufficient** on a datacenter
  IP ([yt-dlp #15865](https://github.com/yt-dlp/yt-dlp/issues/15865)). The n-challenge/`nsig` is
  solved fine by yt-dlp's bundled jsinterp (no Deno needed — the dQw4 extract proves it), so the
  newer "install Deno for EJS" advice is **not** our bottleneck; **IP reputation** is.
- 2026-06-11: **Researched the 2026 escape techniques and audited our stack against them.** We
  already match/exceed the typical setup (current yt-dlp + bgutil PO provider + `tv` client +
  cookie escalation using the correct per-request-copy pattern for #12045, and we correctly avoid
  the `ios` "cookie-drop" trap). The only two levers that actually beat the datacenter wall are
  **(a) logged-in cookies** (wired, unarmed) and **(b) a residential/mobile proxy** (was **not
  wired at all**). **FIXED (b)'s gap:** added an env-driven `YTDLP_PROXY` hook to
  `docker/ytdlp_server.py` (mirrors the cookies pattern, stacks with PO token + cookies, off by
  default) + a `proxy` flag in `/api/ytdlp/health` + the `YTDLP_PROXY` passthrough in compose.
  Now both escalations are a single env var away. Inert until set; live on next chordmini restart.
- 2026-06-11: **Built + deployed the cookie auto-refresher sidecar** (`frelody-cookie-refresher`,
  `docker/cookie_refresher.py`) so the cookie route is sustainable — export by hand **once**, not
  every ~2 weeks. Official MS Playwright image (`v1.60.0-jammy`; browsers baked in — no build; pip
  installs `playwright==1.60.0` at startup to match the bundled chromium). Persistent profile keeps
  the session warm; writes `youtube.txt` **only when authenticated**; shares chordmini's egress IP
  (`164.68.118.82`) so cookies satisfy YouTube's IP-binding (#15392). **E2E tested live (3 phases,
  all PASS):** (1) no-seed → refuses to write, `cookies:false`; (2) `COOKIE_REFRESH_ALLOW_ANON=true`
  → writes jar, chordmini `cookies:true`, **real `--cookies` extract produced an mp3**; (3) reverted
  to clean prod state. **Bug caught + fixed during the test:** jar was written root `0600` →
  chordmini's `app` (uid 1001) got `Permission denied` while `isfile()` health falsely showed
  `cookies:true`; now `0644`, verified readable cross-container. **Remaining handoff:** a human must
  drop a throwaway account's `youtube.seed.txt` (Firefox export) to actually arm the bypass — the
  refresher deliberately does not automate the Google login (headless datacenter login trips Google).

## Auth redirect (verified sound)

`Login.razor` threads `ReturnUrl` through email login, **Google OAuth** (stashed in
`sessionStorage` across the round-trip, restored in the callback — survives the redirect), and
**Register** (`<Register ReturnUrl=…>`). Combined with the layout-family fixes, the anon→auth→back
journey now returns Alex to the exact page+shell they left, for login / signup / OAuth alike.

## How to access Alex's audit trail (#7)

Two independent layers reconstruct any user's journey today:

**1. Distributed traces — Jaeger** (`http://localhost:16686`, service `frelody-api`).
Every request is a span (health/metrics/`_framework` excluded). Filter by HTTP route, see
latency + exceptions for analyze/unlock/payment calls. Best for "what happened in this request".

**2. Durable DB tables** (`SongData`). Query by `UserId` (= `AspNetUsers.Id`):

- **`AnalyzedSongUnlocks`** — the metered-play audit trail. One row per (UserId, Platform, VideoId)
  unlock. Columns: `UnlockedAt`, `PlayCount`, `LastPlayedAt`, `Title`, `SourceUrl`. Re-plays bump
  `PlayCount`/`LastPlayedAt` without a new row (24h window). 17 rows live today.
- **`SongPlayHistories`** — saved library/playlist plays (distinct from analyzed plays).
  Columns: `SongId`, `UserId`, `PlayedAt`, `PlaySource`, `SessionId`.
- **`Orders`** (`CustomerId`, `Status`, `OrderDate`, `TotalAmout`) + **`Payments`** (`OrderId`,
  `PaymentMethod`, `Amount`, `Currency`, `Status`, `CompletedDate`, `RawResponse`) — the money trail.

Example — one user's full activity:
```sql
SELECT 'analyzed' kind, Platform, VideoId, Title, UnlockedAt ts, PlayCount, LastPlayedAt
  FROM AnalyzedSongUnlocks WHERE UserId = @uid
UNION ALL SELECT 'library', PlaySource, CAST(SongId AS varchar), NULL, PlayedAt, NULL, NULL
  FROM SongPlayHistories WHERE UserId = @uid
ORDER BY ts DESC;

SELECT o.OrderDate, o.Status, p.PaymentMethod, p.Amount, p.Currency, p.Status, p.CompletedDate
  FROM Orders o LEFT JOIN Payments p ON p.OrderId = o.Id
 WHERE o.CustomerId = @uid ORDER BY o.OrderDate DESC;
```

**Self-service slice for Alex:** `GET /api/analyzed-access/song-history` already returns the last
7 days + practice streak + 30-day heatmap (surfaced on "Today's songs").

**Gap:** there is **no admin per-user activity UI**. Operators must run the SQL above or read
Jaeger. Building a read-only "user → timeline" admin page (unlocks + plays + payments + device
sessions on one screen) is the top remaining roadmap item — all the data and DTOs exist.

## Fixes applied (this session, deployed + verified live)

1. `/health` + `/health/ready` endpoints (`FRELODYAPIs/Program.cs`); api Docker healthcheck +
   web `depends_on: service_healthy` (`docker-compose.yml`). Verified 200 / 200-or-503.
2. Layout-family redirects (`Discover.razor`, `YoutubePlaybackView.razor`) — stay in landing family.
3. TikTok 502→422 (×3) + anonymous short-circuit (`TikTokController.cs`). Verified 402 / 422 live.

## Carried from prior sessions

- `SongsController` anonymous-write holes closed (`[Authorize]` + ownership checks).
- WASM prod logging gated to Warning; `dragdrop.js` init log removed.

## Verdict — would Alex stay, and what makes FRELODY the standard (#9)

**Would Alex enjoy it today?** The *product* is there: the conversion mechanic (anon →
402 sign-in → 2 free/day → 402 paywall at #3 → PayPal/Pesapal) is intact and verified live;
the playback value (speed/pitch, A→B loop, chords-over-lyrics) exists; the redirect journey
now returns Alex to the exact page+shell for login/signup/OAuth. Browse + cache-hit playback
are fast and friction-free.

**Why Alex might churn anyway — the bot-wall is unreliable roulette:** on prod, with the PO
token alone (no cookies, no proxy), fresh "paste a link → play along" succeeds only ~1 in 4
attempts (measured live this session) — the rest hit "Sign in to confirm you're not a bot."
Cache hits and the whole paywall path are unaffected, but Alex's morning ritual ("open the app,
find today's song, play along") fails most first-new-song attempts. Everything else is polish
next to this. **The stack is already at the 2026 state of the art** (current yt-dlp + bgutil PO
provider + `tv` client + #12045-safe cookie escalation); we are not missing a trick — the wall
is **IP reputation**, and only two levers move it: **arm `docker/cookies/youtube.txt`** (throwaway
account, Netscape export — human-in-the-loop, the highest-leverage single fix) and/or **set
`YTDLP_PROXY`** to a residential/mobile proxy (now wired; the durable answer for scale). Engage
at least one. Until then the promise is conditional.

### Architectural / algorithmic improvements (ranked by leverage)

1. **Keep the bot-wall green automatically.** Add `cookies` + a canary fresh-analysis to a
   monitor that pages when `cookies:false` or a canary 422s. The cost of a silent bot-wall is
   total: every new-song attempt fails. (A `/health/ready`-style readiness for ChordMini.)
2. **Resilient extraction.** Cookie jar → residential proxy → invidious/piped mirror fallback
   chain, tried in order, so a single blocked path doesn't end Alex's session. Today it's
   cookie-or-nothing.
3. **Warm the cache proactively.** A nightly job that pre-analyzes a trending/“song of the day”
   set means Alex's first song is a ~0.1s cache hit, not a 90–180s fresh run — and sidesteps the
   bot-wall for the curated daily flow entirely. This directly serves the "song of the day" ritual.
4. **Admin per-user activity timeline** (the documented #7 gap) — unlocks + plays + payments +
   device sessions on one screen. All data/DTOs exist; it's a read-only page + one aggregate query.
5. **Pesapal sandbox reconcile run** before trusting EA payments in prod (carried from focus plan).
6. **Idempotent, observable money loop:** assert PayPal capture + Pesapal IPN both activate
   premium exactly once; add a span/event per activation so the audit trail in Jaeger is complete.

### Quarks worth fixing (small, real)
- OpenTelemetry packages flag NU1902 (moderate CVEs) — bump `OpenTelemetry.*` past 1.15.1.
- Single-service `--force-recreate` races a concurrent `--build` and grabs the prior image;
  prefer a full-stack `up -d` after a build, or build then recreate as two steps.
