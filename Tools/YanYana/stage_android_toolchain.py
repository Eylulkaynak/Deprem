"""Validate and stage the official archives when Hub's install-state database is incompatible."""
from pathlib import Path, PurePosixPath
import base64
import datetime
import hashlib
import json
import os
import shutil
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[2]
DOWNLOADS = Path(os.environ['APPDATA']) / 'UnityHub/downloads'
PACKAGES = [
    ('jdk17.0.9-9_f12c2989c2f749b13282640a12d7d624097f6c2d45144d87331f21ad352ab63e.zip', 'OpenJDK', ''),
    ('android-ndk-r27c-windows.zip', 'NDK', 'android-ndk-r27c/'),
    ('cmake-3.22.1-windows.zip', 'SDK/cmake/3.22.1', ''),
    ('build-tools_r34-windows.zip', 'SDK/build-tools/34.0.0', 'android-14/'),
    ('commandlinetools-win-12266719_latest.zip', 'SDK/cmdline-tools/16.0', 'cmdline-tools/'),
    ('platform-tools_r34.0.5-windows.zip', 'SDK', ''),
    ('platform-34-ext7_r02.zip', 'SDK/platforms', ''),
    ('platform-35_r01.zip', 'SDK/platforms', ''),
    ('platform-36_r02.zip', 'SDK/platforms', ''),
    ('sdk-tools-windows-4333796.zip', 'SDK', ''),
]


def digest(path, algorithm='sha256'):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, algorithm).hexdigest()


def main():
    stage = Path(os.environ['LOCALAPPDATA']) / 'YanYanaAndroidSetup' / datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
    stage.mkdir(parents=True, exist_ok=False)
    checksums = {}
    repository_url = 'https://dl.google.com/android/repository/repository2-1.xml'
    try:
        data = urllib.request.urlopen(repository_url, timeout=45).read()
        (stage / 'google-repository.xml').write_bytes(data)
        document = ET.fromstring(data)
        for node in document.iter():
            if node.tag.rsplit('}', 1)[-1] != 'complete':
                continue
            fields = {child.tag.rsplit('}', 1)[-1]: child for child in node}
            if 'url' in fields and 'checksum' in fields:
                checksum = fields['checksum']
                checksums[fields['url'].text.rsplit('/', 1)[-1]] = (checksum.attrib.get('type', 'sha1').replace('-', '').lower(), checksum.text.strip())
    except Exception as error:
        print('Google checksum index unavailable: ' + str(error), flush=True)
    records = []
    for name, destination, strip in PACKAGES:
        archive = DOWNLOADS / name
        sha = digest(archive)
        check = None
        if name in checksums:
            algorithm, expected = checksums[name]
            if digest(archive, algorithm) != expected.lower():
                raise SystemExit('Google checksum mismatch: ' + name)
            check = 'Google repository ' + algorithm
        if name.startswith('jdk17.'):
            if sha != 'f12c2989c2f749b13282640a12d7d624097f6c2d45144d87331f21ad352ab63e':
                raise SystemExit('Unity OpenJDK checksum mismatch.')
            check = 'Unity archive SHA256'
        if name.startswith('commandlinetools-'):
            actual = base64.b64encode(bytes.fromhex(digest(archive, 'sha384'))).decode()
            if actual != '3H6q0QmivqDyr88w9L8AsYO9yGt41w/Or/JYqclRY2ZbCh3kdguKuyAQYM6JwyWe':
                raise SystemExit('Unity command-line tools SHA384 mismatch.')
            check = 'Unity module manifest SHA384'
        count = 0
        with zipfile.ZipFile(archive) as bundle:
            for member in bundle.infolist():
                name_in_zip = member.filename
                if strip:
                    if not name_in_zip.startswith(strip):
                        raise SystemExit('Unexpected archive root: ' + name_in_zip)
                    name_in_zip = name_in_zip[len(strip):]
                if not name_in_zip:
                    continue
                relative = PurePosixPath(name_in_zip)
                if relative.is_absolute() or '..' in relative.parts or '\\' in name_in_zip or ':' in name_in_zip:
                    raise SystemExit('Unsafe archive path: ' + name_in_zip)
                output = (stage / destination / name_in_zip).resolve()
                if not output.is_relative_to(stage.resolve()):
                    raise SystemExit('Archive path escaped the stage.')
                # Extended Windows paths keep the NDK's long header names intact.
                target = Path('\\\\?\\' + str(output))
                if member.is_dir():
                    target.mkdir(parents=True, exist_ok=True)
                    continue
                target.parent.mkdir(parents=True, exist_ok=True)
                with bundle.open(member) as source, target.open('wb') as sink:
                    shutil.copyfileobj(source, sink, 1024 * 1024)  # Also verifies each ZIP member CRC.
                count += 1
        records.append({'archive': str(archive), 'sha256': sha, 'checksumVerification': check, 'zipCrcVerified': True, 'files': count, 'destination': destination})
        print('Staged ' + name + ': ' + str(count) + ' files; ' + str(check), flush=True)
    manifest = {'purpose': 'YanYana official Android toolchain stage', 'stage': str(stage), 'target': 'C:/Program Files/Unity/Hub/Editor/6000.0.58f2/Editor/Data/PlaybackEngines/AndroidPlayer', 'archives': records, 'createdAt': datetime.datetime.now().astimezone().isoformat()}
    (stage / 'stage-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    (ROOT / '.codex_tmp/yanyana_implementation/android-stage-current.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print(json.dumps({'stage': str(stage), 'packages': len(records)}), flush=True)


if __name__ == '__main__':
    main()
