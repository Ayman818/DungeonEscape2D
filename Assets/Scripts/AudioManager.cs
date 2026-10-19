
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip foodSound;
    [SerializeField] private AudioClip wallBreakSound;
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip enemyDeathSound;
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private AudioClip gameOverSound;

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)]
    private float musicVolume = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float sfxVolume = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Find or create independent AudioSources.
        AudioSource[] sources = GetComponents<AudioSource>();

        if (musicSource == null)
        {
            musicSource = sources.Length > 0
                ? sources[0]
                : gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null || sfxSource == musicSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 1f;
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    public void PlayWalk()
    {
        PlaySound(walkSound);
    }

    public void PlayAttack()
    {
        PlaySound(attackSound);
    }

    public void PlayFood()
    {
        PlaySound(foodSound);
    }

    public void PlayWallBreak()
    {
        PlaySound(wallBreakSound);
    }

    public void PlayEnemyHit()
    {
        PlaySound(enemyHitSound);
    }

    public void PlayEnemyDeath()
    {
        PlaySound(enemyDeathSound);
    }

    public void PlayLevelComplete()
    {
        PlaySound(levelCompleteSound);
    }

    public void PlayGameOver()
    {
        PlaySound(gameOverSound);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
