// Trigger click on an element by ID
window.clickElement = function (id) {
    var el = document.getElementById(id);
    if (el) el.click();
};

// Scroll element into view within its scroll container
window.scrollIntoViewIfNeeded = function (elementId) {
    var el = document.getElementById(elementId);
    if (el && el.scrollIntoView) {
        el.scrollIntoView({ block: 'nearest' });
    }
};

// Print functionality for web
window.printContent = function (htmlContent) {
    // Create a new window for printing
    const printWindow = window.open('', '_blank', 'width=800,height=600');

    // Write the content to the new window
    printWindow.document.write(htmlContent);
    printWindow.document.close();

    // Wait for content to load, then print
    printWindow.onload = function () {
        printWindow.focus();
        printWindow.print();
        printWindow.close();
    };
};

// Fallback clipboard function for older browsers
window.fallbackCopyToClipboard = function (text) {
    const textArea = document.createElement("textarea");
    textArea.value = text;

    // Avoid scrolling to bottom
    textArea.style.top = "0";
    textArea.style.left = "0";
    textArea.style.position = "fixed";
    textArea.style.opacity = "0";

    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    try {
        const successful = document.execCommand('copy');
        if (!successful) {
            throw new Error('Copy command was unsuccessful');
        }
    } catch (err) {
        console.error('Fallback: Could not copy text: ', err);
        throw err;
    } finally {
        document.body.removeChild(textArea);
    }
};

// Function to close dropdowns when clicking outside
window.closeDropdownsOnClickOutside = function (dotNetReference) {
    document.addEventListener('click', function (event) {
        const dropdowns = document.querySelectorAll('.dropdown');
        dropdowns.forEach(dropdown => {
            if (!dropdown.contains(event.target)) {
                // If click is outside dropdown, close it
                dotNetReference.invokeMethodAsync('CloseDropdown');
            }
        });
    });
};

// Close dropdown/popover when clicking outside
window.addClickOutsideListener = function (elementSelector, dotNetReference, methodName) {
    const handler = function (event) {
        const element = document.querySelector(elementSelector);
        if (element && !element.contains(event.target)) {
            dotNetReference.invokeMethodAsync(methodName);
        }
    };

    document.addEventListener('click', handler);

    // Return cleanup function
    return {
        dispose: function () {
            document.removeEventListener('click', handler);
        }
    };
};

window.getTabIndexOrDefault = (el) => {
    if (!el) return 0;

    // If element has explicit tabIndex, use that
    if (el.tabIndex && el.tabIndex > 0) {
        return el.tabIndex;
    }

    // Otherwise, return a safe base number
    return 1000; // big enough so we don't clash with native flow
};

(function () {
    function appBasePath() {
        const base = document.querySelector("base");
        const href = (base && base.getAttribute("href")) || "/";
        return href.endsWith("/") ? href : href + "/";
    }

    // Call from C#: await JsRt.InvokeVoidAsync("goBack");
    window.goBack = function () {
        try {
            // If there is at least one prior entry in this tab/session, go back.
            if (history.length > 1) {
                history.go(-1);
                return;
            }
        } catch { /* ignore and fall through */ }

        // Fallback to app base (no hardcoded /songs-list)
        location.assign(appBasePath());
    };
})();

function getBoundingClientRect(element) {
    return element.getBoundingClientRect();
}

window.focusInputAndSetCursorToEnd = function (element) {
    if (element && element.focus) {
        element.focus();
        // Set cursor position to end of text
        if (element.setSelectionRange) {
            const length = element.value.length;
            element.setSelectionRange(length, length);
        } else if (element.createTextRange) {
            // Fallback for older browsers
            const range = element.createTextRange();
            range.collapse(true);
            range.moveEnd('character', element.value.length);
            range.moveStart('character', element.value.length);
            range.select();
        }
    }
};

window.setupSearchShortcut = () => {
    window.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            document.querySelector('.search-trigger')?.click();
        }
    });
};

