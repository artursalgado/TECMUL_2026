using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    private HUDManager hud;

    void Start()
    {
        currentHealth = maxHealth;
        hud = FindFirstObjectByType<HUDManager>();
        if (hud != null) hud.AtualizarVida(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (hud != null) hud.AtualizarVida(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (hud != null)
            hud.MostrarMorte();
        else
        {
            // Fallback se não houver HUD
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
