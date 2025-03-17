using UnityEngine;

public class TerrainChunk {

    GameObject meshObject;
    Vector2 position;
    Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    MapData mapData;
    bool mapDataReceived;

    MapGenerator mapGenerator;

    public TerrainChunk(Vector2 coord, int size, Transform parent, Material material, MapGenerator mapGenerator) {
        this.mapGenerator = mapGenerator;

        position = coord * size;
        bounds = new Bounds(position, Vector2.one * size);
        Vector3 positionV3 = new Vector3(position.x, 0, position.y);

        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = positionV3 * StaticTerrainGenerator.scale;
        meshObject.transform.parent = parent;
        meshObject.transform.localScale = Vector3.one * StaticTerrainGenerator.scale;
        SetVisible(false);

        mapGenerator.RequestMapData(position, OnMapDataReceived);
    }

    void OnMapDataReceived(MapData mapData) {
        this.mapData = mapData;
        mapDataReceived = true;

        Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
        meshRenderer.material.mainTexture = texture;

        UpdateTerrainChunk();
    }

    public void UpdateTerrainChunk() {
        if (mapDataReceived) {
            mapGenerator.RequestMeshData(mapData, (meshData) => {
                meshFilter.mesh = meshData.CreateMesh();
                meshCollider.sharedMesh = meshData.CreateMesh();
            });
        }
    }


    public void SetVisible(bool visible) {
        meshObject.SetActive(visible);
        meshCollider.enabled = visible;
    }
}
