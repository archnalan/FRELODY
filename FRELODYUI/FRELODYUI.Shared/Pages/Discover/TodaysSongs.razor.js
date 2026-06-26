// Anchors the activity-calendar scroll to the most-recent (right-most) weeks.
// Replaces the unreliable CSS direction:rtl / scaleX(-1) tricks, which only mirror
// painting and never actually move the scroll position off the oldest weeks.
export function scrollToEnd(el) {
    if (!el) return;
    // rAF so it runs after layout has the final scrollWidth.
    requestAnimationFrame(() => { el.scrollLeft = el.scrollWidth; });
}
