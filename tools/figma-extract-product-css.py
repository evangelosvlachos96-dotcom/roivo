"""Slice the product design system out of the Figma Make stylesheet.

The generated sheet is, in order:
  a Tailwind v4 preamble (reset + utility layers) — the design barely uses
    utilities, and importing it would fight MudBlazor's own reset;
  the product: shell, sidebar, header, cards, every workspace;
  landing page, auth modals, email gallery — Phase 2 backlog;
  media queries for both halves, interleaved.

We take the product range verbatim, then take only the media-query rules whose
selectors belong to it, so the responsive behaviour survives without dragging
in the backlog styles.

The boundaries are found by their anchors rather than written down as line
numbers. They were line numbers once, and the first design update moved all
three — the extractor went on running and quietly sliced the wrong range.
"""
import io
import re

SRC = 'roivo.css'
OUT = 'roivo-app.css'

# The product section opens with the :root rule carrying the typeface and ends
# where the landing page begins; the media blocks follow both halves.
PRODUCT_START_RE = re.compile(r'^:root \{$')
PRODUCT_END_RE = re.compile(r'^\.landing \{$')
MEDIA_START_RE = re.compile(r'^@media ')


def boundaries(lines):
    """The 1-based inclusive product range, and where the media blocks start."""

    def find(pattern, what, start=0):
        for index in range(start, len(lines)):
            if pattern.match(lines[index]):
                return index + 1
        raise SystemExit('could not find %s — the design source changed shape' % what)

    start = find(PRODUCT_START_RE, 'the product :root rule')
    end = find(PRODUCT_END_RE, 'the start of the landing page', start) - 1
    media = find(MEDIA_START_RE, 'the first media query', end)

    return start, end, media

# `plan-badge|plan-index|plan-price` are the landing page's pricing table. The
# design later added `.plan-card` for the subscription panel in Settings, which
# is product and must come through — so these are named rather than matched on
# a `plan` prefix. `brand-mark` is the sidebar lockup and is product too; it was
# listed here while the shell was still being built.
BACKLOG = re.compile(
    r'^\s*\.(relanding|landing|auth-|open-r|email-|wordmark-'
    r'|plan-badge|plan-index|plan-price|hero-)',
    re.I)

# Global rules the design hangs off :root / body. They would reach the existing
# landing and Account pages, which keep their own look in Phase 1, so they are
# dropped here and re-hung on `.app` below. That makes this whole stylesheet
# incapable of affecting markup outside the new shell.
GLOBAL_RULES = [
    ':root {\n  font-family: "DM Sans", sans-serif;\n  font-synthesis: none;\n  color-scheme: light;\n}\n',
    '* {\n  box-sizing: border-box;\n}\n',
    'body {\n  margin: 0;\n  min-width: 320px;\n  min-height: 100vh;\n}\n',
    'button, input, select {\n  font: inherit;\n}\n',
    'button {\n  cursor: pointer;\n}\n',
]


# ---------------------------------------------------------------- type scale
#
# The design is drawn small: 104 of its 130 font declarations are between 5px
# and 9px, and body copy is 11px. Faithfully reproduced, that is what shipped —
# and it is not readable in a product people use all day to read numbers.
#
# So every size is lifted through this map. It is deliberately not a single
# multiplier: the smallest labels need the most help (5px -> 9px is x1.8) while
# headings are already fine (anything 19px and over is left alone), and a flat
# factor would either leave the micro-labels illegible or blow the headings out.
# Scaling here rather than by hand keeps the whole ramp in one reviewable place
# and survives a re-extract.
#
# This is the one place the implementation knowingly departs from the Figma.
TYPE_SCALE = {
    5: 9, 6: 10, 7: 11, 8: 12, 9: 13, 10: 14,
    11: 15, 12: 16, 13: 17, 14: 17, 16: 18, 17: 19, 18: 20,
}


def scale_type(css):
    """Lift every font-size, including the `font:` shorthand's."""
    def one(match):
        size = int(match.group('size'))
        return match.group(0).replace(
            match.group('size') + 'px', '%dpx' % TYPE_SCALE.get(size, size), 1)

    css = re.sub(r'font-size:\s*(?P<size>\d+)px', one, css)
    css = re.sub(r'font:\s*[^;]*?\b(?P<size>\d+)px(?=[/ ;])', one, css)
    return css


SCOPED_PREAMBLE = (
    '.app, .app * {\n  box-sizing: border-box;\n}\n'
    '.app button, .app input, .app select {\n  font: inherit;\n}\n'
    '.app button {\n  cursor: pointer;\n}\n'
)

