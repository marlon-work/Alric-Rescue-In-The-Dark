using UnityEngine;

// Comportamiento de la flecha: aplica daño al impactar y se destruye sola
[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public int damage = 1;
    public float lifeTime = 5f;

    // Referencia al GameObject que disparó la flecha para ignorar colisiones con él
    [HideInInspector]
    public GameObject owner;

    // Programa la autodestrucción al finalizar el tiempo de vida
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // Detecta impacto con colliders en modo trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    // Detecta impacto con colliders físicos normales
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    // Maneja la lógica de impacto: ignora al dueño, aplica daño y se destruye
    void HandleHit(Collider2D other)
    {
        if (other == null) return;

        // Ignorar colisión si el objeto golpeado pertenece al mismo que disparó la flecha
        if (owner != null)
        {
            if (other.gameObject == owner || other.transform.IsChildOf(owner.transform) || other.transform.root == owner.transform.root)
                return;
        }

        // Buscar Health en el collider o en cualquier padre de la jerarquía
        var h = other.GetComponentInParent<Health>();
        if (h != null)
        {
            h.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destruirse al golpear el entorno si el collider no es trigger
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}