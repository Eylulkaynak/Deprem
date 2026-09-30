using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Deprem.Minigames;

namespace Deprem.Story
{
    /// <summary>
    /// Story 04 video prototipinin tek runtime yoneticisi. Yol, engeller ve sehir
    /// Editor tarafinda sahneye basilidir; bu sinif yalnizca surus, sayac ve HUD'u yonetir.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FiretruckRunnerManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform truck;
        [SerializeField] private Camera runnerCamera;
        [SerializeField] private Transform[] obstacles = Array.Empty<Transform>();
        [SerializeField] private Transform[] coins = Array.Empty<Transform>();
        [SerializeField] private Light redBeacon;
        [SerializeField] private Light blueBeacon;
        [SerializeField] private AudioSource engineAudio;
        [SerializeField] private AudioSource hitAudio;
        [SerializeField] private AudioSource coinAudio;

        [Header("HUD")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text coinCountText;
        [SerializeField] private TMP_Text missionText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private Image timerFill;
        [SerializeField] private CanvasGroup hitFlash;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionStats;
        [SerializeField] private MinigameSessionManager resultReporter;

        [Header("Run Tuning")]
        [SerializeField, Min(5f)] private float runDuration = 30f;
        [SerializeField, Min(2f)] private float forwardSpeed = 14.5f;
        [SerializeField, Min(1f)] private float laneWidth = 3.1f;
        [SerializeField, Min(1f)] private float laneChangeSpeed = 6.4f;
        [SerializeField, Min(1f)] private float steeringResponse = 5.5f;
        [SerializeField, Min(0f)] private float countdownDuration = 3.25f;

        private bool[] obstacleHit = Array.Empty<bool>();
        private bool[] coinCollected = Array.Empty<bool>();
        private int collisionCount;
        private int coinCount;
        private float runStartedAt;
        private float countdownRemaining;
        private float slowUntil;
        private float cameraShakeUntil;
        private float steeringInput;
        private float smoothedSteering;
        private float scriptedSteering;
        private float scriptedSteerUntil;
        private float initialZ;
        private bool running;
        private bool finished;
        private bool pointerTracking;
        private Vector2 pointerStart;

        public float RemainingSeconds => running
            ? Mathf.Max(0f, runDuration - (Time.unscaledTime - runStartedAt))
            : runDuration;
        public int CollisionCount => collisionCount;
        public int CollectedCoinCount => coinCount;
        public int AvailableCoinCount => coins != null ? coins.Length : 0;

        private void Awake()
        {
            obstacleHit = new bool[obstacles != null ? obstacles.Length : 0];
            coinCollected = new bool[coins != null ? coins.Length : 0];
            countdownRemaining = countdownDuration;
            initialZ = truck != null ? truck.position.z : 0f;
            if (completionPanel != null)
                completionPanel.SetActive(false);
            if (hitFlash != null)
                hitFlash.alpha = 0f;
            UpdateHud(runDuration);
        }

        private void Start()
        {
            if (missionText != null)
                missionText.text = "ACİL ÇAĞRIYA ULAŞ • BASILI TUTUP SAĞA/SOLA YÖNLENDİR";
            if (engineAudio != null && !engineAudio.isPlaying)
                engineAudio.Play();
        }

        private void Update()
        {
            AnimateBeacons();
            AnimateCoins();
            FadeHitFlash();

            if (finished || truck == null)
                return;

            if (!running)
            {
                countdownRemaining -= Time.unscaledDeltaTime;
                if (countdownText != null)
                {
                    countdownText.gameObject.SetActive(true);
                    countdownText.text = countdownRemaining > 0.35f
                        ? Mathf.CeilToInt(countdownRemaining).ToString()
                        : "ÇIKIŞ!";
                }

                if (countdownRemaining <= 0f)
                    BeginRun();
                return;
            }

            ReadSteeringInput();
            MoveTruck();
            DetectObstacleHits();
            DetectCoinPickups();

            float elapsed = Time.unscaledTime - runStartedAt;
            UpdateHud(Mathf.Max(0f, runDuration - elapsed));
            if (elapsed >= runDuration)
                FinishRun();
        }

        private void LateUpdate()
        {
            if (runnerCamera == null || truck == null)
                return;

            Vector3 cameraTarget = truck.position + new Vector3(0f, 5.8f, -11.8f);
            if (Time.unscaledTime < cameraShakeUntil)
            {
                float strength = (cameraShakeUntil - Time.unscaledTime) * 0.24f;
                cameraTarget += new Vector3(
                    Mathf.Sin(Time.unscaledTime * 48f),
                    Mathf.Cos(Time.unscaledTime * 39f),
                    0f) * strength;
            }

            runnerCamera.transform.position = Vector3.Lerp(
                runnerCamera.transform.position,
                cameraTarget,
                1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            Vector3 lookTarget = truck.position + new Vector3(0f, 1.15f, 8.5f);
            runnerCamera.transform.rotation = Quaternion.Slerp(
                runnerCamera.transform.rotation,
                Quaternion.LookRotation(lookTarget - runnerCamera.transform.position, Vector3.up),
                1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        }

        public void Restart()
        {
            Scene scene = gameObject.scene;
            if (scene.IsValid())
                SceneManager.LoadScene(scene.name);
        }

        private void BeginRun()
        {
            running = true;
            runStartedAt = Time.unscaledTime;
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
            if (missionText != null)
                missionText.text = "30 SANİYE DAYAN • BASILI TUT VE YÖNLENDİR";
        }

        private void ReadSteeringInput()
        {
            float desired = Input.GetAxisRaw("Horizontal");

            if (Input.GetMouseButtonDown(0))
            {
                pointerTracking = true;
                pointerStart = Input.mousePosition;
            }

            if (pointerTracking && Input.GetMouseButton(0))
            {
                float dragRange = Mathf.Max(80f, Screen.width * 0.22f);
                desired = Mathf.Clamp(
                    (Input.mousePosition.x - pointerStart.x) / dragRange,
                    -1f,
                    1f);
            }

            if (pointerTracking && Input.GetMouseButtonUp(0))
                pointerTracking = false;

            if (!pointerTracking && Mathf.Abs(desired) < 0.01f &&
                Time.unscaledTime < scriptedSteerUntil)
                desired = scriptedSteering;

            steeringInput = desired;
            smoothedSteering = Mathf.MoveTowards(
                smoothedSteering,
                steeringInput,
                steeringResponse * Time.unscaledDeltaTime);
        }

        // Compatibility hook for authored captures/tests. This now feeds steering
        // briefly instead of teleporting the truck between three fixed lanes.
        private void ChangeLane(int direction)
        {
            scriptedSteering = Mathf.Clamp(direction, -1, 1);
            scriptedSteerUntil = Time.unscaledTime + 0.65f;
        }

        private void MoveTruck()
        {
            Vector3 position = truck.position;
            position.x = Mathf.Clamp(
                position.x + smoothedSteering * laneChangeSpeed * Time.unscaledDeltaTime,
                -laneWidth,
                laneWidth);

            float speedFactor = Time.unscaledTime < slowUntil ? 0.48f : 1f;
            position.z += forwardSpeed * speedFactor * Time.unscaledDeltaTime;
            truck.position = position;

            float lean = -smoothedSteering * 5f;
            float yaw = smoothedSteering * 10f;
            truck.rotation = Quaternion.Slerp(
                truck.rotation,
                Quaternion.Euler(0f, yaw, lean),
                1f - Mathf.Exp(-9f * Time.unscaledDeltaTime));
            if (engineAudio != null)
                engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, speedFactor < 1f ? 0.72f : 1.05f, 0.1f);
        }

        private void DetectObstacleHits()
        {
            if (obstacles == null)
                return;

            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacleHit[i] || obstacles[i] == null)
                    continue;

                Vector3 delta = obstacles[i].position - truck.position;
                if (delta.z < -3.2f)
                {
                    obstacleHit[i] = true;
                    continue;
                }

                if (Mathf.Abs(delta.z) <= 2.9f && Mathf.Abs(delta.x) <= 1.62f)
                    RegisterHit(i);
            }
        }

