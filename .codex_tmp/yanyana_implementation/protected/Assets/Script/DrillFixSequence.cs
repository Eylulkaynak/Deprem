using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  DrillFixSequence.cs  (HALKA + MATKAP + YESIL EFEKT)
//  1) Kitapliga tikla -> sallanma durur -> kamera zoom
//  2) Yanip sonen halka belirir
//  3) Halkaya tikla -> matkap gelir + ses -> sabitler -> gider
//  4) Matkap gidince KITAPLIK YESIL YANIP SONER
//  5) Kamera geri doner
// ============================================================
[RequireComponent(typeof(AudioSource))]
public class DrillFixSequence : MonoBehaviour
{
    [Header("Matkap")]
    public Transform drill;
    public Transform drillWorkPoint;
    public Transform drillAwayPoint;

    [Header("Hedef Halkasi")]
    public GameObject targetRing;
    public float ringBlinkSpeed = 3f;
    public float ringMinScale = 0.85f;
    public float ringMaxScale = 1.2f;

    [Header("Kamera Zoom")]
    public Camera cam;
    public Transform cameraZoomPoint;
    public float cameraMoveTime = 1.5f;

    [Header("Zamanlama")]
    public float drillMoveTime = 1.5f;
    public float drillWorkTime = 2f;

    [Header("Matkap Titreme")]
    public bool drillShake = true;
    public float shakeAmount = 0.02f;
    public float shakeSpeed = 40f;

    [Header("Ses")]
    public AudioClip drillSound;

    [Header("Sallanma (basta)")]
    public float wobbleAngle = 5f;
    public float wobbleSpeed = 4f;

    [Header("Yesil Efekt (matkap gidince)")]
    public Color successColor = new Color(0.15f, 1f, 0.35f, 1f);
    [Range(0f, 1f)] public float successTintStrength = 0.6f;
    [Range(0f, 1f)] public float settledTintStrength = 0.18f;
    public bool keepSoftGreenTint = true;
    [Tooltip("Kac kez yanip sonsun")]
    public int greenBlinkCount = 3;
    [Tooltip("Her yanip sonme suresi")]
    public float greenBlinkTime = 0.35f;

    private enum State { Wobbling, WaitingRing, Drilling, Done }
    private State state = State.Wobbling;

    private Quaternion baseRotation;
    private AudioSource audioSource;
    private Vector3 camStartPos;
    private Quaternion camStartRot;
    private Vector3 ringBaseScale;

    // yesil renk sistemi
    private RendererColorState[] rendererColorStates;
    private const string BaseColorProperty = "_BaseColor";
    private const string ColorProperty = "_Color";
    private const string EmissionColorProperty = "_EmissionColor";

    private struct RendererColorState
    {
        public Renderer renderer;
        public int materialIndex;
        public string colorProperty;
        public string emissionProperty;
        public Color baseColor;
        public Color baseEmissionColor;
        public MaterialPropertyBlock propertyBlock;
    }

    void Start()
    {
        baseRotation = transform.rotation;
        audioSource = GetComponent<AudioSource>();
        if (cam == null) cam = Camera.main;

        if (drill != null) drill.gameObject.SetActive(false);
        if (targetRing != null)
        {
            ringBaseScale = targetRing.transform.localScale;
            targetRing.SetActive(false);
        }

        CacheRendererColors();
    }

    void Update()
    {
        if (state == State.Wobbling)
        {
            float angle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle;
            transform.rotation = baseRotation * Quaternion.Euler(0, 0, angle);
        }

        if (state == State.WaitingRing && targetRing != null)
        {
            float pulse = (Mathf.Sin(Time.time * ringBlinkSpeed) + 1f) * 0.5f;
            float s = Mathf.Lerp(ringMinScale, ringMaxScale, pulse);
            targetRing.transform.localScale = ringBaseScale * s;
        }
    }

    public void OnTapped()
    {
        if (state == State.Wobbling)
        {
            StartCoroutine(ZoomThenShowRing());
            return;
        }
        if (state == State.WaitingRing)
        {
            StartCoroutine(DrillSequence());
            return;
        }
    }

    IEnumerator ZoomThenShowRing()
    {
        state = State.Drilling;
        transform.rotation = baseRotation;

        if (cam != null && cameraZoomPoint != null)
        {
            camStartPos = cam.transform.position;
            camStartRot = cam.transform.rotation;
            yield return MoveCamera(cameraZoomPoint.position, cameraZoomPoint.rotation, cameraMoveTime);
        }

        if (targetRing != null) targetRing.SetActive(true);
        state = State.WaitingRing;
    }

