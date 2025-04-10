using UnityEngine;
using System.Collections.Generic;

public class TerrainChunk
{
    private GameObject meshObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private List<Vector2> containedCoordinates;

    /* Constructeur : Initialise un chunk de terrain et crée son mesh, sa texture et son collider */
    public TerrainChunk(Vector2 chunkIndex, int chunkSize, Transform parent, Material material, 
                          float[,] globalNoiseMap, Color[] globalColourMap, int worldWidth, 
                          WorldGenerator worldGenerator, float chunkScale, int startX, int startY)
    {
        // Construire la liste des coordonnées contenues dans ce chunk
        containedCoordinates = new List<Vector2>();
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int globalX = startX + x;
                int globalY = startY + y;
                containedCoordinates.Add(new Vector2(globalX, globalY));
            }
        }

        // Calculer la position de l'objet dans le monde (avec décalages spécifiques)
        Vector3 positionV3 = new Vector3(startX + 120, 0, -startY + 600);

        // Créer le GameObject du chunk avec un nom
        meshObject = new GameObject($"Terrain Chunk ({chunkIndex.x}, {chunkIndex.y})");
        
        // Ajouter les composants nécessaires pour le rendu et la physique
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();

        // Appliquer le matériau et configurer la hiérarchie et la transformation
        meshRenderer.material = new Material(material);
        meshObject.transform.SetParent(parent);
        meshObject.transform.position = positionV3 * chunkScale;
        meshObject.transform.localScale = Vector3.one * chunkScale;

        // Demander les données de la carte et construire le mesh et la texture du chunk
        worldGenerator.RequestMapData(new Vector2(startX, startY), (mapData) =>
        {
            // Générer la texture du chunk à partir de la carte de couleur
            Texture2D chunkTexture = TextureGenerator.TextureFromColourMap(mapData.colourMap, chunkSize, chunkSize);
            meshRenderer.sharedMaterial.mainTexture = chunkTexture;
            
            // Générer les données de mesh à partir de la carte de hauteur
            MeshData chunkMeshData = MeshGenerator.GenerateTerrainMesh(
                mapData.heightMap, 
                worldGenerator.meshHeightMultiplier, 
                worldGenerator.meshHeightCurve
            );

            // Créer le mesh et l'assigner aux composants de rendu et de collision
            Mesh mesh = chunkMeshData.CreateMesh();
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
        }, chunkSize);
    }

    /* Retourne la liste des coordonnées contenues dans ce chunk */
    public List<Vector2> GetContainedCoordinates()
    {
        return containedCoordinates;
    }

    /* Définir la visibilité du chunk en activant/désactivant l'objet et son collider */
    public void SetVisible(bool visible)
    {
        // Activer ou désactiver l'objet
        meshObject.SetActive(visible);
        
        // Activer ou désactiver le collider
        meshCollider.enabled = visible;
    }
}
