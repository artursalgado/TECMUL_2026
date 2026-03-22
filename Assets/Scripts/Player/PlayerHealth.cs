using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            GameManager.Instance.GameOver();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
    }
}
