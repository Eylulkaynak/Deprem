using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        [MenuItem("Tools/Yan Yana/QA/Physical Home Exit")]
        static void HomeExit(){Click(Find("Efe"));Click(Find("Erişilebilir acil aydınlatma"));Click(Find("Kapalı kapı"));Inspect();}
        [MenuItem("Tools/Yan Yana/QA/Physical Walk Landing")]
        static void WalkLanding()=>Find("Ada").GetComponent<StoryPlayerMovement>().TrySetDestination(Find("Street_StairsStart").transform.position);
        [MenuItem("Tools/Yan Yana/QA/Physical Aftershock")]
        static void Aftershock()=>Drag(Find("Ada"),Find("Ada").transform.position+Vector3.down*2);
        [MenuItem("Tools/Yan Yana/QA/Physical Neighbor")]
        static void Neighbor()=>Click(Find("Yusuf"));
        [MenuItem("Tools/Yan Yana/QA/Physical Help Neighbor")]
        static void HelpNeighbor(){Find("NeighborWalk").GetComponent<Button>().onClick.Invoke();var box=Find("Yusuf’un önündeki hafif boş kutu");Drag(box,Find("Street_Yusuf").transform.position+new Vector3(.85f,0,-1));}
        [MenuItem("Tools/Yan Yana/QA/Physical Neighbor Team")]
        static void NeighborTeam()=>Find("NeighborTeam").GetComponent<Button>().onClick.Invoke();
        [MenuItem("Tools/Yan Yana/QA/Physical Walk Idil")]
        static void WalkIdil()=>Find("Ada").GetComponent<StoryPlayerMovement>().TrySetDestination(Find("Street_Idil").transform.position+Vector3.forward*.8f);
        [MenuItem("Tools/Yan Yana/QA/Physical Talk Idil")]
        static void TalkIdil()=>Click(Find("Idil"));
        [MenuItem("Tools/Yan Yana/QA/Physical Walk Bora")]
        static void WalkBora()=>Find("Ada").GetComponent<StoryPlayerMovement>().TrySetDestination(Find("Street_Aid").transform.position+Vector3.forward*.9f);
        [MenuItem("Tools/Yan Yana/QA/Physical Talk Bora")]
        static void TalkBora()=>Click(Find("Bora"));
    }
}
