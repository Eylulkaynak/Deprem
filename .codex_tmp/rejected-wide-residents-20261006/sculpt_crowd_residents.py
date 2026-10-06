"""Four additional anatomical resculpts and original fitted garments in Blender.
No files are written to Unity Assets and no runtime scripts are generated.
"""
import bpy,math,pathlib,sys,json,numpy as np,argparse,bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/YanYana'))
from sculpt_story_residents import smooth,g,image_colors,render
from sculpt_eren_resident import mat,mesh_object,tube
OUT=ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop'
SPECS=[dict(name='Deniz',source='Emre',shirt='#AD6549',pants='#384D57'),dict(name='Gul',source='Derya',shirt='#698C86',pants='#444953'),dict(name='Kemal',source='Yusuf',shirt='#9EA6AA',pants='#666257'),dict(name='Arda',source='Efe',shirt='#D2A24B',pants='#476976')]

def warp(p,name):
    x,y,z=p;cx=.018;q=Vector(p);front=smooth((-y-.045)/.14)
    if name=='Deniz':
        head=smooth((z-1.17)/.085);q.x=cx+(x-cx)*(1+head*(-.12+.07*g(z,1.36,.085)))
        q.y+=.017*g(x,cx,.034)*g(z,1.35,.043)*front
        for side in (-1,1):q.x+=side*.006*g(x,cx+side*.070,.039)*g(z,1.407,.036)*front
        q.x=cx+(q.x-cx)*(1-.17*(1-head)*g(z,.80,.23))
        q.y*=1-.13*(1-head)*g(z,.8,.25)
        hair=smooth((z-1.54)/.11);q.x+=.045*hair;q.z-=.18*hair*max(0,z-1.54)
        q.z=q.z*1.055 if z<1.16 else 1.16*1.055+(q.z-1.16)*.94
    elif name=='Gul':
        head=smooth((z-1.07)/.09);q.x=cx+(x-cx)*(1+head*(.25*g(z,1.235,.085)+.10*g(z,1.33,.06)))
        q.y-=.014*g(z,1.25,.095)*g(x,cx,.15)*front
        q.y+=.012*g(x,cx,.037)*g(z,1.276,.038)*front
        for ex in (-.044,.098):q.z-=(z-1.330)*.20*g(x,ex,.04)*g(z,1.330,.038)*front
        body=1-head;q.x=cx+(q.x-cx)*(1+.22*body*g(z,.71,.22));q.y=.015+(q.y-.015)*(1+.15*body*g(z,.75,.24))
        q.z=q.z*.92 if z<1.09 else 1.09*.92+(q.z-1.09)*1.015
        q.z-=.060*smooth((z-1.43)/.14)
    elif name=='Kemal':
        head=smooth((z-1.10)/.09);q.x=cx+(x-cx)*(1-head*.12)
        q.x+=(x-cx)*head*.10*g(z,1.235,.060);q.z-=.009*head*g(z,1.21,.046)*front
        nose=g(x,cx,.029)*g(z,1.29,.040)*front;q.y-=.016*nose;q.z-=.006*nose
        body=1-head;q.x=cx+(q.x-cx)*(1-.12*body*g(z,.80,.23))
        q.y-=.045*smooth((z-.64)/.62);q.z*=.965
        hair=smooth((z-1.48)/.11);q.z-=.49*hair*max(0,z-1.48)
    else:
        head=smooth((z-.76)/.06);q.x=cx+(x-cx)*(1+head*(.09*g(z,.83,.065)-.04*g(z,.96,.05)))
        q.y-=.009*g(x,cx,.026)*g(z,.864,.028)*front
        for side in (-1,1):q.x-=side*.004*g(x,cx+side*.059,.035)*g(z,.906,.032)*front
        hair=smooth((z-1.04)/.07);q.x+=.029*hair;q.y-=.008*hair;q.z-=.17*hair*max(0,z-1.06)
        q.z=.78+(q.z-.78)*.91 if z>.78 else q.z
    return q

