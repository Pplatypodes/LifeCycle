using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

[ExecuteAlways]
public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]

    [HideInInspector]
    public int chunkCountX = 3;

    [HideInInspector]
    public int chunkCountY = 3;
    public float noiseScale = 50f;
    public int seed = 12345;
    public int octaves = 4;
    [Range(0, 1)] public float persistance = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset;
    public bool useFalloff = true;

    [Header("Mesh Settings")]
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    [Header("Falloff Settings")]
    [Range(1, 10)] public float falloffA = 3f;
    [Range(1, 10)] public float falloffB = 2.2f;

    [Header("Colour Settings")]
    public TerrainType[] regions;

    [Header("Noise Settings")]
    public Noise.NormalizeMode normalizeMode;

    [Header("Display Settings")]
    public const int mapChunkSize = 241;
    public Material mapMaterial;
    public enum MapType { HeightMap, ColourMap, Mesh, FalloffMap }
    public MapType mapToShow = MapType.HeightMap;
    public Renderer displayTextureRenderer;
    public MeshFilter displayMeshFilter;
    public MeshRenderer displayMeshRenderer;
    public float chunkScale = 1f;

    // État interne
    private int worldWidth, worldHeight;
    private float[,] globalNoiseMap;
    private Color[] globalColourMap;
    private float[,] falloffMap;

    public float[,] GlobalNoiseMap => globalNoiseMap;
    public Color[] GlobalColourMap => globalColourMap;
    public int WorldWidth => worldWidth;
    public int WorldHeight => worldHeight;

    [HideInInspector]
    public Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    /* Méthode Awake : Génère le monde automatiquement en mode de lecture */
    private void Awake()
    {
        if (Application.isPlaying)
            GenerateWorld();
    }

    /* Génère le monde en créant les cartes de bruit, couleurs et chunks */
    public void GenerateWorld(Action onComplete = null)
    {
        // Nettoyer les anciennes générations
        WorldGeneratorUtility.CleanUp(transform);

        int chunkSize = mapChunkSize;
        worldWidth = (chunkCountX - 1) * (chunkSize - 1) + chunkSize;
        worldHeight = (chunkCountY - 1) * (chunkSize - 1) + chunkSize;

        // Définir les paramètres pour le falloff
        FalloffGenerator.falloffA = falloffA;
        FalloffGenerator.falloffB = falloffB;

        // Générer la carte de falloff et la carte de bruit globale
        falloffMap = FalloffGenerator.GenerateFalloffMap(worldWidth);
        globalNoiseMap = Noise.GenerateNoiseMap(worldWidth, worldHeight, seed, noiseScale, octaves, persistance, lacunarity, offset, normalizeMode);

        if (useFalloff)
            // Appliquer le falloff sur la carte de bruit
            WorldGeneratorUtility.ApplyFalloff(globalNoiseMap, worldWidth, worldHeight);

        // Générer la carte de couleurs globale en se basant sur les régions
        globalColourMap = WorldGeneratorUtility.GenerateColourMap(globalNoiseMap, worldWidth, worldHeight, regions);

        // Créer tous les chunks de terrain
        WorldGeneratorUtility.CreateAllChunks(this);

        // Appeler la fonction de complétion si fournie
        onComplete?.Invoke();
    }

    /* Affiche la carte principale (HeightMap, ColourMap, Mesh ou FalloffMap) */
    public void DisplayMainMap()
    {
        if (globalNoiseMap == null || globalColourMap == null)
        {
            // Erreur si les cartes ne sont pas générées
            Debug.LogError("Global maps not generated. Generate world first.");
            return;
        }

        Texture2D texture = null;
        if (mapToShow == MapType.HeightMap)
        {
            // Créer la texture à partir de la height map
            texture = TextureGenerator.TextureFromHeightMap(globalNoiseMap);
        }
        else if (mapToShow == MapType.ColourMap)
        {
            // Créer la texture à partir de la colour map
            texture = TextureGenerator.TextureFromColourMap(globalColourMap, worldWidth, worldHeight);
        }
        else if (mapToShow == MapType.FalloffMap)
        {
            // Utiliser la carte de falloff pré-calculée
            texture = TextureGenerator.TextureFromHeightMap(falloffMap);
        }

        if (texture != null && displayTextureRenderer != null)
        {
            // Créer une miniature de la texture pour l'affichage
            Texture2D thumb = TextureGenerator.CreateThumbnail(texture, texture.width / 4, texture.height / 4);
            displayTextureRenderer.sharedMaterial.mainTexture = thumb;
        }

        if (mapToShow == MapType.Mesh && displayMeshFilter != null && displayMeshRenderer != null)
        {
            // Générer le mesh à partir de la carte de bruit globale
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(
                globalNoiseMap,
                meshHeightMultiplier,
                meshHeightCurve,
                0, 0,
                worldWidth,
                worldHeight
            );
            Mesh previewMesh = meshData.CreateMesh();
            displayMeshFilter.mesh = previewMesh;

            // Appliquer la texture couleur sur le mesh
            Texture2D colorTex = TextureGenerator.TextureFromColourMap(globalColourMap, worldWidth, worldHeight);
            displayMeshRenderer.sharedMaterial.mainTexture = colorTex;
        }
    }

    /* Efface les textures/meshes affichés de la vue principale */
    public void ClearMainMap()
    {
        if (displayTextureRenderer != null)
        {
            // Supprimer la texture affichée
            displayTextureRenderer.sharedMaterial.mainTexture = null;
        }

        if (displayMeshFilter != null)
        {
            // Supprimer le mesh affiché
            displayMeshFilter.mesh = null;
        }
    }

    /* Découpe et retourne les données de la carte pour un chunk donné */
    public void RequestMapData(Vector2 centre, Action<MapData> callback, int chunkSize)
    {
        float[,] chunkNoiseMap = new float[chunkSize, chunkSize];
        Color[] chunkColourMap = new Color[chunkSize * chunkSize];

        int startX = (int)centre.x;
        int startY = (int)centre.y;

        // Copier la partie correspondante des cartes globales dans le chunk
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int globalX = startX + x;
                int globalY = startY + y;
                chunkNoiseMap[x, y] = globalNoiseMap[globalX, globalY];
                chunkColourMap[y * chunkSize + x] = globalColourMap[globalY * worldWidth + globalX];
            }
        }

        // Créer les données de carte pour le chunk et appeler le callback
        MapData chunkMapData = new MapData(chunkNoiseMap, chunkColourMap);
        callback(chunkMapData);
    }

    /* Retourne le dictionnaire de tous les chunks créés */
    public Dictionary<Vector2, TerrainChunk> GetChunks()
    {
        return chunkDictionary;
    }

    /* Retourne les coordonnées des régions correspondant au nom donné */
    public List<Vector2> GetRegionCoordinatesFromChunks(string regionName)
    {
        Color regionColor = RegionUtils.GetRegionColor(regionName, regions);
        if (regionColor == Color.clear)
        {
            // Affiche une erreur si la région n'est pas trouvée
            Debug.LogError($"Region {regionName} not found.");
            return null;
        }

        List<Vector2> allRegionCoordinates = new List<Vector2>();

        // Parcourir tous les chunks pour trouver les coordonnées correspondantes
        foreach (var chunk in chunkDictionary.Values)
        {
            List<Vector2> chunkCoordinates = chunk.GetContainedCoordinates();
            foreach (var coord in chunkCoordinates)
            {
                Vector2Int roundedCoord = new Vector2Int(Mathf.RoundToInt(coord.x), Mathf.RoundToInt(coord.y));
                if (roundedCoord.x < 0 || roundedCoord.y < 0 || roundedCoord.x >= worldWidth || roundedCoord.y >= worldHeight)
                {
                    // Cas coordonnées hors limites
                    Debug.LogWarning($"Coordinate out of bounds: {roundedCoord}");
                    continue;
                }

                int index = roundedCoord.y * worldWidth + roundedCoord.x;
                
                // Vérifier si la couleur du pixel correspond à la couleur de région cible
                if (globalColourMap[index] == regionColor)
                {
                    allRegionCoordinates.Add(new Vector2(roundedCoord.x, roundedCoord.y));
                }
            }
        }

        return allRegionCoordinates;
    }
}
