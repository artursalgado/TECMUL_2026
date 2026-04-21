using UnityEngine;

public class ObjectiveInteractable : MonoBehaviour, IInteractable
{
    public string zoneName = "Outskirts";
    public string objectiveId = "objective";
    public string prompt = "Press E to interact";
    public string completionMessage = "Objective complete";
    public string requiredResource = string.Empty;
    public int requiredAmount = 0;
    public bool consumesResource = false;
    public bool oneShot = true;

    private bool completed;
    private Renderer cachedRenderer;

    void Start()
    {
        cachedRenderer = GetComponent<Renderer>();
        RegisterObjectiveIfNeeded();
    }

    public string GetPrompt(PlayerInteractor interactor)
    {
        if (completed)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(requiredResource) && requiredAmount > 0)
        {
            return $"{prompt} ({requiredAmount} {requiredResource})";
        }

        return prompt;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (completed)
        {
            return;
        }

        PlayerInventory inventory = interactor != null ? interactor.Inventory : null;
        if (!string.IsNullOrWhiteSpace(requiredResource) && requiredAmount > 0)
        {
            if (inventory == null || !inventory.HasResource(requiredResource, requiredAmount))
            {
                UIManager.Instance?.ShowMessage($"Need {requiredAmount} {requiredResource}");
                return;
            }

            if (consumesResource)
            {
                inventory.RemoveResource(requiredResource, requiredAmount);
            }
        }

        completed = oneShot;
        GameManager.Instance?.CompleteObjective(objectiveId);
        UIManager.Instance?.ShowMessage(completionMessage);

        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = new Color(0.25f, 0.5f, 0.3f);
        }
    }

    void RegisterObjectiveIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(objectiveId))
        {
            return;
        }

        string description = !string.IsNullOrWhiteSpace(prompt) ? prompt : completionMessage;
        GameManager.Instance?.RegisterObjective(zoneName, objectiveId, description);
    }
}
