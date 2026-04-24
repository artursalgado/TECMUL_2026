using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    // Referências opcionais (se configuradas no Inspector sobrepõem as geradas por código)
    [Header("Textos do HUD (opcional — gerado automaticamente se vazio)")]
    public TextMeshProUGUI textoHorda;
    public TextMeshProUGUI textoZombies;
    public TextMeshProUGUI textoContagemRegressiva;

    [Header("Painel de Vitória (opcional)")]
    public GameObject painelVitoria;

    // Gerados por código
    private Image barraVidaFill;
    private TextMeshProUGUI textoVida;
    private GameObject painelMorte;
    private GameObject painelVitoriaGerado;
    private Canvas canvas;

    void Awake()
    {
        ConstruirHUD();
    }

    void ConstruirHUD()
    {
        // ── CANVAS ────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("HUDCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── HORDA ─────────────────────────────────────────────────
        if (textoHorda == null)
            textoHorda = CriarTexto(canvasGO.transform, "TextoHorda",
                "HORDA 1 / 5", new Vector2(-780, 490), new Vector2(350, 55),
                36, Color.white, FontStyles.Bold);

        // ── ZOMBIES ───────────────────────────────────────────────
        if (textoZombies == null)
            textoZombies = CriarTexto(canvasGO.transform, "TextoZombies",
                "Zombies: 5", new Vector2(-780, 440), new Vector2(350, 45),
                28, new Color(0.9f, 0.5f, 0.5f), FontStyles.Normal);

        // ── TEMPO DA HORDA ────────────────────────────────────────
        if (textoContagemRegressiva == null)
            textoContagemRegressiva = CriarTexto(canvasGO.transform, "TextoContagem",
                "", new Vector2(0, 400), new Vector2(600, 60),
                38, new Color(1f, 0.9f, 0.3f), FontStyles.Bold);

        // ── BARRA DE VIDA ─────────────────────────────────────────
        // Fundo da barra
        GameObject fundoBarra = CriarPainel(canvasGO.transform, "VidaFundo",
            new Color(0.15f, 0.05f, 0.05f, 0.9f),
            new Vector2(-750, -465), new Vector2(320, 28));

        // Preenchimento da barra
        GameObject fillBarra = CriarPainel(fundoBarra.transform, "VidaFill",
            new Color(0.8f, 0.1f, 0.1f, 1f),
            Vector2.zero, new Vector2(320, 28));
        RectTransform fillRT = fillBarra.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        barraVidaFill = fillBarra.GetComponent<Image>();

        // Texto "❤ 100"
        textoVida = CriarTexto(canvasGO.transform, "TextoVida",
            "❤  100", new Vector2(-750, -440), new Vector2(320, 35),
            26, new Color(1f, 0.7f, 0.7f), FontStyles.Bold);

        // ── ECRÃ DE MORTE ─────────────────────────────────────────
        painelMorte = CriarPainelFullscreen(canvasGO.transform, "PainelMorte",
            new Color(0.05f, 0f, 0f, 0.92f));
        painelMorte.SetActive(false);

        CriarTexto(painelMorte.transform, "TxtYouDied",
            "YOU DIED", new Vector2(0, 120), new Vector2(800, 160),
            110, new Color(0.85f, 0.08f, 0.08f, 1f), FontStyles.Bold);

        CriarTexto(painelMorte.transform, "TxtSubMorte",
            "The horde consumed you...", new Vector2(0, 20), new Vector2(700, 60),
            36, new Color(0.7f, 0.7f, 0.7f), FontStyles.Normal);

        CriarBotao(painelMorte.transform, "BotaoTryAgain",
            "↺  TRY AGAIN",
            new Vector2(0, -100), new Color(0.55f, 0.05f, 0.05f, 1f),
            () => SceneManager.LoadScene("OpenWorld"));

        CriarBotao(painelMorte.transform, "BotaoMenuMorte",
            "⌂  MAIN MENU",
            new Vector2(0, -200), new Color(0.12f, 0.12f, 0.12f, 1f),
            () => SceneManager.LoadScene("MainMenu"));

        // ── ECRÃ DE VITÓRIA ───────────────────────────────────────
        painelVitoriaGerado = CriarPainelFullscreen(canvasGO.transform, "PainelVitoria",
            new Color(0f, 0.04f, 0f, 0.92f));
        painelVitoriaGerado.SetActive(false);

        CriarTexto(painelVitoriaGerado.transform, "TxtVitoria",
            "YOU SURVIVED", new Vector2(0, 140), new Vector2(900, 160),
            90, new Color(0.2f, 0.9f, 0.2f, 1f), FontStyles.Bold);

        CriarTexto(painelVitoriaGerado.transform, "TxtSubVitoria",
            "All 5 waves defeated. The nightmare is over.", new Vector2(0, 40), new Vector2(800, 60),
            32, new Color(0.75f, 0.75f, 0.75f), FontStyles.Normal);

        CriarTexto(painelVitoriaGerado.transform, "TxtCreditos",
            "TECMUL 2026  ·  Artur Salgado  ·  Tiago Silva",
            new Vector2(0, -50), new Vector2(900, 50),
            28, new Color(0.55f, 0.55f, 0.55f), FontStyles.Normal);

        CriarBotao(painelVitoriaGerado.transform, "BotaoMenuVitoria",
            "⌂  MAIN MENU",
            new Vector2(0, -160), new Color(0.1f, 0.45f, 0.1f, 1f),
            () => SceneManager.LoadScene("MainMenu"));
    }

    // ── API PÚBLICA ───────────────────────────────────────────────

    public void AtualizarVida(float vidaAtual, float vidaMax)
    {
        if (barraVidaFill != null)
        {
            float pct = vidaAtual / vidaMax;
            barraVidaFill.rectTransform.localScale = new Vector3(pct, 1f, 1f);
            barraVidaFill.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(0.1f, 0.7f, 0.1f), pct);
        }
        if (textoVida != null)
            textoVida.text = $"❤  {(int)vidaAtual}";
    }

    public void AtualizarHorda(int hordaAtual, int totalHordas, int zombiesRestantes)
    {
        if (textoHorda != null)
            textoHorda.text = $"HORDA {hordaAtual} / {totalHordas}";
        if (textoZombies != null)
            textoZombies.text = $"Zombies: {zombiesRestantes}";
        if (textoContagemRegressiva != null)
            textoContagemRegressiva.text = "";
    }

    public void AtualizarZombiesRestantes(int zombiesRestantes)
    {
        if (textoZombies != null)
            textoZombies.text = $"Zombies: {zombiesRestantes}";
    }

    public void MostrarTempoHorda(int segundos)
    {
        if (textoContagemRegressiva != null)
            textoContagemRegressiva.text = $"⏱  {segundos}s";
    }

    public void MostrarContagemRegressiva(int segundos)
    {
        if (textoContagemRegressiva != null)
            textoContagemRegressiva.text = $"Próxima horda em: {segundos}s";
    }

    public void MostrarMorte()
    {
        if (painelMorte != null) painelMorte.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void MostrarVitoria()
    {
        GameObject pv = painelVitoriaGerado ?? painelVitoria;
        if (pv != null) pv.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    // ── HELPERS ───────────────────────────────────────────────────

    GameObject CriarPainel(Transform pai, string nome, Color cor, Vector2 pos, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        go.AddComponent<Image>().color = cor;
        return go;
    }

    GameObject CriarPainelFullscreen(Transform pai, string nome, Color cor)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = cor;
        return go;
    }

    TextMeshProUGUI CriarTexto(Transform pai, string nome, string texto, Vector2 pos,
        Vector2 tamanho, int fontSize, Color cor, FontStyles estilo)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.color = cor;
        tmp.fontStyle = estilo;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    void CriarBotao(Transform pai, string nome, string rotulo, Vector2 pos, Color corFundo,
        UnityEngine.Events.UnityAction acao)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(380, 65);
        Image img = go.AddComponent<Image>();
        img.color = corFundo;
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(corFundo.r + 0.2f, 1f),
            Mathf.Min(corFundo.g + 0.15f, 1f),
            Mathf.Min(corFundo.b + 0.15f, 1f));
        btn.colors = cb;
        btn.onClick.AddListener(acao);
        CriarTexto(go.transform, nome + "Txt", rotulo,
            Vector2.zero, new Vector2(380, 65), 30, Color.white, FontStyles.Bold);
    }
}
