using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deprem.Story;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Repeatable scene/prefab authoring. Contains no player behaviour.</summary>
public static class StoryKktcSceneArt
{
    const string Art = StoryKktcArtLibrary.Root;
    [MenuItem("Tools/Deprem Story/KKTC/5 Apply Complete Visual Pass")]
    public static void ApplyAll()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring");
        string previous = SceneManager.GetActiveScene().path;
        GameObject home = PrefabUtility.LoadPrefabContents(StorySharedHomePrefabBuilder.PrefabPath);
        try { DressHome(home.transform); PrefabUtility.SaveAsPrefabAsset(home, StorySharedHomePrefabBuilder.PrefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(home); }
        foreach(string path in StoryKktcArtLibrary.Scenes)
        {
            Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            ApplyToScene(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        if(File.Exists(previous)) EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);
        Debug.Log("KKTC_SCENE_ART_APPLIED");
    }

    public static void ApplyToScene(Scene scene)
    {
        if(!StoryKktcArtLibrary.Scenes.Contains(scene.path)) return;
        StoryKktcArtLibrary.ApplyPropsToScene(scene);
        StoryKktcCharacters.Apply(scene);
        Transform[] all=StoryKktcArtLibrary.Transforms(scene);
        Transform home=StoryKktcArtLibrary.Find(all,"StoryHome_Shared");
        if(home!=null) DressHome(home);
        all=StoryKktcArtLibrary.Transforms(scene);
        Transform oldFamilyFrame=StoryKktcArtLibrary.Find(all,"Story01_Gallery_FamilyMemory");
        if(oldFamilyFrame!=null)foreach(Renderer r in oldFamilyFrame.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        // The existing decorative trim is edited locally; source furniture and legacy scenes
        // retain their materials. Gameplay colliders, navigation and interaction roots stay put.
        foreach(Renderer r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
        {
            string n=r.name;
            if(r.GetComponent<TMP_Text>()!=null || r.GetComponentInParent<Canvas>()!=null) continue;
            if(r is ParticleSystemRenderer)
            {
                if(n.Contains("Dust")) r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Story/Generated/Materials/DustParticles.mat");
                r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;continue;
            }
            if(n.StartsWith("Story01_CeilingBeam_")) r.enabled=false;
            if(n.StartsWith("Story01_Wainscot") || n=="Story01_CameraSideWainscot") r.sharedMaterial=Finish("Plaster","E8E1D2");
            if(n.Contains("ChairRail")) r.sharedMaterial=Finish("Sage","869B87");
            if(n.Contains("Ceiling") && !n.Contains("Beam")) r.sharedMaterial=Finish("Plaster","E8E1D2");
            if(n.Contains("KitchenBacksplash")) r.sharedMaterial=Finish("Sage","869B87");
            if(n.StartsWith("Story01_FamilyPlanFrame") || n=="Story01_FamilyPlanBackdrop") r.enabled=false;
            if(n.StartsWith("Story01_Gallery_"))
                foreach(Renderer child in r.GetComponentsInChildren<Renderer>(true)) child.receiveShadows=true;
            if(r.enabled && !n.Contains("Marker") && !n.Contains("Indicator") && !n.Contains("Beam") && !n.Contains("Glow"))
            {
                bool distant=r.GetComponentsInParent<Transform>(true).Any(t=>t.name.Contains("DistantCity") || t.name.Contains("NeighborhoodDepth"));
                r.shadowCastingMode=distant?ShadowCastingMode.Off:ShadowCastingMode.On;
                r.receiveShadows=true;
            }
        }
        if(scene.name.Contains("04")) DressExterior(scene);
        ConfigureLighting(scene);
        ConfigureCameras(scene);
        LocalizeSerializedContent(scene);
        ClearPrototypeVoices(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    public static void DressHome(Transform home)
    {
        Transform old=home.Find("KKTC_ApartmentDress");
        if(old!=null) Object.DestroyImmediate(old.gameObject);
        Transform dress=new GameObject("KKTC_ApartmentDress").transform;
        dress.SetParent(home,false);
        Material plaster=Finish("Plaster","E8E1D2"), oak=Finish("Oak","A98763"), sage=Finish("Sage","869B87");
        foreach(Transform t in home.GetComponentsInChildren<Transform>(true))
        {
            Renderer r=t.GetComponent<Renderer>(); if(r==null) continue;
            if(t.name=="BackWall_Left" || t.name=="WindowAccentWall" || t.name=="NightSky" ||
               (t.name=="Glass" && t.GetComponentInParent<Transform>().name=="Glass"))
                r.enabled=false;
            if(t.name.Contains("Wall") && !t.name.Contains("Plate")) r.sharedMaterial=plaster;
            if(t.name=="LivingRoom_Floor" || t.name=="CorridorFloor") r.sharedMaterial=Finish("StoneFloor","CBBDA4");
            if(t.name=="RugBorder") r.sharedMaterial=Finish("RugEdge","796C54");
            if(t.name=="Rug") r.sharedMaterial=sage;
            if(t.name=="RugInset") r.sharedMaterial=Finish("Linen","D8CBB3");
            if(t.name.StartsWith("Curtain") && !t.name.Contains("Rod")) r.sharedMaterial=Finish("Linen","D8CBB3");
            if(t.name.StartsWith("Frame") && t.parent.name=="Window_DangerZone") r.sharedMaterial=Finish("WindowFrame","DAD9CF");
        }
        // Four wall pieces frame a real opening, with continuous reveals to the balcony.
        Box(dress,"WindowWall_West",new(-4.3f,2.015f,6),new(1.4f,4.03f,.24f),plaster);
        Box(dress,"WindowWall_East",new(.3f,2.015f,6),new(2.2f,4.03f,.24f),plaster);
        Box(dress,"WindowWall_Sill",new(-2.2f,.51f,6),new(2.8f,1.02f,.24f),plaster);
        Box(dress,"WindowWall_Header",new(-2.2f,3.36f,6),new(2.8f,1.34f,.24f),plaster);
        // Close the camera side and corridor roof even when the base prefab is rebuilt.
        Box(dress,"FrontWall",new(0,2.015f,-5.5f),new(10.2f,4.03f,.25f),plaster);
        Box(dress,"CorridorCeiling",new(2.5f,3.36f,9.5f),new(3.1f,.14f,7),plaster);
        Box(dress,"BalconySlab",new(-2.2f,-.08f,7.1f),new(4.2f,.18f,2.4f),Finish("StoneFloor","CBBDA4"));
        Box(dress,"BalconyParapet",new(-2.2f,.35f,8.25f),new(4.2f,.7f,.2f),plaster);
        Box(dress,"BalconyRail",new(-2.2f,1.02f,8.25f),new(4.2f,.045f,.055f),sage);
        for(int i=0;i<19;i++) Box(dress,"BalconyBaluster_"+i,new(-4.2f+i*.22f,.86f,8.25f),new(.025f,.3f,.025f),sage);
        Prop(dress,"Balcony_Basil","BasilPot",new(-3.45f,1.05f,5.59f),1.05f,0);
        Prop(dress,"Balcony_Planter","BasilPot",new(-.85f,0,7.45f),2.3f,32);
        // Neighboring apartment blocks form depth behind every window view.
        for(int i=0;i<4;i++)
        {
            float x=-9+i*5.5f, z=13+(i%2)*2;
            Material facade=Finish("Facade_"+i,i%2==0?"D4B990":"D8D6C0");
            Box(dress,"Neighbor_"+i,new(x,3,z),new(4.8f,6,3.6f),facade);
            Box(dress,"NeighborRoof_"+i,new(x,6.08f,z),new(5,.2f,3.8f),plaster);
            for(int floor=0;floor<3;floor++) for(int w=0;w<2;w++)
            {
                Vector3 center=new(x-1.2f+w*2.3f,.95f+floor*1.8f,z-1.82f);
                Box(dress,"NeighborReveal",center,new(.96f,1.2f,.08f),plaster);
                Box(dress,"NeighborShutter",center+Vector3.back*.05f,new(.79f,1.02f,.055f),sage);
                for(int slat=0;slat<6;slat++) Box(dress,"ShutterSlat",center+new Vector3(0,-.4f+slat*.16f,-.085f),new(.78f,.025f,.04f),Finish("SageDark","647867"));
            }
            Box(dress,"RoofWaterTank",new(x+.7f,6.45f,z),new(.7f,.65f,.65f),Finish("Tank","D0CFC2"));
        }
        Box(dress,"CourtyardGround",new(0,-.22f,14),new(55,.2f,42),Finish("Courtyard","B6B59B"));
        Box(dress,"DistantCourtyardFacade",new(-2.2f,4,19),new(32,8,.35f),Finish("DistantLimestone","C2C6B2"));
        // The low cabinet is separate from the children's packing/quake table.
        Box(dress,"CoffeeConsoleTop",new(-4.54f,.78f,4.05f),new(.56f,.055f,1.24f),oak);
        Box(dress,"CoffeeConsoleBody",new(-4.58f,.4f,4.05f),new(.45f,.72f,1.08f),sage);
        Prop(dress,"Lefkara_GrandmotherCloth","LefkaraCloth",new(-4.54f,.811f,4.05f),1.5f,90);
        Prop(dress,"EverydayCoffee","CoffeeSet",new(-4.47f,.831f,3.93f),1.25f,90);
        Prop(dress,"BreadBasket","BreadBasket",new(4.5f,.867f,-3.0f),1.1f,90);
        Prop(dress,"Kitchen_Ceramic","OliveVase",new(4.55f,.87f,-3.5f),.65f,20);
        Prop(dress,"SchoolBooks","BookStack",new(-4.48f,.811f,4.43f),.8f,84);
        // Woven runner detail adds small-scale pattern without changing navigation geometry.
        for(int i=0;i<15;i++) Box(dress,"RunnerWeft_"+i,new(.15f,.081f,-1.41f+i*.2f),new(3.62f,.002f,.014f),Finish("RugWeft","BAAB88"));
        Frame(dress,"SchoolDrawing",new(.9f,1.77f,-5.47f),0,"CAN • İLKOKUL",sage);
        Frame(dress,"FamilyPhoto",new(-4.78f,2.15f,4.06f),90,"BİZİM AİLE",plaster,true);
        Label(dress,"ApartmentIdentity","ZEYTİN APARTMANI\nLEFKOŞA",new(3.65f,2.35f,5.67f),180,.11f,new(1.1f,.34f));
    }

    static void Frame(Transform parent,string name,Vector3 p,float yaw,string caption,Material background,bool family=false)
    {
        Transform frame=new GameObject("KKTC_"+name).transform; frame.SetParent(parent,false);
        frame.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
        Box(frame,"Frame",p,new(.62f,.49f,.035f),Finish("Oak","A98763")).rotation=frame.rotation;
        Box(frame,"MatBoard",p+frame.forward*.023f,new(.54f,.41f,.012f),background).rotation=frame.rotation;
        if(family)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("ArtDirection/CharacterReferences/ApprovedFamily_APose_Lineup.png");
            // Unity imports only Assets; the approved image is copied by the production tool.
            texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/ApprovedFamily.png") ?? texture;
            if(texture!=null)
            {
                Material photo=Finish("FamilyPhoto","FFFFFF"); photo.SetTexture("_BaseMap",texture);
                var panel=Box(frame,"Photograph",p+frame.forward*.033f,new(.50f,.28f,.009f),photo); panel.rotation=frame.rotation;
            }
        }
        else
        {
            // Can's simple paper drawing: house, sea and sun, assembled as editable shapes.
            Box(frame,"SchoolHouse",p+frame.forward*.034f+Vector3.up*.02f,new(.16f,.15f,.01f),StoryKktcArtLibrary.Material("Terracotta")).rotation=frame.rotation;
        }
        Label(frame,"Caption",caption,p+frame.forward*.045f-Vector3.up*.163f,yaw+180,.038f,new(.5f,.07f));
    }

    static void DressExterior(Scene scene)
    {
        Transform[] all=StoryKktcArtLibrary.Transforms(scene);
        Transform root=scene.GetRootGameObjects().First(g=>g.name.StartsWith("STORY_")).transform;
        Transform previous=root.Find("KKTC_Neighborhood"); if(previous!=null) Object.DestroyImmediate(previous.gameObject);
        Transform dress=new GameObject("KKTC_Neighborhood").transform; dress.SetParent(root,false);
        Transform backdrop=new GameObject("KKTC_DistantNeighborhood").transform;backdrop.SetParent(dress,false);
        Box(backdrop,"ContinuousCityGround",new(0,-.18f,42),new(180,.12f,180),Finish("CityGround","ACA88A"));
        Vector3[] blocks={new(-22,0,18),new(22,0,18),new(-24,0,40),new(24,0,44),
            new(-18,0,72),new(-6,0,78),new(8,0,76),new(21,0,70)};
        for(int index=0;index<blocks.Length;index++)
        {
            Vector3 p=blocks[index];float height=5.5f+(index%3)*1.3f;
            Box(backdrop,"ApartmentBlock_"+index,p+Vector3.up*(height*.5f),new(8,height,7),Finish("CityLimestone","CEC5AE"));
            Box(backdrop,"FlatRoof_"+index,p+Vector3.up*(height+.15f),new(8.4f,.3f,7.4f),Finish("RoofStone","A8A38E"));
            for(int floor=0;floor<3;floor++)
                Box(backdrop,"ShutterBand_"+index+"_"+floor,p+new Vector3(0,1.3f+floor*1.65f,-3.52f),new(5.6f,.85f,.08f),Finish("DistantShutters","7F948B"));
        }
        foreach(Renderer renderer in backdrop.GetComponentsInChildren<Renderer>())
        {renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        Material stairFinish=Finish("StairPlaster","D7D2C3");
        Box(dress,"StairwellFoundation",new(0,-.25f,10.5f),new(5.5f,.5f,22),Finish("StairStone","89918C"));
        Box(dress,"StairwellRightWall",new(2.62f,3.5f,10.5f),new(.24f,7,22),stairFinish);
        Box(dress,"StairwellLeftWall",new(-2.62f,3.5f,10.5f),new(.24f,7,22),stairFinish);
        Box(dress,"StairwellRoof",new(0,7.06f,10.5f),new(5.5f,.12f,22),stairFinish);
        Label(dress,"NeighborhoodSign","ZEYTİN SOKAK\nLEFKOŞA",new(-5.7f,2.1f,24.2f),180,.18f,new(2.4f,.68f));
        // Keep the shelter fascia above the parents' faces in the wide family shot.
        Transform canopyHeader=StoryKktcArtLibrary.Find(all,"AssemblyCanopyHeader");
        if(canopyHeader!=null)
        {
            canopyHeader.position=new(-2.3f,2.82f,45.64f);
            canopyHeader.localScale=new(5.05f,.2f,.1f);
        }
        Transform canopyLabel=StoryKktcArtLibrary.Find(all,"AssemblyAidStationLabel");
        if(canopyLabel!=null)canopyLabel.position=new(-2.15f,2.82f,45.51f);
        foreach(TMP_Text text in all.Where(t=>t!=null).Select(t=>t.GetComponent<TMP_Text>()).Where(t=>t!=null))
        {
            text.text=(text.text??string.Empty).Replace("AFAD","SİVİL SAVUNMA");
            if(!text.text.Contains("Kıbrıs Türk"))text.text=text.text.Replace("Kızılay","Kıbrıs Türk Kızılayı");
            if(!text.text.Contains("MAHALLE PARKI")) text.text=text.text.Replace("TOPLANMA ALANI","MAHALLE PARKI\nTOPLANMA ALANI");
        }
        foreach(Animator actor in root.GetComponentsInChildren<Animator>(true).Where(a=>a.isHuman))
        {
            string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject);
            if(!(path??string.Empty).Contains("RescueWorker") && actor.transform.parent.Find("CharacterSource_RescueWorker")==null) continue;
            Transform chest=actor.GetBoneTransform(HumanBodyBones.Chest)??actor.GetBoneTransform(HumanBodyBones.Spine);
            if(chest==null) continue;
            Transform old=chest.Find("KKTC_CivilDefenceVest"); if(old!=null) Object.DestroyImmediate(old.gameObject);
            Transform vest=new GameObject("KKTC_CivilDefenceVest").transform; vest.SetParent(chest,false);
            // Local, bone-mounted chest/back identifiers cover the previous institution patch.
            Vector3 forward=actor.transform.parent.forward;
            foreach(int side in new[]{-1,1})
            {
                Vector3 pos=chest.position+forward*(.115f*side);
                Transform patch=Box(vest,"IdentityPatch",pos,new(.25f,.145f,.014f),StoryKktcArtLibrary.Material("BlueInk"));
                patch.rotation=Quaternion.LookRotation(forward*side,Vector3.up);
                Label(vest,"CivilDefenceText","KKTC\nSİVİL SAVUNMA",pos+forward*(.009f*side),patch.eulerAngles.y+180,.036f,new(.23f,.12f),Color.white);
            }
        }
        // Seven identical stationary fence sections share one draw submission. The
        // original transforms/colliders stay available to existing scene contracts.
        Transform assembly=StoryKktcArtLibrary.Find(all,"RebuildAssemblySet");
        if(assembly!=null)
        {
            Transform batch=assembly.Find("KKTC_AssemblyFenceBatch");if(batch!=null)Object.DestroyImmediate(batch.gameObject);
            var fence=assembly.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("AssemblyRearFence_") && f.sharedMesh!=null).ToArray();
            if(fence.Length>1)
            {
                Mesh combined=new(){name="KKTC_AssemblyFence",indexFormat=IndexFormat.UInt32};
                combined.CombineMeshes(fence.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=assembly.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
                string path=Art+"/Meshes/AssemblyFence.asset";Mesh saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(combined,path);saved=combined;}
                else{EditorUtility.CopySerialized(combined,saved);Object.DestroyImmediate(combined);EditorUtility.SetDirty(saved);}
                GameObject fenceBatch=new("KKTC_AssemblyFenceBatch");fenceBatch.transform.SetParent(assembly,false);
                fenceBatch.AddComponent<MeshFilter>().sharedMesh=saved;
                fenceBatch.AddComponent<MeshRenderer>().sharedMaterial=fence[0].GetComponent<Renderer>().sharedMaterial;
                foreach(var f in fence)f.GetComponent<Renderer>().enabled=false;
            }
        }
    }

    static void ConfigureLighting(Scene scene)
    {
        Light[] lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Directional).ToArray();
        Light key=lights.FirstOrDefault(l=>l.name=="Directional Light")??lights.FirstOrDefault(l=>l.name.Contains("Key"))??lights.FirstOrDefault();
        foreach(Light light in lights)
        {
            if(light.type!=LightType.Directional) continue;
            if(light==key)
            {
                light.color=new Color(1,.92f,.80f); light.intensity=1.22f;
                light.shadows=LightShadows.Soft; light.shadowStrength=.65f; light.shadowBias=.03f; light.shadowNormalBias=.18f;
            }
            else { light.intensity=.42f; light.shadows=LightShadows.None; }
        }
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.58f,.65f,.67f);
        RenderSettings.ambientEquatorColor=new Color(.52f,.50f,.43f);
        RenderSettings.ambientGroundColor=new Color(.24f,.22f,.19f);
        RenderSettings.ambientIntensity=1;
        if(Camera.main!=null && scene.name.Contains("03")) Camera.main.backgroundColor=new Color32(46,60,68,255);
        if(Camera.main!=null && scene.name.Contains("04")) Camera.main.backgroundColor=new Color32(112,146,158,255);
    }

    static void ConfigureCameras(Scene scene)
    {
        Transform[] all=StoryKktcArtLibrary.Transforms(scene);
        var context=StoryKktcArtLibrary.Find(all,"ContextPanel") as RectTransform;
        if(context!=null)
        {
            // Retain the existing panel animation while docking its guidance below
            // world drag targets (the old 412px position covered the loose batteries).
            var dock=StoryKktcArtLibrary.Find(all,"KKTC_ContextDock") as RectTransform;
            if(dock==null)
            {
                dock=new GameObject("KKTC_ContextDock",typeof(RectTransform)).GetComponent<RectTransform>();
                dock.SetParent(context.parent,false);
                dock.anchorMin=Vector2.zero;dock.anchorMax=Vector2.one;
                Vector2 panelPosition=context.anchoredPosition;
                context.SetParent(dock,false);context.anchoredPosition=panelPosition;
            }
            dock.offsetMin=new Vector2(0,-210);dock.offsetMax=new Vector2(0,-210);
        }
        if(scene.name.Contains("01"))
        {
            Pose(all,"CM_PreparationOverview_Rebuild",new(1.85f,2.38f,-3.18f),new(-2.55f,.98f,-1.55f),44);
            var follow=StoryKktcArtLibrary.Find(all,"CM_PreparationOverview_Rebuild").GetComponent<CinemachinePositionComposer>();
            follow.CameraDistance=5.72f;
            Transform flash=StoryKktcArtLibrary.Descendant(StoryKktcArtLibrary.Find(all,"WorldItem_Flashlight"),"KKTC_AuthoredVisual");
            Fit(all,"CM_PreparationFlashlight_Rebuild",flash!=null ? -flash.right*1.2f+Vector3.up*.9f-flash.forward*.5f : new(.7f,.9f,-1),38, "WorldItem_Flashlight");
            Fit(all,"CM_PreparationRadio_Rebuild",new(1.15f,.8f,-1.3f),44,"BagReview_EmergencyRadio","Review_RadioBatteryLoose");
            Fit(all,"CM_PreparationBag_Rebuild",new(3f,1.7f,-.6f),54,"EmergencyBag_Open_Packing","WorldItem_Flashlight","WorldItem_Radio","WorldItem_Water","WorldItem_Blanket");
            ComposePackingCamera(all);
            Pose(all,"CM_PreparationRadio_Rebuild",new(1.96f,1.48f,-.1f),new(1.18f,1,.57f),44);
            Fit(all,"CM_PreparationBandageInspection_Rebuild",new(-1,.95f,-.8f),40,"BandageSeal_Unchecked");
            Fit(all,"CM_PreparationWaterInspection_Rebuild",new(1,.8f,-.9f),40,"WorldItem_Water");
            var handoff=StoryKktcArtLibrary.Find(all,"CM_PreparationSiblingHandoff_Rebuild").GetComponent<CinemachineCamera>();
            Vector3 handoffTarget=new(-.27f,.83f,.64f);
            handoff.transform.SetPositionAndRotation(new(1f,1.62f,.25f),Quaternion.LookRotation(handoffTarget-new Vector3(1f,1.62f,.25f)));
            var handoffLens=handoff.Lens;handoffLens.FieldOfView=43;handoff.Lens=handoffLens;
        }
        if(scene.name.Contains("02"))
        {
            var overview=StoryKktcArtLibrary.Find(all,"CM_HomeOverview_Rebuild").GetComponent<CinemachineCamera>();
            Pose(all,"CM_HomeOverview_Rebuild",new(0,2.9f,0),new(2.1f,1f,2.8f),54);
            var composer=overview.GetComponent<CinemachinePositionComposer>();if(composer!=null)composer.CameraDistance=3.65f;
        }
        if(scene.name.Contains("03"))
        {
            // Both actionable parts must remain in view on 9:19.5 as well as 9:16.
            // The previous tight lenses clipped the loose wheel and the crouch surface.
            Pose(all,"CM03R_TableFamilyMoment",new(3.45f,2.42f,-3.65f),new(.15f,.72f,.1f),54);
            Pose(all,"CM03R_QuakeClose",new(-3.35f,2.18f,3.25f),new(-.15f,.88f,-.2f),54);
            Vector3 coverTarget=(StoryKktcArtLibrary.Find(all,"Deniz_CoverAnchor").position+
                StoryKktcArtLibrary.Find(all,"Can_CoverAnchor").position)*.5f+Vector3.up*.42f+Vector3.left*.08f;
            Pose(all,"CM03R_UnderTableTwoShot",new(.2f,.72f,-5.25f),coverTarget,60);
            Pose(all,"CM03R_CorridorLong",new(2.5f,2.9f,-2.6f),new(2.4f,.85f,7.5f),54);
            var consequence=StoryKktcArtLibrary.Find(all,"CM03R_PreparationConsequence").GetComponent<CinemachineCamera>();
            Vector3 position=new(-1.8f,2.8f,-3.8f),target=new(2.8f,1.1f,3.4f);
            consequence.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            var lens=consequence.Lens;lens.FieldOfView=54;consequence.Lens=lens;
        }
        if(scene.name.Contains("04"))
            // The existing assembly interactions leave the children at the plaza
            // approach. Include their actual standing area as well as the parents.
            Pose(all,"CM04R_AssemblyReunion",new(0f,5.75f,30f),new(-.8f,1f,44.5f),50);
        var cameras=all.Select(t=>t.GetComponent<CinemachineCamera>()).Where(c=>c!=null).ToArray();
        ConfigureTrackingCameras(scene,cameras);
        // Distant/room-changing cameras cut; nearby shots blend only along short, clear paths.
        var settings=ScriptableObject.CreateInstance<CinemachineBlenderSettings>();
        var blends=new List<CinemachineBlenderSettings.CustomBlend>();
        foreach(var from in cameras) foreach(var to in cameras)
        {
            if(from==to) continue;
            bool near=Vector3.Distance(from.transform.position,to.transform.position)<1.5f && from.Follow==null && to.Follow==null &&
                !Physics.Linecast(from.transform.position,to.transform.position,~0,QueryTriggerInteraction.Ignore);
            blends.Add(new(){From=from.name,To=to.name,Blend=new(near?CinemachineBlendDefinition.Styles.EaseInOut:CinemachineBlendDefinition.Styles.Cut,near?.35f:0)});
        }
        settings.CustomBlends=blends.ToArray();
        string assetPath=Art+"/"+scene.name+"_CameraBlends.asset";
        var saved=AssetDatabase.LoadAssetAtPath<CinemachineBlenderSettings>(assetPath);
        if(saved==null){AssetDatabase.CreateAsset(settings,assetPath);saved=settings;}
        else {EditorUtility.CopySerialized(settings,saved);Object.DestroyImmediate(settings);EditorUtility.SetDirty(saved);}
        foreach(var brain in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CinemachineBrain>(true))) brain.CustomBlends=saved;
    }