def legacy_palette(obj,s,out,kind=None):
    original,px=image_colors(obj);R,G,B=[px[:,:,i].copy() for i in range(3)];lum=px[:,:,:3].mean(2)
    # Clothing colors are isolated by both existing painted hue and skeletal UV
    # regions. Head skin, eye whites and lips remain untouched.
    H,W=px.shape[:2];shirt=np.zeros((H,W),bool);pants=shirt.copy();hairmask=shirt.copy();source=kind or s['source'];uv=obj.data.uv_layers.active;names={g.index:g.name for g in obj.vertex_groups}
    def raster(target,t):
        a,b,c=t[:3];x0=max(0,int(np.floor(t[:,0].min())));x1=min(W-1,int(np.ceil(t[:,0].max())));y0=max(0,int(np.floor(t[:,1].min())));y1=min(H-1,int(np.ceil(t[:,1].max())));den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-8:return
        xx,yy=np.meshgrid(np.arange(x0,x1+1),np.arange(y0,y1+1));u=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den;v=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;target[y0:y1+1,x0:x1+1]|=(u>=-.025)&(v>=-.025)&(u+v<=1.025)
    for face in obj.data.polygons:
        points=[obj.data.vertices[i].co for i in face.vertices];p=sum(points,Vector())/len(points);bone={}
        for vi in face.vertices:
            for q in obj.data.vertices[vi].groups:bone[names[q.group]]=bone.get(names[q.group],0)+q.weight/len(points)
        hand=bone.get('LeftHand',0)+bone.get('RightHand',0);head=bone.get('Head',0);leg=sum(v for k,v in bone.items() if 'Leg' in k or k=='Hips');foot=sum(v for k,v in bone.items() if 'Foot' in k or 'Toes' in k)
        t=np.array([(uv.data[i].uv.x*(W-1),uv.data[i].uv.y*(H-1)) for i in face.loop_indices]);xy=t.mean(0);r,gg,b=px[int(xy[1]),int(xy[0]),:3]
        if hand<.06 and head<.28 and foot<.18:
            if source=='Emre':is_shirt=b>r*1.04 and b>gg*.96;is_pants=leg>.5 and gg>b*1.03
            elif source=='Yusuf':is_shirt=gg>r*1.02 and gg>b*.97;is_pants=leg>.5 and gg>b*1.01
            else:is_shirt=r>gg*1.09 and b>gg*.88;is_pants=leg>.5 and b<gg*1.04
            if is_shirt:raster(shirt,t)
            if is_pants:raster(pants,t)
        if s['name']=='Gul' and p.z>1.11 and (p.z>1.410 or abs(p.x-.018)>.141 or p.y>.018) and r>gg*1.08 and (r+gg+b)<1.75:raster(hairmask,t)
    def paint(region,hex,reference=None):
        if not np.any(region):return
        target=np.array([int(hex[i:i+2],16)/255 for i in (1,3,5)]);ref=reference or float(np.median(lum[region]));shade=np.clip((lum[region]/ref)**.80,.28,1.65);px[region,:3]=np.clip(shade[:,None]*target,0,1)
    paint(shirt,s['shirt']);paint(pants,s['pants'])
    if s['name']=='Gul':
        paint(hairmask,'#A3A29A',.215)
    im=original.copy()
    if im.packed_file:im.unpack(method='REMOVE')
    im.pixels=px.flatten();im.filepath_raw=str(out/(s['name']+('_Body' if kind else '')+'_Albedo.png'));im.file_format='PNG';im.save();im.pack()
    m=obj.data.materials[0].copy();obj.data.materials[0]=m
    for node in m.node_tree.nodes:
        if node.type=='TEX_IMAGE' and node.image==original:node.image=im

def palette(obj,s,out,kind=None):
    from resident_materials import approved_palette
    source=kind or s['source']
    if source=='Yusuf':return
    approved_palette(obj,s['name'],source,s['shirt'],out,grey=s['name']=='Gul')

