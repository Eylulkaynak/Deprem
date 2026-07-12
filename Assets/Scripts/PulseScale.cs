using UnityEngine;

public class PulseScale : MonoBehaviour
{
    public float minScale = 0.85f;
    public float maxScale = 1.15f;
    public float pulseSpeed = 2f;

    private Vector3 startScale;

    private void Awake()
    {
        startScale = transform.localScale;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = startScale * scale;
    }
}
