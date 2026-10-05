using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deprem.Learning
{
    public sealed partial class LearningAppController
    {
        private bool VisualPlay => miniLevel != null && !miniLevel.adult;
        private VisualElement miniProgress, miniFeedback, miniGuide;
        private Action replayMiniGuide;
        private int feedbackRevision;

        private void ShowVisualGames()
        {
            ResetPage("Oyun zamanı", "Bir resim seç, oyna.");
            AddOnlineGameEntry();
            var grid = Box(body, "grid visual-game-menu");
            foreach (var level in catalog.Levels(false))
            {
                var selected = level;
                var card = Button(grid, "", () => StartMiniGame(selected), "visual-game-card", "Play_" + level.title);
                Art(card, level.previewTextureKey, "game-art"); Text(card, level.title, "small");
                Symbol(card, "play").AddToClassList("menu-play-symbol");
            }
            Text(body, "Maceralar", "section-title");
            void Adventure(string title, string icon, string route)
            {
                var card = Button(body, "", () => OpenGame(route), "visual-adventure row", "Play_" + title);
                Art(card, icon); Text(card, title, "small grow"); Symbol(card, "play");
            }
            Adventure("Yan Yana", "item-meet-family", "adventure");
            Adventure("Deprem Hikâyesi", "item-drop-cover-hold", "story");
            Adventure("3D Pratik Alanı", "item-first-aid", "minigames");
        }

        private Button SymbolButton(VisualElement parent, string symbol, string label, Action action, string name)
        {
            var button = Button(parent, "", action, "symbol-button secondary", name);
            button.tooltip = label;
            Symbol(button, symbol);
            Text(button, name == "MiniHelp" ? "Göster" : symbol == "back" ? "Geri" : symbol == "replay" ? "Tekrar" : symbol == "play" ? "Devam" : "Tamam", "play-caption");
            return button;
        }

        private void BuildVisualToolbar()
        {
            safeArea.AddToClassList("visual-play");
            header.Q<Button>("ModeSwitch").style.display = DisplayStyle.None;
            var toolbar = Box(body, "row spread play-toolbar");
            SymbolButton(toolbar, "back", "Oyunlara dön", Back, "MiniBack");
            var title = Box(toolbar, "play-title");
            Art(title, miniLevel.previewTextureKey, "play-title-art"); Text(title, miniLevel.title, "play-caption");
            SymbolButton(toolbar, "hand", "Nasıl oynanır? Göster", () => {
                replayMiniGuide?.Invoke();
                if (miniLevel.type != "memory") { Narrate(new[] { miniLevel.instruction }, false); PlayNarration(); }
            }, "MiniHelp");
            miniProgress = Box(body, "play-progress"); miniProgress.name = "MiniProgress";
            miniFeedback = Box(body, "play-feedback"); miniFeedback.name = "MiniFeedback";
            Symbol(miniFeedback, "hand");
        }

        private void UpdateVisualProgress()
        {
            if (!VisualPlay || miniProgress == null) return;
            miniProgress.Clear();
            for (int i = 0; i < miniGoal; i++)
            {
                var dot = Box(miniProgress, "play-dot" + (i < miniDone ? " filled" : ""));
                if (i < miniDone) Symbol(dot, "check");
            }
        }

        private void VisualFeedback(bool correct)
        {
            if (!VisualPlay || miniFeedback == null) return;
            int revision = ++feedbackRevision;
            miniFeedback.Clear();
            miniFeedback.EnableInClassList("retry", !correct);
            Symbol(miniFeedback, correct ? "check" : "replay");
            Text(miniFeedback, correct ? "Harika!" : "Tekrar dene", "play-caption");
            miniFeedback.schedule.Execute(() => {
                if (revision != feedbackRevision || !miniActive) return;
                miniFeedback.Clear(); miniFeedback.RemoveFromClassList("retry"); Symbol(miniFeedback, "hand");
            }).StartingIn(1300);
        }

        private void ConfirmMiniExit()
        {
            if (miniOnline) { ConfirmOnlineExit(); return; }
            if (!VisualPlay) { Confirm("Oyuna ara verilsin mi?", "Bu turun puanı oyun tamamlandığında kaydedilir.", "Oyunlara dön", () => ShowTab("games")); return; }
            var panel = Modal("", ""); panel.AddToClassList("visual-exit");
            Art(panel, miniLevel.previewTextureKey, "large-art");
            var actions = Box(panel, "row spread");
            SymbolButton(actions, "back", "Oyunlara dön", () => ShowTab("games"), "LeaveMiniGame");
            SymbolButton(actions, "play", "Oyuna devam et", CloseModal, "ResumeMiniGame");
        }

        // The example is a separate, looping illustration: it never clicks a real answer or changes score.
        private void ShowVisualGuide(string source, string destination, string goal = "check")
        {
            if (!VisualPlay) return;
            miniGuide?.RemoveFromHierarchy();
            miniGuide = Box(miniStage, "visual-guide"); miniGuide.name = "MiniGuide";
            var from = VisualArt(miniGuide, source, "guide-art");
            Symbol(miniGuide, "arrow");
            var to = VisualArt(miniGuide, destination, "guide-art");
            Symbol(miniGuide, goal);
            var hand = Symbol(miniGuide, "hand"); hand.AddToClassList("guide-hand");
            float began = Time.unscaledTime;
            var guide = miniGuide;
            guide.schedule.Execute(() => {
                if (!miniActive || modalLayer.style.display == DisplayStyle.Flex) return;
                float t = (Time.unscaledTime - began) % 3f;
                float move = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .5f) / 1.2f));
                float x = Mathf.Lerp(from.layout.center.x, to.layout.center.x, move);
                hand.style.left = x - 14;
                hand.style.opacity = t > 2.6f ? .35f : 1;
                from.EnableInClassList("guide-lit", t < .7f);
                to.EnableInClassList("guide-lit", t > 1.6f && t < 2.6f);
            }).Every(40);
            replayMiniGuide = () => { began = Time.unscaledTime; body.scrollOffset = Vector2.zero; };
        }

        private VisualElement VisualArt(VisualElement parent, string key, string classes = "art")
        {
            if (key != null && key.StartsWith("symbol:")) { var symbol = Symbol(parent, key.Substring(7)); AddClasses(symbol, classes); return symbol; }
            return Art(parent, key, classes);
        }

        private static string PurposeSymbol(string id)
        {
            switch (id)
            {
                case "water": return "drink";
                case "map": return "route";
                case "battery": return "energy";
                case "whistle": return "call";
                case "flashlight": return "light";
                case "firstAid": return "bandage";
                case "radio": return "news";
                case "blanket": return "warm";
                case "mask": return "mask";
                default: throw new ArgumentException("No visual purpose for " + id);
            }
        }

        private static string ChoiceVisual(LearningChoice choice)
        {
            if (!string.IsNullOrEmpty(choice.icon)) return choice.icon;
            switch (choice.label)
            {
                case "Açık alana git": return "symbol:park";
                case "Bina dibinde bekle": return "symbol:building";
                case "Balkona çık": return "symbol:balcony";
                case "Camdan sark": return "item-run-window";
                case "Asansörü dene": return "item-use-elevator";
                case "Bina ve direkler": return "symbol:park";
                case "Vitrin kenarı": return "symbol:shop";
                case "Balkon altı": return "symbol:under-balcony";
                default: throw new ArgumentException("Missing choice artwork: " + choice.label);
            }
        }

        private static string QuizContext(string prompt)
        {
            if (prompt.Contains("çant") || prompt.Contains("Çant")) return "item-emergency-bag-open";
            if (prompt.Contains("Gaz")) return "symbol:gas";
            if (prompt.Contains("Telefon")) return "symbol:phone";
            if (prompt.Contains("Toplanma")) return "symbol:park";
            if (prompt.Contains("Enkaz")) return "symbol:rubble";
            if (prompt.Contains("Okul")) return "symbol:school";
            if (prompt.Contains("kablo")) return "symbol:cable";
            if (prompt.Contains("Asansör")) return "symbol:elevator";
            if (prompt.Contains("Dışarı")) return "symbol:outdoors";
            if (prompt.Contains("Yardım")) return "symbol:call";
            return "symbol:quake";
        }

        // Font-independent pictograms also work offline and with sound disabled.
        private VisualElement Symbol(VisualElement parent, string kind)
        {
            var icon = Box(parent, "play-symbol"); icon.name = "Symbol_" + kind; icon.pickingMode = PickingMode.Ignore;
            icon.generateVisualContent += context => {
                var p = context.painter2D;
                float size = Mathf.Min(icon.contentRect.width, icon.contentRect.height);
                if (size <= 0) return;
                Vector2 origin = icon.contentRect.center - Vector2.one * size * .5f;
                Vector2 V(float x, float y) => origin + new Vector2(x, y) * size / 100;
                p.strokeColor = new Color32(34, 54, 94, 255); p.fillColor = new Color32(239, 184, 72, 255); p.lineWidth = size * .045f;
                void Line(params float[] xy) { p.BeginPath(); for (int i = 0; i < xy.Length; i += 2) { if (i == 0) p.MoveTo(V(xy[i], xy[i + 1])); else p.LineTo(V(xy[i], xy[i + 1])); } p.Stroke(); }
                void Poly(params float[] xy) { p.BeginPath(); for (int i = 0; i < xy.Length; i += 2) { if (i == 0) p.MoveTo(V(xy[i], xy[i + 1])); else p.LineTo(V(xy[i], xy[i + 1])); } p.ClosePath(); p.Fill(); p.Stroke(); }
                void Circle(float x, float y, float r) { p.BeginPath(); p.Arc(V(x,y), size*r/100, 0, 360); p.Fill(); p.Stroke(); }
                void Person(float x, float y) { Circle(x,y,7); Line(x,y+8,x,y+30); Line(x-12,y+17,x+12,y+17); Line(x-10,y+45,x,y+30,x+10,y+45); }
                void House() { Poly(15,85,15,35,50,10,85,35,85,85); Line(8,40,50,8,92,40); Line(42,85,42,60,58,60,58,85); }
                switch (kind)
                {
                    case "check": p.strokeColor = new Color32(28,126,80,255); Line(18,50,40,73,83,23); break;
                    case "cross": Line(24,24,76,76); Line(76,24,24,76); break;
                    case "back": Line(72,50,24,50,45,27); Line(24,50,45,73); break;
                    case "arrow": Line(12,50,86,50,65,28); Line(86,50,65,72); break;
                    case "play": Poly(30,18,80,50,30,82); break;
                    case "replay": p.BeginPath(); p.Arc(V(50,50),size*.32f,35,315); p.Stroke(); Line(51,12,74,26,78,5); break;
                    case "eye": Poly(8,50,25,28,50,21,75,28,92,50,75,72,50,79,25,72); Circle(50,50,12); break;
                    case "hand": Poly(32,56,32,19,42,15,48,22,48,48,56,39,65,41,70,48,81,48,85,58,76,85,49,89,20,63,22,53); Line(18,24,10,16); Line(58,17,66,9); break;
                    case "home": House(); break;
                    case "drink": Poly(27,41,73,41,67,88,33,88); p.fillColor = new Color32(86,173,215,255); Poly(50,6,37,24,38,34,50,39,62,34,63,24); break;
                    case "energy": Poly(55,8,25,55,48,55,40,93,78,38,55,38); break;
                    case "route": Circle(18,78,9); Circle(80,20,9); Line(28,78,65,78,65,50,35,50,35,20,69,20); break;
                    case "call": Person(32,39); Line(52,34,63,24); Line(56,48,75,48); Line(53,61,65,71); break;
                    case "light": Circle(38,43,23); Line(27,66,27,81,49,81,49,66); Line(74,22,88,11); Line(76,43,95,43); Line(72,64,87,77); break;
                    case "bandage": p.fillColor = new Color32(238,187,151,255); Poly(10,32,24,16,91,67,77,84); Poly(24,84,10,68,77,16,91,32); p.fillColor = Color.white; Poly(38,38,62,38,62,62,38,62); break;
                    case "news": Poly(12,18,88,18,88,82,12,82); Line(24,32,76,32); Poly(23,44,43,44,43,65,23,65); Line(54,47,76,47); Line(54,62,76,62); break;
                    case "warm": Circle(50,24,13); Poly(25,45,50,38,75,45,83,90,17,90); Line(31,54,50,64,68,53); break;
                    case "mask": Circle(50,42,30); p.fillColor = Color.white; Poly(24,43,76,43,69,69,50,77,31,69); Line(32,51,68,51); Line(35,61,65,61); break;
                    case "phone": Poly(28,8,72,8,72,92,28,92); Line(37,20,63,20); Circle(50,82,3); break;
                    case "gas": House(); Line(35,47,27,39,34,29); Line(64,47,56,36,62,25); break;
                    case "rubble": Poly(9,80,29,49,54,77); Poly(40,75,69,40,93,83); Line(14,28,34,34,46,15); break;
                    case "cable": Line(5,68,31,55,39,70); Line(63,46,70,31,94,40); Poly(51,18,36,45,51,45,42,65,66,37,51,37); break;
                    case "elevator": Poly(15,10,85,10,85,90,15,90); Line(50,10,50,90); Line(27,38,27,64,19,55); Line(71,64,71,38,79,47); break;
                    case "park": p.fillColor = new Color32(162,203,143,255); Circle(19,28,16); Line(19,44,19,79); Line(5,91,95,91); Person(64,39); break;
                    case "outdoors": p.fillColor = new Color32(162,203,143,255); Circle(76,24,16); Line(76,40,76,87); Poly(10,87,10,31,42,31,42,87); Line(5,92,95,92); break;
                    case "building": Poly(9,90,9,15,53,15,53,90); Line(20,30,40,30); Line(20,47,40,47); Person(70,44); break;
                    case "shop": Poly(7,90,7,23,68,23,68,90); Line(7,40,68,40); Line(17,40,17,74,56,74,56,40); Person(83,44); break;
                    case "balcony": Poly(12,92,12,12,88,12,88,92); Person(50,24); Line(21,79,21,64,80,64,80,79); Line(35,64,35,79); Line(65,64,65,79); break;
                    case "under-balcony": Poly(10,8,90,8,90,35,10,35); Line(20,8,20,35); Line(40,8,40,35); Line(60,8,60,35); Person(50,47); break;
                    case "school": House(); Circle(50,32,9); Line(50,26,50,32,55,32); break;
                    case "quake": House(); Line(5,53,1,64,8,76); Line(94,50,99,63,93,76); break;
                    case "warning": Poly(50,8,94,87,6,87); Line(50,33,50,59); Circle(50,74,2); break;
                    default: throw new ArgumentException("Unknown pictogram: " + kind);
                }
            };
            return icon;
        }
    }
}