def bind(obj,rig,bone='Hips'):
    obj.parent=rig;obj.vertex_groups.clear();group=obj.vertex_groups.new(name=bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE');mod=obj.modifiers.new('Character skin','ARMATURE');mod.object=rig

def bag(body,rig):
    tree=BVHTree.FromPolygons([v.co for v in body.data.vertices],[tuple(p.vertices) for p in body.data.polygons]);canvas=mat('Deniz sand canvas cloth','#BA9B72');edge=mat('Deniz bag stitched binding','#735D47')
    def fit(x,z,offset=.015):
        hit=tree.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))[0];return Vector((x,(hit.y if hit else -.15)-offset,z))
    vs=[];fs=[];R=84
    for j in range(R):
        t=j/(R-1);x=-.12+.32*t;z=1.195-.540*t
        for i in range(5):vs.append(fit(x+(i/4-.5)*.041,z))
    for j in range(R-1):
        for i in range(4):fs.append(((j+1)*5+i,(j+1)*5+i+1,j*5+i+1,j*5+i))
    ob=mesh_object('Deniz diagonal canvas strap',vs,fs,canvas,rig)
    bpy.ops.mesh.primitive_cube_add(size=1,location=(.205,-.10,.665));o=bpy.context.object;o.name='Deniz messenger bag';o.dimensions=(.210,.110,.195);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(canvas);bevel=o.modifiers.new('Soft sewn corners','BEVEL');bevel.width=.025;bevel.segments=4;bpy.ops.object.modifier_apply(modifier=bevel.name)
    for p in o.data.polygons:p.use_smooth=True
    bpy.ops.object.transform_apply(location=True,rotation=False,scale=False);bind(o,rig)
    flap=tube('Deniz satchel flap stitching',[Vector((.11+t*.19,-.159,.685-.026*math.sin(t*math.pi))) for t in np.linspace(0,1,44)],.0013,edge,None);bind(flap,rig)

def skirt(rig):
    cloth=mat('Gul woven long skirt cloth','#3C525D');hem=mat('Gul skirt hem','#A0B2AE');N=96;R=40;vs=[];fs=[]
    for j in range(R):
        t=j/(R-1);z=.74-.600*t
        for i in range(N):
            a=math.tau*i/N;r=1+.024*math.sin(a*14)*t;vs.append(Vector((.018+(.235+.080*t)*math.sin(a)*r,.025+(.223+.063*t)*math.cos(a)*r,z+.005*t*math.cos(a*14))))
    for j in range(R-1):
        for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    o=mesh_object('Gul softly pleated midi skirt',vs,fs,cloth);bind(o,rig)
    path=[vs[(R-2)*N+i] for i in range(N)]+[vs[(R-2)*N]];o=tube('Gul stitched skirt hem',path,.0014,hem,None);bind(o,rig)

def beret(body,rig):
    top=max(v.co.z for v in body.data.vertices);cz=top+.009
    wool=mat('Kemal plum wool cloth','#625461');band=mat('Kemal beret band','#423F49');N=72;R=32;vs=[];fs=[]
    for j in range(R+1):
        p=math.pi*j/R
        for i in range(N):
            a=math.tau*i/N;r=1+.013*math.cos(a*10)*math.sin(p);vs.append(Vector((.001+.181*math.sin(p)*math.cos(a)*r,-.025+.174*math.sin(p)*math.sin(a)*r,cz+.066*math.cos(p)+.032*math.sin(p)*math.cos(a))))
    for j in range(R):
        for i in range(N):fs.append(((j+1)*N+i,(j+1)*N+(i+1)%N,j*N+(i+1)%N,j*N+i))
    o=mesh_object('Kemal soft beret crown',vs,fs,wool);bind(o,rig,'Head')
    o=tube('Kemal fitted hat band',[Vector((.001+.147*math.cos(a),-.025+.139*math.sin(a),cz-.038)) for a in np.linspace(0,math.tau,96)],.010,band,None);bind(o,rig,'Head')

