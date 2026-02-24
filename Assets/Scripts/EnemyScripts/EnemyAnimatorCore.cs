using UnityEngine;
// using Assets.Scripts.EnemyScripts; // - старый namespace (оставляем для истории)
using Assets.Scripts.Generic; // + новый namespace для CharacterHealthCore и CharacterAttackCore

namespace Assets.Scripts.EnemyScripts
{
    public class EnemyAnimatorCore
    {
        private readonly Animator animator;
        private readonly EnemyMovementCore movement;
        // private readonly EnemyHealthCore health; // - старый тип (до рефакторинга)
        // private readonly EnemyAttackCore attack; // - старый тип (до рефакторинга)
        private readonly CharacterHealthCore health; // + новый универсальный тип
        private readonly CharacterAttackCore attack; // + новый универсальный тип

        // public EnemyAnimatorCore(Animator animator, EnemyMovementCore movement,
        //                         EnemyHealthCore health, EnemyAttackCore attack) // - старый конструктор
        public EnemyAnimatorCore(Animator animator, EnemyMovementCore movement,
                                CharacterHealthCore health, CharacterAttackCore attack) // + новый конструктор с универсальными типами
        {
            this.animator = animator;
            this.movement = movement;
            this.health = health;
            this.attack = attack;

            // Подписка на события здоровья
            // health.OnHurtStart += () => animator.SetBool("IsHurt", true); // - старая версия (Action без параметров)
            health.OnHurtStart += (_) => animator.SetBool("IsHurt", true); // + новая версия (Action<float>), параметр игнорируем через _
            health.OnHurtEnd += () => animator.SetBool("IsHurt", false);
            health.OnFaintStart += () => animator.SetBool("IsFaint", true);
            health.OnFaintEnd += () => animator.SetBool("IsFaint", false);
            health.OnDeath += () => animator.SetBool("IsDead", true);

            // Подписка на события атаки
            // attack.OnAttackStart += OnAttackStart; // - старая версия (Action без параметров)
            attack.OnAttackStart += OnAttackStart; // + новая версия (Action<Vector2>)
            attack.OnAttackEnd += () => animator.SetBool("IsAttacking", false);
        }

        public void UpdateAnimation(Vector2 moveDirection)
        {
            if (health.IsDead() || health.IsFainting()) return;

            Vector2 lastDir = movement.GetLastMoveDirection();
            // --- ИЗМЕНЕНО: Блокировка движения при Faint ---
            // bool isMoving = movement.IsMoving(moveDirection) && !attack.IsAttacking(); // - старая проверка
            bool isMoving = movement.IsMoving(moveDirection) && !attack.IsAttacking() && !health.IsInvincible(); // + IsInvincible теперь включает Faint

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
            //animator.SetBool("IsAttacking", true);
            animator.SetBool("IsWalking", false);
        }
    }
}