// Chord drawing JS interop for FRELODY.
// Wraps the svguitar npm package (loaded as ESM from esm.sh) for live SVG preview,
// plus a small DOM hit-test helper used by the Blazor editor during touch drag
// for barre chords (touchmove events do not target the element under the moving finger).

// jsdelivr's +esm endpoint bundles dependencies inline; esm.sh's version of svg.js
// drops the .viewbox() method which svguitar relies on.
const SVGUITAR_URL = 'https://cdn.jsdelivr.net/npm/svguitar@2.4.1/+esm';

let _svguitarModulePromise = null;
const _instances = new Map(); // elementId -> SVGuitarChord

function loadSvguitar() {
    if (!_svguitarModulePromise) {
        _svguitarModulePromise = import(SVGUITAR_URL).catch(err => {
            _svguitarModulePromise = null; // allow retry on next call
            throw err;
        });
    }
    return _svguitarModulePromise;
}

function mapShape(shape, Shape) {
    if (shape == null || !Shape) return undefined;
    if (typeof shape === 'string') {
        const k = shape.toLowerCase();
        if (k === 'circle') return Shape.CIRCLE;
        if (k === 'square') return Shape.SQUARE;
        if (k === 'triangle') return Shape.TRIANGLE;
        if (k === 'pentagon') return Shape.PENTAGON;
    }
    return undefined;
}

function adaptChart(chart, Shape) {
    const c = chart || {};
    const settings = c.settings || {};
    const chord = c.chord || { fingers: [], barres: [] };

    const fingers = (chord.fingers || []).map(f => {
        const opts = {};
        if (f.text) opts.text = f.text;
        if (f.color) opts.color = f.color;
        if (f.textColor) opts.textColor = f.textColor;
        const shape = mapShape(f.shape, Shape);
        if (shape !== undefined) opts.shape = shape;
        const fret = f.fret === -1 ? 'x' : f.fret; // svguitar uses 'x' for muted, 0 for open
        return Object.keys(opts).length > 0 ? [f.string, fret, opts] : [f.string, fret];
    });

    const barres = (chord.barres || []).map(b => ({
        fromString: b.fromString,
        toString: b.toString,
        fret: b.fret,
        ...(b.text ? { text: b.text } : {}),
        ...(b.color ? { color: b.color } : {})
    }));

    const adaptedSettings = {};
    if (settings.title) adaptedSettings.title = settings.title;
    if (settings.strings) adaptedSettings.strings = settings.strings;
    if (settings.frets) adaptedSettings.frets = settings.frets;
    if (settings.position && settings.position > 0) adaptedSettings.position = settings.position;
    if (settings.tuning && settings.tuning.length) adaptedSettings.tuning = settings.tuning;
    if (settings.orientation) adaptedSettings.orientation = settings.orientation;
    if (settings.style) adaptedSettings.style = settings.style;
    if (settings.color) adaptedSettings.color = settings.color;
    if (settings.backgroundColor) adaptedSettings.backgroundColor = settings.backgroundColor;
    if (settings.noPosition != null) adaptedSettings.noPosition = !!settings.noPosition;
    if (settings.showFretMarkers != null) adaptedSettings.showFretMarkers = !!settings.showFretMarkers;
    if (settings.fixedDiagramPosition != null) adaptedSettings.fixedDiagramPosition = !!settings.fixedDiagramPosition;

    return { fingers, barres, settings: adaptedSettings };
}

window.chordDrawing = {
    /**
     * Render a chord into the given element. Replaces any prior content.
     * @param {string} elementId
     * @param {object} chart - { chord: { fingers, barres }, settings: {...} }
     * @returns {Promise<boolean>}
     */
    async render(elementId, chart) {
        const el = document.getElementById(elementId);
        if (!el) return false;

        try {
            const mod = await loadSvguitar();
            const SVGuitarChord = mod.SVGuitarChord || mod.default;
            const Shape = mod.Shape;
            if (!SVGuitarChord) throw new Error('svguitar SVGuitarChord export not found');

            // Dispose prior instance
            const existing = _instances.get(elementId);
            if (existing && typeof existing.remove === 'function') {
                try { existing.remove(); } catch { /* ignore */ }
            }

            // Clear element
            while (el.firstChild) el.removeChild(el.firstChild);

            const adapted = adaptChart(chart, Shape);
            const instance = new SVGuitarChord(el);
            instance.configure(adapted.settings).chord({ fingers: adapted.fingers, barres: adapted.barres });
            instance.draw();
            _instances.set(elementId, instance);
            return true;
        } catch (err) {
            console.error('chordDrawing.render failed', err);
            // Leave the element empty so server-rendered SVG fallback can be shown by caller
            return false;
        }
    },

    /**
     * Remove a rendered chord and release resources.
     */
    dispose(elementId) {
        const inst = _instances.get(elementId);
        if (inst && typeof inst.remove === 'function') {
            try { inst.remove(); } catch { /* ignore */ }
        }
        _instances.delete(elementId);
        const el = document.getElementById(elementId);
        if (el) while (el.firstChild) el.removeChild(el.firstChild);
    },

    /**
     * Hit-test helper for touch drag. Given a clientX/clientY (from a touchmove),
     * returns { string, fret } if the point is over a `[data-string][data-fret]` cell
     * inside the editor with id `containerId`, otherwise null.
     */
    hitTestCell(containerId, clientX, clientY) {
        const container = document.getElementById(containerId);
        if (!container) return null;
        const target = document.elementFromPoint(clientX, clientY);
        if (!target) return null;
        const cell = target.closest('[data-string][data-fret]');
        if (!cell || !container.contains(cell)) return null;
        const s = parseInt(cell.getAttribute('data-string'), 10);
        const f = parseInt(cell.getAttribute('data-fret'), 10);
        if (isNaN(s) || isNaN(f)) return null;
        return { string: s, fret: f };
    },

    /**
     * Returns the serialized SVG markup of the current rendering (for downloads).
     */
    getSvgMarkup(elementId) {
        const el = document.getElementById(elementId);
        if (!el) return null;
        const svg = el.querySelector('svg');
        if (!svg) return null;
        const clone = svg.cloneNode(true);
        clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
        return new XMLSerializer().serializeToString(clone);
    },

    /**
     * Trigger a client-side download of the rendered SVG as a PNG of the given pixel width.
     * Returns true on success.
     */
    async downloadPng(elementId, width, filename) {
        const markup = this.getSvgMarkup(elementId);
        if (!markup) return false;

        try {
            const svgBlob = new Blob([markup], { type: 'image/svg+xml;charset=utf-8' });
            const url = URL.createObjectURL(svgBlob);
            const img = new Image();
            img.crossOrigin = 'anonymous';
            const loaded = new Promise((resolve, reject) => {
                img.onload = () => resolve();
                img.onerror = (e) => reject(e);
            });
            img.src = url;
            await loaded;
            const ratio = img.naturalHeight / img.naturalWidth || 1;
            const canvas = document.createElement('canvas');
            canvas.width = width;
            canvas.height = Math.round(width * ratio);
            const ctx = canvas.getContext('2d');
            ctx.fillStyle = '#ffffff';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
            URL.revokeObjectURL(url);
            const pngUrl = canvas.toDataURL('image/png');
            const a = document.createElement('a');
            a.href = pngUrl;
            a.download = (filename || 'chord') + '.png';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            return true;
        } catch (err) {
            console.error('chordDrawing.downloadPng failed', err);
            return false;
        }
    }
};
