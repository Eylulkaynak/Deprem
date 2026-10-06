// Editor-only import, rendering, and live graph verification.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static class YanYanaResidentWorkshopQA
    {
        const string Output = "ClientExports/YanYana/ArtReview/ResidentWorkshop";
        const string Report = "ClientExports/YanYana/Reports/resident-workshop-play.txt";
        static IEnumerator<object> run;
        static readonly Dictionary<string, float> blinkMax = new Dictionary<string, float>();
        static bool reviewBindPose;

        [MenuItem("Tools/Yan Yana/QA/Restart Resident Review Audio")]
        public static void RestartAudio()
        {
            var before = AudioSettings.dspTime;
            bool restarted = AudioSettings.Reset(AudioSettings.GetConfiguration());
            InspectAudio();
            File.AppendAllText("ClientExports/YanYana/Reports/resident-audio-diagnostics.txt", "audioReset=" + restarted + " previousDsp=" + before + "\n");
        }

        [MenuItem("Tools/Yan Yana/QA/Inspect Resident Audio")]
        public static void InspectAudio()
        {
            var info = new List<string> { "dspTime=" + AudioSettings.dspTime, "listenerPaused=" + AudioListener.pause, "masterMuted=" + EditorUtility.audioMasterMute, "sampleRate=" + AudioSettings.outputSampleRate };
            foreach (var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                info.Add("listener=" + listener.name + " active=" + listener.isActiveAndEnabled);
            foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(s => s.name == "Kısa Türkçe konuşmalar"))
                info.Add("voice=" + source.name + " active=" + source.isActiveAndEnabled + " time=" + source.time + " playing=" + source.isPlaying + " clip=" + source.clip?.name + " loaded=" + source.clip?.loadState);
            var audioUtil = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil != null)
                foreach (var property in audioUtil.GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.PropertyType == typeof(bool)))
                    try { info.Add(property.Name + "=" + property.GetValue(null)); } catch (Exception) { }
            File.WriteAllLines("ClientExports/YanYana/Reports/resident-audio-diagnostics.txt", info);
        }

        [MenuItem("Tools/Yan Yana/QA/Render Workshop Bind Pose")]
        public static void RenderBindPose()
        {
            reviewBindPose = true; try { Render(); } finally { reviewBindPose = false; }
        }
        static void Check(bool ok, string label)
        {
            File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + label + "\n");
            if (!ok) throw new InvalidOperationException(label);
        }
        static float Weight(SkinnedMeshRenderer skin, string key) => skin.GetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(key));

        [MenuItem("Tools/Yan Yana/QA/Inspect Workshop Import Dimensions")]
        public static void Dimensions()
        {
            var lines = new List<string>(); var actor = UnityEngine.Object.Instantiate(YanYanaResidentWorkshop.Prefab("Eren"));
            try
            {
                var anim = actor.GetComponentInChildren<Animator>();
                lines.Add("Before animator: root=" + actor.transform.localScale + " humanScale=" + anim.humanScale);
                foreach (var t in actor.GetComponentsInChildren<Transform>()) lines.Add(t.name + " local=" + t.localPosition + " scale=" + t.localScale + " world=" + t.position);
                anim.Rebind(); anim.Update(0);
                foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = new Mesh(); skin.BakeMesh(mesh); var b = mesh.bounds;
                    lines.Add(skin.name + " lossy=" + skin.transform.lossyScale + " bounds=" + skin.bounds + " baked=" + b + " material=" + string.Join(",", skin.sharedMaterials.Select(m => m.name))); UnityEngine.Object.DestroyImmediate(mesh);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(actor); }
            File.WriteAllLines("ClientExports/YanYana/Reports/resident-workshop-dimensions.txt", lines);
        }

        [MenuItem("Tools/Yan Yana/Art/Render Resident Workshop")]
        public static void Render()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode for the isolated asset review.");
            Directory.CreateDirectory(Output);
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var cg = new GameObject("Resident review camera"); SceneManager.MoveGameObjectToScene(cg, scene);
                var camera = cg.AddComponent<Camera>(); camera.cameraType = CameraType.Preview; camera.scene = scene;
                camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.83f, .87f, .83f);
                camera.nearClipPlane = .05f; camera.farClipPlane = 40;
                foreach (var spec in new[] { (new Vector3(30, -35, 0), 1.05f, new Color(1, .95f, .88f)), (new Vector3(20, 150, 0), .60f, new Color(.76f, .88f, 1)) })
                {
                    var lg = new GameObject("Review light"); SceneManager.MoveGameObjectToScene(lg, scene);
                    var light = lg.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = spec.Item2; light.color = spec.Item3; light.transform.rotation = Quaternion.Euler(spec.Item1);
                }
                var people = new List<GameObject>(); var names = new[] { "Eren", "Deniz", "Aylin", "Zeynep", "Gul", "Kemal", "Ece", "Arda" };
                for (int i = 0; i < names.Length; i++)
                {
                    var actor = UnityEngine.Object.Instantiate(YanYanaResidentWorkshop.Prefab(names[i])); SceneManager.MoveGameObjectToScene(actor, scene);
                    actor.name = names[i]; actor.transform.position = new Vector3((i - 3.5f) * 1.10f, 0, 0); actor.transform.rotation = Quaternion.Euler(0, 180, 0);
                    var animator = actor.GetComponentInChildren<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    if (reviewBindPose) animator.enabled = false; else { animator.Rebind(); animator.Update(0); }
                    YanYanaResidentWorkshop.Ground(actor, 0); people.Add(actor);
                }
                camera.transform.position = new Vector3(0, 1.8f, -8); camera.transform.LookAt(new Vector3(0, .85f, 0)); camera.orthographicSize = 1.63f;
                Capture(camera, 2560, 960, Output + "/Unity_Lineup.png");
                foreach (var actor in people)
                {
                    foreach (var other in people) other.SetActive(other == actor);
                    var animator = actor.GetComponentInChildren<Animator>(); var skin = YanYanaResidentWorkshop.Face(actor);
                    var head = animator.GetBoneTransform(HumanBodyBones.Head);
                    var lower = head.position.y - .09f;
                    var headPoints = new List<Vector3>();
                    foreach (var part in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var snapshot = new Mesh(); part.BakeMesh(snapshot);
                        headPoints.AddRange(snapshot.vertices.Select(v => part.transform.TransformPoint(v)).Where(v => v.y > lower));
                        UnityEngine.Object.DestroyImmediate(snapshot);
                    }
                    float upper = headPoints.Max(v => v.y) + .035f;
                    float width = headPoints.Max(v => v.x) - headPoints.Min(v => v.x);
                    var center = new Vector3(head.position.x, (lower + upper) * .5f, head.position.z);
                    camera.transform.position = center + new Vector3(.035f, .015f, -3); camera.transform.LookAt(center);
                    camera.orthographicSize = Mathf.Max((upper-lower)*.5f, width/(2f*.9f)) * 1.10f;
                    foreach (string pose in new[] { "Neutral", "Speaking", "Blink", "Fear", "Surprise", "Smile" })
                    {
                        foreach (string key in YanYanaResidentWorkshop.ShapeNames) skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(key), 0);
                        if (pose != "Neutral") skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(pose == "Speaking" ? "Mouth_A" : pose), pose == "Blink" ? 100 : 70);
                        // Explicitly bake each morph for this synchronous Editor render;
                        // the preview scene does not advance a player frame between poses.
                        var baked = new Mesh(); skin.BakeMesh(baked);
                        var posed = new GameObject("Review morph snapshot"); SceneManager.MoveGameObjectToScene(posed, scene);
                        posed.transform.SetParent(skin.transform, false); posed.AddComponent<MeshFilter>().sharedMesh = baked;
                        posed.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials; skin.enabled = false;
                        try { Capture(camera, 540, 600, Output + "/" + actor.name + "_" + pose + ".png"); }
                        finally { skin.enabled = true; UnityEngine.Object.DestroyImmediate(posed); UnityEngine.Object.DestroyImmediate(baked); }
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Debug.Log("RESIDENT_WORKSHOP_RENDERED: imported Unity materials, rigs, and facial shapes.");
        }

        public static void Capture(Camera camera, int width, int height, string path)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32); var previous = RenderTexture.active; Texture2D image = null;
            try
            {
                camera.targetTexture = rt; camera.aspect = width / (float)height; camera.Render(); RenderTexture.active = rt;
                image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); if (image) UnityEngine.Object.DestroyImmediate(image); }
        }

        [MenuItem("Tools/Yan Yana/QA/Play Resident Workshop Faces")]
        public static void PlayFaces()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            Application.runInBackground = true;
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllText(Report, "Native Visual Scripting + imported Blender blend shapes, checked during Play Mode.\n");
            blinkMax.Clear(); run = Sequence().GetEnumerator(); EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            try
            {
                foreach (var actor in YanYanaResidentWorkshop.Residents())
                {
                    var skin = YanYanaResidentWorkshop.Face(actor.gameObject); if (!skin) continue;
                    string n = YanYanaResidentWorkshop.Identity(actor.name); blinkMax[n] = Mathf.Max(blinkMax.TryGetValue(n, out var m) ? m : 0, Weight(skin, "Blink"));
                }
                if (!run.MoveNext()) { EditorApplication.update -= Tick; File.AppendAllText(Report, "PASS completed\n"); }
            }
            catch (Exception e) { EditorApplication.update -= Tick; File.AppendAllText(Report, "FAIL " + e + "\n"); Debug.LogException(e); }
        }
        static IEnumerable<object> Wait(float seconds)
        {
            double until = EditorApplication.timeSinceStartup + seconds; int frame = Time.frameCount;
            while (EditorApplication.timeSinceStartup < until || Time.frameCount < frame + 2) yield return null;
        }
        static IEnumerable<object> Sequence()
        {
            var residents = YanYanaResidentWorkshop.Residents();
            var cast = residents.Where(r => YanYanaResidentWorkshop.Names.Contains(YanYanaResidentWorkshop.Identity(r.name))).ToDictionary(r => YanYanaResidentWorkshop.Identity(r.name), r => r.gameObject);
            var faces = cast.ToDictionary(kv => kv.Key, kv => YanYanaResidentWorkshop.Face(kv.Value));
            var flow = SceneManager.GetActiveScene().GetRootGameObjects().First(x => x.name.StartsWith("01 Akış")); var vars = Variables.Object(flow);
            var voice = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<AudioSource>(true)).First(x => x.name == "Kısa Türkçe konuşmalar");
            Time.timeScale = 1; AudioListener.pause = false; vars.Set("Paused", false); vars.Set("Busy", true); vars.Set("Phase", 6); vars.Set("Workspace", "");
            foreach (string key in new[] { "GasNoticed", "GasRetreated", "ParkChildNoticed", "ParkChildAsked", "ParkChildHelped" }) vars.Set(key, 0);
            voice.Stop();
            foreach (var f in Wait(.3f)) yield return f;
            Check(residents.Length == 22 && cast.Count == 8, "22 resident roots retained; eight workshop characters installed");
            Check(faces.Values.Select(s => s.sharedMesh).Distinct().Count() == 8, "eight distinct facial meshes");
            foreach (var pair in cast)
            {
                var animator = pair.Value.GetComponentInChildren<Animator>();
                Check(animator && animator.avatar && animator.avatar.isValid && animator.avatar.isHuman, pair.Key + " humanoid valid in Play Mode");
                Check(pair.Value.GetComponents<ScriptMachine>().Count(m => m.graph?.title == YanYanaResidentWorkshop.FaceGraph) == 1, pair.Key + " one serialized facial graph");
            }
            foreach (var line in YanYanaResidentWorkshop.ReadSpeech().lines.Where(l => faces.ContainsKey(l.actor)))
            {
                var audio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/YanYana/Audio/Physical/" + line.audio + ".wav");
                Check(audio, "audio asset " + line.audio);
                voice.clip = audio; voice.Play(); float maximum = 0; int firstFrame = Time.frameCount; double until = EditorApplication.timeSinceStartup + 1.5;
                // Aylin's turn follows Ece in the shared reunion recording.
                if (line.actor == "Aylin") voice.time = 1.7f;
                while (EditorApplication.timeSinceStartup < until)
                {
                    maximum = Mathf.Max(maximum, Weight(faces[line.actor], "Mouth_A") + Weight(faces[line.actor], "Mouth_E") + Weight(faces[line.actor], "Mouth_O")); yield return null;
                }
                Check(maximum > 2, line.actor + " mouth follows " + line.audio + " peak=" + maximum.ToString("F1") + " frames=" + (Time.frameCount-firstFrame) + " playing=" + voice.isPlaying + " clip=" + voice.clip?.name + " time=" + voice.time);
                if (line.actor == "Deniz")
                {
                    voice.Pause(); foreach (var f in Wait(.15f)) yield return f;
                    Check(Weight(faces[line.actor], "Mouth_A") < .01f && Weight(faces[line.actor], "Mouth_E") < .01f && Weight(faces[line.actor], "Mouth_O") < .01f, "paused speech closes mouth");
                    voice.UnPause(); foreach (var f in Wait(.15f)) yield return f;
                }
                voice.Stop(); foreach (var f in Wait(.12f)) yield return f;
                Check(faces.All(kv => Weight(kv.Value, "Mouth_A") < .01f && Weight(kv.Value, "Mouth_E") < .01f && Weight(kv.Value, "Mouth_O") < .01f), "speech stop resets every mouth");
            }
            vars.Set("Phase", 1); foreach (var f in Wait(.2f)) yield return f;
            Check(faces.All(kv => Weight(kv.Value, "Fear") > 40), "quake phase drives fear on all eight faces");
            vars.Set("Phase", 6); vars.Set("ParkChildNoticed", 1); vars.Set("ParkChildAsked", 1); vars.Set("GasNoticed", 1); foreach (var f in Wait(.2f)) yield return f;
            Check(Weight(faces["Ece"], "Fear") > 50 && Weight(faces["Aylin"], "Surprise") > 30 && Weight(faces["Eren"], "Surprise") > 40, "lost child, mother and gas report drive distinct reactions");
            vars.Set("ParkChildHelped", 1); vars.Set("GasRetreated", 1); foreach (var f in Wait(.2f)) yield return f;
            Check(Weight(faces["Ece"], "Fear") == 0 && Weight(faces["Ece"], "Smile") > 60 && Weight(faces["Aylin"], "Smile") > 60, "family reunion replaces worry with relief");
            foreach (string name in YanYanaResidentWorkshop.Names) Check(blinkMax.TryGetValue(name, out var peak) && peak > 45, name + " automatic blink observed");
        }
    }
}
