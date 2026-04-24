using UnityEngine;

public class Axe : MonoBehaviour
{
    [Header("Configuração do Machado")]
    public float dano = 40f;
    public float alcance = 2.5f;
    public float cooldown = 0.8f; // segundos entre ataques

    private float tempoCooldown = 0f;
    private Camera cam;

    void Start()
    {
        cam = GetComponentInParent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (tempoCooldown > 0)
            tempoCooldown -= Time.deltaTime;
    }

    public void Atacar()
    {
        if (tempoCooldown > 0) return; // Ainda em cooldown

        tempoCooldown = cooldown;

        // Raycast a partir do centro do ecrã para ver se há zombie à frente
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, alcance))
        {
            ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null)
            {
                zombie.LevarDano(dano);
                Debug.Log($"[Machado] Acertou no zombie! Dano: {dano}");
            }
        }

        // Mesmo sem acertar, a animação (se houver) dispararia aqui
        Debug.Log("[Machado] Swing!");
    }
}
