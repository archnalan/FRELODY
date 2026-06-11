# FRELODY Product Brief — "Alex" (v0.2)

> **Source of truth.** This file shapes ALL user-facing messaging (email templates,
> page copy, button labels, paywall) and feature prioritization. When writing copy or
> deciding what to build, ask: *"Does this help Alex paste a link, get chords, slow it
> down, loop the hard part, and master a song in 30 minutes — then convert at the 3rd
> analyzed song?"* If not, it's off-target.

_Last updated: 2026-06-10 (post end-to-end verification of the money loop)._

## The avatar — "Alex" (pays $10/mo)

- 30–45, working professional or serious hobbyist.
- ~30 min/day to practice guitar or piano.
- **Pain:** wastes ~10 min searching tabs or manually slowing down YouTube.
- **Trigger to pay:** on the **3rd analyzed song of the day** they hit the free limit
  *while in flow* and upgrade to keep playing.
- **Benefit:** saves 5+ hrs/week, learns songs faster.

## Core experience (the $10 value to protect)

1. Search any **YouTube/TikTok** link → chords detected automatically. *(Verified E2E:
   ~90–180s for a fresh analysis; instant on cache hit.)*
2. **Slow down** 50–100%, pitch preserved. *(YouTube only — TikTok's embed API exposes
   no rate control, so the control renders as visibly unavailable there.)*
3. **Loop** any section via the beat grid (A→B picked directly on the chord grid).
4. **Save to library** → a real chart with **chords over lyrics** (LRCLib synced lyrics,
   beat-aligned), not a bare chord progression. Falls back to chords-only when no
   confident lyric match exists.
5. **2 free analyzed songs/day.** Sign-in required.
6. **Upgrade at the 3rd analyzed song:** $10/mo or $99/yr. **No free trial** — the free
   tier (2/day, forever) *is* the trial.

> ⚠️ **Regression guard.** The speed + loop controls live in
> `FRELODYUI.Shared/Pages/Discover/YoutubePlaybackView.razor`. They were once silently
> wiped by an unrelated commit (6f230c3) that checked in a stale copy of the file and
> nobody noticed for 9 days — the single most expensive bug this product has had,
> because it deleted the exact thing Alex pays for. Before committing changes that touch
> the Discover playback files, grep for `pv-speed` and `pv-loop` in the diff.

## What counts toward the daily limit

- **Metered (counts):** analysis-flow content only — YouTube/TikTok songs that require
  chord detection. The unit is a **distinct (platform, video) per UTC day**.
- **Free & unlimited (never counts):** public / lyric-chord songs **not** created from
  the analysis flow. Anonymous and free users play these with zero friction.
- Each unlocked analyzed song stays **available for a rolling 24h** to the signed-in
  user, then expires. Re-playing an already-unlocked song within its window does **not**
  consume a new slot (verified: cached re-play returns in ~0.1s and the counter holds).
- If an analysis *fails* after consuming a slot (bot-wall, timeout), the slot is
  **refunded**; if the server dies mid-analysis the slot is burned but the song remains
  re-openable free within the window, so Alex never pays twice for one song.
- The day's unlocked songs live on **"Today's songs"** (avatar dropdown), with a 7-day
  history + practice streak + 30-day activity heatmap behind it.
- Analyzed plays are logged on the unlock row itself (`AnalyzedSongUnlock.PlayCount`,
  `LastPlayedAt`) — *not* `SongPlayHistory`, which tracks library/playlist plays of
  saved songs. Together they are the audit trail of Alex's practice.

## Frictionless flow

```
Browse/play public songs freely (anon)
  → paste a link / Play on analyzed content
  → "Sign in to play your first free song"   (402 reason=unauthenticated → sheet)
  → 1-click social login
  → song plays, "1 of 2 today" counter, kept 24h on Today's songs
  → daily reset (UTC midnight) builds habit
  → paywall (402 reason=limit-reached → in-context sheet) at analyzed song #3
  → /pricing: choose PayPal (buttons, USD) or Pesapal (iframe, mobile money/local)
```

## Decisions (updated 2026-06-10)

- **Auth:** social login **preferred but not exclusive** — keep email/password.
  Google OAuth + One Tap **live**. "Continue with Facebook / TikTok" still TODO
  (effort-permitting). **Apple: not yet.**
- **Payments:** at the paywall the user **chooses PayPal or Pesapal** on /pricing.
  - **PayPal**: Orders v2 + JS buttons, capture activates premium server-side — live.
  - **Pesapal**: iframe flow wired; IPN now activates premium on COMPLETED and the
    browser-return reconciles idempotently (needs a sandbox run before prod).
- **Pricing/currency:** localize the displayed price to the machine's culture; charge in
  the gateway's nearest supported currency. Canonical base price = **USD $10/mo, $99/yr**.
  **Default to USD** when region detection fails.
- **Strategy:** **B2C-first.** Organizations apply for **custom pricing** — *future*
  work; `TenantId` stays optional for now.

## Technical compass

- Chord detection **cached per (video, model)** in `*Transcriptions` — the 2nd user (or
  2nd play) costs ~0. Verified.
- **Rate-limit by user ID**, not IP — live: 10 analysis requests/min per user (per-IP
  for anonymous), 429 with a JSON body so the UI can message it.
- Lyrics come from **LRCLib via the ChordMini backend** (`/api/lrclib-lyrics`), matched
  by artist/title parsed from the video title, with a ±15% duration guard against
  wrong-song matches. Strictly best-effort: a miss saves chords-only.
- Guest mode (1 song ever via localStorage) is low priority.
- ⚠️ **Prod reality check — the bot-wall gates the core promise.** "Paste a link → play
  along" only works on prod when the ChordMini sidecar can fetch audio. The box exits
  through a datacenter IP that YouTube/TikTok bot-wall *even with a valid PO token*; the
  escalation (logged-in **cookies**, `docker/cookies/youtube.txt`) must be **armed** or
  *fresh* analyses fail (cache hits and the whole paywall path still work). Check
  `docker exec frelody-chordmini curl -s localhost:8081/api/ytdlp/health` → `cookies:true`.
  If `false`, Alex cannot analyze a new song today. This is the #1 thing to keep green.
- App-level analysis failures (bot-wall, region, copyright, timeout) return **422** with a
  JSON body — never 502/503 — so the friendly message survives Cloudflare/nginx. The slot
  is refunded so Alex never pays for a failed analysis.

## Anti-goals (for now)

- No $100 self-serve tier. No MIDI export. No AI bandmate. No recording.
- No anonymous unlimited *analyzed* tier. No weekly cumulative caps. No free-trial mechanic.

## Success metric

10,000 paying users × $10 = **$100k MRR.** Start with 100, iterate.

---

See **[alex-focus-plan.md](./alex-focus-plan.md)** for the implementation roadmap and the
current build-vs-gap audit.
