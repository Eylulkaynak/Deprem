using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deprem.Learning
{
    // Presentation only: catalog IDs, saves and the scene bridge remain the source of truth.
    public sealed partial class LearningAppController
    {
        private int pageRevision;

        // One small vector icon set keeps navigation and status symbols consistent at every DPI.
        private VisualElement LineIcon(VisualElement parent, string symbol, string classes = "")
        {
            var icon = Box(parent, "line-icon " + classes);
            icon.pickingMode = PickingMode.Ignore;
            icon.generateVisualContent += context => {
                var painter = context.painter2D;
                float unit = Mathf.Min(icon.contentRect.width, icon.contentRect.height) / 24f;
                var origin = icon.contentRect.center - new Vector2(12, 12) * unit;
                painter.strokeColor = icon.resolvedStyle.color;
                painter.lineWidth = 1.7f * unit;
                void Line(params Vector2[] points)
                {
                    painter.BeginPath(); painter.MoveTo(origin + points[0] * unit);
                    for (int i = 1; i < points.Length; i++) painter.LineTo(origin + points[i] * unit);
                    painter.Stroke();
                }
                void Ring(float x, float y, float radius)
                {
                    var points = new Vector2[33];
                    for (int i = 0; i <= 32; i++)
                    {
                        float angle = i * Mathf.PI / 16;
                        points[i] = new Vector2(x + Mathf.Cos(angle) * radius, y + Mathf.Sin(angle) * radius);
                    }
                    Line(points);
                }
                switch (symbol)
                {
                    case "education":
                        Line(new(12, 6), new(8, 4), new(3, 4), new(3, 18), new(8, 18), new(12, 20), new(16, 18), new(21, 18), new(21, 4), new(16, 4), new(12, 6), new(12, 20)); break;
                    case "games":
                        Line(new(7, 6), new(17, 6), new(20, 9), new(22, 17), new(20, 19), new(16, 15), new(8, 15), new(4, 19), new(2, 17), new(4, 9), new(7, 6));
                        Line(new(5, 11), new(11, 11)); Line(new(8, 8), new(8, 14)); Ring(16, 10, 0.7f); Ring(19, 12, 0.7f); break;
                    case "family":
                        Ring(9, 7, 3); Ring(18, 9, 2.5f);
                        Line(new(2, 21), new(2, 18), new(4, 14), new(9, 13), new(14, 14), new(16, 18), new(16, 21));
                        Line(new(17, 14), new(20, 15), new(22, 18), new(22, 21)); break;
                    case "profile":
                        Ring(12, 8, 4); Line(new(4, 21), new(4, 18), new(7, 15), new(17, 15), new(20, 18), new(20, 21)); break;
                    case "xp":
                        Line(new(14, 2), new(4, 14), new(11, 14), new(10, 22), new(20, 10), new(13, 10), new(14, 2)); break;
                    case "streak":
                        Line(new(12, 2), new(7, 9), new(7, 13), new(4, 11), new(4, 16), new(7, 21), new(16, 21), new(20, 17), new(20, 12), new(17, 7), new(15, 11), new(15, 7), new(12, 2)); break;
                    case "stars":
                        var points = new Vector2[11];
                        for (int i = 0; i <= 10; i++)
                        {
                            float angle = (i * 36 - 90) * Mathf.Deg2Rad;
                            float radius = i % 2 == 0 ? 10 : 4.8f;
                            points[i] = new Vector2(12 + Mathf.Cos(angle) * radius, 12 + Mathf.Sin(angle) * radius);
                        }
                        Line(points); break;
                    case "lock":
                        Line(new(7, 11), new(7, 7), new(9, 4), new(15, 4), new(17, 7), new(17, 11));
                        Line(new(5, 11), new(19, 11), new(19, 21), new(5, 21), new(5, 11)); Line(new(12, 15), new(12, 18)); break;
                    case "check": Line(new(4, 12), new(10, 18), new(21, 6)); break;
                    default: Line(new(9, 5), new(16, 12), new(9, 19)); break;
                }
            };
            return icon;
        }

        private void RewardEmblem(VisualElement parent)
        {
            var emblem = Box(parent, "reward-emblem");
            var star = Box(emblem, "reward-star"); star.pickingMode = PickingMode.Ignore;
            // Vector geometry stays sharp on high-density screens; the menu icon is only 64px.
            star.generateVisualContent += context => {
                var painter = context.painter2D;
                Vector2 center = star.contentRect.center;
                float radius = Mathf.Min(star.contentRect.width, star.contentRect.height) * 0.46f;
                painter.fillColor = new Color32(255, 197, 51, 255);
                painter.strokeColor = new Color32(212, 142, 29, 255); painter.lineWidth = 3;
                painter.BeginPath();
                for (int i = 0; i < 10; i++)
                {
                    float angle = (i * 36 - 90) * Mathf.Deg2Rad;
                    float distance = radius * (i % 2 == 0 ? 1 : 0.49f);
                    var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (i == 0) painter.MoveTo(point); else painter.LineTo(point);
                }
                painter.ClosePath(); painter.Fill(); painter.Stroke();
            };
        }

        private void BuildHeader(string title, string subtitle)
        {
            var identity = Box(header, "identity grow");
            Text(identity, title, "header-title");
            Text(identity, subtitle, "header-subtitle");
            header.tooltip = subtitle;
            int revision = ++pageRevision;
            body.contentContainer.RemoveFromClassList("page-visible");
            body.contentContainer.AddToClassList("page-enter");
            body.schedule.Execute(() => {
                if (revision == pageRevision) body.contentContainer.AddToClassList("page-visible");
            }).StartingIn(20);
        }

        private void BuildNavigation()
        {
            string[] keys = { "education", "games", "family", "profile" };
            string[] names = { "Öğren", "Oyna", "Arkadaşlar", "Profilim" };
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                var button = Button(nav, "", () => ShowTab(key), "nav-button", "Nav_" + key);
                button.tooltip = names[i];
                button.EnableInClassList("active", key == tab);
                var iconWell = Box(button, "nav-icon-well");
                LineIcon(iconWell, key, "nav-icon"); Text(button, names[i], "nav-label");
            }
        }

        private void ShowEducation()
        {
            ResetPage("Deprem Kahramanları", "Küçük adımlar, güvenli yarınlar.");
            backAction = () => Confirm("Bugünlük bu kadar mı?", "İlerlemen kaydedildi. Yarın kaldığın yerden devam edebilirsin.", "Uygulamadan çık", Application.Quit);
            var courses = catalog.Courses(Profile.adult);
            int done = courses.Count(c => Profile.completed.Contains(c.id));
            var resume = courses.FirstOrDefault(c => c.id == Profile.resumeCourse);
            var next = resume ?? courses.FirstOrDefault(c => !Profile.completed.Contains(c.id)) ?? courses[0];

            var greeting = Box(body, "greeting");
            Text(greeting, "Merhaba, " + Profile.name + "!", "greeting-title");
            Text(greeting, "Birlikte daha güvenli yarınlara.", "greeting-caption");
            var hero = Box(body, "card hero");
            var heroRow = Box(hero, "row");
            var intro = Box(heroRow, "grow");
            Text(intro, "BUGÜNÜN KÜÇÜK ADIMI", "eyebrow");
            Text(intro, Profile.adult ? "Bugün öğren.\nYarına hazırlan." : "Hazır olmayı\nbirlikte öğren.", "title hero-title");
            Text(intro, "Bir ders, bir oyun.\nHer gün biraz daha hazır.", "subtitle");
            var artWell = Box(heroRow, "hero-art-well");
            Box(artWell, "hero-orbit");
            Art(artWell, "item-emergency-bag", "hero-art");
            var badge = Box(artWell, "hero-badge row");
            bool isReview = Profile.completed.Contains(next.id);
            LineIcon(badge, isReview ? "check" : "xp");
            Text(badge, isReview ? "TEKRAR" : "+30 XP", "hero-badge-text");
            var action = Button(hero, "", () => {
                if (resume != null) StartCourse(resume.id, true); else PreviewCourse(next);
            }, "hero-action", resume != null ? "ResumeLesson" : "ContinueJourney");
            var actionCopy = Box(action, "grow");
            Text(actionCopy, resume != null ? "Kaldığın yerden devam et" : done == courses.Length ? "Bilgini tazele" : done == 0 ? "Hadi başlayalım" : "Yolculuğa devam et", "action-title");
            Text(actionCopy, next.title + "  ·  " + next.exercises.Length + " kısa adım", "action-caption");
            LineIcon(action, "arrow", "action-arrow");

            var stats = Box(body, "row stats dashboard-stats");
            JourneyStat(stats, Profile.xp.ToString(), "Toplam XP", "xp");
            JourneyStat(stats, Profile.DisplayStreak(DateTime.Now) + " gün", "Günlük seri", "streak");
            JourneyStat(stats, Profile.TotalStars.ToString(), "Yıldız", "stars");
            var summary = Box(body, "row spread journey-heading");
            Text(summary, "Öğrenme yolculuğun", "section-title");
            Text(summary, done + " / " + courses.Length + " ders", "count-chip");
            Progress(body, (float)done / courses.Length);

            string[] chapters = { "Hazırlık seninle başlar", "Sarsıntı anında", "Her yerde güvende", "Sonrasında doğru adımlar", "Birbirimize ulaşalım", "Birlikte güçlüyüz", "Hazır olmak bir alışkanlık" };
            string[] descriptions = { "Çantanı, aileni ve evini hazırla.", "Güvenli davranışları öğren.", "Farklı yerlerde öğrendiklerini uygula.", "Sakin kal, güvenle ilerle.", "İletişim kur, doğru bilgiye ulaş.", "Kendini ve çevrendekileri koru.", "Bilgini her gün güçlendir." };
            VisualElement chapter = null;
            for (int i = 0; i < courses.Length; i++)
            {
                var item = courses[i]; bool complete = Profile.completed.Contains(item.id);
                bool unlocked = i == 0 || Profile.completed.Contains(courses[i - 1].id) || complete;
                if (i % 3 == 0)
                {
                    chapter = Box(body, "chapter");
                    var chapterHeader = Box(chapter, "chapter-header row");
                    Text(chapterHeader, (i / 3 + 1).ToString("00"), "chapter-number");
                    var chapterCopy = Box(chapterHeader, "grow");
                    Text(chapterCopy, Profile.adult ? "Hazırlık yolculuğu · " + (i / 3 + 1) : chapters[Math.Min(i / 3, chapters.Length - 1)], "chapter-title");
                    Text(chapterCopy, Profile.adult ? "Bilgini günlük yaşamına taşı." : descriptions[Math.Min(i / 3, descriptions.Length - 1)], "small");
                }
                if (i % 3 != 0) Box(chapter, "path-connector");
                var row = Button(chapter, "", () => PreviewCourse(item), "path-row", "Course_" + item.id);
                row.tooltip = item.title; row.SetEnabled(unlocked);
                row.EnableInClassList("path-current", unlocked && !complete);
                row.EnableInClassList("path-locked", !unlocked);
                var node = Box(row, "path-node");
                node.EnableInClassList("completed", complete); node.EnableInClassList("current", unlocked && !complete);
                Art(node, item.icon, "path-art");
                var details = Box(row, "path-text grow");
                Text(details, "ADIM " + (i + 1).ToString("00") + (complete ? "  ·  TAMAMLANDI" : unlocked ? "  ·  SIRA SENDE" : ""), "step-label");
                Text(details, item.title, "path-name");
                Text(details, complete ? "Bilgini tazele" : unlocked ? item.subtitle : "Önceki adımı tamamla", "small");
                LineIcon(row, complete ? "check" : unlocked ? "arrow" : "lock", "path-status");
            }
            var guide = Box(body, "card guide-card");
            Text(guide, "Birlikte daha güvendeyiz.", "section-title");
            Text(guide, "Öğrendiklerini ailenle paylaş. Rehberin her zaman yanında.", "subtitle");
            Button(guide, "Deprem rehberini aç", ShowGuide, "secondary");
            var institution = Box(body, "institution-footer");
            var logo = new Image {
                image = Resources.Load<Texture2D>("LearningApp/Brand/imo-logo-full"),
                scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore,
                tooltip = "KTMMOB İnşaat Mühendisleri Odası"
            };
            logo.AddToClassList("institution-logo"); institution.Add(logo);
        }

        private void JourneyStat(VisualElement parent, string value, string label, string kind)
        {
            var stat = Box(parent, "stat journey-stat " + kind);
            var line = Box(stat, "row"); LineIcon(line, kind, "stat-icon"); Text(line, value, "stat-value");
            Text(stat, label, "small");
        }

        private void ShowGames()
        {
            ResetPage("Oyun zamanı", "Öğrendiklerini deneyerek pekiştir.");
            AddOnlineGameEntry();
            Text(body, "KEŞFET · DENE · ÖĞREN", "eyebrow");
            Text(body, "Bilgini oyuna taşı.", "title");
            Text(body, "Her macera, daha hazır bir sen.", "subtitle");
            var label = Box(body, "row spread journey-heading");
            Text(label, "3D maceralar", "section-title"); Text(label, "3 keşif alanı", "count-chip");
            GameCard("Yan Yana", "Ailenle hazırlan, birlikte güvenliğe ulaş.", "item-meet-family", () => OpenGame("adventure"), "Maceraya başla");
            GameCard("Deprem Hikâyesi", "Hazırlıktan tahliyeye dört oynanabilir bölüm.", "item-drop-cover-hold", () => OpenGame("story"), "Hikâyeye gir");
            GameCard("3D Pratik Alanı", "Hazırlık, korunma, tahliye ve yardım: altı aşamalı deprem senaryosu.", "item-first-aid", () => OpenGame("minigames"), "Pratik yap");
            Text(body, "Kısa oyunlar", "section-title");
            Text(body, "Bir oyun seç, becerilerini güçlendir.", "subtitle");
            foreach (var level in catalog.Levels(Profile.adult))
            {
                var selected = level; var result = Profile.results.FirstOrDefault(r => r.id == level.SaveId);
                GameCard(level.title, level.shortTitle + (result != null ? "\n" + result.stars + " yıldız  ·  " + result.score + " puan" : ""), level.previewTextureKey, () => StartMiniGame(selected), "Oyna");
            }
        }

        private void GameCard(string title, string description, string icon, Action action, string button)
        {
            bool is3D = title == "Yan Yana" || title == "Deprem Hikâyesi" || title == "3D Pratik Alanı";
            var card = Box(body, "card game-panel" + (is3D ? " adventure-card" : ""));
            card.EnableInClassList("featured-game", title == "Yan Yana");
            var row = Box(card, "game-card");
            var art = Box(row, "game-art-well"); Art(art, icon, "game-art");
            var copy = Box(row, "grow");
            if (is3D) Text(copy, title == "Yan Yana" ? "BİRLİKTE KEŞFET" : "3D DENEYİM", "eyebrow");
            Text(copy, title, "title"); Text(copy, description, "subtitle");
            var play = Button(card, "", action, "game-action row" + (title == "Yan Yana" ? "" : " secondary"), "Play_" + title);
            Text(play, button, "grow"); LineIcon(play, "arrow");
        }
    }
}
