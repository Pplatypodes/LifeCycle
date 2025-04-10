using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

public static class WorldGeneratorUtility
{
    /* Nettoie tous les enfants de la transformation donnée */
    public static void CleanUp(Transform parent)
    {
        while (parent.childCount > 0)
        {
#if UNITY_EDITOR
            // dans l'éditeur
            GameObject.DestroyImmediate(parent.GetChild(0).gameObject);
#else
            // en mode runtime
            GameObject.Destroy(parent.GetChild(0).gameObject);
#endif
        }
    }

    /* Applique un 'falloff' au bruit de la carte */
    public static void ApplyFalloff(float[,] noiseMap, int width, int height)
    {
        // Parcourt chaque cellule de la carte
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Normalise les coordonnées entre -1 et 1
                float normX = x / (float)(width - 1) * 2f - 1f;
                float normY = y / (float)(height - 1) * 2f - 1f;

                // Détermine la distance maximale par rapport au centre
                float value = Mathf.Max(Mathf.Abs(normX), Mathf.Abs(normY));

                // Évalue la force du 'falloff' à partir de la distance
                float falloff = FalloffGenerator.Evaluate(value);

                // Applique le 'falloff' et limite la valeur entre 0 et 1
                noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloff);
            }
        }
    }

    /* Génère une carte de couleurs basée sur les hauteurs de la carte de bruit et des régions définies */
    public static Color[] GenerateColourMap(float[,] noiseMap, int width, int height, TerrainType[] regions)
    {
        // Initialise le tableau des couleurs
        Color[] colourMap = new Color[width * height];

        // Parcourt chaque cellule de la carte
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Récupère la hauteur actuelle dans la carte
                float currentHeight = noiseMap[x, y];

                // Compare la hauteur aux seuils définis pour chaque région
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                        // Affecte la couleur correspondante
                        colourMap[y * width + x] = regions[i].colour;
                    else
                        break;
                }
            }
        }
        return colourMap;
    }

    /* Crée et configure tous les chunks de terrain en fonction du générateur */
    public static void CreateAllChunks(WorldGenerator generator)
    {
        int chunkSize = WorldGenerator.mapChunkSize;

        // Itère sur le nombre de chunks défini par le générateur
        for (int yChunk = 0; yChunk < generator.chunkCountY; yChunk++)
        {
            for (int xChunk = 0; xChunk < generator.chunkCountX; xChunk++)
            {
                // Calcule l'indice du chunk
                Vector2 chunkIndex = new Vector2(xChunk, yChunk);

                // Crée une nouvelle instance de TerrainChunk
                TerrainChunk chunk = new TerrainChunk(
                    chunkIndex,
                    chunkSize,
                    generator.transform,
                    generator.mapMaterial,
                    generator.GlobalNoiseMap,
                    generator.GlobalColourMap,
                    generator.WorldWidth,
                    generator,
                    generator.chunkScale,
                    xChunk * (chunkSize - 1),
                    yChunk * (chunkSize - 1)
                );
                
                chunk.SetVisible(true);

                // Ajoute le chunk au dictionnaire s'il n'existe pas déjà
                if (!generator.chunkDictionary.ContainsKey(chunkIndex))
                {
                    generator.chunkDictionary.Add(chunkIndex, chunk);
                }
            }
        }
    }
}
