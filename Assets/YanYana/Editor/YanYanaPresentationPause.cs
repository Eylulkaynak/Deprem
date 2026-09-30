// Editor authoring only: the player executes native component/graph operations.
using UnityEngine;
using System.Linq;
using UnityEngine.Playables;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        public static void RefreshPresentationPause()
        {
            flow=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().First(x=>x.name.StartsWith("01 Akış"));
            foreach(var machine in flow.GetComponents<ScriptMachine>().Where(m=>m.graph.title=="Efekt, karakter ve sekansları birlikte duraklat").ToArray())UnityEngine.Object.DestroyImmediate(machine);
            CreatePresentationPause();
        }
        static void CreatePresentationPause()
        {
            var g=new YanYanaGraphAuthor(flow,"Efekt, karakter ve sekansları birlikte duraklat");
            g.Initial("Frozen",false);
            var first=g.Branch(g.Event("PauseAllPhysical").trigger,Is(g,g.Var("Frozen"),false));
            var p=g.SetVar(first.ifTrue,"Frozen",true);
            int index=0;
            foreach(var particles in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID))
            {
                string key="Particle"+index++;g.Initial(key,false);
                p=g.SetVar(p,key,g.Get(typeof(ParticleSystem),"isPlaying",particles));
                p=g.Do(p,typeof(ParticleSystem),"Pause",particles,OneBool,false);
                var resume=g.Branch(g.Event("ResumePresentation").trigger,And(g,g.Var(key),g.Get(typeof(GameObject),"activeInHierarchy",particles.gameObject)));
                var resumed=g.Do(resume.ifTrue,typeof(ParticleSystem),"Play",particles,OneBool,false);g.SetVar(resumed,key,false);
            }
            foreach(var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID))
            {
                string key="Animator"+index++;g.Initial(key,1f);
                p=g.SetVar(p,key,g.Get(typeof(Animator),"speed",animator));p=g.Set(p,typeof(Animator),"speed",animator,0f);
                g.Set(g.Event("ResumePresentation").trigger,typeof(Animator),"speed",animator,g.Var(key));
            }
            foreach(var animation in UnityEngine.Object.FindObjectsByType<Animation>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID))
                foreach(AnimationState state in animation)
                {
                    string key="Clip"+index++;g.Initial(key,1f);
                    // Animation's indexer accessor produces invalid AOT stubs when invoked
                    // as get_Item. Native collection loops enumerate the same live states.
                    var freeze=g.Add(new ForEach());g.Bind(freeze.collection,animation);g.Link(p,freeze.enter);
                    var match=g.Branch(freeze.body,Is(g,g.Get(typeof(AnimationState),"name",freeze.currentItem),state.name));
                    var frozen=g.SetVar(match.ifTrue,key,g.Get(typeof(AnimationState),"speed",freeze.currentItem));
                    g.Set(frozen,typeof(AnimationState),"speed",freeze.currentItem,0f);p=freeze.exit;
                    var thaw=g.Add(new ForEach());g.Bind(thaw.collection,animation);g.Link(g.Event("ResumePresentation").trigger,thaw.enter);
                    var resume=g.Branch(thaw.body,Is(g,g.Get(typeof(AnimationState),"name",thaw.currentItem),state.name));
                    g.Set(resume.ifTrue,typeof(AnimationState),"speed",thaw.currentItem,g.Var(key));
                }
            foreach(var director in UnityEngine.Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID))
            {
                string key="Timeline"+index++;g.Initial(key,false);
                p=g.SetVar(p,key,Is(g,g.Get(typeof(PlayableDirector),"state",director),PlayState.Playing));p=g.Do(p,typeof(PlayableDirector),"Pause",director,NoArgs);
                var resume=g.Branch(g.Event("ResumePresentation").trigger,g.Var(key));var resumed=g.Do(resume.ifTrue,typeof(PlayableDirector),"Resume",director,NoArgs);g.SetVar(resumed,key,false);
            }
            g.SetVar(g.Event("ResumePresentation").trigger,"Frozen",false);g.Dirty();
        }
    }
}
