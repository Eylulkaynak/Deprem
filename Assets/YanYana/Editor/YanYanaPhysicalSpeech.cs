using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    [Serializable] public class YYPhysicalVoiceList { public YYPhysicalVoice[] lines; }
    [Serializable] public class YYPhysicalVoice { public string key; public string line; public bool ready; }
    public static partial class YanYanaAdventureBuilder
    {
        static void CreatePhysicalSpeech()
        {
            var data=JsonUtility.FromJson<YYPhysicalVoiceList>("{\"lines\":"+File.ReadAllText("ArtDirection/YanYana/Audio/Physical/manifest.json")+"}");
            var lines=data.lines.Where(x=>x.ready&&sounds.ContainsKey(x.key)).ToArray();main.Initial("LastSpokenLine","");
            var speak=main.Event("SpeakPhysicalLine",arguments:1);
            var allowed=main.Branch(speak.trigger,And(main,Is(main,Is(main,main.Var("LastSpokenLine"),speak.argumentPorts[0]),false),And(main,main.Var("Sound"),Is(main,main.Var("Paused"),false))));
            var p=main.SetVar(allowed.ifTrue,"LastSpokenLine",speak.argumentPorts[0]);p=main.Do(p,typeof(AudioSource),"Stop",speech,NoArgs);
            var pick=main.Add(new SwitchOnString{options=lines.Select(x=>x.line).ToList()});main.Bind(pick.selector,speak.argumentPorts[0]);main.Link(p,pick.enter);
            for(int i=0;i<lines.Length;i++){var q=main.Set(pick.branches[i].Value,typeof(AudioSource),"clip",speech,sounds[lines[i].key]);main.Do(q,typeof(AudioSource),"Play",speech,NoArgs);}
        }
    }
}
