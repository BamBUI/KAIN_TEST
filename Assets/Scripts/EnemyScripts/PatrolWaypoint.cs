using UnityEngine;

namespace Assets.Scripts.EnemyScripts
{
    /// <summary>
    /// Точка патрулирования для врагов
    /// Визуализируется через Gizmos в редакторе
    /// </summary>
    public class PatrolWaypoint : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform nextWaypoint;  // + Следующая точка маршрута
        [SerializeField] private float waypointRadius = 0.3f;  // + Радиус для визуализации

        [Header("Gizmos")]
        [SerializeField] private Color gizmoColor = Color.yellow;  // + Цвет точки
        [SerializeField] private Color lineColor = Color.green;  // + Цвет линии пути

        // ━━━ ПУБЛИЧНЫЙ ДОСТУП ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public Transform NextWaypoint => nextWaypoint;
        public float WaypointRadius => waypointRadius;

        // ━━━ GIZMOS (ВИЗУАЛИЗАЦИЯ В РЕДАКТОРЕ) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnDrawGizmos()
        {
            // 1. Рисуем точку waypoint (сфера)
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, waypointRadius);

            // 2. Рисуем линию к следующему waypoint
            if (nextWaypoint != null)
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(transform.position, nextWaypoint.position);

                // 3. Рисуем маленькую сферу на следующем waypoint
                Gizmos.color = gizmoColor;
                Gizmos.DrawSphere(nextWaypoint.position, waypointRadius);
            }
        }

        // ━━━ ОТЛАДКА (ВЫБОР ОБЪЕКТА В РЕДАКТОРЕ) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnDrawGizmosSelected()
        {
            // Более яркая визуализация при выборе
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, waypointRadius * 1.5f);
        }
    }
}