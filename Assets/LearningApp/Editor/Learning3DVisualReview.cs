using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Deprem.Accessibility;
using Deprem.Minigames;
using Deprem.Story;
using Microsoft.Win32;
using TMPro;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deprem.Learning.Editor
{
    public static class Learning3DVisualReview
    {
        const string Folder="ClientExports/DepremApp/Visual3D", Prefix="Deprem.YanYana.v1.";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static readonly List<string> report=new List<string>(), errors=new List<string>();
        static readonly Dictionary<string,byte[]> files=new Dictionary<string,byte[]>();
        static readonly Dictionary<string,object> prefs=new Dictionary<string,object>();
        static string oldStore; static float oldVolume; static int oldSize; static bool running;
        static bool navigationOnly, bridgeOnly, adventureOnly, scenarioOnly;
        static string ReportPath=>Folder+(adventureOnly?"/adventure.txt":scenarioOnly?"/scenario.txt":bridgeOnly?"/bridge.txt":navigationOnly?"/navigation.txt":"/verification.txt");
        static EditorWindow View=>EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        static PropertyInfo Size=>View.GetType().GetProperty("selectedSizeIndex",Private|BindingFlags.Public);
        static void Dimensions(int w,int h)=>typeof(MobilePortraitGameView).GetMethod("ConfigureAndSelect",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{w,h,"Visual 3D "+w+"x"+h,false});
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
        static IEnumerable<string> PrefKeys()
        {
            using(var registry=Registry.CurrentUser.OpenSubKey(@"Software\Unity\UnityEditor\"+PlayerSettings.companyName+"\\"+PlayerSettings.productName))
                return registry?.GetValueNames().Where(n=>n.StartsWith(Prefix)).Select(n=>n.Substring(0,n.LastIndexOf("_h",StringComparison.Ordinal))).ToArray()??Array.Empty<string>();
        }
        [MenuItem("Tools/Deprem App/Review/3D Visual Verification")]
        static void Start()=>Begin(false);
        [MenuItem("Tools/Deprem App/Review/3D Navigation Verification")]
        static void Navigation()=>Begin(true);
        [MenuItem("Tools/Deprem App/Review/3D Bridge Verification")]
        static void Bridge()=>Begin(true,true);
        [MenuItem("Tools/Deprem App/Review/3D Adventure Verification")]
        static void Adventure()=>Begin(false,false,true);
        [MenuItem("Tools/Deprem App/Review/3D Scenario Presentation Verification")]
        static void Scenario()=>Begin(false,false,false,true);
        static void Begin(bool navigation,bool bridge=false,bool adventure=false,bool scenario=false)
        {
            Check(EditorApplication.isPlaying&&!running,"Enter Play mode; one review at a time.");
            running=true;navigationOnly=navigation;bridgeOnly=bridge;adventureOnly=adventure;scenarioOnly=scenario;report.Clear();errors.Clear();files.Clear();prefs.Clear();Directory.CreateDirectory(Folder);
            oldStore=LearningProgress.Current.SavePath;oldVolume=AudioListener.volume;oldSize=(int)Size.GetValue(View);
            foreach(var name in new[]{"story-session.json","minigame-profile.json"}){string path=Path.Combine(Application.persistentDataPath,name);files[path]=File.Exists(path)?File.ReadAllBytes(path):null;}
            foreach(var key in PrefKeys())
            {
                string str=PlayerPrefs.GetString(key,"__NOT_STRING__");
                prefs[key]=str!="__NOT_STRING__"?(object)str:key.EndsWith("playSeconds")?(object)PlayerPrefs.GetFloat(key):PlayerPrefs.GetInt(key);
            }
            EditorApplication.LockReloadAssemblies();EditorApplication.playModeStateChanged+=OnState;Application.logMessageReceived+=Log;
            File.WriteAllText(ReportPath,"RUNNING\n");
            EditorCoroutineUtility.StartCoroutineOwnerless(Guard(Run()));
        }
        static void OnState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Restore();}
        static IEnumerator Guard(IEnumerator routine)
        {
            while(running){object next;try{if(!routine.MoveNext())break;next=routine.Current;}catch(Exception e){report.Add("FAIL: "+e);break;}yield return next;}
            Restore();
        }
        static void Restore()
        {
            if(!running)return;running=false;Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=OnState;
            try
            {
                if(EditorApplication.isPlaying){if(StoryGameManager.Instance!=null)UnityEngine.Object.DestroyImmediate(StoryGameManager.Instance.gameObject);SceneManager.LoadScene(LearningGameBridge.SceneName);}
                foreach(var key in PrefKeys())PlayerPrefs.DeleteKey(key);
                foreach(var pair in prefs){if(pair.Value is string s)PlayerPrefs.SetString(pair.Key,s);else if(pair.Value is float f)PlayerPrefs.SetFloat(pair.Key,f);else PlayerPrefs.SetInt(pair.Key,(int)pair.Value);}PlayerPrefs.Save();
                foreach(var file in files){if(file.Value==null){if(File.Exists(file.Key))File.Delete(file.Key);}else File.WriteAllBytes(file.Key,file.Value);}
                LearningProgress.UseVerificationStore(oldStore);AudioListener.volume=oldVolume;Size.SetValue(View,oldSize);
                report.Add(errors.Count==0?"PASS: no runtime errors.":"FAIL runtime errors: "+string.Join("; ",errors));
                report.Add("RESTORED: learning store, story/minigame saves, Yan Yana preferences, audio and Game View.");File.WriteAllLines(ReportPath,report);
            }
            finally{EditorApplication.UnlockReloadAssemblies();}
        }
        static void Capture(string name)=>ScreenCapture.CaptureScreenshot(Folder+"/"+name+".png");
        static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name);
        static void Click(string name)=>CheckButton(name,true);
        static void CheckButton(string name,bool click=false)
        {
            var b=Button(name);Check(b!=null&&b.isActiveAndEnabled&&b.interactable,"Missing button "+name);
            var rect=(RectTransform)b.transform;var canvas=b.GetComponentInParent<Canvas>();var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(cam,rect.TransformPoint(rect.rect.center)),pointerId=-1,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==b.gameObject,"Picture blocked button "+name);
            if(click)ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        static void Audit(string scene)
        {
            var adapter=UnityEngine.Object.FindFirstObjectByType<ReadingFree3D>();Check(adapter!=null&&adapter.ConvertedLabelCount>0,scene+": adapter missing");
            var graphics=UnityEngine.Object.FindObjectsByType<ReadingFreeIcon>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Check(graphics.All(g=>!g.raycastTarget),scene+": guide intercepts input");
            foreach(var g in graphics.Where(g=>g.Kind.StartsWith("item-")||g.Kind.StartsWith("icon-")))Check(Resources.Load<Texture2D>("LearningApp/Art/"+g.Kind)!=null,"Missing picture "+g.Kind);
            var text=UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.enabled&&t.GetComponentInParent<Canvas>()!=null&&!string.IsNullOrWhiteSpace(t.text)&&!t.name.StartsWith("KKTC_")&&t.text!="+"&&t.text!="-"&&t.text!="−").ToArray();
            Check(text.All(t=>t.name=="Visual caption"&&t.text.Length<=48),scene+": long written instructions remain "+string.Join(",",text.Where(t=>t.name!="Visual caption"||t.text.Length>48).Select(t=>t.name)));
            foreach(var caption in text)Check(!caption.raycastTarget,"Caption intercepts input");
            report.Add("PASS presentation: "+scene+"; "+adapter.ConvertedLabelCount+" converted labels; non-blocking guides; artwork resolved.");
            File.WriteAllLines(ReportPath,report);
        }
        static IEnumerator Run()
        {
            LearningProgress.UseVerificationStore(Path.GetFullPath(".codex_tmp/learning-app/3d-"+Guid.NewGuid().ToString("N")+".json"));AudioListener.volume=0;Dimensions(750,1334);
            if(adventureOnly)
            {
                Time.timeScale=1;SceneManager.LoadScene("YanYana_Adventure");yield return new EditorWaitForSeconds(2);
                Audit("Yan Yana full prepared route");
                string routePath="ClientExports/YanYana/Reports/physical-recorded-route-2.txt";
                Check(EditorApplication.ExecuteMenuItem("Tools/Yan Yana/QA/Record Full Prepared Route"),"Adventure route menu missing");
                yield return null;
                var routeState=typeof(YanYana.Editor.YanYanaPhysicalQA).GetField("recordedRouteRunning",BindingFlags.NonPublic|BindingFlags.Static);
                double deadline=EditorApplication.timeSinceStartup+900,nextCapture=EditorApplication.timeSinceStartup+15;int frame=0;
                while((bool)routeState.GetValue(null))
                {
                    Check(EditorApplication.timeSinceStartup<deadline,"Adventure route exceeded verification timeout");
                    if(EditorApplication.timeSinceStartup>nextCapture){Capture("adventure-play-"+(++frame).ToString("00"));nextCapture+=45;}
                    yield return new EditorWaitForSeconds(.5f);
                }
                string routeReport=File.ReadAllText(routePath);
                Check(routeReport.Contains("COMPLETE Ending=2")&&!routeReport.Contains("FAIL"),"Adventure route failed: "+routeReport);
                report.Add("PASS: complete prepared adventure through preparation, earthquake, aftershock, evacuation, professional response, supplies, official broadcast and family reunion.");
                Capture("adventure-reunion");yield return new EditorWaitForSeconds(.3f);yield break;
            }
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>Path.GetFileNameWithoutExtension(s.path)).Where(ReadingFree3D.Supports)
                .Where(s=>!navigationOnly||s=="YanYana_Adventure"||s=="Story_Rebuild_MainMenu"||s.EndsWith("RebuildPreview"))
                .Where(s=>!bridgeOnly||s=="Story_Rebuild_MainMenu").ToArray();
            if(scenarioOnly)scenes=scenes.Where(s=>s=="Minigame_Hub"||s=="Minigame_Evacuation_25D").ToArray();
            foreach(string scene in scenes)
            {
                Time.timeScale=1;SceneManager.LoadScene(scene);yield return new EditorWaitForSeconds(2);Audit(scene);Capture(scene);yield return new EditorWaitForSeconds(.2f);
                if(scene=="Minigame_Hub")
                {
                    var scroll=UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(s=>s.name=="CardsViewport");
                    foreach(float position in new[]{1f,.5f,0f})
                    {
                        scroll.verticalNormalizedPosition=position;Canvas.ForceUpdateCanvases();yield return new EditorWaitForSeconds(.15f);
                        CheckButton("ScenarioJourneyButton");
                    }
                    scroll.verticalNormalizedPosition=1;report.Add("PASS input: scenario button remains reachable while scrolling the game cards.");
                }
                if(scenarioOnly&&scene=="Minigame_Evacuation_25D")
                {
                    var interactions=UnityEngine.Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    var stages=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>System.Text.RegularExpressions.Regex.IsMatch(t.name,@"^Stage_\d{2}$")).ToArray();
                    var cameraController=UnityEngine.Object.FindFirstObjectByType<StoryCameraController>();
                    foreach(var step in new[]{("evac25d.street.03.gas_valve","Stage_03","street-report-gas"),("evac25d.street.04.rubble_check","Stage_04","street-avoid-rubble")})
                    {
                        foreach(var item in interactions)item.SetAvailable(item.InteractionId==step.Item1);
                        foreach(var stage in stages)stage.gameObject.SetActive(stage.name==step.Item2);
                        var target=interactions.Single(i=>i.InteractionId==step.Item1);
                        cameraController.ActivateZone(target.FocusCameraZone,true);yield return new EditorWaitForSeconds(.7f);
                        var camera=Camera.main;var point=camera.WorldToViewportPoint(target.InteractionPoint.position);
                        Check(point.z>0&&point.x>.02f&&point.x<.98f&&point.y>.15f&&point.y<.85f,"Safe response target is outside the usable frame: "+step.Item1+" "+point);
                        var ray=camera.ScreenPointToRay(camera.WorldToScreenPoint(target.InteractionPoint.position));
                        Check(Physics.Raycast(ray,out var hit,150f,~0,QueryTriggerInteraction.Collide)&&hit.collider.GetComponentInParent<StoryInteractable>()==target,"Safe response picture is not touchable: "+step.Item1);
                        Audit(step.Item3);Capture(step.Item3);yield return new EditorWaitForSeconds(.2f);
                    }
                    report.Add("PASS input: reporting and open-route targets are framed and physically selectable away from the hazards. Presentation previews only.");
                }
                if(scene=="Story_Rebuild_MainMenu" && LearningGameBridge.Instance!=null)
                {
                    var bridge=LearningGameBridge.Instance;
                    typeof(LearningGameBridge).GetMethod("RequestReturn",Private).Invoke(bridge,null);yield return new EditorWaitForSeconds(.2f);
                    var root=bridge.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
                    Check(bridge.GetComponent<UnityEngine.UIElements.UIDocument>().panelSettings.sortingOrder>
                        UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Max(c=>c.sortingOrder),"Return dialog is behind a scene Canvas");
                    var resume=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root,"Resume3DGame");
                    Check(resume!=null&&resume.text==""&&Time.timeScale==0,"Child bridge dialog missing or not paused");Capture("return-dialog");yield return new EditorWaitForSeconds(.2f);
                    typeof(UnityEngine.UIElements.Clickable).GetMethod("SimulateSingleClick",Private).Invoke(resume.clickable,new object[]{null,0});
                    Check(Time.timeScale==1,"Bridge resume did not restore time");report.Add("PASS: short captions and icons in bridge return/resume dialog.");
                }
                var ui=UnityEngine.Object.FindFirstObjectByType<StoryUIController>();
                if(ui!=null)
                {
                    ui.TogglePause();yield return new EditorWaitForSeconds(.2f);Check((bool)typeof(StoryUIController).GetField("paused",Private).GetValue(ui),"Pause state missing");
                    foreach(var group in Button("ResumeButton").GetComponentsInParent<CanvasGroup>())Check(group.alpha>.9f,"Pause controls are transparent");
                    Capture(scene+"-pause");yield return new EditorWaitForSeconds(.2f);Click("ResumeButton");yield return new EditorWaitForSeconds(.15f);Check(!(bool)typeof(StoryUIController).GetField("paused",Private).GetValue(ui),"Resume state missing");report.Add("PASS input: "+scene+" visible pause/resume via raycast.");
                    if(scene=="Story_01_RebuildPreview")
                    {
                        bool advanced=false;ui.ShowSubtitle("Çantayı birlikte hazırlayalım.",3,()=>advanced=true);yield return new EditorWaitForSeconds(.2f);
                        Check(ui.SubtitleRevealComplete&&!advanced,"Child dialogue waited for invisible typewriter / skipped narration");
                        Check(ui.TryHandlePrimaryTap(),"Picture dialogue cannot advance");yield return new EditorWaitForSeconds(.3f);Check(advanced,"Picture dialogue callback did not run");
                        report.Add("PASS: muted picture dialogue reveals immediately and advances through its existing tap callback.");
                    }
                }
                var manager=UnityEngine.Object.FindFirstObjectByType<MinigameSessionManager>();
                if(manager!=null&&!manager.ExternalResultOnly)
                {
                    for(int i=0;i<manager.StageCount;i++)
                    {
                        typeof(MinigameSessionManager).GetMethod("BeginStage",Private).Invoke(manager,new object[]{i});yield return new EditorWaitForSeconds(.6f);Audit(scene+" stage "+i);
                        if(i==manager.StageCount-1){Capture(scene+"-last-stage");yield return new EditorWaitForSeconds(.15f);}
                    }
                    // Stage previews inspect presentation only; they do not claim gameplay completion.
                }
                if(scene=="YanYana_Adventure")
                {
                    Click("New");yield return new EditorWaitForSeconds(3);Audit(scene+" intro");Capture("YanYana-intro");yield return new EditorWaitForSeconds(.2f);
                    Click("Pause");yield return new EditorWaitForSeconds(.3f);Capture("YanYana-pause");yield return new EditorWaitForSeconds(.2f);Click("Resume");yield return new EditorWaitForSeconds(.3f);
                    Check(UnityEngine.Object.FindFirstObjectByType<ReadingFreeYanYanaGuide>()!=null,"Yan Yana guide missing");report.Add("PASS input: Yan Yana new/pause/resume via raycast.");
                }
            }
            if(bridgeOnly)yield break;
            Dimensions(1536,2048);SceneManager.LoadScene("Story_01_RebuildPreview");yield return new EditorWaitForSeconds(2);Audit("tablet Story 01");Capture("tablet-story");yield return new EditorWaitForSeconds(.2f);
            LearningProgress.Current.Data.Active.adult=true;SceneManager.LoadScene("Minigame_Hub");yield return new EditorWaitForSeconds(1);
            Check(UnityEngine.Object.FindFirstObjectByType<ReadingFree3D>()==null,"Adult UI unexpectedly converted");Check(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t=>t.enabled&&!string.IsNullOrWhiteSpace(t.text)),"Adult labels missing");report.Add("PASS: adult text preserved.");
            if(scenarioOnly){CheckButton("ScenarioJourneyButton");Capture("scenario-adult-tablet");yield return new EditorWaitForSeconds(.2f);}
        }
    }
}
