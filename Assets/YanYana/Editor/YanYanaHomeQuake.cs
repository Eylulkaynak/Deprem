using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static GameObject apartmentExit, exitDoor;
        static readonly Dictionary<string,Transform> street=new Dictionary<string,Transform>();
        static GameObject neighborhood;
        static readonly Dictionary<string,Vector3> coverOffsets=new Dictionary<string,Vector3>();
        static readonly Dictionary<string,Vector3> idleOffsets=new Dictionary<string,Vector3>();
        static readonly Dictionary<string,Transform> headPalms=new Dictionary<string,Transform>();
        static readonly Dictionary<string,Transform> secondHeadPalms=new Dictionary<string,Transform>();

        static void CreateCoverPalmAnchors()
        {
            headPalms.Clear();secondHeadPalms.Clear();
            // Head-local surface points measured on the actual crouched meshes.
            // Coordinates retain the imported head bone's centimetre scale.
            foreach(string who in new[]{"Ada","Efe"})
            {
                var head=cast[who].GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                var point=who=="Ada"?new Vector3(3.325426f,10.84628f,-13.30276f):new Vector3(-9.864079f,4.297814f,-18.48821f);
                var inward=who=="Ada"?new Vector3(-.2159547f,.201738f,.9553354f):new Vector3(.3451046f,.5856805f,.7334036f);
                var primary=Group("Başta güvenli avuç teması · "+who,head,point).transform;primary.localRotation=Quaternion.FromToRotation(Vector3.forward,inward);headPalms[who]=primary;
                var secondPoint=who=="Ada"?new Vector3(-12.06684f,13.12171f,-10.04719f):new Vector3(-point.x,point.y,point.z);
                var secondNormal=who=="Ada"?new Vector3(.4269049f,.2570296f,.8669995f):new Vector3(-inward.x,inward.y,inward.z);
                var secondary=Group("İkinci koruyucu avuç · "+who,head,secondPoint).transform;secondary.localRotation=Quaternion.FromToRotation(Vector3.forward,secondNormal);secondHeadPalms[who]=secondary;
                var towardChild=anchors["Anchor_Cover"+who].position-anchors["Anchor_Hold"+who].position;towardChild.y=0;
                anchors["Anchor_Hold"+who].position+=towardChild.normalized*(who=="Efe"?.03f:.012f);
            }
        }

        static AnimationClip CreateAdventureCoverPose()
        {
            var source=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Story/Animations/Generated/ChildCoverPose.anim");
            string path=Root+"Animation/YanYana_Cover.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip)EditorUtility.CopySerialized(source,clip);else {clip=UnityEngine.Object.Instantiate(source);AssetDatabase.CreateAsset(clip,path);}clip.name="YanYana_Cover";
            foreach(var muscle in new[]{("Spine Front-Back",-.30f),("Chest Front-Back",-.34f),("UpperChest Front-Back",-.20f),("Neck Nod Down-Up",-.15f),("Head Nod Down-Up",-.2f),("Chest Left-Right",0f),("Chest Twist Left-Right",0f),("UpperChest Left-Right",0f),("UpperChest Twist Left-Right",0f),("Left Upper Leg Front-Back",-.92f),("Right Upper Leg Front-Back",-.92f),("Left Lower Leg Stretch",-.92f),("Right Lower Leg Stretch",-.92f)})clip.SetCurve("",typeof(Animator),muscle.Item1,AnimationCurve.Constant(0,clip.length,muscle.Item2));
            EditorUtility.SetDirty(clip);return clip;
        }
        static Bounds ActualSkinnedBounds(GameObject actor)
        {
            bool first=true;var bounds=new Bounds();var baked=new Mesh();
            foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.BakeMesh(baked,false);foreach(var vertex in baked.vertices){var point=renderer.transform.TransformPoint(vertex);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
            }
            UnityEngine.Object.DestroyImmediate(baked);return bounds;
        }
        static void CalibrateCoverOffsets()
        {
            coverOffsets.Clear();idleOffsets.Clear();var report=new List<string>();
            foreach(string who in new[]{"Ada","Efe","Derya","Emre"})
            {
                var actor=cast[who];var animator=actor.GetComponentInChildren<Animator>();idleOffsets[who]=animator.transform.localPosition;
                var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                float idleAnkle=Mathf.Min(left.position.y,right.position.y);
                animator.Play("Çök, korun, tutun",0,0);animator.Update(.02f);
                float coverAnkle=Mathf.Min(left.position.y,right.position.y);
                animator.transform.position+=Vector3.up*(idleAnkle-coverAnkle);coverOffsets[who]=animator.transform.localPosition;
                report.Add(who+" idle ankle="+idleAnkle.ToString("F5")+" cover ankle before correction="+coverAnkle.ToString("F5")+" head="+animator.GetBoneTransform(HumanBodyBones.Head).position+" visual offset="+coverOffsets[who]);
                animator.Play("Duruş ve yürüyüş",0,0);animator.Update(0);animator.transform.localPosition=idleOffsets[who];
            }
            System.IO.File.WriteAllLines("ClientExports/YanYana/Reports/cover-calibration.txt",report);
        }

        static void CreateNeighborhoodWorld()
        {
            street.Clear();neighborhood=Model("NeighborhoodGround",world.transform,Vector3.zero);
            foreach(var t in neighborhood.GetComponentsInChildren<Transform>())
            {
                if(t.name.StartsWith("Street_"))street[t.name.Substring(7)]=t;
                if(t.name.StartsWith("COLLIDER_")){var c=t.gameObject.AddComponent<BoxCollider>();c.size=Vector3.one*.01f;}
            }
            CreateNeighborhoodPark();
            for(int i=0;i<5;i++)
            {
                Vector3 p=i<3?new Vector3(-12,-.48f,-12-i*4.1f):new Vector3(12,-.48f,-11-(i-3)*6);
                var facade=Model("Facade_"+(i%4+1),world.transform,p,.65f,i<3?90:270);facade.name="Mahalle evi "+i;
            }
            // Cutaway architecture keeps the room readable but still has a single real doorway.
            Shape("Dairenin kesit duvarı",PrimitiveType.Cube,world.transform,new Vector3(.85f,.25f,-3.83f),new Vector3(6.1f,.5f,.12f),mats["YY_cream"],true);
            Shape("Kapı yan duvarı",PrimitiveType.Cube,world.transform,new Vector3(-3.84f,.25f,-3.83f),new Vector3(.12f,.5f,.12f),mats["YY_cream"],true);
            apartmentExit=Group("Apartmana açılan kapı",world.transform,new Vector3(-3.1f,0,-3.82f));
            exitDoor=Shape("Kapalı kapı",PrimitiveType.Cube,apartmentExit.transform,new Vector3(0,.8f,0),new Vector3(1.20f,1.6f,.10f),mats["YY_teal"]);
            var hit=exitDoor.AddComponent<BoxCollider>();hit.size=Vector3.one;
            var obstacle=exitDoor.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one;obstacle.carving=true;
            Shape("Kapı kolu",PrimitiveType.Sphere,exitDoor.transform,new Vector3(.32f,0,-.65f),Vector3.one*.08f,mats["YY_mustard"]);
        }

        static void CreateHomeQuake()
        {
            CalibrateCoverOffsets();
            CreateCoverPalmAnchors();
            var coverFocus=anchors["Anchor_CoverAda"].position+new Vector3(-.22f,.58f,0);
            var coverView=WorkCamera("cover",coverFocus,1.35f,false);
            coverView.transform.position=coverFocus+new Vector3(-.60f,.45f,-2.30f);coverView.transform.LookAt(coverFocus);
            main.Initial("Quake",false);InitialPhysical("Protected");InitialPhysical("SiblingChecked");InitialPhysical("LightFound");InitialPhysical("ExitOpened");
            var ready=Button("PreparationDone","Efe ile oyuna geç",safeRect,new Vector2(1,0),new Vector2(1,0),new Vector2(-254,214),new Vector2(-20,286)).gameObject;
            var readyGraph=new YanYanaGraphAuthor(flow,"Hazırlığı istediğin noktada tamamla");var update=readyGraph.Add(new Unity.VisualScripting.Update());
            readyGraph.Active(update.trigger,ready,And(readyGraph,CanExplore(readyGraph),Is(readyGraph,readyGraph.Var("Phase",flow),0)));
            var p=ButtonEvent(main,"PreparationDone","ReadyForDay");var allowed=main.Branch(p,And(main,CanExplore(main),Is(main,main.Var("Phase"),0)));p=main.Send(allowed.ifTrue,flow,"CapturePhysicalSnapshot","home");p=main.Active(p,ready,false);
            p=main.SetVar(p,"Busy",true);p=Text(main,p,goalText,"Efe’ye oyuncak köprüsünü kur");p=Text(main,p,lineText,"Efe: Küçük tilki karşıya geçemiyor. Köprü kurabilir miyiz?");main.Send(p,flow,"ToyBridge");
            CreateToyBridge();
            var quake=main.Event("BeginQuake",true);p=main.SetVar(quake.trigger,"Phase",1);p=main.SetVar(p,"Quake",true);p=main.SetVar(p,"Workspace","");p=main.SetVar(p,"Busy",false);
            p=main.Active(p,activityBack,false);p=main.Active(p,rotateControl,false);foreach(var view in workCameras.Values)p=main.Active(p,view.gameObject,false);
            p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,true);p=main.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);
            p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Derya"],OneBool,true);
            p=Text(main,p,chapterText,"SARSINTI");p=Text(main,p,goalText,"Yakındaki sağlam masada korun");p=Text(main,p,gestureText,"Çök · başını koru · tutun");p=Text(main,p,lineText,"Ada: Sarsıntı başladı! Yakınımızda güvenle korunmalıyız.");
            p=main.Set(p,typeof(Light),"intensity",sun,.43f);
            foreach(string adult in new[]{"Derya","Emre"})
            {
                var animator=cast[adult].GetComponentInChildren<Animator>();p=main.Do(p,typeof(Animator),"SetInteger",animator,new[]{typeof(string),typeof(int)},"Pose",1);
                p=main.Set(p,typeof(Transform),"localPosition",animator.transform,coverOffsets[adult]);
            }
            if(sounds.TryGetValue("rumble",out var rumble))p=main.Do(p,typeof(AudioSource),"PlayOneShot",feedback,new[]{typeof(AudioClip)},rumble);
            p=main.Send(p,flow,"ShakeFurniture");main.Send(p,flow,"CommitCheckpoint");
            var table=roomModel.GetComponentsInChildren<Transform>().First(t=>t.name=="COLLIDER_TableTop").gameObject;
            var tableGraph=new YanYanaGraphAuthor(table,"Yakındaki masanın altında çök, korun, tutun");var click=tableGraph.Add(new OnPointerClick());tableGraph.Bind(click.target,table);
            InitialPhysical("CoverStage");main.Initial("CoverRightAim",Vector3.zero);main.Initial("CoverLeftAim",Vector3.zero);main.Initial("CoverRightRest",Vector3.zero);main.Initial("CoverLeftRest",Vector3.zero);
            var safe=tableGraph.Branch(click.trigger,And(tableGraph,And(tableGraph,Available(tableGraph),Is(tableGraph,tableGraph.Var("CoverStage",flow),0)),Is(tableGraph,tableGraph.Var("Phase",flow),1)));tableGraph.Send(safe.ifTrue,flow,"ProtectTogether");tableGraph.Dirty();
            var protection=main.Event("ProtectTogether",true);p=main.SetVar(protection.trigger,"Busy",true);
            p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,false);
            var edge=anchors["Anchor_CoverAda"].position+Vector3.back*.9f;
            p=main.Do(p,typeof(StoryPlayerMovement),"TrySetDestination",movers["Ada"],new[]{typeof(Vector3)},edge);
            var wait=main.Add(new WaitUntilUnit());main.Bind(wait.condition,main.Binary<Less>(main.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},main.Get(typeof(Transform),"position",cast["Ada"].transform),edge).result,.4f));main.Link(p,wait.enter);p=wait.exit;
            p=main.SetVar(p,"CoverStage",1);p=main.Active(p,coverView.gameObject,true);p=Text(main,p,goalText,"Yakında çök");p=Text(main,p,gestureText,"Ada’yı aşağı doğru sürükle");p=Text(main,p,lineText,"Ada: Önce çökelim. Sonra başımızı ve ensemizi koruyalım.");p=main.Wait(p,.82f);p=Released(main,p);main.SetVar(p,"Busy",false);
            var lower=main.Event("LowerIntoCover",true);p=main.SetVar(lower.trigger,"Busy",true);
            foreach(string who in new[]{"Ada","Efe"})
            {
                var animator=cast[who].GetComponentInChildren<Animator>();
                p=main.Do(p,who=="Ada"?typeof(StoryPlayerMovement):typeof(StorySiblingFollower),"SetAuthoredPose",who=="Ada"?(object)movers["Ada"]:follower,new[]{typeof(Vector3),typeof(Quaternion)},anchors["Anchor_Cover"+who].position,Quaternion.Euler(0,180,0));
                p=main.Do(p,typeof(Animator),"SetInteger",animator,new[]{typeof(string),typeof(int)},"Pose",1);
                p=main.Set(p,typeof(Transform),"localPosition",animator.transform,coverOffsets[who]);
            }
            p=main.Wait(p,.45f);var adaAnimator=cast["Ada"].GetComponentInChildren<Animator>();
            foreach(string side in new[]{"Right","Left"}){var hand=adaAnimator.GetBoneTransform(side=="Right"?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);p=main.SetVar(p,"Cover"+side+"Rest",main.Get(typeof(Transform),"position",hand));p=main.SetVar(p,"Cover"+side+"Aim",main.Get(typeof(Transform),"position",hand));}
            p=main.SetVar(p,"CoverStage",2);p=Text(main,p,goalText,"Başını ve enseni koru");p=Text(main,p,gestureText,"İşaretli eli başına doğru götür");p=Text(main,p,lineText,"Ada: Elimi başımın üzerine götürüyorum. Efe de benimle korunuyor.");p=Released(main,p);p=main.SetVar(p,"Busy",false);main.Send(p,flow,"CommitCheckpoint");
            var held=main.Event("FinishCoverGrip",true);p=main.SetVar(held.trigger,"Busy",true);p=main.SetVar(p,"Protected",1);p=Text(main,p,goalText,"Tutunarak korunmayı sürdür");p=Text(main,p,gestureText,"Sarsıntı bitene kadar korun");p=Text(main,p,lineText,"Ada: Başımız korunuyor. Masanın ayağına tutunuyoruz.");p=main.Wait(p,3.2f);
            p=main.Active(p,coverView.gameObject,false);
            p=main.SetVar(p,"Quake",false);p=main.SetVar(p,"Phase",2);p=main.SetVar(p,"Busy",false);p=Text(main,p,chapterText,"BİRLİKTE DIŞARI");p=Text(main,p,goalText,"Efe’yi ve çıkışı kontrol et");p=Text(main,p,gestureText,"Efe’ye yaklaş · çıkış yolunu incele");p=Text(main,p,lineText,"Ada: Sarsıntı durdu. Efe, iyi misin?");
            foreach(string who in new[]{"Ada","Efe"})
            {
                p=main.Do(p,typeof(Animator),"SetInteger",cast[who].GetComponentInChildren<Animator>(),new[]{typeof(string),typeof(int)},"Pose",0);
                p=main.Set(p,typeof(Transform),"localPosition",cast[who].GetComponentInChildren<Animator>().transform,idleOffsets[who]);
                p=main.Do(p,who=="Ada"?typeof(StoryPlayerMovement):typeof(StorySiblingFollower),"ReleaseAuthoredPose",who=="Ada"?(object)movers["Ada"]:follower,new[]{typeof(Vector3)},edge+Vector3.right*(who=="Ada"?0:.65f));
            }
            foreach(string adult in new[]{"Derya","Emre"})
            {
                var animator=cast[adult].GetComponentInChildren<Animator>();p=main.Do(p,typeof(Animator),"SetInteger",animator,new[]{typeof(string),typeof(int)},"Pose",0);
                p=main.Set(p,typeof(Transform),"localPosition",animator.transform,idleOffsets[adult]);
            }
            p=main.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",movers["Ada"],OneBool,false);p=main.Send(p,flow,"SetAfterQuake");main.Send(p,flow,"CommitCheckpoint");
            CreateCoverGestures();CreateFurnitureConsequences();CreateAfterQuakeInvestigation();CreatePhysicalHandContact();
            // Unsafe attempts stay local: no death scene and no preparation reset.
            foreach(var target in roomModel.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("WindowFrame")))
            {
                var b=target.gameObject.AddComponent<BoxCollider>();var g=new YanYanaGraphAuthor(target.gameObject,"Pencereye gitme girişiminde yerel yeniden deneme");var attempt=g.Add(new OnPointerClick());g.Bind(attempt.target,target.gameObject);
                var active=g.Branch(attempt.trigger,Is(g,g.Var("Phase",flow),1));Text(g,active.ifTrue,lineText,"Ada: Camdan uzak duralım. Yakındaki masada korunalım.");g.Dirty();
            }
            readyGraph.Dirty();
        }

        static void CreateToyBridge()
        {
            var at=anchors["Anchor_CoverAda"].position+new Vector3(0,anchors["Anchor_Comfort"].position.y+.01f,0);
            var board=Group("Efe’nin küçük köprü oyunu",world.transform,at);board.SetActive(false);
            Shape("Oyun nehri",PrimitiveType.Cube,board.transform,Vector3.zero,new Vector3(.65f,.012f,.28f),mats["YY_blue"]);
            var fox=Model("ComfortFox",board.transform,new Vector3(-.31f,.02f,-.01f),.38f,90);
            InitialPhysical("BridgePieces");
            for(int i=0;i<3;i++)
            {
                int n=i;InitialPhysical("Bridge"+i);var rest=at+new Vector3(-.22f+i*.22f,.025f,-.36f);var destination=at+new Vector3(-.20f+i*.20f,.025f,0);
                var piece=Shape("Köprü parçası "+i,PrimitiveType.Cube,board.transform,board.transform.InverseTransformPoint(rest),new Vector3(.18f,.04f,.38f),mats["YY_wood"]);piece.AddComponent<BoxCollider>();
                var g=new YanYanaGraphAuthor(piece,"Nehir üzerindeki köprüyü birleştir");var drag=g.Add(new OnDrag());g.Bind(drag.target,piece);var allowed=g.Branch(drag.trigger,AtWork(g,"bridge"));var projected=PointerOnPlane(g,allowed.ifTrue,drag.data,rest.y);g.Set(projected.path,typeof(Transform),"position",piece.transform,projected.point);
                var end=g.Add(new OnEndDrag());g.Bind(end.target,piece);allowed=g.Branch(end.trigger,AtWork(g,"bridge"));var close=g.Branch(allowed.ifTrue,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",piece.transform),destination).result,.12f));
                var p=g.Set(close.ifTrue,typeof(Transform),"position",piece.transform,destination);p=g.SetVar(p,"Bridge"+n,1,flow);p=g.Send(p,flow,"CommitCheckpoint");g.Send(p,flow,"CheckBridge");g.Set(close.ifFalse,typeof(Transform),"position",piece.transform,rest);g.Dirty();
                var restore=main.Event("RestorePhysicalObjects");var assembled=main.Branch(restore.trigger,Is(main,main.Var("Bridge"+n),1));main.Set(assembled.ifTrue,typeof(Transform),"position",piece.transform,destination);main.Set(assembled.ifFalse,typeof(Transform),"position",piece.transform,rest);
            }
            var start=main.Event("ToyBridge",true);var path=main.Active(start.trigger,board,true);path=main.Active(path,physicalItems["ComfortFox"],false);
            var approach=anchors["Anchor_CoverAda"].position+Vector3.back*1.0f;
            path=main.Do(path,typeof(StoryPlayerMovement),"TrySetDestination",movers["Ada"],new[]{typeof(Vector3)},approach);
            var arrived=main.Add(new WaitUntilUnit());main.Bind(arrived.condition,main.Binary<Less>(main.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},main.Get(typeof(Transform),"position",cast["Ada"].transform),approach).result,.4f));main.Link(path,arrived.enter);
            main.Send(arrived.exit,flow,"OpenWork","bridge","Tilkiye bir köprü kur","Efe: Üç tahtayı suyun üzerine yan yana yerleştir.");
            WorkCamera("bridge",at+new Vector3(0,0,-.14f),.77f);
            var check=main.Event("CheckBridge",true);var all=main.Branch(check.trigger,And(main,Is(main,main.Var("Bridge0"),1),And(main,Is(main,main.Var("Bridge1"),1),Is(main,main.Var("Bridge2"),1))));
            path=main.SetVar(all.ifTrue,"Busy",true);path=main.Set(path,typeof(Transform),"localPosition",fox.transform,new Vector3(.30f,.02f,0));path=Text(main,path,lineText,"Efe: Tilki geçti! Birlikte başardık.");path=main.Wait(path,1.4f);path=main.Active(path,board,false);main.Send(path,flow,"BeginQuake");
        }

        static void CreateFurnitureConsequences()
        {
            foreach(var type in new[]{"shelf","wardrobe"})
            {
                var furniture=physicalItems[type];string flag=type=="shelf"?"ShelfSecured":"WardrobeSecured";
                var animation=furniture.AddComponent<Animation>();var clip=new AnimationClip{legacy=true,name="Sarsıntıda "+type};
                clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.x",new AnimationCurve(new Keyframe(0,0),new Keyframe(.25f,3),new Keyframe(.55f,-2),new Keyframe(.85f,8),new Keyframe(1.8f,78)));
                string path=Root+"Animation/"+type+"_Unsecured.anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}else AssetDatabase.CreateAsset(clip,path);
                animation.AddClip(clip,clip.name);animation.clip=clip;animation.playAutomatically=false;animation.wrapMode=WrapMode.ClampForever;
                var shake=main.Event("ShakeFurniture");var unsafeFurniture=main.Branch(shake.trigger,Is(main,main.Var(flag),0));main.Do(unsafeFurniture.ifTrue,typeof(Animation),"Play",animation,OneString,clip.name);
                var restore=main.Event("RestorePhysicalObjects");var fallen=main.Branch(restore.trigger,And(main,main.Binary<GreaterOrEqual>(main.Var("Phase"),2),Is(main,main.Var(flag),0)));
                main.Set(fallen.ifTrue,typeof(Transform),"localEulerAngles",furniture.transform,new Vector3(78,180,0));main.Set(fallen.ifFalse,typeof(Transform),"localEulerAngles",furniture.transform,new Vector3(0,180,0));
            }
        }

        static void CreateAfterQuakeInvestigation()
        {
            var efe=cast["Efe"];var hit=efe.AddComponent<CapsuleCollider>();hit.radius=.24f;hit.height=1.2f;hit.center=Vector3.up*.6f;
            var g=new YanYanaGraphAuthor(efe,"Kardeşini kontrol et, cevap vermesine alan aç");var click=g.Add(new OnPointerClick());g.Bind(click.target,efe);var active=g.Branch(click.trigger,And(g,CanExplore(g),Is(g,g.Var("Phase",flow),2)));
            var uncheckedSibling=g.Branch(active.ifTrue,Is(g,g.Var("SiblingChecked",flow),0));g.Send(uncheckedSibling.ifTrue,flow,"BeginSiblingCheck");g.Send(uncheckedSibling.ifFalse,flow,"RefreshSiblingGoal");g.Dirty();
            ControlOutput p;
            var lightObject=Group("Ada’nın kullanabildiği fener",cast["Ada"].transform,new Vector3(.18f,1.0f,.12f));torch=lightObject.AddComponent<Light>();torch.type=LightType.Spot;torch.range=5;torch.spotAngle=64;torch.intensity=0;lightObject.transform.localRotation=Quaternion.Euler(44,0,0);
            var emergency=Shape("Erişilebilir acil aydınlatma",PrimitiveType.Cube,world.transform,anchors["Anchor_Exit"].position+new Vector3(.25f,1.0f,.3f),new Vector3(.26f,.15f,.13f),mats["YY_mustard"]);emergency.AddComponent<BoxCollider>();
            var emergencyLight=emergency.AddComponent<Light>();emergencyLight.type=LightType.Point;emergencyLight.range=6;emergencyLight.intensity=0;emergencyLight.color=new Color(1,.89f,.67f);
            var lamp=new YanYanaGraphAuthor(emergency,"Çalışmayan fener için erişilebilir güvenli alternatif");var press=lamp.Add(new OnPointerClick());lamp.Bind(press.target,emergency);var allowed=lamp.Branch(press.trigger,And(lamp,CanExplore(lamp),Is(lamp,lamp.Var("Phase",flow),2)));
            p=lamp.SetVar(allowed.ifTrue,"LightFound",1,flow);p=lamp.Set(p,typeof(Light),"intensity",emergencyLight,2.5f);p=Text(lamp,p,lineText,"Ada: Acil ışık burada. Çıkış yolunu görebiliyoruz.");p=lamp.Send(p,flow,"RefreshSiblingGoal");lamp.Send(p,flow,"CommitCheckpoint");lamp.Dirty();
            var after=main.Event("SetAfterQuake");p=Text(main,after.trigger,chapterText,"SARSINTIDAN SONRA");var packed=And(main,Is(main,main.Var("FlashlightReady"),1),PackedForTravel(main,"Flashlight"));var hasLight=main.Branch(p,packed);
            p=main.SetVar(hasLight.ifTrue,"LightFound",1);p=main.Set(p,typeof(Light),"intensity",torch,2.5f);Text(main,p,lineText,"Efe: Denediğimiz fener çantadaydı. Yolumuz aydınlandı.");
            Text(main,hasLight.ifFalse,lineText,"Efe: Çıkıştaki acil ışığı bulabiliriz.");
            var restoreLight=main.Event("RestorePhysicalObjects");var lightOn=main.Branch(restoreLight.trigger,Is(main,main.Var("LightFound"),1));var portable=main.Branch(lightOn.ifTrue,packed);main.Set(portable.ifTrue,typeof(Light),"intensity",torch,2.5f);main.Set(portable.ifFalse,typeof(Light),"intensity",emergencyLight,2.5f);
            var door=new YanYanaGraphAuthor(exitDoor,"Sarsıntı bittikten sonra çıkışı birlikte değerlendir");var touch=door.Add(new OnPointerClick());door.Bind(touch.target,exitDoor);
            var phase=door.Branch(touch.trigger,And(door,CanExplore(door),Is(door,door.Var("Phase",flow),2)));var prepared=door.Branch(phase.ifTrue,And(door,Is(door,door.Var("SiblingChecked",flow),1),Is(door,door.Var("LightFound",flow),1)));
            door.Send(prepared.ifTrue,flow,"OpenApartmentExit");
            var openDoor=main.Event("OpenApartmentExit",true);p=main.SetVar(openDoor.trigger,"Busy",true);p=main.Active(p,exitDoor,false);
            // Allow the removed NavMesh obstacle to update before accepting an exit destination.
            // The completed flags stay untouched until this short door transition finishes.
            p=main.Wait(p,.35f);p=main.SetVar(p,"ExitOpened",1);p=main.SetVar(p,"Phase",3);p=Text(main,p,goalText,"Merdivenden birlikte dışarı ilerle");p=Text(main,p,gestureText,"Merdivendeki açık geçişe dokun");p=Text(main,p,lineText,"Ada: Sarsıntı durdu. Merdivenleri kontrol ederek ilerleyelim.");p=main.SetVar(p,"Busy",false);main.Send(p,flow,"CommitCheckpoint");
            Text(door,prepared.ifFalse,lineText,"Ada: Önce Efe’yi kontrol edip ışık bulalım.");
            var shaking=door.Branch(phase.ifFalse,Is(door,door.Var("Phase",flow),1));Text(door,shaking.ifTrue,lineText,"Ada: Sarsıntıda çıkışa koşmayalım. Yakında korunalım.");
            var restore=main.Event("RestorePhysicalObjects");main.Active(restore.trigger,exitDoor,Is(main,main.Var("ExitOpened"),0));door.Dirty();
            CreateSiblingCooperation();
        }

        static void CreatePhysicalHandContact()
        {
            // Reuse the project's existing two-bone solver. The imported avatars do not
            // reliably apply Animator hand IK; no extra player component is introduced.
            foreach(string who in new[]{"Ada","Efe","Derya","Emre"})
            {
                bool child=who=="Ada"||who=="Efe";
                var animator=cast[who].GetComponentInChildren<Animator>();var g=new YanYanaGraphAuthor(animator.gameObject,"Başını koruyan el ve gerçek tutunma teması");
                g.Initial("RightHandError",0f);g.Initial("LeftHandError",0f);g.Initial("ContactTicks",0f);
                var tick=g.Add(new Unity.VisualScripting.LateUpdate());
                var quake=And(g,Is(g,g.Var("Phase",flow),1),child?(object)g.Binary<GreaterOrEqual>(g.Var("CoverStage",flow),2):true);
                var after=And(g,Is(g,g.Var("Phase",flow),31),g.Var("Busy",flow));
                var active=g.Branch(tick.trigger,And(g,Is(g,g.Var("Paused",flow),false),Or(g,quake,after)));
                var headPoint=child?g.Get(typeof(Transform),"position",headPalms[who]):Add(g,g.Get(typeof(Transform),"position",animator.GetBoneTransform(HumanBodyBones.Head)),new Vector3(0,.21f,0));
                object rightGoal=headPoint,leftGoal=headPoint;
                if(child)
                {
                    var holdGoal=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},headPoint,anchors["Anchor_Hold"+who].position,g.Call(typeof(Convert),"ToSingle",null,OneBool,Is(g,g.Var("Phase",flow),1)).result).result;
                    if(who=="Ada")leftGoal=holdGoal;else rightGoal=holdGoal;
                }
                if(who=="Ada")
                {
                    rightGoal=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Var("CoverRightAim",flow),headPoint,g.Call(typeof(Convert),"ToSingle",null,OneBool,Or(g,Is(g,g.Var("Phase",flow),31),g.Binary<GreaterOrEqual>(g.Var("CoverStage",flow),3))).result).result;
                    leftGoal=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Var("CoverLeftAim",flow),leftGoal,g.Call(typeof(Convert),"ToSingle",null,OneBool,Or(g,Is(g,g.Var("Phase",flow),31),g.Binary<GreaterOrEqual>(g.Var("CoverStage",flow),4))).result).result;
                }
                var types=new[]{typeof(Transform),typeof(Transform),typeof(Transform),typeof(Vector3),typeof(Vector3)};
                var rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);var leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
                var p=g.SetVar(active.ifTrue,"ContactTicks",Sum(g,g.Var("ContactTicks"),1f));
                if(child)
                {
                    // Contact is the palm, not the wrist. The resting hand stays
                    // open on the head; the other cups the rounded table post.
                    bool headRight=who=="Ada";object headGoal=headRight?rightGoal:leftGoal;object postGoal=headRight?leftGoal:rightGoal;
                    var headHint=g.Call(typeof(Transform),"TransformDirection",cast[who].transform,new[]{typeof(Vector3)},new Vector3(headRight?-.8f:.8f,.3f,.2f)).result;
                    var otherHint=g.Call(typeof(Transform),"TransformDirection",cast[who].transform,new[]{typeof(Vector3)},new Vector3(headRight?.8f:-.8f,.3f,.2f)).result;
                    p=SolveGripArm(g,p,who,headGoal,headRight,"Open",g.Get(typeof(Transform),"forward",headPalms[who]),true,headHint);
                    var headBoth=g.Branch(p,Is(g,g.Var("Phase",flow),31));
                    SolveGripArm(g,headBoth.ifTrue,who,g.Get(typeof(Transform),"position",secondHeadPalms[who]),!headRight,"Open",g.Get(typeof(Transform),"forward",secondHeadPalms[who]),true,otherHint);
                    var held=SolveGripArm(g,headBoth.ifFalse,who,postGoal,!headRight,"Soft",Vector3.up,false,otherHint);
                    held=g.SetVar(held,"RightHandError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(g,who,true,headRight?"Open":"Soft"),rightGoal).result);
                    g.SetVar(held,"LeftHandError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(g,who,false,headRight?"Soft":"Open"),leftGoal).result);g.Dirty();continue;
                }
                p=g.Do(p,typeof(Deprem.Minigames.FirefighterExtinguishManager),"SolveArm",null,types,animator.GetBoneTransform(HumanBodyBones.RightUpperArm),animator.GetBoneTransform(HumanBodyBones.RightLowerArm),rightHand,rightGoal,new Vector3(.8f,.3f,-.2f));
                p=g.Do(p,typeof(Deprem.Minigames.FirefighterExtinguishManager),"SolveArm",null,types,animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),leftHand,leftGoal,new Vector3(-.8f,.7f,0));
                p=g.SetVar(p,"RightHandError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",rightHand),rightGoal).result);
                g.SetVar(p,"LeftHandError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",leftHand),leftGoal).result);g.Dirty();
            }
        }
    }
}
