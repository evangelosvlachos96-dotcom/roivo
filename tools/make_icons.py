"""Derive the favicon and PWA icons from the brand app icon.

The source JPEG is the mark photographed on a light board, complete with a drop
shadow, so it cannot be resized directly: the corners would carry that grey
into every icon. This crops to the squircle, masks the corners to transparency,
and writes the sizes each platform actually asks for.

Run from the repo root: python tools/make_icons.py
"""
import io
from PIL import Image, ImageDraw

SOURCE = 'src/Roivo.Web/wwwroot/images/app-icon.jpeg'
OUT = 'src/Roivo.Web/wwwroot'

# Bounds of the squircle inside the source, found by scanning for the dark
# navy field (see the row/column profile in tools/).
CROP = (55, 35, 194, 174)

# Supersample before masking so the rounded corner stays smooth at 32px.
WORK = 1024

# iOS-style corner radius: a little under a quarter of the side.
RADIUS_RATIO = 0.225

# Android masks maskable icons down to a circle inscribed in the middle 80%,
# so the mark is inset to survive the worst-case crop.
MASKABLE_SAFE = 0.72


def squircle(size, radius_ratio=RADIUS_RATIO):
    """An anti-aliased rounded-square alpha mask."""
    mask = Image.new('L', (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [(0, 0), (size - 1, size - 1)],
        radius=int(size * radius_ratio),
        fill=255)
    return mask


def main():
    source = Image.open(SOURCE).convert('RGB').crop(CROP)
    side = min(source.size)
    source = source.crop((0, 0, side, side)).resize((WORK, WORK), Image.LANCZOS)

    # The navy is sampled from the mark itself rather than hard-coded, so a
    # redrawn brand icon keeps the icons consistent without editing this file.
    navy = source.getpixel((WORK // 2, int(WORK * 0.04)))

    transparent = source.copy()
    transparent.putalpha(squircle(WORK))

    written = []

    def write(name, image):
        path = '%s/%s' % (OUT, name)
        image.save(path, 'PNG', optimize=True)
        written.append((name, image.size[0], len(io.open(path, 'rb').read())))

    # Browser tab icons. Transparent corners so the squircle reads on both the
    # light and dark browser chrome.
    for name, size in [('favicon.png', 32), ('icon-192.png', 192), ('icon-512.png', 512)]:
        write(name, transparent.resize((size, size), Image.LANCZOS))

    # iOS re-masks and composites onto white, so this one must be opaque and
    # full-bleed; a transparent-cornered icon shows white triangles there.
    apple = Image.new('RGB', (WORK, WORK), navy)
    apple.paste(source, (0, 0), squircle(WORK))
    write('apple-touch-icon.png', apple.resize((180, 180), Image.LANCZOS))

    # Maskable: navy to the edges, mark inset into the safe zone.
    inner = int(WORK * MASKABLE_SAFE)
    offset = (WORK - inner) // 2
    maskable = Image.new('RGB', (WORK, WORK), navy)
    maskable.paste(
        source.resize((inner, inner), Image.LANCZOS),
        (offset, offset),
        squircle(inner))
    write('icon-maskable-512.png', maskable.resize((512, 512), Image.LANCZOS))

    print('navy sampled as #%02X%02X%02X' % navy)
    for name, size, nbytes in written:
        print('  %-24s %4dpx  %6d bytes' % (name, size, nbytes))


if __name__ == '__main__':
    main()
