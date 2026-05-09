using UnityEngine;

// Agrega sonidos a las animaciones del jugador: salto, daño, muerte y disparo.
// Adjunta este script al mismo GameObject que tiene PlayerMovement y Health.
[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : MonoBehaviour
{
    [Header("Clips de sonido")]
    [Tooltip("Sonido al saltar.")]
    public AudioClip jumpSound;

    [Tooltip("Sonido al recibir daño.")]
    public AudioClip hurtSound;

    [Tooltip("Sonido al morir.")]
    public AudioClip deathSound;

    [Tooltip("Sonido al disparar.")]
    public AudioClip shootSound;

    [Header("Volúmenes (0 a 1)")]
    [Range(0f, 1f)] public float jumpVolume   = 1f;
    [Range(0f, 1f)] public float hurtVolume   = 1f;
    [Range(0f, 1f)] public float deathVolume  = 1f;
    [Range(0f, 1f)] public float shootVolume  = 1f;

    private AudioSource audioSource;
    private PlayerMovement playerMovement;
    private Health health;

    private Animator animator;

    void Awake()
    {
        audioSource    = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();
        health         = GetComponent<Health>();
        animator       = GetComponent<Animator>();

        // Configurar AudioSource para efectos de sonido (no debe tener clip propio)
        audioSource.playOnAwake = false;
        audioSource.loop        = false;
    }

    void Start()
    {
        // Suscribirse a los eventos de Health
        if (health != null)
        {
            health.onHurt.AddListener(PlayHurtSound);
            health.onDie.AddListener(PlayDeathSound);
        }

        // Suscribirse al evento de disparo expuesto por PlayerMovement
        if (playerMovement != null)
        {
            playerMovement.onShoot.AddListener(PlayShootSound);
            playerMovement.onJump.AddListener(PlayJumpSound);
        }
    }

    void OnDestroy()
    {
        // Desuscribirse para evitar referencias huérfanas
        if (health != null)
        {
            health.onHurt.RemoveListener(PlayHurtSound);
            health.onDie.RemoveListener(PlayDeathSound);
        }

        if (playerMovement != null){
            playerMovement.onShoot.RemoveListener(PlayShootSound);
            playerMovement.onJump.RemoveListener(PlayJumpSound); 
        }
    }

    // ── Métodos públicos llamados por los eventos ─────────────────────────

    public void PlayJumpSound()
    {
        Play(jumpSound, jumpVolume);
    }

    public void PlayHurtSound()
    {
        Play(hurtSound, hurtVolume);
        if (animator != null) animator.SetTrigger("Hurt");
    }

    public void PlayDeathSound()
    {
        Play(deathSound, deathVolume);
        if (animator != null) animator.SetTrigger("Death");
    }

    public void PlayShootSound()
    {
        Play(shootSound, shootVolume);
    }

    // ── Utilidad ──────────────────────────────────────────────────────────

    void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}