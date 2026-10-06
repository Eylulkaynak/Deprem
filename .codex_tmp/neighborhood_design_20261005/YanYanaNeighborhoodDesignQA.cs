// Editor-only integration QA through production pointer/movement handlers.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Play KKTC Neighborhood Design")]
        static void NeighborhoodDesignQA() => StartPhysicalCheck(NeighborhoodDesignSequence(), "neighborhood-design-play");

        static IEnumerable<object> NeighborhoodDesignSequence()
        {
            Fixture(6, new Vector3(3, -.48f, -19.9f));
            var vars = Variables.Object(Flow); vars.Set("IntroDone", 1); vars.Set("NavigationReady", true); vars.Set("AidStage", 0);
            for (int i = 0; i < 3; i++) vars.Set("VisitorHelped" + i, 0);
            yield return null;
            Click(Find("Bora"));
            foreach (var frame in WaitPhysical(() => ReadyIn("aid"), "Walk to Bora and open the aid camera")) yield return frame;
            var environment = Find("KKTC · hacimli mahalle ve açık meydan");
            var roofs = environment.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Tente · gerçek kumaş")).ToArray();
            if (roofs.Length != 3 || roofs.Any(r => r.enabled)) throw new InvalidOperationException("The overview must show the desks without a canopy blocking the targets.");
            File.AppendAllText(physicalCheckReport, "PASS actual aid camera and unobstructed table/drag targets\n");
            Directory.CreateDirectory("ClientExports/YanYana/Screenshots");
            ScreenCapture.CaptureScreenshot("ClientExports/YanYana/Screenshots/kktc-gameplay-aid.png");
            yield return null;
            var center = Find("Street_Aid").transform.position;
            var targets = new[] { center + new Vector3(-3, 1.75f, .05f), center + new Vector3(0, 1.75f, -1.4f), center + new Vector3(3, 1.75f, .05f) };
            for (int i = 0; i < 3; i++)
            {
                foreach (var frame in TimedDrag(Find("Taşınan ihtiyaç işareti " + i), targets[i])) yield return frame;
                if (State("VisitorHelped" + i) != 1) throw new InvalidOperationException("Aid drag failed for neighbor " + i);
                File.AppendAllText(physicalCheckReport, "PASS neighbor " + i + " physically walks to the unchanged station destination\n");
            }
            foreach (var frame in WaitPhysical(() => ReadyIn("relief"), "Neighbors arrive and relief opens")) yield return frame;
            ScreenCapture.CaptureScreenshot("ClientExports/YanYana/Screenshots/kktc-gameplay-relief.png");
            File.AppendAllText(physicalCheckReport, "PASS new environment retains aid movement and relief progression\n");
        }
    }
}
