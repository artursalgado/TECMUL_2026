using UnityEngine;

public class LootContainer : MonoBehaviour, IInteractable
{
    public string zoneName = "Outskirts";
    public string displayName = "Supply Cache";
    public string supplyType = "Supplies";
    public int amount = 1;
    public int ammoReward = 0;
    public int healReward = 0;

    private bool isLooted = false;
    private Renderer cachedRenderer;

    void Start()
    {
        cachedRenderer = GetComponent<Renderer>();
        GameManager.Instance?.RegisterSupplyCache(zoneName);
    }

    public string GetPrompt(PlayerInteractor interactor)
    {
        if (isLooted)
        {
            return string.Empty;
        }

        return $"Press E to search {displayName} ({supplyType})";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (isLooted)
        {
            return;
        }

        PlayerHealth playerHealth = interactor != null ? interactor.PlayerHealth : null;
        Shooting shooting = interactor != null ? interactor.Shooting : null;
        PlayerInventory inventory = interactor != null ? interactor.Inventory : null;

        bool addedToInventory = true;
        bool rarePickup = false;
        string inventoryMessage = string.Empty;

        int addedAmmo = 0;
        int addedSupply = 0;

        if (ammoReward > 0 && shooting != null && inventory != null)
        {
            addedToInventory = inventory.TryAddResource("Ammo", ammoReward, out inventoryMessage, out rarePickup);
            if (!addedToInventory)
            {
                UIManager.Instance?.ShowMessage(inventoryMessage);
                return;
            }

            addedAmmo = ammoReward;
        }

        if (healReward > 0 && playerHealth != null)
        {
            playerHealth.Heal(healReward);
        }

        if (inventory != null && !string.IsNullOrWhiteSpace(supplyType))
        {
            addedToInventory = inventory.TryAddResource(supplyType, amount, out inventoryMessage, out rarePickup);
            addedSupply = addedToInventory ? amount : 0;
        }

        if (!addedToInventory)
        {
            if (addedAmmo > 0)
            {
                inventory.RemoveResource("Ammo", addedAmmo);
            }

            if (addedSupply > 0)
            {
                inventory.RemoveResource(supplyType, addedSupply);
            }

            UIManager.Instance?.ShowMessage(inventoryMessage);
            return;
        }

        isLooted = true;

        if (cachedRenderer != null)
        {
            cachedRenderer.sharedMaterial.color = new Color(0.25f, 0.25f, 0.25f);
        }

        GameManager.Instance?.CollectSupply(zoneName);
        if (rarePickup)
        {
            UIManager.Instance?.ShowMessage($"Rare pickup: {displayName}");
        }
    }
}
