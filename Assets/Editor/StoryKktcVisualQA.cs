using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Story;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Authored-state renders. Gameplay proof is recorded separately in PlayMode.</summary>
public static class StoryKktcVisualQA
{
    static UnityEditor.TestTools.TestRunner.Api.TestRunnerApi testApi;
    [MenuItem("Tools/Deprem Story/KKTC/11 Run Walking Camera Regression Checks")]
    public static void RunWalkingCameraChecks()
    {
        testApi=ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
        testApi.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter
        {
            testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,
            testNames=new[]{"StoryRebuildWalkthroughPlayModeTests.Story01_Preparation_PlaysToCompletion",
                "StoryRebuildWalkthroughPlayModeTests.Story02_HomeSafety_PlaysToCompletion"}
        }));
    }
    [MenuItem("Tools/Deprem Story/KKTC/8 Run Four Chapter PlayMode Checks")]
    public static void RunPlayModeChecks()
    {
        testApi=ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
        testApi.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter
        {
            testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,
            testNames=new[]{"StoryRebuildWalkthroughPlayModeTests.Story01_Preparation_PlaysToCompletion",
                "StoryRebuildWalkthroughPlayModeTests.Story02_HomeSafety_PlaysToCompletion",
                "StoryRebuildWalkthroughPlayModeTests.Story03_Quake_PlaysToCompletion",
                "StoryRebuildWalkthroughPlayModeTests.Story04_Evacuation_PlaysToCompletion",
                "StoryPreparationPlayModeTests.OpeningDialogue_ShowsRevisedSubtitlesWithoutStaleVoiceAudio",
                "StoryPreparationPlayModeTests.OpeningDialogue_UsesOnlyTheActiveCommonMouthAndKeepsGazeBounded",
                "StoryPreparationPlayModeTests.FaceRig_BlinksAndActiveSpeakerMouthActuallyMoves",
                "StoryPreparationPlayModeTests.PreparationScene_FinalBagWeightDialogueKeepsBothChildrenGrounded",
                "StoryChapterPlayModeTests.RebuildStoryRoute_PreservesOneSessionFlagsAndActBoundariesAcrossFourScenes",
                "StoryChapterPlayModeTests.EvacuationRebuild_AllMissingEquipmentUsesSafeFallbacksWithoutSoftlock",
                "StoryQuakeHumanPlayModeTests.QuakeRebuild_QuakeActionsUseRealWorldGesturesAndFeetStayPlantedUnderCover"}
        }));
    }

    [MenuItem("Tools/Deprem Story/KKTC/9 Capture All Chapter Cameras")]
    public static void CaptureAllChapterCameras()
    {
        string previous=SceneManager.GetActiveScene().path;
        try
        {
            foreach(string path in StoryKktcArtLibrary.Scenes)
            {
                EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                EditorApplication.ExecuteMenuItem("Tools/Deprem Story/QA/Project Visual/Capture All Cameras");
            }
        }
        finally {EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
    [MenuItem("Tools/Deprem Story/KKTC/6 Run Focused EditMode Checks")]
    public static void RunEditModeChecks()
    {
        testApi=ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
        testApi.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter
        {
            testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
            testNames=new[]{"StoryPreparationRebuildPreviewTests","StoryHomeSafetyRebuildPreviewTests","StoryQuakeRebuildPreviewTests",
                "StoryEvacuationRebuildPreviewTests","StoryRebuildFlowTests","StoryDialogueFaceRebuildTests","MeshyFamilyCharacterImporterTests"}
        }));
    }

    [MenuItem("Tools/Deprem Story/KKTC/7 Validate Camera Environment Coverage")]
    public static void ValidateEnvironmentCoverage()
    {
        string previous=SceneManager.GetActiveScene().path;
        List<string> report=new(){"Scene,Camera,Aspect,UncoveredPixels,PixelCount"};
        Directory.CreateDirectory("ClientExports/KKTC/Coverage");
        try
        {
            foreach(string path in StoryKktcArtLibrary.Scenes)
            {
                Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                Camera camera=Camera.main;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;
                var urp=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if(urp!=null)urp.renderPostProcessing=false;
                RenderSettings.fog=false;
                CinemachineCamera[] shots=StoryKktcArtLibrary.Transforms(scene).Select(t=>t.GetComponent<CinemachineCamera>()).Where(c=>c!=null).ToArray();
                foreach(var shot in shots)
                {
                    // Sky is intentional in the open neighborhood. All interior viewpoints
                    // and their authored short blends must be covered by actual meshes.
                    if(scene.name.Contains("04") && shot.transform.position.z>21)continue;
                    camera.transform.SetPositionAndRotation(shot.transform.position,shot.transform.rotation);camera.fieldOfView=shot.Lens.FieldOfView;
                    foreach(int height in new[]{480,585})
                        CoverageFrame(camera,scene.name,shot.name,270,height,report);
                }
                var brain=camera.GetComponent<CinemachineBrain>();
                if(brain!=null && brain.CustomBlends!=null)
                foreach(var blend in brain.CustomBlends.CustomBlends.Where(b=>b.Blend.Time>0))
                {
                    var from=shots.First(c=>c.name==blend.From);var to=shots.First(c=>c.name==blend.To);
                    if(scene.name.Contains("04") && (from.transform.position.z>21 || to.transform.position.z>21))continue;
                    foreach(float t in new[]{.25f,.5f,.75f})
                    {
                        camera.transform.SetPositionAndRotation(Vector3.Lerp(from.transform.position,to.transform.position,t),Quaternion.Slerp(from.transform.rotation,to.transform.rotation,t));
                        camera.fieldOfView=Mathf.Lerp(from.Lens.FieldOfView,to.Lens.FieldOfView,t);
                        CoverageFrame(camera,scene.name,from.name+"_to_"+to.name+"_"+t,270,585,report);
                    }
                }
            }
        }
        finally
        {
            File.WriteAllLines("ClientExports/KKTC/Reports/CameraCoverage.csv",report);
            EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);
        }
        Debug.Log("KKTC_CAMERA_COVERAGE_READY samples="+(report.Count-1));
    }

    static void CoverageFrame(Camera camera,string scene,string name,int width,int height,List<string> report)
    {
        RenderTexture target=RenderTexture.GetTemporary(width,height,24);
        RenderTexture previous=RenderTexture.active;var oldTarget=camera.targetTexture;
        Texture2D result=new(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;camera.aspect=(float)width/height;camera.Render();
            RenderTexture.active=target;result.ReadPixels(new Rect(0,0,width,height),0,0);result.Apply();
            int uncovered=result.GetPixels32().Count(c=>c.r>235 && c.g<25 && c.b>235);
            report.Add(scene+","+name+","+width+":"+height+","+uncovered+","+(width*height));
            if(uncovered>0)File.WriteAllBytes("ClientExports/KKTC/Coverage/"+scene+"_"+name+"_"+height+".png",result.EncodeToPNG());
        }
        finally{camera.targetTexture=oldTarget;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(result);}
    }
    [MenuItem("Tools/Deprem Story/KKTC/Inspect Scene Layouts")]
    public static void InspectLayouts()
    {
        string previous = SceneManager.GetActiveScene().path;
        foreach (string path in StoryKktcArtLibrary.Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var all = StoryKktcArtLibrary.Transforms(scene);
            var rows = new List<string>();
            foreach (Transform t in all.Where(t => t.GetComponent<CinemachineCamera>() != null ||
                t.GetComponent<StoryInteractable>() != null ||
                new[]{"WorldItem_", "EmergencyBag", "Packed_", "Story01_", "Kitchen", "Window", "Wall", "Floor", "Ceiling", "Review_Radio", "Bandage", "Soap", "WaterExpiry", "FirstAid", "SafeTable"}.Any(k=>t.name.StartsWith(k)) ||
                (t.parent!=null && t.parent.name=="SafeTable")))
            {
                var b = StoryKktcArtLibrary.BoundsOf(t);
                rows.Add(t.name + " active=" + t.gameObject.activeSelf + " p="+ t.position.ToString("F3") + " r="+t.eulerAngles.ToString("F1")+" size="+ b.size.ToString("F3") +" center="+b.center.ToString("F3"));
                foreach(var collider in t.GetComponents<Collider>())
                    rows.Add("  collider="+collider.GetType().Name+" enabled="+collider.enabled+" bounds="+collider.bounds);
                var visual=t.Find("KKTC_AuthoredVisual") ?? StoryKktcArtLibrary.Descendant(t,"KKTC_AuthoredVisual");
                if(visual!=null)rows.Add("  authored scale="+visual.lossyScale.ToString("F3")+" bounds="+StoryKktcArtLibrary.BoundsOf(visual));
                var cam=t.GetComponent<CinemachineCamera>();
                if(cam!=null) rows.Add("  FOV="+cam.Lens.FieldOfView+" follow="+cam.Follow+" look="+cam.LookAt+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c.GetType().Name)));
            }
            File.WriteAllLines("ClientExports/KKTC/Reports/"+scene.name+"_Layout.txt", rows);
            if(scene.name.Contains("04"))
            {
                string[] roots={"Story04_CityWorldBox","RebuildStreetDressing","RebuildAssemblySet"};
                var budget=all.Where(t=>roots.Contains(t.name)).SelectMany(t=>t.GetComponentsInChildren<Renderer>(true))
                    .Where(r=>r.enabled && r.gameObject.activeInHierarchy).Select(r=>r.name+" parent="+r.transform.parent.name+" size="+r.bounds.size.ToString("F2")+" mat="+r.sharedMaterial?.name);
                File.WriteAllLines("ClientExports/KKTC/Reports/ExteriorRendererInventory.txt",budget);
            }
        }
        EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
    }
    [MenuItem("Tools/Deprem Story/KKTC/3 Capture Hero Props")]
    public static void CaptureHeroProps()
    {
        string previous = SceneManager.GetActiveScene().path;
        Scene scene = EditorSceneManager.OpenScene(StoryKktcArtLibrary.Scenes[0], OpenSceneMode.Single);
        try
        {
            Transform[] all = StoryKktcArtLibrary.Transforms(scene);
            Camera camera = Camera.main;
            foreach (Transform t in all.Where(t => t.name.StartsWith("WorldItem_"))) t.gameObject.SetActive(false);
            foreach (var shot in new[]
            {
                ("01_Backpack_Open", "CM_PreparationBag_Rebuild", "EmergencyBag_Open_Packing"),
                ("02_Flashlight", "CM_PreparationFlashlight_Rebuild", "WorldItem_Flashlight"),
                ("03_Radio_Empty", "CM_PreparationRadio_Rebuild", "SignalRadioInspectionStage"),
                ("04_Backpack_Carried", "CM_PreparationDeniz_Rebuild", "EmergencyBag_Worn")
            })
            {
                Transform target = StoryKktcArtLibrary.Find(all, shot.Item3);
                if (target == null) throw new InvalidOperationException("Missing hero: " + shot.Item3);
                bool wasActive = target.gameObject.activeSelf;
                ActivateAncestors(target);
                if(shot.Item1=="01_Backpack_Open")
                    foreach(string item in new[]{"WorldItem_Flashlight","WorldItem_Radio","WorldItem_Water","WorldItem_Blanket"})
                        ActivateAncestors(StoryKktcArtLibrary.Find(all,item));
                CinemachineCamera authored = StoryKktcArtLibrary.Find(all, shot.Item2)?.GetComponent<CinemachineCamera>();
                if (authored == null) throw new InvalidOperationException("Missing hero camera: " + shot.Item2);
                camera.transform.SetPositionAndRotation(authored.transform.position, authored.transform.rotation);
                camera.fieldOfView = authored.Lens.FieldOfView;
                camera.nearClipPlane = .015f;
                Capture(camera, "ClientExports/KKTC/Hero/" + shot.Item1 + ".png", 720, 1280);
                Capture(camera, "ClientExports/KKTC/Hero/" + shot.Item1 + "_Tall.png", 720, 1560);
                if (shot.Item1 == "03_Radio_Empty")
                {
                    Transform battery = StoryKktcArtLibrary.Find(all, "Review_RadioBatteryLoose");
                    Transform slot = StoryKktcArtLibrary.Find(all, "Review_RadioBatterySlot");
                    ActivateAncestors(battery);
                    battery.SetPositionAndRotation(slot.position, slot.rotation);
                    Capture(camera, "ClientExports/KKTC/Hero/03_Radio_Inserted.png", 720, 1280);
                }
                target.gameObject.SetActive(wasActive);
                if(shot.Item1=="01_Backpack_Open")
                    foreach(Transform item in all.Where(t=>t.name.StartsWith("WorldItem_")))item.gameObject.SetActive(false);
            }
        }
        finally
        {
            // No temporary QA visibility/poses are saved into production scenes.
            EditorSceneManager.OpenScene(File.Exists(previous) ? previous : StoryKktcArtLibrary.Scenes[0], OpenSceneMode.Single);
        }
        Debug.Log("KKTC_HERO_RENDERS_READY");
    }

    public static void ActivateAncestors(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent) p.gameObject.SetActive(true);
    }

    public static void Capture(Camera camera, string path, int width, int height)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        float oldAspect = camera.aspect;
        RenderTexture texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = texture;
            camera.aspect = (float)width / height;
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget;
            camera.aspect = oldAspect;
            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(texture);
            Object.DestroyImmediate(image);
        }
    }
}
