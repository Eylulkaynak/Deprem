"""Measure approved mesh anatomy before sculpting. Read-only source operation."""
import bpy,pathlib,sys,json,numpy as np
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'Tools/YanYana'))
from build_original_residents import stage
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ApprovedStyle/Efe.blend'))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');uv=obj.data.uv_layers.active
img=next(n.image for n in obj.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE');pixels=np.array(img.pixels[:],dtype=np.float32).reshape((img.size[1],img.size[0],4));rows=[]
seen=set()
for loop in obj.data.loops:
    vi=loop.vertex_index
    if vi in seen:continue
    seen.add(vi);v=obj.data.vertices[vi].co
    if v.z<.76:continue
    p=uv.data[loop.index].uv;c=pixels[int(p.y*(img.size[1]-1)),int(p.x*(img.size[0]-1)),:3];rows.append((v.x,v.y,v.z,*c))
a=np.array(rows);bins=[]
for z in np.arange(.80,1.201,.025):
    b=a[(a[:,2]>=z-.0125)&(a[:,2]<z+.0125)];front=b[(abs(b[:,0]-.012)<.06)]
    if len(front)==0:continue
    front=front[np.argsort(front[:,1])[:10]]
    bins.append({'z':round(float(z),3),'front_y':round(float(front[:,1].mean()),4),'center_rgb':[round(float(x),3) for x in front[:,3:].mean(0)],'extent_x':[round(float(b[:,0].min()),3),round(float(b[:,0].max()),3)]})
(root/'.codex_tmp/approved-face-measurements.json').write_text(json.dumps(bins,indent=2))
print(json.dumps(bins))
camera=stage(obj,None);sc=bpy.context.scene;camera.data.ortho_scale=.60;camera.location=(0,-4,1.01);camera.rotation_euler=(Vector((0,0,1.01))-camera.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x=720;sc.render.resolution_y=720;sc.render.resolution_percentage=100
sc.render.filepath=str(root/'.codex_tmp/approved-efe-face.png');bpy.ops.render.render(write_still=True)
