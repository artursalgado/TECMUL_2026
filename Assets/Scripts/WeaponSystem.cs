using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [Header("Armas")]
    public AssaultRifle assaultRifle;
    public Pistol pistola;

    private int armaAtual = 1; // 1 = Assault Rifle, 2 = Pistola
    private HUDManager hud;

    void Start()
    {
        hud = FindFirstObjectByType<HUDManager>();
        EquiparArma(1);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquiparArma(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquiparArma(2);

        // Assault rifle: disparo automático (manter clicado)
        if (armaAtual == 1 && assaultRifle != null && Input.GetMouseButton(0))
            assaultRifle.Disparar();

        // Pistola: disparo simples (clique)
        if (armaAtual == 2 && pistola != null && Input.GetMouseButtonDown(0))
            pistola.Disparar();
    }

    void EquiparArma(int numero)
    {
        armaAtual = numero;

        if (assaultRifle != null) assaultRifle.gameObject.SetActive(numero == 1);
        if (pistola != null)      pistola.gameObject.SetActive(numero == 2);

        if (hud != null)
        {
            if (numero == 1)
                hud.AtualizarArma("ASSAULT RIFLE",
                    assaultRifle != null ? assaultRifle.municaoAtual : 0,
                    assaultRifle != null ? assaultRifle.municaoMax   : 0);
            else
                hud.AtualizarArma("PISTOLA",
                    pistola != null ? pistola.municaoAtual : 0,
                    pistola != null ? pistola.municaoMax   : 0);
        }
    }

    public void AtualizarMunicaoHUD(int atual, int max)
    {
        if (hud == null) return;
        string nome = armaAtual == 1 ? "ASSAULT RIFLE" : "PISTOLA";
        hud.AtualizarArma(nome, atual, max);
    }
}
