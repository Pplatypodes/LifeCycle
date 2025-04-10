using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeatherStateProbability
{
    /* Définit le nom de l'état météo et sa probabilité (sera normalisé automatiquement) */
    [Tooltip("Name of the weather state (e.g., Sunny, Raining, Snowing, etc.)")]
    public string weatherState;

    [Tooltip("Probability of this weather state being selected (values will be normalized automatically)")]
    public float probability;
}

public class WeatherSystem : MonoBehaviour 
{
    /* Instance unique du système météo */
    public static WeatherSystem Instance { get; private set; }
    public GameObject rainParticleSystem;
    protected float baselineTemperature;
    protected bool weatherEnabled;
    protected float weatherChangeInterval;
    protected float highTemperatureRainMultiplier;
    protected float lowTemperatureSunnyMultiplier;

    public string currentWeather { get; private set; }
    private List<WeatherStateProbability> weatherStates;

    /* Initialisation de l'instance unique */
    private void Awake() 
    {
        // Vérifie s'il existe déjà une instance, sinon la définit
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /* Configuration initiale du système météo */
    private void Start() 
    {
        if (EnvironmentController.Instance != null)
        {
            // Récupère les paramètres depuis l'EnvironmentController
            weatherEnabled = EnvironmentController.Instance.weatherEnabled;
            baselineTemperature = EnvironmentController.Instance.baselineTemperature;
            weatherChangeInterval = EnvironmentController.Instance.weatherChangeInterval;
            weatherStates = EnvironmentController.Instance.weatherStates;
            highTemperatureRainMultiplier = EnvironmentController.Instance.highTemperatureRainMultiplier;
            lowTemperatureSunnyMultiplier = EnvironmentController.Instance.lowTemperatureSunnyMultiplier;
        }
        else
        {
            Debug.LogWarning("No EnvironmentController found in scene. Using default weather settings.");
            // Définit des valeurs par défaut si aucun EnvironmentController n'est trouvé
            weatherEnabled = true;
            weatherChangeInterval = 30f;
            weatherStates = new List<WeatherStateProbability>()
            {
                new WeatherStateProbability(){ weatherState = "Sunny", probability = 80f },
                new WeatherStateProbability(){ weatherState = "Raining", probability = 20f }
            };
            highTemperatureRainMultiplier = 1f;
            lowTemperatureSunnyMultiplier = 1f;
        }

        // Définit l'état météo initial
        if (weatherStates != null && weatherStates.Count > 0)
        {
            currentWeather = weatherStates[0].weatherState;
        }
        else
        {
            currentWeather = "Sunny";
        }
        UpdateWeather(currentWeather);

        // Met à jour la météo de façon répétée si activé
        if (weatherEnabled)
        {
            InvokeRepeating("SetRandomWeather", weatherChangeInterval, weatherChangeInterval);
        }
    }

    /* Sélectionne aléatoirement un état météo en fonction des probabilités ajustées */
    private void SetRandomWeather()
    {
        // Récupère la température actuelle ou utilise la température de base si le système de température est absent
        float currentTemp = TemperatureSystem.Instance != null 
            ? TemperatureSystem.Instance.GetGlobalTemperature() 
            : baselineTemperature;

        // Ajuste les probabilités effectives en fonction de la température
        List<float> effectiveProbabilities = new List<float>();
        float totalEffectiveProbability = 0f;

        foreach (var entry in weatherStates)
        {
            float effective = entry.probability;

            // Si la température est élevée, augmente la probabilité de pluie
            if (entry.weatherState == "Raining" && currentTemp > baselineTemperature)
            {
                effective *= highTemperatureRainMultiplier;
            }
            // Si la température est basse, augmente la probabilité de soleil
            else if (entry.weatherState == "Sunny" && currentTemp < 0f)
            {
                effective *= lowTemperatureSunnyMultiplier;
            }

            effectiveProbabilities.Add(effective);
            totalEffectiveProbability += effective;
        }

        // Sélection aléatoire pondérée selon les probabilités ajustées
        float randomValue = Random.Range(0f, totalEffectiveProbability);
        float cumulative = 0f;

        for (int i = 0; i < weatherStates.Count; i++)
        {
            cumulative += effectiveProbabilities[i];
            if (randomValue <= cumulative)
            {
                currentWeather = weatherStates[i].weatherState;
                break;
            }
        }

        // Met à jour l'effet météo correspondant
        UpdateWeather(currentWeather);
    }

    /* Met à jour l'effet météo en activant/désactivant les effets associés */
    private void UpdateWeather(string newWeather)
    {
        // Si la météo n'est pas activée, force Sunny
        if (!weatherEnabled)
        {
            newWeather = "Sunny";
        }
        
        // Active le système de pluie si nécessaire
        if (newWeather == "Raining")
        {
            StartRainEffect();
        }
        else
        {
            StopRainEffect();
        }

        Debug.Log("Weather changed to: " + newWeather);
    }

    /* Démarre l'effet de pluie */
    private void StartRainEffect() 
    {
        Debug.Log("Rain started.");
        if (rainParticleSystem != null)
        {
            // Active le système de particules de pluie
            rainParticleSystem.SetActive(true);
        }
    }

    /* Arrête l'effet de pluie */
    private void StopRainEffect() 
    {
        if (rainParticleSystem != null)
        {
            rainParticleSystem.SetActive(false);
        }
    }

    /* Retourne le pourcentage de précipitations actuel (100 pour pluie, 0 sinon) */
    public static float GetCurrentPrecipitation() 
    {
        if (Instance != null)
        {
            return Instance.currentWeather == "Raining" ? 100f : 0f;
        }
        return 0f;
    }
}
