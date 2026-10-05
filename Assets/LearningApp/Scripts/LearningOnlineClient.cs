using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deprem.Learning
{
    [Serializable] public sealed class OnlinePlayer
    {
        public string id, name, code, status;
        public bool online, ready;
        public int done, mistakes, score, eventSeq, rank, wins, played;
        public double elapsed;
        public string[] accepted;
    }
    [Serializable] public sealed class OnlineInvite
    {
        public string roomId, code, gameId;
        public bool adult;
        public OnlinePlayer from;
    }
    [Serializable] public sealed class OnlineRoom
    {
        public string id, code, ownerId, gameId, state, winnerId;
        public bool adult;
        public int seed, goal;
        public double startsAt, deadline;
        public LearningLevel round;
        public OnlinePlayer[] players;
        public bool Terminal => state == "finished" || state == "cancelled" || state == "expired";
        public OnlinePlayer Player(string id) => players?.FirstOrDefault(p => p.id == id);
    }
    [Serializable] public sealed class OnlineSnapshot
    {
        public double serverTime;
        public OnlinePlayer me;
        public OnlinePlayer[] friends, incoming, outgoing;
        public OnlineInvite[] invites;
        public OnlineRoom room;
        public void Normalize()
        {
            // JsonUtility can materialize a default object for a JSON null reference.
            if (string.IsNullOrEmpty(room?.id)) room = null;
            friends ??= Array.Empty<OnlinePlayer>(); incoming ??= Array.Empty<OnlinePlayer>(); outgoing ??= Array.Empty<OnlinePlayer>();
            invites ??= Array.Empty<OnlineInvite>();
        }
    }
    [Serializable] public sealed class OnlineLeaderboard
    {
        public OnlinePlayer[] entries;
        public OnlinePlayer self;
        public double serverTime;
        public void Normalize()
        {
            if (string.IsNullOrEmpty(self?.id)) self = null;
            entries ??= Array.Empty<OnlinePlayer>();
        }
    }
    [Serializable] public sealed class OnlineAction
    {
        public int seq, index;
        public string key, target;
    }
    [Serializable] public sealed class OnlineConfig { public string baseUrl; }
    [Serializable] public sealed class OnlineIdentity
    {
        public string profileId, endpoint, credential, recordedRoom;
    }
    [Serializable] public sealed class OnlineSettings
    {
        public string endpoint;
        public List<OnlineIdentity> identities = new List<OnlineIdentity>();
    }

    // Credentials are separate from lesson/plan data and isolated by server and local profile.
    public sealed class OnlineIdentityStore
    {
        public OnlineSettings Data { get; private set; } = new OnlineSettings();
        public string Path { get; }
        public OnlineIdentityStore(string path)
        {
            Path = path;
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var loaded = JsonUtility.FromJson<OnlineSettings>(File.ReadAllText(candidate));
                    if (loaded?.identities == null) continue;
                    Data = loaded; break;
                }
                catch (Exception) { /* A valid backup is tried before creating a new identity. */ }
            }
        }
        public OnlineIdentity Identity(string profileId, string endpoint)
        {
            var identity = Data.identities.FirstOrDefault(i => i.profileId == profileId && i.endpoint == endpoint);
            if (identity != null) return identity;
            var bytes = new byte[32];
            using (var generator = System.Security.Cryptography.RandomNumberGenerator.Create()) generator.GetBytes(bytes);
            identity = new OnlineIdentity { profileId = profileId, endpoint = endpoint, credential = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant() };
            Data.identities.Add(identity);
            return identity;
        }
        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                File.WriteAllText(Path + ".tmp", JsonUtility.ToJson(Data, true));
                if (File.Exists(Path)) File.Replace(Path + ".tmp", Path, Path + ".bak");
                else File.Move(Path + ".tmp", Path);
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    // Owns requests independently of page/game coroutines, which ResetPage stops.
    public sealed class LearningOnlineClient : MonoBehaviour
    {
        [Serializable] private sealed class SessionRequest { public string name; }
        [Serializable] private sealed class FriendRequest { public string code, playerId; public bool accept; }
        [Serializable] private sealed class RoomRequest { public string gameId, friendId, requestId, code; public bool adult; }
        [Serializable] private sealed class ActionRequest { public OnlineAction[] events; }
        [Serializable] private sealed class ErrorResponse { public string error; }

        private OnlineIdentityStore vault;
        private OnlineIdentity identity;
        private string profileId, playerName;
        private UnityWebRequest currentRequest;
        private readonly List<OnlineAction> pending = new List<OnlineAction>();
        private int generation, eventSequence;
        private string actionRoom;
        private string pendingBoardQuery;
        private double serverBaseTime, receivedAt, nextRequest;
        private float retryDelay = 2;
        public event Action Changed;
        public OnlineSnapshot State { get; private set; }
        public OnlineLeaderboard Board { get; private set; }
        public bool Connected { get; private set; }
        public bool Busy { get; private set; }
        public string LastError { get; private set; }
        public string Endpoint => vault?.Data.endpoint ?? "";
        public OnlineRoom Room => State?.room;
        public OnlinePlayer Self => Room?.Player(State?.me?.id);
        public double ServerTime => serverBaseTime + Time.realtimeSinceStartupAsDouble - receivedAt;
        public string RecordedRoom => identity?.recordedRoom;
        public bool HasPendingActions => pending.Count > 0;

        public void Initialize(string localProfile, string name, string savePath)
        {
            if (vault == null)
            {
                vault = new OnlineIdentityStore(savePath);
                if (string.IsNullOrEmpty(vault.Data.endpoint))
                {
                    var config = Resources.Load<TextAsset>("LearningApp/OnlineConfig");
                    vault.Data.endpoint = config != null ? JsonUtility.FromJson<OnlineConfig>(config.text).baseUrl ?? "" : "";
                }
            }
            if (profileId == localProfile) { playerName = name; return; }
            Disconnect(); profileId = localProfile; playerName = name;
        }
        public static bool ValidateEndpoint(string input, bool allowLocalHttp, out string endpoint)
        {
            endpoint = (input ?? "").Trim().TrimEnd('/');
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/") return false;
            return uri.Scheme == "https" || (allowLocalHttp && uri.Scheme == "http");
        }
        public bool SetEndpoint(string input)
        {
            bool development = Application.isEditor || Debug.isDebugBuild;
            if (!ValidateEndpoint(input, development, out string endpoint))
            { LastError = development ? "Geçerli bir http:// veya https:// sunucu adresi yaz." : "Geçerli bir https:// sunucu adresi yaz."; Changed?.Invoke(); return false; }
            Disconnect(); vault.Data.endpoint = endpoint;
            if (!vault.Save()) { LastError = "Bağlantı ayarı cihaza kaydedilemedi."; Changed?.Invoke(); return false; }
            return true;
        }
        public void Connect()
        {
            if (Busy) return;
            if (!ValidateEndpoint(Endpoint, Application.isEditor || Debug.isDebugBuild, out var endpoint))
            { LastError = "Çevrimiçi sunucu adresini bağlantı ayarından gir."; Changed?.Invoke(); return; }
            identity = vault.Identity(profileId, endpoint);
            // Persist before registering so retrying an unknown outcome uses the same account.
            if (!vault.Save()) { LastError = "Çevrimiçi hesap cihaza kaydedilemedi. Cihazdaki boş alanı kontrol et."; Changed?.Invoke(); return; }
            StartCoroutine(Request<OnlineSnapshot>("POST", "/v1/session", new SessionRequest { name = playerName }, snapshot => { Connected = true; Apply(snapshot); }));
        }
        public void Disconnect()
        {
            generation++; currentRequest?.Abort(); StopAllCoroutines(); currentRequest?.Dispose(); currentRequest = null;
            Busy = Connected = false; State = null; Board = null; LastError = null;
            pending.Clear(); actionRoom = null; eventSequence = 0; nextRequest = 0;
            pendingBoardQuery = null;
        }
        private void OnDisable() => Disconnect();
        private void Update()
        {
            if (!Connected || Busy || Time.realtimeSinceStartupAsDouble < nextRequest) return;
            if (pending.Count > 0 && Room?.state == "playing" && Self?.status == "joined") FlushActions();
            else if (pendingBoardQuery != null)
            {
                string query = pendingBoardQuery; pendingBoardQuery = null;
                StartCoroutine(Request<OnlineLeaderboard>("GET", query, null, board => { board.Normalize(); Board = board; }));
            }
            else StartCoroutine(Request<OnlineSnapshot>("GET", "/v1/state", null, Apply));
        }
        private void Apply(OnlineSnapshot snapshot)
        {
            snapshot.Normalize();
            State = snapshot;
            serverBaseTime = snapshot.serverTime; receivedAt = Time.realtimeSinceStartupAsDouble;
            if (Room?.id != actionRoom)
            {
                pending.Clear(); actionRoom = Room?.id; eventSequence = Self?.eventSeq ?? 0;
            }
            else if (Self != null)
            {
                pending.RemoveAll(a => a.seq <= Self.eventSeq);
                eventSequence = Math.Max(eventSequence, Self.eventSeq);
            }
            if (Room == null || Room.Terminal || Self?.status == "left") pending.Clear();
        }
        private void Mutate(string path, object payload = null, Action completed = null)
        {
            if (!Connected) { LastError = "Önce çevrimiçi hesabına bağlan."; Changed?.Invoke(); return; }
            if (Busy) { LastError = "Bağlantı işlemi sürüyor. Biraz sonra tekrar dene."; Changed?.Invoke(); return; }
            StartCoroutine(Request<OnlineSnapshot>("POST", path, payload, snapshot => { Apply(snapshot); completed?.Invoke(); }));
        }
        public void AddFriend(string code) => Mutate("/v1/friends/request", new FriendRequest { code = code });
        public void Respond(string player, bool accept) => Mutate("/v1/friends/respond", new FriendRequest { playerId = player, accept = accept });
        public void RemoveFriend(string player) => Mutate("/v1/friends/remove", new FriendRequest { playerId = player });
        public void CreateRoom(LearningLevel level, string friend = null) => Mutate("/v1/rooms", new RoomRequest { gameId = level.id, adult = level.adult, friendId = friend, requestId = Guid.NewGuid().ToString("N") });
        public void Join(string code) => Mutate("/v1/rooms/join", new RoomRequest { code = code });
        public void Ready() { if (Room != null) Mutate("/v1/rooms/" + Room.id + "/ready"); }
        public void Leave(Action completed) { if (Room != null) Mutate("/v1/rooms/" + Room.id + "/leave", null, completed); else completed?.Invoke(); }
        public void Acknowledge(Action completed) { if (Room != null) Mutate("/v1/rooms/" + Room.id + "/ack", null, completed); else completed?.Invoke(); }
        public void Decline(string room) => Mutate("/v1/rooms/" + room + "/decline");
        public void QueueAction(string key, string target = "", int index = 0)
        {
            if (Room == null || Room.Terminal) return;
            pending.Add(new OnlineAction { seq = ++eventSequence, key = key, target = target, index = index });
            nextRequest = Math.Min(nextRequest, Time.realtimeSinceStartupAsDouble + .15);
        }
        public void FlushActions()
        {
            if (!Connected || Busy || pending.Count == 0 || Room == null) return;
            var events = pending.Take(32).ToArray();
            StartCoroutine(Request<OnlineSnapshot>("POST", "/v1/rooms/" + Room.id + "/actions", new ActionRequest { events = events }, Apply));
        }
        public void LoadLeaderboard(bool adult, string scope, string period, string game = "")
        {
            if (!Connected) return;
            Board = null;
            pendingBoardQuery = "/v1/leaderboard?adult=" + (adult ? "true" : "false") +
                "&scope=" + UnityWebRequest.EscapeURL(scope) + "&period=" + UnityWebRequest.EscapeURL(period) + "&game=" + UnityWebRequest.EscapeURL(game);
            nextRequest = 0;
        }
        public void RecordLocalResult(string room)
        {
            identity.recordedRoom = room; vault.Save();
        }
        private IEnumerator Request<T>(string method, string path, object payload, Action<T> success) where T : class
        {
            Busy = true;
            int requestGeneration = generation;
            UnityWebRequest request = null;
            UnityWebRequestAsyncOperation operation = null;
            string error = null;
            try
            {
                request = new UnityWebRequest(Endpoint + path, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 10 };
                if (method == "POST")
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload == null ? "{}" : JsonUtility.ToJson(payload)));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.SetRequestHeader("Authorization", "Bearer " + identity.credential);
                currentRequest = request;
                operation = request.SendWebRequest();
            }
            catch (Exception) { error = "Sunucuya bağlanılamadı. Bağlantı ayarını ve internetini kontrol et."; }
            if (operation != null) yield return operation;
            if (requestGeneration != generation) { request?.Dispose(); yield break; }
            if (error == null && request.result != UnityWebRequest.Result.Success)
            {
                error = "Sunucuya ulaşılamıyor. Bağlantı yeniden deneniyor…";
                try { error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text)?.error ?? error; } catch (Exception) { }
                if (request.responseCode == 401) Connected = false;
            }
            T response = null;
            if (error == null)
            {
                try { response = JsonUtility.FromJson<T>(request.downloadHandler.text); if (response == null) throw new InvalidDataException(); }
                catch (Exception) { error = "Sunucu yanıtı okunamadı. Tekrar dene."; }
            }
            request?.Dispose(); currentRequest = null; Busy = false; LastError = error;
            retryDelay = error == null ? 2 : Math.Min(10, retryDelay * 1.5f);
            nextRequest = Time.realtimeSinceStartupAsDouble + (error == null && pending.Count > 0 ? .15 : retryDelay);
            if (error == null) success(response);
            Changed?.Invoke();
        }
    }
}
