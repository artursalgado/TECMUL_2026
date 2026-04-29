using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Portão")]
    public GameObject portao;
    public float alturaAbrir = 8f;      // quanto sobe ao abrir
    public float velocidadePortao = 3f; // velocidade da animação

    [Header("HUD")]
    public HUDManager hud;

    private int geradoresAtivados = 0;
    private const int totalGeradores = 3;
    [HideInInspector] public bool portaoAberto = false;

    void Start()
    {
        hud = FindFirstObjectByType<HUDManager>();
        if (hud != null)
            hud.AtualizarGeradores(0, totalGeradores);
    }

    public void GeneratorActivated(int id)
    {
        geradoresAtivados++;
        Debug.Log($"[GameManager] Geradores ativos: {geradoresAtivados}/{totalGeradores}");

        if (hud != null)
            hud.AtualizarGeradores(geradoresAtivados, totalGeradores);

        if (geradoresAtivados >= totalGeradores)
            AbrirPortao();
        else if (hud != null)
            hud.MostrarMensagem($"Gerador {id} ativado! Faltam {totalGeradores - geradoresAtivados}.");
    }

    void AbrirPortao()
    {
        if (portaoAberto) return;
        portaoAberto = true;

        Debug.Log("[GameManager] Todos os geradores ativos! Portão a abrir!");

        if (hud != null)
            hud.MostrarMensagem("PORTAO ABERTO! ESCAPA!");

    }
}
