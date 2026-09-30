using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using TMPro;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Physical Supply Comparison")]
        static void SupplyComparison()=>StartPhysicalCheck(SupplyComparisonSequence(),"physical-supply-comparison");
        static IEnumerable<object> SupplyComparisonSequence()
        {
            var old=Flow;New();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn(""),"Fresh supplies journey"))yield return f;
            foreach(var f in IntroCarrySequence(true,false))yield return f;
            Click(Find("Odada bulunacak · Water"));foreach(var f in WaitPhysical(()=>ReadyIn("supplyWater"),"Approach water"))yield return f;Capture();
            var center=Find("Ambalaj incelemesi · Water").transform.position;var bad=Find("İncelenen Water 0");
            foreach(var f in TimedDrag(Find("Ambalajı çevir · Water"),center+new Vector3(.2f,.025f,.03f)))yield return f;
            if(State("SupplyViewedWater")!=1)throw new InvalidOperationException("Package inspection did not register.");
            foreach(var f in TimedDrag(bad,center+new Vector3(.45f,.035f,-.1f)))yield return f;
            if(State("Found.Water")!=0)throw new InvalidOperationException("Dropping outside the selection mat collected water.");
            var rest=bad.transform.position;var screen=(Vector2)Camera.main.WorldToScreenPoint(rest);
            var data=new PointerEventData(EventSystem.current){position=screen,pressPosition=screen,pointerId=-1,pointerDrag=bad,pointerPress=bad,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(bad,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(bad,data,ExecuteEvents.beginDragHandler);data.position+=new Vector2(45,-45);ExecuteEvents.Execute(bad,data,ExecuteEvents.dragHandler);
            if(Vector3.Distance(rest,bad.transform.position)<.01f)throw new InvalidOperationException("No partial package drag was exercised.");
            PausePhysical();PausePhysical();yield return null;ResumePhysical();foreach(var f in WaitPhysical(()=>ReadyIn("supplyWater"),"Resume package inspection"))yield return f;
            if(State("Found.Water")!=0||(bool)Variables.Object(Flow).Get("Dragging"))throw new InvalidOperationException("Partial package drag was committed or remained held.");
            foreach(var f in TimedDrag(bad,center+new Vector3(0,.035f,-.34f)))yield return f;
            foreach(var f in WaitPhysical(()=>State("Found.Water")==1&&ReadyIn(""),"Collect opened water"))yield return f;
            if(State("WaterReady")!=0||State("SupplyChoiceWater")!=0)throw new InvalidOperationException("Opened water marked usable.");
            Click(Find("Odada bulunacak · Food"));foreach(var f in WaitPhysical(()=>ReadyIn("supplyFood"),"Approach food"))yield return f;
            center=Find("Ambalaj incelemesi · Food").transform.position;Capture();foreach(var f in TimedDrag(Find("İncelenen Food 1"),center+new Vector3(0,.035f,-.34f)))yield return f;
            foreach(var f in WaitPhysical(()=>State("Found.Food")==1&&ReadyIn(""),"Collect intact food"))yield return f;
            if(State("FoodReady")!=1)throw new InvalidOperationException("Intact food did not remain usable.");
            Bag();foreach(var f in WaitPhysical(()=>ReadyIn("bag"),"Pack the two chosen supplies"))yield return f;
            var origin=Find("Anchor_BagWork").transform.position+new Vector3(-.2375f,.083f,-.13f);
            foreach(var f in TimedDrag(Find("Yerleşim · Water"),origin+new Vector3(.0475f,0,.1425f)))yield return f;
            foreach(var f in TimedDrag(Find("Yerleşim · Food"),origin+new Vector3(.19f,0,.0475f)))yield return f;
            Click(Find("Çantayı kapatma tokası"));foreach(var f in CompleteBackpackFitting())yield return f;
            old=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn(""),"Restore supply decisions"))yield return f;
            if(State("WaterReady")!=0||State("FoodReady")!=1||State("Pack.Water.X")!=0||State("Pack.Food.X")!=1)throw new InvalidOperationException("Supply/packing choices changed on reload.");
            File.AppendAllText(physicalCheckReport,"PASS rotated inspection, rejected outside drop, partial-drag double pause, opened water/intact food choices, physical packing and reload.\n");
            AidFixture();foreach(var f in WaitPhysical(()=>ReadyIn("aid"),"Isolated aid consequence fixture"))yield return f;AidTest();foreach(var f in WaitPhysical(()=>ReadyIn("relief"),"Visitors arrive before relief"))yield return f;Capture();
            yield return null;yield return null;
            if(Find("Malzeme kaynağı · Water").GetComponent<TMP_Text>().text!="MASADAN"||Find("Malzeme kaynağı · Food").GetComponent<TMP_Text>().text!="ÇANTADAN")throw new InvalidOperationException("Wrong supply sources at relief.");
            foreach(var f in TimedDrag(Find("Efe’ye verilecek su"),Find("Dinlenme su hedefi").transform.position))yield return f;
            if(State("ReliefWaterGiven")!=1||State("ReliefFoodGiven")!=0||State("AidStage")!=1)throw new InvalidOperationException("Relief skipped the food interaction.");
            old=Flow;ReloadPhysical();yield return null;foreach(var f in WaitPhysical(()=>Flow!=old&&ReadyIn("relief"),"Resume completed water only"))yield return f;
            if(Find("Efe’ye verilecek su").activeSelf||!Find("Efe’ye verilecek yiyecek").activeSelf)throw new InvalidOperationException("Relief resume did not keep the completed water action.");
            foreach(var f in TimedDrag(Find("Efe’ye verilecek yiyecek"),Find("Efe’nin yiyecek tabağı").transform.position))yield return f;
            foreach(var f in WaitPhysical(()=>ReadyIn("broadcast")&&State("ReliefGiven")==1,"Both supplies delivered"))yield return f;
            File.AppendAllText(physicalCheckReport,"PASS opened packed water replaced from aid table; intact packed food from bag; separate water/food gestures; completed water persists across reload. Aid entry alone used a phase/location fixture; prior supplies were real inputs.\n");
        }
    }
}
