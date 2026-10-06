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
    public static partial class YanYanaFirePresentation
    {
        const string Art = "Assets/YanYana/Art/Materials/";
        const string GraphTitle = "Su ile azalan alev; duman ve ıslak iz kalır";
        static T[] All<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        static Transform Find(string name) => All<Transform>().First(t => t.name == name);

        [MenuItem("Tools/Yan Yana/QA/Fire Pose Review")]
        public static void ReviewPose()
        {
            var actor=Find("Idil"); var animator=actor.GetComponentInChildren<Animator>();
            var report=new List<string>();
            foreach(var bone in new[]{HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand})
                report.Add(bone+"="+actor.InverseTransformPoint(animator.GetBoneTransform(bone).position).ToString("F4"));
            foreach(string name in new[]{"İdil’in gerçek hortum başlığı","Hortum su çıkışı","Sol el hortum kavrama","Sağ el hortum kavrama"})
                report.Add(name+"="+actor.InverseTransformPoint(Find(name).position).ToString("F4"));
            string folder="ClientExports/YanYana/Screenshots/fire-pose-review";
            Directory.CreateDirectory(folder); Directory.CreateDirectory("ClientExports/YanYana/Reports");
            File.WriteAllLines("ClientExports/YanYana/Reports/fire-pose-review.txt",report);
            var go=new GameObject("Temporary editor review camera"); var camera=go.AddComponent<Camera>(); camera.CopyFrom(Camera.main); camera.enabled=false;
            var source=Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if(source){var data=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();EditorUtility.CopySerialized(source,data);}
            var texture=new Texture2D(960,960,TextureFormat.RGB24,false);
            var target=RenderTexture.GetTemporary(960,960,24); var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target; camera.aspect=1; camera.orthographic=false; camera.fieldOfView=39;
                for(int view=0;view<6;view++)
                {
                    if(view<3)
                    {
                        camera.transform.position=actor.TransformPoint(new[]{new Vector3(1.5f,.85f,1.5f),new Vector3(-1.5f,.85f,1.5f),new Vector3(1.6f,1.1f,-.7f)}[view]);
                        camera.transform.LookAt(actor.TransformPoint(new Vector3(0,.63f,.14f)));
                    }
                    else if(view==3)
                    {
                        var fire=Find("Alev odağı 1 1").position+Vector3.up*.3f;
                        camera.transform.position=fire+(Camera.main.transform.position-fire).normalized*2.5f;
                        camera.transform.LookAt(fire);
                    }
                    else
                    {
                        camera.fieldOfView=45;
                        camera.transform.position=actor.TransformPoint(view==4?new Vector3(.95f,.30f,1):new Vector3(1,.30f,-.45f));
                        camera.transform.LookAt(actor.TransformPoint(new Vector3(0,.17f,.08f)));
                    }
                    camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,960,960),0,0); texture.Apply();
                    File.WriteAllBytes(folder+"/grip-"+view+".png",texture.EncodeToPNG());
                }
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);}
        }

        [MenuItem("Tools/Yan Yana/Presentation/Polish Firefighting")]
        public static void ApplyAndSave()
        {
            Apply();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Fire presentation saved: layered flames, independent smoke, cooled targets and authored hose grip.");
        }

        [MenuItem("Tools/Yan Yana/Presentation/Keep Hose Outside Body")]
        public static void ApplyHoseClearance()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring hose grip.");
            Find("Sol el hortum kavrama").localPosition=new Vector3(0,0,.055f);
            Find("Sağ el hortum kavrama").localPosition=new Vector3(0,-.18f,.006f);
            ExtendPistolGrip();
            TaperForearmCloth();
            EnsureHoseControls();
            YanYanaAdventureBuilder.RefreshImmersiveFireInput();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Hose grip saved in front of the chest, with the supply hose routed around the outside of the right leg.");
        }
        public static void EnsureHoseControls()
        {
            var actor=Find("Idil");var nozzle=Find("İdil’in gerçek hortum başlığı");
            if(!actor.Find("Hortum başlığının gövde önündeki konumu"))Group("Hortum başlığının gövde önündeki konumu",actor,new Vector3(-.015f,-.02f,.35f));
            if(!nozzle.Find("Hortumun kol dışındaki kavis kontrolü"))Group("Hortumun kol dışındaki kavis kontrolü",nozzle,new Vector3(.30f,-.06f,-.12f));
            if(!actor.Find("Sağ hortum dirseğinin yönü"))Group("Sağ hortum dirseğinin yönü",actor,new Vector3(1.6f,-1,-.15f));
            if(!actor.Find("Sol hortum dirseğinin yönü"))Group("Sol hortum dirseğinin yönü",actor,new Vector3(-.65f,-1,-.15f));
            if(!nozzle.Find("Sol hortum avuç yönü"))Group("Sol hortum avuç yönü",nozzle,new Vector3(.819152f,.573576f,0));
        }
        static void TaperForearmCloth()
        {
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var cylinder=primitive.GetComponent<MeshFilter>().sharedMesh;
            try
            {
                var kinds=new[]{("FirefighterTaperedSleeve",0f,1f),("FirefighterTaperedSafetyStripe",.52f,.19f),("FirefighterTaperedReflectiveStripe",.52f,.07f)};
                var meshes=new List<Mesh>();
                foreach(var kind in kinds)
                {
                    var mesh=UnityEngine.Object.Instantiate(cylinder);mesh.name=kind.Item1;
                    var points=mesh.vertices;
                    for(int i=0;i<points.Length;i++)
                    {
                        float taper=Mathf.Lerp(1,.48f,Mathf.Clamp01((kind.Item2+points[i].y*kind.Item3+1)*.5f));
                        points[i].x*=taper;points[i].z*=taper;
                    }
                    mesh.vertices=points;mesh.RecalculateNormals();mesh.RecalculateBounds();
                    string path="Assets/YanYana/Art/Models/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
                    meshes.Add(saved);
                }
                foreach(var sleeve in Find("Idil").GetComponentsInChildren<MeshFilter>().Where(f=>f.name.Contains("ön kol kumaşı")))
                {
                    sleeve.sharedMesh=meshes[0];EditorUtility.SetDirty(sleeve);
                    foreach(var stripe in sleeve.GetComponentsInChildren<MeshFilter>().Where(f=>f!=sleeve))
                    {stripe.sharedMesh=meshes[stripe.name.Contains("sarı")?1:2];EditorUtility.SetDirty(stripe);}
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(primitive);}
        }
        static void ExtendPistolGrip()
        {
            var nozzle=Find("İdil’in gerçek hortum başlığı");var model=nozzle.Find("HoseNozzle");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/HoseNozzle.fbx");
            var originals=source.GetComponentsInChildren<MeshFilter>().ToDictionary(f=>f.name,f=>f.sharedMesh);
            int count=0;
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>().Where(f=>new[]{"RearHandGrip","GripEnd","TriggerGuard","Trigger"}.Contains(f.name)))
            {
                var mesh=UnityEngine.Object.Instantiate(originals[filter.name]);mesh.name="FirefighterExtended"+filter.name;
                var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++)
                {
                    var local=nozzle.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                    if(local.y<-.025f)local.y=-.025f+(local.y+.025f)*1.65f;
                    vertices[i]=filter.transform.InverseTransformPoint(nozzle.TransformPoint(local));
                }
                mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();
                string path="Assets/YanYana/Art/Models/"+mesh.name+".asset";var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(asset){EditorUtility.CopySerialized(mesh,asset);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);asset=mesh;}
                filter.sharedMesh=asset;EditorUtility.SetDirty(filter);count++;
            }
            if(count!=4)throw new InvalidOperationException("Expected all four pistol-grip mesh parts, found "+count);
        }

        [MenuItem("Tools/Yan Yana/Presentation/Pressure Hose Water")]
        public static void ApplyPressureWater()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring hose water.");
            var water=Material("FirefighterPressureWater","Deprem/Firefighter Water",Color.white);
            var droplets=Material("FirefighterDroplets","Deprem/Adventure Particles",new Color(.87f,.96f,1f,.96f),2);
            var mist=Material("FirefighterSprayMist","Deprem/Adventure Particles",new Color(.87f,.96f,1f,.45f),1);
            foreach(var manager in All<FirefighterExtinguishManager>().Where(m=>m.name.StartsWith("Gerçek müdahale alanı ")))
            {
                var data=new SerializedObject(manager);
                ConfigureWater((ParticleSystem)data.FindProperty("waterImpact").objectReferenceValue,droplets,mist);
                ConfigurePressureStream((LineRenderer)data.FindProperty("waterStream").objectReferenceValue,water);
            }
            YanYanaAdventureBuilder.RefreshImmersiveFireInput();
            YanYanaAdventureBuilder.RefreshPresentationPause();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Pressure hose water saved: moving foam, native high-speed droplets and impact mist.");
        }
        static void ConfigurePressureStream(LineRenderer stream,Material water)
        {
            stream.sharedMaterial=water; stream.textureMode=LineTextureMode.Stretch;
            stream.alignment=LineAlignment.View; stream.numCapVertices=0; stream.numCornerVertices=8;
            stream.widthMultiplier=1;
            stream.widthCurve=new AnimationCurve(new Keyframe(0,.034f),new Keyframe(.18f,.035f),new Keyframe(.68f,.046f),new Keyframe(1,.075f));
            stream.startColor=stream.endColor=Color.white;
        }
        [MenuItem("Tools/Yan Yana/QA/Hose Arm Clearance")]
        public static void ReviewHoseClearance()
        {
            var value=CheckHoseBodyClearance();ReviewPose();
            var variables=Variables.Object(All<Transform>().First(t=>t.name.StartsWith("01 Akış")).gameObject);
            File.WriteAllText("ClientExports/YanYana/Reports/hose-arm-clearance.txt",value+" leftGrip="+variables.Get("IdilLeftGripError")+" rightGrip="+variables.Get("IdilRightGripError"));
        }
        public static (float nozzle,float hose,float armNozzle,float armHose) CheckHoseBodyClearance()
        {
            var actor=Find("Idil");var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
            var vertices=YanYanaEditableCharacterExport.EvaluateSurface(skin);
            var triangles=skin.sharedMesh.triangles;
            var hands=new HashSet<int>(YanYanaInteractionHands.Read("Idil").hands.SelectMany(h=>h.indices));
            var nozzle=Find("İdil’in gerçek hortum başlığı");
            var hose=All<LineRenderer>().First(l=>l.name=="Zeminden gelen besleme hortumu"&&l.gameObject.activeInHierarchy);
            var clothingTriangles=new List<int>();
            for(int i=0;i<triangles.Length;i+=3)
                if(!hands.Contains(triangles[i])&&!hands.Contains(triangles[i+1])&&!hands.Contains(triangles[i+2]))
                    clothingTriangles.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
            var cloth=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name.EndsWith("kesintisiz kol kumaşı"))
                .Select(r=>(name:r.name,points:YanYanaEditableCharacterExport.EvaluateSurface(r),triangles:r.sharedMesh.triangles,closed:true))
                .Append((name:"İdil’in özgün omuzları ve üst kolları",points:vertices,triangles:clothingTriangles.ToArray(),closed:false)).ToArray();
            var armDiagnostics=new List<string>();
            float ArmTube(Vector3 a,Vector3 b,float radius)
            {
                float best=float.PositiveInfinity;string closest="";
                foreach(var part in cloth)
                {
                    // The original uniform has open wrist boundaries after its forearms are replaced.
                    // Ray parity is a volume test only for closed surfaces; every surface still gets triangle clearance checks.
                    for(int sample=0;part.closed&&sample<=8;sample++)
                    {
                        if(ConnectedClothContains(Vector3.Lerp(a,b,sample/8f),part.points,part.triangles))
                        {armDiagnostics.Add("tube "+armDiagnostics.Count+" inside="+part.name+" clearance="+(-radius));return -radius;}
                    }
                    for(int i=0;i<part.triangles.Length;i+=3)
                    {
                        float distance=SegmentTriangleDistance(a,b,part.points[part.triangles[i]],part.points[part.triangles[i+1]],part.points[part.triangles[i+2]]);
                        if(distance<best){best=distance;closest=part.name;}
                    }
                }
                armDiagnostics.Add("tube "+armDiagnostics.Count+" closest="+closest+" clearance="+(best-radius));
                return best-radius;
            }
            float Tube(Vector3 a,Vector3 b,float radius)
            {
                float best=float.PositiveInfinity;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    int x=triangles[i],y=triangles[i+1],z=triangles[i+2];
                    if(hands.Contains(x)||hands.Contains(y)||hands.Contains(z))continue;
                    best=Mathf.Min(best,SegmentTriangleDistance(a,b,vertices[x],vertices[y],vertices[z]));
                }
                return best-radius;
            }
            float nozzleClearance=Tube(nozzle.TransformPoint(new Vector3(0,0,-.058f)),nozzle.TransformPoint(new Vector3(0,0,.30f)),.058f);
            nozzleClearance=Mathf.Min(nozzleClearance,Tube(nozzle.TransformPoint(new Vector3(0,-.03f,.026f)),nozzle.TransformPoint(new Vector3(0,-.247f,0)),.025f));
            float armNozzle=ArmTube(nozzle.TransformPoint(new Vector3(0,0,-.058f)),nozzle.TransformPoint(new Vector3(0,0,.30f)),.058f);
            armNozzle=Mathf.Min(armNozzle,ArmTube(nozzle.TransformPoint(new Vector3(0,-.03f,.026f)),nozzle.TransformPoint(new Vector3(0,-.247f,0)),.025f));
            var bail=new[]{new Vector3(-.053f,0,.0504f),new Vector3(-.065f,.10f,.0432f),new Vector3(0,.125f,.0432f),new Vector3(.065f,.10f,.0432f),new Vector3(.053f,0,.0504f)};
            for(int i=1;i<bail.Length;i++)armNozzle=Mathf.Min(armNozzle,ArmTube(nozzle.TransformPoint(bail[i-1]),nozzle.TransformPoint(bail[i]),.010f));
            float hoseClearance=float.PositiveInfinity,armHose=float.PositiveInfinity;
            for(int i=1;i<hose.positionCount;i++)
            {
                hoseClearance=Mathf.Min(hoseClearance,Tube(hose.GetPosition(i-1),hose.GetPosition(i),.023f));
                armHose=Mathf.Min(armHose,ArmTube(hose.GetPosition(i-1),hose.GetPosition(i),.023f));
            }
            File.WriteAllLines("ClientExports/YanYana/Reports/hose-arm-parts.txt",armDiagnostics);
            return(nozzleClearance,hoseClearance,armNozzle,armHose);
        }
        static float SegmentTriangleDistance(Vector3 p,Vector3 q,Vector3 a,Vector3 b,Vector3 c)
        {
            var direction=q-p;var e1=b-a;var e2=c-a;var h=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,h);
            if(Mathf.Abs(det)>1e-9f)
            {
                float inverse=1/det;var s=p-a;float u=Vector3.Dot(s,h)*inverse;var cross=Vector3.Cross(s,e1);
                float v=Vector3.Dot(direction,cross)*inverse;float t=Vector3.Dot(e2,cross)*inverse;
                if(u>=0&&v>=0&&u+v<=1&&t>=0&&t<=1)return 0;
            }
            return Mathf.Min(Vector3.Distance(p,ClosestTrianglePoint(p,a,b,c)),Vector3.Distance(q,ClosestTrianglePoint(q,a,b,c)),
                SegmentDistance(p,q,a,b),SegmentDistance(p,q,b,c),SegmentDistance(p,q,c,a));
        }
        static float SegmentDistance(Vector3 p,Vector3 q,Vector3 a,Vector3 b)
        {
            var u=q-p;var v=b-a;var w=p-a;float uu=Vector3.Dot(u,u),vv=Vector3.Dot(v,v),uv=Vector3.Dot(u,v),uw=Vector3.Dot(u,w),vw=Vector3.Dot(v,w);
            float denominator=uu*vv-uv*uv;
            float s=denominator>1e-10f?Mathf.Clamp01((uv*vw-vv*uw)/denominator):0;
            float t=vv>1e-10f?(uv*s+vw)/vv:0;
            if(t<0){t=0;s=uu>1e-10f?Mathf.Clamp01(-uw/uu):0;}
            else if(t>1){t=1;s=uu>1e-10f?Mathf.Clamp01((uv-uw)/uu):0;}
            return Vector3.Distance(p+u*s,a+v*t);
        }
        static Vector3 ClosestTrianglePoint(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
            if(d1<=0&&d2<=0)return a;
            var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
            float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
            var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
            float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
            float va=d3*d6-d5*d4;if(va<=0&&d4-d3>=0&&d5-d6>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
            float sum=va+vb+vc;if(Mathf.Abs(sum)<1e-12f)return a;
            return a+ab*(vb/sum)+ac*(vc/sum);
        }
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring fire presentation.");
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Any(r => r.name.StartsWith("01 Akış")))
                throw new InvalidOperationException("Open YanYana_Adventure before authoring.");
            var flame = Material("FirefighterLivingFlame", "Deprem/Firefighter Flame", Color.white);
            var smoke = Material("FirefighterSmoke", "Deprem/Adventure Particles", new Color(.76f,.78f,.76f,.64f), 1);
            var droplets = Material("FirefighterDroplets", "Deprem/Adventure Particles", new Color(.87f,.96f,1f,.96f), 2);
            var pressureWater = Material("FirefighterPressureWater", "Deprem/Firefighter Water", Color.white);
            var sprayMist = Material("FirefighterSprayMist", "Deprem/Adventure Particles", new Color(.87f,.96f,1f,.45f), 1);
            var scorch = Material("FirefighterScorch", "Deprem/Adventure Particles", new Color(.15f,.105f,.075f,.63f), 4);
            var wet = Material("FirefighterWetGround", "Deprem/Adventure Particles", new Color(.19f,.28f,.29f,.52f), 4);
            var timber = Material("FirefighterCharredWood", "Universal Render Pipeline/Lit", new Color(.24f,.15f,.09f));
            var ash = Material("FirefighterAsh", "Universal Render Pipeline/Lit", new Color(.14f,.15f,.145f));
            var metal = Material("FirefighterBurnedMetal", "Universal Render Pipeline/Lit", new Color(.25f,.29f,.285f));
            var rubber = Material("FirefighterSupplyHose", "Deprem/Firefighter Hose", new Color(.095f,.13f,.13f));
            foreach (var material in new[] { timber, ash, metal }) material.SetFloat("_Smoothness", .12f);
            var owner = All<Transform>().First(t => t.name.StartsWith("01 Akış")).gameObject;
            Find("Sol el hortum kavrama").localPosition = new Vector3(0,0,.055f);
            Find("Sağ el hortum kavrama").localPosition = new Vector3(0,-.18f,.006f);
            Find("Hortum su çıkışı").localPosition = new Vector3(0,0,.30f);
            Find("İdil’in gerçek hortum başlığı").Find("HoseNozzle").localScale = new Vector3(1,.72f,1);
            AuthorUniformArms();
            ExtendPistolGrip();EnsureHoseControls();

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
                ConfigureWater((ParticleSystem)data.FindProperty("waterImpact").objectReferenceValue, droplets, sprayMist);
                var stream = (LineRenderer)data.FindProperty("waterStream").objectReferenceValue;
                var hose = (LineRenderer)data.FindProperty("supplyHose").objectReferenceValue;
                hose.sharedMaterial=rubber; hose.startWidth=hose.endWidth=.046f; hose.numCapVertices=10; hose.numCornerVertices=12;
                hose.alignment=LineAlignment.View; hose.textureMode=LineTextureMode.Tile;
                hose.startColor=hose.endColor=Color.white;
                ConfigurePressureStream(stream,pressureWater);
                data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(manager);
            }
            foreach (var preview in All<Transform>().Where(t => t.name == "Bekleyen yangın odağı").ToArray())
            {
                foreach (var child in preview.Cast<Transform>().Where(t => t.name != "Yangın sunumu").ToArray()) child.gameObject.SetActive(false);
                var effects=BuildTarget(preview, preview.GetSiblingIndex(), flame, smoke, scorch, wet, timber, ash, metal);
                effects.flames.localScale=new Vector3(.82f,.70f,.82f); effects.light.intensity=.18f;
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
        static void AuthorUniformArms()
        {
            AuthorBoundUniformSurface();
            EnsureHoseControls();BakeHosePreview();
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
            lamp.type = LightType.Point; lamp.color = new Color(1,.36f,.07f); lamp.range=.95f; lamp.intensity=.34f; lamp.shadows=LightShadows.None;
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
            main.startLifetime=new ParticleSystem.MinMaxCurve(core?.38f:.55f,core?.60f:.80f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.05f,core?.10f:.20f); main.maxParticles=core?5:9;
            main.startSize3D=true;
            main.startSizeX=new ParticleSystem.MinMaxCurve(core?.20f:.35f,core?.29f:.49f);
            main.startSizeY=new ParticleSystem.MinMaxCurve(core?.28f:.55f,core?.40f:.78f);
            main.startSizeZ=.2f; main.startRotation=new ParticleSystem.MinMaxCurve(-.12f,.12f);
            var emission=ps.emission; emission.rateOverTime=core?5:9;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.radius=core?.09f:.15f; shape.angle=8; shape.rotation=new Vector3(-90,0,0);
            var color=ps.colorOverLifetime; color.enabled=true; color.color=Fade(Color.white,Color.white,core?.92f:.96f);
            var size=ps.sizeOverLifetime; size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.78f),new Keyframe(.20f,1),new Keyframe(.68f,.86f),new Keyframe(1,.35f)));
            var noise=ps.noise; noise.enabled=true; noise.strength=.027f; noise.frequency=1.8f; noise.scrollSpeed=.6f; noise.quality=ParticleSystemNoiseQuality.Low;
            var renderer=ps.GetComponent<ParticleSystemRenderer>(); renderer.renderMode=ParticleSystemRenderMode.VerticalBillboard;
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
        static void ConfigureWater(ParticleSystem ps, Material material, Material mistMaterial)
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
            main=jet.main; main.playOnAwake=false; main.prewarm=false; main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=1; main.startSpeed=20; main.startSize=new ParticleSystem.MinMaxCurve(.008f,.017f);
            main.gravityModifier=0; main.maxParticles=100;
            emission=jet.emission; emission.rateOverTime=480;
            shape=jet.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.radius=.013f; shape.angle=1.4f; shape.length=0;
            color=jet.colorOverLifetime; color.enabled=true; color.color=Fade(Color.white,new Color(.8f,.94f,1),.85f);
            size=jet.sizeOverLifetime; size.enabled=true; size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.75f,1,1.2f));
            renderer=jet.GetComponent<ParticleSystemRenderer>(); renderer.renderMode=ParticleSystemRenderMode.Stretch;
            renderer.velocityScale=.003f; renderer.lengthScale=1.4f;

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
            p=graph.Set(p,typeof(Light),"intensity",lamp,graph.Binary<ScalarMultiply>(health,graph.Binary<ScalarSubtract>(.34f,graph.Binary<ScalarMultiply>(pulse,-.055f))));
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
                foreach(float x in new[]{-.025f,0,.025f}) foreach(float y in new[]{-.02f,-.04f,-.06f}) foreach(float z in new[]{.14f,.16f,.18f})
                {
                    float max=0,maxBend=0;
                    foreach(float yaw in new[]{-35f,0,35f}) foreach(float pitch in new[]{5f,20f,35f})
                    {
                        animator.Rebind(); animator.Update(.2f);
                        var origin=(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position+animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position)*.5f+new Vector3(x,y,z);
                        var rotation=Quaternion.Euler(pitch,yaw,0);
                        foreach(bool right in new[]{true,false})
                        {
                            var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
                            var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
                            var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                            var definition=definitions.hands.Single(h=>h.right==right);
                            var pose=definition.poses.Single(p=>p.name==(right?"Right_Loop":"Left_Cylinder"));
                            var contact=origin+rotation*(right?new Vector3(0,-.082f,.0166f):new Vector3(0,0,.13f));
                            var handleAxis=rotation*(right?Vector3.up:Vector3.forward);
                            var palmNormal=rotation*(right?-Vector3.right:Vector3.up);
                            var palmRotation=Quaternion.LookRotation(handleAxis,palmNormal)*Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(definition.forward,definition.normal).normalized,definition.normal));
                            hand.rotation=palmRotation;
                            var goal=contact-hand.TransformVector(pose.center);
                            solve.Invoke(null,new object[]{upper,lower,hand,goal,new Vector3(right?.45f:-.45f,-1,-.15f)});
                            hand.rotation=palmRotation;
                            max=Mathf.Max(max,Vector3.Distance(hand.TransformPoint(pose.center),contact));
                            maxBend=Mathf.Max(maxBend,Vector3.Angle(lower.position-upper.position,hand.position-lower.position));
                        }
                    }
                    results.Add((max+Mathf.Max(0,maxBend-125)*.001f,$"{x:F3},{y:F2},{z:F2},{max:F5},{maxBend:F1}"));
                }
                Directory.CreateDirectory("ClientExports/YanYana/Reports");
                File.WriteAllLines("ClientExports/YanYana/Reports/fire-grip-calibration.csv",new[]{"shoulderOffsetX,shoulderOffsetY,shoulderOffsetZ,maxContactErrorMetres,maxElbowBendDegrees"}.Concat(results.OrderBy(r=>r.score).Select(r=>r.row)));
                UnityEngine.Object.DestroyImmediate(actor);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
