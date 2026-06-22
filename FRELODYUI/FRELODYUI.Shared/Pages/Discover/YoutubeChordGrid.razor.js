// Scrolls the currently-active beat cell into view within the chord grid.
// Uses block:'nearest' so it only scrolls when the cell is actually off-screen,
// avoiding constant jumping while the user reads a visible measure.
// True on devices with a fine pointer that can hover (desktop). Touch devices
// return false so the grid switches from hover-preview to tap-to-pin.
export function hoverCapable() {
    try {
        return window.matchMedia('(hover: hover) and (pointer: fine)').matches;
    } catch {
        return true;
    }
}

// Bounding rect of the .ycg-beat cell under the given viewport point. Lets the
// chord popover anchor to the button's *edges* (top/bottom) rather than to the
// cursor — so it sits just above the button no matter where the pointer entered
// (from above or from underneath). Returns null if the point isn't over a cell.
export function beatRectAt(x, y) {
    const hit = document.elementFromPoint(x, y);
    const beat = hit && hit.closest ? hit.closest('.ycg-beat') : null;
    if (!beat) return null;
    const r = beat.getBoundingClientRect();
    return { top: r.top, bottom: r.bottom, left: r.left, right: r.right };
}

// Records the last *manual* scroll gesture so auto-scroll can yield to the user.
// We listen to wheel/touchmove (real gestures) — NOT 'scroll', which also fires
// for our own smooth scrollIntoView and would suppress ourselves forever. The
// timestamp lives on window so other components (e.g. the mini-bar's
// jump-to-player) can also defer auto-scroll by stamping it.
let _scrollWatching = false;
const SCROLL_YIELD_MS = 4000;

function _watchUserScroll() {
    if (_scrollWatching) return;
    _scrollWatching = true;
    const mark = () => { window.__pvLastUserScroll = Date.now(); };
    window.addEventListener('wheel', mark, { passive: true });
    window.addEventListener('touchmove', mark, { passive: true });
}

export function scrollActiveIntoView(grid) {
    if (!grid) return;
    _watchUserScroll();
    // Don't fight the user: if they scrolled/touched recently, hold off snapping
    // the page back to the playhead so they can read ahead or reach the player.
    if (Date.now() - (window.__pvLastUserScroll || 0) < SCROLL_YIELD_MS) return;
    const active = grid.querySelector('.ycg-beat--active');
    if (!active) return;
    // Breathing room: reserve one beat-button height above and below the active
    // cell so it never sits flush against the top or bottom edge. scroll-margin
    // is honoured by scrollIntoView({block:'nearest'}), so the cell stops one row
    // short of either edge — leaving the chords just ahead and just behind it
    // readable instead of pinning the playhead to the viewport boundary.
    const h = active.offsetHeight || 0;
    if (h > 0) {
        active.style.scrollMarginTop = `${h}px`;
        active.style.scrollMarginBottom = `${2 * h}px`;
    }
    try {
        active.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
    } catch {
        active.scrollIntoView(false);
    }
}
