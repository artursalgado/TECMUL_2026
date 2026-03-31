using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI reasonText;
    public Button restartButton;
    public Button quitButton;

    [Header("Victory Panel")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryScoreText;
    public TextMeshProUGUI victoryTimeText;
    public Button victoryRestartButton;
    public Button victoryQuitButton;

    [Header("Visual")]
    public Image backgroundOverlay;
    public Color gameOverColor = new Color(0.6f, 0f, 0f, 0.75f);
    public Color victoryColor = new Color(0f, 0.4f, 0.1f, 0.75f);

    private float startTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        startTime = Time.time;
        Hide();
    }

    public void ShowGameOver(int score, string reason = "You died.")
    {
        if (backgroundOverlay != null)
            backgroundOverlay.color = gameOverColor;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        if (titleText != null) titleText.text = "GAME OVER";
        if (finalScoreText != null) finalScoreText.text = "Score: " + score;
        if (reasonText != null) reasonText.text = reason;

        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);
        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowVictory(int score)
    {
        float elapsed = Time.time - startTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);

        if (backgroundOverlay != null)
            backgroundOverlay.color = victoryColor;

        if (victoryPanel != null) victoryPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (victoryScoreText != null)
            victoryScoreText.text = "Score: " + score;
        if (victoryTimeText != null)
            victoryTimeText.text = $"Time: {minutes:00}:{seconds:00}";

        if (victoryRestartButton != null)
            victoryRestartButton.onClick.AddListener(Restart);
        if (victoryQuitButton != null)
            victoryQuitButton.onClick.AddListener(Quit);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Quit()
    {
        Application.Quit();
    }
}
