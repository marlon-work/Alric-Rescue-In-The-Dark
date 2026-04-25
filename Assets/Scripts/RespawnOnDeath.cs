using UnityEngine;

// Script que reinicia el nivel cuando el jugador muere
public class RespawnOnDeath : MonoBehaviour
{
    [Tooltip("Tag del jugador que escuchará la muerte para reiniciar el nivel.")]
    public string playerTag = "Player";

    private Health playerHealth;

    void Start()
    {
        if (string.IsNullOrEmpty(playerTag)) return;

        // Busca el jugador por tag en la escena
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        playerHealth = player.GetComponentInParent<Health>();
        if (playerHealth != null)
        {
            // Suscribirse al evento de muerte del jugador
            playerHealth.onDie.AddListener(OnPlayerDie);
        }
    }

    void OnPlayerDie()
    {
        // Cuando el jugador muere, reiniciar el nivel actual si existe LevelManager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartCurrentLevel();
        }
        else
        {
            Debug.LogWarning("RespawnOnDeath: LevelManager no encontrado, no se pudo reiniciar el nivel.", this);
        }
    }
}
