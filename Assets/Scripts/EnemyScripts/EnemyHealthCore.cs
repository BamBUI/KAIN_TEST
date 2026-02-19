using System;
using UnityEngine;

namespace Assets.Scripts.EnemyScripts
{
    public class EnemyHealthCore
    {
        public event Action OnHurtStart;
        public event Action OnHurtEnd;
        public event Action OnFaintStart;
        public event Action OnFaintEnd;
        public event Action OnDeath;

        private readonly float maxHealth;
        private float currentHealth;
        private bool isHurt = false;
        private bool isFainting = false;
        private bool isDead = false;

        // ━━━ ТАЙМЕРЫ ДЛИТЕЛЬНОСТИ СОСТОЯНИЙ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private readonly float hurtDuration = 0.5f;   // 100 мс — анимация боли
        private readonly float faintDuration = 1.2f;  // 400 мс — анимация обморока
        private readonly float deathDuration = 2.0f;  // 2 сек — анимация смерти

        private float hurtTimer = 0f;
        private float faintTimer = 0f;
        private float deathTimer = 0f;

        public EnemyHealthCore(float maxHealth)
        {
            if (!float.IsFinite(maxHealth) || maxHealth <= 0f)
                throw new ArgumentException("Max health must be positive and finite", nameof(maxHealth));

            this.maxHealth = maxHealth;
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Обновление таймеров состояний (вызывается из EnemyController.Update)
        /// </summary>
        public void Update(float deltaTime)
        {
            // ━━━ СБРОС СОСТОЯНИЯ БОЛИ ПО ТАЙМЕРУ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            if (isHurt && hurtTimer > 0f)
            {
                hurtTimer -= deltaTime;
                if (hurtTimer <= 0f)
                {
                    isHurt = false;
                    OnHurtEnd?.Invoke();

                    // Если здоровье <= 0 — переходим в обморок ПОСЛЕ анимации боли
                    if (currentHealth <= 0f && !isFainting && !isDead)
                    {
                        isFainting = true;
                        faintTimer = faintDuration;
                        OnFaintStart?.Invoke();
                    }
                }
            }

            // ━━━ СБРОС СОСТОЯНИЯ ОБМОРОКА ПО ТАЙМЕРУ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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

            // ━━━ ОЖИДАНИЕ ЗАВЕРШЕНИЯ АНИМАЦИИ СМЕРТИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            if (isDead && deathTimer > 0f)
            {
                deathTimer -= deltaTime;
                // Уничтожение объекта происходит в EnemyController через OnDeathAnimationFinished
            }
        }

        /// <summary>
        /// Получение урона
        /// </summary>
        public bool TakeDamage(float damage)
        {
            if (!float.IsFinite(damage) || damage <= 0f) return false;
            if (isDead || isFainting) return false;

            currentHealth -= damage;

            // Устанавливаем состояние боли (даже при смертельном уроне — сначала болим!)
            if (!isHurt)
            {
                isHurt = true;
                hurtTimer = hurtDuration; // ← ЗАПУСК ТАЙМЕРА БОЛИ
                OnHurtStart?.Invoke();
            }

            return true;
        }

        // ━━━ УДАЛЯЕМ СТАРЫЕ МЕТОДЫ (больше не нужны — управление через таймеры) ━━━━━━━━━━━━━━━━
        public void OnHurtAnimationFinished() { /* Пустой — больше не используется */ }
        public void OnFaintAnimationFinished() { /* Пустой — больше не используется */ }
        public void OnDeathAnimationFinished() { /* Пустой — больше не используется */ }

        public bool IsDead() => isDead;
        public bool IsHurt() => isHurt;
        public bool IsFainting() => isFainting;
        public float GetCurrentHealth() => Mathf.Max(0f, currentHealth);
        public float GetMaxHealth() => maxHealth;

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