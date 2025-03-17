using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StaticTerrainGenerator : MonoBehaviour {

    public const float scale = 2.5f;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    static MapGenerator mapGenerator;
    int chunkSize;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    void Start() {
        mapGenerator = FindFirstObjectByType<MapGenerator>();
        
        chunkSize = MapGenerator.mapChunkSize - 1;

        GenerateSingleChunk();

        InitializeObjectGenerator();
    }

    void GenerateSingleChunk() {
        Vector2 chunkCoord = Vector2.zero; 
        
        if (!terrainChunkDictionary.ContainsKey(chunkCoord)) {
            TerrainChunk newChunk = new TerrainChunk(chunkCoord, chunkSize, transform, mapMaterial, mapGenerator);
            terrainChunkDictionary.Add(chunkCoord, newChunk);
            newChunk.SetVisible(true);
        }
    }

    void InitializeObjectGenerator()
    {
        ObjectGenerator objectGenerator = FindFirstObjectByType<ObjectGenerator>();
        objectGenerator.Initialize();
    }

    void OnDestroy()
    {
        ObjectGenerator objectGenerator = FindFirstObjectByType<ObjectGenerator>();
        if(objectGenerator != null)
        {
            objectGenerator.CleanUpGeneratedObjects();
        }
    }
}
