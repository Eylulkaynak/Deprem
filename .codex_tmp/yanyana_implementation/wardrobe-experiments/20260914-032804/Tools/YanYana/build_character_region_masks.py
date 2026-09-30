"""Read existing albedo/geometry to assign garment materials; no bitmap is modified."""
import json,pathlib,collections
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[2]
src=root/'ArtDirection/YanYana/Characters/ApprovedStyle'
regions=[]
for name in ('Yusuf','Emre','Derya'):
    s=json.loads((src/(name+'.mesh.json')).read_text(encoding='utf-8-sig'))['surfaces'][0]
    im=Image.open(src/s['groups'][0]['texture']).convert('RGB');w,h=im.size
    tris=s['groups'][0]['indices'];v=s['vertices'];uv=s['uv']
    ys=[p['y'] for p in v];height=max(ys)-min(ys)
    adj=collections.defaultdict(set)
    for k in range(0,len(tris),3):
        x,y,z=tris[k:k+3];adj[x].update((y,z));adj[y].update((x,z));adj[z].update((x,y))
    seen=set();hair=set()
    for start in adj:
        if start in seen:continue
        todo=[start];seen.add(start);part=[]
        while todo:
            i=todo.pop();part.append(i)
            for j in adj[i]:
                if j not in seen:seen.add(j);todo.append(j)
        if min(v[i]['y'] for i in part)>height*.825 and len(part)>100:hair.update(part)
    original=[];garment=[];grey=[]
    for k in range(0,len(tris),3):
        ids=tris[k:k+3]
        if all(i in hair for i in ids):grey.extend(ids);continue
        u=sum(uv[i]['x'] for i in ids)/3;vv=sum(uv[i]['y'] for i in ids)/3
        r,g,b=im.getpixel((max(0,min(w-1,int(u*(w-1)))),max(0,min(h-1,int((1-vv)*(h-1))))))
        y=sum(v[i]['y'] for i in ids)/3
        is_cloth=(b>r*.87 and b>g*.95) if name!='Derya' else (r>g*1.23 and r>b*1.05 and b>g*.80)
        (garment if height*.38<y<height*.74 and is_cloth else original).extend(ids)
    regions.append(dict(name=name,original=original,garment=garment,hair=grey))
    print(name,'triangles original/garment/hair',*[len(a)//3 for a in (original,garment,grey)])
(root/'Assets/YanYana/Content/CharacterRegions.json').write_text(json.dumps(dict(regions=regions),separators=(',',':')),encoding='utf-8')
