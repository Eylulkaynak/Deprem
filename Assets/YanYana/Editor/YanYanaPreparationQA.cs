using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static readonly string[] collectibles={"Water","Food","FirstAid","Blanket","FamilyCard","Whistle","ComfortFox"};
        static int collectionIndex;
        static double collectionStarted;
        [MenuItem("Tools/Yan Yana/QA/Physical Collect All")]
        static void CollectAll()
        {
            if((int)Variables.Object(Flow).Get("Phase")!=0||(string)Variables.Object(Flow).Get("Workspace")!="")throw new InvalidOperationException("Start in preparation exploration.");
            collectionIndex=0;File.WriteAllText("ClientExports/YanYana/Reports/physical-collectibles.txt","Actual scene item clicks, production approach graphs and NavMesh movement. No inventory flags or positions assigned by test.\n");
            CollectNext();EditorApplication.update-=CollectionTick;EditorApplication.update+=CollectionTick;
        }
        static void CollectNext()
        {
            collectionStarted=EditorApplication.timeSinceStartup;
            var objects=UnityEngine.Object.FindObjectsByType<Variables>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            string key=collectibles[collectionIndex];
            var item=objects.First(x=>x.gameObject.name=="Odada bulunacak · "+key).gameObject;Click(item);
        }
        static void CollectionTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=CollectionTick;return;}
            string key=collectibles[collectionIndex];double elapsed=EditorApplication.timeSinceStartup-collectionStarted;
            if((key=="Water"||key=="Food")&&ReadyIn("supply"+key))
                Drag(Find("İncelenen "+key+" 1"),Find("Ambalaj incelemesi · "+key).transform.position+new Vector3(0,.035f,-.34f));
            if((int)Variables.Object(Flow).Get("Found."+key)==1)
            {
                if(!ReadyIn(""))return;
                File.AppendAllText("ClientExports/YanYana/Reports/physical-collectibles.txt","PASS "+key+" seconds="+elapsed.ToString("F2")+"\n");
                if(++collectionIndex==collectibles.Length){EditorApplication.update-=CollectionTick;Inspect();return;}CollectNext();
            }
            else if(elapsed>25){File.AppendAllText("ClientExports/YanYana/Reports/physical-collectibles.txt","FAIL "+key+" timeout Ada="+Find("Ada").transform.position+"\n");EditorApplication.update-=CollectionTick;Inspect();}
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Adult Safety")]
        static void AdultSafety()
        {
            Click(Find("Hazırlanabilir raf"));Click(Find("Hazırlanabilir dolap"));
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Pack Collected Items")]
        static void PackCollected()
        {
            var vars=Variables.Object(Flow);if((string)vars.Get("Workspace")!="bag")throw new InvalidOperationException("Approach the bag first.");
            var items=new[]{("Radio",3,2),("FirstAid",2,2),("Blanket",2,2),("Water",1,3),("Flashlight",1,2),("Food",2,1),("ComfortFox",1,2),("FamilyCard",1,1),("Whistle",1,1)};
            if(items.Any(x=>(int)vars.Get("Found."+x.Item1)!=1))throw new InvalidOperationException("Collect and inspect every item through its production interaction first.");
            var layout=new Vector3Int[items.Length];var occupied=new bool[30];
            bool Solve(int n)
            {
                if(n==items.Length)return true;
                for(int rotation=0;rotation<2;rotation++)
                {
                    int w=rotation==0?items[n].Item2:items[n].Item3,h=rotation==0?items[n].Item3:items[n].Item2;
                    for(int y=0;y<=6-h;y++)for(int x=0;x<=5-w;x++)
                    {
                        bool free=true;for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)if(occupied[yy*5+xx])free=false;if(!free)continue;
                        for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)occupied[yy*5+xx]=true;layout[n]=new Vector3Int(x,y,rotation);if(Solve(n+1))return true;
                        for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)occupied[yy*5+xx]=false;
                    }
                }
                return false;
            }
            if(!Solve(0))throw new InvalidOperationException("No complete spatial packing solution exists.");
            var origin=Find("Anchor_BagWork").transform.position+new Vector3(-.2375f,.083f,-.13f);
            for(int i=0;i<items.Length;i++)
            {
                var item=Find("Yerleşim · "+items[i].Item1);var spot=layout[i];Click(item);
                if((int)vars.Get("Pack."+items[i].Item1+".Rot")!=spot.z)Find("RotateItem").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                int w=spot.z==0?items[i].Item2:items[i].Item3,h=spot.z==0?items[i].Item3:items[i].Item2;Drag(item,origin+new Vector3((spot.x+w*.5f)*.095f,0,(spot.y+h*.5f)*.095f));
                if((int)vars.Get("Pack."+items[i].Item1+".X")!=spot.x||(int)vars.Get("Pack."+items[i].Item1+".Y")!=spot.y)throw new InvalidOperationException("Packing failed for "+items[i].Item1);
            }
            Click(Find("Çantayı kapatma tokası"));File.WriteAllText("ClientExports/YanYana/Reports/physical-complete-packing.txt","PASS all nine previously collected/inspected items fit through production pointer handlers, rotations and grid occupancy; BagClosed="+vars.Get("BagClosed")+". No inventory flag, placement flag or world transform assigned by test.");Inspect();Capture();
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Actual Pose Bounds")]
        static void ActualPoseBounds()
        {
            var lines=new System.Collections.Generic.List<string>();var mesh=new Mesh();
            foreach(string who in new[]{"Ada","Efe","Derya","Emre"})
            {
                bool first=true;var bounds=new Bounds();var actor=Find(who);
                foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.BakeMesh(mesh,false);lines.Add(who+" raw baked bounds="+mesh.bounds+" renderer local="+renderer.transform.localScale+" lossy="+renderer.transform.lossyScale);
                    foreach(var vertex in mesh.vertices){var point=renderer.transform.position+renderer.transform.rotation*vertex;if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
                }
                lines.Add(who+" actual baked mesh min="+bounds.min+" max="+bounds.max+" root="+actor.transform.position);
            }
            UnityEngine.Object.DestroyImmediate(mesh);File.WriteAllLines("ClientExports/YanYana/Reports/physical-pose-bounds.txt",lines);
        }
    }
}
