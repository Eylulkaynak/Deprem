from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
for name in ('Ada','Efe','Idil'):
 data=json.loads((root/f'ArtDirection/YanYana/Characters/ApprovedStyle/{name}.mesh.json').read_text(encoding='utf-8-sig'));s=data['surfaces'][0];v=s['vertices'];bones=s['bones']
 print(name)
 for side in ('Right','Left'):
  for part in ('Shoulder',):
   bi=next(i for i,b in enumerate(bones) if b['name']==side+part)
   indices=[i for i,w in enumerate(s['weights']) if sum(ww for b,ww in zip(w['bones'],w['values']) if b==bi)>.55]
   mean={k:round(sum(v[i][k] for i in indices)/len(indices),3) for k in ('x','y','z')}
   ranges={k:(round(min(v[i][k] for i in indices),3),round(max(v[i][k] for i in indices),3)) for k in ('x','y','z')}
   print(side+part,'bone',bones[bi]['point'],'mean',mean,'range',ranges,'n',len(indices))
