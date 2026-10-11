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

        private void BuildBrandedHeader(string title, string subtitle)
        {
            var identity = Box(header, "identity grow");
            var logo = new Image {
                image = Resources.Load<Texture2D>("LearningApp/Brand/imo-logo-full"),
                scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore,
                tooltip = "KTMMOB İnşaat Mühendisleri Odası"
            };
            logo.AddToClassList("institution-logo"); identity.Add(logo);
            Text(identity, title, "header-subtitle");
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
            string[] names = { "Öğren", "Oyna", "Ailem", "Profilim" };
            string[] icons = { "icon-menu-education", "icon-menu-play", "icon-friends", "icon-menu-ranking" };
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                var button = Button(nav, "", () => ShowTab(key), "nav-button", "Nav_" + key);
                button.tooltip = names[i];
                button.EnableInClassList("active", key == tab);
                Art(button, icons[i], "nav-icon"); Text(button, names[i], "nav-label");
                Box(button, "nav-indicator");
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

            var greeting = Box(body, "row spread greeting");
            Text(greeting, "Merhaba, " + Profile.name + "!", "greeting-title");
            Text(greeting, "BİRLİKTE HAZIRIZ", "micro-label");
            var hero = Box(body, "card hero");
            var heroRow = Box(hero, "row");
            var intro = Box(heroRow, "grow");
            Text(intro, "DEPREM KAHRAMANLARI", "eyebrow");
            Text(intro, Profile.adult ? "Bugün öğren.\nYarına hazırlan." : "Küçük adımlar.\nBüyük cesaret!", "title hero-title");
            Text(intro, "Öğren, oyunlarla dene.\nHer gün biraz daha hazır ol.", "subtitle");
            var artWell = Box(heroRow, "hero-art-well");
            Box(artWell, "hero-orbit");
            Art(artWell, "item-emergency-bag", "hero-art");
            Art(artWell, "icon-star", "hero-star");
            var action = Button(hero, "", () => {
                if (resume != null) StartCourse(resume.id, true); else PreviewCourse(next);
            }, "hero-action", resume != null ? "ResumeLesson" : "ContinueJourney");
            var actionCopy = Box(action, "grow");
            Text(actionCopy, resume != null ? "Kaldığın yerden devam et" : done == courses.Length ? "Bilgini tazele" : done == 0 ? "İlk adımını at" : "Yolculuğa devam et", "action-title");
            Text(actionCopy, next.title + "  ·  " + next.exercises.Length + " kısa adım", "action-caption");
            Text(action, "›", "action-arrow");

            var stats = Box(body, "row stats dashboard-stats");
            JourneyStat(stats, Profile.xp.ToString(), "Toplam XP", "icon-menu-ranking", "xp");
            JourneyStat(stats, Profile.DisplayStreak(DateTime.Now) + " gün", "Günlük seri", "item-flashlight", "streak");
            JourneyStat(stats, Profile.TotalStars.ToString(), "Yıldız", "icon-star", "stars");
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
                var row = Box(chapter, "path-row");
                row.EnableInClassList("path-current", unlocked && !complete);
                row.EnableInClassList("path-locked", !unlocked);
                var node = Button(row, "", () => PreviewCourse(item), "path-node", "Course_" + item.id);
                node.tooltip = item.title;
                node.EnableInClassList("completed", complete); node.EnableInClassList("current", unlocked && !complete);
                node.SetEnabled(unlocked); Art(node, item.icon, "path-art");
                var details = Box(row, "path-text grow");
                Text(details, "ADIM " + (i + 1).ToString("00") + (complete ? "  ·  TAMAMLANDI" : unlocked ? "  ·  SIRA SENDE" : ""), "step-label");
                Text(details, item.title, "path-name");
                Text(details, complete ? "Bilgini tazele" : unlocked ? item.subtitle : "Önceki adımı tamamla", "small");
                Text(row, complete ? "✓" : unlocked ? "›" : "·", "path-status");
            }
            var guide = Box(body, "card guide-card");
            Text(guide, "Birlikte daha güvendeyiz.", "section-title");
            Text(guide, "Öğrendiklerini ailenle paylaş. Rehberin her zaman yanında.", "subtitle");
            Button(guide, "Deprem rehberini aç", ShowGuide, "secondary");
            Text(body, "KTMMOB İnşaat Mühendisleri Odası", "institution-footer");
        }

        private void JourneyStat(VisualElement parent, string value, string label, string icon, string kind)
        {
            var stat = Box(parent, "stat journey-stat " + kind);
            var line = Box(stat, "row"); Art(line, icon, "stat-icon"); Text(line, value, "stat-value");
            Text(stat, label, "small");
        }

        private void ShowGames()
        {
            ResetPage("Oyun zamanı", "Öğrendiklerini deneyerek pekiştir.");
            Text(body, "KEŞFET · DENE · ÖĞREN", "eyebrow");
            Text(body, "Bilgini oyuna taşı.", "title");
            Text(body, "Her macera, daha hazır bir sen.", "subtitle");
            var label = Box(body, "row spread journey-heading");
            Text(label, "3D maceralar", "section-title"); Text(label, "3 keşif alanı", "count-chip");
            GameCard("Yan Yana", "Ailenle hazırlan, birlikte güvenliğe ulaş.", "item-meet-family", () => OpenGame("adventure"), "Maceraya başla");
            GameCard("Deprem Hikâyesi", "Hazırlıktan tahliyeye dört oynanabilir bölüm.", "item-drop-cover-hold", () => OpenGame("story"), "Hikâyeye gir");
            GameCard("3D Pratik Alanı", "Kısa görevler ve farklı afet senaryoları.", "item-first-aid", () => OpenGame("minigames"), "Pratik yap");
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
            Button(card, button + "  ›", action, is3D ? "" : "secondary", "Play_" + title);
        }
    }
}
