using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main Menu Manager - builds and manages a high-quality start menu at runtime.
/// Improved version with better visuals, animations, and functionality.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    private GameObject _canvasRoot;
    private GameObject _panelMain;
    private GameObject _panelSettings;
    private GameObject _panelCredits;
    private GameObject _panelLoading;

    private TextMeshProUGUI _diffValue;
    private TextMeshProUGUI _atmosValue;
    private TextMeshProUGUI _sensitValue;
    private TextMeshProUGUI _volumeValue;
    private TextMeshProUGUI _fovValue;

    static readonly Color CPanelBg   = new Color(0.04f, 0.06f, 0.09f, 0.95f);
    static readonly Color CAccent    = new Color(1.00f, 0.25f, 0.25f, 1f);
    static readonly Color CAccentHov = new Color(1.00f, 0.45f, 0.45f, 1f);
    static readonly Color CMuted     = new Color(0.6f,  0.7f,  0.8f,  0.5f);
    static readonly Color CText      = new Color(0.95f, 0.95f, 1.00f, 1f);
    static readonly Color CBtnNorm   = new Color(0.08f, 0.12f, 0.18f, 0.98f);
    static readonly Color CBtnHov    = new Color(0.15f, 0.22f, 0.32f, 1f);
    static readonly Color CBtnPress  = new Color(0.05f, 0.08f, 0.12f, 1f);

    static readonly Vector2 TL = new Vector2(0,1);
    static readonly Vector2 TR = new Vector2(1,1);
    static readonly Vector2 BL = new Vector2(0,0);
    static readonly Vector2 BR = new Vector2(1,0);
    static readonly Vector2 MC = new Vector2(0.5f,0.5f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void TryAutoSpawn()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (!GameConfig.IsMenuScene(scene)) return;
        if (FindFirstObjectByType<MainMenuManager>() != null) return;
        new GameObject("MainMenuManager").AddComponent<MainMenuManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Configura o cursor para o menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        GameConfig.ApplyAudio();

        // Reseta a flag do SceneBootstrapper para permitir novo jogo
        SceneBootstrapper.ResetGameplayBuildFlag();

        Build();
    }

    void Start()
    {
        // Garante que estamos no estado correto
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
        }
    }

    void Build()
    {
        _canvasRoot = new GameObject("MainMenu Canvas");
        _canvasRoot.transform.SetParent(transform, false);
        Canvas cv = _canvasRoot.AddComponent<Canvas>();
        cv.renderMode  = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 500;

        CanvasScaler cs = _canvasRoot.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        _canvasRoot.AddComponent<GraphicRaycaster>();

        // Cria EventSystem se nao existir (necessario para input no UI)
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        MakeImage("BG", _canvasRoot.transform, new Color(0.02f,0.03f,0.05f,1f), BL, TR, MC, Vector2.zero, Vector2.zero);
        BuildDustParticles(_canvasRoot.transform);
        BuildVignette(_canvasRoot.transform);
        BuildScanlinePattern(_canvasRoot.transform);

        _panelMain     = BuildMainPanel(_canvasRoot.transform);
        _panelSettings = BuildSettingsPanel(_canvasRoot.transform);
        _panelCredits  = BuildCreditsPanel(_canvasRoot.transform);
        _panelLoading  = BuildLoadingPanel(_canvasRoot.transform);

        ShowMain();
    }

    GameObject BuildMainPanel(Transform cv)
    {
        GameObject root = new GameObject("PanelMain");
        root.transform.SetParent(cv, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta  = Vector2.zero;

        MakeImage("TopAccent", root.transform, CAccent, TL, TR, TL, new Vector2(0,0), new Vector2(0, 4));

        RectTransform titleBlock = MakeRect("TitleBlock", root.transform,
            new Vector2(0.08f, 0.60f), new Vector2(0.50f, 0.90f), TL, Vector2.zero, Vector2.zero);

        var title = MakeTMP("Title", titleBlock, "DEAD ZONE", 110f, Color.white, BL, TR, MC, Vector2.zero, Vector2.zero);
        title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        title.characterSpacing = 2f;

        var sub = MakeTMP("Sub", titleBlock, "SURVIVAL PROTOCOL - EXT01", 18f, CAccent, BL, new Vector2(1,0), BL,
            new Vector2(4, 12), new Vector2(0, 24));
        sub.characterSpacing = 6f;
        sub.fontStyle = FontStyles.Bold;

        MakeTMP("Version", root.transform, "v1.1 - SYSTEM STABLE - TECMUL 2026", 12f, CMuted,
            BL, BL, BL, new Vector2(32, 32), new Vector2(400, 24));

        float startY = 0.52f;
        float step = 0.09f;
        BuildMainBtn(root.transform, "START MISSION",  new Vector2(0.08f, startY - step*0f), OnStartClicked, true);
        BuildMainBtn(root.transform, "SETTINGS",        new Vector2(0.08f, startY - step*1f), OnSettingsClicked);
        BuildMainBtn(root.transform, "CREDITS",         new Vector2(0.08f, startY - step*2f), OnCreditsClicked);
        BuildMainBtn(root.transform, "QUIT",            new Vector2(0.08f, startY - step*3f), OnQuitClicked, false, CAccent);

        return root;
    }

    void BuildMainBtn(Transform parent, string label, Vector2 anchorPos, UnityEngine.Events.UnityAction action, bool primary = false, Color? accentOverride = null)
    {
        GameObject btnGO = new GameObject("Btn_" + label);
        btnGO.transform.SetParent(parent, false);
        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorPos.x, anchorPos.y);
        rt.anchorMax = new Vector2(anchorPos.x + 0.28f, anchorPos.y + 0.075f);
        rt.sizeDelta  = Vector2.zero;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        Image img = btnGO.AddComponent<Image>();
        img.color = CBtnNorm;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = CBtnNorm;
        cb.highlightedColor = CBtnHov;
        cb.pressedColor     = CBtnPress;
        cb.fadeDuration     = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(action);

        btnGO.AddComponent<MenuHoverScale>();

        Color accent = accentOverride ?? (primary ? CAccent : new Color(CText.r, CText.g, CText.b, 0.2f));
        MakeImage("Strip", btnGO.transform, accent, TL, BL, TL, Vector2.zero, new Vector2(6, 0));

        var tmp = MakeTMP("Label", btnGO.transform, label, 20f, CText,
            Vector2.zero, Vector2.one, MC, new Vector2(24, 0), Vector2.zero);
        tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        tmp.characterSpacing = 3f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }

    GameObject BuildSettingsPanel(Transform cv)
    {
        GameObject root = new GameObject("PanelSettings");
        root.transform.SetParent(cv, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

        MakeImage("Dim", root.transform, new Color(0,0,0,0.6f), BL, TR, MC, Vector2.zero, Vector2.zero);

        Image card = MakeImage("Card", root.transform, CPanelBg,
            new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.85f), MC, Vector2.zero, Vector2.zero);
        AddOutline(card);
        RectTransform p = card.rectTransform;

        MakeTMP("Title", p, "SYSTEM CONFIGURATION", 32f, Color.white, TL, TR, TL,
            new Vector2(40, -40), new Vector2(-80, 48)).fontStyle = FontStyles.Bold | FontStyles.UpperCase;

        MakeImage("Div", p, new Color(1,1,1,0.15f), TL, TR, TL, new Vector2(0, -100), new Vector2(0, 2));

        float rowY = -140f;
        float rowH  = 70f;

        BuildSettingsRow(p, "MISSION DIFFICULTY", rowY, out _diffValue);
        _diffValue.text = GetDiffLabel();
        _diffValue.gameObject.AddComponent<Button>().onClick.AddListener(() => {
            GameConfig.DifficultyLevel = (GameConfig.DifficultyLevel + 1) % 3;
            _diffValue.text = GetDiffLabel();
        });
        rowY -= rowH;

        BuildSettingsRow(p, "TIME OF DAY", rowY, out _atmosValue);
        _atmosValue.text = GameConfig.NightMode ? "NIGHT OPS" : "DAYLIGHT";
        _atmosValue.gameObject.AddComponent<Button>().onClick.AddListener(() => {
            GameConfig.NightMode = !GameConfig.NightMode;
            _atmosValue.text = GameConfig.NightMode ? "NIGHT OPS" : "DAYLIGHT";
        });
        rowY -= rowH;

        BuildSettingsRow(p, "MOUSE SENSITIVITY", rowY, out _sensitValue);
        _sensitValue.text = GameConfig.MouseSensitivity.ToString("F1");
        BuildPlusMinus(p, rowY,
            () => { GameConfig.MouseSensitivity = Mathf.Clamp(GameConfig.MouseSensitivity - 0.5f, 0.5f, 10f); _sensitValue.text = GameConfig.MouseSensitivity.ToString("F1"); },
            () => { GameConfig.MouseSensitivity = Mathf.Clamp(GameConfig.MouseSensitivity + 0.5f, 0.5f, 10f); _sensitValue.text = GameConfig.MouseSensitivity.ToString("F1"); });
        rowY -= rowH;

        BuildSettingsRow(p, "MASTER VOLUME", rowY, out _volumeValue);
        _volumeValue.text = Mathf.RoundToInt(GameConfig.MasterVolume * 100f) + "%";
        BuildPlusMinus(p, rowY,
            () => { GameConfig.MasterVolume = Mathf.Clamp(GameConfig.MasterVolume - 0.1f, 0f, 1f); _volumeValue.text = Mathf.RoundToInt(GameConfig.MasterVolume * 100f) + "%"; },
            () => { GameConfig.MasterVolume = Mathf.Clamp(GameConfig.MasterVolume + 0.1f, 0f, 1f); _volumeValue.text = Mathf.RoundToInt(GameConfig.MasterVolume * 100f) + "%"; });
        rowY -= rowH;

        BuildSettingsRow(p, "FIELD OF VIEW", rowY, out _fovValue);
        _fovValue.text = GameConfig.FieldOfView.ToString();
        BuildPlusMinus(p, rowY,
            () => { GameConfig.FieldOfView = Mathf.Clamp(GameConfig.FieldOfView - 5, 60, 110); _fovValue.text = GameConfig.FieldOfView.ToString(); },
            () => { GameConfig.FieldOfView = Mathf.Clamp(GameConfig.FieldOfView + 5, 60, 110); _fovValue.text = GameConfig.FieldOfView.ToString(); });

        BuildBackButton(p, ShowMain);

        return root;
    }

    void BuildSettingsRow(RectTransform parent, string label, float yOffset, out TextMeshProUGUI valueOut)
    {
        RectTransform row = MakeRect("Row_" + label, parent, TL, TR, TL, new Vector2(0, yOffset), new Vector2(0, 60));
        MakeImage("RowBg", row, new Color(1,1,1,0.03f), Vector2.zero, Vector2.one, MC, Vector2.zero, Vector2.zero);
        MakeTMP("Label", row, label, 15f, CMuted, TL, new Vector2(0.5f,1f), TL, new Vector2(40,-15), new Vector2(0,30)).characterSpacing = 2f;
        valueOut = MakeTMP("Value", row, "-", 18f, Color.white, new Vector2(0.5f,0), TR, TR, new Vector2(-120,-15), new Vector2(200,30));
        valueOut.fontStyle = FontStyles.Bold;
        valueOut.alignment = TextAlignmentOptions.MidlineRight;
    }

    void BuildPlusMinus(RectTransform parent, float yOffset, UnityEngine.Events.UnityAction onMinus, UnityEngine.Events.UnityAction onPlus)
    {
        MakeSmallBtn(parent, "-", new Vector2(0.85f, yOffset + 15), onMinus);
        MakeSmallBtn(parent, "+", new Vector2(0.93f, yOffset + 15), onPlus);
    }

    void MakeSmallBtn(RectTransform parent, string label, Vector2 normalizedPos, UnityEngine.Events.UnityAction action)
    {
        GameObject btnGO = new GameObject("SmBtn_" + label);
        btnGO.transform.SetParent(parent, false);
        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(normalizedPos.x - 0.035f, 0f);
        rt.anchorMax = new Vector2(normalizedPos.x, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, normalizedPos.y);
        rt.sizeDelta = new Vector2(0, 34);

        Image img = btnGO.AddComponent<Image>(); img.color = CBtnNorm;
        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors; cb.normalColor = CBtnNorm; cb.highlightedColor = CBtnHov; cb.pressedColor = CBtnPress; btn.colors = cb;
        btn.onClick.AddListener(action);

        var tmp = MakeTMP("L", btnGO.transform, label, 18f, Color.white, Vector2.zero, Vector2.one, MC, Vector2.zero, Vector2.zero);
        tmp.alignment = TextAlignmentOptions.Center;
    }

    GameObject BuildCreditsPanel(Transform cv)
    {
        GameObject root = new GameObject("PanelCredits");
        root.transform.SetParent(cv, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

        MakeImage("Dim", root.transform, new Color(0,0,0,0.6f), BL, TR, MC, Vector2.zero, Vector2.zero);

        Image card = MakeImage("Card", root.transform, CPanelBg,
            new Vector2(0.32f, 0.20f), new Vector2(0.68f, 0.80f), MC, Vector2.zero, Vector2.zero);
        AddOutline(card);
        RectTransform p = card.rectTransform;

        MakeTMP("Title", p, "CREDITS", 32f, Color.white, TL, TR, TL,
            new Vector2(0,-40), new Vector2(0,44)).alignment = TextAlignmentOptions.Center;

        MakeImage("Div", p, new Color(1,1,1,0.1f), TL, TR, TL, new Vector2(0,-100), new Vector2(0,2));

        string credits = "DEAD ZONE\n\n" +
                         "DEVELOPED BY\n" +
                         "Tiago and Artur Salgado\n\n" +
                         "ENGINE\n" +
                         "Unity URP with C#\n\n" +
                         "ASSETS\n" +
                         "Stylized Nature Kit and TextMeshPro\n\n" +
                         "SURVIVE THE NIGHT";

        var t = MakeTMP("CreditsText", p, credits, 18f, CText, TL, TR, TL, new Vector2(0, -140), new Vector2(0, 300));
        t.alignment = TextAlignmentOptions.Center;
        t.lineSpacing = 10f;

        BuildBackButton(p, ShowMain);
        return root;
    }

    GameObject BuildLoadingPanel(Transform cv)
    {
        GameObject root = new GameObject("PanelLoading");
        root.transform.SetParent(cv, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

        MakeImage("BG", root.transform, Color.black, BL, TR, MC, Vector2.zero, Vector2.zero);

        var t = MakeTMP("LoadingText", root.transform, "INITIALIZING MISSION PROTOCOL", 18f, CAccent, MC, MC, MC, Vector2.zero, new Vector2(600, 40));
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;

        root.SetActive(false);
        return root;
    }

    void ShowMain()
    {
        _panelMain.SetActive(true);
        _panelSettings.SetActive(false);
        _panelCredits.SetActive(false);
        _panelLoading.SetActive(false);
    }

    void OnSettingsClicked() { _panelMain.SetActive(false); _panelSettings.SetActive(true); }
    void OnCreditsClicked()  { _panelMain.SetActive(false); _panelCredits.SetActive(true); }

    void OnStartClicked()
    {
        GameConfig.SkipConfigMenu = true;
        StartCoroutine(LoadGameRoutine());
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator LoadGameRoutine()
    {
        _panelMain.SetActive(false);
        _panelSettings.SetActive(false);
        _panelCredits.SetActive(false);
        _panelLoading.SetActive(true);

        yield return new WaitForSecondsRealtime(1.0f);

        // Verifica se ja estamos na cena do jogo
        if (SceneManager.GetActiveScene().name == GameConfig.GameplaySceneName)
        {
            // Ja estamos no jogo, apenas esconde o menu e continua
            Debug.Log("[MainMenuManager] Ja estamos na cena de jogo, apenas escondendo menu...");

            // Notifica o SceneBootstrapper para continuar a construcao
            if (SceneBootstrapper.ShouldBuildActiveScene())
            {
                SceneBootstrapper.ExecuteGameplayBuild();
            }

            // Destroi o MainMenuManager
            Instance = null;
            Destroy(gameObject);
        }
        else
        {
            // Carrega a cena do jogo
            SceneManager.LoadScene(GameConfig.GameplaySceneName);
        }
    }

    void BuildBackButton(RectTransform parent, UnityEngine.Events.UnityAction action)
    {
        GameObject btn = new GameObject("BackBtn");
        btn.transform.SetParent(parent, false);
        RectTransform rt = btn.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 30);
        rt.sizeDelta = new Vector2(220, 50);

        Image img = btn.AddComponent<Image>(); img.color = CBtnNorm;
        Button b = btn.AddComponent<Button>();
        ColorBlock cb = b.colors; cb.normalColor = CBtnNorm; cb.highlightedColor = CBtnHov; cb.pressedColor = CBtnPress; b.colors = cb;
        b.onClick.AddListener(action);

        var tmp = MakeTMP("L", btn.transform, "RETURN", 16f, CText, Vector2.zero, Vector2.one, MC, Vector2.zero, Vector2.zero);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 4f;
    }

    void BuildScanlinePattern(Transform parent)
    {
        GameObject scan = new GameObject("Scanlines");
        scan.transform.SetParent(parent, false);
        RectTransform rt = scan.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        Image img = scan.AddComponent<Image>();
        img.color = new Color(0,0,0,0.12f);
        img.raycastTarget = false;
    }

    void BuildVignette(Transform parent)
    {
        GameObject vig = new GameObject("Vignette");
        vig.transform.SetParent(parent, false);
        RectTransform rt = vig.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        Image img = vig.AddComponent<Image>();
        img.color = new Color(0,0,0,0.4f);
        img.raycastTarget = false;
    }

    void BuildDustParticles(Transform parent)
    {
        GameObject dustRoot = new GameObject("DustParticles");
        dustRoot.transform.SetParent(parent, false);
        RectTransform rt = dustRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

        for (int i = 0; i < 40; i++)
        {
            GameObject p = new GameObject("P" + i);
            p.transform.SetParent(dustRoot.transform, false);
            var prt = p.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(Random.value, Random.value);
            prt.anchorMax = prt.anchorMin;
            prt.sizeDelta = new Vector2(Random.Range(2, 5), Random.Range(2, 5));

            var img = p.AddComponent<Image>();
            img.color = new Color(1, 1, 1, Random.Range(0.05f, 0.15f));
            img.raycastTarget = false;

            StartCoroutine(DustDrift(prt));
        }
    }

    IEnumerator DustDrift(RectTransform rt)
    {
        Vector2 velocity = new Vector2(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f));
        while (true)
        {
            Vector2 pos = rt.anchorMin;
            pos += velocity * Time.deltaTime;

            if (pos.x < 0) pos.x = 1;
            if (pos.x > 1) pos.x = 0;
            if (pos.y < 0) pos.y = 1;
            if (pos.y > 1) pos.y = 0;

            rt.anchorMin = pos;
            rt.anchorMax = pos;
            yield return null;
        }
    }

    string GetDiffLabel()
    {
        switch (GameConfig.DifficultyLevel)
        {
            case 0: return "RECRUIT EASY";
            case 2: return "NIGHTMARE HARD";
            default: return "SOLDIER NORMAL";
        }
    }

    static RectTransform MakeRect(string n, Transform p, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static Image MakeImage(string n, Transform p, Color c, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = MakeRect(n, p, aMin, aMax, pivot, pos, size);
        Image img = rt.gameObject.AddComponent<Image>(); img.color = c;
        return img;
    }

    static TextMeshProUGUI MakeTMP(string n, Transform p, string txt, float fs, Color c,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = MakeRect(n, p, aMin, aMax, pivot, pos, size);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = fs; t.color = c;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void AddOutline(Image img)
    {
        Outline o = img.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(1,1,1,0.1f); o.effectDistance = new Vector2(1,-1);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
