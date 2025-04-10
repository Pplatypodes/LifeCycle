using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float dayDuration = 120f; // Durée d'un cycle complet (en secondes)
    [SerializeField] private Light directionalLight;  // Soleil
    [SerializeField] private Material skyboxMaterial; // Skybox dynamique
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource dayMusicSource;
    [SerializeField] private AudioSource nightMusicSource;
    
    private float timeOfDay = 0f;
    private bool isDaytime = true;
    
    private void Update()
    {
        // Mise à jour du cycle solaire
        timeOfDay += (Time.deltaTime / dayDuration) * 360f;
        timeOfDay %= 360f;
    
        // Applique la rotation à la Directional Light (soleil)
        directionalLight.transform.rotation = Quaternion.Euler(timeOfDay, 170f, 0f);
    
        // Met à jour la direction dans la Skybox pour simuler l'éclairage
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetVector("_MainLightDirection", directionalLight.transform.forward);
        }
    
        // Gérer le son jour/nuit
        UpdateDayNightMusic();
    }
    
    private void UpdateDayNightMusic()
    {
        // Déterminer si on est dans la phase "jour"
        bool currentlyDay = timeOfDay >= 90f && timeOfDay <= 270f;

        if (currentlyDay && !isDaytime)
        {
            // Passage à la journée
            isDaytime = true;
            if (nightMusicSource.isPlaying) nightMusicSource.Stop();
            if (!dayMusicSource.isPlaying) dayMusicSource.Play();
        }
        else if (!currentlyDay && isDaytime)
        {
            // Passage à la nuit
            isDaytime = false;
            if (dayMusicSource.isPlaying) dayMusicSource.Stop();
            if (!nightMusicSource.isPlaying) nightMusicSource.Play();
        }
    }
}