window.hideDropdown = function (dropdownButtonId) {
    const dropdownBtn = document.getElementById(dropdownButtonId);
    if (dropdownBtn && window.bootstrap && window.bootstrap.Dropdown) {
        const bsDropdown = bootstrap.Dropdown.getInstance(dropdownBtn);
        if (bsDropdown) {
            bsDropdown.hide();
        }
    }
};

// ============================================
// AUTOSCROLL FUNCTIONALITY - UNIVERSAL APPROACH
// ============================================

/**
 * Get the current scroll container with all scroll utilities
 * Returns a unified interface that works with both LandingLayout and MainLayout
 * @returns {object} Scroll container object with utility methods
 */
window.getCurrentScrollContainer = function () {
    // Check for fullscreen mode first (both native API and CSS fallback)
    var fullscreenEl = document.fullscreenElement || document.querySelector('.playback-fullscreen');
    if (fullscreenEl) {
        return {
            element: fullscreenEl,
            scrollTop: fullscreenEl.scrollTop,
            scrollHeight: fullscreenEl.scrollHeight,
            clientHeight: fullscreenEl.clientHeight,
            scrollBy: (pixels) => {
                fullscreenEl.scrollBy({ top: pixels, behavior: 'smooth' });
            },
            scrollTo: (position) => {
                fullscreenEl.scrollTo({ top: position, behavior: 'smooth' });
            },
            scrollToTop: () => {
                fullscreenEl.scrollTo({ top: 0, behavior: 'smooth' });
            },
            scrollToBottom: () => {
                fullscreenEl.scrollTo({ top: fullscreenEl.scrollHeight, behavior: 'smooth' });
            }
        };
    }

    // Check for LandingLayout
    const landingBody = document.querySelector('.landing-body');
    if (landingBody) {
        return {
            element: landingBody,
            scrollTop: landingBody.scrollTop,
            scrollHeight: landingBody.scrollHeight,
            clientHeight: landingBody.clientHeight,
            scrollBy: (pixels) => {
                landingBody.scrollBy({ top: pixels, behavior: 'smooth' });
            },
            scrollTo: (position) => {
                landingBody.scrollTo({ top: position, behavior: 'smooth' });
            },
            scrollToTop: () => {
                landingBody.scrollTo({ top: 0, behavior: 'smooth' });
            },
            scrollToBottom: () => {
                landingBody.scrollTo({ top: landingBody.scrollHeight, behavior: 'smooth' });
            }
        };
    }

    // Fallback to window/document for MainLayout
    return {
        element: window,
        scrollTop: window.pageYOffset || document.documentElement.scrollTop,
        scrollHeight: document.documentElement.scrollHeight,
        clientHeight: document.documentElement.clientHeight,
        scrollBy: (pixels) => {
            window.scrollBy({ top: pixels, behavior: 'smooth' });
        },
        scrollTo: (position) => {
            window.scrollTo({ top: position, behavior: 'smooth' });
        },
        scrollToTop: () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        },
        scrollToBottom: () => {
            const maxScroll = document.documentElement.scrollHeight;
            window.scrollTo({ top: maxScroll, behavior: 'smooth' });
        }
    };
};

/**
 * Smooth scroll by a specific number of pixels
 * Works universally across both layouts
 * @param {number} pixels - Number of pixels to scroll
 */
window.smoothScrollBy = function (pixels) {
    const container = window.getCurrentScrollContainer();
    container.scrollBy(pixels);
};

/**
 * Scroll to top of the page
 * Works universally across both layouts
 */
window.scrollToTop = function () {
    const container = window.getCurrentScrollContainer();
    container.scrollToTop();
};

/**
 * Scroll to bottom of the page
 * Works universally across both layouts
 */
window.scrollToBottom = function () {
    const container = window.getCurrentScrollContainer();
    container.scrollToBottom();
};

/**
 * Scroll the nearest scrollable ancestor so the given element appears at the
 * TOP of the visible area (not all the way to the bottom of the page).
 * Used by the Studio monitor so mon-header lands at top-0 after extraction.
 * @param {HTMLElement} element
 */
