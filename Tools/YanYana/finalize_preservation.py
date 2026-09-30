"""Audit new paths; optionally archive only known, newly generated Unity editor caches."""
from pathlib import Path
import datetime, hashlib, json, shutil, subprocess, sys

root=Path(__file__).resolve().parents[2]
scratch=root/'.codex_tmp/yanyana_implementation'
baseline_record=json.loads((scratch/'baseline.json').read_text(encoding='utf-8'))
baseline=baseline_record['files']
owned=('Assets/YanYana/','ArtDirection/YanYana/','Tools/YanYana/','ClientExports/YanYana/','.codex_tmp/')
generated={
    'Assets/Unity.VisualScripting.Generated.meta',
    'Assets/Unity.VisualScripting.Generated/VisualScripting.Core.meta',
    'Assets/Unity.VisualScripting.Generated/VisualScripting.Flow.meta',
    'Assets/Unity.VisualScripting.Generated/VisualScripting.Flow/UnitOptions.db',
    'Assets/Unity.VisualScripting.Generated/VisualScripting.Flow/UnitOptions.db.meta',
    'ProjectSettings/VisualScriptingSettings.asset',
}
# Unity's performance-test build preprocessor creates an otherwise empty
# Resources folder for temporary run-info JSON. Only accept its orphaned meta.
resources=root/'Assets/Resources'
if resources.is_dir() and not any(resources.iterdir()):
    generated.add('Assets/Resources.meta')
def unexpected():
    data=subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard','-z'],cwd=root)
    paths={entry.decode('utf-8') for entry in data.split(b'\0') if entry}
    return sorted(path for path in paths if path not in baseline and not path.startswith(owned) and path!='Assets/YanYana.meta')

before=unexpected()
unknown=set(before)-generated
if unknown:raise SystemExit('Inspect unrecognized new paths before cleanup: '+str(sorted(unknown)))
archived=[]
if '--archive-generated-support' in sys.argv:
    archive=scratch/'generated-support'/datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
    for relative in before:
        if relative in baseline:raise SystemExit('Refusing to touch a baseline path: '+relative)
        source=(root/relative).resolve()
        if not source.is_relative_to(root):raise SystemExit('Path escaped the project: '+relative)
        destination=archive/relative
        destination.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(source,destination)
        if hashlib.sha256(source.read_bytes()).digest()!=hashlib.sha256(destination.read_bytes()).digest():raise SystemExit('Archive verification failed: '+relative)
        source.unlink()  # Only this explicit new file; never a recursive directory deletion.
        archived.append({'path':relative,'copy':str(destination)})
    for relative in ('Assets/Unity.VisualScripting.Generated/VisualScripting.Core','Assets/Unity.VisualScripting.Generated/VisualScripting.Flow','Assets/Unity.VisualScripting.Generated','Assets/Resources'):
        directory=(root/relative).resolve()
        if not directory.is_relative_to(root):raise SystemExit('Directory escaped the project.')
        if directory.is_dir() and directory.stat().st_birthtime>=baseline_record['created'] and not any(directory.iterdir()):
            directory.rmdir()  # Empty newly generated directory only; refuses nonempty content.
archive_folder=scratch/'generated-support'
result={'checkedAt':datetime.datetime.now().isoformat(),'archivedNewEditorSupport':archived,'generatedSupportArchiveRoots':[str(path) for path in sorted(archive_folder.iterdir()) if path.is_dir()] if archive_folder.exists() else [],'unexpectedNewPaths':unexpected()}
(root/'ClientExports/YanYana/Reports/new-files-preservation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
