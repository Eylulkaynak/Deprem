using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.VisualScripting;
using Deprem.Story;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static Material softParticle;
        static GameObject fireHelp;
        static readonly Dictionary<int,Vector3> fireStands=new Dictionary<int,Vector3>();
        static readonly Dictionary<int,GameObject> wetPatches=new Dictionary<int,GameObject>();
        static readonly Dictionary<int,GameObject> firePreviews=new Dictionary<int,GameObject>();
        static Material CreateWaterStreamMaterial()
        {
            string path=Root+"Art/Materials/PhysicalWaterStream.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",Texture2D.whiteTexture);material.SetColor("_BaseColor",new Color(.66f,.90f,1f,.88f));material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;EditorUtility.SetDirty(material);return material;
        }

        static Material ParticleMaterial()
        {
            string path=Root+"Art/Materials/SoftOriginalParticles.mat";softParticle=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!softParticle){softParticle=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(softParticle,path);}
            string texturePath=Root+"Art/Materials/SoftParticleFalloff.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture){texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Original soft particle falloff";var pixels=new Color[64*64];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2.4f));}texture.SetPixels(pixels);texture.Apply();texture.wrapMode=TextureWrapMode.Clamp;AssetDatabase.CreateAsset(texture,texturePath);}
            softParticle.SetTexture("_BaseMap",texture);softParticle.SetColor("_BaseColor",Color.white);softParticle.SetFloat("_Surface",1);softParticle.SetFloat("_Blend",0);
            softParticle.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);softParticle.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);softParticle.SetFloat("_ZWrite",0);softParticle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");softParticle.renderQueue=3000;return softParticle;
        }
        static ParticleSystem FireParticles(Transform parent,bool smoke)
        {
            var go=Group(smoke?"Yükselen duman":"Hareketli sıcak alev",parent,new Vector3(0,smoke?.55f:.10f,0));var ps=go.AddComponent<ParticleSystem>();
            var m=ps.main;m.startLifetime=smoke?2.2f:.65f;m.startSpeed=smoke?.5f:1.1f;m.startSize=smoke?.55f:.30f;m.startColor=smoke?new Color(.34f,.36f,.34f,.42f):new Color(1,.46f,.16f,.9f);m.maxParticles=60;m.scalingMode=ParticleSystemScalingMode.Hierarchy;m.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=smoke?12:36;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.20f;shape.rotation=new Vector3(-90,0,0);
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(smoke?new Color(.4f,.41f,.38f):new Color(1,.79f,.28f),0),new GradientColorKey(smoke?new Color(.56f,.55f,.5f):new Color(.96f,.26f,.10f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(smoke?.5f:.9f,.15f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.4f),new Keyframe(.3f,1),new Keyframe(1,smoke?1.6f:.15f)));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=softParticle;return ps;
        }

        static void CreatePhysicalFire()
        {
            ParticleMaterial();var streamMaterial=CreateWaterStreamMaterial();fireStands.Clear();wetPatches.Clear();firePreviews.Clear();InitialPhysical("HoseConnected");InitialPhysical("HoseReady");InitialPhysical("FireStage",1);InitialPhysical("FireCompleted");
            var idil=cast["Idil"];var animator=idil.GetComponentInChildren<Animator>();var nozzle=Group("İdil’in gerçek hortum başlığı",idil.transform,new Vector3(.20f,1.15f,.4f));var nozzleVisual=Model("HoseNozzle",nozzle.transform,Vector3.zero,1,0);nozzleVisual.transform.localRotation=Quaternion.Euler(90,0,0);
            var tip=Group("Hortum su çıkışı",nozzle.transform,new Vector3(0,0,.33f));var rightGrip=Group("Sağ el hortum kavrama",nozzle.transform,new Vector3(.12f,.04f,.12f));var leftGrip=Group("Sol el hortum kavrama",nozzle.transform,new Vector3(-.12f,.04f,.12f));
            var colors=new[]{new Vector3(-5.1f,-.48f,-18),new Vector3(-2.7f,-.48f,-19),new Vector3(-4,-.48f,-21)};
            for(int stage=1;stage<=3;stage++)
            {
                Vector3 center=colors[stage-1];fireStands[stage]=center+Vector3.forward*2.2f;
                var root=Group("Gerçek müdahale alanı "+stage,world.transform);root.SetActive(false);
                var preview=Group("Ada’nın uzaktan gördüğü yangın "+stage,world.transform);firePreviews[stage]=preview;
                for(int i=0;i<3;i++){var spot=Group("Bekleyen yangın odağı",preview.transform,center+new Vector3((i-1)*.64f,0,(i%2)*.5f));Model("Parcel",spot.transform,Vector3.zero,1,20+i*25);FireParticles(spot.transform,false);FireParticles(spot.transform,true);}
                var stream=Group("Kesintisiz su akışı",root.transform).AddComponent<LineRenderer>();stream.positionCount=5;stream.startWidth=.035f;stream.endWidth=.080f;stream.numCapVertices=6;stream.numCornerVertices=5;stream.sharedMaterial=streamMaterial;stream.shadowCastingMode=ShadowCastingMode.Off;stream.receiveShadows=false;
                var hose=Group("Zeminden gelen besleme hortumu",root.transform).AddComponent<LineRenderer>();hose.positionCount=6;hose.startWidth=.055f;hose.endWidth=.055f;hose.numCapVertices=5;hose.sharedMaterial=mats["YY_tealDark"];
                var splash=Group("Su çarpma zerrecikleri",root.transform).AddComponent<ParticleSystem>();var sm=splash.main;sm.startLifetime=.36f;sm.startSpeed=1.8f;sm.startSize=.05f;sm.startColor=Hex("#BAEDF0");sm.maxParticles=90;sm.playOnAwake=false;var em=splash.emission;em.rateOverTime=80;splash.GetComponent<ParticleSystemRenderer>().sharedMaterial=softParticle;
                var manager=root.AddComponent<FirefighterExtinguishManager>();
                Serialized(manager,"worldCamera",camera);Serialized(manager,"nozzleTip",tip.transform);Serialized(manager,"waterStream",stream);Serialized(manager,"waterImpact",splash);Serialized(manager,"firefighterRoot",idil.transform);Serialized(manager,"nozzleRig",nozzle.transform);Serialized(manager,"rightNozzleGrip",rightGrip.transform);Serialized(manager,"leftNozzleGrip",leftGrip.transform);Serialized(manager,"supplyHose",hose);Serialized(manager,"progressManager",progress);
                foreach(var pair in new[]{("spineBone",HumanBodyBones.Spine),("chestBone",HumanBodyBones.Chest),("rightUpperArmBone",HumanBodyBones.RightUpperArm),("rightLowerArmBone",HumanBodyBones.RightLowerArm),("rightHandBone",HumanBodyBones.RightHand),("leftUpperArmBone",HumanBodyBones.LeftUpperArm),("leftLowerArmBone",HumanBodyBones.LeftLowerArm),("leftHandBone",HumanBodyBones.LeftHand)})Serialized(manager,pair.Item1,animator.GetBoneTransform(pair.Item2));
                if(!animator.GetBoneTransform(HumanBodyBones.Chest))Serialized(manager,"chestBone",animator.GetBoneTransform(HumanBodyBones.Spine));
                var so=new SerializedObject(manager);var array=so.FindProperty("fires");array.arraySize=3;
                for(int i=0;i<3;i++)
                {
                    var target=Group("Alev odağı "+stage+" "+i,root.transform,center+new Vector3((i-1)*.64f,0,(i%2)*.5f));Model("Parcel",target.transform,Vector3.zero,1,20+i*25);
                    var fireVisual=Group("Alev, duman ve ışık",target.transform);FireParticles(fireVisual.transform,false);FireParticles(fireVisual.transform,true);var light=fireVisual.AddComponent<Light>();light.type=LightType.Point;light.range=2;light.intensity=1.2f;light.color=Hex("#FFC265");
                    var hit=target.AddComponent<BoxCollider>();hit.center=Vector3.up*.45f;hit.size=new Vector3(.7f,1f,.7f);
                    var binding=array.GetArrayElementAtIndex(i);binding.FindPropertyRelative("hitCollider").objectReferenceValue=hit;binding.FindPropertyRelative("visualRoot").objectReferenceValue=fireVisual.transform;binding.FindPropertyRelative("glow").objectReferenceValue=light;
                    string completed="FireDone"+stage+"_"+i;InitialPhysical(completed);
                    var record=new YanYanaGraphAuthor(flow,"Tam sönen odağı kaydet · "+stage+" / "+i);var checkedFrame=record.Add(new Unity.VisualScripting.Update());
                    var justDone=record.Branch(checkedFrame.trigger,And(record,And(record,Is(record,record.Var("Phase",flow),5),Is(record,record.Var("FireStage",flow),stage)),And(record,Is(record,record.Var(completed,flow),0),Is(record,record.Get(typeof(Collider),"enabled",hit),false))));
                    var saved=record.SetVar(justDone.ifTrue,completed,1,flow);record.Send(saved,flow,"CommitCheckpoint");record.Dirty();
                    var restoreTarget=main.Event("RestoreFireGroup"+stage);var already=main.Branch(restoreTarget.trigger,And(main,Is(main,main.Var(completed),1),main.Get(typeof(Collider),"enabled",hit)));
                    // Reapply only a persisted completed target through the existing manager.
                    main.Do(already.ifTrue,typeof(FirefighterExtinguishManager),"ApplyWater",manager,new[]{typeof(Vector3),typeof(int),typeof(float)},target.transform.position,i,12f);
                }
                so.ApplyModifiedPropertiesWithoutUndo();Number(manager,"gameDuration",36000);Number(manager,"extinguishSeconds",6f);Number(manager,"sprayRadius",.28f);Number(manager,"aimPlaneHeight",.15f);
                var audio=Group("Hortumun sesi",root.transform).AddComponent<AudioSource>();audio.loop=true;audio.playOnAwake=false;audio.volume=.35f;if(sounds.TryGetValue("water",out var water))audio.clip=water;Serialized(manager,"sprayAudio",audio);
                if(!manager.ValidateConfiguration(out string error))throw new InvalidOperationException(error);fireManagers["physical"+stage]=manager;
                CreateFirePointerSurface(manager,stage);
                var wet=Shape("Söndürülmüş ıslak alan "+stage,PrimitiveType.Cylinder,world.transform,center+Vector3.up*.017f,new Vector3(2.5f,.012f,1.8f),mats["YY_tealDark"]);wet.SetActive(false);wetPatches[stage]=wet;
                var view=WorkCamera("fire"+stage,center+Vector3.up*.6f,2.9f,false);view.transform.position=fireStands[stage]+new Vector3(2.6f,2.6f,4.2f);view.transform.LookAt(center+new Vector3(-.60f,.5f,.22f));
                var lens=view.Lens;lens.ModeOverride=Unity.Cinemachine.LensSettings.OverrideModes.Perspective;lens.FieldOfView=46;view.Lens=lens;
                var update=main.Add(new Unity.VisualScripting.Update());var finished=main.Branch(update.trigger,And(main,And(main,Is(main,main.Var("Phase"),5),Is(main,main.Var("FireStage"),stage)),And(main,Available(main),main.Get(typeof(FirefighterExtinguishManager),"IsSuccessful",manager))));
                var path=main.SetVar(finished.ifTrue,"FireStage",stage+1);path=main.Active(path,wet,true);main.Send(path,flow,"AdvanceFire");
            }
            for(int i=0;i<5;i++)Model("Cone",world.transform,new Vector3(-6+i,-.48f,-14.4f),1,0);
            var safetyBoundary=Group("İtfaiyenin kapattığı müdahale sınırı",world.transform);
            foreach(var side in new[]{(new Vector3(-4,-.48f,-14.45f),new Vector3(8,.8f,.16f)),(new Vector3(-8,-.48f,-18.2f),new Vector3(.16f,.8f,7.5f)),(new Vector3(0,-.48f,-18.2f),new Vector3(.16f,.8f,7.5f)),(new Vector3(-4,-.48f,-22),new Vector3(8,.8f,.16f))})
            {
                var barrier=Group("Güvenli tarafta kalınan şerit",safetyBoundary.transform,side.Item1);var wall=barrier.AddComponent<BoxCollider>();wall.center=Vector3.up*.4f;wall.size=side.Item2;var obstacle=barrier.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.center=wall.center;obstacle.size=wall.size;obstacle.carving=true;
                Shape("İtfaiye sınır şeridi",PrimitiveType.Cube,barrier.transform,new Vector3(0,.65f,0),new Vector3(side.Item2.x,.06f,side.Item2.z),mats["YY_mustard"]);
            }
            var boundaryGraph=new YanYanaGraphAuthor(flow,"Çocuklar güvenli tarafta; görevli açmadan yangın alanına girilmez");var boundaryUpdate=boundaryGraph.Add(new Unity.VisualScripting.Update());boundaryGraph.Active(boundaryUpdate.trigger,safetyBoundary,Or(boundaryGraph,Is(boundaryGraph,boundaryGraph.Var("FireCompleted",flow),0),Is(boundaryGraph,boundaryGraph.Var("FireAssisted",flow),1)));boundaryGraph.Dirty();
            var responderHit=idil.AddComponent<CapsuleCollider>();responderHit.radius=.3f;responderHit.height=1.75f;responderHit.center=Vector3.up*.87f;
            ApproachEvent(idil,street["Idil"].position+Vector3.forward*.8f,"EnterFirefighter",4);
            fireHelp=Button("FireHelp","Ekipten destek al",safeRect,new Vector2(0,0),new Vector2(0,0),new Vector2(20,214),new Vector2(240,286)).gameObject;fireHelp.SetActive(false);
            var enter=main.Event("EnterFirefighter",true);var p=main.SetVar(enter.trigger,"Phase",5);p=main.SetVar(p,"Busy",true);p=Text(main,p,chapterText,"İDİL’İN GÖREVİ");p=Text(main,p,lineText,"İdil: Siz güvenli noktada kalın. Müdahaleyi ekibimiz yapacak.");p=WalkAndWait(main,p,movers["Ada"],new Vector3(2,-.48f,-13.8f));p=main.Send(p,flow,"EnterRole","Idil");p=main.Wait(p,1.1f);p=Released(main,p);main.Send(p,flow,"PrepareHose");
            var advance=main.Event("AdvanceFire",true);p=main.SetVar(advance.trigger,"Busy",true);p=main.Active(p,activityBack,false);p=main.Active(p,rotateControl,false);p=main.Active(p,fireHelp,true);
            foreach(var item in fireManagers)p=main.Active(p,item.Value.gameObject,false);foreach(var view in workCameras.Values)p=main.Active(p,view.gameObject,false);
            var stages=main.Add(new SwitchOnInteger{options=new List<int>{1,2,3}});main.Bind(stages.selector,main.Var("FireStage"));main.Link(p,stages.enter);
            foreach(var branch in stages.branches)
            {
                int stage=branch.Key;var q=Text(main,branch.Value,goalText,"İdil ile yeni konuma ilerle");q=main.Active(q,workCameras["fire"+stage].gameObject,true);q=WalkAndWait(main,q,movers["Idil"],fireStands[stage]);q=main.Set(q,typeof(Transform),"rotation",idil.transform,Quaternion.Euler(0,180,0));
                q=main.SetVar(q,"Workspace","fire"+stage);q=main.Active(q,workCameras["fire"+stage].gameObject,true);q=Text(main,q,goalText,stage==1?"Soldaki yangın odaklarını kontrol et":stage==2?"Yeni konumdan kalan alevlere ulaş":"Arka bölümü son kez kontrol et");q=Text(main,q,gestureText,"Suyu alevin tabanına yönlendir");q=Text(main,q,lineText,"İdil: Yeni konumdayız. Her odağı dikkatle kontrol edelim.");q=main.Wait(q,.8f);q=Released(main,q);q=main.Active(q,fireManagers["physical"+stage].gameObject,true);q=main.Active(q,firePreviews[stage],false);q=main.Send(q,flow,"RestoreFireGroup"+stage);q=main.SetVar(q,"Busy",false);main.Send(q,flow,"CommitCheckpoint");
            }
            main.Send(stages.@default,flow,"CompleteFire");
            var helpState=new YanYanaGraphAuthor(flow,"Ekip desteği yalnız kontrol sende olduğunda kullanılabilir");var helpUpdate=helpState.Add(new Unity.VisualScripting.Update());helpState.Set(helpUpdate.trigger,typeof(Selectable),"interactable",buttons["FireHelp"],Available(helpState));helpState.Dirty();
            p=ButtonEvent(main,"FireHelp","RequestFireSupport");var during=main.Branch(p,And(main,Is(main,main.Var("Phase"),5),Available(main)));p=main.SetVar(during.ifTrue,"FireAssisted",1);p=main.SetVar(p,"FireStage",4);main.Send(p,flow,"CompleteFire");
            var complete=main.Event("CompleteFire",true);p=main.SetVar(complete.trigger,"Busy",true);p=main.SetVar(p,"FireCompleted",1);p=main.SetVar(p,"Phase",6);p=main.SetVar(p,"Workspace","");p=main.Active(p,fireHelp,false);
            foreach(var item in fireManagers)p=main.Active(p,item.Value.gameObject,false);foreach(var view in workCameras.Values)p=main.Active(p,view.gameObject,false);
            foreach(var preview in firePreviews.Values)p=main.Active(p,preview,false);
            p=Text(main,p,lineText,"İdil: Kontrol tamam. Yardım noktasına güvenli geçişi izleyin.");p=main.Send(p,flow,"EnterRole","Ada");p=main.Wait(p,1.1f);p=Released(main,p);p=Text(main,p,goalText,"Bora’nın yardım noktasına ulaş");p=main.SetVar(p,"Busy",false);main.Send(p,flow,"CommitCheckpoint");
            CreateHoseCoupling();CreatePhysicalRoles();
            var restore=main.Event("RestorePhysicalObjects");p=restore.trigger;for(int i=1;i<=3;i++){p=main.Active(p,firePreviews[i],And(main,And(main,Is(main,main.Var("FireCompleted"),0),main.Binary<LessOrEqual>(main.Var("FireStage"),i)),Is(main,Is(main,main.Var("Workspace"),"fire"+i),false)));p=main.Active(p,wetPatches[i],main.Binary<Greater>(main.Var("FireStage"),i));}
        }

        static void CreateHoseCoupling()
        {
            var center=street["Idil"].position+new Vector3(.7f,.10f,-.5f);var root=Group("İdil’in hortum bağlantısı",world.transform,center);root.SetActive(false);
            var fixedPart=Shape("Pompanın bağlantı ucu",PrimitiveType.Cylinder,root.transform,new Vector3(0,.02f,.15f),new Vector3(.10f,.06f,.10f),mats["YY_metal"]);fixedPart.transform.localRotation=Quaternion.Euler(90,0,0);
            var plug=Shape("Hortumun kavrama ucu",PrimitiveType.Cylinder,root.transform,new Vector3(-.2f,.02f,-.15f),new Vector3(.10f,.07f,.10f),mats["YY_teal"]);plug.transform.localRotation=Quaternion.Euler(90,0,0);plug.AddComponent<BoxCollider>();
            var target=center+new Vector3(0,.02f,.026f);var rest=plug.transform.position;
            var g=new YanYanaGraphAuthor(plug,"Hortum bağlantısını yerleştir");var drag=g.Add(new OnDrag());g.Bind(drag.target,plug);var allowed=g.Branch(drag.trigger,AtWork(g,"hose"));var projected=PointerOnPlane(g,allowed.ifTrue,drag.data,target.y);g.Set(projected.path,typeof(Transform),"position",plug.transform,projected.point);
            var drop=g.Add(new OnEndDrag());g.Bind(drop.target,plug);allowed=g.Branch(drop.trigger,AtWork(g,"hose"));var fits=g.Branch(allowed.ifTrue,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",plug.transform),target).result,.085f));
            var p=g.Set(fits.ifTrue,typeof(Transform),"position",plug.transform,target);p=g.SetVar(p,"HoseConnected",1,flow);Text(g,p,lineText,"İdil: Bağlantı oturdu. Şimdi vanayı çevirelim.");g.Set(fits.ifFalse,typeof(Transform),"position",plug.transform,rest);g.Dirty();
            var valve=Group("Su vanası",root.transform,new Vector3(.19f,.04f,.15f));
            Shape("Vana çubuğu",PrimitiveType.Cube,valve.transform,Vector3.zero,new Vector3(.20f,.025f,.035f),mats["YY_coral"]);var hit=valve.AddComponent<BoxCollider>();hit.size=new Vector3(.23f,.10f,.20f);
            var vg=new YanYanaGraphAuthor(valve,"Bağlantı tamamlandıktan sonra suyu aç");vg.Initial("Angle",0f);var turn=vg.Add(new OnDrag());vg.Bind(turn.target,valve);var can=vg.Branch(turn.trigger,AtWork(vg,"hose"));var point=PointerOnPlane(vg,can.ifTrue,turn.data,valve.transform.position.y);
            var angle=vg.Binary<ScalarMultiply>(vg.Call(typeof(Mathf),"Atan2",null,new[]{typeof(float),typeof(float)},vg.Binary<ScalarSubtract>(vg.Get(typeof(Vector3),"z",point.point),valve.transform.position.z),vg.Binary<ScalarSubtract>(vg.Get(typeof(Vector3),"x",point.point),valve.transform.position.x)).result,Mathf.Rad2Deg);
            p=vg.SetVar(point.path,"Angle",vg.Call(typeof(Mathf),"Abs",null,OneFloat,angle).result);vg.Set(p,typeof(Transform),"eulerAngles",valve.transform,V3(vg,0f,angle,0f));
            var released=vg.Add(new OnEndDrag());vg.Bind(released.target,valve);var open=vg.Branch(released.trigger,And(vg,And(vg,AtWork(vg,"hose"),Is(vg,vg.Var("HoseConnected",flow),1)),vg.Binary<Greater>(vg.Var("Angle"),70f)));
            p=vg.SetVar(open.ifTrue,"HoseReady",1,flow);p=vg.Active(p,root,false);vg.Send(p,flow,"AdvanceFire");vg.Dirty();
            WorkCamera("hose",center+new Vector3(0,0,-.08f),.60f);
            var prepare=main.Event("PrepareHose");p=main.Active(prepare.trigger,root,true);main.Send(p,flow,"OpenWork","hose","Hortumu bağla ve suyu aç","İdil: Önce bağlantıyı oturtalım. Sonra vanayı çevirelim.");
        }

        static void CreatePhysicalRoles()
        {
            var role=main.Event("EnterRole",true,1);var p=main.SetVar(role.trigger,"Busy",true);p=main.SetVar(p,"Dragging",false);p=main.SetVar(p,"PointerOwner",-999);
            foreach(var mover in movers.Values){p=main.Do(p,typeof(StoryPlayerMovement),"Stop",mover,NoArgs);p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",mover,OneBool,true);}
            p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=main.SetVar(p,"Role",role.argumentPorts[0]);p=main.Active(p,rolePanel,true);p=Text(main,p,roleBannerText,Concat(main,"Şimdi ",role.argumentPorts[0]));p=main.Wait(p,.75f);p=Released(main,p);p=main.Active(p,rolePanel,false);
            var select=main.Add(new SwitchOnString{options=new List<string>{"Ada","Idil","Bora"}});main.Bind(select.selector,role.argumentPorts[0]);main.Link(p,select.enter);
            foreach(var branch in select.branches)
            {
                var q=main.SetVar(branch.Value,"ActiveMover",movers[branch.Key]);q=Text(main,q,roleText,branch.Key=="Idil"?"İdil":branch.Key);q=main.Set(q,typeof(Image),"sprite",portraitImage,portraits[branch.Key]);q=main.Do(q,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers[branch.Key],OneBool,false);main.Do(q,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,branch.Key=="Ada");
            }
        }
    }
}