window.scrollToElementStart = function (element) {
    if (!element) return;
    element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    // Move keyboard focus away from the composer into the monitor
    element.focus({ preventScroll: true });
};

/**
 * Scroll the nearest scrollable ancestor so the element appears at the TOP of the viewport.
 * Used to bring the mon-card into view without scrolling all the way to the bottom.
 * @param {HTMLElement} element
 */
window.scrollToElementStart = function (element) {
    if (!element) return;
    element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    // Move keyboard focus away from the composer into the monitor
    element.focus({ preventScroll: true });
};

/**
 * Get current scroll position
 * Works universally across both layouts
 * @returns {object} Object with scrollTop, scrollHeight, and clientHeight
 */
window.getScrollPosition = function () {
    const container = window.getCurrentScrollContainer();
    return {
        scrollTop: container.scrollTop,
        scrollHeight: container.scrollHeight,
        clientHeight: container.clientHeight
    };
};

/**
 * Check if user has scrolled to bottom
 * Works universally across both layouts
 * @param {number} threshold - Pixel threshold before bottom (default: 50)
 * @returns {boolean} True if at bottom
 */
window.isAtBottom = function (threshold = 50) {
    const container = window.getCurrentScrollContainer();
    return (container.scrollTop + container.clientHeight) >= (container.scrollHeight - threshold);
};

/**
 * Check if the current layout is LandingLayout
 * @returns {boolean} True if using LandingLayout
 */
window.isLandingLayout = function () {
    return document.querySelector('.landing-body') !== null;
};

// ============================================
// ELEMENT SCROLL FUNCTIONALITY (for horizontal sliders)
// ============================================

/**
 * Scroll an element horizontally
 * @param {HTMLElement} element - The element to scroll
 * @param {number} position - The scroll position
 */
window.scrollElement = function (element, position) {
    if (element) {
        element.scrollTo({
            left: position,
            behavior: 'smooth'
        });
    }
};

/**
 * Get element scroll position
 * @param {HTMLElement} element - The element
 * @returns {object} Scroll position info
 */
window.getElementScrollPosition = function (element) {
    if (!element) return { scrollLeft: 0, scrollWidth: 0, clientWidth: 0 };

    return {
        scrollLeft: element.scrollLeft,
        scrollWidth: element.scrollWidth,
        clientWidth: element.clientWidth
    };
};

// ============================================
// POPOVER POSITIONING
// ============================================

/**
 * Calculate optimal popover position to keep it within viewport
 * @param {number} mouseX - Mouse X position
 * @param {number} mouseY - Mouse Y position
 * @param {number} popoverWidth - Popover width
 * @param {number} popoverHeight - Popover height
 * @returns {object} Optimal position {top, left}
 */
window.calculatePopoverPosition = function (mouseX, mouseY, popoverWidth, popoverHeight) {
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const scrollX = window.pageXOffset || document.documentElement.scrollLeft;
    const scrollY = window.pageYOffset || document.documentElement.scrollTop;

    let top = mouseY + scrollY + 10;
    let left = mouseX + scrollX + 10;

    // Check if popover would go off right edge
    if (left + popoverWidth > viewportWidth + scrollX) {
        left = mouseX + scrollX - popoverWidth - 10;
    }

    // Check if popover would go off bottom edge
    if (top + popoverHeight > viewportHeight + scrollY) {
        top = mouseY + scrollY - popoverHeight - 10;
    }

    // Ensure popover doesn't go off left edge
    if (left < scrollX) {
        left = scrollX + 10;
    }

    // Ensure popover doesn't go off top edge
    if (top < scrollY) {
        top = scrollY + 10;
    }

    return { top, left };
};

/**
 * Get element position relative to viewport
 * @param {HTMLElement} element - The element
 * @returns {object} Position info
 */
window.getElementPosition = function (element) {
    if (!element) return { top: 0, left: 0, width: 0, height: 0 };

    const rect = element.getBoundingClientRect();
    const scrollX = window.pageXOffset || document.documentElement.scrollLeft;
    const scrollY = window.pageYOffset || document.documentElement.scrollTop;

    return {
        top: rect.top + scrollY,
        left: rect.left + scrollX,
        width: rect.width,
        height: rect.height,
        bottom: rect.bottom + scrollY,
        right: rect.right + scrollX
    };
};

