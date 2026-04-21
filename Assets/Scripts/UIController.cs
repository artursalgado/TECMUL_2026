using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reseta o estado quando volta ao menu principal
        if (scene.name == "MainMenu" || scene.name == "Menu")
        {
            // Destroi o UIController ao voltar ao menu para evitar conflitos
            Instance = null;
            Destroy(gameObject);
        }
    }

    public void SetGameStateUI(GameManager.GameState state)
    {
        Debug.Log($"[UIController] SetGameStateUI: {state}");

        switch (state)
        {
            case GameManager.GameState.MainMenu:
                // Menu principal - garante cursor visivel
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameManager.GameState.Playing:
                // Esconde qualquer menu de pausa aberto
                if (PauseMenu.Instance != null)
                {
                    // Nao chama Resume() aqui para evitar loop
                    // O PauseMenu gerencia seu proprio estado
                }
                break;

            case GameManager.GameState.Paused:
                // O PauseMenu gerencia sua propria UI
                break;

            case GameManager.GameState.GameOver:
                // Garante cursor visivel para interacao com UI
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }
}
