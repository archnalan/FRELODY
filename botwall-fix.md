# YouTube Bot-Wall / Extraction Fix — Progression Log

Tracking the work to restore YouTube song fetch + analysis for users (Alex's report:
"unable to get a YouTube song for analysis"). Reference study lives in
[`chord-mini-ref.md`](./chord-mini-ref.md) (upstream:
https://github.com/ptnghia-j/ChordMiniApp.git).

Investigation + fixes performed **2026-06-22** against the live running stack
(`frelody-chordmini` + `frelody-bgutil` + `frelody-cookie-refresher`).

---

## What was actually broken (three layered faults)

### 1. The yt-dlp sidecar was completely wedged  ✅ FIXED
`docker/ytdlp_server.py` ran on a **single-threaded `HTTPServer` with no socket
timeout**. Its debug port (`8081`, published on `0.0.0.0` as `5002`) is reachable from
the public internet, and scanners (observed: Censys `62.210.142.162`, AWS
`3.131.220.121`) opened connections that never completed a request. Because
`BaseHTTPRequestHandler` blocks on `rfile.readline()` with no timeout and the server
handles one request at a time, **a single half-open scanner connection froze the entire
sidecar** — every real extraction call (and the health check) hung forever. This is the
root cause of "can't get any song."

Verified before fix: `GET /api/ytdlp/health` timed out even from *inside* the container;
`/proc/net/tcp` showed one `ESTABLISHED` + several `CLOSE_WAIT` connections from external
scanner IPs stuck on the listener.

**Fix:**
- `HTTPServer` → **`ThreadingHTTPServer`** (`+ daemon_threads`) so one slow/half-open
  client or one long download can no longer block others.
- Added `Handler.timeout = 30` so half-open connections drop instead of wedging a thread.
- Bound the debug ports to **loopback** in `docker-compose.yml`
  (`127.0.0.1:5001:8080`, `127.0.0.1:5002:8081`) so scanners can't reach the sidecar.
  (frelody-api talks to it over the internal Docker network, so nothing user-facing
  changes.)

### 2. `player_client=default` returned only storyboards  ✅ FIXED
YouTube tightened again (~June 2026): with `player_client=default` (web), the only
"formats" returned from our datacenter IP are **storyboard images — no audio/video**, so
every extraction failed `Requested format is not available`. Tested the full client
matrix live against the running container:

| player_client | result from our IP |
|---|---|
| web / default | ❌ storyboards only |
| web_safari, mweb, tv, android, ios, web_embedded | ❌ no usable formats |
| **tv_embedded** | ✅ real audio formats (140 m4a / 251 webm) |

**Fix:** `YTDLP_PLAYER_CLIENT: "tv_embedded,default"` in `docker-compose.yml`.

### 3. `player_skip=webpage,configs` regression  ✅ AVOIDED
While porting ChordMiniApp's flags I briefly added `player_skip=webpage,configs`. It
**breaks** the `tv_embedded`/`android_vr` path on a cold yt-dlp cache (skips the player
config needed to resolve format URLs) → `Requested format is not available` on a freshly
recreated container. Reverted; documented the trap in `chord-mini-ref.md`.

---

## End-to-end test results (live, 2026-06-22)

Sidecar HTTP API (`127.0.0.1:5002`) after fixes:

- ✅ **Health** responds instantly again.
- ✅ **Concurrency** proven: health returns `ok` immediately *while* an extract is in
  flight (would have blocked before the threading fix).
- ✅ **INFO** returns correct metadata (title/uploader/thumbnail/duration/dims).
- ✅ **EXTRACT** downloads a real MP3 (7 MB in ~8 s) for videos that pass the wall.

Success rate across a spaced sample of popular songs:

| Video | Result |
|---|---|
| Rick Astley `dQw4w9WgXcQ` | ✅ MP3 downloaded |
| Coldplay `yKNxeF4KMsY` | ❌ bot-wall |
| Ed Sheeran `JGwWNGJdvx8` | ❌ bot-wall |
| Imagine Dragons `7wtfhZwyrcc` | ❌ bot-wall |
| Gangnam Style `9bZkp7q19f0` | ❌ bot-wall |

**≈ 1 in 4** — i.e. the acute total outage is fixed (the pipeline works end-to-end and
is responsive), but a **residual datacenter-IP bot-wall** still blocks ~75% of videos.

---

## The residual: datacenter-IP reputation  ✅ FIXED (free Cloudflare WARP egress)

First confirmed the residual was *not* something else in our stack:
- bgutil PO-token provider is **healthy** — returns valid tokens; yt-dlp loads
  `bgutil:http-1.3.1 (external)`.
- Cookies are **present + authenticated** and *do* work sometimes (Rick Astley succeeded).
- On walled videos, **every** player_client — including `web`/`web_safari` *with* cookies
  — got `Sign in to confirm you're not a bot`. The block lands at the **IP layer**.

### Why the alternatives were ruled out (research, 2026-06-22)
- **Browser/Pyodide "visitor IP"** — infeasible: YouTube's API + `googlevideo` CDN send no
  CORS headers for our origin, so a browser must proxy through a server → that reintroduces
  the server IP. The reference (ChordMiniApp) works via its *proxy egress*, not visitor IP.
- **Self-hosted containers** (Cobalt/MeTube) — still run on our IP → same wall.
- **Managed APIs** (Apify/Firecrawl) — work, but per-call cost + external dependency.
- **Residential proxy** (BrightData) — gold-standard but **paid** + pending KYC.

### The fix: free Cloudflare WARP egress
YouTube does **not** blocklist Cloudflare WARP IP ranges. We added a free `warp` sidecar
(`caomingjun/warp`, SOCKS5/HTTP on `warp:1080`) and route yt-dlp through it via the existing
`YTDLP_PROXY=socks5h://warp:1080`. The cookie-refresher's Chromium uses the **same** WARP
egress (`BROWSER_PROXY`) so cookies are minted on the same clean IP yt-dlp calls from. The
sidecar also retries transient walls (`YTDLP_RETRIES`, each attempt a fresh connection that
WARP may route via a different exit IP).

**Stack:** clean WARP IP + IP-matched cookies + bgutil PO token, with retry.

### Measured result (live, 2026-06-22)
- WARP egress confirmed: exit IP `104.28.x` (Cloudflare), not Contabo `164.68.x`.
- Spike sample (format-resolve): **12/12** previously-walled videos resolved audio.
- **Acceptance test — 23 diverse videos, full extract+download path: 23/23 = 100%**
  (includes Coldplay/Ed Sheeran/Imagine Dragons/Gangnam, which all failed pre-WARP).
- Cookie-refresher re-ran through WARP and re-authenticated (24 cookies) from the clean IP.
- Same datacenter box scored ~1/4 direct → **100%** through WARP. **≥95% gate cleared.**

### Upgrade path (paid, unchanged)
Switch `YTDLP_PROXY` to the BrightData residential proxy once KYC clears
(`brd.superproxy.io:33335`) for higher-volume reliability — same plumbing, just the env var.

---

## Definition of done
- [x] Sidecar responsive & concurrent (no more total outage / wedge).
- [x] Audio formats resolvable again (`tv_embedded`).
- [x] Debug ports no longer publicly exposed.
- [x] End-to-end fetch verified working for non-walled videos.
- [x] **Reliable** fetch for arbitrary videos → free Cloudflare WARP egress + matched
      cookies + PO token + retry. Measured ≥95% (see above).

_Update this file as the remaining box gets checked._
