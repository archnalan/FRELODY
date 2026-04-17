export function init(id, group, pull, put, sort, handle, filter, component) {
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
                const toId = (event.to && event.to.id) ? event.to.id : '';
                const isDeleteZone = toId.startsWith('remove-button-');

                if (isDeleteZone) {
                    component.invokeMethodAsync('OnRemoveJS', event.oldDraggableIndex, -1);
                    return;
                }

                if (event.pullMode === 'clone') {
                    if (event.clone && event.clone.remove) event.clone.remove();
                }

                if (event.from) {
                    const restoreBefore = event.from.children[event.oldDraggableIndex] || null;
                    event.from.insertBefore(event.item, restoreBefore);
                }

                component.invokeMethodAsync('OnRemoveJS', event.oldDraggableIndex, event.newDraggableIndex);
        }
    });

    initializeRemoveButtons(group, component);
}

function initializeRemoveButtons(group, component) {
    const removeButtons = document.querySelectorAll('[id^="remove-button-"]');
    removeButtons.forEach(button => {
        if (!button.classList.contains('sortable-initialized')) {
            new Sortable(button, {
                group: {
                    name: group,
                    pull: false,
                    put: true
                },
                sort: false,
                animation: 0,  // Disable animation for remove zones to prevent fast snap
                onAdd: function (evt) {
                    const segmentId = evt.item.getAttribute('data-id');
                    // Revert DOM immediately (snap back without animation)
                    if (evt.from) {
                        const restoreBefore = evt.from.children[evt.oldDraggableIndex] || null;
                        evt.from.insertBefore(evt.item, restoreBefore);
                    }
                    // Notify .NET to show confirmation dialog
                    component.invokeMethodAsync('OnDropToRemoveJS', segmentId);
                }
            });
            button.classList.add('sortable-initialized');
        }
    });
}

// Export function to allow re-initialization when buttons are added/removed
export function reinitializeRemoveButtons(group, component) {
    initializeRemoveButtons(group, component);
}

