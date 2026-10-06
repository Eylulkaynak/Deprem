"""One character only. Side-by-side with approved art, outside Unity Assets."""
import bpy,math,pathlib,sys,json
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'Tools/YanYana'))
from build_original_residents import Sculpture,CAST,stage,color

class EceStudy(Sculpture):
    def mouth_settings(self):
        values=super().mouth_settings();values['Basis']=(.83,.0006,.006)
        return values
    def tube(self,name,path,radius,mat,*args,**kwargs):
        if name in ('Upper lip','Lower lip'):
            radius=[v*.32 for v in radius] if isinstance(radius,list) else radius*.32
        return super().tube(name,path,radius,mat,*args,**kwargs)

bpy.ops.wm.read_factory_settings(use_empty=True)
spec=dict(CAST[0]);spec.update(eye_factor=(.64,.68),sculpted_nose=True,style_study=True,skin='#E6AF8C',freckles=False)
s=EceStudy(spec);s.hh=spec['h']*.30;s.hz=spec['h']*.777;s.w=spec['h']*.144;s.d=spec['h']*.116;s.eye_z=s.hz+s.hh*.055;s.eye_x=s.w*.37;s.mouth_z=s.hz-s.hh*.21;s.mw=s.w*.35
s.materials[s.matindex['Lip']].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=color('#BA8068')
s.materials[s.matindex['Iris']].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=color('#6C442C')
s.body();s.face();obj,rig=s.finish()
# Subtle vertex-painted cheek warmth. The facial topology stays continuous.
attribute=obj.data.color_attributes.new(name='SkinWarmth',type='FLOAT_COLOR',domain='POINT')
base=color(spec['skin']);warm=color('#DB9276')
for v in obj.data.vertices:
    p=v.co;front=max(0,min(1,-p.y/s.d));mask=sum(math.exp(-((p.x-side*s.w*.51)/(s.w*.21))**2-((p.z-(s.eye_z-s.hh*.17))/(s.hh*.09))**2) for side in (-1,1))*front*.24
    attribute.data[v.index].color=tuple(base[k]*(1-mask)+warm[k]*mask for k in range(3))+(1,)
mat=s.materials[s.matindex['Skin']];node=mat.node_tree.nodes.new('ShaderNodeVertexColor');node.layer_name='SkinWarmth';mat.node_tree.links.new(node.outputs['Color'],mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
out=root/'ArtDirection/YanYana/Characters/StyleStudy';out.mkdir(parents=True,exist_ok=True)
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(out/'Ece_ProportionStudy.blend'))
camera=stage(obj,rig);sc=bpy.context.scene;camera.data.ortho_scale=.60;camera.location=(0,-4,s.hz+.016);camera.rotation_euler=(Vector((0,0,s.hz+.01))-camera.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x=720;sc.render.resolution_y=720;sc.render.resolution_percentage=100
sc.render.filepath=str(out/'Ece_FaceStudy.png');bpy.ops.render.render(write_still=True)
camera.data.ortho_scale=1.43;camera.location=(0,-4,.73);camera.rotation_euler=(Vector((0,0,.56))-camera.location).to_track_quat('-Z','Y').to_euler();sc.render.resolution_x=720;sc.render.resolution_y=900;sc.render.filepath=str(out/'Ece_FullStudy.png');bpy.ops.render.render(write_still=True)
print('ONE_STYLE_STUDY_RENDERED',out)
