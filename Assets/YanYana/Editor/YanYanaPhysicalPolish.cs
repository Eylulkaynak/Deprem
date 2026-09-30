using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using Unity.Cinemachine;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static AnimationClip CreateFamilyWavePose()
        {
            var source=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Story/Animations/Generated/ChildNeutralIdle.anim");string path=Root+"Animation/FamilyWave.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(!clip){clip=UnityEngine.Object.Instantiate(source);AssetDatabase.CreateAsset(clip,path);}else EditorUtility.CopySerialized(source,clip);
            clip.name="Aile işareti · el salla";
            // Calibrated through actual Animator playback on all four family
            // avatars: the wrist rises beside the head without a bent-down palm.
            foreach(var part in new[]{("Right Arm Down-Up",1f),("Right Arm Front-Back",-.2f),("Right Arm Twist In-Out",.4f),("Right Forearm Stretch",0f)})
            {
                if(!HumanTrait.MuscleName.Contains(part.Item1))throw new InvalidOperationException("Unknown humanoid muscle: "+part.Item1);
                clip.SetCurve("",typeof(Animator),part.Item1,new AnimationCurve(new Keyframe(0,0),new Keyframe(.4f,part.Item2),new Keyframe(1.8f,part.Item2),new Keyframe(2.3f,0)));
            }
            clip.SetCurve("",typeof(Animator),"Right Hand In-Out",new AnimationCurve(new Keyframe(0,0),new Keyframe(.6f,-.18f),new Keyframe(.9f,.18f),new Keyframe(1.2f,-.18f),new Keyframe(1.5f,.18f),new Keyframe(2.3f,0)));
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
        }
        static void AddFlameTuft(Transform parent)
        {
            var flame=Model("FlameTuft",parent,new Vector3(0,.08f,0),.78f,0);var anim=flame.AddComponent<Animation>();string path=Root+"Animation/OriginalFlameFlicker.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            PrefabUtility.UnpackPrefabInstance(flame,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.ClearCurves();clip.legacy=true;clip.name="Kıvrılan üç alev dili";clip.wrapMode=WrapMode.Loop;
            for(int i=0;i<3;i++)
            {
                var imported=flame.GetComponentsInChildren<Transform>().First(t=>t.name=="FlameLobe_"+i);
                var pivot=Group("AlevHareketi_"+i,flame.transform);imported.SetParent(pivot.transform,true);
                foreach(string property in new[]{"localScale.x","localScale.y","localScale.z","localEulerAnglesRaw.z"})
                {
                    var keys=new Keyframe[7];for(int k=0;k<keys.Length;k++)
                    {
                        float wave=Mathf.Sin((k/6f+i*.31f)*Mathf.PI*2);float value=property=="localScale.y"?1+wave*.15f:property=="localEulerAnglesRaw.z"?wave*4f:1-wave*.065f;
                        keys[k]=new Keyframe(k*.2f,value);
                    }
                    clip.SetCurve(pivot.name,typeof(Transform),property,new AnimationCurve(keys));
                }
            }
            EditorUtility.SetDirty(clip);
            foreach(var renderer in flame.GetComponentsInChildren<Renderer>())
            {
                string layer=renderer.name.StartsWith("WarmHeart")?"Heart":renderer.name.StartsWith("GoldFlame")?"Gold":"Ember";
                string materialPath=Root+"Art/Materials/OriginalFlame"+layer+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,materialPath);}
                material.SetColor("_BaseColor",layer=="Heart"?new Color(1,.86f,.40f):layer=="Gold"?new Color(1,.61f,.10f):new Color(1,.30f,.055f));EditorUtility.SetDirty(material);
                renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            // Use the serialized clip name: editor-only AddClip aliases do not
            // survive scene reload and cannot be targeted by pause graphs.
            anim.AddClip(clip,clip.name);anim.clip=clip;anim.playAutomatically=true;anim.wrapMode=WrapMode.Loop;
        }
        static void CreatePhysicalPolish()
        {
            foreach(var visual in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Alev, duman ve ışık"||t.name=="Bekleyen yangın odağı").ToArray())AddFlameTuft(visual);
            var wave=main.Event("FinishPhysicalAdventure");var p=wave.trigger;foreach(string who in new[]{"Ada","Efe","Derya","Emre"})p=main.Do(p,typeof(Animator),"SetInteger",cast[who].GetComponentInChildren<Animator>(),new[]{typeof(string),typeof(int)},"Pose",2);
            p=main.Event("ShowPhysicalOutcome").trigger;foreach(string who in new[]{"Ada","Efe","Derya","Emre"})p=main.Do(p,typeof(Animator),"SetInteger",cast[who].GetComponentInChildren<Animator>(),new[]{typeof(string),typeof(int)},"Pose",0);
            var dust=Group("Sarsıntıda kısa stilize toz",world.transform,new Vector3(0,2.4f,0)).AddComponent<ParticleSystem>();var m=dust.main;m.loop=false;m.duration=2;m.startLifetime=1.8f;m.startSpeed=.35f;m.startSize=.28f;m.startColor=new Color(.75f,.66f,.51f,.45f);m.gravityModifier=.035f;m.playOnAwake=false;m.maxParticles=35;var emission=dust.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,25)});var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(5.5f,.5f,5.5f);dust.GetComponent<ParticleSystemRenderer>().sharedMaterial=softParticle;
            main.Do(main.Event("BeginQuake").trigger,typeof(ParticleSystem),"Play",dust,NoArgs);
            var follow=exploreCamera.GetComponent<CinemachineFollow>();var offset=follow.FollowOffset;
            var shake=new YanYanaGraphAuthor(flow,"İsteğe bağlı düşük genlikli kamera sarsıntısı");var frame=shake.Add(new Unity.VisualScripting.Update());var moving=shake.Branch(frame.trigger,And(shake,Is(shake,shake.Var("ReducedMotion",flow),false),Or(shake,Is(shake,shake.Var("Phase",flow),1),Is(shake,shake.Var("Phase",flow),31))));
            var sway=shake.Binary<ScalarMultiply>(shake.Call(typeof(Mathf),"Sin",null,OneFloat,shake.Binary<ScalarMultiply>(shake.Get(typeof(Time),"time"),18f)).result,.045f);shake.Set(moving.ifTrue,typeof(CinemachineFollow),"FollowOffset",follow,Add(shake,offset,Mul(shake,Vector3.right,sway)));shake.Set(moving.ifFalse,typeof(CinemachineFollow),"FollowOffset",follow,offset);shake.Dirty();
            var radioAudio=Group("Radyonun gerçek frekans sesi",presentation.transform).AddComponent<AudioSource>();radioAudio.loop=true;radioAudio.playOnAwake=false;radioAudio.volume=.18f;if(sounds.TryGetValue("radio_static",out var noise))radioAudio.clip=noise;
            var radio=new YanYanaGraphAuthor(flow,"Frekans yanlışsa cızırtı duy; netleşince konuşma duy");var update=radio.Add(new Unity.VisualScripting.Update());var noisy=And(radio,AtWork(radio,"radio"),Is(radio,radio.Var("RadioReady",flow),0));var play=radio.Branch(update.trigger,noisy);var stopped=radio.Branch(play.ifTrue,Is(radio,radio.Get(typeof(AudioSource),"isPlaying",radioAudio),false));radio.Do(stopped.ifTrue,typeof(AudioSource),"Play",radioAudio,NoArgs);radio.Do(play.ifFalse,typeof(AudioSource),"Stop",radioAudio,NoArgs);radio.Dirty();
            CreatePhysicalHints();
            var safeArea=new YanYanaGraphAuthor(flow,"Çentik ve sistem çubuklarından uzak arayüz");safeArea.Initial("Width",0);safeArea.Initial("Height",0);var resized=safeArea.Add(new Unity.VisualScripting.Update());
            var changed=safeArea.Branch(resized.trigger,Or(safeArea,Is(safeArea,Is(safeArea,safeArea.Var("Width"),safeArea.Get(typeof(Screen),"width")),false),Is(safeArea,Is(safeArea,safeArea.Var("Height"),safeArea.Get(typeof(Screen),"height")),false)));
            var area=safeArea.Get(typeof(Screen),"safeArea");p=safeArea.Set(changed.ifTrue,typeof(RectTransform),"anchorMin",safeRect,V2(safeArea,safeArea.Binary<ScalarDivide>(safeArea.Get(typeof(UnityEngine.Rect),"x",area),safeArea.Get(typeof(Screen),"width")),safeArea.Binary<ScalarDivide>(safeArea.Get(typeof(UnityEngine.Rect),"y",area),safeArea.Get(typeof(Screen),"height"))));p=safeArea.Set(p,typeof(RectTransform),"anchorMax",safeRect,V2(safeArea,safeArea.Binary<ScalarDivide>(safeArea.Get(typeof(UnityEngine.Rect),"xMax",area),safeArea.Get(typeof(Screen),"width")),safeArea.Binary<ScalarDivide>(safeArea.Get(typeof(UnityEngine.Rect),"yMax",area),safeArea.Get(typeof(Screen),"height"))));p=ResizePhysicalLenses(safeArea,p);p=safeArea.SetVar(p,"Width",safeArea.Get(typeof(Screen),"width"));safeArea.SetVar(p,"Height",safeArea.Get(typeof(Screen),"height"));safeArea.Dirty();
        }
        static ControlOutput ResizePhysicalLenses(YanYanaGraphAuthor g,ControlOutput before)
        {
            var factor=g.Call(typeof(Mathf),"Max",null,new[]{typeof(float),typeof(float)},1f,g.Binary<ScalarMultiply>(g.Binary<ScalarDivide>(g.Get(typeof(Screen),"height"),g.Get(typeof(Screen),"width")),9f/16f)).result;
            var p=before;
            foreach(var view in workCameras.Values.Concat(new[]{exploreCamera}))
            {
                bool ortho=view.Lens.ModeOverride==LensSettings.OverrideModes.Orthographic;
                object size=ortho?(object)g.Binary<ScalarMultiply>(view.Lens.OrthographicSize,factor):g.Binary<ScalarMultiply>(g.Call(typeof(Mathf),"Atan",null,OneFloat,g.Binary<ScalarMultiply>(Mathf.Tan(view.Lens.FieldOfView*.5f*Mathf.Deg2Rad),factor)).result,2f*Mathf.Rad2Deg);
                var setter=g.Add(new SetMember(new Member(typeof(LensSettings),ortho?"OrthographicSize":"FieldOfView")){chainable=true});g.Bind(setter.target,g.Get(typeof(CinemachineCamera),"Lens",view));g.Bind(setter.input,size);g.Link(p,setter.assign);
                p=g.Set(setter.assigned,typeof(CinemachineCamera),"Lens",view,setter.targetOutput);
            }
            return p;
        }
        static void CreatePhysicalHints()
        {
            main.Initial("HintLevel",0);main.Initial("LastWorkspace","");
            var idle=main.Add(new Unity.VisualScripting.Update());var counting=main.Branch(idle.trigger,And(main,Is(main,main.Var("Paused"),false),main.Binary<Less>(main.Var("Phase"),8)));var p=main.SetVar(counting.ifTrue,"PlaySeconds",Sum(main,main.Var("PlaySeconds"),main.Get(typeof(Time),"deltaTime")));p=main.SetVar(p,"IdleSeconds",Sum(main,main.Var("IdleSeconds"),main.Get(typeof(Time),"deltaTime")));
            var input=main.Call(typeof(Input),"GetMouseButton",null,new[]{typeof(int)},0).result;var touched=main.Branch(p,Or(main,input,main.Binary<Greater>(main.Get(typeof(Input),"touchCount"),0)));p=main.SetVar(touched.ifTrue,"IdleSeconds",0f);p=main.SetVar(p,"HintLevel",0);main.Active(p,hintPanel,false);
            var support=main.Branch(touched.ifFalse,And(main,Available(main),And(main,main.Binary<Greater>(main.Var("IdleSeconds"),45f),Is(main,main.Var("HintLevel"),0))));p=main.SetVar(support.ifTrue,"HintLevel",1);p=main.Active(p,hintPanel,true);Text(main,p,hintText,"Bir hareketi birlikte görelim.");
            p=ButtonEvent(main,"Hint","PhysicalHint");p=main.Active(p,hintPanel,false);var selector=main.Add(new SwitchOnString{options=new System.Collections.Generic.List<string>{"flashlight","bag","radio","map","bridge","hose","aid","family","bagfit","supplyWater","supplyFood","relief","broadcast","sibling"}});main.Bind(selector.selector,main.Var("Workspace"));main.Link(p,selector.enter);
            foreach(var branch in selector.branches)
            {
                string hint;
                switch(branch.Key)
                {
                    case "flashlight":hint="Efe: Pillerin artı uçlarını yuvadaki işaretlerle karşılaştıralım.";break;
                    case "bag":hint="Ada: Eşyayı seçip çevirelim. Boş bölmelere yerleştirelim.";break;
                    case "bagfit":hint="Derya: Fermuarın izini takip et. Askıları çizgili yere getir.";break;
                    case "radio":hint="Efe: Düğmeyi yavaş döndür. Cızırtının azaldığı yeri bul.";break;
                    case "map":hint="Emre: Aile taşını yan yana açık karelerden geçir.";break;
                    case "bridge":hint="Efe: Tahtaları suyun iki kıyısı arasına yerleştirelim.";break;
                    case "hose":hint="İdil: Bağlantıyı oturt. Sonra kırmızı vanayı çevir.";break;
                    case "aid":hint="Bora: Kişinin ihtiyacıyla masa simgesi aynı olmalı.";break;
                    case "supplyWater":case "supplyFood":hint="Derya: Ambalajı çevir. Tarihi ve açık olup olmadığını karşılaştır.";break;
                    case "relief":hint="Bora: Suyu mindere, yiyeceği tabağa ulaştır.";break;
                    case "broadcast":hint="Bora: Resmî yayını bul. Sonra dinleme düğmesini kullan.";break;
                    case "sibling":hint="Ada: İşaretli elimizi Efe’nin uzattığı ele götürelim.";break;
                    default:hint="Bora: Kartı aç. Aileyi ve ağaç işaretini eşleştir.";break;
                }
                Text(main,branch.Value,lineText,hint);
            }
            var introHint=main.Branch(selector.@default,IntroActive(main));main.Send(introHint.ifTrue,flow,"IntroHint");Text(main,introHint.ifFalse,lineText,"Efe: Etrafımıza bakalım. Bir nesneye yaklaşarak başlayabiliriz.");
            CreatePhysicalGestureCue();
            // Keep overlays above subsequently authored transparent pointer surfaces.
            hintPanel.transform.SetAsLastSibling();rolePanel.transform.SetAsLastSibling();titlePanel.transform.SetAsLastSibling();endingPanel.transform.SetAsLastSibling();pausePanel.transform.SetAsLastSibling();
        }

        static void CreatePhysicalGestureCue()
        {
            var cue=Rect("On saniyede dikkat, yirmi beşte hareket",safeRect,Vector2.zero,Vector2.zero,new Vector2(-34,-34),new Vector2(34,34));
            var halo=Panel("Dokunma alanı",cue,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(1,.76f,.30f,.42f));
            var hand=Rect("Hareketi gösteren el",cue,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            Panel("Avuç",hand,new Vector2(.43f,.06f),new Vector2(.84f,.51f),Vector2.zero,Vector2.zero,Paper);
            Panel("İşaret parmağı",hand,new Vector2(.43f,.38f),new Vector2(.58f,.91f),Vector2.zero,Vector2.zero,Paper);
            Panel("Başparmak",hand,new Vector2(.28f,.15f),new Vector2(.48f,.42f),Vector2.zero,Vector2.zero,Paper);
            cue.gameObject.SetActive(false);
            var g=new YanYanaGraphAuthor(flow,"İlerleme yoksa nesneyi göster; ardından kısa jest göster");var frame=g.Add(new Unity.VisualScripting.Update());
            var active=And(g,Available(g),And(g,g.Binary<Greater>(g.Var("IdleSeconds",flow),10f),g.Binary<Less>(g.Var("Phase",flow),8)));
            var p=g.Active(frame.trigger,cue.gameObject,active);p=g.Active(p,hand.gameObject,g.Binary<Greater>(g.Var("IdleSeconds",flow),25f));
            var cycle=g.Call(typeof(Mathf),"Sin",null,OneFloat,g.Binary<ScalarMultiply>(g.Get(typeof(Time),"time"),2.5f)).result;
            p=g.Set(p,typeof(Transform),"localScale",halo.transform,Mul(g,Vector3.one,Sum(g,1f,g.Binary<ScalarMultiply>(cycle,.10f))));
            var shown=g.Branch(p,active);var choose=g.Add(new SwitchOnString{options=workCameras.Keys.ToList()});g.Bind(choose.selector,g.Var("Workspace",flow));g.Link(shown.ifTrue,choose.enter);
            foreach(var branch in choose.branches)
            {
                string key=branch.Key;Vector3 point=key=="flashlight"?anchors["Anchor_FlashlightWork"].position:key=="bag"?anchors["Anchor_BagWork"].position:key=="radio"?anchors["Anchor_RadioWork"].position:key=="map"?anchors["Anchor_FamilyMap"].position:key=="family"?familyDesk.transform.position:key=="relief"?aidCenter+new Vector3(-1,.02f,-.3f):key=="aid"?aidCenter+new Vector3(-2.2f,1.75f,2.15f):key=="hose"?street["Idil"].position+new Vector3(.7f,.12f,-.5f):key.StartsWith("fire")?fireStands[int.Parse(key.Substring(4))]+new Vector3(0,.5f,-2.2f):anchors["Anchor_Comfort"].position;
                object target=point;
                if(key=="sibling")target=g.Get(typeof(Transform),"position",physicalItems["SiblingHandCue"].transform);
                if(key=="intro"||key=="introDrop")target=g.Get(typeof(Transform),"position",physicalItems["IntroKit"].transform);
                if(key.StartsWith("supply"))target=anchors["Anchor_RadioWork"].position+new Vector3(0,.15f,-.1f);
                if(key=="relief")target=aidCenter+new Vector3(0,.07f,2.3f);
                if(key=="bagfit")
                {
                    var strap=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Get(typeof(Transform),"position",physicalItems["BagStrapCueLeft"].transform),g.Get(typeof(Transform),"position",physicalItems["BagStrapCueRight"].transform),g.Call(typeof(Convert),"ToSingle",null,OneBool,Is(g,g.Var("BagStrapLeft",flow),1)).result).result;
                    target=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},g.Get(typeof(Transform),"position",physicalItems["BagZipperCue"].transform),strap,g.Call(typeof(Convert),"ToSingle",null,OneBool,g.Binary<GreaterOrEqual>(g.Var("BagZipStep",flow),6)).result).result;
                }
                var screen=g.Call(typeof(Camera),"WorldToScreenPoint",camera,new[]{typeof(Vector3)},target).result;
                var shift=g.Binary<ScalarMultiply>(cycle,18f);g.Set(branch.Value,typeof(Transform),"position",cue,Add(g,screen,V3(g,shift,8f,0f)));
            }
            var phases=g.Add(new SwitchOnInteger{options=new System.Collections.Generic.List<int>{0,1,2,3,31,4,6,7}});g.Bind(phases.selector,g.Var("Phase",flow));g.Link(choose.@default,phases.enter);
            foreach(var branch in phases.branches)
            {
                object point=branch.Key==0?physicalFlashlight.transform.position:branch.Key==1?anchors["Anchor_Comfort"].position:branch.Key==3?street["Yusuf"].position+Vector3.up:branch.Key==4?street["Idil"].position+Vector3.up:branch.Key==6?street["Aid"].position+Vector3.up:g.Get(typeof(Transform),"position",cast[branch.Key==2?"Efe":"Ada"].transform);
                if(branch.Key==0)
                {
                    var walk=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},physicalItems["BagWalkCue0"].transform.position,physicalItems["BagWalkCue1"].transform.position,g.Call(typeof(Convert),"ToSingle",null,OneBool,Is(g,g.Var("BagCarryStep",flow),1)).result).result;
                    point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},point,walk,g.Call(typeof(Convert),"ToSingle",null,OneBool,Is(g,g.Var("BagCarryActive",flow),1)).result).result;
                    point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},point,g.Var("IntroCuePosition",flow),g.Call(typeof(Convert),"ToSingle",null,OneBool,Is(g,g.Var("IntroDone",flow),0)).result).result;
                }
                if(branch.Key==2)
                {
                    var exit=anchors["Anchor_Exit"].position+Vector3.up;
                    point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},point,exit,g.Call(typeof(Convert),"ToSingle",null,OneBool,Is(g,g.Var("SiblingChecked",flow),1)).result).result;
                }
                if(branch.Key==1)
                {
                    var animator=cast["Ada"].GetComponentInChildren<Animator>();point=g.Get(typeof(Transform),"position",cast["Ada"].transform);
                    foreach(var step in new[]{(2,HumanBodyBones.RightHand),(3,HumanBodyBones.LeftHand)})point=g.Call(typeof(Vector3),"Lerp",null,new[]{typeof(Vector3),typeof(Vector3),typeof(float)},point,g.Get(typeof(Transform),"position",animator.GetBoneTransform(step.Item2)),g.Call(typeof(Convert),"ToSingle",null,OneBool,Is(g,g.Var("CoverStage",flow),step.Item1)).result).result;
                }
                var screenPoint=g.Call(typeof(Camera),"WorldToScreenPoint",camera,new[]{typeof(Vector3)},point).result;
                if(branch.Key==0)
                {
                    var intro=g.Branch(branch.Value,IntroActive(g));
                    var x=g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},g.Get(typeof(Vector3),"x",screenPoint),66f,g.Binary<ScalarSubtract>(g.Get(typeof(Screen),"width"),66f)).result;
                    var y=g.Call(typeof(Mathf),"Clamp",null,new[]{typeof(float),typeof(float),typeof(float)},g.Get(typeof(Vector3),"y",screenPoint),320f,g.Binary<ScalarSubtract>(g.Get(typeof(Screen),"height"),180f)).result;
                    g.Set(intro.ifTrue,typeof(Transform),"position",cue,V3(g,x,y,0f));g.Set(intro.ifFalse,typeof(Transform),"position",cue,screenPoint);
                }
                else g.Set(branch.Value,typeof(Transform),"position",cue,screenPoint);
            }
            g.Dirty();
        }
    }
}