// ============================================
// PREVENT SCROLL ON USER INTERACTION
// ============================================

let userScrollTimeout = null;
let isUserScrolling = false;

/**
 * Detect user scroll and temporarily pause autoscroll
 * @param {function} callback - Callback when user stops scrolling
 * @param {number} delay - Delay in ms before callback is called
 */
window.detectUserScroll = function (callback, delay = 2000) {
    const handleScroll = function () {
        isUserScrolling = true;

        if (userScrollTimeout) {
            clearTimeout(userScrollTimeout);
        }

        userScrollTimeout = setTimeout(function () {
            isUserScrolling = false;
            if (callback && typeof callback === 'function') {
                callback();
            }
        }, delay);
    };

    window.addEventListener('wheel', handleScroll, { passive: true });
    window.addEventListener('touchmove', handleScroll, { passive: true });

    return {
        dispose: function () {
            window.removeEventListener('wheel', handleScroll);
            window.removeEventListener('touchmove', handleScroll);
            if (userScrollTimeout) {
                clearTimeout(userScrollTimeout);
            }
        },
        isUserScrolling: function () {
            return isUserScrolling;
        }
    };
};

// ============================================
// CHORD CHARTS CAROUSEL HELPER
// ============================================

/**
 * Initialize Bootstrap carousel callbacks
 * @param {string} carouselId - ID of the carousel element
 * @param {object} dotNetReference - Reference to .NET object
 */
window.initializeCarousel = function (carouselId, dotNetReference) {
    const carouselElement = document.getElementById(carouselId);
    if (!carouselElement) return;

    carouselElement.addEventListener('slid.bs.carousel', function (event) {
        const index = event.to;
        dotNetReference.invokeMethodAsync('OnSlideChanged', index);
    });
};

// ============================================
// UTILITY FUNCTIONS
// ============================================

/**
 * Debounce function for performance optimization
 * @param {function} func - Function to debounce
 * @param {number} wait - Wait time in ms
 * @returns {function} Debounced function
 */
