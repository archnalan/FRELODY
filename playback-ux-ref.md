# Playback / Player-Control UX Reference

Notes on how the Discover playback surface (`VideoPlaybackView`) drives the
YouTube vs TikTok players, why their capabilities differ, and the known
mobile-UX gaps. Written 2026-06-22.

Key files:
- `FRELODYUI.Shared/Pages/Discover/VideoPlaybackView.razor` (+ `.razor.css`, `.razor.js`) — the shared result/analysis player + chord matrix.
- `FRELODYUI.Shared/wwwroot/youtubePlayer.js` — `window.ytPlayer` shim.
- `FRELODYUI.Shared/wwwroot/tiktokPlayer.js` — `window.tiktokPlayer` shim (same interface).
- `FRELODYUI.Shared/Pages/Discover/YoutubeChordGrid.razor` (+ `.razor.js`) — the beat/chord grid ("matrix").
- `FRELODYUI.Shared/Pages/Discover/ChordTimeline.razor` — the sliding-strip alternative view.

## One player component, two backends

`VideoPlaybackView` is generic over a JS player object named by its
`PlayerApi` parameter — `"ytPlayer"` (default) or `"tiktokPlayer"`. Both shims
expose the same interface: `initialize / getCurrentTime / seekTo /
setPlaybackRate / destroy`, plus `OnPlayerReady` / `OnPlayerStateChange`
callbacks back into .NET. All player calls are `JS.InvokeVoidAsync($"{PlayerApi}.…")`.

## Why clicking the matrix plays/seeks on YouTube but does nothing on TikTok

Clicking any beat cell raises `OnSeekTo` → `SeekToAsync(seconds)` in
`VideoPlaybackView`. That method **early-returns unless `_seekSupported`**:

```csharp
private bool _seekSupported => PlayerApi == "ytPlayer";   // hard false for TikTok
private async Task SeekToAsync(float seconds)
{
    if (!_seekSupported) return;                          // ← TikTok no-ops here
    await JS.InvokeVoidAsync($"{PlayerApi}.seekTo", seconds);
}
```

- **YouTube:** `ytPlayer.seekTo` calls `_player.seekTo(seconds, true)`. Per the
  YT IFrame API, calling `seekTo` from the *cued/unstarted* state **starts
  playback** (it only stays paused if it was already explicitly paused). That's
  why "click any cell → it starts playing" works for free on YouTube.
- **TikTok:** `_seekSupported` is hard-coded `false`, so the click is swallowed.
  Every control that depends on programmatic playback (seek, loop A→B, speed,
  the popover "Play here" button) is gated on this one flag and renders in a
  disabled "Not available for TikTok playback" state (`TiktokNoCtrlTitle`).

### The nuance: `tiktokPlayer.js` *does* implement `seekTo`

The TikTok shim posts `{type:'seekTo', value:seconds, 'x-tiktok-player':true}`
to the Player v1 iframe — so the JS path exists. It is **dead** because the C#
side never calls it for TikTok. The gate was a deliberate call: TikTok's Player
v1 has no *documented/stable* seek or playback-rate API, and its tracking SDK
has historically thrown on unrecognised postMessage types across versions, so
the safe default was to disable all programmatic control rather than poke the
iframe and risk console spew / broken state. `setPlaybackRate` is a genuine
no-op (no rate API at all).

**If we want to re-enable TikTok seek:** test whether `seekTo` postMessage
actually moves the v1 player's playhead today and whether `getCurrentTime`
events keep flowing afterward. If yes, the change is mostly flipping
`_seekSupported` to also allow a `"tiktokPlayer"` *seek* capability — but keep
**speed** gated (no rate API), so `_seekSupported` would need to split into
two flags (e.g. `_seekSupported` vs `_rateSupported`).

## Mobile UX gaps (open — not yet fixed)

On small screens (`@media (max-width: 600px)`) the floating player drops out of
`position:fixed` and **docks inline at the very bottom of the content**
(`.pv-float` → `position:static`). This preserves screen real estate for the
chord matrix but creates three problems:

1. **Player is out of frame while you read the chords.** To pause/scrub you
   must scroll all the way down to the docked player. There is no always-visible
   transport control.