    IEnumerator DrillSequence()
    {
        state = State.Drilling;
        if (targetRing != null) targetRing.SetActive(false);

        // matkap gelir + ses
        if (drill != null && drillAwayPoint != null && drillWorkPoint != null)
        {
            drill.gameObject.SetActive(true);
            drill.position = drillAwayPoint.position;
            drill.rotation = drillAwayPoint.rotation;

            if (drillSound != null)
            {
                audioSource.clip = drillSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            yield return MoveTransform(drill, drillAwayPoint, drillWorkPoint, drillMoveTime);
        }

        // calisir (titrer)
        float t = 0f;
        Vector3 workPos = (drillWorkPoint != null) ? drillWorkPoint.position : Vector3.zero;
        while (t < drillWorkTime)
        {
            t += Time.deltaTime;
            if (drill != null && drillShake)
            {
                float sx = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
                float sy = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeAmount;
                drill.position = workPos + new Vector3(sx, sy, 0f);
            }
            yield return null;
        }
        if (drill != null) drill.position = workPos;

        if (audioSource.isPlaying) audioSource.Stop();
        audioSource.loop = false;

        // matkap gider
        if (drill != null && drillWorkPoint != null && drillAwayPoint != null)
        {
            yield return MoveTransform(drill, drillWorkPoint, drillAwayPoint, drillMoveTime);
            drill.gameObject.SetActive(false);
        }

        // *** MATKAP GITTI -> YESIL YANIP SONME ***
        yield return GreenBlink();

        // kamera geri
        if (cam != null && cameraZoomPoint != null)
        {
            yield return MoveCamera(camStartPos, camStartRot, cameraMoveTime);
        }

        if (LevelManager.Instance != null) LevelManager.Instance.HazardFixed(gameObject);
        state = State.Done;
    }

    // Yesil yanip sonme
    IEnumerator GreenBlink()
    {
        for (int i = 0; i < greenBlinkCount; i++)
        {
            // parla
            float t = 0f;
            while (t < greenBlinkTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin((t / greenBlinkTime) * Mathf.PI); // 0->1->0
                ApplyTint(successColor, successTintStrength * k);
                yield return null;
            }
        }
        // hafif kalici yesil
        ApplyTint(successColor, keepSoftGreenTint ? settledTintStrength : 0f);
    }

    // ---- renk sistemi (WobbleFixer'daki) ----
    void CacheRendererColors()
    {
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        List<RendererColorState> states = new List<RendererColorState>();

        foreach (Renderer childRenderer in childRenderers)
        {
            // halkayi boyama disinda tut (istersen)
            if (targetRing != null && childRenderer.gameObject == targetRing) continue;

            Material[] materials = childRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null) continue;

                string colorProperty = "";
                if (material.HasProperty(BaseColorProperty)) colorProperty = BaseColorProperty;
                else if (material.HasProperty(ColorProperty)) colorProperty = ColorProperty;
                if (string.IsNullOrEmpty(colorProperty)) continue;

                string emissionProperty = material.HasProperty(EmissionColorProperty) ? EmissionColorProperty : "";
                states.Add(new RendererColorState
                {
                    renderer = childRenderer,
                    materialIndex = i,
                    colorProperty = colorProperty,
                    emissionProperty = emissionProperty,
                    baseColor = material.GetColor(colorProperty),
                    baseEmissionColor = string.IsNullOrEmpty(emissionProperty) ? Color.black : material.GetColor(emissionProperty),
                    propertyBlock = new MaterialPropertyBlock()
                });
            }
        }
        rendererColorStates = states.ToArray();
    }

    void ApplyTint(Color tint, float strength)
    {
        if (rendererColorStates == null) return;
        float s = Mathf.Clamp01(strength);
        for (int i = 0; i < rendererColorStates.Length; i++)
        {
            RendererColorState st = rendererColorStates[i];
            if (st.renderer == null) continue;

            st.renderer.GetPropertyBlock(st.propertyBlock, st.materialIndex);
            Color color = Color.Lerp(st.baseColor, tint, s);
            color.a = st.baseColor.a;
            st.propertyBlock.SetColor(st.colorProperty, color);

            if (!string.IsNullOrEmpty(st.emissionProperty))
            {
                Color em = Color.Lerp(st.baseEmissionColor, tint * 1.4f, s);
                em.a = 1f;
                st.propertyBlock.SetColor(st.emissionProperty, em);
            }
            st.renderer.SetPropertyBlock(st.propertyBlock, st.materialIndex);
        }
    }

    IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot, float time)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = t / time;
            k = k * k * (3f - 2f * k);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, k);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }
        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    IEnumerator MoveTransform(Transform obj, Transform from, Transform to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = t / time;
            k = k * k * (3f - 2f * k);
            obj.position = Vector3.Lerp(from.position, to.position, k);
            obj.rotation = Quaternion.Slerp(from.rotation, to.rotation, k);
            yield return null;
        }
        obj.position = to.position;
        obj.rotation = to.rotation;
    }

    public bool IsFixed() => state == State.Done;
}