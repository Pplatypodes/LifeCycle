using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BearAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    // Variables de chasse
    public float visionRadius = 20f;  // Rayon de détection de la proie
    public float moveSpeed = 3.5f;    // Vitesse de déplacement
    public float attackRange = 2.0f;  // Plage d'attaque pour "consommer" la proie
    private GameObject targetPrey;    // Proie actuelle

    // Variables d'énergie
    public float maxEnergy = 100f;          // Énergie maximale
    public float energyDepletionRate = 10f; // Taux de diminution d'énergie
    public float energyRegenerationRate = 5f; // Taux de régénération de l'énergie
    private float currentEnergy;             // Énergie actuelle
    private bool isResting = false;          // Si l'ours se repose
    public float lowEnergyThreshold = 20f;   // Seuil d'énergie faible

    // Variables de faim
    public float maxHunger = 100f;            // Faim maximale
    public float hungerDepletionRate = 5f;   // Taux de diminution de la faim
    public float extraHungerDepletionMultiplier = 2f; // Multiplier de faim supplémentaire quand il bouge
    private float currentHunger;              // Faim actuelle
    public float lowHungerThreshold = 40f;    // Seuil de faim faible
    
    // Variables de reproduction
    public bool isMature = true;               // Si l'ours est mature
    public float reproductionCooldown = 60f;   // Délai de reproduction entre les tentatives
    private float reproductionTimer = 0f;      // Minuteur de reproduction
    public float reproductionChance = 0.5f;    // Chance de reproduction
    public float reproductionRange = 2f;       // Plage de recherche de partenaire
    public GameObject bearPrefab;              // Préfabriqué du bébé ours
    public float growthDuration = 120f;        // Durée de croissance du bébé
    public float babyScaleFactor = 0.6f;       // Facteur de mise à l'échelle du bébé
    private Vector3 fullGrownScale;            // Taille adulte

    // Variables de maladie
    private bool isSick = false;               // Si l'ours est malade
    private bool isImmune = false;             // Si l'ours est immunisé
    public float sicknessDuration = 20f;       // Durée de la maladie
    public float baseSicknessChance = 0.01f;   // Chance de tomber malade
    public int bearPopulationThreshold = 100;  // Seuil de population pour augmenter la chance de maladie
    public float populationSicknessIncrease = 0.0003f; // Augmentation de la chance de maladie selon la population
    public float deathChance = 0.5f;           // Chance de mourir en cas de maladie
    public float immunityChance = 0.3f;        // Chance d'obtenir l'immunité après guérison

    // Comptes statiques pour la population
    private static int bearPopulation = 0;     // Population totale d'ours
    private static int deadBearCount = 0;      // Nombre d'ours morts
    public bool IsSick { get { return isSick; } } // Propriété pour savoir si l'ours est malade
    public static int DeadBearCount { get { return deadBearCount; } } // Propriété pour récupérer le nombre d'ours morts

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            Debug.LogError("Composant Animator manquant sur " + gameObject.name);

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("Composant NavMeshAgent manquant sur " + gameObject.name);
            return;
        }
        agent.speed = moveSpeed;

        currentEnergy = maxEnergy;  // Initialisation de l'énergie
        currentHunger = maxHunger;  // Initialisation de la faim
        fullGrownScale = transform.localScale;  // Enregistrer la taille adulte

        // Si l'ours est immature (bébé), on ajuste sa taille et on démarre la croissance
        if (!isMature)
        {
            transform.localScale = fullGrownScale * babyScaleFactor;
            StartCoroutine(Grow());  // Commence la croissance
        }

        bearPopulation++;  // Incrémenter la population d'ours

        // Démarre une routine pour le comportement de patrouille et de chasse
        InvokeRepeating("SearchForPrey", 1f, 2f);
    }

    void Update()
    {
        if (agent == null || anim == null)
            return;
        if (!agent.isOnNavMesh)
            return;

        // Déplétion de la faim
        float extraHunger = extraHungerDepletionMultiplier * (agent.velocity.magnitude / moveSpeed);
        currentHunger -= (hungerDepletionRate + extraHunger) * Time.deltaTime;
        currentHunger = Mathf.Max(currentHunger, 0f);

        // Déplétion de l'énergie
        if (!isResting)
        {
            currentEnergy -= energyDepletionRate * Time.deltaTime;
            currentEnergy = Mathf.Max(currentEnergy, 0f);
        }

        // Vérification de la maladie (si l'ours n'est pas déjà malade ou immunisé)
        if (!isSick && !isImmune)
        {
            float infectionChance = baseSicknessChance;
            if (bearPopulation > bearPopulationThreshold)
                infectionChance += (bearPopulation - bearPopulationThreshold) * populationSicknessIncrease;
            if (Random.value < infectionChance * Time.deltaTime)
                BecomeSick();  // L'ours tombe malade avec une certaine probabilité
        }

        // Ajustement de la vitesse de déplacement en fonction de l'état de l'ours
        if (isSick)
        {
            agent.speed = moveSpeed * 0.5f;  // Réduire la vitesse si malade
        }
        else if (currentEnergy <= 0)
        {
            if (!isResting)
                StartCoroutine(Rest());  // Si l'énergie est épuisée, l'ours se repose
            agent.speed = moveSpeed * 0.5f;
        }
        else
        {
            agent.speed = moveSpeed;
        }

        // Comportement de chasse : si une proie est trouvée, l'ours se dirige vers elle
        if (targetPrey != null)
        {
            if (targetPrey) // Vérification que la proie existe toujours
            {
                agent.SetDestination(targetPrey.transform.position);  // Déplace l'ours vers la proie
                float distanceToPrey = Vector3.Distance(transform.position, targetPrey.transform.position);
                if (distanceToPrey <= attackRange && !isResting)
                    ConsumePrey(targetPrey);  // Consommer la proie si dans la plage d'attaque
            }
            else
            {
                targetPrey = null;  // Si la proie est détruite ou disparaît, on la supprime
            }
        }
        else
        {
            // Si aucune proie n'est disponible, l'ours patrouille de manière aléatoire
            if (agent.remainingDistance < 1f && !isResting)
                SetRandomPatrol();
        }

        // Si la faim est faible et qu'aucune proie n'est trouvée, on recherche une proie
        if (currentHunger < lowHungerThreshold && targetPrey == null)
            SearchForPrey();

        // Logique de reproduction : si l'ours est mature, pas en train de se reposer, et a suffisamment d'énergie/faim
        if (isMature && currentEnergy > lowEnergyThreshold && currentHunger > lowHungerThreshold)
        {
            reproductionTimer += Time.deltaTime;
            if (reproductionTimer >= reproductionCooldown)
            {
                Collider[] nearby = Physics.OverlapSphere(transform.position, reproductionRange);
                bool partnerFound = false;
                if (nearby != null && nearby.Length > 0)
                {
                    foreach (Collider col in nearby)
                    {
                        if (col == null)
                            continue;
                        BearAI otherBear = col.GetComponent<BearAI>();
                        if (otherBear != null && col.gameObject != gameObject && otherBear.isMature)
                        {
                            partnerFound = true;
                            break;
                        }
                    }
                }
                if (partnerFound)
                {
                    AttemptReproduction();  // Tentative de reproduction
                }
                else
                {
                    SeekPartner();  // Cherche un partenaire
                }
            }
        }

        // Ajuste la vitesse de l'animation en fonction du mouvement
        anim.speed = Mathf.Clamp(agent.velocity.magnitude, 0.1f, 2f);
    }

    // Patrouille vers un endroit aléatoire sur le NavMesh
    void SetRandomPatrol()
    {
        if (agent == null)
            return;

        Vector3 randomPos = RandomNavMeshPosition(20f);
        agent.SetDestination(randomPos);
    }

    // Choisit une position aléatoire sur le NavMesh dans un rayon donné
    Vector3 RandomNavMeshPosition(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    // Recherche de la proie dans le rayon de vision
    void SearchForPrey()
    {
        if (agent == null)
            return;
        if (!agent.isOnNavMesh)
            return;
        if (currentHunger <= 0)
            return;

        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, visionRadius);
        float minDistance = Mathf.Infinity;
        if (nearbyObjects != null)
        {
            foreach (Collider obj in nearbyObjects)
            {
                if (obj == null)
                    continue;

                if (obj.CompareTag("Deer")) // Vérifie si l'objet est une proie (cerf)
                {
                    float distance = Vector3.Distance(transform.position, obj.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        targetPrey = obj.gameObject;  // Définit la nouvelle proie
                    }
                }
            }
        }
        if (targetPrey == null)
            SetRandomPatrol();  // Si aucune proie n'est trouvée, l'ours patrouille
    }

    // Consomme la proie lorsqu'elle est dans la plage d'attaque
    void ConsumePrey(GameObject prey)
    {
        if (prey == null)
            return;

        Destroy(prey);  // Détruire la proie
        targetPrey = null;
        currentEnergy = maxEnergy;  // Restaurer l'énergie
        currentHunger = maxHunger;  // Restaurer la faim
        StartCoroutine(HuntCooldown(3f));  // Pause après une chasse réussie
    }

    // Temps de pause après une chasse
    IEnumerator HuntCooldown(float duration)
    {
        yield return new WaitForSeconds(duration);  // Attendre avant de recommencer
    }

    // Si l'énergie est trop basse, l'ours se repose pour récupérer
    IEnumerator Rest()
    {
        if (agent == null)
            yield break;
        isResting = true;
        agent.isStopped = true;  // Arrête l'agent

        yield return new WaitForSeconds(5f);  // Temps de repos
        currentEnergy = maxEnergy;  // Récupérer l'énergie
        isResting = false;
        agent.isStopped = false;  // Reprendre la navigation
    }

    // Tentative de reproduction avec un autre ours à proximité
    void AttemptReproduction()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, reproductionRange);
        if (colliders == null || colliders.Length == 0)
            return;
        foreach (var col in colliders)
        {
            if (col == null)
                continue;
            BearAI otherBear = col.GetComponent<BearAI>();
            if (otherBear != null && col.gameObject != gameObject && otherBear.isMature)
            {
                if (Random.value <= reproductionChance) // Chance de reproduction
                    Reproduce();  // Effectuer la reproduction
                reproductionTimer = 0f;  // Réinitialiser le temps de reproduction
                break;
            }
        }
    }

    // Si aucun partenaire n'est trouvé immédiatement, l'ours cherche un partenaire
    void SeekPartner()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, visionRadius);
        if (colliders == null || colliders.Length == 0)
            return;
        GameObject closestPartner = null;
        float minDistance = Mathf.Infinity;
        foreach (var col in colliders)
        {
            if (col == null)
                continue;
            BearAI otherBear = col.GetComponent<BearAI>();
            if (otherBear != null && col.gameObject != gameObject && otherBear.isMature)
            {
                if (col.transform != null)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPartner = col.gameObject;
                    }
                }
            }
        }
        if (closestPartner != null && agent != null)
            agent.SetDestination(closestPartner.transform.position);  // Se diriger vers le partenaire
    }

    // Instancier un bébé ours avec une échelle réduite et déclencher sa croissance
    void Reproduce()
    {
        if (bearPrefab == null)
        {
            Debug.LogError("Préfabriqué de l'ours manquant.");
            return;
        }
        GameObject baby = Instantiate(bearPrefab, transform.position, Quaternion.identity);
        if (baby == null)
        {
            Debug.LogError("Échec de l'instanciation du bébé ours.");
            return;
        }
        NavMeshHit hit;
        if (baby.transform == null || !NavMesh.SamplePosition(baby.transform.position, out hit, 1f, NavMesh.AllAreas))
        {
            Destroy(baby);
            return;
        }
        BearAI babyBear = baby.GetComponent<BearAI>();
        if (babyBear != null)
        {
            babyBear.isMature = false;
            babyBear.transform.localScale = fullGrownScale * babyScaleFactor; // Taille réduite du bébé
            babyBear.fullGrownScale = fullGrownScale; // Taille adulte
            StartCoroutine(babyBear.Grow());  // Commence la croissance
            if (isSick)
                babyBear.isSick = false;  // Le bébé ne sera pas malade dès la naissance
        }
    }

    // La croissance du bébé ours jusqu'à sa taille adulte
    IEnumerator Grow()
    {
        float growthTime = 0f;
        while (growthTime < growthDuration)
        {
            growthTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, fullGrownScale, growthTime / growthDuration);
            yield return null;
        }
        isMature = true;
    }

    // Devenir malade
    void BecomeSick()
    {
        isSick = true;
        StartCoroutine(SicknessProgress());
    }

    // La progression de la maladie
    IEnumerator SicknessProgress()
    {
        float sicknessTime = 0f;
        while (sicknessTime < sicknessDuration)
        {
            sicknessTime += Time.deltaTime;
            yield return null;
        }

        // Décide si l'ours meurt ou guérit après la maladie
        if (Random.value <= deathChance)
        {
            Die();
        }
        else
        {
            Heal();
        }
    }

    // Guérir après la maladie et obtenir de l'immunité
    void Heal()
    {
        isSick = false;
        isImmune = true;
        if (Random.value <= immunityChance)
        {
            isImmune = true;
        }
    }

    // L'ours meurt (déstruction)
    void Die()
    {
        bearPopulation--;
        deadBearCount++;
        Destroy(gameObject);  // Détruire l'ours
    }

    // Lorsqu'un ours est détruit, mettre à jour la population
    void OnDestroy()
    {
        bearPopulation--;
        deadBearCount++;
    }
}
