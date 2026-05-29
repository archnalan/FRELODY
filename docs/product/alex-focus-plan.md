# Plan — Laser-focus FRELODY on "Alex"

Companion to **[ALEX.md](./ALEX.md)** (the product brief / messaging source of truth).
This is a **plan only** — no implementation has been done. _Last updated: 2026-05-29._

## TL;DR

The practice engine is ~80% built and polished. The **conversion mechanic Alex pays
for does not exist**, and the app's copy speaks to a B2B worship-team admin rather than a
solo hobbyist. The work is mostly *focus, monetization wiring, and messaging* — not net-new
engineering.

## Build-vs-gap audit

| Alex's value | Status | Notes |
|---|---|---|
| Paste YouTube/TikTok → auto chords | ✅ Done | `Discover.razor`, `YouTubeController`/`TikTokController` → ChordMini. Cached in `*Transcriptions`. |
| Slow down, pitch preserved | ✅ Done (YouTube) | `YoutubePlaybackView.razor` + `youtubePlayer.js`. Disabled on TikTok (no rate API). |
| Loop a section | ✅ Done | Beat-grid A/B loop. No waveform — accepted as-is. |
| Chord timeline synced | ✅ Done | `ChordTimeline.razor`. |
| 2 free **analyzed** songs/day | ❌ Missing | `SongPlayHistory` logs plays for analytics only; no quota/reset/enforcement. |
| Paywall at 3rd analyzed song | ❌ Missing | No trigger. `IsPremiumUser()` is hardcoded `return true; //testing` — gate welded open. |
| Social login | ⚠️ Partial | Google OAuth + One Tap done. Facebook/TikTok TODO. Apple deferred. Email/password kept. |
| Localized currency | ✅ Built, unwired | `FRELODYSHRD/Services/CurrencyConverter.cs` (magnitude-normalizes) + `FRELODYUI.Shared/Services/CurrencyDisplayService.cs` (region detection) + `CurrencyConverterController`. Base/fallback currently UGX. |
| Cache chords per URL | ⚠️ Partial | Web-scrape HTML cached 24h; confirm chord results are keyed by URL and reused across users. |
| Rate-limit by user ID | ⚠️ Partial | Only auth-attempt limiting; no usage quotas. |

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
