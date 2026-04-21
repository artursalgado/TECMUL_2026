using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;
    private GameObject _canvas;
    private bool _isTransitioning = false;

    static readonly Color CPanelBg = new Color(0.04f, 0.06f, 0.09f, 0.96f);
    static readonly Color CAccent  = new Color(1.00f, 0.25f, 0.25f, 1f);
    static readonly Color CMuted   = new Color(0.6f,  0.7f,  0.8f,  0.5f);
    static readonly Color CBtnNorm = new Color(0.08f, 0.12f, 0.18f, 0.98f);
    static readonly Color CBtnHov  = new Color(0.15f, 0.22f, 0.32f, 1f);
    static readonly Vector2 MC = new Vector2(0.5f, 0.5f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsurePauseMenu()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu" || scene == "Menu" || scene == "Start") return;
        if (FindFirstObjectByType<PauseMenu>() != null) return;
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
        if (_canvas != null) _canvas.SetActive(false);
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
        // Esconde o menu de pausa quando uma nova cena e carregada
        if (_canvas != null) _canvas.SetActive(false);
        _isTransitioning = false;

        // Destroi se voltar ao menu principal
        if (scene.name == "MainMenu" || scene.name == "Menu" || scene.name == "Start")
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Evita toggle durante transicoes
            if (_isTransitioning) return;
            // Evita toggle se estiver em GameOver ou MainMenu
            if (GameManager.Instance != null &&
                (GameManager.Instance.CurrentState == GameManager.GameState.GameOver ||
                 GameManager.Instance.CurrentState == GameManager.GameState.MainMenu))
                return;
            Toggle();
        }
    }

    public void Toggle()
    {
        if (_isTransitioning) return;
        if (GameManager.Instance == null) return;

        var state = GameManager.Instance.CurrentState;
        Debug.Log($"[PauseMenu] Toggle chamado. Estado atual: {state}");

        if (state == GameManager.GameState.Paused) Resume();
        else if (state == GameManager.GameState.Playing) Pause();
    }

    public void Pause()
    {
        if (_isTransitioning) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        Debug.Log("[PauseMenu] Pausando jogo...");
        _isTransitioning = true;

        // Notifica o GameManager primeiro (ele gere o Time.timeScale)
        GameManager.Instance.ChangeState(GameManager.GameState.Paused);

        // Ativa o canvas
        if (_canvas != null) _canvas.SetActive(true);

        _isTransitioning = false;
    }

    public void Resume()
    {
        if (_isTransitioning) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Paused) return;

        Debug.Log("[PauseMenu] Resumindo jogo...");
        _isTransitioning = true;

        // Desativa o canvas primeiro
        if (_canvas != null) _canvas.SetActive(false);

        // Notifica o GameManager (ele gere o Time.timeScale)
        GameManager.Instance.ChangeState(GameManager.GameState.Playing);

        _isTransitioning = false;
    }

    void Build()
    {
        _canvas = new GameObject("PauseCanvas");
        _canvas.transform.SetParent(transform, false);
        Canvas cv = _canvas.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 1000;
        CanvasScaler cs = _canvas.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
        _canvas.AddComponent<GraphicRaycaster>();

        // Cria EventSystem se nao existir (necessario para input no UI)
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        MakeImage("Dim", _canvas.transform, new Color(0,0,0,0.75f), Vector2.zero, Vector2.one, MC, Vector2.zero, Vector2.zero);
        Image card = MakeImage("Card", _canvas.transform, CPanelBg, new Vector2(0.38f, 0.25f), new Vector2(0.62f, 0.75f), MC, Vector2.zero, Vector2.zero);
        RectTransform p = card.rectTransform;

        var title = MakeTMP("Title", p, "PAUSED", 36f, Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(0, 50));
        title.fontStyle = FontStyles.Bold | FontStyles.UpperCase; title.alignment = TextAlignmentOptions.Center;

        float startY = 0.62f; float step = 0.12f;
        BuildBtn(p, "RESUME",    startY - step*0f, Resume);
        BuildBtn(p, "MAIN MENU", startY - step*1f, ReturnToMenu);
        BuildBtn(p, "QUIT",      startY - step*2f, OnQuitClicked, CAccent);

        var tip = MakeTMP("Tip", p, "PRESS ESC TO RESUME MISSION", 10f, CMuted, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(0, 20));
        tip.alignment = TextAlignmentOptions.Center;
    }

    void BuildBtn(RectTransform parent, string label, float anchorY, UnityEngine.Events.UnityAction action, Color? accent = null)
    {
        GameObject go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.12f, anchorY - 0.05f); rt.anchorMax = new Vector2(0.88f, anchorY + 0.05f); rt.sizeDelta = Vector2.zero;
        Image img = go.AddComponent<Image>(); img.color = CBtnNorm;
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors; cb.normalColor = CBtnNorm; cb.highlightedColor = CBtnHov; cb.pressedColor = new Color(0.05f, 0.08f, 0.12f, 1f); btn.colors = cb;
        btn.onClick.AddListener(action);

        var t = MakeTMP("L", go.transform, label, 18f, Color.white, Vector2.zero, Vector2.one, MC, new Vector2(20, 0), Vector2.zero);
        t.fontStyle = FontStyles.Bold | FontStyles.UpperCase; t.alignment = TextAlignmentOptions.MidlineLeft;
    }

    void ReturnToMenu()
    {
        Debug.Log("[PauseMenu] Retornando ao menu principal...");
        if (_isTransitioning) return;
        _isTransitioning = true;

        // Garante que o canvas esta desativado
        if (_canvas != null) _canvas.SetActive(false);

        // Resume o jogo antes de mudar de cena
        Time.timeScale = 1f;

        // Destroi o PauseMenu
        Instance = null;
        Destroy(gameObject);

        SceneManager.LoadScene("MainMenu");
    }

    void OnQuitClicked()
    {
        Debug.Log("[PauseMenu] Saindo do jogo...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static RectTransform MakeRect(string n, Transform p, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
    }

    static Image MakeImage(string n, Transform p, Color c, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    { var rt = MakeRect(n, p, aMin, aMax, pivot, pos, size); var img = rt.gameObject.AddComponent<Image>(); img.color = c; return img; }

    static TextMeshProUGUI MakeTMP(string n, Transform p, string txt, float fs, Color c, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = MakeRect(n, p, aMin, aMax, pivot, pos, size);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.text = txt; t.fontSize = fs; t.color = c;
        t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; return t;
    }
}
