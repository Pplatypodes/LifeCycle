using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnvironmentController : MonoBehaviour
{
    public static EnvironmentController Instance { get; private set; }

    public bool UseVegetationSpawner = true;
    public bool UseEnvironmentSettings = true;
    
    [Header("Paramètres de propagation du feu")]
    public bool enableFireSpread = true;
    public float probabilityOfFire = 0.0005f;
    public float proximityRadius = 3f;
    public float ashDisplayTime = 3f;
    public float burnDisplayTime = 2f;

    [Header("Paramètres de repousse des arbres")]
    public float regrowthProbability = 0.1f;
    public float regrowthDelay = 3f;
    public float growthDuration = 9f;

    [Header("Paramètres de croissance de l'herbe")]
    public float regrowthProbabilityGrass = 0.5f;
    public float regrowthDelayGrass = 5f;
    public float growthDurationGrass = 3f;

    [Header("Paramètres météo")]
    public bool weatherEnabled = true;
    public float weatherChangeInterval = 30f;
    
    [Tooltip("Température élevée = plus de pluie.")]
    public float highTemperatureRainMultiplier = 1.5f;
    public float lowTemperatureSunnyMultiplier = 1.5f;
    public List<WeatherStateProbability> weatherStates = new List<WeatherStateProbability>()
    {
        new WeatherStateProbability(){ weatherState = "Sunny", probability = 80f },
        new WeatherStateProbability(){ weatherState = "Raining", probability = 20f }
    };

    [Header("Paramètres de température")]
    public float baselineTemperature = 25f;

    
    [Header("Facteurs de réchauffement")]
    
    [Tooltip("Plus d'arbres en cendre / cachés = températures plus élevées.")]
    public float ashAndHiddenVegetation = 0.04f;
    
    [Tooltip("Plus d'arbres en feu = températures plus élevées.")]
    public float burningVegetation = 1.5f;

    [Tooltip("Plus d'herbe cachée (mangée) = températures plus élevées.")]
    public float hiddenGrassHeating = 0.06f;

    public float sunnyHeating = 5f;

    
    [Header("Facteurs de refroidissement")]
    
    [Tooltip("Plus d'arbres sains = températures plus basses.")]
    public float healthyVegetation = 0.08f;
    
    [Tooltip("Plus d'herbe saine = températures plus basses.")]
    public float healthyGrassCooling = 0.05f;
    
    public float rainingCooling = 0.1f;
    
    
    private void Awake()
    {
        // Vérifie s'il existe déjà une instance et la conserve unique
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
