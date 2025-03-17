using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectDensity {
    public GameObject objectPrefab;
    [Range(0, 100)] public int density;
}

[System.Serializable]
public class RegionObjectSettings {
    public string regionName;
    public List<ObjectDensity> objectDensities;
}

public static class RegionUtils {
    
    public static Color GetRegionColor(string regionName, TerrainType[] terrainTypes) {
        foreach (var terrainType in terrainTypes) {
            if (terrainType.name == regionName) {
                return terrainType.colour;
            }
        }

        Debug.LogError($"Region {regionName} not found.");
        return Color.clear;
    }

    public static List<Vector2Int> FindRegionCoordinates(Color[] colorMap, Color targetColor, int mapChunkSize) {
        List<Vector2Int> regionCoordinates = new List<Vector2Int>();

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                if (colorMap[y * mapChunkSize + x] == targetColor) {
                    regionCoordinates.Add(new Vector2Int(x, y));
                }
            }
        }

        if (regionCoordinates.Count == 0) {
            Debug.LogError($"No coordinates found for region with target color.");
        }

        return regionCoordinates;
    }
}
