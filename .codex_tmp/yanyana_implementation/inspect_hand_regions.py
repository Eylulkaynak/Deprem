from pathlib import Path
import json,math
root=Path(__file__).resolve().parents[2]
for name in ('Ada','Efe','Idil'):
 data=json.loads((root/f'ArtDirection/YanYana/Characters/ApprovedStyle/{name}.mesh.json').read_text(encoding='utf-8-sig'));s=data['surfaces'][0];v=s['vertices'];bones=s['bones'];n=len(v)
 print('\nCHARACTER',name,n)
 adjacency=[set() for _ in v]
 for g in s['groups']:
  t=g['indices']
  for i in range(0,len(t),3):
   a,b,c=t[i:i+3];adjacency[a].update((b,c));adjacency[b].update((a,c));adjacency[c].update((a,b))
 remaining=set(range(n));components=[]
 while remaining:
  seed=min(remaining);q=[seed];found=[];remaining.remove(seed)
  while q:
   i=q.pop();found.append(i)
   for j in adjacency[i]&remaining:remaining.remove(j);q.append(j)
  components.append((seed,found))
 for side in ('Right','Left'):
  hi=next(i for i,b in enumerate(bones) if b['name']==side+'Hand');li=next(i for i,b in enumerate(bones) if b['name']==side+'LowerArm');h=bones[hi]['point'];elbow=bones[li]['point']
  print(side,'hand',h,'elbow',elbow)
  rows=[]
  for seed,c in components:
   mean={k:sum(v[i][k] for i in c)/len(c) for k in ('x','y','z')};distance=sum((mean[k]-h[k])**2 for k in mean)**.5
   wt=sum(sum(w for b,w in zip(s['weights'][i]['bones'],s['weights'][i]['values']) if b in (hi,li)) for i in c)/len(c)
   if distance<.19 and wt>.1:
    extent=[round(max(v[i][k] for i in c)-min(v[i][k] for i in c),4) for k in mean]
    rows.append((round(distance,3),seed,len(c),{k:round(x,4) for k,x in mean.items()},extent,round(wt,2)))
  for row in sorted(rows):print(row)
