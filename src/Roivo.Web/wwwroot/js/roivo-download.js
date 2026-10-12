// Hands the browser a generated file.
//
// A global rather than a module so a Razor page can call it with one
// InvokeVoidAsync and no import plumbing. Blazor Server has no filesystem on
// the client, so an in-memory blob and a synthetic click is the only route.
window.roivoDownload = function (fileName, contentType, content) {
    try {
        // The BOM is not decoration: Excel on Windows reads a UTF-8 CSV as
        // Windows-1253 without it, and every Greek label arrives as mojibake.
        const blob = new Blob(['﻿' + content], { type: contentType });
        const url = URL.createObjectURL(blob);

        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();

        // Revoking immediately can cancel the download in some browsers.
        setTimeout(() => URL.revokeObjectURL(url), 10000);
    } catch (error) {
        console.error('[roivo] download failed', error);
    }
};
