using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DeerAI : MonoBehaviour
{
    private NavMeshAgent agent;
    public float visionRadius = 10f;
    public float moveSpeed = 5f;
    public float slowSpeedMultiplier = 0.5f;
    public float fleeDistance = 10f;
    private GameObject predator;
    private Animator anim;

    // Variables pour le système d'énergie
    public float maxEnergy = 100f;
    public float energyDepletionRate = 10f;
    public float energyRegenerationRate = 5f;
    private float currentEnergy;
    private bool isResting = false;
    public float lowEnergyThreshold = 20f;

    // Variables pour le système de faim
    public float maxHunger = 100f;
    private float currentHunger;
    public float hungerDepletionRate = 5f;
    public float extraHungerDepletionMultiplier = 2f;
    public float lowHungerThreshold = 40f;

    // Variables pour la consommation d'herbe
    public float grassDetectionRadius = 15f;
    public float consumptionRange = 2f;

    // Variables pour la reproduction
    public float reproductionCooldown = 45f;
    private float reproductionTimer = 0f;
    public float reproductionChance = 0.5f;
    public float reproductionRange = 2f;
    public GameObject deerPrefab;

    // Variables pour la croissance
    public bool isMature = true;
    public float growthDuration = 60f;
    [HideInInspector]
    public Vector3 fullGrownScale;
    public float babyScaleFactor = 0.6f;

    // Variables pour la maladie
    private bool isSick = false;
    private bool isImmune = false;
    public float sicknessDuration = 20f;
    public float baseSicknessChance = 0.01f;
    public int populationThreshold = 2000;
    public float populationSicknessIncrease = 0.0003F;
    public float deathChance = 0.5f;
    public float immunityChance = 0.3f;

    // Compteurs statiques pour la population et les décès cumulés
    private static int deerPopulation = 0;
    private static int deadDeerCount = 0;

    // Accesseurs publics pour la maladie et le nombre de cerfs morts
    public bool IsSick { get { return isSick; } }
    public static int DeadDeerCount { get { return deadDeerCount; } }

    /* Vérifie si l'agent NavMesh est valide et sur la NavMesh */
    private bool IsAgentValid()
    {
        return agent != null && agent.isOnNavMesh;
    }

    /* Initialisation du cerf : configure l'agent, l'énergie, la faim, etc. */
    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Vérifie la validité de l'agent avant de continuer.
        if (!IsAgentValid())
        {
            Debug.LogWarning($"{gameObject.name} is not on the NavMesh at startup!");
            Destroy(gameObject);
            return;
        }

        if (!isMature)
        {
            // Réduit la hauteur de base de l'agent pour un bébé.
            agent.baseOffset *= babyScaleFactor;
        }

        // Configure la vitesse et initialise énergie et faim.
        agent.speed = moveSpeed;
        currentEnergy = maxEnergy;
        currentHunger = maxHunger;
        deerPopulation++;

        fullGrownScale = transform.localScale;
        if (!isMature)
        {
            // Met à l'échelle le bébé et démarre sa croissance.
            transform.localScale = fullGrownScale * babyScaleFactor;
            StartCoroutine(Grow());
        }

        // Planifie des destinations aléatoires régulièrement.
        InvokeRepeating("SetRandomDestination", 2f, 5f);
    }

    /* Update : Gère les actions du cerf (faim, énergie, fuite, reproduction, etc.) */
    void Update()
    {
        // Sortie précoce si l'agent n'est plus valide.
        if (!IsAgentValid())
            return;

        // Décrémente la faim en fonction du mouvement.
        float extraHunger = extraHungerDepletionMultiplier * (agent.velocity.magnitude / moveSpeed);
        currentHunger -= (hungerDepletionRate + extraHunger) * Time.deltaTime;
        currentHunger = Mathf.Max(currentHunger, 0f);

        // Vérifie la maladie si le cerf n'est pas déjà malade ou immunisé.
        if (!isSick && !isImmune)
        {
            float infectionChance = baseSicknessChance;
            if (deerPopulation > populationThreshold)
            {
                infectionChance += (deerPopulation - populationThreshold) * populationSicknessIncrease;
            }
            if (Random.value < infectionChance * Time.deltaTime)
            {
                BecomeSick();
            }
        }

        // Ajuste la vitesse en cas de maladie ou de fuite.
        if (isSick)
        {
            agent.speed = moveSpeed * slowSpeedMultiplier;
        }
        else if (predator != null)
        {
            Flee();
            if (currentEnergy <= 0 || currentHunger <= 0)
            {
                agent.speed = moveSpeed * slowSpeedMultiplier;
            }
            else
            {
                agent.speed = moveSpeed;
            }
        }
        else
        {
            // Cherche de l'herbe si la faim est faible.
            if (currentHunger < lowHungerThreshold)
            {
                SeekGrass();
            }
            // Se repose si l'énergie est faible.
            else if (currentEnergy < lowEnergyThreshold)
            {
                if (!isResting)
                {
                    StartCoroutine(Rest());
                }
            }
            else
            {
                // Régénère l'énergie jusqu'au maximum.
                if (currentEnergy < maxEnergy)
                {
                    currentEnergy += energyRegenerationRate * Time.deltaTime;
                    currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
                }
                agent.speed = moveSpeed;
            }
        }

        // Détecte les prédateurs si le cerf ne se repose pas.
        if (!isResting && predator == null)
        {
            DetectPredators();
        }

        // Gère la reproduction pour les cerfs matures et disponibles.
        if (isMature && !isResting && predator == null)
        {
            reproductionTimer += Time.deltaTime;
            if (reproductionTimer >= reproductionCooldown)
            {
                Collider[] nearby = Physics.OverlapSphere(transform.position, reproductionRange);
                bool partnerFound = false;
                foreach (Collider col in nearby)
                {
                    DeerAI otherDeer = col.GetComponent<DeerAI>();
                    if (otherDeer != null && col.gameObject != gameObject && otherDeer.isMature)
                    {
                        partnerFound = true;
                        break;
                    }
                }

                if (partnerFound)
                {
                    CheckForReproduction();
                }
                else
                {
                    SeekPartner();
                }
            }
        }

        // Ajuste la vitesse de l'animation en fonction de la vitesse de l'agent.
        anim.speed = Mathf.Clamp(agent.velocity.magnitude, 0.1f, 2f);
    }

    /* Définit une destination aléatoire sur la NavMesh */
    void SetRandomDestination()
    {
        if (!IsAgentValid())
        {
            //Debug.LogWarning($"{gameObject.name} is not on the NavMesh in SetRandomDestination!");
            Destroy(gameObject);
            return;
        }

        if (predator == null && !isResting && (!isMature || reproductionTimer < reproductionCooldown))
        {
            Vector3 randomPos = RandomNavMeshPosition(10f);
            agent.SetDestination(randomPos);
        }
    }

    /* Détecte les prédateurs dans le rayon de vision */
    void DetectPredators()
    {
        Collider[] predators = Physics.OverlapSphere(transform.position, visionRadius);
        foreach (var obj in predators)
        {
            if (obj.CompareTag("Bear"))
            {
                predator = obj.gameObject;
                break;
            }
        }
    }

    /* Fuite face au prédateur, en se dirigeant dans la direction opposée */
    void Flee()
    {
        if (!IsAgentValid())
            return;

        if (predator != null && !isResting)
        {
            float distanceToPredator = Vector3.Distance(transform.position, predator.transform.position);
            float fleeSpeed = moveSpeed;
            if (distanceToPredator < 5f)
            {
                fleeSpeed = moveSpeed * 1.5f;
            }
            agent.speed = fleeSpeed;

            Vector3 fleeDirection = (transform.position - predator.transform.position).normalized;
            float effectiveFleeDistance = distanceToPredator < 5f ? fleeDistance * 1.5f : fleeDistance;
            Vector3 fleePosition = transform.position + fleeDirection * effectiveFleeDistance;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePosition, out hit, effectiveFleeDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            // Décrémente l'énergie et la faim lors de la fuite.
            currentEnergy -= energyDepletionRate * Time.deltaTime;
            currentHunger -= hungerDepletionRate * Time.deltaTime;

            if (currentEnergy <= 0)
            {
                currentEnergy = 0;
                StartCoroutine(Rest());
            }
        }
    }

    /* Permet au cerf de se reposer pour régénérer son énergie */
    IEnumerator Rest()
    {
        if (!IsAgentValid())
            yield break;

        isResting = true;
        agent.isStopped = true;
        yield return new WaitForSeconds(10f);
        currentEnergy = maxEnergy;
        isResting = false;
        if (IsAgentValid())
        {
            agent.isStopped = false;
        }
        predator = null;
    }

    /* Retourne une position aléatoire sur la NavMesh dans le rayon indiqué */
    Vector3 RandomNavMeshPosition(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return transform.position;
    }

    /* Cherche de l'herbe à proximité pour réduire la faim */
    void SeekGrass()
    {
        if (!IsAgentValid())
            return;

        Collider[] grassColliders = Physics.OverlapSphere(transform.position, grassDetectionRadius);
        GameObject closestGrass = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in grassColliders)
        {
            if (col.CompareTag("Grass"))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestGrass = col.gameObject;
                }
            }
        }

        if (closestGrass != null)
        {
            agent.SetDestination(closestGrass.transform.position);
            if (minDistance <= consumptionRange)
            {
                ConsumeGrass(closestGrass);
            }
        }
    }

    /* Consomme l'herbe, réinitialise l'énergie et la faim, et déclenche la régénération de l'herbe */
    void ConsumeGrass(GameObject grass)
    {
        currentEnergy = maxEnergy;
        currentHunger = maxHunger;
        
        Grass grassComponent = grass.GetComponent<Grass>();
        if (grassComponent != null)
        {
            grassComponent.Eat();
        }
        else
        {
            Destroy(grass);
        }
    }

    /* Vérifie la reproduction en détectant un partenaire potentiel */
    void CheckForReproduction()
    {
        if (!IsAgentValid())
            return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, reproductionRange);
        foreach (var col in colliders)
        {
            DeerAI otherDeer = col.GetComponent<DeerAI>();
            if (otherDeer != null && col.gameObject != gameObject && otherDeer.isMature)
            {
                if (Random.value <= reproductionChance)
                {
                    Reproduce();
                }
                reproductionTimer = 0f;
                break;
            }
        }
    }

    /* Cherche un partenaire potentiel en se dirigeant vers le cerf le plus proche */
    void SeekPartner()
    {
        if (!IsAgentValid())
            return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, visionRadius);
        GameObject closestPartner = null;
        float minDistance = Mathf.Infinity;
        foreach (var col in colliders)
        {
            DeerAI otherDeer = col.GetComponent<DeerAI>();
            if (otherDeer != null && col.gameObject != gameObject && otherDeer.isMature)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPartner = col.gameObject;
                }
            }
        }
        if (closestPartner != null)
        {
            agent.SetDestination(closestPartner.transform.position);
        }
    }

    /* Procède à la reproduction en instanciant un bébé cerf */
    void Reproduce()
    {
        GameObject baby = Instantiate(deerPrefab, transform.position, Quaternion.identity);
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(baby.transform.position, out hit, 1f, NavMesh.AllAreas))
        {
            Destroy(baby);
            return;
        }

        DeerAI babyDeer = baby.GetComponent<DeerAI>();
        if (babyDeer != null)
        {
            babyDeer.isMature = false;
            babyDeer.transform.localScale = babyDeer.fullGrownScale * babyScaleFactor;
            StartCoroutine(babyDeer.Grow());

            // Si ce cerf est malade, le bébé devient immunisé.
            if (isSick)
            {
                babyDeer.isImmune = true;
            }
        }
    }

    /* Fait croître le cerf jusqu'à sa taille complète */
    public IEnumerator Grow()
    {
        float elapsedTime = 0f;
        Vector3 startingScale = transform.localScale;
        while (elapsedTime < growthDuration)
        {
            if (this == null)
            {
                yield break;
            }
            transform.localScale = Vector3.Lerp(startingScale, fullGrownScale, elapsedTime / growthDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (this != null)
        {
            transform.localScale = fullGrownScale;
            isMature = true;
        }
    }

    /* Rend le cerf malade et démarre le processus de progression de la maladie */
    void BecomeSick()
    {
        if (!isSick && !isImmune)
        {
            isSick = true;
            StartCoroutine(SicknessProgression());
        }
    }

    /* Gère la progression de la maladie et détermine la mort ou l'immunité */
    IEnumerator SicknessProgression()
    {
        yield return new WaitForSeconds(sicknessDuration);

        if (Random.value < deathChance)
        {
            deadDeerCount++;
            deerPopulation--;
            Destroy(gameObject);
        }
        else
        {
            isSick = false;
            if (Random.value < immunityChance)
            {
                isImmune = true;
            }
        }
    }

    /* À la destruction, met à jour la population */
    void OnDestroy()
    {
        deerPopulation = Mathf.Max(0, deerPopulation - 1);
    }
}
