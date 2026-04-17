// Inject drag-drop visual feedback styles (once)
function ensureDragDropStyles() {
    if (document.getElementById('sortable-drag-drop-styles')) return;
    const style = document.createElement('style');
    style.id = 'sortable-drag-drop-styles';
    style.textContent = `
        .drop-target-available {
            transition: all 0.2s ease;
            box-shadow: 0 0 0 2px rgba(220, 53, 69, 0.3) !important;
        }
        .drop-target-hover {
            background-color: rgba(220, 53, 69, 0.15) !important;
            box-shadow: 0 0 0 3px rgba(220, 53, 69, 0.6) !important;
            transform: scale(1.05);
            transition: all 0.15s ease;
        }
    `;
    document.head.appendChild(style);
}

// Check if a point (clientX, clientY) is over any remove button; returns the button or null.
function getRemoveButtonAtPoint(x, y) {
    const buttons = document.querySelectorAll('[id^="remove-button-"]');
    for (const btn of buttons) {
        const rect = btn.getBoundingClientRect();
        if (x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom) {
            return btn;
        }
    }
    return null;
}

export function init(id, group, pull, put, sort, handle, filter, component) {
    ensureDragDropStyles();
    const el = document.getElementById(id);
    if (!el) {
        console.warn(`Sortable init: container not found for id='${id}'`);
        return;
    }

    let pullOpt = true;
    if (pull === false) pullOpt = false;
    if (pull === 'clone') pullOpt = 'clone';

    // Build the interactive filter: combines the structural filter (.no-drag)
    // with form elements so drag never starts from inputs/textareas/selects/buttons
    const interactiveSelectors = 'input, textarea, select, button, .form-control';
    const fullFilter = filter
        ? `${filter}, ${interactiveSelectors}`
        : interactiveSelectors;

    new Sortable(el, {
        animation: 200,
        group: {
            name: group,
            pull: pullOpt,
            put: !!put
        },
        filter: fullFilter,
        sort: !!sort,
        forceFallback: true,
        handle: handle || undefined,

        // Only make non-filtered items draggable (excludes ActionItemTemplate etc.)
        draggable: filter ? `> :not(${filter})` : undefined,
        // Allow text selection and interaction on filtered elements
        preventOnFilter: false,

        onStart: function (evt) {
            // Highlight remove buttons as available drop targets
            document.querySelectorAll('[id^="remove-button-"]').forEach(btn => {
                btn.classList.add('drop-target-available');
            });

            // Track pointer to toggle hover highlight on remove buttons.
            // SortableJS onMove only fires within Sortable containers, so we
            // use a document-level pointermove to detect hover over non-Sortable
            // remove buttons.
            const hoverHandler = (e) => {
                const hovered = getRemoveButtonAtPoint(e.clientX, e.clientY);
                document.querySelectorAll('[id^="remove-button-"]').forEach(btn => {
                    btn.classList.toggle('drop-target-hover', btn === hovered);
                });
            };
            document.addEventListener('pointermove', hoverHandler);
            // Stash cleanup so onEnd can remove the listener
            el._removeHoverHandler = () => {
                document.removeEventListener('pointermove', hoverHandler);
            };
        },

        onUpdate: (event) => {
            // Restore DOM to pre-move state so Blazor can diff correctly.
            // Must remove first so children indices reflect the original layout
            // minus the moved element, making children[oldIndex] the correct
            // insertion reference.
            if (event.from) {
                event.item.remove();
                if (event.from.children[event.oldIndex]) {
                    event.from.insertBefore(event.item, event.from.children[event.oldIndex]);
                } else {
                    event.from.appendChild(event.item);
                }
            }
            component.invokeMethodAsync('OnUpdateJS', event.oldDraggableIndex, event.newDraggableIndex);
        },
        onAdd: (event) => {
            const fromId = (event.from && event.from.id) ? event.from.id : '';
            if (event.to) {
                const restoreBefore = event.to.children[event.newDraggableIndex] || null;
                event.to.insertBefore(event.item, restoreBefore);
            }
        },
        onRemove: (event) => {
            if (event.pullMode === 'clone') {
                if (event.clone && event.clone.remove) event.clone.remove();
            }

            if (event.from) {
                const restoreBefore = event.from.children[event.oldDraggableIndex] || null;
                event.from.insertBefore(event.item, restoreBefore);
            }

            component.invokeMethodAsync('OnRemoveJS', event.oldDraggableIndex, event.newDraggableIndex);
        },

        onEnd: function (evt) {
            // Stop hover tracking
            if (el._removeHoverHandler) {
                el._removeHoverHandler();
                el._removeHoverHandler = null;
            }

            // Detect drop on remove button via pointer coordinates.
            // Since the remove button is NOT a Sortable, the item never leaves
            // the source list — no DOM move, no animation flash, no button resize.
            const dropBtn = getRemoveButtonAtPoint(
                evt.originalEvent.clientX,
                evt.originalEvent.clientY
            );
            if (dropBtn) {
                const segmentId = evt.item.getAttribute('data-id');
                if (segmentId) {
                    component.invokeMethodAsync('OnDropToRemoveJS', segmentId);
                }
            }

            // Clean up all drop target highlights
            document.querySelectorAll('[id^="remove-button-"]').forEach(btn => {
                btn.classList.remove('drop-target-available', 'drop-target-hover');
            });
        }
    });
}

// No longer needed — remove buttons are no longer Sortable instances.
// Kept as no-op exports so existing .NET callers don't break.
function initializeRemoveButtons(group, component) { }

export function reinitializeRemoveButtons(group, component) { }

