using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Controla el movimiento, salto y disparo del jugador usando el nuevo Input System
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 6f;

    [Header("Salto")]
    public float jumpForce = 7f;
    public Transform groundCheck;
    public float groundDistance = 0.1f;
    public LayerMask groundMask;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float inputX;
    private bool jumpRequested;
    private bool isGrounded;
    private bool controlsEnabled = true; // Desactivar durante el stun por knockback

    [Header("Animación")]
    [Tooltip("Nombre del parámetro bool en el Animator que activa la animación de correr.")]
    public string isRunningParam = "isRunning";
    [Tooltip("Umbral mínimo de input para considerar que el jugador está corriendo (evita ruido del joystick).")]
    public float runThreshold = 0.1f;

    [Header("Disparo")]
    [Tooltip("Prefab del proyectil de flecha (debe tener Rigidbody2D).")]
    public GameObject arrowPrefab;
    [Tooltip("Transform usado como punto de origen de la flecha. Si es nulo, el script busca un hijo llamado 'Arrow'.")]
    public Transform arrowSpawnPoint;
    [Tooltip("Velocidad de la flecha al ser instanciada.")]
    public float arrowSpeed = 10f;
    [Tooltip("Nombre del parámetro bool en el Animator para la animación de disparo.")]
    public string isShootingParam = "isShooting";
    [Tooltip("Duración en segundos que isShooting permanece en true.")]
    public float shootingAnimDuration = 0.15f;
    [Tooltip("Si está activo, oculta cualquier hijo llamado 'Arrow' al iniciar.")]
    public bool hideArrowChildAtStart = true;

    // Obtiene componentes y configura el punto de spawn de la flecha al iniciar
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Si no hay punto de spawn asignado, buscar un hijo llamado "Arrow"
        if (arrowSpawnPoint == null)
        {
            Transform child = transform.Find("Arrow");
            if (child != null)
                arrowSpawnPoint = child;
        }

        // Ocultar los hijos llamados "Arrow" para que no sean visibles en reposo
        if (hideArrowChildAtStart)
        {
            int hiddenCount = 0;
            var children = GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                if (t == transform) continue;
                if (t.name == "Arrow")
                {
                    // Desactivar el GameObject del hijo
                    if (t.gameObject.activeSelf)
                        t.gameObject.SetActive(false);

                    // Como respaldo, deshabilitar el SpriteRenderer para asegurar invisibilidad
                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = false;
                    hiddenCount++;
                }
            }
            Debug.Log($"PlayerMovement: hideArrowChildAtStart ocultó {hiddenCount} hijo(s) Arrow");
        }

        // Desactivar instancias sueltas del prefab Arrow colocadas por error directamente en la escena
        int deactivatedSceneArrows = 0;
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            var trs = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in trs)
            {
                if (t == null) continue;
                if (t.name != "Arrow") continue;

                // Ignorar si es el punto de spawn o un hijo del jugador
                if (arrowSpawnPoint != null && (t == arrowSpawnPoint || t.IsChildOf(arrowSpawnPoint))) continue;
                if (t.IsChildOf(this.transform)) continue;

                if (t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    deactivatedSceneArrows++;
                }
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr != null && sr.enabled)
                    sr.enabled = false;
            }
        }
        if (deactivatedSceneArrows > 0)
            Debug.Log($"PlayerMovement: desactivó {deactivatedSceneArrows} objeto(s) Arrow sueltos en la escena");

        // Crear groundCheck automáticamente si no fue asignado en el Inspector
        if (groundCheck == null)
        {
            GameObject go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            groundCheck = go.transform;
        }
    }

    // Reposiciona al jugador en el SpawnPoint del nivel si existe uno
    void Start()
    {
        PlayerSpawnPoint spawnPoint = FindFirstObjectByType<PlayerSpawnPoint>();
        if (spawnPoint != null)
            transform.position = spawnPoint.transform.position;
    }

    // Lee el input de teclado y gamepad, gestiona salto, disparo y animaciones cada frame
    void Update()
    {
        float horizontal = 0f;

        // Leer movimiento horizontal desde teclado
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        }

        // Sumar input del stick izquierdo del gamepad
        var gamepad = Gamepad.current;
        if (gamepad != null)
            horizontal += gamepad.leftStick.x.ReadValue();

        // Aplicar input solo si los controles están habilitados (no durante stun)
        inputX = controlsEnabled ? Mathf.Clamp(horizontal, -1f, 1f) : 0f;

        // Detectar salto: Espacio en teclado o botón sur del gamepad
        bool jumpPressed = false;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) jumpPressed = true;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) jumpPressed = true;
        if (jumpPressed) jumpRequested = true;

        // Detectar disparo: click izquierdo del ratón o gatillo/bumper derecho del gamepad
        bool shootPressed = false;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) shootPressed = true;
        if (gamepad != null && (gamepad.rightTrigger.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame)) shootPressed = true;
        if (shootPressed) Shoot();

        // Activar animación de correr si el input supera el umbral mínimo
        bool isRunning = Mathf.Abs(inputX) > runThreshold;
        if (animator != null)
            animator.SetBool(isRunningParam, isRunning);

        // Voltear el sprite según la dirección del movimiento
        if (Mathf.Abs(inputX) > runThreshold)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = inputX < 0f;
            }
            else
            {
                // Alternativa si no hay SpriteRenderer: voltear por escala
                Vector3 s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (inputX < 0f ? -1f : 1f);
                transform.localScale = s;
            }
        }
    }

    // Aplica movimiento horizontal y salto usando física en el ciclo fijo
    void FixedUpdate()
    {
        // Verificar si el jugador toca el suelo mediante un círculo de detección 2D
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundDistance, groundMask) != null;

        // Aplicar velocidad horizontal solo si los controles están activos; durante el stun no se sobreescribe para que el knockback funcione
        if (controlsEnabled)
        {
            Vector2 lv = rb.linearVelocity;
            lv.x = inputX * speed;
            rb.linearVelocity = lv;
        }

        // Aplicar salto si fue solicitado y el jugador está en el suelo
        if (jumpRequested && isGrounded)
        {
            Vector2 lv2 = rb.linearVelocity;
            lv2.y = jumpForce;
            rb.linearVelocity = lv2;
        }
        jumpRequested = false;
    }

    // Deshabilita temporalmente los controles del jugador durante el tiempo de stun
    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;
        StartCoroutine(StunCoroutine(duration));
    }

    // Corrutina que mantiene los controles desactivados durante el stun
    IEnumerator StunCoroutine(float duration)
    {
        controlsEnabled = false;
        yield return new WaitForSeconds(duration);
        controlsEnabled = true;
    }

    // Instancia una flecha en la dirección que mira el jugador y le aplica velocidad
    void Shoot()
    {
        Debug.Log("PlayerMovement: Shoot() llamado");
        if (arrowPrefab == null)
        {
            Debug.LogWarning("El prefab de flecha no está asignado en PlayerMovement.");
            return;
        }

        // Calcular posición de spawn con un pequeño offset hacia adelante para evitar solapamiento con el jugador
        Transform spawn = arrowSpawnPoint != null ? arrowSpawnPoint : transform;
        Vector3 spawnPos = spawn.position;
        float dirPreview = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
        spawnPos += new Vector3(dirPreview * 0.2f, 0f, 0f);

        // Instanciar el proyectil en la posición calculada
        GameObject proj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        if (proj == null)
        {
            Debug.LogWarning("PlayerMovement: Instantiate devolvió null para arrowPrefab.");
            return;
        }

        // Asignar al jugador como dueño para evitar que la flecha lo dañe a él mismo
        var arrowComp = proj.GetComponent<Arrow>();
        if (arrowComp != null)
            arrowComp.owner = this.gameObject;

        // Ignorar colisiones entre la flecha recién creada y los colliders del jugador
        var projCol = proj.GetComponent<Collider2D>();
        if (projCol != null)
        {
            var ownerCols = GetComponentsInChildren<Collider2D>(true);
            foreach (var oc in ownerCols)
            {
                if (oc == null) continue;
                Physics2D.IgnoreCollision(oc, projCol, true);
            }
        }

        proj.SetActive(true);

        // Determinar dirección del disparo según el sprite o la escala del transform
        float dir = 1f;
        if (spriteRenderer != null)
            dir = spriteRenderer.flipX ? -1f : 1f;
        else
            dir = transform.localScale.x < 0f ? -1f : 1f;

        // Obtener o agregar Rigidbody2D al proyectil y aplicar la velocidad de vuelo
        Rigidbody2D arb = proj.GetComponent<Rigidbody2D>();
        if (arb == null)
        {
            Debug.Log("PlayerMovement: la flecha no tenía Rigidbody2D, se añadió uno como alternativa");
            arb = proj.AddComponent<Rigidbody2D>();
            arb.bodyType = RigidbodyType2D.Dynamic;
        }
        Vector2 lv = arb.linearVelocity;
        lv.x = dir * arrowSpeed;
        arb.linearVelocity = lv;

        // Ajustar el sprite del proyectil según la dirección y asegurarse de que sea visible
        SpriteRenderer par = proj.GetComponent<SpriteRenderer>();
        if (par != null)
        {
            par.flipX = dir < 0f;
            if (!par.enabled) par.enabled = true;
            // Colocar la flecha por delante del sprite del jugador en el orden de renderizado
            par.sortingOrder = Mathf.Max(par.sortingOrder, spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : par.sortingOrder + 1);
        }
        else
        {
            // Alternativa si no hay SpriteRenderer: voltear por escala
            Vector3 s = proj.transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir < 0f ? -1f : 1f);
            proj.transform.localScale = s;
        }

        Debug.Log($"PlayerMovement: flecha instanciada en {spawnPos} dir={dir} velocidad={arrowSpeed}");

        // Activar animación de disparo y restablecerla tras la duración configurada
        if (animator != null && !string.IsNullOrEmpty(isShootingParam))
        {
            animator.SetBool(isShootingParam, true);
            StartCoroutine(ResetShootingAfterDelay());
        }
    }

    // Restablece el parámetro de animación de disparo tras la duración configurada
    IEnumerator ResetShootingAfterDelay()
    {
        yield return new WaitForSeconds(shootingAnimDuration);
        if (animator != null && !string.IsNullOrEmpty(isShootingParam))
            animator.SetBool(isShootingParam, false);
    }

    // Dibuja en el editor el radio del groundCheck para facilitar la depuración visual
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}