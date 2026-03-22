using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Disparo")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.2f;

    [Header("Efeitos")]
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;

    [Header("Camera")]
    public Camera fpsCam;

    private float nextTimeToFire = 0f;

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();

        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            // Aplica dano se acertar num inimigo
            EnemyController enemy = hit.transform.GetComponent<EnemyController>();
            if (enemy != null)
                enemy.TakeDamage(damage);

            // Efeito de impacto
            if (impactEffect != null)
                Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }
}
