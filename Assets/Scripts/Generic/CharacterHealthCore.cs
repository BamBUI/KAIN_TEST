using System;
using UnityEngine;

namespace Assets.Scripts.Generic
{
    /// <summary>
    /// Универсальный модуль здоровья для всех персонажей (игрок, враги, NPC)
    /// </summary>
    public class CharacterHealthCore
    {
        // ━━━ СОБЫТИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public event Action<float> OnHurtStart;      // Получение урона
        public event Action OnHurtEnd;               // Конец состояния боли
        public event Action OnFaintStart;            // Начало обморока
        public event Action OnFaintEnd;              // Конец обморока
        public event Action OnDeath;                 // Смерть (начало анимации)
        public event Action OnDeathFinished;         // Конец анимации смерти (для уничтожения)

        // ━━━ ПАРАМЕТРЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private readonly float maxHealth;
        private readonly float hurtDuration;
        private readonly float faintDuration;
        private readonly float deathDuration;

        // ━━━ СОСТОЯНИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private float currentHealth;
        private bool isHurt = false;
        private bool isFainting = false;
        private bool isDead = false;

        // ━━━ ТАЙМЕРЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private float hurtTimer = 0f;
        private float faintTimer = 0f;
        private float deathTimer = 0f;

        // ━━━ КОНСТРУКТОР ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// Инициализация модуля здоровья
        /// </summary>
        /// <param name="maxHealth">Максимальное здоровье</param>
        /// <param name="hurtDuration">Длительность состояния боли (сек)</param>
        /// <param name="faintDuration">Длительность обморока (сек)</param>
        /// <param name="deathDuration">Длительность анимации смерти (сек)</param>
        /// <param name="hasDeathFinishedEvent">Нужно ли событие OnDeathFinished (true для игрока, false для врагов)</param>
        public CharacterHealthCore(
            float maxHealth,
            float hurtDuration = 0.5f,
            float faintDuration = 0.5f,
            float deathDuration = 1.0f,
            bool hasDeathFinishedEvent = true)
        {
            // Валидация
            if (!float.IsFinite(maxHealth) || maxHealth <= 0f)
                throw new ArgumentException("Max health must be positive and finite", nameof(maxHealth));
            if (!float.IsFinite(hurtDuration) || hurtDuration < 0f)
                throw new ArgumentException("Hurt duration must be non-negative and finite", nameof(hurtDuration));
            if (!float.IsFinite(faintDuration) || faintDuration < 0f)
                throw new ArgumentException("Faint duration must be non-negative and finite", nameof(faintDuration));
            if (!float.IsFinite(deathDuration) || deathDuration < 0f)
                throw new ArgumentException("Death duration must be non-negative and finite", nameof(deathDuration));

            this.maxHealth = maxHealth;
            this.hurtDuration = hurtDuration;
            this.faintDuration = faintDuration;
            this.deathDuration = deathDuration;
            currentHealth = maxHealth;

            // Если событие смерти не нужно — явно обнуляем
            if (!hasDeathFinishedEvent)
            {
                OnDeathFinished = null;
            }
        }

        // ━━━ ОБНОВЛЕНИЕ ТАЙМЕРОВ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// Обновление таймеров состояний (вызывать из Controller.Update)
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!float.IsFinite(deltaTime) || deltaTime <= 0f) return;

            // ━━━ СОСТОЯНИЕ БОЛИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            if (isHurt && hurtTimer > 0f)
            {
                hurtTimer -= deltaTime;
                if (hurtTimer <= 0f)
                {
                    isHurt = false;
                    OnHurtEnd?.Invoke();

                    // Если здоровье <= 0 — переходим в обморок
                    if (currentHealth <= 0f && !isFainting && !isDead)
                    {
                        StartFaintProcess();
                    }
                }
            }

            // ━━━ СОСТОЯНИЕ ОБМОРОКА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            if (isFainting && faintTimer > 0f)
            {
                faintTimer -= deltaTime;
                if (faintTimer <= 0f)
                {
                    isFainting = false;
                    OnFaintEnd?.Invoke();

                    // Переход в смерть
                    isDead = true;
                    deathTimer = deathDuration;
                    OnDeath?.Invoke();
                }
            }

            // ━━━ СОСТОЯНИЕ СМЕРТИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            if (isDead && deathTimer > 0f)
            {
                deathTimer -= deltaTime;
                if (deathTimer <= 0f)
                {
                    OnDeathFinished?.Invoke();
                }
            }
        }

        // ━━━ ПОЛУЧЕНИЕ УРОНА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// Получить урон
        /// </summary>
        /// <returns>True если урон применён успешно</returns>
        public bool TakeDamage(float damage)
        {
            if (!float.IsFinite(damage) || damage <= 0f) return false;
            if (isDead || isFainting) return false;

            currentHealth -= damage;

            // Если урон смертельный и мы ещё не в состоянии боли — запускаем процесс смерти
            if (currentHealth <= 0f && !isDead && !isFainting)
            {
                if (!isHurt)
                {
                    StartFaintProcess();
                }
                // else: если уже в боли, процесс начнётся в OnHurtEnd
                return true;
            }

            // Обычный урон (не смертельный)
            if (!isHurt && !isFainting)
            {
                isHurt = true;
                hurtTimer = hurtDuration;
                OnHurtStart?.Invoke(damage);
            }

            return true;
        }

        // ━━━ ВСПОМОГАТЕЛЬНЫЙ МЕТОД ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void StartFaintProcess()
        {
            isHurt = false;
            hurtTimer = 0f;
            isFainting = true;
            faintTimer = faintDuration;
            OnFaintStart?.Invoke();
        }

        // ━━━ ПУБЛИЧНЫЕ МЕТОДЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public bool IsDead() => isDead;
        public bool IsHurt() => isHurt;
        public bool IsFainting() => isFainting;
        public bool IsInvincible() => isHurt || isFainting;
        public float GetCurrentHealth() => Mathf.Max(0f, currentHealth);
        public float GetMaxHealth() => maxHealth;
        public float GetHealthPercentage() => currentHealth / maxHealth;

        // ━━━ СБРОС (для респавна/тестов) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public void Reset()
        {
            currentHealth = maxHealth;
            isHurt = false;
            isFainting = false;
            isDead = false;
            hurtTimer = 0f;
            faintTimer = 0f;
            deathTimer = 0f;
        }
    }
}