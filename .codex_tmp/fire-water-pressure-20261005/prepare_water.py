from pathlib import Path
import shutil
import sys

root = Path(__file__).resolve().parents[2]
stage = Path(__file__).resolve().parent

def change(relative, old, new):
    source = (root / relative).read_text(encoding='utf-8-sig')
    assert old in source, relative
    result = source.replace(old, new, 1)
    target = stage / Path(relative).name
    target.write_text(result, encoding='utf-8', newline='\n')
    return relative, target

files = []
files.append(change('Assets/YanYana/Editor/YanYanaFirePresentation.cs',
'            var droplets = Material("FirefighterDroplets", "Deprem/Adventure Particles", new Color(.76f,.91f,1f,.9f), 2);',
'''            var droplets = Material("FirefighterDroplets", "Deprem/Adventure Particles", new Color(.87f,.96f,1f,.96f), 2);
            var pressureWater = Material("FirefighterPressureWater", "Deprem/Firefighter Water", Color.white);
            var sprayMist = Material("FirefighterSprayMist", "Deprem/Adventure Particles", new Color(.87f,.96f,1f,.45f), 1);'''))
presentation = files[-1][1]
source = presentation.read_text(encoding='utf-8')
source = source.replace('ConfigureWater((ParticleSystem)data.FindProperty("waterImpact").objectReferenceValue, droplets);', 'ConfigureWater((ParticleSystem)data.FindProperty("waterImpact").objectReferenceValue, droplets, sprayMist);')
old = '''                stream.startWidth = .030f; stream.endWidth = .055f;
                stream.numCapVertices = 8; stream.numCornerVertices = 8;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(new Color(.86f,.97f,1),0), new GradientColorKey(new Color(.65f,.85f,.93f),1) },
                    new[] { new GradientAlphaKey(.94f,0), new GradientAlphaKey(.70f,1) });
                stream.colorGradient = gradient;'''
new = '''                stream.sharedMaterial=pressureWater; stream.textureMode=LineTextureMode.Stretch;
                stream.alignment=LineAlignment.View; stream.numCapVertices=0; stream.numCornerVertices=8;
                stream.widthMultiplier=1;
                stream.widthCurve=new AnimationCurve(new Keyframe(0,.055f),new Keyframe(.12f,.082f),new Keyframe(.45f,.15f),new Keyframe(1,.25f));
                stream.startColor=stream.endColor=Color.white;'''
assert old in source
source = source.replace(old,new)
start = source.index('        static void ConfigureWater(')
end = source.index('        static void BindSuppression(',start)
source = source[:start] + '''        static void ConfigureWater(ParticleSystem ps, Material material, Material mistMaterial)
        {
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            Remove(ps.transform,"Basınçlı su damlaları"); Remove(ps.transform,"Çarpışmada dağılan su sisi");
            var main=ps.main; main.loop=true; main.playOnAwake=false; main.prewarm=false;
            main.simulationSpace=ParticleSystemSimulationSpace.World; main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.24f,.43f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(1.35f,2.8f); main.startSize=new ParticleSystem.MinMaxCurve(.025f,.055f);
            main.gravityModifier=1.8f; main.maxParticles=110; main.startColor=Color.white;
            var emission=ps.emission; emission.rateOverTime=210;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Hemisphere; shape.radius=.065f; shape.rotation=new Vector3(-90,0,0);
            var color=ps.colorOverLifetime; color.enabled=true; color.color=Fade(Color.white,new Color(.73f,.9f,1),.95f);
            var size=ps.sizeOverLifetime; size.enabled=true; size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.25f));
            var renderer=ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=material;
            renderer.renderMode=ParticleSystemRenderMode.Stretch; renderer.velocityScale=.025f; renderer.lengthScale=1.5f;

            // The existing manager starts and stops the impact system with all native children.
            // The embedded hose graph positions this emitter at the nozzle and sets travel time.
            var jet=Particle("Basınçlı su damlaları",ps.transform,material,433);
            main=jet.main; main.playOnAwake=false; main.prewarm=false; main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.startLifetime=1; main.startSpeed=11; main.startSize=new ParticleSystem.MinMaxCurve(.016f,.03f);
            main.gravityModifier=0; main.maxParticles=100;
            emission=jet.emission; emission.rateOverTime=320;
            shape=jet.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.radius=.018f; shape.angle=4.2f; shape.length=0;
            color=jet.colorOverLifetime; color.enabled=true; color.color=Fade(Color.white,new Color(.8f,.94f,1),.85f);
            size=jet.sizeOverLifetime; size.enabled=true; size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.75f,1,1.2f));
            renderer=jet.GetComponent<ParticleSystemRenderer>(); renderer.renderMode=ParticleSystemRenderMode.Stretch;
            renderer.velocityScale=.012f; renderer.lengthScale=1.8f;

            var mist=Particle("Çarpışmada dağılan su sisi",ps.transform,mistMaterial,461);
            main=mist.main; main.playOnAwake=false; main.prewarm=false; main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,.48f); main.startSpeed=new ParticleSystem.MinMaxCurve(.35f,.85f);
            main.startSize=new ParticleSystem.MinMaxCurve(.09f,.19f); main.maxParticles=36;
            emission=mist.emission; emission.rateOverTime=65;
            shape=mist.shape; shape.shapeType=ParticleSystemShapeType.Hemisphere; shape.radius=.09f; shape.rotation=new Vector3(-90,0,0);
            color=mist.colorOverLifetime; color.enabled=true; color.color=Fade(Color.white,new Color(.77f,.91f,1),.42f);
            size=mist.sizeOverLifetime; size.enabled=true; size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.55f,1,1.8f));
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
''' + source[end:]
presentation.write_text(source,encoding='utf-8',newline='\n')

