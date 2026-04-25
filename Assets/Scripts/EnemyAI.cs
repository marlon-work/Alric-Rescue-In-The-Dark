using System.Collections;
using UnityEngine;

// IA del enemigo base: persigue al jugador, le aplica daño por contacto y lo empuja
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("IA")]
    public float detectionRadius = 5f; // Radio en el que el enemigo detecta al jugador
    public float moveSpeed = 2f;       // Velocidad de desplazamiento hacia el jugador
    public float stoppingDistance = 0.5f; // Distancia mínima a la que el enemigo se detiene

    [Header("Combate")]
    public int contactDamage = 1;     // Daño que inflige al jugador al tocarlo
    public float contactCooldown = 1f; // Tiempo de espera entre cada daño por contacto
    [Tooltip("Impulso horizontal aplicado al jugador al reciibir daño (empuje lateral)")]
    public float knockbackForce = 3f;
    [Tooltip("Impulso vertical aplicado al jugador al recibir daño")]
    public float knockbackUpForce = 0.5f;
    [Tooltip("Duración en segundos en que se ignora la colisión entre enemigo y jugador tras el impacto para evitar que se peguen")]
    public float collisionIgnoreDuration = 0.18f;
    [Tooltip("Separación de posición inmediata aplicada al jugador en metros para evitar solapamiento")]
    public float separationDistance = 0.25f;

    private Transform player;
    private Rigidbody2D rb;
    private float lastContactTime = -999f; // Marca de tiempo del último contacto con el jugador

    // Obtiene el Rigidbody2D e ignora colisiones con otros enemigos al iniciar
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        IgnoreCollisionsWithOtherEnemies();
    }

    // Configura la física para que los enemigos no se empujen entre sí
    void IgnoreCollisionsWithOtherEnemies()
    {
        var myColliders = GetComponentsInChildren<Collider2D>(true);
        var otherEnemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        foreach (var otherEnemy in otherEnemies)
        {
            if (otherEnemy.gameObject == gameObject) continue;
            var otherColliders = otherEnemy.GetComponentsInChildren<Collider2D>(true);

            foreach (var myCol in myColliders)
            {
                foreach (var otherCol in otherColliders)
                {
                    Physics2D.IgnoreCollision(myCol, otherCol, true);
                }
            }
        }
    }

    // Ignora temporalmente la colisión entre este enemigo y otro objeto durante un tiempo dado
    IEnumerator TemporarilyIgnoreCollisionsWith(GameObject other, float duration)
    {
        if (other == null) yield break;

        var myCols = GetComponentsInChildren<Collider2D>(true);
        var otherCols = other.GetComponentsInChildren<Collider2D>(true);
        var ignoredPairs = new System.Collections.Generic.List<(Collider2D, Collider2D)>();

        // Desactivar colisiones entre los pares encontrados
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

        // Restaurar las colisiones una vez pasado el tiempo de ignorado
        foreach (var pair in ignoredPairs)
        {
            if (pair.Item1 != null && pair.Item2 != null)
            {
                Physics2D.IgnoreCollision(pair.Item1, pair.Item2, false);
            }
        }

        // Restaurar la simulación física del enemigo
        rb.simulated = true;
    }

    // Busca al jugador cada frame si aún no tiene referencia a él
    void Update()
    {
        if (player == null)
        {
            var pgo = GameObject.FindWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }
    }

    // Mueve al enemigo hacia el jugador si está dentro del radio de detección
    void FixedUpdate()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectionRadius && dist > stoppingDistance)
        {
            // Calcular dirección normalizada hacia el jugador y aplicar velocidad horizontal
            Vector2 dir = (player.position - transform.position).normalized;
            Vector2 vel = dir * moveSpeed;
            rb.linearVelocity = new Vector2(vel.x, rb.linearVelocity.y);
        }
        else
        {
            // Detener el movimiento horizontal si el jugador está fuera del rango o muy cerca
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    // Filtra colisiones: solo procesa daño si el objeto golpeado es el jugador
    void OnCollisionEnter2D(Collision2D collision)
    {
        var hitHealth = collision.collider.GetComponentInParent<Health>();
        if (hitHealth == null) return;
        if (!collision.collider.CompareTag("Player") && !hitHealth.gameObject.CompareTag("Player")) return;

        TryDamagePlayer(collision.collider);
    }

    // Aplica daño y empuje al jugador si el cooldown de contacto ya expiró
    void TryDamagePlayer(Collider2D col)
    {
        if (Time.time - lastContactTime < contactCooldown) return;
        lastContactTime = Time.time;

        // Buscar Health en el jugador o en su jerarquía padre
        var playerHealth = col.GetComponentInParent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(contactDamage);

            var playerRb = playerHealth.GetComponentInParent<Rigidbody2D>();
            if (playerRb != null)
            {
                // Calcular dirección del empuje: alejarse del enemigo
                float dirX = Mathf.Sign(playerHealth.transform.position.x - transform.position.x);

                // Aplicar velocidad de empuje directamente para evitar reacciones físicas exageradas
                Vector2 newVel = playerRb.linearVelocity;
                newVel.x = dirX * knockbackForce;
                newVel.y = knockbackUpForce;
                playerRb.linearVelocity = newVel;

                // Separar posiciones inmediatamente para evitar solapamiento o pegado
                try
                {
                    Vector2 sep = new Vector2(dirX * separationDistance, 0f);
                    playerRb.MovePosition(playerRb.position + sep);
                }
                catch (System.Exception)
                {
                    // Alternativa si MovePosition falla: mover directamente el transform
                    playerRb.transform.position = (Vector2)playerRb.transform.position + new Vector2(dirX * separationDistance, 0f);
                }

                // Reducir la velocidad vertical del enemigo para que no salga despedido por la colisión
                var myLv = rb.linearVelocity;
                myLv.y = 0f;
                myLv.x = -dirX * 0.5f; // Pequeño retroceso del enemigo al impactar
                rb.linearVelocity = myLv;

                // Ignorar colisiones temporalmente para evitar que se queden pegados
                StartCoroutine(TemporarilyIgnoreCollisionsWith(playerHealth.gameObject, collisionIgnoreDuration));
            }
        }
    }
}