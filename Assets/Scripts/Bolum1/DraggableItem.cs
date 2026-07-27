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

    [Tooltip("Suruklerken gosterilecek ad. Bossa obje adi duzenlenerek kullanilir.")]
    [SerializeField] private string displayName = "";

    [Tooltip("Etiketin takip edecegi nokta. Bossa modelin ust siniri kullanilir.")]
    [SerializeField] private Transform labelAnchor;

    [Tooltip("Otomatik etiket konumuna eklenecek yukseklik.")]
    [SerializeField] private float labelHeightPadding = 0.08f;

    [Header("Surukleme")]
    [Tooltip("Kapalıysa girdi başka bir manager tarafından yönetilir; çantaya giriş animasyonu yine kullanılabilir.")]
    [SerializeField] private bool inputEnabled = true;

    [Tooltip("Kapalıysa yerleştirme Bolum1GameManager skorunu değiştirmez.")]
    [SerializeField] private bool notifyGameManager = true;

    [Tooltip("Suruklerken esyanin zeminden ne kadar yukselecegi.")]
    [SerializeField] private float dragLift = 0.15f;

    [Tooltip("Dikey pano gibi hedeflerde nesne parmağı ekran düzleminde takip etsin.")]
    [SerializeField] private bool dragOnCameraPlane;

    [Tooltip("Atandığında sürükleme düzleminin konumunu ve yönünü bu ortak nokta belirler. Dikey pano kartlarında anchor'ın forward ekseni pano normalidir.")]
    [SerializeField] private Transform dragPlaneAnchor;

    [Tooltip("Açıkken tutulan nesnenin merkezi bu dünya yüksekliğinin altına inemez.")]
    [SerializeField] private bool clampDragMinimumY;

    [Tooltip("Masa gibi katı yüzeylerden geçmeyi önleyen en düşük sürükleme merkezi.")]
    [SerializeField] private float dragMinimumWorldY;

    [Tooltip("Tutulunca düz nesneyi kameraya çevir; kart ve belge sürüklemeleri için.")]
    [SerializeField] private bool faceCameraWhileDragging;

    [Tooltip("Kameraya dönük kartın eldeyken yapacağı hafif salınım (derece).")]
    [SerializeField, Min(0f)] private float dragHoverWobbleDegrees;

    [Tooltip("Eldeki kart salınımının saniyedeki tur sayısı.")]
    [SerializeField, Min(0f)] private float dragHoverWobbleSpeed = 1.6f;

    [Tooltip("Yaklaşınca kartı hafifçe kendine çeken pano yuvaları. Doğru/yanlış bütün yuvalar burada olabilir.")]
    [SerializeField] private Transform[] magneticSnapTargets;

    [Tooltip("Manyetik çekimin ekranın kısa kenarına göre etki yarıçapı.")]
    [SerializeField, Range(0.02f, 0.3f)] private float magneticSnapViewportRadius = 0.14f;

    [Tooltip("Kart masadan gerçekten ayrılmadan slot çekiminin başlamaması için gereken en az parmak hareketi.")]
    [SerializeField, Min(0f)] private float magneticSnapMinTravelPixels = 48f;

    [Tooltip("Yuvaya yaklaşınca uygulanacak en yüksek çekim gücü.")]
    [SerializeField, Range(0f, 1f)] private float magneticSnapStrength = 0.86f;

    [Tooltip("Kartın manyetik hover pozunda slot yüzeyinin ne kadar önünde kalacağı.")]
    [SerializeField, Min(0f)] private float magneticSnapSurfaceOffset;

    [Tooltip("Suruklerken esyanin buyume orani (1 = ayni boyut).")]
    [SerializeField] private float dragScale = 1.1f;

    [Tooltip("Kisa dokunmayla dogru esyayi dogrudan cantaya ekle.")]
    [SerializeField] private bool tapToBagEnabled = true;

    [Tooltip("Story sahnelerinde nesnenin kendi fiziksel hedefi. Boşsa afet çantasının ortak drop zone'u kullanılır.")]
    [SerializeField] private BagDropZone dropZoneOverride;

    [Tooltip("Dokunma sayilmasi icin izin verilen en fazla ekran hareketi (piksel).")]
    [SerializeField] private float tapMaxMovementPixels = 24f;

    [Header("Cantaya Girme Animasyonu")]
    [Tooltip("Esyanin canta agzina gitme suresi (saniye).")]
    [SerializeField] private float moveToOpeningDuration = 0.25f;

    [Tooltip("Esyanin agizdan cantanin icine inme suresi (saniye).")]
    [SerializeField] private float descendIntoBagDuration = 0.2f;

    [Tooltip("Esyanin canta agzina giderken cizecegi kisa yay.")]
    [SerializeField] private float moveToOpeningArcHeight = 0.22f;

    [Tooltip("Cantaya gitmeden onceki kisa vurgu buyumesi.")]
    [SerializeField] private float bagEntryPopScale = 1.12f;

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
    public bool IsDragging => state == ItemState.Dragging;
    public static DraggableItem ActiveDrag => activeDrag;
    public float BagEntryDuration => moveToOpeningDuration + descendIntoBagDuration;
    public BagDropZone DropZoneOverride => dropZoneOverride;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            string fallback = gameObject.name
                .Replace("(Clone)", "")
                .Replace('_', ' ')
                .Trim();

            return fallback.StartsWith("Item ", StringComparison.OrdinalIgnoreCase)
                ? fallback.Substring(5)
                : fallback;
        }
    }

    public Vector3 GetLabelWorldPosition()
    {
        if (labelAnchor != null)
            return labelAnchor.position;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return transform.position + Vector3.up * (0.2f + labelHeightPadding);

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);

        return new Vector3(bounds.center.x, bounds.max.y + labelHeightPadding, bounds.center.z);
    }

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
    private Quaternion originalRotation;
    private float dragStartedAt;
    private Vector3 dragStartWorldPosition;
    private Vector2 pointerDownScreenPosition;
    private Plane dragPlane;
    private Vector3 dragPointerOffset;
    private float dragPlaneHeight;
    private float currentMagnetWeight;
    private Transform currentMagneticTarget;
    private ItemState state = ItemState.Idle;
    private Coroutine activeRoutine;

    private void Awake()
    {
        mainCamera = Camera.main;
        itemCollider = GetComponent<Collider>();
        itemCollider.isTrigger = false;
        startPosition = transform.position;
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
    }

    private void Update()
    {
        if (!inputEnabled)
            return;

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

        BeginDragAt(screenPosition);
    }

    private void BeginDragAt(Vector2 screenPosition)
    {
        activeDrag = this;
        state = ItemState.Dragging;
        dragStartedAt = Time.unscaledTime;
        dragStartWorldPosition = transform.position;
        pointerDownScreenPosition = screenPosition;
        dragPlaneHeight = dragStartWorldPosition.y + dragLift;
        currentMagnetWeight = 0f;
        currentMagneticTarget = null;
        bool useAnchoredPlane = dragOnCameraPlane && dragPlaneAnchor != null;
        Vector3 dragPlanePoint = useAnchoredPlane
            ? dragPlaneAnchor.position
            : dragStartWorldPosition;
        dragPlane = dragOnCameraPlane
            ? new Plane(
                useAnchoredPlane ? dragPlaneAnchor.forward : mainCamera.transform.forward,
                dragPlanePoint)
            : new Plane(Vector3.up, new Vector3(0f, dragPlaneHeight, 0f));

        Ray pointerRay = mainCamera.ScreenPointToRay(screenPosition);
        if (dragPlane.Raycast(pointerRay, out float enter))
        {
            Vector3 pointerWorld = pointerRay.GetPoint(enter);
            dragPointerOffset = useAnchoredPlane
                ? Vector3.zero
                : dragStartWorldPosition - pointerWorld;
            if (!dragOnCameraPlane || useAnchoredPlane)
                dragPointerOffset.y = 0f;
        }
        else
        {
            dragPointerOffset = Vector3.zero;
        }

        transform.localScale = originalScale * dragScale;
        UpdateDragFacing();
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
            // Kamera blend sırasında pano-paralel düzleme kısa süreli ters bakabilir.
            // Konum o karede sabit kalsa da kart yeni kameraya dönük kalmalı.
            UpdateDragFacing();
            return;
        }

        Vector3 target = ray.GetPoint(enter) + dragPointerOffset;
        if (dragOnCameraPlane)
            target += Vector3.up * dragLift;
        else
            target.y = dragPlaneHeight;

        currentMagnetWeight = 0f;
        currentMagneticTarget = null;
        if (TryGetMagneticSnap(
                screenPosition,
                out Vector3 snapPosition,
                out float magnetWeight,
                out Transform magneticTarget))
        {
            currentMagnetWeight = magnetWeight;
            currentMagneticTarget = magneticTarget;
            target = Vector3.Lerp(target, snapPosition, magnetWeight);
        }

        if (clampDragMinimumY)
            target.y = Mathf.Max(target.y, dragMinimumWorldY);

        transform.position = target;
        UpdateDragFacing();
    }

    private bool TryGetMagneticSnap(
        Vector2 screenPosition,
        out Vector3 snapPosition,
        out float magnetWeight,
        out Transform magneticTarget)
    {
        snapPosition = default;
        magnetWeight = 0f;
        magneticTarget = null;
        if (mainCamera == null || magneticSnapTargets == null || magneticSnapTargets.Length == 0)
            return false;
        if (Vector2.Distance(screenPosition, pointerDownScreenPosition) < magneticSnapMinTravelPixels)
            return false;

        float radiusPixels = Mathf.Max(
            32f,
            Mathf.Min(Screen.width, Screen.height) * magneticSnapViewportRadius);
        Transform nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Transform candidate in magneticSnapTargets)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
                continue;

            Vector3 candidateScreen = mainCamera.WorldToScreenPoint(candidate.position);
            if (candidateScreen.z <= 0f)
                continue;

            float distance = Vector2.Distance(screenPosition, candidateScreen);
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        if (nearest == null || nearestDistance > radiusPixels)
            return false;

        float proximity = 1f - nearestDistance / radiusPixels;
        float insertionProgress = Mathf.InverseLerp(0.05f, 0.72f, proximity);
        float smoothInsertion = insertionProgress * insertionProgress * (3f - 2f * insertionProgress);
        magnetWeight = Mathf.Clamp01(smoothInsertion * magneticSnapStrength);
        magneticTarget = nearest;
        snapPosition = dragPlaneAnchor != null
            ? nearest.position + dragPlaneAnchor.forward * magneticSnapSurfaceOffset
            : dragPlane.ClosestPointOnPlane(nearest.position);
        if (dragOnCameraPlane)
            snapPosition += Vector3.up * dragLift;
        else
            snapPosition.y = dragPlaneHeight;
        return true;
    }

    private bool IsWithinMagneticAcceptance(Vector2 screenPosition, BagDropZone dropZone)
    {
        if (mainCamera == null || dropZone == null ||
            magneticSnapTargets == null || magneticSnapTargets.Length == 0)
            return false;

        Vector3 targetScreen = mainCamera.WorldToScreenPoint(dropZone.transform.position);
        if (targetScreen.z <= 0f)
            return false;

        float radiusPixels = Mathf.Max(
            28f,
            Mathf.Min(Screen.width, Screen.height) * magneticSnapViewportRadius * 0.82f);
        return Vector2.Distance(screenPosition, targetScreen) <= radiusPixels;
    }

    private void UpdateDragFacing()
    {
        if (!faceCameraWhileDragging || mainCamera == null)
            return;

        // Cinemachine kart tutulduktan sonra hâlâ blend ediyor olabilir.
        // Açıyı her karede yenile; kart eski kamera açısıyla duvara saplanmasın.
        Quaternion cameraFacing = Quaternion.LookRotation(
            mainCamera.transform.up,
            -mainCamera.transform.forward);
        Quaternion dragRotation = cameraFacing;
        if (currentMagneticTarget != null && dragPlaneAnchor != null)
        {
            Quaternion socketRotation = Quaternion.LookRotation(
                dragPlaneAnchor.up,
                dragPlaneAnchor.forward);
            dragRotation = Quaternion.Slerp(dragRotation, socketRotation, currentMagnetWeight);
        }
        float wobble = dragHoverWobbleDegrees <= 0f
            ? 0f
            : Mathf.Sin(
                (Time.unscaledTime - dragStartedAt) *
                Mathf.Max(0.01f, dragHoverWobbleSpeed) *
                Mathf.PI * 2f) * dragHoverWobbleDegrees *
              Mathf.Lerp(1f, 0.58f, currentMagnetWeight);
        transform.rotation = dragRotation * Quaternion.AngleAxis(wobble, Vector3.up);
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

        BagDropZone dropZone = ResolveDropZone();
        bool droppedOnBag = dropZone != null &&
            (dropZone.ContainsScreenPoint(releasePosition, mainCamera) ||
             dropZone.ContainsPoint(transform.position) ||
             IsWithinMagneticAcceptance(releasePosition, dropZone));

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
        transform.localScale = originalScale * bagEntryPopScale;
        if (notifyGameManager)
            Bolum1GameManager.Instance?.OnCorrectItemPlaced(this);
        StartRoutine(AnimateIntoBag());
    }

    /// <summary>
    /// Story sahneleri eski bölümün fiziksel "eşya çantaya uçar ve içine iner" davranışını
    /// merkezi dokunma yöneticisinden tetiklemek için bunu kullanır.
    /// </summary>
    public bool SendToBag()
    {
        if (state != ItemState.Idle || !isCorrectItem || ResolveDropZone() == null)
            return false;

        PlaceInBag();
        return true;
    }

    /// <summary>
    /// StoryTouchManager icin dogrudan dunya-nesnesi suruklemesini baslatir.
    /// Girdi bu component tarafindan okunmaz; tek girdi sahibi manager olarak kalir.
    /// </summary>
    public bool BeginManagedDrag(Vector2 screenPosition)
    {
        if (state != ItemState.Idle || activeDrag != null || mainCamera == null)
            return false;

        BeginDragAt(screenPosition);
        return state == ItemState.Dragging;
    }

    public void UpdateManagedDrag(Vector2 screenPosition)
    {
        if (state == ItemState.Dragging)
            UpdateDrag(screenPosition);
    }

    /// <summary>
    /// Esya canta agzinda birakildiysa true doner. Dogru esyanin kalici sonucunu
    /// StoryInteractable olayi verir; yanlis esya guvenle baslangic yerine doner.
    /// </summary>
    public bool EndManagedDrag(Vector2 screenPosition)
    {
        if (state != ItemState.Dragging)
            return false;

        UpdateDrag(screenPosition);
        activeDrag = null;
        transform.localScale = originalScale;
        BagDropZone dropZone = ResolveDropZone();
        bool droppedOnBag = dropZone != null &&
            (dropZone.ContainsScreenPoint(screenPosition, mainCamera) ||
             dropZone.ContainsPoint(transform.position) ||
             IsWithinMagneticAcceptance(screenPosition, dropZone));
        if (!droppedOnBag)
        {
            ReturnToStart();
            return false;
        }

        if (!isCorrectItem)
        {
            ReturnToStart();
            return true;
        }

        state = ItemState.Idle;
        itemCollider.enabled = true;
        return true;
    }

    public void CancelManagedDrag()
    {
        if (state != ItemState.Dragging)
            return;

        activeDrag = null;
        transform.localScale = originalScale;
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
        transform.rotation = originalRotation;
        state = ItemState.Idle;
    }

    // ---------- Animasyonlar ----------

    private IEnumerator AnimateIntoBag()
    {
        BagDropZone dropZone = ResolveDropZone();
        if (dropZone == null)
        {
            state = ItemState.InBag;
            yield break;
        }

        Vector3 openingPosition = dropZone.GetOpeningPosition(transform.position);

        // Gorunur kalacaksa bos slot iste; slot yoksa otomatik gizlenir.
        Vector3 slotPosition = default;
        bool stayVisible = !hideItemInBag
            && dropZone.TryClaimSlot(out slotPosition);

        Vector3 targetPosition = stayVisible ? slotPosition : dropZone.GetInsidePosition();
        float targetMultiplier = stayVisible ? visibleInBagMultiplier : hiddenShrinkMultiplier;
        Vector3 targetScale = originalScale * targetMultiplier;

        // 1) Canta agzina git (boyut ayni kalir)
        yield return MoveAndScale(
            transform.position,
            openingPosition,
            transform.localScale,
            originalScale,
            moveToOpeningDuration,
            moveToOpeningArcHeight);

        // 2) Agizdan hedefe in (kuculerek)
        yield return MoveAndScale(openingPosition, targetPosition, transform.localScale, targetScale, descendIntoBagDuration, 0f);

        state = ItemState.InBag;

        if (!stayVisible)
            gameObject.SetActive(false);
    }

    private BagDropZone ResolveDropZone()
    {
        return dropZoneOverride != null ? dropZoneOverride : BagDropZone.Instance;
    }

    private IEnumerator AnimateReturnToStart()
    {
        yield return MoveAndScale(
            transform.position,
            startPosition,
            transform.localScale,
            originalScale,
            returnDuration,
            returnArcHeight,
            faceCameraWhileDragging ? transform.rotation : null,
            faceCameraWhileDragging ? originalRotation : null);

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
        float arcHeight,
        Quaternion? fromRotation = null,
        Quaternion? toRotation = null)
    {
        if (duration <= 0f)
        {
            transform.position = toPosition;
            transform.localScale = toScale;
            if (toRotation.HasValue)
                transform.rotation = toRotation.Value;
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
            if (fromRotation.HasValue && toRotation.HasValue)
                transform.rotation = Quaternion.Slerp(fromRotation.Value, toRotation.Value, easedT);
            yield return null;
        }

        transform.position = toPosition;
        transform.localScale = toScale;
        if (toRotation.HasValue)
            transform.rotation = toRotation.Value;
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
