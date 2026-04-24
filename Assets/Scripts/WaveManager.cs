using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Prefab do Zombie")]
    public GameObject zombiePrefab;

    [Header("Configuração das Hordas")]
    public int totalHordas = 5;
    public float intervaloBetweenHordas = 60f; // 1 minuto entre hordas

    [Header("Zombies por Horda (começa na Horda 1)")]
    public int[] zombiesPorHorda = { 5, 10, 15, 20, 30 };

    [Header("Raio de Spawn")]
    public float raioMinimo = 20f;
    public float raioMaximo = 40f;

    // Estado atual
    [HideInInspector] public int hordaAtual = 0;
    [HideInInspector] public int zombiesRestantes = 0;
    [HideInInspector] public bool jogoTerminado = false;
    [HideInInspector] public float tempoParaProximaHorda = 0f;
    [HideInInspector] public bool aEsperarProximaHorda = false;

    private Transform jogador;
    private List<GameObject> zombiesAtivos = new List<GameObject>();
    private HUDManager hud;

    void Start()
    {
        hud = FindFirstObjectByType<HUDManager>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            jogador = playerObj.transform;
        else
            Debug.LogError("WaveManager: Jogador não encontrado! Coloque a Tag 'Player' no Player.");

        // Começa a primeira horda após 3 segundos
        StartCoroutine(IniciarHordaComDelay(3f));
    }

    IEnumerator IniciarHordaComDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        IniciarProximaHorda();
    }

    void IniciarProximaHorda()
    {
        if (hordaAtual >= totalHordas)
        {
            // Jogo ganho!
            jogoTerminado = true;
            if (hud != null) hud.MostrarVitoria();
            return;
        }

        hordaAtual++;
        aEsperarProximaHorda = false;
        int quantidadeDeZombies = zombiesPorHorda[hordaAtual - 1];
        zombiesRestantes = quantidadeDeZombies;

        if (hud != null) hud.AtualizarHorda(hordaAtual, totalHordas, zombiesRestantes);

        StartCoroutine(FazerSpawnDaHorda(quantidadeDeZombies));
        Debug.Log($"[WaveManager] Horda {hordaAtual} iniciada com {quantidadeDeZombies} zombies!");
    }

    IEnumerator FazerSpawnDaHorda(int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            SpawnZombie();
            yield return new WaitForSeconds(0.5f); // Meio segundo entre cada zombie
        }
    }

    void SpawnZombie()
    {
        if (jogador == null || zombiePrefab == null) return;

        // Gera posição aleatória à volta do jogador
        for (int tentativas = 0; tentativas < 10; tentativas++)
        {
            Vector2 circulo = Random.insideUnitCircle.normalized;
            float distancia = Random.Range(raioMinimo, raioMaximo);
            Vector3 posAleatoria = jogador.position + new Vector3(circulo.x, 0, circulo.y) * distancia;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(posAleatoria, out hit, 5f, NavMesh.AllAreas))
            {
                GameObject novoZombie = Instantiate(zombiePrefab, hit.position, Quaternion.identity);
                zombiesAtivos.Add(novoZombie);

                // Diz ao zombie para avisar quando morrer
                ZombieAI ai = novoZombie.GetComponent<ZombieAI>();
                if (ai != null) ai.waveManager = this;

                return;
            }
        }
    }

    // Chamado pelo ZombieAI quando ele morre
    public void ZombieMorreu()
    {
        zombiesRestantes--;
        zombiesAtivos.RemoveAll(z => z == null);

        if (hud != null) hud.AtualizarZombiesRestantes(zombiesRestantes);

        if (zombiesRestantes <= 0 && !jogoTerminado)
        {
            // Todos os zombies morreram!
            if (hordaAtual >= totalHordas)
            {
                jogoTerminado = true;
                if (hud != null) hud.MostrarVitoria();
            }
            else
            {
                // Prepara próxima horda
                StartCoroutine(ContagemdRegressiva());
            }
        }
    }

    IEnumerator ContagemdRegressiva()
    {
        aEsperarProximaHorda = true;
        tempoParaProximaHorda = intervaloBetweenHordas;

        while (tempoParaProximaHorda > 0)
        {
            if (hud != null) hud.MostrarContagemRegressiva((int)tempoParaProximaHorda);
            yield return new WaitForSeconds(1f);
            tempoParaProximaHorda -= 1f;
        }

        IniciarProximaHorda();
    }
}
