using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Deprem.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryTouchManager : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private StoryPlayerMovement player;
        [SerializeField] private StoryUIController ui;
        [SerializeField] private StoryCameraController cameraController;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField, Min(10f)] private float maxRayDistance = 150f;
        [SerializeField] private bool directWorldGestures;
        [SerializeField, Min(40f)] private float worldSwipeThreshold = 95f;

        private const int HitBufferCapacity = 48;
        private const float WorldHoldDriftLimit = 85f;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferCapacity];

        private bool worldNavigationEnabled = true;
        private bool interactionsEnabled = true;
        private StoryInteractable pendingInteraction;
        private Action pendingReadyCallback;
        private Action pendingCompleteCallback;
        private int cameraRequestVersion;

        private bool worldHoldActive;
        private float worldHoldStartedAt;
        private float worldHoldDuration;
        private Vector2 worldHoldStartPosition;
        private int worldHoldProgressStep = -1;

        private bool pendingPrepared;
        private bool directGestureActive;
        private int completedDirectGestureCount;
        private Vector2 directGestureStartPosition;
        private DraggableItem managedDrag;
        private bool cameraTransitionPointerGuard;

        private void Awake()
        {
            worldCamera ??= Camera.main;
            player ??= FindFirstObjectByType<StoryPlayerMovement>(FindObjectsInactive.Include);
            ui ??= FindFirstObjectByType<StoryUIController>(FindObjectsInactive.Include);
            cameraController ??= FindFirstObjectByType<StoryCameraController>(FindObjectsInactive.Include);
            pendingReadyCallback = PresentPendingInteraction;
            pendingCompleteCallback = CompletePendingInteraction;
        }

        private void OnEnable()
        {
            if (ui != null)
            {
                ui.WorldInputBlockChanged += OnWorldInputBlockChanged;
                ApplyStoryInputLock(ui.WorldInputBlocked);
            }
        }

        private void OnDisable()
        {
            if (ui != null)
                ui.WorldInputBlockChanged -= OnWorldInputBlockChanged;
        }

        private void Update()
        {
            if (ui != null && ui.WorldInputBlocked)
            {
                // Diyalog açılmadan önce verilmiş rota da aynı anda iptal edilir. Böylece yeni bir
                // dokunuş engellense bile karakter eski hedefe doğru yürümeyi sürdüremez.
                ApplyStoryInputLock(true);
                if (worldHoldActive || directGestureActive || managedDrag != null || pendingInteraction != null)
                    ClearPendingInteraction();

                if (TryGetPointerDown(out _, out int blockedPointerId) &&
                    (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(blockedPointerId)))
                    ui.TryHandlePrimaryTap();
                return;
            }

            if (cameraController != null && cameraController.WorldNavigationBlocked)
            {
                // Kamera kadraj değiştirirken aynı dokunuşun zemine düşüp yeni rota üretmesine izin verme.
                // Scripted MoveTo akışları worldNavigationEnabled=false kullanır; onları durdurmadan yalnız
                // oyuncunun serbest dolaşım rotasını kes.
                if (worldNavigationEnabled)
                    player?.Stop();

                // Kadraj değişimini başlatan dokunuş zaten doğrudan nesnenin üzerindeyse aktif fiziksel
                // hareketi kaybetme. Özellikle sürükleme sırasında parmak blend bitmeden bırakıldığında
                // PointerUp karesi kaçarsa nesne sonsuza kadar "sürükleniyor" durumunda kalıyordu.
                if (worldHoldActive)
                {
                    UpdateWorldHold();
                    return;
                }
                if (directWorldGestures && UpdateDirectWorldGesture())
                    return;

                cameraTransitionPointerGuard = true;
                return;
            }

            if (cameraTransitionPointerGuard)
            {
                if (TryGetPointerHeld(out _))
                    return;
                cameraTransitionPointerGuard = false;
            }

            ApplyStoryInputLock(false);

            if (worldHoldActive)
            {
                UpdateWorldHold();
                return;
            }

            if (directWorldGestures && UpdateDirectWorldGesture())
                return;

            if (!TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
                return;

            HandleWorldTap(screenPosition);
        }

        public void SetWorldNavigationEnabled(bool enabled)
        {
            worldNavigationEnabled = enabled;
            if (!enabled)
                player?.Stop();
        }

        public void SetInteractionsEnabled(bool enabled)
        {
            interactionsEnabled = enabled;
            if (!enabled)
                ClearPendingInteraction();
        }

        private void HandleWorldTap(Vector2 screenPosition)
        {
            if (worldCamera == null || player == null)
                return;

            if (cameraController != null && cameraController.WorldNavigationBlocked)
            {
                cameraTransitionPointerGuard = true;
                if (worldNavigationEnabled)
                    player.Stop();
                return;
            }

            if (ui != null && ui.WorldInputBlocked)
            {
                ApplyStoryInputLock(true);
                ClearPendingInteraction();
                return;
            }

            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, maxRayDistance, raycastMask, QueryTriggerInteraction.Collide);
            if (hitCount == 0)
                return;

            SortHitsByDistance(hitCount);
            if (interactionsEnabled)
            {
                StoryInteractable selected = FindBestInteractable(screenPosition, hitCount);
                if (directWorldGestures && pendingPrepared && selected == pendingInteraction)
                {
                    BeginDirectWorldGesture(screenPosition);
                    return;
                }

                if (selected != null)
                {
                    ClearPendingInteraction();
                    pendingInteraction = selected;
                    pendingPrepared = false;
                    completedDirectGestureCount = 0;
                    ui?.ShowContext("Yaklaşılıyor: " + selected.Prompt);
                    bool accepted = selected.PrepareInteraction(player, pendingReadyCallback);
                    if (!accepted)
                    {
                        pendingInteraction = null;
                        // Engelli bir dünya dokunuşu hata metnine dönüşmez. Hareket çözücüsü hedefi
                        // ulaşılabilir zemine yaklaştırır; etkileşim yine de kurulamazsa sessizce iptal edilir.
                        ui?.HideContext();
                    }
                    else if (directWorldGestures && pendingPrepared && pendingInteraction == selected &&
                             !directGestureActive && !worldHoldActive && managedDrag == null)
                    {
                        // Oyuncu zaten menzildeyse ilk dokunuş/sürükleme gerçek etkileşimin başlangıcıdır.
                        // Önce nesneyi seçip sonra aynı hareketi ikinci kez istemek dokunmayı boşa çıkarıyordu.
                        BeginDirectWorldGesture(screenPosition);
                    }
                    return;
                }
            }

            if (!worldNavigationEnabled)
            {
                ui?.ShowContext("Sarsıntı sürerken bulunduğun güvenli alanda kal.");
                return;
            }

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                if (hit.collider.isTrigger)
                    continue;

                // Mobilya ve duvara dokunulduğunda en yakın erişilebilir zemin çözülür.
                player.TrySetDestination(hit.point);
                return;
            }
        }

        private void PresentPendingInteraction()
        {
            StoryInteractable interactable = pendingInteraction;
            if (!interactionsEnabled || interactable == null || !interactable.IsAvailable)
                return;

            if (interactable.FocusCameraZone != StoryCameraZoneId.None)
                cameraController?.ActivateZone(interactable.FocusCameraZone);

            if (directWorldGestures)
            {
                PresentDirectWorldInteraction(interactable);
                return;
            }

            if (interactable.InteractionGesture == StoryInteractionGesture.Approach)
            {
                ui?.ShowContext(interactable.Prompt);
                CompletePendingInteraction();
                return;
            }

            if (interactable.InteractionGesture == StoryInteractionGesture.WorldHold)
            {
                BeginWorldHold(interactable);
                return;
            }

            ui?.PresentAction(interactable.Prompt, interactable.InteractionGesture, interactable.RequiredGestureCount, pendingCompleteCallback);
        }

        private void PresentDirectWorldInteraction(StoryInteractable interactable)
        {
            pendingPrepared = true;
            switch (interactable.InteractionGesture)
            {
                case StoryInteractionGesture.Tap:
                case StoryInteractionGesture.Approach:
                    CompletePendingInteraction();
                    return;
                case StoryInteractionGesture.RepeatedTap:
                    completedDirectGestureCount = 0;
                    ui?.ShowContext($"{interactable.Prompt} — NESNEYE DOKUN 0/{Mathf.Max(2, interactable.RequiredGestureCount)}");
                    return;
                case StoryInteractionGesture.SwipeDown:
                    ui?.ShowContext(interactable.Prompt + " — NESNENİN ÜZERİNDE AŞAĞI ÇEK");
                    return;
                case StoryInteractionGesture.SwipeHorizontal:
                    ui?.ShowContext(interactable.Prompt + " — NESNENİN ÜZERİNDE TUTUP ÇEK");
                    return;
                case StoryInteractionGesture.WorldHold:
                    ui?.ShowContext(interactable.Prompt + " — NESNENİN ÜZERİNDE BASILI TUT");
                    return;
                case StoryInteractionGesture.DragToBag:
                    ui?.ShowContext(interactable.Prompt + " — NESNEYİ ÇANTA AĞZINA SÜRÜKLE");
                    return;
            }
        }

        private void BeginDirectWorldGesture(Vector2 screenPosition)
        {
            StoryInteractable interactable = pendingInteraction;
            if (interactable == null || !pendingPrepared)
                return;

            switch (interactable.InteractionGesture)
            {
                case StoryInteractionGesture.RepeatedTap:
                    completedDirectGestureCount++;
                    if (completedDirectGestureCount >= Mathf.Max(2, interactable.RequiredGestureCount))
                        CompletePendingInteraction();
                    else
                        ui?.ShowContext($"{interactable.Prompt} — NESNEYE DOKUN {completedDirectGestureCount}/{Mathf.Max(2, interactable.RequiredGestureCount)}");
                    break;
                case StoryInteractionGesture.SwipeDown:
                case StoryInteractionGesture.SwipeHorizontal:
                    directGestureStartPosition = screenPosition;
                    directGestureActive = true;
                    break;
                case StoryInteractionGesture.WorldHold:
                    worldHoldStartPosition = screenPosition;
                    worldHoldDuration = Mathf.Max(0.65f, interactable.InteractionSeconds);
                    worldHoldStartedAt = Time.unscaledTime;
                    worldHoldProgressStep = -1;
                    worldHoldActive = true;
                    break;
                case StoryInteractionGesture.DragToBag:
                    managedDrag = interactable.GetComponent<DraggableItem>();
                    if (managedDrag == null || !managedDrag.BeginManagedDrag(screenPosition))
                    {
                        managedDrag = null;
                        ui?.ShowContext("Bu nesne şu anda sürüklenemiyor.");
                    }
                    break;
                default:
                    CompletePendingInteraction();
                    break;
            }
        }

        private bool UpdateDirectWorldGesture()
        {
            if (managedDrag != null)
            {
                if (TryGetPointerHeld(out Vector2 dragPosition))
                    managedDrag.UpdateManagedDrag(dragPosition);

                if (TryGetPointerUp(out Vector2 releasePosition))
                {
                    DraggableItem released = managedDrag;
                    managedDrag = null;
                    if (released.EndManagedDrag(releasePosition))
                        CompletePendingInteraction();
                    else
                        ui?.ShowContext("Nesneyi çantanın açık ağzına bırak.");
                }
                return true;
            }

            if (!directGestureActive)
                return false;

            if (TryGetPointerUp(out Vector2 pointerUp))
            {
                directGestureActive = false;
                Vector2 delta = pointerUp - directGestureStartPosition;
                StoryInteractionGesture gesture = pendingInteraction != null
                    ? pendingInteraction.InteractionGesture
                    : StoryInteractionGesture.Tap;
                bool valid = gesture == StoryInteractionGesture.SwipeDown
                    ? delta.y <= -worldSwipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.2f
                    : Mathf.Abs(delta.x) >= worldSwipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.2f;
                if (valid && gesture == StoryInteractionGesture.SwipeHorizontal)
                    valid = IsHorizontalGestureHeadingToSceneTarget(delta);
                if (valid)
                    CompletePendingInteraction();
                else
                    ui?.ShowContext(gesture == StoryInteractionGesture.SwipeDown
                        ? "Nesnenin üzerinden aşağı doğru tutup çek."
                        : "Nesnenin üzerinden yana doğru tutup çek.");
            }
            return true;
        }

        private StoryInteractable FindBestInteractable(Vector2 screenPosition, int hitCount)
        {
            StoryInteractable best = null;
            float bestScreenDistance = float.PositiveInfinity;
            float bestRayDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                StoryInteractable interactable = hitBuffer[i].collider.GetComponentInParent<StoryInteractable>();
                if (interactable == null)
                {
                    // İlk opak fizik yüzeyi duvar/mobilyaysa arkasındaki görünmez hotspot seçilemez.
                    // Trigger'lar yalnızca etkileşim hacmi olduğundan görüşü kapatmaz.
                    if (!hitBuffer[i].collider.isTrigger)
                        break;
                    continue;
                }
                if (!interactable.IsAvailable || !interactable.WorldSelectable)
                    continue;

                Renderer visibleRenderer = interactable.GetComponentInChildren<Renderer>();
                Vector3 visualCenter = visibleRenderer != null
                    ? visibleRenderer.bounds.center
                    : hitBuffer[i].collider.bounds.center;
                Vector3 projectedCenter = worldCamera.WorldToScreenPoint(visualCenter);
                if (projectedCenter.z <= 0f)
                    continue;

                float screenDistance = ((Vector2)projectedCenter - screenPosition).sqrMagnitude;
                if (screenDistance < bestScreenDistance - 0.01f ||
                    (Mathf.Abs(screenDistance - bestScreenDistance) <= 0.01f && hitBuffer[i].distance < bestRayDistance))
                {
                    best = interactable;
                    bestScreenDistance = screenDistance;
                    bestRayDistance = hitBuffer[i].distance;
                }
            }

            return best;
        }

        private bool IsHorizontalGestureHeadingToSceneTarget(Vector2 gestureDelta)
        {
            StoryInteractable interactable = pendingInteraction;
            Transform target = interactable != null ? interactable.GestureTarget : null;
            if (interactable == null || target == null || worldCamera == null)
                return true;

            Renderer visibleRenderer = interactable.GetComponentInChildren<Renderer>();
            Vector3 sourceWorld = visibleRenderer != null ? visibleRenderer.bounds.center : interactable.transform.position;
            Vector3 sourceScreen = worldCamera.WorldToScreenPoint(sourceWorld);
            Vector3 targetScreen = worldCamera.WorldToScreenPoint(target.position);
            if (sourceScreen.z <= 0f || targetScreen.z <= 0f)
                return true;

            float expectedX = targetScreen.x - sourceScreen.x;
            // Kamera açısından hedef neredeyse dikey hizadaysa yatay yönü zorlamayız.
            if (Mathf.Abs(expectedX) < 24f)
                return true;
            return Mathf.Sign(gestureDelta.x) == Mathf.Sign(expectedX);
        }

        private void BeginWorldHold(StoryInteractable interactable)
        {
            ui?.HideAction();
            if (!TryGetPointerHeld(out worldHoldStartPosition))
            {
                ui?.ShowContext("Hedefin üzerinde basılı tut.");
                return;
            }

            worldHoldDuration = Mathf.Max(0.65f, interactable.InteractionSeconds);
            worldHoldStartedAt = Time.unscaledTime;
            worldHoldProgressStep = -1;
            worldHoldActive = true;
            ui?.ShowContext(interactable.Prompt + " — BASILI TUT");
        }

        private void UpdateWorldHold()
        {
            if (!TryGetPointerHeld(out Vector2 currentPosition))
            {
                CancelWorldHold("Tutmayı bıraktın; hedefin üzerinde yeniden basılı tut.");
                return;
            }

            if ((currentPosition - worldHoldStartPosition).sqrMagnitude > WorldHoldDriftLimit * WorldHoldDriftLimit)
            {
                CancelWorldHold("Parmağını hedefin üzerinde tut.");
                return;
            }

            float progress = Mathf.Clamp01((Time.unscaledTime - worldHoldStartedAt) / worldHoldDuration);
            int step = Mathf.FloorToInt(progress * 10f);
            if (step != worldHoldProgressStep)
            {
                worldHoldProgressStep = step;
                ui?.ShowContext($"{pendingInteraction?.Prompt} — BASILI TUT %{step * 10}");
            }

            if (progress >= 1f)
            {
                worldHoldActive = false;
                CompletePendingInteraction();
            }
        }

        private void CancelWorldHold(string message)
        {
            worldHoldActive = false;
            if (directWorldGestures)
            {
                ui?.ShowContext(message);
                return;
            }
            ClearPendingInteraction();
            ui?.ShowContext(message);
        }

        private void CompletePendingInteraction()
        {
            StoryInteractable interactable = pendingInteraction;
            pendingInteraction = null;
            pendingPrepared = false;
            directGestureActive = false;
            completedDirectGestureCount = 0;
            cameraRequestVersion++;
            ui?.HideAction();
            ui?.HideContext();
            interactable?.CompletePreparedInteraction();
            if (interactable != null && interactable.ReturnCameraAfterCompletion)
                StartCoroutine(RestoreCameraAfterDelay(interactable.ReturnCameraZone, interactable.FocusLingerSeconds, cameraRequestVersion));
        }

        private void ClearPendingInteraction()
        {
            StoryInteractable previous = pendingInteraction;
            managedDrag?.CancelManagedDrag();
            managedDrag = null;
            pendingInteraction = null;
            pendingPrepared = false;
            directGestureActive = false;
            completedDirectGestureCount = 0;
            worldHoldActive = false;
            cameraRequestVersion++;
            ui?.HideAction();
            ui?.HideContext();
            if (previous != null && previous.ReturnCameraAfterCompletion)
                cameraController?.ActivateZone(previous.ReturnCameraZone);
        }

        private void OnWorldInputBlockChanged(bool blocked)
        {
            ApplyStoryInputLock(blocked);

            if (blocked)
                ClearPendingInteraction();
        }

        private void ApplyStoryInputLock(bool blocked)
        {
            if (player == null || player.StoryInputLocked == blocked)
                return;

            player.SetStoryInputLocked(blocked);
        }

        private System.Collections.IEnumerator RestoreCameraAfterDelay(StoryCameraZoneId zone, float delay, int requestVersion)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            if (cameraRequestVersion == requestVersion)
                cameraController?.ActivateZone(zone);
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return touch.phase == TouchPhase.Began;
            }

            screenPosition = Input.mousePosition;
            pointerId = -1;
            return Input.GetMouseButtonDown(0);
        }

        private static bool TryGetPointerHeld(out Vector2 screenPosition)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                screenPosition = touch.position;
                return touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            }

            screenPosition = Input.mousePosition;
            return Input.GetMouseButton(0);
        }

        private static bool TryGetPointerUp(out Vector2 screenPosition)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                screenPosition = touch.position;
                return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }

            screenPosition = Input.mousePosition;
            return Input.GetMouseButtonUp(0);
        }

        private void SortHitsByDistance(int count)
        {
            for (int i = 1; i < count; i++)
            {
                RaycastHit value = hitBuffer[i];
                int index = i - 1;
                while (index >= 0 && hitBuffer[index].distance > value.distance)
                {
                    hitBuffer[index + 1] = hitBuffer[index];
                    index--;
                }
                hitBuffer[index + 1] = value;
            }
        }
    }
}
