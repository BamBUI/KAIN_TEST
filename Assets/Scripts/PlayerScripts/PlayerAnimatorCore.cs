using UnityEngine;
// using Assets.Scripts.PlayerScripts; // - старый namespace (оставляем для истории)
using Assets.Scripts.Generic; // + новый namespace для CharacterHealthCore и CharacterAttackCore

namespace Assets.Scripts.PlayerScripts
{
    public class PlayerAnimatorCore
    {
        private readonly Animator animator;
        private readonly PlayerMovementCore movement;
        // private readonly PlayerHealthCore health; // - старый тип (до рефакторинга)
        // private readonly PlayerAttackCore attack; // - старый тип (до рефакторинга)
        private readonly CharacterHealthCore health; // + новый универсальный тип
        private readonly CharacterAttackCore attack; // + новый универсальный тип

        // public PlayerAnimatorCore(Animator animator, PlayerMovementCore movement,
        //                          PlayerHealthCore health, PlayerAttackCore attack) // - старый конструктор
        public PlayerAnimatorCore(Animator animator, PlayerMovementCore movement,
                                 CharacterHealthCore health, CharacterAttackCore attack) // + новый конструктор с универсальными типами
        {
            this.animator = animator;
            this.movement = movement;
            this.health = health;
            this.attack = attack;

            // Подписка на события здоровья
            // health.OnHurt += (damage) => animator.SetBool("IsHurt", true); // - старое событие с параметром damage
            health.OnHurtStart += (_) => animator.SetBool("IsHurt", true); // + новое событие, параметр игнорируем через _
            health.OnHurtEnd += () => animator.SetBool("IsHurt", false);

            // --- НОВЫЕ ПОДПИСКИ ДЛЯ FAINT ---
            health.OnFaintStart += () => animator.SetBool("IsFaint", true); // + установка параметра IsFaint
            health.OnFaintEnd += () => animator.SetBool("IsFaint", false); // + сброс параметра IsFaint

            health.OnDeath += () => animator.SetBool("IsDead", true); // + установка параметра IsDead

            // Подписка на события атаки
            attack.OnAttackStart += OnAttackStart; // + теперь сигнатуры совпадают (Action<Vector2>)
        }

        public void UpdateAnimation()
        {
            // --- ИЗМЕНЕНО: Проверка на Faint ---
            if (health.IsDead() || health.IsFainting()) return; // + анимация не обновляется в состоянии Death или Faint

            Vector2 lastDir = movement.GetLastDirection();
            // --- ИЗМЕНЕНО: Блокировка движения при Faint ---
            // bool isMoving = movement.IsMoving() && !attack.IsAttacking(); // - старая проверка (без IsInvincible)
            bool isMoving = movement.IsMoving() && !attack.IsAttacking() && !health.IsInvincible(); // + IsInvincible теперь включает Faint

            animator.SetBool("IsWalking", isMoving);
            animator.SetFloat("InputX", lastDir.x);
            animator.SetFloat("InputY", lastDir.y);
            animator.SetFloat("LastInputX", lastDir.x);
            animator.SetFloat("LastInputY", lastDir.y);
        }

        // private void OnAttackStart(Vector2 direction) // - старая версия (для справки)
        private void OnAttackStart(Vector2 direction) // + новая версия (совпадает по сигнатуре)
        {
            animator.SetFloat("AttackDirX", direction.x);
            animator.SetFloat("AttackDirY", direction.y);
            animator.SetTrigger("AttackTrigger");
        }
    }
}