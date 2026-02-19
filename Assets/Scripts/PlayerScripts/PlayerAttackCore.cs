using System;
using UnityEngine;

namespace Assets.Scripts.PlayerScripts
{
    public class PlayerAttackCore
    {
        public event Action<Vector2> OnAttackStart;
        public event Action OnHitboxEnable;   // ← НОВОЕ: активация хитбокса
        public event Action OnHitboxDisable;  // ← НОВОЕ: деактивация хитбокса
        public event Action OnAttackEnd;

        private readonly float hitboxDuration;      // ← НОВОЕ: длительность активной фазы
        private readonly float attackDuration;      // Общая длительность атаки
        private readonly float postAttackDelay;     // Пауза после удара

        private float attackTimer;
        private float hitboxTimer;
        private bool isAttacking = false;
        private Vector2 attackDirection = Vector2.down;

        /// <summary>
        /// Инициализация модуля атаки с поддержкой короткой фазы хитбокса
        /// </summary>
        /// <param name="hitboxDuration">Длительность активной фазы удара (хитбокс активен)</param>
        /// <param name="attackDuration">Общая длительность анимации удара</param>
        /// <param name="postAttackDelay">Пауза после удара (восстановление)</param>
        public PlayerAttackCore(float hitboxDuration, float attackDuration, float postAttackDelay)
        {
            // Валидация
            if (!float.IsFinite(hitboxDuration) || hitboxDuration < 0f)
                throw new ArgumentException("Hitbox duration must be non-negative and finite", nameof(hitboxDuration));
            if (!float.IsFinite(attackDuration) || attackDuration <= 0f)
                throw new ArgumentException("Attack duration must be positive and finite", nameof(attackDuration));
            if (!float.IsFinite(postAttackDelay) || postAttackDelay < 0f)
                throw new ArgumentException("Post-attack delay must be non-negative and finite", nameof(postAttackDelay));

            // Логическая проверка: хитбокс не может быть дольше анимации
            if (hitboxDuration > attackDuration)
            {
                Debug.LogWarning($"[PlayerAttackCore] Hitbox duration ({hitboxDuration}) exceeds attack animation ({attackDuration}). Clamping to animation duration.");
                hitboxDuration = attackDuration;
            }

            this.hitboxDuration = hitboxDuration;
            this.attackDuration = attackDuration;
            this.postAttackDelay = postAttackDelay;
        }

        public bool StartAttack(Vector2 direction)
        {
            if (isAttacking) return false;

            isAttacking = true;
            attackDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.down;

            attackTimer = attackDuration + postAttackDelay;
            hitboxTimer = hitboxDuration;

            OnAttackStart?.Invoke(attackDirection);
            OnHitboxEnable?.Invoke(); // ← Активация хитбокса
            return true;
        }

        public void Update(float deltaTime)
        {
            if (!isAttacking) return;
            if (!float.IsFinite(deltaTime) || deltaTime <= 0f) return;

            bool wasHitboxActive = hitboxTimer > 0f;
            attackTimer -= deltaTime;
            hitboxTimer -= deltaTime;
            bool isHitboxActive = hitboxTimer > 0f;

            // Деактивация хитбокса
            if (wasHitboxActive && !isHitboxActive)
            {
                OnHitboxDisable?.Invoke();
            }

            // Завершение атаки
            if (attackTimer <= 0f)
            {
                isAttacking = false;
                if (isHitboxActive) OnHitboxDisable?.Invoke(); // Финальная проверка
                OnAttackEnd?.Invoke();
            }
        }

        public bool IsAttacking() => isAttacking;
        public Vector2 GetAttackDirection() => attackDirection;
        public bool IsHitboxActive() => isAttacking && hitboxTimer > 0f; // ← Для отладки
    }
}