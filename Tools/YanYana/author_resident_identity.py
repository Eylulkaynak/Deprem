"""Blender-only, individually designed hair meshes with preserved facial indices."""
import bpy,math,json,pathlib,numpy as np
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
REF=ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle'

def smooth(x):
    x=max(0,min(1,x));return x*x*(3-2*x)

def reference(name):
    data=json.loads((REF/(name+'.mesh.json')).read_text(encoding='utf-8-sig'))['surfaces'][0]
    points=[Vector((p['x'],-p['z'],p['y'])) for p in data['vertices']]
    image=bpy.data.images.load(str(REF/data['groups'][0]['texture']),check_existing=True)
    pixels=np.array(image.pixels[:],dtype=np.float32).reshape(image.size[1],image.size[0],4)
    uv=data['uv'];colors=[]
    for p in uv:
        u,v=(p['x'],p['y']) if isinstance(p,dict) else p
        colors.append(pixels[int(max(0,min(1,v))*(image.size[1]-1)),int(max(0,min(1,u))*(image.size[0]-1)),:3])
    trace=json.loads((ROOT/'.codex_tmp'/('eren-mouth-trace.json' if name=='Emre' else name.lower()+'-mouth-trace.json')).read_text())
    eyes=trace['eyes'];ez=sum(e['center'][1] for e in eyes)/2;cx=sum(e['center'][0] for e in eyes)/2
    scale=(eyes[1]['center'][0]-eyes[0]['center'][0])/.1314615
    return points,colors,cx,ez,scale

def eye_anchor(body,source,name):
    points,colors,cx,ez,scale=reference(source)
    if len(points)!=len(body.data.vertices):
        raise RuntimeError('Hair authoring must precede facial remeshing: '+name)
    weights=[math.exp(-((p.z-ez)/(.038*scale))**2)*math.exp(-((abs(p.x-cx)-.065*scale)/(.035*scale))**2) if p.y<-.13*scale else 0 for p in points]
    return sum((v.co*w for v,w in zip(body.data.vertices,weights)),Vector())/sum(weights)

def make_hair(body,rig,name,source,material_factory):
    eye=eye_anchor(body,source,name)
    # Remove inherited arm influence from the ears and hair, with a gradual
    # transition through the neck. The face stays a continuous original mesh.
    head_group=body.vertex_groups.get('Head')
    for vertex in body.data.vertices:
        amount=smooth((vertex.co.z-eye.z+.25)/.11)
        if amount<=0:continue
        weights=[(entry.group,entry.weight) for entry in vertex.groups]
        original_head=sum(weight for index,weight in weights if index==head_group.index)
        for index,weight in weights:
            if index==head_group.index:continue
            value=weight*(1-amount)
            if value<.00001:body.vertex_groups[index].remove([vertex.index])
            else:body.vertex_groups[index].add([vertex.index],value,'REPLACE')
        head_group.add([vertex.index],amount+original_head*(1-amount),'REPLACE')
    from connected_resident_hair import sculpt_connected_hair
    sculpt_connected_hair(body,rig,name,source,eye)

