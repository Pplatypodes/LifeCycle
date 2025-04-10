using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource ambienceSource;

    public AudioClip dayMusic;
    public AudioClip nightMusic;
    public AudioClip rainAmbience;
    public AudioClip fireAmbience;

    private bool isDay;
    private bool isRaining;
    private bool isFire;

    public void UpdateAudio(bool _isDay, bool _isRaining, bool _isFire)
    {
        // Musique jour/nuit
        if (_isDay != isDay)
        {
            isDay = _isDay;
            musicSource.clip = isDay ? dayMusic : nightMusic;
            musicSource.Play();
        }

        // Ambiances environnementales
        AudioClip newAmbience = null;
        if (_isFire)
            newAmbience = fireAmbience;
        else if (_isRaining)
            newAmbience = rainAmbience;

        if (newAmbience != ambienceSource.clip)
        {
            ambienceSource.clip = newAmbience;
            if (newAmbience != null) ambienceSource.Play();
            else ambienceSource.Stop();
        }

        isRaining = _isRaining;
        isFire = _isFire;
    }
}
