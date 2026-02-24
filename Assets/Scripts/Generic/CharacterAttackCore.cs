using System;
using UnityEngine;

namespace Assets.Scripts.Generic
{
    /// <summary>
    /// Универсальный модуль атаки для всех персонажей (игрок, враги, NPC)
    /// </summary>
    public class CharacterAttackCore
    {
        // ━━━ СОБЫТИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // public event Action OnAttackStart; // - старая версия без параметров (не совместима с AnimatorCore)
        public event Action<Vector2> OnAttackStart; // + новая версия с параметром direction (для анимации атаки)
        public event Action OnHitboxEnable;
        public event Action OnHitboxDisable;
        public event Action OnAttackEnd;

        // ━━━ ПАРАМЕТРЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private readonly float hitboxDuration;
        private readonly float attackDuration;
        private readonly float postAttackDelay;

        // ━━━ СОСТОЯНИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private bool isAttacking = false;
        private Vector2 attackDirection = Vector2.down;
        private float attackTimer;
        private float hitboxTimer;

        // ━━━ КОНСТРУКТОР ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// Инициализация модуля атаки
        /// </summary>
        /// <param name="hitboxDuration">Длительность активной фазы удара (хитбокс активен)</param>
        /// <param name="attackDuration">Общая длительность анимации удара</param>
        /// <param name="postAttackDelay">Пауза после удара (восстановление)</param>
        public CharacterAttackCore(
            float hitboxDuration,
            float attackDuration,
            float postAttackDelay)
        {
            // Валидация входных параметров
            if (!float.IsFinite(hitboxDuration) || hitboxDuration < 0f)
                throw new ArgumentException("Hitbox duration must be non-negative and finite", nameof(hitboxDuration));
            if (!float.IsFinite(attackDuration) || attackDuration <= 0f)
                throw new ArgumentException("Attack duration must be positive and finite", nameof(attackDuration));
            if (!float.IsFinite(postAttackDelay) || postAttackDelay < 0f)
                throw new ArgumentException("Post-attack delay must be non-negative and finite", nameof(postAttackDelay));

            // Валидация логических соотношений
            if (hitboxDuration > attackDuration)
            {
                Debug.LogWarning($"[CharacterAttackCore] Hitbox duration ({hitboxDuration}) exceeds attack duration ({attackDuration}). Clamping to attack duration.");
                hitboxDuration = attackDuration;
            }

            this.hitboxDuration = hitboxDuration;
            this.attackDuration = attackDuration;
            this.postAttackDelay = postAttackDelay;
        }

        // ━━━ НАЧАЛО АТАКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// Начать атаку в заданном направлении
        /// </summary>
        /// <returns>True если атака успешно запущена</returns>
        public bool StartAttack(Vector2 direction)
        {
            if (isAttacking) return false;

            isAttacking = true;

            // Защита от нулевого вектора
            attackDirection = direction.sqrMagnitude > 0.01f
                ? direction.normalized
                : Vector2.down;

            attackTimer = attackDuration + postAttackDelay;
            hitboxTimer = hitboxDuration;

            // OnAttackStart?.Invoke(); // - старая версия без параметров
            OnAttackStart?.Invoke(attackDirection); // + передаём направление атаки для анимации
            OnHitboxEnable?.Invoke();
            return true;
        }

        // ━━━ ОБНОВЛЕНИЕ ТАЙМЕРОВ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// Обновление таймеров атаки (вызывать из Controller.Update)
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!isAttacking) return;

            // Защита от некорректного deltaTime
            if (!float.IsFinite(deltaTime) || deltaTime <= 0f) return;

            // Сохраняем состояние ДО обновления
            bool wasHitboxActive = hitboxTimer > 0f;
            bool wasAttacking = attackTimer > 0f;

            // Обновляем таймеры
            attackTimer -= deltaTime;
            hitboxTimer -= deltaTime;

            // Деактивация хитбокса (надёжная проверка перехода состояния)
            bool isHitboxActive = hitboxTimer > 0f;
            if (wasHitboxActive && !isHitboxActive)
            {
                OnHitboxDisable?.Invoke();
            }

            // Завершение атаки
            bool isAttackingNow = attackTimer > 0f;
            if (wasAttacking && !isAttackingNow)
            {
                isAttacking = false;

                // Финальная проверка хитбокса (защита от ошибок)
                if (isHitboxActive)
                {
                    OnHitboxDisable?.Invoke();
                }

                OnAttackEnd?.Invoke();
            }
        }

        // ━━━ ПУБЛИЧНЫЕ МЕТОДЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public bool IsAttacking() => isAttacking;
        public bool IsHitboxActive() => isAttacking && hitboxTimer > 0f;
        public Vector2 GetAttackDirection() => attackDirection;

        // ━━━ СБРОС (для тестов/респавна) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public void Reset()
        {
            isAttacking = false;
            attackTimer = 0f;
            hitboxTimer = 0f;
            attackDirection = Vector2.down;
        }
    }
}