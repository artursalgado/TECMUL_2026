using UnityEngine;

// Coloca este script no trigger da ExtractionZone
// O jogador tem de ter todos os supplies recolhidos para vencer
public class WinCondition : MonoBehaviour
{
    [Header("Requisitos para vencer")]
    public int suppliesRequired = 0; // 0 = usa o total do GameManager

    [Header("Mensagens")]
    public string notReadyMessage = "You need more supplies before extracting!";
    public string readyMessage = "Extraction zone! Press E to extract.";

    private bool playerInZone = false;

    void Update()
    {
        if (!playerInZone) return;

        if (Input.GetKeyDown(KeyCode.E))
            TryExtract();

        // Mostra prompt consoante estado
        if (UIManager.Instance != null)
        {
            bool canExtract = CanExtract();
            UIManager.Instance.UpdatePrompt(canExtract ? readyMessage : notReadyMessage);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone = false;
        if (UIManager.Instance != null)
            UIManager.Instance.UpdatePrompt(string.Empty);
    }

    bool CanExtract()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return true;

        int required = suppliesRequired > 0 ? suppliesRequired : gm.GetTotalSupplies();
        return gm.GetCollectedSupplies() >= required;
    }

    void TryExtract()
    {
        if (!CanExtract())
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowMessage(notReadyMessage);
            return;
        }

        // Vitória!
        GameManager gm = GameManager.Instance;
        int score = gm != null ? gm.GetScore() : 0;

        if (GameOverScreen.Instance != null)
            GameOverScreen.Instance.ShowVictory(score);
        else if (gm != null)
            gm.TriggerVictory();

        if (UIManager.Instance != null)
            UIManager.Instance.UpdatePrompt(string.Empty);

        // Para o tempo do jogo
        Time.timeScale = 0f;
    }
}
