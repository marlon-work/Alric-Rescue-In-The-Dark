using UnityEngine;
using UnityEngine.Events;

// Componente reutilizable de salud: gestiona vida, daño, curación y muerte
public class Health : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth { get; private set; }

    // Evento que se dispara cada vez que el personaje recibe daño
    public UnityEvent onHurt;

    // Evento que se dispara cuando la salud llega a cero
    public UnityEvent onDie;

    // Inicializa la salud actual al máximo al comenzar
    void Awake()
    {
        currentHealth = maxHealth;
    }

    // Aplica la cantidad de daño recibida y dispara los eventos correspondientes
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHealth -= amount;
        onHurt?.Invoke();

        // Si la salud llega o baja de cero, se activa la lógica de muerte
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            onDie?.Invoke();
            Die();
        }
    }

    // Restaura salud sin superar el máximo permitido
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    // Maneja la muerte: si es el jefe, delega en BossAI; si no, destruye el objeto
    void Die()
    {
        // El BossAI gestiona su propia muerte, así que se omite aquí
        if (GetComponent<BossAI>() != null) return;

        // Comportamiento por defecto: destruir el GameObject al morir
        Destroy(gameObject);
    }
}