using System;
using UnityEngine;

namespace Assets.Scripts.EnemyScripts
{
    public class EnemyAICore
    {
        // ━━━ ПАРАМЕТРЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private readonly float aggroRange;
        private readonly float attackRange;
        private readonly float stopDistance;
        private readonly Vector2 spawnPosition;
        private readonly EnemyBehaviorMode behaviorMode;

        // ━━━ RAYCAST ДЛЯ ПРОВЕРКИ ВИДИМОСТИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private LayerMask wallLayer;

        // ━━━ ПАТРУЛИРОВАНИЕ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private PatrolRoute patrolRoute;
        private int currentWaypointIndex = 0;
        private int frozenWaypointIndex = 0;
        private bool hasPatrolRoute = false;
        private bool isPatrolling = false;
        private Vector2? chaseStartPosition = null;
        private bool isReturningToPatrol = false;
        private bool isChasing = false;

        // ━━━ СОСТОЯНИЯ AI ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private bool isReturningToSpawn = false;
        private Vector2? lastKnownPlayerPosition = null;

        // ━━━ СОБЫТИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public event Action OnReturnToSpawnStart;
        public event Action OnReturnToSpawnComplete;
        public event Action OnPatrolWaypointReached;

        // ━━━ КОНСТРУКТОР ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public EnemyAICore(
            float aggroRange,
            float attackRange,
            float stopDistance,
            Vector2 spawnPosition,
            EnemyBehaviorMode mode = EnemyBehaviorMode.Guard,
            LayerMask wallMask = default)
        {
            this.aggroRange = aggroRange;
            this.attackRange = attackRange;
            this.stopDistance = stopDistance;
            this.spawnPosition = spawnPosition;
            this.behaviorMode = mode;
            this.wallLayer = wallMask;
        }

        // ━━━ ИНИЦИАЛИЗАЦИЯ ПАТРУЛЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public void SetPatrolRoute(PatrolRoute route)
        {
            patrolRoute = route;
            hasPatrolRoute = route != null && route.WaypointCount > 0;
            currentWaypointIndex = 0;
            frozenWaypointIndex = 0;
            chaseStartPosition = null;
            isPatrolling = hasPatrolRoute && behaviorMode == EnemyBehaviorMode.Patrol;
        }

        // ━━━ ПРОВЕРКА ВИДИМОСТИ (Raycast Line of Sight) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public bool CanSeePlayer(Vector2 enemyPosition, Vector2 playerPosition)
        {
            // Если слой стен не настроен — считаем что игрок виден
            if (wallLayer.value == 0)
                return true;

            Vector2 direction = playerPosition - enemyPosition;
            float distance = direction.magnitude;

            // Raycast от врага к игроку
            RaycastHit2D hit = Physics2D.Raycast(enemyPosition, direction.normalized, distance, wallLayer);

            // Если не попали в стену — игрок виден
            return hit.collider == null;
        }

        // ━━━ ПОЛУЧЕНИЕ НАПРАВЛЕНИЯ ДВИЖЕНИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public Vector2 GetMoveDirection(Vector2 enemyPosition, Vector2? playerPosition, bool playerIsDead)
        {
            // ━━━ Игрок мёртв или отсутствует ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            if (playerPosition == null || playerIsDead)
            {
                if (isReturningToPatrol && chaseStartPosition.HasValue)
                {
                    return CalculateReturnToPatrolDirection(enemyPosition);
                }

                if (behaviorMode == EnemyBehaviorMode.Patrol && hasPatrolRoute && isPatrolling)
                {
                    return CalculatePatrolDirection(enemyPosition);
                }

                return CalculateReturnToSpawnDirection(enemyPosition);
            }

            float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition.Value);

            // ━━━ Игрок в радиусе аггро — ПРОВЕРКА ВИДИМОСТИ ━━━━━━━━━━━━━━━━━━━━━━━━━━
            if (distanceToPlayer <= aggroRange)
            {
                // Проверка видимости через стены
                bool canSee = CanSeePlayer(enemyPosition, playerPosition.Value);

                if (!canSee)
                {
                    // Игрок в радиусе но за стеной — не преследуем
                    if (isChasing)
                    {
                        isChasing = false;
                        Debug.Log($"[EnemyAICore] 🔍 PLAYER HIDDEN: In aggro range but behind wall");
                    }
                    return Vector2.zero;
                }

                // Игрок виден — начинаем погоню
                if (!isChasing)
                {
                    frozenWaypointIndex = currentWaypointIndex;
                    chaseStartPosition = enemyPosition;
                    isChasing = true;
                    isPatrolling = false;
                    isReturningToPatrol = false;

                    Debug.Log($"[EnemyAICore] ⚠️ PLAYER SPOTTED! Mode: {behaviorMode} | Position: ({enemyPosition.x:F2}, {enemyPosition.y:F2}) | Frozen Waypoint: {frozenWaypointIndex}");
                }

                lastKnownPlayerPosition = playerPosition.Value;
                return (playerPosition.Value - enemyPosition).normalized;
            }

