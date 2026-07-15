using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class DepremUITheme : ScriptableObject
{
    public const string AssetPath = "Assets/UI/DesignSystem/DepremUITheme.asset";
    public const string RegularFontPath = "Assets/Fonts/Bolum1/Inter-Regular SDF.asset";
    public const string SemiboldFontPath = "Assets/Fonts/Bolum1/Inter-SemiBold SDF.asset";
    public const string BoldFontPath = "Assets/Fonts/Bolum1/Inter-Bold SDF.asset";
    public const string RoundedSpritePath = "Assets/Sprites/Bolum1/UI/bolum1-rounded-28.png";

    [Header("Shared Assets")]
    public TMP_FontAsset regularFont;
    public TMP_FontAsset semiboldFont;
    public TMP_FontAsset boldFont;
    public Sprite roundedSprite;

    [Header("Mobile Canvas")]
    public Vector2 referenceResolution = new Vector2(1080f, 1920f);
    [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;
    public float safeHorizontalInset = 46f;
    public float safeTopInset = 30f;
    public float safeBottomInset = 44f;

    [Header("Core Palette")]
    public Color ink = new Color(0.018f, 0.045f, 0.11f, 0.97f);
    public Color overlay = new Color(0.006f, 0.02f, 0.065f, 0.88f);
    public Color panel = new Color(0.035f, 0.11f, 0.24f, 0.88f);
    public Color panelStrong = new Color(0.025f, 0.075f, 0.18f, 0.96f);
    public Color panelSoft = new Color(0.05f, 0.19f, 0.34f, 0.74f);
    public Color primary = new Color(0.08f, 0.42f, 1f, 1f);
    public Color accent = new Color(0.26f, 0.85f, 1f, 1f);
    public Color textPrimary = new Color(0.95f, 0.98f, 1f, 1f);
    public Color textSecondary = new Color(0.66f, 0.76f, 0.86f, 1f);

    [Header("Semantic Palette")]
    public Color success = new Color(0.26f, 0.82f, 0.48f, 1f);
    public Color warning = new Color(1f, 0.74f, 0.25f, 1f);
    public Color danger = new Color(1f, 0.34f, 0.40f, 1f);

    [Header("Effects")]
    public Color glassHighlight = new Color(0.95f, 0.98f, 1f, 0.22f);
    public Color glassOutline = new Color(0.26f, 0.85f, 1f, 0.24f);
    public Color shadow = new Color(0f, 0f, 0f, 0.56f);
    public Vector2 outlineDistance = new Vector2(2f, -2f);
    public Vector2 shadowDistance = new Vector2(0f, -10f);

    [Header("Typography")]
    public float displaySize = 72f;
    public float titleSize = 42f;
    public float bodySize = 28f;
    public float labelSize = 20f;
    public float buttonSize = 34f;

    public void ResolveSharedAssets()
    {
        if (regularFont == null)
            regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontPath);
        if (semiboldFont == null)
            semiboldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SemiboldFontPath);
        if (boldFont == null)
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
        if (roundedSprite == null)
            roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
    }
}

public static class DepremUIThemeStore
{
    public static DepremUITheme LoadOrCreate()
    {
        EnsureFolder("Assets", "UI");
        EnsureFolder("Assets/UI", "DesignSystem");

        DepremUITheme theme = AssetDatabase.LoadAssetAtPath<DepremUITheme>(DepremUITheme.AssetPath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<DepremUITheme>();
            theme.name = "Deprem UI Theme";
            theme.ResolveSharedAssets();
            AssetDatabase.CreateAsset(theme, DepremUITheme.AssetPath);
            AssetDatabase.SaveAssets();
        }
        else
        {
            theme.ResolveSharedAssets();
        }

        EditorUtility.SetDirty(theme);
        return theme;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }
}
