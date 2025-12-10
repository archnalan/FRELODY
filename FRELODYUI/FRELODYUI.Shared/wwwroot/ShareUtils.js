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

// ============================================
// AUTOSCROLL FUNCTIONALITY
// ============================================

/**
 * Smooth scroll by a specific number of pixels
 * @param {number} pixels - Number of pixels to scroll
 */
window.smoothScrollBy = function (pixels) {
    window.scrollBy({
        top: pixels,
        left: 0,
        behavior: 'smooth'
    });
};

/**
 * Scroll to top of the page
 */
window.scrollToTop = function () {
    window.scrollTo({
        top: 0,
        left: 0,
        behavior: 'smooth'
    });
};

/**
 * Scroll to bottom of the page
 */
window.scrollToBottom = function () {
    window.scrollTo({
        top: document.documentElement.scrollHeight,
        left: 0,
        behavior: 'smooth'
    });
};

/**
 * Get current scroll position
 * @returns {object} Object with scrollTop and scrollHeight
 */
window.getScrollPosition = function () {
    return {
        scrollTop: window.pageYOffset || document.documentElement.scrollTop,
        scrollHeight: document.documentElement.scrollHeight,
        clientHeight: document.documentElement.clientHeight
    };
};

/**
 * Check if user has scrolled to bottom
 * @returns {boolean} True if at bottom
 */
window.isAtBottom = function () {
    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
    const scrollHeight = document.documentElement.scrollHeight;
    const clientHeight = document.documentElement.clientHeight;
    
    return (scrollTop + clientHeight) >= (scrollHeight - 50); // 50px threshold
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
// INITIALIZE ON LOAD
// ============================================

(function () {
    // Prevent settings dropdown from being hidden behind other elements
    document.addEventListener('DOMContentLoaded', function () {
        // Ensure popovers and dropdowns have proper z-index
        const style = document.createElement('style');
        style.textContent = `
            .settings-dropdown-overlay {
                position: fixed !important;
                z-index: 9999 !important;
            }
            .chord-popover-overlay {
                position: fixed !important;
                z-index: 10000 !important;
            }
        `;
        document.head.appendChild(style);
    });
})();