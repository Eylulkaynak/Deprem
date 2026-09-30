using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;
using Deprem.Minigames;
using System.Collections.Generic;

namespace YanYana.Editor
{
    public static class YanYanaQA
    {
        const string Reports = "ClientExports/YanYana/Reports/";
        static GameObject Main => SceneManager.GetActiveScene().GetRootGameObjects().First(x => x.name.StartsWith("01 Akış"));
        static string Current => (string)Variables.Object(Main).Get("Current");
        static string errorLog = "";
        static YYCampaign testCampaign;
        static double lastActionTime;
        static string lastActionBeat;
        static int route;
        static readonly List<string> routeNotes = new List<string>();
        static double routeStarted;
        static bool nextRoute;
        static int actionCount;

        [MenuItem("Tools/Yan Yana/QA/Play")]
        public static void Play()
        {
            if (SceneManager.GetActiveScene().path != YanYanaAdventureBuilder.ScenePath) throw new InvalidOperationException("Only the new adventure can be tested.");
            Directory.CreateDirectory(Reports); errorLog = ""; Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
            SetGameView(540, 960); EditorApplication.isPlaying = true;
        }
        [MenuItem("Tools/Yan Yana/QA/Stop")]
        public static void Stop() { EditorApplication.isPlaying = false; }
        [MenuItem("Tools/Yan Yana/QA/Run Four Routes")]
        public static void RunFourRoutes()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter play mode first.");
            testCampaign = JsonUtility.FromJson<YYCampaign>(File.ReadAllText(YanYanaAdventureBuilder.Root + "Content/Campaign.json"));
            route = 0; routeNotes.Clear(); actionCount = 0; nextRoute = false; BeginRoute();
            EditorApplication.update -= AutoTick; EditorApplication.update += AutoTick;
        }
        [MenuItem("Tools/Yan Yana/QA/Stop Route Test")]
        public static void StopRouteTest() { EditorApplication.update -= AutoTick; }
        static void BeginRoute()
        {
            lastActionBeat = ""; routeStarted = EditorApplication.timeSinceStartup; lastActionTime = routeStarted;
            CustomEvent.Trigger(Main, "NewAdventure");
        }
        static void AutoTick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= AutoTick; return; }
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (nextRoute)
                {
                    if (now - lastActionTime < 2) return;
                    nextRoute = false; BeginRoute(); return;
                }
                var variables = Variables.Object(Main);
                if (Current == "finish") { EndRoute(now); return; }
                if ((bool)variables.Get("Paused") || (bool)variables.Get("Busy"))
                {
                    if (now - lastActionTime > 20) throw new Exception("Input gate stayed closed: " + Current);
                    return;
                }
                string id = Current;
                if (id == lastActionBeat)
                {
                    if (now - lastActionTime > 14) throw new Exception("Gesture did not complete: " + id);
                    return;
                }
                var beat = testCampaign.beats.First(b => b.id == id);
                if (beat.actions.Length == 0) return;
                int index = 0;
                if (id == "street_hazard" && route != 0) index = 1;
                if (id == "plan_choice" && route >= 2) index = 1;
                if (id == "neighbor_choice" && route == 3) index = 1;
                if (route == 3 && new[] { "flashlight_choice", "radio_choice", "water_find", "aid_choice", "exit_choice", "shelf_choice", "wardrobe_choice" }.Contains(id)) index = 1;
                var action = beat.actions[index];
                lastActionBeat = id; lastActionTime = now; actionCount++;
                File.WriteAllText(Reports + "route-progress.txt", "Route=" + route + "\nBeat=" + id + "\nGesture=" + action.gesture + "\nActions=" + actionCount + "\n");
                if (id == "bag_open" || id == "crouch" || id == "fire_group_1" || id == "bora_role" || id.StartsWith("ending_") && id.EndsWith("_1")) Capture();
                if (action.gesture == "spray")
                {
                    // Backend integration only. This does not claim physical touchscreen validation.
                    var manager = UnityEngine.Object.FindObjectsByType<FirefighterExtinguishManager>(FindObjectsSortMode.None).Single(x => x.gameObject.activeInHierarchy);
                    var bindings = (FireTargetBinding[])typeof(FirefighterExtinguishManager).GetField("fires", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
                    var apply = typeof(FirefighterExtinguishManager).GetMethod("ApplyWater", BindingFlags.Instance | BindingFlags.NonPublic);
                    for (int i = 0; i < bindings.Length; i++) apply.Invoke(manager, new object[] { bindings[i].hitCollider.bounds.center, i, 4f });
                }
                else Gesture(index);
            }
            catch (Exception ex)
            {
                routeNotes.Add("FAIL route=" + route + " " + ex); WriteRoutes(); EditorApplication.update -= AutoTick; Debug.LogException(ex);
            }
        }
        static void EndRoute(double now)
        {
            int ending = (int)Variables.Object(Main).Get("Ending"); int expected = route + 1;
            routeNotes.Add((ending == expected ? "PASS" : "FAIL") + " route=" + route + " ending=" + ending + " expected=" + expected + " editorSeconds=" + (now - routeStarted).ToString("F2") + " actions=" + actionCount);
            Capture(); WriteRoutes(); route++;
            if (route >= 4) { EditorApplication.update -= AutoTick; Debug.Log("YAN YANA FOUR ROUTE TEST COMPLETE"); return; }
            lastActionTime = now; nextRoute = true;
            EditorSceneManager.LoadSceneInPlayMode(YanYanaAdventureBuilder.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        }
        static void WriteRoutes()
        {
            File.WriteAllText(Reports + "four-route-integration.txt", "Automated editor integration. Pointer gestures dispatched through EventSystem. Fire resolution exercised through existing manager backend; real touch and child timing remain untested.\n" + string.Join("\n", routeNotes));
        }
        static void Log(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            errorLog += type + ": " + message + "\n" + stack + "\n"; File.WriteAllText(Reports + "play-errors.txt", errorLog);
        }
        [MenuItem("Tools/Yan Yana/QA/New Adventure")]
        public static void NewAdventure() { CustomEvent.Trigger(Main, "NewAdventure"); }
        [MenuItem("Tools/Yan Yana/QA/State")]
        public static void State()
        {
            var variables = Variables.Object(Main);
            string text = "Current=" + variables.Get("Current") + "\nBusy=" + variables.Get("Busy") + "\nPaused=" + variables.Get("Paused") + "\nRole=" + variables.Get("Role") + "\nStation=" + variables.Get("Station") + "\n";
            foreach (var obj in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Animator>(true))) text += obj.name + " avatar=" + (obj.avatar && obj.avatar.isValid) + " human=" + (obj.avatar && obj.avatar.isHuman) + "\n";
            File.WriteAllText(Reports + "live-state.txt", text); Debug.Log("YAN YANA QA " + text.Substring(0, Math.Min(180, text.Length)));
        }
        [MenuItem("Tools/Yan Yana/QA/Capture")]
        public static void Capture()
        {
            Directory.CreateDirectory("ClientExports/YanYana/Screenshots");
            int width = 540, height = 960;
            var cam = Camera.main; var render = new RenderTexture(width, height, 24); var previous = cam.targetTexture;
            var canvases = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Canvas>(true)).Where(x => x.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = .4f; }
            cam.targetTexture = render; Canvas.ForceUpdateCanvases(); cam.Render();
            var active = RenderTexture.active; RenderTexture.active = render; var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes("ClientExports/YanYana/Screenshots/" + (EditorApplication.isPlaying ? Current : "editor") + ".png", texture.EncodeToPNG());
            RenderTexture.active = active; cam.targetTexture = previous; foreach (var canvas in canvases) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(render);
        }
        [MenuItem("Tools/Yan Yana/QA/First Gesture")]
        public static void FirstGesture() { Gesture(0); }
        [MenuItem("Tools/Yan Yana/QA/Second Gesture")]
        public static void SecondGesture() { Gesture(1); }
        public static void Gesture(int index)
        {
            var campaign = JsonUtility.FromJson<YYCampaign>(File.ReadAllText(YanYanaAdventureBuilder.Root + "Content/Campaign.json")); var beat = campaign.beats.First(x => x.id == Current); var action = beat.actions[index];
            var stage = (GameObject)Variables.Object(Main).Get("ActiveStage"); var target = stage.transform.Cast<Transform>().First(x => x.name.StartsWith(index + " · ")).gameObject;
            var screen = (Vector2)Camera.main.WorldToScreenPoint(target.transform.position);
            var data = new PointerEventData(EventSystem.current) { position = screen, pressPosition = screen, button = PointerEventData.InputButton.Left, pointerId = -1 };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
            if (action.gesture == "tap" || action.gesture == "approach")
            {
                ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
            }
            else if (action.gesture == "drag" || action.gesture == "swipe")
            {
                ExecuteEvents.Execute(target, data, ExecuteEvents.beginDragHandler);
                data.position = action.gesture == "drag" ? (Vector2)Camera.main.WorldToScreenPoint(stage.transform.Cast<Transform>().First(x => x.name.StartsWith("Bırakma hedefi")).position) : screen + Vector2.down * Screen.height * .16f;
                data.delta = data.position - screen; data.dragging = true;
                ExecuteEvents.Execute(target, data, ExecuteEvents.dragHandler); ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(target, data, ExecuteEvents.endDragHandler);
            }
            State();
        }
        public static void SetGameView(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly; var sizesType = assembly.GetType("UnityEditor.GameViewSizes"); var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null); var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
            var type = assembly.GetType("UnityEditor.GameViewSize"); var sizeType = assembly.GetType("UnityEditor.GameViewSizeType");
            var size = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { Enum.ToObject(sizeType, 1), width, height, "Yan Yana " + width + "×" + height }, null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var viewType = assembly.GetType("UnityEditor.GameView"); var view = EditorWindow.GetWindow(viewType); viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, count - 1); view.Repaint();
        }
    }
}
