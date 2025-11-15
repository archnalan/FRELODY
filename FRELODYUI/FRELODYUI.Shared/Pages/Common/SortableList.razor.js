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
        onUpdate: (event) => {
            // Revert the DOM to match the .NET state
            if (event.from) {
                const restoreBefore = event.from.children[event.oldDraggableIndex] || null;
                event.from.insertBefore(event.item, restoreBefore);
            }
            // Notify .NET to update its model and re-render
            component.invokeMethodAsync('OnUpdateJS', event.oldDraggableIndex, event.newDraggableIndex);
        },
        onAdd: (event) => {
            const fromId = (event.from && event.from.id) ? event.from.id : '';
            // Revert DOM (don't let it stay in target)
            if (event.to) {
                const restoreBefore = event.to.children[event.newDraggableIndex] || null;
                event.to.insertBefore(event.item, restoreBefore);  // Wait, no: for add, revert by moving back to from?
                // Actually, for consistency, move back to original position in from
                if (event.from) {
                    const originalBefore = event.from.children[event.oldDraggableIndex] || null;
                    event.from.insertBefore(event.item, originalBefore);
                }
            }
        },
        onRemove: (event) => {
            const toId = (event.to && event.to.id) ? event.to.id : '';
            const isDeleteZone = toId.startsWith('remove-button-');

            if (isDeleteZone) {
                // No revert for delete; let SongSectionBoard handle removal via HandleRemoveDrop
                component.invokeMethodAsync('OnRemoveJS', event.oldDraggableIndex, -1);
                return;
            }
           
            if (event.pullMode === 'clone') {
                if (event.clone && event.clone.remove) event.clone.remove();
            }

            // Revert DOM in source list for cross-list moves (Blazor is source of truth)
            if (event.from) {
                const restoreBefore = event.from.children[event.oldDraggableIndex] || null;
                event.from.insertBefore(event.item, restoreBefore);
            }

            // Notify .NET to update source and target lists
            component.invokeMethodAsync('OnRemoveJS', event.oldDraggableIndex, event.newDraggableIndex);
        }
    });

    // Initialize external remove buttons as drop zones
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

