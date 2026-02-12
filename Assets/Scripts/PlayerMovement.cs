using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float health, maxHealth = 3f;
    [SerializeField] public Transform Aim;
    [SerializeField] private float hurtDuration = 0.5f;
    [SerializeField] private float deathDuration = 2f;
    [SerializeField] private float knockbackDistance = 0.4f; // Отталкивание назад

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastRawDirection = Vector2.down;
    private Animator animator;
    private bool isAttacking = false;
    private bool isHurt = false;
    private float invincibilityTimer = 0f;
    public bool IsDead { get; private set; } = false;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = maxHealth;
    }

    public void SetIsAttacking(bool value) => isAttacking = value;
    public bool IsAttacking() => isAttacking;

    public Vector2 GetLastDirection() =>
        lastRawDirection.sqrMagnitude > 0.01f ? lastRawDirection.normalized : Vector2.down;

    void Update()
    {
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
        }

        rb.linearVelocity = isAttacking ? Vector2.zero : moveInput * moveSpeed;
    }

    void LateUpdate()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f && !isAttacking && !isHurt;
        animator.SetBool("IsWalking", isMoving);

        if (isMoving)
        {
            lastRawDirection = moveInput;
            Vector2 walkDir = moveInput.normalized;
            animator.SetFloat("InputX", walkDir.x);
            animator.SetFloat("InputY", walkDir.y);
            Vector3 vector3 = Vector3.left * walkDir.x + Vector3.down * walkDir.y;
            Aim.rotation = Quaternion.LookRotation(Vector3.forward, vector3);
        }
        else
        {
            Vector2 normalizedLastDir = lastRawDirection.normalized;
            animator.SetFloat("LastInputX", normalizedLastDir.x);
            animator.SetFloat("LastInputY", normalizedLastDir.y);
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void TakeDamage(float damage, Vector2 attackerPosition)
    {
        if (invincibilityTimer > 0f || health <= 0 || IsDead) return;

        health -= damage;

        // Отталкивание
        Vector2 knockbackDirection = ((Vector2)transform.position - attackerPosition).normalized;
        rb.position += knockbackDirection * knockbackDistance;
        rb.linearVelocity = Vector2.zero;

        // 🔑 СМЕРТЕЛЬНЫЙ УРОН: сбрасываем ВСЕ состояния
        if (health <= 0)
        {
            isAttacking = false; // ← ДОБАВЛЕНО: сброс атаки
            isHurt = false;
            animator.SetBool("IsHurt", false);

            StopAllCoroutines(); // ← ДОБАВЛЕНО: прерываем текущую атаку

            StartCoroutine(DieSequence());
            return;
        }

        // Обычный урон
        isHurt = true;
        animator.SetBool("IsHurt", true);
        StartCoroutine(ResetHurtState());
        invincibilityTimer = hurtDuration;
    }

    private IEnumerator ResetHurtState()
    {
        yield return new WaitForSeconds(hurtDuration);
        isHurt = false;
        animator.SetBool("IsHurt", false);
    }

    private IEnumerator DieSequence()
    {
        // 1. Сбрасываем все состояния движения
        isAttacking = false; // ← ИСПРАВЛЕНО: было = true
        isHurt = false;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        // 2. Сбрасываем анимационные параметры
        animator.SetBool("IsHurt", false);
        animator.SetBool("IsWalking", false);

        // 3. Устанавливаем смерть
        IsDead = true;
        animator.SetBool("IsDead", true);

        // 4. Ждём анимацию
        yield return new WaitForSeconds(deathDuration);

        // 5. Уничтожаем
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastRawDirection = moveInput;
            Vector2 walkDir = moveInput.normalized;
            Vector3 vector3 = Vector3.left * walkDir.x + Vector3.down * walkDir.y;
            Aim.rotation = Quaternion.LookRotation(Vector3.forward, vector3);
        }
    }
}