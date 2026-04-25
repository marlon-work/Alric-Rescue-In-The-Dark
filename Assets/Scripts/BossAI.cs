using System.Collections;
using UnityEngine;

// IA del jefe final: persigue al jugador, dispara flechas, salta plataformas y tiene animaciones propias
[RequireComponent(typeof(Rigidbody2D))]
public class BossAI : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 4f;             // Velocidad de desplazamiento horizontal
    public float jumpForce = 6f;         // Fuerza del salto al perseguir al jugador
    public Transform groundCheck;        // Punto de verificación de contacto con el suelo
    public float groundDistance = 0.1f;  // Radio del círculo de detección de suelo
    public LayerMask groundMask;         // Capas que se consideran suelo
    public float detectionRange = 10f;   // Distancia máxima para detectar al jugador

    [Header("Disparo")]
    public GameObject arrowPrefab;       // Prefab del proyectil que dispara el jefe
    public Transform arrowSpawnPoint;    // Punto de origen del proyectil
    public float arrowSpeed = 8f;        // Velocidad del proyectil al ser disparado
    public float shootCooldown = 2f;     // Tiempo mínimo entre disparos
    public int contactDamage = 1;        // Daño que aplica al jugador por contacto directo

    [Header("Animación")]
    public Animator animator;
    public string isMovingParam = "isMoving";
    public string isIdleParam = "isIdle";
    public string isJumpingParam = "isJumping";
    public string isShootingParam = "isShooting";
    public string hurtTrigger = "Hurt";
    public string deathTrigger = "Death";

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool isGrounded;            // Indica si el jefe está tocando el suelo
    private float lastShootTime;        // Marca de tiempo del último disparo realizado
    private bool isDead = false;        // Evita ejecutar lógica después de morir

    // Obtiene componentes, crea el punto de suelo y busca el spawn de proyectiles si no están asignados
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        // Crear groundCheck automáticamente si no fue asignado en el Inspector
        if (groundCheck == null)
        {
            GameObject go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            groundCheck = go.transform;
        }

        // Buscar el punto de spawn del proyectil entre los hijos del GameObject
        if (arrowSpawnPoint == null)
        {
            arrowSpawnPoint = FindSpawnPoint("ArrowSpawn");
        }
    }

    // Busca el punto de spawn del proyectil por nombre entre los hijos más comunes
    Transform FindSpawnPoint(string defaultName)
    {
        Transform child = transform.Find(defaultName);
        if (child != null) return child;

        child = transform.Find("BulletSpawn");
        if (child != null) return child;

        child = transform.Find("Bullet");
        if (child != null) return child;

        child = transform.Find("SpawnPoint");
        if (child != null) return child;

        return null;
    }

    // Busca al jugador, suscribe eventos de salud e ignora colisiones con otros enemigos
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Suscribir los métodos de daño y muerte al componente Health
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.onHurt.AddListener(TakeDamage);
            health.onDie.AddListener(Die);
        }

        // Evitar que el jefe colisione físicamente con otros enemigos o jefes
        IgnoreCollisionsWithEnemies();
    }

    // Configura la física para ignorar colisiones entre el jefe y todos los enemigos/jefes del nivel
    void IgnoreCollisionsWithEnemies()
    {
        var myColliders = GetComponentsInChildren<Collider2D>(true);

        // Ignorar colisiones con todos los EnemyAI presentes en la escena
        var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.gameObject == gameObject) continue;
            var enemyColliders = enemy.GetComponentsInChildren<Collider2D>(true);
            foreach (var myCol in myColliders)
            {
                foreach (var enemyCol in enemyColliders)
                {
                    Physics2D.IgnoreCollision(myCol, enemyCol, true);
                }
            }
        }

        // Ignorar colisiones con otros BossAI en caso de que haya más de uno
        var bosses = Object.FindObjectsByType<BossAI>(FindObjectsSortMode.None);
        foreach (var boss in bosses)
        {
            if (boss.gameObject == gameObject) continue;
            var bossColliders = boss.GetComponentsInChildren<Collider2D>(true);
            foreach (var myCol in myColliders)
            {
                foreach (var bossCol in bossColliders)
                {
                    Physics2D.IgnoreCollision(myCol, bossCol, true);
                }
            }
        }
    }

    // Controla el comportamiento principal: movimiento, disparo y animaciones cada frame
    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Perseguir al jugador si está dentro del rango de detección, si no patrullar
        if (distanceToPlayer <= detectionRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            Patrol();
        }

        // Disparar si el jugador está en rango y el cooldown de disparo ha terminado
        if (distanceToPlayer <= detectionRange && Time.time >= lastShootTime + shootCooldown)
        {
            Shoot();
        }

        UpdateAnimations();
    }

    // Verifica cada frame de física si el jefe está en contacto con el suelo
    void FixedUpdate()
    {
        if (isDead) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundDistance, groundMask) != null;
    }

    // Mueve al jefe horizontalmente hacia el jugador y salta si el jugador está más arriba
    void MoveTowardsPlayer()
    {
        if (player == null) return;

        float direction = player.position.x > transform.position.x ? 1f : -1f;

        // Aplicar velocidad horizontal manteniendo la velocidad vertical actual
        Vector2 velocity = rb.linearVelocity;
        velocity.x = direction * speed;
        rb.linearVelocity = velocity;

        // Saltar si el jugador está significativamente más alto y el jefe toca el suelo
        if (player.position.y > transform.position.y + 1f && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Voltear el sprite según la dirección del movimiento
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0f;
        }
    }

    // Comportamiento mientras el jugador está fuera del rango de detección (actualmente idle)
    void Patrol()
    {
        // Detener el movimiento horizontal; aquí se puede ampliar con una lógica de patrulla real
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    // Instancia un proyectil y lo lanza en dirección al jugador
    void Shoot()
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("BossAI: arrowPrefab no está asignado.", this);
            return;
        }
        if (arrowSpawnPoint == null)
        {
            Debug.LogWarning("BossAI: arrowSpawnPoint no está asignado ni se encontró un hijo ArrowSpawn/BulletSpawn/SpawnPoint.", this);
            return;
        }

        lastShootTime = Time.time;

        float direction = player.position.x > transform.position.x ? 1f : -1f;

        // Ajustar posición de spawn ligeramente en la dirección del disparo
        Vector3 spawnPos = arrowSpawnPoint.position;
        spawnPos += new Vector3(direction * 0.2f, 0f, 0f);

        GameObject proj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        if (proj != null)
        {
            AssignProjectileOwner(proj);

            // Evitar que el proyectil colisione con el jefe que lo disparó
            var projCol = proj.GetComponent<Collider2D>();
            if (projCol != null)
            {
                var bossCols = GetComponentsInChildren<Collider2D>(true);
                foreach (var bc in bossCols)
                {
                    Physics2D.IgnoreCollision(bc, projCol, true);
                }
            }

            // Asignar o crear Rigidbody2D al proyectil y aplicar la velocidad de vuelo
            Rigidbody2D arb = proj.GetComponent<Rigidbody2D>();
            if (arb == null) arb = proj.AddComponent<Rigidbody2D>();
            arb.linearVelocity = new Vector2(direction * arrowSpeed, 0);

            // Voltear el sprite del proyectil si va hacia la izquierda
            SpriteRenderer sr = proj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = direction < 0f;
        }

        // Activar animación de disparo y restablecerla tras un breve delay
        if (animator != null && !string.IsNullOrEmpty(isShootingParam))
        {
            animator.SetBool(isShootingParam, true);
            StartCoroutine(ResetShootingAfterDelay(0.2f));
        }
    }

    // Asigna este jefe como dueño del proyectil para evitar que se dañe a sí mismo
    void AssignProjectileOwner(GameObject proj)
    {
        var arrowComp = proj.GetComponent<Arrow>();
        if (arrowComp != null)
        {
            arrowComp.owner = this.gameObject;
            return;
        }

        // Intentar asignar el dueño a un componente Bullet genérico por reflexión
        var bulletComp = proj.GetComponent("Bullet");
        if (bulletComp != null)
        {
            var ownerField = bulletComp.GetType().GetField("owner");
            if (ownerField != null)
            {
                ownerField.SetValue(bulletComp, this.gameObject);
                return;
            }

            var ownerProperty = bulletComp.GetType().GetProperty("owner");
            if (ownerProperty != null && ownerProperty.CanWrite)
            {
                ownerProperty.SetValue(bulletComp, this.gameObject);
            }
        }
    }

    // Aplica daño al jugador si colisiona directamente con él
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.collider.CompareTag("Player")) return;

        var playerHealth = collision.collider.GetComponentInParent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(contactDamage);
        }
    }

    // Sincroniza los parámetros del Animator con el estado físico actual del jefe
    void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        animator.SetBool(isMovingParam, isMoving);
        animator.SetBool(isIdleParam, !isMoving);
        animator.SetBool(isJumpingParam, !isGrounded);
    }

    // Reproduce la animación de daño al recibir un golpe
    public void TakeDamage()
    {
        if (isDead) return;

        if (animator != null && !string.IsNullOrEmpty(hurtTrigger))
        {
            animator.SetTrigger(hurtTrigger);
        }
    }

    // Activa la secuencia de muerte: animación, detiene el movimiento y destruye el GameObject
    public void Die()
    {
        if (isDead) return;

        isDead = true;

        if (animator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            animator.SetTrigger(deathTrigger);
        }

        // Detener todo movimiento físico al morir
        rb.linearVelocity = Vector2.zero;
        enabled = false;

        // Destruir el objeto después de que termine la animación de muerte
        StartCoroutine(DestroyAfterDelay(2f));
    }

    // Restablece el parámetro de animación de disparo tras el delay dado
    IEnumerator ResetShootingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null) animator.SetBool(isShootingParam, false);
    }

    // Destruye el GameObject del jefe tras esperar el tiempo de la animación de muerte
    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // Dibuja gizmos en el editor para visualizar el radio de suelo y el rango de detección
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}