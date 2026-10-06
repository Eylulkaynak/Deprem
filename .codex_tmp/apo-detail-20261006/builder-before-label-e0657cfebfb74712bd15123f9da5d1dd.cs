using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Minigames;
using Deprem.Story;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class StoryFiretruckRunnerSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Story_04_FiretruckRunner.unity";
    private const string Art = "Assets/Story/Generated/InfiniteRunner";
    private const string Apo = "Assets/Story/Characters/ApoOriginal";
    private const string Town = "Assets/Story/Environment/SyntyTown";
    private const string City = "Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs";
    private static readonly Color Navy = Hex("#13263D"), Cream = Hex("#F6F2E8"), Teal = Hex("#36D6BC"), Gold = Hex("#FFD05D");
    private static TMP_FontAsset regular, bold;
    private static Sprite rounded, coinIcon;
    private static Material asphalt, white, orange, navy, teal, gold, concrete, grass, metal;

    [MenuItem("Tools/Deprem Story/Build Story 04 Firetruck Runner")]
    public static void BuildFromMenu() => Build(true);
    [MenuItem("Tools/Deprem Story/Build Story 04 Firetruck Runner (Silent)")]
    public static void BuildSilentFromMenu() => Build(false);
    public static void BuildFromCommandLine() => Build(false);

    private static void Build(bool showDialog)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
        var active = EditorSceneManager.GetActiveScene();
        if (active.isDirty && !string.IsNullOrEmpty(active.path)) EditorSceneManager.SaveScene(active);
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(out regular, out _, out bold);
        rounded = MakeRoundedSprite();
        coinIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Story/Collectibles/IMOCoin/IMOCoin_UI_Icon.png");
        asphalt = Mat("Asphalt", "#354D60"); white = Mat("Ivory", "#F6F0D9");
        orange = Mat("Coral", "#F2714D"); navy = Mat("Navy", "#19374F");
        teal = Mat("Mint", "#32D8C1"); gold = Mat("Gold", "#FFC649", .36f);
        concrete = Mat("Pavement", "#C1C7C0"); grass = Mat("Grass", "#75AE72"); metal = Mat("Metal", "#738992");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = Child(null, "STORY_04_FIRETRUCK_RUNNER");
        var manager = Child(root, "_FiretruckRunnerManager").gameObject.AddComponent<FiretruckRunnerManager>();
        var progress = manager.gameObject.AddComponent<MinigameProgressManager>();
        var reporter = manager.gameObject.AddComponent<MinigameSessionManager>();
        var settings = new SerializedObject(reporter);
        settings.FindProperty("minigameId").stringValue = "firetruck-runner";
        settings.FindProperty("displayName").stringValue = "ACİL ROTA • SONSUZ SÜRÜŞ";
        settings.FindProperty("externalResultOnly").boolValue = true;
        settings.FindProperty("progressManager").objectReferenceValue = progress;
        settings.ApplyModifiedPropertiesWithoutUndo();
        manager.resultReporter = reporter;
        BuildTrack(root, manager);
        BuildTruck(root, manager);
        BuildCameraAndLight(root, manager);
        BuildHud(root, manager);
        BuildApo(root, manager);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = manager.truck.gameObject;
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 0, 28), Quaternion.Euler(36,0,0), 48);
        Debug.Log("Infinite Runner authored: 6 reusable road sections, 3 powers, garage and original Apo.");
        if (showDialog) EditorUtility.DisplayDialog("Acil Rota", "Sonsuz runner ve Apo hazır.", "Tamam");
    }

    private static void BuildTrack(Transform root, FiretruckRunnerManager manager)
    {
        Transform pool = Child(root, "SceneAuthoredTrackPool");
        manager.sections = new FiretruckRunnerManager.TrackSection[6];
        string[] buildings = { "Env_ResidentBuilding_01.prefab", "Env_CommercialBuilding_02.prefab",
            "Env_ResidentBuilding_03.prefab", "Env_CompanyBuilding_01.prefab", "Env_ResidentBuilding_05.prefab", "Env_CommercialBuilding_04.prefab" };
        for (int s = 0; s < 6; s++)
        {
            Transform section = Child(pool, "Section_" + s + "_" + new[] { "Mahalle", "Park", "CalismaAlani" }[s % 3]);
            // Author around origin, then move the complete block.
            Box(section, "Asphalt", new Vector3(0,-.10f,48), new Vector3(10.6f,.2f,96), asphalt);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(section, "Sidewalk", new Vector3(side*6.55f,.02f,48), new Vector3(2.5f,.25f,96), concrete);
                Box(section, "Grass", new Vector3(side*12,-.09f,48), new Vector3(8.4f,.20f,96), grass);
                Box(section, "RoadEdge", new Vector3(side*5.12f,.021f,48), new Vector3(.13f,.027f,96), white);
                for (int k = 0; k < 12; k++)
                {
                    Box(section,"Curb",new Vector3(side*5.3f,.16f,k*8+2),new Vector3(.18f,.24f,3.8f), k%2==0?white:orange);
                    for (int div = -1; div <= 1; div += 2)
                        Box(section,"LaneMark",new Vector3(div*1.55f,.025f,k*8+1),new Vector3(.105f,.025f,3.5f),white);
                }
                for (int k = 0; k < 4; k++)
                {
                    float z = 7 + k * 24;
                    if (s % 3 != 1 || k % 2 == 0)
                        Asset(City+"/"+buildings[(s+k+(side>0?2:0))%buildings.Length], "CityFacade", section,
                            new Vector3(side*11.8f,.15f,z), new Vector3(8,12+(s+k)%4*2.5f,18), new Vector3(0,side*-90,0));
                    else
                    {
                        for (int t = 0; t < 3; t++) Tree(section, new Vector3(side*(9.5f+t%2*2.4f),.15f,z+t*4));
                        Box(section,"ParkBench",new Vector3(side*7.3f,.62f,z),new Vector3(.8f,.16f,2),orange);
                    }
                    StreetLight(section,new Vector3(side*6.4f,0,z+7));
                }
            }
            var hazards = new List<FiretruckRunnerManager.Hazard>();
            var coins = new List<Transform>();
            var powers = new List<FiretruckRunnerManager.PowerPickup>();
            // Each row leaves a full free lane. At least 32m between decisions.
            for (int row = 0; row < 3; row++)
            {
                int safeLane = (s + row * 2) % 3 - 1;
                float z = 22 + row * 32;
                if (s == 0 && row == 0) safeLane = 0;
                if (!(s == 0 && row == 0))
                {
                    for (int lane = -1; lane <= 1; lane++)
                    {
                        if (lane == safeLane) continue;
                        // Some rows offer two exits instead of always blocking two lanes.
                        if (row == 1 && lane == (safeLane == 0 ? -1 : 0)) continue;
                        int kind = (s * 3 + row + lane + 1) % 5;
                        hazards.Add(BuildHazard(section, new Vector3(lane * 3.1f, .10f, z), kind));
                    }
                }
                for (int c = 0; c < 5; c++)
                {
                    Transform coin = Child(section,"Coin_"+row+"_"+c);
                    coin.localPosition = new Vector3(safeLane*3.1f,1.15f,z-11+c*3.1f);
                    Coin(coin);
                    coins.Add(coin);
                }
                if (row == 1)
                {
                    var kind = (FiretruckRunnerManager.PowerKind)(s % 3);
                    Transform power = Child(section,kind.ToString()+"Pickup");
                    power.localPosition = new Vector3(safeLane*3.1f,1.55f,z-5f);
                    PowerIcon(power,kind);
                    powers.Add(new FiretruckRunnerManager.PowerPickup { root=power, kind=kind });
                }
            }
            // An overhead district marker turns the route into readable milestones.
            Box(section,"RouteGateL",new Vector3(-5.2f,3.8f,8),new Vector3(.24f,7.6f,.24f),teal);
            Box(section,"RouteGateR",new Vector3(5.2f,3.8f,8),new Vector3(.24f,7.6f,.24f),teal);
            Box(section,"RouteGateTop",new Vector3(0,7.45f,8),new Vector3(10.6f,.32f,.3f),teal);
            var board=Box(section,"DistrictSign",new Vector3(0,7.8f,8),new Vector3(3.5f,.75f,.2f),navy);
            WorldText(board.transform,"ROTA 0"+(s+1),new Vector3(0,0,-.52f),1.2f,white.color);
            section.localPosition = new Vector3(0,0,s*96-24);
            manager.sections[s]=new FiretruckRunnerManager.TrackSection { root=section, hazards=hazards.ToArray(), coins=coins.ToArray(), powers=powers.ToArray() };
        }
    }

    private static FiretruckRunnerManager.Hazard BuildHazard(Transform section, Vector3 p, int kind)
    {
        Transform root=Child(section,new[]{"StripedBarrier","TrafficCones","StalledPickup","CargoCrates","OncomingTraffic"}[kind]);
        root.localPosition=p;
        Vector2 bounds=new Vector2(1.05f,.48f);
        if (kind==0)
        {
            Box(root,"Barrier",new Vector3(0,.88f,0),new Vector3(2.25f,.64f,.38f),white);
            for(int i=-2;i<=2;i++) Box(root,"ReflectiveStripe",new Vector3(i*.46f,.88f,-.21f),new Vector3(.24f,.70f,.04f),orange,new Vector3(0,0,-24));
            for(int i=-1;i<=1;i+=2) { Box(root,"Leg",new Vector3(i*.85f,.38f,0),new Vector3(.13f,.75f,.15f),metal); Box(root,"Foot",new Vector3(i*.85f,.07f,0),new Vector3(.48f,.13f,.65f),navy); }
            for(int i=-1;i<=1;i+=2) Ball(root,"Beacon",new Vector3(i*.92f,1.3f,0),new Vector3(.17f,.17f,.17f),gold);
        }
        else if(kind==1)
        {
            for(int i=-1;i<=1;i++) Asset(City+"/Prop_RoadCone_01.prefab","Cone",root,root.position+new Vector3(i*.68f,0,Mathf.Abs(i)*.35f),new Vector3(.62f,1.02f,.62f),Vector3.zero);
            bounds=new Vector2(1.03f,.7f);
        }
        else if(kind==2 || kind==4)
        {
            Asset(Town+"/SM_Veh_Pickup_01.fbx","Vehicle",root,root.position,new Vector3(2.15f,1.8f,4.3f),new Vector3(0,kind==4?180:0,0));
            bounds=new Vector2(1.02f,2.1f);
        }
        else
        {
            Asset(Town+"/SM_Prop_CardboardBox_01.fbx","Crate",root,root.position,new Vector3(1.5f,1.45f,1.5f),new Vector3(0,12,0));
            Asset(Town+"/SM_Prop_CardboardBox_01.fbx","CrateSmall",root,root.position+new Vector3(.64f,0,-.48f),new Vector3(.75f,.78f,.75f),new Vector3(0,-13,0));
            bounds=new Vector2(.95f,.9f);
        }
        return new FiretruckRunnerManager.Hazard { root=root,halfExtents=bounds,moving=kind==4 };
    }

    private static void Coin(Transform parent)
    {
        var go=Primitive(parent,"CoinRim",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.82f,.075f,.82f),gold,new Vector3(90,0,0));
        for(int side=-1;side<=1;side+=2)
        {
            var face=Child(parent,"ImoFace");
            face.localPosition=new Vector3(0,0,side*.079f);
            face.localRotation=Quaternion.Euler(0,side>0?180:0,0);
            var sr=face.gameObject.AddComponent<SpriteRenderer>();sr.sprite=coinIcon;
            if(coinIcon!=null) face.localScale=Vector3.one*.79f/coinIcon.bounds.size.x;
        }
    }
    private static void PowerIcon(Transform root,FiretruckRunnerManager.PowerKind kind)
    {
        Material m=kind==FiretruckRunnerManager.PowerKind.Shield?teal:kind==FiretruckRunnerManager.PowerKind.Magnet?orange:gold;
        Primitive(root,"PowerBase",PrimitiveType.Cylinder,new Vector3(0,-.65f,0),new Vector3(1.15f,.08f,1.15f),m);
        if(kind==FiretruckRunnerManager.PowerKind.Shield)
        {
            var mesh=new Mesh {name="ShieldMesh"};
            mesh.vertices=new[]{new Vector3(-.52f,.52f,0),new Vector3(.52f,.52f,0),new Vector3(.48f,-.12f,0),new Vector3(0,-.56f,0),new Vector3(-.48f,-.12f,0)};
            mesh.triangles=new[]{0,2,1,0,3,2,0,4,3,0,1,2,0,2,3,0,3,4};mesh.RecalculateNormals();
            string path=Art+"/Shield.asset";var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(asset==null){AssetDatabase.CreateAsset(mesh,path);asset=mesh;}else Object.DestroyImmediate(mesh);
            var face=Child(root,"Shield");face.gameObject.AddComponent<MeshFilter>().sharedMesh=asset;face.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;
            Box(root,"CrossV",new Vector3(0,0,-.03f),new Vector3(.13f,.52f,.06f),white);
            Box(root,"CrossH",new Vector3(0,0,-.03f),new Vector3(.44f,.13f,.06f),white);
        }
        else if(kind==FiretruckRunnerManager.PowerKind.Magnet)
        {
            Box(root,"MagnetL",new Vector3(-.32f,.08f,0),new Vector3(.25f,.85f,.25f),m);
            Box(root,"MagnetR",new Vector3(.32f,.08f,0),new Vector3(.25f,.85f,.25f),m);
            Box(root,"MagnetBridge",new Vector3(0,-.34f,0),new Vector3(.88f,.25f,.25f),m);
            for(int s=-1;s<=1;s+=2) Box(root,"MetalTip",new Vector3(s*.32f,.46f,0),new Vector3(.25f,.20f,.25f),white);
        }
        else
        {
            for(int k=0;k<2;k++)
            {
                Box(root,"Bolt",new Vector3(-.13f,-.2f+k*.42f,0),new Vector3(.27f,.57f,.18f),m,new Vector3(0,0,-32));
                Box(root,"Bolt",new Vector3(.13f,-.2f+k*.42f,0),new Vector3(.27f,.57f,.18f),m,new Vector3(0,0,32));
            }
        }
    }
    private static void Tree(Transform parent,Vector3 p)
    {
        Primitive(parent,"TreeTrunk",PrimitiveType.Cylinder,p+Vector3.up*1.3f,new Vector3(.35f,1.3f,.35f),metal);
        Ball(parent,"TreeCrown",p+Vector3.up*3.2f,new Vector3(2.5f,3.4f,2.5f),teal);
    }
    private static void StreetLight(Transform parent,Vector3 p)
    {
        Box(parent,"StreetLamp",p+Vector3.up*2.9f,new Vector3(.11f,5.8f,.11f),navy);
        Box(parent,"LampArm",p+new Vector3(Mathf.Sign(p.x)*-.46f,5.8f,0),new Vector3(1,.10f,.13f),navy);
        Box(parent,"Lamp",p+new Vector3(Mathf.Sign(p.x)*-.86f,5.7f,0),new Vector3(.45f,.14f,.5f),white);
    }

    private static void BuildTruck(Transform root,FiretruckRunnerManager m)
    {
        m.truck=Child(root,"RunnerFiretruck");
        m.truck.position=new Vector3(0,.13f,8);
        Asset(Town+"/SM_Veh_Firetruck_01.fbx","FiretruckVisual",m.truck,m.truck.position,new Vector3(2.55f,2.6f,5.7f),Vector3.zero);
        for(int side=-1;side<=1;side+=2)
        {
            var lightGo=Ball(m.truck,"EmergencyBeacon",new Vector3(side*.5f,2.65f,.85f),new Vector3(.34f,.15f,.28f),side<0?orange:teal);
            var l=lightGo.AddComponent<Light>();l.type=LightType.Point;l.range=4;l.color=side<0?Hex("#FF6252"):Hex("#43B9FF");l.shadows=LightShadows.None;
            if(side<0)m.redBeacon=l;else m.blueBeacon=l;
        }
        m.shieldVisual=Child(m.truck,"ShieldEffect").gameObject;
        for(int i=0;i<20;i++)
        {
            float a=i*Mathf.PI*2/20;
            Ball(m.shieldVisual.transform,"ShieldSpark",new Vector3(Mathf.Sin(a)*1.58f,.35f,Mathf.Cos(a)*3.1f),new Vector3(.17f,.12f,.25f),teal);
        }
        m.turboVisual=Child(m.truck,"TurboEffect").gameObject;
        for(int i=-1;i<=1;i+=2) Box(m.turboVisual.transform,"TurboTrail",new Vector3(i*.67f,.44f,-3.7f),new Vector3(.32f,.28f,1.9f),gold);
        m.shieldVisual.SetActive(false);m.turboVisual.SetActive(false);
        m.engineAudio=Audio(m.truck,"Engine","sfx100v2_loop_machine_01.ogg",.05f,true);
        m.hitAudio=Audio(root,"Impact","sfx100v2_metal_hit_01.ogg",.22f,false);
        m.coinAudio=Audio(root,"Coin","sfx100v2_items_01.ogg",.19f,false);
        m.powerAudio=Audio(root,"Power","sfx100v2_items_01.ogg",.25f,false);
    }
    private static void BuildCameraAndLight(Transform root,FiretruckRunnerManager m)
    {
        var cam=Child(root,"Runner Camera");cam.tag="MainCamera";
        cam.position=new Vector3(0,6.8f,-4.8f);cam.LookAt(new Vector3(0,1.3f,18));
        m.runnerCamera=cam.gameObject.AddComponent<Camera>();m.runnerCamera.fieldOfView=59;m.runnerCamera.nearClipPlane=.15f;m.runnerCamera.farClipPlane=235;
        m.runnerCamera.backgroundColor=Hex("#A9D7DF");m.runnerCamera.clearFlags=CameraClearFlags.SolidColor;m.runnerCamera.cullingMask=~(1<<30);
        cam.gameObject.AddComponent<AudioListener>();
        var sun=Child(root,"Warm afternoon");sun.rotation=Quaternion.Euler(48,-35,0);
        var light=sun.gameObject.AddComponent<Light>();light.type=LightType.Directional;light.color=Hex("#FFF1D6");light.intensity=1.25f;light.shadows=LightShadows.Soft;light.cullingMask=~(1<<30);
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=Hex("#C6E1F0");RenderSettings.ambientEquatorColor=Hex("#9BAAB8");RenderSettings.ambientGroundColor=Hex("#777D84");
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=Hex("#A9D7DF");RenderSettings.fogStartDistance=90;RenderSettings.fogEndDistance=220;
    }

    private static void BuildHud(Transform root,FiretruckRunnerManager m)
    {
        var cv=Rect(root,"RunnerHUD",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var canvas=cv.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
        var scaler=cv.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        cv.gameObject.AddComponent<GraphicRaycaster>();
        RectTransform safe=Rect(cv,"SafeArea",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);safe.gameObject.AddComponent<StorySafeAreaPanel>();
        RectTransform drive=Rect(safe,"Driving",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);m.drivingHud=drive.gameObject;
        var topLeft=Panel(drive,"RunTelemetry",new Vector2(0,1),new Vector2(240,-92),new Vector2(414,126),Navy);
        Text(topLeft,"Eyebrow","ACİL ROTA  /  SONSUZ",18,Teal,new Vector2(0,39),new Vector2(368,30));
        m.distanceText=Text(topLeft,"Distance","0 m",43,Cream,new Vector2(-67,-1),new Vector2(206,56));
        m.speedText=Text(topLeft,"Speed","61 km/sa",22,Cream,new Vector2(112,-3),new Vector2(154,38));
        m.healthText=Text(topLeft,"Health","● ● ●",25,Hex("#FF846C"),new Vector2(-65,-43),new Vector2(214,34));
        Text(topLeft,"LivesCaption","CAN",15,Cream,new Vector2(96,-43),new Vector2(156,24));
        var topRight=Panel(drive,"ScoreWallet",new Vector2(1,1),new Vector2(-230,-92),new Vector2(390,126),Navy);
        m.scoreText=Text(topRight,"Score","0",43,Cream,new Vector2(-72,10),new Vector2(190,58));
        Text(topRight,"ScoreCaption","PUAN",16,Teal,new Vector2(-72,-38),new Vector2(190,24));
        var icon=Rect(topRight,"CoinIcon",Vector2.one*.5f,Vector2.one*.5f,new Vector2(47,6),new Vector2(55,55));
        var image=icon.gameObject.AddComponent<Image>();image.sprite=coinIcon;image.preserveAspect=true;image.raycastTarget=false;
        m.coinCountText=Text(topRight,"Coins","0",34,Gold,new Vector2(119,9),new Vector2(100,58));
        Text(topRight,"CoinCaption","İMO",16,Gold,new Vector2(110,-38),new Vector2(126,24));
        var mission=Panel(drive,"Mission",new Vector2(.5f,1),new Vector2(0,-198),new Vector2(650,62),new Color(Navy.r,Navy.g,Navy.b,.94f));
        m.missionText=Text(mission,"MissionLabel","GÖREV 1 • 300 m yol al",21,Cream,new Vector2(0,7),new Vector2(610,36));
        var track=Panel(mission,"ProgressTrack",new Vector2(.5f,.5f),new Vector2(0,-21),new Vector2(590,5),Hex("#405C70"));
        m.missionFill=Panel(track,"Progress",new Vector2(.5f,.5f),Vector2.zero,new Vector2(590,5),Teal).GetComponent<Image>();
        m.missionFill.type=Image.Type.Filled;m.missionFill.fillMethod=Image.FillMethod.Horizontal;m.missionFill.fillAmount=0;
        m.comboText=Text(drive,"Combo","AÇIK ŞERİDİ TAKİP ET",22,Gold,new Vector2(0,250),new Vector2(630,40));
        m.powerText=Text(drive,"Powers","",23,Teal,new Vector2(0,206),new Vector2(700,38));
        m.countdownText=Text(safe,"Countdown","3",145,Cream,new Vector2(0,20),new Vector2(500,220));
        m.countdownText.outlineWidth=.15f;m.countdownText.outlineColor=Navy;
        Button(drive,"Pause","II",new Vector2(.5f,1),new Vector2(0,-67),new Vector2(80,74),Navy,Cream,m.Pause);
        Button(drive,"Left","‹",new Vector2(0,0),new Vector2(120,97),new Vector2(148,120),Navy,Cream,m.MoveLeft);
        Button(drive,"Right","›",new Vector2(1,0),new Vector2(-120,97),new Vector2(148,120),Navy,Cream,m.MoveRight);
        m.turboButton=Button(drive,"Turbo","TURBO  ↑",new Vector2(.5f,0),new Vector2(0,90),new Vector2(286,90),Gold,Navy,m.ActivateTurbo);
        var flash=Panel(safe,"DamageFlash",new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,4000),new Color(1,.18f,.12f,.3f));
        m.hitFlash=flash.gameObject.AddComponent<CanvasGroup>();m.hitFlash.alpha=0;m.hitFlash.blocksRaycasts=false;
        m.garagePanel=Overlay(safe,"Garage",new Vector2(0,100),new Vector2(1040,684),out var garage).gameObject;
        Text(garage,"Edition","İMO  /  MİNİ OYUNLAR",18,Teal,new Vector2(0,286),new Vector2(880,32));
        Text(garage,"Title","ACİL ROTA",76,Cream,new Vector2(0,207),new Vector2(880,92));
        Text(garage,"Subtitle","SONSUZ SÜRÜŞ  •  HER TUR BİR ADIM İLERİ",20,Gold,new Vector2(0,142),new Vector2(880,40));
        m.walletText=Text(garage,"Wallet","0 İMO",31,Gold,new Vector2(333,84),new Vector2(300,45));
        m.recordText=Text(garage,"Record","REKOR  0  •  0 m",23,Cream,new Vector2(-144,84),new Vector2(560,45));
        m.upgradeLabels=new TMP_Text[3];m.upgradeButtons=new Button[3];
        string[] names={"KALKAN","MIKNATIS","TURBO"};string[] symbols={"+","∩","»"};
        Color[] accents={Teal,Hex("#FF9679"),Gold};
        for(int i=0;i<3;i++)
        {
            int n=i;
            var card=Panel(garage,"Upgrade_"+names[i],Vector2.one*.5f,new Vector2((i-1)*312,-38),new Vector2(288,170),Hex("#223E55"));
            Text(card,"PowerSymbol",symbols[i],42,accents[i],new Vector2(-92,39),new Vector2(68,66));
            Text(card,"PowerName",names[i],24,accents[i],new Vector2(29,39),new Vector2(170,42));
            m.upgradeButtons[i]=Button(card,"BuyUpgrade","",Vector2.one*.5f,new Vector2(0,-40),new Vector2(260,68),Hex("#36536A"),Cream,
                i==0?(UnityEngine.Events.UnityAction)m.UpgradeShield:i==1?m.UpgradeMagnet:m.UpgradeTurbo);
            m.upgradeLabels[i]=m.upgradeButtons[i].GetComponentInChildren<TMP_Text>();m.upgradeLabels[i].fontSize=19;
        }
        m.garageMessage=Text(garage,"GarageHint","Her turda 3 can + 1 hazır turbo.",20,Cream,new Vector2(0,-164),new Vector2(920,53));
        Button(garage,"Start","YOLA ÇIK  ›",Vector2.one*.5f,new Vector2(164,-261),new Vector2(525,83),Teal,Navy,m.StartRun);
        Button(garage,"Hub","OYUNLAR",Vector2.one*.5f,new Vector2(-333,-261),new Vector2(275,83),Hex("#36536A"),Cream,m.ReturnToHub);
        m.completionPanel=Overlay(safe,"Results",new Vector2(0,100),new Vector2(890,620),out var results).gameObject;
        Text(results,"ResultTitle","TUR TAMAMLANDI",42,Teal,new Vector2(0,237),new Vector2(800,80));
        m.completionStats=Text(results,"ResultStats","",31,Cream,new Vector2(0,36),new Vector2(790,294));
        Button(results,"Retry","TEKRAR SÜR",Vector2.one*.5f,new Vector2(186,-208),new Vector2(350,90),Teal,Navy,m.Restart);
        Button(results,"Garage","GARAJ",Vector2.one*.5f,new Vector2(-186,-208),new Vector2(350,90),Gold,Navy,m.ShowGarage);
        Button(results,"BackToGames","OYUNLARA DÖN",Vector2.one*.5f,new Vector2(0,-276),new Vector2(480,38),Navy,Cream,m.ReturnToHub);
        m.pausePanel=Overlay(safe,"PauseOverlay",new Vector2(0,80),new Vector2(750,490),out var paused).gameObject;
        Text(paused,"PauseTitle","BİR NEFES MOLASI",42,Teal,new Vector2(0,151),new Vector2(700,80));
        Text(paused,"Controls","A / D veya ← → : şerit değiştir\nSPACE veya yukarı kaydır : turbo\nESC : duraklat / devam",26,Cream,new Vector2(0,22),new Vector2(670,140));
        Button(paused,"Resume","DEVAM ET",Vector2.one*.5f,new Vector2(0,-113),new Vector2(570,86),Teal,Navy,m.Resume);
        Button(paused,"Exit","OYUNLARA DÖN",Vector2.one*.5f,new Vector2(0,-195),new Vector2(490,45),Navy,Cream,m.ReturnToHub);
        var guide=Panel(safe,"ApoGuide",new Vector2(.5f,0),new Vector2(0,239),new Vector2(826,144),Cream);
        m.guidePanel=guide.gameObject;
        Text(guide,"ApoName","APO  •  YOL ARKADAŞIN",17,Hex("#248978"),new Vector2(64,45),new Vector2(590,30));
        m.guideText=Text(guide,"GuideMessage","",23,Navy,new Vector2(68,-11),new Vector2(594,81));m.guideText.alignment=TextAlignmentOptions.MidlineLeft;
        m.guideText.textWrappingMode=TextWrappingModes.Normal;
        m.guideTips=new[]{"Coin sıraları açık şeridi gösterir. Gözünü biraz ileride tut!",
            "Kalkan bir darbeyi karşılar. Mıknatıs bütün şeritlerden coin çeker.",
            "Turbo kısa süreli hız ve koruma sağlar. Zor bir bölüm için saklayabilirsin.",
            "Yol uzadıkça hız artar. Şerit değişimini erkenden planla.",
            "Her kısa görev garajına bonus İMO kazandırır.",
            "Deprem çantanı ailenle birlikte hazırlamayı unutma."};
        if(Object.FindAnyObjectByType<EventSystem>()==null) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        m.drivingHud.SetActive(false);m.completionPanel.SetActive(false);m.pausePanel.SetActive(false);m.countdownText.gameObject.SetActive(false);
    }

    [MenuItem("Tools/Deprem Story/Refresh Apo Guide")]
    public static void RefreshApoGuide()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring Apo.");
        var m=Object.FindFirstObjectByType<FiretruckRunnerManager>();
        if(m==null)throw new InvalidOperationException("Open the Firetruck Runner scene first.");
        AssetDatabase.Refresh();
        var stage=m.transform.root.Find("ApoPortraitStudio");
        if(stage!=null)Object.DestroyImmediate(stage.gameObject);
        var image=m.guidePanel.transform.Find("Live3DApo");
        if(image!=null)Object.DestroyImmediate(image.gameObject);
        BuildApo(m.transform.root,m);
        EditorUtility.SetDirty(m);
        EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
        EditorSceneManager.SaveScene(m.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Apo detailed mesh, surface maps and portrait refreshed.");
    }

    private static Texture2D ApoMap(string file,bool normal)
    {
        string path=Apo+"/"+file;
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Apo surface map is missing: "+file);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.sRGBTexture=false;importer.maxTextureSize=2048;
        importer.mipmapEnabled=true;importer.anisoLevel=4;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void BuildApo(Transform root,FiretruckRunnerManager m)
    {
        string path=Apo+"/ApoOriginal.fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(importer==null) throw new InvalidOperationException("Author ApoOriginal.fbx before building the runner.");
        importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        var character=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        character.name="Apo_AbdullahEkinci";
        string albedoPath=Apo+"/Apo_Albedo.png";
        var textureImporter=AssetImporter.GetAtPath(albedoPath) as TextureImporter;
        if(textureImporter==null)throw new InvalidOperationException("The reviewed Apo texture is missing.");
        textureImporter.textureType=TextureImporterType.Default;textureImporter.sRGBTexture=true;
        textureImporter.maxTextureSize=4096;textureImporter.mipmapEnabled=true;textureImporter.SaveAndReimport();
        var referenceMaterial=Mat("Apo_ReferenceMaterial","#FFFFFF",.13f);
        referenceMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath));
        referenceMaterial.SetTexture("_BumpMap",ApoMap("Apo_Normal.png",true));
        referenceMaterial.SetFloat("_BumpScale",1f);
        referenceMaterial.EnableKeyword("_NORMALMAP");
        referenceMaterial.SetTexture("_MetallicGlossMap",ApoMap("Apo_Surface.png",false));
        referenceMaterial.SetFloat("_Smoothness",1f);
        referenceMaterial.SetFloat("_Metallic",1f);
        referenceMaterial.SetFloat("_SmoothnessTextureChannel",0f);
        referenceMaterial.EnableKeyword("_METALLICSPECGLOSSMAP");
        referenceMaterial.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        EditorUtility.SetDirty(referenceMaterial);
        foreach(var renderer in character.GetComponentsInChildren<Renderer>(true))
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {

                materials[i]=referenceMaterial;
            }
            renderer.sharedMaterials=materials;
            if(renderer is SkinnedMeshRenderer skin)
                for(int shape=0;shape<skin.sharedMesh.blendShapeCount;shape++)skin.SetBlendShapeWeight(shape,0);
        }
        Animator animator=character.GetComponent<Animator>();
        if(animator==null)animator=character.AddComponent<Animator>();
        animator.runtimeAnimatorController=MakeApoAnimation(character);
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var prefab=PrefabUtility.SaveAsPrefabAsset(character,Apo+"/Apo_Guide.prefab");
        Object.DestroyImmediate(character);
        var stage=Child(root,"ApoPortraitStudio");stage.position=new Vector3(32,-32,0);
        character=(GameObject)PrefabUtility.InstantiatePrefab(prefab,stage);character.transform.localPosition=Vector3.zero;
        character.transform.localRotation=Quaternion.Euler(0,180,0);
        foreach(var t in character.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        m.guideAnimator=character.GetComponent<Animator>();
        var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(Art+"/ApoPortrait.renderTexture");
        if(rt==null){rt=new RenderTexture(384,384,24,RenderTextureFormat.ARGB32){name="ApoPortrait",antiAliasing=2};AssetDatabase.CreateAsset(rt,Art+"/ApoPortrait.renderTexture");}
        rt.Release();rt.width=512;rt.height=512;rt.antiAliasing=4;rt.filterMode=FilterMode.Bilinear;EditorUtility.SetDirty(rt);
        var cam=Child(stage,"ApoPortraitCamera");cam.localPosition=new Vector3(0,1.67f,-3);cam.LookAt(stage.position+new Vector3(0,1.62f,0));
        var camera=cam.gameObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.30f;camera.nearClipPlane=.1f;camera.farClipPlane=6;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0,0,0,0);camera.cullingMask=1<<30;camera.targetTexture=rt;camera.depth=-2;
        var light=Child(stage,"PortraitLight");light.localRotation=Quaternion.Euler(34,-28,0);
        var lamp=light.gameObject.AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=.85f;lamp.color=Cream;lamp.cullingMask=1<<30;
        var face=Rect(m.guidePanel.transform,"Live3DApo",Vector2.one*.5f,Vector2.one*.5f,new Vector2(-327,5),new Vector2(153,153));
        var raw=face.gameObject.AddComponent<RawImage>();raw.texture=rt;raw.raycastTarget=false;
    }
    private static AnimatorController MakeApoAnimation(GameObject actor)
    {
        string controllerPath=Apo+"/ApoGuide.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        bool newController=controller==null;
        if(newController){controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Explain",AnimatorControllerParameterType.Trigger);}
        var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(Apo+"/Apo_Idle.anim") ?? new AnimationClip{name="Apo_Breathing_Blink"};var explain=AssetDatabase.LoadAssetAtPath<AnimationClip>(Apo+"/Apo_Explain.anim") ?? new AnimationClip{name="Apo_Explain"}; idle.ClearCurves();explain.ClearCurves();
        foreach(var t in actor.GetComponentsInChildren<Transform>())
        {
            string p=AnimationUtility.CalculateTransformPath(t,actor.transform);
            if(t.name=="Head")
            {
                float y=t.localEulerAngles.y;
                idle.SetCurve(p,typeof(Transform),"localEulerAnglesRaw.y",AnimationCurve.EaseInOut(0,y-1.2f,2,y+1.2f));
                explain.SetCurve(p,typeof(Transform),"localEulerAnglesRaw.y",new AnimationCurve(new Keyframe(0,y),new Keyframe(.55f,y-5),new Keyframe(1.3f,y+3),new Keyframe(2,y)));
            }
            if(t.name=="Forearm.R")
            {
                float x=t.localEulerAngles.x;
                explain.SetCurve(p,typeof(Transform),"localEulerAnglesRaw.x",new AnimationCurve(new Keyframe(0,x),new Keyframe(.55f,x-32),new Keyframe(1.3f,x-25),new Keyframe(2,x)));
            }
        }
        var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
        if(skin!=null)
        {
            string p=AnimationUtility.CalculateTransformPath(skin.transform,actor.transform);
            idle.SetCurve(p,typeof(SkinnedMeshRenderer),"blendShape.Blink",new AnimationCurve(new Keyframe(0,0),new Keyframe(1.65f,0),new Keyframe(1.74f,100),new Keyframe(1.83f,0),new Keyframe(4,0)));
            explain.SetCurve(p,typeof(SkinnedMeshRenderer),"blendShape.Talk",new AnimationCurve(new Keyframe(0,0),new Keyframe(.25f,70),new Keyframe(.45f,0),new Keyframe(.7f,60),new Keyframe(1,0),new Keyframe(1.25f,45),new Keyframe(1.5f,0),new Keyframe(2,0)));
        }
        var clipSettings=AnimationUtility.GetAnimationClipSettings(idle);clipSettings.loopTime=true;AnimationUtility.SetAnimationClipSettings(idle,clipSettings);
        if(!AssetDatabase.Contains(idle))AssetDatabase.CreateAsset(idle,Apo+"/Apo_Idle.anim");if(!AssetDatabase.Contains(explain))AssetDatabase.CreateAsset(explain,Apo+"/Apo_Explain.anim"); EditorUtility.SetDirty(idle);EditorUtility.SetDirty(explain);if(!newController)return controller;
        var machine=controller.layers[0].stateMachine;
        var resting=machine.AddState("Idle");resting.motion=idle;machine.defaultState=resting;
        var explaining=machine.AddState("Explain");explaining.motion=explain;
        var enter=resting.AddTransition(explaining);enter.hasExitTime=false;enter.duration=.2f;enter.AddCondition(AnimatorConditionMode.If,0,"Explain");
        var leave=explaining.AddTransition(resting);leave.hasExitTime=true;leave.exitTime=1;leave.duration=.25f;
        return controller;
    }

    private static RectTransform Overlay(Transform parent,string name,Vector2 position,Vector2 size,out RectTransform card)
    {
        var overlay=Rect(parent,name,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var dim=overlay.gameObject.AddComponent<Image>();dim.color=new Color(.035f,.07f,.12f,.55f);dim.raycastTarget=true;
        card=Panel(overlay,name+"Card",Vector2.one*.5f,position,size,Navy);
        return overlay;
    }
    private static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var t=(RectTransform)go.transform;t.SetParent(parent,false);
        t.anchorMin=min;t.anchorMax=max;t.anchoredPosition=pos;t.sizeDelta=size;return t;
    }
    private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
    {
        var rect=Rect(parent,name,anchor,anchor,pos,size);var image=rect.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;return rect;
    }
    private static TMP_Text Text(Transform parent,string name,string content,float size,Color color,Vector2 p,Vector2 dimensions)
    {
        var rect=Rect(parent,name,Vector2.one*.5f,Vector2.one*.5f,p,dimensions);
        var t=rect.gameObject.AddComponent<TextMeshProUGUI>();t.font=bold;t.fontSize=size;t.color=color;t.text=content;t.alignment=TextAlignmentOptions.Center;
        t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;return t;
    }
    private static Button Button(Transform parent,string name,string label,Vector2 anchor,Vector2 p,Vector2 size,Color color,Color ink,UnityEngine.Events.UnityAction action)
    {
        var rect=Panel(parent,name,anchor,p,size,color);var image=rect.GetComponent<Image>();image.raycastTarget=true;
        var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=image;
        var colors=b.colors;colors.highlightedColor=new Color(.9f,1,1);colors.pressedColor=new Color(.7f,.88f,.9f);colors.disabledColor=new Color(.4f,.45f,.48f);b.colors=colors;
        Text(rect,"Label",label,label.Length<3?59:26,ink,Vector2.zero,size-new Vector2(20,4));
        UnityEventTools.AddPersistentListener(b.onClick,action);
        return b;
    }
    private static Transform Child(Transform parent,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
    }
    private static GameObject Box(Transform p,string n,Vector3 at,Vector3 size,Material m,Vector3 rotation=default)=>Primitive(p,n,PrimitiveType.Cube,at,size,m,rotation);
    private static GameObject Ball(Transform p,string n,Vector3 at,Vector3 size,Material m)=>Primitive(p,n,PrimitiveType.Sphere,at,size,m);
    private static GameObject Primitive(Transform p,string n,PrimitiveType type,Vector3 at,Vector3 size,Material m,Vector3 rotation=default)
    {
        var go=GameObject.CreatePrimitive(type);go.name=n;go.transform.SetParent(p,false);go.transform.localPosition=at;go.transform.localScale=size;go.transform.localEulerAngles=rotation;
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;return go;
    }
    private static GameObject Asset(string path,string name,Transform parent,Vector3 p,Vector3 size,Vector3 rotation)
    {
        Material atlas=path.StartsWith(Town,StringComparison.Ordinal)?AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonTown/Materials/PolygonTown_01_A.mat"):null;
        var go=StoryChapterBuilderCommon.InstantiateAsset(path,name,parent,p,size,rotation,false,false,atlas);
        foreach(var collider in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        return go;
    }
    private static Material Mat(string name,string color,float smooth=.1f)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",Hex(color));m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    private static Color Hex(string s){ColorUtility.TryParseHtmlString(s,out var c);return c;}
    private static AudioSource Audio(Transform parent,string name,string clip,float volume,bool loop)
    {
        var source=Child(parent,name).gameObject.AddComponent<AudioSource>();source.clip=StoryChapterBuilderCommon.LoadLicensedSfx(clip);source.volume=volume;source.loop=loop;source.playOnAwake=false;source.spatialBlend=0;return source;
    }
    private static void WorldText(Transform parent,string value,Vector3 position,float size,Color color)
    {
        var child=Child(parent,"RouteLabel");child.localPosition=position;
        child.localScale=new Vector3(1/parent.localScale.x,1/parent.localScale.y,1/parent.localScale.z);
        var text=child.gameObject.AddComponent<TextMeshPro>();text.font=bold;text.text=value;text.fontSize=size;text.color=color;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(4,1);text.raycastTarget=false;
    }
    private static Sprite MakeRoundedSprite()
    {
        string path=Art+"/Rounded.png";
        if(!File.Exists(path))
        {
            var t=new Texture2D(64,64,TextureFormat.RGBA32,false);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float dx=Mathf.Max(12-x,0,x-51),dy=Mathf.Max(12-y,0,y-51);
                float a=Mathf.Clamp01(12.5f-Mathf.Sqrt(dx*dx+dy*dy));t.SetPixel(x,y,new Color(1,1,1,a));
            }
            t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteBorder=new Vector4(16,16,16,16);importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