def bake_approved_material(obj):
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    im=bpy.data.images.new('Kemal approved material bake',2048,2048,alpha=True)
    for material in obj.data.materials:
        node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;material.node_tree.nodes.active=node
    bpy.ops.object.bake(type='DIFFUSE',pass_filter={'COLOR'},use_clear=True,margin=8)
    im.filepath_raw=str(OUT/'Kemal/ApprovedMaterial.png');im.file_format='PNG';im.save();im.pack();m=bpy.data.materials.new('Kemal approved baked material');m.use_nodes=True;node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;m.node_tree.links.new(node.outputs['Color'],m.node_tree.nodes['Principled BSDF'].inputs['Base Color']);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.72
    obj.data.materials.clear();obj.data.materials.append(m)
    for face in obj.data.polygons:face.material_index=0

def knit_vest(body,rig):
    tree=BVHTree.FromPolygons([v.co for v in body.data.vertices],[tuple(p.vertices) for p in body.data.polygons]);cloth=mat('Kemal burgundy knitted cloth','#826164');rib=mat('Kemal vest ribbing','#635058');N=64;R=46
    def fit(a,z):
        center=Vector((.018,-.014,z));d=Vector((math.sin(a),-math.cos(a),0));hit=tree.ray_cast(center,d,.5)[0]
        if hit is None:hit=center+d*.17
        if abs(math.sin(a))>.74:hit=center+d*min((hit-center).length,.204/abs(math.sin(a)))
        return hit+d*.009
    vs=[];fs=[]
    for j in range(R):
        t=j/(R-1)
        for i in range(N):
            a=math.tau*i/N;front=min(a,math.tau-a);top=1.081-.135*g(front,0,.50)-.180*g(front,math.pi/2,.70);vs.append(fit(a,.587+(top-.587)*t))
    for j in range(R-1):
        for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    mesh_object('Kemal V neck knitted vest',vs,fs,cloth,rig)
    tube('Kemal ribbed neckline',[vs[(R-1)*N+i] for i in range(N)]+[vs[(R-1)*N]],.005,rib,rig)
    tube('Kemal ribbed hem',[vs[i] for i in range(N)]+[vs[0]],.0045,rib,rig)

def child_body(head,oldrig,s,out):
    old=head.data;image,pixels=image_colors(head);keep=[]
    for p in old.polygons:
        if min(old.vertices[i].co.z for i in p.vertices)<=.752:continue
        uv=sum((old.uv_layers.active.data[l].uv for l in p.loop_indices),Vector((0,0)))/len(p.loop_indices);c=pixels[int(uv.y*(image.size[1]-1)),int(uv.x*(image.size[0]-1)),:3]
        if max(old.vertices[i].co.z for i in p.vertices)<.878 and c[1]>c[0]*1.20 and c[1]>c[2]*.96:continue
        keep.append(p)
    new=bpy.data.meshes.new('Arda resculpted face');new.from_pydata([v.co for v in old.vertices],[],[tuple(p.vertices) for p in keep]);new.update()
    for m in old.materials:new.materials.append(m)
    uv=new.uv_layers.new(name='OriginalUV')
    for p,src in zip(new.polygons,keep):
        p.material_index=src.material_index;p.use_smooth=True
        for li,si in zip(p.loop_indices,src.loop_indices):uv.data[li].uv=old.uv_layers.active.data[si].uv
    head.data=new
    with bpy.data.libraries.load(str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle/Emre.blend'),link=False) as (src,dst):dst.objects=src.objects
    for o in dst.objects:
        if o:bpy.context.collection.objects.link(o)
    body=next(o for o in dst.objects if o.type=='MESH');rig=next(o for o in dst.objects if o.type=='ARMATURE');palette(body,s,out,'Emre')
    body.shape_key_clear();bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z>1.205],context='VERTS');bm.to_mesh(body.data);bm.free()
    for v in body.data.vertices:v.co*=.66
    bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    for b in rig.data.edit_bones:b.head*=.66;b.tail*=.66
    bpy.ops.object.mode_set(mode='OBJECT');body.name='Arda_ChildClothing';head.modifiers.clear();bind(head,rig,'Head');bpy.data.objects.remove(oldrig,do_unlink=True);rig.name='Arda_Rig'
    return rig

