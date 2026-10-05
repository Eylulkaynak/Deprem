// Editor authoring for the continuous, physical adventure. No player C# is added.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static readonly Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();
        static readonly Dictionary<string, GameObject> physicalItems = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, CinemachineCamera> workCameras = new Dictionary<string, CinemachineCamera>();
        static readonly List<string> physicalKeys = new List<string>();
        static CinemachineCamera exploreCamera;
        static GameObject roomModel;
        static GameObject activityBack, rotateControl;
        static GameObject physicalBag, physicalFlashlight, physicalRadio;
        static Transform selectedHandTarget;

        [MenuItem("Tools/Yan Yana/Build Adventure")]
        public static void BuildPhysicalAdventure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave play mode before authoring.");
            var previous = SceneManager.GetActiveScene();
            if (previous.isDirty && previous.path != ScenePath && !previous.GetRootGameObjects().Any(x => x.name.StartsWith("01 Akış"))) throw new InvalidOperationException("Save the current scene before building Yan Yana.");
            Directory.CreateDirectory("ClientExports/YanYana/Reports");
            try
            {
                campaign = JsonUtility.FromJson<YYCampaign>(File.ReadAllText(Root + "Content/Campaign.json"));
                mats.Clear(); cast.Clear(); movers.Clear(); stations.Clear(); controllers.Clear(); beatWorlds.Clear(); views.Clear(); targets.Clear(); portraits.Clear(); fireManagers.Clear(); sounds.Clear();
                anchors.Clear(); physicalItems.Clear(); workCameras.Clear(); physicalKeys.Clear();
                ImportArt(); CreateOwnFont();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                flow = new GameObject("01 Akış — fiziksel macera"); world = new GameObject("02 Dünya — ölçülü ve kesintisiz");
                castRoot = new GameObject("03 Karakterler — onaylı görsel dil"); interactions = new GameObject("04 Etkileşimler — nesnelerin gerçek davranışları");
                presentation = new GameObject("05 Sunum — takip ve nesne kameraları"); uiRoot = new GameObject("06 Arayüz ve kayıt");
                CreatePresentation();
                CreatePhysicalApartment();
                CreateNeighborhoodWorld();
                CreateApprovedCast();
                CreatePhysicalCameras();
                CreateUI();
                CreatePhysicalState();
                CreateInteractionHandPresentation();
                CreatePhysicalSpeech();
                CreatePhysicalNavigation();
                CreateFlashlightAssembly();
                CreatePackingPuzzle();
                CreateBackpackTrial();
                CreateRadioTuning();
                CreateSupplyInspection();
                CreateFamilyMapPuzzle();
                CreatePhysicalHomeSafety();
                CreateHomeQuake();
                CreateEvacuation();
                CreatePhysicalFire();
                CreatePhysicalAid();
                CreateNeighborhoodResidents();
                CreatePhysicalFinals();
                CreatePhysicalCarry();
                CreatePhysicalIntro();
                CreatePhysicalPolish();
                CreatePhysicalMenusAndSave();
                YanYanaInterfaceArtDirection.Apply();
                YanYanaVisualStability.Apply();
                BakeNavigation();
                main.Dirty();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath); AssetDatabase.SaveAssets();
                var authoredMachines=UnityEngine.Object.FindObjectsByType<ScriptMachine>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                File.WriteAllText("ClientExports/YanYana/Reports/scene-inventory.json","{\n  \"scene\": \""+ScenePath+"\",\n  \"version\": \"physical-adventure\",\n  \"chapters\": 8,\n  \"endings\": 4,\n  \"scriptMachines\": "+authoredMachines.Length+",\n  \"graphUnits\": "+authoredMachines.Sum(x=>x.graph?.units.Count??0)+",\n  \"newAuthoredRuntimeCSharp\": 0\n}");
                Selection.activeGameObject = null;
                File.WriteAllText("ClientExports/YanYana/Reports/physical-authoring.txt", "Physical scene authored: preparation, quake, evacuation, firefighter, aid and four reunions. Gameplay, timing, save/resume and device validation remain separate checks.");
                string errorPath="ClientExports/YanYana/Reports/physical-authoring-error.txt";if(File.Exists(errorPath))File.Delete(errorPath);
                Debug.Log("YAN YANA PHYSICAL ADVENTURE AUTHORED — gameplay QA pending");
            }
            catch (Exception ex) { File.WriteAllText("ClientExports/YanYana/Reports/physical-authoring-error.txt", ex.ToString()); Debug.LogException(ex); }
        }

        static void CreatePhysicalApartment()
        {
            roomModel = Model("HomeRoom", world.transform, Vector3.zero);
            foreach (var t in roomModel.GetComponentsInChildren<Transform>())
            {
                if (t.name.StartsWith("Anchor_") || t.name.StartsWith("Approach_")) anchors[t.name] = t;
                // FBX empty transforms carry centimetre scale although mesh vertices are metres.
                if (t.name.StartsWith("COLLIDER_")) { var collider = t.gameObject.AddComponent<BoxCollider>(); collider.size = Vector3.one * .01f; }
            }
            if (!anchors.ContainsKey("Anchor_Start")) throw new InvalidOperationException("The authored room has no interaction anchors.");
            var shelf = Model("Shelf", world.transform, anchors["Anchor_Shelf"].position, 1f, 180); shelf.name = "Hazırlanabilir raf";
            var wardrobe = Model("Wardrobe", world.transform, anchors["Anchor_Wardrobe"].position, 1f, 180); wardrobe.name = "Hazırlanabilir dolap";
            physicalItems["shelf"] = shelf; physicalItems["wardrobe"] = wardrobe;
            AddObstacle(shelf); AddObstacle(wardrobe);
            physicalBag = Model("BackpackClosed", world.transform, anchors["Anchor_BagWork"].position, 1, 180); physicalBag.name = "Aile çantası";
            physicalFlashlight = Model("Flashlight", world.transform, anchors["Anchor_FlashlightWork"].position, 1, 180); physicalFlashlight.name = "Pilli fener";
            physicalRadio = Model("Radio", world.transform, anchors["Anchor_RadioWork"].position, 1, 180); physicalRadio.name = "Ayarlanabilir radyo";
            foreach (var pair in new[] { ("Water","Water"),("Food","Food"),("FirstAid","Aid"),("Blanket","Blanket"),("FamilyCard","Card"),("ComfortFox","Comfort"),("Whistle","Whistle") })
            {
                var item = Model(pair.Item1, world.transform, anchors["Anchor_" + pair.Item2].position, 1, 180);item.name="Odada bulunacak · "+pair.Item1;physicalItems[pair.Item1] = item;
                if(pair.Item1=="FirstAid")item.transform.SetParent(shelf.transform,true);
            }
            selectedHandTarget = Group("Elin takip ettiği gerçek nesne", interactions.transform, anchors["Anchor_Start"].position + Vector3.up).transform;
        }
        static void AddObstacle(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(); var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            var collider = root.AddComponent<BoxCollider>(); collider.center = root.transform.InverseTransformPoint(b.center); collider.size = b.size;
            var obstacle = root.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box; obstacle.center = collider.center; obstacle.size = collider.size; obstacle.carving = true;
        }

        static void CreateApprovedCast()
        {
            foreach (string name in Names)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root + "/" + name + ".prefab");
                if (!prefab) throw new InvalidOperationException("Prepare the approved character style first: " + name);
                var person = (GameObject)PrefabUtility.InstantiatePrefab(prefab, castRoot.transform); person.name = name;
                YanYanaCharacterStyle.NormalizeRoot(person);
                var start = name == "Ada" ? "Anchor_Start" : "Anchor_" + name;
                person.transform.position = anchors.ContainsKey(start) ? anchors[start].position : street.ContainsKey(name)?street[name].position:street["Aid"].position;
                person.transform.rotation = Quaternion.Euler(0, 180, 0);
                var animator = person.GetComponentInChildren<Animator>(); animator.runtimeAnimatorController = YanYanaAdultLocomotion.ForCharacter(name, actorController); animator.Rebind(); animator.Update(0);
                GroundAnimatedVisual(person);
                cast[name] = person;
                float height = YanYanaCharacterStyle.Heights[Array.IndexOf(Names, name)];
                if (name == "Ada" || name == "Idil" || name == "Bora" || name == "Derya")
                {
                    var agent = person.AddComponent<NavMeshAgent>(); agent.radius = .21f; agent.height = height; agent.speed = 1.7f; agent.acceleration = 8; agent.stoppingDistance = .1f;
                    var mover = person.AddComponent<StoryPlayerMovement>(); Serialized(mover, "animator", animator); movers[name] = mover;
                }
                if (name == "Efe")
                {
                    var agent = person.AddComponent<NavMeshAgent>(); agent.radius = .20f; agent.height = height; agent.speed = 1.85f; agent.acceleration = 8;
                    follower = person.AddComponent<StorySiblingFollower>(); Serialized(follower, "animator", animator);
                }
            }
            Serialized(follower, "target", cast["Ada"].transform);
        }

        static void GroundAnimatedVisual(GameObject actor)
        {
            var animator=actor.GetComponentInChildren<Animator>();var mesh=new Mesh();float lowest=float.PositiveInfinity,highest=float.NegativeInfinity;
            foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.BakeMesh(mesh,true);
                foreach(var vertex in mesh.vertices)
                {float y=renderer.transform.TransformPoint(vertex).y;lowest=Mathf.Min(lowest,y);highest=Mathf.Max(highest,y);}
            }
            UnityEngine.Object.DestroyImmediate(mesh);
            if(highest-lowest<.6f||highest-lowest>2.5f)throw new InvalidOperationException("Cannot verify animated grounding for "+actor.name+": "+(highest-lowest));
            animator.transform.position+=Vector3.up*(actor.transform.position.y+.004f-lowest);
        }

        static void CreatePhysicalCameras()
        {
            brain.LensModeOverride=new CinemachineBrain.LensModeOverrideSettings{Enabled=true,DefaultMode=LensSettings.OverrideModes.Perspective};
            var followTarget = Group("Takip odağı", cast["Ada"].transform, new Vector3(0, .8f, 0));
            var go = Group("Ada ile odada gezin", presentation.transform); exploreCamera = go.AddComponent<CinemachineCamera>();
            var lens = exploreCamera.Lens; lens.ModeOverride = LensSettings.OverrideModes.Perspective; lens.FieldOfView = 42; lens.NearClipPlane = .08f; lens.FarClipPlane = 60; exploreCamera.Lens = lens;
            exploreCamera.Follow = followTarget.transform; exploreCamera.LookAt = followTarget.transform;
            var follow = go.AddComponent<CinemachineFollow>(); follow.FollowOffset = (anchors["Anchor_Camera"].position - anchors["Anchor_CameraLook"].position).normalized * 7.9f;
            follow.TrackerSettings.BindingMode = BindingMode.WorldSpace; follow.TrackerSettings.PositionDamping = new Vector3(.15f, .18f, .15f);
            var aim = go.AddComponent<CinemachineRotationComposer>(); aim.Damping = new Vector2(.1f, .1f);
            exploreCamera.Priority = 10;
            camera.orthographic = false; camera.fieldOfView = 42;
        }
        static CinemachineCamera WorkCamera(string name, Vector3 focus, float size, bool top = true)
        {
            var go = Group("Yakından incele · " + name, presentation.transform, focus + (top ? new Vector3(0, 2, -.001f) : new Vector3(.15f, .65f, -1.2f)));
            go.transform.LookAt(focus, top ? Vector3.forward : Vector3.up);
            var view = go.AddComponent<CinemachineCamera>(); var lens = view.Lens; lens.ModeOverride = LensSettings.OverrideModes.Orthographic; lens.OrthographicSize = size; lens.NearClipPlane = .01f; lens.FarClipPlane = 20; view.Lens = lens; view.Priority = 20; go.SetActive(false); workCameras[name] = view; return view;
        }

        static void InitialPhysical(string name, int value = 0)
        {
            main.Initial(name, value); physicalKeys.Add(name);
        }
        static void CreatePhysicalState()
        {
            main = new YanYanaGraphAuthor(flow, "Kesintisiz macera · gerçek nesne durumları");
            main.Initial("Current", "free_preparation"); main.Initial("Workspace", ""); main.Initial("Role", "Ada"); main.Initial("Busy", false); main.Initial("Paused", true);
            main.Initial("Dragging", false); main.Initial("PointerOwner", -999); main.Initial("HandContact", false); main.Initial("ActiveHandTarget", selectedHandTarget);
            main.Initial("ActiveMover", movers["Ada"]); main.Initial("SelectedItem", flow); main.Initial("IdleSeconds", 0f); main.Initial("PlaySeconds", 0f);
            main.Initial("ReducedMotion", true); main.Initial("Sound", true); main.Initial("Captions", true); main.Initial("Vibration", false);
            main.Initial("AdultWorking",false);
            main.Initial("ApproachTarget",flow);
            foreach (string flag in campaign.flags) InitialPhysical(flag);
            InitialPhysical("Phase");
            activityBack = Button("ActivityBack", "Odaya dön", safeRect, Vector2.zero, new Vector2(0,0), new Vector2(20,214),new Vector2(184,286)).gameObject; activityBack.SetActive(false);
            rotateControl = Button("RotateItem", "Çevir", safeRect, new Vector2(1,0),new Vector2(1,0),new Vector2(-164,214),new Vector2(-20,286)).gameObject; rotateControl.SetActive(false);
            goalText.text = "Çantanı işe yarar hâle getir"; lineText.text = "Efe: Eşyaları deneyelim. Sonra çantaya sığdıralım."; gestureText.text = "Zemine dokunarak odada dolaş";
            chapterText.text = "BİZİM MAHALLE";
        }

        static ValueOutput CanExplore(YanYanaGraphAuthor g) => And(g,And(g,And(g, Available(g), Is(g, g.Var("Workspace", flow), "")),Is(g,Or(g,Is(g,g.Var("Phase",flow),1),Is(g,g.Var("Phase",flow),31)),false)),Or(g,Is(g,g.Var("IntroDone",flow),1),g.Binary<Greater>(g.Var("Phase",flow),0)));
        static ValueOutput AtWork(YanYanaGraphAuthor g, string name) => And(g, Available(g), Is(g, g.Var("Workspace", flow), name));
        static void CreatePhysicalNavigation()
        {
            foreach (var floor in world.GetComponentsInChildren<Transform>().Where(t => t.name == "COLLIDER_Floor" || t.name.StartsWith("COLLIDER_MahalleZemini") || t.name.StartsWith("COLLIDER_Avlu") || t.name.StartsWith("COLLIDER_Sahanlik") || t.name.StartsWith("COLLIDER_Basamak") || t.name.StartsWith("COLLIDER_AnaYol") || t.name.StartsWith("COLLIDER_YanYol") || t.name.StartsWith("COLLIDER_Baglanti")))
            {
                var g = new YanYanaGraphAuthor(floor.gameObject, "Serbest ve kesintisiz yürüme"); var click = g.Add(new OnPointerClick()); g.Bind(click.target, floor.gameObject);
                var allowed = g.Branch(click.trigger, CanExplore(g)); var raycast = g.Get(typeof(PointerEventData), "pointerCurrentRaycast", click.data);
                var p=g.SetVar(allowed.ifTrue,"ApproachTarget",flow,flow);g.Do(p, typeof(StoryPlayerMovement), "TrySetDestination", g.Var("ActiveMover",flow), new[] { typeof(Vector3) }, g.Get(typeof(RaycastResult), "worldPosition", raycast)); g.Dirty();
            }
        }
        static void ApproachWorkspace(GameObject objectRoot, string id, Vector3 approachPoint, string goal, string line)
        {
            var renderers = objectRoot.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var localBounds=new Bounds(objectRoot.transform.InverseTransformPoint(bounds.center),Vector3.zero);
            for(int corner=0;corner<8;corner++)localBounds.Encapsulate(objectRoot.transform.InverseTransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
            var hit = objectRoot.GetComponent<BoxCollider>(); if(!hit)hit=objectRoot.AddComponent<BoxCollider>(); hit.center = localBounds.center; hit.size = new Vector3(Mathf.Max(.18f,localBounds.size.x),Mathf.Max(.18f,localBounds.size.y),Mathf.Max(.18f,localBounds.size.z));
            var g = new YanYanaGraphAuthor(objectRoot, "Yaklaşınca " + id + " nesnesini incele"); g.Initial("Approaching", false);
            var click = g.Add(new OnPointerClick()); g.Bind(click.target, objectRoot); var allowed = g.Branch(click.trigger, CanExplore(g));
            var p = g.SetVar(allowed.ifTrue,"ApproachTarget",objectRoot,flow);p=g.Do(p, typeof(StoryPlayerMovement), "TrySetDestination", movers["Ada"], new[] { typeof(Vector3) }, approachPoint); g.SetVar(p, "Approaching", true);
            var update = g.Add(new Unity.VisualScripting.Update()); var pending = g.Branch(update.trigger, And(g,And(g,Is(g,g.Var("ApproachTarget",flow),objectRoot),Is(g,g.Var("Phase",flow),0)),And(g, g.Var("Approaching"), CanExplore(g))));
            var distance = g.Call(typeof(Vector3), "Distance", null, new[] { typeof(Vector3),typeof(Vector3) }, g.Get(typeof(Transform),"position",cast["Ada"].transform),approachPoint).result;
            var arrived = g.Branch(pending.ifTrue,g.Binary<Less>(distance,.35f)); p=g.SetVar(arrived.ifTrue,"Approaching",false); g.Send(p,flow,"OpenWork",id,goal,line); g.Dirty();
        }

        static (ControlOutput path, ValueOutput point) PointerOnPlane(YanYanaGraphAuthor g, ControlOutput path, ValueOutput data, float height)
        {
            var screen = g.Get(typeof(PointerEventData), "position", data);
            var ray = g.Call(typeof(Camera), "ScreenPointToRay", camera, new[] { typeof(Vector3) }, V3(g,g.Get(typeof(Vector2),"x",screen),g.Get(typeof(Vector2),"y",screen),0f)).result;
            // Plane literals do not serialize their private normal/distance in native VS.
            // Intersect the authored horizontal work surface using the ray's public vectors.
            var origin=g.Get(typeof(Ray),"origin",ray);var direction=g.Get(typeof(Ray),"direction",ray);
            var distance=g.Binary<ScalarDivide>(g.Binary<ScalarSubtract>(height,g.Get(typeof(Vector3),"y",origin)),g.Get(typeof(Vector3),"y",direction));
            var point=g.Call(typeof(Ray),"GetPoint",ray,OneFloat,distance).result;
            return (path,point);
        }

        static void CreatePhysicalMenusAndSave()
        {
            var open=main.Event("OpenWork",true,3); var p=main.SetVar(open.trigger,"Busy",true); p=main.SetVar(p,"Workspace",open.argumentPorts[0]);
            p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,true);
            p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);
            p=Text(main,p,goalText,open.argumentPorts[1]); p=Text(main,p,lineText,open.argumentPorts[2]); p=main.Active(p,activityBack,Is(main,main.Var("Phase"),0));
            var select=main.Add(new SwitchOnString{options=workCameras.Keys.ToList()}); main.Bind(select.selector,open.argumentPorts[0]); main.Link(p,select.enter);
            foreach(var branch in select.branches)
            {
                string gesture=branch.Key.StartsWith("supply")?"Çevir ve incele · seçtiğini çanta yerine sürükle":branch.Key=="bagfit"?"Fermuarı kumaştaki iz boyunca çek":branch.Key=="flashlight"?"Kapağı kaydır · pilleri çevir ve yerleştir":branch.Key=="bag"?"Bir eşya seç · sürükle · çevir":branch.Key=="radio"?"Düğmeyi döndür · yayını bul":branch.Key=="bridge"?"Tahtaları suyun üzerine yerleştir":branch.Key=="hose"?"Bağlantıyı yerleştir · vanayı çevir":"Aile taşını açık yoldan götür";
                if(branch.Key=="sibling")gesture="Ada’nın elini Efe’nin eline götür";
                if(branch.Key=="intro")gesture="Kutuyu Ada’nın ellerine doğru sürükle";
                if(branch.Key=="introDrop")gesture="Yana sürükle: çevir · aşağı sürükle: bırak";
                var q=Text(main,branch.Value,gestureText,gesture);q=main.Active(q,workCameras[branch.Key].gameObject,true); q=main.Wait(q,.75f); q=Released(main,q); main.SetVar(q,"Busy",false);
            }
            p=ButtonEvent(main,"ActivityBack","BackPhysical");p=main.Send(p,flow,"LeaveWorkspace",main.Var("Workspace")); main.Send(p,flow,"Explore");
            var explore=main.Event("Explore",true); p=main.SetVar(explore.trigger,"Busy",true); p=main.SetVar(p,"Dragging",false);p=main.SetVar(p,"HandContact",false);
            p=main.SetVar(p,"Workspace","");p=main.Send(p,flow,"RestorePhysicalObjects");foreach(var view in workCameras.Values) p=main.Active(p,view.gameObject,false);
            p=main.Active(p,activityBack,false); p=main.Active(p,rotateControl,false);p=Text(main,p,goalText,"Çantanı işe yarar hâle getir");p=Text(main,p,gestureText,"Zemine dokunarak odada dolaş");p=Text(main,p,lineText,"Efe: Eşyaları deneyelim. Sonra çantaya sığdıralım.");
            p=main.Send(p,flow,"RefreshCarryGoal");p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,false);p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,true);
            p=main.Send(p,flow,"CommitCheckpoint"); p=main.Wait(p,.5f);p=Released(main,p);main.SetVar(p,"Busy",false);
            var phaseText=main.Event("Explore");var which=main.Add(new SwitchOnInteger{options=new List<int>{2,3,4,6}});main.Bind(which.selector,main.Var("Phase"));main.Link(phaseText.trigger,which.enter);
            foreach(var branch in which.branches)
            {
                string goal=branch.Key==2?"Efe’yi ve çıkışı kontrol et":branch.Key==3?"Merdivenden birlikte dışarı ilerle":branch.Key==4?"Güvenli taraftaki İdil’e ulaş":"Bora’nın yardım noktasına ulaş";
                var q=Text(main,branch.Value,goalText,goal);q=Text(main,q,lineText,branch.Key==2?"Ada: Önce birbirimizi ve güvenli çıkışı kontrol edelim.":branch.Key==3?"Efe: Açık geçişleri izleyerek birlikte yürüyelim.":branch.Key==4?"Efe: İtfaiye ekibi burada. Güvenli taraftan yaklaşalım.":"Ada: Yardım noktası ileride. Bora’yla konuşalım.");
                if(branch.Key==2)q=main.Send(q,flow,"RefreshSiblingGoal");
                if(branch.Key>=4)q=main.Set(q,typeof(Light),"intensity",sun,1.05f);
            }
            p=ButtonEvent(main,"RotateItem","RotatePhysical"); main.Send(p,main.Var("SelectedItem"),"Rotate");
            CreatePhysicalPersistence();
            main.Dirty();
        }
    }
}
