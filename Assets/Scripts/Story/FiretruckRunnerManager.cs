using System;
using Deprem.Minigames;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deprem.Story
{
    /// <summary>Only drives scene-authored objects. No runtime geometry, UI or pool creation.</summary>
    [DisallowMultipleComponent]
    public sealed class FiretruckRunnerManager : MonoBehaviour
    {
        public enum RunState { Garage, Countdown, Driving, Paused, Results }
        public enum PowerKind { Shield, Magnet, Turbo }
        [Serializable] public sealed class Hazard
        {
            public Transform root;
            public Vector2 halfExtents = new Vector2(1.1f, 1f);
            public bool moving;
            [NonSerialized] public bool passed;
            [NonSerialized] public float approachTravel;
        }
        [Serializable] public sealed class PowerPickup
        {
            public Transform root;
            public PowerKind kind;
        }
        [Serializable] public sealed class TrackSection
        {
            public Transform root;
            public Hazard[] hazards = Array.Empty<Hazard>();
            public Transform[] coins = Array.Empty<Transform>();
            public PowerPickup[] powers = Array.Empty<PowerPickup>();
            [NonSerialized] public Vector3 origin;
            [NonSerialized] public Vector3[] hazardOrigins, coinOrigins, powerOrigins;
        }
        [Serializable] private sealed class GarageSave
        {
            public int wallet, shield, magnet, turbo, bestScore;
            public float bestDistance;
        }

        [Header("Scene-authored pool")]
        public Transform truck;
        public Camera runnerCamera;
        public TrackSection[] sections = Array.Empty<TrackSection>();
        public float sectionLength = 96f, laneWidth = 3.1f, laneChangeSpeed = 15f;
        public float forwardSpeed = 17f, maximumSpeed = 29f, countdownDuration = 2.5f;
        public Light redBeacon, blueBeacon;
        public AudioSource engineAudio, hitAudio, coinAudio, powerAudio;
        public GameObject shieldVisual, turboVisual;
        [Header("Authored HUD")]
        public GameObject garagePanel, completionPanel, pausePanel, drivingHud;
        public TMP_Text distanceText, coinCountText, missionText, countdownText;
        public TMP_Text scoreText, speedText, healthText, powerText, comboText;
        public TMP_Text completionStats, walletText, recordText, garageMessage;
        public TMP_Text[] upgradeLabels = Array.Empty<TMP_Text>();
        public Button[] upgradeButtons = Array.Empty<Button>();
        public Image missionFill;
        public CanvasGroup hitFlash;
        public Button turboButton;
        [Header("Apo guide")]
        public GameObject guidePanel;
        public TMP_Text guideText;
        public Animator guideAnimator;
        [TextArea] public string[] guideTips = Array.Empty<string>();
        public MinigameSessionManager resultReporter;

        private const string SaveKey = "Deprem.FiretruckRunner.Garage.v2";
        private GarageSave save;
        private RunState state, resumeState;
        private int lane, health, collisionCount, coinCount, score, streak, powersCollected;
        private int challenge, challengeCoins, challengePowers, recycledSections, lastBankedCoins;
        private float distance, runSeconds, clock, countdown, shieldUntil, magnetUntil, turboUntil;
        private float invulnerableUntil, slowUntil, shakeUntil, challengeDistance, guideUntil;
        private float nextHud, previousTruckX;
        private bool pointerTracking, saveDirty, boostCharged, resultReported;
        private Vector2 pointerStart;
        private Vector3 truckOrigin, cameraOrigin;

        public RunState State => state;
        public float Distance => distance;
        public float CurrentSpeed => Mathf.Min(maximumSpeed, forwardSpeed + distance / 170f) *
            (TurboActive ? 1.32f : clock < slowUntil ? .58f : 1f);
        public int CollisionCount => collisionCount;
        public int CollectedCoinCount => coinCount;
        public int AvailableCoinCount => Mathf.Max(42, coinCount);
        public float RemainingSeconds => float.PositiveInfinity;
        public int Health => health;
        public int Score => score;
        public int Wallet => save != null ? save.wallet : 0;
        public int RecycledSections => recycledSections;
        public bool ShieldActive => clock < shieldUntil;
        public bool MagnetActive => clock < magnetUntil;
        public bool TurboActive => clock < turboUntil;

        private void Awake()
        {
            try { save = JsonUtility.FromJson<GarageSave>(PlayerPrefs.GetString(SaveKey, "{}")); }
            catch (ArgumentException) { save = null; }
            save ??= new GarageSave();
            save.wallet = Mathf.Clamp(save.wallet, 0, 10000000);
            save.shield = Mathf.Clamp(save.shield, 0, 5);
            save.magnet = Mathf.Clamp(save.magnet, 0, 5);
            save.turbo = Mathf.Clamp(save.turbo, 0, 5);
            if (truck == null || runnerCamera == null || sections.Length < 3)
            {
                Debug.LogError("Runner scene references are incomplete.", this);
                enabled = false;
                return;
            }
            truckOrigin = truck.position;
            cameraOrigin = runnerCamera.transform.position;
            foreach (TrackSection s in sections)
            {
                s.origin = s.root.localPosition;
                s.hazardOrigins = new Vector3[s.hazards.Length];
                s.coinOrigins = new Vector3[s.coins.Length];
                s.powerOrigins = new Vector3[s.powers.Length];
                for (int i = 0; i < s.hazards.Length; i++) s.hazardOrigins[i] = s.hazards[i].root.localPosition;
                for (int i = 0; i < s.coins.Length; i++) s.coinOrigins[i] = s.coins[i].localPosition;
                for (int i = 0; i < s.powers.Length; i++) s.powerOrigins[i] = s.powers[i].root.localPosition;
            }
            SetState(RunState.Garage);
            RefreshGarage();

        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (state == RunState.Paused) Resume();
                else if (state == RunState.Driving || state == RunState.Countdown) Pause();
            }
            if (state == RunState.Paused || Time.timeScale <= 0f) return;
            float dt = Mathf.Min(Time.deltaTime, .1f);
            clock += dt;
            AnimateScene(dt);
            if (guidePanel != null && clock > guideUntil) guidePanel.SetActive(false);
            if (state == RunState.Countdown)
            {
                countdown -= dt;
                countdownText.text = countdown > .3f ? Mathf.CeilToInt(countdown).ToString() : "Başla";
                if (countdown <= 0f) { SetState(RunState.Driving); }
                return;
            }
            if (state != RunState.Driving) return;
            ReadInput();
            float travel = CurrentSpeed * dt;
            runSeconds += dt;
            distance += travel;
            previousTruckX = truck.position.x;
            MoveTruck(dt);
            MoveTrack(travel);
            DetectPickups(travel, dt);
            DetectHazards(travel, dt);
            if (state != RunState.Driving) return;
            UpdateChallenge();
            score = Mathf.FloorToInt(distance * 2f) + coinCount * 12 + challenge * 250;
            if (clock >= nextHud) { UpdateHud(); nextHud = clock + .08f; }
        }

        public void StartRun()
        {
            if (state != RunState.Garage && state != RunState.Results) return;
            BankRun();
            lane = collisionCount = coinCount = score = streak = powersCollected = challenge = recycledSections = 0;
            challengeCoins = challengePowers = lastBankedCoins = 0;
            distance = runSeconds = challengeDistance = 0;
            shieldUntil = magnetUntil = turboUntil = invulnerableUntil = slowUntil = shakeUntil = 0;
            countdown = countdownDuration;
            health = 3;
            boostCharged = true;
            truck.position = truckOrigin;
            truck.rotation = Quaternion.identity;
            runnerCamera.transform.position = cameraOrigin;
            foreach (TrackSection s in sections) { s.root.localPosition = s.origin; ResetSection(s, false); }
            if (hitFlash != null) hitFlash.alpha = 0;
            if (missionFill != null) missionFill.fillAmount = 0;
            SetState(RunState.Countdown);
            if (engineAudio != null) engineAudio.Play();
            Say("Şerit değiştirmek için kaydır veya ← → kullan.", 4f);
            UpdateHud();
        }
        public void Restart() => StartRun();
        public void MoveLeft() => ChangeLane(-1);
        public void MoveRight() => ChangeLane(1);
        private void ChangeLane(int direction)
        {
            if (state == RunState.Driving) lane = Mathf.Clamp(lane + direction, -1, 1);
        }
        private void ReadInput()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) ChangeLane(-1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ChangeLane(1);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)) ActivateTurbo();
            bool touch = Input.touchCount > 0;
            Vector2 position = touch ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            bool down = touch ? Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButtonDown(0);
            bool up = touch ? Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled : Input.GetMouseButtonUp(0);
            if (down)
            {
                bool overUi = EventSystem.current != null && (touch
                    ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                    : EventSystem.current.IsPointerOverGameObject());
                pointerTracking = !overUi;
                pointerStart = position;
            }
            if (pointerTracking)
            {
                Vector2 delta = position - pointerStart;
                float threshold = Mathf.Max(28f, Screen.width * .045f);
                if (Mathf.Abs(delta.x) > threshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    ChangeLane(delta.x > 0 ? 1 : -1);
                    pointerStart = position;
                }
                else if (delta.y > threshold * 1.3f) { ActivateTurbo(); pointerTracking = false; }
            }
            if (up) pointerTracking = false;
        }
        private void MoveTruck(float dt)
        {
            Vector3 p = truck.position;
            p.x = Mathf.MoveTowards(p.x, lane * laneWidth, laneChangeSpeed * dt);
            p.y = truckOrigin.y + Mathf.Sin(clock * 12f) * .013f;
            truck.position = p;
            float lean = Mathf.Clamp((lane * laneWidth - p.x) / laneWidth, -1, 1);
            truck.rotation = Quaternion.Slerp(truck.rotation, Quaternion.Euler(0, lean * 9, -lean * 5), 12f * dt);
        }
        private void MoveTrack(float travel)
        {
            foreach (TrackSection s in sections) s.root.position += Vector3.back * travel;
            foreach (TrackSection s in sections)
            {
                // Recycle only after the far edge has left the camera. Keep coordinates near origin.
                if (s.root.position.z + sectionLength >= -28f) continue;
                float farthest = float.NegativeInfinity;
                foreach (TrackSection other in sections) farthest = Mathf.Max(farthest, other.root.position.z);
                s.root.position = new Vector3(0, 0, farthest + sectionLength);
                ResetSection(s, UnityEngine.Random.value > .5f);
                recycledSections++;
            }
        }
        private void ResetSection(TrackSection s, bool mirror)
        {
            float sign = mirror ? -1f : 1f;
            for (int i = 0; i < s.hazards.Length; i++)
            {
                Vector3 p = s.hazardOrigins[i]; p.x *= sign;
                s.hazards[i].root.localPosition = p;
                s.hazards[i].root.gameObject.SetActive(true);
                s.hazards[i].passed = false;
                s.hazards[i].approachTravel = 0;
            }
            for (int i = 0; i < s.coins.Length; i++)
            {
                Vector3 p = s.coinOrigins[i]; p.x *= sign;
                s.coins[i].localPosition = p;
                s.coins[i].gameObject.SetActive(true);
            }
            for (int i = 0; i < s.powers.Length; i++)
            {
                Vector3 p = s.powerOrigins[i]; p.x *= sign;
                s.powers[i].root.localPosition = p;
                s.powers[i].root.gameObject.SetActive(true);
            }
        }
        private void DetectHazards(float travel, float dt)
        {
            foreach (TrackSection s in sections)
            foreach (Hazard h in s.hazards)
            {
                if (h.passed || !h.root.gameObject.activeSelf) continue;
                float approach = h.moving && h.root.position.z - truck.position.z < 75f
                    ? Mathf.Min(7f - h.approachTravel, CurrentSpeed * .28f * dt) : 0;
                h.approachTravel += approach;
                h.root.position += Vector3.back * approach;
                Vector3 delta = h.root.position - truck.position;
                float zRadius = h.halfExtents.y + 2.05f;
                // Sweep travel and lane movement to avoid tunnelling at turbo speed.
                if (delta.z <= zRadius && delta.z + travel + approach >= -zRadius)
                {
                    float minX = Mathf.Min(previousTruckX, truck.position.x) - h.halfExtents.x - .92f;
                    float maxX = Mathf.Max(previousTruckX, truck.position.x) + h.halfExtents.x + .92f;
                    if (h.root.position.x >= minX && h.root.position.x <= maxX)
                    {
                        h.passed = true;
                        if (clock < invulnerableUntil) continue;
                        if (TurboActive || ShieldActive)
                        {
                            if (!TurboActive) shieldUntil = 0;
                            invulnerableUntil = clock + .65f;
                            h.root.gameObject.SetActive(false);
                            Say("Darbe engellendi.", 1.8f);
                        }
                        else RegisterHit();
                        if (state == RunState.Results) return;
                    }
                }
                if (delta.z < -zRadius)
                {
                    h.passed = true;
                    if (Mathf.Abs(delta.x) < 2.9f) streak++;
                }
            }
        }
        private void RegisterHit()
        {
            health--; collisionCount++; streak = 0;
            invulnerableUntil = clock + 1.55f; slowUntil = clock + .85f; shakeUntil = clock + .22f;
            if (hitFlash != null) hitFlash.alpha = .65f;
            if (hitAudio != null) hitAudio.Play();
            Say(health > 0 ? health + " can kaldı. Boş şeride geç."
                : "Tur bitti.", 2.5f);
            UpdateHud();
            if (health <= 0) FinishRun();
        }
        private void DetectPickups(float travel, float dt)
        {
            foreach (TrackSection s in sections)
            {
                foreach (Transform coin in s.coins)
                {
                    if (!coin.gameObject.activeSelf) continue;
                    Vector3 delta = coin.position - truck.position;
                    if (MagnetActive && delta.z < 17f && delta.z > -2f)
                        coin.position = Vector3.MoveTowards(coin.position, truck.position + Vector3.up, dt * 24f);
                    delta = coin.position - truck.position;
                    if (delta.z <= 2.3f && delta.z + travel >= -2.3f && Mathf.Abs(delta.x) < 1.4f)
                    {
                        coin.gameObject.SetActive(false); coinCount++; streak++;
                        if (coinAudio != null) { coinAudio.pitch = 1f + Mathf.Min(streak, 12) * .025f; coinAudio.Play(); }
                    }
                }
                foreach (PowerPickup p in s.powers)
                {
                    if (!p.root.gameObject.activeSelf) continue;
                    Vector3 d = p.root.position - truck.position;
                    if (d.z > 2.5f || d.z + travel < -2.5f || Mathf.Abs(d.x) > 1.5f) continue;
                    p.root.gameObject.SetActive(false); ApplyPower(p.kind);
                }
            }
        }
        private void ApplyPower(PowerKind kind)
        {
            powersCollected++;
            if (kind == PowerKind.Shield) { shieldUntil = clock + 8f + save.shield * 2f; Say("Kalkan açık. Bir darbeyi karşılar.", 2.5f); }
            else if (kind == PowerKind.Magnet) { magnetUntil = clock + 7f + save.magnet * 2f; Say("Mıknatıs açık. Madalyalar sana gelir.", 2.5f); }
            else { turboUntil = clock + 3f + save.turbo * .7f; Say("Turbo açık. Bu sürede darbe almazsın.", 2.5f); }
            if (powerAudio != null) powerAudio.Play();
            UpdateHud();
        }
        public void ActivateTurbo()
        {
            if (state != RunState.Driving || !boostCharged) return;
            boostCharged = false; ApplyPower(PowerKind.Turbo);
        }
        private void UpdateChallenge()
        {
            int type = challenge % 3;
            float p = type == 0 ? (distance - challengeDistance) / (300f + challenge * 20f)
                : type == 1 ? (coinCount - challengeCoins) / 15f : (powersCollected - challengePowers) / 2f;
            if (missionFill != null) missionFill.fillAmount = Mathf.Clamp01(p);
            if (p < 1) return;
            int reward = 15 + challenge * 3;
            save.wallet += reward; saveDirty = true; challenge++;
            challengeDistance = distance; challengeCoins = coinCount; challengePowers = powersCollected;
            Say("Hedef tamamlandı. +" + reward + " İMO", 2.5f);
            BankRun();
        }
        private void FinishRun()
        {
            SetState(RunState.Results);
            score = Mathf.FloorToInt(distance * 2f) + coinCount * 12 + challenge * 250;
            bool record = score > save.bestScore;
            BankRun();
            completionStats.text = (record ? "Yeni rekor" : "Bu tur") + "\n<size=58>" + score +
                "</size> puan\n" + Mathf.FloorToInt(distance) + " m    •    " + coinCount + " İMO\n" +
                challenge + " görev    •    " + Mathf.FloorToInt(runSeconds) + " sn";
            RefreshGarage();
            if (!resultReported && resultReporter != null)
            {
                resultReported = true;
                resultReporter.ReportExternalFiretruck(collisionCount, coinCount, AvailableCoinCount);
            }
        }
        private void BankRun()
        {
            if (save == null) return;
            int earned = Mathf.Max(0, coinCount - lastBankedCoins);
            save.wallet = Mathf.Min(10000000, save.wallet + earned); lastBankedCoins = coinCount;
            if (score > save.bestScore) { save.bestScore = score; saveDirty = true; }
            if (distance > save.bestDistance) { save.bestDistance = distance; saveDirty = true; }
            saveDirty |= earned > 0;
            if (!saveDirty) return;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save)); PlayerPrefs.Save(); saveDirty = false;
        }
        public void ShowGarage()
        {
            if (state != RunState.Results && state != RunState.Garage) return;
            SetState(RunState.Garage); RefreshGarage();

        }
        public void UpgradeShield() => Upgrade(0);
        public void UpgradeMagnet() => Upgrade(1);
        public void UpgradeTurbo() => Upgrade(2);
        public static int UpgradeCost(int level) => 45 + level * 40;
        private void Upgrade(int kind)
        {
            if (state != RunState.Garage) return;
            int level = kind == 0 ? save.shield : kind == 1 ? save.magnet : save.turbo;
            int cost = UpgradeCost(level);
            if (level >= 5) return;
            if (save.wallet < cost) { garageMessage.text = (cost - save.wallet) + " İMO daha gerekiyor."; return; }
            save.wallet -= cost;
            if (kind == 0) save.shield++; else if (kind == 1) save.magnet++; else save.turbo++;
            saveDirty = true; BankRun(); RefreshGarage();
            garageMessage.text = "Geliştirildi. Sonraki turda daha uzun sürer.";
            if (powerAudio != null) powerAudio.Play();
        }
        private void RefreshGarage()
        {
            walletText.text = save.wallet + " İMO";
            recordText.text = "Rekor  " + save.bestScore + "   •   " + Mathf.FloorToInt(save.bestDistance) + " m";
            garageMessage.text = "3 can · 1 turbo hakkı. Topladığın madalyalarla süreleri uzat.";
            for (int i = 0; i < upgradeLabels.Length; i++)
            {
                int level = i == 0 ? save.shield : i == 1 ? save.magnet : save.turbo;
                float duration = i == 0 ? 8 + level * 2 : i == 1 ? 7 + level * 2 : 3 + level * .7f;
                upgradeLabels[i].text = "Seviye " + level + "  ·  " + duration.ToString("0.#") + " sn\n" +
                    (level >= 5 ? "En üst seviye" : UpgradeCost(level) + " İMO  ·  Geliştir");
                if (i < upgradeButtons.Length) upgradeButtons[i].interactable = level < 5;
            }
        }
        public void Pause()
        {
            if (state != RunState.Driving && state != RunState.Countdown) return;
            resumeState = state; SetState(RunState.Paused); BankRun();
        }
        public void Resume() { if (state == RunState.Paused) SetState(resumeState); }
        public void ReturnToHub()
        {
            BankRun();
            if (resultReporter != null) resultReporter.ReturnToHub(); else SceneManager.LoadScene("Minigame_Hub");
        }
        private void OnApplicationPause(bool paused) { if (paused) { Pause(); BankRun(); } }
        private void OnApplicationFocus(bool focus) { if (!focus) Pause(); }
        private void OnDisable() => BankRun();
        private void SetState(RunState value)
        {
            state = value; pointerTracking = false;
            if (guidePanel != null && value != RunState.Driving && value != RunState.Countdown) guidePanel.SetActive(false);
            garagePanel.SetActive(value == RunState.Garage);
            completionPanel.SetActive(value == RunState.Results);
            pausePanel.SetActive(value == RunState.Paused);
            drivingHud.SetActive(value == RunState.Driving || value == RunState.Countdown || value == RunState.Paused);
            countdownText.gameObject.SetActive(value == RunState.Countdown);
            if (engineAudio != null)
            {
                if (value == RunState.Driving || value == RunState.Countdown) engineAudio.UnPause();
                else engineAudio.Pause();
            }
            if (guideAnimator != null) guideAnimator.speed = value == RunState.Paused ? 0 : 1;
        }
        private void Say(string message, float duration)
        {
            if (guidePanel != null) guidePanel.SetActive(true);
            if (guideText != null) guideText.text = message;
            if (guideAnimator != null) guideAnimator.SetTrigger("Explain");
            guideUntil = clock + duration;
        }
        private void UpdateHud()
        {
            distanceText.text = Mathf.FloorToInt(distance).ToString("N0") + " m";
            coinCountText.text = coinCount.ToString(); scoreText.text = score.ToString("N0");
            speedText.text = Mathf.RoundToInt(CurrentSpeed * 3.6f) + " km/sa";
            healthText.text = health == 3 ? "● ● ●" : health == 2 ? "● ● <color=#526477>●</color>" : "● <color=#526477>● ●</color>";
            comboText.text = streak >= 5 ? streak + " seri" : "";
            powerText.text = (ShieldActive ? "Kalkan " + Mathf.CeilToInt(shieldUntil - clock) + " sn   " : "") +
                (MagnetActive ? "Mıknatıs " + Mathf.CeilToInt(magnetUntil - clock) + " sn   " : "") +
                (TurboActive ? "Turbo " + Mathf.CeilToInt(turboUntil - clock) + " sn" : "");
            int type = challenge % 3;
            missionText.text = type == 0 ? Mathf.Min(Mathf.FloorToInt(distance - challengeDistance), 300 + challenge * 20) + "/" + (300 + challenge * 20) + " m"
                : type == 1 ? (coinCount - challengeCoins) + " / 15 madalya"
                : (powersCollected - challengePowers) + " / 2 güçlendirme";
            if (turboButton != null) turboButton.interactable = boostCharged && state == RunState.Driving;
        }
        private void AnimateScene(float dt)
        {
            if (redBeacon != null) redBeacon.intensity = Mathf.Sin(clock * 14f) > 0 ? 3.5f : .1f;
            if (blueBeacon != null) blueBeacon.intensity = Mathf.Sin(clock * 14f) < 0 ? 3.5f : .1f;
            if (shieldVisual != null) shieldVisual.SetActive(ShieldActive && state == RunState.Driving);
            if (turboVisual != null) turboVisual.SetActive(TurboActive && state == RunState.Driving);
            if (hitFlash != null) hitFlash.alpha = Mathf.MoveTowards(hitFlash.alpha, 0, dt * 2f);
            if (state != RunState.Driving) return;
            foreach (TrackSection s in sections)
            {
                if (s.root.position.z > 190f || s.root.position.z < -sectionLength) continue;
                foreach (Transform coin in s.coins) if (coin.gameObject.activeSelf) coin.Rotate(0, dt * 100, 0, Space.World);
                foreach (PowerPickup p in s.powers) if (p.root.gameObject.activeSelf) p.root.Rotate(0, dt * 55, 0, Space.World);
            }
            if (engineAudio != null) engineAudio.pitch = Mathf.Lerp(.85f, 1.35f, CurrentSpeed / maximumSpeed);
        }
        private void LateUpdate()
        {
            if (state == RunState.Paused || truck == null || runnerCamera == null || Time.timeScale <= 0) return;
            float dt = Mathf.Min(Time.deltaTime, .1f);
            Vector3 target = cameraOrigin + new Vector3(truck.position.x * .2f, 0, 0);
            if (clock < shakeUntil) target += new Vector3(Mathf.Sin(clock * 66), Mathf.Cos(clock * 52), 0) * .045f;
            runnerCamera.transform.position = Vector3.Lerp(runnerCamera.transform.position, target, dt * 7);
            runnerCamera.fieldOfView = Mathf.Lerp(runnerCamera.fieldOfView, TurboActive ? 63f : 59f, dt * 3);
        }
    }
}
