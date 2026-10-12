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

// Sidebar width, remembered the same way and for the same reasons. Collapsing
// is a working preference, not account state: someone on a laptop wants the
// icons-only rail on every page, and wants it back when they return.

const SIDEBAR_KEY = 'roivo.sidebar';

export function getSidebar() {
    try {
        return localStorage.getItem(SIDEBAR_KEY) === 'collapsed';
    } catch {
        return false;
    }
}

export function setSidebar(collapsed) {
    try {
        localStorage.setItem(SIDEBAR_KEY, collapsed ? 'collapsed' : 'expanded');
    } catch { /* the toggle still works for this page view */ }
}
