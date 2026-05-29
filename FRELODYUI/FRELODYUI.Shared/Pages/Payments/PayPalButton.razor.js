// PayPal Smart Buttons interop.
//
// Loads the JS SDK on demand (once per page), then renders the buttons into a
// Blazor-owned container. All decisions (create order, capture) are delegated
// back to the .NET component so the server stays authoritative.

let sdkPromise = null;
let loadedClientId = null;

function loadSdk(clientId, currency) {
    // SDK is keyed by client-id + currency; if it's already on the page for the
    // same client, reuse it.
    if (window.paypal && loadedClientId === clientId) return Promise.resolve();
    if (sdkPromise && loadedClientId === clientId) return sdkPromise;

    loadedClientId = clientId;
    sdkPromise = new Promise((resolve, reject) => {
        const params = new URLSearchParams({
            'client-id': clientId,
            currency: currency || 'USD',
            intent: 'capture',
            components: 'buttons',
            'disable-funding': 'credit'
        });
        const s = document.createElement('script');
        s.src = `https://www.paypal.com/sdk/js?${params.toString()}`;
        s.async = true;
        s.onload = () => resolve();
        s.onerror = () => { sdkPromise = null; loadedClientId = null; reject(new Error('PayPal SDK failed to load')); };
        document.head.appendChild(s);
    });
    return sdkPromise;
}

// Track the rendered Buttons instance per container so we can close it cleanly.
const INSTANCES = new WeakMap();

export async function render(container, clientId, currency, dotNetRef) {
    if (!container || !clientId) throw new Error('Missing PayPal container or client id');
    await loadSdk(clientId, currency);
    if (!window.paypal) throw new Error('PayPal SDK unavailable');

    container.innerHTML = '';

    const buttons = window.paypal.Buttons({
        style: {
            layout: 'vertical',
            shape: 'pill',
            color: 'gold',
            label: 'paypal',
            height: 45,
            tagline: false
        },
        createOrder: async () => {
            const orderId = await dotNetRef.invokeMethodAsync('CreateOrderAsync');
            if (!orderId) throw new Error('Order creation failed');
            return orderId;
        },
        onApprove: async (data) => {
            await dotNetRef.invokeMethodAsync('OnApproveAsync', data.orderID);
        },
        onCancel: () => dotNetRef.invokeMethodAsync('OnCancelAsync'),
        onError: (err) => dotNetRef.invokeMethodAsync(
            'OnErrorAsync', err && err.message ? err.message : String(err))
    });

    INSTANCES.set(container, buttons);

    if (buttons.isEligible && !buttons.isEligible()) {
        throw new Error('PayPal buttons are not eligible in this context');
    }

    await buttons.render(container);
}

export function dispose(container) {
    const buttons = container && INSTANCES.get(container);
    if (buttons && typeof buttons.close === 'function') {
        try { buttons.close(); } catch { /* already torn down */ }
    }
    if (container) {
        INSTANCES.delete(container);
        container.innerHTML = '';
    }
}
