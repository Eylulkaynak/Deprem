using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace Deprem.Learning
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class LearningAppController : MonoBehaviour
    {
        [SerializeField] private Font bodyFont;
        private LearningCatalog catalog;
        private LearningProgress store;
        private LearningProfile Profile => store.Data.Active;
        private VisualElement root, safeArea, header, nav, modalLayer;
        private ScrollView body;
        private AudioSource music, effects;
        private AudioClip positiveTone, negativeTone;
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private string tab = "education";
        private Action backAction;
        private LearningCourse course;
        private int exerciseIndex, lessonMistakes;
        private Rect lastSafeArea;
        private Vector2Int lastSize;
        public LearningCatalog Catalog => catalog;
        public LearningProfile ActiveProfile => Profile;
        public string CurrentTab => tab;
        public string CurrentCourseId => course?.id;

        private void OnEnable()
        {
            store = LearningProgress.Current;
            catalog = LearningCatalog.Load();
            root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("LearningApp/LearningStyles"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("LearningApp/VisualPlay"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("LearningApp/Online"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("LearningApp/FamilyStyles"));
            if (bodyFont != null) root.style.unityFont = bodyFont;
            safeArea = root.Q("SafeArea"); header = root.Q("Header");
            root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());
            body = root.Q<ScrollView>("Body"); nav = root.Q("BottomNav"); modalLayer = root.Q("ModalLayer");
            if (music == null) music = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false; music.clip = Resources.Load<AudioClip>("LearningApp/Audio/music");
            if (effects == null) effects = gameObject.AddComponent<AudioSource>(); effects.playOnAwake = false;
            if (positiveTone == null) positiveTone = Tone(660);
            if (negativeTone == null) negativeTone = Tone(220);
            InitializeNarration();
            InitializeOnline();
            InitializeFamily();
            ApplyAudio();
            LearningGameBridge.Ensure(GetComponent<UIDocument>().panelSettings, bodyFont);
            ShowTab(LearningGameBridge.ReturnedFromGame ? "games" : "education");
            LearningGameBridge.ReturnedFromGame = false;
            ApplySafeArea();
        }
        private void OnDisable() { ShutdownFamily(); ShutdownOnline(); StopNarration(); StopAllCoroutines(); music?.Stop(); effects?.Stop(); store?.Save(); }
        private void OnDestroy()
        {
            if (positiveTone != null) Destroy(positiveTone);
            if (negativeTone != null) Destroy(negativeTone);
        }
        private void OnApplicationPause(bool pause) { PauseNarration(pause); if (pause) store?.Save(); }
        private void OnApplicationQuit() => store?.Save();
        private void Update()
        {
            UpdateNarrationMix();
            UpdateOnlineRace();
            if (lastSafeArea != Screen.safeArea || lastSize != new Vector2Int(Screen.width, Screen.height)) ApplySafeArea();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Back();
        }
        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea; lastSize = new Vector2Int(Screen.width, Screen.height);
            if (safeArea == null || Screen.width == 0 || Screen.height == 0) return;
            float ratio = root.resolvedStyle.width / Screen.width;
            if (float.IsNaN(ratio) || ratio <= 0) return;
            safeArea.style.paddingTop = (Screen.height - lastSafeArea.yMax) * ratio;
            safeArea.style.paddingBottom = lastSafeArea.yMin * ratio;
            safeArea.style.paddingLeft = lastSafeArea.xMin * ratio;
            safeArea.style.paddingRight = (Screen.width - lastSafeArea.xMax) * ratio;
        }
        private void ApplyAudio()
        {
            if (music == null) return;
            music.volume = store.Data.volume * 0.38f;
            if (store.Data.music && !music.isPlaying) music.Play();
            else if (!store.Data.music) music.Pause();
            effects.volume = store.Data.volume * 0.25f;
        }
        private AudioClip Tone(float frequency)
        {
            const int count = 6615; var samples = new float[count];
            for (int i = 0; i < count; i++) samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 44100f) * Mathf.Sin(Mathf.PI * i / count) * 0.3f;
            var clip = AudioClip.Create("Learning feedback", count, 1, 44100, false); clip.SetData(samples, 0); return clip;
        }
        private void Sound(bool positive) { if (store.Data.effects) effects.PlayOneShot(positive ? positiveTone : negativeTone); }
        private bool Save()
        {
            if (store.Save()) { familyService?.MarkDirty(Profile.id); return true; }
            Notice("Kayıt tamamlanamadı", "Cihazdaki boş alanı kontrol et. İlerlemen bu oturumda korunuyor."); return false;
        }
        private VisualElement Box(VisualElement parent, string classes = null)
        {
            var element = new VisualElement(); AddClasses(element, classes); parent.Add(element); return element;
        }
        private static void AddClasses(VisualElement element, string classes)
        {
            foreach (string c in (classes ?? "").Split(' ')) if (c.Length > 0) element.AddToClassList(c);
        }
        private Label Text(VisualElement parent, string value, string classes = "paragraph")
        {
            var label = new Label(value ?? "") { enableRichText = false }; AddClasses(label, classes); parent.Add(label); return label;
        }
        private Button Button(VisualElement parent, string title, Action action, string classes = null, string name = null, bool clickSound = true)
        {
            var button = new Button(() => { if (clickSound) Sound(true); action(); }) { text = title, name = name ?? title, enableRichText = false };
            AddClasses(button, classes); parent.Add(button); return button;
        }
        private Image Art(VisualElement parent, string key, string classes = "art")
        {
            var image = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            if (!string.IsNullOrEmpty(key))
            {
                if (!textures.TryGetValue(key, out Texture2D texture))
                {
                    var entry = catalog.art.FirstOrDefault(a => a.key == key);
                    texture = entry == null ? null : Resources.Load<Texture2D>(entry.resource);
                    textures[key] = texture;
                }
                image.image = texture;
            }
            AddClasses(image, classes); parent.Add(image); return image;
        }
        private void Progress(VisualElement parent, float value)
        {
            var track = Box(parent, "progress-track"); var fill = Box(track, "progress-fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100);
        }
        private void ResetPage(string title, string subtitle, bool showNav = true)
        {
            LeaveFamilyPage();
            ResetNarrationPage();
            onlineScreen = null; onlineContent = null; onlineStatus = null; onlineHud = null;
            StopAllCoroutines(); miniActive = false; course = null;
            memoryWatching = false; replayMiniGuide = null; miniGuide = null; feedbackRevision++;
            safeArea.RemoveFromClassList("visual-play");
            header.Clear(); body.Clear(); nav.Clear(); modalLayer.Clear(); modalLayer.style.display = DisplayStyle.None;
            body.scrollOffset = Vector2.zero; backAction = () => ShowTab("education");
            safeArea.EnableInClassList("adult", Profile.adult);
            BuildHeader(title, subtitle);
            Button(header, Profile.adult ? "Yetişkin" : "Çocuk", () => {
                Profile.adult = !Profile.adult; Save(); ShowTab("education");
            }, "secondary compact", "ModeSwitch");
            nav.style.display = showNav ? DisplayStyle.Flex : DisplayStyle.None;
            if (showNav) BuildNavigation();
        }
        public void ShowTab(string value)
        {
            if (online?.Room != null && !online.Room.Terminal && online.Self?.status == "joined")
            { ShowOnlineScreen("room"); return; }
            tab = value;
            switch (value)
            {
                case "games": if (Profile.adult) ShowGames(); else ShowVisualGames(); break;
                case "family": ShowOnlineScreen("friends"); break;
                case "profile": ShowProfile(); break;
                default: tab = "education"; ShowEducation(); break;
            }
        }
        private void Stat(VisualElement parent, string value, string label)
        { var stat = Box(parent, "stat"); Text(stat, value, "stat-value"); Text(stat, label, "small"); }
        private void PreviewCourse(LearningCourse item)
        {
            var panel = Modal(item.title, item.subtitle); Art(panel, item.icon, "large-art");
            Text(panel, item.exercises.Length + " kısa adım · +30 XP", "subtitle");
            Button(panel, "Derse başla", () => StartCourse(item.id), "green");
            Button(panel, "Yola dön", CloseModal, "secondary");
        }
        public void StartCourse(string id, bool resume = false)
        {
            var found = catalog.courses.First(c => c.id == id);
            int savedIndex = resume && Profile.resumeCourse == id ? Profile.resumeExercise : 0;
            int savedMistakes = resume && Profile.resumeCourse == id ? Profile.resumeMistakes : 0;
            ResetPage("Öğrenme zamanı", found.title, false); course = found;
            exerciseIndex = Mathf.Clamp(savedIndex, 0, course.exercises.Length - 1); lessonMistakes = Mathf.Clamp(savedMistakes, 0, 2);
            Profile.resumeCourse = id; Profile.resumeExercise = exerciseIndex; Profile.resumeMistakes = lessonMistakes; Save();
            backAction = () => Confirm("Derse ara verilsin mi?", "Kaldığın adım kaydedildi. Eğitim yolundan devam edebilirsin.", "Eğitim yoluna dön", () => ShowTab("education"));
            header.Q<Button>("ModeSwitch").SetEnabled(false);
            RenderExercise();
        }
        private void RenderExercise()
        {
            ResetNarrationPage();
            body.Clear(); var exercise = course.exercises[exerciseIndex];
            Button(body, "‹ Eğitim yoluna dön", Back, "secondary compact");
            var stats = Box(body, "row spread"); stats.style.marginTop = 20;
            Text(stats, "ADIM " + (exerciseIndex + 1) + " / " + course.exercises.Length, "eyebrow"); Text(stats, "Can: " + Math.Max(0, 3 - lessonMistakes) + " / 3", "small");
            Progress(body, (float)exerciseIndex / course.exercises.Length);
            NarrationControls(body);
            Narrate(ExerciseVoice(exercise));
            if (exercise.kind == "info")
            {
                Art(body, string.IsNullOrEmpty(exercise.icon) ? course.icon : exercise.icon, "large-art");
                Text(body, exercise.title, "lesson-title"); Text(body, exercise.text, "paragraph");
                Button(body, "Anladım, devam et →", NextExercise, "green", "ContinueLesson"); return;
            }
            Text(body, exercise.prompt, "lesson-title");
            var choices = Box(body);
            var feedback = Box(body); bool answered = false;
            for (int i = 0; i < exercise.choices.Length; i++)
            {
                var choice = exercise.choices[i];
                Button answerButton = null;
                var button = answerButton = Button(choices, "", () => {
                    if (answered) return; answered = true;
                    answerButton.AddToClassList(choice.correct ? "correct" : "wrong");
                    choices.SetEnabled(false); Sound(choice.correct);
                    if (!choice.correct)
                    {
                        lessonMistakes++; Profile.resumeMistakes = lessonMistakes;
                        Profile.RecordMistake(exercise.prompt, choice.label, exercise.choices.First(c => c.correct).label); Save();
                    }
                    var message = Box(feedback, choice.correct ? "feedback" : "feedback wrong");
                    Text(message, choice.correct ? "Çok iyi!" : "Birlikte öğrenelim.", "path-name");
                    string explanation = !string.IsNullOrEmpty(choice.feedback) ? choice.feedback : choice.correct ? exercise.success : exercise.tip;
                    Text(message, string.IsNullOrEmpty(explanation) ? exercise.text : explanation, "paragraph");
                    Narrate(new[] { choice.correct ? null : RetryVoice, string.IsNullOrEmpty(explanation) ? exercise.text : explanation });
                    if (choice.correct) Button(message, "Devam et →", NextExercise, "green", "NextExercise");
                    else if (lessonMistakes >= 3) Button(message, "Bir kez daha deneyelim", () => StartCourse(course.id), "orange");
                    else Button(message, "Tekrar dene", RenderExercise, "orange");
                    body.schedule.Execute(() => {
                        // A quick tap on Continue can replace this page before layout finishes.
                        if (message.panel != null && body.contentContainer.Contains(message)) body.ScrollTo(message);
                    });
                }, "choice", "Answer_" + i);
                if (!string.IsNullOrEmpty(choice.icon)) Art(button, choice.icon);
                Text(button, choice.label, "");
            }
        }
        private void NextExercise()
        {
            exerciseIndex++;
            if (exerciseIndex < course.exercises.Length)
            { Profile.resumeExercise = exerciseIndex; Save(); RenderExercise(); return; }
            var finished = course; bool first = Profile.CompleteCourse(finished.id, DateTime.Now); Save();
            ResetPage("Harika iş çıkardın!", finished.title, false);
            RewardEmblem(body);
            Text(body, first ? "+30 XP" : "Bilgin güçlendi", "reward-value");
            Text(body, "Bir adım daha hazırsın.", "title");
            Text(body, first ? "+30 XP kazandın. Yeni adım açıldı!" : "Tekrar etmek bilgini güçlendirir.", "paragraph");
            var stats = Box(body, "row stats"); Stat(stats, Math.Max(0, 3 - lessonMistakes) + " / 3", "KALAN CAN"); Stat(stats, Profile.DisplayStreak(DateTime.Now) + " gün", "SERİ");
            Button(body, "Eğitim yoluna dön", () => ShowTab("education"), "green", "FinishLesson");
            Button(body, "Şimdi bir oyunda dene", () => ShowTab("games"), "secondary");
            NarrationControls(body); Narrate(new[] { LessonCompleteVoice });
        }
        private void OpenGame(string route)
        {
            if (!Save()) return;
            StopNarration();
            LearningGameBridge.Instance.Launch(route, error => Notice("Oyun açılamadı", error));
        }
        private void ShowProfile()
        {
            ResetPage("Profilim", "Hazırlık alışkanlığını birlikte büyütelim.");
            var card = Box(body, "card profile-card");
            LineIcon(Box(card, "profile-avatar"), "profile"); Text(card, Profile.name, "title");
            var stats = Box(card, "row stats"); Stat(stats, Profile.xp.ToString(), "XP"); Stat(stats, Profile.completed.Count.ToString(), "DERS"); Stat(stats, Profile.TotalStars.ToString(), "YILDIZ");
            Text(card, Profile.lastActivity == DateTime.Now.ToString("yyyy-MM-dd") ? "Bugün " + Profile.todayActivities + " etkinlik tamamladın." : "Bugünün ilk adımı seni bekliyor.", "subtitle");
            ProfileAction("Hazırlık planım", ShowPlan);
            ProfileAction("Deprem rehberi", ShowGuide);
            ProfileAction("Başarımlar", ShowAchievements);
            ProfileAction("Hatalarımdan öğren", ShowMistakes);
            ProfileAction("Veli bağlantısı", () => { if (Profile.adult) ShowParentEntry(); else ShowChildConnection(); });
            ProfileAction("Arkadaşlar ve çevrimiçi yarış", () => ShowTab("family"));
            ProfileAction("Ses ve profil ayarları", ShowSettings);
        }
        private void ProfileAction(string title, Action action)
        {
            var button = Button(body, "", action, "secondary profile-action", title);
            Text(button, title, "grow"); LineIcon(button, "arrow");
        }
        private void ShowFamily()
        {
            ResetPage("Birlikte öğreniyoruz", "Bu cihazdaki aile profilleri");
            AddFamilyConnectionEntry();
            backAction = () => ShowOnlineScreen("friends");
            Button(body, "‹ Arkadaşlara dön", backAction, "secondary compact");
            Text(body, "Herkesin kendi yolculuğu.", "title");
            Text(body, "Bir profil seç. Dersler, yıldızlar ve hazırlık planı her kişi için ayrı saklanır.", "subtitle");
            foreach (var profile in store.Data.profiles.OrderByDescending(p => p.xp))
            {
                var selected = profile; var card = Box(body, "card family-card");
                card.EnableInClassList("active-family", profile == Profile);
                var row = Box(card, "row");
                Text(row, string.IsNullOrEmpty(profile.name) ? "?" : System.Globalization.StringInfo.GetNextTextElement(profile.name).ToUpperInvariant(), "family-avatar");
                var copy = Box(row, "grow");
                Text(copy, profile.name, "path-name");
                Text(copy, profile.xp + " XP  ·  " + profile.completed.Count + " ders  ·  " + profile.TotalStars + " yıldız", "small");
                if (profile == Profile) Text(row, "Sen", "count-chip");
                if (profile != Profile) Button(card, "Bu profille devam et", () => { store.Data.activeProfile = store.Data.profiles.IndexOf(selected); Save(); ShowTab("education"); }, "secondary compact");
            }
            var field = new TextField("Yeni profil adı") { maxLength = 24, name = "NewProfileName" }; body.Add(field);
            Button(body, "Aile profili ekle", () => {
                try { store.AddProfile(field.value); ShowTab("education"); }
                catch (ArgumentException e) { Notice("Bir isim yaz", e.Message); }
            }, "green", "AddProfile");
            Text(body, "Aile sıralaması bu cihazdaki gerçek ilerlemeyi gösterir.", "small");
        }
        private void ShowSettings()
        {
            ResetPage("Ayarlar", "Kendi hızında, kendi sesinde."); backAction = ShowProfile;
            var name = new TextField("Görünen ad") { value = Profile.name, maxLength = 24 }; body.Add(name);
            Button(body, "Adımı kaydet", () => {
                if (string.IsNullOrWhiteSpace(name.value)) return;
                Profile.name = name.value.Trim(); Save(); Notice("Kaydedildi", "Profil adın güncellendi.");
            }, "secondary");
            var musicToggle = new Toggle("Müzik") { value = store.Data.music }; body.Add(musicToggle);
            musicToggle.RegisterValueChangedCallback(e => { store.Data.music = e.newValue; ApplyAudio(); Save(); });
            var effectsToggle = new Toggle("Efektler") { value = store.Data.effects }; body.Add(effectsToggle);
            effectsToggle.RegisterValueChangedCallback(e => { store.Data.effects = e.newValue; Save(); });
            var volume = new Slider("Ses düzeyi", 0, 1) { value = store.Data.volume }; body.Add(volume);
            volume.RegisterValueChangedCallback(e => { store.Data.volume = e.newValue; ApplyAudio(); });
            volume.RegisterCallback<PointerCaptureOutEvent>(_ => Save());
            AddNarrationSettings();
            Button(body, "Profile dön", ShowProfile, "secondary");
        }
        private void ShowPlan()
        {
            ResetPage("Hazırlık planım", "Ailenin ihtiyaçlarına göre düzenle."); backAction = ShowProfile;
            Text(body, "Planın burada, elinin altında.", "title");
            foreach (var field in catalog.planFields)
            {
                var value = Profile.Plan(field.key); Text(body, field.label, "path-name");
                var input = new TextField { value = value.value, maxLength = 600, multiline = true, name = "Plan_" + field.key }; body.Add(input);
                Text(body, field.placeholder, "small");
                input.RegisterValueChangedCallback(e => { value.value = e.newValue; });
                input.RegisterCallback<FocusOutEvent>(_ => Save());
            }
            Text(body, "Hazırlık kontrol listesi", "title");
            foreach (var item in catalog.checklist)
            {
                var value = Profile.Plan(item.key + ":check");
                var toggle = new Toggle(item.label) { value = value.done, name = "Check_" + item.key }; body.Add(toggle);
                toggle.RegisterValueChangedCallback(e => { value.done = e.newValue; Save(); });
            }
            Button(body, "Planımı kaydet", () => { if (Save()) Notice("Planın kaydedildi", "Bilgilerin bu cihazda, seçili profilinde saklanıyor."); }, "green");
            Button(body, "Profile dön", () => { Save(); ShowProfile(); }, "secondary");
        }
        private void ShowGuide()
        {
            ResetPage("Deprem rehberi", "Önce, sırasında ve sonrasında."); backAction = ShowProfile;
            foreach (var guide in catalog.guides.Where(g => g.adult == Profile.adult))
            {
                var card = Box(body, "card"); Text(card, guide.tag, "eyebrow");
                if (!string.IsNullOrEmpty(guide.iconKey)) Art(card, guide.iconKey);
                Text(card, guide.title, "title"); if (!string.IsNullOrEmpty(guide.summary)) Text(card, guide.summary, "subtitle");
                foreach (string point in guide.points) Text(card, "• " + point, "paragraph");
                var selected = guide;
                Button(card, "Dinle", () => { Narrate(new[] { selected.title, selected.summary }.Concat(selected.points), false); PlayNarration(); }, "secondary compact", "ReadGuide_" + guide.title, false);
            }
            NarrationControls(body);
            Button(body, "Profile dön", ShowProfile, "secondary");
        }
        private void ShowMistakes()
        {
            ResetPage("Bir kez daha bakalım", "Hatalar, öğrenmenin bir parçası."); backAction = ShowProfile;
            if (Profile.mistakes.Count == 0) Text(body, "Henüz yanlış yanıtın yok. Öğrenme yolunda devam et!", "paragraph");
            foreach (var mistake in Profile.mistakes.OrderByDescending(m => m.count))
            {
                var card = Box(body, "card"); Text(card, mistake.question, "path-name");
                Text(card, "Seçtiğin: " + mistake.answer, "small"); Text(card, "Doğru yanıt: " + mistake.correct, "paragraph");
            }
            Button(body, "Eğitim yoluna dön", () => ShowTab("education"), "green");
        }
        private void ShowAchievements()
        {
            ResetPage("Başarımlar", "Her küçük adım bir başarı."); backAction = ShowProfile;
            foreach (var badge in catalog.badges)
            {
                bool unlocked = BadgeUnlocked(badge.id); var card = Box(body, "card");
                Text(card, (unlocked ? "★  " : "○  ") + badge.title, "path-name"); Text(card, badge.description, "small");
                Text(card, unlocked ? "KAZANILDI" : "SIRADAKİ HEDEFİN", "eyebrow");
                if (!unlocked) card.style.opacity = 0.65f;
            }
        }
        private bool BadgeUnlocked(string id)
        {
            var best = Profile.results.GroupBy(r => r.id.Split(':').Last()).Select(g => new LearningResult { id = g.Key, stars = g.Max(r => r.stars), score = g.Max(r => r.score) }).ToArray();
            bool Has(string game) => best.Any(r => r.id == game);
            int perfect = best.Count(r => r.stars == 3), stars = best.Sum(r => r.stars), score = Profile.results.Sum(r => r.score);
            int lessons = Profile.completed.Count(c => c.StartsWith("child-", StringComparison.Ordinal));
            switch (id)
            {
                case "first-step": return best.Length >= 1;
                case "bag-ready": return Has("bag-packing"); case "quiz-whiz": return Has("safe-choice");
                case "mech-sort": return Has("category-sorting"); case "mech-match": return Has("match-pairs");
                case "mech-danger": return Has("danger-hunt"); case "mech-catch": return Has("catch-bag");
                case "mech-sequence": return Has("safe-order"); case "mech-memory": return Has("memory-bag");
                case "first-perfect": return perfect >= 1; case "perfect-3": return perfect >= 3; case "perfect-5": return perfect >= 5;
                case "star-collector": return stars >= 10; case "star-20": return stars >= 20;
                case "all-levels": case "all-mechanics": return best.Length >= 8;
                case "three-star-master": return perfect >= 8;
                case "score-2000": return score >= 2000; case "score-5000": return score >= 5000;
                case "streak-3": return Profile.longestStreak >= 3; case "streak-7": return Profile.longestStreak >= 7;
                case "graduate": return Profile.results.Count(r => r.id.StartsWith("child:", StringComparison.Ordinal)) >= 8;
                case "edu-first": return lessons >= 1; case "edu-5": return lessons >= 5; case "edu-10": return lessons >= 10; case "edu-all": return lessons >= 21;
                default: return false;
            }
        }
        private VisualElement Modal(string title, string detail)
        {
            StopNarration();
            modalLayer.Clear(); modalLayer.style.display = DisplayStyle.Flex;
            var panel = Box(modalLayer, "modal"); Text(panel, title, "title"); Text(panel, detail, "paragraph"); return panel;
        }
        private void CloseModal() { modalLayer.style.display = DisplayStyle.None; modalLayer.Clear(); }
        private void Notice(string title, string message) { var panel = Modal(title, message); Button(panel, "Tamam", CloseModal); }
        private void Confirm(string title, string message, string confirm, Action action)
        {
            var panel = Modal(title, message); Button(panel, confirm, () => { CloseModal(); action(); }); Button(panel, "Devam et", CloseModal, "secondary");
        }
        private void Back() { if (modalLayer.style.display == DisplayStyle.Flex) CloseModal(); else backAction?.Invoke(); }
    }
}
