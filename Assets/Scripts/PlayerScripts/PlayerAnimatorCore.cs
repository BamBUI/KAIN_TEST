using UnityEngine;

namespace Assets.Scripts.PlayerScripts
{
    public class PlayerAnimatorCore
    {
        private readonly Animator animator;
        private readonly PlayerMovementCore movement;
        private readonly PlayerHealthCore health;
        private readonly PlayerAttackCore attack;

        public PlayerAnimatorCore(Animator animator, PlayerMovementCore movement,
                                 PlayerHealthCore health, PlayerAttackCore attack)
        {
            this.animator = animator;
            this.movement = movement;
            this.health = health;
            this.attack = attack;

            // Подписка на события здоровья
            health.OnHurt += (damage) => animator.SetBool("IsHurt", true);
            health.OnHurtEnd += () => animator.SetBool("IsHurt", false);

            // --- НОВЫЕ ПОДПИСКИ ДЛЯ FAINT ---
            health.OnFaintStart += () => animator.SetBool("IsFaint", true); // <-- Установка параметра IsFaint
            health.OnFaintEnd += () => animator.SetBool("IsFaint", false); // <-- Сброс параметра IsFaint

            health.OnDeath += () => animator.SetBool("IsDead", true); // <-- Установка параметра IsDead (после Faint)

            // Подписка на события атаки
            attack.OnAttackStart += OnAttackStart;
        }

        public void UpdateAnimation()
        {
            // --- ИЗМЕНЕНО: Проверка на Faint ---
            if (health.IsDead() || health.IsFainting()) return; // <-- Анимация не обновляется в состоянии Death или Faint

            Vector2 lastDir = movement.GetLastDirection();
            // --- ИЗМЕНЕНО: Блокировка движения при Faint ---
            bool isMoving = movement.IsMoving() && !attack.IsAttacking() && !health.IsInvincible(); // <-- IsInvincible теперь включает Faint

            animator.SetBool("IsWalking", isMoving);
            animator.SetFloat("InputX", lastDir.x);
            animator.SetFloat("InputY", lastDir.y);
            animator.SetFloat("LastInputX", lastDir.x);
            animator.SetFloat("LastInputY", lastDir.y);
        }

        private void OnAttackStart(Vector2 direction)
        {
            animator.SetFloat("AttackDirX", direction.x);
            animator.SetFloat("AttackDirY", direction.y);
            animator.SetTrigger("AttackTrigger");
        }
    }
}