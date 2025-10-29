using UnityEngine;
using System.Collections.Generic;

/// Singleton Audio Manager for handling all game sounds and music
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip playerShootSound;
    [SerializeField] private AudioClip enemyShootSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] private AudioClip enemyHitSound;

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Settings")]
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] private bool playMusicOnStart = true;

    // Pool of audio sources for overlapping sounds
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private const int POOL_SIZE = 18;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playMusicOnStart && backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    void InitializeAudioSources()
    {
        // Create SFX source if not assigned
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        // Create Music source if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        // Set volumes
        sfxSource.volume = sfxVolume;
        musicSource.volume = musicVolume;

        // Create audio source pool for overlapping sounds
        for (int i = 0; i < POOL_SIZE; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = sfxVolume;
            sfxPool.Add(source);
        }
    }

    #region Public Sound Methods


    public void PlayPlayerShoot()
    {
        PlaySFX(playerShootSound);
    }

    public void PlayEnemyShoot()
    {
        PlaySFX(enemyShootSound);
    }

    public void PlayExplosion()
    {
        PlaySFX(explosionSound);
    }

    public void PlayPlayerHit()
    {
        PlaySFX(playerHitSound);
    }

    public void PlayEnemyHit()
    {
        PlaySFX(enemyHitSound);
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        // Find available audio source from pool
        AudioSource availableSource = GetAvailableAudioSource();
        if (availableSource != null)
        {
            availableSource.PlayOneShot(clip, volumeScale * sfxVolume);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        musicSource.clip = clip;
        musicSource.loop = true; // Ensure loop is enabled
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }

        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
            {
                source.volume = sfxVolume;
            }
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    #endregion

    #region Private Helper Methods

    private AudioSource GetAvailableAudioSource()
    {
        // Find an audio source that's not currently playing
        foreach (AudioSource source in sfxPool)
        {
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        // If all are busy, use the first one (oldest sound gets cut off)
        return sfxPool.Count > 0 ? sfxPool[0] : sfxSource;
    }

    #endregion
}