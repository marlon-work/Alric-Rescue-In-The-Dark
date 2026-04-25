using UnityEngine;

// Zona de muerte: aplica daño letal al jugador al entrar en contacto con ella
[RequireComponent(typeof(Collider2D))]
public class DeathZone : MonoBehaviour
{
    [Tooltip("Cuánto daño aplica al jugador al entrar en la zona.")]
    public int damage = 9999;

    [Tooltip("Si el collider está en modo trigger, usa OnTriggerEnter2D; de lo contrario usa OnCollisionEnter2D.")]
    public bool useTrigger = true;

    [Tooltip("Tag que identifica al jugador.")]
    public string playerTag = "Player";

    // Se activa cuando un objeto entra al área definida como trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        TryKillPlayer(other);
    }

    // Se activa cuando un objeto colisiona físicamente con la zona (modo no-trigger)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (useTrigger) return;
        TryKillPlayer(collision.collider);
    }

    // Verifica si el collider pertenece al jugador y le aplica el daño configurado
    void TryKillPlayer(Collider2D collider)
    {
        if (collider == null) return;

        // Salir si el objeto que entró no tiene el tag del jugador
        if (!string.IsNullOrEmpty(playerTag) && !collider.CompareTag(playerTag)) return;

        // Buscar el componente Health en el jugador o en su jerarquía padre
        var health = collider.GetComponentInParent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }
}