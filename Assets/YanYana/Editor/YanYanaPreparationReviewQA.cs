// Editor-only regression and normal-speed preparation walkthrough using production input.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static readonly Dictionary<string, object> preparationPrefs = new Dictionary<string, object>();
        static bool preparationReviewActive;
        static float preparationVolume;
        const string PreparationBackupKey = "YanYana.PreparationReview.PlayerPrefsBackup";
        [Serializable] sealed class PreparationPreference { public string key, value; public int kind; }
        [Serializable] sealed class PreparationPreferenceSnapshot { public PreparationPreference[] entries; }
        [InitializeOnLoadMethod]
        static void RegisterPreparationFinalRestore()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode) return;
                string json = SessionState.GetString(PreparationBackupKey, ""); if (json.Length == 0) return;
                var saved = JsonUtility.FromJson<PreparationPreferenceSnapshot>(json);
                foreach (var key in QuakePreferenceKeys()) PlayerPrefs.DeleteKey(key);
                foreach (var entry in saved.entries)
                    if (entry.kind == 0) PlayerPrefs.SetString(entry.key, entry.value);
                    else if (entry.kind == 1) PlayerPrefs.SetFloat(entry.key, float.Parse(entry.value, System.Globalization.CultureInfo.InvariantCulture));
                    else PlayerPrefs.SetInt(entry.key, int.Parse(entry.value, System.Globalization.CultureInfo.InvariantCulture));
                PlayerPrefs.Save(); SessionState.SetString(PreparationBackupKey, "");
                File.AppendAllText(YanYanaPreparationPolish.Evidence + "/walkthrough.txt", "RESTORED preferences after Play mode teardown.\n");
            };
        }
        [MenuItem("Tools/Yan Yana/QA/Preparation Complete Review")]
        static void ReviewPreparation()
        {
            if (!EditorApplication.isPlaying || physicalCheckRunning || recordedRouteRunning || preparationReviewActive)
                throw new InvalidOperationException("Enter Play mode and finish the current review first.");
            Directory.CreateDirectory(YanYanaPreparationPolish.Evidence);
            preparationPrefs.Clear();
            foreach (var key in QuakePreferenceKeys())
            {
                string str = PlayerPrefs.GetString(key, "__NOT_STRING__");
                preparationPrefs[key] = str != "__NOT_STRING__" ? (object)str : key.EndsWith("playSeconds") ? (object)PlayerPrefs.GetFloat(key) : PlayerPrefs.GetInt(key);
            }
            var snapshot = new PreparationPreferenceSnapshot { entries = preparationPrefs.Select(p => new PreparationPreference
            {
                key = p.Key, kind = p.Value is string ? 0 : p.Value is float ? 1 : 2,
                value = p.Value is float number ? number.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : Convert.ToString(p.Value, System.Globalization.CultureInfo.InvariantCulture)
            }).ToArray() };
            SessionState.SetString(PreparationBackupKey, JsonUtility.ToJson(snapshot));
            preparationReviewActive = true; preparationVolume = AudioListener.volume; AudioListener.volume = 0;
            RouteReport = YanYanaPreparationPolish.Evidence + "/walkthrough.txt"; File.WriteAllText(RouteReport, "Preparation review; real approach, pointer handlers, packing and carrying.\n");
            EditorApplication.update += PreparationReviewMonitor;
            EditorApplication.playModeStateChanged += PreparationReviewExit;
            StartPhysicalCheck(PreparationReviewSequence(), "preparation-review");
        }
        static void PreparationReviewMonitor() { if (preparationReviewActive && !physicalCheckRunning) RestorePreparationReview(); }
        static void PreparationReviewExit(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) RestorePreparationReview(); }
        static void RestorePreparationReview()
        {
            if (!preparationReviewActive) return;
            preparationReviewActive = false; EditorApplication.update -= PreparationReviewMonitor; EditorApplication.playModeStateChanged -= PreparationReviewExit;
            foreach (var key in QuakePreferenceKeys()) PlayerPrefs.DeleteKey(key);
            foreach (var pair in preparationPrefs)
                if (pair.Value is string str) PlayerPrefs.SetString(pair.Key, str); else if (pair.Value is float number) PlayerPrefs.SetFloat(pair.Key, number); else PlayerPrefs.SetInt(pair.Key, (int)pair.Value);
            PlayerPrefs.Save(); AudioListener.volume = preparationVolume;
            File.AppendAllText(RouteReport, "RESTORED original preparation save and audio.\n");
        }
        static void PrepCheck(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException(description);
            File.AppendAllText(RouteReport, "PASS " + description + "\n");
        }
        static void PrepFrame(string name) => ScreenCapture.CaptureScreenshot(YanYanaPreparationPolish.Evidence + "/play-" + name + ".png");
        static void PrepHit(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            var point = Camera.main.WorldToScreenPoint(collider ? collider.bounds.center : target.transform.position);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            var first = hits.Count == 0 ? null : hits[0].gameObject;
            bool reachable = first && (ExecuteEvents.GetEventHandler<IPointerDownHandler>(first) == target ||
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(first) == target || ExecuteEvents.GetEventHandler<IDragHandler>(first) == target);
            PrepCheck(point.z > 0 && point.x > 0 && point.x < Screen.width && point.y > 0 && point.y < Screen.height && reachable,
                "Screen raycast " + Screen.width + "x" + Screen.height + " reaches " + target.name + " (first=" + (first ? first.name : "none") + ")");
        }
        static IEnumerable<object> PreparationReviewSequence()
        {
            var previous = Flow; New(); yield return null;
            foreach (var f in WaitPhysical(() => Flow != previous && ReadyIn(""), "New preparation")) yield return f;
            foreach (var f in IntroCarrySequence(false, false)) yield return f;
            foreach (var f in WaitPhysical(() => State("IntroDone") == 1 && ReadyIn(""), "Opening completed")) yield return f;
            PrepFrame("room"); foreach (var f in FramesFor(.35f)) yield return f;
            Bag(); foreach (var f in WaitPhysical(() => ReadyIn("bag"), "Empty bag")) yield return f;
            Click(Find("Çantayı kapatma tokası")); yield return null;
            PrepCheck(ReadyIn("bag") && State("BagClosed") == 0 && State("BagReady") == 0, "Empty bag cannot enter zipper trial or become ready");
            Back(); foreach (var f in WaitPhysical(() => ReadyIn(""), "Leave empty bag")) yield return f;
            Fener(); foreach (var f in WaitPhysical(() => ReadyIn("flashlight"), "Inspect unpowered flashlight")) yield return f;
            Back(); foreach (var f in WaitPhysical(() => ReadyIn(""), "Leave unpowered flashlight")) yield return f;
            PrepCheck(State("Found.Flashlight") == 0, "Leaving an untested flashlight does not collect it");
            Fener(); foreach (var f in WaitPhysical(() => ReadyIn("flashlight"), "Return to flashlight")) yield return f;
            foreach (var f in FramesFor(.6f)) yield return f;
            foreach (string name in new[]{"Kaydırılabilir pil kapağı", "AA pil 1", "AA pil 2", "Gerçek açma anahtarı"}) PrepHit(Find(name));
            var c = Find("Anchor_FlashlightWork").transform.position + Vector3.up * .04f;
            foreach (var f in TimedDrag(Find("Kaydırılabilir pil kapağı"), c + new Vector3(.21f, .068f, -.04f))) yield return f;
            foreach (var f in TimedDrag(Find("AA pil 1"), c + new Vector3(-.031f, .035f, -.028f))) yield return f;
            foreach (var f in TimedDrag(Find("AA pil 2"), c + new Vector3(.031f, .035f, -.028f))) yield return f;
            Click(Find("AA pil 1")); Find("RotateItem").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            foreach (var f in TimedDrag(Find("Kaydırılabilir pil kapağı"), c + new Vector3(0, .068f, -.018f))) yield return f;
            Click(Find("Gerçek açma anahtarı")); PrepCheck(State("FlashlightReady") == 0, "Reversed battery prevents flashlight power");
            foreach (var f in TimedDrag(Find("Kaydırılabilir pil kapağı"), c + new Vector3(.21f, .068f, -.04f))) yield return f;
            Click(Find("AA pil 1")); Find("RotateItem").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            foreach (var f in TimedDrag(Find("Kaydırılabilir pil kapağı"), c + new Vector3(0, .068f, -.018f))) yield return f;
            PrepCheck(State("FlashlightReady") == 1, "Correct polarity, closed cover and switch produce light"); PrepFrame("flashlight"); foreach (var f in FramesFor(.35f)) yield return f;
            Back(); foreach (var f in WaitPhysical(() => ReadyIn(""), "Collect tested flashlight")) yield return f;
            PrepCheck(State("Found.Flashlight") == 1, "Working flashlight transfers to the preparation cloth");
            Radio(); foreach (var f in WaitPhysical(() => ReadyIn("radio"), "Untuned radio")) yield return f;
            Back(); foreach (var f in WaitPhysical(() => ReadyIn(""), "Leave untuned radio")) yield return f;
            PrepCheck(State("Found.Radio") == 0, "Untuned radio cannot skip its mechanism");
            Radio(); foreach (var f in WaitPhysical(() => ReadyIn("radio"), "Tune radio")) yield return f;
            foreach (var f in FramesFor(.6f)) yield return f; PrepHit(Find("Frekans düğmesi"));
            var dial = Find("Frekans düğmesi"); foreach (var f in TimedDrag(dial, dial.transform.position + new Vector3(.10f, 0, .173205f))) yield return f;
            PrepCheck(State("RadioReady") == 1, "Radio requires tuning into the clear band"); PrepFrame("radio"); foreach (var f in FramesFor(.35f)) yield return f;
            Back(); foreach (var f in WaitPhysical(() => ReadyIn(""), "Leave tested radio")) yield return f;
            foreach (string key in new[]{"Water", "Food"})
            {
                Click(Find("Odada bulunacak · " + key)); foreach (var f in WaitPhysical(() => ReadyIn("supply" + key), "Inspect " + key)) yield return f;
                var root = Find("Ambalaj incelemesi · " + key).transform;
                var good = Find("İncelenen " + key + " 1"); var bad = Find("İncelenen " + key + " 0");
                foreach (var f in FramesFor(.6f)) yield return f; PrepHit(good); PrepHit(bad); PrepHit(Find("Ambalajı çevir · " + key));
                var destination = root.position + new Vector3(0, good.transform.position.y - root.position.y, -.34f);
                foreach (var f in TimedDrag(good, destination)) yield return f;
                PrepCheck(State("Found." + key) == 0 && ReadyIn("supply" + key), key + " must be inspected before selection");
                foreach (var f in TimedDrag(Find("Ambalajı çevir · " + key), root.position + new Vector3(.18f, .025f, .03f))) yield return f;
                foreach (var f in TimedDrag(bad, destination)) yield return f;
                PrepCheck(State("Found." + key) == 0 && State(key + "Ready") == 0 && ReadyIn("supply" + key), "Opened/expired " + key + " rejected with retry available");
                PrepFrame("inspect-" + key); foreach (var f in FramesFor(.35f)) yield return f;
                if (Deprem.Accessibility.ReadingFree3D.Enabled)
                {
                    var captions = Find("Kısa konuşma").GetComponentsInChildren<TMPro.TMP_Text>(true);
                    string expected = key == "Water" ? "Kapak açık. Yeniden seç." : "Paket yırtık. Yeniden seç.";
                    PrepCheck(captions.Any(t => t.enabled && t.gameObject.activeInHierarchy && t.text == expected), "Child sees the specific rejection reason for " + key);
                }
                foreach (var f in TimedDrag(good, destination)) yield return f;
                foreach (var f in WaitPhysical(() => ReadyIn("") && State("Found." + key) == 1, "Choose sealed " + key)) yield return f;
                PrepCheck(State(key + "Ready") == 1, "Inspected sealed " + key + " accepted");
            }
            foreach (string key in new[]{"FirstAid", "Blanket", "FamilyCard", "Whistle", "ComfortFox"})
            {
                Click(Find("Odada bulunacak · " + key)); foreach (var f in WaitPhysical(() => State("Found." + key) == 1 && ReadyIn(""), "Collect " + key)) yield return f;
            }
            Map(); foreach (var f in WaitPhysical(() => ReadyIn("map"), "Family route")) yield return f;
            var map = Find("Ailecek denenen resimli mahalle planı").transform.position;
            foreach (var f in TimedDrag(Find("Haritadaki aile taşı"), map + new Vector3(.22f, .065f, 0))) yield return f;
            PrepCheck(State("MapCell") == 0 && State("FamilyPlan") == 0, "Family route rejects the blocked building cell");
            foreach (int cell in new[]{3, 6, 7, 10, 11}) foreach (var f in TimedDrag(Find("Haritadaki aile taşı"), map + new Vector3(cell % 3 * .22f, .065f, cell / 3 * .22f))) yield return f;
            PrepCheck(State("FamilyPlan") == 1, "Connected safe route reaches the family meeting point");
            Back(); foreach (var f in WaitPhysical(() => ReadyIn(""), "Leave family map")) yield return f;
            Bag(); foreach (var f in WaitPhysical(() => ReadyIn("bag"), "Pack inspected supplies")) yield return f;
            foreach (int height in new[]{960, 1170, 1200})
            {
                YanYanaQA.SetGameView(540, height); foreach (var f in FramesFor(.7f)) yield return f;
                foreach (string key in new[]{"Water", "Radio", "Flashlight", "FirstAid", "Blanket", "Food", "FamilyCard", "Whistle", "ComfortFox"}) PrepHit(Find("Yerleşim · " + key));
                PrepHit(Find("Çantayı kapatma tokası"));
            }
            YanYanaQA.SetGameView(540, 960); foreach (var f in FramesFor(.7f)) yield return f;
            PrepFrame("cloth"); foreach (var f in FramesFor(.35f)) yield return f;
            var origin = Find("Anchor_BagWork").transform.position + new Vector3(-.2375f, .083f, -.13f);
            var water = Find("Yerleşim · Water"); var radio = Find("Yerleşim · Radio");
            Click(water); PrepCheck(State("Pack.Water.X") == -1 && !(bool)Variables.Object(Flow).Get("Dragging"), "Clicking an item neither packs it nor leaves input held");
            foreach (var f in TimedDrag(water, origin + new Vector3(.0475f, 0, .1425f))) yield return f;
            foreach (var f in TimedDrag(radio, origin + new Vector3(.1425f, 0, .095f))) yield return f;
            PrepCheck(State("Pack.Water.X") == 0 && State("Pack.Radio.X") == -1, "Overlapping placement rejected");
            Click(radio); Find("RotateItem").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            foreach (var f in TimedDrag(radio, origin + new Vector3(.19f, 0, .1425f))) yield return f;
            PrepCheck(State("Pack.Radio.X") == 1 && State("Pack.Radio.Rot") == 1, "Rotated radio fits beside the water bottle");
            foreach (var f in TimedDrag(water, origin + new Vector3(-.35f, 0, -.40f))) yield return f;
            foreach (var f in TimedDrag(radio, origin + new Vector3(-.35f, 0, -.40f))) yield return f;
            PrepCheck(State("Pack.Water.X") == -1 && State("BagCell0") == 0, "Unpacking releases occupied space");
            Click(radio); Find("RotateItem").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            foreach (var item in new[]{("Radio",0,0,3,2),("FirstAid",3,0,2,2),("Blanket",0,2,2,2),("Water",2,2,1,3),("Flashlight",3,2,1,2),("Food",0,4,2,1),("ComfortFox",4,2,1,2),("FamilyCard",0,5,1,1)})
            {
                foreach (var f in TimedDrag(Find("Yerleşim · " + item.Item1), origin + new Vector3((item.Item2 + item.Item4 * .5f) * .095f, 0, (item.Item3 + item.Item5 * .5f) * .095f))) yield return f;
                PrepCheck(State("Pack." + item.Item1 + ".X") == item.Item2 && State("Pack." + item.Item1 + ".Y") == item.Item3, "Physical packing " + item.Item1);
            }
            Click(Find("Çantayı kapatma tokası")); yield return null;
            PrepCheck(ReadyIn("bag") && State("BagClosed") == 0, "Missing essential item blocks closure and remains repairable");
            foreach (var f in TimedDrag(Find("Yerleşim · Whistle"), origin + new Vector3(1.5f * .095f, 0, 5.5f * .095f))) yield return f;
            PrepFrame("packed"); foreach (var f in FramesFor(.35f)) yield return f;
            previous = Flow; ReloadPhysical(); yield return null;
            foreach (var f in WaitPhysical(() => Flow != previous && ReadyIn("bag"), "Restore completed packing")) yield return f;
            PrepCheck(State("Pack.Whistle.X") == 1 && State("WaterReady") == 1 && State("RadioReady") == 1, "Reload preserves inspected and physically packed supplies");
            Click(Find("Çantayı kapatma tokası")); foreach (var f in WaitPhysical(() => ReadyIn("bagfit"), "Fitting")) yield return f;
            foreach (var f in TimedDrag(Find("Çantanın sürüklenen fermuarı"), Find("BagZipWaypoint6").transform.position)) yield return f;
            PrepCheck(State("BagZipStep") == 0 && State("BagReady") == 0, "Zipper cannot skip its path");
            foreach (var f in CompleteBackpackFitting()) yield return f;
            PrepCheck(State("BagReady") == 1 && State("BagCarried") == 1, "Zipper, both straps and actual walking required for readiness"); PrepFrame("carried"); foreach (var f in FramesFor(.35f)) yield return f;
        }
    }
}
