// Editor-only scene authoring. All behavior is serialized into native Visual Scripting
// graphs and uses the existing movement, save, pause and dialogue managers.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        const float ParkGround = -.48f;
        static GameObject neighborhoodPark, parkChild, parkMother, parkSteward, gasSteward;
        static readonly List<GameObject> parkResidents = new List<GameObject>();

        static void CreateNeighborhoodPark()
        {
            neighborhoodPark = Group("Mahalle Parkı · buluşma ve dayanışma", world.transform);
            var p = neighborhoodPark.transform;
            Mat("ParkGrass", "#91AD65"); Mat("ParkGrassDark", "#6D8D55");
            Mat("ParkPath", "#E3CFAA"); Mat("ParkBorder", "#E7DCC7");
            Mat("ParkCoral", "#CE7961"); Mat("ParkFlower", "#E8B957");
            Mat("ParkSign", "#286C58"); Mat("ParkLeaf", "#6F9257");
            Mat("ParkLeafLight", "#A2B872");
            // The old FBX placed trees directly in front of aid and reunion targets.
            foreach(var t in neighborhood.GetComponentsInChildren<Transform>())
                if(t.GetComponent<Renderer>()&&(t.name.StartsWith("Agac")||t.name.StartsWith("YuvarlakTac"))&&t.position.z<-20f)t.gameObject.SetActive(false);
            Shape("Park ve arka mahalle peyzajı",PrimitiveType.Cube,p,new Vector3(0,-.64f,-37.2f),new Vector3(55,.30f,12),mats["ParkGrass"]);
            Shape("Arka mahallenin yaya yolu",PrimitiveType.Cube,p,new Vector3(0,-.474f,-37.3f),new Vector3(50,.022f,3.0f),mats["ParkPath"]);
            Shape("Parkın yeşil tabanı", PrimitiveType.Cube, p, new Vector3(0,-.62f,-28), new Vector3(29,.25f,15), mats["ParkGrass"]);
            Shape("COLLIDER_Baglanti_ParkGezinti", PrimitiveType.Cube, p, new Vector3(0,-.60f,-29.9f), new Vector3(26,.24f,5.8f), mats["ParkPath"], true);
            Shape("Parkın açık buluşma meydanı", PrimitiveType.Cube, p, new Vector3(.2f,-.447f,-25.5f), new Vector3(24,.022f,6.9f), mats["ParkPath"]);
            // Keep both existing reunion destinations and the central circulation clear.
            foreach (float x in new[]{-7f,8f})
            {
                Shape("Buluşma meydanı taş çember", PrimitiveType.Cylinder, p, new Vector3(x,-.428f,x<0?-26:-25), new Vector3(4.7f,.012f,4.7f), mats["ParkBorder"]);
                Shape("Açık buluşma merkezi", PrimitiveType.Cylinder, p, new Vector3(x,-.413f,x<0?-26:-25), new Vector3(4.25f,.008f,4.25f), mats["ParkPath"]);
                AssemblySign(p,new Vector3(x<0?-10.2f:10.5f,ParkGround,-26.7f));
            }
            foreach (var bed in new[]{new Vector3(-12.1f,0,-25.1f),new Vector3(12.0f,0,-27.4f),new Vector3(-7.5f,0,-31.9f),new Vector3(7.6f,0,-31.9f)})
            {
                var b = Group("Çiçekli park adası",p,bed+Vector3.up*ParkGround);
                Shape("Yuvarlak taş bordür",PrimitiveType.Cylinder,b.transform,new Vector3(0,.07f,0),new Vector3(3.0f,.11f,3.8f),mats["ParkBorder"]);
                Shape("Çim",PrimitiveType.Cylinder,b.transform,new Vector3(0,.18f,0),new Vector3(2.75f,.015f,3.55f),mats["ParkGrassDark"]);
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/4;var at=new Vector3(Mathf.Cos(a)*1.14f,.27f,Mathf.Sin(a)*1.42f);
                    Shape("Yaprak kümesi",PrimitiveType.Sphere,b.transform,at,new Vector3(.42f,.24f,.42f),mats["ParkLeaf"]);
                    Shape("Park çiçeği",PrimitiveType.Sphere,b.transform,at+Vector3.up*.16f,new Vector3(.15f,.10f,.15f),mats[i%2==0?"ParkCoral":"ParkFlower"]);
                }
            }
            foreach(var at in new[]{new Vector3(-13.2f,0,-21.4f),new Vector3(13.1f,0,-23),new Vector3(-12.5f,0,-31.7f),new Vector3(-6.5f,0,-34.1f),new Vector3(0,0,-34.5f),new Vector3(6.6f,0,-34.0f),new Vector3(12.7f,0,-32.3f)})
                ParkTree(p,at+Vector3.up*ParkGround);
            foreach(var spec in new[]{new Vector3(-10.8f,90,-27.7f),new Vector3(-8.4f,0,-29.5f),new Vector3(8.3f,0,-29.5f),new Vector3(10.8f,-90,-22.6f)})
            {
                var bench=Model("Bench",p,new Vector3(spec.x,ParkGround,spec.z),1,spec.y);bench.name="Park bankı · açık geçişin kenarı";
                var obstacle=bench.AddComponent<BoxCollider>();obstacle.center=new Vector3(0,.38f,0);obstacle.size=new Vector3(1.9f,.76f,.72f);
            }
            ParkPlayground(p,new Vector3(0,ParkGround,-30.9f));
            // Low fixtures stay outside the family reunion circles and wheelchair-width route.
            foreach(float x in new[]{-11.0f,11.0f})
            {
                var bin=Group("Kapaklı park çöp kutusu",p,new Vector3(x,ParkGround,-28.6f));
                Shape("Gövde",PrimitiveType.Cylinder,bin.transform,new Vector3(0,.38f,0),new Vector3(.46f,.38f,.46f),mats["YY_tealDark"]);
                Shape("Kapak",PrimitiveType.Cylinder,bin.transform,new Vector3(0,.78f,0),new Vector3(.53f,.04f,.53f),mats["YY_wood"]);
            }
            var desk=Group("Park danışma noktası · aileyi birlikte bul",p,new Vector3(-10.0f,ParkGround,-22.8f));
            Model("Table",desk.transform,Vector3.zero,.9f,0);
            Model("FamilyCard",desk.transform,new Vector3(-.25f,.72f,0),1,25);
            Model("Radio",desk.transform,new Vector3(.25f,.72f,0),.8f,0);
            ParkBoard(p,"AİLE DANIŞMA",new Vector3(-10.0f,ParkGround,-23.6f),mats["ParkSign"]);
            ParkBoard(p,"MAHALLE PARKI",new Vector3(.2f,ParkGround,-32.5f),mats["YY_tealDark"]);
            ParkBoard(p,"SU VE DİNLENME",new Vector3(10.4f,ParkGround,-29.7f),mats["YY_tealDark"]);
            for(int i=0;i<4;i++)Model("Water",p,new Vector3(9.5f+i*.23f,ParkGround+.04f,-28.8f),.9f,0);
            for(int i=0;i<5;i++)
            {
                Shape("Seksek taşı "+(i+1),PrimitiveType.Cube,p,new Vector3(-2.4f+(i%2)*.55f,-.452f,-29.4f-i*.48f),new Vector3(.46f,.01f,.40f),mats[i%2==0?"ParkCoral":"ParkFlower"]);
            }
        }

        static void ParkTree(Transform parent,Vector3 at)
        {
            var tree=Group("Park ağacı · açık meydanın dışında",parent,at);
            Shape("Gövde",PrimitiveType.Cylinder,tree.transform,new Vector3(0,1.05f,0),new Vector3(.27f,1.05f,.27f),mats["YY_wood"]);
            for(int i=0;i<4;i++)Shape("Yuvarlak ağaç tacı",PrimitiveType.Sphere,tree.transform,new Vector3(Mathf.Cos(i*2.1f)*.47f,2.65f+(i%2)*.35f,Mathf.Sin(i*2.1f)*.47f),new Vector3(1.9f,1.65f,1.8f),mats[i%2==0?"ParkLeaf":"ParkLeafLight"]);
            Shape("Ağaç dibi",PrimitiveType.Cylinder,tree.transform,new Vector3(0,.03f,0),new Vector3(1.05f,.03f,1.05f),mats["Soil"]);
        }

        static void ParkPlayground(Transform parent,Vector3 at)
        {
            var play=Group("Çocuk parkı · kaydırak ve oyun köşesi",parent,at);
            Shape("Yumuşak oyun zemini",PrimitiveType.Cylinder,play.transform,new Vector3(0,.035f,0),new Vector3(4.3f,.025f,3.0f),mats["ParkCoral"]);
            var tower=Group("Ahşap kaydırak",play.transform,new Vector3(.7f,0,-.3f));
            foreach(float x in new[]{-.45f,.45f})foreach(float z in new[]{-.4f,.4f})Shape("Kaydırak direği",PrimitiveType.Cylinder,tower.transform,new Vector3(x,.75f,z),new Vector3(.10f,.75f,.10f),mats["YY_wood"]);
            Shape("Kaydırak sahanlığı",PrimitiveType.Cube,tower.transform,new Vector3(0,.95f,0),new Vector3(1.05f,.12f,1.0f),mats["YY_teal"]);
            for(int i=0;i<4;i++)Shape("Merdiven basamağı",PrimitiveType.Cube,tower.transform,new Vector3(0,.20f+i*.24f,-.53f),new Vector3(.72f,.08f,.18f),mats["ParkFlower"]);
            var slide=Shape("Kaydırak yüzeyi",PrimitiveType.Cube,tower.transform,new Vector3(0,.52f,1.02f),new Vector3(.68f,.08f,1.72f),mats["ParkFlower"]);slide.transform.localRotation=Quaternion.Euler(31,0,0);
            foreach(float x in new[]{-.36f,.36f}){var side=Shape("Kaydırak kenarı",PrimitiveType.Cube,tower.transform,new Vector3(x,.62f,1.02f),new Vector3(.08f,.21f,1.72f),mats["YY_teal"]);side.transform.localRotation=slide.transform.localRotation;}
            Shape("Gölgelik çatı",PrimitiveType.Cube,tower.transform,new Vector3(0,1.62f,0),new Vector3(1.18f,.10f,1.14f),mats["YY_teal"]);
            var obstacle=play.AddComponent<BoxCollider>();obstacle.center=new Vector3(.6f,.7f,.3f);obstacle.size=new Vector3(1.4f,1.8f,2.7f);
            Model("ToyBox",play.transform,new Vector3(-1.1f,.02f,-.5f),.8f,-15);
            Model("ComfortFox",play.transform,new Vector3(-1.1f,.38f,-.5f),.8f,0);
            var ball=Shape("Birlikte oynanan top",PrimitiveType.Sphere,play.transform,new Vector3(-.8f,.24f,.9f),Vector3.one*.39f,mats["ParkFlower"]);
            Shape("Topun renkli şeridi",PrimitiveType.Cylinder,ball.transform,Vector3.zero,new Vector3(1.02f,.1f,1.02f),mats["YY_teal"]);
        }

        static void ParkBoard(Transform parent,string title,Vector3 at,Material material)
        {
            var sign=Group(title+" · park tabelası",parent,at);
            Shape("Yuvarlatılmış levha",PrimitiveType.Cube,sign.transform,new Vector3(0,1.38f,0),new Vector3(1.9f,.58f,.12f),material);
            foreach(float x in new[]{-.71f,.71f})Shape("Tabela direği",PrimitiveType.Cylinder,sign.transform,new Vector3(x,.65f,0),new Vector3(.065f,.65f,.065f),mats["YY_wood"]);
            foreach(int side in new[]{-1,1})
            {
                var text=WorldText(title,sign.transform,new Vector3(0,1.38f,side*.071f),.15f,Paper);text.rectTransform.sizeDelta=new Vector2(1.75f,.48f);text.transform.localRotation=Quaternion.Euler(0,side<0?0:180,0);
            }
        }

        static void AssemblySign(Transform parent,Vector3 at)
        {
            var root=Group("Toplanma alanı · dört ok ve aile simgesi",parent,at);
            Shape("Yeşil alan levhası",PrimitiveType.Cube,root.transform,new Vector3(0,1.55f,0),new Vector3(1.05f,.96f,.10f),mats["ParkSign"]);
            Shape("Direk",PrimitiveType.Cylinder,root.transform,new Vector3(0,.65f,0),new Vector3(.075f,.65f,.075f),mats["YY_wood"]);
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<3;i++)
                {
                    float x=(i-1)*.18f;
                    Shape("Aile başı",PrimitiveType.Sphere,root.transform,new Vector3(x,1.62f,side*.066f),Vector3.one*.09f,mats["YY_cream"]);
                    Shape("Aile gövdesi",PrimitiveType.Capsule,root.transform,new Vector3(x,1.48f,side*.066f),new Vector3(.09f,.09f,.04f),mats["YY_cream"]);
                }
                foreach(float x in new[]{-.35f,.35f})foreach(float y in new[]{1.27f,1.84f})
                {
                    var arrow=Shape("İçe bakan ok",PrimitiveType.Cube,root.transform,new Vector3(x,y,side*.066f),new Vector3(.19f,.04f,.025f),mats["YY_cream"]);arrow.transform.localRotation=Quaternion.Euler(0,0,x*(y-1.55f)>0?45:-45);
                }
            }
        }

        static GameObject ParkPerson(string name,string source,Vector3 at,float yaw=180,float scale=1,bool moving=false)
        {
            var workshop=YanYanaResidentWorkshop.Prefab(name);
            var prefab=workshop??AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterVariants.Root+"/"+source+".prefab")??AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root+"/"+source+".prefab");
            if(!prefab)throw new InvalidOperationException("Missing approved resident: "+source);
            GameObject actor;
            if(workshop)
            {
                actor=Group("Park sakini · "+name,castRoot.transform);
                PrefabUtility.InstantiatePrefab(prefab,actor.transform);
                scale=1;source=YanYanaResidentWorkshop.IsChild(YanYanaResidentWorkshop.Identity(name))?"Efe":name;
            }
            else {actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab,castRoot.transform);actor.name="Park sakini · "+name;}
            YanYanaCharacterStyle.NormalizeRoot(actor);actor.transform.position=at;actor.transform.rotation=Quaternion.Euler(0,yaw,0);actor.transform.localScale=Vector3.one*scale;YanYanaCharacterStyle.NormalizeRoot(actor);
            var anim=actor.GetComponentInChildren<Animator>();anim.runtimeAnimatorController=YanYanaAdultLocomotion.ForCharacter(source,actorController);anim.Rebind();anim.Update(0);GroundAnimatedVisual(actor);anim.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            if(moving)
            {
                var agent=actor.AddComponent<NavMeshAgent>();agent.radius=.23f;agent.height=1.72f;agent.speed=1.2f;agent.acceleration=5;agent.stoppingDistance=.15f;
                var mover=actor.AddComponent<StoryPlayerMovement>();Serialized(mover,"animator",anim);
            }
            parkResidents.Add(actor);return actor;
        }

        static void CreateParkResidents()
        {
            parkResidents.Clear();
            // Families face one another, with room around all story targets.
            var adults=new[]{("Deniz","Deniz",-10.7f,-24.7f,130f),("Aslı","Asli",-10.0f,-25.5f,-40f),("Ozan","Ozan",9.6f,-24.0f,230f),("Selma","Selma",8.9f,-23.2f,50f),("Mert","Mert",6.2f,-28.3f,240f),("Gül","Gul",5.3f,-28.8f,45f),("Cem","Deniz",-9.2f,-29.3f,100f),("Nermin","Asli",-5.2f,-29.5f,270f),("Orhan","Mert",10.7f,-21.4f,180f),("Suna","Gul",-11.6f,-22.0f,90f)};
            foreach(var a in adults)ParkPerson(a.Item1,a.Item2,new Vector3(a.Item3,ParkGround,a.Item4),a.Item5);
            ParkPerson("Kemal dede","Yusuf",new Vector3(7.4f,ParkGround,-28.8f),230,.97f);
            ParkPerson("Ali dede","Yusuf",new Vector3(-8.1f,ParkGround,-29.1f),95,1.02f);
            var children=new[]{("Mina","Ada",-9.7f,-24.3f,110f,.84f),("Can","Efe",9.1f,-24.3f,10f,.91f),("Elif","Ada",5.7f,-28.1f,170f,.9f),("Arda","Efe",-.6f,-28.8f,140f,.88f),("Lale","Ada",-1.7f,-30.0f,60f,.82f),("Umut","Efe",-8.7f,-28.8f,10f,1.04f)};
            foreach(var c in children)
            {
                var child=ParkPerson(c.Item1,c.Item2,new Vector3(c.Item3,ParkGround,c.Item4),c.Item5,c.Item6);
                // Original clothing and faces stay intact; accessories provide silhouettes.
                if(c.Item2=="Efe")Model("BackpackClosed",child.transform,new Vector3(0,.68f,-.19f),.55f,0);
            }
            parkChild=ParkPerson("Ece · ailesini bekleyen çocuk","Ada",new Vector3(-8.9f,ParkGround,-24.0f),90,.82f);
            parkMother=ParkPerson("Aylin · Ece’nin annesi","Selma",new Vector3(-11.0f,ParkGround,-27.4f),15,1,true);
            parkSteward=ParkPerson("Zeynep · aile danışma görevlisi","Bora",new Vector3(-10.0f,ParkGround,-22.0f),-90,.97f);
            gasSteward=ParkPerson("Eren · sokak güvenlik görevlisi","Bora",new Vector3(-1.1f,ParkGround,-13.4f),-65,1.02f);
            foreach(var actor in new[]{parkChild,parkSteward,gasSteward})
            {var hit=actor.AddComponent<CapsuleCollider>();hit.radius=.3f;hit.height=actor==parkChild?1.2f:1.75f;hit.center=Vector3.up*hit.height*.5f;}
            // Small social beats use the same dialogue pipeline as the main cast.
            ResidentLine(parkResidents[0],"Deniz: Komşularımızı sayıyoruz. Herkesin burada olduğundan emin olalım.");
            ResidentLine(parkResidents[5],"Gül: Açık meydanı boş bırakalım. Gelen ailelerin geçebileceği yer olsun.");
            ResidentLine(parkResidents[12],"Mina: Okuldan arkadaşlarımı gördüm. Ailemle aynı yerde bekliyoruz.");
        }

        static ValueOutput NeighborhoodAvailable(YanYanaGraphAuthor g,int phase)
            => And(g,CanExplore(g),Is(g,g.Var("Phase",flow),phase));

        static ValueOutput NeighborhoodNeedsCue(YanYanaGraphAuthor g)
        {
            var gas=And(g,Is(g,g.Var("Phase",flow),4),And(g,Is(g,g.Var("GasNoticed",flow),1),Is(g,g.Var("GasReported",flow),0)));
            var family=And(g,Is(g,g.Var("Phase",flow),6),And(g,Is(g,g.Var("ParkChildNoticed",flow),1),Is(g,g.Var("ParkChildHelped",flow),0)));
            return And(g,Is(g,g.Var("Workspace",flow),""),Or(g,gas,family));
        }

        static object NeighborhoodCuePoint(YanYanaGraphAuthor g,int phase,object fallback)
        {
            if(phase!=4&&phase!=6)return fallback;
            ValueOutput Select(object a,object b,object condition)=>g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},a,b,g.Call(typeof(Convert),"ToSingle",null,OneBool,condition).result).result;
            object target=phase==4
                ?Select(new Vector3(-3.4f,ParkGround+.12f,-13),gasSteward.transform.position+Vector3.up,Is(g,g.Var("GasRetreated",flow),1))
                :Select(parkChild.transform.position+Vector3.up,parkSteward.transform.position+Vector3.up,Is(g,g.Var("ParkChildAsked",flow),1));
            return Select(fallback,target,NeighborhoodNeedsCue(g));
        }

        static void ResidentLine(GameObject actor,string line)
        {
            var hit=actor.AddComponent<CapsuleCollider>();hit.radius=.3f;hit.height=1.6f;hit.center=Vector3.up*.8f;
            var g=new YanYanaGraphAuthor(actor,"Mahalle komşusunu dinle");var click=g.Add(new OnPointerClick());g.Bind(click.target,actor);
            var close=g.Binary<Less>(g.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},g.Get(typeof(Transform),"position",cast["Ada"].transform),actor.transform.position).result,3.5f);
            var can=g.Branch(click.trigger,And(g,And(g,CanExplore(g),close),Or(g,Is(g,g.Var("Phase",flow),6),Is(g,g.Var("Phase",flow),7))));Text(g,can.ifTrue,lineText,line);g.Dirty();
        }

        static GameObject GroundCue(string name,Vector3 at,Material material)
        {
            var cue=Group(name,world.transform,at);
            Shape("Açık rota halkası",PrimitiveType.Cylinder,cue.transform,Vector3.up*.05f,new Vector3(.92f,.035f,.92f),material);
            Shape("Adım izi sol",PrimitiveType.Capsule,cue.transform,new Vector3(-.13f,.095f,0),new Vector3(.13f,.035f,.32f),mats["YY_cream"]);
            Shape("Adım izi sağ",PrimitiveType.Capsule,cue.transform,new Vector3(.13f,.095f,.07f),new Vector3(.13f,.035f,.32f),mats["YY_cream"]);
            var hit=cue.AddComponent<BoxCollider>();hit.center=Vector3.up*.10f;hit.size=new Vector3(1,.22f,1);return cue;
        }

        static void DressDamagedWall(Transform parent)
        {
            var mortar=Mat("WallMortar","#C9B9A1");
            for(int row=0;row<5;row++)for(int col=0;col<5;col++)
            {
                if(col>1&&row>4-col)continue;
                Shape("Yıkılmış bahçe duvarı · tuğla",PrimitiveType.Cube,parent,new Vector3(-.62f+col*.38f+(row%2)*.10f,.14f+row*.27f,-.58f),new Vector3(.36f,.25f,.30f),mats[row%2==0?"YY_terracotta":"ParkCoral"]);
            }
            Shape("Kırık sıva",PrimitiveType.Cube,parent,new Vector3(-.35f,.52f,-.78f),new Vector3(.90f,.95f,.04f),mortar);
            for(int i=0;i<11;i++)
            {var piece=Shape("Yola yaklaşılmayan duvar parçası "+i,PrimitiveType.Cube,parent,new Vector3(-.50f+(i%4)*.36f,.07f+(i%2)*.06f,-.12f+(i/4)*.30f),new Vector3(.34f,.14f,.23f),mats["YY_terracotta"]);piece.transform.localRotation=Quaternion.Euler(i*7%25,i*37,i*13%20);}
            var boundary=parent.gameObject.AddComponent<NavMeshObstacle>();boundary.shape=NavMeshObstacleShape.Box;boundary.center=new Vector3(0,.4f,-.15f);boundary.size=new Vector3(2.0f,1.5f,1.8f);boundary.carving=true;
        }

        static void CreateWallRouteChoice(Vector3 point)
        {
            InitialPhysical("WallRouteChosen");
            var destination=new Vector3(-4.9f,ParkGround,-12.4f);
            var cue=GroundCue("Duvarın açığındaki güvenli adım",destination,mats["ParkSign"]);
            var g=new YanYanaGraphAuthor(cue,"Yıkık duvarın dibinden geçme · açık rotaya yürü");var click=g.Add(new OnPointerClick());g.Bind(click.target,cue);
            var can=g.Branch(click.trigger,And(g,NeighborhoodAvailable(g,4),Is(g,g.Var("WallRouteChosen",flow),0)));g.Send(can.ifTrue,flow,"WalkOpenWallRoute");
            var tick=g.Add(new Unity.VisualScripting.Update());var visible=And(g,Is(g,g.Var("Phase",flow),4),Is(g,g.Var("WallRouteChosen",flow),0));var state=tick.trigger;foreach(var renderer in cue.GetComponentsInChildren<Renderer>())state=g.Set(state,typeof(Renderer),"enabled",renderer,visible);g.Set(state,typeof(Collider),"enabled",cue.GetComponent<Collider>(),visible);g.Dirty();
            var route=main.Event("WalkOpenWallRoute",true);var p=main.SetVar(route.trigger,"Busy",true);p=Text(main,p,lineText,"Efe: Duvarın dibi kısa görünüyor. Ada: Parçalar düşebilir; açık taraftan gidelim.");
            p=WalkAndWait(main,p,movers["Ada"],destination);p=main.SetVar(p,"WallRouteChosen",1);p=main.SetVar(p,"FacadeReported",1);p=main.Active(p,facadeBarrier,true);p=Text(main,p,lineText,"Görevli: Bildirdiğiniz duvarı şeritle kapattık. Açık yol burada.");p=main.SetVar(p,"Busy",false);main.Send(p,flow,"CommitCheckpoint");
        }

        static void CreateNeighborhoodEncounters()
        {
            CreateGasEncounter();CreateParkFamilyEncounter();
        }

        static void CreateGasEncounter()
        {
            InitialPhysical("GasNoticed");InitialPhysical("GasRetreated");InitialPhysical("GasReported");
            // A leak is indicated by dialogue about smell, not an invented visible gas cloud.
            var utility=Group("Gaz kokusu gelen servis köşesi",world.transform,new Vector3(.8f,ParkGround,-11.3f));
            Shape("Alçak servis duvarı",PrimitiveType.Cube,utility.transform,new Vector3(0,.7f,0),new Vector3(1.25f,1.4f,.26f),mats["YY_cream"]);
            Shape("Kapalı tesisat kutusu",PrimitiveType.Cube,utility.transform,new Vector3(0,.72f,-.22f),new Vector3(.55f,.65f,.25f),mats["YY_tealDark"]);
            Shape("Servis borusu",PrimitiveType.Cylinder,utility.transform,new Vector3(.46f,.42f,-.21f),new Vector3(.065f,.42f,.065f),mats["YY_stone"]);
            var utilityHit=utility.AddComponent<BoxCollider>();utilityHit.center=Vector3.up*.7f;utilityHit.size=new Vector3(1.4f,1.5f,.9f);
            var exclusion=utility.AddComponent<NavMeshObstacle>();exclusion.shape=NavMeshObstacleShape.Box;exclusion.center=Vector3.up*.5f;exclusion.size=new Vector3(3f,1.5f,2.0f);exclusion.carving=true;
            var u=new YanYanaGraphAuthor(utility,"Tesisata dokunma · önce uzaklaş");var click=u.Add(new OnPointerClick());u.Bind(click.target,utility);var can=u.Branch(click.trigger,NeighborhoodAvailable(u,4));
            var p=u.SetVar(can.ifTrue,"GasNoticed",1,flow);Text(u,p,lineText,"Ada: Gaz kokusu geliyor. Tesisata ve elektrik düğmelerine dokunmadan açık tarafa uzaklaşalım.");u.Dirty();
            var safePoint=new Vector3(-3.4f,ParkGround,-13.0f);
            var cue=GroundCue("Gazdan uzak açık bekleme noktası",safePoint,mats["ParkSign"]);
            var cueVisuals=cue.GetComponentsInChildren<Renderer>();
            var barrier=Group("Gaz için görevlinin kapattığı şerit",world.transform,new Vector3(.5f,ParkGround,-12.1f));
            for(int i=0;i<3;i++)Model("Cone",barrier.transform,new Vector3(-.75f+i*.75f,0,0),.8f,0);
            Shape("Görevli güvenlik şeridi",PrimitiveType.Cube,barrier.transform,new Vector3(0,.48f,0),new Vector3(2.2f,.09f,.04f),mats["ParkFlower"]);barrier.SetActive(false);
            var g=new YanYanaGraphAuthor(cue,"Gaz kokusundan uzaklaşıp yetişkine haber ver");click=g.Add(new OnPointerClick());g.Bind(click.target,cue);
            can=g.Branch(click.trigger,And(g,NeighborhoodAvailable(g,4),And(g,Is(g,g.Var("GasNoticed",flow),1),Is(g,g.Var("GasRetreated",flow),0))));g.Send(can.ifTrue,flow,"RetreatFromGas");
            var frame=g.Add(new Unity.VisualScripting.Update());p=frame.trigger;var shown=And(g,Is(g,g.Var("Phase",flow),4),And(g,Is(g,g.Var("GasNoticed",flow),1),Is(g,g.Var("GasRetreated",flow),0)));foreach(var r in cueVisuals)p=g.Set(p,typeof(Renderer),"enabled",r,shown);g.Set(p,typeof(Collider),"enabled",cue.GetComponent<Collider>(),shown);g.Dirty();
            var notice=main.Add(new Unity.VisualScripting.Update());var near=main.Binary<Less>(main.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},main.Get(typeof(Transform),"position",cast["Ada"].transform),new Vector3(-3.6f,ParkGround,-11.6f)).result,1.5f);
            can=main.Branch(notice.trigger,And(main,NeighborhoodAvailable(main,4),And(main,near,Is(main,main.Var("GasNoticed"),0))));p=main.SetVar(can.ifTrue,"GasNoticed",1);p=Text(main,p,goalText,"Kokudan uzaklaş ve görevliye bildir");p=Text(main,p,lineText,"Efe: Burada garip bir koku var. Ada: Gaz olabilir; açık tarafa uzaklaşıp bir yetişkine söyleyelim.");Text(main,p,gestureText,"Açık bekleme işaretine dokun");
            var retreat=main.Event("RetreatFromGas",true);p=main.SetVar(retreat.trigger,"Busy",true);p=WalkAndWait(main,p,movers["Ada"],safePoint);p=main.SetVar(p,"GasRetreated",1);p=Text(main,p,goalText,"Turuncu yelekli Eren’e haber ver");p=Text(main,p,lineText,"Ada: Kokudan uzaklaştık. Şimdi görevliye nereden geldiğini söyleyelim.");p=Text(main,p,gestureText,"Yakındaki turuncu yelekli görevliye dokun");p=main.SetVar(p,"Busy",false);main.Send(p,flow,"CommitCheckpoint");
            var staff=new YanYanaGraphAuthor(gasSteward,"Eren · güvenli mesafeden gaz bildirimi");click=staff.Add(new OnPointerClick());staff.Bind(click.target,gasSteward);can=staff.Branch(click.trigger,NeighborhoodAvailable(staff,4));
            var retreated=staff.Branch(can.ifTrue,Is(staff,staff.Var("GasRetreated",flow),1));p=staff.SetVar(retreated.ifTrue,"GasReported",1,flow);p=staff.Active(p,barrier,true);p=Text(staff,p,goalText,"İdil’e açık yoldan ulaş");p=Text(staff,p,lineText,"Eren: Yerini öğrendim, ilgili ekibe bildiriyorum. Siz tesisata yaklaşmadan açık yoldan devam edin.");p=Text(staff,p,gestureText,"Güvenli geçişten ilerle");staff.Send(p,flow,"CommitCheckpoint");Text(staff,retreated.ifFalse,lineText,"Eren: Önce kokudan uzaklaşıp açık bekleme noktasına geçin. Sonra bana yerini anlatın.");staff.Dirty();
            var restore=main.Event("RestorePhysicalObjects");main.Active(restore.trigger,barrier,Is(main,main.Var("GasReported"),1));
        }

        static void CreateParkFamilyEncounter()
        {
            InitialPhysical("ParkChildNoticed");InitialPhysical("ParkChildAsked");InitialPhysical("ParkChildHelped");
            ApproachEvent(parkChild,parkChild.transform.position+new Vector3(.7f,0,.6f),"ListenParkChild",6);
            var listen=main.Event("ListenParkChild");var fresh=main.Branch(listen.trigger,Is(main,main.Var("ParkChildHelped"),0));var p=main.SetVar(fresh.ifTrue,"ParkChildAsked",1);
            p=Text(main,p,goalText,"Ece için aile danışma görevlisine haber ver");p=Text(main,p,lineText,"Ece: Annemi göremiyorum. Ada: Burada kalalım. Hemen yanımızdaki görevliye haber verelim.");p=Text(main,p,gestureText,"Danışma masasındaki turuncu yelekli görevliye dokun");main.Send(p,flow,"CommitCheckpoint");
            var noticed=main.Add(new Unity.VisualScripting.Update());var near=main.Binary<Less>(main.Call(typeof(Vector3),"Distance",null,new[]{typeof(Vector3),typeof(Vector3)},main.Get(typeof(Transform),"position",cast["Ada"].transform),parkChild.transform.position).result,7f);
            var first=main.Branch(noticed.trigger,And(main,NeighborhoodAvailable(main,6),And(main,near,Is(main,main.Var("ParkChildNoticed"),0))));p=main.SetVar(first.ifTrue,"ParkChildNoticed",1);p=Text(main,p,lineText,"Efe: Danışma masasının yanında bir çocuk annesini arıyor. Ona yardım isteyip istemediğini sorabiliriz.");Text(main,p,gestureText,"Ece’yi dinle · Bora’nın yardım noktasına uğra");
            var staff=new YanYanaGraphAuthor(parkSteward,"Zeynep · çocuğun ailesini güvenli noktaya çağır");var click=staff.Add(new OnPointerClick());staff.Bind(click.target,parkSteward);
            var can=staff.Branch(click.trigger,And(staff,NeighborhoodAvailable(staff,6),And(staff,Is(staff,staff.Var("ParkChildAsked",flow),1),Is(staff,staff.Var("ParkChildHelped",flow),0))));staff.Send(can.ifTrue,flow,"HelpParkChild");staff.Dirty();
            var help=main.Event("HelpParkChild",true);p=main.SetVar(help.trigger,"Busy",true);p=WalkAndWait(main,p,movers["Ada"],parkSteward.transform.position+new Vector3(.6f,0,.5f));p=Text(main,p,lineText,"Zeynep: Ece burada güvende. Annesi danışma noktasına adını bıraktı; onu buraya çağırıyorum.");
            var reunion=parkChild.transform.position+new Vector3(-.72f,0,-.3f);
            p=WalkAndWait(main,p,parkMother.GetComponent<StoryPlayerMovement>(),reunion,.35f);p=main.Set(p,typeof(Transform),"rotation",parkMother.transform,Quaternion.Euler(0,65,0));p=main.SetVar(p,"ParkChildHelped",1);p=Text(main,p,lineText,"Ece: Anne! Aylin: Görevlinin yanında beklemen iyi oldu. Teşekkür ederiz Ada.");p=Text(main,p,goalText,"Bora’nın yardım noktasına uğra");p=Text(main,p,gestureText,"Bora’ya dokunarak komşulara yardıma devam et");p=main.SetVar(p,"Busy",false);main.Send(p,flow,"CommitCheckpoint");
            var restore=main.Event("RestorePhysicalObjects");var done=main.Branch(restore.trigger,Is(main,main.Var("ParkChildHelped"),1));main.Do(done.ifTrue,typeof(StoryPlayerMovement),"Warp",parkMother.GetComponent<StoryPlayerMovement>(),new[]{typeof(Vector3)},reunion);main.Do(done.ifFalse,typeof(StoryPlayerMovement),"Warp",parkMother.GetComponent<StoryPlayerMovement>(),new[]{typeof(Vector3)},new Vector3(-11.0f,ParkGround,-27.4f));
        }
    }
}
