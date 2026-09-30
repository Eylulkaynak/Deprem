using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  WobbleFixer.cs  (GELISTIRILMIS)
//  Obje sallanir -> dokun -> yavasca duzelir + braket belirir
//  + yesil glow + "Sabitlendi!" yazisi + ses.
//  Her sabitlenecek objeye ekle + Box Collider ekle.
// ============================================================
[RequireComponent(typeof(AudioSource))]
public class WobbleFixer : MonoBehaviour
{
    [Header("Kimlik")]
    public string objectName = "Kitaplik";

    [Header("Sallanma (tehlikeli hal)")]
    public float wobbleAngle = 6f;
    public float wobbleSpeed = 4f;

    [Header("Sabitleme")]
    [Tooltip("Duzelme kac saniye sursun")]
    public float fixDuration = 0.7f;

    [Header("Gorsel Oduller (opsiyonel - varsa surukle)")]
    public GameObject bracketMarker;   // braket + kayis gorseli (basta kapali)
    public GameObject greenGlow;       // yesil parlama (basta kapali)
    public GameObject checkMark;       // onay isareti (basta kapali)
    public GameObject fixedLabel;      // "Sabitlendi! Artik dusmez" yazisi (basta kapali)

    [Header("Yesil Efekt (otomatik)")]
    public bool playAutoGreenEffect = true;
    public Color successColor = new Color(0.15f, 1f, 0.35f, 1f);
    [Range(0f, 1f)] public float successTintStrength = 0.55f;
    [Range(0f, 1f)] public float settledTintStrength = 0.18f;
    public bool keepSoftGreenTint = true;
    public float successEffectDuration = 0.9f;
    public float successPulseScale = 1.06f;
    public bool createSuccessParticles = true;

    [Header("Ses (opsiyonel)")]
    public AudioClip clickSound;       // dokunma ani
    public AudioClip fixedSound;       // sabitlendi ani

    // --- ic durum ---
    private bool isFixed = false;
    private bool isFixing = false;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private AudioSource audioSource;
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
        baseScale = transform.localScale;
        audioSource = GetComponent<AudioSource>();
        CacheRendererColors();

        SetActiveSafe(bracketMarker, false);
        SetActiveSafe(greenGlow, false);
        SetActiveSafe(checkMark, false);
        SetActiveSafe(fixedLabel, false);
    }

    void Update()
    {
        if (isFixed) return;

        // Henuz sabitlenmedi -> salla
        float angle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle;
        transform.rotation = baseRotation * Quaternion.Euler(0, 0, angle);
    }

    public void OnTapped()
    {
        if (isFixed || isFixing) return;
        StartCoroutine(FixRoutine());
    }

    IEnumerator FixRoutine()
    {
        isFixing = true;

        if (clickSound != null) audioSource.PlayOneShot(clickSound);

        SetActiveSafe(bracketMarker, true);

        Quaternion startRot = transform.rotation;
        float duration = Mathf.Max(0.05f, fixDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            k = k * k * (3f - 2f * k);   // yumusak ease
            transform.rotation = Quaternion.Slerp(startRot, baseRotation, k);
            yield return null;
        }
        transform.rotation = baseRotation;

        isFixed = true;
        if (LevelManager.Instance != null) LevelManager.Instance.HazardFixed(gameObject);
        isFixing = false;

       // SetActiveSafe(greenGlow, true);
        SetActiveSafe(checkMark, true);
        SetActiveSafe(fixedLabel, true);

        if (fixedSound != null) audioSource.PlayOneShot(fixedSound);
       // if (playAutoGreenEffect) StartCoroutine(PlaySuccessEffect());

        yield return new WaitForSeconds(2f);
        SetActiveSafe(checkMark, false);
        SetActiveSafe(fixedLabel, false);
    }

    IEnumerator PlaySuccessEffect()
    {
        if (createSuccessParticles) SpawnSuccessParticles();

        float duration = Mathf.Max(0.05f, successEffectDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);

            ApplyGreenTint(successTintStrength * pulse);
            transform.localScale = baseScale * Mathf.Lerp(1f, successPulseScale, pulse);
            yield return null;
        }

        transform.localScale = baseScale;
        ApplyGreenTint(keepSoftGreenTint ? settledTintStrength : 0f);
    }

    void CacheRendererColors()
    {
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        List<RendererColorState> states = new List<RendererColorState>();

        foreach (Renderer childRenderer in childRenderers)
        {
            Material[] materials = childRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null) continue;

                string colorProperty = "";
                if (material.HasProperty(BaseColorProperty))
                {
                    colorProperty = BaseColorProperty;
                }
                else if (material.HasProperty(ColorProperty))
                {
                    colorProperty = ColorProperty;
                }

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

    void ApplyGreenTint(float strength)
    {
        if (rendererColorStates == null) return;

        float clampedStrength = Mathf.Clamp01(strength);
        for (int i = 0; i < rendererColorStates.Length; i++)
        {
            RendererColorState state = rendererColorStates[i];
            if (state.renderer == null) continue;

            state.renderer.GetPropertyBlock(state.propertyBlock, state.materialIndex);

            Color color = Color.Lerp(state.baseColor, successColor, clampedStrength);
            color.a = state.baseColor.a;
            state.propertyBlock.SetColor(state.colorProperty, color);

            if (!string.IsNullOrEmpty(state.emissionProperty))
            {
                Color emissionColor = Color.Lerp(state.baseEmissionColor, successColor * 1.4f, clampedStrength);
                emissionColor.a = 1f;
                state.propertyBlock.SetColor(state.emissionProperty, emissionColor);
            }

            state.renderer.SetPropertyBlock(state.propertyBlock, state.materialIndex);
        }
    }

    void SpawnSuccessParticles()
    {
        Vector3 center = GetRendererBounds(out float radius).center;
        GameObject effectObject = new GameObject(objectName + "_YesilEfekt");
        effectObject.transform.position = center;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.65f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = successColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 28)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.radiusThickness = 0.2f;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        particles.Play();
        Destroy(effectObject, 2f);
    }

    Bounds GetRendererBounds(out float radius)
    {
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(transform.position, Vector3.one * 0.8f);
        bool hasBounds = false;

        foreach (Renderer childRenderer in childRenderers)
        {
            if (childRenderer == null) continue;

            if (!hasBounds)
            {
                bounds = childRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(childRenderer.bounds);
            }
        }

        radius = Mathf.Max(0.35f, bounds.extents.magnitude * 0.55f);
        return bounds;
    }

    void SetActiveSafe(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }

    public bool IsFixed() => isFixed;
}
