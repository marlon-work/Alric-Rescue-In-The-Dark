using UnityEngine;

// Objeto de curación: al tocarlo el jugador recupera vida, suena un audio y desaparece
[RequireComponent(typeof(AudioSource))]
public class HealthPickup : MonoBehaviour
{
    [Header("Curación")]
    [Tooltip("Cantidad de vida que restaura al jugador.")]
    public int healAmount = 1;

    [Header("Sonido")]
    [Tooltip("Sonido que se reproduce al recoger el objeto.")]
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float pickupVolume = 1f;

    [Header("Visual")]
    [Tooltip("Si está activo, el objeto rota suavemente para verse más atractivo.")]
    public bool rotate = true;
    [Tooltip("Velocidad de rotación en grados por segundo.")]
    public float rotateSpeed = 90f;

    private bool collected = false;

    void Update()
    {
        if (rotate)
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        // Buscar el componente Health en el jugador o en su padre
        Health health = other.GetComponentInParent<Health>();
        if (health == null) return;

        // No recoger si el jugador ya tiene vida máxima
        if (health.currentHealth >= health.maxHealth) return;

        collected = true;

        // Curar al jugador
        health.Heal(healAmount);

        // Reproducir sonido y destruir el objeto
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

        Destroy(gameObject);
    }

    // Dibuja el área de detección en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        var col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
    }
}