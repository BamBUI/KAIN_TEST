using UnityEngine;
using Assets.Scripts.Generic;

public class SceneMusicController : MonoBehaviour
{
    [Header("Scene Music")]
    [SerializeField] private AudioClip sceneMusic; // ← Назначается в каждой сцене отдельно!

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1f; // ← Плавный переход

    private void Start()
    {
        // Сменить музыку при загрузке сцены
        if (AudioManager.Instance != null && sceneMusic != null)
        {
            AudioManager.Instance.ChangeMusic(sceneMusic, fadeDuration);
        }
        else if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneMusicController] AudioManager not found!");
        }
    }
}