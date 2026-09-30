import concurrent.futures
import hashlib
import json
import pathlib
import shutil
import subprocess
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = pathlib.Path(__file__).resolve().parent
BASELINE = OUT / 'baseline.json'

def digest(relative):
    path = ROOT / relative
    if not path.is_file():
        return relative, None
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return relative, {'sha256': h.hexdigest(), 'bytes': path.stat().st_size}

def capture():
    if BASELINE.exists():
        raise SystemExit('Baseline already exists; refusing to replace it.')
    raw = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z'], cwd=ROOT)
    paths = sorted(set(p.decode('utf-8') for p in raw.split(b'\0') if p))
    paths = [p for p in paths if not p.startswith(('Assets/YanYana/', 'ArtDirection/YanYana/', 'Tools/YanYana/', 'ClientExports/YanYana/'))]
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        entries = dict(pool.map(digest, paths))
    BASELINE.write_text(json.dumps({'created': time.time(), 'files': entries}, ensure_ascii=False, indent=2), encoding='utf-8')
    status = subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=ROOT)
    (OUT / 'git-status-before.bin').write_bytes(status)
    for relative in paths:
        if relative.startswith('ProjectSettings/') or relative.endswith(('.cs', '.asmdef', '.unity', '.prefab', '.controller', '.playable', '.mat', '.meta')):
            source = ROOT / relative
            if source.is_file():
                destination = OUT / 'protected' / relative
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, destination)
    print(json.dumps({'baselineFiles': len(entries), 'bytes': sum(v['bytes'] for v in entries.values() if v)}))

def verify():
    data = json.loads(BASELINE.read_text(encoding='utf-8'))
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        current = dict(pool.map(digest, data['files']))
    changes = [p for p, old in data['files'].items() if old != current[p]]
    report = {'checked': len(current), 'changedExistingFiles': changes, 'unchanged': not changes}
    (OUT / 'preservation-report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False))

if __name__ == '__main__':
    verify() if '--verify' in sys.argv else capture()