    static void ConfigureTrackingCameras(Scene scene,CinemachineCamera[] cameras)
    {
        var tracking=cameras.Where(c=>c.GetComponent<CinemachinePositionComposer>()!=null).ToArray();
        if(tracking.Length==0)return;
        Transform root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name=="KKTC_CameraBounds");
        if(root==null)
        {
            root=new GameObject("KKTC_CameraBounds").transform;
            root.SetParent(tracking[0].transform.parent,false);
        }
        root.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.localScale=Vector3.one;
        root.gameObject.layer=2;
        var volume=root.GetComponent<BoxCollider>();
        if(volume==null)volume=root.gameObject.AddComponent<BoxCollider>();
        volume.isTrigger=true;volume.center=new(0,2.8f,.2f);volume.size=new(9.2f,1.6f,10.7f);
        foreach(var camera in tracking)
        {
            // Deoccluder can change RawOrientation. Without an authored Aim component,
            // that correction becomes next frame's basis and slowly sends the camera over the roof.
            Vector3 angles=camera.transform.eulerAngles;
            var aim=camera.GetComponent<CinemachinePanTilt>();
            if(aim==null)aim=camera.gameObject.AddComponent<CinemachinePanTilt>();
            aim.ReferenceFrame=CinemachinePanTilt.ReferenceFrames.World;
            aim.PanAxis.Value=aim.PanAxis.Center=Mathf.DeltaAngle(0,angles.y);
            aim.TiltAxis.Value=aim.TiltAxis.Center=Mathf.DeltaAngle(0,angles.x);
            aim.PanAxis.Recentering.Enabled=false;aim.TiltAxis.Recentering.Enabled=false;
            var body=camera.GetComponent<CinemachinePositionComposer>();
            body.CenterOnActivate=true;body.TargetOffset=new(0,1f,0);
            var obstacle=camera.GetComponent<CinemachineDeoccluder>();
            if(obstacle!=null)
            {
                obstacle.MinimumDistanceFromTarget=1.45f;
                var avoidance=obstacle.AvoidObstacles;
                avoidance.Strategy=CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
                avoidance.UseFollowTarget.YOffset=1f;
                obstacle.AvoidObstacles=avoidance;
            }
            var confiner=camera.GetComponent<CinemachineConfiner3D>();
            if(confiner==null)confiner=camera.gameObject.AddComponent<CinemachineConfiner3D>();
            confiner.BoundingVolume=volume;confiner.SlowingDistance=.25f;
            if(camera.name=="CM_PreparationOverview_Rebuild") ConfigureOpeningCameraAnimation(camera,aim);
        }
    }

    static void ConfigureOpeningCameraAnimation(CinemachineCamera camera,CinemachinePanTilt aim)
    {
        // The first ten seconds retain the family's established three-quarter shot.
        // While the board close-up is active, an authored clip settles the follow camera
        // to the rear angle used later for the walk with the backpack. No player script is added.
        string folder=Art+"/Animations";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(Art,"Animations");
        string path=folder+"/PreparationCameraOpening.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null){clip=new AnimationClip{name="PreparationCameraOpening",frameRate=50};AssetDatabase.CreateAsset(clip,path);}
        clip.ClearCurves();
        void Curve(Type type,string property,float opening,float carried)
        {
            var curve=new AnimationCurve(new Keyframe(0,opening),new Keyframe(10,opening),new Keyframe(10.02f,carried),new Keyframe(11,carried));
            for(int i=0;i<curve.length;i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",type,property),curve);
        }
        Curve(typeof(CinemachinePanTilt),"PanAxis.Value",aim.PanAxis.Value,10);
        Curve(typeof(CinemachinePanTilt),"TiltAxis.Value",aim.TiltAxis.Value,33.6f);
        Curve(typeof(CinemachinePositionComposer),"CameraDistance",5.72f,3.6f);
        Curve(typeof(CinemachineCamera),"Lens.FieldOfView",44,52);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;
        AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
        string controllerPath=folder+"/PreparationCameraOpening.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine=controller.layers[0].stateMachine;
        var state=machine.defaultState;
        if(state==null){state=machine.AddState("Opening then carry");machine.defaultState=state;}
        state.motion=clip;EditorUtility.SetDirty(controller);
        var animator=camera.GetComponent<Animator>();if(animator==null)animator=camera.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController=controller;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    }

    static void Fit(Transform[] all,string cameraName,Vector3 direction,float fov,params string[] names)
    {
        var camera=StoryKktcArtLibrary.Find(all,cameraName)?.GetComponent<CinemachineCamera>(); if(camera==null)return;
        var renderers=names.Select(n=>StoryKktcArtLibrary.Find(all,n)).Where(t=>t!=null)
            .SelectMany(t=>t.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="KKTC_AuthoredVisual").ToArray();
        if(renderers.Length==0)return;
        Bounds bounds=StoryKktcArtLibrary.BoundsOf(renderers[0]);
        foreach(var item in renderers.Skip(1))bounds.Encapsulate(StoryKktcArtLibrary.BoundsOf(item));
        Quaternion rotation=Quaternion.LookRotation(-direction.normalized,Vector3.up);
        float distance=.6f, vertical=Mathf.Tan(fov*Mathf.Deg2Rad*.5f)*.62f, horizontal=vertical*(9f/19.5f)*.9f;
        for(int i=0;i<8;i++)
        {
            Vector3 corner=Vector3.Scale(bounds.extents,new((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            Vector3 local=Quaternion.Inverse(rotation)*corner;
            distance=Mathf.Max(distance,Mathf.Abs(local.x)/horizontal-local.z,Mathf.Abs(local.y)/vertical-local.z);
        }
        camera.transform.SetPositionAndRotation(bounds.center+direction.normalized*distance,rotation);
        var lens=camera.Lens;lens.FieldOfView=fov;lens.NearClipPlane=.025f;camera.Lens=lens;
    }

    static void Pose(Transform[] all,string name,Vector3 position,Vector3 target,float fov)
    {
        var camera=StoryKktcArtLibrary.Find(all,name).GetComponent<CinemachineCamera>();
        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
        var lens=camera.Lens;lens.FieldOfView=fov;camera.Lens=lens;
    }

    static void ComposePackingCamera(Transform[] all)
    {
        string[] names={"EmergencyBag_Open_Packing","WorldItem_Flashlight","WorldItem_Radio","WorldItem_Water","WorldItem_Blanket"};
        Bounds[] bounds=names.Select(n=>StoryKktcArtLibrary.Find(all,n)).Select(t=>t.Find("KKTC_AuthoredVisual") ?? StoryKktcArtLibrary.Descendant(t,"KKTC_AuthoredVisual"))
            .Where(t=>t!=null).Select(t=>StoryKktcArtLibrary.BoundsOf(t)).ToArray();
        var points=bounds.SelectMany(b=>Enumerable.Range(0,8).Select(i=>b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)))).ToArray();
        float tangent=Mathf.Tan(30*Mathf.Deg2Rad),best=-1;Vector3 bestP=new(3.3f,3.5f,.4f),bestT=new(.9f,.05f,.5f);
        for(float x=2.6f;x<=4.21f;x+=.3f)for(float y=2.2f;y<=3.71f;y+=.2f)for(float z=-1.5f;z<=.61f;z+=.3f)
        for(float tx=.6f;tx<=1.31f;tx+=.2f)for(float ty=0;ty<=.61f;ty+=.1f)
        {
            Vector3 p=new(x,y,z),target=new(tx,ty,.5f);Quaternion inverse=Quaternion.Inverse(Quaternion.LookRotation(target-p));
            bool good=true;float bagMin=1,bagMax=0;
            for(int i=0;i<points.Length;i++)
            {
                Vector3 v=inverse*(points[i]-p);if(v.z<=.2f){good=false;break;}
                float sx=.5f+v.x/(2*v.z*tangent*(9f/19.5f)),sy=.5f+v.y/(2*v.z*tangent);
                if(sx<.08f || sx>.92f || sy<.365f || sy>.83f){good=false;break;}
                if(i<8){bagMin=Mathf.Min(bagMin,sy);bagMax=Mathf.Max(bagMax,sy);}
            }
            if(good && bagMax-bagMin>best){best=bagMax-bagMin;bestP=p;bestT=target;}
        }
        Pose(all,"CM_PreparationBag_Rebuild",bestP,bestT,60);
        File.WriteAllText("ClientExports/KKTC/Reports/PackingComposition.txt","position="+bestP+" target="+bestT+" bagViewportHeight="+best);
    }

    static void ClearPrototypeVoices(Scene scene)
    {
        foreach(var ui in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StoryUIController>(true)))
        {
            SerializedObject serialized=new(ui);var voices=serialized.FindProperty("dialogueVoices");
            if(voices!=null)voices.ClearArray();serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var source in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AudioSource>(true)))
            if(source.clip!=null && AssetDatabase.GetAssetPath(source.clip).Contains("/Voices/")) source.clip=null;
    }

    [Serializable] class ContentEntry { public string before; public string after; }
    [Serializable] class ContentTable { public ContentEntry[] entries; }
    static void LocalizeSerializedContent(Scene scene)
    {
        string path=Art+"/Dialogue/ContentReplacements.json";
        if(!File.Exists(path))return;
        var table=JsonUtility.FromJson<ContentTable>(File.ReadAllText(path));
        var replacements=table.entries.GroupBy(e=>e.before).ToDictionary(g=>g.Key,g=>g.Last().after);
        foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)))
        {
            if(component==null)continue;
            SerializedObject serialized=new(component);var field=serialized.GetIterator();bool changed=false;
            while(field.NextVisible(true))
                if(field.propertyType==SerializedPropertyType.String && replacements.TryGetValue(field.stringValue,out string value))
                {field.stringValue=value;changed=true;}
            if(changed)serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static Material Finish(string name,string hex)
    {
        string path=Art+"/Materials/KKTC_Home_"+name+".mat";
        Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        ColorUtility.TryParseHtmlString("#"+hex,out Color color);material.SetColor("_BaseColor",color);
        material.SetFloat("_Smoothness",.2f);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }
    static Transform Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        GameObject obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name="KKTC_"+name;
        obj.transform.SetParent(parent,true);obj.transform.position=position;
        Vector3 inherited=parent.lossyScale;
        obj.transform.localScale=new(size.x/Mathf.Abs(inherited.x),size.y/Mathf.Abs(inherited.y),size.z/Mathf.Abs(inherited.z));
        obj.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.transform;
    }
    static Transform Prop(Transform parent,string name,string model,Vector3 position,float scale,float yaw)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/"+model+".prefab");
        if(prefab==null)return null;
        GameObject obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);obj.name="KKTC_"+name;
        obj.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));obj.transform.localScale=Vector3.one*scale;return obj.transform;
    }
    static void Label(Transform parent,string name,string text,Vector3 p,float yaw,float size,Vector2 rect,Color? color=null)
        =>StoryChapterBuilderCommon.CreateWorldLabel("KKTC_"+name,text,p,new Vector3(0,yaw,0),size,color??new Color(.16f,.22f,.23f),parent,rect);
}
