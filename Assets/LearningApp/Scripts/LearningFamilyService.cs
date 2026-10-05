using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deprem.Learning
{
    // Owns network coroutines independently of page/game coroutines in the controller.
    public sealed class LearningFamilyService : MonoBehaviour
    {
        private LearningProgress learning;
        private LearningCatalog catalog;
        private FamilyDeviceStore devices;
        private LearningFamilyConfig config;
        private UnityWebRequest activeRequest;
        private FamilySession parent;
        private int parentGeneration;
        private float nextParentRefresh;
        public bool Busy { get; private set; }
        public bool Available => !string.IsNullOrEmpty(config?.serviceUrl);
        public bool ParentSignedIn => parent != null;
        public string ParentName => parent?.name ?? "";
        public string ParentEmail => parent?.email ?? "";
        public bool PollParent;
        public FamilyChild[] Children { get; private set; } = Array.Empty<FamilyChild>();
        public string ParentRefreshError { get; private set; }
        public string ParentFetchedAt { get; private set; }
        public event Action StatusChanged, ChildrenUpdated;

        public void Initialize(LearningProgress store, LearningCatalog content)
        {
            Shutdown(); learning = store; catalog = content; config = LearningFamilyConfig.Load();
            devices = new FamilyDeviceStore(store.SavePath + ".family.json", config.serviceUrl);
            if (Available) StartCoroutine(SyncLoop());
        }
        public FamilyDeviceCredential Device(string profileId) => devices?.ForProfile(profileId);
        public void MarkDirty(string profileId)
        {
            var device = Device(profileId);
            if (device != null && device.enabled) device.retryAfter = 0;
        }
        public void Shutdown()
        {
            activeRequest?.Abort(); StopAllCoroutines();
            activeRequest?.Dispose(); activeRequest = null; Busy = false;
            LockParent();
        }
        private void OnDisable() => Shutdown();
        private void OnApplicationPause(bool paused) { if (paused) LockParent(); }
        public void LockParent()
        {
            parentGeneration++; parent = null; PollParent = false;
            Children = Array.Empty<FamilyChild>(); ParentFetchedAt = null; ParentRefreshError = null;
            ChildrenUpdated?.Invoke();
        }
        private static FamilyResponse<T> Failure<T>(string message, long status = 0) where T : class => new FamilyResponse<T> { Error = message, Status = status };

        private IEnumerator Api<T>(string method, string path, string token, object payload, Action<FamilyResponse<T>> done) where T : class
        {
            while (activeRequest != null) yield return null;
            if (!Available) { done(Failure<T>("Veli bağlantısı henüz kullanıma açılmadı.")); yield break; }
            var request = new UnityWebRequest(config.serviceUrl + path, method) {
                downloadHandler = new DownloadHandlerBuffer(), timeout = config.timeoutSeconds, redirectLimit = 0
            };
            if (payload != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
            activeRequest = request;
            try
            {
                yield return request.SendWebRequest();
                FamilyResponse<T> result;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var value = JsonUtility.FromJson<T>(request.downloadHandler.text);
                        result = value == null ? Failure<T>("Hizmetin yanıtı okunamadı.", request.responseCode) : new FamilyResponse<T> { Value = value, Status = request.responseCode };
                    }
                    catch { result = Failure<T>("Hizmetin yanıtı okunamadı.", request.responseCode); }
                }
                else
                {
                    string error = "Bağlantı kurulamadı. İnternet geldiğinde tekrar deneyin.";
                    try
                    {
                        var problem = JsonUtility.FromJson<FamilyError>(request.downloadHandler.text);
                        if (!string.IsNullOrEmpty(problem?.message)) error = problem.message;
                    }
                    catch { /* Transport/HTML errors are never exposed in the product. */ }
                    result = Failure<T>(error, request.responseCode);
                }
                done(result);
            }
            finally
            {
                if (activeRequest == request) activeRequest = null;
                request.Dispose();
            }
        }
        private bool Interactive(IEnumerator work)
        {
            if (Busy) return false;
            Busy = true; StatusChanged?.Invoke(); StartCoroutine(InteractiveRoutine(work)); return true;
        }
        private IEnumerator InteractiveRoutine(IEnumerator work)
        {
            try { yield return work; }
            finally { Busy = false; StatusChanged?.Invoke(); }
        }

        public void Authenticate(string email, string password, string name, bool register, Action<string> done)
        { Interactive(AuthenticateRoutine(new FamilyAuthRequest { email = email.Trim(), password = password, name = name.Trim() }, register, parentGeneration, done)); }
        private IEnumerator AuthenticateRoutine(FamilyAuthRequest credentials, bool register, int generation, Action<string> done)
        {
            FamilyResponse<FamilySession> response = null;
            yield return Api<FamilySession>("POST", "/v1/parent/" + (register ? "register" : "login"), null, credentials, r => response = r);
            credentials.password = "";
            if (generation != parentGeneration) yield break;
            if (!response.Success) { done(response.Error); yield break; }
            if (string.IsNullOrEmpty(response.Value.token)) { done("Oturum açılamadı. Tekrar deneyin."); yield break; }
            parent = response.Value;
            yield return FetchChildren(null);
            done(null);
        }
        public void RefreshChildren(Action<string> done = null) { Interactive(FetchChildren(done)); }
        private IEnumerator FetchChildren(Action<string> done)
        {
            if (!ParentSignedIn) { done?.Invoke("Yeniden giriş yapın."); yield break; }
            int generation = parentGeneration;
            FamilyResponse<FamilyChildrenResponse> response = null;
            yield return Api<FamilyChildrenResponse>("GET", "/v1/parent/children", parent.token, null, r => response = r);
            if (generation != parentGeneration) yield break;
            if (response.Success)
            {
                Children = (response.Value.children ?? Array.Empty<FamilyChild>()).Where(c => c?.progress != null).ToArray();
                ParentFetchedAt = DateTime.UtcNow.ToString("o"); ParentRefreshError = null;
            }
            else
            {
                ParentRefreshError = response.Error;
                if (response.Status == 401) { LockParent(); done?.Invoke(response.Error); yield break; }
            }
            nextParentRefresh = Time.unscaledTime + 30;
            ChildrenUpdated?.Invoke(); done?.Invoke(response.Error);
        }
        public void LinkChild(string code, Action<string> done) { Interactive(LinkRoutine(code, parentGeneration, done)); }
        private IEnumerator LinkRoutine(string code, int generation, Action<string> done)
        {
            if (!ParentSignedIn) { done("Yeniden giriş yapın."); yield break; }
            FamilyResponse<FamilyOk> response = null;
            yield return Api<FamilyOk>("POST", "/v1/parent/links", parent.token, new FamilyCodeRequest { code = code }, r => response = r);
            if (generation != parentGeneration) yield break;
            if (!response.Success) { done(response.Error); yield break; }
            yield return FetchChildren(null); done(null);
        }
        public void UnlinkChild(string id, Action<string> done) { Interactive(UnlinkRoutine(id, parentGeneration, done)); }
        private IEnumerator UnlinkRoutine(string id, int generation, Action<string> done)
        {
            if (!ParentSignedIn) { done("Yeniden giriş yapın."); yield break; }
            FamilyResponse<FamilyOk> response = null;
            yield return Api<FamilyOk>("DELETE", "/v1/parent/children/" + Uri.EscapeDataString(id), parent.token, null, r => response = r);
            if (generation != parentGeneration) yield break;
            if (response.Success) { Children = Children.Where(c => c.id != id).ToArray(); ChildrenUpdated?.Invoke(); }
            done(response.Error);
        }
        public void DeleteParentAccount(Action<string> done) { Interactive(DeleteAccountRoutine(parentGeneration, done)); }
        private IEnumerator DeleteAccountRoutine(int generation, Action<string> done)
        {
            if (!ParentSignedIn) { done("Yeniden giriş yapın."); yield break; }
            FamilyResponse<FamilyOk> response = null;
            yield return Api<FamilyOk>("DELETE", "/v1/parent/account", parent.token, null, r => response = r);
            if (generation != parentGeneration) yield break;
            if (response.Success) LockParent();
            done(response.Error);
        }
        public void Logout()
        {
            string token = parent?.token;
            LockParent();
            if (!string.IsNullOrEmpty(token)) StartCoroutine(Api<FamilyOk>("POST", "/v1/parent/logout", token, new FamilyCodeRequest(), _ => { }));
        }

        public void CreateCode(string profileId, Action<FamilyPairCode, string> done)
        {
            if (Busy) return;
            if (!Available) { done(null, "Veli bağlantısı henüz kullanıma açılmadı."); return; }
            var device = devices.Add(profileId);
            if (device.pendingStop) { done(null, "Önceki paylaşım kapatılıyor. İnternet bağlantısıyla biraz sonra tekrar deneyin."); return; }
            if (!device.enabled)
            {
                device.token = FamilyDeviceStore.NewDeviceToken(); device.enabled = true; device.uploadedJson = null;
                if (!devices.Save()) { device.enabled = false; device.token = ""; done(null, devices.LastError); return; }
            }
            Interactive(CreateCodeRoutine(device, done));
        }
        private IEnumerator CreateCodeRoutine(FamilyDeviceCredential device, Action<FamilyPairCode, string> done)
        {
            string error = null;
            yield return Upload(device, true, e => error = e);
            if (error != null) { done(null, error); yield break; }
            FamilyResponse<FamilyPairCode> response = null;
            yield return Api<FamilyPairCode>("POST", "/v1/child/pair-code", device.token, new FamilyCodeRequest(), r => response = r);
            done(response.Value, response.Error);
        }
        public void LoadGuardians(string profileId, Action<FamilyGuardian[], string> done)
        {
            var device = Device(profileId);
            if (device == null || !device.enabled) { done(Array.Empty<FamilyGuardian>(), null); return; }
            Interactive(GuardiansRoutine(device, done));
        }
        private IEnumerator GuardiansRoutine(FamilyDeviceCredential device, Action<FamilyGuardian[], string> done)
        {
            FamilyResponse<FamilyGuardiansResponse> response = null;
            yield return Api<FamilyGuardiansResponse>("GET", "/v1/child/guardians", device.token, null, r => response = r);
            done(response.Value?.guardians ?? Array.Empty<FamilyGuardian>(), response.Error);
        }
        public void RevokeGuardian(string profileId, string parentId, Action<string> done)
        {
            var device = Device(profileId);
            if (device != null) Interactive(RevokeRoutine(device, parentId, done));
        }
        private IEnumerator RevokeRoutine(FamilyDeviceCredential device, string parentId, Action<string> done)
        {
            FamilyResponse<FamilyOk> response = null;
            yield return Api<FamilyOk>("DELETE", "/v1/child/guardians/" + Uri.EscapeDataString(parentId), device.token, null, r => response = r);
            done(response.Error);
        }
        public void StopSharing(string profileId, Action<string> done)
        {
            if (Busy) return;
            var device = Device(profileId);
            if (device == null) { done(null); return; }
            device.enabled = false; device.pendingStop = !string.IsNullOrEmpty(device.token);
            bool saved = devices.Save();
            StatusChanged?.Invoke();
            Interactive(StopRoutine(device, e => done(e ?? (saved ? null : devices.LastError))));
        }
        private IEnumerator StopRoutine(FamilyDeviceCredential device, Action<string> done)
        {
            if (!device.pendingStop) { done(null); yield break; }
            FamilyResponse<FamilyOk> response = null;
            yield return Api<FamilyOk>("DELETE", "/v1/child", device.token, null, r => response = r);
            if (response.Success)
            {
                device.pendingStop = false; device.token = ""; device.uploadedJson = null; device.lastSyncUtc = ""; device.syncError = null;
                devices.Save(); done(null);
            }
            else
            {
                device.retryAfter = Time.unscaledTime + 15;
                done("Paylaşım bu cihazda durduruldu. Velilerin erişimi internet geldiğinde kaldırılacak.");
            }
            StatusChanged?.Invoke();
        }
        private IEnumerator Upload(FamilyDeviceCredential device, bool force, Action<string> done)
        {
            var profile = learning.Data.profiles.FirstOrDefault(p => p.id == device.profileId);
            if (!device.enabled || profile == null) { done("Paylaşım kapalı."); yield break; }
            var payload = new FamilyProgressRequest { progress = FamilySnapshot.FromProfile(profile, catalog) };
            string json = JsonUtility.ToJson(payload);
            if (!force && device.uploadedJson == json) { done(null); yield break; }
            FamilyResponse<FamilyOk> response = null;
            yield return Api<FamilyOk>("PUT", "/v1/child/progress", device.token, payload, r => response = r);
            if (response.Success)
            {
                device.uploadedJson = json; device.lastSyncUtc = response.Value.updatedAt; device.syncError = null; device.failures = 0;
                devices.Save();
            }
            else
            {
                device.syncError = response.Error;
                device.failures = Math.Min(device.failures + 1, 5);
                device.retryAfter = Time.unscaledTime + Math.Min(120, 5 * (1 << device.failures));
                if (response.Status == 410) { device.enabled = false; device.token = ""; devices.Save(); }
            }
            StatusChanged?.Invoke(); done(response.Error);
        }
        private IEnumerator SyncLoop()
        {
            yield return new WaitForSecondsRealtime(2);
            while (true)
            {
                foreach (var device in devices.Data.children.ToArray())
                {
                    if (Busy || Time.unscaledTime < device.retryAfter) continue;
                    if (device.pendingStop) yield return StopRoutine(device, _ => { });
                    else if (device.enabled) yield return Upload(device, false, _ => { });
                }
                if (!Busy && PollParent && ParentSignedIn && Time.unscaledTime >= nextParentRefresh) yield return FetchChildren(null);
                yield return new WaitForSecondsRealtime(2);
            }
        }
    }
}
