using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.EnemyScripts
{
    public class EnemyAICore
    {
        private readonly float aggroRange;
        private readonly float attackRange;
        private readonly float stopDistance;
        private readonly float overlapEscapeDistance = 0.2f;

        private readonly Vector2 spawnPosition;
        private bool isReturningToSpawn = false;

        public event Action OnReturnToSpawnStart;

        public event Action OnReturnToSpawnComplete;

        public EnemyAICore(float aggroRange, float attackRange, float stopDistance, Vector2 spawnPosition)
        {
            this.aggroRange = aggroRange;
            this.attackRange = attackRange;
            this.stopDistance = stopDistance;
            this.spawnPosition = spawnPosition;
        }

        public Vector2 GetMoveDirection(Vector2 enemyPosition, Vector2? playerPosition, bool playerIsDead)
        {
            if (playerPosition == null || playerIsDead)
            {
                return CalculateReturnToSpawnDirection(enemyPosition);
            }

            float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition.Value);
            if (distanceToPlayer > aggroRange)
            {
                return CalculateReturnToSpawnDirection(enemyPosition);
            }

            Vector2 directionToPlayer = (playerPosition.Value - enemyPosition).normalized;
            return directionToPlayer;
        }

        public bool ShouldAttack(Vector2 enemyPosition, Vector2 playerPosition, bool isAttacking, bool isHurt)
        {
            if (isAttacking || isHurt) return false;

            float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition);
            return distanceToPlayer <= attackRange;
        }

        public bool IsOverlapping(Vector2 enemyPosition, Vector2 playerPosition, float overlapThreshold = 0.01f)
        {
            float distance = Vector2.Distance(enemyPosition, playerPosition);
            return distance < overlapThreshold;
        }

        public Vector2 GetEscapeDirection(Vector2 enemyPosition, Vector2 playerPosition)
        {
            Vector2 direction = (enemyPosition - playerPosition).normalized;

            // Защита от нулевого вектора
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.up;
            }

            return direction;
        }

        private Vector2 CalculateReturnToSpawnDirection(Vector2 enemyPosition)
        {
            Vector2 direction = spawnPosition - enemyPosition;
            float distanceToSpawn = direction.magnitude;

            // Проверка достижения спауна
            if (distanceToSpawn <= 0.1f)
            {
                if (isReturningToSpawn)
                {
                    isReturningToSpawn = false;
                    OnReturnToSpawnComplete?.Invoke();
                }
                return Vector2.zero;
            }

            // Начало возврата к спауну
            if (!isReturningToSpawn)
            {
                isReturningToSpawn = true;
                OnReturnToSpawnStart?.Invoke();
            }

            return direction.normalized;
        }


        public void ForceReturnToSpawn()
        {
            isReturningToSpawn = true;
            OnReturnToSpawnStart?.Invoke();
        }

        public bool IsReturningToSpawn() => isReturningToSpawn;
    }
}
