using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Deprem.Story;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Editor-only acceptance checks and image capture; preserves the player's garage save.</summary>
[InitializeOnLoad]
public static class FiretruckRunnerQA
{
    private const string Out = "ClientExports/InfiniteRunner";
    private const string Key = "Deprem.FiretruckRunner.Garage.v2";
    private const string Pending = "RunnerQA.Active";
    private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static double readyAt;
    private static bool tested;
    static FiretruckRunnerQA()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
        EditorApplication.update += Tick;
    }
    [MenuItem("Tools/Deprem Story/Runner QA/Run acceptance")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before QA.");
        Directory.CreateDirectory(Out);
        SessionState.SetBool(Pending,true);
        SessionState.SetBool("RunnerQA.HadSave",PlayerPrefs.HasKey(Key));
        SessionState.SetString("RunnerQA.Save",PlayerPrefs.GetString(Key,""));
        File.WriteAllText(Out+"/garage-before-qa.json",PlayerPrefs.GetString(Key,"{}"));
        PlayerPrefs.SetString(Key,"{\"wallet\":500,\"shield\":0,\"magnet\":0,\"turbo\":0,\"bestScore\":0,\"bestDistance\":0}");
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene(StoryFiretruckRunnerSceneBuilder.ScenePath);
        tested=false;
        EditorApplication.isPlaying=true;
    }
    private static void OnPlayState(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Pending,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){readyAt=EditorApplication.timeSinceStartup+3;tested=false;}
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            if(SessionState.GetBool("RunnerQA.HadSave",false))PlayerPrefs.SetString(Key,SessionState.GetString("RunnerQA.Save",""));
            else PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();SessionState.SetBool(Pending,false);
            File.AppendAllText(Out+"/acceptance.txt","\nPlayer garage save restored after Play mode.\n");
        }
    }
    private static void Tick()
    {
        if(!EditorApplication.isPlaying || !SessionState.GetBool(Pending,false) || tested || EditorApplication.timeSinceStartup<readyAt)return;
        tested=true;
        var report=new List<string>();
        try { Check(Object.FindFirstObjectByType<FiretruckRunnerManager>(),report); }
        catch(Exception e){report.Add("FAIL: "+e);Debug.LogException(e);}
        File.WriteAllLines(Out+"/acceptance.txt",report);
        EditorApplication.isPlaying=false;
    }
    private static void Need(bool condition,string label,List<string> report)
    {
        report.Add((condition?"PASS: ":"FAIL: ")+label);
        if(!condition)throw new InvalidOperationException(label);
    }
    private static void Set(FiretruckRunnerManager m,string name,object value)=>typeof(FiretruckRunnerManager).GetField(name,Flags).SetValue(m,value);
    private static T Get<T>(FiretruckRunnerManager m,string name)=>(T)typeof(FiretruckRunnerManager).GetField(name,Flags).GetValue(m);
    private static void Call(FiretruckRunnerManager m,string name,params object[] args)=>typeof(FiretruckRunnerManager).GetMethod(name,Flags).Invoke(m,args);
    private static void Check(FiretruckRunnerManager m,List<string> r)
    {
        Need(m!=null,"Scene contains existing runner manager",r);
        // Keep test runs out of the shared minigame profile.
        m.resultReporter=null;
        Capture("garage",1920,1080);
        Capture("garage-portrait",1080,1920);
        Need(m.sections.Length==6 && m.sections.All(s=>s.root!=null),"Six authored track sections",r);
        Need(m.sections.Sum(s=>s.coins.Length)==90,"90 reusable coins",r);
        Need(m.guideAnimator!=null && m.guideAnimator.GetComponentInChildren<SkinnedMeshRenderer>()!=null,"Apo is a rigged 3D mesh",r);
        var skin=m.guideAnimator.GetComponentInChildren<SkinnedMeshRenderer>();
        Need(skin.sharedMesh.GetBlendShapeIndex("Blink")>=0,"Apo blink shape imported",r);
        foreach(var button in m.GetComponentsInParent<Transform>().First().root.GetComponentsInChildren<Button>(true))
            Need(button.onClick.GetPersistentEventCount()>0,button.name+" has a persistent callback",r);
        Need(m.Wallet==500,"Isolated test garage loaded",r);
        m.upgradeButtons[0].onClick.Invoke();
        Need(m.Wallet==455,"Shield upgrade charges 45 once",r);
        m.UpgradeMagnet();m.UpgradeTurbo();
        Need(m.Wallet==365,"Three upgrade purchases saved",r);
        string saved=PlayerPrefs.GetString(Key);
        Need(saved.Contains("\"shield\":1")&&saved.Contains("\"magnet\":1")&&saved.Contains("\"turbo\":1"),"Upgrade levels persisted to storage",r);
        m.StartRun();Set(m,"state",FiretruckRunnerManager.RunState.Driving);m.countdownText.gameObject.SetActive(false);
        m.MoveRight();Call(m,"MoveTruck",1f);
        Need(Mathf.Abs(m.truck.position.x-3.1f)<.01f,"Right control reaches right lane",r);
        m.MoveLeft();m.MoveLeft();Call(m,"MoveTruck",1f);
        Need(Mathf.Abs(m.truck.position.x+3.1f)<.01f,"Left control reaches left lane",r);
        float clock=Get<float>(m,"clock");
        m.Pause();float before=m.Distance;
        Call(m,"Update");
        Need(m.State==FiretruckRunnerManager.RunState.Paused&&m.Distance==before&&Get<float>(m,"clock")==clock,"Pause freezes distance and power clock",r);
        m.Resume();Need(m.State==FiretruckRunnerManager.RunState.Driving,"Resume returns to driving",r);
        m.ActivateTurbo();Need(m.TurboActive,"Turbo button grants turbo",r);
        float until=Get<float>(m,"turboUntil");m.ActivateTurbo();
        Need(Get<float>(m,"turboUntil")==until,"Manual turbo usable only once per run",r);
        Set(m,"turboUntil",0f);
        Call(m,"ApplyPower",FiretruckRunnerManager.PowerKind.Shield);
        Need(Mathf.Abs(Get<float>(m,"shieldUntil")-clock-10f)<.01f,"Shield level increases duration to 10s",r);
        var hazard=m.sections[0].hazards[0];Vector3 original=hazard.root.position;
        hazard.root.position=m.truck.position;hazard.passed=false;Set(m,"previousTruckX",m.truck.position.x);
        Call(m,"DetectHazards",.1f,.016f);
        Need(m.Health==3&&!m.ShieldActive,"Shield absorbs a real hazard overlap",r);
        hazard.root.gameObject.SetActive(true);hazard.passed=false;Set(m,"invulnerableUntil",0f);
        Call(m,"DetectHazards",.1f,.016f);
        Need(m.Health==2&&m.CollisionCount==1,"Unprotected overlap removes one life",r);
        hazard.passed=false;Call(m,"DetectHazards",.1f,.016f);
        Need(m.Health==2,"Grace period prevents stacked damage",r);
        hazard.root.position=original;
        var coin=m.sections[0].coins[0];coin.position=m.truck.position+Vector3.up;int count=m.CollectedCoinCount;
        Call(m,"DetectPickups",.1f,.016f);Call(m,"DetectPickups",.1f,.016f);
        Need(m.CollectedCoinCount==count+1,"Coin collected once",r);
        Call(m,"ApplyPower",FiretruckRunnerManager.PowerKind.Magnet);
        coin=m.sections[0].coins[1];coin.gameObject.SetActive(true);coin.position=m.truck.position+new Vector3(3,1,8);float d=Vector3.Distance(coin.position,m.truck.position);
        Call(m,"DetectPickups",.1f,.1f);
        Need(Vector3.Distance(coin.position,m.truck.position)<d,"Magnet attracts off-lane coins",r);
        int wallet=m.Wallet;
        Set(m,"distance",310f);Call(m,"UpdateChallenge");
        Need(m.Wallet>=wallet+15,"Distance challenge awards garage currency",r);
        int objects=m.transform.root.GetComponentsInChildren<Transform>(true).Length;
        for(int i=0;i<2500;i++)Call(m,"MoveTrack",8f);
        Need(m.RecycledSections>190,"20 km pool recycle simulation completed",r);
        Need(m.transform.root.GetComponentsInChildren<Transform>(true).Length==objects,"Object count stays constant after 20 km",r);
        var zs=m.sections.Select(s=>s.root.position.z).OrderBy(z=>z).ToArray();
        Need(zs.Zip(zs.Skip(1),(a,b)=>Mathf.Abs(b-a-96)<.02f).All(x=>x),"No gaps between recycled sections",r);
        Need(zs.Last()<600&&zs.First()>-125,"Floating coordinates remain bounded",r);
        Set(m,"clock",clock+1000);Need(!m.ShieldActive&&!m.MagnetActive&&!m.TurboActive,"Powers expire on the gameplay clock",r);
        Set(m,"health",1);Set(m,"invulnerableUntil",0f);Call(m,"RegisterHit");
        Need(m.State==FiretruckRunnerManager.RunState.Results,"Last life opens result panel",r);
        int banked=m.Wallet;Call(m,"BankRun");Call(m,"BankRun");
        Need(m.Wallet==banked,"Results cannot double-bank rewards",r);
        m.Restart();Need(m.Health==3&&m.Distance==0&&m.CollectedCoinCount==0,"Retry resets gameplay state",r);
        Need(m.sections.All(s=>s.coins.All(c=>c.gameObject.activeSelf)),"Retry restores every collectible",r);
        Set(m,"state",FiretruckRunnerManager.RunState.Driving);m.countdownText.gameObject.SetActive(false);
        Set(m,"distance",220f);Set(m,"score",760);Set(m,"coinCount",24);
        Call(m,"MoveTrack",32f);Call(m,"UpdateHud");
        m.guidePanel.SetActive(false);
        Capture("gameplay",1920,1080);Capture("gameplay-portrait",1080,1920);
        Call(m,"Say","Ben Apo! Açık şeridi takip et; engeller yaklaşmadan yönünü seç.",10f);
        Capture("apo-guide",1920,1080);
        m.Pause();Capture("pause",1920,1080);
        r.Add("QA completed; screenshots exported in "+Out);
    }

    [MenuItem("Tools/Deprem Story/Runner QA/Capture current")]
    public static void CaptureCurrent(){Capture("current",1920,1080);Capture("current-portrait",1080,1920);}
    [MenuItem("Tools/Deprem Story/Runner QA/Inspect Apo")]
    public static void InspectApo()
    {
        var m=Object.FindFirstObjectByType<FiretruckRunnerManager>();
        var skin=m.guideAnimator.GetComponentInChildren<SkinnedMeshRenderer>();
        var camera=GameObject.Find("ApoPortraitCamera").GetComponent<Camera>();
        var report=new List<string>();
        report.Add("Skin: "+skin.name+" vertices="+skin.sharedMesh.vertexCount);
        for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)report.Add(skin.sharedMesh.GetBlendShapeName(i)+"="+skin.GetBlendShapeWeight(i));
        for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)skin.SetBlendShapeWeight(i,0);
        foreach(var material in skin.sharedMaterials)report.Add("Material="+material.name+" color="+material.color);
        var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(768,768,24);var prior=camera.targetTexture;
        camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();
        File.WriteAllBytes(Out+"/apo-unity.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
        camera.targetTexture=prior;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        File.WriteAllLines(Out+"/apo-import.txt",report);
    }
    public static void Capture(string name,int width,int height)
    {
        Directory.CreateDirectory(Out);
        var camera=Camera.main;
        var canvases=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
        var modes=canvases.Select(c=>c.renderMode).ToArray();
        var cameras=canvases.Select(c=>c.worldCamera).ToArray();
        var distances=canvases.Select(c=>c.planeDistance).ToArray();
        var previous=RenderTexture.active;var prior=camera.targetTexture;
        var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
        try
        {
            camera.targetTexture=target;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            Canvas.ForceUpdateCanvases();
            foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))if(c!=camera&&c.targetTexture!=null)c.Render();
            camera.Render();RenderTexture.active=target;
            var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();
            File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
        }
        finally
        {
            camera.targetTexture=prior;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
            Canvas.ForceUpdateCanvases();
        }
    }
}

