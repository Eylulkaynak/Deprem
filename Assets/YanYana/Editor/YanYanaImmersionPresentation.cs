// Editor authoring: the player contains scene graphs and existing native components only.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Cinemachine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static class YanYanaImmersionPresentation
    {
        [Serializable] public class Shot { public string name; public Vector3 position, euler; public float size; }
        [Serializable] public class Storyboard { public Shot[] shots; }
        static T[] All<T>() where T:UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static Transform Find(string name)=>All<Transform>().First(t=>t.name==name);
        const string StoryboardPath="ArtDirection/YanYana/Camera/ImmersionShots.json";
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play before authoring presentation.");
            Cameras();FireVolumes();Nozzle();Coupling();StreetDepth();Clock();YanYanaAdventureBuilder.RefreshImmersiveFireInput();
        }
        static void Cameras()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StoryboardPath));
            if(!File.Exists(StoryboardPath))File.WriteAllText(StoryboardPath,JsonUtility.ToJson(new Storyboard{shots=All<CinemachineCamera>().Select(c=>new Shot{name=c.name,position=c.transform.position,euler=c.transform.eulerAngles,size=c.Lens.OrthographicSize}).ToArray()},true));
            var originals=JsonUtility.FromJson<Storyboard>(File.ReadAllText(StoryboardPath)).shots.ToDictionary(s=>s.name);
            var brain=Camera.main.GetComponent<CinemachineBrain>();brain.LensModeOverride=new CinemachineBrain.LensModeOverrideSettings{Enabled=true,DefaultMode=LensSettings.OverrideModes.Perspective};
            const string blendPath="Assets/YanYana/Animation/ImmersionCameraBlends.asset";
            var blends=AssetDatabase.LoadAssetAtPath<CinemachineBlenderSettings>(blendPath);if(!blends){blends=ScriptableObject.CreateInstance<CinemachineBlenderSettings>();AssetDatabase.CreateAsset(blends,blendPath);}
            blends.CustomBlends=new[]{new CinemachineBlenderSettings.CustomBlend{From="AD · mahalle açılış kadrajı",To="**ANY CAMERA**",Blend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut,0)},new CinemachineBlenderSettings.CustomBlend{From="**ANY CAMERA**",To="AD · mahalle açılış kadrajı",Blend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut,0)},new CinemachineBlenderSettings.CustomBlend{From="Yakından incele · aid",To="Yakından incele · relief",Blend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut,1.85f)}};brain.CustomBlends=blends;EditorUtility.SetDirty(blends);
            foreach(var view in All<CinemachineCamera>())
            {
                if(view.name=="AD · mahalle açılış kadrajı")continue;
                var old=originals[view.name];var lens=view.Lens;lens.ModeOverride=LensSettings.OverrideModes.Perspective;lens.FieldOfView=52;lens.NearClipPlane=.06f;lens.FarClipPlane=65;
                var follow=view.GetComponent<CinemachineFollow>();
                if(follow)
                {
                    follow.FollowOffset=view.name.EndsWith("introCarry")?new Vector3(-1.25f,2.15f,-4.2f):new Vector3(-2.1f,2.05f,-4.15f);
                    follow.TrackerSettings.PositionDamping=new Vector3(.35f,.65f,.35f);
                    view.transform.SetPositionAndRotation(view.Follow.position+follow.FollowOffset,Quaternion.LookRotation(-follow.FollowOffset,Vector3.up));lens.FieldOfView=54;
                }
                else if(view.name.StartsWith("Yakından incele · fire"))
                {
                    int stage=int.Parse(view.name.Substring(view.name.Length-1));var center=new[]{new Vector3(-5.1f,-.48f,-18),new Vector3(-2.7f,-.48f,-19),new Vector3(-4,-.48f,-21)}[stage-1];
                    view.transform.position=center+new Vector3(-2.9f,2.45f,5.95f);view.transform.LookAt(center+new Vector3(.1f,.9f,.35f));lens.FieldOfView=54;
                }
                else
                {
                    string key=view.name.Replace("Yakından incele · ","");
                    bool table=old.euler.x>85&&old.euler.x<95;
                    if(table)
                    {
                        var focus=old.position+Quaternion.Euler(old.euler)*Vector3.forward*2;
                        float yaw=key=="flashlight"||key=="radio"?90:key=="bag"||key=="bagfit"?180:0;
                        float pitch=key=="map"?50:55;float half=old.size;
                        lens.FieldOfView=key=="bag"?57:46;
                        float distance=half/Mathf.Tan(lens.FieldOfView*.5f*Mathf.Deg2Rad);
                        var rotation=Quaternion.Euler(pitch,yaw,0);view.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*distance,rotation);
                        if(key=="bag")view.transform.position-=view.transform.up*.46f;
                    }
                    else
                    {
                        view.transform.SetPositionAndRotation(old.position,Quaternion.Euler(old.euler));
                        lens.FieldOfView=key.StartsWith("reunion")&&!key.Contains("Route")&&!key.Contains("Approach")?55:52;
                        if(key.StartsWith("reunionRoute")||key.StartsWith("reunionApproach")||key=="aid")
                        {
                            float focusHeight=key=="aid"?.35f:.3f;var forward=Quaternion.Euler(old.euler)*Vector3.forward;
                            float d=(focusHeight-old.position.y)/forward.y;var focus=old.position+forward*d;
                            float distance=Mathf.Max(4,old.size*.86f/Mathf.Tan(26*Mathf.Deg2Rad));var rotation=Quaternion.Euler(32,old.euler.y,0);
                            view.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*distance,rotation);
                        }
                        if(key=="intro") {view.transform.position=new Vector3(-1.15f,1.95f,-.45f);view.transform.LookAt(new Vector3(-2.4f,.78f,-2.9f));lens.FieldOfView=48;}
                        if(key=="introDrop") {view.transform.position=new Vector3(3.05f,2.3f,-4.7f);view.transform.LookAt(new Vector3(1.85f,.83f,-3.2f));lens.FieldOfView=48;}
                        if(key=="cover") {var focus=Find("Anchor_CoverAda").position+Vector3.up*.5f;view.transform.position=focus+new Vector3(-.65f,.65f,-2.7f);view.transform.LookAt(focus);lens.FieldOfView=52;}
                        if(key=="aftershock") {var focus=old.position+Quaternion.Euler(old.euler)*Vector3.forward*1.6f;view.transform.position=focus+new Vector3(-.8f,1.0f,-3.1f);view.transform.LookAt(focus);lens.FieldOfView=50;}
                        if(key=="sibling") {view.transform.position=new Vector3(.7f,1.4f,-4.2f);view.transform.LookAt(new Vector3(.7f,.8f,-1.65f));lens.FieldOfView=48;}
                        if(key=="relief") {view.transform.position=new Vector3(4,1.8f,-23.1f);view.transform.LookAt(new Vector3(3,.5f,-20.2f));lens.FieldOfView=50;}
                    }
                }
                view.Lens=lens;
            }
            Camera.main.orthographic=false;
        }
        static Material Material(string name,string shader)
        {
            string path="Assets/YanYana/Art/Materials/"+name+".mat";var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!value){value=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(value,path);}value.shader=Shader.Find(shader);EditorUtility.SetDirty(value);return value;
        }
        static void FireVolumes()
        {
            var volumeAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/FireVolume.fbx");
            if(!volumeAsset)throw new InvalidOperationException("Import the authored FireVolume FBX first.");
            var fire=Material("FireVolumeWarmth","Deprem/Volumetric Fire");fire.SetFloat("_Intensity",1.0f);
            var soot=Material("ScorchedParcel","Universal Render Pipeline/Lit");soot.SetColor("_BaseColor",new Color(.19f,.12f,.075f));soot.SetFloat("_Smoothness",.08f);
            var ember=Material("FireEmber","Deprem/Adventure Particles");ember.SetColor("_BaseColor",new Color(2.1f,.9f,.17f));ember.SetFloat("_Kind",2);ember.SetFloat("_SoftDistance",.025f);
            foreach(var parent in All<Transform>().Where(t=>t.name=="Alev, duman ve ışık"||t.name=="Bekleyen yangın odağı").ToArray())
            {
                foreach(var old in parent.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="FlameTuft"||t.name=="Hareketli sıcak alev").ToArray())old.gameObject.SetActive(false);
                var previous=parent.Find("Hacimli sıcak alev");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var model=UnityEngine.Object.Instantiate(volumeAsset,parent,false);model.name="Hacimli sıcak alev";model.transform.localPosition=new Vector3(0,.18f,0);
                float variation=Mathf.Abs(Mathf.Sin(parent.position.x*4.37f+parent.position.z*2.15f));model.transform.localScale=new Vector3(.83f,.72f+variation*.24f,.83f);model.transform.localRotation=Quaternion.Euler(0,variation*270,0);
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>()){renderer.sharedMaterial=fire;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
                var sparks=parent.Find("Yükselen küçük kıvılcımlar");if(sparks)UnityEngine.Object.DestroyImmediate(sparks.gameObject);
                var sparkObject=new GameObject("Yükselen küçük kıvılcımlar");sparkObject.transform.SetParent(parent,false);sparkObject.transform.localPosition=Vector3.up*.34f;sparkObject.transform.localRotation=Quaternion.Euler(-90,0,0);
                var ps=sparkObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.loop=true;main.playOnAwake=true;main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.1f);main.startSpeed=new ParticleSystem.MinMaxCurve(.55f,.85f);main.startSize=new ParticleSystem.MinMaxCurve(.018f,.035f);main.maxParticles=9;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=ps.emission;emission.rateOverTime=3;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.13f;shape.angle=12;
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
                var rendererPS=ps.GetComponent<ParticleSystemRenderer>();rendererPS.sharedMaterial=ember;rendererPS.shadowCastingMode=ShadowCastingMode.Off;rendererPS.receiveShadows=false;
                var source=parent.name=="Bekleyen yangın odağı"?parent:parent.parent;
                foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.transform.parent&&r.transform.parent.name=="Parcel"))
                {if(renderer.sharedMaterial&&renderer.sharedMaterial.name.IndexOf("sand",StringComparison.OrdinalIgnoreCase)>=0)renderer.sharedMaterial=soot;}
            }
            foreach(var ps in All<ParticleSystem>().Where(p=>p.name=="Yükselen duman"))
            {
                var m=ps.main;m.startSize=new ParticleSystem.MinMaxCurve(.20f,.38f);m.startLifetime=new ParticleSystem.MinMaxCurve(1.7f,2.8f);m.startSpeed=new ParticleSystem.MinMaxCurve(.36f,.58f);m.maxParticles=28;
                var e=ps.emission;e.rateOverTime=7;var c=ps.colorOverLifetime;c.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(new Color(.36f,.30f,.25f),0),new GradientColorKey(new Color(.59f,.56f,.50f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.27f,.2f),new GradientAlphaKey(.15f,.65f),new GradientAlphaKey(0,1)});c.color=g;
                var n=ps.noise;n.strength=.10f;n.scrollSpeed=.14f;
            }
            foreach(var line in All<LineRenderer>().Where(l=>l.name=="Kesintisiz su akışı")) {line.startWidth=.022f;line.endWidth=.055f;line.numCornerVertices=10;line.numCapVertices=8;}
            foreach(var line in All<LineRenderer>().Where(l=>l.name=="Zeminden gelen besleme hortumu")){line.startWidth=line.endWidth=.065f;line.numCornerVertices=12;line.numCapVertices=8;}
        }
        static void Nozzle()
        {
            var nozzle=Find("İdil’in gerçek hortum başlığı");var old=nozzle.Find("HoseNozzle");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/HoseNozzle.fbx"),nozzle,false);model.name="HoseNozzle";model.transform.localRotation=Quaternion.Euler(90,0,0);
            var materials=AssetDatabase.FindAssets("t:Material",new[]{"Assets/YanYana/Art/Materials"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).GroupBy(m=>m.name).ToDictionary(g=>g.Key,g=>g.First());
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m&&materials.TryGetValue(m.name,out var shared)?shared:m).ToArray();
            Find("Hortum su çıkışı").localPosition=new Vector3(0,0,.416f);
            Find("Sağ el hortum kavrama").localPosition=new Vector3(0,-.082f,.023f);
            Find("Sol el hortum kavrama").localPosition=new Vector3(0,0,.205f);
        }
        static void Coupling()
        {
            void Install(string modelName,Transform parent,bool correctScale=false)
            {
                var prior=parent.Find(modelName);if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/"+modelName+".fbx");if(!asset)throw new InvalidOperationException("Import "+modelName);
                var model=UnityEngine.Object.Instantiate(asset,parent,false);model.name=modelName;
                if(correctScale)model.transform.localScale=new Vector3(1/parent.localScale.x,1/parent.localScale.y,1/parent.localScale.z);
            }
            var root=Find("İdil’in hortum bağlantısı");Install("CouplingStation",root);
            var socket=Find("Pompanın bağlantı ucu");socket.GetComponent<MeshRenderer>().enabled=false;Install("CouplingSocket",socket,true);
            var plug=Find("Hortumun kavrama ucu");plug.GetComponent<MeshRenderer>().enabled=false;Install("CouplingPlug",plug,true);plug.GetComponent<BoxCollider>().size=new Vector3(1.8f,1.8f,1.8f);
            var valve=Find("Su vanası");valve.Find("Vana çubuğu").GetComponent<MeshRenderer>().enabled=false;Install("CouplingWheel",valve);
        }
        static void Clock()
        {
            var owner=All<Transform>().First(t=>t.name.StartsWith("01 Akış")).gameObject;
            const string name="Alev hareketi oyunla birlikte durur";
            foreach(var old in owner.GetComponents<ScriptMachine>().Where(m=>m.graph.title==name).ToArray())UnityEngine.Object.DestroyImmediate(old);
            var g=new YanYanaGraphAuthor(owner,name);g.Initial("FxTime",0f);var tick=g.Add(new Unity.VisualScripting.Update());var free=g.Branch(tick.trigger,g.Binary<Equal>(g.Var("Paused",owner),false));
            var p=g.SetVar(free.ifTrue,"FxTime",g.Binary<ScalarSubtract>(g.Var("FxTime"),g.Binary<ScalarMultiply>(g.Get(typeof(Time),"deltaTime"),-1f)));
            g.Do(p,typeof(Shader),"SetGlobalFloat",null,new[]{typeof(string),typeof(float)},"_DepremFxTime",g.Var("FxTime"));g.Dirty();
        }
        static void StreetDepth()
        {
            var world=All<Transform>().First(t=>t.name.StartsWith("02 Dünya"));var old=world.Find("Sokağın güney cephesi");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Sokağın güney cephesi").transform;root.SetParent(world,false);
            var materials=AssetDatabase.FindAssets("t:Material",new[]{"Assets/YanYana/Art/Materials"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).GroupBy(m=>m.name).ToDictionary(g=>g.Key,g=>g.First());
            for(int i=0;i<4;i++)
            {
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/Facade_"+(i+1)+".fbx"),root,false);model.name="Sokağın sonundaki ev "+(i+1);model.transform.position=new Vector3(-12.2f+i*3.84f,-.48f,-28.6f);model.transform.localScale=Vector3.one*.83f;model.transform.rotation=Quaternion.identity;
                foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m&&materials.TryGetValue(m.name,out var shared)?shared:m).ToArray();
            }
            var help=(RectTransform)Find("FireHelp");help.anchorMin=help.anchorMax=help.pivot=Vector2.one;help.anchoredPosition=new Vector2(-20,-134);help.sizeDelta=new Vector2(164,56);
            var label=help.GetComponentInChildren<TMPro.TMP_Text>(true);label.text="Ekip desteği";label.fontSize=20;label.fontSizeMin=18;label.fontSizeMax=20;
            foreach(string name in new[]{"ActivityBack","RotateItem"})
            {
                var button=(RectTransform)Find(name);bool left=name=="ActivityBack";button.anchorMin=button.anchorMax=button.pivot=new Vector2(left?0:1,1);button.anchoredPosition=new Vector2(left?20:-20,-134);button.sizeDelta=new Vector2(left?164:136,56);
                var text=button.GetComponentInChildren<TMPro.TMP_Text>(true);text.fontSize=20;text.fontSizeMin=18;text.fontSizeMax=20;
            }
        }
    }
}
