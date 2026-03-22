using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Movimento")]
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackRate = 1f;

    private Transform player;
    private PlayerHealth playerHealth;
    private float nextAttackTime = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Persegue o jogador
        if (distToPlayer > attackRange)
        {
            transform.LookAt(player);
            transform.position = Vector3.MoveTowards(
                transform.position, player.position, moveSpeed * Time.deltaTime);
        }
        // Ataca se estiver perto
        else if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackRate;
            if (playerHealth != null)
                playerHealth.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        GameManager.Instance.AddScore(10);
        Destroy(gameObject);
    }
}
