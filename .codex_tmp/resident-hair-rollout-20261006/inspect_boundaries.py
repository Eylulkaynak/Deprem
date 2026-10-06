import bpy,pathlib,numpy as np
from collections import defaultdict
for name in ('Kemal','Ece','Gul'):
 bpy.ops.wm.open_mainfile(filepath=str(pathlib.Path.cwd()/'ArtDirection/YanYana/Characters/ResidentWorkshop'/name/(name+'.blend')))
 ob=bpy.data.objects[name+'_SculptedBody'];coords=np.array([v.co[:] for v in ob.data.vertices]);unique,weld=np.unique(np.round(coords,5),axis=0,return_inverse=True)
 edges=defaultdict(list)
 for p in ob.data.polygons:
  vs=list(p.vertices)
  for a,b in zip(vs,vs[1:]+vs[:1]):
   a,b=int(weld[a]),int(weld[b]);edges[tuple(sorted((a,b)))].append((a,b))
 outer=[r[0] for r in edges.values() if len(r)==1];adj=defaultdict(set)
 for a,b in outer:adj[a].add(b);adj[b].add(a)
 seen=set();rows=[]
 for v in adj:
  if v in seen:continue
  stack=[v];component=[]
  while stack:
   i=stack.pop()
   if i in seen:continue
   seen.add(i);component.append(i);stack.extend(adj[i]-seen)
  points=unique[component]
  if len(points)>5:rows.append((len(points),np.round(points.min(0),3).tolist(),np.round(points.max(0),3).tolist(),sum(len(adj[i])!=2 for i in component)))
 print('BOUNDARIES',name,sorted(rows,reverse=True)[:18],flush=True)
