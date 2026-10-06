import bpy,pathlib,json,numpy as np
root=pathlib.Path.cwd();folder=root/'ArtDirection/YanYana/Characters/ApprovedStyle'
for name in ('Ada','Derya','Yusuf'):
 data=json.loads((folder/(name+'.mesh.json')).read_text(encoding='utf-8-sig'))['surfaces'][0]
 ps=np.array([(p['x'],-p['z'],p['y']) for p in data['vertices']]);uv=np.array([(p['x'],p['y']) for p in data['uv']]);im=bpy.data.images.load(str(folder/data['groups'][0]['texture']))
 pix=np.array(im.pixels[:]).reshape(im.size[1],im.size[0],4);col=pix[np.clip((uv[:,1]*(im.size[1]-1)).astype(int),0,im.size[1]-1),np.clip((uv[:,0]*(im.size[0]-1)).astype(int),0,im.size[0]-1),:3]
 trace=json.loads((root/'.codex_tmp'/(name.lower()+'-mouth-trace.json')).read_text());ez=sum(e['center'][1] for e in trace['eyes'])/2;cx=sum(e['center'][0] for e in trace['eyes'])/2
 hair=(ps[:,2]>ez-.35)&(col.mean(1)<.55)&(col[:,0]>col[:,1]*1.05)&(col[:,2]<col[:,1]*.98)
 print(name,'EYE',cx,ez,'BOUNDS',ps[hair].min(0).tolist(),ps[hair].max(0).tolist(),flush=True)
 for dz in (-.20,-.1,0,.1,.2,.3,.4):
  near=ps[hair & (abs(ps[:,2]-ez-dz)<.02)]
  if len(near):print('SLICE',dz,len(near),np.round(near.min(0),3).tolist(),np.round(near.max(0),3).tolist(),flush=True)
