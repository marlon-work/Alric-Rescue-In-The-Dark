using UnityEngine;

// NPC decorativo que patrulla entre dos puntos, descansa un momento y repite el ciclo
public class NPCPatrol : MonoBehaviour
{
    [Header("Patrulla")]
    [Tooltip("Punto A de la patrulla.")]
    public Transform pointA;
    [Tooltip("Punto B de la patrulla.")]
    public Transform pointB;
    [Tooltip("Velocidad de desplazamiento.")]
    public float speed = 3f;

    [Header("Descanso")]
    [Tooltip("Tiempo en segundos que descansa al llegar a cada punto.")]
    public float restDuration = 2f;

    [Header("Animación")]
    [Tooltip("Nombre del parámetro bool de correr en el Animator.")]
    public string isRunningParam = "isRunning";

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Transform currentTarget;
    private bool isResting = false;

    void Awake()
    {
        animator       = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("NPCPatrol: asigna pointA y pointB en el Inspector.", this);
            enabled = false;
            return;
        }

        currentTarget = pointB;
    }

    void Update()
    {
        if (isResting) return;

        MoveTowardsTarget();

        if (Mathf.Abs(transform.position.x - currentTarget.position.x) < 0.1f)
        {
            currentTarget = currentTarget == pointB ? pointA : pointB;
            StartCoroutine(Rest());
        }
    }

    void MoveTowardsTarget()
    {
        // Mover directamente por Transform, sin física
        transform.position = Vector2.MoveTowards(
            transform.position,
            new Vector2(currentTarget.position.x, transform.position.y), // mantiene Y fijo
            speed * Time.deltaTime
        );

        float direction = currentTarget.position.x > transform.position.x ? 1f : -1f;
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction < 0f;

        if (animator != null)
            animator.SetBool(isRunningParam, true);
    }

    System.Collections.IEnumerator Rest()
    {
        isResting = true;
        if (animator != null) animator.SetBool(isRunningParam, false);

        yield return new WaitForSeconds(restDuration);

        isResting = false;
    }

    void OnDrawGizmosSelected()
    {
        if (pointA != null) { Gizmos.color = Color.cyan;  Gizmos.DrawWireSphere(pointA.position, 0.2f); }
        if (pointB != null) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(pointB.position, 0.2f); }
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}