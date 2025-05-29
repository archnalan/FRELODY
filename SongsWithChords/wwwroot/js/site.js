// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
export function initDragAndDrop() {
    // Setup global event listeners for drag and drop
}

export function setDragData(key, value) {
    event.dataTransfer.setData(key, value);
}

export function getDragData(key) {
    return event.dataTransfer.getData(key);
}