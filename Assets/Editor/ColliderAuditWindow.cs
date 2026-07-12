using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ColliderAuditWindow : EditorWindow
{
    private readonly List<GameObject> objectsMissingColliders = new List<GameObject>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Physics/Collider Audit")]
    public static void Open()
    {
        GetWindow<ColliderAuditWindow>("Collider Audit");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("MeshRenderer Objects Without Colliders", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool only lists objects with MeshRenderer but no Collider on the same GameObject. It will not automatically add colliders to the whole scene.",
            MessageType.Info);

        if (GUILayout.Button("Scan Open Scene"))
        {
            ScanOpenScene();
        }

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Add BoxCollider To Selected Objects"))
            {
                AddBoxCollidersToSelection();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Found: {objectsMissingColliders.Count}");

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (GameObject sceneObject in objectsMissingColliders)
        {
            if (sceneObject == null)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(sceneObject, typeof(GameObject), true);

            if (GUILayout.Button("Select", GUILayout.Width(70f)))
            {
                Selection.activeGameObject = sceneObject;
                EditorGUIUtility.PingObject(sceneObject);
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void ScanOpenScene()
    {
        objectsMissingColliders.Clear();

        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>(true);
        foreach (MeshRenderer meshRenderer in renderers)
        {
            GameObject sceneObject = meshRenderer.gameObject;

            if (!sceneObject.scene.IsValid() ||
                sceneObject.GetComponent<Collider>() != null)
            {
                continue;
            }

            objectsMissingColliders.Add(sceneObject);
        }
    }

    private void AddBoxCollidersToSelection()
    {
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            if (selectedObject == null ||
                selectedObject.GetComponent<Collider>() != null ||
                selectedObject.GetComponent<MeshRenderer>() == null)
            {
                continue;
            }

            Undo.AddComponent<BoxCollider>(selectedObject);
            EditorUtility.SetDirty(selectedObject);
        }

        ScanOpenScene();
    }
}
