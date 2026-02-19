using UnityEngine;

namespace Assets.Scripts.EnemyScripts
{
    public class EnemyAnimatorCore
    {
        private readonly Animator animator;
        private readonly EnemyMovementCore movement;
        private readonly EnemyHealthCore health;
        private readonly EnemyAttackCore attack;

        public EnemyAnimatorCore(Animator animator, EnemyMovementCore movement,
                                EnemyHealthCore health, EnemyAttackCore attack)
        {
            this.animator = animator;
            this.movement = movement;
            this.health = health;
            this.attack = attack;

            // Подписка ТОЛЬКО на события анимации
            health.OnHurtStart += () => animator.SetBool("IsHurt", true);
            health.OnHurtEnd += () => animator.SetBool("IsHurt", false);
            health.OnFaintStart += () => animator.SetBool("IsFaint", true);
            health.OnFaintEnd += () => animator.SetBool("IsFaint", false);
            health.OnDeath += () => animator.SetBool("IsDead", true);

            attack.OnAttackStart += OnAttackStart;
            attack.OnAttackEnd += () => animator.SetBool("IsAttacking", false);
        }

        /// <summary>
        /// Обновление параметров анимации движения
        /// </summary>
        public void UpdateAnimation(Vector2 moveDirection)
        {
            if (health.IsDead() || health.IsFainting() || health.IsDead()) return;

            Vector2 lastDir = movement.GetLastMoveDirection();
            bool isMoving = movement.IsMoving(moveDirection) && !attack.IsAttacking() && !health.IsHurt();

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
            animator.SetBool("IsAttacking", true);
            animator.SetBool("IsWalking", false);
        }
    }
}