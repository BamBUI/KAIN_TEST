using UnityEngine;
using Assets.Scripts.Generic;

namespace Assets.Scripts.CharacterControllers
{
    /// <summary>
    /// Базовый класс для всех персонажей (игрок, враги, NPC)
    /// Содержит общую логику: здоровье, атака, отбрасывание, анимации
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public abstract class CharacterBase : MonoBehaviour
    {
        // ━━━ НАСТРОЙКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        [Header("Health Settings")]
        [SerializeField] protected float maxHealth = 3f;
        [SerializeField] protected float hurtDuration = 0.5f;
        [SerializeField] protected float faintDuration = 0.5f;
        [SerializeField] protected float deathDuration = 1.0f;

        [Header("Attack Settings")]
        [SerializeField] protected float hitboxDuration = 0.1f;
        [SerializeField] protected float attackDuration = 0.5f;
        [SerializeField] protected float postAttackDelay = 0.3f;

        [Header("Movement Settings")]
        [SerializeField] protected float moveSpeed = 3f;

        [Header("Knockback Settings")]
        [SerializeField] protected float knockbackDistance = 0.4f;

        [Header("References")]
        [SerializeField] protected Transform aimTransform;
        [SerializeField] protected MeleeWeapon meleeWeapon;

        // ━━━ ЗАВИСИМОСТИ UNITY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected Rigidbody2D rb;
        protected Animator animator;
        protected AudioManager audioManager;

        // ━━━ МОДУЛИ (CORE) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected CharacterHealthCore health;
        protected CharacterAttackCore attack;
        protected KnockbackCore knockback;

        // ━━━ ИНИЦИАЛИЗАЦИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected virtual void Awake()
        {
            // Получаем компоненты
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();

            // Валидация
            if (rb == null) throw new System.NullReferenceException($"Rigidbody2D missing on {gameObject.name}");
            if (animator == null) throw new System.NullReferenceException($"Animator missing on {gameObject.name}");
            if (aimTransform == null) throw new System.NullReferenceException($"Aim Transform missing on {gameObject.name}");

            // Создаём Core-модули
            health = new CharacterHealthCore(
                maxHealth: maxHealth,
                hurtDuration: hurtDuration,
                faintDuration: faintDuration,
                deathDuration: deathDuration,
                hasDeathFinishedEvent: ShouldUseDeathFinishedEvent()
            );

            attack = new CharacterAttackCore(
                hitboxDuration: hitboxDuration,
                attackDuration: attackDuration,
                postAttackDelay: postAttackDelay
            );

            knockback = new KnockbackCore(rb);

            // Подписка на события
            health.OnDeath += HandleDeath;
            attack.OnHitboxEnable += OnHitboxEnable;
            attack.OnHitboxDisable += OnHitboxDisable;

            // Поиск AudioManager (один на сцену)
            audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning($"AudioManager not found in scene! ({gameObject.name})");
            }
        }

        protected virtual void Start()
        {
            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);
        }

        // ━━━ ОБНОВЛЕНИЕ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected virtual void Update()
        {
            if (health.IsDead()) return;

            health.Update(Time.deltaTime);
            attack.Update(Time.deltaTime);
        }

        // ━━━ ОБРАБОТКА УРОНА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// Получить урон (вызывается из MeleeWeapon)
        /// </summary>
        public virtual void TakeDamage(float damage, Vector2 attackerPosition)
        {
            if (health.IsDead() || health.IsInvincible()) return;

            knockback.ApplyFromAttacker(transform.position, attackerPosition, knockbackDistance);
            health.TakeDamage(damage);
        }

        // ━━━ СОБЫТИЯ АТАКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected virtual void OnHitboxEnable()
        {
            if (meleeWeapon != null)
            {
                meleeWeapon.ResetHit();
                meleeWeapon.gameObject.SetActive(true);
            }
        }

        protected virtual void OnHitboxDisable()
        {
            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);
        }

        // ━━━ ОБРАБОТКА СМЕРТИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected virtual void HandleDeath()
        {
            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(false);

            // Физика: фиксируем тело
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // ━━━ ОТПИСКА ОТ СОБЫТИЙ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected virtual void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }
            if (attack != null)
            {
                attack.OnHitboxEnable -= OnHitboxEnable;
                attack.OnHitboxDisable -= OnHitboxDisable;
            }
        }

        // ━━━ ВНЕШНИЙ ИНТЕРФЕЙС ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public bool IsDead() => health.IsDead();
        public bool IsInvincible() => health.IsInvincible();
        public float GetCurrentHealth() => health.GetCurrentHealth();
        public float GetMaxHealth() => health.GetMaxHealth();

        // ━━━ АБСТРАКТНЫЕ МЕТОДЫ (должны реализовать наследники) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// Нужно ли событие OnDeathFinished? (true для игрока, false для врагов)
        /// </summary>
        protected abstract bool ShouldUseDeathFinishedEvent();

        /// <summary>
        /// Обновление анимации (вызывать из LateUpdate в наследнике)
        /// </summary>
        protected abstract void UpdateAnimation();
    }
}