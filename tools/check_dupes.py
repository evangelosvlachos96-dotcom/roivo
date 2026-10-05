"""Find duplicate English-override keys: RegisterEnglish overwrites, so a dupe
silently reduces TranslationCount and leaves one property untranslated."""
import io, re, glob, os
from collections import Counter

keys = []
for f in sorted(glob.glob('src/Roivo.Resources/*.cs')):
    if os.path.basename(f)[:-3] in {'Strings', 'FormatExtensions', 'ValidationMessages'}:
        continue
    s = io.open(f, encoding='utf-8').read()
    found = re.findall(r'\["([A-Za-z]+\.\w+)"\]\s*=', s)
    keys.extend(found)
    inner = Counter(found)
    for k, n in inner.items():
        if n > 1:
            print('DUPLICATE within %s: %s x%d' % (os.path.basename(f), k, n))

total = Counter(keys)
dupes = {k: n for k, n in total.items() if n > 1}
print('total registration entries: %d, distinct keys: %d, duplicates: %d'
      % (len(keys), len(total), len(keys) - len(total)))
for k, n in sorted(dupes.items()):
    print('  %s x%d' % (k, n))
