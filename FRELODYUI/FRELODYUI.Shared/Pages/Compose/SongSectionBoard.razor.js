export function initRemoveZones(component) {
    const buttons = document.querySelectorAll('[id^="remove-button-"]');
    if (!buttons || buttons.length === 0) return;

    buttons.forEach(button => {
        new Sortable(button, {
            group: {
                name: 'segments',
                pull: false,
                put: true
            },
            sort: false,
            onAdd: function (evt) {
                const line = parseInt(evt.to.getAttribute('data-line'));
                const segmentId = evt.item.getAttribute('data-id');

                // Notify .NET (instance method)
                component.invokeMethodAsync('HandleRemoveDrop', line, segmentId);

                // Remove visually from the delete zone
                evt.item.remove();
            }
        });
    });
}