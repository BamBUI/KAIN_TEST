using UnityEngine;
using Assets.Scripts.Generic;
using Assets.Scripts.EnemyScripts;

namespace Assets.Scripts.CharacterControllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public class EnemyController : CharacterBase
    {
        // ━━━ НАСТРОЙКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        [Header("AI Settings")]
        [SerializeField] private EnemyBehaviorMode behaviorMode = EnemyBehaviorMode.Patrol;
        [SerializeField] private float aggroRange = 5f;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float stopDistance = 0.05f;
        [SerializeField] private PatrolRoute patrolRoute;
        [SerializeField] private LayerMask wallLayer;  // + НОВОЕ: слой стен для Raycast

        // ━━━ ЗАВИСИМОСТИ UNITY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private Vector2 spawnPosition;
        private Vector2 lastMoveDirection = Vector2.zero;

        // ━━━ МОДУЛИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private EnemyAICore ai;
        private EnemyMovementCore movement;
        private EnemyAnimatorCore animatorCore;

        // ━━━ ИНИЦИАЛИЗАЦИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void Awake()
        {
            // 1. Сначала инициализируем базу (создаёт health, attack, knockback, rb, animator)
            base.Awake();

            spawnPosition = transform.position;

            // 2. Создаём AI модуль с режимом поведения и слоем стен
            ai = new EnemyAICore(
                aggroRange,
                attackRange,
                stopDistance,
                spawnPosition,
                behaviorMode,
                wallLayer  // + Передаём слой стен для Raycast
            );

            // 3. Инициализация патруля (только для Patrol режима)
            if (patrolRoute != null && behaviorMode == EnemyBehaviorMode.Patrol)
            {
                Debug.Log($"[EnemyController] {gameObject.name} - PatrolRoute assigned with {patrolRoute.WaypointCount} waypoints");
                ai.SetPatrolRoute(patrolRoute);

                // Проверка каждой точки
                for (int i = 0; i < patrolRoute.WaypointCount; i++)
                {
                    var wp = patrolRoute.GetWaypoint(i);
                    if (wp != null)
                    {
                        Debug.Log($"  └─ Waypoint {i}: {wp.name} at {wp.transform.position}");
                    }
                    else
                    {
                        Debug.LogWarning($"  └─ Waypoint {i}: NULL!");
                    }
                }
            }
            else if (behaviorMode == EnemyBehaviorMode.Patrol)
            {
                Debug.LogWarning($"[EnemyController] {gameObject.name} - Patrol mode but NO PatrolRoute assigned!");
            }
            else
            {
                Debug.Log($"[EnemyController] {gameObject.name} - Behavior Mode: {behaviorMode}");
            }

            // 4. Создаём остальные модули
            movement = new EnemyMovementCore(rb, aimTransform, moveSpeed);
            animatorCore = new EnemyAnimatorCore(animator, movement, health, attack);

            // 5. Подписка на события
            health.OnDeath += HandleDeath;
            ai.OnReturnToSpawnStart += OnReturnToSpawnStart;
            ai.OnReturnToSpawnComplete += OnReturnToSpawnComplete;
            ai.OnPatrolWaypointReached += OnPatrolWaypointReached;
        }

        protected override void Start()
        {
            base.Start();
        }

        // ━━━ ОБНОВЛЕНИЕ ЛОГИКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void Update()
        {
            // База уже проверяет IsDead() и обновляет health.Update() и attack.Update()
            base.Update();

            if (health.IsDead()) return;

            // Обновляем кэш игрока
            var player = FindFirstObjectByType<PlayerController>();
            Vector2? playerPos = player != null && !player.IsDead() ? player.transform.position : (Vector2?)null;
            bool playerIsDead = player == null || player.IsDead();

            // AI логика
            Vector2 moveDir = ai.GetMoveDirection(transform.position, playerPos, playerIsDead);
            lastMoveDirection = moveDir;

            // ━━━ ДВИЖЕНИЕ (единый код для всех режимов) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // Избегание перекрытия с игроком
            if (playerPos.HasValue && ai.IsOverlapping(transform.position, playerPos.Value))
            {
                Vector2 escapeDir = ai.GetEscapeDirection(transform.position, playerPos.Value);
                knockback.Apply(escapeDir, 0.2f);
                moveDir = Vector2.zero;
            }

            // Физика движения
            movement.UpdatePhysics(
                moveDirection: moveDir,
                isAttacking: attack.IsAttacking(),
                isHurt: health.IsHurt() || health.IsFainting() || health.IsDead()
            );

            // Атака
            if (playerPos.HasValue &&
                ai.ShouldAttack(transform.position, playerPos.Value, attack.IsAttacking(), health.IsHurt()))
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
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null && !player.IsDead() && !health.IsHurt())
            {
                movement.UpdateAimRotation(
                    lookTargetPosition: player.transform.position,
                    enemyPosition: transform.position,
                    shouldLook: true
                );
            }

            // 2. Обновление анимации
            animatorCore.UpdateAnimation(lastMoveDirection);
        }

        // ━━━ ОБРАБОТЧИКИ ФИЗИКИ/ЛОГИКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnFaintStart()
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        protected override void OnHitboxEnable()
        {
            base.OnHitboxEnable();

            if (audioManager != null)
            {
                audioManager.PlaySwordSlashSound();
            }
        }

        protected override void OnHitboxDisable()
        {
            base.OnHitboxDisable();
        }

        // ━━━ ПОЛУЧЕНИЕ УРОНА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public override void TakeDamage(float damage, Vector2 playerPosition)
        {
            if (audioManager != null)
            {
                audioManager.PlayEnemyHurtSound();
            }

            if (health.IsDead() || health.IsInvincible()) return;

            knockback.ApplyFromAttacker(transform.position, playerPosition, knockbackDistance);
            health.TakeDamage(damage);
        }

        // ━━━ СОБЫТИЯ ИЗ АНИМАТОРА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public void OnHurtAnimationFinished()
        {
            Debug.Log("[DEBUG] OnHurtAnimationFinished called (stub)!");
        }

        public void OnFaintAnimationFinished()
        {
            Debug.Log("[DEBUG] OnFaintAnimationFinished called (stub)!");
        }

        public void OnDeathAnimationFinished()
        {
            Destroy(gameObject);
        }

        // ━━━ ОБРАБОТКА СМЕРТИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void HandleDeath()
        {
            base.HandleDeath();

            if (audioManager != null)
            {
                audioManager.PlayEnemyDeathSound();
            }

            enabled = false;

            Debug.Log("[EnemyController] HandleDeath called!");
        }

        // ━━━ AI-СОБЫТИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnReturnToSpawnStart()
        {
            Debug.Log($"[EnemyController] Return to spawn started | Mode: {behaviorMode}");
        }

        private void OnReturnToSpawnComplete()
        {
            Debug.Log($"[EnemyController] Return to spawn complete | Mode: {behaviorMode}");
            ai.Reset();
        }

        private void OnPatrolWaypointReached()
        {
            Debug.Log($"[EnemyController] Waypoint reached: {ai.GetCurrentWaypointIndex()} | Mode: {behaviorMode}");
        }

        // ━━━ ОТПИСКА ОТ СОБЫТИЙ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (ai != null)
            {
                ai.OnReturnToSpawnStart -= OnReturnToSpawnStart;
                ai.OnReturnToSpawnComplete -= OnReturnToSpawnComplete;
                ai.OnPatrolWaypointReached -= OnPatrolWaypointReached;
            }

            if (health != null)
            {
                health.OnFaintStart -= OnFaintStart;
                health.OnDeath -= HandleDeath;
            }
        }

        // ━━━ РЕАЛИЗАЦИЯ АБСТРАКТНЫХ МЕТОДОВ БАЗЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        protected override bool ShouldUseDeathFinishedEvent()
        {
            return false;
        }

        protected override void UpdateAnimation()
        {
            // Для врага анимация обновляется в LateUpdate через animatorCore
        }
    }
}