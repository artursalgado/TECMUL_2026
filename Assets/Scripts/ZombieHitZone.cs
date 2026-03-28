using UnityEngine;

public class ZombieHitZone : MonoBehaviour
{
    public ZombieHealth zombieHealth;
    public float damageMultiplier = 1f;
    public bool instantKill;

    public void ApplyHit(int baseDamage)
    {
        if (zombieHealth == null)
        {
            zombieHealth = GetComponentInParent<ZombieHealth>();
        }

        if (zombieHealth == null)
        {
            return;
        }

        if (instantKill)
        {
            zombieHealth.ApplyHit(zombieHealth.GetCurrentHealth(), true);
            return;
        }

        int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageMultiplier));
        zombieHealth.ApplyHit(damage, false);
    }
}
