using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float stopDistance = 0.05f;
    public float attackRange = 1.2f;   // Радиус атаки (настройте под спрайты)
    public float aggroRange = 5.0f;    // Радиус агрессии (враг замечает игрока)

    public float hurtDuration = 1.0f;
    public float faintDuration = 4.0f;
    public float deathDuration = 2.0f;
    public float knockbackDistance = 0.4f; // Отталкивание на 40 см

    private Vector2 spawnPosition;      // Точка спауна
    private bool isReturningToSpawn = false; // Флаг возврата

    [SerializeField] private Transform enemyAim;

    Rigidbody2D rb;
    Transform target;
    Vector2 moveDirection;
    Animator animator;
    float lastInputX = 0f;
    float lastInputY = -1f;
    float health;
    public float maxHealth = 3f;

    bool isDying = false;
    bool isHurt = false;
    bool isAttacking = false; // ← НОВОЕ: блокировка движения во время атаки

    [SerializeField] private Enemy_Attack enemyAttack; // ← Ссылка на компонент атаки

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = maxHealth;
        spawnPosition = transform.position;
    }

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            Debug.LogError("Player not found!", gameObject);
        }
    }

    // ← НОВЫЕ МЕТОДЫ (зеркала из PlayerMovement)
    public void SetIsAttacking(bool value) => isAttacking = value;

    public Vector2 GetLastDirection() =>
        lastInputX != 0f || lastInputY != 0f ?
            new Vector2(lastInputX, lastInputY).normalized :
            Vector2.down;

    void Update()
    {
        // 1. БЕЗОПАСНАЯ ПРОВЕРКА ЦЕЛИ
        if (target == null || isDying) return;

        // 2. ПОЛУЧАЕМ КОЛЛАЙДЕРЫ
        var enemyCol = GetComponent<Collider2D>();
        var playerCol = target.GetComponent<Collider2D>();
        if (enemyCol == null || playerCol == null) return;

        // 3. ПРОВЕРКА СОСТОЯНИЯ ИГРОКА
        if (target == null || target.gameObject == null)
        {
            // Цель уничтожена — возвращаемся к спауну
            ReturnToSpawn();
            UpdateAnimation();
            return;
        }

        var player = target.GetComponent<PlayerMovement>();
        if (player == null || player.IsDead)
        {
            // Игрок мёртв — немедленно возвращаемся к спауну
            ReturnToSpawn();
            UpdateAnimation();
            return;
        }

        // Игрок жив — проверяем зону агрессии
        float distanceToPlayer = Vector2.Distance(transform.position, target.position);
        if (distanceToPlayer > aggroRange)
        {
            ReturnToSpawn();
            UpdateAnimation();
            return;
        }

        // 4. АВАРИЙНОЕ ВЫТАЛКИВАНИЕ ПРИ ПЕРЕСЕЧЕНИИ
        var dist = Physics2D.Distance(enemyCol, playerCol);
        if (dist.isOverlapped)
        {
            Vector2 escapeDir = (transform.position - target.position).normalized;
            rb.position += escapeDir * 0.2f;
            rb.linearVelocity = Vector2.zero;

            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.SetFloat("InputX", moveDirection.x);
                animator.SetFloat("InputY", moveDirection.y);
                animator.SetFloat("LastInputX", lastInputX);
                animator.SetFloat("LastInputY", lastInputY);
            }
            return;
        }

        // 5. БЛОКИРУЕМ ДВИЖЕНИЕ ВО ВРЕМЯ БОЛИ ИЛИ АТАКИ
        if (isHurt || isAttacking)
        {
            moveDirection = Vector2.zero; // ← КЛЮЧЕВОЕ: обнуляем направление ДО анимации

            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.SetFloat("InputX", moveDirection.x);
                animator.SetFloat("InputY", moveDirection.y);
                animator.SetFloat("LastInputX", lastInputX);
                animator.SetFloat("LastInputY", lastInputY);
            }
            return; // ← Выходим из Update() — логика движения ниже НЕ выполняется
        }

        // 6. ЛОГИКА ДВИЖЕНИЯ
        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            moveDirection = direction.normalized;
            lastInputX = moveDirection.x;
            lastInputY = moveDirection.y;
        }
        else
        {
            moveDirection = Vector2.zero;
        }

        // 7. ПРОВЕРКА ЗОНЫ АТАКИ
        float currentDist = Physics2D.Distance(enemyCol, playerCol).distance;
        if (currentDist <= attackRange && !isAttacking && !isHurt)
        {
            moveDirection = Vector2.zero;

            // ← ВЫЗОВ АТАКИ (флаг уже будет установлен внутри Attack())
            if (enemyAttack != null)
            {
                enemyAttack.Attack();
            }
        }
        else if (currentDist <= stopDistance)
        {
            moveDirection = Vector2.zero;
        }

        // 8. ОБНОВЛЕНИЕ АНИМАЦИИ
        if (animator != null)
        {
            bool isWalking = moveDirection != Vector2.zero && !isAttacking;
            animator.SetBool("IsWalking", isWalking);
            animator.SetFloat("InputX", moveDirection.x);
            animator.SetFloat("InputY", moveDirection.y);
            animator.SetFloat("LastInputX", lastInputX);
            animator.SetFloat("LastInputY", lastInputY);
        }
    }

    void FixedUpdate()
    {
        if (isDying || target == null) return;

        // БЛОКИРУЕМ ФИЗИКУ во время боли или атаки
        if (isHurt || isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveDirection * moveSpeed;
    }

    public void TakeDamage(float damage)
    {
        if (isDying) return;

        // Отталкивание на 40 см
        if (target != null)
        {
            Vector2 direction = (transform.position - target.position).normalized;
            rb.position += direction * knockbackDistance;
            rb.linearVelocity = Vector2.zero;
        }

        health -= damage;

        animator.SetBool("IsHurt", true);
        StopCoroutine("ResetHurtState");
        StartCoroutine(ResetHurtState());
        isHurt = true;

        if (health <= 0 && !isDying)
        {
            isDying = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            StopCoroutine("ResetHurtState");
            StartCoroutine(DieSequence());
        }
    }

    IEnumerator ResetHurtState()
    {
        yield return new WaitForSeconds(hurtDuration);
        if (!isDying)
        {
            animator.SetBool("IsHurt", false);
            isHurt = false;
        }
    }

    IEnumerator DieSequence()
    {
        yield return new WaitForSeconds(hurtDuration);
        animator.SetBool("IsHurt", false);

        animator.SetBool("IsFaint", true);
        yield return new WaitForSeconds(faintDuration);

        animator.SetBool("IsFaint", false);
        animator.SetBool("IsDead", true);
        yield return new WaitForSeconds(deathDuration);

        Destroy(gameObject);
    }

    // Добавьте в класс Enemy (рядом с другими методами):
    public Vector2 GetDirectionToPlayer()
    {
        if (target == null) return Vector2.down;
        return ((Vector2)target.position - (Vector2)transform.position).normalized;
    }

    void LateUpdate()
    {
        if (target != null && !isDying && !isHurt)
        {
            // Вращаем Enemy_Aim в сторону игрока
            Vector2 direction = (target.position - transform.position).normalized;
            Vector3 aimDirection = Vector3.left * direction.x + Vector3.down * direction.y;
            enemyAim.rotation = Quaternion.LookRotation(Vector3.forward, aimDirection);
        }
    }

    private void ReturnToSpawn()
    {
        isReturningToSpawn = true;

        // Двигаемся к точке спауна
        Vector2 direction = spawnPosition - (Vector2)transform.position;
        float distanceToSpawn = direction.magnitude;

        if (distanceToSpawn > 0.1f) // Если ещё не на месте
        {
            moveDirection = direction.normalized;
            lastInputX = moveDirection.x;
            lastInputY = moveDirection.y;
        }
        else // Достигли спауна
        {
            moveDirection = Vector2.zero;
            isReturningToSpawn = false;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isWalking = moveDirection != Vector2.zero;
        animator.SetBool("IsWalking", isWalking);
        animator.SetFloat("InputX", moveDirection.x);
        animator.SetFloat("InputY", moveDirection.y);
        animator.SetFloat("LastInputX", lastInputX);
        animator.SetFloat("LastInputY", lastInputY);
    }
}