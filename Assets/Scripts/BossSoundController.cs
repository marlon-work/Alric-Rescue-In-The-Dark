using UnityEngine;

// Agrega sonidos al jefe final: ataque, daño y muerte.
// Adjunta este script al mismo GameObject que tiene BossAI y Health.
[RequireComponent(typeof(AudioSource))]
public class BossSoundController : MonoBehaviour
{
    [Header("Clips de sonido")]
    [Tooltip("Sonido al disparar.")]
    public AudioClip shootSound;

    [Tooltip("Sonido al recibir daño.")]
    public AudioClip hurtSound;

    [Tooltip("Sonido al morir.")]
    public AudioClip deathSound;

    [Header("Volúmenes (0 a 1)")]
    [Range(0f, 1f)] public float shootVolume = 1f;
    [Range(0f, 1f)] public float hurtVolume  = 1f;
    [Range(0f, 1f)] public float deathVolume = 1f;

    private AudioSource audioSource;
    private Health health;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        health      = GetComponent<Health>();

        audioSource.playOnAwake = false;
        audioSource.loop        = false;
    }

    void Start()
    {
        if (health != null)
        {
            health.onHurt.AddListener(PlayHurtSound);
            health.onDie.AddListener(PlayDeathSound);
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.onHurt.RemoveListener(PlayHurtSound);
            health.onDie.RemoveListener(PlayDeathSound);
        }
    }

    // Llamado desde BossAI cuando dispara — agrégalo en el método Shoot() de BossAI
    public void PlayShootSound()
    {
        Play(shootSound, shootVolume);
    }

    public void PlayHurtSound()
    {
        Play(hurtSound, hurtVolume);
    }

    public void PlayDeathSound()
    {
        Play(deathSound, deathVolume);
    }

    void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}