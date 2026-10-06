// Editor authoring only. Native particles and embedded scene graphs run the presentation.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.VisualScripting;
using TMPro;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static class YanYanaFirePresentation
    {
        const string Art = "Assets/YanYana/Art/Materials/";
        const string GraphTitle = "Su ile azalan alev; duman ve ıslak iz kalır";
        static T[] All<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        static Transform Find(string name) => All<Transform>().First(t => t.name == name);

        [MenuItem("Tools/Yan Yana/Presentation/Polish Firefighting")]
        public static void ApplyAndSave()
        {
            Apply();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Fire presentation saved: layered flames, independent smoke, cooled targets and authored hose grip.");
        }

        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring fire presentation.");
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Any(r => r.name.StartsWith("01 Akış")))
                throw new InvalidOperationException("Open YanYana_Adventure before authoring.");
            var flame = Material("FirefighterLivingFlame", "Deprem/Firefighter Flame", Color.white);
            var smoke = Material("FirefighterSmoke", "Deprem/Adventure Particles", new Color(.76f,.78f,.76f,.64f), 1);
            var droplets = Material("FirefighterDroplets", "Deprem/Adventure Particles", new Color(.76f,.91f,1f,.9f), 2);
            var scorch = Material("FirefighterScorch", "Deprem/Adventure Particles", new Color(.15f,.105f,.075f,.63f), 4);
            var wet = Material("FirefighterWetGround", "Deprem/Adventure Particles", new Color(.19f,.28f,.29f,.52f), 4);
            var timber = Material("FirefighterCharredWood", "Universal Render Pipeline/Lit", new Color(.24f,.15f,.09f));
            var ash = Material("FirefighterAsh", "Universal Render Pipeline/Lit", new Color(.14f,.15f,.145f));
            var metal = Material("FirefighterBurnedMetal", "Universal Render Pipeline/Lit", new Color(.25f,.29f,.285f));
            foreach (var material in new[] { timber, ash, metal }) material.SetFloat("_Smoothness", .12f);
            var owner = All<Transform>().First(t => t.name.StartsWith("01 Akış")).gameObject;
            Find("Sol el hortum kavrama").localPosition = new Vector3(0,0,.18f);

            foreach (var manager in All<FirefighterExtinguishManager>().Where(m => m.name.StartsWith("Gerçek müdahale alanı ")))
            {
                var data = new SerializedObject(manager);
                data.FindProperty("extinguishSeconds").floatValue = 2.6f;
                data.FindProperty("sprayRadius").floatValue = .26f;
                data.FindProperty("upperBodyAimWeight").floatValue = 0;
                var bindings = data.FindProperty("fires");
                for (int i = 0; i < bindings.arraySize; i++)
                {
                    var binding = bindings.GetArrayElementAtIndex(i);
                    var collider = (BoxCollider)binding.FindPropertyRelative("hitCollider").objectReferenceValue;
                    var target = collider.transform;
                    collider.center = new Vector3(0,.34f,0);
                    collider.size = new Vector3(.57f,.82f,.57f);
                    var oldVisual = target.Find("Alev, duman ve ışık");
                    if (oldVisual) oldVisual.gameObject.SetActive(false);
                    var parcel = target.Find("Parcel"); if (parcel) parcel.gameObject.SetActive(false);
                    Remove(target, "Söndürme ilerlemesi");
                    var progress = Group("Söndürme ilerlemesi", target);
                    binding.FindPropertyRelative("visualRoot").objectReferenceValue = progress;
                    binding.FindPropertyRelative("glow").objectReferenceValue = null;
                    var effects = BuildTarget(target, i, flame, smoke, scorch, wet, timber, ash, metal);
                    BindSuppression(target, progress, effects.flames, effects.smoke, effects.light, effects.wet, owner);
                }
                ConfigureWater((ParticleSystem)data.FindProperty("waterImpact").objectReferenceValue, droplets);
                var stream = (LineRenderer)data.FindProperty("waterStream").objectReferenceValue;
                stream.startWidth = .030f; stream.endWidth = .055f;
                stream.numCapVertices = 8; stream.numCornerVertices = 8;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(new Color(.86f,.97f,1),0), new GradientColorKey(new Color(.65f,.85f,.93f),1) },
                    new[] { new GradientAlphaKey(.94f,0), new GradientAlphaKey(.70f,1) });
                stream.colorGradient = gradient;
                data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(manager);
            }
            foreach (var preview in All<Transform>().Where(t => t.name == "Bekleyen yangın odağı").ToArray())
            {
                foreach (var child in preview.Cast<Transform>().Where(t => t.name != "Yangın sunumu").ToArray()) child.gameObject.SetActive(false);
                var effects=BuildTarget(preview, preview.GetSiblingIndex(), flame, smoke, scorch, wet, timber, ash, metal);
                effects.flames.localScale=new Vector3(.82f,.70f,.82f); effects.light.intensity=.65f;
            }
            foreach (var patch in All<Transform>().Where(t => t.name.StartsWith("Söndürülmüş ıslak alan ")))
            {
                var renderer = patch.GetComponent<MeshRenderer>(); if (renderer) renderer.enabled = false;
            }
            // Keep the existing detached completion steam and pause system.
            foreach (var steam in All<ParticleSystem>().Where(p => p.name.StartsWith("Sönünce kalan buhar · ")))
            {
                var main = steam.main; main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f,1.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.25f,.48f); main.startSize = new ParticleSystem.MinMaxCurve(.12f,.22f);
            }
            YanYanaAdventureBuilder.RefreshImmersiveFireInput();
            AuthorStatus(owner);
            YanYanaAdventureBuilder.RefreshPresentationPause();
        }

        static Material Material(string name, string shaderName, Color color, float kind = -1)
        {
            var shader = Shader.Find(shaderName); if (!shader) throw new InvalidOperationException("Missing shader " + shaderName);
            string path = Art + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.shader = shader; material.SetColor("_BaseColor",color);
            if (kind >= 0) { material.SetFloat("_Kind",kind); material.SetFloat("_SoftDistance",.035f); }
            EditorUtility.SetDirty(material); return material;
        }
        static Transform Group(string name, Transform parent, Vector3 position = default)
        {
            var transform = new GameObject(name).transform; transform.SetParent(parent,false); transform.localPosition = position; return transform;
        }
        static void Remove(Transform parent, string name)
        {
            var old = parent.Find(name); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, float yaw = 0)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.transform.localRotation = Quaternion.Euler(0,yaw,0);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            if (type == PrimitiveType.Quad) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
            return go;
        }
        static GameObject Ground(string name, Transform parent, float height, Vector2 size, Material material)
        {
            var go = Shape(name,PrimitiveType.Quad,parent,Vector3.up*height,new Vector3(size.x,size.y,1),material);
            go.transform.localRotation = Quaternion.Euler(90,0,0); return go;
        }
        static (Transform flames, ParticleSystem smoke, Light light, GameObject wet) BuildTarget(Transform target, int variant, Material flameMaterial, Material smokeMaterial, Material scorchMaterial, Material wetMaterial, Material timber, Material ash, Material metal)
        {
            Remove(target,"Yangın sunumu");
            var root = Group("Yangın sunumu",target);
            Ground("Yanık zemin",root,.009f,new Vector2(.84f,.80f),scorchMaterial);
            var wet = Ground("Hedefte kalan ıslak iz",root,.013f,new Vector2(.93f,.88f),wetMaterial); wet.SetActive(false);
            var fuel = Group("Yanan malzeme",root); fuel.localRotation = Quaternion.Euler(0,19+variant*23,0);
            if (variant % 3 == 0)
            {
                for (int i=0;i<4;i++) Shape("Kömürleşen palet çıtası",PrimitiveType.Cube,fuel,new Vector3(0,.10f,i*.11f-.165f),new Vector3(.49f,.065f,.065f),timber);
                for (int i=0;i<2;i++) Shape("Palet ayağı",PrimitiveType.Cube,fuel,new Vector3(i*.32f-.16f,.045f,0),new Vector3(.075f,.08f,.42f),ash);
                Shape("Yanmış tahta parçası",PrimitiveType.Cube,fuel,new Vector3(.02f,.20f,0),new Vector3(.10f,.10f,.46f),timber,34);
            }
            else if (variant % 3 == 1)
            {
                Shape("Yanmış kasa tabanı",PrimitiveType.Cube,fuel,new Vector3(0,.06f,0),new Vector3(.43f,.08f,.38f),ash);
                for (int i=0;i<2;i++)
                {
                    Shape("Kasa yan tahtası",PrimitiveType.Cube,fuel,new Vector3(i*.37f-.185f,.18f,0),new Vector3(.045f,.18f,.38f),timber);
                    Shape("Kasa uç tahtası",PrimitiveType.Cube,fuel,new Vector3(0,.18f,i*.34f-.17f),new Vector3(.38f,.13f,.045f),timber);
                }
                Shape("Kararmış kasa içi",PrimitiveType.Cube,fuel,new Vector3(0,.12f,0),new Vector3(.31f,.07f,.29f),ash);
            }
            else
            {
                for (int i=0;i<3;i++) Shape("Kırık ahşap",PrimitiveType.Cube,fuel,new Vector3(i*.13f-.13f,.07f+i*.025f,0),new Vector3(.085f,.10f,.49f),i==1?ash:timber,i*21-20);
                Shape("Bükülmüş metal kalıntı",PrimitiveType.Cube,fuel,new Vector3(.12f,.15f,-.04f),new Vector3(.08f,.06f,.34f),metal,45);
            }
            var tongues = Group("Alev dilleri",root,new Vector3(0,.16f,0));
            Flame("Dış alev",tongues,flameMaterial,variant,false);
            Flame("Sıcak alev çekirdeği",tongues,flameMaterial,variant,true);
            var smoke = Smoke(root,smokeMaterial,variant);
            var lamp = Group("Alevin çevre ışığı",root,Vector3.up*.32f).gameObject.AddComponent<Light>();
            lamp.type = LightType.Point; lamp.color = new Color(1,.50f,.14f); lamp.range=1.7f; lamp.intensity=1.3f; lamp.shadows=LightShadows.None;
            return (tongues,smoke,lamp,wet);
        }
        static Gradient Fade(Color start, Color end, float opacity)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(start,0), new GradientColorKey(end,1) },
                new[] { new GradientAlphaKey(0,0), new GradientAlphaKey(opacity,.15f), new GradientAlphaKey(opacity*.75f,.52f), new GradientAlphaKey(0,1) });
            return gradient;
        }
        static ParticleSystem Particle(string name, Transform parent, Material material, uint seed)
        {
            var ps = Group(name,parent).gameObject.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed=false; ps.randomSeed=seed;
            var main=ps.main; main.loop=true; main.playOnAwake=true; main.prewarm=true; main.duration=2;
            main.startColor=Color.white; main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var renderer=ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false; renderer.sortMode=ParticleSystemSortMode.Distance;
            return ps;
        }
        static void Flame(string name, Transform parent, Material material, int variant, bool core)
        {
            var ps=Particle(name,parent,material,(uint)(71+variant*17+(core?3:0)));
            var main=ps.main; main.simulationSpace=ParticleSystemSimulationSpace.Local; main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            main.startLifetime=new ParticleSystem.MinMaxCurve(core?.35f:.48f,core?.54f:.82f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.16f,core?.32f:.43f); main.maxParticles=core?16:26;
            main.startSize3D=true;
            main.startSizeX=new ParticleSystem.MinMaxCurve(core?.14f:.24f,core?.22f:.36f);
            main.startSizeY=new ParticleSystem.MinMaxCurve(core?.24f:.64f,core?.40f:.96f);
            main.startSizeZ=.2f; main.startRotation=new ParticleSystem.MinMaxCurve(-.10f,.10f);
            var emission=ps.emission; emission.rateOverTime=core?11:18;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.radius=core?.09f:.19f; shape.angle=7; shape.rotation=new Vector3(-90,0,0);
            var color=ps.colorOverLifetime; color.enabled=true; color.color=Fade(Color.white,new Color(1,.64f,.40f),core?.74f:.82f);
            var size=ps.sizeOverLifetime; size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.62f),new Keyframe(.22f,1),new Keyframe(.72f,.72f),new Keyframe(1,.08f)));
            var noise=ps.noise; noise.enabled=true; noise.strength=.045f; noise.frequency=1.4f; noise.scrollSpeed=.6f; noise.quality=ParticleSystemNoiseQuality.Low;
        }
        static ParticleSystem Smoke(Transform parent, Material material, int variant)
        {
            var ps=Particle("Duman yukarıda dağılır",parent,material,(uint)(121+variant*7)); ps.transform.localPosition=Vector3.up*.40f;
            var main=ps.main; main.simulationSpace=ParticleSystemSimulationSpace.World; main.scalingMode=ParticleSystemScalingMode.Shape;
            main.startLifetime=new ParticleSystem.MinMaxCurve(1.5f,2.3f); main.startSpeed=new ParticleSystem.MinMaxCurve(.22f,.40f);
            main.startSize=new ParticleSystem.MinMaxCurve(.18f,.30f); main.startRotation=new ParticleSystem.MinMaxCurve(-3f,3f); main.maxParticles=18;
            var emission=ps.emission; emission.rateOverTime=5;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.radius=.15f; shape.angle=12; shape.rotation=new Vector3(-90,0,0);
            var color=ps.colorOverLifetime; color.enabled=true; color.color=Fade(new Color(.28f,.27f,.245f),new Color(.58f,.58f,.54f),.33f);
            var size=ps.sizeOverLifetime; size.enabled=true; size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.65f,1,2.2f));
            var noise=ps.noise; noise.enabled=true; noise.strength=.08f; noise.frequency=.6f; noise.scrollSpeed=.18f;
            return ps;
        }
        static void ConfigureWater(ParticleSystem ps, Material material)
        {
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.simulationSpace=ParticleSystemSimulationSpace.World; main.startLifetime=new ParticleSystem.MinMaxCurve(.18f,.38f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.7f,1.45f); main.startSize=new ParticleSystem.MinMaxCurve(.014f,.030f);
            main.gravityModifier=1.2f; main.maxParticles=42; main.startColor=Color.white;
            var emission=ps.emission; emission.rateOverTime=65;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Hemisphere; shape.radius=.045f; shape.rotation=new Vector3(-90,0,0);
            var color=ps.colorOverLifetime; color.enabled=true; color.color=Fade(Color.white,new Color(.66f,.85f,.92f),.9f);
            var size=ps.sizeOverLifetime; size.enabled=true; size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.32f));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
        }
        static void BindSuppression(Transform target, Transform progress, Transform flames, ParticleSystem smoke, Light lamp, GameObject wet, GameObject owner)
        {
            foreach(var old in target.GetComponents<ScriptMachine>().Where(m=>m.graph.title==GraphTitle).ToArray()) UnityEngine.Object.DestroyImmediate(old);
            var graph=new YanYanaGraphAuthor(target.gameObject,GraphTitle); graph.Initial("Cooled",false);
            var tick=graph.Add(new Unity.VisualScripting.LateUpdate());
            var free=graph.Branch(tick.trigger,graph.Binary<Equal>(graph.Var("Paused",owner),false));
            var health=graph.Call(typeof(Mathf),"Clamp01",null,new[]{typeof(float)},
                graph.Binary<ScalarDivide>(graph.Binary<ScalarSubtract>(graph.Get(typeof(Vector3),"y",graph.Get(typeof(Transform),"localScale",progress)),.34f),.66f)).result;
            var lit=graph.Branch(free.ifTrue,graph.Get(typeof(GameObject),"activeSelf",progress.gameObject));
            var width=graph.Call(typeof(Mathf),"Lerp",null,new[]{typeof(float),typeof(float),typeof(float)},.24f,1f,health).result;
            var height=graph.Call(typeof(Mathf),"Lerp",null,new[]{typeof(float),typeof(float),typeof(float)},.08f,1f,health).result;
            var scale=graph.Call(typeof(Vector3),"op_Addition",null,new[]{typeof(Vector3),typeof(Vector3)},
                graph.Call(typeof(Vector3),"op_Multiply",null,new[]{typeof(Vector3),typeof(float)},new Vector3(1,0,1),width).result,
                graph.Call(typeof(Vector3),"op_Multiply",null,new[]{typeof(Vector3),typeof(float)},Vector3.up,height).result).result;
            var p=graph.Set(lit.ifTrue,typeof(Transform),"localScale",flames,scale);
            var pulse=graph.Call(typeof(Mathf),"Sin",null,new[]{typeof(float)},graph.Binary<ScalarSubtract>(
                graph.Binary<ScalarMultiply>(graph.Call(typeof(Shader),"GetGlobalFloat",null,new[]{typeof(string)},"_DepremFxTime").result,9f),target.position.x*2.1f)).result;
            p=graph.Set(p,typeof(Light),"intensity",lamp,graph.Binary<ScalarMultiply>(health,graph.Binary<ScalarSubtract>(1.3f,graph.Binary<ScalarMultiply>(pulse,-.13f))));
            foreach(var ps in flames.GetComponentsInChildren<ParticleSystem>())
                p=graph.Set(p,typeof(ParticleSystem.EmissionModule),"rateOverTimeMultiplier",graph.Get(typeof(ParticleSystem),"emission",ps),graph.Binary<ScalarMultiply>(health,ps.emission.rateOverTimeMultiplier));
            p=graph.Set(p,typeof(ParticleSystem.EmissionModule),"rateOverTimeMultiplier",graph.Get(typeof(ParticleSystem),"emission",smoke),graph.Binary<ScalarMultiply>(health,5f));
            graph.Active(p,wet,graph.Binary<Less>(health,.90f));
            var cool=graph.Branch(lit.ifFalse,graph.Binary<Equal>(graph.Var("Cooled"),false));
            p=graph.SetVar(cool.ifTrue,"Cooled",true);
            foreach(var ps in flames.GetComponentsInChildren<ParticleSystem>())
                p=graph.Do(p,typeof(ParticleSystem),"Stop",ps,new[]{typeof(bool),typeof(ParticleSystemStopBehavior)},false,ParticleSystemStopBehavior.StopEmitting);
            p=graph.Set(p,typeof(Transform),"localScale",flames,new Vector3(.24f,.04f,.24f));
            p=graph.Do(p,typeof(ParticleSystem),"Stop",smoke,new[]{typeof(bool),typeof(ParticleSystemStopBehavior)},false,ParticleSystemStopBehavior.StopEmitting);
            p=graph.Set(p,typeof(Behaviour),"enabled",lamp,false); graph.Active(p,wet,true); graph.Dirty();
        }

        static void AuthorStatus(GameObject owner)
        {
            const string title="Etkin yangın grubu ve kalan odak sayısı";
            foreach(var old in owner.GetComponents<ScriptMachine>().Where(m=>m.graph.title==title).ToArray()) UnityEngine.Object.DestroyImmediate(old);
            var parent=Find("FireHelp").parent; Remove(parent,"Yangın müdahale sayacı");
            var panel=new GameObject("Yangın müdahale sayacı",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(parent,false);
            var rect=(RectTransform)panel.transform; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(20,-134); rect.sizeDelta=new Vector2(188,56);
            var background=panel.GetComponent<Image>(); background.sprite=Find("FireHelp").GetComponent<Image>().sprite; background.type=Image.Type.Sliced;
            background.color=new Color(.07f,.18f,.18f,.96f); background.raycastTarget=false;
            var label=new GameObject("Kalan odak",typeof(RectTransform),typeof(TextMeshProUGUI)); label.transform.SetParent(panel.transform,false);
            var labelRect=(RectTransform)label.transform; labelRect.anchorMin=Vector2.zero; labelRect.anchorMax=Vector2.one; labelRect.offsetMin=new Vector2(8,4); labelRect.offsetMax=new Vector2(-8,-4);
            var text=label.GetComponent<TextMeshProUGUI>(); text.font=Find("FireHelp").GetComponentInChildren<TMP_Text>().font; text.fontSize=20; text.fontStyle=FontStyles.Bold;
            text.color=new Color(1,.96f,.86f); text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; text.text="1/3 · 3 odak";
            panel.SetActive(false);
            var g=new YanYanaGraphAuthor(owner,title); var tick=g.Add(new Unity.VisualScripting.LateUpdate());
            var free=g.Binary<And>(g.Binary<Equal>(g.Var("Busy"),false),g.Binary<Equal>(g.Var("Paused"),false));
            var isFire=g.Call(typeof(string),"StartsWith",g.Var("Workspace"),new[]{typeof(string)},"fire").result;
            var p=g.Active(tick.trigger,panel,g.Binary<And>(free,isFire));
            for(int stage=1;stage<=3;stage++)
            {
                var manager=Find("Gerçek müdahale alanı "+stage).GetComponent<FirefighterExtinguishManager>();
                var branch=g.Branch(p,g.Binary<Equal>(g.Var("Workspace"),"fire"+stage));
                var count=g.Call(typeof(Convert),"ToString",null,new[]{typeof(int)},g.Get(typeof(FirefighterExtinguishManager),"RemainingFireCount",manager)).result;
                var status=g.Call(typeof(string),"Concat",null,new[]{typeof(string),typeof(string)},stage+"/3 · ",count).result;
                var line=g.Call(typeof(string),"Concat",null,new[]{typeof(string),typeof(string)},status," odak").result;
                g.Set(branch.ifTrue,typeof(TMP_Text),"text",text,line); p=branch.ifFalse;
            }
            g.Dirty();
        }

        [MenuItem("Tools/Yan Yana/QA/Calibrate Fire Grip")]
        public static void CalibrateGrip()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before calibrating.");
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var actor=UnityEngine.Object.Instantiate(Find("Idil").gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
                actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                foreach(var machine in actor.GetComponentsInChildren<ScriptMachine>(true)) machine.enabled=false;
                var animator=actor.GetComponentInChildren<Animator>(); var definitions=YanYanaInteractionHands.Read("Idil");
                var solve=typeof(FirefighterExtinguishManager).GetMethod("SolveArm",BindingFlags.Static|BindingFlags.NonPublic);
                var results=new List<(float score,string row)>();
                foreach(float y in new[]{-.06f,-.08f,-.10f,-.12f}) foreach(float z in new[]{0,.02f,.04f,.06f})
                {
                    float max=0,maxBend=0;
                    foreach(float yaw in new[]{-35f,0,35f}) foreach(float pitch in new[]{5f,20f,35f})
                    {
                        animator.Rebind(); animator.Update(.2f);
                        var origin=(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position+animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position)*.5f+new Vector3(.025f,y,z);
                        var rotation=Quaternion.Euler(pitch,yaw,0);
                        foreach(bool right in new[]{true,false})
                        {
                            var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
                            var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
                            var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                            var definition=definitions.hands.Single(h=>h.right==right);
                            var pose=definition.poses.Single(p=>p.name==(right?"Right_Loop":"Left_Cylinder"));
                            var contact=origin+rotation*(right?new Vector3(0,-.082f,.023f):new Vector3(0,0,.18f));
                            var handleAxis=rotation*(right?Vector3.up:Vector3.forward);
                            for(int pass=0;pass<12;pass++)
                            {
                                solve.Invoke(null,new object[]{upper,lower,hand,Vector3.Lerp(hand.position,contact-hand.TransformVector(pose.center),.55f),new Vector3(right?.4f:-.4f,-1,-.1f)});
                                var axis=hand.position-lower.position; var across=hand.TransformDirection(Vector3.Cross(definition.forward,definition.normal).normalized);
                                float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(across,axis),Vector3.ProjectOnPlane(handleAxis,axis),axis);
                                lower.rotation=Quaternion.AngleAxis(angle*.55f,axis)*lower.rotation;
                            }
                            max=Mathf.Max(max,Vector3.Distance(hand.TransformPoint(pose.center),contact));
                            maxBend=Mathf.Max(maxBend,Vector3.Angle(lower.position-upper.position,hand.position-lower.position));
                        }
                    }
                    results.Add((max+Mathf.Max(0,maxBend-140)*.001f,$"{y:F2},{z:F2},{max:F5},{maxBend:F1}"));
                }
                Directory.CreateDirectory("ClientExports/YanYana/Reports");
                File.WriteAllLines("ClientExports/YanYana/Reports/fire-grip-calibration.csv",new[]{"shoulderOffsetY,shoulderOffsetZ,maxContactErrorMetres,maxElbowBendDegrees"}.Concat(results.OrderBy(r=>r.score).Select(r=>r.row)));
                UnityEngine.Object.DestroyImmediate(actor);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
