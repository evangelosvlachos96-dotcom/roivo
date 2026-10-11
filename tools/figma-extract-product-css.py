"""Slice the product design system out of the Figma Make stylesheet.

The generated sheet is, in order:
  1-204     Tailwind v4 preamble (reset + utility layers) — the design barely
            uses utilities, and importing it would fight MudBlazor's own reset.
  205-2536  the product: shell, sidebar, header, cards, every workspace.
  2537-4634 landing page, auth modals, email gallery — Phase 2 backlog.
  4635-end  media queries for both halves, interleaved.

We take the product range verbatim, then take only the media-query rules whose
selectors belong to it, so the responsive behaviour survives without dragging
in the backlog styles.
"""
import io
import re

SRC = 'roivo.css'
OUT = 'roivo-app.css'

PRODUCT_START, PRODUCT_END = 205, 2536     # 1-based, inclusive
MEDIA_START = 4635

BACKLOG = re.compile(
    r'^\s*\.(relanding|landing|auth-|open-r|email-|wordmark-|plan\b|hero-|brand-mark)',
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
    product = '\n'.join(lines[PRODUCT_START - 1:PRODUCT_END])

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
    media_src = '\n'.join(lines[MEDIA_START - 1:])

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
    print('%s: %d bytes, %d lines, %d media blocks'
          % (OUT, len(text), text.count('\n') + 1, len(kept_media)))
    print('backlog selectors leaked: %d%s'
          % (len(leaked), (' -> ' + ', '.join(s.strip()[:40] for s in leaked[:5])) if leaked else ''))


if __name__ == '__main__':
    main()
