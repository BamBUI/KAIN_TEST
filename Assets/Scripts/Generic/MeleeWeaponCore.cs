using Assets.Scripts.CharacterControllers;
using UnityEngine;

namespace Assets.Scripts.Generic
{
    [RequireComponent(typeof(Collider2D))]
    public class MeleeWeapon : MonoBehaviour
    {
        [Header("Damage Settings")]
        [SerializeField] private float damage = 1f;

        [Header("Spam Protection")]
        [SerializeField] private bool useHitCooldown = true;

        private bool hasHit = false;
        private Transform cachedOwner; // Кэшируем владельца для проверки самоповреждения

        private void Awake()
        {
            // Кэшируем владельца при старте (игрок или враг)
            cachedOwner = transform.parent?.parent;
            if (cachedOwner == null)
            {
                Debug.LogWarning($"[MeleeWeapon] Unexpected hierarchy on {gameObject.name}. Expected: Character → Aim → Melee", this);
            }
        }

        private void OnEnable()
        {
            hasHit = false; // Чистое состояние для каждой атаки
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!enabled) return;
            TryApplyDamage(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!enabled) return;
            TryApplyDamage(collision);
        }

        private void TryApplyDamage(Collider2D collision)
        {
            // Защита от спама
            if (useHitCooldown && hasHit) return;

            // Защита от самоповреждения
            if (cachedOwner != null && collision.transform.IsChildOf(cachedOwner))
                return;

            // Игрок как цель
            var player = collision.GetComponent<PlayerController>();
            if (player != null && !player.IsDead())
            {
                player.TakeDamage(damage, GetAttackerPosition());
                hasHit = true;
                return;
            }

            // Враг как цель
            var enemy = collision.GetComponent<EnemyController>();
            if (enemy != null && !enemy.IsDead())
            {
                enemy.TakeDamage(damage, GetAttackerPosition());
                hasHit = true;
                return;
            }
        }

        public void ResetHit()
        {
            hasHit = false;
        }

        private Vector2 GetAttackerPosition()
        {
            return cachedOwner != null ? cachedOwner.position : transform.position;
        }
    }
}