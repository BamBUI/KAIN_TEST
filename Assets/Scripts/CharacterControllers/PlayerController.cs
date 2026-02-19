using UnityEngine;
using UnityEngine.InputSystem;
using Assets.Scripts.Generic;
using Assets.Scripts.PlayerScripts;

namespace Assets.Scripts.CharacterControllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 3f;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Attack Settings")]
        [SerializeField] private float hitboxDuration = 0.1f;
        [SerializeField] private float attackDuration = 1f;
        [SerializeField] private float postAttackDelay = 0.3f;

        [Header("Knockback Settings")]
        [SerializeField] private float knockbackDistance = 0.4f;

        [Header("References")]
        [SerializeField] private Transform aimTransform;
        [SerializeField] private MeleeWeapon meleeWeapon;

        private Rigidbody2D rb;
        private Animator animator;
        private PlayerHealthCore health;
        private PlayerMovementCore movement;
        private PlayerAttackCore attack;
        private PlayerAnimatorCore animatorCore;
        private KnockbackCore knockback;
        private AudioManager audioManager;
        private InputActionAsset inputActions;
        private InputAction moveAction;
        private InputAction attackAction;

        private void Awake()
        {
            inputActions = GetComponent<PlayerInput>()?.actions ?? Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset not found!");
                return;
            }

            // Находим и подписываемся на действия
            moveAction = inputActions.FindAction("Move");
            attackAction = inputActions.FindAction("Attack");

            moveAction.performed += OnMove;
            moveAction.canceled += OnMove; // Для обнуления при отпускании
            attackAction.performed += OnAttack;

            // КРИТИЧЕСКИ ВАЖНО: активируем карту
            inputActions.Enable();

            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();

            if (rb == null) throw new System.NullReferenceException($"Rigidbody2D missing on {gameObject.name}");
            if (animator == null) throw new System.NullReferenceException($"Animator missing on {gameObject.name}");
            if (aimTransform == null) throw new System.NullReferenceException($"Aim Transform reference missing on {gameObject.name}");

            // Создаём модули
            health = new PlayerHealthCore(maxHealth, 0.5f);
            movement = new PlayerMovementCore(rb, aimTransform, moveSpeed);
            attack = new PlayerAttackCore(hitboxDuration, attackDuration, postAttackDelay);
            knockback = new KnockbackCore(rb);
            animatorCore = new PlayerAnimatorCore(animator, movement, health, attack);

            // Подписка на события
            health.OnDeath += HandleDeath;
            health.OnDeathFinished += OnDeathFinished;

            attack.OnHitboxEnable += OnHitboxEnable;
            attack.OnHitboxDisable += OnHitboxDisable;

            audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogError("AudioManager not found in the scene!");
            }
        }

        private void Start()
        {
            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (health.IsDead()) return;

            health.Update(Time.deltaTime);
            attack.Update(Time.deltaTime);
            movement.UpdatePhysics(
                isAttacking: attack.IsAttacking(),
                isHurt: health.IsInvincible()
            );
        }

        private void LateUpdate()
        {
            if (health.IsDead()) return; // Пропускаем, если мертв

            // --- ДОБАВИТЬ ЭТОТ БЛОК ДЛЯ ОТЛАДКИ ---
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Faint"))
            {
                // Предположим, ваш клип Faint.anim имеет frameRate = 60 (проверьте в инспекторе клипа)
                // normalizedTime = 0.0 -> первый кадр, normalizedTime = 1.0 -> последний кадр
                float frameRate = 60f; // Установите правильный frameRate для вашего клипа Faint.anim
                float currentFrame = stateInfo.normalizedTime * stateInfo.length * frameRate;
                Debug.Log($"[ANIM DEBUG] Faint: normTime={stateInfo.normalizedTime:F3}, length={stateInfo.length:F3}, currentFrame={currentFrame:F1}");
            }
            // --- КОНЕЦ БЛОКА ОТЛАДКИ ---

            movement.UpdateAimRotation();
            animatorCore.UpdateAnimation();
        }

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

            // Воспроизвести звук удара мечом
            if (audioManager != null)
            {
                audioManager.PlaySwordSlashSound();
            }
        }

        private void OnHitboxEnable()
        {
            if (meleeWeapon != null)
            {
                meleeWeapon.ResetHit();
                meleeWeapon.gameObject.SetActive(true);
            }
        }

        private void OnHitboxDisable()
        {
            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);
        }

        public void TakeDamage(float damage, Vector2 attackerPosition)
        {
            if (health.IsDead() || health.IsInvincible()) return;

            if (audioManager != null && !health.IsInvincible() && health.GetCurrentHealth() > damage) // Проверка, чтобы не звучало на смертельном
            {
                audioManager.PlayKainHurtSound();
            }

            knockback.ApplyFromAttacker(transform.position, attackerPosition, knockbackDistance);
            health.TakeDamage(damage);
        }

        private void HandleDeath()
        {
            // Воспроизвести звук смерти
            if (audioManager != null)
            {
                audioManager.PlayKainDeathSound();
            }

            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);
            Debug.Log("[PlayerController] HandleDeath called!");
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
                health.OnDeathFinished -= () => Destroy(gameObject);
            }
            if (attack != null)
            {
                attack.OnHitboxEnable -= OnHitboxEnable;
                attack.OnHitboxDisable -= OnHitboxDisable;
            }
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

        public bool IsDead() => health.IsDead();

        private void OnDeathFinished()
        {
            Debug.Log("[PlayerController] OnDeathFinished called! Destroying..."); // <-- ДОБАВЬТЕ ЭТУ СТРОКУ
            Destroy(gameObject); // <-- УБЕДИТЕСЬ, ЧТО ЭТА СТРОКА ВЫЗЫВАЕТСЯ
        }
    }
}