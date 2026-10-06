import bpy,pathlib,numpy as np
root=pathlib.Path.cwd()
for name in ('Kemal','Ece','Gul'):
 bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ResidentWorkshop'/name/(name+'.blend')))
 ob=next(o for o in bpy.context.scene.objects if o.name.startswith(name+' tailored hair silhouette'));d=ob.data
 blend=np.array([v.value for v in d.attributes['ForeheadBlend'].data]);cols=np.array([v.color[:] for v in d.color_attributes['RecipientForehead'].data]);on=blend>.5
 print(name,'BLEND',blend.min(),blend.max(),sum(on),'ATTRCOLOR',np.round(np.percentile(cols[on,:3],[0,50,100],axis=0),3).tolist(),flush=True)
 tex=next(n.image for n in d.materials[0].node_tree.nodes if n.type=='TEX_IMAGE');pix=np.array(tex.pixels[:]).reshape(tex.size[1],tex.size[0],4)
 uv={l.vertex_index:d.uv_layers.active.data[l.index].uv for l in d.loops};samples=[]
 for i in np.where(on)[0]:
  p=uv[i];samples.append(pix[int(p.y*(tex.size[1]-1)),int(p.x*(tex.size[0]-1)),:3])
 print(name,'BAKE',np.round(np.percentile(samples,[0,50,100],axis=0),3).tolist(),'NORMALS',np.round(np.mean([d.vertices[i].normal[:] for i in np.where(on)[0]],axis=0),3).tolist(),flush=True)
