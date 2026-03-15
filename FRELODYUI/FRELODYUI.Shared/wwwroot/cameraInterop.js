// ── Camera Interop for Web (Blazor) ──
// Provides camera capture and gallery pick via hidden file inputs

window.cameraInterop = {
    /**
     * Opens the device camera (on mobile) or file picker (on desktop)
     * to capture a photo. Returns a base64-encoded image or null if cancelled.
     */
    capturePhoto: function () {
        return new Promise((resolve) => {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = 'image/*';
            input.capture = 'environment'; // rear camera on mobile

            input.onchange = async () => {
                if (input.files && input.files.length > 0) {
                    const file = input.files[0];
                    const reader = new FileReader();
                    reader.onload = () => {
                        resolve({
                            base64: reader.result.split(',')[1],
                            fileName: file.name
                        });
                    };
                    reader.onerror = () => resolve(null);
                    reader.readAsDataURL(file);
                } else {
                    resolve(null);
                }
            };

            // Handle cancel (user closes file dialog without selecting)
            // Use a focus listener as a fallback
            const onFocus = () => {
                setTimeout(() => {
                    if (!input.files || input.files.length === 0) {
                        resolve(null);
                    }
                    window.removeEventListener('focus', onFocus);
                }, 500);
            };
            window.addEventListener('focus', onFocus);

            input.click();
        });
    },

    /**
     * Opens the device gallery / file picker to select an existing image.
     * Returns a base64-encoded image or null if cancelled.
     */
    pickPhoto: function () {
        return new Promise((resolve) => {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = 'image/*';
            // No capture attribute — opens gallery/file picker

            input.onchange = async () => {
                if (input.files && input.files.length > 0) {
                    const file = input.files[0];
                    const reader = new FileReader();
                    reader.onload = () => {
                        resolve({
                            base64: reader.result.split(',')[1],
                            fileName: file.name
                        });
                    };
                    reader.onerror = () => resolve(null);
                    reader.readAsDataURL(file);
                } else {
                    resolve(null);
                }
            };

            const onFocus = () => {
                setTimeout(() => {
                    if (!input.files || input.files.length === 0) {
                        resolve(null);
                    }
                    window.removeEventListener('focus', onFocus);
                }, 500);
            };
            window.addEventListener('focus', onFocus);

            input.click();
        });
    }
};
