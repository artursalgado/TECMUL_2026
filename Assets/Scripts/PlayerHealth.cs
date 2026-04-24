using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"[Player] Sofreu {damage} de dano! Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("[Player] O jogador Morreu! A recarregar o mapa...");
        // Recarrega a cena atual para recomeçar o jogo
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
