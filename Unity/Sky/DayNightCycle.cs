using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float dayDuration = 60f; // Durée d'un cycle complet en secondes
    [SerializeField] private Light directionalLight;

    private float timeOfDay = 0f; // Temps actuel dans le cycle

    void Update()
    {
        // Avancer le temps en fonction du temps écoulé
        timeOfDay += (Time.deltaTime / dayDuration) * 360f;
        timeOfDay %= 360f; // Garde la valeur entre 0 et 360

        // Appliquer la rotation à la Directional Light
        directionalLight.transform.rotation = Quaternion.Euler(timeOfDay, 170f, 0f);
    }
}
