using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerInventory : MonoBehaviour
{
    public float maxCarryWeight = 20f;
    public int medkits = 1;
    public int food = 0;
    public int ammoReserve = 0;
    public int scrap = 0;
    public int fuel = 0;
    public int keys = 0;

    private readonly Dictionary<string, float> resourceWeights = new Dictionary<string, float>
    {
        { "Ammo", 0.2f },
        { "Meds", 1.5f },
        { "Food", 1.0f },
        { "Scrap", 0.8f },
        { "Fuel", 2.5f },
        { "Keys", 0.1f }
    };

    private PlayerHealth playerHealth;
    private Shooting shooting;
    private bool hudInitialized;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        shooting = GetComponent<Shooting>();
        medkits = Mathf.Max(1, medkits);
        RefreshHUD();
    }

    void Update()
    {
        if (!hudInitialized && UIManager.Instance != null)
        {
            RefreshHUD();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            UseMedkit();
        }
    }

    public bool TryAddResource(string resourceType, int amount, out string resultMessage, out bool rarePickup)
    {
        rarePickup = resourceType == "Fuel" || resourceType == "Keys";

        if (amount <= 0)
        {
            resultMessage = string.Empty;
            return false;
        }

        float additionalWeight = GetWeightFor(resourceType) * amount;
        if (GetCurrentWeight() + additionalWeight > maxCarryWeight)
        {
            resultMessage = $"Inventory full for {resourceType}";
            return false;
        }

        switch (resourceType)
        {
            case "Ammo":
                ammoReserve += amount;
                break;
            case "Meds":
                medkits += amount;
                break;
            case "Food":
                food += amount;
                break;
            case "Scrap":
                scrap += amount;
                break;
            case "Fuel":
                fuel += amount;
                break;
            case "Keys":
                keys += amount;
                break;
            default:
                resultMessage = $"Unknown resource: {resourceType}";
                return false;
        }

        resultMessage = $"+{amount} {resourceType}";
        RefreshHUD();
        return true;
    }

    public bool TryConsumeReserveAmmo(int amount)
    {
        if (ammoReserve < amount)
        {
            return false;
        }

        ammoReserve -= amount;
        RefreshHUD();
        return true;
    }

    public void UseMedkit()
    {
        if (medkits <= 0 || playerHealth == null)
        {
            UIManager.Instance?.ShowMessage("No medkits left");
            return;
        }

        if (playerHealth.GetCurrentHealth() >= playerHealth.GetMaxHealth())
        {
            UIManager.Instance?.ShowMessage("Already at full health");
            return;
        }

        medkits--;
        playerHealth.Heal(35);
        UIManager.Instance?.ShowMessage("Used medkit");
        RefreshHUD();
    }

    public bool HasResource(string resourceType, int amount)
    {
        return resourceType switch
        {
            "Ammo" => ammoReserve >= amount,
            "Meds" => medkits >= amount,
            "Food" => food >= amount,
            "Scrap" => scrap >= amount,
            "Fuel" => fuel >= amount,
            "Keys" => keys >= amount,
            _ => false,
        };
    }

    public void RemoveResource(string resourceType, int amount)
    {
        switch (resourceType)
        {
            case "Ammo":
                ammoReserve = Mathf.Max(0, ammoReserve - amount);
                break;
            case "Meds":
                medkits = Mathf.Max(0, medkits - amount);
                break;
            case "Food":
                food = Mathf.Max(0, food - amount);
                break;
            case "Scrap":
                scrap = Mathf.Max(0, scrap - amount);
                break;
            case "Fuel":
                fuel = Mathf.Max(0, fuel - amount);
                break;
            case "Keys":
                keys = Mathf.Max(0, keys - amount);
                break;
        }

        RefreshHUD();
    }

    public float GetCurrentWeight()
    {
        return ammoReserve * GetWeightFor("Ammo")
            + medkits * GetWeightFor("Meds")
            + food * GetWeightFor("Food")
            + scrap * GetWeightFor("Scrap")
            + fuel * GetWeightFor("Fuel")
            + keys * GetWeightFor("Keys");
    }

    public string GetSummary()
    {
        return $"Meds:{medkits} Food:{food} Ammo:{ammoReserve} Scrap:{scrap} Fuel:{fuel} Keys:{keys}  {GetCurrentWeight():0.0}/{maxCarryWeight:0}";
    }

    public int GetReserveAmmo() => ammoReserve;

    float GetWeightFor(string resourceType)
    {
        return resourceWeights.TryGetValue(resourceType, out float value) ? value : 1f;
    }

    void RefreshHUD()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.UpdateMedkits(medkits);
        UIManager.Instance.UpdateSupplies($"AMMO {ammoReserve}   FOOD {food}   {GetCurrentWeight():0.0}/{maxCarryWeight:0} KG");
        hudInitialized = true;
    }
}
