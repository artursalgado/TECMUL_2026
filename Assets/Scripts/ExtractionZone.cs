using UnityEngine;

public class ExtractionZone : MonoBehaviour, IInteractable
{
    public string prompt = "Hold E to extract";

    public string GetPrompt(PlayerInteractor interactor)
    {
        if (GameManager.Instance == null)
        {
            return string.Empty;
        }

        return GameManager.Instance.CanExtract()
            ? prompt
            : "Finish all objectives to unlock extraction";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (!GameManager.Instance.CanExtract())
        {
            UIManager.Instance?.ShowMessage("Extraction is still locked");
            return;
        }

        GameManager.Instance.Extract();
    }
}
