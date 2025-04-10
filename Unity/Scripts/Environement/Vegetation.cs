using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vegetation : MonoBehaviour 
{
    /* Compteurs globaux pour suivre les états de la végétation */
    public static int healthyCount = 0;
    public static int burningCount = 0;
    public static int ashCount = 0;
    public static int hiddenCount = 0;

    private string state = "";
    private Vector2Int gridCoordinates;

    // Matériaux pour les différents états de la végétation
    public Material healthyTrunkMaterial;
    public Material healthyLeavesMaterial;
    public Material burningTrunkMaterial;
    public Material burningLeavesMaterial;
    public Material ashTrunkMaterial;
    public Material ashLeavesMaterial;

    // Paramètres de feu et de croissance
    private bool fireSpreadEnabled;
    private float probabilityOfFire;
    private float proximityRadius;
    private float ashDisplayTime;
    private float burnDisplayTime;
    private float regrowthProbability;
    private float regrowthDelay;
    private float growthDuration;

    private Vector3 originalScale;
    private ObjectStorage objectStorage;

    // Paramètres de surbrillance (lorsqu'un objet est cliqué)
    [Header("Highlight Settings")]
    [Tooltip("The material used to highlight objects when clicked.")]
    public Material highlightMaterial;

    [Tooltip("The duration (in seconds) for which objects remain highlighted.")]
    public float highlightDuration = 2f;

    /* Propriété en lecture seule de l'état actuel */
    public string currentState { get { return state; } }


    /* Initialise la végétation en récupérant ObjectStorage et les paramètres environnementaux */
    public void InitializeVegetation(ObjectStorage storage) 
    {
        // Stocke la référence et la taille d'origine
        objectStorage = storage;
        originalScale = transform.localScale;
        
        if (EnvironmentController.Instance != null)
        {
            fireSpreadEnabled = EnvironmentController.Instance.enableFireSpread;
            probabilityOfFire = EnvironmentController.Instance.probabilityOfFire;
            proximityRadius = EnvironmentController.Instance.proximityRadius;
            ashDisplayTime = EnvironmentController.Instance.ashDisplayTime;
            regrowthProbability = EnvironmentController.Instance.regrowthProbability;
            regrowthDelay = EnvironmentController.Instance.regrowthDelay;
            growthDuration = EnvironmentController.Instance.growthDuration;
            burnDisplayTime = EnvironmentController.Instance.burnDisplayTime;
        }
        else
        {
            Debug.LogWarning("No EnvironmentController found in scene. Using default fire settings.");
            
            // Valeurs par défaut si EnvironmentController est absent
            fireSpreadEnabled = true;
            probabilityOfFire = 0.001f;
            proximityRadius = 3f;
            ashDisplayTime = 3f;
            burnDisplayTime = 2f;
            regrowthProbability = 0.5f;
            regrowthDelay = 5f;
            growthDuration = 3f;
        }
        
        // Récupère les coordonnées de l'objet dans le stockage
        Vector2Int? coords = objectStorage.GetCoordinates(gameObject);
        if (coords.HasValue)
            gridCoordinates = coords.Value;
        else
            Debug.LogWarning("Vegetation could not retrieve its coordinates from storage: " + gameObject.name);
        
        // Définit l'état initial sur "Healthy"
        SetState("Healthy");

        if (fireSpreadEnabled)
        {
            // Démarre la vérification du feu en continu
            StartCoroutine(CheckFireStatus());
        }
    }
    
    /* Définit l'état de la végétation en ajustant les compteurs globaux */
    private void SetState(string newState)
    {
        // Réduit le compteur de l'ancien état si nécessaire
        if (!string.IsNullOrEmpty(state))
        {
            switch(state)
            {
                case "Healthy": healthyCount = Mathf.Max(healthyCount - 1, 0); break;
                case "Burning": burningCount = Mathf.Max(burningCount - 1, 0); break;
                case "Ash": ashCount = Mathf.Max(ashCount - 1, 0); break;
                case "Hidden": hiddenCount = Mathf.Max(hiddenCount - 1, 0); break;
            }
        }
        state = newState;
        
        // Augmente le compteur du nouvel état
        switch(newState)
        {
            case "Healthy": healthyCount++; break;
            case "Burning": burningCount++; break;
            case "Ash": ashCount++; break;
            case "Hidden": hiddenCount++; break;
        }
        
        // Met à jour les matériaux pour le nouvel état
        UpdateMaterials();
    }
    
    /* Vérifie périodiquement l'état d'incendie de la végétation */
    private IEnumerator CheckFireStatus() 
    {
        while (state != "Ash") 
        {
            // Si la pluie est active, extinct tout incendie
            if (WeatherSystem.Instance != null && WeatherSystem.Instance.currentWeather == "Raining")
            {
                if (state == "Burning")
                {
                    SetState("Healthy");
                }
                yield return new WaitForSeconds(1);
                continue;
            }

            if (state == "Healthy" && fireSpreadEnabled)
            {
                // Vérifie si des végétations proches brûlent
                if (IsNearbyVegetationBurning())
                {
                    SetState("Burning");
                    StartCoroutine(Burn());
                }
                else 
                {
                    // Calcul de la probabilité ajustée pour déclencher un incendie
                    float adjustedProbability = Mathf.Clamp(probabilityOfFire * Mathf.Log(Vegetation.healthyCount + 1), 0f, 1f);
                    if (Random.value < adjustedProbability)
                    {
                        SetState("Burning");
                        StartCoroutine(Burn());
                    }
                }
            }
            
            yield return new WaitForSeconds(1);
        }
    }

    /* Gère le processus de combustion sur une durée définie */
    private IEnumerator Burn() 
    {
        float elapsed = 0f;
        while (elapsed < burnDisplayTime)
        {
            if (WeatherSystem.Instance != null && WeatherSystem.Instance.currentWeather == "Raining")
            {
                SetState("Healthy");
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetState("Ash");
        yield return new WaitForSeconds(ashDisplayTime);
        StartCoroutine(RegrowCycle());
    }
    
    /* Cycle de repousse après combustion en masquant puis faisant croître la végétation */
    private IEnumerator RegrowCycle() 
    {
        HideVegetation();

        while (true)
        {
            yield return new WaitForSeconds(regrowthDelay);
            
            float effectiveRegrowthProbability = regrowthProbability;
            if (WeatherSystem.Instance != null && WeatherSystem.Instance.currentWeather == "Raining")
            {
                effectiveRegrowthProbability = Mathf.Min(regrowthProbability * 3.5f, 1f);
            }
            
            if (Random.value < effectiveRegrowthProbability)
            {
                SetState("Healthy");
                transform.localScale = originalScale * 0.1f;
                ShowVegetation();
                yield return StartCoroutine(Grow());
                StartCoroutine(CheckFireStatus());
                yield break;
            }
        }
    }
    
    /* Gère la croissance de la végétation sur une durée donnée */
    private IEnumerator Grow()
    {
        float elapsed = 0f;
        Vector3 startScale = originalScale * 0.1f;
        Vector3 endScale = originalScale;

        while (elapsed < growthDuration)
        {
            // Interpole l'échelle de croissance
            transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / growthDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = endScale;
    }
    
    /* Masque la végétation en désactivant ses MeshRenderers */
    private void HideVegetation() 
    {
        SetState("Hidden");
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }
    
    /* Affiche la végétation en activant ses MeshRenderers */
    private void ShowVegetation() 
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = true;
        }
    }
    
    /* Retourne true si cette végétation est en combustion */
    public bool IsBurning() 
    {
        return state == "Burning";
    }
    
    /* Met à jour les matériaux des parties de la végétation en fonction de l'état */
    private void UpdateMaterials() 
    {
        foreach (Transform child in transform)
        {
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                if (child.name.ToLower().Contains("trunk"))
                {
                    switch (state)
                    {
                        case "Healthy": meshRenderer.material = healthyTrunkMaterial; break;
                        case "Burning": meshRenderer.material = burningTrunkMaterial; break;
                        case "Ash": meshRenderer.material = ashTrunkMaterial; break;
                        case "Hidden": meshRenderer.material = healthyTrunkMaterial; break;
                    }
                }
                else if (child.name.ToLower().Contains("leaves"))
                {
                    switch (state)
                    {
                        case "Healthy": meshRenderer.material = healthyLeavesMaterial; break;
                        case "Burning": meshRenderer.material = burningLeavesMaterial; break;
                        case "Ash": meshRenderer.material = ashLeavesMaterial; break;
                        case "Hidden": meshRenderer.material = healthyLeavesMaterial; break;
                    }
                }
            }
        }
    }
    
    /* Vérifie si une végétation à proximité est en combustion */
    public bool IsNearbyVegetationBurning()
    {
        List<GameObject> nearbyObjects = objectStorage.GetObjectsInRadius(gridCoordinates, Mathf.CeilToInt(proximityRadius));
        foreach (GameObject obj in nearbyObjects)
        {
            if(obj == null)
                continue;
            if (obj == this.gameObject)
                continue;
            Vegetation vegetation = obj.GetComponent<Vegetation>();
            if (vegetation != null && vegetation.IsBurning())
            {
                return true;
            }
        }
        return false;
    }
    
    /* Retourne l'état actuel de la végétation */
    public string GetState()
    {
        return state;
    }
    
    /* Lorsque l'objet est détruit, ajuste les compteurs et supprime l'objet du stockage */
    private void OnDestroy() 
    {
        switch(state)
        {
            case "Healthy": healthyCount = Mathf.Max(healthyCount - 1, 0); break;
            case "Burning": burningCount = Mathf.Max(burningCount - 1, 0); break;
            case "Ash": ashCount = Mathf.Max(ashCount - 1, 0); break;
            case "Hidden": hiddenCount = Mathf.Max(hiddenCount - 1, 0); break;
        }
        if (objectStorage != null)
            objectStorage.RemoveObject(gameObject);
    }

    /* Gère la surbrillance lorsqu'on clique sur cet objet */
    private void OnMouseDown()
    {
        // S'assure que la référence à ObjectStorage est valide
        if (objectStorage == null)
            objectStorage = ObjectStorage.Instance;

        // Récupère les coordonnées de la grille pour cet objet
        Vector2Int? coords = objectStorage.GetCoordinates(gameObject);
        if (!coords.HasValue)
        {
            Debug.LogWarning("No grid coordinates found for " + gameObject.name);
            return;
        }
        gridCoordinates = coords.Value;

        HighlightNearbyObjects();
    }

    /* Met en surbrillance les objets à proximité pendant la durée définie */
    private void HighlightNearbyObjects()
    {
        // Définit la portée à partir de proximityRadius convertie en entier
        int highlightRange = Mathf.CeilToInt(proximityRadius);

        // Récupère tous les objets dans le rayon spécifié
        List<GameObject> objectsInRange = objectStorage.GetObjectsInRadius(gridCoordinates, highlightRange);

        foreach (GameObject go in objectsInRange)
        {
            if (go == null)
                continue;

            // Pour chaque enfant, recherche un MeshRenderer et change son matériau pour le surlignage
            foreach (Transform child in go.transform)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    // Sauvegarde le matériau original
                    Material original = renderer.material;
                    
                    // Applique le matériau de surbrillance
                    renderer.material = highlightMaterial;
                    
                    // Lance la coroutine pour réinitialiser le matériau
                    StartCoroutine(ResetHighlight(renderer, original, highlightDuration));
                }
            }
        }
    }

    /* Coroutine qui réinitialise le matériau après un délai donné */
    private IEnumerator ResetHighlight(MeshRenderer renderer, Material originalMaterial, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (renderer != null)
            renderer.material = originalMaterial;
    }
}
