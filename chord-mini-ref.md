# ChordMiniApp — Reference Findings

> **Upstream source:** https://github.com/ptnghia-j/ChordMiniApp.git
> This is the reference implementation we study whenever FRELODY's YouTube
> audio-extraction pipeline misbehaves. The notes below were captured by reading
> that repo (cloned at `/tmp/ChordMiniApp` during the 2026-06-22 investigation).
> Re-clone from the URL above to refresh.

## Why their app "just works" where ours bot-walls

The single most important architectural difference:

**ChordMiniApp's primary extraction runs yt-dlp _inside the end user's browser_, not
on the server.** The fetch to YouTube therefore originates from each visitor's own
(usually residential) IP, which sidesteps the datacenter-IP bot-wall entirely.
Server-side `yt-dlp` is only their _fallback_.

Their two-tier strategy:

| Tier | Where | File(s) | Notes |
|---|---|---|---|
| 1 (primary) | **User's browser** | `public/browser-ytdlp-worker.js`, `src/app/api/audio/finalize-browser-extraction/route.ts` | Runs yt-dlp via **Pyodide** (CPython compiled to WASM). Uses the visitor's IP → no datacenter wall. Result is hashed (sha256), validated, and uploaded to Firebase Storage by `finalize-browser-extraction`. |
| 2 (fallback) | **Server** | `src/app/api/audio/native-ytdlp-fallback/route.ts` | Plain server-side `yt-dlp` subprocess — same approach as FRELODY's sidecar, and subject to the same datacenter bot-wall. |

### Browser worker mechanics (`public/browser-ytdlp-worker.js`)
- `PYODIDE_VERSION = '0.26.2'`, `YTDLP_VERSION = '2026.3.17'` loaded from jsDelivr CDN
  / a local wheel proxy (`/api/pyodide-package-proxy/yt_dlp-…-py3-none-any.whl`).
- Installs **XHR interceptors** that rewrite every YouTube request through a
  same-origin proxy (`/api/youtube-media-proxy?url=…`) — this is only a CORS shim;
  the extraction *logic and the credentialed fetch* still execute in the browser.
- Browser worker uses `player_client: ['android', 'ios']`.

### Server fallback flags (`native-ytdlp-fallback/route.ts`)
- Player-client list (env `YTDLP_PLAYER_CLIENTS`, default): `android,android_vr,ios,tv,web`
  — tried in order until one yields a downloadable format.
- Extractor args: `youtube:player_client=${playerClient};player_skip=webpage,configs`.
  ⚠️ **`player_skip=webpage,configs` is NOT safe to copy verbatim into FRELODY** — see
  `botwall-fix.md`; it breaks the `tv_embedded`/`android_vr` client we depend on when
  the yt-dlp player cache is cold.
- Per-call timeout: `180_000` ms.
- Cookies: optional `--cookies <file>` from `YOUTUBE_COOKIE` env (Netscape), `unshift`ed
  to the front of the arg list.
- Error heuristic: if stderr matches `/sign in to confirm|not a bot|cookies/i` and no
  cookie file was supplied, it surfaces an "auth hint" to the user.

## Takeaways for FRELODY
1. **The durable fix for the datacenter bot-wall is to move the YouTube fetch off the
   server IP.** ChordMiniApp does this with browser/Pyodide extraction; our equivalent
   levers are (a) a residential/mobile proxy (`YTDLP_PROXY`, currently BrightData —
   pending KYC), or (b) implementing browser-side extraction like theirs.
2. Their PO-token + cookies + client-list tricks are the *same family* of mitigations we
   already have in `docker/ytdlp_server.py`; they do not, on their own, beat a
   datacenter IP — which is exactly why their primary path is the browser.
3. Do not blindly port `player_skip=webpage,configs`. It suits their `android/ios`
   browser clients but regresses our server-side `tv_embedded` path.

_Last verified against upstream `main`: 2026-06-22._
