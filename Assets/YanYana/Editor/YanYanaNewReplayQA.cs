using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using TMPro;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Physical New Decision Replays")]
        static void NewDecisionReplays()=>StartPhysicalCheck(NewDecisionReplaySequence(),"physical-new-decision-replays");
        static IEnumerable<object> NewDecisionReplaySequence()
        {
            Find("Continue").GetComponent<Button>().onClick.Invoke();
            foreach(var f in WaitPhysical(()=>ReadyIn("")&&State("Phase")==8,"Continue completed prepared route"))yield return f;
            Find("ReplayPlan").GetComponent<Button>().onClick.Invoke();yield return null;
            foreach(string id in new[]{"map","flashlight","radio","supplyWater","supplyFood","bagfit","home","evacuation","fire","aid"})
                if(!Find("ReplayDecision"+id).GetComponent<Button>().interactable)throw new InvalidOperationException("Visited replay not available: "+id);
            ScreenCapture.CaptureScreenshot("ClientExports/YanYana/Screenshots/physical-replay-ten-choices.png");double captured=EditorApplication.timeSinceStartup;while(EditorApplication.timeSinceStartup-captured<.6)yield return null;
            var old=Flow;Find("ReplayDecisionbagfit").GetComponent<Button>().onClick.Invoke();yield return null;
            foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn("bagfit"),"Replay the actual bag fitting entry"))yield return f;
            if(State("FamilyPlan")!=1||State("FoodReady")!=1||State("BagZipStep")!=0||State("BagCarried")!=0||State("Phase")!=0)throw new InvalidOperationException("Bag replay lost earlier preparation or retained later carrying.");
            if(PlayerPrefs.GetInt("Deprem.YanYana.v1.snapshot.supplyFood.exists")!=1||PlayerPrefs.GetInt("Deprem.YanYana.v1.snapshot.fire.exists")!=0)throw new InvalidOperationException("Bag replay invalidated the wrong snapshots.");
            Find("Hint").GetComponent<Button>().onClick.Invoke();yield return null;
            if(!UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.text.StartsWith("Derya: Fermuarın izini")))throw new InvalidOperationException("Bag hint is unrelated to its workspace.");
            old=Flow;CustomEvent.Trigger(Flow,"ReplayPhysical","supplyWater");yield return null;
            foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn("supplyWater"),"Replay earlier package decision"))yield return f;
            if(State("FamilyPlan")!=1||State("Found.Water")!=0||State("Found.Food")!=0||State("BagClosed")!=0||State("BagZipStep")!=0||State("Ending")!=0)throw new InvalidOperationException("Package replay retained downstream consequences.");
            if(PlayerPrefs.GetInt("Deprem.YanYana.v1.snapshot.bagfit.exists")!=0||PlayerPrefs.GetInt("Deprem.YanYana.v1.snapshot.supplyFood.exists")!=0||PlayerPrefs.GetInt("Deprem.YanYana.v1.snapshot.map.exists")!=1)throw new InvalidOperationException("Package chronology invalidation failed.");
            Find("Hint").GetComponent<Button>().onClick.Invoke();yield return null;
            if(!UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.text.StartsWith("Derya: Ambalajı çevir")))throw new InvalidOperationException("Package hint is unrelated to its workspace.");
            File.AppendAllText(physicalCheckReport,"PASS all ten visited menu entries; bag replay through final UI; earlier map/supplies retained; carrying and later chapters reset; earlier water replay through the same production graph event; chronological snapshots cleared; contextual hints. No decision-state assignment.\n");
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Unclosed Bag Consequence")]
        static void UnclosedBag()=>StartPhysicalCheck(UnclosedBagSequence(),"physical-unclosed-bag");
        [MenuItem("Tools/Yan Yana/QA/Physical Replay Menu Layout")]
        static void ReplayLayout()=>StartPhysicalCheck(ReplayLayoutSequence(),"physical-replay-layout");
        static IEnumerable<object> ReplayLayoutSequence()
        {
            Find("Continue").GetComponent<Button>().onClick.Invoke();foreach(var f in WaitPhysical(()=>!(bool)Variables.Object(Flow).Get("Paused")&&!(bool)Variables.Object(Flow).Get("Busy"),"Continue before layout inspection"))yield return f;
            Find("ReplayPlan").GetComponent<Button>().onClick.Invoke();double shown=EditorApplication.timeSinceStartup;while(EditorApplication.timeSinceStartup-shown<.6)yield return null;
            ThreeRatios();File.AppendAllText(physicalCheckReport,"Layout fixture opens the production replay panel from the current checkpoint. True final activation and chronological replay were tested separately. Three-ratio captures follow in aspect-replay-* files.\n");
        }
        static IEnumerable<object> UnclosedBagSequence()
        {
            var old=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn(""),"New unpacked journey"))yield return f;
            foreach(var f in IntroCarrySequence(true,false))yield return f;
            Click(Find("Odada bulunacak · Water"));foreach(var f in WaitPhysical(()=>ReadyIn("supplyWater"),"Inspect water before carrying"))yield return f;
            var center=Find("Ambalaj incelemesi · Water").transform.position;
            foreach(var f in TimedDrag(Find("İncelenen Water 1"),center+new Vector3(0,.035f,-.34f)))yield return f;
            foreach(var f in WaitPhysical(()=>ReadyIn(""),"Usable water selected"))yield return f;
            Bag();foreach(var f in WaitPhysical(()=>ReadyIn("bag"),"Pack water but leave the bag open"))yield return f;
            var origin=Find("Anchor_BagWork").transform.position+new Vector3(-.2375f,.083f,-.13f);
            foreach(var f in TimedDrag(Find("Yerleşim · Water"),origin+new Vector3(.0475f,0,.1425f)))yield return f;
            Back();foreach(var f in WaitPhysical(()=>ReadyIn(""),"Leave the open bag"))yield return f;
            if(State("WaterReady")!=1||State("Pack.Water.X")!=0||State("BagClosed")!=0)throw new InvalidOperationException("The unclosed-bag scenario was not exercised.");
            AidFixture();foreach(var f in WaitPhysical(()=>ReadyIn("aid"),"Isolated later aid consequence"))yield return f;AidTest();foreach(var f in WaitPhysical(()=>ReadyIn("relief"),"Visitors arrive before assistance"))yield return f;
            if(Find("Malzeme kaynağı · Water").GetComponent<TMP_Text>().text!="MASADAN")throw new InvalidOperationException("An open bag teleported water into the aid scene.");
            File.AppendAllText(physicalCheckReport,"PASS usable water was selected and physically packed but bag left open; aid table supplies the later water. Only aid entry used a phase/location fixture; water and packing were production inputs.\n");
        }
    }
}
