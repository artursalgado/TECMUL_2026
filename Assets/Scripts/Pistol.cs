using UnityEngine;

public class Pistol : MonoBehaviour
{
    [Header("Configuração da Pistola")]
    public float dano = 25f;
    public float alcance = 100f;
    public float cooldown = 0.4f;
    public int municaoMax = 12;

    [HideInInspector] public int municaoAtual;

    private float tempoCooldown = 0f;
    private Camera cam;
    private WeaponSystem weaponSystem;

    void Start()
    {
        municaoAtual = municaoMax;
        cam = GetComponentInParent<Camera>();
        if (cam == null) cam = Camera.main;
        weaponSystem = GetComponentInParent<WeaponSystem>();
    }

    void Update()
    {
        if (tempoCooldown > 0)
            tempoCooldown -= Time.deltaTime;

        // Recarregar com tecla R
        if (Input.GetKeyDown(KeyCode.R))
            Recarregar();
    }

    public void Disparar()
    {
        if (tempoCooldown > 0) return;
        if (municaoAtual <= 0)
        {
            Debug.Log("[Pistola] Sem munição! Prima R para recarregar.");
            return;
        }

        tempoCooldown = cooldown;
        municaoAtual--;

        // Atualiza HUD
        if (weaponSystem != null)
            weaponSystem.AtualizarMunicaoHUD(municaoAtual, municaoMax);

        // Raycast do centro do ecrã
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, alcance))
        {
            ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null)
            {
                zombie.LevarDano(dano);
                Debug.Log($"[Pistola] Acertou no zombie! Dano: {dano}");
            }
        }

        Debug.Log($"[Pistola] BANG! Munição: {municaoAtual}/{municaoMax}");
    }

    void Recarregar()
    {
        municaoAtual = municaoMax;
        if (weaponSystem != null)
            weaponSystem.AtualizarMunicaoHUD(municaoAtual, municaoMax);
        Debug.Log("[Pistola] Recarregado!");
    }
}
