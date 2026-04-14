using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public Shooting shooting;

    // HUD refs — built at runtime
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
    private GameObject       _legacyGameOverPanel;
    private GameObject       _hudCanvasRoot;

    private int _maxHealth = 100;
    private const int MedkitMax = 4;

    static readonly Color CPanelBg     = new Color(0f,    0f,    0f,    0.55f);
    static readonly Color CHealthGreen = new Color(0.298f,0.686f,0.314f,1f);
    static readonly Color CMuted       = new Color(1f,    1f,    1f,    0.45f);
    static readonly Color CVeryMuted   = new Color(1f,    1f,    1f,    0.3f);
    static readonly Color CSupplies    = new Color(1f,    1f,    1f,    0.55f);
    static readonly Color CWaveBg      = new Color(0.937f,0.267f,0.267f,0.22f);
    static readonly Color CWaveText    = new Color(0.973f,0.533f,0.533f,1f);
    static readonly Color CMedEmpty    = new Color(1f,    1f,    1f,    0.12f);
    static readonly Color CAmberText   = new Color(0.98f, 0.62f, 0.09f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureRuntimeUIManager()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            return;
        }

        if (FindFirstObjectByType<UIManager>() != null)
        {
            return;
        }

        GameObject uiManagerObject = new GameObject("UIManager");
        uiManagerObject.AddComponent<UIManager>();
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        RemoveLegacyHudChildren();
        BuildHUD();
        EnsureCrosshairPresent();
    }

    void Start()
    {
        UpdatePrompt(string.Empty);
        UpdateHealth(100, 100);
    }

    void Update()
    {
        if (shooting == null)
            shooting = FindFirstObjectByType<Shooting>();

        if (shooting == null) return;

        string ammoStatus = shooting.IsReloading()
            ? "RELOADING"
            : shooting.GetCurrentAmmo().ToString();

        if (_ammoCurrentText != null)
            _ammoCurrentText.text = ammoStatus;

        if (_ammoReserveText != null)
            _ammoReserveText.text = shooting.IsReloading()
                ? "—"
                : shooting.GetMaxAmmo().ToString();

        if (_reloadHintText != null)
            _reloadHintText.color = shooting.IsReloading() ? CAmberText : CVeryMuted;
    }

    // ─── BUILD ───────────────────────────────────────────────────────────────

    void BuildHUD()
    {
        GameObject cvGO = new GameObject("HUD Canvas");
        _hudCanvasRoot = cvGO;

        Canvas cv = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 15;

        CanvasScaler cs = cvGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.45f;

        cvGO.AddComponent<GraphicRaycaster>();

        BuildTopLeft(cvGO.transform);
        BuildTopRight(cvGO.transform);
        BuildBottomLeft(cvGO.transform);
        BuildBottomRight(cvGO.transform);
        BuildPrompt(cvGO.transform);
        BuildMessage(cvGO.transform);
        BuildDamageVignette(cvGO.transform);
    }

    void RemoveLegacyHudChildren()
    {
        string[] legacyHudRoots =
        {
            "Health Text",
            "Health Bar",
            "Ammo Text",
            "Score Text",
            "Supplies Text",
            "Pressure Text",
            "Objective Text",
            "Prompt Text",
            "Message Text",
            "Game Over Panel"
        };

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            for (int j = 0; j < legacyHudRoots.Length; j++)
            {
                if (child.name == legacyHudRoots[j])
                {
                    Destroy(child.gameObject);
                    break;
                }
            }
        }
    }

    void EnsureCrosshairPresent()
    {
        if (FindFirstObjectByType<Crosshair>() != null)
        {
            return;
        }

        GameObject canvasObject = GameObject.Find("Crosshair Canvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Crosshair Canvas");
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        for (int i = canvasObject.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(canvasObject.transform.GetChild(i).gameObject);
        }

        GameObject crosshairObject = new GameObject("Crosshair");
        crosshairObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = crosshairObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(64f, 64f);

        crosshairObject.AddComponent<Crosshair>();
    }

    void BuildTopLeft(Transform cv)
    {
        RectTransform root = MakeRect("TL", cv, TL, TL, TL, new Vector2(24, -24), new Vector2(360, 180));

        _zoneText = MakeTMP("Zone", root, "ZONE: —", 10f, CVeryMuted,
            TL, TL, TL, new Vector2(0, 0), new Vector2(360, 18));
        _zoneText.fontStyle = FontStyles.UpperCase;
        _zoneText.characterSpacing = 2f;

        Image panel = MakeImage("ObjPanel", root, CPanelBg,
            TL, TR, TL, new Vector2(0, -24), new Vector2(0, 150));
        AddOutline(panel);

        RectTransform p = panel.rectTransform;

        MakeTMP("ObjLabel", p, "OBJECTIVES", 9f, CMuted,
            TL, TR, TL, new Vector2(12, -10), new Vector2(-24, 16))
            .characterSpacing = 1.5f;

        _objectiveText = MakeTMP("ObjText", p, "—", 11f, new Color(1,1,1,0.75f),
            TL, TR, TL, new Vector2(12, -30), new Vector2(-24, 104));
        _objectiveText.enableWordWrapping = true;
        _objectiveText.overflowMode = TextOverflowModes.Ellipsis;
        _objectiveText.lineSpacing = 5f;
    }

    void BuildTopRight(Transform cv)
    {
        RectTransform root = MakeRect("TR", cv, TR, TR, TR, new Vector2(-24, -24), new Vector2(220, 98));

        Image panel = MakeImage("ScorePanel", root, CPanelBg,
            TR, TR, TR, Vector2.zero, new Vector2(220, 98));
        AddOutline(panel);

        RectTransform p = panel.rectTransform;

        MakeTMP("ScoreLabel", p, "SCORE", 9f, CMuted,
            TR, TR, TR, new Vector2(-12, -10), new Vector2(200, 16))
            .characterSpacing = 1.5f;

        _scoreText = MakeTMP("ScoreVal", p, "0", 24f, Color.white,
            TR, TR, TR, new Vector2(-12, -24), new Vector2(190, 30));
        _scoreText.fontStyle = FontStyles.Bold;
        _scoreText.alignment = TextAlignmentOptions.Right;

        Image wavePill = MakeImage("WavePill", p, CWaveBg,
            TR, TR, TR, new Vector2(-12, -66), new Vector2(150, 20));
        wavePill.rectTransform.pivot = TR;
        wavePill.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 20);
        var wr = wavePill.GetComponent<RectTransform>();
        wr.anchorMin = TR; wr.anchorMax = TR; wr.pivot = TR;
        wr.anchoredPosition = new Vector2(-12, -66);
        wr.sizeDelta = new Vector2(150, 20);
        _wavePillBg = wavePill;

        _waveText = MakeTMP("WaveText", wavePill.rectTransform, "PRESSURE: LOW",
            9.5f, CWaveText, TR, TR, TR, new Vector2(-8, -2), new Vector2(-14, 16));
        _waveText.characterSpacing = 1f;
        _waveText.alignment = TextAlignmentOptions.Right;
    }

    void BuildBottomLeft(Transform cv)
    {
        Image panel = MakeImage("BL Panel", cv, CPanelBg,
            BL, BL, BL, new Vector2(24, 24), new Vector2(360, 108));
        AddOutline(panel);
        RectTransform p = panel.rectTransform;

        // health label
        MakeTMP("HpLabel", p, "HEALTH", 9f, CMuted,
            TL, TL, TL, new Vector2(12, -10), new Vector2(90, 16))
            .characterSpacing = 1.5f;

        // hp value
        _healthValueText = MakeTMP("HpVal", p, "100", 28f, CHealthGreen,
            TL, TL, TL, new Vector2(12, -24), new Vector2(96, 30));
        _healthValueText.fontStyle = FontStyles.Bold;

        // /100
        MakeTMP("HpMax", p, "/ 100", 12f, CVeryMuted,
            TL, TL, TL, new Vector2(74, -31), new Vector2(56, 18));

        // health bar bg
        Image barBg = MakeImage("BarBg", p, new Color(1,1,1,0.1f),
            TL, TL, TL, new Vector2(12, -58), new Vector2(210, 8));
        barBg.rectTransform.pivot = new Vector2(0, 0.5f);

        // health bar fill (Filled image)
        _healthBarFill = MakeImage("BarFill", barBg.rectTransform, CHealthGreen,
            BL, TL, new Vector2(0,0.5f), Vector2.zero, new Vector2(210, 8));
        _healthBarFill.type = Image.Type.Filled;
        _healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        _healthBarFill.fillAmount = 1f;

        // medkits label
        MakeTMP("MkLabel", p, "MEDKITS", 9f, CMuted,
            TL, TL, TL, new Vector2(236, -10), new Vector2(110, 16))
            .characterSpacing = 1.5f;

        // medkit segments
        _medkitSegs = new Image[MedkitMax];
        for (int i = 0; i < MedkitMax; i++)
        {
            Image seg = MakeImage($"Mk{i}", p, CHealthGreen,
                TL, TL, TL, new Vector2(236 + i * 24, -28), new Vector2(18, 22));
            _medkitSegs[i] = seg;
        }

        // supplies row
        _suppliesText = MakeTMP("Supplies", p, "AMMO 0  FOOD 0  0.0/20 KG",
            10f, CSupplies, TL, TL, TL, new Vector2(12, -82), new Vector2(336, 18));
        _suppliesText.characterSpacing = 0.5f;
    }

    void BuildBottomRight(Transform cv)
    {
        Image panel = MakeImage("BR Panel", cv, CPanelBg,
            BR, BR, BR, new Vector2(-24, 24), new Vector2(250, 108));
        AddOutline(panel);
        RectTransform p = panel.rectTransform;

        MakeTMP("AmmoLabel", p, "AMMO", 9f, CMuted,
            TR, TR, TR, new Vector2(-12, -10), new Vector2(226, 16))
            .characterSpacing = 1.5f;

        // current ammo — large
        _ammoCurrentText = MakeTMP("AmmoCur", p, "30", 50f, Color.white,
            TR, TR, TR, new Vector2(-100, -20), new Vector2(120, 56));
        _ammoCurrentText.fontStyle = FontStyles.Bold;
        _ammoCurrentText.alignment = TextAlignmentOptions.Right;

        // separator /
        MakeTMP("AmmoSep", p, "/", 22f, CVeryMuted,
            TR, TR, TR, new Vector2(-96, -40), new Vector2(24, 32))
            .alignment = TextAlignmentOptions.Center;

        // reserve ammo — smaller
        _ammoReserveText = MakeTMP("AmmoRes", p, "120", 22f,
            new Color(1,1,1,0.55f), TR, TR, TR,
            new Vector2(-12, -36), new Vector2(72, 30));
        _ammoReserveText.alignment = TextAlignmentOptions.Right;

        // reload hint
        _reloadHintText = MakeTMP("ReloadHint", p, "[ R ]  RELOAD", 9.5f,
            CVeryMuted, TR, TR, TR, new Vector2(-12, -86), new Vector2(220, 16));
        _reloadHintText.characterSpacing = 1f;
        _reloadHintText.alignment = TextAlignmentOptions.Right;
    }

    void BuildPrompt(Transform cv)
    {
        _promptGO = new GameObject("Prompt");
        _promptGO.transform.SetParent(cv, false);
        RectTransform rt = _promptGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 24f);
        rt.sizeDelta = new Vector2(600f, 40f);

        Image bg = _promptGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.68f);

        _promptText = MakeTMP("PromptText", rt, "", 13f, Color.white,
            BL, BR, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(-28, -6f));
        _promptText.enableWordWrapping = true;
        _promptText.alignment = TextAlignmentOptions.Center;
        _promptText.characterSpacing = 0.5f;
        _promptGO.SetActive(false);
    }

    void BuildMessage(Transform cv)
    {
        _messageText = MakeTMP("MsgText", cv, "", 16f, Color.white,
            new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.73f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 40f));
        _messageText.enableWordWrapping = true;
        _messageText.alignment = TextAlignmentOptions.Center;
        _messageText.fontStyle = FontStyles.Bold;
        _messageText.gameObject.SetActive(false);
    }

    void BuildDamageVignette(Transform cv)
    {
        _damageVignette = MakeImage("Vignette", cv, new Color(0.86f, 0.1f, 0.1f, 0f),
            BL, TR, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        _damageVignette.rectTransform.anchorMin = Vector2.zero;
        _damageVignette.rectTransform.anchorMax = Vector2.one;
        _damageVignette.rectTransform.sizeDelta = Vector2.zero;
        _damageVignette.gameObject.SetActive(false);
    }

    // ─── PUBLIC API ──────────────────────────────────────────────────────────

    public void UpdateHealth(int current, int max)
    {
        _maxHealth = max;
        if (_healthValueText != null) _healthValueText.text = current.ToString();
        if (_healthBarFill    != null) _healthBarFill.fillAmount = max > 0 ? (float)current / max : 0f;

        if (_healthValueText != null)
        {
            float ratio = max > 0 ? (float)current / max : 1f;
            _healthValueText.color = ratio > 0.5f ? CHealthGreen
                : ratio > 0.25f ? CAmberText
                : new Color(0.94f, 0.27f, 0.27f, 1f);
            if (_healthBarFill != null) _healthBarFill.color = _healthValueText.color;
        }
    }

    public void UpdateScore(int score)
    {
        if (_scoreText != null)
            _scoreText.text = score.ToString("N0").Replace(",", " ");
    }

    public void UpdateWave(int wave)
    {
        if (_waveText == null) return;
        _waveText.text = wave > 0 ? $"PRESSURE: {wave}" : "PRESSURE: LOW";
    }

    public void UpdateSupplies(string supplies)
    {
        if (_suppliesText != null) _suppliesText.text = supplies;
    }

    public void UpdateInventory(string inventory)
    {
        if (_suppliesText == null) return;
        // parse "Meds:X Food:Y Ammo:Z ... W/MaxW"
        // show ammo reserve, food, weight in the supplies row
        _suppliesText.text = inventory;
    }

    public void UpdateMedkits(int count)
    {
        if (_medkitSegs == null) return;
        for (int i = 0; i < _medkitSegs.Length; i++)
            if (_medkitSegs[i] != null)
                _medkitSegs[i].color = i < count ? CHealthGreen : CMedEmpty;
    }

    public void UpdateObjective(string objective)
    {
        if (_zoneText != null || _objectiveText != null)
        {
            int newline = objective.IndexOf('\n');
            if (newline >= 0)
            {
                if (_zoneText      != null) _zoneText.text      = objective.Substring(0, newline).ToUpper();
                if (_objectiveText != null) _objectiveText.text = objective.Substring(newline + 1);
            }
            else
            {
                if (_objectiveText != null) _objectiveText.text = objective;
            }
        }
    }

    public void UpdatePrompt(string prompt)
    {
        if (_promptGO == null) return;
        bool show = !string.IsNullOrWhiteSpace(prompt);
        _promptGO.SetActive(show);
        if (_promptText != null) _promptText.text = prompt;
    }

    public void ShowMessage(string message)
    {
        if (_messageText == null) return;
        _messageText.text = message;
        _messageText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 3f);
    }

    public void ShowDamageIndicator()
    {
        if (_damageVignette == null) return;
        _damageVignette.gameObject.SetActive(true);
        Color c = _damageVignette.color;
        c.a = 0.38f;
        _damageVignette.color = c;
        CancelInvoke(nameof(FadeDamage));
        Invoke(nameof(FadeDamage), 0.15f);
    }

    // Legacy fallback — GameOverScreen handles this now
    public void ShowGameOver(int score)
    {
        if (GameOverScreen.Instance != null)
            GameOverScreen.Instance.ShowGameOver(score);
    }

    public void SetGameOverVisible(bool visible)
    {
        if (_legacyGameOverPanel != null)
            _legacyGameOverPanel.SetActive(visible);
    }

    // ─── PRIVATE ─────────────────────────────────────────────────────────────

    void HideMessage()
    {
        if (_messageText != null) _messageText.gameObject.SetActive(false);
    }

    void FadeDamage()
    {
        if (_damageVignette == null) return;
        Color c = _damageVignette.color;
        c.a = 0f;
        _damageVignette.color = c;
        _damageVignette.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_hudCanvasRoot != null)
            Destroy(_hudCanvasRoot);

        if (Instance == this) Instance = null;
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TR = new Vector2(1f, 1f);
    static readonly Vector2 BL = new Vector2(0f, 0f);
    static readonly Vector2 BR = new Vector2(1f, 0f);

    static RectTransform MakeRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image MakeImage(string name, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 pos, Vector2 size)
    {
        RectTransform rt = MakeRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 pos, Vector2 size)
    {
        RectTransform rt = MakeRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    static void AddOutline(Image img)
    {
        Outline o = img.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(1f, 1f, 1f, 0.08f);
        o.effectDistance = new Vector2(1f, -1f);
    }
}
