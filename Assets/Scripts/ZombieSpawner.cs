using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Configurações do Spawner")]
    public GameObject zombiePrefab; // O ZOMBIEBRABO vai aqui!
    public int maxZombiesAtivos = 10;
    public float tempoEntreSpawns = 5f;
    
    [Header("Raio de Spawn (Distância)")]
    public float raioMinimo = 15f; // Não nascer em cima do jogador
    public float raioMaximo = 30f;

    private Transform jogador;
    private List<GameObject> zombiesAtivos = new List<GameObject>();

    void Start()
    {
        // Procura o jogador automaticamente
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jogador = playerObj.transform;
            StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.LogError("ZombieSpawner: Jogador não encontrado! O Spawner vai parar.");
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            LimparZombiesMortos();

            // Só faz spawn se não tiver atingido o limite
            if (zombiesAtivos.Count < maxZombiesAtivos && zombiePrefab != null)
            {
                FazerSpawnDeZombie();
            }

            yield return new WaitForSeconds(tempoEntreSpawns);
        }
    }

    void FazerSpawnDeZombie()
    {
        // Tenta encontrar uma posição válida no chão
        Vector3 posAleatoria = PegarPosicaoAleatoria();
        
        NavMeshHit hit;
        // Verifica se essa posição toca no NavMesh (chão azul) num raio de 5 metros
        if (NavMesh.SamplePosition(posAleatoria, out hit, 5f, NavMesh.AllAreas))
        {
            GameObject novoZombie = Instantiate(zombiePrefab, hit.position, Quaternion.identity);
            zombiesAtivos.Add(novoZombie);
            // Debug.Log("ZombieSpawner: Novo zombie apareceu!");
        }
    }

    Vector3 PegarPosicaoAleatoria()
    {
        // Gera uma posição num círculo à volta do jogador
        Vector2 circulo = Random.insideUnitCircle.normalized;
        float distancia = Random.Range(raioMinimo, raioMaximo);
        
        Vector3 offset = new Vector3(circulo.x, 0, circulo.y) * distancia;
        return jogador.position + offset;
    }

    void LimparZombiesMortos()
    {
        // Remove da lista os zombies que entretanto morreram (ficaram null)
        zombiesAtivos.RemoveAll(z => z == null);
    }
}
