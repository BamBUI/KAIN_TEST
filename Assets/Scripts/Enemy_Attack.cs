using UnityEngine;
using System.Collections;

public class Enemy_Attack : MonoBehaviour
{
    [SerializeField] private float hitboxDuration = 0.01f;
    [SerializeField] private float attackAnimationDuration = 0.5f; // ← Настройте под вашу анимацию!
    [SerializeField] private float postAttackDelay = 0.3f;
    [SerializeField] private Animator animator;
    [SerializeField] public GameObject Melee;

    private Enemy enemy;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("Enemy component not found!", gameObject);
        }
    }

    public void Attack()
    {
        if (enemy == null) return;
        enemy.SetIsAttacking(true); // ← Устанавливаем СРАЗУ
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        if (Melee != null)
        {
            Enemy_Weapon weapon = Melee.GetComponent<Enemy_Weapon>();
            if (weapon != null) weapon.ResetHit();
            Melee.SetActive(true);
        }

        // Направление атаки (без доступа к приватному полю)
        Vector2 direction = enemy.GetDirectionToPlayer();
        animator.SetFloat("AttackDirX", direction.x);
        animator.SetFloat("AttackDirY", direction.y);
        animator.SetTrigger("AttackTrigger");

        yield return new WaitForSeconds(hitboxDuration);
        if (Melee != null) Melee.SetActive(false);

        // Ждём окончания анимации атаки
        yield return new WaitForSeconds(attackAnimationDuration - hitboxDuration);

        // ← НОВОЕ: задержка ПОСЛЕ атаки перед сбросом флага
        yield return new WaitForSeconds(postAttackDelay);

        if (enemy != null)
        {
            enemy.SetIsAttacking(false); // ← Сбрасываем ТОЛЬКО здесь
        }
    }
}