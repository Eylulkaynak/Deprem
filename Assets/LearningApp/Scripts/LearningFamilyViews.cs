using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deprem.Learning
{
    public sealed partial class LearningAppController
    {
        private LearningFamilyService familyService;
        private string familyPage = "", reportChildId, codeProfileId, guardiansProfileId;
        private int familyRevision;
        private FamilyPairCode visiblePairCode;
        private FamilyGuardian[] visibleGuardians = Array.Empty<FamilyGuardian>();
        private bool guardiansLoaded;
        private string guardiansError;
        public LearningFamilyService FamilyService => familyService;

        private void InitializeFamily()
        {
            familyService = GetComponent<LearningFamilyService>() ?? gameObject.AddComponent<LearningFamilyService>();
            familyService.Initialize(store, catalog);
            familyService.StatusChanged += RefreshFamilyControls;
            familyService.ChildrenUpdated += ParentChildrenUpdated;
        }
        private void ShutdownFamily()
        {
            if (familyService == null) return;
            familyService.StatusChanged -= RefreshFamilyControls;
            familyService.ChildrenUpdated -= ParentChildrenUpdated;
            familyService.Shutdown(); visiblePairCode = null; visibleGuardians = Array.Empty<FamilyGuardian>();
        }
        private void LeaveFamilyPage()
        { familyRevision++; familyPage = ""; if (familyService != null) familyService.PollParent = false; }
        private int BeginFamilyPage(string title, string subtitle, string page)
        {
            ResetPage(title, subtitle); familyPage = page; backAction = () => ShowTab("family");
            header.Q<Button>("ModeSwitch").style.display = DisplayStyle.None;
            return familyRevision;
        }
        private Button FamilyButton(VisualElement parent, string title, Action action, string name, string classes = null, Func<bool> allowed = null)
        {
            var button = Button(parent, title, action, (classes ?? "") + " family-request", name);
            button.userData = allowed;
            button.SetEnabled(familyService != null && familyService.Available && !familyService.Busy && (allowed?.Invoke() ?? true));
            return button;
        }
        private void RefreshFamilyControls()
        {
            if (root == null || familyService == null) return;
            root.Query<Button>(className: "family-request").ForEach(button =>
                button.SetEnabled(familyService.Available && !familyService.Busy && ((button.userData as Func<bool>)?.Invoke() ?? true)));
            var activity = root.Q<Label>("FamilyBusy");
            if (activity != null) activity.text = familyService.Busy ? "Bağlantı kuruluyor…" : "";
            var status = root.Q<Label>("ChildSyncStatus");
            if (status != null) status.text = ChildSyncCaption(familyService.Device(Profile.id));
        }
        private void ParentChildrenUpdated()
        {
            if (familyPage != "parent" && familyPage != "report") return;
            if (!familyService.ParentSignedIn) { ShowParentLogin(); return; }
            if (modalLayer.style.display == DisplayStyle.Flex) return;
            Vector2 scroll = body.scrollOffset;
            if (familyPage == "report" && familyService.Children.Any(c => c.id == reportChildId)) ShowChildReport(reportChildId);
            else ShowParentDashboard(false);
            body.schedule.Execute(() => body.scrollOffset = scroll).StartingIn(30);
        }
        private void AddFamilyConnectionEntry()
        {
            var card = Box(body, "card family-connection-card"); card.name = "FamilyConnectionCard";
            Text(card, "BİRLİKTE TAKİP", "eyebrow"); Text(card, "Küçük adımlarını birlikte görün.", "title");
            Text(card, "Veli, çocuğunun uygulamasını kodla bağlar; tamamlanan dersleri ve oyunlardaki ilerlemeyi kendi telefonundan görür.", "paragraph");
            Button(card, "Veli panelini aç", ShowParentEntry, "green", "OpenParentPanel");
            if (!Profile.adult) Button(card, "Velime bağlan", () => ShowChildConnection(), "secondary", "OpenChildConnection");
        }
        private bool FamilyAvailable()
        {
            if (familyService != null && familyService.Available) return true;
            BeginFamilyPage("Veli bağlantısı", "Birlikte öğreniyoruz.", "unavailable");
            Text(body, "Veli bağlantısı henüz kullanıma açılmadı.", "title");
            Text(body, "Derslerine ve oyunlarına devam edebilirsin. İlerlemen cihazında saklanır.", "paragraph");
            Button(body, "Geri dön", () => ShowTab("family"), "secondary", "FamilyBack"); return false;
        }
        private void ShowParentEntry()
        {
            if (!FamilyAvailable()) return;
            if (familyService.ParentSignedIn) ShowParentDashboard(); else ShowParentLogin();
        }
        private void ShowParentLogin(bool register = false)
        {
            if (!FamilyAvailable()) return;
            int revision = BeginFamilyPage("Veli hesabı", "Çocuğunun öğrenme yolculuğuna eşlik et.", "login");
            var card = Box(body, "card family-connection-card");
            Text(card, register ? "Birlikte başlayalım." : "Yolculuğunu birlikte takip edin.", "title");
            Text(card, "Kendi telefonunuzdan dersleri, yıldızları ve tekrar önerilerini görün.", "paragraph");
            var name = new TextField("Adınız") { maxLength = 48, name = "ParentName" };
            if (register) body.Add(name);
            var email = new TextField("E-posta adresiniz") { maxLength = 254, name = "ParentEmail" }; body.Add(email);
            var password = new TextField("Parolanız") { maxLength = 128, isPasswordField = true, name = "ParentPassword" }; body.Add(password);
            Text(body, "Parola en az 10 karakter olmalı.", "small");
            var repeated = new TextField("Parolanızı tekrar yazın") { maxLength = 128, isPasswordField = true, name = "ParentPasswordRepeat" };
            if (register) body.Add(repeated);
            var error = Text(body, "", "family-error"); error.name = "ParentAuthError";
            var activity = Text(body, "", "small"); activity.name = "FamilyBusy";
            FamilyButton(body, register ? "Veli hesabı oluştur" : "Giriş yap", () => {
                error.text = "";
                if (string.IsNullOrWhiteSpace(email.value) || password.value.Length < 10 || (register && string.IsNullOrWhiteSpace(name.value)))
                { error.text = "E-posta adresini ve en az 10 karakterlik parolanı yaz."; return; }
                if (register && password.value != repeated.value) { error.text = "Parolalar aynı olmalı."; return; }
                string enteredPassword = password.value; password.value = ""; repeated.value = "";
                familyService.Authenticate(email.value, enteredPassword, name.value, register, message => {
                    if (familyRevision != revision) return;
                    if (message != null) error.text = message;
                    else ShowParentDashboard(false);
                });
            }, "ParentSubmit", "green");
            FamilyButton(body, register ? "Hesabım var, giriş yap" : "Yeni veli hesabı oluştur", () => ShowParentLogin(!register), "ParentToggleRegister", "secondary");
            Text(body, "Çocuk uygulamasındaki bağlantı kodunu giriş yaptıktan sonra ekleyebilirsiniz.", "small");
            Button(body, "Geri dön", () => ShowTab("family"), "secondary", "FamilyBack");
        }
        private void ShowParentDashboard(bool refresh = true)
        {
            if (!familyService.ParentSignedIn) { ShowParentLogin(); return; }
            BeginFamilyPage("Veli paneli", "Çocuklarının küçük adımlarını takip et.", "parent");
            familyService.PollParent = true;
            Text(body, "Merhaba, " + familyService.ParentName, "title"); Text(body, familyService.ParentEmail, "small");
            FamilyButton(body, "Çocuk bağla", OpenParentPairModal, "ParentLinkChild", "green");
            if (!string.IsNullOrEmpty(familyService.ParentRefreshError)) Text(body, "Şu an yenilenemedi. Son alınan ilerleme gösteriliyor. " + familyService.ParentRefreshError, "family-error");
            else if (familyService.ParentFetchedAt != null) Text(body, "Panel yenilendi: " + LocalTime(familyService.ParentFetchedAt), "small");
            if (familyService.Children.Length == 0)
            {
                var empty = Box(body, "card family-empty"); Text(empty, "İlk çocuğunu bağla.", "title");
                Text(empty, "1. Çocuğun telefonunda Profilim → Veli bağlantısı ekranını aç.\n2. Bağlantı kodu oluştur.\n3. Burada Çocuk bağla düğmesine basıp kodu gir.", "paragraph");
            }
            foreach (var child in familyService.Children)
            {
                var selected = child;
                var card = Box(body, "card family-child-card"); card.name = "ParentChild_" + child.id;
                Text(card, child.progress.name, "title");
                Text(card, "Son etkinlik: " + ActivityCaption(child.progress.lastActivity), "small");
                ChildStats(card, child.progress);
                int completed = ChildCompleted(child.progress);
                Progress(card, (float)completed / catalog.Courses(false).Length);
                Text(card, completed + " / " + catalog.Courses(false).Length + " ders tamamlandı", "small");
                Text(card, "İlerleme alındı: " + LocalTime(child.updatedAt), "small");
                Button(card, "İlerlemeyi gör", () => ShowChildReport(selected.id), "secondary", "ParentReport_" + child.id);
            }
            var activity = Text(body, "", "small"); activity.name = "FamilyBusy";
            FamilyButton(body, "İlerlemeyi yenile", () => familyService.RefreshChildren(), "ParentRefresh", "secondary");
            var logout = Button(body, "Hesabımdan çık", () => { LeaveFamilyPage(); familyService.Logout(); ShowParentLogin(); }, "secondary", "ParentLogout");
            logout.AddToClassList("family-request");
            FamilyButton(body, "Veli hesabını sil", () => Confirm("Veli hesabı silinsin mi?", "Veli hesabınız ve çocuk bağlantılarınız kaldırılır. Çocuğun cihazındaki ders ve oyun ilerlemesi korunur.", "Hesabımı sil", () =>
                familyService.DeleteParentAccount(message => { if (message != null) Notice("Hesap silinemedi", message); else ShowParentLogin(); })), "ParentDeleteAccount", "secondary compact");
            RefreshFamilyControls();
            if (refresh && !familyService.Busy) familyService.RefreshChildren();
        }
        private void OpenParentPairModal()
        {
            familyService.PollParent = false;
            int revision = familyRevision;
            var panel = Modal("Çocuğunu bağla", "Çocuğun uygulamasında Profilim → Veli bağlantısı ekranından alınan 8 karakterli kodu yaz. Kod 10 dakika geçerlidir.");
            var code = new TextField("Bağlantı kodu") { maxLength = 16, name = "ParentPairInput" }; panel.Add(code);
            var error = Text(panel, "", "family-error"); error.name = "ParentPairError";
            var activity = Text(panel, "", "small"); activity.name = "FamilyBusy";
            FamilyButton(panel, "Çocuğumu bağla", () => {
                string normalized = new string((code.value ?? "").Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();
                if (normalized.Length != 8) { error.text = "8 karakterli bağlantı kodunu yaz."; return; }
                familyService.LinkChild(normalized, message => {
                    if (revision != familyRevision) return;
                    if (message != null) error.text = message;
                    else { CloseModal(); ShowParentDashboard(false); }
                });
            }, "ParentPairSubmit", "green");
            Button(panel, "Vazgeç", () => { CloseModal(); familyService.PollParent = true; ShowParentDashboard(false); }, "secondary", "ParentPairCancel");
        }
        private int ChildCompleted(FamilySnapshot snapshot) => catalog.Courses(false).Count(c => (snapshot.completed ?? Array.Empty<string>()).Contains(c.id));
        private void ChildStats(VisualElement parent, FamilySnapshot snapshot)
        {
            var stats = Box(parent, "row stats");
            Stat(stats, snapshot.xp.ToString(), "XP"); Stat(stats, snapshot.TotalStars.ToString(), "YILDIZ"); Stat(stats, snapshot.DisplayStreak(DateTime.Now).ToString(), "GÜNLÜK SERİ");
        }
        private void ShowChildReport(string id)
        {
            if (!familyService.ParentSignedIn) { ShowParentLogin(); return; }
            var child = familyService.Children.FirstOrDefault(c => c.id == id);
            if (child == null) { ShowParentDashboard(false); return; }
            BeginFamilyPage(child.progress.name + " · İlerleme", "Öğrenme yolculuğuna eşlik et.", "report");
            reportChildId = id; familyService.PollParent = true; backAction = () => ShowParentDashboard(false);
            Text(body, child.progress.name + " · Öğrenme yolculuğu", "title");
            Text(body, "Son etkinlik: " + ActivityCaption(child.progress.lastActivity) + " · İlerleme alındı: " + LocalTime(child.updatedAt), "small");
            if (!string.IsNullOrEmpty(familyService.ParentRefreshError)) Text(body, "Son alınan ilerleme gösteriliyor. " + familyService.ParentRefreshError, "family-error");
            ChildStats(body, child.progress);
            int done = ChildCompleted(child.progress);
            Progress(body, (float)done / catalog.Courses(false).Length);
            Text(body, done + " / " + catalog.Courses(false).Length + " ders tamamlandı", "paragraph");
            Text(body, "Birlikte tekrar edebilirsiniz", "title");
            var topics = child.progress.reviewTopics ?? Array.Empty<FamilyReviewTopic>();
            if (topics.Length == 0) Text(body, "Henüz tekrar önerisi yok.", "small");
            foreach (var topic in topics.OrderByDescending(t => t.count))
            {
                var lesson = catalog.Courses(false).FirstOrDefault(c => c.id == topic.courseId);
                if (lesson == null) continue;
                var card = Box(body, "card family-review-card");
                Text(card, lesson.title, "path-name");
                Text(card, "Önceki çalışmalarda " + topic.count + " yanlış cevap. Kısa bir tekrar bilgiyi güçlendirebilir.", "small");
            }
            Text(body, "Ders yolculuğu", "title");
            bool next = true;
            foreach (var lesson in catalog.Courses(false))
            {
                bool complete = (child.progress.completed ?? Array.Empty<string>()).Contains(lesson.id);
                var row = Box(body, "card family-report-row"); Text(row, lesson.title, "path-name");
                string status = complete ? "Tamamlandı" : lesson.id == child.progress.resumeCourse ? "Devam ediyor · " + (child.progress.resumeExercise + 1) + ". adım" : next ? "Sıradaki ders" : "Henüz tamamlanmadı";
                Text(row, status, "small"); if (!complete) next = false;
            }
            Text(body, "Oyunlarda öğrendikleri", "title");
            foreach (var game in catalog.Levels(false))
            {
                var result = (child.progress.results ?? Array.Empty<LearningResult>()).FirstOrDefault(r => r.id == game.SaveId);
                var row = Box(body, "card family-report-row"); Text(row, game.title, "path-name");
                Text(row, result == null ? "Henüz tamamlanmadı" : result.stars + " / 3 yıldız · " + result.score + " puan", "small");
            }
            Text(body, "Bu özet eğitim derslerini ve uygulama içindeki kısa oyunları kapsar.", "small");
            FamilyButton(body, "İlerlemeyi yenile", () => familyService.RefreshChildren(), "ParentRefresh", "secondary");
            FamilyButton(body, "Çocuk bağlantısını kaldır", () => Confirm("Bağlantı kaldırılsın mı?", child.progress.name + " için ilerleme takibiniz sona erer. Çocuğun öğrenme kaydı korunur.", "Bağlantıyı kaldır", () =>
                familyService.UnlinkChild(id, message => { if (message != null) Notice("Bağlantı kaldırılamadı", message); else ShowParentDashboard(false); })), "ParentUnlinkChild", "secondary");
            Button(body, "Veli paneline dön", () => ShowParentDashboard(false), "secondary", "ParentReportBack");
        }
        private void ShowChildConnection(bool load = true)
        {
            if (!FamilyAvailable()) return;
            if (Profile.adult) { ShowParentEntry(); return; }
            int revision = BeginFamilyPage("Velime bağlan", "Öğrenme yolculuğunu velinle paylaş.", "child");
            string profileId = Profile.id;
            var device = familyService.Device(profileId);
            if (guardiansProfileId != profileId) { guardiansProfileId = profileId; visibleGuardians = Array.Empty<FamilyGuardian>(); guardiansLoaded = false; guardiansError = null; }
            var card = Box(body, "card family-connection-card"); Text(card, Profile.name + " · Veli bağlantısı", "title");
            Text(card, "Velin, kendi telefonunda bu kodu ekleyerek tamamladığın dersleri ve kazandığın yıldızları görür.", "paragraph");
            Text(card, "Yalnızca adın ve eğitim ilerlemen paylaşılır. Hazırlık planındaki kişisel bilgiler cihazında kalır.", "small");
            var sync = Text(body, ChildSyncCaption(device), "small"); sync.name = "ChildSyncStatus";
            bool sharing = device?.enabled == true;
            if (device?.pendingStop == true) Text(body, "Paylaşım bu cihazda kapalı. Velilerin erişimi internet bağlantısı geldiğinde kaldırılacak.", "family-error");
            Toggle consent = null;
            if (!sharing)
            {
                consent = new Toggle("Adımın ve eğitim ilerlememin velimle paylaşılmasını istiyorum.") { name = "ChildShareConsent" };
                consent.AddToClassList("family-consent"); body.Add(consent); consent.RegisterValueChangedCallback(_ => RefreshFamilyControls());
            }
            var error = Text(body, "", "family-error"); error.name = "ChildConnectionError";
            var busy = Text(body, "", "small"); busy.name = "FamilyBusy";
            FamilyButton(body, sharing ? "Yeni bağlantı kodu oluştur" : "İlerlememi paylaş ve kod oluştur", () =>
                familyService.CreateCode(profileId, (code, message) => {
                    if (familyRevision != revision) return;
                    if (message != null)
                    {
                        ShowChildConnection(false);
                        root.Q<Label>("ChildConnectionError").text = message;
                    }
                    else { visiblePairCode = code; codeProfileId = profileId; ShowChildConnection(false); }
                }), "ChildCreateCode", "green", () => familyService.Device(profileId)?.pendingStop != true && (sharing || consent?.value == true));
            if (visiblePairCode != null && codeProfileId == profileId) ShowPairCodeCard(visiblePairCode);
            if (sharing)
            {
                Text(body, "İlerlemeni görebilen veliler", "title");
                var guardians = Box(body, "family-guardians"); guardians.name = "LinkedGuardians";
                RenderGuardians(guardians, profileId, revision);
                FamilyButton(body, "Bağlı velileri yenile", () => RefreshGuardians(profileId, revision), "ChildRefreshGuardians", "secondary");
                guardians.schedule.Execute(() => {
                    if (familyRevision == revision && !familyService.Busy) RefreshGuardians(profileId, revision);
                }).Every(15000);
                FamilyButton(body, "İlerleme paylaşımını kapat", () => Confirm("Paylaşım kapatılsın mı?", "Tüm veli bağlantıları kaldırılır. Derslerin, yıldızların ve oyun ilerlemen cihazında korunur.", "Paylaşımı kapat", () => {
                    visiblePairCode = null;
                    familyService.StopSharing(profileId, message => {
                        if (familyRevision != revision) return;
                        visibleGuardians = Array.Empty<FamilyGuardian>(); guardiansLoaded = false; ShowChildConnection(false);
                        if (message != null) Notice("Paylaşım durduruldu", message);
                    });
                }), "ChildStopSharing", "secondary");
                if (load && !familyService.Busy) RefreshGuardians(profileId, revision);
            }
            Button(body, "Geri dön", () => ShowTab("family"), "secondary", "FamilyBack");
        }
        private void ShowPairCodeCard(FamilyPairCode code)
        {
            var card = Box(body, "card family-code-card"); card.name = "ChildPairCodeCard";
            Text(card, "VELİNE VER", "eyebrow");
            string raw = code.code ?? "";
            Text(card, raw.Length == 8 ? raw.Substring(0, 4) + " " + raw.Substring(4) : raw, "family-pair-code").name = "ChildPairCode";
            var expiry = Text(card, "", "small"); expiry.name = "ChildPairExpiry";
            var copy = Button(card, "Kodu kopyala", () => GUIUtility.systemCopyBuffer = raw, "secondary compact", "ChildCopyCode");
            Action tick = () => {
                bool valid = DateTime.TryParse(code.expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expires);
                int seconds = valid ? Math.Max(0, (int)(expires.ToUniversalTime() - DateTime.UtcNow).TotalSeconds) : 0;
                expiry.text = seconds > 0 ? "Kalan süre: " + (seconds / 60) + ":" + (seconds % 60).ToString("00") : "Kodun süresi doldu. Yeni bir kod oluştur.";
                copy.SetEnabled(seconds > 0);
            };
            tick(); card.schedule.Execute(tick).Every(1000);
            Text(card, "Kodu yalnızca velinle paylaş. Her kod bir kez kullanılır; yeni kod oluşturmak önceki kodu kapatır.", "small");
        }
        private void RefreshGuardians(string profileId, int revision)
        {
            familyService.LoadGuardians(profileId, (guardians, message) => {
                if (familyRevision != revision) return;
                if (message == null) { visibleGuardians = guardians; guardiansLoaded = true; }
                guardiansError = message;
                var panel = root.Q("LinkedGuardians"); if (panel != null) RenderGuardians(panel, profileId, revision);
            });
        }
        private void RenderGuardians(VisualElement panel, string profileId, int revision)
        {
            panel.Clear();
            if (guardiansError != null) Text(panel, "Bağlantılar yenilenemedi. " + guardiansError, "family-error");
            else if (visibleGuardians.Length == 0) Text(panel, guardiansLoaded ? "Henüz bağlı veli yok. Kodunu veline ver." : "Bağlı velileri görmek için yenile.", "small");
            foreach (var guardian in visibleGuardians)
            {
                var selected = guardian;
                var card = Box(panel, "card family-report-row"); Text(card, guardian.name, "path-name");
                FamilyButton(card, "Bağlantıyı kaldır", () => Confirm("Veli bağlantısı kaldırılsın mı?", selected.name + " artık ilerlemeni göremez.", "Bağlantıyı kaldır", () =>
                    familyService.RevokeGuardian(profileId, selected.id, message => {
                        if (familyRevision != revision) return;
                        if (message != null) Notice("Bağlantı kaldırılamadı", message);
                        else { visiblePairCode = null; visibleGuardians = visibleGuardians.Where(g => g.id != selected.id).ToArray(); ShowChildConnection(false); }
                    })), "ChildRevoke_" + guardian.id, "secondary compact");
            }
        }
        private static string ChildSyncCaption(FamilyDeviceCredential device)
        {
            if (device?.pendingStop == true) return "Paylaşım kapalı · bağlantıların kaldırılması bekleniyor.";
            if (device?.enabled != true) return "İlerleme paylaşımı kapalı.";
            if (!string.IsNullOrEmpty(device.syncError)) return "İnternet bekleniyor. İlerlemen cihazında kaydediliyor.";
            return string.IsNullOrEmpty(device.lastSyncUtc) ? "İlerleme paylaşımı hazırlanıyor." : "Paylaşım açık · son eşitleme: " + LocalTime(device.lastSyncUtc);
        }
        private static string LocalTime(string timestamp) => DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time)
            ? time.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) : "Henüz alınmadı";
        private static string ActivityCaption(string value)
        {
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)) return "Henüz etkinlik yok";
            int difference = (DateTime.Now.Date - day.Date).Days;
            return difference == 0 ? "Bugün" : difference == 1 ? "Dün" : day.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }
    }
}