            // ━━━ Игрок вне радиуса аггро — ВОЗВРАТ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            if (distanceToPlayer > aggroRange)
            {
                if (isChasing && !isReturningToPatrol)
                {
                    isChasing = false;
                    if (behaviorMode == EnemyBehaviorMode.Patrol && hasPatrolRoute)
                    {
                        isReturningToPatrol = true;
                        Debug.Log($"[EnemyAICore] 🏃 Player escaped! Returning to chase start position ({chaseStartPosition.Value.x:F2}, {chaseStartPosition.Value.y:F2})");
                    }
                    else
                    {
                        isReturningToSpawn = true;
                        Debug.Log($"[EnemyAICore] 🏃 Player escaped! Returning to spawn ({spawnPosition.x:F2}, {spawnPosition.y:F2})");
                    }
                }

                // Возврат к точке погони
                if (isReturningToPatrol && chaseStartPosition.HasValue)
                {
                    return CalculateReturnToPatrolDirection(enemyPosition);
                }

                // + ПРОВЕРКА: если Patrol и патрулим → продолжать патруль
                if (behaviorMode == EnemyBehaviorMode.Patrol && hasPatrolRoute && isPatrolling)
                {
                    return CalculatePatrolDirection(enemyPosition);
                }

                return CalculateReturnToSpawnDirection(enemyPosition);
            }

            return Vector2.zero;
        }

        // ━━━ ПАТРУЛИРОВАНИЕ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private Vector2 CalculatePatrolDirection(Vector2 enemyPosition)
        {
            if (!hasPatrolRoute || patrolRoute == null)
            {
                Debug.LogWarning("[EnemyAICore] No patrol route available!");
                return Vector2.zero;
            }

            PatrolWaypoint currentWaypoint = patrolRoute.GetWaypoint(currentWaypointIndex);
            if (currentWaypoint == null)
            {
                Debug.LogWarning($"[EnemyAICore] Waypoint at index {currentWaypointIndex} is null!");
                return Vector2.zero;
            }

            Vector2 direction = (Vector2)currentWaypoint.transform.position - enemyPosition;
            float distanceToWaypoint = direction.magnitude;

            if (distanceToWaypoint <= currentWaypoint.WaypointRadius)
            {
                Debug.Log($"[EnemyAICore] ✓ Waypoint {currentWaypointIndex} REACHED at ({currentWaypoint.transform.position.x:F2}, {currentWaypoint.transform.position.y:F2})!");

                currentWaypointIndex++;
                if (currentWaypointIndex >= patrolRoute.WaypointCount)
                {
                    currentWaypointIndex = patrolRoute.IsLooping ? 0 : patrolRoute.WaypointCount - 1;
                }

                OnPatrolWaypointReached?.Invoke();
            }

            return direction.normalized;
        }

        // ━━━ ВОЗВРАТ К СПАВНУ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private Vector2 CalculateReturnToSpawnDirection(Vector2 enemyPosition)
        {
            Vector2 direction = spawnPosition - enemyPosition;
            float distanceToSpawn = direction.magnitude;

            if (distanceToSpawn <= 0.1f)
            {
                if (isReturningToSpawn)
                {
                    isReturningToSpawn = false;
                    OnReturnToSpawnComplete?.Invoke();
                }
                return Vector2.zero;
            }

            if (!isReturningToSpawn)
            {
                isReturningToSpawn = true;
                OnReturnToSpawnStart?.Invoke();
            }

            return direction.normalized;
        }

        // ━━━ ВОЗВРАТ К ПОЗИЦИИ НАЧАЛА ПОГОНИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private Vector2 CalculateReturnToPatrolDirection(Vector2 enemyPosition)
        {
            if (!chaseStartPosition.HasValue)
            {
                isReturningToPatrol = false;
                return Vector2.zero;
            }

            Vector2 direction = chaseStartPosition.Value - enemyPosition;
            float distanceToPatrol = direction.magnitude;

            if (distanceToPatrol <= 0.5f)
            {
                Debug.Log($"[EnemyAICore] ✓ Returned to chase start position ({chaseStartPosition.Value.x:F2}, {chaseStartPosition.Value.y:F2}) - resuming patrol from waypoint {frozenWaypointIndex}");
                isReturningToPatrol = false;
                chaseStartPosition = null;
                currentWaypointIndex = frozenWaypointIndex;
                isPatrolling = true;
            }

            return direction.normalized;
        }

        // ━━━ АТАКА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public bool ShouldAttack(Vector2 enemyPosition, Vector2 playerPosition, bool isAttacking, bool isHurt)
        {
            if (isAttacking || isHurt) return false;
            return Vector2.Distance(enemyPosition, playerPosition) <= attackRange;
        }

        // ━━━ ПРОВЕРКА ПЕРЕКРЫТИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public bool IsOverlapping(Vector2 enemyPosition, Vector2 playerPosition, float threshold = 0.01f)
            => Vector2.Distance(enemyPosition, playerPosition) < threshold;

        // ━━━ ИЗБЕЖАНИЕ СТОЛКНОВЕНИЙ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public Vector2 GetEscapeDirection(Vector2 enemyPosition, Vector2 playerPosition)
        {
            var dir = (enemyPosition - playerPosition).normalized;
            return dir.sqrMagnitude > 0.01f ? dir : Vector2.up;
        }

        // ━━━ СБРОС ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public void Reset()
        {
            isReturningToSpawn = false;
            isPatrolling = hasPatrolRoute && behaviorMode == EnemyBehaviorMode.Patrol;
            isReturningToPatrol = false;
            isChasing = false;
            currentWaypointIndex = 0;
            frozenWaypointIndex = 0;
            chaseStartPosition = null;
            lastKnownPlayerPosition = null;
        }

        // ━━━ ПУБЛИЧНЫЕ МЕТОДЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public bool IsReturningToSpawn() => isReturningToSpawn;
        public bool IsPatrolling() => isPatrolling && hasPatrolRoute;
        public bool IsChasing() => isChasing;
        public int GetCurrentWaypointIndex() => currentWaypointIndex;
        public EnemyBehaviorMode GetBehaviorMode() => behaviorMode;
    }
}