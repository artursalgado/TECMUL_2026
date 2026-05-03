using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    [Header("Estatísticas do Zombie")]
    public float vidaMaxima = 50f;
    public float dano = 10f;
    public float distanciaDeAtaque = 2f;
    public float tempoEntreAtaques = 1.5f;

    [Header("Efeitos")]
    public GameObject bloodPrefab;

    private float vidaAtual;
    private NavMeshAgent agente;
    private Transform alvo;
    private PlayerHealth playerVida;
    private float temporizadorAtaque;
    private Animator animador;

    [HideInInspector] public WaveManager waveManager;
    private AudioSource audioSource;

    void Start()
    {
        vidaAtual = vidaMaxima;
        agente = GetComponent<NavMeshAgent>();
        animador = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.spatialBlend = 1f;       // som 3D
            audioSource.minDistance = 3f;         // volume máximo a 3m
            audioSource.maxDistance = 25f;        // ouve-se até 25m
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.loop = true;
            audioSource.volume = 0.6f;
            audioSource.pitch = Random.Range(0.85f, 1.15f); // variação entre zombies
            audioSource.Play();
        }

        agente.stoppingDistance = distanciaDeAtaque - 0.2f;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            alvo = playerObj.transform;
            playerVida = playerObj.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogError("ZombieAI: Jogador não encontrado! Certifique-se que o Player tem a Tag 'Player'.");
        }
    }

    void Update()
    {
        if (alvo == null || playerVida == null) return;

        float distancia = Vector3.Distance(transform.position, alvo.position);

        if (distancia <= distanciaDeAtaque)
        {
            // Parado — a atacar
            agente.isStopped = true;

            // Animação: parar de andar, ativar ataque
            if (animador != null)
            {
                animador.SetFloat("Speed", 0f);
                animador.SetBool("isAttacking", true);
            }

            temporizadorAtaque += Time.deltaTime;
            if (temporizadorAtaque >= tempoEntreAtaques)
            {
                Atacar();
                temporizadorAtaque = 0f;
            }
        }
        else
        {
            // A andar em direção ao jogador
            agente.isStopped = false;
            agente.SetDestination(alvo.position);

            // Animação: andar, cancelar ataque
            if (animador != null)
            {
                animador.SetFloat("Speed", agente.velocity.magnitude);
                animador.SetBool("isAttacking", false);
            }

            temporizadorAtaque = 0f;
        }
    }

    void Atacar()
    {
        playerVida.TakeDamage(dano);
        if (audioSource != null) audioSource.volume = 1f; // mais alto ao atacar
    }

    public void LevarDano(float danoRecebido, Vector3 posicaoImpacto = default)
    {
        vidaAtual -= danoRecebido;

        if (bloodPrefab != null)
        {
            Vector3 pos = posicaoImpacto == default ? transform.position + Vector3.up : posicaoImpacto;
            Instantiate(bloodPrefab, pos, Quaternion.identity);
        }

        if (vidaAtual <= 0) Morrer();
    }

    void Morrer()
    {
        // Avisa o WaveManager que este zombie morreu
        if (waveManager != null)
            waveManager.ZombieMorreu();

        Destroy(gameObject);
    }
}

