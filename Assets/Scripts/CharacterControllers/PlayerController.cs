using UnityEngine;
using UnityEngine.InputSystem;
using Assets.Scripts.Generic;
using Assets.Scripts.PlayerScripts;

namespace Assets.Scripts.CharacterControllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public class PlayerController : CharacterBase
    {
        // ━━━ СПЕЦИФИЧНЫЕ НАСТРОЙКИ ИГРОКА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // (Общие настройки теперь в CharacterBase через [SerializeField] protected)

        // ━━━ ЗАВИСИМОСТИ UNITY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private PlayerMovementCore movement;
        private PlayerAnimatorCore animatorCore;
        private InputActionAsset inputActions;
        private InputAction moveAction;
        private InputAction attackAction;

        // ━━━ ИНИЦИАЛИЗАЦИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void Awake()
        {
            // 1. Сначала инициализируем базу (создаёт health, attack, knockback, rb, animator)
            base.Awake();

            // 2. Input System (только у игрока)
            inputActions = GetComponent<PlayerInput>()?.actions ?? Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset not found!");
                return;
            }

            moveAction = inputActions.FindAction("Move");
            attackAction = inputActions.FindAction("Attack");

            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
            attackAction.performed += OnAttack;

            inputActions.Enable();

            // 3. Создаём специфичные модули игрока (используем protected поля из базы)
            movement = new PlayerMovementCore(rb, aimTransform, moveSpeed);
            animatorCore = new PlayerAnimatorCore(animator, movement, health, attack);

            // 4. Подписка на события (база уже подписала attack, мы добавляем health)
            health.OnDeathFinished += OnDeathFinished;
        }

        protected override void Start()
        {
            base.Start(); // База уже скрывает meleeWeapon
            // Дополнительные инициализации игрока если нужны
        }

        // ━━━ ОБНОВЛЕНИЕ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void Update()
        {
            // База уже проверяет IsDead() и обновляет health.Update() и attack.Update()
            base.Update();

            if (health.IsDead()) return;

            movement.UpdatePhysics(
                isAttacking: attack.IsAttacking(),
                isHurt: health.IsInvincible()
            );
        }

        // ━━━ ПОЗДНЕЕ ОБНОВЛЕНИЕ (НЕ override, в CharacterBase нет такого метода) ━━━━━━━━━━━
        private void LateUpdate()
        {
            if (health.IsDead()) return;

            movement.UpdateAimRotation();
            animatorCore.UpdateAnimation();
        }

        // ━━━ ВВОД (INPUT) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public void OnMove(InputAction.CallbackContext context)
        {
            if (health.IsDead()) return;
            if (attack.IsAttacking() || health.IsInvincible()) return;

            movement.SetMoveInput(context.ReadValue<Vector2>());
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.started || health.IsDead() || attack.IsAttacking() || health.IsInvincible()) return;

            Vector2 attackDir = movement.GetLastDirection();
            attack.StartAttack(attackDir);

            // Звук удара мечом
            if (audioManager != null)
            {
                audioManager.PlaySwordSlashSound();
            }
        }

        // ━━━ СОБЫТИЯ АТАКИ (сохраняем имена!) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void OnHitboxEnable()
        {
            base.OnHitboxEnable(); // База активирует meleeWeapon

            // Дополнительная логика игрока если нужна
        }

        protected override void OnHitboxDisable()
        {
            base.OnHitboxDisable(); // База деактивирует meleeWeapon

            // Дополнительная логика игрока если нужна
        }

        // ━━━ ПОЛУЧЕНИЕ УРОНА (сохраняем имя!) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public override void TakeDamage(float damage, Vector2 attackerPosition)
        {
            if (health.IsDead() || health.IsInvincible()) return;

            // Звук боли (только если урон не смертельный)
            if (audioManager != null && !health.IsInvincible() && health.GetCurrentHealth() > damage)
            {
                audioManager.PlayKainHurtSound();
            }

            knockback.ApplyFromAttacker(transform.position, attackerPosition, knockbackDistance);
            health.TakeDamage(damage);
        }

        // ━━━ ОБРАБОТКА СМЕРТИ (сохраняем имя!) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void HandleDeath()
        {
            base.HandleDeath(); // База фиксирует физику и скрывает оружие

            // Звук смерти игрока
            if (audioManager != null)
            {
                audioManager.PlayKainDeathSound();
            }

            Debug.Log("[PlayerController] HandleDeath called!");
        }

        // ━━━ УНИЧТОЖЕНИЕ (сохраняем имя!) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnDeathFinished()
        {
            Debug.Log("[PlayerController] OnDeathFinished called! Destroying...");
            Destroy(gameObject);
        }

        // ━━━ ОТПИСКА ОТ СОБЫТИЙ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void OnDestroy()
        {
            // Отписка от событий базы
            base.OnDestroy();

            // Отписка от специфичных событий игрока
            if (health != null)
            {
                health.OnDeathFinished -= OnDeathFinished;
            }

            // Отписка от Input System
            if (inputActions != null && inputActions.enabled)
            {
                if (moveAction != null)
                {
                    moveAction.performed -= OnMove;
                    moveAction.canceled -= OnMove;
                }
                if (attackAction != null)
                {
                    attackAction.performed -= OnAttack;
                }
                inputActions.Disable();
            }
        }

        // ━━━ РЕАЛИЗАЦИЯ АБСТРАКТНЫХ МЕТОДОВ БАЗЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override bool ShouldUseDeathFinishedEvent()
        {
            return true; // Игроку нужно событие для уничтожения объекта
        }

        protected override void UpdateAnimation()
        {
            // Для игрока анимация обновляется в LateUpdate через animatorCore
            // Этот метод может использоваться базой если нужно
        }
    }
}