// Editor-only screenshots and production EventSystem checks for the interface.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Cinemachine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Interface Edit Preview")]
        static void InterfaceEditPreview()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Play captures while playing.");
            var cam = Camera.main; var view = Find("AD · mahalle açılış kadrajı").GetComponent<CinemachineCamera>();
            var transform = cam.transform; var position = transform.position; var rotation = transform.rotation; bool ortho = cam.orthographic; float size = cam.orthographicSize;
            var brain = cam.GetComponent<CinemachineBrain>(); bool enabled = brain.enabled;
            var canvas = Find("Dikey ekran · 540 × 960").GetComponent<Canvas>(); var mode = canvas.renderMode; var worldCamera = canvas.worldCamera; float plane = canvas.planeDistance;
            var before = RenderTexture.active; var priorTarget = cam.targetTexture; var target = RenderTexture.GetTemporary(540, 960, 24, RenderTextureFormat.ARGB32);
            Texture2D tex = null;
            try
            {
                brain.enabled = false; transform.SetPositionAndRotation(view.transform.position, view.transform.rotation); cam.orthographic = true; cam.orthographicSize = view.Lens.OrthographicSize;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = .25f;
                cam.targetTexture = target; Canvas.ForceUpdateCanvases(); cam.Render(); RenderTexture.active = target;
                tex = new Texture2D(540, 960, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); tex.Apply();
                Directory.CreateDirectory("ClientExports/YanYana/Screenshots/interface"); File.WriteAllBytes("ClientExports/YanYana/Screenshots/interface/title-edit.png", tex.EncodeToPNG());
            }
            finally
            {
                cam.targetTexture = priorTarget; RenderTexture.active = before; RenderTexture.ReleaseTemporary(target); if (tex) UnityEngine.Object.DestroyImmediate(tex);
                canvas.renderMode = mode; canvas.worldCamera = worldCamera; canvas.planeDistance = plane; transform.SetPositionAndRotation(position, rotation); cam.orthographic = ortho; cam.orthographicSize = size; brain.enabled = enabled;
            }
        }
        [MenuItem("Tools/Yan Yana/QA/Interface Screens And Input")]
        static void InterfaceScreensAndInput() => StartPhysicalCheck(InterfaceSequence(), "interface-input-qa");
        static void InterfaceButton(string name, bool click)
        {
            var obj = Find(name); var rect = obj.GetComponent<RectTransform>();
            Vector2 at = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = at, pointerId = -1, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != obj) throw new InvalidOperationException("Interface target blocked: " + name);
            if (rect.rect.width < 48 || rect.rect.height < 48) throw new InvalidOperationException("Interface hit target too small: " + name);
            File.AppendAllText(physicalCheckReport, "PASS screen raycast " + name + " " + Screen.width + "x" + Screen.height + "\n");
            if (!click) return;
            pointer.pointerCurrentRaycast = hits[0]; pointer.pointerPressRaycast = hits[0]; pointer.pressPosition = at; pointer.pointerPress = obj; pointer.eligibleForClick = true;
            ExecuteEvents.Execute(obj, pointer, ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(obj, pointer, ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(obj, pointer, ExecuteEvents.pointerClickHandler);
        }
        static void InterfaceTextCheck(GameObject root)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var t in root.GetComponentsInChildren<TMP_Text>())
            {
                if (!t.enabled || string.IsNullOrWhiteSpace(t.text)) continue;
                t.ForceMeshUpdate(); if (t.isTextOverflowing) throw new InvalidOperationException("Text overflow: " + t.name + " | " + t.text);
            }
        }
        static IEnumerable<object> InterfaceSequence()
        {
            string folder = "ClientExports/YanYana/Screenshots/interface"; Directory.CreateDirectory(folder);
            if (!Find("Yan Yana · başlangıç").activeSelf) throw new InvalidOperationException("Start this check at the opening menu.");
            foreach (int height in new[] { 960, 1170, 1200 })
            {
                YanYanaQA.SetGameView(540, height); foreach (var f in FramesFor(.9f)) yield return f;
                InterfaceButton("Continue", false); InterfaceButton("New", false); InterfaceTextCheck(Find("Yan Yana · başlangıç"));
                if (Find("AD · oyun arayüzü").GetComponent<CanvasGroup>().alpha != 0) throw new InvalidOperationException("HUD visible behind title.");
                foreach (string name in new[] { "Ada", "Efe" })
                {
                    var head = Find("AD · " + name + " açılış").GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head).position;
                    var viewport = Camera.main.WorldToViewportPoint(head);
                    if (viewport.z <= 0 || viewport.x < .08f || viewport.x > .92f || viewport.y < .25f || viewport.y > .67f) throw new InvalidOperationException("Title face obscured: " + name + " " + viewport);
                    File.AppendAllText(physicalCheckReport, "PASS title face " + name + " " + viewport + "\n");
                }
                ScreenCapture.CaptureScreenshot(folder + "/title-" + height + ".png"); foreach (var f in FramesFor(.15f)) yield return f;
            }
            YanYanaQA.SetGameView(540, 960); foreach (var f in FramesFor(.5f)) yield return f;
            var old = Flow; InterfaceButton("New", true); yield return null;
            foreach (var f in WaitPhysical(() => Flow != old && ReadyIn(""), "New adventure button")) yield return f;
            foreach (var f in FramesFor(1f)) yield return f;
            if (Find("Açılış · mahallede bir öğleden sonra").activeSelf) throw new InvalidOperationException("Title world still active in gameplay.");
            foreach (int height in new[] { 960, 1170, 1200 })
            {
                YanYanaQA.SetGameView(540, height); foreach (var f in FramesFor(.65f)) yield return f;
                InterfaceButton("Pause", false); InterfaceTextCheck(Find("Tek amaç")); InterfaceTextCheck(Find("Kısa konuşma"));
                ScreenCapture.CaptureScreenshot(folder + "/gameplay-" + height + ".png"); foreach (var f in FramesFor(.15f)) yield return f;
            }
            InterfaceButton("Pause", true); foreach (var f in FramesFor(.3f)) yield return f;
            if (!(bool)Variables.Object(Flow).Get("Paused") || Find("AD · oyun arayüzü").GetComponent<CanvasGroup>().alpha != 0) throw new InvalidOperationException("Pause did not isolate its interface.");
            InterfaceTextCheck(Find("Duraklatma")); InterfaceButton("ReducedMotion", false); InterfaceButton("Sound", false); InterfaceButton("Captions", false); InterfaceButton("Vibration", false);
            ScreenCapture.CaptureScreenshot(folder + "/pause.png"); foreach (var f in FramesFor(.2f)) yield return f;
            InterfaceButton("Resume", true); foreach (var f in WaitPhysical(() => ReadyIn(""), "Resume button")) yield return f;
            old = Flow; UnityEngine.SceneManagement.SceneManager.LoadScene(YanYanaAdventureBuilder.ScenePath); yield return null;
            foreach (var f in WaitPhysical(() => Flow != old && Find("Yan Yana · başlangıç").activeSelf, "Saved game menu")) yield return f;
            foreach (var f in FramesFor(1f)) yield return f; InterfaceButton("Continue", true);
            foreach (var f in WaitPhysical(() => ReadyIn(""), "Continue saved adventure")) yield return f;
            if (State("IntroDone") != 0) throw new InvalidOperationException("UI changed an unfinished adventure decision.");
            YanYanaQA.SetGameView(540, 960);
        }
    }
}