# --------------------------------------------------------- local corrections
#
# Rules the design does not have, for cases its own preview never reached.
# Appended after the generated CSS so they win, and kept here so a re-extract
# does not drop them.
LOCAL_OVERRIDES = """

/* -----------------------------------------------------------------------------
   Local corrections. Not from the Figma source.
   ----------------------------------------------------------------------------- */

/* The design's mark is cropped out of a photograph of the logo, so it zooms the
   image to 185% and offsets it to centre the glyph. Our asset is already the
   tightly-cropped squircle, so that same rule zooms into an image that has no
   margin left to crop. Fill the box instead. */
.brand-mark img {
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
}

/* The sidebar is a fixed 100vh column with no scroll. The design's demo has two
   workspace entries; a real accountant has dozens, and the overflow spilled out
   of the dark panel as a pale block below the fold. */
.app-sidebar {
    overflow-y: auto;
    overscroll-behavior: contain;

    /* A scrollable box computes overflow-x: auto too, and a long client name
       is enough to earn a horizontal scrollbar across the bottom of the
       sidebar. There is nothing to reach sideways, so there is nothing to
       scroll to. */
    overflow-x: hidden;
}

/* `.widget-heading > div` is display:grid and, at one class plus one element,
   out-specifies `.match-actions`. Inside a widget heading that stacked the
   report export buttons into a column instead of a row. */
.widget-heading > .match-actions {
    display: flex;
    margin-top: 0;
    align-items: center;
    gap: 7px;
}

/* Knock-on from the type scale above. In the mobile tab bar the design pins the
   count badge to the nav item's top-right corner, sized for its own 7px type.
   Scaled to 11px the badge grew enough to sit on the icon rather than beside
   it, so it is pulled out and trimmed back here. */
@media (max-width: 820px) {
    .nav-item b {
        top: -3px;
        right: 12%;
        min-width: 17px;
        height: 16px;
        padding: 0 4px;
        font-size: 10px;
    }
}

/* New-device banner. The design has no equivalent, so it is built from the
   design's own vocabulary: the amber of the PREMIUM pill for the warning tone,
   the connection-health banner's shape, and the standard card radius. */
.device-alert {
    display: grid;
    grid-template-columns: auto 1fr auto;
    align-items: center;
    gap: 14px;
    margin-bottom: 22px;
    padding: 14px 16px;
    border: 1px solid rgba(217, 119, 6, .28);
    border-radius: 12px;
    background: #fff8e8;
    animation: device-alert-in .22s ease-out;
}

.app[data-theme="dark"] .device-alert {
    border-color: rgba(225, 183, 72, .24);
    background: rgba(225, 183, 72, .09);
}

.device-alert.is-leaving {
    animation: device-alert-out .18s ease-in forwards;
}

.device-alert-icon {
    display: grid;
    width: 34px;
    height: 34px;
    place-items: center;
    border-radius: 9px;
    color: #a86a08;
    background: rgba(217, 119, 6, .14);
}

.app[data-theme="dark"] .device-alert-icon {
    color: #e5c36e;
}

.device-alert > div:nth-child(2) {
    display: grid;
    gap: 3px;
}

.device-alert strong {
    color: #7c4a02;
    font-size: 13px;
}

.device-alert span {
    color: #9a6c22;
    font-size: 11px;
}

.app[data-theme="dark"] .device-alert strong {
    color: #f0d9a0;
}

.app[data-theme="dark"] .device-alert span {
    color: #c9a85f;
}

.device-alert-actions {
    display: flex;
    align-items: center;
    gap: 8px;
}

@keyframes device-alert-in {
    from { opacity: 0; transform: translateY(-8px); }
}

@keyframes device-alert-out {
    to { opacity: 0; transform: translateY(-8px); }
}

/* Someone who turns animation down should not be made to watch it. */
@media (prefers-reduced-motion: reduce) {
    .device-alert, .device-alert.is-leaving {
        animation: none;
    }
}

/* Portfolio: table on wide screens, cards on narrow.

   The design offers these as a view the user picks, so its CSS never switches
   between them — and its preview is 979px wide, where the table always fits. At
   380px the table is 760px and the page scrolled sideways. Both layouts are the
   design's; only this choice between them is ours. */
@media (max-width: 820px) {
    .company-table-wrap {
        display: none;
    }
}

@media (min-width: 821px) {
    .company-cards {
        display: none;
    }
}

/* MudBlazor's reset puts `justify-content: center` on every button. The design
   was authored against the browser default and only states the property when it
   wants centring, so every button it lays out as a row — the workspace switcher
   worst of all — came out centred instead of left aligned.

   `.app` is wrapped in :where() so this weighs exactly the same as MudBlazor's
   bare `button` — this sheet now loads after MudBlazor, so it wins the tie —
   while any design rule carrying a class, `.secondary-button` among them, still
   outranks it and keeps its own centring. */
:where(.app) button {
    justify-content: flex-start;
}

/* Settings. The design hangs its width cap on `settings-content`, a class on
   the page's own <main>; ours is the shared shell's, so the cap is hung on the
   content column when a settings layout is what it is holding. */
.content:has(> .settings-layout) {
    max-width: 1200px;
}

/* The design's panel holds one heading, so it styles the sub-line as
   `> div:first-child p`. Ours holds a few sections — profile, password,
   devices — so the same declarations are generalised to every heading block. */
.settings-panel > .settings-section {
    margin-top: 26px;
}

.settings-panel > .settings-section:first-child {
    margin-top: 0;
}

.settings-panel > .settings-section > p {
    margin-top: 6px;
    color: var(--text-faint);
    font-size: 11px;
}

/* A read-only value where the design has an input: same box, same rhythm, but
   visibly not something you can type into. */
.settings-static {
    display: flex;
    /* Height rather than a fixed one: the email and its pill sit side by side
       on a desktop and wrap onto two lines on a phone. */
    min-height: 37px;
    padding: 6px 10px;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
    border: 1px solid var(--line-soft);
    border-radius: 8px;
    color: var(--text-soft);
    background: var(--surface-subtle);
    font-size: 12px;
}

/* The design's save bar shows only the green confirmation. A failure has to
   say something too, and in the tone of the figures it sits beside. */
.settings-save > span.is-error {
    color: #cf6d5f;
}

/* Device and member rows reuse `.cash-event`, which is built for four columns.
   With two text columns and no figure the spare track has to come out. */
.settings-panel .cash-event {
    grid-template-columns: auto 1fr auto;
}

.settings-panel .cash-event:last-child {
    border-bottom: 0;
}

/* `.cash-event > span` wins on specificity, so the avatar's own size is lost
   here; its gradient and weight carry through, which is the part that reads. */
.settings-panel .cash-event > span.avatar {
    color: #06223b;
}

/* Stacked, the design writes `grid-template-columns: 1fr`, which is really
   `minmax(auto, 1fr)`. The section list beside it turns into a scrolling flex
   row there, and its min-content width — four buttons side by side — then sets
   the track, dragging the panel off the right of a phone screen. Naming the
   zero minimum lets the list scroll inside its track instead. */
@media (max-width: 1180px) {
    .settings-layout {
        grid-template-columns: minmax(0, 1fr);
    }
}

@media (max-width: 560px) {
    .settings-panel .cash-event {
        grid-template-columns: auto 1fr;
    }

    .settings-panel .cash-event > button {
        grid-column: 2;
        justify-self: start;
    }
}
"""

