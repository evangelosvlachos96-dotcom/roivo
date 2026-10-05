"""Report hardcoded Greek in Razor markup.

Resource classes are the legitimate home for Greek text, so they are never
scanned. Comment lines are skipped: a Greek word inside an explanatory comment
is not a localization defect.
"""
import io, re, glob, sys

GREEK = re.compile(r'[Ͱ-Ͽἀ-῿]{2,}')

roots = sys.argv[1:] or ['src/Roivo.Web/Components', 'src/Roivo.Web/Areas']
total = 0

for root in roots:
    files = sorted(glob.glob(root + '/**/*.razor', recursive=True) +
                   glob.glob(root + '/**/*.cshtml', recursive=True))
    for f in files:
        hits = []
        for n, line in enumerate(io.open(f, encoding='utf-8'), 1):
            stripped = line.strip()
            if stripped.startswith(('@*', '//', '<!--', '*')):
                continue
            if GREEK.search(line):
                hits.append((n, stripped[:88]))
        if hits:
            print('%s  (%d)' % (f.replace(chr(92), '/'), len(hits)))
            for n, text in hits:
                print('    %4d  %s' % (n, text))
            total += len(hits)

print('TOTAL hardcoded-Greek lines: %d' % total)
