using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deprem.Learning.Editor
{
    public static class LearningOnlineRuntimeVerification
    {
        private const string Folder = ".codex_tmp/learning-online";
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly MethodInfo Click = typeof(Clickable).GetMethod("SimulateSingleClick",Private);
        private static LearningAppController App => UnityEngine.Object.FindFirstObjectByType<LearningAppController>();
        private static LearningOnlineClient Client => App.GetComponent<LearningOnlineClient>();
        private static VisualElement UI => App.GetComponent<UIDocument>().rootVisualElement;
        private static LearningOnlineClient peer;
        private static bool running;
        private static string originalSave, runFolder;
        private static readonly List<string> errors = new List<string>();

        [MenuItem("Tools/Deprem App/Review/Run Online Verification")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Enter Play mode and run one online verification at a time.");
            originalSave = LearningProgress.Current.SavePath;
            runFolder = Path.GetFullPath(Folder + "/run-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(runFolder);
            File.WriteAllText(Folder + "/runtime-report.txt","RUNNING\n");
            running = true; errors.Clear(); Application.logMessageReceived += Log;
            EditorCoroutineUtility.StartCoroutineOwnerless(Guard(Run()));
        }
        private static void Log(string text, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text); }
        private static void Pass(string message) => File.AppendAllText(Folder + "/runtime-report.txt","PASS: " + message + "\n");
        private static void Check(bool condition,string message) { if (!condition) throw new InvalidOperationException(message); }
        private static object Field(string name) => typeof(LearningAppController).GetField(name,Private).GetValue(App);
        private static void Screen(string name) => typeof(LearningAppController).GetMethod("ShowOnlineScreen",Private).Invoke(App,new object[] { name });
        private static void Press(string name)
        {
            var button = UI.Q<Button>(name); Check(button != null && button.enabledInHierarchy,"Missing/disabled button: " + name);
            Click.Invoke(button.clickable,new object[] { null,0 });
        }
        private static IEnumerator Await(Func<bool> condition,string reason,float timeout = 25)
        {
            double deadline = EditorApplication.timeSinceStartup + timeout;
            while (!condition())
            {
                Check(EditorApplication.timeSinceStartup < deadline,reason + " · " + Client.LastError + " · " + peer?.LastError);
                yield return null;
            }
        }
        private static IEnumerator Guard(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine); bool passed = true;
            while (stack.Count>0)
            {
                object next;
                try
                {
                    var current = stack.Peek();
                    if (!current.MoveNext()) { stack.Pop(); continue; }
                    next = current.Current;
                }
                catch (Exception e) { File.AppendAllText(Folder + "/runtime-report.txt","FAIL: " + e + "\n"); passed = false; break; }
                if (next is IEnumerator nested) stack.Push(nested); else yield return next;
            }
            Application.logMessageReceived -= Log;
            if (errors.Count>0) { passed = false; File.AppendAllText(Folder + "/runtime-report.txt","FAIL: runtime errors: " + string.Join("; ",errors.Take(5)) + "\n"); }
            if (peer != null) UnityEngine.Object.Destroy(peer.gameObject);
            LearningProgress.UseVerificationStore(originalSave); SceneManager.LoadScene(LearningGameBridge.SceneName);
            running = false;
            File.AppendAllText(Folder + "/runtime-report.txt",passed ? "COMPLETE: online runtime verification passed.\n" : "FAILED\n");
        }
        private static IEnumerator Run()
        {
            LearningProgress.UseVerificationStore(Path.Combine(runFolder,"progress.json"));
            SceneManager.LoadScene(LearningGameBridge.SceneName); yield return new EditorWaitForSeconds(.5f);
            Check(App != null,"Native app not loaded");
            App.ActiveProfile.voiceAutoplay = false;
            var other = new GameObject("Online verification peer"); UnityEngine.Object.DontDestroyOnLoad(other);
            peer = other.AddComponent<LearningOnlineClient>(); peer.Initialize("peer","Yarış Arkadaşı",Path.Combine(runFolder,"peer.json"));
            Check(peer.SetEndpoint("http://127.0.0.1:8766"),"Peer endpoint failed"); peer.Connect();
            Check(Client.SetEndpoint("http://127.0.0.1:8766"),"App endpoint failed");
            Screen("friends"); yield return Await(() => Client.Connected && peer.Connected && !Client.Busy && !peer.Busy,"Clients did not connect");
            UI.Q<TextField>("FriendCode").value = peer.State.me.code; Press("SendFriendRequest");
            yield return Await(() => peer.State.incoming.Length==1 && !peer.Busy,"Friend request not delivered");
            peer.Respond(Client.State.me.id,true);
            yield return Await(() => Client.State.friends.Length==1 && peer.State.friends.Length==1 && !Client.Busy,"Friend approval not synchronized");
            ScreenCapture.CaptureScreenshot(Folder + "/friends.png"); yield return new EditorWaitForSeconds(.2f);
            Pass("Two Unity HTTP clients connect; friend code request and approval synchronize.");
            bool reconnect = true;
            foreach (bool adult in new[] { false,true })
            {
                App.ActiveProfile.adult = adult;
                foreach (var definition in App.Catalog.Levels(adult))
                {
                    Screen("play"); yield return Await(() => !Client.Busy,"App busy before create");
                    Press("CreateOnlineRoom_" + definition.id);
                    yield return Await(() => Client.Room != null && Client.Room.state=="waiting" && !peer.Busy,"Room not created");
                    peer.Join(Client.Room.code);
                    yield return Await(() => Client.Room.players.Length==2 && peer.Room?.id==Client.Room.id && !Client.Busy && !peer.Busy,"Join not synchronized");
                    if (reconnect) { ScreenCapture.CaptureScreenshot(Folder + "/lobby.png"); yield return new EditorWaitForSeconds(.2f); }
                    Press("OnlineReady"); yield return Await(() => Client.Self.ready && !peer.Busy,"Host ready not saved");
                    peer.Ready();
                    yield return Await(() => UI.Q("OnlineRaceHud") != null && (bool)Field("miniActive") && peer.Room.state=="playing","Countdown did not start game",15);
                    var round = (LearningLevel)Field("miniLevel");
                    if (reconnect)
                    {
                        string wrong = round.items.First(i => !round.correctItems.Contains(i)); Press("Tile_" + wrong);
                        foreach (string id in round.correctItems.Take(2)) Press("Tile_" + id);
                        yield return Await(() => Client.Self.done==2 && Client.Self.mistakes==1,"Live server progress incorrect");
                        ScreenCapture.CaptureScreenshot(Folder + "/live-race.png"); yield return new EditorWaitForSeconds(.2f);
                        SceneManager.LoadScene(LearningGameBridge.SceneName); yield return new EditorWaitForSeconds(.5f);
                        Screen("friends"); yield return Await(() => UI.Q("OnlineRaceHud") != null && (int)Field("miniDone")==2,"Reload did not restore race progress");
                        Check((int)Field("miniMistakes")==1,"Reload lost penalty");
                        round = (LearningLevel)Field("miniLevel");
                        Pass("Live score updates; scene reload restores room, account, accepted items and penalty."); reconnect = false;
                    }
                    if (round.type=="bag") foreach (string id in round.correctItems)
                    { var tile = UI.Q<Button>("Tile_" + id); if (tile.enabledInHierarchy) Press(tile.name); }
                    if (round.type=="sort") foreach (string id in round.items)
                    { Press("Tile_" + id); Press(round.packItems.Contains(id) ? "Çantaya koy" : "Dışarıda bırak"); }
                    if (round.type=="match") foreach (var target in round.targets) { Press("Tile_" + target.accepts); Press(target.label); }
                    if (round.type=="quiz") foreach (var q in round.questions) Press("MiniChoice_" + Array.FindIndex(q.choices,c => c.correct));
                    if (round.type=="danger") foreach (var target in round.actions.Where(a => a.dangerous)) Press("Tile_" + target.id);
                    if (round.type=="sequence") foreach (var q in round.rounds) foreach (var step in q.steps) Press("Step_" + step.id);
                    if (round.type=="memory")
                    {
                        yield return Await(() => !(bool)Field("memoryWatching"),"Memory demonstration did not finish",15);
                        foreach (string id in round.sequence) Press("Tile_" + id);
                    }
                    if (round.type=="catch")
                    {
                        double deadline = EditorApplication.timeSinceStartup + 50;
                        while ((bool)Field("miniActive"))
                        {
                            Check(EditorApplication.timeSinceStartup<deadline,"Catch did not complete");
                            var field = UI.Q("CatchField");
                            var good = field.Query<Image>(className:"catch-item").ToList().Where(i => round.goodItems.Any(id => ((Texture2D)i.image).name == App.Catalog.Item(id).textureKey)).OrderByDescending(i => i.worldBound.y).FirstOrDefault();
                            float x = good != null ? good.worldBound.center.x : field.worldBound.xMin+30;
                            var evt = new Event { type=EventType.MouseDown,button=0,mousePosition=new Vector2(x,field.worldBound.yMax-40) };
                            using (var down=PointerDownEvent.GetPooled(evt)) { down.target=field; field.SendEvent(down); }
                            evt.type=EventType.MouseUp; using (var up=PointerUpEvent.GetPooled(evt)) { up.target=field; field.SendEvent(up); }
                            yield return null;
                        }
                    }
                    yield return Await(() => Client.Self?.status=="finished" && !peer.Busy,"Server did not verify UI completion");
                    foreach (var move in PeerMoves(peer.Room.round)) peer.QueueAction(move.key,move.target,move.index);
                    peer.FlushActions();
                    yield return Await(() => Client.Room.Terminal && peer.Room.Terminal && !Client.Busy && !peer.Busy,"Match result not delivered");
                    Check(UI.Q("OnlineRaceResult")!=null,"Result UI missing");
                    Check(App.ActiveProfile.results.Any(r => r.id==round.SaveId),"Verified game result not recorded locally");
                    Pass((adult ? "Adult " : "Child ") + round.type + " completes through native UI and receives shared server result.");
                    if (round.type=="bag" && !adult) { ScreenCapture.CaptureScreenshot(Folder + "/race-result.png"); yield return new EditorWaitForSeconds(.2f); }
                    Press("FinishOnlineRace"); peer.Acknowledge(null);
                    yield return Await(() => Client.Room==null && peer.Room==null && !Client.Busy && !peer.Busy,"Room acknowledgment failed");
                }
            }
            Screen("board"); yield return Await(() => Client.Board?.self != null,"Leaderboard not loaded");
            Check(Client.Board.self.score>0,"Leaderboard has no verified points");
            UI.Q<DropdownField>("LeaderboardScope").value="Arkadaşlarım";
            yield return Await(() => Client.Board != null && Client.Board.entries.Length==2,"Friends board incorrect");
            ScreenCapture.CaptureScreenshot(Folder + "/leaderboard.png"); yield return new EditorWaitForSeconds(.2f);
            Pass("Global/friends leaderboard loads verified results; both age modes remain separate.");
            Screen("friends"); yield return Await(() => !Client.Busy,"Busy before remove");
            Press("RemoveFriend_" + peer.State.me.id); Press("Arkadaşı çıkar");
            yield return Await(() => Client.State.friends.Length==0 && peer.State.friends.Length==0,"Friend removal not synchronized");
            Pass("Friend removal synchronizes; no runtime errors during verification.");
        }
        private static IEnumerable<OnlineAction> PeerMoves(LearningLevel d)
        {
            if (d.type=="bag") foreach (string key in d.correctItems) yield return new OnlineAction { key=key };
            if (d.type=="sort") foreach (string key in d.items) yield return new OnlineAction { key=key,target=d.packItems.Contains(key) ? "pack" : "leave" };
            if (d.type=="match") foreach (var t in d.targets) yield return new OnlineAction { key=t.accepts,target=t.id };
            if (d.type=="quiz") for (int i=0;i<d.questions.Length;i++) yield return new OnlineAction { index=i,key=Array.FindIndex(d.questions[i].choices,c => c.correct).ToString() };
            if (d.type=="danger") foreach (var a in d.actions.Where(a => a.dangerous)) yield return new OnlineAction { key=a.id };
            if (d.type=="sequence") foreach (var q in d.rounds) foreach (var s in q.steps) yield return new OnlineAction { key=s.id };
            if (d.type=="memory") foreach (string key in d.sequence) yield return new OnlineAction { key=key };
            if (d.type=="catch") foreach (int i in Enumerable.Range(0,d.catchSpawns.Length).Where(i => d.catchSpawns[i].good).Take(d.catchTarget))
                yield return new OnlineAction { key="catch",index=i };
        }
    }
}
