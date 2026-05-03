using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    // ── Componentes gerados ────────────────────────────────────────
    private Canvas canvas;

    // Saúde
    private TextMeshProUGUI textoVidaNum;
    private Image barraVidaFill;

    // Arma / Munição
    private TextMeshProUGUI textoNomeArma;
    private TextMeshProUGUI textoMunicaoAtual;
    private TextMeshProUGUI textoMunicaoMax;
    private TextMeshProUGUI textoSeparadorMunicao;

    // Geradores (top-left)
    private TextMeshProUGUI textoGeradores;

    // Mensagem central
    private TextMeshProUGUI textoMensagem;

    // Hit marker
    private Image[] hitMarkerLinhas = new Image[4];
    private float hitMarkerTimer = 0f;
    private const float HIT_MARKER_DURACAO = 0.15f;

    // Mira dinâmica
    private RectTransform miraEsq, miraDir, miraCima, miraBaixo;
    private float miraAbertura = 6f;
    private float miraAberturaAlvo = 6f;
    private const float MIRA_FECHADA = 4f;
    private const float MIRA_ABERTA = 16f;
    private const float MIRA_SPRINT = 28f;

    // Ecrãs
    private GameObject painelMorte;
    private GameObject painelVitoriaGerado;

    // Referências externas
    [Header("Paineis externos (opcional)")]
    public GameObject painelVitoria;

    [Header("Minimapa")]
    public RenderTexture minimapTexture;

    void Awake()
    {
        ConstruirHUD();
    }

    void Update()
    {
        bool emSprint = Input.GetKey(KeyCode.LeftShift) &&
                       (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.05f ||
                        Mathf.Abs(Input.GetAxis("Vertical")) > 0.05f);
        bool emMovimento = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.05f
                        || Mathf.Abs(Input.GetAxis("Vertical")) > 0.05f;
        miraAberturaAlvo = emSprint ? MIRA_SPRINT : emMovimento ? MIRA_ABERTA : MIRA_FECHADA;
        miraAbertura = Mathf.Lerp(miraAbertura, miraAberturaAlvo, Time.deltaTime * 8f);
        AtualizarMira();

        if (hitMarkerTimer > 0f)
        {
            hitMarkerTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(hitMarkerTimer / HIT_MARKER_DURACAO);
            foreach (var img in hitMarkerLinhas)
                if (img != null) img.color = new Color(1f, 0.15f, 0.15f, alpha);
        }
    }

    void ConstruirHUD()
    {
        // CANVAS
        GameObject canvasGO = new GameObject("HUDCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── MINIMAPA top-right ────────────────────────────────────
        ConstruirMinimapa(canvasGO.transform);

        // ── GERADORES top-left ─────────────────────────────────────
        textoGeradores = CriarTexto(canvasGO.transform, "TextoGeradores",
            "GERADORES  0 / 3",
            Anchor.TopLeft, new Vector2(30, -30), new Vector2(340, 50),
            28, new Color(0.9f, 0.75f, 0.2f, 1f), FontStyles.Bold,
            TextAlignmentOptions.Left);

        // ── MENSAGEM CENTRAL ──────────────────────────────────────
        textoMensagem = CriarTexto(canvasGO.transform, "TextoMensagem",
            "",
            Anchor.Center, new Vector2(0, 80), new Vector2(900, 70),
            40, new Color(1f, 0.9f, 0.3f, 1f), FontStyles.Bold,
            TextAlignmentOptions.Center);

        // ── MIRA ──────────────────────────────────────────────────
        ConstruirMira(canvasGO.transform);

        // ── HIT MARKER ────────────────────────────────────────────
        ConstruirHitMarker(canvasGO.transform);

        // ── SAÚDE bottom-left ─────────────────────────────────────
        // Número grande
        textoVidaNum = CriarTexto(canvasGO.transform, "TextoVidaNum",
            "100",
            Anchor.BottomLeft, new Vector2(30, 70), new Vector2(160, 90),
            80, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Left);

        // Label "HP"
        CriarTexto(canvasGO.transform, "TextoHP",
            "HP",
            Anchor.BottomLeft, new Vector2(195, 88), new Vector2(60, 40),
            26, new Color(0.6f, 0.6f, 0.6f, 1f), FontStyles.Normal,
            TextAlignmentOptions.Left);

        // Barra fina de vida
        GameObject fundoBarra = CriarPainelAncorado(canvasGO.transform, "VidaBarraFundo",
            new Color(0.15f, 0.15f, 0.15f, 0.85f),
            Anchor.BottomLeft, new Vector2(30, 40), new Vector2(220, 8));

        GameObject fillBarra = new GameObject("VidaBarraFill");
        fillBarra.transform.SetParent(fundoBarra.transform, false);
        RectTransform fillRT = fillBarra.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        barraVidaFill = fillBarra.AddComponent<Image>();
        barraVidaFill.color = new Color(0.2f, 0.85f, 0.2f, 1f);

        // ── ARMA bottom-right ─────────────────────────────────────
        // Nome da arma
        textoNomeArma = CriarTexto(canvasGO.transform, "TextoNomeArma",
            "PISTOLA",
            Anchor.BottomRight, new Vector2(-30, 100), new Vector2(320, 40),
            30, new Color(0.75f, 0.75f, 0.75f, 1f), FontStyles.Bold,
            TextAlignmentOptions.Right);

        // Munição atual (grande)
        textoMunicaoAtual = CriarTexto(canvasGO.transform, "TextoMunicaoAtual",
            "12",
            Anchor.BottomRight, new Vector2(-110, 48), new Vector2(180, 70),
            62, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Right);

        // Separador " / "
        textoSeparadorMunicao = CriarTexto(canvasGO.transform, "TextoSep",
            "/",
            Anchor.BottomRight, new Vector2(-82, 44), new Vector2(40, 55),
            36, new Color(0.5f, 0.5f, 0.5f, 1f), FontStyles.Normal,
            TextAlignmentOptions.Center);

        // Munição reserva (pequena)
        textoMunicaoMax = CriarTexto(canvasGO.transform, "TextoMunicaoMax",
            "36",
            Anchor.BottomRight, new Vector2(-28, 44), new Vector2(100, 55),
            34, new Color(0.55f, 0.55f, 0.55f, 1f), FontStyles.Normal,
            TextAlignmentOptions.Left);

        // Linha separadora fina acima da munição
        CriarPainelAncorado(canvasGO.transform, "ArmaDivisor",
            new Color(0.4f, 0.4f, 0.4f, 0.5f),
            Anchor.BottomRight, new Vector2(-30, 128), new Vector2(300, 1));

        // ── ECRÃ DE MORTE ─────────────────────────────────────────
        painelMorte = CriarPainelFullscreen(canvasGO.transform, "PainelMorte",
            new Color(0.04f, 0f, 0f, 0.93f));
        painelMorte.SetActive(false);

        CriarTexto(painelMorte.transform, "TxtYouDied",
            "YOU DIED",
            Anchor.Center, new Vector2(0, 130), new Vector2(800, 170),
            110, new Color(0.85f, 0.08f, 0.08f, 1f), FontStyles.Bold,
            TextAlignmentOptions.Center);

        CriarTexto(painelMorte.transform, "TxtSub",
            "The zone consumed you.",
            Anchor.Center, new Vector2(0, 30), new Vector2(700, 60),
            36, new Color(0.65f, 0.65f, 0.65f, 1f), FontStyles.Normal,
            TextAlignmentOptions.Center);

        CriarBotao(painelMorte.transform, "BotaoTryAgain",
            "TRY AGAIN",
            new Vector2(0, -90), new Color(0.55f, 0.05f, 0.05f, 1f),
            () => { Time.timeScale = 1f; SceneManager.LoadScene("OpenWorld"); });

        CriarBotao(painelMorte.transform, "BotaoMenuMorte",
            "MAIN MENU",
            new Vector2(0, -185), new Color(0.13f, 0.13f, 0.13f, 1f),
            () => { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); });

        // ── ECRÃ DE VITÓRIA ───────────────────────────────────────
        painelVitoriaGerado = CriarPainelFullscreen(canvasGO.transform, "PainelVitoria",
            new Color(0f, 0.03f, 0f, 0.93f));
        painelVitoriaGerado.SetActive(false);

        CriarTexto(painelVitoriaGerado.transform, "TxtVitoria",
            "ESCAPED",
            Anchor.Center, new Vector2(0, 150), new Vector2(900, 160),
            100, new Color(0.15f, 0.9f, 0.15f, 1f), FontStyles.Bold,
            TextAlignmentOptions.Center);

        CriarTexto(painelVitoriaGerado.transform, "TxtSubVitoria",
            "You activated the generators and escaped the zone.",
            Anchor.Center, new Vector2(0, 50), new Vector2(850, 60),
            32, new Color(0.7f, 0.7f, 0.7f, 1f), FontStyles.Normal,
            TextAlignmentOptions.Center);

        CriarTexto(painelVitoriaGerado.transform, "TxtCreditos",
            "TECMUL 2026  |  Artur Salgado  |  Tiago Costa",
            Anchor.Center, new Vector2(0, -40), new Vector2(900, 50),
            26, new Color(0.45f, 0.45f, 0.45f, 1f), FontStyles.Normal,
            TextAlignmentOptions.Center);

        CriarBotao(painelVitoriaGerado.transform, "BotaoMenuVitoria",
            "MAIN MENU",
            new Vector2(0, -150), new Color(0.08f, 0.42f, 0.08f, 1f),
            () => { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); });
    }

    void ConstruirMinimapa(Transform pai)
    {
        // Fundo circular escuro
        GameObject fundo = new GameObject("MinimapFundo");
        fundo.transform.SetParent(pai, false);
        RectTransform fundoRT = fundo.AddComponent<RectTransform>();
        AplicarAncora(fundoRT, Anchor.TopRight);
        fundoRT.anchoredPosition = new Vector2(-20, -20);
        fundoRT.sizeDelta = new Vector2(180, 180);
        Image fundoImg = fundo.AddComponent<Image>();
        fundoImg.color = new Color(0f, 0f, 0f, 0.6f);

        // Máscara circular
        fundo.AddComponent<Mask>().showMaskGraphic = true;

        // Imagem do render texture
        if (minimapTexture != null)
        {
            GameObject mapImg = new GameObject("MinimapImage");
            mapImg.transform.SetParent(fundo.transform, false);
            RectTransform mapRT = mapImg.AddComponent<RectTransform>();
            mapRT.anchorMin = Vector2.zero;
            mapRT.anchorMax = Vector2.one;
            mapRT.offsetMin = Vector2.zero;
            mapRT.offsetMax = Vector2.zero;
            RawImage raw = mapImg.AddComponent<RawImage>();
            raw.texture = minimapTexture;
        }

        // Ponto do jogador (centro)
        GameObject ponto = new GameObject("MinimapPlayer");
        ponto.transform.SetParent(fundo.transform, false);
        RectTransform pontoRT = ponto.AddComponent<RectTransform>();
        pontoRT.anchorMin = new Vector2(0.5f, 0.5f);
        pontoRT.anchorMax = new Vector2(0.5f, 0.5f);
        pontoRT.sizeDelta = new Vector2(10, 10);
        pontoRT.anchoredPosition = Vector2.zero;
        Image pontoImg = ponto.AddComponent<Image>();
        pontoImg.color = new Color(0.2f, 0.8f, 1f, 1f);

        // Borda do minimapa
        GameObject borda = new GameObject("MinimapBorda");
        borda.transform.SetParent(pai, false);
        RectTransform bordaRT = borda.AddComponent<RectTransform>();
        AplicarAncora(bordaRT, Anchor.TopRight);
        bordaRT.anchoredPosition = new Vector2(-20, -20);
        bordaRT.sizeDelta = new Vector2(184, 184);
        Image bordaImg = borda.AddComponent<Image>();
        bordaImg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        borda.transform.SetSiblingIndex(fundo.transform.GetSiblingIndex());
    }

    void ConstruirHitMarker(Transform pai)
    {
        float offset = 9f;
        float comprimento = 8f;
        float espessura = 2f;
        float[] angulos = { 45f, 135f, 225f, 315f };

        for (int i = 0; i < 4; i++)
        {
            float rad = angulos[i] * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(rad) * offset, Mathf.Sin(rad) * offset);

            GameObject go = new GameObject($"HitMarker_{i}");
            go.transform.SetParent(pai, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(comprimento, espessura);
            rt.localEulerAngles = new Vector3(0, 0, angulos[i]);
            hitMarkerLinhas[i] = go.AddComponent<Image>();
            hitMarkerLinhas[i].color = new Color(1f, 0.15f, 0.15f, 0f);
        }
    }

    void ConstruirMira(Transform pai)
    {
        Color corMira = new Color(0.9f, 0.9f, 0.9f, 0.85f);
        int espessura = 2;
        int comprimento = 10;

        miraEsq = CriarLinhaMira(pai, "MiraEsq", corMira,
            new Vector2(-(MIRA_FECHADA + comprimento / 2f), 0),
            new Vector2(comprimento, espessura));

        miraDir = CriarLinhaMira(pai, "MiraDir", corMira,
            new Vector2(MIRA_FECHADA + comprimento / 2f, 0),
            new Vector2(comprimento, espessura));

        miraCima = CriarLinhaMira(pai, "MiraCima", corMira,
            new Vector2(0, MIRA_FECHADA + comprimento / 2f),
            new Vector2(espessura, comprimento));

        miraBaixo = CriarLinhaMira(pai, "MiraBaixo", corMira,
            new Vector2(0, -(MIRA_FECHADA + comprimento / 2f)),
            new Vector2(espessura, comprimento));

        // Ponto central
        CriarLinhaMira(pai, "MiraPonto", new Color(0.9f, 0.9f, 0.9f, 0.6f),
            Vector2.zero, new Vector2(espessura, espessura));
    }

    RectTransform CriarLinhaMira(Transform pai, string nome, Color cor, Vector2 pos, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        go.AddComponent<Image>().color = cor;
        return rt;
    }

    void AtualizarMira()
    {
        if (miraEsq == null) return;
        float meio = miraAbertura;
        int comp = 10;
        miraEsq.anchoredPosition  = new Vector2(-(meio + comp / 2f), 0);
        miraDir.anchoredPosition  = new Vector2( meio + comp / 2f,  0);
        miraCima.anchoredPosition = new Vector2(0,  meio + comp / 2f);
        miraBaixo.anchoredPosition= new Vector2(0, -(meio + comp / 2f));
    }

    // ── API PÚBLICA ───────────────────────────────────────────────

    public void AtualizarVida(float vidaAtual, float vidaMax)
    {
        if (textoVidaNum != null)
            textoVidaNum.text = ((int)vidaAtual).ToString();

        if (barraVidaFill != null)
        {
            float pct = Mathf.Clamp01(vidaAtual / vidaMax);
            barraVidaFill.rectTransform.anchorMax = new Vector2(pct, 1f);
            barraVidaFill.rectTransform.offsetMax = Vector2.zero;

            if (pct > 0.6f)
                barraVidaFill.color = new Color(0.2f, 0.85f, 0.2f, 1f);
            else if (pct > 0.3f)
                barraVidaFill.color = new Color(0.9f, 0.75f, 0.1f, 1f);
            else
                barraVidaFill.color = new Color(0.9f, 0.15f, 0.1f, 1f);
        }
    }

    public void AtualizarArma(string nome, int municaoAtual, int municaoMax)
    {
        if (textoNomeArma != null)
            textoNomeArma.text = nome;

        bool temMunicao = municaoAtual >= 0;
        if (textoMunicaoAtual != null)
        {
            textoMunicaoAtual.gameObject.SetActive(temMunicao);
            if (temMunicao) textoMunicaoAtual.text = municaoAtual.ToString();
        }
        if (textoSeparadorMunicao != null) textoSeparadorMunicao.gameObject.SetActive(temMunicao);
        if (textoMunicaoMax != null)
        {
            textoMunicaoMax.gameObject.SetActive(temMunicao);
            if (temMunicao) textoMunicaoMax.text = municaoMax.ToString();
        }
    }

    public void AtualizarGeradores(int ativos, int total)
    {
        if (textoGeradores != null)
            textoGeradores.text = $"GERADORES  {ativos} / {total}";
    }

    public void MostrarHeadshot()
    {
        MostrarMensagemRapida("HEAD SHOT!", new Color(1f, 0.85f, 0f, 1f));
    }

    public void MostrarAviso(string texto)
    {
        MostrarMensagemRapida(texto, new Color(1f, 0.3f, 0.3f, 1f));
    }

    void MostrarMensagemRapida(string texto, Color cor)
    {
        if (textoMensagem == null) return;
        StopCoroutine(nameof(FadeOutMensagem));
        textoMensagem.text = texto;
        textoMensagem.color = cor;
        textoMensagem.fontSize = texto == "HEAD SHOT!" ? 52 : 40;
        StartCoroutine(nameof(FadeOutMensagem));
    }

    public void MostrarHitMarker()
    {
        foreach (var img in hitMarkerLinhas)
            if (img != null) img.color = new Color(1f, 0.15f, 0.15f, 1f);
        hitMarkerTimer = HIT_MARKER_DURACAO;
    }

    public void MostrarMensagem(string mensagem)
    {
        if (textoMensagem != null)
        {
            StopCoroutine(nameof(FadeOutMensagem));
            textoMensagem.text = mensagem;
            textoMensagem.color = new Color(1f, 0.9f, 0.3f, 1f);
            StartCoroutine(nameof(FadeOutMensagem));
        }
    }

    IEnumerator FadeOutMensagem()
    {
        yield return new WaitForSeconds(3f);
        float t = 0f;
        Color cor = textoMensagem.color;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            textoMensagem.color = new Color(cor.r, cor.g, cor.b, 1f - t);
            yield return null;
        }
        textoMensagem.text = "";
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

    // Compatibilidade com WaveManager (não usada no modo geradores mas mantida)
    public void AtualizarHorda(int hordaAtual, int totalHordas, int zombiesRestantes) { }
    public void AtualizarZombiesRestantes(int zombiesRestantes) { }
    public void MostrarTempoHorda(int segundos) { }
    public void MostrarContagemRegressiva(int segundos) { }

    // ── HELPERS ───────────────────────────────────────────────────

    enum Anchor { TopLeft, TopRight, Center, BottomLeft, BottomRight }

    TextMeshProUGUI CriarTexto(Transform pai, string nome, string texto,
        Anchor ancora, Vector2 pos, Vector2 tamanho,
        int fontSize, Color cor, FontStyles estilo, TextAlignmentOptions alinhamento)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        AplicarAncora(rt, ancora);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.color = cor;
        tmp.fontStyle = estilo;
        tmp.alignment = alinhamento;
        return tmp;
    }

    GameObject CriarPainelAncorado(Transform pai, string nome, Color cor,
        Anchor ancora, Vector2 pos, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        AplicarAncora(rt, ancora);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        go.AddComponent<Image>().color = cor;
        return go;
    }

    void AplicarAncora(RectTransform rt, Anchor ancora)
    {
        switch (ancora)
        {
            case Anchor.TopLeft:
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1); break;
            case Anchor.TopRight:
                rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1); break;
            case Anchor.BottomLeft:
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0); break;
            case Anchor.BottomRight:
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0); break;
            case Anchor.Center:
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f); break;
        }
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

    void CriarBotao(Transform pai, string nome, string rotulo, Vector2 pos, Color corFundo,
        UnityEngine.Events.UnityAction acao)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(380, 65);
        Image img = go.AddComponent<Image>();
        img.color = corFundo;
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(corFundo.r + 0.25f, 1f),
            Mathf.Min(corFundo.g + 0.15f, 1f),
            Mathf.Min(corFundo.b + 0.15f, 1f));
        cb.pressedColor = new Color(corFundo.r * 0.7f, corFundo.g * 0.7f, corFundo.b * 0.7f);
        btn.colors = cb;
        btn.onClick.AddListener(acao);
        CriarTexto(go.transform, nome + "Txt", rotulo,
            Anchor.Center, Vector2.zero, new Vector2(380, 65),
            30, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
    }
}
