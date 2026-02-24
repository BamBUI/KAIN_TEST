using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    [Header("Audio Clips - General")]
    public AudioClip backgroundMusic;

    [Header("Audio Clips - UI")]
    public AudioClip[] buttonClickSounds;

    [Header("Audio Clips - Player")]
    public AudioClip[] kainHurtSounds;
    public AudioClip kainDeathSound;

    [Header("Audio Clips - Enemies")]
    public AudioClip[] enemyHurtSounds;
    public AudioClip enemyDeathSound;

    [Header("Audio Clips - Actions")]
    public AudioClip[] swordSlashSounds;

    // ━━━ SINGLETON ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private static AudioManager instance;
    public static AudioManager Instance => instance;

    // ━━━ ИНИЦИАЛИЗАЦИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true; // ✅ ЗАЦИКЛИВАЕМ музыку
            musicSource.Play();
        }
    }

    // ━━━ НОВЫЙ МЕТОД: Смена музыки для конкретной сцены ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void ChangeMusic(AudioClip newMusic, float fadeDuration = 0f)
    {
        if (musicSource == null || newMusic == null)
        {
            Debug.LogWarning("[AudioManager] Cannot change music: source or clip is null");
            return;
        }

        // Если тот же клип — не меняем
        if (musicSource.clip == newMusic)
        {
            Debug.Log("[AudioManager] Music already playing, skipping change");
            return;
        }

        if (fadeDuration > 0f)
        {
            StartCoroutine(FadeAndChangeMusic(newMusic, fadeDuration));
        }
        else
        {
            // Мгновенная смена
            musicSource.Stop();
            musicSource.clip = newMusic;
            musicSource.loop = true; // ✅ ЗАЦИКЛИВАЕМ новую музыку
            musicSource.Play();
            Debug.Log($"[AudioManager] Music changed to: {newMusic.name}");
        }
    }

    // ━━━ COROUTINE: Плавный переход между музыкой ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private IEnumerator FadeAndChangeMusic(AudioClip newMusic, float duration)
    {
        // Fade out
        float startVolume = musicSource.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        // Смена трека
        musicSource.Stop();
        musicSource.clip = newMusic;
        musicSource.loop = true; // ✅ ЗАЦИКЛИВАЕМ новую музыку
        musicSource.Play();

        // Fade in
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, startVolume, t / duration);
            yield return null;
        }

        musicSource.volume = startVolume;
        Debug.Log($"[AudioManager] Music changed with fade to: {newMusic.name}");
    }

    // ━━━ МЕТОДЫ ДЛЯ ВОСПРОИЗВЕДЕНИЯ ЗВУКОВ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private void PlayRandomClipFromArray(AudioClip[] clips, AudioSource source)
    {
        if (clips != null && clips.Length > 0 && source != null)
        {
            int randomIndex = Random.Range(0, clips.Length);
            if (clips[randomIndex] != null)
            {
                source.PlayOneShot(clips[randomIndex]);
            }
        }
        else
        {
            Debug.LogWarning("Array is null or empty, or AudioSource is null.");
        }
    }

    // ━━━ ПУБЛИЧНЫЕ МЕТОДЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void PlayButtonClickSound()
    {
        PlayRandomClipFromArray(buttonClickSounds, sfxSource);
    }

    public void PlayKainHurtSound()
    {
        PlayRandomClipFromArray(kainHurtSounds, sfxSource);
    }

    public void PlayKainDeathSound()
    {
        if (kainDeathSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(kainDeathSound);
        }
    }

    public void PlayEnemyHurtSound()
    {
        PlayRandomClipFromArray(enemyHurtSounds, sfxSource);
    }

    public void PlayEnemyDeathSound()
    {
        if (enemyDeathSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(enemyDeathSound);
        }
    }

    public void PlaySwordSlashSound()
    {
        PlayRandomClipFromArray(swordSlashSounds, sfxSource);
    }
}