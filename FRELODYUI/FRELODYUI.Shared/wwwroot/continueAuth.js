// Google Identity Services (One Tap) bridge for the "Continue as …" prompt.
// Loads the gsi client on demand and surfaces the signed credential (ID token)
// back to .NET, which verifies it server-side via /api/authorization/google-one-tap.

let _dotNet = null;

function loadGis() {
    return new Promise((resolve, reject) => {
        if (window.google?.accounts?.id) { resolve(); return; }

        const existing = document.getElementById('gis-client-script');
        if (existing) {
            existing.addEventListener('load', () => resolve());
            existing.addEventListener('error', () => reject(new Error('GIS load failed')));
            return;
        }

        const s = document.createElement('script');
        s.id = 'gis-client-script';
        s.src = 'https://accounts.google.com/gsi/client';
        s.async = true;
        s.defer = true;
        s.onload = () => resolve();
        s.onerror = () => reject(new Error('GIS load failed'));
        document.head.appendChild(s);
    });
}

// Initialize One Tap and show the prompt. loginHint (optional) biases Google to a
// specific account. Best-effort: any failure is swallowed so the page is never blocked.
export async function initOneTap(clientId, dotNetRef, loginHint) {
    if (!clientId) return;
    _dotNet = dotNetRef;
    try {
        await loadGis();
        const config = {
            client_id: clientId,
            auto_select: false,
            cancel_on_tap_outside: true,
            callback: (resp) => {
                if (_dotNet && resp && resp.credential) {
                    _dotNet.invokeMethodAsync('OnGoogleCredential', resp.credential);
                }
            }
        };
        if (loginHint) config.login_hint = loginHint;

        window.google.accounts.id.initialize(config);
        window.google.accounts.id.prompt();
    } catch (_) {
        /* prompt is a progressive enhancement */
    }
}

export function cancel() {
    try { window.google?.accounts?.id?.cancel(); } catch (_) { }
    _dotNet = null;
}
