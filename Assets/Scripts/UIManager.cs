using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public Shooting shooting;

    private TextMeshProUGUI _zoneText;
    private TextMeshProUGUI _objectiveText;
    private TextMeshProUGUI _scoreText;
    private TextMeshProUGUI _waveText;
    private Image            _wavePillBg;
    private TextMeshProUGUI _healthValueText;
    private Image            _healthBarFill;
    private Image[]          _medkitSegs;
    private TextMeshProUGUI _suppliesText;
    private TextMeshProUGUI _ammoCurrentText;
    private TextMeshProUGUI _ammoReserveText;
    private TextMeshProUGUI _reloadHintText;
    private TextMeshProUGUI _promptText;
    private GameObject       _promptGO;
    private TextMeshProUGUI _messageText;
    private Image            _damageVignette;
    private GameObject       _hudCanvasRoot;
    private Vector3          _hudBasePos = Vector3.zero;
    private float            _shakeIntensity = 0f;

    private const int MedkitMax = 4;

    static readonly Color CPanelBg     = new Color(0.05f, 0.07f, 0.1f, 0.75f);
    static readonly Color CHealthGreen = new Color(0.35f, 1f, 0.6f, 1f);
    static readonly Color CMuted       = new Color(1f,    1f,    1f,    0.45f);
    static readonly Color CVeryMuted   = new Color(1f,    1f,    1f,    0.3f);
    static readonly Color CSupplies    = new Color(1f,    1f,    1f,    0.55f);
    static readonly Color CWaveBg      = new Color(0.937f,0.267f,0.267f,0.22f);
    static readonly Color CWaveText    = new Color(0.973f,0.533f,0.533f,1f);
    static readonly Color CMedEmpty    = new Color(1f,    1f,    1f,    0.12f);
    static readonly Color CAmberText   = new Color(0.98f, 0.62f, 0.09f, 1f);
    static readonly Color CAccent      = new Color(0.93f, 0.27f, 0.27f, 1f);
    static readonly Vector2 MC         = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TL = new Vector2(0,1);
    static readonly Vector2 TR = new Vector2(1,1);
    static readonly Vector2 BL = new Vector2(0,0);
    static readonly Vector2 BR = new Vector2(1,0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureRuntimeUIManager()
    {
        string scene = SceneManager.GetActiveScene().name;
        // Nao cria UIManager em cenas de menu
        if (scene == "MainMenu" || scene == "Menu" || scene == "Start") return;
        if (FindFirstObjectByType<UIManager>() != null) return;
        new GameObject("UIManager").AddComponent<UIManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildHUD();
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
        // Destroi se voltar ao menu principal
        if (scene.name == "MainMenu" || scene.name == "Menu" || scene.name == "Start")
        {
            Instance = null;
            Destroy(gameObject);
        }
        else
        {
            // Reconstroi o HUD ao carregar nova cena de jogo
            BuildHUD();
        }
    }

    void Start()
    {
        UpdatePrompt(string.Empty);
        UpdateHealth(100, 100);
    }

    void Update()
    {
        if (shooting == null) shooting = Object.FindFirstObjectByType<Shooting>();
        if (shooting == null) return;

        if (_shakeIntensity > 0f)
        {
            _shakeIntensity -= Time.deltaTime * 5f;
            if (_hudCanvasRoot != null)
                _hudCanvasRoot.transform.position = _hudBasePos + (Vector3)(Random.insideUnitCircle * _shakeIntensity * 20f);
        }
        else if (_hudCanvasRoot != null)
        {
            _hudCanvasRoot.transform.position = _hudBasePos;
        }

        if (_ammoCurrentText != null) 
        {
            _ammoCurrentText.text = shooting.IsReloading() ? "RELOADING" : shooting.GetCurrentAmmo().ToString();
        }
        
        if (_ammoReserveText != null) 
        {
            _ammoReserveText.text = shooting.IsReloading() ? "-" : shooting.GetReserveAmmo().ToString();
        }
        
        if (_reloadHintText != null) 
        {
            _reloadHintText.color = shooting.IsReloading() ? CAmberText : CVeryMuted;
        }
    }

    void BuildHUD()
    {
        // Limpa o HUD anterior se existir
        if (_hudCanvasRoot != null)
        {
            Destroy(_hudCanvasRoot);
        }

        GameObject cvGO = new GameObject("HUD Canvas");
        _hudCanvasRoot = cvGO;
        _hudBasePos = cvGO.transform.position;
        Canvas cv = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 15;
        CanvasScaler cs = cvGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f); cs.matchWidthOrHeight = 0.45f;
        cvGO.AddComponent<GraphicRaycaster>();

        BuildTopLeft(cvGO.transform);
        BuildTopRight(cvGO.transform);
        BuildBottomLeft(cvGO.transform);
        BuildBottomRight(cvGO.transform);
        BuildPrompt(cvGO.transform);
        BuildMessage(cvGO.transform);
        BuildDamageVignette(cvGO.transform);
    }

    void BuildTopLeft(Transform cv)
    {
        RectTransform root = MakeRect("TL", cv, TL, TL, TL, new Vector2(24, -24), new Vector2(360, 180));
        _zoneText = MakeTMP("Zone", root, "ZONE", 10f, CVeryMuted, TL, TL, TL, new Vector2(0, 0), new Vector2(360, 18));
        Image panel = MakeImage("ObjPanel", root, CPanelBg, TL, TR, TL, new Vector2(0, -24), new Vector2(0, 150));
        RectTransform p = panel.rectTransform;
        MakeTMP("ObjLabel", p, "OBJECTIVES", 9f, CMuted, TL, TR, TL, new Vector2(12, -10), new Vector2(-24, 16));
        _objectiveText = MakeTMP("ObjText", p, "-", 11f, new Color(1,1,1,0.75f), TL, TR, TL, new Vector2(12, -30), new Vector2(-24, 104));
    }

    void BuildTopRight(Transform cv)
    {
        RectTransform root = MakeRect("TR", cv, TR, TR, TR, new Vector2(-24, -24), new Vector2(220, 98));
        Image panel = MakeImage("ScorePanel", root, CPanelBg, TR, TR, TR, Vector2.zero, new Vector2(220, 98));
        RectTransform p = panel.rectTransform;
        MakeTMP("ScoreLabel", p, "SCORE", 9f, CMuted, TR, TR, TR, new Vector2(-12, -10), new Vector2(200, 16));
        _scoreText = MakeTMP("ScoreVal", p, "0", 24f, Color.white, TR, TR, TR, new Vector2(-12, -24), new Vector2(190, 30));
        _scoreText.alignment = TextAlignmentOptions.Right;
    }

    void BuildBottomLeft(Transform cv)
    {
        RectTransform root = MakeRect("BL", cv, BL, BL, BL, new Vector2(24, 24), new Vector2(400, 120));
        Image panel = MakeImage("HealthPanel", root, CPanelBg, BL, BR, BL, Vector2.zero, new Vector2(0, 80));
        RectTransform p = panel.rectTransform;
        MakeTMP("HLabel", p, "HEALTH", 9f, CMuted, TL, TL, TL, new Vector2(12, -10), new Vector2(100, 16));
        _healthValueText = MakeTMP("HVal", p, "100", 22f, Color.white, TL, TL, TL, new Vector2(12, -24), new Vector2(80, 30));
        _healthBarFill = MakeImage("Bar", p, CHealthGreen, TL, TL, TL, new Vector2(80, -26), new Vector2(300, 24));

        // Medkit Segments
        _medkitSegs = new Image[MedkitMax];
        for (int i = 0; i < MedkitMax; i++)
        {
            _medkitSegs[i] = MakeImage("Med_" + i, p, CMedEmpty, TL, TL, TL, new Vector2(12 + i * 22, -60), new Vector2(18, 10));
        }
    }

    void BuildBottomRight(Transform cv)
    {
        RectTransform root = MakeRect("BR", cv, BR, BR, BR, new Vector2(-24, 24), new Vector2(300, 120));
        Image panel = MakeImage("AmmoPanel", root, CPanelBg, BR, BR, BR, Vector2.zero, new Vector2(220, 100));
        RectTransform p = panel.rectTransform;
        _ammoCurrentText = MakeTMP("Ammo", p, "30", 42f, Color.white, TR, TR, TR, new Vector2(-60, -10), new Vector2(120, 60));
        _ammoCurrentText.alignment = TextAlignmentOptions.Right;
        _ammoReserveText = MakeTMP("Res", p, "90", 18f, CMuted, TR, TR, TR, new Vector2(-12, -24), new Vector2(60, 30));
    }

    void BuildPrompt(Transform cv)
    {
        _promptGO = new GameObject("Prompt");
        _promptGO.transform.SetParent(cv, false);
        _promptText = MakeTMP("T", _promptGO.transform, "PRESS E TO INTERACT", 14f, Color.white, MC, MC, MC, new Vector2(0, -100), new Vector2(600, 40));
        _promptText.alignment = TextAlignmentOptions.Center;
    }

    void BuildMessage(Transform cv)
    {
        _messageText = MakeTMP("Msg", cv, "", 18f, CAccent, MC, MC, MC, new Vector2(0, 200), new Vector2(800, 60));
        _messageText.alignment = TextAlignmentOptions.Center;
    }

    void BuildDamageVignette(Transform cv)
    {
        _damageVignette = MakeImage("Vig", cv, new Color(1,0,0,0), BL, TR, MC, Vector2.zero, Vector2.zero);
        _damageVignette.raycastTarget = false;
    }

    // Methods needed by other scripts
    public void UpdateHealth(int cur, int max)
    {
        if (_healthValueText != null) _healthValueText.text = cur.ToString();
        if (_healthBarFill != null) _healthBarFill.rectTransform.sizeDelta = new Vector2(300f * ((float)cur / max), 24f);
    }

    public void UpdateMedkits(int current)
    {
        if (_medkitSegs == null) return;
        for (int i = 0; i < MedkitMax; i++)
        {
            if (_medkitSegs[i] != null) _medkitSegs[i].color = i < current ? CHealthGreen : CMedEmpty;
        }
    }

    public void ShowDamageIndicator()
    {
        if (_damageVignette != null) StartCoroutine(FlashVignette());
        _shakeIntensity = 0.2f;
    }

    IEnumerator FlashVignette()
    {
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            _damageVignette.color = new Color(1, 0, 0, Mathf.Lerp(0, 0.4f, t / 0.2f));
            yield return null;
        }
        while (t > 0)
        {
            t -= Time.deltaTime;
            _damageVignette.color = new Color(1, 0, 0, Mathf.Lerp(0, 0.4f, t / 0.2f));
            yield return null;
        }
        _damageVignette.color = new Color(1, 0, 0, 0);
    }

    public void ShowConfigMenu()
    {
        // Se estamos no menu principal, apenas ativa
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.gameObject.SetActive(true);
            return;
        }

        // Se estamos no jogo e queremos mostrar o menu de configuração/pausa
        // Criamos um menu de configuração temporário ou usamos o PauseMenu
        if (PauseMenu.Instance != null)
        {
            // Se o GameManager está em Playing, alternamos para pausa
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                PauseMenu.Instance.Pause();
            }
        }
    }

    public void EnsureRuntimeHud() { BuildHUD(); }

    public void UpdateScore(int s) { if (_scoreText != null) _scoreText.text = s.ToString(); }
    public void UpdateWave(int w) { if (_waveText != null) _waveText.text = "WAVE " + w; }
    public void UpdateSupplies(string s) { if (_suppliesText != null) _suppliesText.text = s; }
    public void UpdateObjective(string o) { if (_objectiveText != null) _objectiveText.text = o; }
    public void UpdatePrompt(string p) { if (_promptGO != null) _promptGO.SetActive(!string.IsNullOrEmpty(p)); if (_promptText != null) _promptText.text = p; }

    public void ShowMessage(string m) { if (_messageText != null) { _messageText.text = m; StartCoroutine(ClearMsg()); } }
    IEnumerator ClearMsg() { yield return new WaitForSeconds(3f); if (_messageText != null) _messageText.text = ""; }

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

    static void AddOutline(Image img) { var o = img.gameObject.AddComponent<Outline>(); o.effectColor = new Color(1,1,1,0.1f); }
}
