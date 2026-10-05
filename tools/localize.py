"""One-off migration: turn `public const string` resources into culture-aware
`static string` properties backed by Strings.Get, and register their English
overrides. Kept in the repo so the remaining resource files can be converted
the same way rather than by hand."""

import io, re, sys, json

path, cls, mapping_json = sys.argv[1], sys.argv[2], sys.argv[3]
english = json.loads(io.open(mapping_json, encoding='utf-8').read())

s = io.open(path, encoding='utf-8').read()

# Matches:  public const string Name = "literal";
# The literal may contain escaped quotes.
pattern = re.compile(r'public const string (\w+) = ("(?:\\.|[^"\\])*");')

keys = []


def repl(m):
    name, lit = m.group(1), m.group(2)
    keys.append(name)
    return 'public static string {0} => Strings.Get("{1}.{0}", {2});'.format(name, cls, lit)


s2 = pattern.sub(repl, s)
if not keys:
    print('NO MATCHES in ' + path)
    sys.exit(1)

registered = {k: v for k, v in english.items() if k in keys}
lines = ''.join(
    '        ["{0}.{1}"] = {2},\n'.format(cls, k, json.dumps(v, ensure_ascii=False))
    for k, v in registered.items())

block = (
    '\n    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>\n'
    '    static {0}() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)\n'
    '    {{\n{1}    }});\n'.format(cls, lines))

idx = s2.rstrip().rfind('}')
s2 = s2.rstrip()[:idx].rstrip('\n') + '\n' + block + '}\n'
io.open(path, 'w', encoding='utf-8', newline='').write(s2)

print('{0}: {1} strings, {2} translated, {3} untranslated'.format(
    cls, len(keys), len(registered), len(keys) - len(registered)))