BANNER = """/* =============================================================================
   Roivo product design system.

   Ported from the Figma Make project by tools/figma/extract_product_css.py, so
   the running app and the design cannot drift through a hand transcription.
   Only the product half is here: the landing page, auth modals and email
   gallery stay on their current design until Phase 2.

   Both palettes live on `.app` and `.app[data-theme="dark"]`, so switching
   theme is one attribute on the shell element and every rule below follows
   from the custom properties.

   The design assumed its reset and typeface on :root; both are re-hung on
   `.app` here so nothing in this file can reach the pages Phase 1 leaves alone.

   Do not hand-edit: re-run the extractor against the Figma source instead.
   ============================================================================= */

"""


def main():
    lines = io.open(SRC, encoding='utf-8').read().split('\n')
    product_start, product_end, media_start = boundaries(lines)
    product = '\n'.join(lines[product_start - 1:product_end])

    for rule in GLOBAL_RULES:
        if rule not in product:
            raise SystemExit('global rule not found, the source moved: %r' % rule[:32])
        product = product.replace(rule, '', 1)

    product = SCOPED_PREAMBLE + product.lstrip('\n')

    anchor = '.app {\n  --bg: #f4f7fa;'
    if anchor not in product:
        raise SystemExit('could not find the .app palette block')
    product = product.replace(
        anchor,
        '.app {\n  font-family: "DM Sans", sans-serif;\n  font-synthesis: none;\n  --bg: #f4f7fa;',
        1)

    kept_media = []
    media_src = '\n'.join(lines[media_start - 1:])

    for mq in re.finditer(r'(@media[^{]+)\{(.*?)\n\}', media_src, re.S):
        header, body = mq.group(1).strip(), mq.group(2)
        rules = re.findall(r'([^{}]+)\{([^{}]*)\}', body)
        keep = [(sel.strip(), decls) for sel, decls in rules
                if not BACKLOG.match(sel.strip())]
        if keep:
            kept_media.append('%s {\n%s\n}' % (header, '\n'.join(
                '  %s {%s}' % (sel, decls.rstrip()) for sel, decls in keep)))

    body = product.rstrip() + '\n\n' + '\n\n'.join(kept_media) + '\n'
    body = scale_type(body)

    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(
        BANNER + body + LOCAL_OVERRIDES)

    text = io.open(OUT, encoding='utf-8').read()
    leaked = [l for l in text.split('\n') if BACKLOG.match(l)]
    print('product lines %d-%d, media from %d'
          % (product_start, product_end, media_start))
    print('%s: %d bytes, %d lines, %d media blocks'
          % (OUT, len(text), text.count('\n') + 1, len(kept_media)))
    print('backlog selectors leaked: %d%s'
          % (len(leaked), (' -> ' + ', '.join(s.strip()[:40] for s in leaked[:5])) if leaked else ''))


if __name__ == '__main__':
    main()
