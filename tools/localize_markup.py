"""Replace hardcoded Greek in auth .cshtml markup with the Auth.* resource
property whose Greek literal matches exactly. Reports anything with no match so
it can be handled by hand rather than guessed at."""
import io, re, glob, os

auth = io.open('src/Roivo.Resources/Auth.cs', encoding='utf-8').read()
# property name -> greek literal
lit = dict((m[1], m[2]) for m in
           re.finditer(r'public static string (\w+)\s*=>\s*Strings\.Get\("Auth\.(\w+)",\s*\n?\s*"([^"]*)"', auth)
           and [])
pairs = re.findall(r'public static string \w+\s*=>\s*Strings\.Get\("Auth\.(\w+)",\s*"([^"]*)"\)', auth)
pairs += re.findall(r'public static string \w+\s*=>\s*Strings\.Get\("Auth\.(\w+)",\s*\n\s*"([^"]*)"\)', auth)
by_greek = {}
for key, greek in pairs:
    by_greek.setdefault(greek, key)

greek_re = re.compile(r'[Ͱ-Ͽἀ-῿]')
unmatched = []

for f in sorted(glob.glob('src/Roivo.Web/Areas/Account/Pages/*.cshtml')):
    s = io.open(f, encoding='utf-8').read()
    original = s
    # Longest first so a substring never shadows a longer phrase.
    for greek in sorted(by_greek, key=len, reverse=True):
        key = by_greek[greek]
        if greek and greek in s:
            s = s.replace('>%s<' % greek, '>@Auth.%s<' % key)
            s = s.replace('"%s"' % greek, 'Auth.%s' % key)
    if s != original:
        if '@using Roivo.Resources' not in s:
            s = re.sub(r'(@model [^\n]+\n)', r'\1@using Roivo.Resources\n', s, count=1)
        io.open(f, 'w', encoding='utf-8', newline='').write(s)

    left = [l.strip() for l in io.open(f, encoding='utf-8') if greek_re.search(l)]
    if left:
        unmatched.append((os.path.basename(f), left))

for name, lines in unmatched:
    print('%s — %d line(s) with no exact resource match:' % (name, len(lines)))
    for l in lines:
        print('    ' + l[:95])
