using System;
using System.IO;
using System.Linq;
using Deprem.Story;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Editor authoring only. All graphics, materials and UI persist in the scene.
public static partial class StoryFiretruckRunnerSceneBuilder
{
    private static readonly Color Paper = Hex("#F4F1E9"), Ink = Hex("#20343D"), Muted = Hex("#63787D");
    private static readonly Color Accent = Hex("#218976"), Amber = Hex("#EAAF43");

    [MenuItem("Tools/Deprem Story/Refresh Runner Presentation")]
    public static void RefreshRunnerPresentation()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
        var m = Object.FindFirstObjectByType<FiretruckRunnerManager>();
        if (m == null || m.gameObject.scene.path != ScenePath) throw new InvalidOperationException("Open the runner scene first.");
        var root = m.transform.root;
        LoadRunnerPresentationAssets();
        var portrait = m.guidePanel != null ? m.guidePanel.GetComponentInChildren<RawImage>(true) : null;
        if (portrait != null) portrait.transform.SetParent(root, false);
        var oldHud = root.Find("RunnerHUD");
        if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);
        BuildHud(root, m);
        if (portrait != null) PlaceGuidePortrait(m, portrait);
        ApplyRunnerPresentation(root, m);
        EditorUtility.SetDirty(m);
        EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
        EditorSceneManager.SaveScene(m.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Runner presentation saved: compact HUD, contextual guide, authored ribbon effects and street dressing.");
    }

    private static void LoadRunnerPresentationAssets()
    {
        Directory.CreateDirectory(Art);
        regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/StoryPlayful/Lexend Regular SDF.asset");
        bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/StoryPlayful/Lexend SemiBold SDF.asset");
        if (regular == null || bold == null) throw new InvalidOperationException("Runner fonts are missing.");
        rounded = MakeRoundedSprite();
        coinIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Story/Collectibles/IMOCoin/IMOCoin_UI_Icon.png");
    }

    private static TMP_Text Copy(Transform parent, string name, string value, float size, Color color,
        Vector2 position, Vector2 dimensions, bool strong = false, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var text = Text(parent, name, value, size, color, position, dimensions);
        text.font = strong ? bold : regular;
        text.alignment = alignment;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static RectTransform PaperCard(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        var card = Panel(parent, name, anchor, position, size, color);
        var shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(.04f,.10f,.13f,.16f);
        shadow.effectDistance = new Vector2(0,-4);
        return card;
    }

    private static void BuildHud(Transform root, FiretruckRunnerManager m)
    {
        var cv = Rect(root,"RunnerHUD",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var canvas = cv.gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
        var scaler = cv.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        cv.gameObject.AddComponent<GraphicRaycaster>();
        var safe=Rect(cv,"SafeArea",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);safe.gameObject.AddComponent<StorySafeAreaPanel>();
        var drive=Rect(safe,"Driving",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);m.drivingHud=drive.gameObject;

        var telemetry=PaperCard(drive,"RunTelemetry",new Vector2(0,1),new Vector2(193,-70),new Vector2(322,96),Paper);
        m.distanceText=Copy(telemetry,"Distance","0 m",34,Ink,new Vector2(-66,11),new Vector2(162,44),true);
        m.speedText=Copy(telemetry,"Speed","61 km/sa",18,Muted,new Vector2(83,13),new Vector2(129,32));
        m.healthText=Copy(telemetry,"Health","● ● ●",19,Hex("#D96B54"),new Vector2(-63,-26),new Vector2(158,28));
        Copy(telemetry,"LivesCaption","CAN",12,Muted,new Vector2(83,-25),new Vector2(129,23));
        var wallet=PaperCard(drive,"ScoreWallet",new Vector2(1,1),new Vector2(-203,-70),new Vector2(342,96),Paper);
        m.scoreText=Copy(wallet,"Score","0",32,Ink,new Vector2(-72,12),new Vector2(154,44),true);
        Copy(wallet,"ScoreCaption","PUAN",12,Muted,new Vector2(-72,-25),new Vector2(154,24));
        var icon=Rect(wallet,"CoinIcon",Vector2.one*.5f,Vector2.one*.5f,new Vector2(34,12),new Vector2(35,35));
        var im=icon.gameObject.AddComponent<Image>();im.sprite=coinIcon;im.preserveAspect=true;im.raycastTarget=false;
        m.coinCountText=Copy(wallet,"Coins","0",28,Ink,new Vector2(100,12),new Vector2(90,44),true);
        m.comboText=Copy(wallet,"Combo","",14,Accent,new Vector2(80,-26),new Vector2(142,23));
        var mission=PaperCard(drive,"Mission",new Vector2(0,1),new Vector2(193,-155),new Vector2(322,54),Paper);
        Copy(mission,"MissionCaption","HEDEF",11,Muted,new Vector2(-113,6),new Vector2(61,22));
        m.missionText=Copy(mission,"MissionLabel","0 / 300 m",17,Ink,new Vector2(32,6),new Vector2(213,27));
        var track=Panel(mission,"ProgressTrack",Vector2.one*.5f,new Vector2(0,-17),new Vector2(282,4),Hex("#D2DAD5"));
        m.missionFill=Panel(track,"Progress",Vector2.one*.5f,Vector2.zero,new Vector2(282,4),Accent).GetComponent<Image>();
        m.missionFill.type=Image.Type.Filled;m.missionFill.fillMethod=Image.FillMethod.Horizontal;m.missionFill.fillOrigin=0;m.missionFill.fillAmount=0;
        var pause=Button(drive,"Pause","",new Vector2(.5f,1),new Vector2(0,-52),new Vector2(58,58),Paper,Ink,m.Pause);
        pause.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        for(int side=-1;side<=1;side+=2)Panel(pause.transform,"PauseBar",Vector2.one*.5f,new Vector2(side*6,0),new Vector2(5,21),Ink);
        var left=Button(drive,"Left","‹",new Vector2(0,0),new Vector2(83,76),new Vector2(102,88),Paper,Ink,m.MoveLeft);
        var right=Button(drive,"Right","›",new Vector2(1,0),new Vector2(-83,76),new Vector2(102,88),Paper,Ink,m.MoveRight);
        left.GetComponent<Image>().color=new Color(Paper.r,Paper.g,Paper.b,.88f);right.GetComponent<Image>().color=left.GetComponent<Image>().color;
        m.turboButton=Button(drive,"Turbo","Turbo  ↑",new Vector2(.5f,0),new Vector2(0,70),new Vector2(220,68),Amber,Ink,m.ActivateTurbo);
        m.turboButton.GetComponentInChildren<TMP_Text>().fontSize=24;
        m.powerText=Copy(drive,"Powers","",18,Paper,Vector2.zero,new Vector2(660,35),true);
        m.powerText.rectTransform.anchorMin=m.powerText.rectTransform.anchorMax=new Vector2(.5f,0);
        m.powerText.rectTransform.anchoredPosition=new Vector2(0,133);m.powerText.outlineWidth=.18f;m.powerText.outlineColor=Ink;
        m.countdownText=Copy(safe,"Countdown","3",105,Paper,new Vector2(0,38),new Vector2(360,170),true);
        m.countdownText.outlineWidth=.12f;m.countdownText.outlineColor=Ink;

        var flash=Rect(safe,"DamageFlash",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var flashImage=flash.gameObject.AddComponent<Image>();flashImage.sprite=PresentationSprite("EdgeFade",128,(x,y)=> {
            float edge=Mathf.Max(Mathf.Abs(x),Mathf.Abs(y));return new Color(1,1,1,Mathf.Pow(Mathf.InverseLerp(.64f,1,edge),2));
        });flashImage.color=new Color(.85f,.22f,.12f,.48f);flashImage.raycastTarget=false;
        m.hitFlash=flash.gameObject.AddComponent<CanvasGroup>();m.hitFlash.alpha=0;m.hitFlash.blocksRaycasts=false;

        m.garagePanel=PresentationOverlay(safe,"Garage",new Vector2(0,45),new Vector2(940,634),out var garage).gameObject;
        Copy(garage,"Edition","GARAJ",14,Accent,new Vector2(-228,264),new Vector2(380,26),true,TextAlignmentOptions.MidlineLeft);
        Copy(garage,"Title","Acil Rota",48,Ink,new Vector2(-218,211),new Vector2(400,70),true,TextAlignmentOptions.MidlineLeft);
        Copy(garage,"Subtitle","Engelleri aş. Madalyaları topla.",20,Muted,new Vector2(-130,157),new Vector2(576,36),false,TextAlignmentOptions.MidlineLeft);
        m.walletText=Copy(garage,"Wallet","0 İMO",29,Accent,new Vector2(304,229),new Vector2(238,45),true,TextAlignmentOptions.MidlineRight);
        m.recordText=Copy(garage,"Record","Rekor  0  ·  0 m",16,Muted,new Vector2(266,186),new Vector2(315,36),false,TextAlignmentOptions.MidlineRight);
        Panel(garage,"Divider",Vector2.one*.5f,new Vector2(0,116),new Vector2(840,2),Hex("#D9E0D9"));
        m.upgradeLabels=new TMP_Text[3];m.upgradeButtons=new Button[3];
        string[] names={"Kalkan","Mıknatıs","Turbo"};
        string[] details={"Bir darbeyi karşılar","Madalyaları çeker","Kısa süreli hız ve koruma"};
        Color[] accents={Accent,Hex("#B85F46"),Hex("#A27621")};
        for(int i=0;i<3;i++)
        {
            var card=Panel(garage,"Upgrade_"+names[i],Vector2.one*.5f,new Vector2((i-1)*282,-15),new Vector2(264,220),Hex("#E7EBE3"));
            var glyph=Rect(card,"PowerIcon",Vector2.one*.5f,Vector2.one*.5f,new Vector2(-76,65),new Vector2(49,49));
            var powerImage=glyph.gameObject.AddComponent<Image>();powerImage.sprite=PowerSprite(i);powerImage.color=accents[i];powerImage.raycastTarget=false;
            Copy(card,"PowerName",names[i],23,Ink,new Vector2(36,65),new Vector2(160,38),true);
            Copy(card,"Description",details[i],15,Muted,new Vector2(0,18),new Vector2(242,35));
            m.upgradeButtons[i]=Button(card,"BuyUpgrade","",Vector2.one*.5f,new Vector2(0,-63),new Vector2(234,66),Paper,Ink,
                i==0?(UnityEngine.Events.UnityAction)m.UpgradeShield:i==1?m.UpgradeMagnet:m.UpgradeTurbo);
            m.upgradeLabels[i]=m.upgradeButtons[i].GetComponentInChildren<TMP_Text>();m.upgradeLabels[i].fontSize=17;m.upgradeLabels[i].font=regular;
        }
        m.garageMessage=Copy(garage,"GarageHint","3 can · 1 turbo hakkı",17,Muted,new Vector2(0,-162),new Vector2(840,48));
        Button(garage,"Start","Sürüşe başla  ›",Vector2.one*.5f,new Vector2(174,-252),new Vector2(490,68),Accent,Paper,m.StartRun);
        Button(garage,"Hub","Oyunlar",Vector2.one*.5f,new Vector2(-294,-252),new Vector2(250,68),Hex("#E0E6DF"),Ink,m.ReturnToHub);

        m.completionPanel=PresentationOverlay(safe,"Results",new Vector2(0,30),new Vector2(820,566),out var results).gameObject;
        Copy(results,"ResultTitle","Tur bitti",36,Ink,new Vector2(0,219),new Vector2(730,65),true);
        m.completionStats=Copy(results,"ResultStats","",25,Muted,new Vector2(0,37),new Vector2(720,265));
        Button(results,"Retry","Tekrar sür",Vector2.one*.5f,new Vector2(180,-173),new Vector2(318,68),Accent,Paper,m.Restart);
        Button(results,"Garage","Garaj",Vector2.one*.5f,new Vector2(-180,-173),new Vector2(318,68),Hex("#E0E6DF"),Ink,m.ShowGarage);
        Button(results,"BackToGames","Oyunlara dön",Vector2.one*.5f,new Vector2(0,-237),new Vector2(400,36),Paper,Muted,m.ReturnToHub).GetComponentInChildren<TMP_Text>().fontSize=17;
        m.pausePanel=PresentationOverlay(safe,"PauseOverlay",Vector2.zero,new Vector2(670,412),out var paused).gameObject;
        Copy(paused,"PauseTitle","Duraklatıldı",34,Ink,new Vector2(0,142),new Vector2(600,58),true);
        Copy(paused,"Controls","← →  veya kaydır: şerit değiştir\nSpace / ↑ : turbo     Esc : devam",21,Muted,new Vector2(0,38),new Vector2(598,105));
        Button(paused,"Resume","Devam et",Vector2.one*.5f,new Vector2(0,-75),new Vector2(550,65),Accent,Paper,m.Resume);
        Button(paused,"Exit","Oyunlara dön",Vector2.one*.5f,new Vector2(0,-153),new Vector2(420,38),Paper,Muted,m.ReturnToHub).GetComponentInChildren<TMP_Text>().fontSize=18;

        var guide=PaperCard(safe,"ApoGuide",new Vector2(0,0),new Vector2(266,204),new Vector2(468,96),Paper);m.guidePanel=guide.gameObject;
        m.guideText=Copy(guide,"GuideMessage","",20,Ink,new Vector2(49,0),new Vector2(330,74),false,TextAlignmentOptions.MidlineLeft);
        m.guideTips=Array.Empty<string>();
        if(Object.FindAnyObjectByType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        m.guidePanel.SetActive(false);m.drivingHud.SetActive(false);m.completionPanel.SetActive(false);m.pausePanel.SetActive(false);m.countdownText.gameObject.SetActive(false);
    }

    private static RectTransform PresentationOverlay(Transform parent,string name,Vector2 p,Vector2 size,out RectTransform card)
    {
        var root=Rect(parent,name,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var dim=root.gameObject.AddComponent<Image>();dim.color=new Color(.06f,.13f,.17f,.4f);dim.raycastTarget=true;
        card=PaperCard(root,name+"Card",Vector2.one*.5f,p,size,Paper);return root;
    }

    private static void PlaceGuidePortrait(FiretruckRunnerManager m, RawImage portrait)
    {
        var mask=Panel(m.guidePanel.transform,"PortraitWindow",Vector2.one*.5f,new Vector2(-182,0),new Vector2(78,78),Color.white);
        mask.GetComponent<Image>().sprite=PresentationSprite("PortraitDisc",128,(x,y)=>new Color(1,1,1,Mathf.Clamp01((1-Mathf.Sqrt(x*x+y*y))*64)));
        var clipping=mask.gameObject.AddComponent<Mask>();clipping.showMaskGraphic=false;
        var r=portrait.rectTransform;r.SetParent(mask,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;
    }

    private static Sprite PresentationSprite(string name,int side,Func<float,float,Color> sample)
    {
        string path=Art+"/"+name+".png";
        if(!File.Exists(path))
        {
            var tex=new Texture2D(side,side,TextureFormat.RGBA32,false);
            for(int y=0;y<side;y++)for(int x=0;x<side;x++)tex.SetPixel(x,y,sample((x+.5f)/side*2-1,(y+.5f)/side*2-1));
            tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=Vector4.zero;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite PowerSprite(int kind)
    {
        return PresentationSprite("PowerGlyph"+kind,128,(x,y)=> {
            bool inside;
            if(kind==0)
            {
                float width=y>0?.65f:Mathf.Lerp(.10f,.65f,Mathf.InverseLerp(-.82f,0,y));
                inside=y<.76f&&y>-.82f&&Mathf.Abs(x)<width;
                if(Mathf.Abs(x)<.105f&&Mathf.Abs(y-.12f)<.39f||Mathf.Abs(y-.12f)<.105f&&Mathf.Abs(x)<.36f)inside=false;
            }
            else if(kind==1)
            {
                float d=Mathf.Sqrt(x*x+y*y);
                inside=y<0?d<.70f&&d>.35f:(Mathf.Abs(x)>.35f&&Mathf.Abs(x)<.7f&&y<.73f);
            }
            else inside=InsidePolygon(new Vector2(x,y),new[]{new Vector2(.10f,.88f),new Vector2(-.62f,-.12f),new Vector2(-.06f,-.12f),new Vector2(-.18f,-.87f),new Vector2(.64f,.22f),new Vector2(.08f,.22f)});
            return new Color(1,1,1,inside?1:0);
        });
    }

    private static bool InsidePolygon(Vector2 p,Vector2[] polygon)
    {
        bool inside=false;
        for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            if((polygon[i].y>p.y)!=(polygon[j].y>p.y)&&p.x<(polygon[j].x-polygon[i].x)*(p.y-polygon[i].y)/(polygon[j].y-polygon[i].y)+polygon[i].x)inside=!inside;
        return inside;
    }

    private static void ApplyRunnerPresentation(Transform root,FiretruckRunnerManager m)
    {
        if(m.guidePanel.GetComponentInChildren<Mask>(true)==null)
        {
            var portrait=m.guidePanel.GetComponentInChildren<RawImage>(true);if(portrait!=null)PlaceGuidePortrait(m,portrait);
        }
        BuildRibbonEffects(m);
        DressRunnerStreet(m);
        ApplyRunnerLight(root,m);
        m.coinAudio.volume=.075f;m.powerAudio.volume=.15f;m.hitAudio.volume=.16f;
    }

    private static void BuildRibbonEffects(FiretruckRunnerManager m)
    {
        if(m.shieldVisual!=null)Object.DestroyImmediate(m.shieldVisual);
        if(m.turboVisual!=null)Object.DestroyImmediate(m.turboVisual);
        var sprite=PresentationSprite("SoftRibbon",64,(x,y)=>new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(y)),2)));
        string path=Art+"/RibbonEffect.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetTexture("_BaseMap",sprite.texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);
        mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=(int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        m.shieldVisual=Child(m.truck,"ShieldEffect").gameObject;
        for(int arc=0;arc<3;arc++)
        {
            var line=MakeRibbon(m.shieldVisual.transform,"ShieldArc",mat,.15f,new Color(.35f,.93f,.85f,.7f));line.positionCount=31;
            for(int i=0;i<31;i++){float a=(arc*120+12+i*3.2f)*Mathf.Deg2Rad;line.SetPosition(i,new Vector3(Mathf.Sin(a)*1.49f,.35f,Mathf.Cos(a)*3.05f));}
            line.widthCurve=AnimationCurve.Linear(0,.6f,1,.6f);
        }
        m.turboVisual=Child(m.truck,"TurboEffect").gameObject;
        for(int side=-1;side<=1;side+=2)
        {
            var glow=MakeRibbon(m.turboVisual.transform,"TurboRibbon",mat,.36f,new Color(.55f,.86f,1,.64f));glow.positionCount=4;
            glow.SetPositions(new[]{new Vector3(side*.82f,.42f,-2.0f),new Vector3(side*.88f,.42f,-2.8f),new Vector3(side*.96f,.39f,-4.0f),new Vector3(side*1.12f,.32f,-5.7f)});
            glow.widthCurve=new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.2f,1),new Keyframe(1,0));
        }
        m.shieldVisual.SetActive(false);m.turboVisual.SetActive(false);
    }

    private static LineRenderer MakeRibbon(Transform root,string name,Material material,float width,Color color)
    {
        var line=Child(root,name).gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=material;
        line.widthMultiplier=width;line.numCapVertices=4;line.numCornerVertices=4;line.textureMode=LineTextureMode.Stretch;line.alignment=LineAlignment.View;
        line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(color,.22f),new GradientColorKey(color,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(color.a,.15f),new GradientAlphaKey(color.a,.65f),new GradientAlphaKey(0,1)});line.colorGradient=gradient;
        return line;
    }

    private static void DressRunnerStreet(FiretruckRunnerManager m)
    {
        var ground=Mat("Grass","#789879");Mat("Asphalt","#3D4850");Mat("Pavement","#C6C6B8");Mat("Coral","#D88562");Mat("Mint","#4F927D");
        var paving=Mat("DressedPaving","#AFB5A7");
        for(int s=0;s<m.sections.Length;s++)
        {
            var section=m.sections[s].root;
            foreach(var child in section.Cast<Transform>().ToArray())
                if(child.name=="StreetDressing"||child.name.StartsWith("RouteGate")||child.name=="DistrictSign"||child.name=="TreeCrown"||child.name=="TreeTrunk")Object.DestroyImmediate(child.gameObject);
            var seen=new System.Collections.Generic.HashSet<Vector3>();
            foreach(var mark in section.Cast<Transform>().Where(t=>t.name=="LaneMark").ToArray())if(!seen.Add(mark.localPosition))Object.DestroyImmediate(mark.gameObject);
            var dressing=Child(section,"StreetDressing");
            for(int side=-1;side<=1;side+=2)
            {
                Box(dressing,"GroundContinuation",new Vector3(side*29,-.21f,48),new Vector3(29,.15f,96),ground);
                for(int i=0;i<3;i++)
                {
                    float z=16+i*30;
                    var tree=Asset(Town+"/SM_Env_Tree_0"+(1+(i+s)%2)+".fbx","StreetTree",dressing,dressing.position+new Vector3(side*8.2f,.13f,z),new Vector3(3.0f,5.8f,3.0f),new Vector3(0,41*i,0));
                    Box(dressing,"TreeBed",new Vector3(side*8.2f,.07f,z),new Vector3(2.8f,.12f,2.8f),paving);
                    Asset(Town+"/SM_Env_Bush_01.fbx","LowPlanting",dressing,dressing.position+new Vector3(side*7.9f,.18f,z+3.1f),new Vector3(1.9f,.9f,1.5f),new Vector3(0,side*24,0));
                }
                Asset(Town+"/SM_Prop_ParkBench_01.fbx","StreetBench",dressing,dressing.position+new Vector3(side*7.5f,.15f,29),new Vector3(1.6f,1.05f,2.1f),new Vector3(0,side*90,0));
                string[] background={"Env_ResidentBuilding_03.prefab","Env_CommercialBuilding_02.prefab","Env_ResidentBuilding_05.prefab"};
                for(int i=0;i<4;i++)
                    Asset(City+"/"+background[(s+i)%3],"NeighborhoodBackdrop",dressing,dressing.position+new Vector3(side*(23+i%2*5),.1f,7+i*24),new Vector3(11,16+(s+i)%3*4,21),new Vector3(0,-side*90,0));
            }
        }
    }

    private static void ApplyRunnerLight(Transform root,FiretruckRunnerManager m)
    {
        var sun=root.Find("Warm afternoon");
        if(sun!=null){sun.rotation=Quaternion.Euler(40,-32,0);var light=sun.GetComponent<Light>();light.intensity=1.02f;light.color=Hex("#FFF1DC");light.shadowStrength=.72f;}
        RenderSettings.ambientSkyColor=Hex("#B4CBDB");RenderSettings.ambientEquatorColor=Hex("#ABB8B8");RenderSettings.ambientGroundColor=Hex("#69766F");
        RenderSettings.fogColor=Hex("#C4D3D9");RenderSettings.fogStartDistance=60;RenderSettings.fogEndDistance=185;
        string skyPath=Art+"/RunnerSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if(sky==null){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,skyPath);}
        sky.SetColor("_SkyTint",Hex("#819CB5"));sky.SetColor("_GroundColor",Hex("#C4CECA"));sky.SetFloat("_AtmosphereThickness",.65f);sky.SetFloat("_Exposure",1.1f);sky.SetFloat("_SunSize",.022f);
        RenderSettings.skybox=sky;EditorUtility.SetDirty(sky);m.runnerCamera.clearFlags=CameraClearFlags.Skybox;
        var data=m.runnerCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
        data.volumeLayerMask=1<<29;
        var old=root.Find("RunnerColor");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var color=Child(root,"RunnerColor");color.gameObject.layer=29;var volume=color.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=5;
        string path=Art+"/RunnerColor.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.Neutral);
        if(!profile.TryGet<ColorAdjustments>(out var adjust)){adjust=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(adjust,profile);}adjust.contrast.Override(8);adjust.saturation.Override(-8);adjust.postExposure.Override(-.12f);
        if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.threshold.Override(1.3f);bloom.intensity.Override(.08f);
        if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}vignette.intensity.Override(.08f);vignette.smoothness.Override(.7f);
        volume.sharedProfile=profile;foreach(var component in profile.components)EditorUtility.SetDirty(component);EditorUtility.SetDirty(profile);
    }
}
