using UnityEngine;

// Salida de nivel: carga el siguiente nivel cuando el jugador entra en el trigger
public class LevelExit : MonoBehaviour
{
    // Detecta si el jugador tocó la zona de salida y solicita cargar el siguiente nivel
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager.Instance.LoadNextLevel();
        }
    }
}