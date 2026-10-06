// Authors native scene graphs, Animator assets and scenery. No new player C#.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Cinemachine;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        const string QuakeGraphPrefix = "Deprem etkileşimi · ";
        static T[] QuakeAll<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        static Transform QuakeFind(string name) => QuakeAll<Transform>().First(t => t.name == name);
        static CinemachineCamera quakeWide, quakeHands;
        static Transform quakeCameraRig;
        static Material quakeCueMaterial;
        static readonly Dictionary<TMP_Text, TMP_Text> quakeLabels = new Dictionary<TMP_Text, TMP_Text>();
        static RectTransform quakeProgressFill;
        static GameObject quakeProgress;
        static RectTransform quakeTouch;

        [MenuItem("Tools/Yan Yana/Art/Rebuild Interactive Earthquake")]
        public static void ApplyInteractiveQuakeAndSave()
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open the adventure in Edit Mode first.");
            ApplyInteractiveQuake();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("INTERACTIVE EARTHQUAKE AUTHORED: continuous crouch, two sustained hand drags, held grip, indoor cameras.");
        }

        public static void ApplyInteractiveQuake()
        {
            HydrateQuakeContext();
            RepairSiblingArrival();
            Variables.Object(flow).Set("ReducedMotion", false);
            // New journeys start with the authored mild shake; saved accessibility choices win.
            foreach (var machine in flow.GetComponents<ScriptMachine>())
                foreach (var call in machine.graph.units.OfType<InvokeMember>().Where(u => u.member.targetType == typeof(PlayerPrefs) && u.member.name == "GetInt"))
                {
                    if (!call.inputParameters.TryGetValue(0, out var key) || !call.inputParameters.TryGetValue(1, out var fallback)) continue;
                    object Read(ValueInput input) => input.unit.defaultValues.TryGetValue(input.key, out var value) ? value : (machine.graph.valueConnections.FirstOrDefault(c => c.destination == input)?.source.unit as Literal)?.value;
                    if ((string)(Read(key) as string) != SavePrefix + "option.ReducedMotion") continue;
                    if (fallback.hasDefaultValue) fallback.unit.defaultValues[fallback.key] = 0;
                    else if (machine.graph.valueConnections.FirstOrDefault(c => c.destination == fallback)?.source.unit is Literal literal) literal.value = 0;
                    EditorUtility.SetDirty(machine);
                }
            // Change only the quake listeners; preparation, fire and save graphs keep their references.
            foreach (var machine in flow.GetComponents<ScriptMachine>())
            {
                if (machine.graph.title != "Kesintisiz macera · gerçek nesne durumları") continue;
                foreach (var evt in machine.graph.units.OfType<CustomEvent>())
                {
                    if (!evt.defaultValues.TryGetValue(evt.name.key, out var value) || !(value is string name)) continue;
                    if (new[] { "BeginQuake", "ProtectTogether", "LowerIntoCover", "FinishCoverGrip" }.Contains(name))
                        evt.defaultValues[evt.name.key] = "Superseded " + name;
                }
                EditorUtility.SetDirty(machine);
            }
            foreach (var machine in QuakeAll<ScriptMachine>().Where(m => m.graph.title.StartsWith(QuakeGraphPrefix) ||
                m.graph.title == "Yakındaki korunma alanında gerçekten çök" ||
                m.graph.title == "Yakındaki masanın altında çök, korun, tutun" ||
                m.graph.title.StartsWith("Eli doğru korunma noktasına taşı")))
                UnityEngine.Object.DestroyImmediate(machine);
            foreach (string name in new[] { "Başını koruyan el", "Masaya tutunan el", "Başını koruma hedefi", "Tutunma hedefi", "Deprem kamera taşıyıcısı", "Deprem hareket rehberi", "Deprem tutuş göstergesi", "Deprem oda ayrıntıları", "Evin tamamlanmış yakın çevresi" })
                foreach (var t in QuakeAll<Transform>().Where(t => t.name == name).ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach (var t in QuakeAll<Transform>().Where(t => t.name.StartsWith("KKTC_Quake_")).ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
            CalibrateCoverOffsets();
            AuthorQuakeAnimator();
            AuthorQuakeCameras();
            AuthorQuakeScenery();
            AuthorQuakeUI();
            main = new YanYanaGraphAuthor(flow, QuakeGraphPrefix + "korunma akışı");
            AuthorQuakeSpeech();
            AuthorQuakeState();
            AuthorCrouchInput();
            AuthorQuakeHandInput(true);
            AuthorQuakeHandInput(false);
            AuthorQuakeMotionAndAtmosphere();
            main.Dirty();
            RefreshPresentationPause();
        }

        static void HydrateQuakeContext()
        {
            flow = QuakeAll<Transform>().First(t => t.name.StartsWith("01 Akış")).gameObject;
            world = QuakeAll<Transform>().First(t => t.name.StartsWith("02 Dünya")).gameObject;
            castRoot = QuakeAll<Transform>().First(t => t.name.StartsWith("03 Karakterler")).gameObject;
            interactions = QuakeAll<Transform>().First(t => t.name.StartsWith("04 Etkileşimler")).gameObject;
            presentation = QuakeAll<Transform>().First(t => t.name.StartsWith("05 Sunum")).gameObject;
            camera = Camera.main; brain = camera.GetComponent<CinemachineBrain>();
            roomModel = QuakeFind("HomeRoom").gameObject;
            foreach (var t in roomModel.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("Anchor_")) anchors[t.name] = t;
            foreach (string who in Names)
            {
                cast[who] = castRoot.transform.Find(who).gameObject;
                var mover = cast[who].GetComponent<StoryPlayerMovement>(); if (mover) movers[who] = mover;
            }
            follower = cast["Efe"].GetComponent<StorySiblingFollower>();
            foreach (string who in new[] { "Ada", "Efe" })
            {
                headPalms[who] = QuakeFind("Başta güvenli avuç teması · " + who);
                secondHeadPalms[who] = QuakeFind("İkinci koruyucu avuç · " + who);
            }
            var top = QuakeFind("Tek amaç");
            chapterText = top.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "BİZİM MAHALLE");
            goalText = top.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Efe’nin yanına git");
            lineText = QuakeFind("Kısa konuşma").GetComponentsInChildren<TMP_Text>(true).First(t => !t.name.StartsWith("KKTC_Quake_"));
            gestureText = QuakeFind("Hareket ipucu").GetComponentsInChildren<TMP_Text>(true).First(t => !t.name.StartsWith("KKTC_Quake_"));
            hintPanel = QuakeFind("İsteğe bağlı destek").gameObject;
            safeRect = (RectTransform)QuakeFind("Güvenli ekran alanı");
            font = goalText.font;
            exploreCamera = QuakeAll<CinemachineCamera>().First(c => c.name == "Ada ile odada gezin");
            feedback = QuakeFind("Dokunma ve etkileşim sesleri").GetComponent<AudioSource>();
            speech = QuakeFind("Kısa Türkçe konuşmalar").GetComponent<AudioSource>();
            sun = QuakeAll<Light>().First(l => l.type == LightType.Directional);
            foreach (var mat in AssetDatabase.FindAssets("t:Material", new[] { Root + "Art/Materials" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>)) mats[mat.name] = mat;
            quakeCueMaterial = Mat("QuakeGestureGold", "#FFCB69");
        }

        static void RepairSiblingArrival()
        {
            // The player's hand gesture completes sibling support; do not offer a Next bypass.
            var skip = QuakeFind("SiblingContinue").gameObject;
            skip.SetActive(false);
            foreach (var machine in QuakeAll<ScriptMachine>().Where(m => m.graph.title == "Kardeşine kendi elinle karşılık ver"))
            {
                foreach (var call in machine.graph.units.OfType<InvokeMember>().Where(u => u.member.targetType == typeof(GameObject) && u.member.name == "SetActive"))
                {
                    object target = call.defaultValues.TryGetValue(call.target.key, out var value) ? value : (machine.graph.valueConnections.FirstOrDefault(c => c.destination == call.target)?.source.unit as Literal)?.value;
                    if (target as GameObject != skip || !call.inputParameters.TryGetValue(0, out var enabled)) continue;
                    foreach (var connection in machine.graph.valueConnections.Where(c => c.destination == enabled).ToArray()) machine.graph.valueConnections.Remove(connection);
                    call.defaultValues[enabled.key] = false;
                }
                EditorUtility.SetDirty(machine);
            }
            foreach (var machine in flow.GetComponents<ScriptMachine>().Where(m => m.graph.title == "Kesintisiz macera · gerçek nesne durumları"))
            {
                var start = machine.graph.units.OfType<CustomEvent>().FirstOrDefault(e => e.defaultValues.TryGetValue(e.name.key, out var name) && (string)(name as string) == "BeginSiblingCheck");
                if (start == null) continue;
                var pending = new Queue<IUnit>(); var visited = new HashSet<IUnit>(); pending.Enqueue(start);
                while (pending.Count > 0)
                {
                    var unit = pending.Dequeue(); if (!visited.Add(unit)) continue;
                    if (unit is WaitUntilUnit wait)
                    {
                        var less = machine.graph.valueConnections.FirstOrDefault(c => c.destination == wait.condition)?.source.unit as Less;
                        if (less != null)
                        {
                            var threshold = less.valueInputs.ElementAt(1);
                            var literal = machine.graph.valueConnections.FirstOrDefault(c => c.destination == threshold)?.source.unit as Literal;
                            if (literal != null) literal.value = .16f;
                            else threshold.unit.defaultValues[threshold.key] = .16f;
                            EditorUtility.SetDirty(machine);
                        }
                        break;
                    }
                    foreach (var next in machine.graph.controlConnections.Where(c => c.source.unit == unit).Select(c => c.destination.unit)) pending.Enqueue(next);
                }
            }
        }

        static void AuthorQuakeUI()
        {
            quakeLabels.Clear();
            // The existing picture adapter explicitly preserves KKTC_ authored labels.
            // Scene-only labels keep the continuous gesture visible in the child presentation.
            foreach (var source in new[] { chapterText, goalText, lineText, gestureText })
            {
                var clone = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent);
                clone.name = "KKTC_Quake_" + (source == chapterText ? "chapter" : source == goalText ? "goal" : source == lineText ? "speech" : "gesture");
                foreach (Transform child in clone.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                var label = clone.GetComponent<TMP_Text>(); label.enabled = true; label.raycastTarget = false; label.text = "";
                label.enableAutoSizing = source.enableAutoSizing; label.overflowMode = TextOverflowModes.Ellipsis;
                clone.SetActive(false); quakeLabels[source] = label;
            }
            quakeProgress = Panel("Deprem tutuş göstergesi", safeRect, Vector2.zero, new Vector2(1, 0), new Vector2(64, 193), new Vector2(-64, 199), new Color(.09f, .16f, .17f, .9f)).gameObject;
            quakeProgressFill = Panel("KKTC_Quake_progress", quakeProgress.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Hex("#FFCB69")).rectTransform;
            quakeProgressFill.pivot = new Vector2(0, .5f); quakeProgress.SetActive(false);
            var touch = Panel("KKTC_Quake_touch", safeRect, Vector2.zero, Vector2.zero, new Vector2(-14, -14), new Vector2(14, 14), new Color(1, .80f, .41f, .58f));
            touch.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "UI/Generated/TouchCircle.png"); touch.type = Image.Type.Simple;
            quakeTouch = touch.rectTransform; touch.gameObject.SetActive(false);
        }

        static void AuthorQuakeAnimator()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "Animation/AdventureCharacters.controller");
            if (!controller.parameters.Any(p => p.name == "QuakeCrouch")) controller.AddParameter("QuakeCrouch", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Parmakla kontrollü çöküş");
            if (!state)
            {
                state = machine.AddState("Parmakla kontrollü çöküş");
                var tree = new BlendTree { name = "Ayakta / çökmüş · sürekli kontrol", blendParameter = "QuakeCrouch", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(tree, controller);
                tree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Story/Animations/Generated/ChildNeutralIdle.anim"), 0);
                tree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "Animation/YanYana_Cover.anim"), 1);
                state.motion = tree;
                var enter = machine.AddAnyStateTransition(state); enter.hasExitTime = false; enter.duration = .28f; enter.hasFixedDuration = true; enter.canTransitionToSelf = false; enter.AddCondition(AnimatorConditionMode.Equals, 3, "Pose");
                var idle = machine.states.Select(s => s.state).First(s => s.name == "Duruş ve yürüyüş");
                var leave = state.AddTransition(idle); leave.hasExitTime = false; leave.duration = .4f; leave.hasFixedDuration = true; leave.AddCondition(AnimatorConditionMode.NotEqual, 3, "Pose");
            }
            EditorUtility.SetDirty(controller);
        }

        static CinemachineCamera QuakeCamera(string name, Vector3 position, Vector3 focus, float fieldOfView)
        {
            var go = new GameObject(name); go.transform.SetParent(quakeCameraRig, false); go.transform.position = position; go.transform.LookAt(focus);
            var view = go.AddComponent<CinemachineCamera>(); var lens = view.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Perspective; lens.FieldOfView = fieldOfView; lens.NearClipPlane = .04f; lens.FarClipPlane = 18;
            view.Lens = lens; view.Priority = 35; go.SetActive(false); return view;
        }

        static void AuthorQuakeCameras()
        {
            quakeCameraRig = Group("Deprem kamera taşıyıcısı", presentation.transform).transform;
            quakeWide = QuakeCamera("Deprem · oda içinden başlangıç", new Vector3(.7f, 3.1f, -3.65f), new Vector3(.7f, .6f, -1.15f), 70);
            quakeHands = QuakeCamera("Deprem · eller ve masa ayağı", new Vector3(.45f, .83f, -2.5f), new Vector3(.4f, .46f, .74f), 50);
            // Preparation/exit framing looks down onto the authored room rather than at an empty horizon.
            var follow = exploreCamera.GetComponent<CinemachineFollow>(); follow.FollowOffset = new Vector3(-.7f, 4.2f, -3.3f);
            follow.TrackerSettings.PositionDamping = new Vector3(.38f, .45f, .38f);
            exploreCamera.transform.SetPositionAndRotation(exploreCamera.Follow.position + follow.FollowOffset, Quaternion.LookRotation(-follow.FollowOffset, Vector3.up));
            var lens = exploreCamera.Lens; lens.FieldOfView = 48; exploreCamera.Lens = lens;
            // The previous shake graph restores its cached offset every frame; update its baseline too.
            foreach (var machine in flow.GetComponents<ScriptMachine>().Where(m => m.graph.title == "İsteğe bağlı düşük genlikli kamera sarsıntısı"))
            {
                foreach (var unit in machine.graph.units)
                {
                    foreach (string key in unit.defaultValues.Keys.ToArray()) if (unit.defaultValues[key] is Vector3 v && v.magnitude > 3f && v.y > 1f && v.z < -.5f) unit.defaultValues[key] = follow.FollowOffset;
                    if (unit is Literal literal && literal.value is Vector3 offset && offset.magnitude > 3f && offset.y > 1f && offset.z < -.5f) literal.value = follow.FollowOffset;
                }
                EditorUtility.SetDirty(machine);
            }
        }

        static void AuthorQuakeScenery()
        {
            var scenery = Group("Evin tamamlanmış yakın çevresi", world.transform);
            var paving = Mat("QuakeCourtyardStone", "#D5C9AB");
            Shape("Dairenin kesit altında temel", PrimitiveType.Cube, scenery.transform, new Vector3(0, -.28f, 0), new Vector3(8.2f, .46f, 8.1f), paving);
            Shape("Kapı önü taş döşeme", PrimitiveType.Cube, scenery.transform, new Vector3(.65f, -.48f, -5.2f), new Vector3(8.7f, .08f, 2.8f), paving);
            Shape("Pencere arkasındaki avlu", PrimitiveType.Cube, scenery.transform, new Vector3(0, -.48f, 8.6f), new Vector3(22, .08f, 9.4f), paving);
            var garden = Mat("QuakeGardenGreen", "#86996D");
            for (int i = 0; i < 3; i++)
            {
                var legacyHouse = Model("Facade_" + (i + 1), scenery.transform, new Vector3(-7.2f + i * 7.2f, -.48f, 12.9f), .75f, 180);
                legacyHouse.SetActive(!world.transform.Find("KKTC · hacimli mahalle ve açık meydan"));
                Model("Plant", scenery.transform, new Vector3(-5.5f + i * 5.5f, -.45f, 7.3f), 1.5f, i * 60);
                Shape("Avlu bitki yatağı " + i, PrimitiveType.Cube, scenery.transform, new Vector3(-5.5f + i * 5.5f, -.42f, 7.3f), new Vector3(2.5f, .1f, 1.6f), garden);
            }
            Shape("Arka avlu sınır duvarı", PrimitiveType.Cube, scenery.transform, new Vector3(0, .12f, 14), new Vector3(22, 1.2f, .22f), mats["YY_cream"]);
            var detail = Group("Deprem oda ayrıntıları", world.transform);
            var hanging = Group("Sallanan tavan lambası", detail.transform, new Vector3(.35f, 2.83f, .75f));
            Shape("Lamba kablosu", PrimitiveType.Cylinder, hanging.transform, new Vector3(0, -.22f, 0), new Vector3(.018f, .22f, .018f), mats["YY_tealDark"]);
            Shape("Lamba başlığı", PrimitiveType.Sphere, hanging.transform, new Vector3(0, -.52f, 0), new Vector3(.48f, .18f, .48f), mats["YY_mustard"]);
            var animation = hanging.AddComponent<Animation>();
            const string path = Root + "Animation/QuakeLampSway.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path); if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves(); clip.name = "Deprem lamba salınımı"; clip.legacy = true; clip.wrapMode = WrapMode.Loop;
            var keys = Enumerable.Range(0, 25).Select(i => new Keyframe(i * .075f, Mathf.Sin(i * Mathf.PI / 12) * 5.5f)).ToArray();
            clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(keys)); animation.clip = clip; animation.AddClip(clip, clip.name); animation.playAutomatically = false;
            EditorUtility.SetDirty(clip);
            var rumble = detail.AddComponent<AudioSource>(); rumble.clip = AuthorQuakeAudio(); rumble.playOnAwake = false; rumble.loop = true; rumble.volume = .7f; rumble.spatialBlend = 0;
        }

        static AudioClip AuthorQuakeAudio()
        {
            const string path = Root + "Audio/QuakeRoomRumble.wav";
            if (!File.Exists(path))
            {
                const int rate = 24000, samples = rate * 12;
                var random = new System.Random(4105); double smooth = 0;
                using (var output = new BinaryWriter(File.Create(path)))
                {
                    output.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); output.Write(36 + samples * 2); output.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); output.Write(16); output.Write((short)1); output.Write((short)1); output.Write(rate); output.Write(rate * 2); output.Write((short)2); output.Write((short)16); output.Write(System.Text.Encoding.ASCII.GetBytes("data")); output.Write(samples * 2);
                    for (int i = 0; i < samples; i++)
                    {
                        double t = (double)i / rate, noise = random.NextDouble() * 2 - 1; smooth = smooth * .965 + noise * .035;
                        double edge = Math.Min(1, Math.Min(t / .25, (12 - t) / .25));
                        double pulse = .75 + .25 * Math.Sin(t * 2.7), rattle = Math.Pow(Math.Max(0, Math.Sin(t * 9.3)), 10);
                        double value = edge * (.20 * Math.Sin(2 * Math.PI * 49 * t) * pulse + .12 * Math.Sin(2 * Math.PI * 83 * t + Math.Sin(t * 3)) + smooth * .45 + noise * rattle * .065);
                        output.Write((short)(Math.Clamp(value, -.85, .85) * short.MaxValue));
                    }
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static void AuthorQuakeState()
        {
            foreach (string flag in new[] { "QPressed", "QGripPressed", "QCompleting", "QMovingOut" }) main.Initial(flag, false);
            foreach (string flag in new[] { "QCrouch", "QCrouchTarget", "QNearTime", "QHeldTime", "QClock", "QAdultCrouch", "QExitProgress" }) main.Initial(flag, 0f);
            main.Initial("QPointer", -999); main.Initial("QScreen", Vector2.zero); main.Initial("QHandTargetRight", Vector3.zero); main.Initial("QHandTargetLeft", Vector3.zero);
            foreach (string who in new[] { "Ada", "Efe" }) { main.Initial("QStart" + who, cast[who].transform.position); main.Initial("QRotation" + who, cast[who].transform.rotation); }
            var begin = main.Event("BeginQuake", true);
            var p = main.SetVar(begin.trigger, "Phase", 1); p = main.SetVar(p, "Quake", true); p = main.SetVar(p, "Protected", 0); p = main.SetVar(p, "CoverStage", 1); p = main.SetVar(p, "Busy", true); p = main.SetVar(p, "Workspace", "");
            foreach (string flag in new[] { "QCrouch", "QCrouchTarget", "QNearTime", "QHeldTime", "QClock", "QAdultCrouch", "QExitProgress" }) p = main.SetVar(p, flag, 0f);
            foreach (string flag in new[] { "QPressed", "QGripPressed", "QCompleting", "QMovingOut", "CoverDragging" }) p = main.SetVar(p, flag, false);
            p = main.SetVar(p, "QPointer", -999);
            foreach (var view in QuakeAll<CinemachineCamera>().Where(c => c.name.StartsWith("Yakından incele"))) p = main.Active(p, view.gameObject, false);
            p = main.Active(p, quakeWide.gameObject, true); p = main.Active(p, quakeHands.gameObject, false);
            foreach (string name in new[] { "ActivityBack", "RotateItem", "PreparationDone" }) p = main.Active(p, QuakeFind(name).gameObject, false);
            p = main.Do(p, typeof(StorySiblingFollower), "SetFollowing", follower, OneBool, false);
            foreach (string who in new[] { "Ada", "Efe" })
            {
                p = main.SetVar(p, "QStart" + who, main.Get(typeof(Transform), "position", cast[who].transform));
                p = main.SetVar(p, "QRotation" + who, main.Get(typeof(Transform), "rotation", cast[who].transform));
                var mover = who == "Ada" ? (object)movers["Ada"] : follower;
                p = main.Do(p, who == "Ada" ? typeof(StoryPlayerMovement) : typeof(StorySiblingFollower), "SetAuthoredPose", mover, new[] { typeof(Vector3), typeof(Quaternion) }, main.Get(typeof(Transform), "position", cast[who].transform), main.Get(typeof(Transform), "rotation", cast[who].transform));
            }
            p = main.Do(p, typeof(StoryPlayerMovement), "SetStoryInputLocked", movers["Ada"], OneBool, true);
            p = main.Do(p, typeof(StoryPlayerMovement), "SetStoryInputLocked", movers["Derya"], OneBool, true);
            foreach (string who in new[] { "Ada", "Efe", "Derya", "Emre" }) p = main.Do(p, typeof(Animator), "SetInteger", cast[who].GetComponentInChildren<Animator>(), new[] { typeof(string), typeof(int) }, "Pose", 3);
            p = main.Send(p, flow, "ShakeFurniture");
            var dust = QuakeFind("Sarsıntıda kısa stilize toz").GetComponent<ParticleSystem>(); p = main.Do(p, typeof(ParticleSystem), "Play", dust, NoArgs);
            p = main.Set(p, typeof(Light), "intensity", sun, .58f);
            p = QuakeText(main, p, chapterText, "DEPREM ANI"); p = QuakeText(main, p, goalText, "Ada’yı alçalt · masanın altına gir"); p = QuakeText(main, p, gestureText, "Ada’yı tut · aşağı sürükle · alçakta bırak"); p = QuakeText(main, p, lineText, "Ada: Efe, yanımda kal. Birlikte çökelim!");
            p = QuakeShotReady(main, p); p = Released(main, p); main.SetVar(p, "Busy", false);
            var crouched = main.Event("QuakeCrouched", true); p = main.SetVar(crouched.trigger, "Busy", true);
            p = main.SetVar(p, "QCrouchTarget", 1f); p = main.SetVar(p, "QPressed", false); p = main.SetVar(p, "QNearTime", 0f);
            foreach (string side in new[] { "Right", "Left" })
            {
                var palm = HandContactPoint(main, "Ada", side == "Right", side == "Right" ? "Open" : "Soft");
                p = main.SetVar(p, "Cover" + side + "Rest", palm);
                p = main.SetVar(p, "Cover" + side + "Aim", palm);
                p = main.SetVar(p, "QHandTarget" + side, palm);
            }
            p = main.Active(p, quakeWide.gameObject, false); p = main.Active(p, quakeHands.gameObject, true); p = main.SetVar(p, "CoverStage", 2);
            p = QuakeText(main, p, goalText, "Başını ve enseni kendi elinle koru"); p = QuakeText(main, p, gestureText, "Elini başındaki halkaya sürükle · orada tut"); p = QuakeText(main, p, lineText, "Ada: Bir elimle başımı ve ensemi koruyorum.");
            p = QuakeShotReady(main, p); p = Released(main, p); p = main.SetVar(p, "Busy", false); main.Send(p, flow, "CommitCheckpoint");
            var finish = main.Event("QuakeFinish", true); p = main.SetVar(finish.trigger, "Busy", true); p = main.SetVar(p, "QCompleting", true); p = main.SetVar(p, "QPressed", false); p = main.SetVar(p, "Protected", 1); p = main.SetVar(p, "Quake", false); p = main.SetVar(p, "CoverStage", 5);
            p = QuakeText(main, p, goalText, "Sarsıntı durdu · yavaşça doğrul"); p = QuakeText(main, p, gestureText, "Efe’yle birlikte çıkışı kontrol et"); p = QuakeText(main, p, lineText, "Ada: Sarsıntı durdu. Efe, iyi misin?");
            p = main.Wait(p, .45f); p = main.Active(p, quakeHands.gameObject, false); p = main.Active(p, quakeWide.gameObject, true); p = main.SetVar(p, "QMovingOut", true); p = main.Wait(p, 1.4f); p = main.SetVar(p, "QCrouchTarget", 0f); p = main.Wait(p, 1.2f);
            foreach (string who in new[] { "Ada", "Efe", "Derya", "Emre" }) p = main.Do(p, typeof(Animator), "SetInteger", cast[who].GetComponentInChildren<Animator>(), new[] { typeof(string), typeof(int) }, "Pose", 0);
            foreach (string who in new[] { "Ada", "Efe" }) p = main.Do(p, who == "Ada" ? typeof(StoryPlayerMovement) : typeof(StorySiblingFollower), "ReleaseAuthoredPose", who == "Ada" ? (object)movers["Ada"] : follower, new[] { typeof(Vector3) }, main.Get(typeof(Transform), "position", cast[who].transform));
            p = main.Active(p, quakeWide.gameObject, false); p = main.SetVar(p, "Phase", 2); p = main.SetVar(p, "CoverStage", 4); p = main.SetVar(p, "Busy", false); p = main.SetVar(p, "QCompleting", false); p = main.SetVar(p, "QMovingOut", false);
            p = main.Do(p, typeof(StoryPlayerMovement), "SetStoryInputLocked", movers["Ada"], OneBool, false); p = main.Send(p, flow, "SetAfterQuake"); main.Send(p, flow, "CommitCheckpoint");
            var restore = main.Event("RestorePhysicalObjects"); p = main.SetVar(restore.trigger, "QPressed", false); p = main.SetVar(p, "QGripPressed", false); p = main.SetVar(p, "QPointer", -999); p = main.SetVar(p, "QNearTime", 0f); p = main.SetVar(p, "QHeldTime", 0f);
            var during = main.Branch(p, Is(main, main.Var("Phase"), 1)); var partial = main.Branch(during.ifTrue, Is(main, main.Var("CoverStage"), 1)); main.SetVar(partial.ifTrue, "QCrouchTarget", 0f);
            p = main.SetVar(partial.ifFalse, "QHandTargetRight", main.Var("CoverRightRest")); main.SetVar(p, "QHandTargetLeft", main.Var("CoverLeftRest"));
            // Restarting the quake resumes its actual gestures from the nearby floor position.
            // Saving a body under the table would make the existing NavMesh Warp choose its top.
            var checkpoint = main.Event("CommitCheckpoint"); var saveQuake = main.Branch(checkpoint.trigger, Is(main, main.Var("Phase"), 1)); p = saveQuake.ifTrue;
            foreach (string who in new[] { "Ada", "Efe" }) foreach (string axis in new[] { "x", "y", "z" })
            {
                var coordinate = main.Call(typeof(Mathf), "RoundToInt", null, OneFloat, main.Binary<ScalarMultiply>(main.Get(typeof(Vector3), axis, main.Var("QStart" + who)), 1000f)).result;
                p = main.SetVar(p, "Pos" + who + axis, coordinate); p = PutInt(main, p, SavePrefix + "physical.Pos" + who + axis, coordinate);
            }
            main.Do(p, typeof(PlayerPrefs), "Save", null, NoArgs);
        }

        static ControlOutput QuakeText(YanYanaGraphAuthor g, ControlOutput before, TMP_Text target, string value)
        {
            var p = g.Set(before, typeof(TMP_Text), "text", target, value);
            if (quakeLabels.TryGetValue(target, out var label)) p = g.Set(p, typeof(TMP_Text), "text", label, value);
            if (target == lineText) p = g.Send(p, flow, "SpeakQuakeLine", value);
            return p;
        }

        static void AuthorQuakeSpeech()
        {
            var texts = new[] { "Ada: Efe, yanımda kal. Birlikte çökelim!", "Ada: Bir elimle başımı ve ensemi koruyorum.", "Ada: Başım korunuyor. Şimdi sağlamca tutunacağım.", "Ada: Efe, yanımdayız. Sarsıntı bitene kadar tutunalım.", "Ada: Sarsıntı durdu. Efe, iyi misin?" };
            var manifest = JsonUtility.FromJson<YYPhysicalVoiceList>("{\"lines\":" + File.ReadAllText("ArtDirection/YanYana/Audio/Physical/manifest.json") + "}");
            var lines = manifest.lines.Where(v => v.ready && texts.Contains(v.line)).ToArray();
            var speak = main.Event("SpeakQuakeLine", arguments: 1);
            var allowed = main.Branch(speak.trigger, And(main, main.Var("Sound"), Is(main, main.Var("Paused"), false)));
            var p = main.Do(allowed.ifTrue, typeof(AudioSource), "Stop", speech, NoArgs);
            var pick = main.Add(new SwitchOnString { options = lines.Select(v => v.line).ToList() }); main.Bind(pick.selector, speak.argumentPorts[0]); main.Link(p, pick.enter);
            for (int i = 0; i < lines.Length; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Physical/" + lines[i].key + ".wav");
                var play = main.Set(pick.branches[i].Value, typeof(AudioSource), "clip", speech, clip); main.Do(play, typeof(AudioSource), "Play", speech, NoArgs);
            }
        }

        static ControlOutput QuakeShotReady(YanYanaGraphAuthor g, ControlOutput before)
        {
            var p = g.Wait(before, .15f); var settled = g.Add(new WaitUntilUnit());
            g.Bind(settled.condition, Is(g, g.Get(typeof(CinemachineBrain), "IsBlending", brain), false)); g.Link(p, settled.enter);
            return g.Wait(settled.exit, .12f);
        }

        static ValueOutput QuakeInput(YanYanaGraphAuthor g, int stage) => And(g, Available(g), And(g, Is(g, g.Var("Phase", flow), 1), Is(g, g.Var("CoverStage", flow), stage)));
        static ValueOutput QuakeOwns(YanYanaGraphAuthor g, object data) => Is(g, g.Var("QPointer", flow), g.Get(typeof(PointerEventData), "pointerId", data));
        static ValueOutput QuakeDelta(YanYanaGraphAuthor g) => g.Get(typeof(Time), "deltaTime");

        static void AuthorCrouchInput()
        {
            var ada = cast["Ada"]; var g = new YanYanaGraphAuthor(ada, QuakeGraphPrefix + "parmağın kadar çök");
            var down = g.Add(new OnPointerDown()); g.Bind(down.target, ada); var allowed = g.Branch(down.trigger, And(g, QuakeInput(g, 1), Is(g, g.Var("QPressed", flow), false)));
            var p = g.SetVar(allowed.ifTrue, "QPressed", true, flow); p = g.SetVar(p, "QNearTime", 0f, flow); p = g.SetVar(p, "QPointer", g.Get(typeof(PointerEventData), "pointerId", down.data), flow); g.SetVar(p, "QScreen", g.Get(typeof(PointerEventData), "position", down.data), flow);
            var drag = g.Add(new OnDrag()); g.Bind(drag.target, ada); allowed = g.Branch(drag.trigger, And(g, QuakeInput(g, 1), And(g, g.Var("QPressed", flow), QuakeOwns(g, drag.data))));
            var distance = g.Binary<ScalarSubtract>(g.Get(typeof(Vector2), "y", g.Get(typeof(PointerEventData), "pressPosition", drag.data)), g.Get(typeof(Vector2), "y", g.Get(typeof(PointerEventData), "position", drag.data)));
            var progress = g.Call(typeof(Mathf), "Clamp01", null, OneFloat, g.Binary<ScalarDivide>(distance, g.Binary<ScalarMultiply>(g.Get(typeof(Screen), "height"), .17f))).result;
            p = g.SetVar(allowed.ifTrue, "QCrouchTarget", progress, flow); g.SetVar(p, "QScreen", g.Get(typeof(PointerEventData), "position", drag.data), flow);
            var drop = g.Add(new OnEndDrag()); g.Bind(drop.target, ada); allowed = g.Branch(drop.trigger, And(g, QuakeInput(g, 1), And(g, g.Var("QPressed", flow), QuakeOwns(g, drop.data))));
            var fit = g.Branch(allowed.ifTrue, And(g, g.Binary<GreaterOrEqual>(g.Var("QCrouch", flow), .97f), g.Binary<GreaterOrEqual>(g.Var("QNearTime", flow), .42f)));
            g.Send(fit.ifTrue, flow, "QuakeCrouched"); p = g.SetVar(fit.ifFalse, "QPressed", false, flow); p = g.SetVar(p, "QCrouchTarget", 0f, flow); QuakeText(g, p, gestureText, "Aşağı sürükle · çökmüşken kısa süre tut · bırak");
            var up = g.Add(new OnPointerUp()); g.Bind(up.target, ada); var tapped = g.Branch(up.trigger, And(g, QuakeOwns(g, up.data), Is(g, g.Get(typeof(PointerEventData), "dragging", up.data), false))); p = g.SetVar(tapped.ifTrue, "QPressed", false, flow); g.SetVar(p, "QCrouchTarget", 0f, flow);
            g.Dirty();
        }

        static void AuthorQuakeHandInput(bool head)
        {
            string side = head ? "Right" : "Left"; int stage = head ? 2 : 3;
            var token = Group(head ? "Başını koruyan el" : "Masaya tutunan el", interactions.transform);
            Shape("Elin küçük hareket halkası", PrimitiveType.Sphere, token.transform, Vector3.zero, Vector3.one * .07f, quakeCueMaterial);
            var hit = token.AddComponent<SphereCollider>(); hit.radius = .20f;
            var g = new YanYanaGraphAuthor(token, QuakeGraphPrefix + "eli sürükle ve temasını koru · " + side);
            object goal = head ? (object)g.Get(typeof(Transform), "position", headPalms["Ada"]) : anchors["Anchor_HoldAda"].position;
            var hand = InteractionHand("Ada", head);
            var frame = g.Add(new Unity.VisualScripting.LateUpdate());
            var visible = And(g, Is(g, g.Var("Phase", flow), 1), head ? (object)Is(g, g.Var("CoverStage", flow), stage) : Or(g, Is(g, g.Var("CoverStage", flow), stage), Is(g, g.Var("CoverStage", flow), 4)));
            var p = g.Set(frame.trigger, typeof(Transform), "position", token.transform, g.Get(typeof(Transform), "position", hand)); p = g.Set(p, typeof(Collider), "enabled", hit, visible);
            foreach (var renderer in token.GetComponentsInChildren<Renderer>()) p = g.Set(p, typeof(Renderer), "enabled", renderer, visible);
            var marker = Group(head ? "Başını koruma hedefi" : "Tutunma hedefi", token.transform.parent);
            var ring = marker.AddComponent<LineRenderer>(); ring.sharedMaterial = quakeCueMaterial; ring.useWorldSpace = false; ring.loop = true; ring.widthMultiplier = .012f; ring.positionCount = 40;
            ring.SetPositions(Enumerable.Range(0, 40).Select(i => new Vector3(Mathf.Cos(i * Mathf.PI / 20), Mathf.Sin(i * Mathf.PI / 20), 0) * .12f).ToArray());
            p = g.Set(p, typeof(Renderer), "enabled", ring, visible); p = g.Set(p, typeof(Transform), "position", marker.transform, goal); g.Set(p, typeof(Transform), "rotation", marker.transform, g.Get(typeof(Transform), "rotation", camera.transform));
            var screenGoal = g.Call(typeof(Camera), "WorldToScreenPoint", camera, new[] { typeof(Vector3) }, goal).result;
            var near = g.Binary<Less>(g.Call(typeof(Vector2), "Distance", null, new[] { typeof(Vector2), typeof(Vector2) }, g.Var("QScreen", flow), V2(g, g.Get(typeof(Vector3), "x", screenGoal), g.Get(typeof(Vector3), "y", screenGoal))).result, g.Binary<ScalarMultiply>(g.Get(typeof(Screen), "width"), .085f));
            var down = g.Add(new OnPointerDown()); g.Bind(down.target, token); var allowed = g.Branch(down.trigger, And(g, And(g, Available(g), visible), Is(g, g.Var("QPressed", flow), false)));
            p = g.SetVar(allowed.ifTrue, "QPressed", true, flow); p = g.SetVar(p, "QNearTime", 0f, flow); p = g.SetVar(p, "QPointer", g.Get(typeof(PointerEventData), "pointerId", down.data), flow); p = g.SetVar(p, "QScreen", g.Get(typeof(PointerEventData), "position", down.data), flow);
            if (!head) g.SetVar(p, "QGripPressed", Is(g, g.Var("CoverStage", flow), 4), flow);
            var owns = And(g, And(g, Available(g), visible), g.Var("QPressed", flow));
            var drag = g.Add(new OnDrag()); g.Bind(drag.target, token); allowed = g.Branch(drag.trigger, And(g, owns, QuakeOwns(g, drag.data)));
            var pointer = g.Get(typeof(PointerEventData), "position", drag.data);
            var point = g.Call(typeof(Camera), "ScreenToWorldPoint", camera, new[] { typeof(Vector3) }, V3(g, g.Get(typeof(Vector2), "x", pointer), g.Get(typeof(Vector2), "y", pointer), g.Get(typeof(Vector3), "z", screenGoal))).result;
            p = g.SetVar(allowed.ifTrue, "QScreen", pointer, flow); var moving = g.Branch(p, Is(g, g.Var("CoverStage", flow), stage)); g.SetVar(moving.ifTrue, "QHandTarget" + side, point, flow);
            var tick = g.Add(new Unity.VisualScripting.Update()); var atStage = g.Branch(tick.trigger, And(g, Is(g, g.Var("Paused", flow), false), visible)); var held = g.Branch(atStage.ifTrue, And(g, owns, near));
            var dwell = g.SetVar(held.ifTrue, "QNearTime", Sum(g, g.Var("QNearTime", flow), QuakeDelta(g)), flow); g.SetVar(held.ifFalse, "QNearTime", 0f, flow);
            if (!head)
            {
                // The same drag becomes the sustained grip; there is no release-and-next step.
                var seated = g.Branch(dwell, And(g, Is(g, g.Var("CoverStage", flow), 3), g.Binary<GreaterOrEqual>(g.Var("QNearTime", flow), .48f)));
                p = g.SetVar(seated.ifTrue, "CoverStage", 4, flow); p = g.SetVar(p, "QGripPressed", true, flow); p = g.SetVar(p, "QHeldTime", 0f, flow);
                p = g.SetVar(p, "QHandTargetLeft", goal, flow); p = g.SetVar(p, "CoverLeftAim", goal, flow);
                p = QuakeText(g, p, goalText, "Masa ayağını tut · sarsıntı boyunca bırakma"); p = QuakeText(g, p, gestureText, "Parmağını bırakma · masa ayağını tutmayı sürdür");
                QuakeText(g, p, lineText, "Ada: Efe, yanımdayız. Sarsıntı bitene kadar tutunalım.");
            }
            var drop = g.Add(new OnEndDrag()); g.Bind(drop.target, token); allowed = g.Branch(drop.trigger, And(g, And(g, owns, QuakeOwns(g, drop.data)), Is(g, g.Var("CoverStage", flow), stage)));
            var fit = g.Branch(allowed.ifTrue, And(g, near, g.Binary<GreaterOrEqual>(g.Var("QNearTime", flow), .48f)));
            p = g.SetVar(fit.ifTrue, "QPressed", false, flow); p = g.SetVar(p, "QNearTime", 0f, flow); p = g.SetVar(p, "CoverStage", stage + 1, flow); p = g.SetVar(p, "QHandTarget" + side, goal, flow); p = g.SetVar(p, "Cover" + side + "Aim", goal, flow);
            p = QuakeText(g, p, goalText, head ? "Diğer elinle masa ayağını kavra" : "Masa ayağını tut · sarsıntı boyunca bırakma"); p = QuakeText(g, p, gestureText, head ? "Diğer eli masa ayağındaki halkaya taşı · tut · bırak" : "Tutunan eline basılı tut · kayarsa halkaya getir"); QuakeText(g, p, lineText, head ? "Ada: Başım korunuyor. Şimdi sağlamca tutunacağım." : "Ada: Efe, yanımdayız. Sarsıntı bitene kadar tutunalım.");
            p = g.SetVar(fit.ifFalse, "QPressed", false, flow); g.SetVar(p, "QHandTarget" + side, g.Var("Cover" + side + "Rest", flow), flow);
            var up = g.Add(new OnPointerUp()); g.Bind(up.target, token); var release = g.Branch(up.trigger, QuakeOwns(g, up.data)); p = g.SetVar(release.ifTrue, "QGripPressed", false, flow);
            var noDrag = g.Branch(p, Is(g, g.Get(typeof(PointerEventData), "dragging", up.data), false)); g.SetVar(noDrag.ifTrue, "QPressed", false, flow);
            if (!head)
            {
                var gripDrop = g.Add(new OnEndDrag()); g.Bind(gripDrop.target, token);
                var releasedGrip = g.Branch(gripDrop.trigger, And(g, QuakeOwns(g, gripDrop.data), Is(g, g.Var("CoverStage", flow), 4)));
                p = g.SetVar(releasedGrip.ifTrue, "QPressed", false, flow); p = g.SetVar(p, "QGripPressed", false, flow); g.SetVar(p, "QHeldTime", 0f, flow);
                var holdTick = g.Add(new Unity.VisualScripting.Update());
                var holding = g.Branch(holdTick.trigger, And(g, QuakeInput(g, 4), And(g, g.Var("QGripPressed", flow), near)));
                p = g.SetVar(holding.ifTrue, "QHeldTime", Sum(g, g.Var("QHeldTime", flow), QuakeDelta(g)), flow);
                var complete = g.Branch(p, And(g, g.Binary<GreaterOrEqual>(g.Var("QHeldTime", flow), 7f), g.Binary<GreaterOrEqual>(g.Var("QClock", flow), 14f)));
                p = g.SetVar(complete.ifTrue, "QGripPressed", false, flow); g.Send(p, flow, "QuakeFinish");
                g.SetVar(holding.ifFalse, "QHeldTime", 0f, flow);
            }
            g.Dirty();
        }

        static void AuthorQuakeMotionAndAtmosphere()
        {
            var g = new YanYanaGraphAuthor(flow, QuakeGraphPrefix + "bedenler, kadraj ve sarsıntı");
            var update = g.Add(new Unity.VisualScripting.Update()); var active = And(g, And(g, Is(g, g.Var("Phase", flow), 1), Is(g, g.Var("Paused", flow), false)), Or(g, g.Var("Quake", flow), g.Var("QCompleting", flow))); var moving = g.Branch(update.trigger, active);
            var dt = QuakeDelta(g); var crouchSpeed = g.Add(new SelectUnit()); g.Bind(crouchSpeed.condition, g.Var("QCompleting", flow)); g.Bind(crouchSpeed.ifTrue, .9f); g.Bind(crouchSpeed.ifFalse, 2.2f);
            var p = g.SetVar(moving.ifTrue, "QCrouch", g.Call(typeof(Mathf), "MoveTowards", null, new[] { typeof(float), typeof(float), typeof(float) }, g.Var("QCrouch", flow), g.Var("QCrouchTarget", flow), g.Binary<ScalarMultiply>(dt, crouchSpeed.selection)).result, flow);
            var lowered = g.Call(typeof(Mathf), "SmoothStep", null, new[] { typeof(float), typeof(float), typeof(float) }, 0f, 1f, g.Var("QCrouch", flow)).result;
            var shelter = g.Call(typeof(Mathf), "Min", null, new[] { typeof(float), typeof(float) }, 1f, g.Binary<ScalarMultiply>(lowered, 1.35f)).result;
            p = g.SetVar(p, "QExitProgress", g.Call(typeof(Mathf), "MoveTowards", null, new[] { typeof(float), typeof(float), typeof(float) }, g.Var("QExitProgress", flow), g.Call(typeof(Convert), "ToSingle", null, OneBool, g.Var("QMovingOut", flow)).result, g.Binary<ScalarMultiply>(dt, .85f)).result, flow);
            foreach (string who in new[] { "Ada", "Efe" })
            {
                var animator = cast[who].GetComponentInChildren<Animator>();
                // The root crosses the last short distance only while the player lowers the body.
                object destination = g.Call(typeof(Vector3), "Lerp", null, new[] { typeof(Vector3), typeof(Vector3), typeof(float) }, g.Var("QStart" + who, flow), anchors["Anchor_Cover" + who].position, shelter).result;
                var rising = g.Branch(p, g.Var("QCompleting", flow));
                var position = g.Get(typeof(Transform), "position", cast[who].transform);
                var facing = g.Call(typeof(Quaternion), "Slerp", null, new[] { typeof(Quaternion), typeof(Quaternion), typeof(float) }, g.Var("QRotation" + who, flow), Quaternion.Euler(0, 180, 0), shelter).result;
                var during = g.Do(rising.ifFalse, who == "Ada" ? typeof(StoryPlayerMovement) : typeof(StorySiblingFollower), "SetAuthoredPose", who == "Ada" ? (object)movers["Ada"] : follower, new[] { typeof(Vector3), typeof(Quaternion) }, destination, facing);
                var exit = anchors["Anchor_Cover" + who].position + Vector3.back * 1.35f;
                var outPosition = g.Call(typeof(Vector3), "Lerp", null, new[] { typeof(Vector3), typeof(Vector3), typeof(float) }, anchors["Anchor_Cover" + who].position, exit, g.Call(typeof(Mathf), "SmoothStep", null, new[] { typeof(float), typeof(float), typeof(float) }, 0f, 1f, g.Var("QExitProgress", flow)).result).result;
                var leaving = g.Do(rising.ifTrue, who == "Ada" ? typeof(StoryPlayerMovement) : typeof(StorySiblingFollower), "SetAuthoredPose", who == "Ada" ? (object)movers["Ada"] : follower, new[] { typeof(Vector3), typeof(Quaternion) }, outPosition, Quaternion.Euler(0, 180, 0));
                var join = g.Add(new Unity.VisualScripting.Sequence { outputCount = 1 }); g.Link(during, join.enter); g.Link(leaving, join.enter); p = join.multiOutputs[0];
                p = g.Do(p, typeof(Animator), "SetFloat", animator, new[] { typeof(string), typeof(float) }, "QuakeCrouch", lowered);
                p = g.Set(p, typeof(Transform), "localPosition", animator.transform, g.Call(typeof(Vector3), "Lerp", null, new[] { typeof(Vector3), typeof(Vector3), typeof(float) }, idleOffsets[who], coverOffsets[who], lowered).result);
            }
            p = g.SetVar(p, "QAdultCrouch", g.Call(typeof(Mathf), "MoveTowards", null, new[] { typeof(float), typeof(float), typeof(float) }, g.Var("QAdultCrouch", flow), g.Call(typeof(Convert), "ToSingle", null, OneBool, Is(g, g.Var("QCompleting", flow), false)).result, g.Binary<ScalarMultiply>(dt, 1.3f)).result, flow);
            foreach (string who in new[] { "Derya", "Emre" })
            {
                var animator = cast[who].GetComponentInChildren<Animator>(); p = g.Do(p, typeof(Animator), "SetFloat", animator, new[] { typeof(string), typeof(float) }, "QuakeCrouch", g.Var("QAdultCrouch", flow));
                p = g.Set(p, typeof(Transform), "localPosition", animator.transform, g.Call(typeof(Vector3), "Lerp", null, new[] { typeof(Vector3), typeof(Vector3), typeof(float) }, idleOffsets[who], coverOffsets[who], g.Var("QAdultCrouch", flow)).result);
            }
            var lowStage = g.Branch(p, Is(g, g.Var("CoverStage", flow), 1));
            var lowHeld = g.Branch(lowStage.ifTrue, And(g, g.Var("QPressed", flow), g.Binary<GreaterOrEqual>(g.Var("QCrouch", flow), .97f)));
            g.SetVar(lowHeld.ifTrue, "QNearTime", Sum(g, g.Var("QNearTime", flow), dt), flow); g.SetVar(lowHeld.ifFalse, "QNearTime", 0f, flow);
            var animate = g.Add(new Unity.VisualScripting.LateUpdate()); var free = g.Branch(animate.trigger, active); p = free.ifTrue;
            foreach (string side in new[] { "Right", "Left" }) p = g.SetVar(p, "Cover" + side + "Aim", g.Call(typeof(Vector3), "Lerp", null, new[] { typeof(Vector3), typeof(Vector3), typeof(float) }, g.Var("Cover" + side + "Aim", flow), g.Var("QHandTarget" + side, flow), g.Call(typeof(Mathf), "Clamp01", null, OneFloat, g.Binary<ScalarMultiply>(dt, 16f)).result).result, flow);
            var clock = g.Branch(p, g.Var("Quake", flow)); g.SetVar(clock.ifTrue, "QClock", Sum(g, g.Var("QClock", flow), dt), flow);
            // Local camera rig movement affects both shots without moving the interaction targets.
            var shake = g.Branch(g.Add(new Unity.VisualScripting.LateUpdate()).trigger, And(g, Is(g, g.Var("Phase", flow), 1), And(g, g.Var("Quake", flow), Is(g, g.Var("ReducedMotion", flow), false))));
            var shakeUnpaused = g.Branch(shake.ifTrue, Is(g, g.Var("Paused", flow), false));
            object Wave(float frequency, float magnitude) => g.Binary<ScalarMultiply>(g.Call(typeof(Mathf), "Sin", null, OneFloat, g.Binary<ScalarMultiply>(g.Var("QClock", flow), frequency)).result, magnitude);
            p = g.Set(shakeUnpaused.ifTrue, typeof(Transform), "localPosition", quakeCameraRig, V3(g, Sum(g, Wave(17.8f, .027f), Wave(28.1f, .013f)), Wave(21.3f, .015f), Wave(12.9f, .009f)));
            g.Set(p, typeof(Transform), "localRotation", quakeCameraRig, g.Call(typeof(Quaternion), "Euler", null, new[] { typeof(Vector3) }, V3(g, Wave(11.5f, .22f), Wave(13.1f, .18f), Wave(18.7f, .34f))).result);
            p = g.Set(shake.ifFalse, typeof(Transform), "localPosition", quakeCameraRig, Vector3.zero); g.Set(p, typeof(Transform), "localRotation", quakeCameraRig, Quaternion.identity);
            var sound = QuakeFind("Deprem oda ayrıntıları").GetComponent<AudioSource>();
            var sounding = g.Branch(g.Add(new Unity.VisualScripting.Update()).trigger, And(g, active, And(g, g.Var("Quake", flow), g.Var("Sound", flow)))); var stopped = g.Branch(sounding.ifTrue, Is(g, g.Get(typeof(AudioSource), "isPlaying", sound), false)); g.Do(stopped.ifTrue, typeof(AudioSource), "Play", sound, NoArgs); g.Do(sounding.ifFalse, typeof(AudioSource), "Pause", sound, NoArgs);
            var lamp = QuakeFind("Sallanan tavan lambası").GetComponent<Animation>(); var lamping = g.Branch(g.Add(new Unity.VisualScripting.Update()).trigger, And(g, Is(g, g.Var("Phase", flow), 1), g.Var("Quake", flow))); var still = g.Branch(lamping.ifTrue, Is(g, g.Get(typeof(Animation), "isPlaying", lamp), false)); g.Do(still.ifTrue, typeof(Animation), "Play", lamp, OneString, lamp.clip.name); g.Do(lamping.ifFalse, typeof(Animation), "Stop", lamp, NoArgs);
            // Always display the actual continuous gesture, including when the old generic hint graph runs.
            var ui = g.Branch(g.Add(new Unity.VisualScripting.LateUpdate()).trigger, active); var stages = g.Add(new SwitchOnInteger { options = new List<int> { 1, 2, 3, 4 } }); g.Bind(stages.selector, g.Var("CoverStage", flow)); g.Link(ui.ifTrue, stages.enter);
            var quake = Is(g, g.Var("Phase", flow), 1);
            var siblingWork = Is(g, g.Var("Workspace", flow), "sibling");
            var checkingSibling = And(g, Is(g, g.Var("Phase", flow), 2), Or(g, siblingWork, And(g, Is(g, g.Var("SiblingChecked", flow), 0), Is(g, g.Var("Workspace", flow), ""))));
            var authoredLabels = Or(g, quake, checkingSibling);
            var supportUI = g.Branch(g.Add(new Unity.VisualScripting.LateUpdate()).trigger, checkingSibling);
            var supportText = g.Set(supportUI.ifTrue, typeof(TMP_Text), "text", quakeLabels[chapterText], "DEPREM SONRASI");
            supportText = g.Set(supportText, typeof(TMP_Text), "text", quakeLabels[goalText], "Efe’yi kontrol et · elini tut");
            var supportGesture = g.Add(new SelectUnit()); g.Bind(supportGesture.condition, siblingWork);
            g.Bind(supportGesture.ifTrue, "Elini Efe’nin eline sürükle · temas edince bırak"); g.Bind(supportGesture.ifFalse, "Efe’nin yanına git · onu kontrol et");
            supportText = g.Set(supportText, typeof(TMP_Text), "text", quakeLabels[gestureText], supportGesture.selection);
            var supportTalk = g.Add(new SelectUnit()); g.Bind(supportTalk.condition, siblingWork);
            g.Bind(supportTalk.ifTrue, "Efe: İyiyim. Biraz korktum. Elimi tutar mısın?"); g.Bind(supportTalk.ifFalse, "Ada: Sarsıntı durdu. Efe, iyi misin?");
            var supportDone = g.Add(new SelectUnit()); g.Bind(supportDone.condition, Is(g, g.Var("SiblingChecked", flow), 1));
            g.Bind(supportDone.ifTrue, "Efe: Birlikteyiz. Çıkış yoluna beraber bakalım."); g.Bind(supportDone.ifFalse, supportTalk.selection);
            g.Set(supportText, typeof(TMP_Text), "text", quakeLabels[lineText], supportDone.selection);
            var texts = new[] { "Ada’yı tut · aşağı sürükle · alçakta bırak", "Elini başındaki halkaya sürükle · orada tut", "Diğer eli masa ayağına taşı · parmağını bırakma", "Parmağını bırakma · sarsıntı bitene kadar tutun" };
            for (int i = 0; i < 4; i++) QuakeText(g, stages.branches[i].Value, gestureText, texts[i]);
            // Hide only the old source label objects while these quake labels are shown.
            // Their generated icons/captions are children and therefore hide with them.
            var hudTick = g.Add(new Unity.VisualScripting.LateUpdate()); p = hudTick.trigger;
            p = g.Active(p, quakeTouch.gameObject, And(g, quake, And(g, g.Var("QPressed", flow), Is(g, g.Var("Paused", flow), false))));
            p = g.Set(p, typeof(Transform), "position", quakeTouch, V3(g, g.Get(typeof(Vector2), "x", g.Var("QScreen", flow)), g.Get(typeof(Vector2), "y", g.Var("QScreen", flow)), 0f));
            var quietCue = g.Branch(g.Add(new Unity.VisualScripting.LateUpdate()).trigger, Or(g, quake, siblingWork));
            g.Active(quietCue.ifTrue, QuakeFind("On saniyede dikkat, yirmi beşte hareket").gameObject, false);
            var engaged = g.Branch(g.Add(new Unity.VisualScripting.LateUpdate()).trigger, And(g, quake, g.Var("QPressed", flow)));
            var attention = g.SetVar(engaged.ifTrue, "IdleSeconds", 0f, flow);
            attention = g.SetVar(attention, "HintLevel", 0, flow); g.Active(attention, hintPanel, false);
            // The standing capsule must not sit in front of the child's crouched hand targets.
            // Restore actor hit volumes immediately after the quake for sibling/exit interaction.
            foreach (string who in new[] { "Ada", "Efe" }) foreach (var collider in cast[who].GetComponents<Collider>())
                p = g.Set(p, typeof(Collider), "enabled", collider, Is(g, And(g, quake, g.Binary<GreaterOrEqual>(g.Var("CoverStage", flow), 2)), false));
            var aspect = g.Call(typeof(Mathf), "Max", null, new[] { typeof(float), typeof(float) }, 1f, g.Binary<ScalarMultiply>(g.Binary<ScalarDivide>(g.Get(typeof(Screen), "height"), g.Get(typeof(Screen), "width")), 9f / 16f)).result;
            foreach (var view in new[] { quakeWide, quakeHands })
            {
                var lensSet = g.Add(new SetMember(new Member(typeof(LensSettings), "FieldOfView")) { chainable = true });
                g.Bind(lensSet.target, g.Get(typeof(CinemachineCamera), "Lens", view));
                g.Bind(lensSet.input, g.Binary<ScalarMultiply>(g.Call(typeof(Mathf), "Atan", null, OneFloat, g.Binary<ScalarMultiply>(Mathf.Tan(view.Lens.FieldOfView * .5f * Mathf.Deg2Rad), aspect)).result, 2f * Mathf.Rad2Deg));
                g.Link(p, lensSet.assign); p = g.Set(lensSet.assigned, typeof(CinemachineCamera), "Lens", view, lensSet.targetOutput);
            }
            foreach (var contact in QuakeAll<ScriptMachine>().Where(m => m.graph.title == "Başını koruyan el ve gerçek tutunma teması"))
                p = g.Set(p, typeof(Behaviour), "enabled", contact, Is(g, And(g, quake, g.Var("QCompleting", flow)), false));
            foreach (var pair in quakeLabels)
            {
                p = g.Active(p, pair.Key.gameObject, Is(g, authoredLabels, false));
                p = g.Active(p, pair.Value.gameObject, pair.Key == lineText ? (object)And(g, authoredLabels, g.Var("Captions", flow)) : authoredLabels);
            }
            p = g.Active(p, quakeProgress, And(g, quake, Is(g, g.Var("Paused", flow), false)));
            var progress = g.Add(new SelectUnit()); g.Bind(progress.condition, Is(g, g.Var("CoverStage", flow), 1));
            g.Bind(progress.ifTrue, g.Var("QCrouch", flow));
            var handsProgress = g.Add(new SelectUnit()); g.Bind(handsProgress.condition, Is(g, g.Var("CoverStage", flow), 4));
            g.Bind(handsProgress.ifTrue, g.Binary<ScalarDivide>(g.Var("QHeldTime", flow), 7f)); g.Bind(handsProgress.ifFalse, g.Binary<ScalarDivide>(g.Var("QNearTime", flow), .48f));
            g.Bind(progress.ifFalse, handsProgress.selection);
            p = g.Set(p, typeof(Transform), "localScale", quakeProgressFill, V3(g, g.Call(typeof(Mathf), "Clamp01", null, OneFloat, progress.selection).result, 1f, 1f));
            var pictureRoot = g.Call(typeof(GameObject), "Find", null, OneString, "Reading-free 3D presentation").result;
            var foundRoot = g.Branch(p, g.Binary<NotEqual>(pictureRoot, null));
            var pictureGuide = g.Call(typeof(Transform), "Find", g.Get(typeof(GameObject), "transform", pictureRoot), OneString, "Immediate picture guide").result;
            var foundGuide = g.Branch(foundRoot.ifTrue, g.Binary<NotEqual>(pictureGuide, null)); g.Do(foundGuide.ifTrue, typeof(GameObject), "SetActive", g.Get(typeof(Transform), "gameObject", pictureGuide), OneBool, Is(g, Or(g, quake, siblingWork), false));
            g.Dirty();
        }
    }
}
