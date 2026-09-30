using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static YanYanaGraphAuthor main;
        static readonly Type[] NoArgs = Type.EmptyTypes;
        static readonly Type[] OneString = { typeof(string) };
        static readonly Type[] OneBool = { typeof(bool) };
        static readonly Type[] OneFloat = { typeof(float) };
        static ValueOutput Is(YanYanaGraphAuthor g, object a, object b) => g.Binary<Equal>(a, b);
        static ValueOutput And(YanYanaGraphAuthor g, object a, object b) => g.Binary<And>(a, b);
        static ValueOutput Or(YanYanaGraphAuthor g, object a, object b) => g.Binary<Or>(a, b);
        static ValueOutput Sum(YanYanaGraphAuthor g, object a, object b) => g.Binary<ScalarSubtract>(a, g.Binary<ScalarMultiply>(b, -1f));
        static ValueOutput Add(YanYanaGraphAuthor g, object a, object b) => g.Call(typeof(Vector3), "op_Addition", null, new[] { typeof(Vector3), typeof(Vector3) }, a, b).result;
        static ValueOutput Mul(YanYanaGraphAuthor g, object a, object b) => g.Call(typeof(Vector3), "op_Multiply", null, new[] { typeof(Vector3), typeof(float) }, a, b).result;
        static ValueOutput V2(YanYanaGraphAuthor g, object x, object y)
        {
            var a = g.Call(typeof(Vector2), "op_Multiply", null, new[] { typeof(Vector2), typeof(float) }, Vector2.right, x).result;
            var b = g.Call(typeof(Vector2), "op_Multiply", null, new[] { typeof(Vector2), typeof(float) }, Vector2.up, y).result;
            return g.Call(typeof(Vector2), "op_Addition", null, new[] { typeof(Vector2), typeof(Vector2) }, a, b).result;
        }
        static ValueOutput V3(YanYanaGraphAuthor g, object x, object y, object z) => Add(g, Add(g, Mul(g, Vector3.right, x), Mul(g, Vector3.up, y)), Mul(g, Vector3.forward, z));
        static ValueOutput Concat(YanYanaGraphAuthor g, object a, object b) => g.Call(typeof(string), "Concat", null, new[] { typeof(string), typeof(string) }, a, b).result;
        static ValueOutput PrefInt(YanYanaGraphAuthor g, object key, int value = 0) => g.Call(typeof(PlayerPrefs), "GetInt", null, new[] { typeof(string), typeof(int) }, key, value).result;
        static ValueOutput PrefString(YanYanaGraphAuthor g, object key, string value = "") => g.Call(typeof(PlayerPrefs), "GetString", null, new[] { typeof(string), typeof(string) }, key, value).result;
        static ControlOutput PutInt(YanYanaGraphAuthor g, ControlOutput before, object key, object value) => g.Do(before, typeof(PlayerPrefs), "SetInt", null, new[] { typeof(string), typeof(int) }, key, value);
        static ControlOutput PutString(YanYanaGraphAuthor g, ControlOutput before, object key, object value) => g.Do(before, typeof(PlayerPrefs), "SetString", null, new[] { typeof(string), typeof(string) }, key, value);
        static ControlOutput Text(YanYanaGraphAuthor g, ControlOutput before, TMP_Text target, object text)
        {
            var p=g.Set(before,typeof(TMP_Text),"text",target,text is string s?SafeText(s):text);
            if(target==lineText||target==goalText)
            {
                p=g.SetVar(p,"IdleSeconds",0f,flow);
                p=g.SetVar(p,"HintLevel",0,flow);
                p=g.Active(p,hintPanel,false);
            }
            if(target==lineText)p=g.Send(p,flow,"SpeakPhysicalLine",text);
            return p;
        }
        static ValueOutput Available(YanYanaGraphAuthor g) => And(g, Is(g, g.Var("Busy", flow), false), Is(g, g.Var("Paused", flow), false));
        static ControlOutput Released(YanYanaGraphAuthor g, ControlOutput before)
        {
            var wait = g.Add(new WaitUntilUnit());
            var mouse = g.Call(typeof(Input), "GetMouseButton", null, new[] { typeof(int) }, 0).result;
            g.Bind(wait.condition, And(g, Is(g, mouse, false), Is(g, g.Get(typeof(Input), "touchCount"), 0)));
            g.Link(before, wait.enter); return wait.exit;
        }

        static void CreateCampaign()
        {
            main = new YanYanaGraphAuthor(flow, "Yan Yana · durum, kayıt, karar ve rol olayları");
            foreach (string flag in campaign.flags) main.Initial(flag, 0);
            main.Initial("Busy", true); main.Initial("Paused", true); main.Initial("Current", "welcome"); main.Initial("Station", ""); main.Initial("Role", "Ada");
            main.Initial("IdleSeconds", 0f); main.Initial("PlaySeconds", 0f); main.Initial("Chapter", 0); main.Initial("ActiveGesture", "approach");
            main.Initial("ReducedMotion", true); main.Initial("Sound", true); main.Initial("Captions", true); main.Initial("Vibration", false);
            main.Initial("RoleTransition", false);
            var empty = Group("Başlangıçta boş etkinlik", interactions.transform); empty.SetActive(false);
            var emptyView = Group("Başlangıçta boş kamera", presentation.transform); emptyView.SetActive(false);
            main.Initial("ActiveStage", empty); main.Initial("ActiveView", emptyView); main.Initial("ActiveController", flow); main.Initial("ActiveTarget", empty);
            main.Initial("ActiveMover", movers["Ada"]);
            foreach (var beat in campaign.beats)
            {
                controllers[beat.id] = Group(beat.id + " · " + beat.title, flow.transform);
                var objects = Group(beat.id, interactions.transform); objects.SetActive(false); beatWorlds[beat.id] = objects;
                if (beat.actions.Length > 0) View(beat);
            }
            CreateFireActivities();
            foreach (var beat in campaign.beats) CreateBeat(beat);
            var go = main.Event("Go", arguments: 1);
            var path = main.SetVar(go.trigger, "Busy", true);
            var route = main.Add(new SwitchOnString { options = campaign.beats.Select(x => x.id).ToList() }); main.Bind(route.selector, go.argumentPorts[0]); main.Link(path, route.enter);
            foreach (var b in route.branches) main.Send(b.Value, controllers[b.Key], "Show");
            main.Send(route.@default, controllers["welcome"], "Show");
            var decision = main.Event("DecisionTaken", arguments: 2);
            path = main.SetVar(decision.trigger, "IdleSeconds", 0f);
            var sw = main.Add(new SwitchOnString { options = campaign.flags.ToList() }); main.Bind(sw.selector, decision.argumentPorts[0]); main.Link(path, sw.enter);
            foreach (var b in sw.branches)
            {
                var set = main.SetVar(b.Value, b.Key, decision.argumentPorts[1]); main.Send(set, flow, "ApplyWorld");
            }
            var activity = main.Event("ActivityFinished", arguments: 2);
            path = PutString(main, activity.trigger, SavePrefix + "lastActivity", activity.argumentPorts[0]);
            PutString(main, path, SavePrefix + "lastActivityResult", activity.argumentPorts[1]);
            main.Dirty();
        }

        static void CreateBeat(YYBeat beat)
        {
            var owner = controllers[beat.id]; var g = new YanYanaGraphAuthor(owner, beat.id + " · sahnede kurulmuş bölüm");
            var enter = g.Event("Show", true);
            if (!string.IsNullOrEmpty(beat.condition))
            {
                if (beat.condition == "ENDING")
                {
                    var detour = g.Branch(enter.trigger, Or(g, Is(g, g.Var("FacadeReported", flow), 1), Is(g, g.Var("FireAssisted", flow), 1)));
                    g.Send(detour.ifTrue, flow, "Go", "ending_detour_1");
                    var plan = g.Branch(detour.ifFalse, Is(g, g.Var("FamilyPlan", flow), 1)); g.Send(plan.ifTrue, flow, "Go", "ending_plan_1");
                    var neighbor = g.Branch(plan.ifFalse, Is(g, g.Var("NeighborTogether", flow), 1)); g.Send(neighbor.ifTrue, flow, "Go", "ending_neighbor_1"); g.Send(neighbor.ifFalse, flow, "Go", "ending_signal_1");
                }
                else
                {
                    var branch = g.Branch(enter.trigger, Is(g, g.Var(beat.condition, flow), 1)); g.Send(branch.ifTrue, flow, "Go", beat.yes); g.Send(branch.ifFalse, flow, "Go", beat.no);
                }
                g.Dirty(); return;
            }
            ControlOutput path = g.Do(enter.trigger, typeof(GameObject), "SetActive", g.Var("ActiveStage", flow), OneBool, false);
            path = g.Do(path, typeof(GameObject), "SetActive", g.Var("ActiveView", flow), OneBool, false);
            path = g.SetVar(path, "ActiveStage", beatWorlds[beat.id], flow); path = g.SetVar(path, "ActiveView", View(beat).gameObject, flow);
            path = g.SetVar(path, "ActiveController", owner, flow); path = g.SetVar(path, "Current", beat.id, flow); path = g.SetVar(path, "Chapter", beat.chapter, flow);
            path = g.SetVar(path, "IdleSeconds", 0f, flow); path = g.Active(path, hintPanel, false);
            path = Text(g, path, goalText, beat.title); path = Text(g, path, lineText, beat.line); path = Text(g, path, chapterText, campaign.chapters[beat.chapter].ToUpperInvariant());
            path = Text(g, path, roleText, beat.role == "Idil" ? "İdil" : beat.role);
            path = g.Set(path, typeof(Image), "sprite", portraitImage, portraits[beat.role]);
            path = g.SetVar(path, "ActiveGesture", beat.actions[0].gesture, flow);
            path = Text(g, path, gestureText, GestureCaption(beat.actions[0].gesture));
            path = g.Active(path, beatWorlds[beat.id], true); path = g.Active(path, View(beat).gameObject, true);
            var moved = g.Branch(path, Is(g, g.Var("Station", flow), beat.station));
            var at = stations[beat.station];
            var movePath = g.Do(moved.ifFalse, typeof(StoryPlayerMovement), "Warp", movers[beat.role], new[] { typeof(Vector3) }, at + new Vector3(0, 0, -2.1f));
            if (beat.role == "Ada")
                movePath = g.Do(movePath, typeof(StorySiblingFollower), "ReleaseAuthoredPose", follower, new[] { typeof(Vector3) }, at + new Vector3(1.2f, 0, -2.3f));
            movePath = g.SetVar(movePath, "Station", beat.station, flow);
            var merge = g.Add(new Unity.VisualScripting.Sequence { outputCount = 1 });
            g.Link(movePath, merge.enter); g.Link(moved.ifTrue, merge.enter); path = merge.multiOutputs[0];
            path = g.Send(path, flow, "EnterRole", beat.role);
            path = g.Send(path, flow, "ApplyWorld");
            path = ApplyBeatEffect(g, path, beat);
            path = g.Send(path, flow, "CommitCheckpoint", beat.id);
            if (sounds.TryGetValue(beat.id, out var spoken))
            {
                path = g.Do(path, typeof(AudioSource), "Stop", speech, NoArgs);
                path = g.Set(path, typeof(AudioSource), "clip", speech, spoken);
                path = g.Do(path, typeof(AudioSource), "Play", speech, NoArgs);
            }
            path = g.Wait(path, .12f); path = Released(g, path);
            var roleReady = g.Add(new WaitUntilUnit()); g.Bind(roleReady.condition, Is(g, g.Var("RoleTransition", flow), false)); g.Link(path, roleReady.enter); path = roleReady.exit;
            path = g.SetVar(path, "Busy", false, flow);
            if (beat.effect == "complete") path = g.Send(path, flow, "FinishAdventure");
            targets[beat.id] = new List<GameObject>();
            for (int i = 0; i < beat.actions.Length; i++) CreateAction(beat, beat.actions[i], i);
            if (targets[beat.id].Count > 0) g.SetVar(path, "ActiveTarget", targets[beat.id][0], flow);
            g.Dirty();
        }

        static string GestureCaption(string gesture)
        {
            switch (gesture)
            {
                case "drag": return "Nesneyi tut · işaretli yere sürükle";
                case "hold": return "Dokun ve basılı tut";
                case "swipe": return "Parmağını aşağı kaydır";
                case "approach": return "İşaretli zemine dokun · birlikte yürü";
                case "spray": return "Basılı tut · suyu alevlere yönlendir";
                default: return "Nesneye dokun · ne olduğunu gör";
            }
        }

        static void ConfigurePersistence()
        {
            var boot = main.Add(new Unity.VisualScripting.Start()); ControlOutput path = boot.trigger;
            path = main.Set(path, typeof(Application), "targetFrameRate", null, 60);
            path = main.Set(path, typeof(Screen), "orientation", null, ScreenOrientation.Portrait);
            path = main.Set(path, typeof(Screen), "sleepTimeout", null, SleepTimeout.NeverSleep);
            foreach (string flag in campaign.flags) path = main.SetVar(path, flag, PrefInt(main, SavePrefix + flag));
            path = main.SetVar(path, "Current", PrefString(main, SavePrefix + "checkpoint", "welcome"));
            path = main.SetVar(path, "PlaySeconds", main.Call(typeof(PlayerPrefs), "GetFloat", null, new[] { typeof(string), typeof(float) }, SavePrefix + "playSeconds", 0f).result);
            path = main.SetVar(path, "ReducedMotion", Is(main, PrefInt(main, SavePrefix + "option.motion", 1), 1));
            path = main.SetVar(path, "Sound", Is(main, PrefInt(main, SavePrefix + "option.sound", 1), 1));
            path = main.SetVar(path, "Captions", Is(main, PrefInt(main, SavePrefix + "option.captions", 1), 1));
            path = main.SetVar(path, "Vibration", Is(main, PrefInt(main, SavePrefix + "option.vibration", 0), 1));
            path = main.Send(path, flow, "ApplyOptions");
            path = main.Active(path, View(campaign.beats[0]).gameObject, true);
            path = main.SetVar(path, "ActiveView", View(campaign.beats[0]).gameObject);
            path = main.Do(path, typeof(StorySiblingFollower), "SetFollowing", follower, OneBool, true);
            var save = main.Event("CommitCheckpoint", arguments: 1);
            path = PutString(main, save.trigger, SavePrefix + "checkpoint", save.argumentPorts[0]);
            foreach (string flag in campaign.flags)
            {
                path = PutInt(main, path, SavePrefix + flag, main.Var(flag));
                var key = Concat(main, Concat(main, SavePrefix + "snap.", save.argumentPorts[0]), "." + flag);
                path = PutInt(main, path, key, main.Var(flag));
            }
            path = PutInt(main, path, Concat(main, SavePrefix + "visited.", save.argumentPorts[0]), 1);
            path = main.Do(path, typeof(PlayerPrefs), "SetFloat", null, new[] { typeof(string), typeof(float) }, SavePrefix + "playSeconds", main.Var("PlaySeconds"));
            main.Do(path, typeof(PlayerPrefs), "Save", null, NoArgs);
            var replay = main.Event("Replay", arguments: 1);
            var visited = main.Branch(replay.trigger, Is(main, PrefInt(main, Concat(main, SavePrefix + "visited.", replay.argumentPorts[0])), 1)); path = visited.ifTrue;
            foreach (string flag in campaign.flags)
            {
                var key = Concat(main, Concat(main, SavePrefix + "snap.", replay.argumentPorts[0]), "." + flag);
                path = PutInt(main, path, SavePrefix + flag, PrefInt(main, key));
            }
            path = PutString(main, path, SavePrefix + "checkpoint", replay.argumentPorts[0]);
            // Replay clears downstream visited markers; previous flags are restored from the selected entry snapshot.
            var replaySwitch = main.Add(new SwitchOnString { options = campaign.beats.Select(b => b.id).ToList() }); main.Bind(replaySwitch.selector, replay.argumentPorts[0]); main.Link(path, replaySwitch.enter);
            for (int i = 0; i < campaign.beats.Length; i++)
            {
                var branch = replaySwitch.branches[i].Value;
                for (int j = i; j < campaign.beats.Length; j++) branch = main.Do(branch, typeof(PlayerPrefs), "DeleteKey", null, OneString, SavePrefix + "visited." + campaign.beats[j].id);
                branch = main.Do(branch, typeof(PlayerPrefs), "Save", null, NoArgs);
                branch = main.Set(branch, typeof(Time), "timeScale", null, 1f);
                main.Do(branch, typeof(SceneManager), "LoadScene", null, OneString, "YanYana_Adventure");
            }
            var newRun = main.Event("NewAdventure"); path = newRun.trigger;
            foreach (string flag in campaign.flags) { path = main.SetVar(path, flag, 0); path = PutInt(main, path, SavePrefix + flag, 0); }
            foreach (var b in campaign.beats) path = main.Do(path, typeof(PlayerPrefs), "DeleteKey", null, OneString, SavePrefix + "visited." + b.id);
            path = PutString(main, path, SavePrefix + "checkpoint", "welcome"); path = main.SetVar(path, "PlaySeconds", 0f);
            path = main.Active(path, titlePanel, false); path = main.Active(path, endingPanel, false); path = main.SetVar(path, "Paused", false);
            path = main.Set(path, typeof(Time), "timeScale", null, 1f); main.Send(path, flow, "Go", "welcome");
            var pause = main.Add(new Unity.VisualScripting.OnApplicationPause()); main.Send(pause.trigger, flow, "PauseAdventure");
            var quit = main.Add(new Unity.VisualScripting.OnApplicationQuit()); main.Do(quit.trigger, typeof(PlayerPrefs), "Save", null, NoArgs);
            main.Dirty();
        }

        static void ConfigureMenus()
        {
            var path = ButtonEvent(main, "Continue", "Continue"); path = main.Active(path, titlePanel, false); path = main.SetVar(path, "Paused", false); main.Send(path, flow, "Go", main.Var("Current"));
            path = ButtonEvent(main, "New", "New"); main.Send(path, flow, "NewAdventure");
            path = ButtonEvent(main, "Pause", "Pause"); main.Send(path, flow, "PauseAdventure");
            var pause = main.Event("PauseAdventure"); path = main.SetVar(pause.trigger, "Paused", true);
            path = main.Set(path, typeof(Time), "timeScale", null, 0f); path = main.Active(path, pausePanel, true);
            foreach (var m in movers.Values) path = main.Do(path, typeof(StoryPlayerMovement), "SetStoryInputLocked", m, OneBool, true);
            foreach (var audio in new[] { ambience, feedback, speech }) path = main.Do(path, typeof(AudioSource), "Pause", audio, NoArgs);
            foreach (var item in fireManagers)
            {
                path = main.SetVar(path, "PausedFire." + item.Key, main.Get(typeof(Behaviour), "enabled", item.Value));
                path = main.Set(path, typeof(Behaviour), "enabled", item.Value, false);
            }
            path = ButtonEvent(main, "Resume", "Resume"); path = main.Active(path, pausePanel, false); path = main.Set(path, typeof(Time), "timeScale", null, 1f); path = main.SetVar(path, "Paused", false);
            foreach (var audio in new[] { ambience, feedback, speech }) path = main.Do(path, typeof(AudioSource), "UnPause", audio, NoArgs);
            foreach (var item in fireManagers) path = main.Set(path, typeof(Behaviour), "enabled", item.Value, main.Var("PausedFire." + item.Key));
            path = main.Send(path, flow, "EnterRole", main.Var("Role"));
            // All pointer gestures require a new down after pause; their local update clears held state when paused.
            foreach (string option in new[] { "ReducedMotion", "Sound", "Captions", "Vibration" })
            {
                path = ButtonEvent(main, option, option); path = main.SetVar(path, option, Is(main, main.Var(option), false)); main.Send(path, flow, "ApplyOptions");
            }
            var settings = main.Event("ApplyOptions"); path = settings.trigger;
            foreach (string option in new[] { "ReducedMotion", "Sound", "Captions", "Vibration" })
            {
                var branch = main.Branch(path, main.Var(option));
                string id = option == "ReducedMotion" ? "motion" : option == "Sound" ? "sound" : option == "Captions" ? "captions" : "vibration";
                string yes = option == "ReducedMotion" ? "Kamera sarsıntısı: sakin" : option == "Sound" ? "Ses: açık" : option == "Captions" ? "Altyazı: açık" : "Titreşim: açık";
                string no = option == "ReducedMotion" ? "Kamera sarsıntısı: doğal" : option == "Sound" ? "Ses: kapalı" : option == "Captions" ? "Altyazı: kapalı" : "Titreşim: kapalı";
                var a = Text(main, branch.ifTrue, buttons[option].GetComponentInChildren<TMP_Text>(), yes); a = PutInt(main, a, SavePrefix + "option." + id, 1);
                var b = Text(main, branch.ifFalse, buttons[option].GetComponentInChildren<TMP_Text>(), no); b = PutInt(main, b, SavePrefix + "option." + id, 0);
                var sequence = main.Add(new Unity.VisualScripting.Sequence { outputCount = 1 }); main.Link(a, sequence.enter); main.Link(b, sequence.enter); path = sequence.multiOutputs[0];
            }
            path = main.Set(path, typeof(AudioListener), "volume", null, main.Call(typeof(Convert), "ToSingle", null, OneBool, main.Var("Sound")).result);
            path = main.Active(path, lineText.gameObject, main.Var("Captions"));
            path = ButtonEvent(main, "ReplayPlan", "ReplayPlan"); main.Send(path, flow, "Replay", "plan_choice");
            path = ButtonEvent(main, "ReplayBag", "ReplayBag"); main.Send(path, flow, "Replay", "flashlight_choice");
            path = ButtonEvent(main, "Hint", "Hint"); main.Send(path, flow, "ShowHint");
            main.Dirty();
        }
    }
}
