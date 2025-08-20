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


window.getTabIndexOrDefault = (el) => {
    if (!el) return 0;

    // If element has explicit tabIndex, use that
    if (el.tabIndex && el.tabIndex > 0) {
        return el.tabIndex;
    }

    // Otherwise, return a safe base number
    return 1000; // big enough so we don't clash with native flow
};