window.debounce = function (func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = function () {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
};

/**
 * Throttle function for performance optimization
 * @param {function} func - Function to throttle
 * @param {number} limit - Time limit in ms
 * @returns {function} Throttled function
 */
window.throttle = function (func, limit) {
    let inThrottle;
    return function () {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
};

/**
 * Add keyboard shortcut listener
 * @param {string} key - Key to listen for (e.g., 'Space', 'Escape')
 * @param {function} callback - Callback function
 * @param {boolean} ctrlKey - Whether Ctrl key is required
 * @param {boolean} shiftKey - Whether Shift key is required
 * @returns {object} Disposable object
 */
window.addKeyboardShortcut = function (key, callback, ctrlKey = false, shiftKey = false) {
    const handler = function (e) {
        const matchesCtrl = ctrlKey ? (e.ctrlKey || e.metaKey) : true;
        const matchesShift = shiftKey ? e.shiftKey : true;

        if (e.key === key && matchesCtrl && matchesShift) {
            e.preventDefault();
            callback();
        }
    };

    window.addEventListener('keydown', handler);

    return {
        dispose: function () {
            window.removeEventListener('keydown', handler);
        }
    };
};

// ============================================
// RESPONSIVE HELPERS
// ============================================

/**
 * Check if device is mobile
 * @returns {boolean} True if mobile device
 */
window.isMobileDevice = function () {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
};

/**
 * Get viewport dimensions
 * @returns {object} Width and height of viewport
 */
window.getViewportDimensions = function () {
    return {
        width: Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0),
        height: Math.max(document.documentElement.clientHeight || 0, window.innerHeight || 0)
    };
};

/**
 * Check if element is in viewport
 * @param {HTMLElement} element - Element to check
 * @returns {boolean} True if in viewport
 */
window.isElementInViewport = function (element) {
    if (!element) return false;

    const rect = element.getBoundingClientRect();
    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
};


// ============================================
// SONG FULLSCREEN FUNCTIONALITY
// ============================================

window.songFullscreen = {
    _dotNetRef: null,
    _handler: null,

    enter: function (element) {
        if (element && element.requestFullscreen) {
            element.requestFullscreen().catch(function () {
                // Fallback: use CSS-only fullscreen if API is blocked
                element.classList.add('playback-fullscreen');
            });
        } else if (element) {
            element.classList.add('playback-fullscreen');
        }
    },

    exit: function () {
        if (document.fullscreenElement) {
            document.exitFullscreen().catch(function () { });
        }
        // Also remove CSS fallback class
        var el = document.querySelector('.playback-fullscreen');
        if (el) {
            el.classList.remove('playback-fullscreen');
        }
    },

    registerEscHandler: function (dotNetRef) {
        // Clean up previous handlers if any
        if (window.songFullscreen._handler) {
            document.removeEventListener('keydown', window.songFullscreen._handler);
        }
        if (window.songFullscreen._fullscreenChangeHandler) {
            document.removeEventListener('fullscreenchange', window.songFullscreen._fullscreenChangeHandler);
        }
        window.songFullscreen._dotNetRef = dotNetRef;
        window.songFullscreen._handler = function (e) {
            if (e.key === 'Escape') {
                var fullscreenEl = document.querySelector('.playback-fullscreen');
                if (fullscreenEl && !document.fullscreenElement) {
                    // CSS-only fullscreen — notify Blazor
                    e.preventDefault();
                    dotNetRef.invokeMethodAsync('OnEscapePressed');
                }
            }
        };
        window.songFullscreen._fullscreenChangeHandler = function () {
            if (!document.fullscreenElement) {
                var el = document.querySelector('.playback-fullscreen');
                if (el) {
                    el.classList.remove('playback-fullscreen');
                }
                // Notify Blazor that fullscreen was exited (e.g. via browser ESC)
                dotNetRef.invokeMethodAsync('OnEscapePressed');
            }
        };
        document.addEventListener('keydown', window.songFullscreen._handler);
        document.addEventListener('fullscreenchange', window.songFullscreen._fullscreenChangeHandler);
    },

    dispose: function () {
        if (window.songFullscreen._handler) {
            document.removeEventListener('keydown', window.songFullscreen._handler);
            window.songFullscreen._handler = null;
        }
        if (window.songFullscreen._fullscreenChangeHandler) {
            document.removeEventListener('fullscreenchange', window.songFullscreen._fullscreenChangeHandler);
            window.songFullscreen._fullscreenChangeHandler = null;
        }
        window.songFullscreen._dotNetRef = null;
    }
};

// ============================================
// DRAGGABLE PALETTE
// ============================================

/**
 * Make a palette element draggable via its header (drag handle)
 * Supports both mouse and touch, clamped to viewport
 * @param {HTMLElement} paletteEl - The palette element to make draggable
 */
window.initDraggablePalette = function (paletteEl) {
    if (!paletteEl) return;

    // Clean up previous drag listeners if re-initialized
    if (paletteEl._dragCleanup) {
        paletteEl._dragCleanup();
    }

    var handle = paletteEl.querySelector('.palette-drag-handle');
    if (!handle) handle = paletteEl.querySelector('.settings-header');
    if (!handle) return;

    var offsetX = 0, offsetY = 0;
    var isDragging = false;

    function clampPosition(x, y) {
        var rect = paletteEl.getBoundingClientRect();
        var vw = window.innerWidth;
        var vh = window.innerHeight;
        x = Math.max(0, Math.min(x, vw - rect.width));
        y = Math.max(0, Math.min(y, vh - rect.height));
        return { x: x, y: y };
    }

    function onStart(clientX, clientY) {
        isDragging = true;
        var rect = paletteEl.getBoundingClientRect();
        offsetX = clientX - rect.left;
        offsetY = clientY - rect.top;

        // Switch to fixed positioning so coords are viewport-relative
        paletteEl.style.position = 'fixed';
        paletteEl.style.left = rect.left + 'px';
        paletteEl.style.top = rect.top + 'px';
        paletteEl.style.right = 'auto';
        paletteEl.style.bottom = 'auto';
        paletteEl.style.transition = 'none';
        paletteEl.classList.add('is-dragging');
    }

    function onMove(clientX, clientY) {
        if (!isDragging) return;
        var pos = clampPosition(clientX - offsetX, clientY - offsetY);
        paletteEl.style.left = pos.x + 'px';
        paletteEl.style.top = pos.y + 'px';
    }

    function onEnd() {
        if (!isDragging) return;
        isDragging = false;
        paletteEl.style.transition = '';
        paletteEl.classList.remove('is-dragging');
    }

    // Mouse events
    function onMouseDown(e) {
        if (e.target.closest('button, input, select')) return;
        e.preventDefault();
        onStart(e.clientX, e.clientY);
    }
    function onMouseMove(e) {
        onMove(e.clientX, e.clientY);
    }

    // Touch events
    function onTouchStart(e) {
        if (e.target.closest('button, input, select')) return;
        var t = e.touches[0];
        onStart(t.clientX, t.clientY);
    }
    function onTouchMove(e) {
        if (!isDragging) return;
        var t = e.touches[0];
        onMove(t.clientX, t.clientY);
    }

    handle.addEventListener('mousedown', onMouseDown);
    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onEnd);
    handle.addEventListener('touchstart', onTouchStart, { passive: true });
    document.addEventListener('touchmove', onTouchMove, { passive: true });
    document.addEventListener('touchend', onEnd);

    handle.style.cursor = 'grab';

    // Store cleanup function for re-initialization
    paletteEl._dragCleanup = function () {
        handle.removeEventListener('mousedown', onMouseDown);
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onEnd);
        handle.removeEventListener('touchstart', onTouchStart);
        document.removeEventListener('touchmove', onTouchMove);
        document.removeEventListener('touchend', onEnd);
    };
};

