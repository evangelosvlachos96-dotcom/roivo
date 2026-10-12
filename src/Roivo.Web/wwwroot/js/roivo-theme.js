// Theme persistence for the product shell.
//
// The choice is per-viewer convenience, so it lives in localStorage rather than
// on the server: it must survive a reload and apply before first paint, and
// neither is worth a round trip or a column.
//
// Every accessor is guarded. localStorage throws outright in a private window
// with site data blocked, and a theme toggle must never be the thing that
// breaks a page.

const KEY = 'roivo.theme';

export function get() {
    try {
        const stored = localStorage.getItem(KEY);
        if (stored === 'dark' || stored === 'light') return stored;
    } catch { /* blocked or unavailable; fall through to the system preference */ }

    try {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    } catch {
        return 'light';
    }
}

export function set(theme) {
    try {
        localStorage.setItem(KEY, theme);
    } catch { /* the toggle still works for this page view */ }
}
