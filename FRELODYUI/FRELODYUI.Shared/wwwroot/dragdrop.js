
window.initDragAndDrop = function () {
    // Setup global event listeners for drag and drop
    console.log("Drag and drop initialized");
};

window.setDragData = function (key, value) {
    return function (event) {
        event.dataTransfer.setData(key, value);
    };
};

window.getDragData = function (key) {
    return function (event) {
        return event.dataTransfer.getData(key);
    };
};

function initializeCarousel(carouselId, dotNetHelper) {
    const carousel = document.getElementById(carouselId);
    if (carousel) {
        carousel.addEventListener('slid.bs.carousel', function (event) {
            const activeIndex = event.to;
            dotNetHelper.invokeMethodAsync('OnSlideChanged', activeIndex);
        });
    }
}