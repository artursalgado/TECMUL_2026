using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Textos do HUD")]
    public TextMeshProUGUI textoHorda;         // Ex: "HORDA 2 / 5"
    public TextMeshProUGUI textoZombies;       // Ex: "Zombies: 8"
    public TextMeshProUGUI textoContagemRegressiva; // Ex: "Próxima horda em: 45s"

    [Header("Painel de Vitória")]
    public GameObject painelVitoria;           // Painel com "SOBREVIVESTE!"

    void Start()
    {
        // Esconde tudo no início
        if (textoContagemRegressiva != null)
            textoContagemRegressiva.gameObject.SetActive(false);

        if (painelVitoria != null)
            painelVitoria.SetActive(false);
    }

    public void AtualizarHorda(int hordaAtual, int totalHordas, int zombiesRestantes)
    {
        if (textoHorda != null)
            textoHorda.text = $"HORDA {hordaAtual} / {totalHordas}";

        if (textoZombies != null)
            textoZombies.text = $"Zombies: {zombiesRestantes}";

        // Esconde a contagem regressiva quando começa a horda
        if (textoContagemRegressiva != null)
            textoContagemRegressiva.gameObject.SetActive(false);
    }

    public void AtualizarZombiesRestantes(int zombiesRestantes)
    {
        if (textoZombies != null)
            textoZombies.text = $"Zombies: {zombiesRestantes}";
    }

    public void MostrarContagemRegressiva(int segundos)
    {
        if (textoContagemRegressiva != null)
        {
            textoContagemRegressiva.gameObject.SetActive(true);
            textoContagemRegressiva.text = $"Próxima horda em: {segundos}s";
        }
    }

    public void MostrarVitoria()
    {
        if (painelVitoria != null)
            painelVitoria.SetActive(true);

        if (textoHorda != null)
            textoHorda.text = "SOBREVIVESTE A TODAS AS HORDAS!";

        // Desbloqueia o rato
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Para o tempo do jogo
        Time.timeScale = 0f;
    }
}
