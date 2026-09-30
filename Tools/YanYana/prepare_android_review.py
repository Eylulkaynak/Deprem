"""Copy the current project for an isolated Android build; never switch the source target."""
from pathlib import Path
import datetime
import hashlib
import json
import shutil

ROOT = Path(__file__).resolve().parents[2]
SCENE = 'Assets/YanYana/Scenes/YanYana_Adventure.unity'
SCRATCH = ROOT / '.codex_tmp/yanyana_implementation'


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
    clone = ROOT / '.codex_tmp' / ('yanyana_android_' + stamp)
    if clone.exists():
        raise SystemExit('Refusing to overwrite an existing project copy.')
    source_sha = sha(ROOT / SCENE)
    clone.mkdir(parents=True)
    (clone / '.gitignore').write_text('*\n', encoding='utf-8')
    marker = {
        'purpose': 'YanYana isolated Android review build',
        'source': str(ROOT),
        'clone': str(clone),
        'createdAt': datetime.datetime.now().astimezone().isoformat(),
        'scene': SCENE,
        'sceneSha256': source_sha,
    }
    (clone / 'yanyana-android-copy.json').write_text(json.dumps(marker, indent=2), encoding='utf-8')
    counts = {}
    for name in ('Assets', 'Packages', 'ProjectSettings'):
        source = ROOT / name
        shutil.copytree(source, clone / name)
        files = [p for p in source.rglob('*') if p.is_file()]
        counts[name] = {'files': len(files), 'bytes': sum(p.stat().st_size for p in files)}
        print('Copied ' + name, counts[name], flush=True)
    # Preserve resolved package versions without sharing writable package/cache files.
    package_cache = ROOT / 'Library/PackageCache'
    if package_cache.exists():
        shutil.copytree(package_cache, clone / 'Library/PackageCache')
    (clone / 'ClientExports/YanYana/Reports').mkdir(parents=True)
    helper = clone / 'Assets/YanYana/Editor/YanYanaAndroidReviewBuild.cs'
    shutil.copy2(ROOT / 'Tools/YanYana/YanYanaAndroidReviewBuild.cs.template', helper)
    if sha(clone / SCENE) != source_sha or sha(ROOT / SCENE) != source_sha:
        raise SystemExit('Scene changed during the copy; do not build this snapshot.')
    marker['copied'] = counts
    marker['helperSha256'] = sha(helper)
    (clone / 'yanyana-android-copy.json').write_text(json.dumps(marker, indent=2), encoding='utf-8')
    SCRATCH.mkdir(parents=True, exist_ok=True)
    (SCRATCH / 'android-project-current.json').write_text(json.dumps(marker, indent=2), encoding='utf-8')
    print(json.dumps(marker, indent=2), flush=True)


if __name__ == '__main__':
    main()
