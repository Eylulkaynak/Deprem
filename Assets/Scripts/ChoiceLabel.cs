using TMPro;
using UnityEngine;

public class ChoiceLabel : MonoBehaviour
{
    public string labelText = "Choice";
    public Vector3 localOffset = new Vector3(0f, 1.1f, 0f);
    public float fontSize = 3f;
    public Color textColor = Color.white;
    public TextMeshPro textMesh;

    private void Awake()
    {
        EnsureLabel();
        ApplyLabelSettings();
    }

    private void OnValidate()
    {
        EnsureLabel();
        ApplyLabelSettings();
    }

    [ContextMenu("Refresh Label")]
    public void RefreshLabel()
    {
        EnsureLabel();
        ApplyLabelSettings();
    }

    private void EnsureLabel()
    {
        if (textMesh != null)
        {
            return;
        }

        Transform existingLabel = transform.Find("ChoiceLabel");
        if (existingLabel != null)
        {
            textMesh = existingLabel.GetComponent<TextMeshPro>();
        }

        if (textMesh == null)
        {
            GameObject labelObject = new GameObject("ChoiceLabel");
            labelObject.transform.SetParent(transform, false);
            textMesh = labelObject.AddComponent<TextMeshPro>();
            labelObject.AddComponent<BillboardToCamera>();
        }
    }

    private void ApplyLabelSettings()
    {
        if (textMesh == null)
        {
            return;
        }

        textMesh.transform.localPosition = localOffset;
        textMesh.text = labelText;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.rectTransform.sizeDelta = new Vector2(5f, 1f);

        BillboardToCamera billboard = textMesh.GetComponent<BillboardToCamera>();
        if (billboard == null)
        {
            textMesh.gameObject.AddComponent<BillboardToCamera>();
        }
    }
}
