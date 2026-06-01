// =========================================================
// FRELODY Docs — PDF export (browser-native print-to-PDF).
//
// Exposes window.frelodyPdf with two helpers used from C# via IJSRuntime:
//   getArticleHtml(): returns the .doc-article innerHTML for the current page.
//   printDocument({ title, bodyHtml }): opens a hidden iframe, writes a
//     self-contained printable document, waits for images, and triggers the
//     browser's print dialog (where users choose "Save as PDF").
//
// We deliberately avoid heavy client-side PDF libraries (html2canvas, jsPDF):
//   - Native print-to-PDF preserves selectable text, links, and headings.
//   - No extra MB downloaded.
//   - Works the same in Chrome, Edge, Firefox, Safari.
// =========================================================
(function () {
    "use strict";

    function escapeHtml(s) {
        return (s || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function buildPrintableHtml(title, bodyHtml) {
        // Inline minimal print CSS so the iframe is self-contained and is not
        // affected by the host app's theme (dark mode, sidebars, etc).
        var css = [
            "@page { size: A4; margin: 18mm 16mm 20mm 16mm; }",
            "* { box-sizing: border-box; }",
            "html, body { margin: 0; padding: 0; background: #ffffff; color: #1f2430; }",
            "body { font-family: 'Kanit', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; font-size: 11pt; line-height: 1.55; }",
            ".pdf-cover { text-align: center; padding: 40mm 0 30mm; page-break-after: always; }",
            ".pdf-cover h1 { font-size: 28pt; margin: 0 0 8mm; color: #4f46e5; }",
            ".pdf-cover .pdf-cover-sub { font-size: 13pt; color: #5b6678; margin: 0 0 6mm; }",
            ".pdf-cover .pdf-cover-meta { font-size: 10pt; color: #8a94a6; margin-top: 12mm; }",
            ".pdf-toc { page-break-after: always; }",
            ".pdf-toc h2 { font-size: 18pt; color: #4f46e5; border-bottom: 2px solid #4f46e5; padding-bottom: 4mm; }",
            ".pdf-toc ul { list-style: none; padding-left: 0; margin: 0; }",
            ".pdf-toc .toc-section { margin: 4mm 0 1mm; font-weight: 700; color: #4f46e5; }",
            ".pdf-toc .toc-page { font-size: 11pt; margin: 0.5mm 0; padding-left: 5mm; }",
            ".pdf-toc a { color: #4f46e5; text-decoration: none; }",
            ".pdf-page { page-break-before: always; }",
            ".pdf-page:first-of-type { page-break-before: auto; }",
            ".pdf-page-header { font-size: 9pt; color: #8a94a6; text-transform: uppercase; letter-spacing: 0.08em; margin-bottom: 2mm; }",
            "h1 { font-size: 22pt; color: #4f46e5; margin: 0 0 4mm; page-break-after: avoid; }",
            "h2 { font-size: 16pt; color: #4f46e5; margin: 6mm 0 3mm; page-break-after: avoid; }",
            "h3 { font-size: 13pt; color: #4338ca; margin: 5mm 0 2mm; page-break-after: avoid; }",
            "h4, h5, h6 { color: #4338ca; margin: 4mm 0 2mm; page-break-after: avoid; }",
            "p { margin: 0 0 3mm; }",
            "ul, ol { margin: 0 0 4mm; padding-left: 6mm; }",
            "li { margin: 1mm 0; }",
            "blockquote { border-left: 3px solid #4f46e5; background: #eef2ff; padding: 3mm 4mm; margin: 3mm 0; color: #312a6b; }",
            "code { background: #eef2f7; padding: 0.5mm 1.5mm; border-radius: 1mm; font-family: 'JetBrains Mono', Consolas, monospace; font-size: 9.5pt; }",
            "pre { background: #f6f8fb; border: 1px solid #e5e7eb; padding: 3mm 4mm; border-radius: 2mm; overflow-x: auto; font-size: 9.5pt; page-break-inside: avoid; }",
            "pre code { background: transparent; padding: 0; }",
            "table { border-collapse: collapse; width: 100%; margin: 3mm 0; font-size: 10pt; page-break-inside: avoid; }",
            "th, td { border: 1px solid #cdd5e0; padding: 2mm 3mm; text-align: left; vertical-align: top; }",
            "th { background: #eef2f7; color: #4338ca; }",
            "a { color: #4f46e5; text-decoration: underline; }",
            "img { max-width: 100%; height: auto; page-break-inside: avoid; }",
            // Reproduce the doc-site image fallback behaviour.
            ".img-frame { position: relative; width: 100%; aspect-ratio: 16 / 9; background: #eef2f7; border: 1px solid #cdd5e0; border-radius: 2mm; overflow: hidden; margin: 3mm 0; page-break-inside: avoid; }",
            ".img-frame > img { position: absolute; inset: 0; width: 100%; height: 100%; object-fit: cover; }",
            ".img-frame-placeholder { display: flex; align-items: center; justify-content: center; height: 100%; color: #8a94a6; font-style: italic; padding: 4mm; text-align: center; }",
            // Video embeds collapse to a printable note (iframes don't print).
            ".video-embed { display: none; }"
        ].join("\n");

        var safeTitle = escapeHtml(title || "FRELODY Documentation");
        var baseHref = (window.location && window.location.origin) ? window.location.origin + "/" : "/";
        return "<!doctype html>" +
            "<html lang=\"en\"><head>" +
            "<meta charset=\"utf-8\" />" +
            "<base href=\"" + baseHref + "\" />" +
            "<title>" + safeTitle + "</title>" +
            "<link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">" +
            "<link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>" +
            "<link href=\"https://fonts.googleapis.com/css2?family=Kanit:wght@400;500;600;700&family=JetBrains+Mono&display=swap\" rel=\"stylesheet\">" +
            "<style>" + css + "</style>" +
            "</head><body>" + bodyHtml + "</body></html>";
    }

    function waitForImages(doc) {
        var imgs = Array.prototype.slice.call(doc.images || []);
        if (imgs.length === 0) return Promise.resolve();
        var pending = imgs.map(function (img) {
            if (img.complete) return Promise.resolve();
            return new Promise(function (resolve) {
                img.addEventListener("load", resolve, { once: true });
                img.addEventListener("error", resolve, { once: true });
                setTimeout(resolve, 4000);
            });
        });
        return Promise.all(pending);
    }

    function printViaIframe(html) {
        return new Promise(function (resolve) {
            var iframe = document.createElement("iframe");
            iframe.setAttribute("aria-hidden", "true");
            iframe.style.position = "fixed";
            iframe.style.right = "0";
            iframe.style.bottom = "0";
            iframe.style.width = "0";
            iframe.style.height = "0";
            iframe.style.border = "0";
            iframe.style.opacity = "0";

            iframe.onload = function () {
                try {
                    var win = iframe.contentWindow;
                    var doc = iframe.contentDocument;
                    waitForImages(doc).then(function () {
                        setTimeout(function () {
                            try {
                                win.focus();
                                win.print();
                            } catch (e) { /* ignore */ }
                            setTimeout(function () {
                                if (iframe.parentNode) iframe.parentNode.removeChild(iframe);
                                resolve();
                            }, 1500);
                        }, 250);
                    });
                } catch (e) {
                    if (iframe.parentNode) iframe.parentNode.removeChild(iframe);
                    resolve();
                }
            };

            document.body.appendChild(iframe);
            var doc = iframe.contentDocument || iframe.contentWindow.document;
            doc.open();
            doc.write(html);
            doc.close();
        });
    }

    window.frelodyPdf = {
        getArticleHtml: function () {
            var el = document.querySelector(".doc-article");
            return el ? el.innerHTML : "";
        },

        printDocument: function (payload) {
            payload = payload || {};
            var html = buildPrintableHtml(payload.title, payload.bodyHtml || "");
            return printViaIframe(html);
        }
    };
})();
