using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance; // Singleton para acceder desde otros scripts

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persiste entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Carga el siguiente nivel
    public void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SaveCurrentLevel(nextSceneIndex);
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            // Fin del juego o loop al primer nivel
            Debug.Log("Juego completado");
        }
    }

    // Reinicia el nivel actual (usado cuando el personaje pierde vida)
    public void RestartCurrentLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    // Guarda el nivel actual
    private void SaveCurrentLevel(int levelIndex)
    {
        PlayerPrefs.SetInt("CurrentLevel", levelIndex);
        PlayerPrefs.Save();
    }

    // Carga el nivel guardado (al iniciar el juego)
    public void LoadSavedLevel()
    {
        int savedLevel = PlayerPrefs.GetInt("CurrentLevel", 0); // 0 es el primer nivel
        SceneManager.LoadScene(savedLevel);
    }

    // Método para guardar checkpoint (posición del jugador)
    public void SaveCheckpoint(Vector3 position)
    {
        PlayerPrefs.SetFloat("CheckpointX", position.x);
        PlayerPrefs.SetFloat("CheckpointY", position.y);
        PlayerPrefs.SetFloat("CheckpointZ", position.z);
        PlayerPrefs.Save();
    }

    // Carga la posición del checkpoint
    public Vector3 LoadCheckpoint()
    {
        float x = PlayerPrefs.GetFloat("CheckpointX", 0);
        float y = PlayerPrefs.GetFloat("CheckpointY", 0);
        float z = PlayerPrefs.GetFloat("CheckpointZ", 0);
        return new Vector3(x, y, z);
    }
}