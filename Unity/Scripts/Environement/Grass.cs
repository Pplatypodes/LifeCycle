using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grass : MonoBehaviour
{
    public static int healthyCount = 0;
    public static int hiddenCount = 0;
    private string state = "";
    private Vector2Int gridCoordinates;
    private float regrowthProbability;
    private float regrowthDelay;
    private float growthDuration;
    private Vector3 originalScale;
    private ObjectStorage objectStorage;

    /* Renvoie l'état actuel de l'herbe */
    public string currentState { get { return state; } }

    /* Initialise l'herbe avec les paramètres de repousse depuis l'EnvironmentController */
    public void InitializeGrass(ObjectStorage storage)
    {
        objectStorage = storage;
        originalScale = transform.localScale;

        if (EnvironmentController.Instance != null)
        {
            // Utilise les paramètres de croissance de l'herbe définis dans EnvironmentController
            regrowthProbability = EnvironmentController.Instance.regrowthProbabilityGrass;
            regrowthDelay = EnvironmentController.Instance.regrowthDelayGrass;
            growthDuration = EnvironmentController.Instance.growthDurationGrass;
        }
        else
        {
            Debug.LogWarning("No EnvironmentController found in scene. Using default grass regrowth parameters.");
            regrowthProbability = 0.1f;
            regrowthDelay = 3f;
            growthDuration = 9f;
        }

        // Récupère les coordonnées de la grille pour cet objet
        Vector2Int? coords = objectStorage.GetCoordinates(gameObject);
        if (coords.HasValue)
            gridCoordinates = coords.Value;
        else
            Debug.LogWarning("Grass could not retrieve its coordinates from storage: " + gameObject.name);

        // Définit l'état initial sur "Healthy"
        SetState("Healthy");
    }

    /* Définit l'état de l'herbe et ajuste les compteurs globaux */
    private void SetState(string newState)
    {
        if (!string.IsNullOrEmpty(state))
        {
            switch (state)
            {
                case "Healthy":
                    healthyCount = Mathf.Max(healthyCount - 1, 0);
                    break;
                case "Hidden":
                    hiddenCount = Mathf.Max(hiddenCount - 1, 0);
                    break;
            }
        }

        state = newState;

        switch (newState)
        {
            case "Healthy":
                healthyCount++;
                break;
            case "Hidden":
                hiddenCount++;
                break;
        }
    }

    /* Déclenche l'action "Eat", masque l'herbe et démarre le cycle de repousse */
    public void Eat()
    {
        if (state == "Healthy")
        {
            HideGrass();
            StartCoroutine(RegrowCycle());
        }
    }

    /* Cycle de repousse de l'herbe après avoir été mangée */
    private IEnumerator RegrowCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(regrowthDelay);
            
            float effectiveRegrowthProbability = regrowthProbability;
            if (WeatherSystem.Instance != null && WeatherSystem.Instance.currentWeather == "Raining")
            {
                effectiveRegrowthProbability = Mathf.Min(regrowthProbability * 4f, 1f);
            }

            if (Random.value < effectiveRegrowthProbability)
            {
                SetState("Healthy");
                transform.localScale = originalScale * 0.1f;
                ShowGrass();
                yield return StartCoroutine(Grow());
                break;
            }
        }
    }

    /* Fait croître l'herbe de sa petite taille à sa taille normale */
    private IEnumerator Grow()
    {
        float elapsed = 0f;
        Vector3 startScale = originalScale * 0.1f;
        Vector3 endScale = originalScale;

        while (elapsed < growthDuration)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / growthDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = endScale;
    }

    /* Masque l'herbe en désactivant ses MeshRenderers et Colliders */
    private void HideGrass()
    {
        SetState("Hidden");
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    /* Affiche l'herbe en activant ses MeshRenderers et Colliders */
    private void ShowGrass()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = true;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }
    }

    /* Retourne l'état actuel de l'herbe */
    public string GetState()
    {
        return state;
    }

    /* À la destruction, ajuste les compteurs et supprime cet objet du stockage */
    private void OnDestroy()
    {
        switch (state)
        {
            case "Healthy":
                healthyCount = Mathf.Max(healthyCount - 1, 0);
                break;
            case "Hidden":
                hiddenCount = Mathf.Max(hiddenCount - 1, 0);
                break;
        }

        if (objectStorage != null)
            objectStorage.RemoveObject(gameObject);
    }
}
