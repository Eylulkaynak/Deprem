"""Blender sculpt authoring for Eren. Source templates are never overwritten.

Preserves the approved anatomical/material detail; sculpts a different face and
stocky silhouette, then builds a tailored garment with actual pockets and seams.
Everything stays in ArtDirection until geometry and visual review pass.
"""
import bpy,math,pathlib,sys,json,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/YanYana'))
from build_original_residents import color
OUT=ROOT/'ArtDirection/YanYana/Characters/ResidentWorkshop/Eren'

def clamp(x):return max(0,min(1,x))
def smooth(x):x=clamp(x);return x*x*(3-2*x)
def gauss(x,c,r):return math.exp(-((x-c)/r)**2)

def sculpt(p):
    x,y,z=p;cx=.018;front=smooth((-y-.03)/.17);head=smooth((z-1.17)/.08);q=Vector(p)
    # Distinct broad lower face, full cheeks, compact chin, and a shorter nose.
    q.x=cx+(x-cx)*(1+head*(.40*gauss(z,1.285,.091)+.14*gauss(z,1.405,.091)))
    q.y-=head*front*.016*gauss(z,1.32,.081)
    q.z=z+head*((1.40+(z-1.40)*.91)-z)
    nose=gauss(x,cx,.044)*gauss(z,1.351,.045)*front
    q.x+=nose*(x-cx)*.28;q.y+=nose*.021;q.z+=nose*.002
    for side in (-1,1):
        ex=cx+side*.075;eye=gauss(x,ex,.041)*gauss(z,1.407,.036)*front
        q.x+=eye*side*.005;q.z-=eye*(z-1.407)*.09
    mouth=gauss(x,cx,.075)*gauss(z,1.288,.028)*front
    q.x+=mouth*(x-cx)*.18;q.z+=mouth*.004
    # Collapse the high quiff into a compact combed cut, keeping its sculpted ridges.
    short=smooth((z-1.53)/.09)
    q.z-=short*(z-1.53)*.29;q.y+=short*max(0,-y-.18)*.24
    # The torso is built differently, not merely uniformly scaled.
    body=1-head;belly=gauss(z,.81,.23);chest=gauss(z,1.07,.16)
    q.x=cx+(q.x-cx)*(1+body*(.29*belly+.11*chest)*gauss(x,cx,.32))
    q.y+=body*(y-.01)*(.34*belly+.10*chest)*gauss(x,cx,.26)
    q.y-=body*.023*belly*front*gauss(x,cx,.22)
    # Slightly lower center of gravity; preserve hands and feet detail.
    q.z-=.018*smooth(z/.65)
    return q

def mat(name,hex,rough=.74):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value=color(hex);p.inputs['Roughness'].default_value=rough;p.inputs['Specular IOR Level'].default_value=.25;m.diffuse_color=color(hex)
    if 'cloth' in name:
        n=m.node_tree.nodes.new('ShaderNodeTexNoise');n.inputs['Scale'].default_value=280;n.inputs['Detail'].default_value=2
        bump=m.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.10;bump.inputs['Distance'].default_value=.00025;m.node_tree.links.new(n.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],p.inputs['Normal'])
    return m

def mesh_object(name,vs,fs,material,rig=None):
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);me.materials.append(material)
    for p in me.polygons:p.use_smooth=True
    if rig:
        groups={n:ob.vertex_groups.new(name=n) for n in ('Hips','Spine','Chest')}
        for v in me.vertices:
            z=v.co.z;ch=smooth((z-.82)/.18);hip=1-smooth((z-.61)/.13);sp=max(0,1-ch-hip)
            for n,w in [('Hips',hip),('Spine',sp),('Chest',ch)]:
                if w>0:groups[n].add([v.index],w,'REPLACE')
        mod=ob.modifiers.new('Tailored garment skin','ARMATURE');mod.object=rig;ob.parent=rig
    return ob

