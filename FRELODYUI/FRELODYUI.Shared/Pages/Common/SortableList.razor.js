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
}

