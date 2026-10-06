"""Offline Blender resculpt using the approved anatomical/material templates.

This preserves the actual family's sculptural detail while changing facial
structure, silhouette and garment construction. No Unity import is performed.
"""
import bpy,bmesh,math,pathlib,sys,json,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/YanYana'))
from build_original_residents import stage,color
OUT=ROOT/'ArtDirection/YanYana/Characters/FamilyResculpt/Ece'

def gaussian(x,z,cx,cz,rx,rz):return math.exp(-((x-cx)/rx)**2-((z-cz)/rz)**2)
def clamp(x):return max(0,min(1,x))
def texcolors(obj):
    image=next(n.image for n in obj.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
    pix=np.array(image.pixels[:],dtype=np.float32).reshape(image.size[1],image.size[0],4);uv=obj.data.uv_layers.active;cols={}
    for loop in obj.data.loops:
        p=uv.data[loop.index].uv;cols[loop.vertex_index]=pix[int(p.y*(image.size[1]-1)),int(p.x*(image.size[0]-1)),:3]
    return image,pix,cols

def remove_vertices(obj,predicate):
    bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if predicate(v.index,v.co)],context='VERTS');bm.to_mesh(obj.data);bm.free();obj.data.update()

def warp(p):
    x,y,z=p;cx=.012;front=clamp((-y-.045)/.12);head=clamp((z-.71)/.08)
    q=Vector((x,y,z))
    # Broader cheek pads, softer compact chin and a smaller upturned nose.
    cheek=math.exp(-((z-.857)/.059)**2);chin=math.exp(-((z-.786)/.036)**2)
    q.x=cx+(x-cx)*(1+head*(.14*cheek-.065*chin))
    q.y-=.010*cheek*front*head;q.z=.89+(z-.89)*(.965 if head>.95 else 1)
    nose=gaussian(x,z,cx,.858,.034,.032)*front
    q.y+=.010*nose;q.z+=.004*nose
    # Move and reshape the actual eye anatomy and its UVs together.
    for side in (-1,1):
        ex=cx+side*.066;eye=gaussian(x,z,ex,.910,.048,.043)*front
        q.x+=eye*(side*.006+(x-ex)*.085);q.z+=eye*(z-.910)*.055
    mouth=gaussian(x,z,cx,.817,.071,.031)*front
    q.x-=mouth*(x-cx)*.14;q.z-=.0015*mouth
    # A shorter child silhouette, with the entire rig warped identically.
    body=clamp((.785-z)/.13);q.x=cx+(q.x-cx)*(1-.10*body)
    q.z-=.055*clamp(z/.73)
    return q

