using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CameraAndAreaSetupFixer
{
    private static readonly string[] AreaRootNames =
    {
        "HouseAll",
        "StairArea",
        "StairsB\u00f6l\u00fcm\u00fc",
        "SafeArea"
    };

    [MenuItem("Tools/Level Tools/Fix Camera And Area Setup")]
    public static void FixSetup()
    {
        Transform player = FindSceneTransform("Boy0");
        Camera mainCamera = Camera.main;

        if (player == null)
        {
            Debug.LogWarning("CameraAndAreaSetupFixer: Boy0 was not found.");
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("CameraAndAreaSetupFixer: Main Camera was not found.");
        }
        else
        {
            FixCamera(mainCamera, player);
        }

        FixAreaVisibility();
        ProtectImportantRoots(player, mainCamera != null ? mainCamera.transform : null);
        InspectSafeAreaScale();

        if (mainCamera != null)
        {
            EditorUtility.SetDirty(mainCamera);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("CameraAndAreaSetupFixer: Camera and area setup check completed.");
    }

    private static void FixCamera(Camera mainCamera, Transform player)
    {
        IsometricCameraFollow follow = mainCamera.GetComponent<IsometricCameraFollow>();
        if (follow == null)
        {
            follow = Undo.AddComponent<IsometricCameraFollow>(mainCamera.gameObject);
        }

        SerializedObject serializedFollow = new SerializedObject(follow);
        SetSerializedObjectReference(serializedFollow, "target", player);
        SetSerializedFloat(serializedFollow, "distance", 5f);
        SetSerializedFloat(serializedFollow, "height", 5f);
        SetSerializedFloat(serializedFollow, "yawAngle", 120f);
        SetSerializedFloat(serializedFollow, "pitchAngle", 0f);
        SetSerializedVector3(serializedFollow, "focusOffset", new Vector3(0f, 1f, 0f));
        SetSerializedFloat(serializedFollow, "smoothSpeed", 6f);
        serializedFollow.ApplyModifiedProperties();

        if (player != null)
        {
            follow.SnapToTarget();
        }

        Debug.Log("CameraAndAreaSetupFixer: Main Camera IsometricCameraFollow set to Boy0, distance 5, height 5, yaw 120.");
    }

    private static void FixAreaVisibility()
    {
        Transform houseAll = FindSceneTransform("HouseAll");
        Transform safeArea = FindSceneTransform("SafeArea");
        Transform stairArea = FindSceneTransform("StairArea");
        Transform stairsBolumu = FindSceneTransform("StairsB\u00f6l\u00fcm\u00fc");

        SetActiveWithUndo(houseAll, true);
        SetActiveWithUndo(stairArea, false);
        SetActiveWithUndo(stairsBolumu, false);
        SetActiveWithUndo(safeArea, false);
    }

    private static void ProtectImportantRoots(Transform player, Transform cameraTransform)
    {
        Transform[] protectedTransforms =
        {
            player,
            cameraTransform,
            FindSceneTransform("Canvas"),
            FindComponentTransform<CardGameManager>(),
            FindComponentTransform<DoorMissionManager>(),
            FindComponentTransform<StairChoiceManager>()
        };

        foreach (Transform areaRoot in GetAreaRoots())
        {
            if (areaRoot == null)
            {
                continue;
            }

            foreach (Transform protectedTransform in protectedTransforms)
            {
                if (protectedTransform == null ||
                    protectedTransform == areaRoot ||
                    !protectedTransform.IsChildOf(areaRoot))
                {
                    continue;
                }

                Undo.SetTransformParent(protectedTransform, null, "Detach Persistent Object From Area Root");
                Debug.LogWarning($"CameraAndAreaSetupFixer: '{protectedTransform.name}' was under '{areaRoot.name}' and was moved to scene root so area hiding will not disable it.");
            }
        }
    }

    private static void InspectSafeAreaScale()
    {
        Transform safeArea = FindSceneTransform("SafeArea");
        if (safeArea == null)
        {
            Debug.LogWarning("CameraAndAreaSetupFixer: SafeArea was not found.");
            return;
        }

        if (!TryGetRendererBounds(safeArea.gameObject, out Bounds bounds))
        {
            Debug.LogWarning("CameraAndAreaSetupFixer: SafeArea has no Renderers to inspect scale/bounds.");
            return;
        }

        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        Debug.Log($"CameraAndAreaSetupFixer: SafeArea renderer bounds size is {bounds.size}. Max dimension: {maxDimension:0.00}");

        if (maxDimension > 500f)
        {
            Debug.LogWarning("CameraAndAreaSetupFixer: SafeArea looks extremely large. Try scaling the imported field asset under SafeArea to around 0.01 - 0.05, then use the snap/placement tools again.");
        }
        else if (maxDimension > 120f)
        {
            Debug.LogWarning("CameraAndAreaSetupFixer: SafeArea looks large for a character-scale scene. Try scaling the field asset under SafeArea to around 0.1 - 0.25 if the camera still feels too far.");
        }
    }

    private static bool TryGetRendererBounds(GameObject rootObject, out Bounds combinedBounds)
    {
        combinedBounds = new Bounds();
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static Transform[] GetAreaRoots()
    {
        Transform[] roots = new Transform[AreaRootNames.Length];
        for (int i = 0; i < AreaRootNames.Length; i++)
        {
            roots[i] = FindSceneTransform(AreaRootNames[i]);
        }

        return roots;
    }

    private static void SetActiveWithUndo(Transform target, bool isActive)
    {
        if (target == null)
        {
            return;
        }

        Undo.RecordObject(target.gameObject, "Set Area Active State");
        target.gameObject.SetActive(isActive);
        EditorUtility.SetDirty(target.gameObject);
    }

    private static Transform FindSceneTransform(string objectName)
    {
        foreach (GameObject sceneObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (sceneObject.name == objectName &&
                sceneObject.scene.IsValid() &&
                sceneObject.hideFlags == HideFlags.None)
            {
                return sceneObject.transform;
            }
        }

        return null;
    }

    private static Transform FindComponentTransform<T>() where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component != null &&
                component.gameObject.scene.IsValid() &&
                component.hideFlags == HideFlags.None)
            {
                return component.transform;
            }
        }

        return null;
    }

    private static void SetSerializedObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetSerializedFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetSerializedVector3(SerializedObject serializedObject, string propertyName, Vector3 value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.vector3Value = value;
        }
    }
}
