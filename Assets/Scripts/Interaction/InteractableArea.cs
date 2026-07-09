using UnityEngine;

public class InteractableArea : MonoBehaviour
{
    public enum AreaType
    {
        Safe,
        Unsafe
    }

    [Header("Area")]
    public AreaType areaType;

    [Header("Safe Area")]
    public Transform targetPoint;

    [Header("Unsafe Area")]
    [TextArea]
    public string wrongFeedbackMessage = "Bu güvenli bir alan değil!";

    [Header("Bubble")]
    public GameObject bubble;

    public void ShowBubble()
    {
        if (bubble != null)
            bubble.SetActive(true);
    }

    public void HideBubble()
    {
        if (bubble != null)
            bubble.SetActive(false);
    }
}