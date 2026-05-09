using System.Collections;
using UnityEngine;

// IA del enemigo base: persigue al jugador, le aplica daño por contacto y lo empuja
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("IA")]
    public float detectionRadius = 5f;
    public float moveSpeed = 2f;
    public float stoppingDistance = 0.5f;

    [Header("Combate")]
    public int contactDamage = 1;
    public float contactCooldown = 1f;
    [Tooltip("Impulso horizontal aplicado al jugador al recibir daño (empuje lateral)")]
    public float knockbackForce = 3f;
    [Tooltip("Impulso vertical aplicado al jugador al recibir daño")]
    public float knockbackUpForce = 0.5f;
    [Tooltip("Duración en segundos en que se ignora la colisión entre enemigo y jugador tras el impacto")]
    public float collisionIgnoreDuration = 0.18f;
    [Tooltip("Separación de posición inmediata aplicada al jugador en metros para evitar solapamiento")]
    public float separationDistance = 0.25f;

    [Header("Patrulla")]
    [Tooltip("Distancia que el enemigo recorre a cada lado desde su posición inicial.")]
    public float patrolRange = 3f;
    [Tooltip("Velocidad de patrulla (independiente de la velocidad de persecución).")]
    public float patrolSpeed = 1.5f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float lastContactTime = -999f;
    private Vector2 startPosition;
    private float patrolDirection = 1f;

    [Header("Sprite")]
[Tooltip("Activa esto si el sprite base mira hacia la izquierda en lugar de la derecha.")]
public bool flipSpriteByDefault = false;

    void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition  = transform.position;
        IgnoreCollisionsWithOtherEnemies();
    }

    void IgnoreCollisionsWithOtherEnemies()
    {
        var myColliders  = GetComponentsInChildren<Collider2D>(true);
        var otherEnemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        foreach (var otherEnemy in otherEnemies)
        {
            if (otherEnemy.gameObject == gameObject) continue;
            var otherColliders = otherEnemy.GetComponentsInChildren<Collider2D>(true);
            foreach (var myCol in myColliders)
                foreach (var otherCol in otherColliders)
                    Physics2D.IgnoreCollision(myCol, otherCol, true);
        }
    }

    IEnumerator TemporarilyIgnoreCollisionsWith(GameObject other, float duration)
    {
        if (other == null) yield break;

        var myCols    = GetComponentsInChildren<Collider2D>(true);
        var otherCols = other.GetComponentsInChildren<Collider2D>(true);
        var ignoredPairs = new System.Collections.Generic.List<(Collider2D, Collider2D)>();

        foreach (var a in myCols)
        {
            if (a == null) continue;
            foreach (var b in otherCols)
            {
                if (b == null) continue;
                Physics2D.IgnoreCollision(a, b, true);
                ignoredPairs.Add((a, b));
            }
        }

        yield return new WaitForSeconds(duration);

        foreach (var pair in ignoredPairs)
            if (pair.Item1 != null && pair.Item2 != null)
                Physics2D.IgnoreCollision(pair.Item1, pair.Item2, false);

        rb.simulated = true;
    }

    void Update()
    {
        if (player == null)
        {
            var pgo = GameObject.FindWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float dist          = Vector2.Distance(transform.position, player.position);
        bool playerDetected = dist <= detectionRadius;

        if (playerDetected && dist > stoppingDistance)
        {
            // Perseguir al jugador
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirX * moveSpeed, rb.linearVelocity.y);
            FlipSprite(dirX);
        }
        else if (!playerDetected)
        {
            // Patrullar entre los límites definidos por patrolRange
            float leftLimit  = startPosition.x - patrolRange;
            float rightLimit = startPosition.x + patrolRange;

            if (transform.position.x >= rightLimit)
                patrolDirection = -1f;
            else if (transform.position.x <= leftLimit)
                patrolDirection = 1f;

            rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, rb.linearVelocity.y);
            FlipSprite(patrolDirection);
        }
        else
        {
            // Dentro de stoppingDistance — detenerse
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

 void FlipSprite(float dirX)
{
    if (spriteRenderer != null)
    {
        bool goingLeft = dirX < 0f;
        spriteRenderer.flipX = flipSpriteByDefault ? goingLeft : !goingLeft;
    }
}

    void OnCollisionEnter2D(Collision2D collision)
    {
        var hitHealth = collision.collider.GetComponentInParent<Health>();
        if (hitHealth == null) return;
        if (!collision.collider.CompareTag("Player") && !hitHealth.gameObject.CompareTag("Player")) return;

        TryDamagePlayer(collision.collider);
    }

    void TryDamagePlayer(Collider2D col)
    {
        if (Time.time - lastContactTime < contactCooldown) return;
        lastContactTime = Time.time;

        var playerHealth = col.GetComponentInParent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(contactDamage);

            var playerRb = playerHealth.GetComponentInParent<Rigidbody2D>();
            if (playerRb != null)
            {
                float dirX = Mathf.Sign(playerHealth.transform.position.x - transform.position.x);

                Vector2 newVel = playerRb.linearVelocity;
                newVel.x = dirX * knockbackForce;
                newVel.y = knockbackUpForce;
                playerRb.linearVelocity = newVel;

                try
                {
                    playerRb.MovePosition(playerRb.position + new Vector2(dirX * separationDistance, 0f));
                }
                catch (System.Exception)
                {
                    playerRb.transform.position = (Vector2)playerRb.transform.position + new Vector2(dirX * separationDistance, 0f);
                }

                var myLv = rb.linearVelocity;
                myLv.y = 0f;
                myLv.x = -dirX * 0.5f;
                rb.linearVelocity = myLv;

                StartCoroutine(TemporarilyIgnoreCollisionsWith(playerHealth.gameObject, collisionIgnoreDuration));
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Vector2 origin = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin + Vector2.left  * patrolRange, origin + Vector2.right * patrolRange);
        Gizmos.DrawWireSphere(origin + Vector2.left  * patrolRange, 0.15f);
        Gizmos.DrawWireSphere(origin + Vector2.right * patrolRange, 0.15f);
    }
}