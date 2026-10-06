import bpy,pathlib,json
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2]
for name,z in [('Ece',1.06),('Eren',1.4)]:
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ResidentWorkshop/ReviewPack'/name/(name+'.blend')))
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        mesh=obj.data;rows={}
        for poly in mesh.polygons:
            v=poly.center
            key=mesh.materials[poly.material_index].name
            if key not in rows:rows[key]=dict(polys=0,inward_front=0,outward_front=0,signedvolume=0)
            r=rows[key];r['polys']+=1
            if v.y<-.16 and abs(v.z-z)<.08 and abs(v.x)<.12:
                if poly.normal.y>0:r['inward_front']+=1
                else:r['outward_front']+=1
            vs=[mesh.vertices[i].co for i in poly.vertices]
            for i in range(1,len(vs)-1):r['signedvolume']+=vs[0].dot(vs[i].cross(vs[i+1]))/6
        print('WINDING_AUDIT',name,obj.name,json.dumps(rows),flush=True)
