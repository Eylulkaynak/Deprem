"""Read-only anatomical landmark measurements for Blender sculpt authoring."""
import bpy,pathlib,numpy as np,json
root=pathlib.Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ApprovedStyle/Emre.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');uv=o.data.uv_layers.active
im=next(n.image for n in o.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE');pix=np.array(im.pixels[:]).reshape(im.size[1],im.size[0],4)
rows={}
for l in o.data.loops:
 p=uv.data[l.index].uv;v=o.data.vertices[l.vertex_index].co;c=pix[int(p.y*(im.size[1]-1)),int(p.x*(im.size[0]-1)),:3];rows[l.vertex_index]=(*v,*c)
a=np.array(list(rows.values()));bins=[]
for z in np.arange(1.16,1.751,.020):
 b=a[(a[:,2]>z-.01)&(a[:,2]<=z+.01)];f=b[abs(b[:,0]-.018)<.045];f=f[np.argsort(f[:,1])[:8]]
 if not len(f):continue
 bins.append({'z':round(float(z),3),'front_y':round(float(f[:,1].mean()),4),'rgb':[round(float(x),3) for x in f[:,3:].mean(0)],'width':round(float(b[:,0].max()-b[:,0].min()),3)})
white=a[(a[:,2]>1.28)&(a[:,2]<1.48)&(a[:,1]<-.18)&(a[:,3]>.80)&(a[:,4]>.78)&(a[:,5]>.71)]
report={'bins':bins,'eye_white_bounds':[(float(white[:,i].min()),float(white[:,i].max())) for i in range(3)] if len(white) else [],'material':[(n.name,n.type) for n in o.data.materials[0].node_tree.nodes],'image_colorspace':im.colorspace_settings.name,'pose_nonidentity':[p.name for ob in bpy.context.scene.objects if ob.type=='ARMATURE' for p in ob.pose.bones if any(abs(p.matrix_basis[i][j]-(1 if i==j else 0))>.00001 for i in range(4) for j in range(4))]}
(root/'.codex_tmp/eren-template-landmarks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
