"""Refresh only YanYana assets in the owned, closed Android review copy."""
from pathlib import Path
import datetime
import hashlib
import json
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[2]
record = ROOT / '.codex_tmp/yanyana_implementation/android-project-current.json'
info = json.loads(record.read_text(encoding='utf-8-sig'))
clone = Path(info['clone']).resolve()
assert clone.parent == ROOT / '.codex_tmp'
assert Path(info['source']).resolve() == ROOT
marker = clone / 'yanyana-android-copy.json'
assert json.loads(marker.read_text(encoding='utf-8-sig'))['purpose'] == 'YanYana isolated Android review build'
# Query only process IDs for this known copy. Do not expose unrelated command lines.
quoted = str(clone).replace("'", "''")
check = subprocess.run(['powershell', '-NoProfile', '-Command',
    "Get-CimInstance Win32_Process -Filter \"Name = 'Unity.exe'\" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains('" + quoted + "') } | Select-Object -ExpandProperty ProcessId"],
    capture_output=True, text=True, timeout=30)
if check.returncode or check.stdout.strip():
    raise SystemExit('Cannot refresh an open or unverified Android copy.')

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

scene_hash = sha(ROOT / info['scene'])
changed = 0
for source in (ROOT / 'Assets/YanYana').rglob('*'):
    if not source.is_file():
        continue
    target = clone / source.relative_to(ROOT)
    if not target.exists() or sha(source) != sha(target):
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        changed += 1
assert sha(clone / info['scene']) == scene_hash == sha(ROOT / info['scene'])
info.update(sceneSha256=scene_hash, refreshedAt=datetime.datetime.now().astimezone().isoformat(), refreshedFiles=changed)
for path in (marker, record):
    path.write_text(json.dumps(info, indent=2), encoding='utf-8')
print(f'Refreshed {changed} YanYana files in the isolated Android copy. Scene SHA256: {scene_hash}')
