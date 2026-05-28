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

export function scrollActiveIntoView(grid) {
    if (!grid) return;
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
