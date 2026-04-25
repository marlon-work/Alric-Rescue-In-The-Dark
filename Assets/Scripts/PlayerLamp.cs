using UnityEngine;

// Activa la lámpara del jugador al iniciar la escena
public class PlayerLamp : MonoBehaviour
{
    [Tooltip("Objeto hijo que representa la lámpara o el efecto de luz.")]
    public GameObject lampObject;

    // Busca la lámpara entre los hijos si no fue asignada, y la activa
    void Start()
    {
        // Si no se asignó la lámpara en el Inspector, buscarla por nombre entre los hijos
        if (lampObject == null)
        {
            Transform lampChild = transform.Find("Lamp");
            if (lampChild != null)
                lampObject = lampChild.gameObject;
        }

        // Activar la lámpara si se encontró o asignó correctamente
        if (lampObject != null)
            lampObject.SetActive(true);
    }
}