using UnityEngine;
using UnityEngine.Serialization;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [FormerlySerializedAs("vidaMaxima")]
    public int maxHealth = 100;

    [Header("Regeneration")]
    [FormerlySerializedAs("regenerar")]
    public bool canRegenerate = false;

    [FormerlySerializedAs("taxaRegeneracao")]
    public float regenerationRate = 5f;

    [FormerlySerializedAs("tempoEsperaRegen")]
    public float regenerationDelay = 5f;

    [Header("Audio")]
    [FormerlySerializedAs("somDano")]
    public AudioClip damageClip;

    [FormerlySerializedAs("somMorte")]
    public AudioClip deathClip;

    private int currentHealth;
    private bool isDead = false;
    private float lastDamageTime = 0f;
    private AudioSource audioSource;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        UIManager.Instance?.UpdateHealth(currentHealth, maxHealth);
    }

    void Update()
    {
        if (!canRegenerate || currentHealth >= maxHealth || isDead)
        {
            return;
        }

        if (Time.time - lastDamageTime < regenerationDelay)
        {
            return;
        }

        currentHealth += Mathf.RoundToInt(regenerationRate * Time.deltaTime);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UIManager.Instance?.UpdateHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        lastDamageTime = Time.time;

        if (audioSource != null && damageClip != null)
        {
            audioSource.PlayOneShot(damageClip);
        }

        UIManager.Instance?.UpdateHealth(currentHealth, maxHealth);
        UIManager.Instance?.ShowDamageIndicator();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UIManager.Instance?.UpdateHealth(currentHealth, maxHealth);
    }

    void Die()
    {
        isDead = true;

        if (audioSource != null && deathClip != null)
        {
            audioSource.PlayOneShot(deathClip);
        }

        GameManager.Instance?.GameOver();
    }

    public int GetCurrentHealth() => currentHealth;

    public int GetMaxHealth() => maxHealth;

    public bool IsDead() => isDead;
}
