using UnityEngine;

public class Enemy_Weapon : MonoBehaviour
{
    [SerializeField] private float damage = 20f;
    private bool hasHit = false; // ← Флаг для предотвращения спама урона

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryApplyDamage(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryApplyDamage(collision);
    }

    private void TryApplyDamage(Collider2D collision)
    {
        if (hasHit) return; // ← Уже нанесли урон в этой атаке

        PlayerMovement player = collision.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.TakeDamage(damage, transform.parent.parent.position);
            hasHit = true; // ← Блокируем повторный урон
        }
    }

    // Вызывается из Enemy_Attack при начале новой атаки
    public void ResetHit()
    {
        hasHit = false;
    }
}