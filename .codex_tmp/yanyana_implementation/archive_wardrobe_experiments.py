"""Archive only our unreferenced wardrobe experiments; baseline files are prohibited."""
from pathlib import Path
import datetime, hashlib, json, re, shutil

root=Path(__file__).resolve().parents[2]
baseline=json.loads((root/'.codex_tmp/yanyana_implementation/baseline.json').read_text())['files']
owned=root/'Assets/YanYana'
names=[f'Assets/YanYana/Characters/Neighbors/{name}_WardrobeSurface.asset' for name in ('Asli','Deniz','Gul','Mert','Ozan','Selma','Yusuf')]
names+=['Assets/YanYana/Characters/Neighbors/Yusuf_SilverHair.mat','Assets/YanYana/Content/CharacterRegions.json','Tools/YanYana/build_character_region_masks.py']
paths=[]
for name in names:
    for rel in (name,name+'.meta'):
        if rel in baseline:raise RuntimeError('Baseline file is prohibited: '+rel)
        path=(root/rel).resolve()
        if not path.is_relative_to(root):raise RuntimeError('Path escaped workspace')
        if path.is_file():paths.append(path)
guids=[]
for path in paths:
    if path.suffix=='.meta':
        match=re.search(r'^guid: (\w+)',path.read_text(),re.M)
        if match:guids.append(match.group(1))
for path in owned.rglob('*'):
    if path.is_file() and path not in paths and path.suffix in ('.unity','.prefab','.asset','.mat','.controller','.overrideController'):
        text=path.read_text(encoding='utf-8',errors='ignore')
        if any(guid in text for guid in guids):raise RuntimeError('Still referenced by '+str(path))
archive=root/'.codex_tmp/yanyana_implementation/wardrobe-experiments'/datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
rows=[]
for source in paths:
    target=archive/source.relative_to(root);target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,target)
    digest=hashlib.sha256(source.read_bytes()).hexdigest()
    if digest!=hashlib.sha256(target.read_bytes()).hexdigest():raise RuntimeError('Copy verification failed')
    source.unlink()
    rows.append(dict(source=str(source.relative_to(root)),archive=str(target),sha256=digest))
report=root/'ClientExports/YanYana/Reports/wardrobe-experiment-archive.json'
report.write_text(json.dumps(rows,indent=2),encoding='utf-8')
print('Archived',len(rows),'unreferenced new files; no baseline paths touched.')
