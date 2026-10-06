"""Blender sculpt authoring for three different story residents.

Approved anatomical templates supply the visual detail. Each resident changes
facial structure, body proportions and hair construction. No Unity writes.
"""
import bpy,bmesh,math,pathlib,sys,json,numpy as np,argparse
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/YanYana'))
from build_original_residents import color
from mathutils.bvhtree import BVHTree
from sculpt_eren_resident import mat,mesh_object,tube
OUT=ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop'
SPECS=[
 dict(name='Ece',source='Ada',jacket='#A77CAF',pants='#4D486A',hair='#39291F',age='child',height=1.18),
 dict(name='Aylin',source='Ada',jacket='#69745B',pants='#4B494A',hair='#67402C',age='adult',height=1.75),
 dict(name='Zeynep',source='Derya',jacket='#A9B653',pants='#465565',hair='#332A24',age='adult',height=1.62),
]
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def g(x,c,r):return math.exp(-((x-c)/r)**2)

def warp(p,s,hair_factor=0):
    x,y,z=p;cx=.012;q=Vector(p);name=s['name'];front=smooth((-y-.04)/.15)
    if name=='Ece':
        head=smooth((z-.91)/.07)
        q.x=cx+(x-cx)*(1+head*(-.035+.045*g(z,1.008,.060)+.025*g(z,.950,.035)))
        q.y-=.002*head*g(z,.990,.060)*front
        nose=g(x,cx,.035)*g(z,1.019,.029)*front;q.y+=.006*nose;q.z+=.002*nose
        q.z+=.007*head*g(z,.949,.040)
        for side in (-1,1):
            eye=g(x,cx+side*.061,.039)*g(z,1.055,.033)*front;q.x+=side*.001*eye
        q.x=cx+(q.x-cx)*(1-.025*(1-head));q.z=z*.79 if z<.92 else .92*.79+(q.z-.92)*.98
        if q.z<.749:
            neck=smooth((.749-q.z)/.032);q.x=q.x*(1-neck)+(cx+max(-.045,min(.045,q.x-cx)))*neck;q.y=q.y*(1-neck)+max(-.042,min(.055,q.y))*neck
        # Preserve the approved crown, hairline and sculpted knot volume.
    elif name=='Aylin':
        head=smooth((z-.91)/.07);q.x=cx+(x-cx)*(1+head*.01)
        q.x+=head*(x-cx)*(-.03*g(z,.958,.042)+.01*g(z,1.015,.042))
        nose=g(x,cx,.031)*g(z,1.017,.033)*front;q.y-=.003*nose
        q.x-=(x-cx)*.025*g(z,.976,.025)*front
        q.x=cx+(q.x-cx)*(1+.16*(1-head)*g(z,.69,.25))
        for side in (-1,1):
            leg=cx+side*.105;q.x+=(x-leg)*.30*g(x,leg,.092)*g(z,.34,.22)
        q.y*=1+.03*(1-head)*g(z,.58,.18)
        q.z=z*1.18 if z<.92 else .92*1.18+(z-.92)*1.045
        # Preserve the approved crown and individual locks at this head scale.
    else:
        head=smooth((z-1.08)/.10);q.x=cx+(x-cx)*(1+head*(.03*g(z,1.227,.068)+.01*g(z,1.36,.060)))
        q.y-=.002*front*g(z,1.27,.067)*head;nose=g(x,cx,.041)*g(z,1.315,.034)*front;q.y+=.003*nose
        q.x=cx+(q.x-cx)*(1+.055*(1-head)*g(z,.71,.20))
        # Keep the reference waves intact; a color-mask warp tore the lower locks.
        q.z-=.020*smooth(z/.70)
    return q

