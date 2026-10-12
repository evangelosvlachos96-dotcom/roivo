import io, re, glob, os
SKIP = {'Strings', 'FormatExtensions', 'ValidationMessages'}
tot_props = 0
tot_pinned = 0
for f in sorted(glob.glob('src/Roivo.Resources/*.cs')):
    cls = os.path.basename(f)[:-3]
    if cls in SKIP:
        continue
    s = io.open(f, encoding='utf-8').read()
    props = re.findall(r'public static string (\w+)', s)
    pinned = [p for p in props if p.endswith('_El') or p.endswith('_En')]
    reg = re.findall(r'\["' + cls + r'\.(\w+)"\]\s*=', s)
    tot_props += len(props)
    tot_pinned += len(pinned)
    flag = '' if len(props) - len(pinned) == len(reg) else '   <-- MISMATCH'
    print('%-16s props=%-4d pinned=%-3d registered=%-4d%s' % (cls, len(props), len(pinned), len(reg), flag))
print('TOTAL props=%d pinned=%d translatable=%d' % (tot_props, tot_pinned, tot_props - tot_pinned))
