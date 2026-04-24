using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("Regeneration")]
    public bool  canRegenerate    = false;
    public float regenerationRate = 5f;
    public float regenerationDelay = 5f;

    [Header("Audio")]
    public AudioClip damageClip;
    public AudioClip deathClip;

    private int         _currentHealth;
    private bool        _isDead;
    private float       _lastDamageTime;
    private AudioSource _audio;

    void Start()
    {
        canRegenerate = true;
        regenerationRate = 2f; // Heals 2 HP per second after delay
        regenerationDelay = 8f; // Delays regeneration after taking damage

        // Apply per-difficulty health multiplier
        maxHealth = Mathf.RoundToInt(maxHealth * GameConfig.PlayerHealthMultiplier);
        _currentHealth = maxHealth;
        _audio = GetComponent<AudioSource>();
        UIManager.Instance?.UpdateHealth(_currentHealth, maxHealth);
    }

    void Update()
    {
        if (!canRegenerate || _currentHealth >= maxHealth || _isDead) return;
        if (Time.time - _lastDamageTime < regenerationDelay) return;
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + Mathf.RoundToInt(regenerationRate * Time.deltaTime));
        UIManager.Instance?.UpdateHealth(_currentHealth, maxHealth);
        AudioManager.Instance?.SetPlayerHealth(_currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;
        _currentHealth   = Mathf.Clamp(_currentHealth - damage, 0, maxHealth);
        _lastDamageTime  = Time.time;

        if (_audio != null && damageClip != null) _audio.PlayOneShot(damageClip);

        UIManager.Instance?.UpdateHealth(_currentHealth, maxHealth);
        UIManager.Instance?.ShowDamageIndicator();
        AudioManager.Instance?.SetPlayerHealth(_currentHealth, maxHealth);

        if (_currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0, maxHealth);
        UIManager.Instance?.UpdateHealth(_currentHealth, maxHealth);
        AudioManager.Instance?.SetPlayerHealth(_currentHealth, maxHealth);
    }

    void Die()
    {
        _isDead = true;
        if (_audio != null && deathClip != null) _audio.PlayOneShot(deathClip);
        AudioManager.Instance?.PlayGameOver();
        GameManager.Instance?.GameOver();
    }

    public int  GetCurrentHealth() => _currentHealth;
    public int  GetMaxHealth()     => maxHealth;
    public bool IsDead()           => _isDead;
}
