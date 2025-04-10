using System.Collections.Generic;
using UnityEngine;

/// Associe un prefab à une valeur de densité (0-100).
[System.Serializable]
public class ObjectDensity
{
    public GameObject objectPrefab;
    [Range(0, 100)]
    public int density;
}

/// Contient les paramètres d'une région, y compris son nom et tous les objets avec leur densité.
[System.Serializable]
public class RegionObjectSettings
{
    public string regionName;
    public List<ObjectDensity> objectDensities;
}

/// Représente un type de terrain avec un seuil de hauteur et une couleur associée.
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}

/// Structure simple encapsulant les données de la carte de hauteur et de la carte de couleur.
[System.Serializable]
public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    /* Constructeur : Initialise la structure MapData avec une carte de hauteur et une carte de couleur */
    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}

/// Contient des méthodes d'assistance pour le traitement des régions.
public static class RegionUtils
{
    /* Retourne la couleur associée au nom de la région en parcourant le tableau de terrain types */
    public static Color GetRegionColor(string regionName, TerrainType[] terrainTypes)
    {
        // Parcourir chaque type de terrain pour trouver une correspondance
        foreach (var terrainType in terrainTypes)
        {
            if (terrainType.name == regionName)
            {
                // Retourner la couleur correspondante dès qu'une correspondance est trouvée
                return terrainType.colour;
            }
        }

        // En cas d'absence de correspondance, afficher une erreur et retourner une couleur vide
        Debug.LogError($"Region {regionName} not found.");
        return Color.clear;
    }

    /* Recherche dans la carte de couleurs les coordonnées où la couleur correspond exactement à la couleur cible */
    public static List<Vector2Int> FindRegionCoordinates(Color[] colorMap, Color targetColor, int mapChunkSize)
    {
        List<Vector2Int> regionCoordinates = new List<Vector2Int>();

        // Parcourt chaque pixel de la carte de couleurs
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                // Calculer l'index dans le tableau 1D à partir des coordonnées 2D (ordre de rangées)
                if (colorMap[y * mapChunkSize + x] == targetColor)
                {
                    // Ajouter les coordonnées si la couleur correspond exactement
                    regionCoordinates.Add(new Vector2Int(x, y));
                }
            }
        }

        // Si aucune coordonnée n'a été trouvée, afficher un message d'erreur
        if (regionCoordinates.Count == 0)
        {
            Debug.LogError($"No coordinates found for region with target color {targetColor}.");
        }

        // Retourner la liste des coordonnées trouvées
        return regionCoordinates;
    }
}