// ============================================
// BOOTSTRAP TOOLTIP INITIALIZATION
// ============================================

/**
 * Initialize Bootstrap tooltips on all elements with a title attribute.
 * Moves native title → data-bs-title to prevent ghost native tooltips.
 * Safely disposes existing instances to avoid duplicates during Blazor re-renders.
 * @param {HTMLElement} [root=document] - Root element to search within
 */
window.initializeTooltips = function (root) {
    root = root || document;
    if (typeof bootstrap === 'undefined' || !bootstrap.Tooltip) return;

    var elements = root.querySelectorAll('[title]:not([data-bs-tooltip-init])');
    elements.forEach(function (el) {
        var titleText = el.getAttribute('title');
        // Skip elements with empty titles
        if (!titleText) return;

        // Dispose any existing tooltip instance
        var existing = bootstrap.Tooltip.getInstance(el);
        if (existing) existing.dispose();

        // Move native title to data-bs-title so no orphaned native tooltip appears
        el.setAttribute('data-bs-title', titleText);
        el.removeAttribute('title');
        el.setAttribute('data-bs-toggle', 'tooltip');
        el.setAttribute('data-bs-tooltip-init', '');
        new bootstrap.Tooltip(el);
    });
};

/**
 * Hide every visible Bootstrap tooltip (removes orphaned tooltip DOM nodes).
 * Call before navigation, opening modals, or any major DOM swap.
 */
window.hideAllTooltips = function () {
    if (typeof bootstrap === 'undefined' || !bootstrap.Tooltip) return;
    document.querySelectorAll('[data-bs-tooltip-init]').forEach(function (el) {
        try {
            var instance = bootstrap.Tooltip.getInstance(el);
            if (instance) {
                // Use dispose() instead of hide() to avoid async transition
                // callbacks that crash when internal state is already null
                instance.dispose();
            }
        } catch (e) {
            // Tooltip may already be disposed during navigation
        }
        el.removeAttribute('data-bs-tooltip-init');
    });
};

/**
 * Dispose all Bootstrap tooltips within a root element
 * @param {HTMLElement} [root=document] - Root element to search within
 */
