using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deprem.Learning
{
    public sealed partial class LearningAppController
    {
        private LearningLevel miniLevel;
        private VisualElement miniStage;
        private Label miniStatus, miniCounter;
        private int miniMistakes, miniDone, miniGoal, miniRound;
        private bool miniActive, memoryWatching;
        private readonly HashSet<string> miniAccepted = new HashSet<string>();
        private string selectedItem;
        private System.Random miniRandom;

        public void StartMiniGame(LearningLevel level)
        {
            if (online?.Room != null && !online.Room.Terminal && online.Self?.status == "joined")
            { ShowOnlineScreen("room"); return; }
            BeginMiniGame(level);
        }
        private void BeginMiniGame(LearningLevel level, LearningLevel suppliedRound = null, int seed = 0, OnlinePlayer resume = null)
        {
            ResetPage(level.title, "Deneyerek öğreniyoruz.", false);
            header.Q<Button>("ModeSwitch").SetEnabled(false);
            miniOnline = suppliedRound != null; miniRandom = miniOnline ? new System.Random(seed) : null;
            miniLevel = suppliedRound == null ? level.CreateRound() : JsonUtility.FromJson<LearningLevel>(JsonUtility.ToJson(suppliedRound));
            miniMistakes = resume?.mistakes ?? 0; miniDone = resume?.done ?? 0; miniRound = 0;
            miniAccepted.Clear(); foreach (string accepted in resume?.accepted ?? Array.Empty<string>()) miniAccepted.Add(accepted);
            if (level.type == "sequence")
            {
                int remaining = miniDone;
                while (miniRound < miniLevel.rounds.Length - 1 && remaining >= miniLevel.rounds[miniRound].steps.Length)
                { remaining -= miniLevel.rounds[miniRound].steps.Length; miniRound++; }
            }
            selectedItem = null; miniActive = true;
            // The source question lists three hazards. Give the visual version one safe destination.
            if (VisualPlay && miniLevel.type == "quiz") foreach (var question in miniLevel.questions)
                if (question.prompt == "Dışarıdaysan nereden uzak durursun?")
                { question.prompt = "Dışarıda güvenli yer hangisi?"; question.choices.First(c => c.correct).label = "Açık alana git"; }
            backAction = ConfirmMiniExit;
            if (VisualPlay) BuildVisualToolbar();
            else { Button(body, "‹ Oyunlara dön", Back, "secondary compact"); Text(body, level.title, "lesson-title"); }
            miniCounter = Text(body, "", "eyebrow");
            miniStatus = Text(body, level.instruction, "subtitle");
            if (VisualPlay) { miniCounter.style.display = DisplayStyle.None; miniStatus.style.display = DisplayStyle.None; }
            NarrationControls(body);
            miniStage = Box(body);
            switch (level.type)
            {
                case "bag": BuildBag(); break;
                case "sort": BuildSort(); break;
                case "match": BuildMatch(); break;
                case "quiz": miniGoal = miniLevel.questions.Length; BuildMiniQuiz(); break;
                case "danger": BuildDanger(); break;
                case "catch": BuildCatch(); break;
                case "sequence": miniGoal = level.rounds.Sum(r => r.steps.Length); BuildSequence(); break;
                case "memory": BuildMemory(); break;
                default: throw new InvalidOperationException("Unknown mini-game type: " + level.type);
            }
            MiniCounter();
            if (level.type != "quiz") Narrate(new[] { level.instruction });
        }
        private void MiniCounter() { miniCounter.text = miniDone + " / " + miniGoal + " TAMAMLANDI   ·   " + miniMistakes + " HATA"; UpdateVisualProgress(); }
        private void MiniAnswer(bool correct, string message = null)
        {
            if (!miniActive) return;
            if (correct) miniDone++; else miniMistakes++;
            miniStatus.text = message ?? (correct ? "Harika, devam et!" : "Bir daha düşün. Tekrar deneyebilirsin.");
            Sound(correct); VisualFeedback(correct); MiniCounter();
            if (!correct && !IsNarrating)
                Narrate(new[] { miniLevel.type == "danger" ? "Bu, güvenli bir davranış. Biz tehlikeli olanları arıyoruz. Bir daha bakalım." : TryAgainVoice });
            if (miniDone >= miniGoal) CompleteMiniGame();
        }
        private void CompleteMiniGame()
        {
            if (!miniActive) return; miniActive = false;
            if (miniOnline) { CompleteOnlineRound(); return; }
            var finished = miniLevel; int mistakes = miniMistakes;
            int stars = mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1;
            int score = Math.Max(100, 1000 - mistakes * 75);
            Profile.CompleteGame(finished.SaveId, stars, score, DateTime.Now); Save();
            ResetPage("Görev tamamlandı", finished.title, false);
            if (!finished.adult)
            {
                safeArea.AddToClassList("visual-play"); header.Q<Button>("ModeSwitch").style.display = DisplayStyle.None;
                RewardEmblem(body);
                var rewards = Box(body, "row reward-stars");
                for (int i = 0; i < stars; i++) Art(rewards, "icon-star", "reward-small-star");
                Art(body, finished.previewTextureKey, "large-art");
                var actions = Box(body, "row spread reward-actions");
                SymbolButton(actions, "replay", "Bir daha oyna", () => StartMiniGame(finished), "ReplayMiniGame");
                SymbolButton(actions, "back", "Oyunlara dön", () => ShowTab("games"), "FinishMiniGame");
                NarrationControls(body); Narrate(new[] { GameCompleteVoice });
                return;
            }
            Art(body, finished.previewTextureKey, "large-art"); Text(body, new string('★', stars), "title");
            Text(body, finished.successMessage, "lesson-title");
            Text(body, score + " puan · " + mistakes + " hata\nEn iyi sonucun profilinde saklandı.", "paragraph");
            Button(body, "Bir daha oyna", () => StartMiniGame(finished), "green");
            Button(body, "Oyunlara dön", () => ShowTab("games"), "secondary", "FinishMiniGame");
            NarrationControls(body); Narrate(new[] { GameCompleteVoice });
        }
        private Button ItemTile(VisualElement parent, string itemId, Action click)
        {
            var item = catalog.Item(itemId); return PictureTile(parent, item?.label ?? itemId, item?.textureKey, click, itemId);
        }
        private Button PictureTile(VisualElement parent, string label, string icon, Action action, string id)
        {
            var button = Button(parent, "", action, "item-tile", "Tile_" + id, false); button.tooltip = label;
            VisualArt(button, icon); if (!VisualPlay) Text(button, label, ""); return button;
        }
        private float MiniRandomValue() => miniRandom == null ? UnityEngine.Random.value : (float)miniRandom.NextDouble();
        private IEnumerable<T> Shuffled<T>(IEnumerable<T> source) => source.OrderBy(_ => MiniRandomValue());
        private void BuildBag()
        {
            miniGoal = miniLevel.correctItems.Length;
            miniStatus.text = "Gerekli eşyaya dokun veya çantaya sürükle.";
            ShowVisualGuide("item-water", "item-emergency-bag-open");
            var target = Box(miniStage, "card row bag-target"); Art(target, "item-emergency-bag-open");
            if (!VisualPlay) Text(target, "ACİL DURUM ÇANTAM", "path-name");
            var grid = Box(miniStage, "grid");
            foreach (string id in Shuffled(miniLevel.items))
            {
                string item = id; Button tile = null;
                Action choose = () => {
                    if (!miniActive || miniAccepted.Contains(item)) return;
                    OnlineMove(item);
                    bool correct = miniLevel.correctItems.Contains(item);
                    if (correct) { miniAccepted.Add(item); tile.SetEnabled(false); tile.AddToClassList("done"); if (VisualPlay) Symbol(tile, "check").AddToClassList("tile-mark"); }
                    MiniAnswer(correct, correct ? "Çantana eklendi!" : "Bu eşya yerine temel ihtiyaçlara yer ayıralım.");
                };
                tile = ItemTile(grid, item, choose); AddDrag(tile, target, choose);
                if (miniAccepted.Contains(item)) { tile.SetEnabled(false); tile.AddToClassList("done"); }
            }
        }
        private void BuildSort()
        {
            miniGoal = miniLevel.items.Length; miniStatus.text = "Eşyayı seç, sonra doğru alana dokun. Sürükleyerek de ayırabilirsin.";
            ShowVisualGuide("item-water", "item-emergency-bag-open");
            var zones = Box(miniStage, "row"); var grid = new VisualElement(); grid.AddToClassList("grid");
            var tiles = new Dictionary<string, Button>();
            void Sort(bool pack)
            {
                if (!miniActive || selectedItem == null || miniAccepted.Contains(selectedItem)) return;
                OnlineMove(selectedItem, pack ? "pack" : "leave");
                bool correct = miniLevel.packItems.Contains(selectedItem) == pack;
                if (correct) { miniAccepted.Add(selectedItem); tiles[selectedItem].SetEnabled(false); tiles[selectedItem].AddToClassList("done"); tiles[selectedItem].RemoveFromClassList("selected"); if (VisualPlay) Symbol(tiles[selectedItem], "check").AddToClassList("tile-mark"); selectedItem = null; }
                MiniAnswer(correct);
            }
            var packZone = Button(zones, VisualPlay ? "" : "Çantaya koy", () => Sort(true), "green grow sort-zone", "Çantaya koy", false);
            var leaveZone = Button(zones, VisualPlay ? "" : "Dışarıda bırak", () => Sort(false), "orange grow sort-zone", "Dışarıda bırak", false);
            packZone.tooltip = "Çantaya koy"; leaveZone.tooltip = "Evde bırak";
            if (VisualPlay) { Art(packZone, "item-emergency-bag-open"); Symbol(leaveZone, "home"); }
            leaveZone.style.marginLeft = 8; miniStage.Add(grid);
            foreach (string id in Shuffled(miniLevel.items))
            {
                string item = id;
                void Select() { selectedItem = item; foreach (var pair in tiles) pair.Value.EnableInClassList("selected", pair.Key == item); }
                var tile = ItemTile(grid, item, Select); tiles.Add(item, tile);
                if (miniAccepted.Contains(item)) { tile.SetEnabled(false); tile.AddToClassList("done"); }
                AddDrag(tile, packZone, () => { Select(); Sort(true); }, leaveZone, () => { Select(); Sort(false); });
            }
        }
        private void BuildMatch()
        {
            miniGoal = miniLevel.targets.Length; miniStatus.text = "Bir eşya seç, sonra kullanım amacına dokun.";
            ShowVisualGuide("item-water", "symbol:drink");
            var grid = Box(miniStage, "grid match-grid"); var tiles = new Dictionary<string, Button>();
            foreach (string id in Shuffled(miniLevel.items))
            {
                string item = id; tiles[item] = ItemTile(grid, item, () => { selectedItem = item; foreach (var p in tiles) p.Value.EnableInClassList("selected", p.Key == item); });
                if (miniAccepted.Contains(item)) tiles[item].SetEnabled(false);
            }
            var destinations = Box(miniStage, VisualPlay ? "grid purpose-grid" : null);
            foreach (var target in Shuffled(miniLevel.targets))
            {
                var destination = target; Button button = null;
                Action choose = () => {
                    if (!miniActive || selectedItem == null || miniAccepted.Contains(selectedItem)) return;
                    OnlineMove(selectedItem, destination.id);
                    bool correct = destination.accepts == selectedItem;
                    if (correct) { miniAccepted.Add(selectedItem); tiles[selectedItem].SetEnabled(false); button.SetEnabled(false); tiles[selectedItem].RemoveFromClassList("selected"); if (VisualPlay) { Symbol(button, "check").AddToClassList("tile-mark"); Symbol(tiles[selectedItem], "check").AddToClassList("tile-mark"); } selectedItem = null; }
                    MiniAnswer(correct);
                };
                button = Button(destinations, VisualPlay ? "" : target.label, choose, VisualPlay ? "item-tile purpose-tile" : "secondary", target.label, false);
                button.tooltip = target.label;
                if (miniAccepted.Contains(destination.accepts)) button.SetEnabled(false);
                if (VisualPlay) Symbol(button, PurposeSymbol(target.accepts)).AddToClassList("purpose-art");
            }
        }
        private void BuildMiniQuiz()
        {
            miniStage.Clear(); var exercise = miniLevel.questions[miniDone];
            Narrate((miniDone == 0 ? new[] { miniLevel.instruction } : Array.Empty<string>()).Concat(ExerciseVoice(exercise)));
            if (VisualPlay) ShowVisualGuide(QuizContext(exercise.prompt), "symbol:check");
            else Text(miniStage, exercise.prompt, "lesson-title");
            var choices = Box(miniStage, VisualPlay ? "visual-choices" : null);
            foreach (var choice in exercise.choices)
            {
                var answer = choice; var button = Button(choices, "", () => {
                    if (!miniActive) return;
                    OnlineMove(Array.IndexOf(exercise.choices, answer).ToString(), "", miniDone);
                    if (!answer.correct) Profile.RecordMistake(exercise.prompt, answer.label, exercise.choices.First(c => c.correct).label);
                    MiniAnswer(answer.correct, answer.correct ? "Doğru!" : "Güvenli davranışı düşün ve tekrar dene.");
                    if (!answer.correct) Narrate(new[] { RetryVoice }.Concat(ExerciseVoice(exercise)));
                    if (answer.correct && miniActive) BuildMiniQuiz();
                }, "choice", "MiniChoice_" + Array.IndexOf(exercise.choices, choice), false);
                button.tooltip = answer.label;
                if (VisualPlay) VisualArt(button, ChoiceVisual(answer));
                else { Art(button, answer.icon); Text(button, answer.label, ""); }
            }
        }
        private void BuildDanger()
        {
            miniGoal = miniLevel.actions.Count(a => a.dangerous); miniStatus.text = "Tehlikeli davranışların hepsini bul. Güvenli olanlara dokunma.";
            ShowVisualGuide("item-touch-wire", "symbol:cross", "warning");
            var grid = Box(miniStage, "grid danger-grid");
            foreach (var action in Shuffled(miniLevel.actions))
            {
                var item = action; Button button = null;
                button = PictureTile(grid, item.label, item.textureKey, () => {
                    if (!miniActive || miniAccepted.Contains(item.id)) return;
                    OnlineMove(item.id);
                    if (item.dangerous) miniAccepted.Add(item.id);
                    if (item.dangerous) { button.SetEnabled(false); button.AddToClassList("done"); if (VisualPlay) Symbol(button, "cross").AddToClassList("tile-mark"); }
                    MiniAnswer(item.dangerous, item.dangerous ? "Evet, bu davranış tehlikeli!" : "Bu davranış güvenli; tehlikeli olanları arıyoruz.");
                }, item.id);
                if (miniAccepted.Contains(item.id)) { button.SetEnabled(false); button.AddToClassList("done"); }
            }
        }
        private void BuildSequence()
        {
            miniStage.Clear(); selectedItem = null; int index = miniDone - miniLevel.rounds.Take(miniRound).Sum(r => r.steps.Length);
            var steps = miniLevel.rounds[miniRound].steps;
            miniStatus.text = "TUR " + (miniRound + 1) + ": Kartlara doğru sırayla dokun.";
            if (VisualPlay)
            {
                var strip = Box(miniStage, "sequence-example"); strip.name = "SequenceExample";
                for (int i = 0; i < steps.Length; i++) { if (i > 0) Symbol(strip, "arrow"); Art(strip, steps[i].textureKey, "sequence-art"); }
                ShowVisualGuide(steps[0].textureKey, "symbol:hand");
                miniStage.Insert(0, miniGuide);
            }
            foreach (var step in Shuffled(steps))
            {
                var selected = step; Button button = null;
                button = Button(miniStage, "", () => {
                    if (!miniActive) return;
                    OnlineMove(selected.id);
                    bool correct = selected.id == steps[index].id;
                    if (correct) { button.SetEnabled(false); index++; if (VisualPlay) Symbol(button, "check").AddToClassList("tile-mark"); }
                    MiniAnswer(correct);
                    if (miniActive && index == steps.Length) { miniRound++; BuildSequence(); }
                }, "choice", "Step_" + step.id, false); Art(button, step.textureKey); button.tooltip = step.label; if (!VisualPlay) Text(button, step.label, "");
                if (Array.IndexOf(steps, selected) < index) button.SetEnabled(false);
            }
        }
        private void BuildMemory()
        {
            miniGoal = miniLevel.sequenceLength; var sequence = miniOnline ? miniLevel.sequence : Shuffled(miniLevel.memoryPads).Take(miniGoal).ToArray();
            ShowVisualGuide("symbol:eye", "symbol:hand");
            var grid = Box(miniStage, "grid memory-grid"); var pads = new Dictionary<string, Button>();
            foreach (string id in miniLevel.memoryPads)
            {
                string item = id; pads[item] = ItemTile(grid, item, () => {
                    if (memoryWatching || !miniActive) return;
                    OnlineMove(item);
                    bool correct = sequence[miniDone] == item;
                    if (!correct) { miniMistakes++; miniDone = 0; MiniCounter(); Sound(false); VisualFeedback(false); StartCoroutine(ShowMemory(sequence, pads)); }
                    else MiniAnswer(true);
                });
            }
            Action replay = () => { if (!memoryWatching && miniActive) { OnlineMove("replay"); miniDone = 0; MiniCounter(); StartCoroutine(ShowMemory(sequence, pads)); } };
            if (VisualPlay) { SymbolButton(miniStage, "replay", "Sırayı tekrar göster", replay, "MemoryReplay"); replayMiniGuide = replay; }
            else Button(miniStage, "Sırayı tekrar göster", replay, "secondary");
            StartCoroutine(ShowMemory(sequence, pads));
        }
        private IEnumerator ShowMemory(string[] sequence, Dictionary<string, Button> pads)
        {
            memoryWatching = true; miniStatus.text = "Dikkatle izle, sırayı aklında tut.";
            Narrate(new[] { "Önce sırayı birlikte izleyelim." });
            feedbackRevision++;
            if (VisualPlay) { miniFeedback.Clear(); miniFeedback.RemoveFromClassList("retry"); Symbol(miniFeedback, "eye"); }
            yield return MiniDelay(0.5f);
            while (IsNarrating && !miniOnline) yield return null;
            foreach (string id in sequence)
            {
                while (modalLayer.style.display == DisplayStyle.Flex && !miniOnline) yield return null;
                pads[id].AddToClassList("memory-lit"); var marker = VisualPlay ? Symbol(pads[id], "hand") : null; marker?.AddToClassList("tile-mark"); Sound(true); yield return MiniDelay(0.75f);
                marker?.RemoveFromHierarchy();
                pads[id].RemoveFromClassList("memory-lit"); yield return MiniDelay(0.25f);
            }
            miniStatus.text = "Şimdi aynı sırayla dokun."; memoryWatching = false;
            Narrate(new[] { "Şimdi aynı sırayla dokunabilirsin." });
            if (VisualPlay) { miniFeedback.Clear(); Symbol(miniFeedback, "hand"); }
        }
        private IEnumerator MiniDelay(float seconds)
        {
            while (seconds > 0)
            {
                if (miniOnline || modalLayer.style.display != DisplayStyle.Flex) seconds -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
        private void BuildCatch()
        {
            miniGoal = Math.Max(1, miniLevel.catchTarget); miniStatus.text = "Çantayı parmağınla hareket ettir. Gerekli eşyaları yakala.";
            ShowVisualGuide("item-water", "item-emergency-bag-open");
            var field = Box(miniStage, "play-field"); field.name = "CatchField";
            var basket = Art(field, "item-emergency-bag-open", "basket"); float basketX = 150;
            bool ready = miniOnline || !VisualPlay;
            var hand = VisualPlay && !miniOnline ? Symbol(field, "hand") : null;
            if (hand != null)
            {
                hand.AddToClassList("catch-hand");
                field.schedule.Execute(() => {
                    if (ready) return;
                    basketX = Mathf.Max(0, (field.resolvedStyle.width - 100) * (.5f + .35f * Mathf.Sin(Time.unscaledTime * 2)));
                    basket.style.left = basketX; hand.style.left = basketX + 30;
                }).Every(40);
            }
            void Move(float position) { basketX = Mathf.Clamp(position - 50, 0, Mathf.Max(0, field.resolvedStyle.width - 100)); basket.style.left = basketX; }
            field.RegisterCallback<PointerDownEvent>(e => { ready = true; hand?.RemoveFromHierarchy(); field.CapturePointer(e.pointerId); Move(e.localPosition.x); });
            field.RegisterCallback<PointerMoveEvent>(e => { if (field.HasPointerCapture(e.pointerId)) Move(e.localPosition.x); });
            field.RegisterCallback<PointerUpEvent>(e => field.ReleasePointer(e.pointerId));
            basket.style.left = basketX;
            StartCoroutine(CatchLoop(field, basket, () => basketX, () => ready));
        }
        private sealed class FallingItem { public Image image; public float x, y; public bool good; public int index = -1; public float at; }
        private IEnumerator CatchLoop(VisualElement field, Image basket, Func<float> basketX, Func<bool> ready)
        {
            yield return null;
            var falling = new List<FallingItem>(); float spawn = 0;
            int onlineSpawnIndex = 0;
            while (miniActive)
            {
                // A modal pauses this mini-game without changing the application's time scale.
                if (!ready() || (!miniOnline && modalLayer.style.display == DisplayStyle.Flex)) { yield return null; continue; }
                float delta = Mathf.Min(Time.unscaledDeltaTime, 0.05f); spawn -= delta;
                double onlineElapsed = miniOnline ? online.ServerTime - online.Room.startsAt : 0;
                if (miniOnline)
                {
                    while (onlineSpawnIndex < miniLevel.catchSpawns.Length && miniLevel.catchSpawns[onlineSpawnIndex].at <= onlineElapsed)
                    {
                        int index = onlineSpawnIndex++; var next = miniLevel.catchSpawns[index];
                        if (miniAccepted.Contains(index.ToString())) continue;
                        var image = Art(field, catalog.Item(next.id)?.textureKey, "catch-item");
                        float x = next.x * Mathf.Max(1,field.resolvedStyle.width-66); image.style.left = x;
                        falling.Add(new FallingItem { image = image, x = x, y = -70, good = next.good, index = index, at = next.at });
                    }
                }
                else if (spawn <= 0 && falling.Count < 3)
                {
                    bool good = UnityEngine.Random.value < 0.74f; var pool = good ? miniLevel.goodItems : miniLevel.badItems;
                    string item = pool[UnityEngine.Random.Range(0, pool.Length)];
                    var image = Art(field, catalog.Item(item)?.textureKey, "catch-item");
                    float x = UnityEngine.Random.Range(0, Mathf.Max(1, field.resolvedStyle.width - 66));
                    image.style.left = x; image.style.top = -70;
                    falling.Add(new FallingItem { image = image, x = x, y = -70, good = good }); spawn = 0.9f;
                }
                for (int i = falling.Count - 1; i >= 0; i--)
                {
                    var item = falling[i];
                    if (miniOnline) item.y = Mathf.LerpUnclamped(-70, field.resolvedStyle.height+70, (float)(onlineElapsed-item.at)/(miniLevel.adult ? 2.9f : 3.5f));
                    else item.y += delta * (miniLevel.adult ? 180 : 140);
                    item.image.style.top = item.y;
                    bool inBasket = item.y + 50 >= field.resolvedStyle.height - 86 && item.y < field.resolvedStyle.height - 20 && item.x + 50 >= basketX() && item.x + 16 <= basketX() + 100;
                    if (inBasket || item.y > field.resolvedStyle.height)
                    {
                        item.image.RemoveFromHierarchy(); falling.RemoveAt(i);
                        if (miniOnline) { miniAccepted.Add(item.index.ToString()); OnlineMove(inBasket ? "catch" : "miss", "", item.index); }
                        if (inBasket) MiniAnswer(item.good, item.good ? "Çantana eklendi!" : "Gerekli eşyalara yer açalım.");
                        // Missed good items cost a mistake but the round remains retry-friendly.
                        else if (item.good) { miniMistakes++; MiniCounter(); }
                        if (!miniActive) yield break;
                    }
                }
                yield return null;
            }
        }
        private void AddDrag(Button tile, VisualElement target, Action dropped, VisualElement secondTarget = null, Action secondDrop = null)
        {
            Vector2 start = default; bool dragging = false; int pointer = -1;
            tile.RegisterCallback<PointerDownEvent>(e => { start = e.position; pointer = e.pointerId; dragging = false; }, TrickleDown.TrickleDown);
            tile.RegisterCallback<PointerMoveEvent>(e => {
                if (pointer != e.pointerId || e.pressedButtons == 0) return;
                Vector2 delta = (Vector2)e.position - start;
                if (delta.magnitude > 18) { dragging = true; tile.CapturePointer(pointer); tile.transform.position = delta; e.StopPropagation(); }
            });
            tile.RegisterCallback<PointerUpEvent>(e => {
                if (pointer != e.pointerId) return; pointer = -1;
                if (!dragging) return;
                tile.transform.position = Vector3.zero; tile.ReleasePointer(e.pointerId); dragging = false;
                e.StopImmediatePropagation();
                if (target.worldBound.Contains(e.position)) dropped();
                else if (secondTarget != null && secondTarget.worldBound.Contains(e.position)) secondDrop?.Invoke();
            }, TrickleDown.TrickleDown);
            tile.RegisterCallback<PointerCaptureOutEvent>(_ => { tile.transform.position = Vector3.zero; pointer = -1; });
        }
    }
}
