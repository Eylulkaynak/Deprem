using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  HazardMover.cs
//  1) Basta KIRMIZI yanip soner.
//  2) Tiklayinca safeTarget'a dogru yavasca gider.
//  3) Varinca YESIL yanar.
//  (WobbleFixer'daki calisan renk sistemi kullanildi.)
// ============================================================
public class HazardMover : MonoBehaviour
{
    [Header("Hedef")]
    public Transform safeTarget;

    [Header("Hiz")]
    public float moveDuration = 1.5f;
    public bool alignRotationToTarget = true;
    public bool preserveStartHeight = true;

    [Header("Kirmizi (tehlike)")]
    public Color dangerColor = new Color(1f, 0.15f, 0.1f, 1f);
    public float blinkSpeed = 2.5f;
    [Range(0f, 1f)] public float dangerTintMin = 0.1f;
    [Range(0f, 1f)] public float dangerTintMax = 0.55f;

    [Header("Yesil (guvenli)")]
    public Color successColor = new Color(0.15f, 1f, 0.35f, 1f);
    [Range(0f, 1f)] public float successTintStrength = 0.55f;
    [Range(0f, 1f)] public float settledTintStrength = 0.18f;
    public bool keepSoftGreenTint = true;
    public float successEffectDuration = 0.9f;

    private bool isMoved = false;
    private bool isMoving = false;
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
        CacheRendererColors();
    }

    void Update()
    {
        if (isMoved || isMoving) return;

        // KIRMIZI yanip sonme
        float pulse = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
        ApplyTint(dangerColor, Mathf.Lerp(dangerTintMin, dangerTintMax, pulse));
    }

    public void OnTapped()
    {
        if (isMoved || isMoving) return;
        if (safeTarget == null)
        {
            Debug.LogWarning(gameObject.name + ": Safe Target bos!");
            return;
        }
        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        isMoving = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = safeTarget.position;
        if (preserveStartHeight)
        {
            targetPos.y = startPos.y;
        }

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = alignRotationToTarget ? safeTarget.rotation : startRot;

        float t = 0f;
        float dur = Mathf.Max(0.05f, moveDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            k = k * k * (3f - 2f * k);
            transform.position = Vector3.Lerp(startPos, targetPos, k);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }
        transform.position = targetPos;
        transform.rotation = targetRot;

        isMoving = false;
        isMoved = true;
        if (LevelManager.Instance != null) LevelManager.Instance.HazardFixed(gameObject);
        // YESIL efekt (WobbleFixer'daki gibi)
        StartCoroutine(PlaySuccessEffect());
    }

    IEnumerator PlaySuccessEffect()
    {
        float duration = Mathf.Max(0.05f, successEffectDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            ApplyTint(successColor, successTintStrength * pulse);
            yield return null;
        }
        ApplyTint(successColor, keepSoftGreenTint ? settledTintStrength : 0f);
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

        float clampedStrength = Mathf.Clamp01(strength);
        for (int i = 0; i < rendererColorStates.Length; i++)
        {
            RendererColorState state = rendererColorStates[i];
            if (state.renderer == null) continue;

            state.renderer.GetPropertyBlock(state.propertyBlock, state.materialIndex);

            Color color = Color.Lerp(state.baseColor, tint, clampedStrength);
            color.a = state.baseColor.a;
            state.propertyBlock.SetColor(state.colorProperty, color);

            if (!string.IsNullOrEmpty(state.emissionProperty))
            {
                Color emissionColor = Color.Lerp(state.baseEmissionColor, tint * 1.4f, clampedStrength);
                emissionColor.a = 1f;
                state.propertyBlock.SetColor(state.emissionProperty, emissionColor);
            }

            state.renderer.SetPropertyBlock(state.propertyBlock, state.materialIndex);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (safeTarget == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, safeTarget.position);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(safeTarget.position, 0.3f);
    }

    public bool IsMoved() => isMoved;
}