def image_colors(obj):
    im=next(n.image for n in obj.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE');px=np.array(im.pixels[:],dtype=np.float32).reshape(im.size[1],im.size[0],4)
    return im,px

def save_character_image(image,path):
    # Write to a fresh path before replacing an albedo that Blender may have
    # opened earlier in this process; retain the stable external asset name.
    path=pathlib.Path(path);pending=path.with_name(path.stem+'.writing.png')
    image.filepath_raw=str(pending);image.file_format='PNG';image.save()
    pending.replace(path);image.filepath_raw=str(path);image.pack()

def body_uv_mask(obj,h,w,top=None):
    names={g.index:g.name for g in obj.vertex_groups};weights=[sum(q.weight for q in v.groups if names[q.group] not in ('Head','LeftHand','RightHand')) for v in obj.data.vertices]
    headweights=[sum(q.weight for q in v.groups if names[q.group]=='Head') for v in obj.data.vertices]
    mask=np.zeros((h,w),dtype=bool);uv=obj.data.uv_layers.active;top=top if top is not None else (1.015 if max(v.co.z for v in obj.data.vertices)<1.5 else 1.17)
    for p in obj.data.polygons:
        if sum(weights[i] for i in p.vertices)/len(p.vertices)<.25:continue
        if sum(headweights[i] for i in p.vertices)/len(p.vertices)>.10:continue
        if max(obj.data.vertices[i].co.z for i in p.vertices)>top:continue
        if max(obj.data.vertices[i].co.z for i in p.vertices)<.055:continue
        t=np.array([(uv.data[l].uv.x*(w-1),uv.data[l].uv.y*(h-1)) for l in p.loop_indices]);a,b,c=t[:3]
        x0=max(0,int(np.floor(t[:,0].min())));x1=min(w-1,int(np.ceil(t[:,0].max())));y0=max(0,int(np.floor(t[:,1].min())));y1=min(h-1,int(np.ceil(t[:,1].max())))
        xx,yy=np.meshgrid(np.arange(x0,x1+1),np.arange(y0,y1+1));den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-8:continue
        u=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den;v=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den
        mask[y0:y1+1,x0:x1+1]|=(u>=-.01)&(v>=-.01)&(u+v<=1.01)
    # Pad one texel at UV seams.
    mask|=np.roll(mask,1,0)|np.roll(mask,-1,0)|np.roll(mask,1,1)|np.roll(mask,-1,1)
    return mask

def garment_palette(obj,s,out):
    if s['source']=='Derya':
        from resident_materials import approved_palette
        approved_palette(obj,s['name'],'Derya',s['jacket'],out)
        return
    original,px=image_colors(obj);bodymask=body_uv_mask(obj,*px.shape[:2]);r,gg,b=[px[:,:,i].copy() for i in range(3)];lum=px[:,:,:3].mean(axis=2)
    jacket=bodymask & ((r>gg*.97)&(b<gg*.68) if s['source']=='Ada' else (r>gg*1.10)&(b>gg*.91))
    lowmask=body_uv_mask(obj,*px.shape[:2],top=.64 if s['source']=='Ada' else .87)
    pants=lowmask &(gg>r*1.18)&(gg>b*.80) if s['source']=='Ada' else lowmask &(r<gg*1.19)&(b<gg*1.015)&(lum<.72)
    # Resolve clothing by UV-triangle region so original colored stitch pixels
    # cannot survive as isolated speckles inside a newly colored garment.
    original_pants=pants.copy();uv=obj.data.uv_layers.active;H,W=px.shape[:2]
    for face in obj.data.polygons:
        verts=[obj.data.vertices[i].co for i in face.vertices]
        z=sum(p.z for p in verts)/len(verts)
        if not (.055<z<(.64 if s['source']=='Ada' else .87)):continue
        if max(abs(p.x-.012) for p in verts)>.31:continue
        t=np.array([(uv.data[l].uv.x*(W-1),uv.data[l].uv.y*(H-1)) for l in face.loop_indices]);mid=t.mean(0);mi,mj=int(mid[1]),int(mid[0])
        if not original_pants[mi,mj] and not(z<(.30 if s['source']=='Ada' else .35) and gg[mi,mj]>r[mi,mj]*.88):continue
        a,bv,c=t[:3];x0=max(0,int(np.floor(t[:,0].min())));x1=min(W-1,int(np.ceil(t[:,0].max())));y0=max(0,int(np.floor(t[:,1].min())));y1=min(H-1,int(np.ceil(t[:,1].max())))
        xx,yy=np.meshgrid(np.arange(x0,x1+1),np.arange(y0,y1+1));den=(bv[1]-c[1])*(a[0]-c[0])+(c[0]-bv[0])*(a[1]-c[1])
        if abs(den)<1e-8:continue
        u=((bv[1]-c[1])*(xx-c[0])+(c[0]-bv[0])*(yy-c[1]))/den;v=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den
        pants[y0:y1+1,x0:x1+1]|=(u>=-.01)&(v>=-.01)&(u+v<=1.01)
    def paint(mask,hex,reference):
        target=np.array([int(hex[i:i+2],16)/255 for i in (1,3,5)]);shade=np.clip((lum[mask]/reference)**.88,.24,1.55);px[mask,:3]=np.clip(shade[:,None]*target,0,1)
    if s['name']!='Ece':
        if np.any(jacket):paint(jacket,s['jacket'],float(np.median(lum[jacket])))
        if np.any(pants):paint(pants,s['pants'],float(np.median(lum[pants])))
    # Hair region is explicitly outside the skeletal body UV mask.
    hair=(~bodymask)&(r>gg*1.16)&(b<r*.91)&(lum>.10)&(lum<.38)
    if np.any(hair):paint(hair,s['hair'],.215)
    im=original.copy()
    if im.packed_file:im.unpack(method='REMOVE')
    im.name=s['name']+' character albedo';im.pixels=px.flatten();save_character_image(im,out/(s['name']+'_Albedo.png'))
    mat=obj.data.materials[0].copy();obj.data.materials[0]=mat
    for n in mat.node_tree.nodes:
        if n.type=='TEX_IMAGE' and n.image==original:n.image=im


def ece_child_outfit(head,old_rig,s):
    # Retain the newly sculpted head while rebuilding a child's body proportions.
    # Source topology indices stay intact for native facial articulation.
    image,pixels=image_colors(head);oldUV=head.data.uv_layers.active
    keep=[]
    for p in head.data.polygons:
        zlo=min(head.data.vertices[i].co.z for i in p.vertices);zhi=max(head.data.vertices[i].co.z for i in p.vertices)
        if zlo<=.697:continue
        uv=sum((oldUV.data[l].uv for l in p.loop_indices),Vector((0,0)))/len(p.loop_indices);rgb=pixels[int(uv.y*(image.size[1]-1)),int(uv.x*(image.size[0]-1)),:3]
        x=sum(head.data.vertices[i].co.x for i in p.vertices)/len(p.vertices)
        if zhi<.80 and abs(x-.012)<.115 and rgb[2]<rgb[1]*.50:continue
        keep.append(p)
    old=head.data;new=bpy.data.meshes.new('Ece detailed head');new.from_pydata([v.co for v in old.vertices],[],[tuple(p.vertices) for p in keep]);new.update()
    for m in old.materials:new.materials.append(m)
    uv=new.uv_layers.new(name='OriginalUV')
    for p,src in zip(new.polygons,keep):
        p.use_smooth=True;p.material_index=src.material_index
        for li,si in zip(p.loop_indices,src.loop_indices):uv.data[li].uv=old.uv_layers.active.data[si].uv
    head.data=new;head.vertex_groups.clear();head.vertex_groups.new(name='Head').add(list(range(len(new.vertices))),1,'REPLACE')
    sourcepath=str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle/Efe.blend')
    with bpy.data.libraries.load(sourcepath,link=False) as (src,dst):dst.objects=src.objects
    for o in dst.objects:
        if o:bpy.context.collection.objects.link(o)
    child=next(o for o in dst.objects if o.type=='MESH');rig=next(o for o in dst.objects if o.type=='ARMATURE')
    child.shape_key_clear();bm=bmesh.new();bm.from_mesh(child.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z>.776],context='VERTS');bm.to_mesh(child.data);bm.free();child.data.update()
    child.name='Ece_ChildClothing';rig.name='Ece_Rig'
    image,pixels=image_colors(child);r,gg,b=[pixels[:,:,i].copy() for i in range(3)];lum=pixels[:,:,:3].mean(2)
    green=(gg>r*1.16)&(gg>b*.95);target=np.array([.47,.31,.62]);shade=np.clip((lum[green]/.43)**.85,.25,1.65);pixels[green,:3]=np.clip(shade[:,None]*target,0,1)
    image=image.copy()
    if image.packed_file:image.unpack(method='REMOVE')
    image.pixels=pixels.flatten();save_character_image(image,OUT/'Ece/Ece_HoodieAlbedo.png')
    material=child.data.materials[0].copy();child.data.materials[0]=material
    for n in material.node_tree.nodes:
        if n.type=='TEX_IMAGE':n.image=image
    # A layered, scalloped denim skirt with twelve broad pleats.
    denim=mat('Ece soft plum denim cloth','#575477');seam=mat('Ece skirt hem stitching','#BEB6CD');N=96;R=28;vs=[];fs=[]
    for j in range(R):
        t=j/(R-1);z=.466-.205*t;rx=.144+.055*t;ry=.127+.050*t
        for i in range(N):
            a=math.tau*i/N;pleat=1+.032*t*math.cos(a*12);vs.append(Vector((.012+rx*math.sin(a)*pleat,.016+ry*math.cos(a)*pleat,z+.004*t*math.cos(a*12))))
    for j in range(R-1):
        for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    ob=mesh_object('Ece pleated skirt',vs,fs,denim);ob.parent=rig;grp=ob.vertex_groups.new(name='Hips');grp.add(list(range(len(vs))),1,'REPLACE');mod=ob.modifiers.new('Skirt skin','ARMATURE');mod.object=rig;solid=ob.modifiers.new('Denim thickness','SOLIDIFY');solid.thickness=.0017
    hem=tube('Ece skirt stitched hem',[vs[(R-2)*N+i] for i in range(N)]+[vs[(R-2)*N]],.001,seam,None);hem.parent=rig;grp=hem.vertex_groups.new(name='Hips');grp.add(list(range(len(hem.data.vertices))),1,'REPLACE');mod=hem.modifiers.new('Hem skin','ARMATURE');mod.object=rig
    for o in list(bpy.context.scene.objects):
        if o.parent==old_rig:
            o.parent=rig
            for m in o.modifiers:
                if m.type=='ARMATURE':m.object=rig
    bpy.data.objects.remove(old_rig,do_unlink=True)
    bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones['Head'].head.z=.760;rig.data.edit_bones['Neck'].tail.z=.760;bpy.ops.object.mode_set(mode='OBJECT')
    return rig

