// Editor-only inspection and scene authoring. No player component is introduced.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static class YanYanaVisualStability
    {
        const string Reports = "ClientExports/YanYana/Reports/";
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
        static T[] All<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        static string Vec(Vector3 v) => v.ToString("F4", CultureInfo.InvariantCulture);

        [MenuItem("Tools/Yan Yana/Art/Apply Stable Cameras And Effects")]
        public static void ApplyAndSave()
        {
            Apply();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Inspect();
        }
        public static void Apply()
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != YanYanaAdventureBuilder.ScenePath)
                throw new InvalidOperationException("Open the adventure in Edit mode first.");
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.05f);
            brain.LensModeOverride = new CinemachineBrain.LensModeOverrideSettings { Enabled = true, DefaultMode = LensSettings.OverrideModes.Orthographic };
            foreach (var view in All<CinemachineCamera>())
            {
                var lens = view.Lens;
                lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
                lens.NearClipPlane = Mathf.Max(.08f, lens.NearClipPlane);
                view.BlendHint = CinemachineCore.BlendHints.IgnoreTarget | CinemachineCore.BlendHints.FreezeWhenBlendingOut;
                if (view.name == "Ada ile odada gezin") lens.OrthographicSize = 3.10f;
                if (view.name == "Yakından incele · map") lens.OrthographicSize = 1.0f;
                if (view.name.EndsWith("introCarry")) lens.OrthographicSize = 2.6f;
                if (view.name.StartsWith("Yakından incele · fire"))
                {
                    int stage = int.Parse(view.name.Substring(view.name.Length - 1));
                    var center = new[] { new Vector3(-5.1f,-.48f,-18), new Vector3(-2.7f,-.48f,-19), new Vector3(-4,-.48f,-21) }[stage-1];
                    view.transform.position = center + new Vector3(2.1f,3.7f,4.8f);
                    view.transform.LookAt(center + new Vector3(-.25f,.7f,.7f));
                    lens.OrthographicSize = 2.4f;
                }
                var follow = view.GetComponent<CinemachineFollow>();
                if (follow)
                {
                    if (view.name.EndsWith("introCarry")) follow.FollowOffset = new Vector3(0,4.8f,-3.4f);
                    follow.TrackerSettings.PositionDamping = new Vector3(.40f,.65f,.40f);
                    var aim = view.GetComponent<CinemachineRotationComposer>();
                    if (aim) UnityEngine.Object.DestroyImmediate(aim);
                    view.LookAt = null;
                    view.transform.SetPositionAndRotation(view.Follow.position + follow.FollowOffset, Quaternion.LookRotation(-follow.FollowOffset, Vector3.up));
                }
                view.Lens = lens;
            }
            Camera.main.orthographic = true;
            var data = Camera.main.GetUniversalAdditionalCameraData();
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.requiresDepthTexture = true;
            MoveMapIntoRoom();
            MatchIntroArrivalTolerance();
            AuthorTransitionInput();
            StyleEffects();
            AuthorFireContinuity();
            YanYanaImmersionPresentation.Apply();
            ReplaceAspectGraph();
            YanYanaAdventureBuilder.RefreshPresentationPause();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static void MatchIntroArrivalTolerance()
        {
            // The existing mover can settle 4.7 cm away on a curved NavMesh path.
            // Allow 7.5 cm before the authored hand placement; never snap the actor.
            foreach(var machine in All<ScriptMachine>().Where(m=>m.graph.title=="Bizim mahalle · hafif kutuyu iki farklı yoldan taşı"))
            {
                object Fix(object value)=>value is float f&&Mathf.Abs(f-.047f)<.000001f?.075f:value;
                foreach(var unit in machine.graph.units)
                {
                    if(unit is Literal literal)literal.value=Fix(literal.value);
                    foreach(var key in unit.defaultValues.Keys.ToArray())unit.defaultValues[key]=Fix(unit.defaultValues[key]);
                }
                EditorUtility.SetDirty(machine);
            }
        }
        static void ReplaceAspectGraph()
        {
            var owner = All<Transform>().First(t => t.name.StartsWith("01 Akış")).gameObject;
            const string label = "Çentik ve sistem çubuklarından uzak arayüz";
            foreach (var machine in owner.GetComponents<ScriptMachine>().Where(m => m.graph.title == label).ToArray()) UnityEngine.Object.DestroyImmediate(machine);
            var safe = All<RectTransform>().First(t => t.name == "Güvenli ekran alanı");
            var g = new YanYanaGraphAuthor(owner, label); g.Initial("Width",0); g.Initial("Height",0);
            var tick = g.Add(new Unity.VisualScripting.Update());
            var width = g.Get(typeof(Screen),"width"); var height = g.Get(typeof(Screen),"height");
            var changed = g.Branch(tick.trigger,g.Binary<Or>(g.Binary<NotEqual>(g.Var("Width"),width),g.Binary<NotEqual>(g.Var("Height"),height)));
            var area = g.Get(typeof(Screen),"safeArea");
            ValueOutput Vector(object x, object y)
            {
                var a=g.Call(typeof(Vector2),"op_Multiply",null,new[]{typeof(Vector2),typeof(float)},Vector2.right,x).result;
                var b=g.Call(typeof(Vector2),"op_Multiply",null,new[]{typeof(Vector2),typeof(float)},Vector2.up,y).result;
                return g.Call(typeof(Vector2),"op_Addition",null,new[]{typeof(Vector2),typeof(Vector2)},a,b).result;
            }
            var p = g.Set(changed.ifTrue,typeof(RectTransform),"anchorMin",safe,Vector(g.Binary<ScalarDivide>(g.Get(typeof(Rect),"x",area),width),g.Binary<ScalarDivide>(g.Get(typeof(Rect),"y",area),height)));
            p = g.Set(p,typeof(RectTransform),"anchorMax",safe,Vector(g.Binary<ScalarDivide>(g.Get(typeof(Rect),"xMax",area),width),g.Binary<ScalarDivide>(g.Get(typeof(Rect),"yMax",area),height)));
            var factor = g.Call(typeof(Mathf),"Max",null,new[]{typeof(float),typeof(float)},1f,g.Binary<ScalarMultiply>(g.Binary<ScalarDivide>(height,width),9f/16f)).result;
            foreach (var camera in All<CinemachineCamera>().Where(c => c.name != "AD · mahalle açılış kadrajı"))
            {
                var set = g.Add(new SetMember(new Member(typeof(LensSettings),"FieldOfView")){chainable=true});
                var tangent=g.Binary<ScalarMultiply>(Mathf.Tan(camera.Lens.FieldOfView*.5f*Mathf.Deg2Rad),factor);
                var fov=g.Binary<ScalarMultiply>(g.Call(typeof(Mathf),"Atan",null,new[]{typeof(float)},tangent).result,2f*Mathf.Rad2Deg);
                g.Bind(set.target,g.Get(typeof(CinemachineCamera),"Lens",camera)); g.Bind(set.input,fov); g.Link(p,set.assign);
                p=g.Set(set.assigned,typeof(CinemachineCamera),"Lens",camera,set.targetOutput);
            }
            p=g.SetVar(p,"Width",width);g.SetVar(p,"Height",height);g.Dirty();
        }
        static void MoveMapIntoRoom()
        {
            var board=All<Transform>().First(t=>t.name=="Ailecek denenen resimli mahalle planı");
            var token=All<Transform>().First(t=>t.name=="Haritadaki aile taşı");
            var anchor=All<Transform>().First(t=>t.name=="Anchor_FamilyMap");
            var old=board.position;var delta=anchor.position+new Vector3(-.78f,-.30f,-.40f)-old;
            if(delta.sqrMagnitude>.000001f)
            {
                board.position+=delta;token.position+=delta;
                All<CinemachineCamera>().First(c=>c.name=="Yakından incele · map").transform.position+=delta;
                object Shift(object value,bool tokenGraph)
                {
                    if(value is Vector3 v&&Mathf.Abs(v.y-(old.y+.065f))<.0001f&&v.x>=old.x-.001f&&v.x<=old.x+.45f&&v.z>=old.z-.001f&&v.z<=old.z+.67f)return v+delta;
                    if(tokenGraph&&value is float f&&Mathf.Abs(f-old.x)<.0001f)return f+delta.x;
                    return value;
                }
                foreach(var machine in All<ScriptMachine>().Where(m=>m.gameObject==token.gameObject||m.graph.title=="Kesintisiz macera · gerçek nesne durumları"))
                {
                    foreach(var unit in machine.graph.units)
                    {
                        bool tokenGraph=machine.gameObject==token.gameObject;
                        if(unit is Literal literal)literal.value=Shift(literal.value,tokenGraph);
                        foreach(var key in unit.defaultValues.Keys.ToArray())unit.defaultValues[key]=Shift(unit.defaultValues[key],tokenGraph);
                    }
                    EditorUtility.SetDirty(machine);
                }
            }
            var approach=All<Transform>().First(t=>t.name=="Approach_FamilyMap").position;
            foreach(var machine in All<ScriptMachine>().Where(m=>m.graph.title=="Yaklaşınca map nesnesini incele"))
            {
                object StepAside(object value)=>value is Vector3 v&&(v-approach).sqrMagnitude<.000001f?v+Vector3.left*.84f:value;
                foreach(var unit in machine.graph.units)
                {
                    if(unit is Literal literal)literal.value=StepAside(literal.value);
                    foreach(var key in unit.defaultValues.Keys.ToArray())unit.defaultValues[key]=StepAside(unit.defaultValues[key]);
                }
                EditorUtility.SetDirty(machine);
            }
            foreach(var text in board.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t=>t.text=="EV"))text.fontSize=.8f;
            var owner=All<Transform>().First(t=>t.name.StartsWith("01 Akış")).gameObject;
            const string label="İncelemede büyük yaklaşma kutusu küçük nesneleri örtmesin";
            foreach(var oldMachine in owner.GetComponents<ScriptMachine>().Where(m=>m.graph.title==label).ToArray())UnityEngine.Object.DestroyImmediate(oldMachine);
            var g=new YanYanaGraphAuthor(owner,label);var tick=g.Add(new Unity.VisualScripting.Update());var p=tick.trigger;
            foreach(var machine in All<ScriptMachine>().Where(m=>m.graph.title.StartsWith("Yaklaşınca ")))
            {var box=machine.GetComponent<BoxCollider>();if(box)p=g.Set(p,typeof(Collider),"enabled",box,g.Binary<Equal>(g.Var("Workspace",owner),""));}
            g.Dirty();
        }
        static void AuthorTransitionInput()
        {
            var safe=All<RectTransform>().First(t=>t.name=="Güvenli ekran alanı");
            var previous=safe.Find("Kamera geçişinde dokunmayı beklet");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var go=new GameObject("Kamera geçişinde dokunmayı beklet",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            var rt=(RectTransform)go.transform;rt.SetParent(safe,false);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=Color.clear;image.raycastTarget=false;
            var g=new YanYanaGraphAuthor(go,"Geçiş boyunca bekleyen dokunma nesneye gitmesin");var tick=g.Add(new Unity.VisualScripting.Update());
            g.Set(tick.trigger,typeof(UnityEngine.UI.Graphic),"raycastTarget",image,g.Get(typeof(CinemachineBrain),"IsBlending",Camera.main.GetComponent<CinemachineBrain>()));g.Dirty();
        }
        static Material EffectMaterial(string name, int kind, Color color)
        {
            string path="Assets/YanYana/Art/Materials/"+name+".mat";
            var shader=Shader.Find("Deprem/Adventure Particles");if(!shader)throw new InvalidOperationException("Import the authored particle shader first.");
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}
            m.shader=shader;m.SetColor("_BaseColor",color);m.SetFloat("_Kind",kind);m.SetFloat("_SoftDistance",kind==3?.025f:.12f);EditorUtility.SetDirty(m);return m;
        }
        static Gradient Fade(Color a, Color b, float opacity)
        {
            var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(a,0),new GradientColorKey(b,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(opacity,.18f),new GradientAlphaKey(opacity*.7f,.55f),new GradientAlphaKey(0,1)});return g;
        }
        static void StyleEffects()
        {
            var flame=EffectMaterial("AdventureFlame",0,Color.white);
            var smoke=EffectMaterial("AdventureSmoke",1,Color.white);
            var dust=EffectMaterial("AdventureDust",2,Color.white);
            var water=EffectMaterial("AdventureWater",3,new Color(.68f,.86f,.91f,.85f));
            var wet=EffectMaterial("AdventureWetGround",4,new Color(.19f,.25f,.23f,.38f));
            foreach(var ps in All<ParticleSystem>())
            {
                bool isFlame=ps.name=="Hareketli sıcak alev",isSmoke=ps.name=="Yükselen duman",isDust=ps.name=="Sarsıntıda kısa stilize toz",isSplash=ps.name=="Su çarpma zerrecikleri";
                if(!isFlame&&!isSmoke&&!isDust&&!isSplash)continue;
                var m=ps.main;var render=ps.GetComponent<ParticleSystemRenderer>();
                render.shadowCastingMode=ShadowCastingMode.Off;render.receiveShadows=false;render.sortMode=ParticleSystemSortMode.Distance;
                render.sharedMaterial=isFlame?flame:isSmoke?smoke:dust;
                m.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;m.maxParticles=isSmoke?36:isDust?65:90;
                m.startRotation=new ParticleSystem.MinMaxCurve(-.45f,.45f);
                var color=ps.colorOverLifetime;color.enabled=true;
                var size=ps.sizeOverLifetime;size.enabled=true;
                if(isFlame)
                {
                    m.prewarm=true;
                    m.startLifetime=new ParticleSystem.MinMaxCurve(.55f,.95f);m.startSpeed=new ParticleSystem.MinMaxCurve(.4f,.8f);m.startSize3D=true;
                    m.startSizeX=new ParticleSystem.MinMaxCurve(.22f,.36f);m.startSizeY=new ParticleSystem.MinMaxCurve(.40f,.72f);m.startSizeZ=.25f;m.startColor=Color.white;
                    var emission=ps.emission;emission.rateOverTime=24;var shape=ps.shape;shape.radius=.19f;shape.angle=8;
                    color.color=Fade(Color.white,new Color(1,.70f,.44f),.9f);
                    size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.45f),new Keyframe(.25f,1),new Keyframe(1,.12f)));
                }
                if(isSmoke)
                {
                    m.prewarm=true;
                    m.startLifetime=new ParticleSystem.MinMaxCurve(2.1f,3.2f);m.startSpeed=new ParticleSystem.MinMaxCurve(.25f,.5f);m.startSize=new ParticleSystem.MinMaxCurve(.34f,.6f);m.startColor=Color.white;
                    var emission=ps.emission;emission.rateOverTime=8;
                    color.color=Fade(new Color(.24f,.27f,.26f),new Color(.53f,.53f,.47f),.5f);
                    size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.6f),new Keyframe(1,2.0f)));
                    var noise=ps.noise;noise.enabled=true;noise.strength=.14f;noise.frequency=.45f;noise.scrollSpeed=.18f;noise.quality=ParticleSystemNoiseQuality.Low;
                }
                if(isDust)
                {
                    m.startSize=new ParticleSystem.MinMaxCurve(.07f,.21f);m.startLifetime=new ParticleSystem.MinMaxCurve(1.4f,2.6f);m.startSpeed=new ParticleSystem.MinMaxCurve(.08f,.30f);m.startColor=Color.white;
                    var emission=ps.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,38),new ParticleSystem.Burst(.55f,16)});
                    color.color=Fade(new Color(.69f,.59f,.45f),new Color(.78f,.71f,.58f),.36f);size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.35f,1,1.4f));
                }
                if(isSplash)
                {
                    m.startLifetime=new ParticleSystem.MinMaxCurve(.15f,.4f);m.startSpeed=new ParticleSystem.MinMaxCurve(.45f,1.1f);m.startSize=new ParticleSystem.MinMaxCurve(.018f,.044f);m.startColor=Color.white;m.gravityModifier=.5f;
                    var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.075f;shape.rotation=new Vector3(-90,0,0);
                    color.color=Fade(new Color(.87f,.98f,1f),new Color(.62f,.83f,.86f),.9f);size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.25f));
                }
            }
            foreach(var line in All<LineRenderer>().Where(r=>r.name=="Kesintisiz su akışı"))
            {line.sharedMaterial=water;line.startWidth=.032f;line.endWidth=.072f;line.numCornerVertices=8;line.numCapVertices=8;line.textureMode=LineTextureMode.Stretch;line.colorGradient=new Gradient();}
            foreach(var r in All<MeshRenderer>().Where(r=>r.name.StartsWith("Söndürülmüş ıslak alan")))
            {r.sharedMaterial=wet;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
            foreach(var t in All<Transform>().Where(t=>t.name=="FlameTuft"))t.localScale=Vector3.one*.50f;
            foreach(var volume in All<Volume>())
            {
                var profile=volume.sharedProfile;if(!profile||!AssetDatabase.GetAssetPath(profile).StartsWith("Assets/YanYana/"))continue;
                if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
                bloom.threshold.Override(1.25f);bloom.intensity.Override(.12f);bloom.scatter.Override(.55f);EditorUtility.SetDirty(profile);
            }
        }
        static void AuthorFireContinuity()
        {
            var owner=All<Transform>().First(t=>t.name.StartsWith("01 Akış")).gameObject;
            var world=All<Transform>().First(t=>t.name.StartsWith("02 Dünya"));
            const string label="Yangın kadrajı değişirken alevleri kesintisiz göster";
            foreach(var old in owner.GetComponents<ScriptMachine>().Where(m=>m.graph.title==label).ToArray())UnityEngine.Object.DestroyImmediate(old);
            var g=new YanYanaGraphAuthor(owner,label);var frame=g.Add(new Unity.VisualScripting.LateUpdate());var path=frame.trigger;
            for(int stage=1;stage<=3;stage++)
            {
                var preview=All<Transform>().First(t=>t.name=="Ada’nın uzaktan gördüğü yangın "+stage).gameObject;
                var root=All<Transform>().First(t=>t.name=="Gerçek müdahale alanı "+stage).gameObject;
                var waiting=g.Binary<And>(g.Binary<Equal>(g.Var("FireCompleted",owner),0),g.Binary<And>(g.Binary<LessOrEqual>(g.Var("FireStage",owner),stage),g.Binary<Equal>(g.Get(typeof(GameObject),"activeInHierarchy",root),false)));
                waiting=g.Binary<And>(waiting,g.Binary<GreaterOrEqual>(g.Var("Phase",owner),4));
                path=g.Active(path,preview,waiting);
            }
            g.Dirty();
            var material=EffectMaterial("AdventureSteam",1,new Color(.74f,.83f,.81f,.58f));
            foreach(var target in All<Transform>().Where(t=>t.name.StartsWith("Alev odağı ")).ToArray())
            {
                string name="Sönünce kalan buhar · "+target.name;
                var previous=All<Transform>().FirstOrDefault(t=>t.name==name);if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var go=new GameObject(name);go.transform.SetParent(world,false);go.transform.position=target.position+Vector3.up*.16f;
                var ps=go.AddComponent<ParticleSystem>();var main=ps.main;main.loop=false;main.duration=.8f;main.playOnAwake=false;main.startLifetime=new ParticleSystem.MinMaxCurve(.65f,1.35f);main.startSpeed=new ParticleSystem.MinMaxCurve(.3f,.6f);main.startSize=new ParticleSystem.MinMaxCurve(.13f,.29f);main.startColor=Color.white;main.maxParticles=24;
                var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,16)});
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=16;shape.radius=.20f;shape.rotation=new Vector3(-90,0,0);
                var color=ps.colorOverLifetime;color.enabled=true;color.color=Fade(Color.white,Color.white,.58f);
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,2.1f));
                var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sortMode=ParticleSystemSortMode.Distance;
                var collider=target.GetComponent<Collider>();var graph=new YanYanaGraphAuthor(go,"Son su darbesinden sonra kısa buhar");graph.Initial("WasLit",false);
                var update=graph.Add(new Unity.VisualScripting.LateUpdate());var enabled=graph.Get(typeof(Collider),"enabled",collider);
                var extinguished=graph.Branch(update.trigger,graph.Binary<And>(graph.Var("WasLit"),graph.Binary<Equal>(enabled,false)));
                var played=graph.Do(extinguished.ifTrue,typeof(ParticleSystem),"Play",ps,new[]{typeof(bool)},false);
                var lit=graph.Binary<And>(enabled,graph.Get(typeof(GameObject),"activeInHierarchy",target.gameObject));
                graph.SetVar(played,"WasLit",lit);graph.SetVar(extinguished.ifFalse,"WasLit",lit);graph.Dirty();
            }
        }

        [MenuItem("Tools/Yan Yana/QA/Inspect Visual Stability")]
        public static void Inspect()
        {
            var rows = new List<string> { "Play=" + EditorApplication.isPlaying };
            foreach (var b in All<CinemachineBrain>()) rows.Add("BRAIN " + b.name + " update=" + b.UpdateMethod + " blend=" + b.DefaultBlend.Style + "/" + b.DefaultBlend.Time + " active=" + b.ActiveVirtualCamera?.Name);
            foreach (var c in All<CinemachineCamera>())
            {
                rows.Add("CAM " + c.name + " enabled=" + c.gameObject.activeSelf + " lens=" + c.Lens.ModeOverride + " size=" + c.Lens.OrthographicSize + " fov=" + c.Lens.FieldOfView + " near=" + c.Lens.NearClipPlane + " pos=" + Vec(c.transform.position) + " rotation=" + Vec(c.transform.eulerAngles));
                var f = c.GetComponent<CinemachineFollow>(); if (f) rows.Add("  FOLLOW " + PathOf(c.Follow) + " offset=" + Vec(f.FollowOffset) + " damp=" + Vec(f.TrackerSettings.PositionDamping));
            }
            var rr = All<MeshRenderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            var flats = rr.Where(r => r.bounds.size.y < .7f && r.bounds.size.x > .08f && r.bounds.size.z > .08f).ToArray();
            for (int i = 0; i < flats.Length; i++) for (int j = i + 1; j < flats.Length; j++)
            {
                var a = flats[i].bounds; var b = flats[j].bounds;
                float overlapX = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                float overlapZ = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                float gap = Mathf.Abs(a.max.y - b.max.y);
                if (gap < .006f && overlapX > .025f && overlapZ > .025f)
                    rows.Add("SURFACE gap=" + gap.ToString("F6", CultureInfo.InvariantCulture) + " area=" + (overlapX * overlapZ).ToString("F4", CultureInfo.InvariantCulture) + " y=" + a.max.y.ToString("F4", CultureInfo.InvariantCulture) + " | " + PathOf(flats[i].transform) + " | " + PathOf(flats[j].transform));
            }
            foreach (var p in All<ParticleSystem>()) rows.Add("FX " + PathOf(p.transform) + " active=" + p.gameObject.activeInHierarchy + " material=" + p.GetComponent<ParticleSystemRenderer>().sharedMaterial?.name + " size=" + p.main.startSize.constantMax + " duration=" + p.main.duration);
            File.WriteAllLines(Reports + "visual-stability-inspection.txt", rows);
        }
    }
}
