using System.Collections;
using UnityEngine;

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
    private Vector3 dragOffset;
    private float dragPlaneY;
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
        if (state == ItemState.Idle && TryGetPointerDown(out Vector2 downPosition))
            TryBeginDrag(downPosition);

        if (state == ItemState.Dragging)
        {
            if (TryGetPointerPosition(out Vector2 dragPosition))
                UpdateDrag(dragPosition);

            if (TryGetPointerUp(out _))
                EndDrag();
        }
    }

    // ---------- Surukleme ----------

    private void TryBeginDrag(Vector2 screenPosition)
    {
        if (activeDrag != null || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.collider != itemCollider)
            return;

        activeDrag = this;
        state = ItemState.Dragging;
        dragPlaneY = transform.position.y;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            dragOffset = transform.position - hitPoint;
            dragOffset.y = 0f;
        }
        else
        {
            dragOffset = Vector3.zero;
        }

        transform.localScale = originalScale * dragScale;
    }

    private void UpdateDrag(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));

        if (!plane.Raycast(ray, out float enter))
            return;

        Vector3 target = ray.GetPoint(enter) + dragOffset;
        target.y = dragPlaneY + dragLift;
        transform.position = target;
    }

    private void EndDrag()
    {
        activeDrag = null;
        transform.localScale = originalScale;

        bool droppedOnBag = BagDropZone.Instance != null
            && BagDropZone.Instance.ContainsPoint(transform.position);

        if (droppedOnBag && isCorrectItem)
        {
            state = ItemState.GoingToBag;
            itemCollider.enabled = false;
            Bolum1GameManager.Instance?.OnCorrectItemPlaced(this);
            StartRoutine(AnimateIntoBag());
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

    private static bool TryGetPointerDown(out Vector2 screenPosition)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;
    }

    private static bool TryGetPointerPosition(out Vector2 screenPosition)
    {
        if (Input.touchCount > 0)
        {
            screenPosition = Input.GetTouch(0).position;
            return true;
        }

        if (Input.GetMouseButton(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;
    }

    private static bool TryGetPointerUp(out Vector2 screenPosition)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;
    }
}
