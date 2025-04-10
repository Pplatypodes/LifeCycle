using UnityEngine;

public static class MeshGenerator
{
    /* Surcharge utilisant la résolution complète de la height map */
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve)
    {
        int size = heightMap.GetLength(0);
        return GenerateTerrainMesh(heightMap, heightMultiplier, heightCurve, 0, 0, size, size);
    }

    /* Génère le mesh du terrain en se basant sur la height map et des informations globales d'offset */
    public static MeshData GenerateTerrainMesh(
        float[,] heightMap, 
        float heightMultiplier, 
        AnimationCurve _heightCurve, 
        int startX, int startY, 
        int worldWidth, int worldHeight)
    {
        // Copie des clés de la courbe pour une évaluation localisée
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        int meshSize = heightMap.GetLength(0) - 1; // Nombre de cellules par ligne
        float topLeftX = (meshSize - 1) / -2f;
        float topLeftZ = (meshSize - 1) / 2f;

        MeshData meshData = new MeshData(meshSize + 1);

        // Parcourt chaque vertex pour définir sa position, son UV et construire les triangles
        for (int y = 0; y <= meshSize; y++)
        {
            for (int x = 0; x <= meshSize; x++)
            {
                int vertexIndex = y * (meshSize + 1) + x;
                Vector2 localPercent = new Vector2(x / (float)meshSize, y / (float)meshSize);
                int globalX = startX + x;
                int globalY = startY + y;
                Vector2 globalUV = new Vector2(
                    globalX / (float)(worldWidth - 1),
                    globalY / (float)(worldHeight - 1)
                );

                float vertexHeight = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(
                    topLeftX + localPercent.x * meshSize,
                    vertexHeight,
                    topLeftZ - localPercent.y * meshSize
                );

                meshData.AddVertex(vertexPosition, globalUV, vertexIndex);

                // Crée les deux triangles pour la cellule actuelle
                if (x < meshSize && y < meshSize)
                {
                    int a = vertexIndex;
                    int b = vertexIndex + 1;
                    int c = vertexIndex + (meshSize + 1);
                    int d = vertexIndex + (meshSize + 2);

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
            }
        }

        return meshData;
    }
}
