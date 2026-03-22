using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI healthText;
    public GameObject gameOverPanel;

    private int score = 0;
    private bool gameIsOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        gameOverPanel.SetActive(false);
        UpdateScoreUI();
    }

    public void AddScore(int amount)
    {
        if (gameIsOver) return;
        score += amount;
        UpdateScoreUI();
    }

    public void UpdateHealthUI(float current, float max)
    {
        if (healthText != null)
            healthText.text = "HP: " + (int)current + " / " + (int)max;
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    public void GameOver()
    {
        if (gameIsOver) return;
        gameIsOver = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    // Chamado pelo botao Reiniciar no GameOver panel
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Chamado pelo botao Sair
    public void QuitGame()
    {
        Application.Quit();
    }
}