def tube(name,path,radius,material,rig):
    vs=[];fs=[];N=8
    for j,c in enumerate(path):
        c=Vector(c);d=(Vector(path[min(j+1,len(path)-1)])-Vector(path[max(0,j-1)])).normalized();u=d.cross(Vector((0,0,1)))
        if u.length<.05:u=d.cross(Vector((0,-1,0)))
        u.normalize();w=d.cross(u).normalized()
        for i in range(N):a=math.tau*i/N;vs.append(c+radius*(u*math.cos(a)+w*math.sin(a)))
    for j in range(len(path)-1):
        for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    return mesh_object(name,vs,fs,material,rig)

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/YanYana/Characters/ApprovedStyle/Emre.blend'))
    bpy.context.preferences.filepaths.save_version=0
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    body.name='Eren_SculptedBody';rig.name='Eren_Rig';base=[v.co.copy() for v in body.data.vertices]
    if body.data.shape_keys:
        for key in body.data.shape_keys.key_blocks:
            for v in key.data:v.co=sculpt(v.co)
            key.value=0
    else:
        for v in body.data.vertices:v.co=sculpt(v.co)
    # Basis values are also copied to the mesh before fitting garment surfaces.
    if body.data.shape_keys:
        for v,k in zip(body.data.vertices,body.data.shape_keys.key_blocks[0].data):v.co=k.co
    bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    for bone in rig.data.edit_bones:bone.head=sculpt(bone.head);bone.tail=sculpt(bone.tail)
    bpy.ops.object.mode_set(mode='OBJECT');bpy.context.view_layer.update()
    group_names={g.index:g.name for g in body.vertex_groups}
    torso_weights=[sum(g.weight for g in v.groups if group_names[g.group] in ('Hips','Spine','Chest','Neck')) for v in body.data.vertices]
    torso_faces=[tuple(p.vertices) for p in body.data.polygons]
    bvh=BVHTree.FromPolygons([v.co.copy() for v in body.data.vertices],torso_faces);cx=.018
    def fit(a,z,offset=.008):
        center=Vector((cx,.025,z));direction=Vector((math.sin(a),-math.cos(a),0));loc,n,idx,dist=bvh.ray_cast(center,direction,.6)
        if loc is None:loc=center+direction*.195
        if abs(math.sin(a))>.72:
            radius=min((loc-center).length,.240/abs(math.sin(a)));loc=center+direction*radius
        return loc+direction*offset
    orange=mat('Eren ochre safety cloth','#D27735');orange_edge=mat('Eren reinforced orange cloth','#B85C29');dark=mat('Eren navy binding','#283C4B');tape=mat('Eren woven silver tape','#B9B9A8');seam=mat('Eren ochre stitching','#E3B378');badge=mat('Eren cream ID patch','#EBE5CF')
    for side in (-1,1):
        N=64;R=56;vs=[];fs=[]
        for j in range(R):
            t=j/(R-1)
            for i in range(N):
                a=side*(.14+(math.pi-.14)*i/(N-1));aa=abs(a)
                # An actual lowered armhole instead of a cylinder across the sleeve.
                top=1.174-.148*gauss(aa,math.pi/2,.49)-.012*gauss(aa,0,.4)
                z=.628+(top-.628)*t;p=fit(a,z)
                # Subtle hem weight and crease along the opening.
                p+=Vector((math.sin(a),-math.cos(a),0))*(.002*math.sin(t*math.pi)+.0025*gauss(t,.08,.07));vs.append(p)
        for j in range(R-1):
            for i in range(N-1):
                face=(j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i);fs.append(face if side==1 else tuple(reversed(face)))
        ob=mesh_object('Eren tailored vest panel',vs,fs,orange,rig)
        solid=ob.modifiers.new('Two millimeter cloth edge','SOLIDIFY');solid.thickness=.002;solid.offset=0
        # Deliberate piping around the whole opening and the armhole.
        edges=[[vs[j*N] for j in range(R)],[vs[(R-1)*N+i] for i in range(N)],[vs[i] for i in range(N)]]
        for path in edges:tube('Eren bound vest edge',path,.0021,dark,rig)
        for zcenter in (.716,.914):
            tv=[];tf=[];RR=8
            for j in range(RR):
                z=zcenter+(j/(RR-1)-.5)*.029
                for i in range(N):a=side*(.15+(math.pi-.15)*i/(N-1));tv.append(fit(a,z,.013))
            for j in range(RR-1):
                for i in range(N-1):
                    face=(j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i);tf.append(face if side==1 else tuple(reversed(face)))
            mesh_object('Eren reflective tape',tv,tf,tape,rig)
        # A fitted pocket with a separate flap and topstitching.
        def front(x,z,offset=.018):
            loc,n,idx,dist=bvh.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
            return Vector((x,(loc.y if loc else -.16)-offset,z))
        center=cx+side*.135
        for label,zlo,zhi,w,off in [('Patch pocket',.745,.853,.098,.020),('Pocket flap',.829,.863,.104,.024)]:
            vv=[];ff=[];NX=20;NZ=18
            for j in range(NZ):
                z=zlo+(zhi-zlo)*j/(NZ-1)
                for i in range(NX):
                    u=-1+2*i/(NX-1);x=center+u*w/2;zz=z+.005*(abs(u)**6)*gauss(j/(NZ-1),0,.14);p=front(x,zz,off);p.y-=.0018*math.sin(i/(NX-1)*math.pi)*math.sin(j/(NZ-1)*math.pi);vv.append(p)
            for j in range(NZ-1):
                for i in range(NX-1):ff.append((j*NX+i,j*NX+i+1,(j+1)*NX+i+1,(j+1)*NX+i))
            mesh_object('Eren '+label,vv,ff,orange_edge if 'flap' in label else orange,rig)
            stitchpath=[front(center-w*.43+q*w*.86,zlo+.006,off+.002) for q in np.linspace(0,1,35)]
            tube('Eren pocket topstitch',stitchpath,.00065,seam,rig)
        if side==-1:
            # A small plain identification patch with readable modeled lettering.
            z=.995;vv=[front(center+dx,z+dz,.023) for dx,dz in [(-.039,-.022),(.039,-.022),(.039,.022),(-.039,.022)]]
            mesh_object('Eren identification patch',vv,[(0,1,2,3)],badge,rig)
            cu=bpy.data.curves.new('Eren badge lettering','FONT');cu.body='EREN';cu.align_x='CENTER';cu.align_y='CENTER';cu.size=.019;cu.extrude=.0001;cu.materials.append(dark)
            textob=bpy.data.objects.new('Eren badge lettering',cu);bpy.context.collection.objects.link(textob);textob.location=front(center,z,.025);textob.rotation_euler=(math.pi/2,0,0);textob.parent=rig
    # Metadata distinguishes a resculpt from entirely original topology.
    body['authorship']='Blender resculpt of approved Emre anatomical topology: jaw, cheeks, nose, eyes, haircut, torso and limb proportions changed.'
    rig['status']='Single character in visual review. No Unity scene import.'
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Eren.blend'))
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=48
    if not sc.world:sc.world=bpy.data.worlds.new('Warm studio')
    sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.62,.68,.77,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.30
    floor=mat('Warm studio floor','#DADCCF');bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.007));bpy.context.object.data.materials.append(floor)
    for name,pos,power,size in [('Key',(-3,-4,5),600,3),('Fill',(3,-2,3),150,3),('Rim',(1,3,4),300,2.5)]:
        ld=bpy.data.lights.new(name,'AREA');ld.energy=power;ld.shape='DISK';ld.size=size;lo=bpy.data.objects.new(name,ld);sc.collection.objects.link(lo);lo.location=pos;lo.rotation_euler=(Vector((0,0,1))-lo.location).to_track_quat('-Z','Y').to_euler()
    ca=bpy.data.cameras.new('Character review camera');cam=bpy.data.objects.new('Character review camera',ca);sc.collection.objects.link(cam);sc.camera=cam;ca.type='ORTHO';sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.view_settings.exposure=-.15
    sc.render.image_settings.file_format='PNG';sc.render.resolution_x=850;sc.render.resolution_y=1050;sc.render.resolution_percentage=100
    ca.ortho_scale=2.02;cam.location=(.16,-4,1.17);cam.rotation_euler=(Vector((.018,0,.84))-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/'FullBody.png');bpy.ops.render.render(write_still=True)
    ca.ortho_scale=.65;cam.location=(.06,-4,1.44);cam.rotation_euler=(Vector((.018,0,1.43))-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x=850;sc.render.resolution_y=850;sc.render.filepath=str(OUT/'Face.png');bpy.ops.render.render(write_still=True)
    (OUT/'review-status.json').write_text(json.dumps({'status':'needs_visual_review','integrated_into_scene':False,'production_ready':False,'source_anatomy':'ApprovedStyle/Emre.blend','face_and_body_geometry_changed':True,'new_clothing':'Modeled safety vest, tape, pockets, flaps, seams and ID patch','expression_rig_complete':False},indent=2))
    print('EREN_SCULPT_READY',OUT,flush=True)

if __name__=='__main__':main()