files.append(change('Assets/YanYana/Editor/YanYanaFirePointerAuthor.cs',
'                float t=i/17f;var point=Add(g,g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},start,aim,t).result,Vector3.up*(.045f*4*t*(1-t)));',
'                float t=i/17f;var point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},start,aim,t).result;'))
pointer = files[-1][1]
source = pointer.read_text(encoding='utf-8')
old = '            return g.Set(p,typeof(Transform),"position",impact.transform,aim);'
new = '''            p=g.Set(p,typeof(Transform),"position",impact.transform,aim);
            var jet=impact.transform.Find("Basınçlı su damlaları");
            if(jet)
            {
                p=g.Set(p,typeof(Transform),"position",jet,start);
                var direction=g.Call(typeof(Vector3),"op_Subtraction",null,new[]{typeof(Vector3),typeof(Vector3)},aim,start).result;
                p=g.Set(p,typeof(Transform),"rotation",jet,g.Call(typeof(Quaternion),"LookRotation",null,new[]{typeof(Vector3),typeof(Vector3)},direction,Vector3.up).result);
                var distance=g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},start,aim).result;
                p=g.Set(p,typeof(ParticleSystem.MainModule),"startLifetimeMultiplier",g.Get(typeof(ParticleSystem),"main",jet.GetComponent<ParticleSystem>()),g.Binary<ScalarDivide>(distance,11f));
            }
            return p;'''
assert old in source
pointer.write_text(source.replace(old,new,1),encoding='utf-8',newline='\n')

files.append(change('Assets/YanYana/Editor/YanYanaImmersionQA.cs',
'                for(int target=0;target<3;target++)\n                {',
'''                if(stage==1)YanYanaDeliveryTools.StartRecording();
                for(int target=0;target<3;target++)
                {'''))
qa=files[-1][1]
source=qa.read_text(encoding='utf-8')
source=source.replace('                            ScreenCapture.CaptureScreenshot(folder+"/spray"+stage+"-"+target+".png");captured=true;',
'''                            var impact=(ParticleSystem)new SerializedObject(Find("Gerçek müdahale alanı "+stage).GetComponent<Deprem.Minigames.FirefighterExtinguishManager>()).FindProperty("waterImpact").objectReferenceValue;
                            var jet=impact.transform.Find("Basınçlı su damlaları").GetComponent<ParticleSystem>();
                            if(!jet.isEmitting||jet.particleCount==0||impact.particleCount==0)
                                throw new InvalidOperationException("Pressure jet or impact splash did not emit during held screen input.");
                            if(stage==1&&target==1)YanYanaFirePresentation.ReviewPose();
                            ScreenCapture.CaptureScreenshot(folder+"/spray"+stage+"-"+target+".png");captured=true;''')
source=source.replace('                        if(stream.enabled)throw new InvalidOperationException("Water did not stop on pointer release.");',
'''                        var impact=(ParticleSystem)new SerializedObject(manager).FindProperty("waterImpact").objectReferenceValue;
                        if(stream.enabled||impact.GetComponentsInChildren<ParticleSystem>().Any(ps=>ps.isEmitting))
                            throw new InvalidOperationException("Water or pressure mist did not stop on pointer release.");''')
source=source.replace('                File.AppendAllText(physicalCheckReport,"PASS stage "+stage', '                if(stage==1)YanYanaDeliveryTools.StopRecording();\n                File.AppendAllText(physicalCheckReport,"PASS stage "+stage')
# Insert immediately after each stage's target loop; preserve the remaining checks.
if 'if(stage==1)YanYanaDeliveryTools.StopRecording();' not in source:
    needle='                }\n            }\n            foreach(var f in WaitPhysical(()=>State("FireCompleted")'
    assert needle in source
    source=source.replace(needle,'                }\n                if(stage==1)YanYanaDeliveryTools.StopRecording();\n            }\n            foreach(var f in WaitPhysical(()=>State("FireCompleted")',1)
qa.write_text(source,encoding='utf-8',newline='\n')

shader=stage/'FirefighterWater.shader'
shader.write_text('''Shader "Deprem/Firefighter Water"
{
    Properties { _BaseColor ("Tint", Color) = (.86,.96,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            float _DepremFxTime;
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color*_BaseColor; return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 a=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            half4 Frag(Varyings i):SV_Target
            {
                float x=saturate(i.uv.x), flow=_DepremFxTime*17;
                float ripple=Noise(float2(x*37-flow,i.uv.y*6));
                float center=(ripple-.5)*.11*x;
                float y=abs((i.uv.y-.5)*2-center);
                float core=1-smoothstep(lerp(.46,.18,x),lerp(.91,.55,x),y);
                float foam=Noise(float2(x*72-flow*2.3,i.uv.y*19));
                float wisps=(1-smoothstep(.3,1,y))*smoothstep(.35,.85,ripple)*x;
                float alpha=(core*lerp(.72,.88,foam)+wisps*.22)*(1-smoothstep(.97,1,x));
                half3 tint=lerp(half3(.48,.79,.92),half3(.94,.99,1),core*.65+foam*.35);
                return half4(tint*i.color.rgb,alpha*i.color.a);
            }
            ENDHLSL
        }
    }
}
''',encoding='utf-8',newline='\n')
files.append(('Assets/YanYana/Art/Shaders/FirefighterWater.shader',shader))
if '--apply' in sys.argv:
    for relative,target in files:
        shutil.copyfile(target,root/relative)
print('Prepared '+str(len(files))+' editor/shader files'+(' and applied' if '--apply' in sys.argv else ' outside Assets'))
