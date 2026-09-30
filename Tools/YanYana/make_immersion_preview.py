"""Cut a short, normal-speed review from the recorded production-input route."""
from pathlib import Path
import csv
import hashlib
import json
import re
import subprocess

root = Path(__file__).resolve().parents[2]
reports = root / 'ClientExports/YanYana/Reports'
recordings = root / 'ClientExports/YanYana/Recordings'
ffmpeg = root / '.codex_tmp/kktc_visual_20260908/video_deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
fields = dict(line.split('=', 1) for line in (reports / 'camera-motion-latest.txt').read_text().splitlines() if '=' in line)
rows = list(csv.DictReader((root / fields['Csv']).open(encoding='utf-8-sig')))
movie = max(recordings.glob('physical-playthrough-20260915-*.mp4'), key=lambda p: p.stat().st_mtime)
assert 'COMPLETE Ending=2' in (reports / 'physical-recorded-route-2.txt').read_text(encoding='utf-8-sig')
probe = subprocess.run([str(ffmpeg), '-hide_banner', '-i', str(movie)], capture_output=True, text=True)
match = re.search(r'Duration: (\d+):(\d+):([\d.]+)', probe.stderr)
assert match and 'Audio:' in probe.stderr
duration = int(match[1])*3600 + int(match[2])*60 + float(match[3])
origin = float(rows[0]['time'])

def time_for(name, walking=False):
    for row in rows:
        if row['camera'] != name or row['blending'] != 'False':
            continue
        elapsed = float(row['time']) - origin
        if walking and (elapsed < 4 or float(row['step']) < .03):
            continue
        return elapsed + .35
    raise ValueError(name)

clips = [
    ('carry', time_for('Yakından incele · introCarry', True), 6),
    ('packing', time_for('Yakından incele · bag') + 1.0, 5.5),
    ('coupling', time_for('Yakından incele · hose'), 1.5),
    ('water and extinguishing', time_for('Yakından incele · fire1') + 2.0, 14),
    ('third position', time_for('Yakından incele · fire3') + 2.0, 6),
    ('reunion', time_for('Yakından incele · reunion2') + .8, 3.5),
]
filters = []
for i, (_, start, length) in enumerate(clips):
    end = min(duration-.1, start+length)
    assert end > start
    filters += [f'[0:v]trim=start={start}:end={end},setpts=PTS-STARTPTS[v{i}]',
                f'[0:a]atrim=start={start}:end={end},asetpts=PTS-STARTPTS[a{i}]']
filters.append(''.join(f'[v{i}][a{i}]' for i in range(len(clips))) + f'concat=n={len(clips)}:v=1:a=1[v][a]')
output = recordings / 'Deprem-kamera-hortum-20260915.mp4'
command = [str(ffmpeg), '-y', '-hide_banner', '-loglevel', 'error', '-i', str(movie), '-filter_complex', ';'.join(filters),
           '-map', '[v]', '-map', '[a]', '-r', '30', '-c:v', 'libx264', '-preset', 'fast', '-crf', '20', '-pix_fmt', 'yuv420p',
           '-c:a', 'aac', '-b:a', '128k', '-movflags', '+faststart', str(output)]
subprocess.run(command, check=True)
data = {'source': str(movie), 'sourceDurationSeconds': duration, 'output': str(output), 'clips': clips,
        'playbackSpeed': 1, 'cuts': 'Hard cuts between actual gameplay excerpts; no acceleration or simulated visuals.',
        'sceneSha256': hashlib.sha256((root / 'Assets/YanYana/Scenes/YanYana_Adventure.unity').read_bytes()).hexdigest()}
(reports / 'immersion-preview.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(data, ensure_ascii=False))
