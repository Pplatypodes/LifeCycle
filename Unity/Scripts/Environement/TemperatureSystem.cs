using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemperatureSystem : MonoBehaviour
{
    /* Instance unique du système de température */
    public static TemperatureSystem Instance { get; private set; }
    private float globalTemperature;

    // Paramètres de base pour la température et la végétation
    protected float baselineTemperature;
    protected float healthyVegetation;
    protected float ashAndHiddenVegetation;
    protected float burningVegetation;
    protected float rainingCooling;
    protected float sunnyHeating;

    // Facteurs liés à l'herbe
    public float healthyGrassCoolingFactor;
    public float hiddenGrassHeating;

    private ObjectStorage objectStorage;

    /* Initialisation de l'instance unique */
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /* Initialise la température globale et démarre la routine de mise à jour */
    public void InitializeTemperature(ObjectStorage storage)
    {
        objectStorage = storage;
        StartCoroutine(UpdateTemperatureRoutine());

        if (EnvironmentController.Instance != null)
        {
            // Récupère les paramètres de température depuis l'EnvironmentController
            baselineTemperature = EnvironmentController.Instance.baselineTemperature;
            healthyVegetation = EnvironmentController.Instance.healthyVegetation;
            ashAndHiddenVegetation = EnvironmentController.Instance.ashAndHiddenVegetation;
            burningVegetation = EnvironmentController.Instance.burningVegetation;
            rainingCooling = EnvironmentController.Instance.rainingCooling;
            sunnyHeating = EnvironmentController.Instance.sunnyHeating;
            healthyGrassCoolingFactor = EnvironmentController.Instance.healthyGrassCooling;
            hiddenGrassHeating = EnvironmentController.Instance.hiddenGrassHeating;
        }
        else
        {
            Debug.LogWarning("No EnvironmentController found in scene. Using default temperature settings.");
            
            // Crée un EnvironmentController par défaut si nécessaire
            GameObject defaultECGO = new GameObject("DefaultEnvironmentController");
            EnvironmentController defaultEC = defaultECGO.AddComponent<EnvironmentController>();
            defaultEC.baselineTemperature = 30f;
            defaultEC.healthyVegetation = 0.02f;
            defaultEC.ashAndHiddenVegetation = 0.1f;
            defaultEC.burningVegetation = 0.09f;
            defaultEC.rainingCooling = 0.09f;
            defaultEC.sunnyHeating = 0.01f;
            defaultEC.healthyGrassCooling = 0.05f;
            defaultEC.hiddenGrassHeating = 0.06f;
            Debug.Log("Default EnvironmentController created with baselineTemperature = " + defaultEC.baselineTemperature);
        }

        // Initialise la température globale avec la valeur de base
        globalTemperature = baselineTemperature;
    }

    /* Routine qui met à jour la température globale chaque seconde */
    private IEnumerator UpdateTemperatureRoutine()
    {
        while (true)
        {
            UpdateGlobalTemperature();
            yield return new WaitForSeconds(1f);
        }
    }

    /* Calcule et met à jour la température globale en fonction des contributions de la végétation et des conditions météo */
    public void UpdateGlobalTemperature()
    {
        // Récupère les compteurs de la végétation
        int healthyTrees = Vegetation.healthyCount;
        int burningTrees = Vegetation.burningCount;
        int ashTrees = Vegetation.ashCount;
        
        // Récupère les compteurs de l'herbe
        int healthyGrass = Grass.healthyCount;
        int hiddenGrass = Grass.hiddenCount;
      
        // Calcul du refroidissement fourni par les arbres sains
        float treeCooling = healthyTrees * healthyVegetation;
        
        // Calcul du refroidissement fourni par l'herbe saine
        float grassCooling = healthyGrass * healthyGrassCoolingFactor;
        float totalHealthyCooling = treeCooling + grassCooling;
      
        // Refroidissement additionnel si la pluie est active
        float rainCooling = 0f;
        if (WeatherSystem.Instance != null && WeatherSystem.Instance.currentWeather == "Raining")
        {
            rainCooling = rainingCooling;
        }
  
        // Chauffage additionnel si le soleil est actif
        float sunHeating = 0f;
        if (WeatherSystem.Instance != null && WeatherSystem.Instance.currentWeather == "Sunny")
        {
            sunHeating = sunnyHeating;
        }
  
        // Effets de chauffage
        float burningHeating = burningTrees * burningVegetation * ashAndHiddenVegetation;
        float ashHeating = ashTrees * ashAndHiddenVegetation;
        float totalHiddenGrassHeating = hiddenGrass * hiddenGrassHeating;

        float totalHeating = burningHeating + ashHeating + totalHiddenGrassHeating;
  
        // Met à jour la température globale en appliquant les effets nets
        globalTemperature += totalHeating - totalHealthyCooling - rainCooling + sunHeating;
    }

    /* Retourne la température globale actuelle */
    public float GetGlobalTemperature()
    {
        return globalTemperature;
    }
}
