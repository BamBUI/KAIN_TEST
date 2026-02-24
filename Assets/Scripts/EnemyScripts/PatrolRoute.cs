using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.EnemyScripts
{
    /// <summary>
    /// Маршрут патрулирования (коллекция waypoints)
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private List<PatrolWaypoint> waypoints = new List<PatrolWaypoint>();
        [SerializeField] private bool isLooping = true;  // + Зациклить маршрут

        // ━━━ ПУБЛИЧНЫЙ ДОСТУП ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public List<PatrolWaypoint> Waypoints => waypoints;
        public bool IsLooping => isLooping;
        public int WaypointCount => waypoints.Count;

        // ━━━ МЕТОДЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public PatrolWaypoint GetWaypoint(int index)
        {
            if (index < 0 || index >= waypoints.Count) return null;
            return waypoints[index];
        }

        public PatrolWaypoint GetNextWaypoint(int currentIndex)
        {
            if (waypoints.Count == 0) return null;

            int nextIndex = currentIndex + 1;

            if (nextIndex >= waypoints.Count)
            {
                if (isLooping)
                    nextIndex = 0;  // + Возврат к началу
                else
                    nextIndex = waypoints.Count - 1;  // + Остановка на последнем
            }

            return waypoints[nextIndex];
        }

        public PatrolWaypoint GetPreviousWaypoint(int currentIndex)
        {
            if (waypoints.Count == 0) return null;

            int prevIndex = currentIndex - 1;

            if (prevIndex < 0)
            {
                if (isLooping)
                    prevIndex = waypoints.Count - 1;  // + Возврат к концу
                else
                    prevIndex = 0;  // + Остановка на первом
            }

            return waypoints[prevIndex];
        }

        // ━━━ ОТЛАДКА В РЕДАКТОРЕ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnDrawGizmos()
        {
            if (waypoints.Count < 2) return;

            Gizmos.color = Color.green;

            for (int i = 0; i < waypoints.Count; i++)
            {
                PatrolWaypoint current = waypoints[i];
                PatrolWaypoint next = GetNextWaypoint(i);

                if (current != null && next != null)
                {
                    Gizmos.DrawLine(current.transform.position, next.transform.position);
                }
            }
        }
    }
}