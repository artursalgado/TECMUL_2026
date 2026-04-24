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
    public float duracaoDeHorda = 60f; // 1 minuto por horda

    [Header("Zombies por Horda")]
    public int[] zombiesPorHorda = { 5, 10, 15, 20, 30 };

    [Header("Raio de Spawn")]
    public float raioMinimo = 20f;
    public float raioMaximo = 40f;

    [HideInInspector] public int hordaAtual = 0;
    [HideInInspector] public int zombiesRestantes = 0;
    [HideInInspector] public bool jogoTerminado = false;

    private Transform jogador;
    private HUDManager hud;
    private float timerHorda;
    private bool hordaAtiva = false;

    void Start()
    {
        hud = FindFirstObjectByType<HUDManager>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            jogador = playerObj.transform;
        else
            Debug.LogError("WaveManager: Player não encontrado! Coloque a Tag 'Player' no Player.");

        // Começa a primeira horda após 3 segundos
        Invoke(nameof(IniciarProximaHorda), 3f);
    }

    void Update()
    {
        if (!hordaAtiva || jogoTerminado) return;

        timerHorda -= Time.deltaTime;

        // Atualiza a contagem regressiva no HUD
        if (hud != null)
            hud.MostrarTempoHorda((int)timerHorda);

        // Quando o tempo acabar, avança para a próxima horda
        if (timerHorda <= 0f)
        {
            hordaAtiva = false;

            if (hordaAtual >= totalHordas)
            {
                jogoTerminado = true;
                if (hud != null) hud.MostrarVitoria();
            }
            else
            {
                // Próxima horda começa logo após 3 segundos
                Invoke(nameof(IniciarProximaHorda), 3f);
            }
        }
    }

    void IniciarProximaHorda()
    {
        hordaAtual++;
        int quantidade = zombiesPorHorda[hordaAtual - 1];
        zombiesRestantes = quantidade;
        timerHorda = duracaoDeHorda;
        hordaAtiva = true;

        if (hud != null) hud.AtualizarHorda(hordaAtual, totalHordas, zombiesRestantes);

        StartCoroutine(FazerSpawnDaHorda(quantidade));
        Debug.Log($"[WaveManager] Horda {hordaAtual} iniciada com {quantidade} zombies!");
    }

    IEnumerator FazerSpawnDaHorda(int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            SpawnZombie();
            yield return new WaitForSeconds(0.5f);
        }
    }

    void SpawnZombie()
    {
        if (jogador == null || zombiePrefab == null) return;

        for (int tentativas = 0; tentativas < 10; tentativas++)
        {
            Vector2 circulo = Random.insideUnitCircle.normalized;
            float distancia = Random.Range(raioMinimo, raioMaximo);
            Vector3 posAleatoria = jogador.position + new Vector3(circulo.x, 0, circulo.y) * distancia;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(posAleatoria, out hit, 5f, NavMesh.AllAreas))
            {
                GameObject novoZombie = Instantiate(zombiePrefab, hit.position, Quaternion.identity);

                ZombieAI ai = novoZombie.GetComponent<ZombieAI>();
                if (ai != null) ai.waveManager = this;

                return;
            }
        }
    }

    // Chamado pelo ZombieAI quando morre — para atualizar o contador no HUD
    public void ZombieMorreu()
    {
        zombiesRestantes = Mathf.Max(0, zombiesRestantes - 1);
        if (hud != null) hud.AtualizarZombiesRestantes(zombiesRestantes);
    }
}

