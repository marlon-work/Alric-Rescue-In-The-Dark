using UnityEngine;

// Hace que la cámara siga al personaje objetivo dentro de los límites del nivel
public class CameraFollow : MonoBehaviour
{
    public Transform target; // Transform del personaje que la cámara debe seguir
    public Vector3 offset = new Vector3(0, 5, -10); // Desplazamiento de la cámara respecto al objetivo
    public Vector3 minBounds = new Vector3(-10, 0, -10); // Límite mínimo de posición de la cámara
    public Vector3 maxBounds = new Vector3(10, 10, 10); // Límite máximo de posición de la cámara

    // Se ejecuta al final de cada frame para evitar que la cámara tiemble tras el movimiento del jugador
    void LateUpdate()
    {
        if (target != null)
        {
            // Calcula la posición deseada sumando el offset al objetivo
            Vector3 desiredPosition = target.position + offset;

            // Restringe la posición dentro de los límites del nivel en cada eje
            float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            float clampedZ = Mathf.Clamp(desiredPosition.z, minBounds.z, maxBounds.z);

            transform.position = new Vector3(clampedX, clampedY, clampedZ);
        }
    }
}