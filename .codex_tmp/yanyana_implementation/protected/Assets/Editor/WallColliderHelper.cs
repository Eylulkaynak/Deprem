using UnityEditor;
using UnityEngine;

public static class WallColliderHelper
{
    private const string ParentName = "WallColliders";

    [MenuItem("Tools/Level Tools/Make Selected Cubes Collider Only")]
    public static void MakeSelectedCubesColliderOnly()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("Wall Collider Helper: No objects selected.");
            return;
        }

        Transform parent = GetOrCreateParent();

        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject == null)
            {
                continue;
            }

            MakeColliderOnly(selectedObject, parent);
        }
    }

    private static Transform GetOrCreateParent()
    {
        GameObject parentObject = GameObject.Find(ParentName);

        if (parentObject == null)
        {
            parentObject = new GameObject(ParentName);
            Undo.RegisterCreatedObjectUndo(parentObject, "Create WallColliders Parent");
        }

        return parentObject.transform;
    }

    private static void MakeColliderOnly(GameObject targetObject, Transform parent)
    {
        Undo.RegisterFullObjectHierarchyUndo(targetObject, "Make Collider Only");

        MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Object.DestroyImmediate(meshFilter);
        }

        BoxCollider boxCollider = targetObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = Undo.AddComponent<BoxCollider>(targetObject);
        }

        boxCollider.isTrigger = false;

        if (parent != null && targetObject.transform.parent != parent)
        {
            Undo.SetTransformParent(targetObject.transform, parent, "Move To WallColliders");
        }

        targetObject.name = targetObject.name.Contains("WallCollider")
            ? targetObject.name
            : $"WallCollider_{targetObject.name}";

        EditorUtility.SetDirty(targetObject);
    }
}
