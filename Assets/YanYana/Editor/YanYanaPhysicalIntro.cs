// Editor authoring only. Carrying, collision, gestures and persistence are native scene graphs.
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static readonly Vector3 introSource=new Vector3(-2.5f,.64f,-2.9f);
        static readonly Vector3 introPickup=new Vector3(-2.13f,0,-2.9f);
        static Vector3 introDrop=new Vector3(1.75f,.735f,-3.2f);
        static readonly Vector3 introDestination=new Vector3(1.4f,0,-3.2f);
        static ValueOutput IntroActive(YanYanaGraphAuthor g)=>And(g,Is(g,g.Var("Phase",flow),0),Is(g,g.Var("IntroDone",flow),0));
        static ValueOutput IntroFree(YanYanaGraphAuthor g)=>And(g,IntroActive(g),And(g,Available(g),Is(g,g.Var("Workspace",flow),"")));
        static ValueOutput IntroAt(YanYanaGraphAuthor g,int stage)=>Is(g,g.Var("IntroStage",flow),stage);
        static ValueOutput IntroHeight(YanYanaGraphAuthor g,float basis)=>Sum(g,basis,g.Binary<ScalarMultiply>(g.Call(typeof(Convert),"ToSingle",null,OneBool,Is(g,g.Var("Workspace",flow),"introDrop")).result,.12f));
        static ValueOutput IntroDepth(YanYanaGraphAuthor g,float basis)=>Sum(g,basis,0f);
        static ValueOutput IntroHeldPoint(YanYanaGraphAuthor g)=>g.Call(typeof(Transform),"TransformPoint",cast["Ada"].transform,new[]{typeof(Vector3)},V3(g,0f,IntroHeight(g,.65f),Sum(g,IntroDepth(g,.38f),g.Binary<ScalarMultiply>(g.Var("IntroTurn",flow),.26f)))).result;
        static ValueOutput IntroHeldRotation(YanYanaGraphAuthor g)=>g.Call(typeof(Quaternion),"Euler",null,new[]{typeof(float),typeof(float),typeof(float)},0f,Sum(g,g.Get(typeof(Vector3),"y",g.Get(typeof(Transform),"eulerAngles",cast["Ada"].transform)),g.Binary<ScalarMultiply>(g.Var("IntroTurn",flow),90f)),0f).result;
        static ValueOutput IntroCollision(YanYanaGraphAuthor g,object point,object rotation)=>g.Call(typeof(Physics),"CheckBox",null,new[]{typeof(Vector3),typeof(Vector3),typeof(Quaternion),typeof(int),typeof(QueryTriggerInteraction)},point,new Vector3(.36f,.067f,.10f),rotation,1<<29,QueryTriggerInteraction.Ignore).result;
        static ControlOutput SolveIntroArm(YanYanaGraphAuthor g,ControlOutput p,Animator animator,object target,bool right)
        {
            return SolveGripArm(g,p,"Ada",target,right,"Loop",g.Get(typeof(Transform),"right",cast["Ada"].transform));
        }
        static (Vector3 forward,Vector3 normal) IntroHandBasis(Animator animator,Transform hand)
        {
            // This approved rig ends at each hand. Derive the palm plane from actual weighted
            // mesh geometry at authoring time; do not invent absent finger transforms.
            var points=new System.Collections.Generic.List<Vector3>();
            foreach(var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                int id=Array.IndexOf(renderer.bones,hand);if(id<0)continue;var mesh=renderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes[id];
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=weights[i];float weight=(w.boneIndex0==id?w.weight0:0)+(w.boneIndex1==id?w.weight1:0)+(w.boneIndex2==id?w.weight2:0)+(w.boneIndex3==id?w.weight3:0);
                    if(weight>.65f)points.Add(bind.MultiplyPoint3x4(vertices[i]));
                }
            }
            if(points.Count<8)throw new InvalidOperationException("Cannot calibrate hand geometry: "+hand.name);
            Vector3 mean=Vector3.zero;foreach(var point in points)mean+=point;mean/=points.Count;
            var covariance=Matrix4x4.zero;covariance[3,3]=1f;
            foreach(var point in points){var d=point-mean;for(int r=0;r<3;r++)for(int c=0;c<3;c++)covariance[r,c]+=d[r]*d[c]/points.Count;}
            for(int i=0;i<3;i++)covariance[i,i]+=1e-8f;
            Vector3 forward=mean.normalized;for(int i=0;i<20;i++)forward=covariance.MultiplyVector(forward).normalized;if(Vector3.Dot(forward,mean)<0)forward=-forward;
            var inverse=covariance.inverse;Vector3 normal=new Vector3(.37f,.63f,.21f).normalized;for(int i=0;i<20;i++)normal=inverse.MultiplyVector(normal).normalized;
            normal=Vector3.ProjectOnPlane(normal,forward).normalized;if(Vector3.Dot(hand.TransformDirection(normal),cast["Ada"].transform.up)<0)normal=-normal;
            System.IO.File.AppendAllText("ClientExports/YanYana/Reports/intro-hand-basis.txt",hand.name+" weighted vertices="+points.Count+" forward="+forward+" normal="+normal+"; no finger bones in the approved rig.\n");
            return(forward,normal);
        }
        static void IntroObstacle(GameObject obj,Vector3 size,Vector3 center,bool gate=false)
        {
            var collider=obj.AddComponent<BoxCollider>();collider.size=size;collider.center=center;
            obj.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
            var obstacle=obj.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=size;obstacle.center=center;obstacle.carving=true;
            if(gate)obj.layer=29;
        }
        static void CreatePhysicalIntro()
        {
            System.IO.File.WriteAllText("ClientExports/YanYana/Reports/intro-hand-basis.txt","Authored loop blend shapes and mesh-derived contact centers. Native scene nodes solve both arms and roll the forearms without detaching the wrists.\n");
            foreach(string key in new[]{"IntroDone","IntroStage","IntroOrientation","IntroRoute","IntroCourseCleared","IntroEfeWaiting"})InitialPhysical(key);
            InitialPhysical("IntroYaw",180);
            main.Initial("IntroPointer",-999);main.Initial("IntroDragging",false);main.Initial("IntroTurn",0f);main.Initial("IntroDragStart",Vector2.zero);main.Initial("IntroDropStart",Vector3.zero);main.Initial("IntroPlaceProgress",0f);main.Initial("IntroBlocked",false);main.Initial("IntroCuePosition",cast["Efe"].transform.position+Vector3.up*.65f);
            main.Initial("IntroLeftContactError",0f);main.Initial("IntroRightContactError",0f);
            var course=Group("Efe’nin minder geçidi",world.transform);
            var stand=Model("IntroToyStand",course.transform,new Vector3(introSource.x,0,introSource.z),1,270);stand.name="Oyun kutusunun alçak sehpası";IntroObstacle(stand,new Vector3(.86f,.56f,.26f),Vector3.up*.28f);
            foreach(float z in new[]{-3.26f,-2.18f}){var post=Model("IntroCushionStack",course.transform,new Vector3(-.1f,0,z),1,90);post.name="Yumuşak geçit minderi "+z;IntroObstacle(post,new Vector3(.46f,.68f,.24f),Vector3.up*.34f,true);}
            var kit=Model("IntroToyKit",world.transform,introSource,1,270);kit.name="Efe’nin taşınabilir oyun kutusu";physicalItems["IntroKit"]=kit;
            var hit=kit.AddComponent<BoxCollider>();hit.size=new Vector3(.84f,.21f,.35f);
            float tableTop=roomModel.GetComponentsInChildren<Renderer>().First(r=>r.name.StartsWith("PackingBench_Pad",StringComparison.Ordinal)).bounds.max.y;
            var landingBoard=Model("IntroTrayDock",course.transform,new Vector3(introDrop.x,tableTop+.012f,introDrop.z),1,0);landingBoard.name="Oyun masasındaki erişilebilir yerleştirme tahtası";landingBoard.AddComponent<NavMeshModifier>().ignoreFromBuild=true;tableTop+=.024f;
            float kitBottom=kit.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y)-kit.transform.position.y;
            introDrop.y=tableTop+.006f-kitBottom;
            var marker=Shape("Oyun kutusunun masadaki yeri",PrimitiveType.Cube,world.transform,new Vector3(introDrop.x,tableTop+.003f,introDrop.z),new Vector3(.25f,.006f,.75f),mats["YY_teal"]);marker.AddComponent<BoxCollider>().size=new Vector3(1,17,1);
            System.IO.File.WriteAllText("ClientExports/YanYana/Reports/intro-table-contact.txt","Authored geometry: table top="+tableTop.ToString("F5")+" marker thickness=0.00600 kit local bottom="+kitBottom.ToString("F5")+" placed root="+introDrop+" bottom-to-marker error="+Mathf.Abs(introDrop.y+kitBottom-(tableTop+.006f)).ToString("F6")+" m. Arm/finger pose is inspected separately.");
            var pickupTarget=Group("Kutuyu kavrama noktası",interactions.transform,new Vector3(-2.34f,.64f,-2.9f));
            var deliveryWait=Group("Efe’nin oyun kutusunu beklediği yer",interactions.transform,new Vector3(1.25f,0,-2.45f));
            var sourceView=WorkCamera("intro",new Vector3(-2.3f,.65f,-2.9f),1.05f,false);sourceView.transform.position=new Vector3(-2.45f,2.1f,-1.8f);sourceView.transform.LookAt(new Vector3(-2.3f,.65f,-2.9f));
            var dropView=WorkCamera("introDrop",new Vector3(1.72f,1f,-3.12f),1.16f,false);dropView.transform.position=new Vector3(2.65f,2.17f,-4.3f);dropView.transform.LookAt(new Vector3(1.72f,1f,-3.12f));
            var carryFocus=Group("Başlangıçta taşıma odağı",cast["Ada"].transform,new Vector3(0,.55f,0));
            var carryView=WorkCamera("introCarry",carryFocus.transform.position,2.30f,false);carryView.Priority=15;carryView.Follow=carryFocus.transform;carryView.LookAt=carryFocus.transform;
            var follow=carryView.gameObject.AddComponent<CinemachineFollow>();follow.FollowOffset=new Vector3(0,4.8f,-.1f);follow.TrackerSettings.BindingMode=BindingMode.WorldSpace;follow.TrackerSettings.PositionDamping=new Vector3(.1f,.1f,.1f);
            var aim=carryView.gameObject.AddComponent<CinemachineRotationComposer>();aim.Damping=new Vector2(.1f,.1f);
            var g=new YanYanaGraphAuthor(flow,"Bizim mahalle · hafif kutuyu iki farklı yoldan taşı");
            var mover=movers["Ada"];var agent=cast["Ada"].GetComponent<NavMeshAgent>();var animator=cast["Ada"].GetComponentInChildren<Animator>();
            float padding=new SerializedObject(mover).FindProperty("arrivalPadding").floatValue;
            float following=new SerializedObject(follower).FindProperty("followDistance").floatValue;
            var resume=g.Event("ResumeIntro");var p=g.SetVar(resume.trigger,"Busy",false,flow);p=g.SetVar(p,"Workspace","",flow);p=g.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",mover,OneBool,false);p=g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);
            p=g.Set(p,typeof(Transform),"rotation",cast["Ada"].transform,g.Call(typeof(Quaternion),"Euler",null,new[]{typeof(float),typeof(float),typeof(float)},0f,g.Var("IntroYaw",flow),0f).result);p=g.Send(p,flow,"RestoreIntro");p=g.Send(p,flow,"IntroGoal");
            var dropped=g.Branch(p,And(g,IntroAt(g,2),Is(g,g.Var("SavedWorkspace",flow),"introDrop")));g.Send(dropped.ifTrue,flow,"ApproachIntroDrop");
            var picking=g.Branch(dropped.ifFalse,And(g,IntroAt(g,1),Is(g,g.Var("SavedWorkspace",flow),"intro")));g.Send(picking.ifTrue,flow,"ApproachIntroSource");
            var goal=g.Event("IntroGoal");p=Text(g,goal.trigger,chapterText,"BİZİM MAHALLE");p=Text(g,p,gestureText,"Zemine dokunarak yürü");
            var stages=g.Add(new SwitchOnInteger{options=new System.Collections.Generic.List<int>{0,1,2}});g.Bind(stages.selector,g.Var("IntroStage",flow));g.Link(p,stages.enter);
            p=Text(g,stages.branches[0].Value,goalText,"Efe’nin yanına yürü");Text(g,p,lineText,"Efe: Ada! Oyun yerimi hazırlıyorum. Yanıma gelir misin?");
            p=Text(g,stages.branches[1].Value,goalText,"Oyun kutusunu al");Text(g,p,lineText,"Efe: Kutum kapının yanındaki sehpada. Masaya birlikte götürelim.");
            p=Text(g,stages.branches[2].Value,goalText,"Kutuyu oyun masasına götür");p=Text(g,p,gestureText,"Yürü · kutuyu yana sürükleyerek çevir");Text(g,p,lineText,"Efe: Minderlerin arasından mı, geniş taraftan mı geçelim?");
            var hint=g.Event("IntroHint");var hs=g.Add(new SwitchOnInteger{options=new System.Collections.Generic.List<int>{0,1,2}});g.Bind(hs.selector,g.Var("IntroStage",flow));g.Link(hint.trigger,hs.enter);
            Text(g,hs.branches[0].Value,lineText,"Efe: Yanımdaki zemine dokun. Birlikte oyun yerini hazırlayalım.");Text(g,hs.branches[1].Value,lineText,"Ada: Kutuyu tutamaklarından kendime doğru sürüklemeliyim.");Text(g,hs.branches[2].Value,lineText,"Ada: Kutuyu yana sürükleyerek çevir. İstersen minderlerin üst tarafından dolaş.");
            var frame=g.Add(new Unity.VisualScripting.Update());var active=g.Branch(frame.trigger,And(g,IntroActive(g),Is(g,g.Var("Paused",flow),false)));
            var greet=g.Branch(active.ifTrue,And(g,IntroAt(g,0),g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",cast["Ada"].transform),g.Get(typeof(Transform),"position",cast["Efe"].transform)).result,.75f)));
            p=g.Do(greet.ifTrue,typeof(StoryPlayerMovement),"Stop",mover,NoArgs);p=g.SetVar(p,"IntroStage",1,flow);p=g.Send(p,flow,"IntroGoal");g.Send(p,flow,"CommitCheckpoint");
            var walk=g.Branch(greet.ifFalse,And(g,IntroAt(g,2),Is(g,g.Var("IntroDragging",flow),false)));
            var velocity=g.Get(typeof(NavMeshAgent),"velocity",agent);var moving=g.Branch(walk.ifTrue,g.Binary<Greater>(g.Get(typeof(Vector3),"sqrMagnitude",velocity),.005f));
            var next=Add(g,IntroHeldPoint(g),Mul(g,g.Get(typeof(Vector3),"normalized",velocity),.18f));var collision=g.Branch(moving.ifTrue,IntroCollision(g,next,IntroHeldRotation(g)));
            p=g.Do(collision.ifTrue,typeof(StoryPlayerMovement),"Stop",mover,NoArgs);p=g.SetVar(p,"IntroBlocked",true,flow);Text(g,p,lineText,"Ada: Kutu sığmıyor. Çevirebilir veya geniş taraftan geçebilirim.");
            var crossTick=g.Add(new Unity.VisualScripting.Update());var crossing=g.Branch(crossTick.trigger,And(g,And(g,IntroActive(g),IntroAt(g,2)),And(g,Is(g,g.Var("IntroRoute",flow),0),g.Binary<Greater>(g.Get(typeof(Vector3),"x",g.Get(typeof(Transform),"position",cast["Ada"].transform)),.6f))));
            var narrow=g.Branch(crossing.ifTrue,g.Binary<Less>(g.Get(typeof(Vector3),"z",g.Get(typeof(Transform),"position",cast["Ada"].transform)),-2.3f));p=g.SetVar(narrow.ifTrue,"IntroRoute",1,flow);g.Send(p,flow,"CommitCheckpoint");p=g.SetVar(narrow.ifFalse,"IntroRoute",2,flow);g.Send(p,flow,"CommitCheckpoint");
            var source=g.Event("ApproachIntroSource",true);var allowed=g.Branch(source.trigger,And(g,IntroFree(g),IntroAt(g,1)));p=g.SetVar(allowed.ifTrue,"Busy",true,flow);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",agent,.025f);p=g.Set(p,typeof(StoryPlayerMovement),"arrivalPadding",mover,.005f);p=WalkAndWait(g,p,mover,introPickup,.075f);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",agent,.1f);p=g.Set(p,typeof(StoryPlayerMovement),"arrivalPadding",mover,padding);p=g.Do(p,typeof(StoryPlayerMovement),"FaceTowards",mover,new[]{typeof(Vector3)},introSource);
            g.Send(p,flow,"OpenWork","intro","Oyun kutusunu kavra","Ada: Hafifmiş. İki tutamağından kavrayabilirim.");
            var drop=g.Event("ApproachIntroDrop",true);allowed=g.Branch(drop.trigger,And(g,IntroFree(g),IntroAt(g,2)));p=g.SetVar(allowed.ifTrue,"Busy",true,flow);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",agent,.025f);p=g.Set(p,typeof(StoryPlayerMovement),"arrivalPadding",mover,.005f);p=WalkAndWait(g,p,mover,introDestination,.075f);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",agent,.1f);p=g.Set(p,typeof(StoryPlayerMovement),"arrivalPadding",mover,padding);p=g.Do(p,typeof(StoryPlayerMovement),"FaceTowards",mover,new[]{typeof(Vector3)},introDrop);
            g.Send(p,flow,"OpenWork","introDrop","Kutuyu masadaki yere hizala","Efe: Ağaç işaretli yer! Kutuyu oraya bırakalım.");
            var collect=g.Event("IntroPicked");p=g.SetVar(collect.trigger,"IntroStage",2,flow);p=g.SetVar(p,"Workspace","",flow);p=g.SetVar(p,"IntroDragging",false,flow);p=g.SetVar(p,"IntroPointer",-999,flow);p=g.Active(p,sourceView.gameObject,false);p=g.Active(p,activityBack,false);p=g.SetVar(p,"Busy",true,flow);p=g.Send(p,flow,"IntroGoal");p=g.Send(p,flow,"RestoreIntro");g.Send(p,flow,"IntroReturnCamera");
            var blend=g.Event("IntroReturnCamera",true);p=g.Wait(blend.trigger,.85f);p=Released(g,p);p=g.Do(p,typeof(StoryPlayerMovement),"SetStoryInputLocked",mover,OneBool,false);p=g.SetVar(p,"Busy",false,flow);g.Send(p,flow,"CommitCheckpoint");
            var place=g.Event("IntroPlace");p=g.SetVar(place.trigger,"IntroStage",3,flow);p=g.SetVar(p,"IntroPlaceProgress",0f,flow);p=g.SetVar(p,"IntroDropStart",g.Get(typeof(Transform),"position",kit.transform),flow);p=g.SetVar(p,"Busy",true,flow);p=g.SetVar(p,"IntroDragging",false,flow);Text(g,p,gestureText,"Ada kutuyu masaya yerleştiriyor");
            var placeFrame=g.Add(new Unity.VisualScripting.Update());var progress=g.Branch(placeFrame.trigger,And(g,IntroAt(g,3),And(g,IntroActive(g),Is(g,g.Var("Paused",flow),false))));p=g.SetVar(progress.ifTrue,"IntroPlaceProgress",Sum(g,g.Var("IntroPlaceProgress",flow),g.Get(typeof(Time),"deltaTime")),flow);var complete=g.Branch(p,g.Binary<GreaterOrEqual>(g.Var("IntroPlaceProgress",flow),.85f));
            p=g.SetVar(complete.ifTrue,"IntroStage",4,flow);p=g.SetVar(p,"IntroDone",1,flow);p=RestoreEfeFollowing(g,p,following);p=g.Send(p,flow,"Explore");Text(g,p,lineText,"Efe: Oyun yerimiz hazır! Köprüyü hazırlıklardan sonra kurarız.");
            // Efe walks to the destination himself and waits beside, rather than covering the work surface.
            var waiting=g.Event("SetIntroEfe");var carry=g.Branch(waiting.trigger,And(g,IntroActive(g),IntroAt(g,2)));var settled=g.Branch(carry.ifTrue,Is(g,g.Var("IntroEfeWaiting",flow),1));g.Do(settled.ifTrue,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);
            p=g.Do(settled.ifFalse,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);p=g.Set(p,typeof(StorySiblingFollower),"target",follower,deliveryWait.transform);p=g.Set(p,typeof(StorySiblingFollower),"targetMovement",follower,null);p=g.Set(p,typeof(StorySiblingFollower),"followDistance",follower,.1f);p=g.Set(p,typeof(NavMeshAgent),"stoppingDistance",cast["Efe"].GetComponent<NavMeshAgent>(),.1f);g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,true);
            var efeFrame=g.Add(new Unity.VisualScripting.Update());var efeArrived=g.Branch(efeFrame.trigger,And(g,And(g,IntroActive(g),IntroAt(g,2)),And(g,Is(g,g.Var("IntroEfeWaiting",flow),0),g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",cast["Efe"].transform),deliveryWait.transform.position).result,.18f))));p=g.SetVar(efeArrived.ifTrue,"IntroEfeWaiting",1,flow);p=g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);g.Do(p,typeof(StorySiblingFollower),"FaceTowards",follower,new[]{typeof(Vector3)},introDestination);
            var late=g.Add(new Unity.VisualScripting.LateUpdate());p=g.Active(late.trigger,carryView.gameObject,And(g,IntroActive(g),Is(g,g.Var("Workspace",flow),"")));p=g.Active(p,course,And(g,Is(g,g.Var("Phase",flow),0),Is(g,g.Var("IntroCourseCleared",flow),0)));p=g.Active(p,marker,IntroActive(g));p=g.Active(p,kit,And(g,Is(g,g.Var("Phase",flow),0),Is(g,g.Var("IntroCourseCleared",flow),0)));
            var inIntro=g.Branch(p,IntroActive(g));p=g.Active(inIntro.ifTrue,activityBack,false);var held=g.Branch(p,IntroAt(g,2));p=g.Set(held.ifTrue,typeof(Transform),"position",kit.transform,IntroHeldPoint(g));p=g.Set(p,typeof(Transform),"rotation",kit.transform,IntroHeldRotation(g));
            foreach(bool right in new[]{false,true})
            {
                var target=g.Call(typeof(Transform),"TransformPoint",cast["Ada"].transform,new[]{typeof(Vector3)},V3(g,g.Binary<ScalarMultiply>(Sum(g,.14f,g.Binary<ScalarMultiply>(g.Var("IntroTurn",flow),-.06f)),right?1f:-1f),IntroHeight(g,.69f),IntroDepth(g,.18f))).result;
                p=SolveIntroArm(g,p,animator,target,right);p=g.SetVar(p,right?"IntroRightContactError":"IntroLeftContactError",g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},HandContactPoint(g,"Ada",right,"Loop"),target).result,flow);
            }
            var lowering=g.Branch(held.ifFalse,IntroAt(g,3));var ratio=g.Call(typeof(Mathf),"Clamp01",null,OneFloat,g.Binary<ScalarDivide>(g.Var("IntroPlaceProgress",flow),.85f)).result;
            p=g.Set(lowering.ifTrue,typeof(Transform),"position",kit.transform,g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Var("IntroDropStart",flow),introDrop,ratio).result);
            foreach(bool right in new[]{false,true})p=SolveIntroArm(g,p,animator,g.Call(typeof(Transform),"TransformPoint",kit.transform,new[]{typeof(Vector3)},new Vector3(right?.14f:-.14f,.04f,-.20f)).result,right);
            var reaching=g.Branch(lowering.ifFalse,And(g,IntroAt(g,1),Is(g,g.Var("Workspace",flow),"intro")));p=reaching.ifTrue;
            foreach(bool right in new[]{false,true})p=SolveIntroArm(g,p,animator,g.Call(typeof(Transform),"TransformPoint",kit.transform,new[]{typeof(Vector3)},new Vector3(right?.14f:-.14f,.04f,-.20f)).result,right);
            var cueFrame=g.Add(new Unity.VisualScripting.Update());var cueStage=g.Add(new SwitchOnInteger{options=new System.Collections.Generic.List<int>{0,1,2}});g.Bind(cueStage.selector,g.Var("IntroStage",flow));g.Link(cueFrame.trigger,cueStage.enter);
            g.SetVar(cueStage.branches[0].Value,"IntroCuePosition",Add(g,g.Get(typeof(Transform),"position",cast["Efe"].transform),Vector3.up*.7f),flow);g.SetVar(cueStage.branches[1].Value,"IntroCuePosition",introSource,flow);g.SetVar(cueStage.branches[2].Value,"IntroCuePosition",introDrop,flow);
            var restore=g.Event("RestoreIntro");p=g.SetVar(restore.trigger,"IntroDragging",false,flow);p=g.SetVar(p,"IntroPointer",-999,flow);p=g.SetVar(p,"IntroTurn",g.Call(typeof(Convert),"ToSingle",null,new[]{typeof(int)},g.Var("IntroOrientation",flow)).result,flow);
            var interrupted=g.Branch(p,IntroAt(g,3));p=g.SetVar(interrupted.ifTrue,"IntroStage",2,flow);g.SetVar(p,"Busy",false,flow);
            var redraw=g.Event("RestoreIntro");var packed=g.Branch(redraw.trigger,Is(g,g.Var("IntroDone",flow),1));p=g.Set(packed.ifTrue,typeof(Transform),"position",kit.transform,introDrop);g.Set(p,typeof(Transform),"rotation",kit.transform,Quaternion.Euler(0,90,0));
            var onStand=g.Branch(packed.ifFalse,g.Binary<Less>(g.Var("IntroStage",flow),2));p=g.Set(onStand.ifTrue,typeof(Transform),"position",kit.transform,introSource);p=g.Set(p,typeof(Transform),"rotation",kit.transform,Quaternion.Euler(0,270,0));g.Do(p,typeof(StorySiblingFollower),"SetFollowing",follower,OneBool,false);g.Send(onStand.ifFalse,flow,"SetIntroEfe");
            main.Send(main.Event("RestorePhysicalObjects").trigger,flow,"RestoreIntro");var cleared=main.Event("ToyBridge",true);p=main.Wait(cleared.trigger,1f);p=main.SetVar(p,"IntroCourseCleared",1);main.Send(p,flow,"CommitCheckpoint");
            var migration=main.Event("MigrateIntroSave");var legacy=main.Branch(migration.trigger,And(main,main.Call(typeof(PlayerPrefs),"HasKey",null,OneString,SavePrefix+"physical.Phase").result,Is(main,main.Call(typeof(PlayerPrefs),"HasKey",null,OneString,SavePrefix+"physical.IntroStage").result,false)));p=main.SetVar(legacy.ifTrue,"IntroDone",1);main.SetVar(p,"IntroStage",4);
            g.Dirty();
            CreateIntroPointerGraph(kit,pickupTarget.transform.position);
            var mg=new YanYanaGraphAuthor(marker,"Yaklaşıp oyun kutusunu masaya yerleştir");var click=mg.Add(new OnPointerClick());mg.Bind(click.target,marker);var ok=mg.Branch(click.trigger,And(mg,IntroFree(mg),IntroAt(mg,2)));mg.Do(ok.ifTrue,typeof(StoryPlayerMovement),"TrySetDestination",mover,new[]{typeof(Vector3)},introDestination);
            var arrival=mg.Add(new Unity.VisualScripting.Update());var nearDesk=mg.Branch(arrival.trigger,And(mg,And(mg,IntroFree(mg),IntroAt(mg,2)),mg.Binary<Less>(mg.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},mg.Get(typeof(Transform),"position",cast["Ada"].transform),introDestination).result,.23f)));mg.Send(nearDesk.ifTrue,flow,"ApproachIntroDrop");mg.Dirty();
            foreach(var floor in roomModel.GetComponentsInChildren<Transform>().Where(t=>t.name=="COLLIDER_Floor"))
            {
                var fg=new YanYanaGraphAuthor(floor.gameObject,"Oyun kutusunu taşırken zemine dokunarak yürü");var tap=fg.Add(new OnPointerClick());fg.Bind(tap.target,floor.gameObject);var free=fg.Branch(tap.trigger,And(fg,IntroFree(fg),Is(fg,fg.Var("IntroDragging",flow),false)));var ray=fg.Get(typeof(PointerEventData),"pointerCurrentRaycast",tap.data);p=fg.SetVar(free.ifTrue,"IntroBlocked",false,flow);fg.Do(p,typeof(StoryPlayerMovement),"TrySetDestination",mover,new[]{typeof(Vector3)},fg.Get(typeof(RaycastResult),"worldPosition",ray));fg.Dirty();
            }
        }
        static void CreateIntroPointerGraph(GameObject kit,Vector3 pickupTarget)
        {
            var g=new YanYanaGraphAuthor(kit,"Kutuyu kavra, sürükleyerek çevir ve masaya hizala");
            var click=g.Add(new OnPointerClick());g.Bind(click.target,kit);var approach=g.Branch(click.trigger,And(g,IntroFree(g),IntroAt(g,1)));g.Send(approach.ifTrue,flow,"ApproachIntroSource");
            var down=g.Add(new OnPointerDown());g.Bind(down.target,kit);var allowed=g.Branch(down.trigger,And(g,IntroActive(g),And(g,Available(g),And(g,Is(g,g.Var("IntroDragging",flow),false),Or(g,IntroAt(g,2),AtWork(g,"intro"))))));
            var p=g.SetVar(allowed.ifTrue,"IntroDragging",true,flow);p=g.SetVar(p,"IntroPointer",g.Get(typeof(PointerEventData),"pointerId",down.data),flow);p=g.SetVar(p,"IntroDragStart",g.Get(typeof(PointerEventData),"position",down.data),flow);g.Do(p,typeof(StoryPlayerMovement),"Stop",movers["Ada"],NoArgs);
            var drag=g.Add(new OnDrag());g.Bind(drag.target,kit);var owned=And(g,g.Var("IntroDragging",flow),Is(g,g.Var("IntroPointer",flow),g.Get(typeof(PointerEventData),"pointerId",drag.data)));var dragging=g.Branch(drag.trigger,And(g,owned,Available(g)));var pick=g.Branch(dragging.ifTrue,IntroAt(g,1));var plane=PointerOnPlane(g,pick.ifTrue,drag.data,.64f);g.Set(plane.path,typeof(Transform),"position",kit.transform,plane.point);
            var delta=g.Binary<Vector2Subtract>(g.Get(typeof(PointerEventData),"position",drag.data),g.Var("IntroDragStart",flow));var amount=g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},g.Binary<ScalarDivide>(g.Get(typeof(Vector2),"x",delta),g.Binary<ScalarMultiply>(g.Get(typeof(Screen),"width"),.22f)),-1f,1f).result;
            g.SetVar(pick.ifFalse,"IntroTurn",g.Call(typeof(Mathf),"Clamp01",null,OneFloat,Sum(g,g.Var("IntroOrientation",flow),amount)).result,flow);
            var end=g.Add(new OnEndDrag());g.Bind(end.target,kit);var ended=g.Branch(end.trigger,And(g,And(g,Available(g),g.Var("IntroDragging",flow)),Is(g,g.Var("IntroPointer",flow),g.Get(typeof(PointerEventData),"pointerId",end.data))));p=g.SetVar(ended.ifTrue,"IntroDragging",false,flow);p=g.SetVar(p,"IntroPointer",-999,flow);
            var picked=g.Branch(p,IntroAt(g,1));var near=g.Branch(picked.ifTrue,g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",kit.transform),pickupTarget).result,.12f));g.Send(near.ifTrue,flow,"IntroPicked");p=g.Set(near.ifFalse,typeof(Transform),"position",kit.transform,introSource);Text(g,p,lineText,"Ada: Kutuyu ellerime doğru yaklaştırmalıyım.");
            var deltaEnd=g.Binary<Vector2Subtract>(g.Get(typeof(PointerEventData),"position",end.data),g.Var("IntroDragStart",flow));var horizontal=g.Call(typeof(Mathf),"Abs",null,OneFloat,g.Get(typeof(Vector2),"x",deltaEnd)).result;var vertical=g.Get(typeof(Vector2),"y",deltaEnd);
            var drop=g.Branch(picked.ifFalse,And(g,AtWork(g,"introDrop"),And(g,g.Binary<Less>(vertical,g.Binary<ScalarMultiply>(g.Get(typeof(Screen),"width"),-.10f)),g.Binary<Greater>(g.Call(typeof(Mathf),"Abs",null,OneFloat,vertical).result,horizontal))));
            var aligned=g.Branch(drop.ifTrue,Is(g,g.Var("IntroOrientation",flow),0));g.Send(aligned.ifTrue,flow,"IntroPlace");p=g.SetVar(aligned.ifFalse,"IntroTurn",1f,flow);Text(g,p,lineText,"Efe: Masadaki şekle uymadı. Kutuyu yana çevir.");
            var blocked=g.Branch(drop.ifFalse,IntroCollision(g,IntroHeldPoint(g),IntroHeldRotation(g)));p=g.SetVar(blocked.ifTrue,"IntroTurn",g.Call(typeof(Convert),"ToSingle",null,new[]{typeof(int)},g.Var("IntroOrientation",flow)).result,flow);Text(g,p,lineText,"Ada: Çevirmek için biraz daha boş yer bulmalıyım.");
            var turned=g.Branch(blocked.ifFalse,g.Binary<GreaterOrEqual>(g.Var("IntroTurn",flow),.5f));p=g.SetVar(turned.ifTrue,"IntroOrientation",1,flow);p=g.SetVar(p,"IntroTurn",1f,flow);g.Send(p,flow,"CommitCheckpoint");p=g.SetVar(turned.ifFalse,"IntroOrientation",0,flow);p=g.SetVar(p,"IntroTurn",0f,flow);g.Send(p,flow,"CommitCheckpoint");
            var up=g.Add(new OnPointerUp());g.Bind(up.target,kit);var cancel=g.Branch(up.trigger,And(g,Is(g,g.Get(typeof(PointerEventData),"dragging",up.data),false),Is(g,g.Var("IntroPointer",flow),g.Get(typeof(PointerEventData),"pointerId",up.data))));p=g.SetVar(cancel.ifTrue,"IntroDragging",false,flow);p=g.SetVar(p,"IntroPointer",-999,flow);g.SetVar(p,"IntroTurn",g.Call(typeof(Convert),"ToSingle",null,new[]{typeof(int)},g.Var("IntroOrientation",flow)).result,flow);g.Dirty();
        }
    }
}
