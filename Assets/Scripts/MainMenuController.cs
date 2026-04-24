using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;

        // Garante que o rato está visível e livre no menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ConstruirMenu();
    }

    void ConstruirMenu()
    {
        // ── EVENT SYSTEM (necessário para os botões funcionarem) ──
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── CANVAS ──────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MenuCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── FUNDO PRETO ──────────────────────────────────────────
        GameObject fundo = CriarPainel(canvasGO.transform, "Fundo",
            new Color(0.04f, 0.02f, 0.02f, 1f),
            Vector2.zero, new Vector2(1920, 1080));

        // ── LINHA VERMELHA DECORATIVA (CIMA) ─────────────────────
        CriarPainel(canvasGO.transform, "LinhaTop",
            new Color(0.7f, 0.05f, 0.05f, 1f),
            new Vector2(0, 470), new Vector2(1920, 4));

        // ── LINHA VERMELHA DECORATIVA (BAIXO) ────────────────────
        CriarPainel(canvasGO.transform, "LinhaBot",
            new Color(0.7f, 0.05f, 0.05f, 1f),
            new Vector2(0, -470), new Vector2(1920, 4));

        // ── TITULO PRINCIPAL "KILL THEM ALL" ─────────────────────
        CriarTexto(canvasGO.transform, "TituloKill",
            "KILL THEM ALL",
            new Vector2(0, 280), new Vector2(1200, 160),
            120, new Color(0.85f, 0.08f, 0.08f, 1f), FontStyles.Bold);

        // ── SUBTITULO "TECMUL 2026" ───────────────────────────────
        CriarTexto(canvasGO.transform, "TituloTECMUL",
            "TECMUL 2026",
            new Vector2(0, 170), new Vector2(800, 80),
            52, new Color(0.9f, 0.9f, 0.9f, 0.85f), FontStyles.Bold);

        // ── LINHA SEPARADORA ─────────────────────────────────────
        CriarPainel(canvasGO.transform, "Separador",
            new Color(0.6f, 0.06f, 0.06f, 0.8f),
            new Vector2(0, 110), new Vector2(500, 2));

        // ── BOTÃO START MISSION ───────────────────────────────────
        CriarBotao(canvasGO.transform, "BotaoStart",
            "▶  START MISSION",
            new Vector2(0, 20),
            new Color(0.7f, 0.05f, 0.05f, 1f),
            () => SceneManager.LoadScene("OpenWorld"));

        // ── BOTÃO QUIT ────────────────────────────────────────────
        CriarBotao(canvasGO.transform, "BotaoQuit",
            "✕  QUIT",
            new Vector2(0, -80),
            new Color(0.12f, 0.12f, 0.12f, 1f),
            () => Application.Quit());

        // ── CRÉDITOS ──────────────────────────────────────────────
        CriarTexto(canvasGO.transform, "Creditos",
            "Artur Salgado  ·  Tiago Silva",
            new Vector2(0, -420), new Vector2(800, 50),
            28, new Color(0.55f, 0.55f, 0.55f, 1f), FontStyles.Normal);

        // ── VERSÃO ───────────────────────────────────────────────
        CriarTexto(canvasGO.transform, "Versao",
            "v1.0",
            new Vector2(880, -500), new Vector2(200, 40),
            22, new Color(0.35f, 0.35f, 0.35f, 1f), FontStyles.Normal);

        // ── AVISO "SURVIVE ALL 5 WAVES" ──────────────────────────
        CriarTexto(canvasGO.transform, "Descricao",
            "SURVIVE ALL 5 WAVES",
            new Vector2(0, -170), new Vector2(800, 50),
            34, new Color(0.65f, 0.65f, 0.65f, 0.7f), FontStyles.Normal);

        // Animação de pulsar no título
        StartCoroutine(PulsarTitulo(GameObject.Find("TituloKill").GetComponent<TextMeshProUGUI>()));
    }

    // ── HELPERS ───────────────────────────────────────────────────

    GameObject CriarPainel(Transform pai, string nome, Color cor, Vector2 pos, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        Image img = go.AddComponent<Image>();
        img.color = cor;
        return go;
    }

    TextMeshProUGUI CriarTexto(Transform pai, string nome, string texto, Vector2 pos, Vector2 tamanho,
        int fontSize, Color cor, FontStyles estilo)
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

    void CriarBotao(Transform pai, string nome, string rotulo, Vector2 pos, Color corFundo, UnityEngine.Events.UnityAction acao)
    {
        // Fundo do botão
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(420, 70);
        Image img = go.AddComponent<Image>();
        img.color = corFundo;

        Button btn = go.AddComponent<Button>();

        // Efeito de cor ao passar o rato
        ColorBlock cb = btn.colors;
        cb.normalColor = corFundo;
        cb.highlightedColor = new Color(
            Mathf.Min(corFundo.r + 0.2f, 1f),
            Mathf.Min(corFundo.g + 0.1f, 1f),
            Mathf.Min(corFundo.b + 0.1f, 1f));
        cb.pressedColor = new Color(corFundo.r * 0.7f, corFundo.g * 0.7f, corFundo.b * 0.7f);
        btn.colors = cb;

        btn.onClick.AddListener(acao);

        // Texto do botão
        CriarTexto(go.transform, nome + "Txt", rotulo,
            Vector2.zero, new Vector2(420, 70),
            32, Color.white, FontStyles.Bold);
    }

    // Faz o título piscar suavemente
    IEnumerator PulsarTitulo(TextMeshProUGUI titulo)
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.8f;
                float alpha = Mathf.Lerp(0.75f, 1f, Mathf.SmoothStep(0, 1, t));
                titulo.color = new Color(0.85f, 0.08f, 0.08f, alpha);
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.8f;
                float alpha = Mathf.Lerp(1f, 0.75f, Mathf.SmoothStep(0, 1, t));
                titulo.color = new Color(0.85f, 0.08f, 0.08f, alpha);
                yield return null;
            }
        }
    }
}
