using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    struct ObjectiveRecord
    {
        public string ZoneName;
        public string Description;
        public bool Completed;
    }

    public static GameManager Instance;

    [Header("Dynamic Waves")]
    [FormerlySerializedAs("prefabZombie")]
    public GameObject zombiePrefab;

    [FormerlySerializedAs("pontosSpawn")]
    public List<Transform> spawnPoints;

    [FormerlySerializedAs("zombiesPorWave")]
    public int zombiesPerWave = 5;

    [FormerlySerializedAs("tempoEntreWaves")]
    public float timeBetweenWaves = 10f;

    [FormerlySerializedAs("multiplicadorWave")]
    public float waveMultiplier = 1.5f;

    public bool autoSpawnWaves = false;

    [Header("Game State")]
    [FormerlySerializedAs("waveAtual")]
    public int currentWave = 0;

    [FormerlySerializedAs("pontos")]
    public int score = 0;

    private readonly Dictionary<string, int> zoneZombieTotals = new Dictionary<string, int>();
    private readonly Dictionary<string, int> zoneZombieRemaining = new Dictionary<string, int>();
    private readonly Dictionary<string, int> zoneSupplyTotals = new Dictionary<string, int>();
    private readonly Dictionary<string, int> zoneSupplyCollected = new Dictionary<string, int>();
    private readonly Dictionary<string, ObjectiveRecord> objectivesById = new Dictionary<string, ObjectiveRecord>();

    private int aliveZombies = 0;
    private int suppliesFound = 0;
    private int totalSupplyCaches = 0;
    private bool isGameActive = true;
    private string activeZone = "Approach";
    private bool extractionUnlocked;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            Instance = null;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        RefreshHUD();

        if (autoSpawnWaves)
        {
            StartNextWave();
        }
        else
        {
            UpdateObjectiveText();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void RegisterZombie(string zoneName)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
        {
            zoneName = "Outskirts";
        }

        if (!zoneZombieTotals.ContainsKey(zoneName))
        {
            zoneZombieTotals[zoneName] = 0;
            zoneZombieRemaining[zoneName] = 0;
        }

        zoneZombieTotals[zoneName]++;
        zoneZombieRemaining[zoneName]++;
        aliveZombies++;
        UpdateObjectiveText();
    }

    public void RegisterSupplyCache(string zoneName)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
        {
            zoneName = "Unknown";
        }

        if (!zoneSupplyTotals.ContainsKey(zoneName))
        {
            zoneSupplyTotals[zoneName] = 0;
            zoneSupplyCollected[zoneName] = 0;
        }

        zoneSupplyTotals[zoneName]++;
        totalSupplyCaches++;
        UpdateSuppliesText();
        UpdateObjectiveText();
    }

    public void RegisterObjective(string zoneName, string objectiveId, string description)
    {
        if (string.IsNullOrWhiteSpace(zoneName) || string.IsNullOrWhiteSpace(objectiveId))
        {
            return;
        }

        if (!objectivesById.ContainsKey(objectiveId))
        {
            objectivesById[objectiveId] = new ObjectiveRecord
            {
                ZoneName = zoneName,
                Description = description,
                Completed = false
            };
        }

        UpdateObjectiveText();
    }

    public void CompleteObjective(string zoneName, string objectiveId)
    {
        if (string.IsNullOrWhiteSpace(objectiveId))
        {
            return;
        }

        if (!objectivesById.TryGetValue(objectiveId, out ObjectiveRecord record))
        {
            return;
        }

        record.Completed = true;
        objectivesById[objectiveId] = record;
        score += 100;

        if (AllObjectivesCompleted())
        {
            extractionUnlocked = true;
            UIManager.Instance?.ShowMessage("Extraction unlocked");
        }

        RefreshHUD();
    }

    public void EnterZone(string zoneName)
    {
        if (!isGameActive || string.IsNullOrWhiteSpace(zoneName))
        {
            return;
        }

        if (activeZone == zoneName)
        {
            return;
        }

        activeZone = zoneName;
        UIManager.Instance?.ShowMessage($"Entered {zoneName}");
        UpdateObjectiveText();
    }

    public void CollectSupply(string zoneName, string supplyType, int amount)
    {
        if (!isGameActive)
        {
            return;
        }

        if (!zoneSupplyCollected.ContainsKey(zoneName))
        {
            zoneSupplyCollected[zoneName] = 0;
        }

        zoneSupplyCollected[zoneName]++;
        suppliesFound++;
        score += Mathf.Max(10, amount * 5);

        UIManager.Instance?.ShowMessage($"Looted {supplyType} in {zoneName}");
        RefreshHUD();
    }

    public void StartNextWave()
    {
        if (!isGameActive || zombiePrefab == null || spawnPoints == null || spawnPoints.Count == 0)
        {
            return;
        }

        currentWave++;
        int totalZombies = Mathf.RoundToInt(zombiesPerWave * Mathf.Pow(waveMultiplier, currentWave - 1));
        aliveZombies = totalZombies;

        UIManager.Instance?.UpdateWave(currentWave);
        StartCoroutine(SpawnZombies(totalZombies));
    }

    IEnumerator SpawnZombies(int amount)
    {
        yield return new WaitForSeconds(3f);

        for (int i = 0; i < amount; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            GameObject zombieInstance = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
            zombieInstance.SetActive(true);

            ZombieHealth zombieHealth = zombieInstance.GetComponent<ZombieHealth>();
            if (zombieHealth != null)
            {
                zombieHealth.zoneName = "Dynamic Infestation";
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    public void OnZombieKilled(string zoneName)
    {
        if (!isGameActive)
        {
            return;
        }

        aliveZombies = Mathf.Max(0, aliveZombies - 1);

        if (!string.IsNullOrWhiteSpace(zoneName) && zoneZombieRemaining.ContainsKey(zoneName))
        {
            zoneZombieRemaining[zoneName] = Mathf.Max(0, zoneZombieRemaining[zoneName] - 1);

            if (zoneZombieRemaining[zoneName] == 0)
            {
                UIManager.Instance?.ShowMessage($"{zoneName} is clear. Search for supplies.");
            }
        }

        UpdateObjectiveText();

        if (autoSpawnWaves && aliveZombies <= 0)
        {
            UIManager.Instance?.ShowMessage("Wave complete!");
            Invoke(nameof(StartNextWave), timeBetweenWaves);
        }
    }

    public void AddScore(int amount)
    {
        if (!isGameActive)
        {
            return;
        }

        score += amount;
        RefreshHUD();
    }

    public void GameOver()
    {
        if (!isGameActive)
        {
            return;
        }

        isGameActive = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameOverScreen.Instance != null)
            GameOverScreen.Instance.ShowGameOver(score);
        else
            UIManager.Instance?.ShowGameOver(score);
    }

    public bool CanExtract()
    {
        if (!isGameActive) return false;
        if (extractionUnlocked) return true;
        // fallback: sem objetivos registados, verifica se todos os supplies foram recolhidos
        if (objectivesById.Count == 0 && totalSupplyCaches > 0)
            return suppliesFound >= totalSupplyCaches;
        return false;
    }

    public void Extract()
    {
        if (!CanExtract())
        {
            return;
        }

        isGameActive = false;
        score += 250;
        UIManager.Instance?.ShowMessage("Extraction successful");
        TriggerVictory();
    }

    void RefreshHUD()
    {
        UIManager.Instance?.UpdateScore(score);
        UIManager.Instance?.UpdateWave(currentWave);
        UpdateSuppliesText();
        UpdateObjectiveText();
    }

    void UpdateSuppliesText()
    {
        UIManager.Instance?.UpdateSupplies($"Supplies: {suppliesFound}/{Mathf.Max(totalSupplyCaches, 1)}");
    }

    void UpdateObjectiveText()
    {
        string zoneText = $"Zone: {activeZone}";

        if (autoSpawnWaves)
        {
            UIManager.Instance?.UpdateObjective($"{zoneText}\nHold out and survive the next wave.");
            return;
        }

        int zoneZombies = zoneZombieRemaining.ContainsKey(activeZone) ? zoneZombieRemaining[activeZone] : 0;
        int zoneLoot = zoneSupplyTotals.ContainsKey(activeZone) ? zoneSupplyTotals[activeZone] : 0;
        int zoneLooted = zoneSupplyCollected.ContainsKey(activeZone) ? zoneSupplyCollected[activeZone] : 0;
        string zoneObjective = GetObjectiveTextForZone(activeZone);
        string objective = zoneZombies > 0
            ? $"Clear {activeZone}: {zoneZombies} infected remaining"
            : $"Search {activeZone}: {zoneLooted}/{zoneLoot} caches looted";

        if (!string.IsNullOrWhiteSpace(zoneObjective))
        {
            objective = $"{zoneObjective}\n{objective}";
        }

        if (extractionUnlocked)
        {
            objective += "\nReturn to extraction point";
        }

        UIManager.Instance?.UpdateObjective($"{zoneText}\n{objective}");
    }

    bool AllObjectivesCompleted()
    {
        if (objectivesById.Count == 0)
        {
            return false;
        }

        foreach (ObjectiveRecord record in objectivesById.Values)
        {
            if (!record.Completed)
            {
                return false;
            }
        }

        return true;
    }

    string GetObjectiveTextForZone(string zoneName)
    {
        foreach (ObjectiveRecord record in objectivesById.Values)
        {
            if (record.ZoneName == zoneName)
            {
                return record.Completed
                    ? $"{record.Description} [Done]"
                    : record.Description;
            }
        }

        return string.Empty;
    }

    // --- Métodos públicos usados por UI e ecrã final ---

    public int GetScore() => score;

    public int GetTotalSupplies() => totalSupplyCaches;

    public int GetCollectedSupplies() => suppliesFound;

    public void TriggerVictory()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowMessage("EXTRACTION SUCCESSFUL! You win!");

        if (GameOverScreen.Instance != null)
            GameOverScreen.Instance.ShowVictory(score);
    }
}
