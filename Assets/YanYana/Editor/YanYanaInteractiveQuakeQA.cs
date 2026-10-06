// Editor-only pointer tests and a normal-speed recording of the production scene.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static readonly Dictionary<string, object> quakeSavedPrefs = new Dictionary<string, object>();
        static float quakeOriginalVolume;
        static bool quakeReviewActive, quakeOriginalMute;
        static PropertyInfo quakeMuteProperty;
        static string quakeCaptureFolder;
        static PointerEventData quakePointer;
        static GameObject quakePointerObject;
        static bool quakeRegressionRun;

        static IEnumerable<string> QuakePreferenceKeys()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Unity\UnityEditor\" + PlayerSettings.companyName + "\\" + PlayerSettings.productName))
                return key?.GetValueNames().Where(n => n.StartsWith(YanYanaAdventureBuilder.SavePrefix)).Select(n => n.Substring(0, n.LastIndexOf("_h", StringComparison.Ordinal))).ToArray() ?? Array.Empty<string>();
        }

        [MenuItem("Tools/Yan Yana/QA/Record Interactive Earthquake Review")]
        static void RecordInteractiveEarthquakeReview() => RecordInteractiveEarthquake(true);
        [MenuItem("Tools/Yan Yana/QA/Record Earthquake Gameplay")]
        static void RecordEarthquakeGameplay() => RecordInteractiveEarthquake(false);
        static void RecordInteractiveEarthquake(bool regressions)
        {
            if (!EditorApplication.isPlaying || quakeReviewActive || physicalCheckRunning || recordedRouteRunning)
                throw new InvalidOperationException("Enter adventure Play mode and finish other active reviews first.");
            quakeSavedPrefs.Clear();
            foreach (string key in QuakePreferenceKeys())
            {
                string text = PlayerPrefs.GetString(key, "__NOT_STRING__");
                quakeSavedPrefs[key] = text != "__NOT_STRING__" ? (object)text : key.EndsWith("playSeconds") ? (object)PlayerPrefs.GetFloat(key) : PlayerPrefs.GetInt(key);
            }
            quakeReviewActive = true; quakeOriginalVolume = AudioListener.volume;
            quakeMuteProperty = typeof(EditorUtility).GetProperty("audioMasterMute", BindingFlags.Public | BindingFlags.Static);
            if (quakeMuteProperty != null) { quakeOriginalMute = (bool)quakeMuteProperty.GetValue(null); quakeMuteProperty.SetValue(null, false); }
            PlayerPrefs.SetInt(YanYanaAdventureBuilder.SavePrefix + "option.Sound", 1); PlayerPrefs.SetInt(YanYanaAdventureBuilder.SavePrefix + "option.ReducedMotion", 0); PlayerPrefs.Save(); AudioListener.volume = 1;
            quakeCaptureFolder = "ClientExports/YanYana/Screenshots/interactive-quake-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"); Directory.CreateDirectory(quakeCaptureFolder);
            EditorApplication.playModeStateChanged += RestoreQuakeOnExit;
            quakeRegressionRun = regressions;
            StartPhysicalCheck(InteractiveQuakeReviewSequence(), regressions ? "interactive-quake-review" : "interactive-quake-gameplay");
        }

        static void RestoreQuakeOnExit(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) RestoreQuakePreferences(); }
        static void RestoreQuakePreferences()
        {
            if (!quakeReviewActive) return; quakeReviewActive = false;
            foreach (string key in QuakePreferenceKeys()) PlayerPrefs.DeleteKey(key);
            foreach (var pair in quakeSavedPrefs)
                if (pair.Value is string text) PlayerPrefs.SetString(pair.Key, text); else if (pair.Value is float number) PlayerPrefs.SetFloat(pair.Key, number); else PlayerPrefs.SetInt(pair.Key, (int)pair.Value);
            PlayerPrefs.Save(); AudioListener.volume = quakeOriginalVolume;
            if (quakeMuteProperty != null) quakeMuteProperty.SetValue(null, quakeOriginalMute);
            EditorApplication.playModeStateChanged -= RestoreQuakeOnExit;
        }

        static float QuakeNumber(string name) => Convert.ToSingle(Variables.Object(Flow).Get(name));
        static IEnumerable<object> QuakeSeconds(float seconds)
        {
            double end = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < end) yield return null;
        }
        static void QuakeAssert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            File.AppendAllText(physicalCheckReport, "PASS " + message + "\n");
        }
        static void QuakeFrame(string name)
        {
            if (!string.IsNullOrEmpty(quakeCaptureFolder)) ScreenCapture.CaptureScreenshot(quakeCaptureFolder + "/" + name + ".png");
        }
        static Vector2 QuakeScreen(Vector3 point) => Camera.main.WorldToScreenPoint(point);
        static PointerEventData QuakeData(GameObject target, Vector2 screen, int id = -1) => new PointerEventData(EventSystem.current)
        {
            position = screen, pressPosition = screen, pointerId = id, button = PointerEventData.InputButton.Left,
            pointerDrag = target, pointerPress = target, pointerCurrentRaycast = new RaycastResult { gameObject = target, worldPosition = target.transform.position }
        };
        static void QuakePress(GameObject target, Vector2 screen)
        {
            quakePointerObject = target; quakePointer = QuakeData(target, screen);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(quakePointer, hits);
            var handler = hits.Count > 0 ? ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject) : null;
            var drag = hits.Count > 0 ? ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject) : null;
            if (screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height || (handler != target && drag != target))
                throw new InvalidOperationException("Quake target cannot be touched: " + target.name + " screen=" + screen + " first=" + (hits.Count > 0 ? hits[0].gameObject.name : "none"));
            quakePointer.pointerCurrentRaycast = hits[0];
            File.AppendAllText(physicalCheckReport, "PASS real screen raycast " + target.name + " at " + screen + "\n");
            ExecuteEvents.Execute(target, quakePointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, quakePointer, ExecuteEvents.initializePotentialDrag);
        }
        static void QuakeMove(Vector2 screen)
        {
            if (!quakePointer.dragging) { ExecuteEvents.Execute(quakePointerObject, quakePointer, ExecuteEvents.beginDragHandler); quakePointer.dragging = true; }
            quakePointer.delta = screen - quakePointer.position; quakePointer.position = screen;
            ExecuteEvents.Execute(quakePointerObject, quakePointer, ExecuteEvents.dragHandler);
        }
        static void QuakeRelease()
        {
            ExecuteEvents.Execute(quakePointerObject, quakePointer, ExecuteEvents.pointerUpHandler);
            if (quakePointer.dragging) ExecuteEvents.Execute(quakePointerObject, quakePointer, ExecuteEvents.endDragHandler);
            quakePointer.dragging = false;
        }
        static IEnumerable<object> QuakeDragTo(GameObject target, Func<Vector3> destination, float travel = 1f, float stay = .8f, bool release = true)
        {
            Vector2 start = QuakeScreen(target.transform.position); QuakePress(target, start);
            double began = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - began < travel)
            {
                QuakeMove(Vector2.Lerp(start, QuakeScreen(destination()), Mathf.Clamp01((float)(EditorApplication.timeSinceStartup - began) / travel))); yield return null;
            }
            double until = EditorApplication.timeSinceStartup + stay;
            while (EditorApplication.timeSinceStartup < until) { QuakeMove(QuakeScreen(destination())); yield return null; }
            if (release) QuakeRelease();
        }

        static IEnumerable<object> InteractiveCoverSequence(bool regressions = false)
        {
            foreach (var frame in WaitPhysical(() => State("CoverStage") == 1 && ReadyIn(""), "Automatic indoor quake onset")) yield return frame;
            QuakeFrame("01-onset");
            var ada = Find("Ada");
            if (regressions)
            {
                QuakePress(ada, QuakeScreen(ada.transform.position + Vector3.up * .8f)); QuakeRelease();
                foreach (var frame in QuakeSeconds(.3f)) yield return frame;
                QuakeAssert(State("CoverStage") == 1 && QuakeNumber("QCrouch") < .03f, "A tap cannot crouch or advance");
                QuakePress(ada, QuakeScreen(ada.transform.position + Vector3.up * .8f));
                QuakeMove(quakePointer.pressPosition + Vector2.down * Screen.height * .085f);
                foreach (var frame in QuakeSeconds(.65f)) yield return frame;
                QuakeAssert(QuakeNumber("QCrouch") > .40f && QuakeNumber("QCrouch") < .60f, "Half gesture produces a half crouch rather than the complete pose"); QuakeFrame("02-half-crouch");
                var other = QuakeData(ada, quakePointer.pressPosition + Vector2.down * Screen.height * .3f, 8); ExecuteEvents.Execute(ada, other, ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(ada, other, ExecuteEvents.dragHandler);
                QuakeAssert(QuakeNumber("QCrouchTarget") < .6f, "A second pointer cannot steal the crouch");
                QuakeRelease(); foreach (var frame in QuakeSeconds(.6f)) yield return frame;
                QuakeAssert(State("CoverStage") == 1 && QuakeNumber("QCrouch") < .03f, "An unfinished gesture returns smoothly without advancing");
            }
            if (regressions)
            {
                var previous = Flow; ReloadPhysical(); yield return null;
                foreach (var frame in WaitPhysical(() => Flow != previous && State("CoverStage") == 1 && ReadyIn(""), "Quake reload safely restarts its gestures")) yield return frame;
                ada = Find("Ada"); QuakeAssert(Mathf.Abs(ada.transform.position.y) < .12f && QuakeNumber("QCrouch") < .03f && !Convert.ToBoolean(Variables.Object(Flow).Get("QPressed")), "Reload keeps the children on the floor and cancels the previous gesture");
            }
            Vector2 beginning = QuakeScreen(ada.transform.position + Vector3.up * .8f); QuakePress(ada, beginning);
            double start = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - start < 1.2)
            {
                QuakeMove(beginning + Vector2.down * Screen.height * .19f * Mathf.Clamp01((float)(EditorApplication.timeSinceStartup - start) / 1.2f)); yield return null;
            }
            foreach (var frame in QuakeSeconds(.75f)) yield return frame; QuakeFrame("03-crouched"); QuakeRelease();
            foreach (var frame in WaitPhysical(() => State("CoverStage") == 2 && ReadyIn(""), "Controlled crouch complete")) yield return frame;
            var palm = Find("Başta güvenli avuç teması · Ada").transform;
            foreach (var frame in QuakeDragTo(Find("Başını koruyan el"), () => palm.position, 1.1f, .8f)) yield return frame;
            foreach (var frame in WaitPhysical(() => State("CoverStage") == 3 && Find("Masaya tutunan el").GetComponent<Collider>().enabled, "Sustained hand-to-head gesture and rendered next target")) yield return frame;
            QuakeFrame("04-head-protection");
            var post = Find("Anchor_HoldAda").transform;
            foreach (var frame in QuakeDragTo(Find("Masaya tutunan el"), () => post.position, 1.1f, .8f, regressions)) yield return frame;
            foreach (var frame in WaitPhysical(() => State("CoverStage") == 4, "Sustained hand-to-table gesture")) yield return frame;
            QuakeFrame("05-table-contact");
            if (regressions)
            {
                foreach (var frame in QuakeSeconds(3.5f)) yield return frame;
                QuakeAssert(State("Phase") == 1 && State("Protected") == 0, "Waiting without a held grip cannot complete the earthquake");
                QuakePress(Find("Masaya tutunan el"), QuakeScreen(post.position)); foreach (var frame in QuakeSeconds(1.2f)) yield return frame; QuakeRelease();
                foreach (var frame in QuakeSeconds(.2f)) yield return frame;
                QuakeAssert(QuakeNumber("QHeldTime") < .1f && State("Protected") == 0, "Releasing the grip resets its sustained hold");
                QuakePress(Find("Masaya tutunan el"), QuakeScreen(post.position)); foreach (var frame in QuakeSeconds(.8f)) yield return frame;
                PausePhysical(); yield return null; float clock = QuakeNumber("QClock"); foreach (var frame in QuakeSeconds(.7f)) yield return frame;
                QuakeAssert(Mathf.Abs(QuakeNumber("QClock") - clock) < .001f && !Convert.ToBoolean(Variables.Object(Flow).Get("QGripPressed")), "Pause freezes the quake and cancels the held pointer");
                QuakeRelease(); ResumePhysical(); foreach (var frame in QuakeSeconds(.45f)) yield return frame;
                QuakeAssert(State("Protected") == 0 && QuakeNumber("QHeldTime") < .1f, "Resume requires a fresh grip");
            }
            if (regressions) QuakePress(Find("Masaya tutunan el"), QuakeScreen(post.position));
            else QuakeAssert(Convert.ToBoolean(Variables.Object(Flow).Get("QGripPressed")), "The original hand drag continues straight into the held grip without a second press");
            start = EditorApplication.timeSinceStartup;
            while (State("Phase") == 1 && State("Protected") == 0)
            {
                if (EditorApplication.timeSinceStartup - start > 20) throw new InvalidOperationException("Sustained grip did not finish.");
                QuakeMove(QuakeScreen(post.position)); yield return null;
            }
            QuakeRelease(); foreach (var frame in WaitPhysical(() => State("Phase") == 2 && ReadyIn(""), "Earthquake ends only after protected hold")) yield return frame;
            QuakeAssert(Mathf.Abs(Find("Ada").transform.position.y) < .12f && Mathf.Abs(Find("Efe").transform.position.y) < .12f, "Both children leave the table on the floor rather than its NavMesh top");
            QuakeFrame("06-after-quake");
        }

        static IEnumerable<object> InteractiveQuakeReviewSequence()
        {
            try
            {
                var previous = Flow; New(); yield return null;
                foreach (var frame in WaitPhysical(() => Flow != previous && ReadyIn(""), "Fresh review journey")) yield return frame;
                foreach (var frame in IntroCarrySequence(false, false)) yield return frame;
                foreach (var frame in WaitPhysical(() => State("IntroDone") == 1 && ReadyIn(""), "Opening carried physically")) yield return frame;
                YanYanaQA.SetGameView(540, 960); foreach (var frame in QuakeSeconds(.5f)) yield return frame;
                Variables.Object(Flow).Set("Sound", true); Variables.Object(Flow).Set("ReducedMotion", false); AudioListener.volume = 1;
                YanYanaDeliveryTools.StartRecording();
                BeginBridge(); foreach (var frame in WaitPhysical(() => ReadyIn("bridge"), "Walk to Efe's bridge")) yield return frame;
                var bridge = Find("Anchor_CoverAda").transform.position + Vector3.up * (Find("Anchor_Comfort").transform.position.y + .035f);
                for (int i = 0; i < 3; i++) foreach (var frame in TimedDrag(Find("Köprü parçası " + i), bridge + new Vector3(-.2f + i * .2f, 0, 0))) yield return frame;
                foreach (var frame in InteractiveCoverSequence(quakeRegressionRun)) yield return frame;
                Click(Find("Efe")); foreach (var frame in WaitPhysical(() => ReadyIn("sibling"), "Efe remains reachable")) yield return frame;
                yield return null;
                QuakeAssert(!Find("SiblingContinue").activeInHierarchy, "Sibling support offers no Next button to skip the hand gesture");
                foreach (var frame in TimedDrag(Find("Efe’ye uzanan el"), (Vector3)Variables.Object(Flow).Get("SiblingTarget"), 1.0f)) yield return frame;
                foreach (var frame in WaitPhysical(() => State("SiblingChecked") == 1 && ReadyIn(""), "Physical support after the earthquake")) yield return frame;
                QuakeFrame("07-sibling-support");
                foreach (var frame in QuakeSeconds(1.3f)) yield return frame;
                File.AppendAllText(physicalCheckReport, quakeRegressionRun ? "COMPLETE: actual bridge onset, analog crouch, head/hand drags, held grip, cancellation, pointer ownership, reload, pause/resume and sibling continuity.\n" : "COMPLETE: actual bridge onset, analog crouch, head/hand drags, grip sustained without re-pressing, floor exit and sibling support.\n");
            }
            finally { YanYanaDeliveryTools.StopRecording(); RestoreQuakePreferences(); }
        }
    }
}