        private void RegisterHit(int obstacleIndex)
        {
            obstacleHit[obstacleIndex] = true;
            collisionCount++;
            slowUntil = Time.unscaledTime + 0.82f;
            cameraShakeUntil = Time.unscaledTime + 0.48f;
            if (hitFlash != null)
                hitFlash.alpha = 0.72f;
            if (hitAudio != null)
                hitAudio.Play();
            if (missionText != null)
                missionText.text = "DİKKAT! HIZI TOPARLA VE AÇIK ŞERİDE GEÇ";
        }

        private void DetectCoinPickups()
        {
            if (coins == null)
                return;

            for (int i = 0; i < coins.Length; i++)
            {
                Transform coin = coins[i];
                if (coinCollected[i] || coin == null)
                    continue;

                Vector3 delta = coin.position - truck.position;
                if (delta.z < -2.4f)
                {
                    coinCollected[i] = true;
                    continue;
                }

                if (Mathf.Abs(delta.z) > 2.55f || Mathf.Abs(delta.x) > 1.55f)
                    continue;

                coinCollected[i] = true;
                coinCount++;
                coin.gameObject.SetActive(false);
                if (coinAudio != null)
                    coinAudio.Play();
                if (missionText != null)
                    missionText.text = "İMO COIN +1  •  AÇIK ŞERİDİ TAKİP ET";
                UpdateCoinHud();
            }
        }

