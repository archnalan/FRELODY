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

export function scrollActiveIntoView(grid) {
    if (!grid) return;
    const active = grid.querySelector('.ycg-beat--active');
    if (!active) return;
    try {
        active.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
    } catch {
        active.scrollIntoView(false);
    }
}