def render(obj,s,out):
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=40
    if not sc.world:sc.world=bpy.data.worlds.new('Resident studio')
    sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.64,.69,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.30
    floor=bpy.data.materials.new('Studio floor');floor.diffuse_color=color('#DADCCF');bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.005));bpy.context.object.data.materials.append(floor)
    for name,pos,power,size in [('Key',(-3,-4,5),600,3),('Fill',(3,-2,3),170,3),('Rim',(1,3,4),300,2.5)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);sc.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
    d=bpy.data.cameras.new('Resident review');cam=bpy.data.objects.new('Resident review',d);sc.collection.objects.link(cam);sc.camera=cam;d.type='ORTHO'
    h=max((o.matrix_world@v.co).z for o in sc.objects if o.type=='MESH' and o.parent for v in o.data.vertices)
    d.ortho_scale=h*1.20;cam.location=(.10,-4,h*.70);cam.rotation_euler=(Vector((.014,0,h*.50))-cam.location).to_track_quat('-Z','Y').to_euler();sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.view_settings.exposure=-.15
    sc.render.resolution_x=760;sc.render.resolution_y=960;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(out/'FullBody.png');bpy.ops.render.render(write_still=True)

def main():
    p=argparse.ArgumentParser();p.add_argument('--names',nargs='*');p.add_argument('--skip-render',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    bpy.context.preferences.filepaths.save_version=0
    for s in SPECS:
        if args.names and s['name'] not in args.names:continue
        bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle'/(s['source']+'.blend')))
        obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');out=OUT/s['name'];out.mkdir(parents=True,exist_ok=True)
        image,pixels=image_colors(obj);hints=[0.0]*len(obj.data.vertices);group_names={g.index:g.name for g in obj.vertex_groups};uv=obj.data.uv_layers.active
        for loop in obj.data.loops:
            v=obj.data.vertices[loop.vertex_index];p=uv.data[loop.index].uv;c=pixels[int(p.y*(image.size[1]-1)),int(p.x*(image.size[0]-1)),:3]
            head_weight=sum(q.weight for q in v.groups if group_names[q.group]=='Head')
            brown=1.0 if c[0]>c[1]*1.12 and c[2]<c[1]*.98 else 0.0
            hints[v.index]=(brown if s['name']=='Zeynep' and v.co.z>1.025 else head_weight)*smooth((.62-float(c.mean()))/.14)
        # Hair is one continuous sculpted surface. Diffuse color classification
        # across mesh adjacency so UV color differences cannot tear its shape.
        if s['name']=='Zeynep':
            coordinates=np.round(np.array([v.co[:] for v in obj.data.vertices]),5)
            unique,inverse=np.unique(coordinates,axis=0,return_inverse=True)
            edges=np.array([(inverse[e.vertices[0]],inverse[e.vertices[1]]) for e in obj.data.edges]);a,b=edges.T
            values=np.bincount(inverse,weights=hints)/np.maximum(1,np.bincount(inverse))
            count=np.bincount(np.r_[a,b],minlength=len(values))
            for _ in range(40):
                total=np.bincount(np.r_[a,b],weights=np.r_[values[b],values[a]],minlength=len(values))
                values=.45*values+.55*total/np.maximum(1,count)
            hints=values[inverse].tolist()
        garment_palette(obj,s,out)
        if obj.data.shape_keys:
            for key in obj.data.shape_keys.key_blocks:
                for i,v in enumerate(key.data):v.co=warp(v.co,s,hints[i])
                key.value=0
            for v,k in zip(obj.data.vertices,obj.data.shape_keys.key_blocks[0].data):v.co=k.co
        else:
            for v in obj.data.vertices:v.co=warp(v.co,s,hints[v.index])
        obj.name=s['name']+'_SculptedBody';rig.name=s['name']+'_Rig'
        bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
        for b in rig.data.edit_bones:b.head=warp(b.head,s);b.tail=warp(b.tail,s)
        bpy.ops.object.mode_set(mode='OBJECT');bpy.context.view_layer.update()
        # Use the detailed approved hair mesh; remove the experimental sphere
        # buns and tubular ponytail instead of layering them over the reference.
        if s['name']=='Ece':rig=ece_child_outfit(obj,rig,s)
        if s['name'] in ('Ece','Zeynep'):
            from author_resident_identity import make_hair
            make_hair(obj,rig,s['name'],s['source'],mat)
        obj['authorship']='Blender resculpt; approved '+s['source']+' anatomical topology, individually sculpted proportions and wardrobe.'
        if s['name'] not in ('Ece','Zeynep'):obj['hair_reference']=s['source']+'.blend; no crown compression or color-mask deformation'
        rig['status']='Editable source; export and Unity validation are recorded in ReviewPack.'
        bpy.ops.wm.save_as_mainfile(filepath=str(out/(s['name']+'.blend')))
        if not args.skip_render:render(obj,s,out)
        (out/'review-status.json').write_text(json.dumps(dict(status='needs_visual_review',production_ready=False,integrated_into_scene=False,source_anatomy=s['source'],expression_rig_complete=False),indent=2))
        print('STORY_RESIDENT_SCULPT',s['name'],flush=True)

if __name__=='__main__':main()
