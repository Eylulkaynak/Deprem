using UnityEditor;
using UnityEngine;

public static class SnapSelectedObjectsToTerrain
{
    [MenuItem("Tools/Level Tools/Snap Selected Objects To Terrain")]
    public static void SnapSelection()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("Snap Selected Objects To Terrain: No objects selected.");
            return;
        }

        foreach (GameObject selectedObject in selectedObjects)
        {
            SnapObject(selectedObject);
        }
    }

    private static void SnapObject(GameObject selectedObject)
    {
        if (selectedObject == null)
        {
            return;
        }

        if (!TryGetRendererBounds(selectedObject, out Bounds rendererBounds))
        {
            Debug.LogWarning($"Snap Selected Objects To Terrain: '{selectedObject.name}' has no Renderer in itself or its children.");
            return;
        }

        Vector3 currentPosition = selectedObject.transform.position;
        float targetGroundY = GetGroundY(currentPosition);
        float yOffset = targetGroundY - rendererBounds.min.y;

        Undo.RecordObject(selectedObject.transform, "Snap Object To Terrain");
        selectedObject.transform.position = new Vector3(
            currentPosition.x,
            currentPosition.y + yOffset,
            currentPosition.z);

        EditorUtility.SetDirty(selectedObject.transform);
    }

    private static bool TryGetRendererBounds(GameObject rootObject, out Bounds combinedBounds)
    {
        combinedBounds = new Bounds();

        if (rootObject == null)
        {
            return false;
        }

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
                continue;
            }

            combinedBounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private static float GetGroundY(Vector3 worldPosition)
    {
        Terrain terrain = FindTerrainAtPosition(worldPosition);

        if (terrain == null)
        {
            return 0f;
        }

        return terrain.transform.position.y + terrain.SampleHeight(worldPosition);
    }

    private static Terrain FindTerrainAtPosition(Vector3 worldPosition)
    {
        Terrain[] terrains = Terrain.activeTerrains;

        if (terrains == null || terrains.Length == 0)
        {
            return null;
        }

        foreach (Terrain terrain in terrains)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            bool containsX = worldPosition.x >= terrainPosition.x &&
                worldPosition.x <= terrainPosition.x + terrainSize.x;
            bool containsZ = worldPosition.z >= terrainPosition.z &&
                worldPosition.z <= terrainPosition.z + terrainSize.z;

            if (containsX && containsZ)
            {
                return terrain;
            }
        }

        return terrains[0];
    }
}
