"""Report resource properties with no English override registered."""
import io, re, glob, os

SKIP = {'Strings', 'FormatExtensions', 'ValidationMessages'}

for f in sorted(glob.glob('src/Roivo.Resources/*.cs')):
    cls = os.path.basename(f)[:-3]
    if cls in SKIP:
        continue
    s = io.open(f, encoding='utf-8').read()
    props = set(re.findall(r'public static string (\w+)', s))
    reg = set(re.findall(r'\["' + cls + r'\.(\w+)"\]', s))
    pinned = {p for p in props if p.endswith('_El') or p.endswith('_En')}
    missing = props - reg - pinned
    if missing:
        print('%s: %d props, %d registered, MISSING %d' % (cls, len(props), len(reg), len(missing)))
        for m in sorted(missing):
            lit = re.search(r'"' + cls + r'\.' + m + r'",\s*\n?\s*"([^"]*)"', s)
            print('    %-32s %s' % (m, lit.group(1)[:60] if lit else '(literal off-line)'))