        private void UpdateHud(float remaining)
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(remaining).ToString("00") + " sn";
            if (distanceText != null && truck != null)
                distanceText.text = "ACİL ROTA  •  " + Mathf.Max(0f, truck.position.z - initialZ).ToString("000") + " m";
            if (timerFill != null)
                timerFill.fillAmount = runDuration <= 0f ? 0f : Mathf.Clamp01(remaining / runDuration);
            UpdateCoinHud();
        }

        private void UpdateCoinHud()
        {
            if (coinCountText != null)
                coinCountText.text = coinCount.ToString("00") + " / " +
                                     (coins != null ? coins.Length : 0).ToString("00");
        }

        private void FinishRun()
        {
            finished = true;
            running = false;
            UpdateHud(0f);
            if (missionText != null)
                missionText.text = "ACİL ROTA TAMAMLANDI";
            if (completionStats != null)
            {
                string performance = collisionCount == 0
                    ? "KUSURSUZ SÜRÜŞ"
                    : collisionCount <= 2 ? "GÜVENLİ VARIŞ" : "GÖREV TAMAMLANDI";
                completionStats.text = performance + "\n" +
                                       Mathf.Max(0f, truck.position.z - initialZ).ToString("0") +
                                       " m  •  " + collisionCount + " temas";
                completionStats.text += "  •  " + coinCount + " İMO";
            }
            if (completionPanel != null)
                completionPanel.SetActive(true);
            if (engineAudio != null)
                engineAudio.pitch = 0.55f;
            if (resultReporter != null)
                resultReporter.ReportExternalFiretruck(collisionCount, coinCount, coins != null ? coins.Length : 0);
        }

        private void AnimateBeacons()
        {
            float pulse = Mathf.PingPong(Time.unscaledTime * 5.5f, 1f);
            if (redBeacon != null)
                redBeacon.intensity = pulse > 0.52f ? 4.2f : 0.35f;
            if (blueBeacon != null)
                blueBeacon.intensity = pulse < 0.48f ? 4.2f : 0.35f;
        }

        private void AnimateCoins()
        {
            if (coins == null)
                return;

            float rotation = 110f * Time.unscaledDeltaTime;
            for (int i = 0; i < coins.Length; i++)
            {
                if (!coinCollected[i] && coins[i] != null)
                    coins[i].Rotate(0f, rotation, 0f, Space.World);
            }
        }

        private void FadeHitFlash()
        {
            if (hitFlash == null || hitFlash.alpha <= 0f)
                return;
            hitFlash.alpha = Mathf.MoveTowards(hitFlash.alpha, 0f, Time.unscaledDeltaTime * 1.7f);
            if (hitFlash.alpha <= 0.05f && running && missionText != null)
                missionText.text = "30 SANİYE DAYAN • BASILI TUT VE YÖNLENDİR";
        }
    }
}
