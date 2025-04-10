using UnityEngine;
using System;

public static class Noise
{
    public enum NormalizeMode { Local, Global };

    /* Génère une carte de bruit en utilisant le bruit de Perlin et divers paramètres */
    public static float[,] GenerateNoiseMap(
        int mapWidth, int mapHeight,
        int seed, float scale,
        int octaves, float persistance,
        float lacunarity, Vector2 offset,
        NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Initialiser le générateur de nombres aléatoires avec la graine donnée
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        // Calculer les décalages pour chaque octave et le maximum théorique
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        // Gérer le cas d'une échelle nulle ou négative
        if (scale <= 0)
            scale = 0.0001f;

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // Calculer le centre de la carte
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // Générer la carte de bruit en parcourant chaque point
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1f;
                frequency = 1f;
                float noiseHeight = 0f;

                // Combiner plusieurs octaves pour obtenir un bruit plus détaillé
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // Suivre les extrêmes locaux pour la normalisation
                if (noiseHeight > maxLocalNoiseHeight)
                    maxLocalNoiseHeight = noiseHeight;
                else if (noiseHeight < minLocalNoiseHeight)
                    minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        // Normaliser la carte de bruit
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    // Normalisation locale entre le minimum et le maximum local
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else // Normalisation globale
                {
                    float normalizedHeight = (noiseMap[x, y] + 1f) / (maxPossibleHeight / 0.9f);
                    
                    // Limiter la valeur normalisée
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0f, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
