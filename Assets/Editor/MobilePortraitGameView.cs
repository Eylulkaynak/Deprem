using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class MobilePortraitGameView
{
    private const string SessionKey = "Deprem.MobilePortraitGameView.Configured";
    private const string PortraitLabel = "Portrait 1080x1920";
    private const int PortraitWidth = 1080;
    private const int PortraitHeight = 1920;
    private const string TallPortraitLabel = "Tall Portrait 1080x2340";
    private const int TallPortraitWidth = 1080;
    private const int TallPortraitHeight = 2340;

    private static readonly BindingFlags AllMembers =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    static MobilePortraitGameView()
    {
        EditorApplication.delayCall += ConfigureOncePerSession;
    }

    [MenuItem("Tools/Deprem/Use Portrait Game View")]
    private static void UsePortraitGameView()
    {
        ConfigureAndSelect(PortraitWidth, PortraitHeight, PortraitLabel, true);
    }

    [MenuItem("Tools/Deprem/Use Tall Portrait Game View")]
    private static void UseTallPortraitGameView()
    {
        ConfigureAndSelect(TallPortraitWidth, TallPortraitHeight, TallPortraitLabel, true);
    }

    private static void ConfigureOncePerSession()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        ConfigureAndSelect(PortraitWidth, PortraitHeight, PortraitLabel, false);
    }

    private static void ConfigureAndSelect(int width, int height, string label, bool logSuccess)
    {
        try
        {
            Assembly editorAssembly = typeof(Editor).Assembly;
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type sizeModeType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            Type groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");

            if (sizesType == null || sizeType == null || sizeModeType == null ||
                groupType == null || gameViewType == null)
            {
                throw new InvalidOperationException("Unity Game View editor types were not found.");
            }

            Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singletonType.GetProperty("instance", AllMembers)?.GetValue(null);
            object standaloneGroup = sizesType.GetMethod("GetGroup", AllMembers)?.Invoke(
                sizes,
                new[] { Enum.Parse(groupType, "Standalone") });

            if (standaloneGroup == null)
            {
                throw new InvalidOperationException("Standalone Game View size group was not found.");
            }

            int portraitIndex = FindSizeIndex(standaloneGroup, width, height);
            if (portraitIndex < 0)
            {
                object portraitSize = Activator.CreateInstance(
                    sizeType,
                    AllMembers,
                    null,
                    new[]
                    {
                        Enum.Parse(sizeModeType, "FixedResolution"),
                        width,
                        height,
                        label
                    },
                    null);

                standaloneGroup.GetType().GetMethod("AddCustomSize", AllMembers)?.Invoke(
                    standaloneGroup,
                    new[] { portraitSize });
                portraitIndex = FindSizeIndex(standaloneGroup, width, height);
            }

            if (portraitIndex < 0)
            {
                throw new InvalidOperationException("Portrait Game View size could not be created.");
            }

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            PropertyInfo selectedSize = gameViewType.GetProperty("selectedSizeIndex", AllMembers);
            if (selectedSize != null)
            {
                selectedSize.SetValue(gameView, portraitIndex);
            }
            else
            {
                MethodInfo selectionCallback = gameViewType.GetMethod("SizeSelectionCallback", AllMembers);
                selectionCallback?.Invoke(gameView, new object[] { portraitIndex, null });
            }

            gameView.Repaint();

            if (logSuccess)
            {
                Debug.Log($"Game View set to {label}.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Portrait Game View setup failed: {exception.Message}");
        }
    }

    private static int FindSizeIndex(object group, int width, int height)
    {
        Type groupInstanceType = group.GetType();
        MethodInfo getTotalCount = groupInstanceType.GetMethod("GetTotalCount", AllMembers);
        MethodInfo getGameViewSize = groupInstanceType.GetMethod("GetGameViewSize", AllMembers);

        if (getTotalCount == null || getGameViewSize == null)
        {
            return -1;
        }

        int count = (int)getTotalCount.Invoke(group, null);
        for (int index = 0; index < count; index++)
        {
            object size = getGameViewSize.Invoke(group, new object[] { index });
            if (ReadIntMember(size, "width") == width &&
                ReadIntMember(size, "height") == height)
            {
                return index;
            }
        }

        return -1;
    }

    private static int ReadIntMember(object target, string memberName)
    {
        Type type = target.GetType();
        PropertyInfo property = type.GetProperty(memberName, AllMembers);
        if (property != null)
        {
            return (int)property.GetValue(target);
        }

        FieldInfo field = type.GetField(memberName, AllMembers);
        return field != null ? (int)field.GetValue(target) : -1;
    }
}
