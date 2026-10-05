using System;
using System.Linq;
using Deprem.Minigames;
using Deprem.Story;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class MinigamePolishedSceneBuilder
{
    [MenuItem("Tools/Deprem App/Polish/Apply Scenario Journey")]
    public static void ApplyScenarioJourney()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the open scene first.");
        try
        {
            EditorSceneManager.OpenScene(MinigameSceneCatalog.HubPath);
            var manager = Object.FindFirstObjectByType<MinigameHubManager>();
            var safeArea = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => t.name == "SafeArea" && t.GetComponentInParent<Canvas>()?.name == "MinigameHubCanvas");
            StoryChapterBuilderCommon.LoadPlayfulStoryFonts(out _, out _, out TMP_FontAsset bold);
            AddScenarioJourneyButton(safeArea, manager, bold);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(MinigameSceneCatalog.Evacuation25DPath);
            var reporter = Object.FindFirstObjectByType<MinigameSessionManager>();
            AddEvacuationResultHud(reporter);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    static void AddScenarioJourneyButton(Transform safeArea, MinigameHubManager manager, TMP_FontAsset bold)
    {
        var header = safeArea.Find("HubHeader").GetComponent<RectTransform>();
        header.sizeDelta = new Vector2(980, 370);
        header.anchoredPosition = new Vector2(0, -35);
        header.Find("Eyebrow").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 134);
        header.Find("Title").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 72);
        header.Find("TotalCoin").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -12);
        var viewport = safeArea.Find("CardsViewport");
        if (viewport != null)
        {
            var rect = viewport.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1010, -645);
            rect.anchoredPosition = new Vector2(0, -112.5f);
            var cards = viewport.Find("CardsContent");
            int index = 0;
            foreach (Transform child in cards)
                if (child.name.EndsWith("_Card", StringComparison.Ordinal))
                    child.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -24 - index++ * 365);
            cards.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 386 + Mathf.Max(0, index - 1) * 365);
        }
        if (header.Find("ScenarioJourneyButton") != null) return;
        var button = StoryChapterBuilderCommon.CreateButton("ScenarioJourneyButton", header, "DEPREM SENARYOSU",
            bold, Vector2.one * .5f, new Vector2(0, -114), new Vector2(790, 92),
            StoryChapterBuilderCommon.Amber, StoryChapterBuilderCommon.Navy);
        UnityEventTools.AddPersistentListener(button.onClick, manager.StartScenarioJourney);
    }

    static void AddEvacuationResultHud(MinigameSessionManager reporter)
    {
        var serialized = new SerializedObject(reporter);
        if (serialized.FindProperty("resultPanel").objectReferenceValue != null) return;
        StoryChapterBuilderCommon.LoadPlayfulStoryFonts(out _, out TMP_FontAsset semibold, out TMP_FontAsset bold);
        var host = new GameObject("EvacuationResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 110;
        var scaler = host.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
        var safeArea = StoryChapterBuilderCommon.CreateUIRect("SafeArea", host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<Deprem.Story.StorySafeAreaPanel>();
        var result = BuildResultHud(safeArea.transform, bold, semibold, reporter);
        SetField(reporter, "resultPanel", result.root);
        SetField(reporter, "resultTitleText", result.title);
        SetField(reporter, "resultDetailText", result.detail);
        SetField(reporter, "resultStarsText", result.stars);
        SetField(reporter, "resultCoinsText", result.coins);
        EditorUtility.SetDirty(reporter);
    }

    [MenuItem("Tools/Deprem App/Polish/Apply Safe Street Tasks")]
    public static void ApplySafeStreetTasks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the open scene first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(MinigameSceneCatalog.Evacuation25DPath);
            var interactions = Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var gas = interactions.Single(i => i.InteractionId == "evac25d.street.03.gas_valve");
            var rubble = interactions.Single(i => i.InteractionId == "evac25d.street.04.rubble_check");
            SetField(gas, "prompt", "VANAYA DOKUNMA • UZAKTAN YETİŞKİNE HABER VER");
            SetField(rubble, "prompt", "MOLOZA DOKUNMA • AÇIK GEÇİŞİ GÖSTER");
            foreach (var interaction in new[] { gas, rubble })
            {
                var data = new SerializedObject(interaction);
                data.FindProperty("interactionGesture").intValue = (int)StoryInteractionGesture.Tap;
                data.FindProperty("requiredGestureCount").intValue = 1;
                data.ApplyModifiedPropertiesWithoutUndo();
                for (int index = interaction.OnInteracted.GetPersistentEventCount() - 1; index >= 0; index--)
                {
                    var target = interaction.OnInteracted.GetPersistentTarget(index);
                    if (target != null && new[] { "GasLeakVisibleHazard", "GasValve_OPEN", "GasValve_CLOSED", "LooseRubble_BLOCKED" }.Contains(target.name))
                        UnityEventTools.RemovePersistentListener(interaction.OnInteracted, index);
                }
                ConfigureStreetHazardResponse(interaction, interaction == gas ? "item-send-message" : "route");
                EditorUtility.SetDirty(interaction);
            }
            foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (label.transform.parent.name == "Stage_03")
                    label.text = label.name == "StageTitle" ? "GAZ TEHLİKESİNİ UZAKTAN BİLDİR" : "Vanaya dokunma. Tehlikeden uzak dur ve yetişkine haber ver.";
                else if (label.transform.parent.name == "Stage_04")
                    label.text = label.name == "StageTitle" ? "GEVŞEK MOLOZDAN UZAK DUR" : "Moloza dokunma. Binalardan uzak, açık geçişi göster.";
                else continue;
                EditorUtility.SetDirty(label);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    // The response target sits on the open sidewalk. The hand guide and hit test
    // point to reporting/route selection instead of touching the damaged prop.
    internal static void ConfigureStreetHazardResponse(StoryInteractable interaction, string artwork)
    {
        var root = interaction.transform;
        var point = interaction.InteractionPoint;
        point.position = root.position + new Vector3(-1.3f, 0.15f, -0.65f);
        foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        var existing = root.Find("HazardResponse");
        var target = existing != null ? existing.gameObject : new GameObject("HazardResponse", typeof(RectTransform), typeof(Canvas), typeof(RawImage), typeof(BoxCollider));
        target.transform.SetParent(root, false);
        target.transform.position = point.position;
        target.transform.localScale = Vector3.one * 0.01f;
        var rect = target.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(72, 72);
        target.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var picture = target.GetComponent<RawImage>();
        picture.texture = Resources.Load<Texture2D>("LearningApp/Art/" + artwork);
        picture.raycastTarget = false;
        picture.enabled = artwork != "route";
        if (artwork == "route")
        {
            var child = target.transform.Find("Open route picture");
            var icon = child != null ? child.GetComponent<Deprem.Accessibility.ReadingFreeIcon>() :
                Deprem.Accessibility.ReadingFree3D.NewIcon(target.transform, "Open route picture");
            icon.rectTransform.anchorMin = Vector2.zero; icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
            icon.Set("route"); icon.raycastTarget = false;
        }
        var hit = target.GetComponent<BoxCollider>(); hit.enabled = true; hit.isTrigger = true; hit.size = new Vector3(80, 80, 12);
    }
}
