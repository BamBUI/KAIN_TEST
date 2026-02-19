using System;
using UnityEngine;

namespace Assets.Scripts.EnemyScripts
{
    public class EnemyAttackCore
    {
        public event Action<Vector2> OnAttackStart;
        public event Action OnHitboxEnable;
        public event Action OnHitboxDisable;
        public event Action OnAttackEnd;

        private readonly float hitboxDuration;
        private readonly float attackAnimationDuration;
        private readonly float postAttackDelay;

        private bool isAttacking = false;
        private Vector2 attackDirection = Vector2.down;
        private float attackTimer;
        private float hitboxTimer;

        public EnemyAttackCore(float hitboxDuration, float attackAnimationDuration, float postAttackDelay)
        {
            // Валидация входных параметров
            if (!float.IsFinite(hitboxDuration) || hitboxDuration < 0f)
                throw new ArgumentException("Hitbox duration must be non-negative and finite", nameof(hitboxDuration));
            if (!float.IsFinite(attackAnimationDuration) || attackAnimationDuration <= 0f)
                throw new ArgumentException("Attack animation duration must be positive and finite", nameof(attackAnimationDuration));
            if (!float.IsFinite(postAttackDelay) || postAttackDelay < 0f)
                throw new ArgumentException("Post-attack delay must be non-negative and finite", nameof(postAttackDelay));

            // Валидация логических соотношений
            if (hitboxDuration > attackAnimationDuration)
            {
                Debug.LogWarning($"[EnemyAttackCore] Hitbox duration ({hitboxDuration}) exceeds animation duration ({attackAnimationDuration}). Clamping to animation duration.");
                hitboxDuration = attackAnimationDuration;
            }

            this.hitboxDuration = hitboxDuration;
            this.attackAnimationDuration = attackAnimationDuration;
            this.postAttackDelay = postAttackDelay;
        }

        public bool StartAttack(Vector2 direction)
        {
            if (isAttacking) return false;

            isAttacking = true;

            // Защита от нулевого вектора
            attackDirection = direction.sqrMagnitude > 0.01f
                ? direction.normalized
                : Vector2.down;

            attackTimer = attackAnimationDuration + postAttackDelay;
            hitboxTimer = hitboxDuration;

            OnAttackStart?.Invoke(attackDirection);
            OnHitboxEnable?.Invoke();
            return true;
        }

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

        public bool IsAttacking() => isAttacking;
        public bool IsHitboxActive() => isAttacking && hitboxTimer > 0f;
        public Vector2 GetAttackDirection() => attackDirection;
    }
}