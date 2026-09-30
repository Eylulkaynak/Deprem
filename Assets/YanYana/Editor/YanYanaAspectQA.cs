using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using TMPro;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static int aspectIndex;
        static double aspectReady;
        static bool aspectChanging;
        static readonly int[] portraitHeights={960,1170,1200};
        [MenuItem("Tools/Yan Yana/QA/Physical Three Portrait Ratios")]
        static void ThreeRatios()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");aspectIndex=0;aspectChanging=false;SetAspect();EditorApplication.update-=AspectTick;EditorApplication.update+=AspectTick;
        }
        static void SetAspect(){YanYanaQA.SetGameView(540,portraitHeights[aspectIndex]);aspectReady=EditorApplication.timeSinceStartup+.8;}
        static void AspectTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=AspectTick;return;}if(EditorApplication.timeSinceStartup<aspectReady)return;if(aspectChanging){aspectChanging=false;SetAspect();return;}
            var lines=new System.Collections.Generic.List<string>();string work=(string)Variables.Object(Flow).Get("Workspace");if(work=="")work="phase"+Variables.Object(Flow).Get("Phase");
            if(Find("Dönüp deneyebileceğin kararlar").activeInHierarchy)work="replay";
            lines.Add("Editor viewport only; not physical phone touch. Screen="+Screen.width+"x"+Screen.height+" workspace="+work+" safeArea="+Screen.safeArea);
            foreach(var text in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if(!text.isActiveAndEnabled)continue;text.ForceMeshUpdate();var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
                if(corners.Any(x=>x.x<-.5f||x.x>Screen.width+.5f||x.y<-.5f||x.y>Screen.height+.5f))lines.Add("OUTSIDE_SCREEN "+text.name);
                if(text.preferredHeight>text.rectTransform.rect.height+3)lines.Add("TEXT_HEIGHT "+text.name+" preferred="+text.preferredHeight+" available="+text.rectTransform.rect.height);
            }
            foreach(var view in UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None))lines.Add(view.name+" ortho="+view.Lens.OrthographicSize+" FOV="+view.Lens.FieldOfView);
            File.WriteAllLines("ClientExports/YanYana/Reports/aspect-"+work+"-"+Screen.height+".txt",lines);ScreenCapture.CaptureScreenshot("ClientExports/YanYana/Screenshots/aspect-"+work+"-"+Screen.height+".png");
            if(++aspectIndex==portraitHeights.Length){EditorApplication.update-=AspectTick;return;}aspectChanging=true;aspectReady=EditorApplication.timeSinceStartup+.2;
        }
    }
}
