using UnityEngine;
using UnityEngine.UI;

// Barra de vida en pantalla que sigue a un objetivo en el mundo (por ejemplo, un enemigo)
public class WorldHealthBar : MonoBehaviour
{
    [Tooltip("Componente Health del objetivo a observar (asigna el Health del enemigo)")]
    public Health target;

    [Tooltip("Transform que se sigue en el mundo (normalmente el transform del enemigo)")]
    public Transform followTarget;
    public Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    [Tooltip("Cámara usada para convertir de coordenadas del mundo a pantalla. Si es null se usa Camera.main.")]
    public Camera uiCamera;

    [Tooltip("Imagen que muestra la barra de vida (se recomienda usar Image Type = Filled)")]
    public Image fillImage;

    [Tooltip("Si es true, el elemento UI se desactiva cuando el objetivo muere")]
    public bool hideOnDie = true;

    RectTransform rect;

    void Awake()
    {
        // Guardamos el RectTransform para mover la UI
        rect = GetComponent<RectTransform>();
        if (uiCamera == null) uiCamera = Camera.main;
    }

    void OnEnable()
    {
        // Nos suscribimos a los eventos de la vida del objetivo
        if (target != null)
        {
            target.onHurt.AddListener(UpdateFill);
            target.onDie.AddListener(OnTargetDie);
        }
        UpdateFill();
    }

    void OnDisable()
    {
        // Limpiamos las suscripciones para evitar fugas de memoria
        if (target != null)
        {
            target.onHurt.RemoveListener(UpdateFill);
            target.onDie.RemoveListener(OnTargetDie);
        }
    }

    void Update()
    {
        // Mover la UI en pantalla para seguir al objetivo en el mundo
        if (followTarget != null && rect != null && uiCamera != null)
        {
            Vector3 worldPos = followTarget.position + worldOffset;
            Vector3 screenPos = uiCamera.WorldToScreenPoint(worldPos);
            rect.position = screenPos;
        }

        // Si el target no existe, ocultamos la barra si está configurado
        if (target == null)
        {
            if (hideOnDie && fillImage != null)
            {
                fillImage.fillAmount = 0f;
                gameObject.SetActive(false);
            }
            return;
        }
        if (fillImage != null)
            fillImage.fillAmount = target.maxHealth > 0 ? (float)target.currentHealth / target.maxHealth : 0f;
    }

    void UpdateFill()
    {
        // Actualiza el relleno de la barra según la vida actual
        if (target == null || fillImage == null) return;
        fillImage.fillAmount = target.maxHealth > 0 ? (float)target.currentHealth / target.maxHealth : 0f;
    }

    void OnTargetDie()
    {
        // Cuando el objetivo muere, mostramos la barra vacía y la ocultamos si corresponde
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (hideOnDie) gameObject.SetActive(false);
    }
}

