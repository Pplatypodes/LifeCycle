using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObjectGenerator : MonoBehaviour
{
    /* Liste des réglages de région où générer les objets */
    public List<RegionObjectSettings> regionsToGenerateOn;
    public ObjectStorage objectStorage;
    public string generatorType;
    private WorldGenerator worldGenerator;
    private HashSet<Vector2Int> usedCoordinates = new HashSet<Vector2Int>();
    private Dictionary<Vector2, TerrainChunk> chunkDictionary;

    /* Initialisation de base lors de l'éveil de l'objet */
    void Awake()
    {
        if (objectStorage == null)
        {
            // Récupère l'instance unique d'ObjectStorage
            objectStorage = ObjectStorage.Instance;
        }
        if (string.IsNullOrEmpty(generatorType))
            Debug.LogError(gameObject.name + ": Generator type is not set!");
    }

    /* Démarrage du processus de génération si l'application est en mode lecture */
    void Start()
    {
        if (Application.isPlaying)
            Initialize();  
            if(EnvironmentController.Instance.UseVegetationSpawner)
            {
                InitializeAllVegetation();
                InitializeAllGrass();
            }
            if(EnvironmentController.Instance.UseEnvironmentSettings)
            {
                InitializeLogger();
                InitializeTemperature();
            }
    }

    /* Initialise la génération d'objets en récupérant le WorldGenerator */
    public void Initialize()
    {
        worldGenerator = FindFirstObjectByType<WorldGenerator>();
        if (worldGenerator == null)
            return;
        
        // Récupère le dictionnaire des chunks du WorldGenerator
        chunkDictionary = worldGenerator.GetChunks();
        if (chunkDictionary == null || chunkDictionary.Count == 0)
        {
            // Génère le monde et, une fois terminé, génère les objets et affiche les objets stockés
            worldGenerator.GenerateWorld(() =>
            {
                GenerateObjectsByRegion();
                //PrintStoredObjects();
            });
        }
        else
        {
            GenerateObjectsByRegion();
            //PrintStoredObjects();
        }
    }

    /* Affiche dans la console le nombre d'objets stockés pour ce type */
    private void PrintStoredObjects()
    {
        List<StoredObject> storedObjects = objectStorage.GetAllObjects(generatorType);
        Debug.Log("Generator '" + generatorType + "' holds " + storedObjects.Count + " saved objects.");
    }

    /* Génère des objets par région en fonction des réglages de région */
    private void GenerateObjectsByRegion()
    {
        foreach (var regionSettings in regionsToGenerateOn)
        {
            // Récupère la couleur de la région cible
            Color targetColor = RegionUtils.GetRegionColor(regionSettings.regionName, worldGenerator.regions);
            if (targetColor == Color.clear) continue;
            foreach (var kvp in chunkDictionary)
            {
                TerrainChunk chunk = kvp.Value;
                // Filtre les coordonnées du chunk qui correspondent à la région cible
                List<Vector2> regionCoordinates = FilterCoordinatesByRegion(chunk.GetContainedCoordinates(), targetColor);
                if (regionCoordinates.Count > 0)
                    PlaceObjectsInRegion(regionCoordinates, regionSettings.objectDensities);
            }
        }
    }

    /* Filtre les coordonnées d'un chunk par couleur de région cible */
    private List<Vector2> FilterCoordinatesByRegion(List<Vector2> chunkCoordinates, Color targetColor)
    {
        List<Vector2> filteredCoordinates = new List<Vector2>();
        foreach (Vector2 coord in chunkCoordinates)
        {
            Vector2Int roundedCoord = new Vector2Int(Mathf.RoundToInt(coord.x), Mathf.RoundToInt(coord.y));
            if (usedCoordinates.Contains(roundedCoord))
                continue;
            int index = roundedCoord.y * worldGenerator.WorldWidth + roundedCoord.x;
            if (worldGenerator.GlobalColourMap[index] == targetColor)
                filteredCoordinates.Add(coord);
        }
        return filteredCoordinates;
    }

    /* Place des objets dans la région en fonction de leurs densités */
    private void PlaceObjectsInRegion(List<Vector2> regionCoordinates, List<ObjectDensity> objectDensities)
    {
        foreach (Vector2 coord in regionCoordinates)
        {
            Vector2Int roundedCoord = new Vector2Int(Mathf.RoundToInt(coord.x), Mathf.RoundToInt(coord.y));
            if (usedCoordinates.Contains(roundedCoord)) continue;
            
            float height = worldGenerator.GlobalNoiseMap[roundedCoord.x, roundedCoord.y];
            if (TryPlaceObject(roundedCoord, height, objectDensities))
            {
                // Marque la coordonnée comme utilisée
                usedCoordinates.Add(roundedCoord);
            }
        }
    }

    /* Essaye de placer un objet sur une coordonnée donnée en fonction des densités */
    private bool TryPlaceObject(Vector2Int coord, float height, List<ObjectDensity> objectDensities)
    {
        Vector3 worldPosition = GetWorldPositionFromCoord(coord, height);
        foreach (var densitySetting in objectDensities)
        {
            // Vérifier le tirage aléatoire pour décider si l'objet doit être placé
            if (Random.Range(0, 100) < densitySetting.density)
            {
                // Obtient la position ajustée et la rotation de base
                (Vector3 adjustedPosition, Quaternion baseRotation) = GetPositionAndRotation(worldPosition);
                
                // Applique une rotation aléatoire autour de l'axe vertical
                float randomRotation = Random.Range(-60f, 90f);
                Quaternion finalRotation = baseRotation * Quaternion.Euler(0, randomRotation, 0);
                
                // Récupère ou crée l'objet parent pour ce type d'objet
                GameObject parentObject = objectStorage.GetOrCreateParentObject(densitySetting.objectPrefab);
                GameObject newObject = Instantiate(densitySetting.objectPrefab, adjustedPosition, finalRotation);
                newObject.transform.SetParent(parentObject.transform);
                newObject.tag = densitySetting.objectPrefab.tag;
                
                // Ajoute l'objet dans le stockage
                objectStorage.AddObject(newObject, coord, generatorType);
                return true;
            }
        }
        return false;
    }

    /* Convertit une coordonnée et une hauteur en position mondiale */
    private Vector3 GetWorldPositionFromCoord(Vector2Int coord, float height)
    {
        float worldX = coord.x;
        float worldZ = (worldGenerator.WorldHeight - 1 - coord.y);
        float worldY = worldGenerator.meshHeightCurve.Evaluate(height) * worldGenerator.meshHeightMultiplier;
        Vector3 localPosition = new Vector3(worldX * worldGenerator.chunkScale, worldY, worldZ * worldGenerator.chunkScale);
        return worldGenerator.transform.TransformPoint(localPosition);
    }

    /* Obtient la position exacte et la rotation en utilisant un raycast pour ajuster la hauteur */
    private (Vector3, Quaternion) GetPositionAndRotation(Vector3 worldPosition)
    {
        if (Physics.Raycast(worldPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            return (hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
        return (worldPosition, Quaternion.identity);
    }
    
    /* Initialise la végétation pour chaque objet stocké s'il possède un composant Vegetation */
    public void InitializeAllVegetation()
    {
        if (generatorType != "Plant" && generatorType != "Vegetation")
        {
            return;
        }
    
        List<StoredObject> storedObjects = objectStorage.GetAllObjects(generatorType);
        foreach (StoredObject stored in storedObjects)
        {
            GameObject go = objectStorage.GetGameObjectFor(stored);
            if (go == null)
            {
                Debug.LogWarning("No GameObject found for stored object of type " + stored.ObjectType + " at " + stored.Coordinate);
                continue;
            }
            Vegetation veg = go.GetComponent<Vegetation>();
            if(!veg)
            {
                continue;
            }
            veg.InitializeVegetation(objectStorage);
        }
    }

    /* Initialise le gazon pour chaque objet stocké s'il possède un composant Grass */
    public void InitializeAllGrass()
    {
        if (generatorType != "Grass")
        {
            return;
        }
        
        List<StoredObject> storedObjects = objectStorage.GetAllObjects(generatorType);
        foreach (StoredObject stored in storedObjects)
        {
            GameObject go = objectStorage.GetGameObjectFor(stored);
            if (go == null)
            {
                Debug.LogWarning("No GameObject found for stored object of type " + stored.ObjectType + " at " + stored.Coordinate);
                continue;
            }

            Grass grass = go.GetComponent<Grass>();
            if (grass == null)
                continue;

            grass.InitializeGrass(objectStorage);
        }
    }

    /* Initialise le logger système d'évolution */
    public void InitializeLogger()
    {
        SystemEvolutionLogger logger = SystemEvolutionLogger.Instance;
        if (logger == null)
        {
            Debug.LogError("SystemEvolutionLogger instance not found in the scene. Please add the logger to your scene.");
            return;
        }

        logger.InitializeLogger(objectStorage);
    }

    /* Initialise le système de température */
    public void InitializeTemperature()
    {
        TemperatureSystem temperature = TemperatureSystem.Instance;
        if (temperature == null)
        {
            Debug.LogError("TemperatureSystem instance not found in the scene.");
            return;
        }

        temperature.InitializeTemperature(objectStorage);
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Objects")]
    
    /* Génère les objets en vidant d'abord le stockage et en nettoyant les parents */
    public void GenerateObjectsButton()
    {
        objectStorage.Clear();
        foreach (var regionSettings in regionsToGenerateOn)
        {
            foreach (var densitySetting in regionSettings.objectDensities)
            {
                string parentName = densitySetting.objectPrefab.name + "_Parent";
                GameObject parentObject = GameObject.Find(parentName);
                if (parentObject != null)
                    DestroyImmediate(parentObject);
            }
        }
        Initialize();
    }

    [ContextMenu("Clean Up Objects")]
    
    /* Nettoie les objets générés pour ce générateur en vidant le stockage et détruisant les objets parents */
    public void CleanUpObjectsButton()
    {
        if (objectStorage == null || string.IsNullOrEmpty(generatorType))
        {
            Debug.LogError("ObjectStorage or generatorType is not set!");
            return;
        }
        objectStorage.Clear();
        foreach (var regionSettings in regionsToGenerateOn)
        {
            foreach (var densitySetting in regionSettings.objectDensities)
            {
                string parentName = densitySetting.objectPrefab.name + "_Parent";
                GameObject parentObject = GameObject.Find(parentName);
                if (parentObject != null)
                    DestroyImmediate(parentObject);
            }
        }
    }
#endif
}
