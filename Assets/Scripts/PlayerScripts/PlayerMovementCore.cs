using UnityEngine;

namespace Assets.Scripts.PlayerScripts
{
    public class PlayerMovementCore
    {
        private readonly Rigidbody2D rb;
        private readonly Transform aimTransform;
        private readonly float moveSpeed;

        private Vector2 moveInput;
        private Vector2 lastRawDirection = Vector2.down;

        public PlayerMovementCore(Rigidbody2D rb, Transform aimTransform, float moveSpeed)
        {
            // Защита от некорректной инициализации
            if (rb == null)
                throw new System.ArgumentNullException(nameof(rb), "Rigidbody2D cannot be null");
            if (aimTransform == null)
                throw new System.ArgumentNullException(nameof(aimTransform), "Aim Transform cannot be null");
            if (moveSpeed < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(moveSpeed), "Move speed cannot be negative");

            this.rb = rb;
            this.aimTransform = aimTransform;
            this.moveSpeed = moveSpeed;
        }

        public void SetMoveInput(Vector2 input)
        {
            // Защита от NaN/Infinity
            if (!float.IsFinite(input.x) || !float.IsFinite(input.y))
            {
                moveInput = Vector2.zero;
                Debug.LogWarning($"[PlayerMovementCore] Invalid input detected (NaN/Infinity), resetting to zero", aimTransform);
                return;
            }
            moveInput = input;
        }

        public Vector2 GetLastDirection() =>
            lastRawDirection.sqrMagnitude > 0.01f ? lastRawDirection.normalized : Vector2.down;

        public bool IsMoving() => moveInput.sqrMagnitude > 0.01f;

        public void UpdatePhysics(bool isAttacking, bool isHurt)
        {
            if (isAttacking || isHurt)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Защита от некорректной скорости
            if (!float.IsFinite(moveSpeed))
            {
                rb.linearVelocity = Vector2.zero;
                Debug.LogWarning($"[PlayerMovementCore] Invalid moveSpeed (NaN/Infinity)", aimTransform);
                return;
            }

            // Защита от некорректного ввода
            if (!float.IsFinite(moveInput.x) || !float.IsFinite(moveInput.y))
            {
                rb.linearVelocity = Vector2.zero;
                moveInput = Vector2.zero;
                Debug.LogWarning($"[PlayerMovementCore] Invalid moveInput (NaN/Infinity), resetting velocity", aimTransform);
                return;
            }

            rb.linearVelocity = moveInput * moveSpeed;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                lastRawDirection = moveInput;
            }
        }

        public void UpdateAimRotation()
        {
            // Двойная защита: конструктор уже проверил, но на случай уничтожения объекта во время игры
            if (aimTransform == null) return;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector2 walkDir = moveInput.normalized;
                Vector3 aimDirection = Vector3.left * walkDir.x + Vector3.down * walkDir.y;
                aimTransform.rotation = Quaternion.LookRotation(Vector3.forward, aimDirection);
            }
            else if (lastRawDirection.sqrMagnitude > 0.01f)
            {
                Vector2 lookDir = lastRawDirection.normalized;
                Vector3 aimDirection = Vector3.left * lookDir.x + Vector3.down * lookDir.y;
                aimTransform.rotation = Quaternion.LookRotation(Vector3.forward, aimDirection);
            }
        }
    }
}