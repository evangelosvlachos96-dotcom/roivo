# Logo options

Working document for the Roivo brand mark. Nothing here is final — this records the directions considered and why, so the decision doesn't have to be re-argued later.

## Concept

The name reads on two levels. *ROI* is the financial return; *ροή* is Greek for **flow**. Roivo is cashflow software for a Greek market, so both readings land. Every direction below is built on flow rather than on the usual fintech vocabulary of upward arrows, wallets, and shields.

## Directions

### 1. Confluence — recommended

Two strokes enter from the left, converge, and leave as a single thicker stroke in the accent colour.

This is the product's actual function drawn literally: PSD2 banking data and myDATA tax data arrive separately and leave reconciled. The accent colour marks the reconciled output, so colour carries meaning instead of decoration.

It also survives scaling. At 16px favicon size it stays readable as a distinct shape, which the other two do not.

### 2. Channel R

An R monogram where the leg breaks from the letterform and flows forward.

Safest for recognition, since the letterform anchors it immediately. The weakness is that the flowing leg makes an ambiguous R at small sizes — it stops reading as a letter and starts reading as a squiggle.

### 3. Flow node

A circle with a rising line passing through it, terminating in a dot.

Weakest of the three. A rising line through a circle is close to generic fintech, and the terminal dot risks reading as a map pin rather than a data point.

## Colours

| Role | Hex | Notes |
|---|---|---|
| Ink | `#153542` | Deep slate. Carries the mark and wordmark. |
| Ink (dark mode) | `#E4ECEE` | Swapped via `prefers-color-scheme`. |
| Accent | `#1F9D6B` | The reconciled stream. Unchanged in both modes. |

Green because positive cashflow is the product's promise. The deep slate keeps it from reading as a crypto app, which a brighter palette would.

## Typography

The wordmark is set lowercase, medium weight, with slightly tightened letter-spacing. Lowercase keeps it approachable for small business owners rather than institutional.

The current SVGs use a font stack with Helvetica and Arial fallbacks, so the wordmark will render differently across machines. **Before any official use, convert the type to outlines** so it is identical everywhere and does not depend on installed fonts.

## Files

| File | Contents |
|---|---|
| `roivo-confluence-logo.svg` | The recommended mark plus wordmark, tightly cropped. |
| `roivo-logo-concepts.svg` | All three directions side by side, captioned. |

Both are self-contained and adapt to dark backgrounds automatically.

## Outstanding

- Convert wordmark type to paths
- Export PNG at favicon sizes (16, 32, 180, 512)
- Produce a horizontal lockup (mark left, wordmark right) for site headers
- Decide whether the mark works standalone, without the wordmark, as an app icon
- Check the mark does not collide with existing marks in Greek fintech before committing to it
