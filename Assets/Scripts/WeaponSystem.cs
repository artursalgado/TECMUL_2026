using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [Header("Armas")]
    public Axe machado;
    public Pistol pistola;

    private int armaAtual = 1; // 1 = Machado, 2 = Pistola
    private HUDManager hud;

    void Start()
    {
        hud = FindFirstObjectByType<HUDManager>();
        EquiparArma(1); // Começa com o machado
    }

    void Update()
    {
        // Trocar arma com teclas 1 e 2
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquiparArma(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquiparArma(2);

        // Atacar com clique esquerdo
        if (Input.GetMouseButtonDown(0))
        {
            if (armaAtual == 1 && machado != null) machado.Atacar();
            if (armaAtual == 2 && pistola != null) pistola.Disparar();
        }
    }

    void EquiparArma(int numero)
    {
        armaAtual = numero;

        // Ativa/desativa os modelos de arma (se existirem)
        if (machado != null) machado.gameObject.SetActive(numero == 1);
        if (pistola != null) pistola.gameObject.SetActive(numero == 2);

        // Atualiza o HUD
        if (hud != null)
        {
            string nome = numero == 1 ? "🪓 MACHADO" : "🔫 PISTOLA";
            hud.AtualizarArma(nome, numero == 2 ? pistola?.municaoAtual ?? 0 : -1,
                              numero == 2 ? pistola?.municaoMax ?? 0 : -1);
        }
    }

    // Chamado pela Pistola para atualizar a munição no HUD
    public void AtualizarMunicaoHUD(int atual, int max)
    {
        if (hud != null) hud.AtualizarArma("🔫 PISTOLA", atual, max);
    }
}
