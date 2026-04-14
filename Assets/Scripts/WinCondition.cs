using UnityEngine;

public class WinCondition : MonoBehaviour
{
    [Header("Mensagens")]
    public string notReadyMessage = "You need more supplies before extracting!";
    public string readyMessage = "Extraction zone! Press E to extract.";

    private bool playerInZone = false;

    void Update()
    {
        if (!playerInZone) return;

        if (Input.GetKeyDown(KeyCode.E))
            TryExtract();

        bool canExtract = GameManager.Instance != null && GameManager.Instance.CanExtract();
        UIManager.Instance?.UpdatePrompt(canExtract ? readyMessage : notReadyMessage);
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
        UIManager.Instance?.UpdatePrompt(string.Empty);
    }

    void TryExtract()
    {
        if (GameManager.Instance == null || !GameManager.Instance.CanExtract())
        {
            UIManager.Instance?.ShowMessage(notReadyMessage);
            return;
        }

        GameManager.Instance.Extract();
        UIManager.Instance?.UpdatePrompt(string.Empty);
    }
}
