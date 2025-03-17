using System.Collections.Generic;
using UnityEngine;

public class ObjectGenerator : MonoBehaviour {

    public List<RegionObjectSettings> regionsToGenerateOn;

    private MapGenerator mapGenerator;
    private HashSet<Vector2Int> usedCoordinates = new HashSet<Vector2Int>();

    private ObjectStorage objectStorage = new ObjectStorage(); 

    public bool enableFireSpread = true;

    public List<RegionObjectSettings> GetRegionsToGenerateOn() {
        return regionsToGenerateOn;
    }

    public void Initialize() {
        mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null) {
            Debug.LogError("MapGenerator not found in the scene!");
            return;
        }

        foreach (var regionSettings in regionsToGenerateOn) {
            GenerateObjectsForRegion(regionSettings);
        }
    }

    private void GenerateObjectsForRegion(RegionObjectSettings regionSettings) {
        mapGenerator.RequestMapData(Vector2.zero, mapData => {
            GenerateObjectsOnRegion(mapData, regionSettings);
        });
    }

    private void GenerateObjectsOnRegion(MapData mapData, RegionObjectSettings regionSettings) {
        string regionName = regionSettings.regionName;
        Color targetColor = RegionUtils.GetRegionColor(regionName, mapGenerator.regions);
        if (targetColor == Color.clear) return;

        List<Vector2Int> regionCoordinates = RegionUtils.FindRegionCoordinates(mapData.colourMap, targetColor, MapGenerator.mapChunkSize);
        if (regionCoordinates == null || regionCoordinates.Count == 0) return;

        PlaceObjectsOnRegion(regionCoordinates, mapData, regionSettings.objectDensities);
    }

    private void PlaceObjectsOnRegion(List<Vector2Int> regionCoordinates, MapData mapData, List<ObjectDensity> objectDensities) {
        foreach (var coord in regionCoordinates) {
            if (usedCoordinates.Contains(coord)) continue;

            float height = mapData.heightMap[coord.x, coord.y];
            if (TryPlaceObject(coord, height, objectDensities)) {
                usedCoordinates.Add(coord);
            }
        }
    }

    private bool TryPlaceObject(Vector2Int coord, float height, List<ObjectDensity> objectDensities) {
        foreach (var objectDensity in objectDensities) {
            if (Random.Range(0, 100) < objectDensity.density) {
                Vector3 worldPosition = GetWorldPositionFromCoord(coord.x, coord.y, height);

                var (adjustedPosition, rotation) = GetPositionAndRotation(worldPosition);

                GameObject parentObject = objectStorage.GetOrCreateParentObject(objectDensity.objectPrefab);
                GameObject newObject = Instantiate(objectDensity.objectPrefab, adjustedPosition, rotation);
                newObject.transform.SetParent(parentObject.transform);

                objectStorage.AddObject(newObject, coord);

                InitializeObject(newObject);

                return true;
            }
        }

        return false;
    }

    private (Vector3 adjustedPosition, Quaternion rotation) GetPositionAndRotation(Vector3 worldPosition) {
        RaycastHit hit;
        if (Physics.Raycast(worldPosition + Vector3.up * 5f, Vector3.down, out hit, 10f)) {
            Vector3 adjustedPosition = hit.point;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            return (adjustedPosition, rotation);
        }
        return (worldPosition, Quaternion.identity);
    }

    private Vector3 GetWorldPositionFromCoord(int x, int y, float height) {
        float scale = StaticTerrainGenerator.scale;
        float worldX = (x - (MapGenerator.mapChunkSize / 2f)) * scale;
        float worldZ = ((MapGenerator.mapChunkSize / 2f) - y) * scale;
        float worldHeight = mapGenerator.meshHeightCurve.Evaluate(height) * mapGenerator.meshHeightMultiplier * scale;

        return new Vector3(worldX, worldHeight, worldZ);
    }

    private void InitializeObject(GameObject obj) {
        if (obj.TryGetComponent<Vegetation>(out Vegetation vegetation)) {
            vegetation.Initialize(objectStorage, enableFireSpread);
        }
        
        // Add more initialization logic here as needed for other types of objects
    }

    public void CleanUpGeneratedObjects() {
        objectStorage.Clear();
        usedCoordinates.Clear();
    }
}
