using UnityEngine;
using UnityEngine.UI;

// Barra de vida en pantalla (HUD) que refleja el estado del componente Health del jugador
public class HealthBarUI : MonoBehaviour
{
    [Tooltip("Componente Health del jugador que se va a observar.")]
    public Health target;

    [Tooltip("Imagen usada como relleno de la barra (debe tener Image Type = Filled).")]
    public Image fillImage;

    [Tooltip("Si está activo, oculta el elemento de UI cuando el objetivo muere.")]
    public bool hideOnDie = true;

    // Suscribe los eventos de salud al activarse el componente
    void OnEnable()
    {
        if (target != null)
        {
            target.onHurt.AddListener(UpdateFill);
            target.onDie.AddListener(OnTargetDie);
        }
        UpdateFill();
    }

    // Cancela la suscripción a los eventos al desactivarse para evitar referencias perdidas
    void OnDisable()
    {
        if (target != null)
        {
            target.onHurt.RemoveListener(UpdateFill);
            target.onDie.RemoveListener(OnTargetDie);
        }
    }

    // Intenta asignar automáticamente fillImage si hay una Image hija disponible
    void Reset()
    {
        fillImage = GetComponentInChildren<Image>();
    }

    // Actualiza el relleno cada frame como respaldo en caso de que los eventos fallen
    void Update()
    {
        // Si el objetivo fue destruido, ocultar la barra
        if (target == null)
        {
            if (hideOnDie && fillImage != null)
            {
                fillImage.fillAmount = 0f;
                gameObject.SetActive(false);
            }
            return;
        }

        if (fillImage == null) return;

        // Calcula el porcentaje de vida restante y lo aplica al relleno de la imagen
        fillImage.fillAmount = target.maxHealth > 0 ? (float)target.currentHealth / target.maxHealth : 0f;
    }

    // Recalcula el relleno de la barra al recibir el evento de daño
    void UpdateFill()
    {
        if (target == null || fillImage == null) return;
        fillImage.fillAmount = target.maxHealth > 0 ? (float)target.currentHealth / target.maxHealth : 0f;
    }

    // Vacía la barra y la oculta al recibir el evento de muerte
    void OnTargetDie()
    {
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (hideOnDie) gameObject.SetActive(false);
    }
}