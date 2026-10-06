import bpy,pathlib,json,sys,numpy as np
root=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'Tools/YanYana'))
from author_surface_expressions import SurfaceExpressions
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtDirection/YanYana/Characters/ResidentWorkshop/Eren/Eren.blend'))
source=json.loads((root/'ArtDirection/YanYana/Characters/ApprovedStyle/Emre.mesh.json').read_text(encoding='utf-8-sig'))['surfaces'][0]
a=SurfaceExpressions(bpy.data.objects['Eren_SculptedBody'],source,json.loads((root/'.codex_tmp/eren-mouth-trace.json').read_text()),'Eren')
for ex,ez,c in a.lidfits:
    print('FIT',ex,ez,c.tolist())
    for dx,dz in ((0,0),(0,.043),(0,-.039),(-.043,0),(.043,0)):
        nearest=sorted(zip(a.src,a.v),key=lambda p:(p[0].x-ex-dx)**2+(p[0].z-ez-dz)**2+(max(0,p[0].y+.1))**2)[:4]
        print('SAMPLE',dx,dz,'source Y',[round(p[0].y,4) for p in nearest],'surface Y',[round(p[1].y,4) for p in nearest],'target',float(np.dot(c,[1,dx,dz,dx*dx,dx*dz,dz*dz])))
