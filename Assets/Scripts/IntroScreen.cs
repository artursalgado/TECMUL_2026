using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class IntroScreen : MonoBehaviour
{
    [Header("Fonte manuscrita")]
    public TMP_FontAsset fonteManuscrita;

    private SimplePlayer playerMovement;
    private WaveManager waveManager;

    private Canvas canvas;
    private TextMeshProUGUI textoHistoria;
    private TextMeshProUGUI textoIniciar;
    private GameObject painelIntro;

    private readonly string[] linhas = new string[]
    {
        "Dia 14 do surto.",
        "",
        "A zona foi selada pelas autoridades.",
        "Ninguém entra. Ninguém sai.",
        "",
        "Tu és o único sobrevivente ainda de pé.",
        "",
        "Há um portão na saída norte.",
        "Está sem energia.",
        "",
        "Três geradores espalhados pela zona",
        "podem restaurar a corrente.",
        "",
        "Mas cada vez que um arrancar...",
        "eles vão ouvir.",
        "",
        "Encontra os geradores.",
        "Abre o portão.",
        "Escapa.",
        "",
        "Boa sorte."
    };

    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "OpenWorld")
        {
            gameObject.SetActive(false);
            return;
        }

        playerMovement = FindFirstObjectByType<SimplePlayer>();
        waveManager = FindFirstObjectByType<WaveManager>();

        if (playerMovement != null) playerMovement.enabled = false;

        ConstruirEcra();
        StartCoroutine(EscreverTexto());
    }

    void ConstruirEcra()
    {
        // Canvas fullscreen
        GameObject canvasGO = new GameObject("IntroCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fundo escuro com tom sépia/quente
        painelIntro = CriarPainel(canvasGO.transform, "Fundo",
            new Color(0.06f, 0.04f, 0.03f, 0.97f),
            Vector2.zero, new Vector2(1920, 1080));

        // Linha decorativa topo
        CriarPainel(canvasGO.transform, "LinhaTopo",
            new Color(0.55f, 0.35f, 0.15f, 0.6f),
            new Vector2(0, 460), new Vector2(900, 1));

        // Linha decorativa base
        CriarPainel(canvasGO.transform, "LinhaBase",
            new Color(0.55f, 0.35f, 0.15f, 0.6f),
            new Vector2(0, -460), new Vector2(900, 1));

        // Título "DIÁRIO DE SOBREVIVÊNCIA"
        GameObject tituloGO = new GameObject("Titulo");
        tituloGO.transform.SetParent(canvasGO.transform, false);
        RectTransform tituloRT = tituloGO.AddComponent<RectTransform>();
        tituloRT.anchoredPosition = new Vector2(0, 430);
        tituloRT.sizeDelta = new Vector2(800, 60);
        TextMeshProUGUI titulo = tituloGO.AddComponent<TextMeshProUGUI>();
        titulo.text = "DIÁRIO DE SOBREVIVÊNCIA";
        titulo.fontSize = 28;
        titulo.color = new Color(0.55f, 0.35f, 0.15f, 0.8f);
        titulo.alignment = TextAlignmentOptions.Center;
        titulo.fontStyle = FontStyles.Bold | FontStyles.Italic;
        if (fonteManuscrita != null) titulo.font = fonteManuscrita;

        // Área do texto principal
        GameObject textoGO = new GameObject("TextoHistoria");
        textoGO.transform.SetParent(canvasGO.transform, false);
        RectTransform textoRT = textoGO.AddComponent<RectTransform>();
        textoRT.anchoredPosition = new Vector2(0, 20);
        textoRT.sizeDelta = new Vector2(780, 800);
        textoHistoria = textoGO.AddComponent<TextMeshProUGUI>();
        textoHistoria.text = "";
        textoHistoria.fontSize = 38;
        textoHistoria.color = new Color(0.92f, 0.87f, 0.75f, 1f);
        textoHistoria.alignment = TextAlignmentOptions.Center;
        textoHistoria.lineSpacing = 8f;
        if (fonteManuscrita != null) textoHistoria.font = fonteManuscrita;

        // Botão "Iniciar" (aparece no fim)
        GameObject btnGO = new GameObject("BotaoIniciar");
        btnGO.transform.SetParent(canvasGO.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchoredPosition = new Vector2(0, -430);
        btnRT.sizeDelta = new Vector2(320, 55);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.55f, 0.2f, 0.05f, 0.9f);
        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(FecharIntro);

        GameObject btnTextoGO = new GameObject("BotaoTexto");
        btnTextoGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextoRT = btnTextoGO.AddComponent<RectTransform>();
        btnTextoRT.anchorMin = Vector2.zero;
        btnTextoRT.anchorMax = Vector2.one;
        btnTextoRT.offsetMin = Vector2.zero;
        btnTextoRT.offsetMax = Vector2.zero;
        textoIniciar = btnTextoGO.AddComponent<TextMeshProUGUI>();
        textoIniciar.text = "SOBREVIVER";
        textoIniciar.fontSize = 30;
        textoIniciar.color = new Color(0.95f, 0.85f, 0.65f);
        textoIniciar.alignment = TextAlignmentOptions.Center;
        textoIniciar.fontStyle = FontStyles.Bold;
        if (fonteManuscrita != null) textoIniciar.font = fonteManuscrita;

        btnGO.SetActive(false);
        textoIniciar.gameObject.SetActive(false);

        // Guarda referência ao botão para ativar no fim
        _botaoIniciar = btnGO;
    }

    private GameObject _botaoIniciar;

    IEnumerator EscreverTexto()
    {
        yield return new WaitForSeconds(0.8f);

        string textoCompleto = "";

        foreach (string linha in linhas)
        {
            foreach (char c in linha)
            {
                textoCompleto += c;
                textoHistoria.text = textoCompleto;
                yield return new WaitForSeconds(0.04f);
            }
            textoCompleto += "\n";
            textoHistoria.text = textoCompleto;
            yield return new WaitForSeconds(linha.Length > 0 ? 0.15f : 0.05f);
        }

        yield return new WaitForSeconds(0.5f);
        _botaoIniciar.SetActive(true);
        textoIniciar.gameObject.SetActive(true);
    }

    void FecharIntro()
    {
        if (playerMovement != null) playerMovement.enabled = true;
        if (waveManager != null) waveManager.IniciarJogo();
        Destroy(canvas.gameObject);
        Destroy(gameObject);
    }

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
}