window.disposeTooltips = function (root) {
    root = root || document;
    if (typeof bootstrap === 'undefined' || !bootstrap.Tooltip) return;

    var elements = root.querySelectorAll('[data-bs-tooltip-init]');
    elements.forEach(function (el) {
        try {
            var instance = bootstrap.Tooltip.getInstance(el);
            if (instance) instance.dispose();
        } catch (e) {
            // Tooltip may already be disposed
        }
        el.removeAttribute('data-bs-tooltip-init');
    });
};

// ============================================
// SCREEN ORIENTATION LOCK (fullscreen mobile)
// ============================================

/**
 * Lock screen to a given orientation. Requires Fullscreen API to be active on most browsers.
 * @param {string} orientation - 'landscape' or 'portrait'
 */
window.lockScreenOrientation = async function (orientation) {
    try {
        if (screen.orientation && screen.orientation.lock) {
            var type = orientation === 'landscape' ? 'landscape-primary' : 'portrait-primary';
            await screen.orientation.lock(type);
        }
    } catch (e) {
        console.warn('Screen orientation lock not supported or failed:', e);
    }
};

/**
 * Unlock screen orientation, allowing the device to rotate freely.
 */
window.unlockScreenOrientation = function () {
    try {
        if (screen.orientation && screen.orientation.unlock) {
            screen.orientation.unlock();
        }
    } catch (e) {
        console.warn('Screen orientation unlock failed:', e);
    }
};

// ============================================
// INITIALIZE ON LOAD
// ============================================

(function () {
    // Ensure popovers and dropdowns have proper z-index
    document.addEventListener('DOMContentLoaded', function () {
        var style = document.createElement('style');
        style.textContent = `
            .chord-popover-overlay {
                position: fixed !important;
                z-index: 200002 !important;
            }
        `;
        document.head.appendChild(style);

        // Initialize Bootstrap tooltips on page load
        window.initializeTooltips();
    });

    // Re-initialize tooltips when Blazor updates the DOM
    // Also dispose tooltips whose trigger elements were removed (prevents orphans)
    var tooltipInitTimer = null;
    var tooltipObserver = new MutationObserver(function (mutations) {
        // Dispose tooltips on any removed nodes so their tip DOM is cleaned up
        mutations.forEach(function (m) {
            m.removedNodes.forEach(function (node) {
                if (node.nodeType !== 1) return;
                try {
                    var targets = node.querySelectorAll
                        ? node.querySelectorAll('[data-bs-tooltip-init]')
                        : [];
                    // Also check the node itself
                    if (node.matches && node.matches('[data-bs-tooltip-init]')) {
                        var inst = bootstrap.Tooltip.getInstance(node);
                        if (inst) inst.dispose();
                    }
                    targets.forEach(function (el) {
                        try {
                            var inst = bootstrap.Tooltip.getInstance(el);
                            if (inst) inst.dispose();
                        } catch (e) { /* already disposed */ }
                    });
                } catch (e) {
                    // Tooltip may already be disposed or element detached
                }
            });
        });

        clearTimeout(tooltipInitTimer);
        tooltipInitTimer = setTimeout(function () {
            window.initializeTooltips();
        }, 200);
    });

    // Start observing once DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        tooltipObserver.observe(document.body, {
            childList: true,
            subtree: true
        });
    });

    // Hide all Bootstrap tooltips when the user clicks anywhere
    document.addEventListener('click', function () {
        window.hideAllTooltips();
    }, true);

    // Hide all tooltips on Blazor enhanced navigation (popstate / pushState)
    window.addEventListener('popstate', function () { window.hideAllTooltips(); });
    // Intercept pushState/replaceState so SPA navigations also clean up
    ['pushState', 'replaceState'].forEach(function (method) {
        var original = history[method];
        history[method] = function () {
            window.hideAllTooltips();
            return original.apply(this, arguments);
        };
    });
    // Also hide when the page becomes hidden (tab switch, app switch on mobile)
    document.addEventListener('visibilitychange', function () {
        if (document.hidden) window.hideAllTooltips();
    });
})();

