"""Read-only scope audit against the pre-production workspace snapshot."""
from pathlib import Path
import json,re,hashlib
root=Path(__file__).resolve().parents[2]
baseline=root/'.codex_tmp/kktc_visual_20260908/baseline'
literal=re.compile(r'"(?:\\.|[^"\\])*"')
report={'runtime_content_changes':[],'runtime_non_content_changes':[], 'missing_baseline':[]}
for path in (root/'Assets/Scripts/Story').rglob('*.cs'):
    before=baseline/path.relative_to(root)
    if not before.exists():
        report['missing_baseline'].append(str(path.relative_to(root)));continue
    old=before.read_text(encoding='utf-8-sig');new=path.read_text(encoding='utf-8-sig')
    if old==new:continue
    key='runtime_content_changes' if literal.sub('"CONTENT"',old)==literal.sub('"CONTENT"',new) else 'runtime_non_content_changes'
    report[key].append(str(path.relative_to(root)))
report['authored_prop_prefabs']=len(list((root/'Assets/Story/Art/KKTC/Prefabs').glob('*.prefab')))
report['refined_character_prefabs']=len(list((root/'Assets/Story/Art/KKTC/Characters').glob('*.prefab')))
(root/'ClientExports/KKTC/Reports/ScopeAudit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
