using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deprem.Minigames
{
    [Serializable]
    public sealed class FireTargetBinding
    {
        public Collider hitCollider;
        public Transform visualRoot;
        public Light glow;
    }

    /// <summary>
    /// İtfaiyeci söndürme sahnesinin tek runtime yöneticisi. Alev, çevre, su hattı
    /// ve HUD Editor tarafında sahneye basılır; bu sınıf yalnız dokunma hedefini,
    /// söndürme ilerlemesini ve sonucu yönetir.
    /// </summary>
    [DefaultExecutionOrder(-450)]
    [DisallowMultipleComponent]
    public sealed class FirefighterExtinguishManager : MonoBehaviour
    {
        private const string MinigameId = "firefighter-extinguish";

        [Header("Scene References")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform nozzleTip;
        [SerializeField] private LineRenderer waterStream;
        [SerializeField] private ParticleSystem waterImpact;
        [SerializeField] private FireTargetBinding[] fires = Array.Empty<FireTargetBinding>();

        [Header("Firefighter Aim Rig")]
        [SerializeField] private Transform firefighterRoot;
        [SerializeField] private Transform spineBone;
        [SerializeField] private Transform chestBone;
        [SerializeField] private Transform rightUpperArmBone;
        [SerializeField] private Transform rightLowerArmBone;
        [SerializeField] private Transform rightHandBone;
        [SerializeField] private Transform leftUpperArmBone;
        [SerializeField] private Transform leftLowerArmBone;
        [SerializeField] private Transform leftHandBone;
        [SerializeField] private Transform nozzleRig;
        [SerializeField] private Transform rightNozzleGrip;
        [SerializeField] private Transform leftNozzleGrip;
        [SerializeField] private LineRenderer supplyHose;

        [Header("HUD")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text remainingText;
        [SerializeField] private TMP_Text missionText;
        [SerializeField] private Image timerFill;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultDetailText;

        [Header("Audio and Progress")]
        [SerializeField] private AudioSource sprayAudio;
        [SerializeField] private AudioSource extinguishAudio;
        [SerializeField] private MinigameProgressManager progressManager;

        [Header("Tuning")]
        [SerializeField, Min(5f)] private float gameDuration = 32f;
        [SerializeField, Min(0.15f)] private float extinguishSeconds = 0.85f;
        [SerializeField, Min(0.25f)] private float sprayRadius = 1.05f;
        [SerializeField] private float aimPlaneHeight = 0.65f;
        [SerializeField, Min(1f)] private float bodyTurnSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float upperBodyAimWeight = 0.72f;

        private float[] fireHealth = Array.Empty<float>();
        private Vector3[] initialVisualScales = Array.Empty<Vector3>();
        private float elapsedSeconds;
        private int extinguishedCount;
        private bool finished;
        private bool successful;
        private bool spraying;
        private Vector3 currentAimPoint;

        public int FireCount => fires?.Length ?? 0;
        public int ExtinguishedCount => extinguishedCount;
        public int RemainingFireCount => Mathf.Max(0, FireCount - extinguishedCount);
        public float RemainingSeconds => Mathf.Max(0f, gameDuration - elapsedSeconds);
        public bool IsFinished => finished;
        public bool IsSuccessful => successful;
        public Vector3 CurrentAimPoint => currentAimPoint;

        private void Awake()
        {
            int count = FireCount;
            fireHealth = new float[count];
            initialVisualScales = new Vector3[count];
            for (int index = 0; index < count; index++)
            {
                fireHealth[index] = 1f;
                Transform visual = fires[index]?.visualRoot;
                initialVisualScales[index] = visual != null ? visual.localScale : Vector3.one;
            }

            if (waterStream != null)
                waterStream.enabled = false;
            if (waterImpact != null)
                waterImpact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (completionPanel != null)
                completionPanel.SetActive(false);
            if (FireCount > 0 && fires[0]?.hitCollider != null)
                currentAimPoint = fires[0].hitCollider.bounds.center + Vector3.up * 0.2f;
            UpdateHud();
        }

        private void Start()
        {
            if (!ValidateConfiguration(out string error))
            {
                enabled = false;
                Debug.LogError("FirefighterExtinguishManager yapılandırma hatası: " + error, this);
                return;
            }

            progressManager.LoadNow();
            if (missionText != null)
                missionText.text = "BASILI TUT • SUYU ALEVE YÖNLENDİR";
        }

        private void Update()
        {
            if (finished || Time.timeScale <= 0f)
            {
                SetSpraying(false);
                return;
            }

            // Editor/ci Play Mode girişinde ilk kare onlarca saniye raporlanabilir.
            // Tek bir takılan kare bütün turu bitirmesin ve yangını aniden söndürmesin.
            float frameDelta = Mathf.Clamp(Time.deltaTime, 0f, 0.1f);
            elapsedSeconds += frameDelta;
            if (RemainingSeconds <= 0f)
            {
                Finish(false);
                return;
            }

            if (TryGetPointer(out Vector2 screenPosition, out int pointerId) && !IsPointerOverUi(pointerId))
            {
                Ray ray = worldCamera.ScreenPointToRay(screenPosition);
                int directTarget = ResolveAimPoint(ray, out Vector3 aimPoint);
                currentAimPoint = aimPoint;
                SetSpraying(true);
                RenderWater(aimPoint);
                ApplyWater(aimPoint, directTarget, frameDelta);
            }
            else
            {
                SetSpraying(false);
            }

            UpdateHud();
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f) return;
            UpdateFirefighterAimRig(Mathf.Clamp(Time.deltaTime, 0f, 0.1f));
        }

        public void PoseForEditorPreview(Vector3 aimPoint)
        {
            currentAimPoint = aimPoint;
            for (int index = 0; index < 8; index++)
                UpdateFirefighterAimRig(0.1f);
        }

        public bool ValidateConfiguration(out string error)
        {
            if (worldCamera == null || nozzleTip == null || waterStream == null || waterImpact == null)
            {
                error = "Kamera, nozzle veya su efekti referansı eksik.";
                return false;
            }
            if (firefighterRoot == null || chestBone == null || nozzleRig == null ||
                rightNozzleGrip == null || leftNozzleGrip == null || supplyHose == null ||
                rightUpperArmBone == null || rightLowerArmBone == null || rightHandBone == null ||
                leftUpperArmBone == null || leftLowerArmBone == null || leftHandBone == null)
            {
                error = "İtfaiyeci üst gövde, kol veya hortum rig referansı eksik.";
                return false;
            }
            if (progressManager == null)
            {
                error = "Progress manager referansı eksik.";
                return false;
            }
            if (fires == null || fires.Length == 0)
            {
                error = "En az bir yangın hedefi gerekli.";
                return false;
            }
            for (int index = 0; index < fires.Length; index++)
            {
                if (fires[index] == null || fires[index].hitCollider == null || fires[index].visualRoot == null)
                {
                    error = $"{index + 1}. yangın hedefinin Collider veya görsel referansı eksik.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public void Restart()
        {
            Scene scene = gameObject.scene;
            if (scene.IsValid())
                SceneManager.LoadScene(scene.name);
        }

        public void ReturnToHub()
        {
            SceneManager.LoadScene("Minigame_Hub");
        }

        private bool TryGetPointer(out Vector2 position, out int pointerId)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                {
                    position = touch.position;
                    pointerId = touch.fingerId;
                    return true;
                }
            }

            if (Input.GetMouseButton(0))
            {
                position = Input.mousePosition;
                pointerId = -1;
                return true;
            }

            position = default;
            pointerId = -1;
            return false;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
                return false;
            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private int ResolveAimPoint(Ray ray, out Vector3 aimPoint)
        {
            float nearestDistance = float.PositiveInfinity;
            int directTarget = -1;
            aimPoint = ray.GetPoint(18f);
            for (int index = 0; index < fires.Length; index++)
            {
                Collider target = fires[index]?.hitCollider;
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
                    continue;
                if (!target.Raycast(ray, out RaycastHit hit, 100f) || hit.distance >= nearestDistance)
                    continue;
                nearestDistance = hit.distance;
                directTarget = index;
                aimPoint = hit.point;
            }

            if (directTarget >= 0)
                return directTarget;

            Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, aimPlaneHeight, 0f));
            if (aimPlane.Raycast(ray, out float planeDistance))
                aimPoint = ray.GetPoint(planeDistance);
            return -1;
        }

        private void ApplyWater(Vector3 aimPoint, int directTarget, float deltaSeconds)
        {
            int targetIndex = directTarget;
            if (targetIndex < 0)
            {
                float nearest = sprayRadius;
                for (int index = 0; index < fires.Length; index++)
                {
                    Collider target = fires[index]?.hitCollider;
                    if (target == null || !target.enabled)
                        continue;
                    float distance = Vector3.Distance(aimPoint, target.bounds.center);
                    if (distance >= nearest)
                        continue;
                    nearest = distance;
                    targetIndex = index;
                }
            }

            if (targetIndex < 0 || fireHealth[targetIndex] <= 0f)
                return;

            fireHealth[targetIndex] = Mathf.Max(
                0f,
                fireHealth[targetIndex] - deltaSeconds / Mathf.Max(0.15f, extinguishSeconds));
            FireTargetBinding binding = fires[targetIndex];
            float scale = Mathf.Lerp(0.34f, 1f, fireHealth[targetIndex]);
            binding.visualRoot.localScale = initialVisualScales[targetIndex] * scale;
            if (binding.glow != null)
                binding.glow.intensity = Mathf.Lerp(0f, 2.8f, fireHealth[targetIndex]);

            if (fireHealth[targetIndex] > 0f)
                return;

            binding.hitCollider.enabled = false;
            binding.visualRoot.gameObject.SetActive(false);
            if (binding.glow != null)
                binding.glow.enabled = false;
            extinguishedCount++;
            if (extinguishAudio != null)
                extinguishAudio.Play();
            if (missionText != null)
                missionText.text = RemainingFireCount > 0
                    ? "SÜPER! SIRADAKİ ALEVE SU TUT"
                    : "BÖLGE GÜVENDE!";
            if (RemainingFireCount == 0)
                Finish(true);
        }

        private void RenderWater(Vector3 aimPoint)
        {
            const int segmentCount = 14;
            waterStream.positionCount = segmentCount;
            Vector3 start = nozzleTip.position;
            Vector3 delta = aimPoint - start;
            for (int index = 0; index < segmentCount; index++)
            {
                float t = index / (segmentCount - 1f);
                Vector3 position = start + delta * t;
                position += Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.18f);
                float ripple = Mathf.Sin(Time.time * 44f + index * 1.7f) * 0.018f * t;
                position += worldCamera.transform.right * ripple;
                waterStream.SetPosition(index, position);
            }

            waterImpact.transform.position = aimPoint;
        }

        private void UpdateFirefighterAimRig(float deltaTime)
        {
            if (firefighterRoot == null || nozzleRig == null || chestBone == null)
                return;

            float turnT = 1f - Mathf.Exp(-bodyTurnSpeed * deltaTime);
            Vector3 target = currentAimPoint;
            Vector3 flatDirection = target - firefighterRoot.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                firefighterRoot.rotation = Quaternion.Slerp(firefighterRoot.rotation, targetRotation, turnT * 0.72f);
            }

            AimUpperBody(spineBone, target, upperBodyAimWeight * 0.38f);
            AimUpperBody(chestBone, target, upperBodyAimWeight);

            Vector3 rigOrigin = chestBone.position +
                                firefighterRoot.forward * 0.43f -
                                firefighterRoot.up * 0.18f +
                                firefighterRoot.right * 0.035f;
            Vector3 aimDirection = target - rigOrigin;
            if (aimDirection.sqrMagnitude < 0.01f)
                aimDirection = firefighterRoot.forward;
            aimDirection.Normalize();

            nozzleRig.position = Vector3.Lerp(nozzleRig.position, rigOrigin, turnT * 1.4f);
            Quaternion nozzleRotation = Quaternion.LookRotation(aimDirection, firefighterRoot.up);
            nozzleRig.rotation = Quaternion.Slerp(nozzleRig.rotation, nozzleRotation, turnT * 1.65f);

            SolveArm(
                rightUpperArmBone,
                rightLowerArmBone,
                rightHandBone,
                rightNozzleGrip.position,
                -firefighterRoot.up + firefighterRoot.right * 0.55f);
            SolveArm(
                leftUpperArmBone,
                leftLowerArmBone,
                leftHandBone,
                leftNozzleGrip.position,
                -firefighterRoot.up - firefighterRoot.right * 0.55f);
            UpdateSupplyHose();
        }

        private void AimUpperBody(Transform bone, Vector3 target, float weight)
        {
            if (bone == null || weight <= 0f)
                return;
            Vector3 desired = target - bone.position;
            if (desired.sqrMagnitude < 0.01f)
                return;

            Quaternion delta = Quaternion.FromToRotation(firefighterRoot.forward, desired.normalized);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
                angle -= 360f;
            angle = Mathf.Clamp(angle, -52f, 52f);
            Quaternion limited = Quaternion.AngleAxis(angle, axis);
            bone.rotation = Quaternion.Slerp(bone.rotation, limited * bone.rotation, weight);
        }

        private static void SolveArm(
            Transform upperArm,
            Transform lowerArm,
            Transform hand,
            Vector3 handTarget,
            Vector3 bendHint)
        {
            if (upperArm == null || lowerArm == null || hand == null)
                return;

            Vector3 shoulder = upperArm.position;
            float upperLength = Vector3.Distance(shoulder, lowerArm.position);
            float lowerLength = Vector3.Distance(lowerArm.position, hand.position);
            if (upperLength < 0.001f || lowerLength < 0.001f)
                return;

            Vector3 toTarget = handTarget - shoulder;
            float distance = Mathf.Clamp(
                toTarget.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.001f,
                upperLength + lowerLength - 0.001f);
            Vector3 direction = toTarget.sqrMagnitude > 0.0001f
                ? toTarget.normalized
                : upperArm.forward;
            Vector3 bendDirection = Vector3.ProjectOnPlane(bendHint, direction).normalized;
            if (bendDirection.sqrMagnitude < 0.001f)
                bendDirection = Vector3.Cross(direction, Vector3.right).normalized;

            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) /
                          (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 elbowTarget = shoulder + direction * along + bendDirection * height;

            RotateBoneTowardChild(upperArm, lowerArm, elbowTarget);
            RotateBoneTowardChild(lowerArm, hand, handTarget);
        }

        private static void RotateBoneTowardChild(Transform bone, Transform child, Vector3 childTarget)
        {
            Vector3 current = child.position - bone.position;
            Vector3 desired = childTarget - bone.position;
            if (current.sqrMagnitude < 0.0001f || desired.sqrMagnitude < 0.0001f)
                return;
            bone.rotation = Quaternion.FromToRotation(current, desired) * bone.rotation;
        }

        private void UpdateSupplyHose()
        {
            if (supplyHose == null || firefighterRoot == null || nozzleRig == null)
                return;
            supplyHose.positionCount = 6;
            Vector3 rear = nozzleRig.TransformPoint(new Vector3(0f, 0f, -0.34f));
            Vector3 hip = firefighterRoot.position + firefighterRoot.up * 0.72f - firefighterRoot.right * 0.18f;
            Vector3 knee = firefighterRoot.position + firefighterRoot.up * 0.25f - firefighterRoot.right * 0.36f;
            Vector3 ground = firefighterRoot.position - firefighterRoot.right * 0.7f - firefighterRoot.forward * 0.25f;
            supplyHose.SetPositions(new[]
            {
                rear,
                Vector3.Lerp(rear, hip, 0.46f) - firefighterRoot.right * 0.12f,
                hip,
                knee,
                ground + Vector3.up * 0.04f,
                ground - firefighterRoot.right * 2.2f - firefighterRoot.forward * 0.45f + Vector3.up * 0.04f
            });
        }

        private void SetSpraying(bool value)
        {
            if (spraying == value)
                return;
            spraying = value;
            waterStream.enabled = value;
            if (value)
            {
                waterImpact.Play(true);
                if (sprayAudio != null && !sprayAudio.isPlaying)
                    sprayAudio.Play();
            }
            else
            {
                waterImpact.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (sprayAudio != null && sprayAudio.isPlaying)
                    sprayAudio.Stop();
            }
        }

        private void UpdateHud()
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(RemainingSeconds).ToString("00") + " sn";
            if (remainingText != null)
                remainingText.text = RemainingFireCount.ToString("00") + " YANGIN";
            if (timerFill != null)
                timerFill.fillAmount = gameDuration <= 0f ? 0f : RemainingSeconds / gameDuration;
        }

        private void Finish(bool success)
        {
            if (finished)
                return;
            finished = true;
            successful = success;
            SetSpraying(false);
            UpdateHud();

            if (completionPanel != null)
                completionPanel.SetActive(true);
            if (resultTitleText != null)
                resultTitleText.text = success ? "TÜM YANGINLAR SÖNDÜ!" : "SÜRE DOLDU";
            if (resultDetailText != null)
                resultDetailText.text = success
                    ? $"{FireCount}/{FireCount} yangın • {elapsedSeconds:0.0} saniye"
                    : $"{extinguishedCount}/{FireCount} yangın söndürüldü\nKalan alevlere daha hızlı su tut.";
            if (missionText != null)
                missionText.text = success ? "GÖREV TAMAMLANDI" : "TEKRAR DENE • ALEVE BASILI TUT";

            if (!success)
                return;
            int score = Mathf.Clamp(
                Mathf.RoundToInt(1000f - elapsedSeconds * 12f),
                600,
                1000);
            int stars = elapsedSeconds <= 18f ? 3 : elapsedSeconds <= 26f ? 2 : 1;
            int coins = MinigameProgressManager.CoinsForStars(stars);
            progressManager.RecordResult(MinigameId, stars, score, coins, elapsedSeconds);
        }
    }
}
