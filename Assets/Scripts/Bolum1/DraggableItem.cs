using System.Collections;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Surukle-birak esyasi. Dokunma (mobil) ve mouse (editor) ile calisir.
/// Dogru esya cantaya birakilinca agiz noktasina gidip icine iner,
/// yanlis esya veya bosa birakilan esya yumusak animasyonla yerine doner.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DraggableItem : MonoBehaviour
{
    private enum ItemState
    {
        Idle,           // Yerinde duruyor, sureklenebilir
        Dragging,       // Parmak/mouse ile tasiniyor
        GoingToBag,     // Cantaya girme animasyonunda
        Returning,      // Yerine donme animasyonunda
        InBag           // Cantada, artik sureklenemez
    }

    private static DraggableItem activeDrag;

    [Header("Esya")]
    [Tooltip("Bu esya afet cantasina konmasi gereken dogru bir esya mi?")]
    [SerializeField] private bool isCorrectItem = true;

    [Header("Surukleme")]
    [Tooltip("Suruklerken esyanin zeminden ne kadar yukselecegi.")]
    [SerializeField] private float dragLift = 0.15f;

    [Tooltip("Suruklerken esyanin buyume orani (1 = ayni boyut).")]
    [SerializeField] private float dragScale = 1.1f;

    [Tooltip("Kisa dokunmayla dogru esyayi dogrudan cantaya ekle.")]
    [SerializeField] private bool tapToBagEnabled = true;

    [Tooltip("Dokunma sayilmasi icin izin verilen en fazla ekran hareketi (piksel).")]
    [SerializeField] private float tapMaxMovementPixels = 24f;

    [Header("Cantaya Girme Animasyonu")]
    [Tooltip("Esyanin canta agzina gitme suresi (saniye).")]
    [SerializeField] private float moveToOpeningDuration = 0.25f;

    [Tooltip("Esyanin agizdan cantanin icine inme suresi (saniye).")]
    [SerializeField] private float descendIntoBagDuration = 0.2f;

    [Tooltip("Animasyon bitince esya gizlensin mi? Kapaliysa cantada kucuk halde gorunur.")]
    [SerializeField] private bool hideItemInBag = true;

    [Tooltip("Gizlenecekse animasyon sonunda esyanin kuculecegi oran.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float hiddenShrinkMultiplier = 0.15f;

    [Tooltip("Gizlenmeyecekse cantada gorunecegi boyut orani.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float visibleInBagMultiplier = 0.6f;

    [Header("Yerine Donme Animasyonu")]
    [Tooltip("Yanlis esyanin veya bosa birakilan esyanin yerine donme suresi (saniye).")]
    [SerializeField] private float returnDuration = 0.8f;

    [Tooltip("Yerine donerken havada cizecegi kavis yuksekligi (0 = duz gider).")]
    [SerializeField] private float returnArcHeight = 0.3f;

    [Header("Animasyon Egrisi")]
    [Tooltip("Tum hareket animasyonlarinda kullanilan hiz egrisi.")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public bool IsCorrectItem => isCorrectItem;
    public bool IsInBag => state == ItemState.InBag;

    /// <summary>Su an herhangi bir esya surukleniyor mu? (ipucu sistemi icin)</summary>
    public static bool IsAnyDragging => activeDrag != null;

    /// <summary>Esya masada bos duruyor mu? (ipucu gosterilebilir mi)</summary>
    public bool IsIdleOnTable => state == ItemState.Idle && gameObject.activeInHierarchy;

    /// <summary>Ipucu icin kisa yanip sonme (buyuyup kuculme) efekti oynatir.</summary>
    public void PlayHintPulse(float pulseScale, float duration, int pulseCount)
    {
        if (state != ItemState.Idle)
            return;

        StartRoutine(HintPulseRoutine(pulseScale, duration, pulseCount));
    }

    private IEnumerator HintPulseRoutine(float pulseScale, float duration, int pulseCount)
    {
        float elapsed = 0f;
        float totalDuration = duration * pulseCount;

        while (elapsed < totalDuration)
        {
            // Kullanici esyayi tutarsa efekti birak
            if (state != ItemState.Idle)
                break;

            elapsed += Time.deltaTime;
            float wave = Mathf.Sin((elapsed / duration) * Mathf.PI * 2f);
            float scale = 1f + Mathf.Abs(wave) * (pulseScale - 1f);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        if (state == ItemState.Idle)
            transform.localScale = originalScale;

        activeRoutine = null;
    }

    private Camera mainCamera;
    private Collider itemCollider;
    private Vector3 startPosition;
    private Vector3 originalScale;
    private Vector3 dragStartWorldPosition;
    private Vector2 pointerDownScreenPosition;
    private Plane dragPlane;
    private Vector3 dragPointerOffset;
    private float dragPlaneHeight;
    private ItemState state = ItemState.Idle;
    private Coroutine activeRoutine;

    private void Awake()
    {
        mainCamera = Camera.main;
        itemCollider = GetComponent<Collider>();
        itemCollider.isTrigger = false;
        startPosition = transform.position;
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (state == ItemState.Idle &&
            TryGetPointerDown(out Vector2 downPosition, out int pointerId) &&
            !IsPointerOverUi(pointerId))
        {
            TryBeginDrag(downPosition);
        }

        if (state == ItemState.Dragging)
        {
            if (TryGetPointerPosition(out Vector2 dragPosition))
                UpdateDrag(dragPosition);

            if (TryGetPointerUp(out Vector2 releasePosition))
                EndDrag(releasePosition);
        }
    }

    // ---------- Surukleme ----------

    private void TryBeginDrag(Vector2 screenPosition)
    {
        if (activeDrag != null || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (FindFirstDraggableItem(ray) != this)
            return;

        activeDrag = this;
        state = ItemState.Dragging;
        dragStartWorldPosition = transform.position;
        pointerDownScreenPosition = screenPosition;
        dragPlaneHeight = dragStartWorldPosition.y + dragLift;
        dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneHeight, 0f));

        Ray pointerRay = mainCamera.ScreenPointToRay(screenPosition);
        if (dragPlane.Raycast(pointerRay, out float enter))
        {
            Vector3 pointerWorld = pointerRay.GetPoint(enter);
            dragPointerOffset = dragStartWorldPosition - pointerWorld;
            dragPointerOffset.y = 0f;
        }
        else
        {
            dragPointerOffset = Vector3.zero;
        }

        transform.localScale = originalScale * dragScale;
        UpdateDrag(screenPosition);
    }

    private static DraggableItem FindFirstDraggableItem(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        if (hits.Length == 0)
            return null;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            DraggableItem item = hit.collider.GetComponentInParent<DraggableItem>();
            if (item != null && item.state == ItemState.Idle && item.isActiveAndEnabled)
                return item;
        }

        return null;
    }

    private void UpdateDrag(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!dragPlane.Raycast(ray, out float enter))
        {
            return;
        }

        Vector3 target = ray.GetPoint(enter) + dragPointerOffset;
        target.y = dragPlaneHeight;
        transform.position = target;
    }

    private void EndDrag(Vector2 releasePosition)
    {
        activeDrag = null;
        transform.localScale = originalScale;

        bool wasTap = tapToBagEnabled
            && Vector2.Distance(pointerDownScreenPosition, releasePosition) <= tapMaxMovementPixels;

        if (wasTap)
        {
            if (isCorrectItem)
                PlaceInBag();
            else
                Bolum1GameManager.Instance?.OnWrongItemPlaced(this);

            return;
        }

        bool droppedOnBag = BagDropZone.Instance != null
            && BagDropZone.Instance.ContainsPoint(transform.position);

        if (droppedOnBag && isCorrectItem)
        {
            PlaceInBag();
            return;
        }

        if (droppedOnBag && !isCorrectItem)
        {
            // Manager mesaji gosterir; donus animasyonunu ReturnToStart baslatir.
            Bolum1GameManager.Instance?.OnWrongItemPlaced(this);
            return;
        }

        ReturnToStart();
    }

    private void PlaceInBag()
    {
        state = ItemState.GoingToBag;
        itemCollider.enabled = false;
        Bolum1GameManager.Instance?.OnCorrectItemPlaced(this);
        StartRoutine(AnimateIntoBag());
    }

    // ---------- Disaridan cagrilan ----------

    /// <summary>Esyayi yumusak animasyonla baslangic yerine dondurur.</summary>
    public void ReturnToStart()
    {
        if (state == ItemState.Returning)
            return;

        gameObject.SetActive(true);
        itemCollider.enabled = false;
        state = ItemState.Returning;
        StartRoutine(AnimateReturnToStart());
    }

    /// <summary>Esyayi animasyonsuz aninda sifirlar (yeniden baslatma icin).</summary>
    public void ResetInstant()
    {
        StopRoutine();
        gameObject.SetActive(true);
        itemCollider.enabled = true;
        transform.position = startPosition;
        transform.localScale = originalScale;
        state = ItemState.Idle;
    }

    // ---------- Animasyonlar ----------

    private IEnumerator AnimateIntoBag()
    {
        if (BagDropZone.Instance == null)
        {
            state = ItemState.InBag;
            yield break;
        }

        Vector3 openingPosition = BagDropZone.Instance.GetOpeningPosition(transform.position);

        // Gorunur kalacaksa bos slot iste; slot yoksa otomatik gizlenir.
        Vector3 slotPosition = default;
        bool stayVisible = !hideItemInBag
            && BagDropZone.Instance.TryClaimSlot(out slotPosition);

        Vector3 targetPosition = stayVisible ? slotPosition : BagDropZone.Instance.GetInsidePosition();
        float targetMultiplier = stayVisible ? visibleInBagMultiplier : hiddenShrinkMultiplier;
        Vector3 targetScale = originalScale * targetMultiplier;

        // 1) Canta agzina git (boyut ayni kalir)
        yield return MoveAndScale(transform.position, openingPosition, transform.localScale, transform.localScale, moveToOpeningDuration, 0f);

        // 2) Agizdan hedefe in (kuculerek)
        yield return MoveAndScale(openingPosition, targetPosition, transform.localScale, targetScale, descendIntoBagDuration, 0f);

        state = ItemState.InBag;

        if (!stayVisible)
            gameObject.SetActive(false);
    }

    private IEnumerator AnimateReturnToStart()
    {
        yield return MoveAndScale(transform.position, startPosition, transform.localScale, originalScale, returnDuration, returnArcHeight);

        itemCollider.enabled = true;
        state = ItemState.Idle;
    }

    /// <summary>
    /// Pozisyon ve boyutu ayni anda animasyonlu degistirir.
    /// arcHeight > 0 ise hareket ortasinda yukari kavis yapar.
    /// </summary>
    private IEnumerator MoveAndScale(
        Vector3 fromPosition,
        Vector3 toPosition,
        Vector3 fromScale,
        Vector3 toScale,
        float duration,
        float arcHeight)
    {
        if (duration <= 0f)
        {
            transform.position = toPosition;
            transform.localScale = toScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = moveCurve.Evaluate(t);

            Vector3 position = Vector3.Lerp(fromPosition, toPosition, easedT);

            if (arcHeight > 0f)
                position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            transform.position = position;
            transform.localScale = Vector3.Lerp(fromScale, toScale, easedT);
            yield return null;
        }

        transform.position = toPosition;
        transform.localScale = toScale;
    }

    // ---------- Coroutine yonetimi ----------

    private void StartRoutine(IEnumerator routine)
    {
        StopRoutine();
        activeRoutine = StartCoroutine(routine);
    }

    private void StopRoutine()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
    }

    // ---------- Girdi (dokunma + mouse) ----------

    private static bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        if (UnityEngine.Input.touchCount > 0)
        {
            UnityEngine.Touch touch = UnityEngine.Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }
        }

        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            screenPosition = UnityEngine.Input.mousePosition;
            pointerId = -1;
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = 0;
            return true;
        }

        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }
#endif

        screenPosition = default;
        pointerId = -1;
        return false;
    }

    private static bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (pointerId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private static bool TryGetPointerPosition(out Vector2 screenPosition)
    {
        if (UnityEngine.Input.touchCount > 0)
        {
            screenPosition = UnityEngine.Input.GetTouch(0).position;
            return true;
        }

        if (UnityEngine.Input.GetMouseButton(0))
        {
            screenPosition = UnityEngine.Input.mousePosition;
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
        {
            screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return true;
        }
#endif

        screenPosition = default;
        return false;
    }

    private static bool TryGetPointerUp(out Vector2 screenPosition)
    {
        if (UnityEngine.Input.touchCount > 0)
        {
            UnityEngine.Touch touch = UnityEngine.Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        if (UnityEngine.Input.GetMouseButtonUp(0))
        {
            screenPosition = UnityEngine.Input.mousePosition;
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null &&
            UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            screenPosition = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
        {
            screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return true;
        }
#endif

        screenPosition = default;
        return false;
    }
}