def main():
    p=argparse.ArgumentParser();p.add_argument('--names',nargs='*');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []);bpy.context.preferences.filepaths.save_version=0
    for s in SPECS:
        if args.names and s['name'] not in args.names:continue
        name=s['name'];out=OUT/name;out.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle'/(s['source']+'.blend')))
        body=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
        if name=='Kemal':bake_approved_material(body)
        if name!='Arda':palette(body,s,out)
        for accessory in list(bpy.context.scene.objects):
            if accessory.type=='MESH' and accessory!=body:
                for v in accessory.data.vertices:v.co=warp(v.co,name)
        if body.data.shape_keys:
            for key in body.data.shape_keys.key_blocks:
                for v in key.data:v.co=warp(v.co,name)
                key.value=0
            for v,k in zip(body.data.vertices,body.data.shape_keys.key_blocks[0].data):v.co=k.co
        else:
            for v in body.data.vertices:v.co=warp(v.co,name)
        body.name=name+'_SculptedBody';rig.name=name+'_Rig';bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
        for b in rig.data.edit_bones:b.head=warp(b.head,name);b.tail=warp(b.tail,name)
        bpy.ops.object.mode_set(mode='OBJECT');bpy.context.view_layer.update()
        if name=='Deniz':bag(body,rig)
        elif name=='Gul':
            # Pants under a long skirt are hidden geometry, not a second garment
            # allowed to protrude through the skirt when the hips move.
            old=body.data;groups={g.index:g.name for g in body.vertex_groups};keep=[]
            for p in old.polygons:
                zlo=min(old.vertices[i].co.z for i in p.vertices);zhi=max(old.vertices[i].co.z for i in p.vertices);legs=sum(q.weight for i in p.vertices for q in old.vertices[i].groups if 'Leg' in groups[q.group] or groups[q.group]=='Hips')/len(p.vertices)
                if zlo>.136 and zhi<.700 and legs>.50:continue
                keep.append(p)
            new=bpy.data.meshes.new('Gul skirt body');new.from_pydata([v.co for v in old.vertices],[],[tuple(p.vertices) for p in keep]);new.update();weights=[[(q.group,q.weight) for q in v.groups] for v in old.vertices];groupnames=[g.name for g in body.vertex_groups]
            for m in old.materials:new.materials.append(m)
            uv=new.uv_layers.new(name='OriginalUV')
            for p,src in zip(new.polygons,keep):
                p.use_smooth=True;p.material_index=src.material_index
                for li,si in zip(p.loop_indices,src.loop_indices):uv.data[li].uv=old.uv_layers.active.data[si].uv
            body.data=new;body.vertex_groups.clear();gs=[body.vertex_groups.new(name=n) for n in groupnames]
            for i,ws in enumerate(weights):
                for group,w in ws:gs[group].add([i],w,'REPLACE')
            skirt(rig)
        elif name=='Kemal':beret(body,rig);knit_vest(body,rig)
        else:rig=child_body(body,rig,s,out)
        body['authorship']='Blender anatomical resculpt based on approved '+s['source']+' topology; modified face/body proportions and newly modeled wardrobe.'
        bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'.blend')));render(body,s,out)
        (out/'review-status.json').write_text(json.dumps(dict(status='needs_visual_review',production_ready=False,integrated_into_scene=False,source_anatomy=s['source'],expression_rig_complete=False),indent=2));print('CROWD_SCULPT',name,flush=True)

if __name__=='__main__':main()
