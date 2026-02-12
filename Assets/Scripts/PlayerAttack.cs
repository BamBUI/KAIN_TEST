using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackDuration = 0.35f; // ← Длительность блокировки
    [SerializeField] private Animator animator;
    [SerializeField] public GameObject Melee;

    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void Attack(InputAction.CallbackContext context)
    {
        // ← ДОБАВЛЕНА ПРОВЕРКА: блокируем атаку если уже атакуем
        if (context.started && !playerMovement.IsAttacking())
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        playerMovement.SetIsAttacking(true);

        Melee.SetActive(true);
        Vector2 dir = playerMovement.GetLastDirection();
        animator.SetFloat("AttackDirX", dir.x);
        animator.SetFloat("AttackDirY", dir.y);

        animator.SetTrigger("AttackTrigger");

        yield return new WaitForSeconds(attackDuration);

        playerMovement.SetIsAttacking(false);
        Melee.SetActive(false);
    }
}