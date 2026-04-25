using UnityEngine;

// Marca el lugar donde el jugador debe aparecer al iniciar o cargar el nivel
public class PlayerSpawnPoint : MonoBehaviour
{
    // Dibuja un gizmo en la escena para visualizar el punto de spawn
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.35f, transform.position - Vector3.up * 0.35f);
        Gizmos.DrawLine(transform.position + Vector3.right * 0.35f, transform.position - Vector3.right * 0.35f);
    }
}
