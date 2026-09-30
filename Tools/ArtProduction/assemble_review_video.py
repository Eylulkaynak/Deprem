"""Join normal-speed chapter recordings at an inspected chapter boundary.

No action, dialogue, image or audio speed is changed. Both original recordings
and an explicit source/cut manifest are retained beside the review evidence.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys
from datetime import datetime, timezone

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / '.codex_tmp/kktc_visual_20260908/video_deps'))
import imageio_ffmpeg

parser = argparse.ArgumentParser()
parser.add_argument('first_recording', type=Path)
parser.add_argument('last_recording', type=Path)
parser.add_argument('--first-end', type=float, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
first = args.first_recording.resolve()
last = args.last_recording.resolve()
output = args.output.resolve()
for source in (first, last):
    status = source.with_name(source.stem + '_STATUS.txt')
    if not source.is_file() or not status.is_file() or 'COMPLETED' not in status.read_text(encoding='utf-8-sig'):
        raise SystemExit('Only completed Unity Recorder sources can be assembled: ' + str(source))
if args.first_end <= 0 or output.exists():
    raise SystemExit('Use a positive inspected boundary and a new output path.')
output.parent.mkdir(parents=True, exist_ok=True)
prefix = output.with_name(output.stem + '_Chapters01-02.mp4')
if prefix.exists():
    raise SystemExit('Chapter prefix already exists; do not overwrite review evidence.')
subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'warning', '-n',
                '-i', str(first), '-t', format(args.first_end, '.6f'),
                '-map', '0:v:0', '-map', '0:a:0', '-c', 'copy',
                '-movflags', '+faststart', str(prefix)], check=True)
concat = output.with_suffix('.ffconcat')
def quote(path):
    return "'" + path.as_posix().replace("'", "'\\''") + "'"
concat.write_text('ffconcat version 1.0\nfile ' + quote(prefix) + '\nfile ' + quote(last) + '\n', encoding='utf-8')
subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'warning', '-n',
                '-f', 'concat', '-safe', '0', '-i', str(concat),
                '-map', '0:v:0', '-map', '0:a:0', '-c', 'copy',
                '-movflags', '+faststart', str(output)], check=True)
def sha256(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(8 * 1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()
reports = root / 'ClientExports/KKTC/Reports'
manifest = {
    'assembled_utc': datetime.now(timezone.utc).isoformat(),
    'output': output.as_posix(),
    'sha256': sha256(output),
    'resolution': '1080x2340',
    'fps': 30,
    'speed': 1,
    'method': 'FFmpeg stream-copy concatenation at a reviewed chapter boundary; original image/audio streams retained.',
    'sources': [
        {'file': first.as_posix(), 'sha256': sha256(first), 'start_seconds': 0,
         'end_seconds_exclusive': args.first_end, 'chapters': [1, 2]},
        {'file': last.as_posix(), 'sha256': sha256(last), 'start_seconds': 0,
         'end_seconds_exclusive': None, 'chapters': [3, 4]}
    ],
    'note': 'Automated touch/navigation driver, authored dialogue pacing, separate fresh scene loads. Save/flag continuity is covered by its separate PlayMode test. Final voiceover is deferred.'
}
(reports / 'NormalSpeedRecording_Assembly.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
(reports / 'NormalSpeedRecording_Status.txt').write_text(
    'COMPLETED\nvideo=' + output.as_posix() + '\nresolution=1080x2340\nfps=30\nspeed=1x\naudio=on\n'
    'provenance=NormalSpeedRecording_Assembly.json\n', encoding='utf-8')
print(output)
