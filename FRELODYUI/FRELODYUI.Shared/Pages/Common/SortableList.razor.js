export function init(id, group, pull, put, sort, handle, filter, component) {
    const el = document.getElementById(id);
    if (!el) {
        console.warn(`Sortable init: container not found for id='${id}'`);
        return;
    }

    let pullOpt = true;
    if (pull === false) pullOpt = false;
    if (pull === 'clone') pullOpt = 'clone';

    new Sortable(el, {
        animation: 200,
        group: {
            name: group,
            pull: pullOpt,
            put: !!put
        },
        filter: filter || undefined,
        sort: !!sort,
        forceFallback: true,
        handle: handle || undefined,

        // Add these options to allow nested interactions
        draggable: `> :not(${filter})`, // Only make non-filtered items draggable
        preventOnFilter: true,

        // Custom check to allow nested element interactions
        onStart: function (evt) {
            // Check if the actual dragged element is filtered
            const isFiltered = evt.item.matches(filter);
            if (isFiltered) {
                evt.sortable.cancel(); // Cancel the drag if it's a filtered element
            }
        },

        onUpdate: (event) => {
            // Only process if the dragged element is not filtered
            if (!event.item.matches(filter)) {
                if (event.from) {
                    const restoreBefore = event.from.children[event.oldDraggableIndex] || null;
                    event.from.insertBefore(event.item, restoreBefore);
                }
                component.invokeMethodAsync('OnUpdateJS', event.oldDraggableIndex, event.newDraggableIndex);
            }
        },
        onAdd: (event) => {
            // Only process if the dragged element is not filtered
            if (!event.item.matches(filter)) {
                const fromId = (event.from && event.from.id) ? event.from.id : '';
                if (event.to) {
                    const restoreBefore = event.to.children[event.newDraggableIndex] || null;
                    event.to.insertBefore(event.item, restoreBefore);
                }
            }
        },
        onRemove: (event) => {
            // Only process if the dragged element is not filtered
            if (!event.item.matches(filter)) {
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
        }
    });

    // Add event listeners to allow interaction with nested elements in filtered containers
    if (filter) {
        const filteredElements = el.querySelectorAll(filter);
        filteredElements.forEach(container => {
            // Allow events to propagate from nested interactive elements
            const interactiveElements = container.querySelectorAll('input, button, select, textarea, [tabindex]');
            interactiveElements.forEach(element => {
                element.addEventListener('mousedown', (e) => {
                    e.stopPropagation(); // Prevent the event from reaching SortableJS
                });

                element.addEventListener('touchstart', (e) => {
                    e.stopPropagation(); // Prevent the event from reaching SortableJS
                });

                element.addEventListener('click', (e) => {
                    e.stopPropagation(); // Prevent the event from reaching SortableJS
                });
            });
        });
    }

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

