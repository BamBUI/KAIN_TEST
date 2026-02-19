using UnityEngine;
using Assets.Scripts.Generic;
using Assets.Scripts.EnemyScripts;
using Assets.Scripts.PlayerScripts;

namespace Assets.Scripts.CharacterControllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public class EnemyController : MonoBehaviour
    {
        // ━━━ НАСТРОЙКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        [Header("AI Settings")]
        [SerializeField] private float aggroRange = 5f;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float stopDistance = 0.05f;

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 3f;

        [Header("Attack Settings")]
        [SerializeField] private float hitboxDuration = 0.1f;
        [SerializeField] private float attackAnimationDuration = 0.5f;
        [SerializeField] private float postAttackDelay = 0.3f;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;

        [Header("Knockback Settings")]
        [SerializeField] private float knockbackDistance = 0.4f;

        [Header("References")]
        [SerializeField] private Transform enemyAim;
        [SerializeField] private MeleeWeapon meleeWeapon;

        // ━━━ ЗАВИСИМОСТИ UNITY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private Rigidbody2D rb;
        private Animator animator;
        private Vector2 spawnPosition;
        private PlayerController cachedPlayer;
        private Vector2 lastMoveDirection = Vector2.zero; // ← Для передачи в animatorCore

        // ━━━ МОДУЛИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private EnemyHealthCore health;
        private EnemyAICore ai;
        private EnemyMovementCore movement;
        private EnemyAttackCore attack;
        private EnemyAnimatorCore animatorCore;
        private KnockbackCore knockback;
        private AudioManager audioManager;

        // ━━━ ИНИЦИАЛИЗАЦИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();

            if (rb == null) throw new System.NullReferenceException($"Rigidbody2D missing on {gameObject.name}");
            if (animator == null) throw new System.NullReferenceException($"Animator missing on {gameObject.name}");
            if (enemyAim == null) throw new System.NullReferenceException($"EnemyAim reference missing on {gameObject.name}");

            spawnPosition = transform.position;

            // Создаём модули
            health = new EnemyHealthCore(maxHealth);
            ai = new EnemyAICore(aggroRange, attackRange, stopDistance, spawnPosition);
            movement = new EnemyMovementCore(rb, enemyAim, moveSpeed);
            attack = new EnemyAttackCore(hitboxDuration, attackAnimationDuration, postAttackDelay);
            knockback = new KnockbackCore(rb);
            animatorCore = new EnemyAnimatorCore(animator, movement, health, attack); // ← ЕДИНСТВЕННЫЙ источник анимации

            // Подписка ТОЛЬКО на события физики/логики (НЕ анимации!)
            health.OnFaintStart += OnFaintStart; // ← Физика: фиксация тела
            health.OnDeath += HandleDeath;       // ← Логика: отключение скрипта

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
            cachedPlayer = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);
        }

        // ━━━ ОБНОВЛЕНИЕ ЛОГИКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void Update()
        {
            if (health.IsDead()) return;

            // Обновляем кэш игрока
            if (cachedPlayer == null || cachedPlayer.IsDead())
            {
                cachedPlayer = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
            }

            Vector2? playerPos = cachedPlayer != null ? cachedPlayer.transform.position : (Vector2?)null;
            bool playerIsDead = cachedPlayer == null || cachedPlayer.IsDead();

            health.Update(Time.deltaTime);
            attack.Update(Time.deltaTime);

            Vector2 moveDir = ai.GetMoveDirection(transform.position, playerPos, playerIsDead);
            lastMoveDirection = moveDir; // ← Сохраняем для аниматора

            bool shouldAttack = playerPos.HasValue &&
                               ai.ShouldAttack(transform.position, playerPos.Value, attack.IsAttacking(), health.IsHurt());

            if (playerPos.HasValue && ai.IsOverlapping(transform.position, playerPos.Value))
            {
                Vector2 escapeDir = ai.GetEscapeDirection(transform.position, playerPos.Value);
                knockback.Apply(escapeDir, 0.2f);
                moveDir = Vector2.zero;
            }

            movement.UpdatePhysics(
                moveDirection: moveDir,
                isAttacking: attack.IsAttacking(),
                isHurt: health.IsHurt() || health.IsFainting() || health.IsDead() // ← ТРИ состояния!
            );

            if (shouldAttack && !attack.IsAttacking() && !health.IsHurt() && !health.IsFainting())
            {
                Vector2 attackDir = playerPos.HasValue
                    ? (playerPos.Value - (Vector2)transform.position).normalized
                    : Vector2.down;
                attack.StartAttack(attackDir);
            }
        }

        private void LateUpdate()
        {
            if (health.IsDead() || health.IsFainting()) return;

            // 1. Поворот взгляда
            if (cachedPlayer != null && !cachedPlayer.IsDead() && !health.IsHurt())
            {
                movement.UpdateAimRotation(
                    lookTargetPosition: cachedPlayer.transform.position,
                    enemyPosition: transform.position,
                    shouldLook: true
                );
            }

            // 2. Обновление анимации — ЕДИНСТВЕННЫЙ вызов к аниматору
            animatorCore.UpdateAnimation(lastMoveDirection);
        }

        // ━━━ ОБРАБОТЧИКИ ФИЗИКИ/ЛОГИКИ (НЕ АНИМАЦИИ!) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnFaintStart()
        {
            // ФИЗИКА: фиксируем тело при обмороке
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        private void OnHitboxEnable()
        {
            if (meleeWeapon != null)
            {
                meleeWeapon.ResetHit();
                meleeWeapon.gameObject.SetActive(true);
                if (audioManager != null)
                {
                    // Предположим, вы добавили в AudioManager новый метод и массив клипов
                    audioManager.PlaySwordSlashSound(); // <-- НОВЫЙ МЕТОД
                }
            }
        }

        private void OnHitboxDisable()
        {
            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);
        }

        // ━━━ ПОЛУЧЕНИЕ УРОНА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public void TakeDamage(float damage, Vector2 playerPosition)
        {
            // Воспроизвести звук боли
            if (audioManager != null)
            {
                audioManager.PlayEnemyHurtSound();
            }

            if (health.IsDead() || health.IsHurt() || health.IsFainting()) return;

            knockback.ApplyFromAttacker(transform.position, playerPosition, knockbackDistance);
            health.TakeDamage(damage);
        }

        // ━━━ СОБЫТИЯ ИЗ АНИМАТОРА (временно — будут удалены после таймеров) ━━━━━━━━━━━━━━━━━━━━━
        public void OnHurtAnimationFinished()
        {
            Debug.Log("[DEBUG] OnHurtAnimationFinished called!");
            if (!health.IsDead() && !health.IsFainting())
                health.OnHurtAnimationFinished();
        }

        public void OnFaintAnimationFinished()
        {
            if (health.IsFainting() && !health.IsDead())
                health.OnFaintAnimationFinished();
        }

        public void OnDeathAnimationFinished()
        {
            Destroy(gameObject);
        }

        // ━━━ ОБРАБОТКА СМЕРТИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void HandleDeath()
        {
            // Воспроизвести звук смерти
            if (audioManager != null)
            {
                audioManager.PlayEnemyDeathSound();
            }
            enabled = false;

            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);

            // ФИЗИКА: фиксируем тело при смерти
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // ━━━ БЕЗОПАСНАЯ ОТПИСКА ОТ СОБЫТИЙ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnDestroy()
        {
            // Отписка только от событий, на которые подписывались в этом классе
            if (health != null)
            {
                health.OnFaintStart -= OnFaintStart;
                health.OnDeath -= HandleDeath;
            }
            if (attack != null)
            {
                attack.OnHitboxEnable -= OnHitboxEnable;
                attack.OnHitboxDisable -= OnHitboxDisable;
            }
        }

        // ━━━ ВНЕШНИЙ ИНТЕРФЕЙС ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public bool IsDead() => health.IsDead();
    }
}