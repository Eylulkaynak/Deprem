"""Sculpt a distinct head using the approved topology as an anatomical template.

This is a single art study, not a production batch or a reskin. It stays outside
Assets. The original approved source is never overwritten.
"""
import bpy,bmesh,math,pathlib,sys,numpy as np
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'Tools/YanYana'))
from build_original_residents import stage,color
src=root/'ArtDirection/YanYana/Characters/ApprovedStyle/Efe.blend'
bpy.ops.wm.open_mainfile(filepath=str(src));sc=bpy.context.scene
obj=next(o for o in sc.objects if o.type=='MESH');rig=next(o for o in sc.objects if o.type=='ARMATURE')
obj.name='Ece_SculptedHeadStudy';obj.shape_key_clear();obj.modifiers.clear();obj.parent=None
img=next(n.image for n in obj.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
pixels=np.array(img.pixels[:],dtype=np.float32).reshape((img.size[1],img.size[0],4));uv=obj.data.uv_layers.active
vertcol={}
for loop in obj.data.loops:
    p=uv.data[loop.index].uv;vertcol[loop.vertex_index]=pixels[int(p.y*(img.size[1]-1)),int(p.x*(img.size[0]-1)),:3]
# Remove the entire old hairstyle and body from this head study.
mesh=bmesh.new();mesh.from_mesh(obj.data);mesh.verts.ensure_lookup_table();drop=[]
for v in mesh.verts:
    x,y,z=v.co;c=vertcol.get(v.index,np.ones(3));dark=float(c.mean())<.35
    is_hair=dark and (z>1.005 or (z>.89 and y>-.035) or (z>.95 and abs(x-.012)>.14))
    old_clothes=z<.85 and c[1]>c[0]*1.15
    if z<.745 or is_hair or old_clothes:drop.append(v)
bmesh.ops.delete(mesh,geom=drop,context='VERTS');mesh.to_mesh(obj.data);mesh.free()
# Cheek volume, rounded jaw, shorter face and a smaller upturned nose create
# a new facial structure while preserving the approved anatomical surface.
for v in obj.data.vertices:
    x,y,z=v.co;xx=x-.012
    cheek=math.exp(-((z-.865)/.078)**2);forehead=math.exp(-((z-1.01)/.07)**2)
    v.co.x=.012+xx*(1+.19*cheek-.035*forehead)
    v.co.z=.895+(z-.895)*.92
    front=max(0,min(1,(-y-.06)/.15));v.co.y=y-.010*cheek*front
    nose=math.exp(-(xx/.036)**2-((z-.86)/.027)**2)*front
    v.co.y+=.009*nose;v.co.z+=.005*nose
for poly in obj.data.polygons:poly.use_smooth=True
bpy.data.objects.remove(rig,do_unlink=True)

hair=bpy.data.materials.new('Ece warm dark chestnut');hair.use_nodes=True
bs=hair.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=color('#482F26');bs.inputs['Roughness'].default_value=.78;bs.inputs['Specular IOR Level'].default_value=.18
accent=bpy.data.materials.new('Ece chestnut strand highlights');accent.use_nodes=True;accent.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=color('#483832');accent.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.59
cx=.012;cz=.965;wx=.177;dy=.176;hh=.185
def strand(name,points,width,depth,mat):
    verts=[];faces=[];N=12
    for j,p in enumerate(points):
        p=Vector(p);normal=Vector((p.x-cx,p.y,0)).normalized();across=Vector((-normal.y,normal.x,0));t=j/(len(points)-1);taper=.18+.82*math.sin(math.pi*(.12+.86*t))
        for i in range(N):
            a=i*math.tau/N;verts.append(p+across*(math.cos(a)*width*taper)+normal*(math.sin(a)*depth*taper))
    for j in range(len(points)-1):
        for i in range(N):faces.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    faces.extend([tuple(reversed(range(N))),tuple((len(points)-1)*N+i for i in range(N))]);me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);me.materials.append(mat)
    for f in me.polygons:f.use_smooth=True
    return o
# A continuous closed bob with shallow sculpted partings.
verts=[];faces=[];N=128;R=40
for j in range(R+1):
    t=j/R
    for i in range(N):
        a=-math.pi+math.tau*i/N;aa=abs(a)
        transition=max(0,min(1,(aa-.92)/.50));transition=transition*transition*(3-2*transition)
        bottom=(1-transition)*(.988+.009*math.cos(a*5))+transition*(.773+.008*math.cos(a*3))
        z=1.153+(bottom-1.153)*t
        if z>=.972:rad=math.sqrt(max(.0001,1-((z-.972)/.181)**2))
        else:rad=1.0+.065*math.sin((.972-z)/.205*math.pi)
        groove=.010*math.cos(a*29+.24*math.sin(t*math.pi))*math.sin(t*math.pi*.85)
        rad+=groove
        x=cx+math.sin(a)*.207*rad;y=-math.cos(a)*(.228 if math.cos(a)>0 else .188)*rad+.004
        verts.append((x,y,z))
for j in range(R):
    for i in range(N):faces.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
faces.append(tuple(reversed(range(N))))
# Curl the lower edge inward and close it, keeping the back of the hairstyle solid.
for i in range(N):
    x,y,z=verts[R*N+i];verts.append((cx+(x-cx)*.93,y*.93,z+.012))
for i in range(N):faces.append((R*N+i,R*N+(i+1)%N,(R+1)*N+(i+1)%N,(R+1)*N+i))
me=bpy.data.meshes.new('Ece continuous bob');me.from_pydata(verts,[],faces);me.update();bob=bpy.data.objects.new('Ece continuous bob',me);sc.collection.objects.link(bob);me.materials.append(hair)
for f in me.polygons:f.use_smooth=True
sub=bob.modifiers.new('Soft hair contour','SUBSURF');sub.levels=1
# A small modeled hair clip provides a deliberate asymmetrical detail.
clipmat=bpy.data.materials.new('Ece coral hair clip');clipmat.diffuse_color=color('#CB695A')
curve=bpy.data.curves.new('Hair clip outline','CURVE');curve.dimensions='3D';curve.bevel_depth=.003;curve.bevel_resolution=3
spl=curve.splines.new('POLY');points=[(-.157+.006*math.cos(i*math.tau/32),-.169-.003*math.sin(i*math.tau/32),1.027+.019*math.sin(i*math.tau/32)) for i in range(33)];spl.points.add(len(points)-1)
for p,xyz in zip(spl.points,points):p.co=(*xyz,1)
clip=bpy.data.objects.new('Coral clip',curve);sc.collection.objects.link(clip);curve.materials.append(clipmat)
out=root/'ArtDirection/YanYana/Characters/StyleStudy';out.mkdir(parents=True,exist_ok=True)
obj['source_anatomy']='Approved Efe topology; cheek, jaw, cranium and nose resculpted; original hair removed.'
obj['status']='Single unapproved sculpt study; outside Unity Assets.'
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(out/'Ece_SculptStudy.blend'))
camera=stage(obj,None);camera.data.ortho_scale=.56;camera.location=(.012,-4,.959);camera.rotation_euler=(Vector((.012,0,.945))-camera.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x=720;sc.render.resolution_y=720;sc.render.resolution_percentage=100
sc.render.filepath=str(out/'Ece_SculptStudy.png');bpy.ops.render.render(write_still=True)
print('SINGLE_SCULPT_STUDY',out)
