// Makes a fixed-position element draggable by a handle, using pointer events
// (mouse + touch). Position is clamped to the viewport. The element keeps no
// layout space (it's position:fixed), so it floats over the chord grid.

const STATE = new WeakMap();

export function makeDraggable(el, handleSelector) {
    if (!el) return;
    const handle = el.querySelector(handleSelector) || el;

    let startX = 0, startY = 0, startLeft = 0, startTop = 0, dragging = false;

    const clamp = (v, min, max) => Math.min(Math.max(v, min), max);

    const onDown = (e) => {
        // Ignore clicks on interactive controls inside the handle (e.g. collapse btn).
        if (e.target.closest('button')) return;
        const rect = el.getBoundingClientRect();
        // Switch from bottom/right anchoring to explicit top/left on first drag.
        startLeft = rect.left;
        startTop = rect.top;
        el.style.left = `${startLeft}px`;
        el.style.top = `${startTop}px`;
        el.style.right = 'auto';
        el.style.bottom = 'auto';

        startX = e.clientX;
        startY = e.clientY;
        dragging = true;
        el.classList.add('pv-float--dragging');
        handle.setPointerCapture?.(e.pointerId);
        e.preventDefault();
    };

    const onMove = (e) => {
        if (!dragging) return;
        const rect = el.getBoundingClientRect();
        const maxLeft = window.innerWidth - rect.width;
        const maxTop = window.innerHeight - rect.height;
        el.style.left = `${clamp(startLeft + (e.clientX - startX), 0, Math.max(0, maxLeft))}px`;
        el.style.top = `${clamp(startTop + (e.clientY - startY), 0, Math.max(0, maxTop))}px`;
    };

    const onUp = (e) => {
        if (!dragging) return;
        dragging = false;
        el.classList.remove('pv-float--dragging');
        handle.releasePointerCapture?.(e.pointerId);
    };

    const onResize = () => {
        // Keep the window on-screen if the viewport shrinks.
        const rect = el.getBoundingClientRect();
        if (el.style.left === '' && el.style.top === '') return;
        const maxLeft = Math.max(0, window.innerWidth - rect.width);
        const maxTop = Math.max(0, window.innerHeight - rect.height);
        el.style.left = `${clamp(rect.left, 0, maxLeft)}px`;
        el.style.top = `${clamp(rect.top, 0, maxTop)}px`;
    };

    handle.addEventListener('pointerdown', onDown);
    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp);
    window.addEventListener('resize', onResize);

    handle.style.touchAction = 'none';
    STATE.set(el, { handle, onDown, onMove, onUp, onResize });
}

// Escape closes the CSS-overlay "maximize" view. Kept here (not the global
// songFullscreen) so the playback view owns its own lifecycle.
let _escHandler = null;

export function watchEscape(dotNetRef) {
    unwatchEscape();
    _escHandler = (e) => {
        if (e.key === 'Escape') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnMaximizeEscape');
        }
    };
    document.addEventListener('keydown', _escHandler);
}

export function unwatchEscape() {
    if (_escHandler) {
        document.removeEventListener('keydown', _escHandler);
        _escHandler = null;
    }
}

// Brings the docked/floating player into view — used by the mobile mini-bar's
// "jump to player" affordance (e.g. TikTok, whose transport lives in its own
// iframe controls rather than our programmatic API).
export function scrollIntoView(el) {
    if (!el) return;
    // Stamp the shared timestamp the grid's auto-scroll yields to, so the next
    // beat tick doesn't immediately yank the page back off the player.
    window.__pvLastUserScroll = Date.now();
    try { el.scrollIntoView({ behavior: 'smooth', block: 'center' }); }
    catch { el.scrollIntoView(false); }
}

export function dispose(el) {
    const s = STATE.get(el);
    if (!s) return;
    s.handle.removeEventListener('pointerdown', s.onDown);
    window.removeEventListener('pointermove', s.onMove);
    window.removeEventListener('pointerup', s.onUp);
    window.removeEventListener('resize', s.onResize);
    STATE.delete(el);
}
