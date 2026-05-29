# FRELODY Product Brief — "Alex" (v0.1)

> **Source of truth.** This file shapes ALL user-facing messaging (email templates,
> page copy, button labels, paywall) and feature prioritization. When writing copy or
> deciding what to build, ask: *"Does this help Alex paste a link, get chords, slow it
> down, loop the hard part, and master a song in 30 minutes — then convert at the 3rd
> analyzed song?"* If not, it's off-target.

_Last updated: 2026-05-29._

## The avatar — "Alex" (pays $10/mo)

- 30–45, working professional or serious hobbyist.
- ~30 min/day to practice guitar or piano.
- **Pain:** wastes ~10 min searching tabs or manually slowing down YouTube.
- **Trigger to pay:** on the **3rd analyzed song of the day** they hit the free limit
  *while in flow* and upgrade to keep playing.
- **Benefit:** saves 5+ hrs/week, learns songs faster.

## Core experience (the $10 value to protect)

1. Search any **YouTube/TikTok** link → chords detected automatically.
2. **Slow down** 50–100%, pitch preserved.
3. **Loop** any section. *(The current beat-grid loop is considered DONE — no waveform required.)*
4. **2 free analyzed songs/day.** Sign-in required.
5. **Upgrade at the 3rd analyzed song:** $10/mo or $99/yr. **No free trial** — the free
   tier (2/day, forever) *is* the trial.

## What counts toward the daily limit

- **Metered (counts):** analysis-flow content only — YouTube/TikTok songs that require
  chord detection.
- **Free & unlimited (never counts):** public / lyric-chord songs **not** created from
  the analysis flow. Anonymous and free users play these with zero friction.
- Each unlocked analyzed song stays **available for 24h** to the signed-in user, then
  expires (not available on day 2). Re-playing an already-unlocked song within its 24h
  window does **not** consume a new slot.
- Surface the day's unlocked songs on a dedicated **"Today's songs"** page reachable from
  the **avatar dropdown**, so Alex can find what he worked on today.
- Log analyzed-song plays in `SongPlayHistory`.

## Frictionless flow

```
Browse/play public songs freely (anon)
  → paste a link / Play on analyzed content
  → "Sign in to play your first free song"
  → 1-click social login
  → song plays, "1 of 2 today" counter, kept for 24h on the Today's songs page
  → daily reset builds habit
  → paywall (PayPal / Pesapal) at analyzed song #3
```

## Decisions (2026-05-29)

- **Auth:** social login **preferred but not exclusive** — keep email/password. Have
  Google OAuth + One Tap; **add "Continue with Facebook" and "Continue with TikTok"**
  (effort-permitting). **Apple: not yet.**
- **Payments:** at the paywall the user **chooses PayPal or Pesapal**. PayPal is new work;
  Pesapal is already wired.
- **Pricing/currency:** localize the displayed price to the machine's culture and charge
  in a familiar currency where the gateway supports it — Alex shouldn't feel he's paying a
  foreign corporation. Canonical base price = **USD $10/mo, $99/yr**. **Default to USD**
  when region detection fails. (The converter + magnitude-normalizer already exist — see
  the plan doc.)
- **Strategy:** **B2C-first.** Organizations apply for **custom pricing** (multi-user
  config) — *future* work; `TenantId` stays optional for now.

## Technical compass

- Cache chord detection **per song URL** (near-zero cost after the first request).
- Rate-limit by **user ID**, not IP.
- Guest mode (1 song ever via localStorage) is low priority.

## Anti-goals (for now)

- No $100 self-serve tier. No MIDI export. No AI bandmate. No recording.
- No anonymous unlimited *analyzed* tier. No weekly cumulative caps. No free-trial mechanic.

## Success metric

10,000 paying users × $10 = **$100k MRR.** Start with 100, iterate.

---

See **[alex-focus-plan.md](./alex-focus-plan.md)** for the implementation roadmap and the
current build-vs-gap audit.
