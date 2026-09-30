"""Package the isolated Windows review build after build/startup/preservation checks."""
from pathlib import Path
import datetime
import hashlib
import json
import zipfile

root=Path(__file__).resolve().parents[2]
exports=root/'ClientExports/YanYana'
player=exports/'Windows'
reports=exports/'Reports'
build=reports/'windows-build.txt'
startup=reports/'windows-startup.log'
scene=root/'Assets/YanYana/Scenes/YanYana_Adventure.unity'

build_text=build.read_text(encoding='utf-8-sig')
if not build_text.startswith('Result=Succeeded'):
    raise SystemExit('A successful isolated Windows build is required.')
if build.stat().st_mtime<scene.stat().st_mtime:
    raise SystemExit('The Windows build is older than the authored scene.')
if startup.stat().st_mtime<build.stat().st_mtime:
    raise SystemExit('Run the freshly built player before packaging it.')
log=startup.read_text(encoding='utf-8-sig')
if 'YAN YANA navigation ready: True' not in log:
    raise SystemExit('The standalone scene did not confirm its navigation startup.')
for marker in ('Exception:', 'Failed to create agent', 'NullReferenceException', 'MissingMethodException'):
    if marker in log:
        raise SystemExit('The standalone startup requires review: '+marker)
preservation=json.loads((root/'.codex_tmp/yanyana_implementation/preservation-report.json').read_text(encoding='utf-8-sig'))
if not preservation['unchanged']:
    raise SystemExit('Original project files must match the baseline before packaging.')
new_files=json.loads((reports/'new-files-preservation.json').read_text(encoding='utf-8'))
if new_files['unexpectedNewPaths']:
    raise SystemExit('Unexpected generated files outside the new adventure must be reviewed before packaging.')
for name in ('YanYana.exe','YanYana_Data','UnityPlayer.dll','MonoBleedingEdge','Oyna.cmd','OKU.txt'):
    if not (player/name).exists():raise SystemExit('Missing build component: '+name)

stamp=datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
archive=exports/('Deprem-Windows-Inceleme-'+stamp+'.zip')
count=0
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as bundle:
    for path in sorted(player.rglob('*')):
        if not path.is_file() or any('DoNotShip' in part for part in path.parts):continue
        if path.suffix=='.log':continue
        bundle.write(path,Path('Deprem')/path.relative_to(player));count+=1
with zipfile.ZipFile(archive) as bundle:
    bad=bundle.testzip()
    if bad:raise SystemExit('Archive CRC check failed: '+bad)
result={'archive':str(archive),'files':count,'bytes':archive.stat().st_size,'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'sceneSha256':hashlib.sha256(scene.read_bytes()).hexdigest(),'originalFilesUnchanged':preservation['checked'],'scope':'Windows review package. Startup log and archive integrity checked; full input routes were tested in Unity Editor. Not Android or child playtime evidence.'}
(reports/'windows-package.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