def material(name,hex):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=color(hex);p.inputs['Roughness'].default_value=.77;m.diffuse_color=color(hex)
    return m

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle/Efe.blend'))
    bpy.context.preferences.filepaths.save_version=0
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body.shape_key_clear()
    old_image,pixels,cols=texcolors(body)
    # Drop the prior hairstyle; keep the detailed face, ears, hands and clothing.
    def discard(i,p):
        x,y,z=p;c=cols[i];dark=float(c.mean())<.40
        face_feature=y<-.12 and .785<z<.980 and abs(x-.012)<.143
        hair=dark and z>.870 and not face_feature
        hood=z>.711 and c[1]>c[0]*1.16 and c[1]>c[2]*.98
        return hair or hood
    remove_vertices(body,discard)
    for v in body.data.vertices:v.co=warp(v.co)
    body.name='Ece_SculptedAnatomy';rig.name='Ece_Rig'
    bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    for bone in rig.data.edit_bones:bone.head=warp(bone.head);bone.tail=warp(bone.tail)
    bpy.ops.object.mode_set(mode='OBJECT')
    # Duplicate the image so the approved source texture is never edited.
    copy=old_image.copy();copy.name='Ece_NewGarmentTexture'
    mask=(pixels[:,:,1]>pixels[:,:,0]*1.16)&(pixels[:,:,1]>pixels[:,:,2]*.98)
    lum=pixels[:,:,:3].mean(axis=2);target=np.array([.59,.43,.73]);pixels[mask,:3]=(lum[mask,None]/.45*target).clip(0,1)
    copy.pixels=pixels.flatten();copy.filepath_raw=str(OUT/'Ece_Garments.png');copy.file_format='PNG';copy.save()
    mat=body.data.materials[0].copy();body.data.materials[0]=mat
    for n in mat.node_tree.nodes:
        if n.type=='TEX_IMAGE' and n.image==old_image:n.image=copy
    # Load only Derya's sculptural hair template, preserving its modeled layers.
    with bpy.data.libraries.load(str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle/Derya.blend'),link=False) as (src,dst):dst.objects=['Anne.001']
    hair=dst.objects[0];bpy.context.scene.collection.objects.link(hair);hair.parent=None;hair.modifiers.clear();hair.shape_key_clear()
    hi,hpx,hcol=texcolors(hair)
    candidates=[]
    for v in hair.data.vertices:
        x,y,z=v.co;c=hcol[v.index]
        dark=float(c.mean())<.42 and c[0]>c[2]*1.02
        feature=y<-.17 and 1.11<z<1.405 and abs(x-.016)<.16
        if z>1.03 and dark and not feature:candidates.append(v.index)
    chosen=set(candidates)
    remove_vertices(hair,lambda i,p:i not in chosen)
    print('HAIR_SOURCE_BOUNDS',[(min(v.co[k] for v in hair.data.vertices),max(v.co[k] for v in hair.data.vertices)) for k in range(3)],flush=True)
    for v in hair.data.vertices:
        x,y,z=v.co
        # Wavy cropped bob: a new silhouette, neither the original long cut nor a cap.
        x=.012+(x-.016)*.88;y=y*.79-.013
        zz=.977+(z-1.414)*.91
        if zz<.977:zz=.977+(zz-.977)*.43
        v.co=Vector((x,y,zz-.055))
    hair.name='Ece_ResculptedLayeredBob'
    hair.vertex_groups.clear();g=hair.vertex_groups.new(name='Head');g.add(list(range(len(hair.data.vertices))),1,'REPLACE')
    mod=hair.modifiers.new('Head skin','ARMATURE');mod.object=rig;hair.parent=rig
    # Add a fitted cloth overall front and real stitched edges over the source drape.
    bvh=BVHTree.FromPolygons([v.co.copy() for v in body.data.vertices],[tuple(p.vertices) for p in body.data.polygons])
    def front(x,z):
        loc,normal,index,dist=bvh.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
        return loc.y-.006 if loc else -.10
    cloth=material('Ece deep plum cotton','#5C405F');stitch=material('Ece warm topstitch','#BCAA85');metal=material('Ece antique brass buckle','#C9A977')
    verts=[];faces=[];N=24;R=28
    for j in range(R):
        t=j/(R-1);z=.295+.292*t;w=.102-.031*clamp((t-.58)/.42)
        for i in range(N):
            u=-1+2*i/(N-1);x=.010+u*w;zz=z-.013*abs(u)**6*clamp((t-.9)/.1);verts.append((x,front(x,zz),zz))
    for j in range(R-1):
        for i in range(N-1):faces.append((j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i))
    me=bpy.data.meshes.new('Ece tailored overall front');me.from_pydata(verts,[],faces);me.update();bib=bpy.data.objects.new('Ece tailored overall front',me);bpy.context.collection.objects.link(bib);me.materials.append(cloth)
    for p in me.polygons:p.use_smooth=True
    solid=bib.modifiers.new('Cloth thickness','SOLIDIFY');solid.thickness=.0015
    group=bib.vertex_groups.new(name='Spine');group.add(list(range(len(verts))),1,'REPLACE');m=bib.modifiers.new('Garment skin','ARMATURE');m.object=rig;bib.parent=rig
    def ribbon(name,points,width,mat):
        vs=[];fs=[]
        for x,y,z in points:vs.extend([(x-width/2,y,z),(x+width/2,y,z)])
        for i in range(len(points)-1):fs.append((2*i,2*i+1,2*i+3,2*i+2))
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);mesh.materials.append(mat)
        for p in mesh.polygons:p.use_smooth=True
        sol=o.modifiers.new('Woven strap thickness','SOLIDIFY');sol.thickness=.002
        g=o.vertex_groups.new(name='Chest');g.add(list(range(len(vs))),1,'REPLACE');m=o.modifiers.new('Strap skin','ARMATURE');m.object=rig;o.parent=rig
        return o
    for side in (-1,1):
        points=[]
        for j in range(25):
            t=j/24;z=.565+.069*t;x=.011+side*(.055+.015*t);points.append((x,front(x,z)-.002,z))
        ribbon('Ece flat overall strap',points,.015,cloth)
    body['source_template']='Approved Efe anatomy. Face, nose, cheeks, eye placement and body proportions resculpted.'
    hair['source_template']='Approved Derya hair surface, resculpted into a short layered bob.'
    rig['status']='Single unapproved art study. Outside Unity. Facial animation is not integrated.'
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Ece_Resculpt.blend'))
    cam=stage(body,rig);sc=bpy.context.scene;sc.cycles.samples=48;sc.render.resolution_x=760;sc.render.resolution_y=960;sc.render.resolution_percentage=100
    cam.data.ortho_scale=1.36;cam.location=(.12,-4,.84);cam.rotation_euler=(Vector((.01,0,.56))-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/'FullBody.png');bpy.ops.render.render(write_still=True)
    cam.data.ortho_scale=.60;cam.location=(.055,-4,.954);cam.rotation_euler=(Vector((.012,0,.90))-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x=760;sc.render.resolution_y=760;sc.render.filepath=str(OUT/'Face.png');bpy.ops.render.render(write_still=True)
    (OUT/'review-status.json').write_text(json.dumps({'status':'unapproved_sculpt_study','integrated_into_scene':False,'do_not_import':True,'facial_animation':False,'templates':['Efe anatomical mesh','Derya hair sculpt']},indent=2))
    print('FAMILY_RESculpt_READY',OUT,flush=True)

if __name__=='__main__':main()
