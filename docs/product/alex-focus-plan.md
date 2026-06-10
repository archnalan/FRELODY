# Plan — Laser-focus FRELODY on "Alex"

Companion to **[ALEX.md](./ALEX.md)** (the product brief / messaging source of truth).
_Last updated: 2026-06-10 — the money loop is now built and verified end to end._

## TL;DR

The conversion mechanic exists and works: anon nudge (402) → 2 free analyzed songs/day →
paywall at #3 → PayPal/Pesapal on /pricing → premium activation. Verified live against
the running stack on 2026-06-10. Remaining work is polish (Facebook/TikTok login,
Pesapal sandbox run, admin per-user activity view) — not net-new engineering.

## Build-vs-gap audit (verified 2026-06-10)

| Alex's value | Status | Notes |
|---|---|---|
| Paste YouTube/TikTok → auto chords | ✅ Verified E2E | Fresh analysis ~90–180s; cache hit ~0.1s, reused across users. |
| Slow down, pitch preserved | ✅ Restored | Was silently deleted by commit 6f230c3 (stale file in the docs commit) — restored 2026-06-10. YouTube only; TikTok shows controls as unavailable (no rate API). |
| Loop a section | ✅ Restored | Beat-grid A→B loop, same 6f230c3 deletion + restore. Grep `pv-speed`/`pv-loop` in any diff touching Discover playback. |
| Chord timeline synced | ✅ Done | `ChordTimeline.razor`, Grid/Timeline view pill. |
| 2 free **analyzed** songs/day | ✅ Verified E2E | `AnalyzedAccessService` + `AnalyzedSongUnlock`; distinct (platform,video) per UTC day; failed analyses refund the slot; re-plays bump `PlayCount`/`LastPlayedAt` (audit trail). |
| Paywall at 3rd analyzed song | ✅ Verified E2E | 3rd distinct song → immediate 402 `limit-reached` → `AnalyzedAccessSheet` → /pricing. |
| Payments | ✅ PayPal / ⚠️ Pesapal | PayPal buttons + capture + activation live. Pesapal fixed 2026-06-10 (merchant ref was the ProductId → IPN orphaned every payment; no premium activation on COMPLETED; no browser-return reconcile) — needs one sandbox run before prod. |
| Social login | ⚠️ Partial | Google OAuth + One Tap done. Facebook/TikTok TODO. Apple deferred. Email/password kept. |
| Localized currency | ✅ Wired | USD base on /pricing with converted display via `CurrencyDisplayService`. |
| Cache chords per URL | ✅ Verified | `YouTubeTranscriptions`/`TikTokTranscriptions` keyed by (video, models); 2nd request is a DB read. |
| Rate-limit by user ID | ✅ Done | 10 analysis req/min per user (per-IP anon), 429 + JSON body. `Program.cs` "analysis" policy. |
| Chords **over lyrics** on save | ✅ New 2026-06-10 | Save-to-library fetches LRCLib synced lyrics (ChordMini `/api/lrclib-lyrics`), aligns chords to words by time window, ±15% duration guard; falls back to chords-only. |
| Admin per-user activity view | ❌ Missing | Operators reconstruct journeys via SQL (`AnalyzedSongUnlocks`, `SongPlayHistory`, `Orders`/`Payments`, device sessions) or Jaeger traces; no admin UI yet. |

## Currency: what's actually left

The robust converter already exists. Remaining work is small:

1. **Base + fallback → USD.** `CurrencyDisplayService` defaults base to `UGX` and falls back
   to UGX; change to USD ($10/$99). Re-seed `Product` prices in USD (`DatabaseSeeder`).
2. **Detection must run client-side.** `RegionInfo.CurrentRegion` reflects the user's machine
   only in WASM; on Blazor **Server** prerender it returns the *server's* locale. Resolve
   currency on the client (or via Accept-Language / IP hint).
3. **Separate display from settlement.** Display localized price freely, but each gateway
   settles in its own currencies (PayPal≈USD + supported set; Pesapal≈UGX/KES/TZS). At
   checkout, charge in the gateway's nearest supported currency and show the exact amount
   before confirm. EA → Pesapal in local currency; elsewhere → PayPal in USD.

## Workstreams (sequenced by leverage)

> **Status 2026-06-10:** A is done and verified; B is done pending a Pesapal sandbox run;
> C/D/E are partially done (Google live, metered-vs-free boundary live, hero copy updated);
> F remains future. The lists below are kept for the record.

### A — The money loop (build first; existential)
1. **Daily analyzed-play quota.** Count per user per UTC day, **only** for analysis-flow
   plays (YouTube/TikTok). Public/lyric-chord songs are unlimited and never decrement.
   Enforce N=2 (config-driven). Premium users bypass.
2. **24h availability + "Today's songs" page.** Each unlocked analyzed song stays available
   for 24h; expires on day 2. Re-playing within the window does not consume a new slot.
   Add a page listing the day's unlocked songs, linked from the **avatar dropdown**.
3. **Re-arm the gate.** Replace `IsPremiumUser()`'s `return true; //testing` with real
   `BillingStatus` evaluation.
4. **Paywall at analyzed song #3.** In-context "keep playing" sheet (not a hard wall)
   offering **PayPal or Pesapal**.
5. **Remove "14-day trial" copy** from the pricing page (no trial mechanic).

### B — Payments
6. **PayPal integration** (orders/subscriptions) alongside existing Pesapal; gateway selector
   at checkout.
7. **Route gateway by region/currency** (EA → Pesapal local; rest → PayPal USD), both reachable.
8. **Wire localized pricing** (the three currency fixes above).

### C — Auth (light touch)
9. Keep email/password; order buttons social-first. Add **Facebook + TikTok** "Continue with…"
   if effort is reasonable. Apple deferred.

### D — Access model ("Alex plays public songs freely")
10. Define the metered-vs-free boundary in one place: a song's origin (analysis-flow vs.
    public/lyric-chord) decides whether playing it is metered. Anonymous + free users browse
    and play the public catalog friction-free.

### E — Copy rewrite for Alex (uses ALEX.md)
11. Landing hero = paste-link→chords→slow→loop + the 30-min promise. Sign-in nudge =
    "Sign in to play your first free song" + "1 of 2 today" counter. Warm welcome email.
    Empty states say "paste a link → get chords," not "organize." Replace worship-leader
    testimonials with hobbyist outcomes.

### F — Org/B2C split (future)
12. Keep `TenantId` optional; add an "Organizations → apply for custom pricing" path later.
    Hide org-admin framing ("manage members/settings") from the consumer flow now.
13. **Margin:** confirm chord-detection results are cached per song URL and reused across
    users; add per-user-ID rate limiting on analysis endpoints.

## Open question

- None outstanding on the daily-limit model (resolved 2026-05-29: distinct analyzed song,
  24h availability, surfaced on a "Today's songs" page).
