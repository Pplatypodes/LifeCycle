using System.Collections.Generic;
using UnityEngine;

public class ObjectStorage {
    private Dictionary<Vector2Int, GameObject> objectCoordinates = new Dictionary<Vector2Int, GameObject>();

    public GameObject GetOrCreateParentObject(GameObject prefab) {
        string parentName = $"{prefab.name}_Parent";
        GameObject parent = GameObject.Find(parentName);

        if (parent == null) {
            parent = new GameObject(parentName);
        }

        return parent;
    }

    public void AddObject(GameObject obj, Vector2Int coord) {
        objectCoordinates[coord] = obj;
    }

    public void RemoveObject(GameObject obj) {
        Vector2Int? coordToRemove = null;

        foreach (var kvp in objectCoordinates) {
            if (kvp.Value == obj) {
                coordToRemove = kvp.Key;
                break;
            }
        }

        if (coordToRemove.HasValue) {
            objectCoordinates.Remove(coordToRemove.Value);
        }
    }

    public Vector2Int? GetCoordinates(GameObject obj) {
        foreach (var kvp in objectCoordinates) {
            if (kvp.Value == obj) {
                return kvp.Key;
            }
        }
        return null;
    }

    public List<GameObject> GetObjectsInRadius(Vector2Int center, int radius) {
        List<GameObject> nearbyObjects = new List<GameObject>();

        for (int x = -radius; x <= radius; x++) {
            for (int y = -radius; y <= radius; y++) {
                Vector2Int neighborCoord = new Vector2Int(center.x + x, center.y + y);

                if (objectCoordinates.TryGetValue(neighborCoord, out GameObject neighbor)) {
                    nearbyObjects.Add(neighbor);
                }
            }
        }

        return nearbyObjects;
    }

    public void HighlightObjectsInRadius(Vector2Int center, int radius, Material highlightMaterial, float highlightDuration) {
        List<GameObject> nearbyObjects = GetObjectsInRadius(center, radius);

        foreach (var obj in nearbyObjects) {
            foreach (Transform child in obj.transform) {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (renderer != null) {
   
                    Material originalMaterial = renderer.material;

                    renderer.material = highlightMaterial;

                    obj.GetComponent<MonoBehaviour>().StartCoroutine(ResetHighlight(renderer, originalMaterial, highlightDuration));
                }
            }
        }
    }

    private System.Collections.IEnumerator ResetHighlight(MeshRenderer renderer, Material originalMaterial, float delay) {
        yield return new WaitForSeconds(delay);
        renderer.material = originalMaterial;
    }

    public void Clear() {
        objectCoordinates.Clear();
    }
}
