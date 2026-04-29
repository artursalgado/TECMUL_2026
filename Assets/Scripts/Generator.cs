using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Generator : MonoBehaviour
{
    [Header("Configuração")]
    public int generatorID = 1;
    public float distanciaInteracao = 6f;
    public float tempoAtivacao = 2f;

    [HideInInspector] public bool ativado = false;

    private GameManager gameManager;
    private Transform jogador;
    private bool dentroDoRaio = false;
    private float progressoAtivacao = 0f;
    private AudioSource audioLigar;
    private AudioSource audioFuncional;

    // UI gerada dinamicamente
    private Canvas canvasUI;
    private TextMeshProUGUI textoPrompt;
    private GameObject painelBarra;
    private Image barraFill;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            jogador = playerObj.transform;

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length > 0) audioLigar    = sources[0];
        if (sources.Length > 1) audioFuncional = sources[1];

        CriarUI();
    }

    void CriarUI()
    {
        GameObject canvasGO = new GameObject("GeneratorCanvas_" + generatorID);
        canvasGO.transform.SetParent(transform);
        canvasUI = canvasGO.AddComponent<Canvas>();
        canvasUI.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(400, 100);
        canvasGO.transform.localPosition = new Vector3(0, 2.5f, 0);
        canvasGO.transform.localScale = Vector3.one * 0.012f;

        // Texto "Pressiona E"
        GameObject textoGO = new GameObject("TextoPrompt");
        textoGO.transform.SetParent(canvasGO.transform, false);
        RectTransform textoRT = textoGO.AddComponent<RectTransform>();
        textoRT.anchoredPosition = new Vector2(0, 20);
        textoRT.sizeDelta = new Vector2(300, 40);
        textoPrompt = textoGO.AddComponent<TextMeshProUGUI>();
        textoPrompt.text = "[E] Ativar Gerador";
        textoPrompt.fontSize = 32;
        textoPrompt.color = new Color(1f, 0.9f, 0.2f);
        textoPrompt.alignment = TextAlignmentOptions.Center;
        textoPrompt.fontStyle = FontStyles.Bold;

        // Fundo da barra
        GameObject fundoGO = new GameObject("BarraFundo");
        fundoGO.transform.SetParent(canvasGO.transform, false);
        RectTransform fundoRT = fundoGO.AddComponent<RectTransform>();
        fundoRT.anchoredPosition = new Vector2(0, -15);
        fundoRT.sizeDelta = new Vector2(340, 22);
        Image fundoImg = fundoGO.AddComponent<Image>();
        fundoImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Preenchimento da barra
        painelBarra = new GameObject("BarraFill");
        painelBarra.transform.SetParent(fundoGO.transform, false);
        RectTransform fillRT = painelBarra.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        barraFill = painelBarra.AddComponent<Image>();
        barraFill.color = new Color(0.2f, 0.8f, 0.2f);

        canvasUI.gameObject.SetActive(false);
    }

    void Update()
    {
        if (ativado || jogador == null) return;

        float distancia = Vector3.Distance(transform.position, jogador.position);
        dentroDoRaio = distancia <= distanciaInteracao;

        // Faz o canvas olhar para a câmara (sem espelhar)
        if (canvasUI != null && Camera.main != null)
        {
            Vector3 dir = canvasUI.transform.position - Camera.main.transform.position;
            canvasUI.transform.rotation = Quaternion.LookRotation(dir);
        }

        if (dentroDoRaio)
        {
            canvasUI.gameObject.SetActive(true);

            if (Input.GetKey(KeyCode.E))
            {
                progressoAtivacao += Time.deltaTime;
                barraFill.rectTransform.localScale = new Vector3(progressoAtivacao / tempoAtivacao, 1f, 1f);
                textoPrompt.text = "A ativar...";
                if (audioLigar != null && !audioLigar.isPlaying) audioLigar.Play();

                if (progressoAtivacao >= tempoAtivacao)
                    Ativar();
            }
            else
            {
                progressoAtivacao = 0f;
                barraFill.rectTransform.localScale = Vector3.zero;
                textoPrompt.text = "[E] Ativar Gerador";
                if (audioLigar != null && audioLigar.isPlaying) audioLigar.Stop();
            }
        }
        else
        {
            canvasUI.gameObject.SetActive(false);
            progressoAtivacao = 0f;
        }
    }

    void Ativar()
    {
        ativado = true;
        canvasUI.gameObject.SetActive(false);
        if (audioLigar != null) audioLigar.Stop();
        if (audioFuncional != null) audioFuncional.Play();

        if (gameManager != null)
            gameManager.GeneratorActivated(generatorID);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}
