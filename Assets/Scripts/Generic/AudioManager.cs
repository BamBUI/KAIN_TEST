using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource; // Для звуков эффектов

    [Header("Audio Clips - General")]
    public AudioClip backgroundMusic;

    [Header("Audio Clips - Player")]
    public AudioClip[] kainHurtSounds; // Массив звуков боли Каина
    public AudioClip kainDeathSound;

    [Header("Audio Clips - Enemies")]
    public AudioClip[] enemyHurtSounds; // Массив звуков боли врага
    public AudioClip enemyDeathSound;

    [Header("Audio Clips - Actions")]
    public AudioClip[] swordSlashSounds; // Массив звуков удара мечом

    // Ссылка на единственный экземпляр (Singleton Pattern)
    private static AudioManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Опционально: сохранить при смене сцен
        }
        else
        {
            Destroy(gameObject); // Уничтожить дубликат
            return;
        }
    }

    private void Start()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true; // Убедитесь, что музыка зациклена
            musicSource.Play();
        }
    }

    // --- МЕТОДЫ ДЛЯ ВОСПРОИЗВЕДЕНИЯ ЗВУКОВ ---

    // Воспроизвести случайный звук из массива
    private void PlayRandomClipFromArray(AudioClip[] clips, AudioSource source)
    {
        if (clips != null && clips.Length > 0 && source != null)
        {
            int randomIndex = Random.Range(0, clips.Length);
            if (clips[randomIndex] != null) // Проверка на null на всякий случай
            {
                source.PlayOneShot(clips[randomIndex]);
            }
        }
        else
        {
            Debug.LogWarning("Array is null or empty, or AudioSource is null.");
        }
    }

    // --- Публичные методы для вызова из других скриптов ---

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