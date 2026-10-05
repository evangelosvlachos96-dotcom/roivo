"""Replace exact literals in a file, refusing to guess.

Every replacement must match exactly the expected number of times (default 1).
A miss or an unexpected extra match aborts the whole file, so a typo in a
pattern can never silently half-apply a page's localization.

Usage: python tools/swap_literals.py <file> <swaps.json>

swaps.json: [["old", "new"], ...]  or  [["old", "new", count], ...]
"""
import io, json, sys

path, swaps_path = sys.argv[1], sys.argv[2]
swaps = json.loads(io.open(swaps_path, encoding='utf-8').read())

s = io.open(path, encoding='utf-8').read()
problems = []

for swap in swaps:
    old, new = swap[0], swap[1]
    want = swap[2] if len(swap) > 2 else 1
    got = s.count(old)
    if got != want:
        problems.append('want %d got %d: %s' % (want, got, old[:70]))
        continue
    s = s.replace(old, new)

if problems:
    print('ABORTED %s' % path)
    for p in problems:
        print('  ' + p)
    sys.exit(1)

io.open(path, 'w', encoding='utf-8', newline='').write(s)
print('%s: %d swaps applied' % (path, len(swaps)))
