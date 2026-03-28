using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD - Health")]
    [FormerlySerializedAs("barraVida")]
    public Slider healthBar;

    [FormerlySerializedAs("textoVida")]
    public TextMeshProUGUI healthText;

    [Header("HUD - Ammo")]
    [FormerlySerializedAs("textoMunicao")]
    public TextMeshProUGUI ammoText;

    [Header("HUD - Game")]
    [FormerlySerializedAs("textoPontos")]
    public TextMeshProUGUI scoreText;

    [FormerlySerializedAs("textoWave")]
    public TextMeshProUGUI waveText;

    public TextMeshProUGUI suppliesText;
    public TextMeshProUGUI inventoryText;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI promptText;
    public Image damageIndicator;

    [FormerlySerializedAs("textoMensagem")]
    public TextMeshProUGUI messageText;

    [Header("Game Over Panel")]
    [FormerlySerializedAs("painelGameOver")]
    public GameObject gameOverPanel;

    [FormerlySerializedAs("textoPontosFinais")]
    public TextMeshProUGUI finalScoreText;

    [Header("References")]
    public Shooting shooting;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        SetGameOverVisible(false);
        UpdatePrompt(string.Empty);
        RefreshPlayerHUD();
    }

    void Update()
    {
        if (shooting == null)
        {
            RefreshPlayerHUD();
            return;
        }

        string ammoStatus = shooting.IsReloading()
            ? "RELOADING..."
            : $"{shooting.GetCurrentAmmo()} / {shooting.GetMaxAmmo()}";

        if (ammoText != null)
        {
            ammoText.text = ammoStatus;
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"HP: {currentHealth}/{maxHealth}";
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void UpdateWave(int wave)
    {
        if (waveText != null)
        {
            waveText.text = wave > 0 ? $"Pressure: {wave}" : "Pressure: Low";
        }
    }

    public void UpdateSupplies(string supplies)
    {
        if (suppliesText != null)
        {
            suppliesText.text = supplies;
        }
    }

    public void UpdateInventory(string inventory)
    {
        if (inventoryText != null)
        {
            inventoryText.text = inventory;
        }
    }

    public void UpdateObjective(string objective)
    {
        if (objectiveText != null)
        {
            objectiveText.text = objective;
        }
    }

    public void UpdatePrompt(string prompt)
    {
        if (promptText == null)
        {
            return;
        }

        promptText.text = prompt;
        promptText.gameObject.SetActive(!string.IsNullOrWhiteSpace(prompt));
    }

    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        messageText.text = message;
        messageText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 3f);
    }

    public void ShowDamageIndicator()
    {
        if (damageIndicator == null)
        {
            return;
        }

        damageIndicator.gameObject.SetActive(true);
        Color color = damageIndicator.color;
        color.a = 0.32f;
        damageIndicator.color = color;
        CancelInvoke(nameof(FadeDamageIndicator));
        Invoke(nameof(FadeDamageIndicator), 0.18f);
    }

    void HideMessage()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    void FadeDamageIndicator()
    {
        if (damageIndicator == null)
        {
            return;
        }

        Color color = damageIndicator.color;
        color.a = 0f;
        damageIndicator.color = color;
        damageIndicator.gameObject.SetActive(false);
    }

    public void ShowGameOver(int score)
    {
        SetGameOverVisible(true);

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {score}";
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetGameOverVisible(bool isVisible)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(isVisible);
        }
    }

    void RefreshPlayerHUD()
    {
        if (shooting == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null)
            {
                UpdateInventory(inventory.GetSummary());
            }
        }
    }
}
