// Read-only visual observer plus a pause test using the production UI handlers.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaPhysicalQA
    {
        static string presentationFolder,presentationKey="";
        static double presentationChanged,presentationStarted;
        static readonly HashSet<string> presentationShots=new HashSet<string>();

        [MenuItem("Tools/Yan Yana/QA/Watch Presentation During Route")]
        static void WatchPresentation()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play first.");
            presentationFolder="ClientExports/YanYana/Screenshots/presentation-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(presentationFolder);
            presentationShots.Clear();presentationKey="";presentationStarted=EditorApplication.timeSinceStartup;
            EditorApplication.update-=ObservePresentation;EditorApplication.update+=ObservePresentation;
        }
        static void ObservePresentation()
        {
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup-presentationStarted>720){EditorApplication.update-=ObservePresentation;return;}
            if(!Flow)return;
            var vars=Variables.Object(Flow);int phase=Convert.ToInt32(vars.Get("Phase"));string work=(string)vars.Get("Workspace");
            string key=phase==0?(work==""?"explore":"work-"+work):phase==1?"quake-"+vars.Get("CoverStage"):phase==31?"aftershock":phase==5?work:phase>=7?"ending-"+vars.Get("Ending")+"-"+phase+"-step"+vars.Get("FinalStep"):"";
            if(key==""||key=="hose")return;
            if(key!=presentationKey){presentationKey=key;presentationChanged=EditorApplication.timeSinceStartup;return;}
            double settle=phase==31?1.1:phase==7&&State("FinalStep")==3?1.2:.22;
            if(EditorApplication.timeSinceStartup-presentationChanged<settle||presentationShots.Contains(key))return;
            var brain=Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();if(brain&&brain.IsBlending)return;
            if(Find("Kamera geçişinde dokunmayı beklet").GetComponent<UnityEngine.UI.Image>().raycastTarget)return;
            if((phase==0||phase==5)&&(bool)vars.Get("Busy"))return;
            if(phase==7&&State("Ending")>0)
            {
                string expected="Yakından incele · "+(State("FinalStep")==0?"reunionRoute":State("FinalStep")==1?"reunionApproach":"reunion")+State("Ending");
                if(!brain||brain.ActiveVirtualCamera==null||brain.ActiveVirtualCamera.Name!=expected)return;
            }
            presentationShots.Add(key);ScreenCapture.CaptureScreenshot(presentationFolder+"/"+key+".png");
            if(work=="map")
            {
                var token=Find("Haritadaki aile taşı");var at=Camera.main.WorldToScreenPoint(token.transform.position);
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=at,pointerId=-1},hits);
                bool reachable=hits.Count>0&&ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject)==token;
                File.AppendAllText(presentationFolder+"/observations.txt","MAP_DRAG_RAYCAST="+reachable+" first="+(hits.Count>0?hits[0].gameObject.name:"none")+"\n");
            }
            File.AppendAllText(presentationFolder+"/observations.txt",key+" camera="+Camera.main.transform.position+" phase="+phase+" timeScale="+Time.timeScale+"\n"+EffectsSnapshot());
        }
        static string EffectsSnapshot()
        {
            var lines=new List<string>();
            if(State("Phase")==1||State("Phase")==31)foreach(string who in new[]{"Ada","Efe"})
            {
                var animator=Find(who).GetComponentInChildren<Animator>();bool right=who=="Ada";var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var definition=YanYanaInteractionHands.Read(who).hands.Single(h=>h.right==right);
                lines.Add(who+" head-palm alignment="+Vector3.Dot(hand.TransformDirection(definition.normal),Find("Başta güvenli avuç teması · "+who).transform.forward).ToString("F3")+" right contact="+Variables.Object(Flow).Get(who+"RightGripError")+" left contact="+Variables.Object(Flow).Get(who+"LeftGripError"));
            }
            if(State("Phase")==31)lines.Add("Aftershock children separation="+Vector3.Distance(Find("Ada").transform.position,Find("Efe").transform.position).ToString("F3"));
            foreach(var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                if(ps.particleCount>0||ps.name=="Yükselen duman")lines.Add("Particle "+ps.name+" playing="+ps.isPlaying+" paused="+ps.isPaused+" count="+ps.particleCount+" time="+ps.time.ToString("F3"));
            foreach(var anim in UnityEngine.Object.FindObjectsByType<Animation>(FindObjectsSortMode.None))
                lines.Add("Animation "+anim.name+" playing="+anim.isPlaying+" time="+anim[anim.clip.name].time.ToString("F3")+" speed="+anim[anim.clip.name].speed);
            return string.Join("\n",lines)+"\n";
        }
        [MenuItem("Tools/Yan Yana/QA/Physical Effects Pause")]
        static void EffectsPauseQA()=>StartPhysicalCheck(EffectsPauseSequence(),"physical-effects-pause");
        static IEnumerable<object> EffectsPauseSequence()
        {
            if(!ReadyIn("fire1")&&!ReadyIn("fire2")&&!ReadyIn("fire3"))throw new InvalidOperationException("Start at an active fire group.");
            // A short-lived steam fixture also verifies explicit component pause independent of timeScale.
            var steam=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).First(p=>p.name.StartsWith("Sönünce kalan buhar"));steam.Play(false);
            foreach(var f in FramesFor(.5f))yield return f;
            var particles=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(p=>p.isPlaying).ToArray();
            if(!particles.Contains(steam)||!particles.Any(p=>p.name=="Yükselen duman"&&p.particleCount>0))throw new InvalidOperationException("Smoke/steam fixture is not emitting.");
            var animations=UnityEngine.Object.FindObjectsByType<Animation>(FindObjectsSortMode.None).ToArray();
            File.AppendAllText(physicalCheckReport,"BEFORE\n"+EffectsSnapshot());
            PausePhysical();PausePhysical();foreach(var f in FramesFor(.1f))yield return f;
            var times=particles.Select(p=>p.time).ToArray();var clipTimes=animations.Select(a=>a[a.clip.name].time).ToArray();
            foreach(var f in FramesFor(.6f))yield return f;
            for(int i=0;i<particles.Length;i++)if(!particles[i].isPaused||Mathf.Abs(particles[i].time-times[i])>.0001f)throw new InvalidOperationException("Smoke advances while paused.");
            for(int i=0;i<animations.Length;i++)if(animations[i][animations[i].clip.name].speed!=0||Mathf.Abs(animations[i][animations[i].clip.name].time-clipTimes[i])>.0001f)throw new InvalidOperationException("Animation not frozen: "+animations[i].name+" clip="+animations[i].clip.name+" speed="+animations[i][animations[i].clip.name].speed);
            foreach(var actor in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))if(actor.speed!=0)throw new InvalidOperationException("Actor animation was not explicitly paused.");
            ResumePhysical();foreach(var f in WaitPhysical(()=>!(bool)Variables.Object(Flow).Get("Paused"),"Effects resume after pointer release"))yield return f;foreach(var f in FramesFor(.4f))yield return f;
            if(particles.Any(p=>p.isPaused)||animations.Any(a=>a[a.clip.name].speed<=0))throw new InvalidOperationException("A visual effect did not resume.");
            File.AppendAllText(physicalCheckReport,"RESUMED\n"+EffectsSnapshot()+"PASS explicit component pause, repeated pause, frozen smoke/flame times and resumed playback.\n");Capture();
        }

        [MenuItem("Tools/Yan Yana/QA/Physical Map Screen Reachability")]
        static void MapScreenQA()=>StartPhysicalCheck(MapScreenSequence(),"physical-map-screen-reachability");
        static IEnumerable<object> MapScreenSequence()
        {
            foreach(var f in IntroCarrySequence(true,true))yield return f;
            foreach(var f in WaitPhysical(()=>ReadyIn("")&&State("IntroDone")==1,"Opening complete"))yield return f;
            Map();foreach(var f in WaitPhysical(()=>ReadyIn("map")&&!Find("Kamera geçişinde dokunmayı beklet").GetComponent<UnityEngine.UI.Image>().raycastTarget,"Map and input settled"))yield return f;
            var board=Find("Ailecek denenen resimli mahalle planı").transform;var token=Find("Haritadaki aile taşı");
            var folder="ClientExports/YanYana/Screenshots/map-stability-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(folder);
            foreach(int height in new[]{960,1170,1200})
            {
                YanYanaQA.SetGameView(540,height);foreach(var f in FramesFor(.8f))yield return f;
                ScreenCapture.CaptureScreenshot(folder+"/map-"+height+".png");foreach(var f in FramesFor(.1f))yield return f;
                foreach(int cell in new[]{3,6,7,10,11})
                {
                    var at=Camera.main.WorldToScreenPoint(token.transform.position);var hits=new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=at},hits);
                    if(hits.Count==0||ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject)!=token)throw new InvalidOperationException("Map token occluded by "+(hits.Count>0?hits[0].gameObject.name:"NONE"));
                    foreach(var f in TimedDrag(token,board.position+new Vector3(cell%3*.22f,.065f,cell/3*.22f)))yield return f;
                    if(State("MapCell")!=cell)throw new InvalidOperationException("Map gesture did not reach cell "+cell);
                    File.AppendAllText(physicalCheckReport,"PASS actual raycast and drag 540x"+height+" cell="+cell+"\n");
                }
                if(State("FamilyPlan")!=1)throw new InvalidOperationException("Map route did not register family preparation.");
                foreach(int cell in new[]{10,7,6,3,0})foreach(var f in TimedDrag(token,board.position+new Vector3(cell%3*.22f,.065f,cell/3*.22f)))yield return f;
            }
            File.AppendAllText(physicalCheckReport,"PASS fresh opening, walked approach, 15 reachable token drags, three portrait ratios. Screenshots="+folder+"\n");
            YanYanaQA.SetGameView(540,960);
        }
    }
}