2. **Auto-scroll fights manual scroll ("refocusing on the matrix").**
   `YoutubeChordGrid.OnAfterRenderAsync` calls `scrollActiveIntoView` on **every
   beat change** during playback. The grid is **not its own scroll container**
   (no `overflow`/`max-height` in `YoutubeChordGrid.razor.css`), so
   `scrollIntoView({block:'nearest'})` scrolls the **window**. When the user
   scrolls down to the docked player, the next beat tick yanks the page back up
   to the active cell. Net effect: you can't stay at the player.

3. **The loop / slow-down promise is hard to reach on mobile.** The loop and
   speed controls live in `.pv-stage-bar` above the grid; on a phone they're
   cramped, and (a) loop requires tapping A then B cells in the grid while the
   player is off-screen, (b) on TikTok both are disabled entirely (see above).

### Layout constraints to respect for any fix

- `MainLayout` and `LandingLayout` already own fixed/sticky chrome. Observed
  z-index ladder: `pv-float` = 1040; layout fixed bars ≈ 1030–1050; mobile
  NavMenu offcanvas/hamburger = 10000–10001. Any new floating transport (FAB or
  mini-bar) must sit **above the chord content but below the nav offcanvas**, and
  must clear a fixed bottom mobile nav if present (add bottom inset so it doesn't
  overlap layout chrome).
- Prefer the `--k-*` token layer and keep new styles in `VideoPlaybackView.razor.css`.

### Implemented 2026-06-22

1. **Persistent mobile mini-transport bar** (`.pv-mini` in
   `VideoPlaybackView.razor` + `.razor.css`). Fixed to the viewport bottom on
   `≤600px`, hidden on desktop. Layout:
   - **YouTube (`_seekSupported`):** play/pause button (drives new shim
     `play`/`pause`) + live capo-aware chord label (`CurrentChordLabel`) +
     interactive scrub `<input range>` (`OnScrubInput` → `seekTo`, span from new
     `getDuration`).
   - **TikTok (not `_seekSupported`):** live chord label + a **jump-to-player**
     button (`ScrollToPlayerAsync` → `VideoPlaybackView.razor.js#scrollIntoView`),
     since TikTok's transport lives in its own iframe controls. No fake scrub.
   - Shim additions: `play` / `pause` / `getDuration` on **both**
     `youtubePlayer.js` and `tiktokPlayer.js`. `OnPlayerStateChange` now calls
     `StateHasChanged` so the play/pause icon flips on pause (sync loop stops).
   - `.pv` gets bottom padding on mobile to clear the fixed bar; z-index 1041
     (above content, below nav offcanvas at 10000+); honours
     `env(safe-area-inset-bottom)`.

2. **Auto-scroll yields to the user** (`YoutubeChordGrid.razor.js`).
   `scrollActiveIntoView` now skips for `SCROLL_YIELD_MS` (4s) after a manual
   `wheel`/`touchmove`, tracked via `window.__pvLastUserScroll`. The mini-bar's
   jump-to-player also stamps that timestamp so the next beat tick doesn't yank
   the page back off the player. (Listens to gestures, not `scroll`, so our own
   smooth scrolls don't suppress us.)

### Still pending — TikTok seek/transport verification (needs a live browser)

`tiktokPlayer.js` now *implements* `play`/`pause`/`seekTo`/`getDuration` over
postMessage, but **`_seekSupported` is still `ytPlayer`-only**, so TikTok gets
the jump-to-player fallback, not programmatic transport. Before enabling TikTok
transport, verify in a running browser against a real TikTok clip:
- Does `play`/`pause`/`seekTo` postMessage actually move the Player v1 playhead?
- Does `getCurrentTime`/`onCurrentTime` keep flowing after a `seekTo`?
- Does `v.duration` ever arrive on the `onCurrentTime` event (drives the scrub)?

If yes: split the single `_seekSupported` flag into seek-capable vs rate-capable
(speed has **no** Player v1 API and must stay YouTube-only), and let TikTok use
the same mini-bar transport instead of the jump button. This could not be
verified from the dev/CI environment (no browser, no `dotnet`).
