using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deprem.Learning
{
    public sealed partial class LearningAppController
    {
        private LearningOnlineClient online;
        private string onlineScreen, onlineStartedRoom;
        private bool miniOnline, boardRequested;
        private string boardScope = "global", boardPeriod = "all", boardGame = "";
        private VisualElement onlineContent, onlineHud;
        private Label onlineStatus, onlineCountdown, onlineScoreLabel, onlineRivalLabel, onlineClockLabel;
        private Button onlineConnectButton, onlineFriendButton, onlineJoinButton;

        private void InitializeOnline()
        {
            online = GetComponent<LearningOnlineClient>() ?? gameObject.AddComponent<LearningOnlineClient>();
            EnsureOnlineProfile();
            online.Changed += OnOnlineChanged;
        }
        private void EnsureOnlineProfile() => online.Initialize(Profile.id, Profile.name,
            Path.Combine(Path.GetDirectoryName(store.SavePath), "learning-online-v1.json"));
        private void ShutdownOnline()
        {
            if (online == null) return;
            online.Changed -= OnOnlineChanged; online.Disconnect();
        }
        private void AddOnlineGameEntry()
        {
            var card = Box(body, "card online-entry");
            var title = Box(card, "row"); LineIcon(title, "family"); Text(title, "Birlikte oyna", "section-title grow");
            Text(card, "Arkadaşını ekle, aynı oyunda yarış, sıralamada yerini gör.", "small");
            Button(card, "Arkadaşınla yarış", () => ShowOnlineScreen("play"), "", "OpenOnlinePlay");
        }
        private void ShowOnlineScreen(string screen)
        {
            EnsureOnlineProfile();
            if (online.Room != null && !online.Room.Terminal && online.Self?.status == "joined") screen = "room";
            tab = "family";
            ResetPage(screen == "board" ? "Sıralama" : screen == "room" ? "Arkadaş yarışı" : screen == "play" ? "Birlikte oyna" : "Arkadaşlar",
                screen == "friends" ? "Bir kod paylaş, birlikte öğren." : "Aynı oyun, aynı başlangıç, tatlı bir rekabet.", screen != "room");
            onlineScreen = screen; boardRequested = false;
            onlineCountdown = null; onlineFriendButton = onlineJoinButton = null;
            backAction = screen == "room" ? (Action)ConfirmOnlineExit : () => ShowTab("games");
            if (screen == "room") header.Q<Button>("ModeSwitch").SetEnabled(false);
            else
            {
                var tabs = Box(body, "row online-tabs");
                void Tab(string key, string label)
                {
                    var button = Button(tabs, label, () => ShowOnlineScreen(key), "secondary", "OnlineTab_" + key);
                    button.EnableInClassList("selected", key == screen);
                }
                Tab("friends", "Arkadaşlar"); Tab("play", "Yarış"); Tab("board", "Sıralama");
            }
            onlineStatus = Text(body, "", "online-status"); onlineStatus.name = "OnlineStatus";
            onlineConnectButton = Button(body, "Bağlan / yeniden dene", online.Connect, "green", "OnlineConnect");
            if (screen == "friends")
            {
                var add = Box(body, "card"); Text(add, "Arkadaş ekle", "section-title");
                var code = new TextField("Arkadaş kodu") { maxLength = 12, name = "FriendCode" }; add.Add(code);
                onlineFriendButton = Button(add, "İstek gönder", () => online.AddFriend(code.value), "green", "SendFriendRequest");
                Text(add, "Arkadaşının isteği kabul etmesiyle birbirinizi listenizde görebilirsiniz.", "small");
            }
            else if (screen == "play")
            {
                var join = Box(body, "card"); Text(join, "Bir yarışa katıl", "section-title");
                var code = new TextField("Oda kodu") { maxLength = 12, name = "RoomCode" }; join.Add(code);
                onlineJoinButton = Button(join, "Odaya katıl", () => online.Join(code.value), "green", "JoinOnlineRoom");
            }
            else if (screen == "board") BuildBoardFilters();
            onlineContent = Box(body); onlineContent.name = "OnlineContent";
            if (screen == "friends") AddFamilyConnectionEntry();
            if (screen != "room")
            {
                var footer = Box(body, "online-footer");
                Button(footer, "Bu cihazdaki aile profilleri", ShowFamily, "secondary", "LocalFamilyProfiles");
                Button(footer, "Bağlantı ayarı", ShowOnlineConnection, "secondary compact", "OnlineConnectionSettings");
            }
            RefreshOnlineUI();
            if (!online.Connected && !online.Busy) online.Connect();
            if (screen == "board" && online.Connected) RequestOnlineBoard();
        }
        private void ShowOnlineConnection()
        {
            var panel = Modal("Çevrimiçi bağlantı", "İki oyuncu aynı sunucuya bağlanmalı.");
            var address = new TextField("Sunucu adresi") { value = online.Endpoint, maxLength = 200, name = "OnlineServerAddress" }; panel.Add(address);
            Text(panel, "Sunucu adresini kaydettiğinde bu profilin arkadaş hesabına bağlanılır.", "small");
            Button(panel, "Kaydet ve bağlan", () => {
                if (!online.SetEndpoint(address.value)) { Text(panel, online.LastError, "small"); return; }
                string screen = onlineScreen ?? "friends"; CloseModal(); ShowOnlineScreen(screen);
            }, "green", "SaveOnlineConnection");
            Button(panel, "Geri", CloseModal, "secondary");
        }
        private void OnOnlineChanged()
        {
            var room = online.Room; var self = online.Self;
            if (room != null && self?.status == "finished" && online.RecordedRoom != room.id)
            {
                int stars = self.mistakes == 0 ? 3 : self.mistakes <= 2 ? 2 : 1;
                Profile.CompleteGame(room.round.SaveId, stars, self.score, DateTime.Now);
                if (Save()) online.RecordLocalResult(room.id);
            }
            if (room != null && !room.Terminal && onlineScreen != null && onlineScreen != "room")
            { ShowOnlineScreen("room"); return; }
            if (miniOnline && (room == null || room.Terminal || self?.status == "left"))
            {
                miniOnline = false; ShowOnlineScreen(room == null ? "play" : "room"); return;
            }
            if (onlineScreen != null) RefreshOnlineUI();
            if (onlineScreen == "board" && online.Connected && !boardRequested) RequestOnlineBoard();
            UpdateOnlineRace();
        }
        private void RefreshOnlineUI()
        {
            if (onlineStatus == null || onlineContent == null) return;
            onlineStatus.text = online.LastError ?? (online.Connected ? "Bağlı · " + online.State.me.name : online.Busy ? "Sunucuya bağlanılıyor…" : "Çevrimiçi oynamak için bağlan.");
            onlineStatus.EnableInClassList("offline", !online.Connected || online.LastError != null);
            onlineConnectButton.style.display = !online.Connected || online.LastError != null ? DisplayStyle.Flex : DisplayStyle.None;
            onlineConnectButton.SetEnabled(!online.Busy);
            onlineFriendButton?.SetEnabled(online.Connected && !online.Busy);
            onlineJoinButton?.SetEnabled(online.Connected && !online.Busy);
            onlineContent.Clear();
            if (!online.Connected)
            {
                Text(onlineContent, "Arkadaşların ve yarış sonuçların bağlantı kurulduğunda burada görünür.", "subtitle"); return;
            }
            switch (onlineScreen)
            {
                case "friends": RenderOnlineFriends(); break;
                case "play": RenderOnlineGames(); break;
                case "board": RenderOnlineBoard(); break;
                case "room": RenderOnlineRoom(); break;
            }
        }
        private void RenderOnlineFriends()
        {
            var state = online.State;
            var identity = Box(onlineContent, "card"); Text(identity, "Senin arkadaş kodun", "small");
            Text(identity, DisplayOnlineCode(state.me.code), "online-code").name = "MyFriendCode";
            Button(identity, "Kodu kopyala", () => GUIUtility.systemCopyBuffer = state.me.code, "secondary compact", "CopyFriendCode");
            RenderOnlineInvites();
            if (state.incoming.Length > 0)
            {
                Text(onlineContent, "Arkadaşlık istekleri", "section-title");
                foreach (var person in state.incoming)
                {
                    var p = person; var card = OnlinePerson(onlineContent, person);
                    var actions = Box(card, "row online-actions");
                    Button(actions, "Kabul et", () => online.Respond(p.id, true), "green compact", "AcceptFriend_" + p.id);
                    Button(actions, "Reddet", () => online.Respond(p.id, false), "secondary compact", "RejectFriend_" + p.id);
                }
            }
            Text(onlineContent, "Arkadaşların · " + state.friends.Length, "section-title");
            if (state.friends.Length == 0) Text(onlineContent, "Kodu arkadaşınla paylaş veya onun koduyla istek gönder.", "subtitle");
            foreach (var person in state.friends.OrderByDescending(p => p.online).ThenBy(p => p.name))
            {
                var p = person; var card = OnlinePerson(onlineContent, p);
                var actions = Box(card, "row online-actions");
                Button(actions, "Yarışa davet et", () => ChooseFriendGame(p), "green compact", "ChallengeFriend_" + p.id);
                Button(actions, "Çıkar", () => Confirm("Arkadaş çıkarılsın mı?", p.name + " arkadaş listenden çıkarılacak.", "Arkadaşı çıkar", () => { CloseModal(); online.RemoveFriend(p.id); }), "secondary compact", "RemoveFriend_" + p.id);
            }
            foreach (var p in state.outgoing)
            {
                var card = Box(onlineContent, "online-person"); Text(card, p.name, "path-name"); Text(card, "Arkadaşlık isteğin bekliyor.", "small");
                string id = p.id; Button(card, "İsteği geri al", () => online.RemoveFriend(id), "secondary compact");
            }
        }
        private VisualElement OnlinePerson(VisualElement parent, OnlinePlayer person)
        {
            var card = Box(parent, "online-person"); var row = Box(card, "row");
            Box(row, "online-presence" + (person.online ? " connected" : ""));
            Text(row, person.name, "path-name grow"); Text(card, person.online ? "Çevrimiçi" : "Şu an çevrimdışı", "small");
            return card;
        }
        private void ChooseFriendGame(OnlinePlayer friend)
        {
            var panel = Modal(friend.name + " ile yarış", "Bir oyun seç; davet arkadaşının ekranında görünecek.");
            var choices = new ScrollView(); choices.style.maxHeight = 360; choices.style.flexShrink = 1; panel.Add(choices);
            foreach (var definition in catalog.Levels(Profile.adult))
            {
                var level = definition;
                Button(choices, level.title, () => { CloseModal(); online.CreateRoom(level, friend.id); }, "secondary", "InviteGame_" + level.id);
            }
            Button(panel, "Geri", CloseModal, "secondary");
        }
        private void RenderOnlineInvites()
        {
            foreach (var invitation in online.State.invites)
            {
                var invite = invitation; var card = Box(onlineContent, "card");
                Text(card, invite.from.name + " seni yarışa davet etti", "path-name");
                Text(card, OnlineGameTitle(invite.gameId, invite.adult) + " · " + (invite.adult ? "Yetişkin" : "Çocuk"), "small");
                var actions = Box(card, "row online-actions");
                Button(actions, "Katıl", () => online.Join(invite.code), "green compact", "AcceptRace_" + invite.roomId);
                Button(actions, "Reddet", () => online.Decline(invite.roomId), "secondary compact", "DeclineRace_" + invite.roomId);
            }
        }
        private void RenderOnlineGames()
        {
            RenderOnlineInvites();
            Text(onlineContent, "Bir oyun seç, oda aç", "section-title");
            Text(onlineContent, "Oda kodunu paylaş. İkiniz de hazır olduğunuzda 5 saniyelik geri sayımla yarış başlar.", "subtitle");
            foreach (var definition in catalog.Levels(Profile.adult))
            {
                var level = definition; var card = Box(onlineContent, "online-person");
                var row = Box(card, "row"); Art(row, level.previewTextureKey); Text(row, level.title, "path-name grow");
                Button(card, "Yarış odası aç", () => online.CreateRoom(level), "green", "CreateOnlineRoom_" + level.id);
            }
        }
        private void RenderOnlineRoom()
        {
            var room = online.Room;
            if (room == null)
            {
                Text(onlineContent, "Aktif bir yarış odan yok.", "subtitle");
                Button(onlineContent, "Yeni yarış", () => ShowOnlineScreen("play"), "green"); return;
            }
            var card = Box(onlineContent, "card"); card.name = "OnlineRoomCard";
            Art(card, room.round.previewTextureKey, "large-art"); Text(card, room.round.title, "section-title");
            Text(card, room.adult ? "Yetişkin yarışı" : "Çocuk yarışı", "small");
            if (room.state == "waiting")
            {
                Text(card, "Oda kodu", "small"); Text(card, DisplayOnlineCode(room.code), "online-code").name = "MyRoomCode";
                Button(card, "Oda kodunu kopyala", () => GUIUtility.systemCopyBuffer = room.code, "secondary compact", "CopyRoomCode");
            }
            foreach (var player in room.players)
            {
                var person = Box(card, "online-member"); Text(person, player.name + (player.id == online.State.me.id ? " · Sen" : ""), "path-name");
                Text(person, room.state == "waiting" ? player.ready ? "Hazır" : "Hazırlanıyor" : player.status == "left" ? "Yarıştan ayrıldı" : player.score + " puan · " + player.done + " / " + room.goal, "small");
            }
            if (room.state == "waiting")
            {
                if (room.players.Length < 2) Text(card, "Arkadaşının katılması bekleniyor…", "subtitle");
                Button(card, online.Self.ready ? "Hazırsın · Arkadaşın bekleniyor" : "Hazırım", online.Ready, "green", "OnlineReady").SetEnabled(!online.Self.ready && !online.Busy);
            }
            else if (room.state == "countdown")
            {
                Text(card, "İkiniz de hazırsınız. Başlıyoruz!", "subtitle"); onlineCountdown = Text(card, "5", "online-countdown"); onlineCountdown.name = "OnlineCountdown";
            }
            else if (room.state == "playing")
            {
                Text(card, online.Self.status == "finished" ? "Tamamladın! Arkadaşının sonucu bekleniyor…" : online.HasPendingActions ? "Yanıtların sunucuya gönderiliyor…" : online.Self.status == "left" ? "Yarıştan ayrıldın." : "Yarış sürüyor…", "subtitle");
                onlineCountdown = Text(card, "", "small");
            }
            else if (room.state == "finished")
            {
                string title = string.IsNullOrEmpty(room.winnerId) ? room.players.All(p => p.status == "left") ? "Yarış tamamlanamadı" : "Berabere!" : room.winnerId == online.State.me.id ? "Yarışı kazandın!" : room.Player(room.winnerId).name + " kazandı";
                Text(card, title, "title").name = "OnlineRaceResult";
                Text(card, "Önce puan, eşit puanda bitirme süresi belirleyici. Tamamlanan oyunların puanı sıralamana eklenir.", "small");
                foreach (var player in room.players.Where(p => p.status == "finished"))
                    Text(card, player.name + " · " + player.score + " puan · " + player.elapsed.ToString("0.0") + " saniye", "paragraph");
            }
            else Text(card, room.state == "expired" ? "Odanın süresi doldu. Yeni bir oda açabilirsin." : "Yarış iptal edildi.", "subtitle");
            if (room.Terminal)
            {
                Button(card, "Yeni yarış", () => online.Acknowledge(() => { miniOnline = false; ShowOnlineScreen("play"); }), "green", "FinishOnlineRace");
                Button(card, "Sıralamayı gör", () => online.Acknowledge(() => { miniOnline = false; ShowOnlineScreen("board"); }), "secondary", "RaceLeaderboard");
            }
            else Button(card, "Odadan ayrıl", ConfirmOnlineExit, "secondary", "LeaveOnlineRoom");
        }
        private void ConfirmOnlineExit()
        {
            if (online.Room == null) { miniOnline = false; ShowOnlineScreen("play"); return; }
            if (online.Room.Terminal) { online.Acknowledge(() => { miniOnline = false; ShowOnlineScreen("play"); }); return; }
            Confirm("Yarıştan ayrılmak istiyor musun?", online.Room.state == "waiting" || online.Room.state == "countdown" ? "Bu yarış odası kapanacak." : "Yarış sürüyor. Ayrılırsan bu tur tamamlanmamış sayılacak.", "Yarıştan ayrıl", () => {
                CloseModal(); online.Leave(() => { miniOnline = false; onlineStartedRoom = null; ShowOnlineScreen("play"); });
            });
        }
        private void BuildBoardFilters()
        {
            var filters = Box(body, "online-filter");
            var scope = new DropdownField("Kimler", new List<string> { "Herkes", "Arkadaşlarım" }, boardScope == "friends" ? 1 : 0) { name = "LeaderboardScope" }; filters.Add(scope);
            scope.RegisterValueChangedCallback(e => { boardScope = e.newValue == "Herkes" ? "global" : "friends"; RequestOnlineBoard(); });
            var period = new DropdownField("Dönem", new List<string> { "Tüm zamanlar", "Son 7 gün" }, boardPeriod == "week" ? 1 : 0) { name = "LeaderboardPeriod" }; filters.Add(period);
            period.RegisterValueChangedCallback(e => { boardPeriod = e.newValue == "Son 7 gün" ? "week" : "all"; RequestOnlineBoard(); });
            var levels = catalog.Levels(Profile.adult); var names = new List<string> { "Tüm oyunlar" }; names.AddRange(levels.Select(l => l.title));
            int index = Array.FindIndex(levels, l => l.id == boardGame)+1;
            var game = new DropdownField("Oyun", names, Math.Max(0,index)) { name = "LeaderboardGame" }; filters.Add(game);
            game.RegisterValueChangedCallback(e => { int selected = names.IndexOf(e.newValue); boardGame = selected <= 0 ? "" : levels[selected-1].id; RequestOnlineBoard(); });
            Text(filters, (Profile.adult ? "Yetişkin" : "Çocuk") + " oyunları · Her oyundaki en iyi yarış puanın toplanır.", "small");
            Button(filters, "Sıralamayı yenile", RequestOnlineBoard, "secondary compact", "RefreshOnlineLeaderboard");
        }
        private void RequestOnlineBoard()
        {
            if (!online.Connected) return;
            boardRequested = true; online.LoadLeaderboard(Profile.adult, boardScope, boardPeriod, boardGame);
        }
        private void RenderOnlineBoard()
        {
            var board = online.Board;
            if (board == null) { Text(onlineContent, "Sıralama yükleniyor…", "subtitle"); return; }
            if (board.entries.Length == 0) Text(onlineContent, "Bu sıralamada henüz sonuç yok. Bir arkadaş yarışı tamamlayarak ilk adımı at.", "subtitle");
            void Entry(OnlinePlayer p)
            {
                var row = Box(onlineContent, "online-person row" + (p.id == online.State.me.id ? " current-player" : ""));
                Text(row, p.rank.ToString(), "online-rank");
                var copy = Box(row, "grow"); Text(copy, p.name + (p.id == online.State.me.id ? " · Sen" : ""), "path-name");
                Text(copy, p.wins + " galibiyet · " + p.played + " yarış", "small"); Text(row, p.score + " puan", "path-name");
            }
            foreach (var p in board.entries) Entry(p);
            if (board.self != null && !board.entries.Any(p => p.id == board.self.id)) { Text(onlineContent, "Senin sıran", "section-title"); Entry(board.self); }
            if (board.self == null) Text(onlineContent, "Sıralamaya girmek için bir çevrimiçi oyunu tamamla.", "small");
        }
        private static string DisplayOnlineCode(string code) => code != null && code.Length == 8 ? code.Substring(0,4) + "-" + code.Substring(4) : code;
        private string OnlineGameTitle(string id, bool adult) => catalog.levels.FirstOrDefault(l => l.id == id && l.adult == adult)?.title ?? "Arkadaş yarışı";
        private void BeginOnlineRound(OnlineRoom room)
        {
            onlineStartedRoom = room.id;
            BeginMiniGame(room.round, room.round, room.seed, online.Self);
            miniOnline = true; backAction = ConfirmOnlineExit;
            onlineHud = new VisualElement(); onlineHud.AddToClassList("online-hud"); onlineHud.name = "OnlineRaceHud"; body.Insert(0, onlineHud);
            onlineScoreLabel = Text(onlineHud, "", "online-score");
            onlineRivalLabel = Text(onlineHud, "", "small");
            onlineClockLabel = Text(onlineHud, "", "online-timer");
        }
        private void UpdateOnlineRace()
        {
            if (online == null || !online.Connected || online.Room == null) return;
            var room = online.Room;
            if ((room.state == "countdown" || room.state == "playing") && online.ServerTime >= room.startsAt &&
                online.Self?.status == "joined" && onlineStartedRoom != room.id) BeginOnlineRound(room);
            if (onlineCountdown != null && onlineScreen == "room")
                onlineCountdown.text = room.state == "countdown" ? Math.Max(0,Math.Ceiling(room.startsAt-online.ServerTime)).ToString("0") : "Kalan süre · " + Math.Max(0,Math.Ceiling(room.deadline-online.ServerTime)) + " sn";
            if (onlineHud == null || !miniOnline) return;
            var rival = room.players.FirstOrDefault(p => p.id != online.State.me.id);
            int score = miniDone >= miniGoal ? Math.Max(100,1000-miniMistakes*75) : Math.Max(0,Mathf.RoundToInt(1000f*miniDone/miniGoal)-miniMistakes*75);
            onlineScoreLabel.text = "Sen · " + score + " puan · " + miniDone + " / " + miniGoal;
            onlineRivalLabel.text = rival == null ? "Rakip bekleniyor" : rival.name + " · " + rival.score + " puan · " + rival.done + " / " + room.goal + (rival.status == "finished" ? " · Bitirdi" : rival.status == "left" ? " · Ayrıldı" : "");
            onlineClockLabel.text = online.LastError ?? "Kalan süre · " + Math.Max(0,Math.Ceiling(room.deadline-online.ServerTime)) + " sn";
        }
        private void OnlineMove(string key, string target = "", int index = 0)
        {
            if (miniOnline) online.QueueAction(key, target, index);
        }
        private void CompleteOnlineRound()
        {
            online.FlushActions(); ShowOnlineScreen("room");
        }
    }
}
