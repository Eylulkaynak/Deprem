import re, yaml, json
from pathlib import Path
paths = re.findall(r'path: (.+\.unity)', Path('ProjectSettings/EditorBuildSettings.asset').read_text())
out={}
for filename in paths[1:]:
    source=Path(filename).read_text(encoding='utf-8-sig')
    blocks=re.split(r'(?m)^--- !u!\d+ &(\d+).*\n',source)
    objects={}; transforms={}; texts=[]; buttons=[]
    for n in range(1,len(blocks),2):
        ident, block=blocks[n:n+2]
        go=re.search(r'(?m)^  m_GameObject: \{fileID: (\d+)\}',block)
        if block.startswith('GameObject:'):
            objects[ident]=yaml.load(block,Loader=yaml.CSafeLoader)['GameObject'].get('m_Name','?')
        if block.startswith(('Transform:','RectTransform:')) and go:
            parent=re.search(r'(?m)^  m_Father: \{fileID: (\d+)\}',block)
            transforms[ident]=(go[1],parent[1] if parent else '0')
        if '  m_text:' in block and go:
            data=yaml.load(block,Loader=yaml.CSafeLoader)['MonoBehaviour']
            texts.append((go[1],str(data.get('m_text',''))))
    parents={go:parent for go,parent in transforms.values()}
    def path(go):
        name=str(objects.get(go,'?')); parent=parents.get(go,'0'); seen=set()
        while parent in transforms and parent not in seen:
            seen.add(parent); go,parent=transforms[parent]; name=str(objects.get(go,'?'))+'/'+name
        return name
    out[Path(filename).stem]=[{'path':path(go),'text':s} for go,s in texts]
Path('.codex_tmp/3d-text-inventory.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
for scene,items in out.items():
    print('\n'+scene, len(items))
    for item in items: print(item['path'].split('/')[-2:] ,'|',item['text'][:115].replace('\n',' / '))
