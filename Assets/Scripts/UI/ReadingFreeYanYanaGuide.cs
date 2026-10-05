using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Deprem.Accessibility
{
    // Reads the authored graph's state. Never advances a task, resets idle time or writes a save.
    public sealed class ReadingFreeYanYanaGuide : MonoBehaviour
    {
        private VariableDeclarations state;
        private readonly Dictionary<string, Transform> objects = new Dictionary<string, Transform>();
        private RectTransform canvas;
        private ReadingFreeIcon hand, ring;
        private TMP_Text gesture;
        private Transform authoredCue;
        private void Start()
        {
            foreach(var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.gameObject.scene == gameObject.scene && !objects.ContainsKey(t.name)) objects.Add(t.name,t);
            var flow = objects.Values.FirstOrDefault(t => t.name.StartsWith("01 Akış"));
            if (flow == null) { enabled=false; return; } state=Variables.Object(flow.gameObject);
            authoredCue=Find("On saniyede dikkat, yirmi beşte hareket");
            var gestures=Find("Hareket ipucu"); gesture=gestures != null ? gestures.GetComponentInChildren<TMP_Text>(true) : null;
            var go=new GameObject("Immediate picture guide",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=80;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            canvas=(RectTransform)go.transform; hand=ReadingFree3D.NewIcon(canvas,"Gesture hand");ring=ReadingFree3D.NewIcon(canvas,"Target ring");
            hand.rectTransform.sizeDelta=new Vector2(92,92);ring.rectTransform.sizeDelta=new Vector2(88,88);
        }
        private Transform Find(string key)=>objects.TryGetValue(key,out var result)?result:null;
        private T Read<T>(string key,T fallback=default)=>state!=null&&state.IsDefined(key)&&state.Get(key) is T value?value:fallback;
        private void LateUpdate()
        {
            if (hand==null) return;
            bool show=Time.timeScale>0f&&!Read("Paused",true)&&!Read("Busy",false)&&Read("Phase",0)<8&&Camera.main!=null;
            Vector3? world=null;string workspace=Read("Workspace","");
            if (show && Read("IntroDone",0)==0) world=Read("IntroCuePosition",Vector3.zero);
            if (show && world==null)
            {
                Transform target=Read("HandContact",false)?Read<Transform>("ActiveHandTarget"):null;
                if(target==null||!target.gameObject.activeInHierarchy)
                {
                    string key=workspace=="flashlight"?"Anchor_FlashlightWork":workspace=="bag"||workspace=="bagfit"?"Anchor_BagWork":workspace=="radio"||workspace.StartsWith("supply")?"Anchor_RadioWork":workspace=="map"||workspace=="family"?"Anchor_FamilyMap":null;
                    if (key!=null) target=Find(key);
                    else if(workspace=="")
                    {
                        int phase=Read("Phase",0);
                        target=Find(phase==0?"Anchor_FlashlightWork":phase==1?"Anchor_Comfort":phase==2?(Read("SiblingChecked",0)==1?"Anchor_Exit":"Efe"):phase==3||phase==31?"Yusuf":phase==4?"Idil":phase==6?"Aid":"Ada");
                    }
                }
                if(target!=null&&target.gameObject.activeInHierarchy) world=target.position;
            }
            Vector3 screen=Vector3.zero;
            if(show&&authoredCue!=null&&authoredCue.gameObject.activeInHierarchy) screen=authoredCue.position;
            else if(show&&world.HasValue) screen=Camera.main.WorldToScreenPoint(world.Value);
            else show=false;
            if(screen.z<0)show=false;
            hand.gameObject.SetActive(show);ring.gameObject.SetActive(show);if(!show)return;
            string hint=ReadingFree3D.Normalize(gesture!=null?gesture.text:"");
            float t=Mathf.Repeat(Time.time,2)/2,travel=Mathf.SmoothStep(0,1,Mathf.Clamp01(t*1.5f));
            Vector3 delta=hint.Contains("asagi")?Vector3.down*Screen.height*.05f:hint.Contains("surukle")||hint.Contains("kaydir")?Vector3.right*Screen.width*.10f:Vector3.zero;
            if(workspace=="intro" && Find("Kutuyu kavrama noktası")!=null)
                delta=Camera.main.WorldToScreenPoint(Find("Kutuyu kavrama noktası").position)-screen;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen+delta*travel,null,out var point);
            hand.rectTransform.anchoredPosition=point;hand.Set(hint.Contains("basili")?"hold":"hand");
            hand.rectTransform.localScale=Vector3.one*(delta==Vector3.zero?1-.10f*Mathf.Sin(t*Mathf.PI*2):1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen+delta,null,out point);ring.rectTransform.anchoredPosition=point;ring.Set("target");
        }
    }
}
