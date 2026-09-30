"""Preserve only the new adventure's Editor save while checking its New button."""
import base64
import json
from pathlib import Path
import sys
import struct
import winreg

ROOT = Path(__file__).resolve().parents[2]
SNAPSHOT = ROOT / (sys.argv[2] if len(sys.argv) > 2 else '.codex_tmp/ui_redesign_20260914/preferences.json')
assert SNAPSHOT.resolve().is_relative_to(ROOT / '.codex_tmp')
KEY = r'Software\Unity\UnityEditor\DepremEgitim\Deprem'
PREFIX = 'Deprem.YanYana.v1.'


def values(key):
    result = []
    for i in range(winreg.QueryInfoKey(key)[1]):
        name, data, kind = winreg.EnumValue(key, i)
        if name.startswith(PREFIX):
            result.append({'name': name, 'kind': kind,
                           'binary': isinstance(data, bytes),
                           'data': base64.b64encode(data).decode() if isinstance(data, bytes) else data})
    return result


if sys.argv[1] == 'backup':
    if SNAPSHOT.exists():
        raise SystemExit('Existing snapshot retained; do not overwrite it.')
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, KEY) as key:
        saved = values(key)
    SNAPSHOT.parent.mkdir(parents=True, exist_ok=True)
    SNAPSHOT.write_text(json.dumps({'key': KEY, 'values': saved}), encoding='utf-8')
    print(f'Preserved {len(saved)} adventure save values; no other preferences read.')
elif sys.argv[1] == 'prepare_unity':
    saved = json.loads(SNAPSHOT.read_text(encoding='utf-8'))
    entries = []
    for value in saved['values']:
        name = value['name'].rsplit('_h', 1)[0]
        assert name.startswith(PREFIX)
        if value['binary']:
            entries.append(dict(key=name, type='string', text=base64.b64decode(value['data']).decode('utf-8').rstrip('\0')))
        elif name.endswith('.playSeconds'):
            entries.append(dict(key=name, type='float', number=struct.unpack('<f', struct.pack('<I', value['data'] & 0xffffffff))[0]))
        else:
            entries.append(dict(key=name, type='int', integer=struct.unpack('<i', struct.pack('<I', value['data'] & 0xffffffff))[0]))
    SNAPSHOT.with_name('preferences-unity.json').write_text(json.dumps({'entries': entries}, ensure_ascii=False), encoding='utf-8')
    print(f'Prepared {len(entries)} typed values for Unity cache restoration.')
elif sys.argv[1] == 'restore':
    saved = json.loads(SNAPSHOT.read_text(encoding='utf-8'))
    assert saved['key'] == KEY
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, KEY, 0, winreg.KEY_READ | winreg.KEY_SET_VALUE) as key:
        for value in values(key):
            winreg.DeleteValue(key, value['name'])
        for value in saved['values']:
            assert value['name'].startswith(PREFIX)
            data = base64.b64decode(value['data']) if value['binary'] else value['data']
            winreg.SetValueEx(key, value['name'], 0, value['kind'], data)
        assert sorted(values(key), key=lambda x: x['name']) == sorted(saved['values'], key=lambda x: x['name'])
    print(f'Restored and verified {len(saved["values"])} adventure save values.')
else:
    raise SystemExit('Use backup or restore.')
