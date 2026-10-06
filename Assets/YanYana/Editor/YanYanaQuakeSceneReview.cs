// Editor inspection only. No component from this file is included in the player.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static class YanYanaQuakeSceneReview
    {
        [MenuItem("Tools/Yan Yana/QA/Inspect Earthquake Scene")]
        public static void Inspect()
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var lines = new System.Collections.Generic.List<string> { "Playing=" + EditorApplication.isPlaying + " Compiling=" + EditorApplication.isCompiling };
            foreach (var t in transforms.Where(t => t.name.StartsWith("Anchor_") || new[] { "Ada", "Efe", "Derya", "Emre", "HomeRoom" }.Contains(t.name)))
                lines.Add("POINT " + t.name + " " + t.position.ToString("F3") + " rotation=" + t.eulerAngles.ToString("F1"));
            foreach (var c in UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                lines.Add("CAM " + c.name + " " + c.transform.position.ToString("F3") + " rotation=" + c.transform.eulerAngles.ToString("F1") + " fov=" + c.Lens.FieldOfView + " active=" + c.gameObject.activeSelf);
            foreach (var m in UnityEngine.Object.FindObjectsByType<ScriptMachine>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                lines.Add("GRAPH " + m.gameObject.name + " : " + m.graph.title);
            foreach (var a in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                lines.Add("AUDIO " + a.name + " clip=" + (a.clip ? a.clip.name : "none") + " volume=" + a.volume + " enabled=" + a.enabled);
            var room = transforms.First(t => t.name == "HomeRoom");
            foreach (var r in room.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("Wall") || r.name.Contains("Window") || r.name.Contains("Table") || r.name.Contains("Lamp")) lines.Add("MESH " + r.name + " " + r.bounds.ToString("F2"));
            Directory.CreateDirectory("ClientExports/YanYana/Reports");
            File.WriteAllLines("ClientExports/YanYana/Reports/quake-scene-inspection.txt", lines);
            Debug.Log("Earthquake inspection written. Playing=" + EditorApplication.isPlaying);
        }
    }
}
