using UnityEngine;

namespace Assets.Scripts.EnemyScripts
{
    public class EnemyMovementCore
    {
        private readonly Rigidbody2D rb;
        private readonly Transform aimTransform;
        private readonly float moveSpeed;
        private readonly float movementThreshold = 0.01f; // ← Явная константа для порога движения

        private Vector2 lastMoveDirection = Vector2.down;

        public EnemyMovementCore(Rigidbody2D rb, Transform aimTransform, float moveSpeed)
        {
            // Валидация зависимостей
            if (rb == null)
                throw new System.ArgumentNullException(nameof(rb), "Rigidbody2D cannot be null");
            if (aimTransform == null)
                throw new System.ArgumentNullException(nameof(aimTransform), "Aim Transform cannot be null");
            if (!float.IsFinite(moveSpeed) || moveSpeed < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(moveSpeed), "Move speed must be non-negative and finite");

            this.rb = rb;
            this.aimTransform = aimTransform;
            this.moveSpeed = moveSpeed;
        }

        public void UpdatePhysics(Vector2 moveDirection, bool isAttacking, bool isHurt)
        {
            // Защита от некорректного ввода
            if (!IsValidVector(moveDirection))
            {
                rb.linearVelocity = Vector2.zero;
                Debug.LogWarning($"[EnemyMovementCore] Invalid move direction (NaN/Infinity) on {aimTransform?.name}", aimTransform);
                return;
            }

            if (isAttacking || isHurt)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Защита от некорректной скорости
            if (!float.IsFinite(moveSpeed))
            {
                rb.linearVelocity = Vector2.zero;
                Debug.LogWarning($"[EnemyMovementCore] Invalid move speed (NaN/Infinity) on {aimTransform?.name}", aimTransform);
                return;
            }

            rb.linearVelocity = moveDirection * moveSpeed;

            if (moveDirection.sqrMagnitude > movementThreshold)
            {
                lastMoveDirection = moveDirection;
            }
        }

        public void UpdateAimRotation(Vector2 lookTargetPosition, Vector2 enemyPosition, bool shouldLook)
        {
            if (!shouldLook) return;

            // Защита от некорректных позиций
            if (!IsValidVector(lookTargetPosition) || !IsValidVector(enemyPosition))
            {
                Debug.LogWarning($"[EnemyMovementCore] Invalid position (NaN/Infinity) on {aimTransform?.name}", aimTransform);
                return;
            }

            // Защита от уничтоженного aimTransform
            if (aimTransform == null)
            {
                Debug.LogWarning($"[EnemyMovementCore] Aim Transform destroyed on {rb?.name}", rb);
                return;
            }

            Vector2 lookDirection = lookTargetPosition - enemyPosition;

            // Защита от нулевого вектора
            if (lookDirection.sqrMagnitude < movementThreshold)
            {
                lookDirection = lastMoveDirection.sqrMagnitude > movementThreshold
                    ? lastMoveDirection
                    : Vector2.down;
            }
            else
            {
                lookDirection = lookDirection.normalized;
            }

            // Финальная защита перед применением
            if (!IsValidVector(lookDirection))
            {
                lookDirection = Vector2.down;
            }

            Vector3 aimDirection = Vector3.left * lookDirection.x + Vector3.down * lookDirection.y;
            aimTransform.rotation = Quaternion.LookRotation(Vector3.forward, aimDirection);
        }

        public Vector2 GetLastMoveDirection() =>
            lastMoveDirection.sqrMagnitude > movementThreshold
                ? lastMoveDirection.normalized
                : Vector2.down;

        public bool IsMoving(Vector2 moveDirection) => moveDirection.sqrMagnitude > movementThreshold;

        // ━━━ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private bool IsValidVector(Vector2 vector)
        {
            return float.IsFinite(vector.x) && float.IsFinite(vector.y);
        }
    }
}