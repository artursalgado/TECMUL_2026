using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private GameObject runtimeCanvasRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureRuntimeGameOverScreen()
    {

        if (FindFirstObjectByType<GameOverScreen>() != null)
        {
            return;
        }

        GameObject go = new GameObject("GameOverScreen");
        go.AddComponent<GameOverScreen>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance.gameObject.activeInHierarchy)
            {
                DestroySafe(gameObject);
                return;
            }

            Instance = null;
        }

        Instance = this;
        BuildRuntimeUIIfNeeded();
    }

    void Start()
    {
        startTime = Time.unscaledTime;
        Hide();
    }

    public void ShowGameOver(int score, string reason = "You died.")
    {
        BuildRuntimeUIIfNeeded();
        Time.timeScale = 0f;

        if (backgroundOverlay != null)
        {
            backgroundOverlay.gameObject.SetActive(true);
            backgroundOverlay.color = gameOverColor;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        if (titleText != null) titleText.text = "GAME OVER";
        if (finalScoreText != null) finalScoreText.text = "Score: " + score;
        if (reasonText != null) reasonText.text = reason;

        BindButtons(restartButton, quitButton);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowVictory(int score)
    {
        BuildRuntimeUIIfNeeded();
        Time.timeScale = 0f;

        float elapsed = Time.unscaledTime - startTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);

        if (backgroundOverlay != null)
        {
            backgroundOverlay.gameObject.SetActive(true);
            backgroundOverlay.color = victoryColor;
        }

        if (victoryPanel != null) victoryPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (victoryScoreText != null)
            victoryScoreText.text = "Score: " + score;
        if (victoryTimeText != null)
            victoryTimeText.text = $"Time: {minutes:00}:{seconds:00}";

        BindButtons(victoryRestartButton, victoryQuitButton);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        if (backgroundOverlay != null) backgroundOverlay.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    void BuildRuntimeUIIfNeeded()
    {
        bool missingRefs =
            gameOverPanel == null ||
            titleText == null ||
            finalScoreText == null ||
            reasonText == null ||
            restartButton == null ||
            quitButton == null ||
            victoryPanel == null ||
            victoryScoreText == null ||
            victoryTimeText == null ||
            victoryRestartButton == null ||
            victoryQuitButton == null ||
            backgroundOverlay == null;

        if (!missingRefs)
        {
            return;
        }

        if (runtimeCanvasRoot != null)
        {
            return;
        }

        runtimeCanvasRoot = new GameObject("Game Over Canvas");
        Canvas canvas = runtimeCanvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = runtimeCanvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.45f;

        runtimeCanvasRoot.AddComponent<GraphicRaycaster>();

        backgroundOverlay = CreateImage("Overlay", runtimeCanvasRoot.transform, new Color(0f, 0f, 0f, 0.72f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        gameOverPanel = CreatePanel("GameOver Panel", runtimeCanvasRoot.transform, new Vector2(760f, 420f));
        titleText = CreateText("Title", gameOverPanel.transform, "GAME OVER", 56f, FontStyles.Bold, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(680f, 72f));
        finalScoreText = CreateText("FinalScore", gameOverPanel.transform, "Score: 0", 32f, FontStyles.Bold, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(680f, 50f));
        reasonText = CreateText("Reason", gameOverPanel.transform, "You died.", 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.85f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(680f, 52f));

        restartButton = CreateButton("Restart", gameOverPanel.transform, "RESTART", new Vector2(-130f, -152f));
        quitButton = CreateButton("Quit", gameOverPanel.transform, "QUIT", new Vector2(130f, -152f));

        victoryPanel = CreatePanel("Victory Panel", runtimeCanvasRoot.transform, new Vector2(760f, 420f));
        CreateText("VictoryTitle", victoryPanel.transform, "EXTRACTION SUCCESSFUL", 46f, FontStyles.Bold, new Color(0.73f, 0.98f, 0.73f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(700f, 72f));
        victoryScoreText = CreateText("VictoryScore", victoryPanel.transform, "Score: 0", 32f, FontStyles.Bold, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(680f, 50f));
        victoryTimeText = CreateText("VictoryTime", victoryPanel.transform, "Time: 00:00", 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.85f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(680f, 52f));

        victoryRestartButton = CreateButton("VictoryRestart", victoryPanel.transform, "RESTART", new Vector2(-130f, -152f));
        victoryQuitButton = CreateButton("VictoryQuit", victoryPanel.transform, "QUIT", new Vector2(130f, -152f));

        Hide();
    }

    void BindButtons(Button restart, Button quit)
    {
        if (restart != null)
        {
            restart.onClick.RemoveAllListeners();
            restart.onClick.AddListener(Restart);
        }

        if (quit != null)
        {
            quit.onClick.RemoveAllListeners();
            quit.onClick.AddListener(Quit);
        }
    }

    GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.14f);
        outline.effectDistance = new Vector2(2f, -2f);

        return panel;
    }

    TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.textWrappingMode = TextWrappingModes.Normal;

        return textComponent;
    }

    Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f, 54f);

        Image image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.16f, 0.2f, 0.24f, 0.92f);

        Button button = buttonGO.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.16f, 0.2f, 0.24f, 0.92f);
        colors.highlightedColor = new Color(0.24f, 0.29f, 0.35f, 0.96f);
        colors.pressedColor = new Color(0.11f, 0.14f, 0.17f, 0.96f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        button.colors = colors;

        Outline outline = buttonGO.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.1f);
        outline.effectDistance = new Vector2(1f, -1f);

        TextMeshProUGUI labelText = CreateText(
            "Label",
            buttonGO.transform,
            label,
            20f,
            FontStyles.Bold,
            Color.white,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
        labelText.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }

    Image CreateImage(
        string name,
        Transform parent,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject imageGO = new GameObject(name);
        imageGO.transform.SetParent(parent, false);

        RectTransform rect = imageGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = imageGO.AddComponent<Image>();
        image.color = color;
        return image;
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

    void OnDestroy()
    {
        if (runtimeCanvasRoot != null)
        {
            DestroySafe(runtimeCanvasRoot);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    static void DestroySafe(Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }
}
