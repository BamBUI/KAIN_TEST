using System;
using UnityEngine;

namespace Assets.Scripts.PlayerScripts
{
    public class PlayerHealthCore
    {
        public event Action<float> OnHurt;
        public event Action OnHurtEnd;
        public event Action OnFaintStart;
        public event Action OnFaintEnd;
        public event Action OnDeath;
        public event Action OnDeathFinished;

        private readonly float maxHealth;
        private float currentHealth;
        private bool isDead = false;
        private bool isHurt = false;
        private bool isFainting = false;

        private readonly float hurtDuration = 0.5f;
        private readonly float faintDuration = 0.5f; // Убедитесь, что это соответствует длительности вашего клипа Faint.anim
        private readonly float deathDuration = 1f; // Убедитесь, что это соответствует длительности вашего клипа Death.anim

        private float hurtTimer = 0f;
        private float faintTimer = 0f;
        private float deathTimer = 0f;

        public PlayerHealthCore(float maxHealth, float invincibilityDuration)
        {
            this.maxHealth = maxHealth;
            currentHealth = maxHealth;
        }

        public void Update(float deltaTime)
        {
            // --- Обработка состояния Hurt ---
            if (isHurt && hurtTimer > 0f)
            {
                hurtTimer -= deltaTime;
                if (hurtTimer <= 0f)
                {
                    isHurt = false;
                    Debug.Log("[PlayerHealthCore] Hurt ended, calling OnHurtEnd."); // <-- НОВЫЙ ЛОГ
                    OnHurtEnd?.Invoke();

                    // --- КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ ---
                    // Если здоровье <= 0 и мы вышли из Hurt, начать Faint
                    if (currentHealth <= 0f && !isDead && !isFainting) // <-- Добавлены проверки
                    {
                        Debug.Log("[PlayerHealthCore] Health <= 0 after Hurt ended, starting Faint process."); // <-- НОВЫЙ ЛОГ
                        StartFaintProcess();
                    }
                }
            }

            // --- Обработка состояния Faint ---
            if (isFainting && faintTimer > 0f) // <-- Проверка, если в состоянии Faint и таймер > 0
            {
                faintTimer -= deltaTime;
                Debug.Log($"[PlayerHealthCore] Faint Timer: {faintTimer:F3}, Target: {faintDuration:F3}"); // <-- НОВЫЙ ЛОГ - показывает тикание таймера
                if (faintTimer <= 0f)
                {
                    isFainting = false;
                    Debug.Log("[PlayerHealthCore] Faint ended, calling OnFaintEnd, transitioning to Death."); // <-- НОВЫЙ ЛОГ
                    OnFaintEnd?.Invoke();

                    // --- ПЕРЕХОД К СМЕРТИ ПОСЛЕ ОБМОРОКА ---
                    isDead = true;
                    deathTimer = deathDuration;
                    Debug.Log($"[PlayerHealthCore] Setting IsDead=true, deathTimer={deathTimer:F3}, calling OnDeath."); // <-- НОВЫЙ ЛОГ
                    OnDeath?.Invoke(); // <-- Это должно запустить анимацию Death
                }
            }

            // --- Обработка состояния Death (ТАЙМЕР УНИЧТОЖЕНИЯ) ---
            if (isDead && deathTimer > 0f) // <-- Проверка, если в состоянии Death и таймер > 0
            {
                deathTimer -= deltaTime;
                Debug.Log($"[PlayerHealthCore] Death Timer: {deathTimer:F3}, Target: {deathDuration:F3}"); // <-- ВАШ СТАРЫЙ ЛОГ - показывает тикание таймера смерти
                if (deathTimer <= 0f)
                {
                    Debug.Log("[PlayerHealthCore] Death Timer finished, calling OnDeathFinished."); // <-- НОВЫЙ ЛОГ
                    OnDeathFinished?.Invoke();
                    Debug.Log("[PlayerHealthCore] OnDeathFinished invoked!"); // <-- ВАШ СТАРЫЙ ЛОГ
                }
            }
        }

        public bool TakeDamage(float damage)
        {
            if (!float.IsFinite(damage) || damage <= 0f) return false;
            if (isDead || isFainting) return false; // Блокировка урона во время смерти или обморока

            currentHealth -= damage;
            Debug.Log($"[PlayerHealthCore] Took damage: {damage}, currentHealth: {currentHealth:F2}"); // <-- НОВЫЙ ЛОГ - отладка урона

            if (currentHealth <= 0f && !isDead && !isFainting) // <-- Основная проверка на смертельный урон
            {
                Debug.Log("[PlayerHealthCore] Health <= 0, checking current state for Faint start."); // <-- НОВЫЙ ЛОГ
                // НЕ ЗАВИСИТ от isHurt. В любом случае, если здоровье <= 0, начать процесс смерти.
                // Если isHurt, процесс начнётся в OnHurtEnd.
                // Если не isHurt, процесс начнётся сразу.
                if (!isHurt) // <-- Если не в состоянии боли, начать faint немедленно
                {
                    Debug.Log("[PlayerHealthCore] Not in Hurt state, starting Faint process immediately."); // <-- НОВЫЙ ЛОГ
                    StartFaintProcess();
                }
                // else if (isHurt) -> ничего не делаем. Пусть OnHurtEnd вызывает StartFaintProcess().
                return true;
            }

            // --- ОБЫЧНЫЙ УРОН ---
            if (!isHurt && !isFainting) // <-- Блокировка обычного урона во время боли или обморока
            {
                isHurt = true;
                hurtTimer = hurtDuration;
                Debug.Log($"[PlayerHealthCore] Entering Hurt state, timer={hurtTimer:F3}."); // <-- НОВЫЙ ЛОГ
                OnHurt?.Invoke(damage);
            }
            return true;
        }

        // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ НАЧАЛА ОБМОРОКА ---
        private void StartFaintProcess()
        {
            isHurt = false; // <-- Сбросить боль
            hurtTimer = 0f; // <-- Остановить таймер боли
            // OnHurtEnd?.Invoke(); // <-- НЕ ВЫЗЫВАЕМ, так как может быть вызвано в Update
            isFainting = true; // <-- Установить состояние обморока
            faintTimer = faintDuration; // <-- Запустить таймер обморока
            Debug.Log($"[PlayerHealthCore] Starting Faint process, timer={faintTimer:F3}, calling OnFaintStart."); // <-- НОВЫЙ ЛОГ
            OnFaintStart?.Invoke(); // <-- Сигнал о начале обморока (для анимации)
            // isDead устанавливается в Update() после истечения faintTimer
        }


        public bool IsDead() => isDead;
        public bool IsInvincible() => isHurt || isFainting;
        public bool IsFainting() => isFainting;
        public float GetCurrentHealth() => Mathf.Max(0f, currentHealth);
        public float GetMaxHealth() => maxHealth;

        public void Reset()
        {
            currentHealth = maxHealth;
            isDead = false;
            isHurt = false;
            isFainting = false;
            hurtTimer = 0f;
            faintTimer = 0f;
            deathTimer = 0f;
        }
    }
}