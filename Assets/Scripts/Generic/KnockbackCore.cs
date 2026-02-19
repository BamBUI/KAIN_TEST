using UnityEngine;

namespace Assets.Scripts.Generic // ← ИСПРАВЛЕНО: общее пространство имён
{
    /// <summary>
    /// Модуль отталкивания (общий для всех персонажей и объектов)
    /// </summary>
    public class KnockbackCore
    {
        private readonly Rigidbody2D rb;

        public KnockbackCore(Rigidbody2D rb)
        {
            if (rb == null)
                throw new System.ArgumentNullException(nameof(rb), "Rigidbody2D cannot be null");

            this.rb = rb;
        }

        /// <summary>
        /// Применить отталкивание ОТ атакующего
        /// </summary>
        public void ApplyFromAttacker(Vector2 victimPosition, Vector2 attackerPosition, float distance, bool resetVelocity = true)
        {
            // Защита от некорректной дистанции
            if (distance < 0f)
            {
                Debug.LogWarning($"[KnockbackCore] Negative distance ({distance}) clamped to 0", rb.gameObject);
                distance = 0f;
            }

            Vector2 direction = victimPosition - attackerPosition;

            // Защита от совпадения позиций
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.up;
            }
            else
            {
                // Защита от NaN перед нормализацией
                if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y))
                {
                    Debug.LogWarning($"[KnockbackCore] Invalid direction (NaN/Infinity), using fallback", rb.gameObject);
                    direction = Vector2.up;
                }
                else
                {
                    direction = direction.normalized;
                }
            }

            Apply(direction, distance, resetVelocity);
        }

        /// <summary>
        /// Применить отталкивание В ЗАДАННОМ НАПРАВЛЕНИИ
        /// </summary>
        public void Apply(Vector2 direction, float distance, bool resetVelocity = true)
        {
            // Защита от некорректной дистанции
            if (distance < 0f)
            {
                Debug.LogWarning($"[KnockbackCore] Negative distance ({distance}) clamped to 0", rb.gameObject);
                distance = 0f;
            }

            // Защита от нулевого/некорректного направления
            if (direction.sqrMagnitude < 0.01f || !float.IsFinite(direction.x) || !float.IsFinite(direction.y))
            {
                Debug.LogWarning($"[KnockbackCore] Invalid direction, using fallback (up)", rb.gameObject);
                direction = Vector2.up;
            }
            else
            {
                direction = direction.normalized;
            }

            // Применяем отталкивание к позиции
            // Важно: вызывать ДО физического обновления (в Update, а не FixedUpdate)
            rb.position += direction * distance;

            // Сбрасываем скорость для чистого отталкивания
            if (resetVelocity)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Применить отталкивание ОТ точки (синоним ApplyFromAttacker)
        /// </summary>
        public void ApplyFromPoint(Vector2 victimPosition, Vector2 sourcePoint, float distance, bool resetVelocity = true)
        {
            ApplyFromAttacker(victimPosition, sourcePoint, distance, resetVelocity);
        }
    }
}