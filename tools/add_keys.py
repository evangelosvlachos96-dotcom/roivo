"""Add culture-aware properties to an existing resource class.

The resource classes all share one shape: properties, then an
`/// <summary>English overrides` marker, then a static constructor whose
dictionary literal ends with `});`. Hand-editing that reliably is fiddly, so
new keys go in through here instead.

Usage: python tools/add_keys.py <ClassName> <keys.json>

keys.json: {"PropertyName": ["Greek text", "English text"], ...}
English may be null when the Greek is already language-neutral.
"""
import io, json, sys

cls, keys_path = sys.argv[1], sys.argv[2]
path = 'src/Roivo.Resources/%s.cs' % cls
keys = json.loads(io.open(keys_path, encoding='utf-8').read())

s = io.open(path, encoding='utf-8').read()

MARKER = '    /// <summary>English overrides.'
if MARKER not in s:
    sys.exit('no English-overrides marker in ' + path)

existing = ['"%s.%s"' % (cls, k) for k in keys if '"%s.%s"' % (cls, k) in s]
if existing:
    sys.exit('already defined in %s: %s' % (path, ', '.join(existing)))

props = ''.join(
    '    public static string %s => Strings.Get("%s.%s", %s);\n'
    % (k, cls, k, json.dumps(gr, ensure_ascii=False))
    for k, (gr, en) in keys.items())

head, tail = s.split(MARKER, 1)
s = head.rstrip('\n') + '\n\n' + props + '\n' + MARKER + tail

entries = ''.join(
    '        ["%s.%s"] = %s,\n' % (cls, k, json.dumps(en, ensure_ascii=False))
    for k, (gr, en) in keys.items() if en is not None)

# The dictionary literal is the last `});` in the file.
idx = s.rfind('    });')
s = s[:idx] + entries + s[idx:]

io.open(path, 'w', encoding='utf-8', newline='').write(s)
print('%s: +%d properties, %d English overrides'
      % (cls, len(keys), sum(1 for v in keys.values() if v[1] is not None)))
