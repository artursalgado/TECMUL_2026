using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    struct ObjectiveRecord
    {
        public string ZoneName;
        public string Description;
        public bool Completed;
    }

    public enum GameState
    {
        Initializing,
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Initializing;

    [Header("Dynamic Waves")]
    public GameObject zombiePrefab;
    public List<Transform> spawnPoints;
    public int zombiesPerWave = 6;          // Tier 1: começa com 6
    public float timeBetweenWaves = 12f;
    public float waveMultiplier = 1.4f;
    public bool autoSpawnWaves = true;
    public int maxActiveWaveZombies = 40;
    public float dynamicSpawnMinDistance = 22f;
    public float dynamicSpawnMaxDistance = 48f;
    public int wavesPerTier = 3;            // waves 1-3=T1, 4-6=T2, 7+=T3

    [Header("Game State")]
    public int currentWave = 0;
    public int score = 0;

    const string WaveZoneName = "Horde";
    const string PrefBestWave = "Run_BestWave";
    const string PrefBestScore = "Run_BestScore";
    const string PrefTotalRuns = "Run_TotalRuns";
    const string PrefTotalKills = "Run_TotalKills";

    private int suppliesFound = 0;
    private int totalSupplyCaches = 0;
    private string activeZone = "Approach";
    private bool extractionUnlocked;
    private Coroutine waveRoutine;
    private int waveAlive;
    private int waveSpawnTarget;

    private readonly Dictionary<string, int> zoneZombieRemaining = new Dictionary<string, int>();
    private readonly Dictionary<string, int> zoneSupplyTotals = new Dictionary<string, int>();
    private readonly Dictionary<string, int> zoneSupplyCollected = new Dictionary<string, int>();
    private readonly Dictionary<string, ObjectiveRecord> objectivesById = new Dictionary<string, ObjectiveRecord>();

    public int BestWave => PlayerPrefs.GetInt(PrefBestWave, 0);
    public int BestScore => PlayerPrefs.GetInt(PrefBestScore, 0);
    public int TotalRuns => PlayerPrefs.GetInt(PrefTotalRuns, 0);
    public int TotalKills => PlayerPrefs.GetInt(PrefTotalKills, 0);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (GameConfig.IsMenuScene(sceneName))
            ChangeState(GameState.MainMenu);
        else
            PrepareGameplaySession();
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");

        if (UIController.Instance != null)
            UIController.Instance.SetGameStateUI(newState);

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                string activeScene = SceneManager.GetActiveScene().name;
                if (GameConfig.IsMenuScene(activeScene))
                {
                    SceneManager.LoadScene(GameConfig.GameplaySceneName);
                }
                else
                {
                    StartWaveLoopIfNeeded();
                }
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameState.GameOver:
                StopWaveLoop();
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }

    // Methods needed by other scripts
    public void RegisterSupplyCache(string zone)
    {
        totalSupplyCaches++;
        zone = NormalizeZoneName(zone, "Outskirts");
        if (!zoneSupplyTotals.ContainsKey(zone)) zoneSupplyTotals[zone] = 0;
        zoneSupplyTotals[zone]++;
        UpdateSuppliesText();
    }

    public void CollectSupply(string zone)
    {
        suppliesFound++;
        zone = NormalizeZoneName(zone, "Outskirts");
        if (!zoneSupplyCollected.ContainsKey(zone)) zoneSupplyCollected[zone] = 0;
        zoneSupplyCollected[zone]++;

        AddScore(50);
        UpdateSuppliesText();
        CheckExtractionUnlock();
    }

    public void EnterZone(string zoneName)
    {
        activeZone = zoneName;
        UpdateObjectiveText();
    }

    public void RegisterObjective(string id, string zone, string desc)
    {
        objectivesById[id] = new ObjectiveRecord { ZoneName = zone, Description = desc, Completed = false };
        UpdateObjectiveText();
    }

    public void CompleteObjective(string id)
    {
        if (objectivesById.ContainsKey(id))
        {
            var record = objectivesById[id];
            if (record.Completed) return; // Already completed

            record.Completed = true;
            objectivesById[id] = record;
            AddScore(100);
            UpdateObjectiveText();
            CheckExtractionUnlock();
        }
    }

    public void RegisterZombie(string zone)
    {
        zone = NormalizeZoneName(zone, "Outskirts");
        if (!zoneZombieRemaining.ContainsKey(zone)) zoneZombieRemaining[zone] = 0;
        zoneZombieRemaining[zone]++;
        UpdateObjectiveText();
    }

    public void OnZombieKilled(string zone)
    {
        zone = NormalizeZoneName(zone, "Outskirts");
        if (zoneZombieRemaining.ContainsKey(zone) && zoneZombieRemaining[zone] > 0)
            zoneZombieRemaining[zone]--;

        if (zone == WaveZoneName)
        {
            waveAlive = Mathf.Max(0, waveAlive - 1);
            UIManager.Instance?.UpdateWave(currentWave, GetCurrentWaveTier(), waveAlive, waveSpawnTarget);
        }

        AddScore(10);
        PlayerPrefs.SetInt(PrefTotalKills, TotalKills + 1);
        UpdateObjectiveText();
        CheckExtractionUnlock();
    }

    public void GameOver(string reason = "You died.")
    {
        SaveRunResult(false);
        ChangeState(GameState.GameOver);
        AudioManager.Instance?.PlayGameOver();
        if (GameOverScreen.Instance != null)
        {
            GameOverScreen.Instance.ShowGameOver(score, reason);
        }
        else
        {
            Debug.LogWarning("[GameManager] GameOverScreen.Instance is null. Cannot show screen.");
        }
    }

    public void AddScore(int amount) { score += amount; UIManager.Instance?.UpdateScore(score); }

    public bool CanExtract() => extractionUnlocked || (objectivesById.Count == 0 && totalSupplyCaches > 0 && suppliesFound >= totalSupplyCaches);

    public void Extract() { if (CanExtract()) TriggerVictory(); }

    public void TriggerVictory()
    {
        score += 250;
        SaveRunResult(true);
        ChangeState(GameState.GameOver);
        AudioManager.Instance?.PlayVictory();
        GameOverScreen.Instance?.ShowVictory(score);
    }

    private void CheckExtractionUnlock()
    {
        if (AllObjectivesCompleted() || (objectivesById.Count == 0 && suppliesFound >= totalSupplyCaches))
        {
            extractionUnlocked = true;
            UIManager.Instance?.ShowMessage("EXTRACTION READY");
            UpdateObjectiveText();
        }
    }

    private bool AllObjectivesCompleted()
    {
        if (objectivesById.Count == 0) return false;
        foreach (var obj in objectivesById.Values) if (!obj.Completed) return false;
        return true;
    }

    private void UpdateSuppliesText() => UIManager.Instance?.UpdateSupplies($"Supplies: {suppliesFound}/{totalSupplyCaches}");

    private void UpdateObjectiveText()
    {
        string objective = $"Zone: {activeZone}\n";
        if (extractionUnlocked) objective += "Return to extraction point";
        else objective += GetObjectiveTextForZone(activeZone);
        UIManager.Instance?.UpdateObjective(objective);
    }

    private string GetObjectiveTextForZone(string zoneName)
    {
        zoneName = NormalizeZoneName(zoneName, "Outskirts");
        foreach (var record in objectivesById.Values)
        {
            if (record.ZoneName == zoneName)
                return record.Completed ? $"{record.Description} [Done]" : record.Description;
        }
        int remaining = zoneZombieRemaining.ContainsKey(zoneName) ? zoneZombieRemaining[zoneName] : 0;
        return remaining > 0 ? $"Clear infected: {remaining} remaining" : "Explore area";
    }

    private string NormalizeZoneName(string zoneName, string fallback) => string.IsNullOrWhiteSpace(zoneName) ? fallback : zoneName.Trim();

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameConfig.IsMenuScene(scene.name))
        {
            StopWaveLoop();
            ChangeState(GameState.MainMenu);
            return;
        }

        PrepareGameplaySession();
    }

    void PrepareGameplaySession()
    {
        Time.timeScale = 1f;
        currentWave = 0;
        score = 0;
        suppliesFound = 0;
        totalSupplyCaches = 0;
        extractionUnlocked = false;
        activeZone = "Approach";

        zoneZombieRemaining.Clear();
        zoneSupplyTotals.Clear();
        zoneSupplyCollected.Clear();
        objectivesById.Clear();

        waveAlive = 0;
        waveSpawnTarget = 0;

        UIManager.Instance?.UpdateScore(score);
        UIManager.Instance?.UpdateSupplies("Supplies: 0/0");
        UIManager.Instance?.UpdateWave(currentWave, 1, waveAlive, waveSpawnTarget);
        UpdateObjectiveText();
        ShowPersistentRunStats();

        if (CurrentState != GameState.Playing)
        {
            ChangeState(GameState.Playing);
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StartWaveLoopIfNeeded();
        }
    }

    // Chamado pelo Bootstrap após criar o template — garante que a wave loop arranca mesmo que
    // o Start() do GameManager tenha corrido antes do prefab estar disponível.
    public void SetZombiePrefabAndStartWaves(GameObject prefab)
    {
        if (prefab == null) return;
        zombiePrefab = prefab;
        Debug.Log($"[GameManager] SetZombiePrefabAndStartWaves: prefab={prefab.name}, state={CurrentState}");
        // Reinicia a wave loop se não estiver a correr
        if (autoSpawnWaves && CurrentState == GameState.Playing && waveRoutine == null)
            waveRoutine = StartCoroutine(WaveLoopRoutine());
    }

    void StartWaveLoopIfNeeded()
    {
        if (!autoSpawnWaves || CurrentState != GameState.Playing)
            return;

        // Se zombiePrefab ainda está null, tenta carregar agora
        if (zombiePrefab == null)
            zombiePrefab = LoadZombiePrefab();

        if (zombiePrefab == null)
        {
            Debug.LogError("[GameManager] StartWaveLoopIfNeeded: zombiePrefab é null mesmo após tentativa de carga. Waves canceladas.");
            return;
        }

        if (waveRoutine != null)
            return;

        waveRoutine = StartCoroutine(WaveLoopRoutine());
    }

    // Carrega o prefab zombie directamente — não depende do Bootstrap ter corrido.
    GameObject LoadZombiePrefab()
    {
#if UNITY_EDITOR
        string[] paths =
        {
            "Assets/ZombieMale_AAB/Prefabs/URP/ZombieMale_AAB_URP.prefab",
            "Assets/ZombieMale_AAB/Prefabs/ZombieMale_AAB.prefab",
            "Assets/ZombieMale_AAB/Prefabs/HDRP/ZombieMale_AAB_HDRP.prefab"
        };
        foreach (string path in paths)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p != null)
            {
                Debug.Log($"[GameManager] Prefab zombie carregado: {path}");
                return p;
            }
        }
        string[] guids = AssetDatabase.FindAssets("ZombieMale_AAB t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || path.Contains("BodyParts")) continue;
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p != null) { Debug.Log($"[GameManager] Prefab zombie (GUID) carregado: {path}"); return p; }
        }
        Debug.LogError("[GameManager] Prefab zombie não encontrado. Certifica que ZombieMale_AAB_URP.prefab está em Assets/ZombieMale_AAB/Prefabs/URP/");
        return null;
#else
        // RUNTIME BUILD: tenta Resources primeiro, depois procura na cena um template
        GameObject p = Resources.Load<GameObject>("Prefabs/ZombieMale_AAB_URP");
        if (p != null) return p;

        // Último recurso: usa o template que o Bootstrap pode ter deixado na cena
        ZombieHealth existingTemplate = Object.FindFirstObjectByType<ZombieHealth>(FindObjectsInactive.Include);
        if (existingTemplate != null)
        {
            Debug.Log("[GameManager] Usando zombie existente na cena como template de Instantiate.");
            return existingTemplate.gameObject;
        }

        Debug.LogError("[GameManager] BUILD: Prefab zombie não encontrado. Copia ZombieMale_AAB_URP.prefab para Assets/Resources/Prefabs/");
        return null;
#endif
    }

    void StopWaveLoop()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
    }

    IEnumerator WaveLoopRoutine()
    {
        yield return new WaitForSeconds(2f);

        while (CurrentState == GameState.Playing)
        {
            yield return StartCoroutine(RunNextWave());
            float tierDelayMultiplier = 1f - (GetCurrentWaveTier() - 1) * 0.15f;
            float pause = Mathf.Max(3f, timeBetweenWaves * tierDelayMultiplier);
            yield return new WaitForSeconds(pause);
        }
    }

    IEnumerator RunNextWave()
    {
        currentWave++;
        int tier = GetCurrentWaveTier();
        int targetCount = GetWaveZombieCount(currentWave, tier);
        waveSpawnTarget = targetCount;
        waveAlive = 0;

        UIManager.Instance?.UpdateWave(currentWave, tier, waveAlive, waveSpawnTarget);
        UIManager.Instance?.ShowMessage($"Wave {currentWave} started (Tier {tier})");
        AudioManager.Instance?.SwitchToCombat();

        float spawnInterval = Mathf.Max(0.18f, 0.48f - tier * 0.08f);
        int failSafe = targetCount * 3;
        int spawned = 0;

        while (spawned < targetCount && CurrentState == GameState.Playing && failSafe > 0)
        {
            failSafe--;
            if (waveAlive >= maxActiveWaveZombies)
            {
                yield return new WaitForSeconds(0.75f);
                continue;
            }

            if (!TrySpawnWaveZombie(currentWave, tier))
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            spawned++;
            yield return new WaitForSeconds(spawnInterval);
        }

        while (CurrentState == GameState.Playing && waveAlive > 0)
        {
            UIManager.Instance?.UpdateWave(currentWave, tier, waveAlive, waveSpawnTarget);
            yield return new WaitForSeconds(0.3f);
        }

        if (CurrentState == GameState.Playing)
        {
            AudioManager.Instance?.SwitchToAmbient();
            AddScore(120 + currentWave * 10);
            SaveProgressCheckpoint();
            UIManager.Instance?.ShowMessage($"Wave {currentWave} cleared");
        }
    }

    int GetCurrentWaveTier()
    {
        if (wavesPerTier <= 0)
        {
            return 1;
        }

        return Mathf.Clamp(((currentWave - 1) / wavesPerTier) + 1, 1, 3);
    }

    int GetWaveZombieCount(int wave, int tier)
    {
        float difficultyCountMultiplier = GameConfig.DifficultyLevel switch
        {
            0 => 0.85f,
            2 => 1.25f,
            _ => 1f
        };

        float raw = zombiesPerWave * Mathf.Pow(waveMultiplier, wave - 1) * difficultyCountMultiplier;
        float tierBonus = 1f + (tier - 1) * 0.25f;
        return Mathf.Clamp(Mathf.RoundToInt(raw * tierBonus), 4, 90);
    }

    bool TrySpawnWaveZombie(int wave, int tier)
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("[GameManager] zombiePrefab é null — waves não conseguem spawnar. Verifica CreateZombieTemplate no Bootstrap.");
            return false;
        }

        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
        {
            return false;
        }

        if (!ResolveValidSpawnHeight(ref spawnPosition))
        {
            return false;
        }

        // Instancia o prefab URP e força ativo
        GameObject zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        zombie.name = $"Wave {wave} T{tier} Zombie";
        zombie.SetActive(true);

        // Garante CapsuleCollider
        CapsuleCollider col = zombie.GetComponent<CapsuleCollider>();
        if (col == null) col = zombie.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.35f;
        col.center = new Vector3(0f, 0.9f, 0f);

        // Garante AudioSource
        AudioSource audio = zombie.GetComponent<AudioSource>();
        if (audio == null) { audio = zombie.AddComponent<AudioSource>(); audio.playOnAwake = false; }

        // Garante ZombieAI
        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai == null) ai = zombie.AddComponent<ZombieAI>();

        // Garante ZombieHealth
        ZombieHealth zh = zombie.GetComponent<ZombieHealth>();
        if (zh == null) zh = zombie.AddComponent<ZombieHealth>();

        // Configura zona e stats base pelo tier
        zh.zoneName = WaveZoneName;
        zh.maxHealth = GetTierBaseHealth(tier);
        zh.scoreOnDeath = 40 + tier * 20;

        // Aplica variante aleatória com pesos por tier
        ai.variant = PickVariantForTier(tier);
        ai.ApplyHordeModifiers(
            GetTierSpeedMultiplier(tier),
            GetTierDamageMultiplier(tier),
            GetTierDetectionMultiplier(tier));

        waveAlive++;
        UIManager.Instance?.UpdateWave(currentWave, tier, waveAlive, waveSpawnTarget);
        return true;
    }

    // ---------- helpers de tier ----------

    int GetTierBaseHealth(int tier) => tier switch
    {
        1 => 80,
        2 => 140,
        _ => 220   // tier 3
    };

    ZombieAI.ZombieVariant PickVariantForTier(int tier)
    {
        // Tier 1: só walkers e runners
        // Tier 2: + crawlers e screamers
        // Tier 3: tudo incluindo tanks
        float r = Random.value;
        return tier switch
        {
            1 => r < 0.65f ? ZombieAI.ZombieVariant.Walker : ZombieAI.ZombieVariant.Runner,
            2 => r < 0.30f ? ZombieAI.ZombieVariant.Walker
               : r < 0.55f ? ZombieAI.ZombieVariant.Runner
               : r < 0.75f ? ZombieAI.ZombieVariant.Crawler
               : ZombieAI.ZombieVariant.Screamer,
            _ => r < 0.20f ? ZombieAI.ZombieVariant.Walker
               : r < 0.38f ? ZombieAI.ZombieVariant.Runner
               : r < 0.54f ? ZombieAI.ZombieVariant.Crawler
               : r < 0.72f ? ZombieAI.ZombieVariant.Screamer
               : ZombieAI.ZombieVariant.Tank
        };
    }

    bool ResolveValidSpawnHeight(ref Vector3 spawnPosition)
    {
        Vector3 probeStart = spawnPosition + Vector3.up * 120f;
        RaycastHit[] hits = Physics.RaycastAll(probeStart, Vector3.down, 420f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (!IsWalkableSurface(h.collider)) continue;
                spawnPosition = h.point + Vector3.up * 0.1f;

                // Snap to NavMesh se disponível
                if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
                    spawnPosition = navHit.position + Vector3.up * 0.05f;

                return true;
            }
        }

        // Sem geometria walkable — aceita a posição com altura plana do player
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        spawnPosition.y = player != null ? player.position.y + 0.1f : 0.1f;
        return true;
    }

    bool IsWalkableSurface(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        int layerBit = 1 << hitCollider.gameObject.layer;
        if ((Physics.DefaultRaycastLayers & layerBit) != 0)
        {
            return true;
        }

        string surfaceName = hitCollider.gameObject.name;
        return surfaceName == "Ground"
            || surfaceName == "Outer Terrain"
            || surfaceName == "Main Street"
            || surfaceName == "Cross Road"
            || surfaceName.Contains("Road")
            || surfaceName.Contains("Terrain");
    }

    bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Vector3 center = player != null ? player.position : Vector3.zero;
        Vector3 playerEye = center + Vector3.up * 1.5f;

        if (player == null)
        {
            Debug.LogWarning("[GameManager] TryGetSpawnPosition: Player não encontrado. Spawnar em posição de emergência.");
            // Sem player, usa posição fixa de emergência
            spawnPosition = new Vector3(30f, 0.5f, 0f);
            return true;
        }

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            int tries = Mathf.Min(spawnPoints.Count, 16);
            for (int i = 0; i < tries; i++)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
                if (point == null) continue;
                Vector3 candidate = point.position;
                if (!IsFairSpawnPosition(candidate, player, playerEye, center)) continue;
                spawnPosition = candidate;
                return true;
            }
        }

        // Ring spawn dinâmico — tenta 35 vezes (mais tentativas que antes)
        for (int i = 0; i < 35; i++)
        {
            Vector2 raw = Random.insideUnitCircle;
            if (raw.sqrMagnitude < 0.0005f) continue;

            Vector2 ring = raw.normalized * Random.Range(dynamicSpawnMinDistance, dynamicSpawnMaxDistance);
            Vector3 probe = new Vector3(center.x + ring.x, center.y + 20f, center.z + ring.y);
            if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 candidate = hit.point + Vector3.up * 0.1f;
                if (!IsFairSpawnPosition(candidate, player, playerEye, center)) continue;
                spawnPosition = candidate;
                return true;
            }
        }

        // Fallback garantido: ignora fairness, apenas garante distância mínima
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist  = Random.Range(dynamicSpawnMinDistance, dynamicSpawnMaxDistance);
            Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            candidate.y = center.y + 0.5f;
            spawnPosition = candidate;
            return true;
        }
    }

    bool IsFairSpawnPosition(Vector3 candidate, Transform playerTransform, Vector3 playerEye, Vector3 playerPos)
    {
        float dist = Vector3.Distance(candidate, playerPos);
        if (dist < dynamicSpawnMinDistance * 0.95f)
        {
            return false;
        }

        if (dist > dynamicSpawnMaxDistance * 1.6f)
        {
            return false;
        }

        if (playerTransform == null)
        {
            return true;
        }

        Vector3 targetPoint = candidate + Vector3.up * 0.9f;
        Vector3 toTarget = targetPoint - playerEye;
        float rayDistance = toTarget.magnitude;
        if (rayDistance <= 0.01f)
        {
            return false;
        }

        Vector3 dir = toTarget / rayDistance;
        float viewDot = Vector3.Dot(playerTransform.forward, dir);
        bool isBehindPlayer = viewDot <= 0.1f;
        if (isBehindPlayer)
        {
            return true;
        }

        // If not directly in front, allow some spawns to keep wave flow healthy.
        if (viewDot <= 0.45f && dist >= dynamicSpawnMinDistance * 1.15f)
        {
            return true;
        }

        // Fair if there is real geometry between player and spawn point (not counting own collider).
        if (Physics.Raycast(playerEye, dir, out RaycastHit hit, rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != null && (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform)))
            {
                return false;
            }

            float blockThreshold = rayDistance - 0.35f;
            bool hasOccluderBeforeTarget = hit.distance < blockThreshold;
            if (hasOccluderBeforeTarget)
            {
                return true;
            }
        }

        return false;
    }

    float GetTierSpeedMultiplier(int tier)
    {
        return tier switch
        {
            1 => 1.0f,
            2 => 1.2f,
            _ => 1.35f
        };
    }

    float GetTierDamageMultiplier(int tier)
    {
        return tier switch
        {
            1 => 1.0f,
            2 => 1.3f,
            _ => 1.6f
        };
    }

    float GetTierDetectionMultiplier(int tier)
    {
        return tier switch
        {
            1 => 1.0f,
            2 => 1.12f,
            _ => 1.22f
        };
    }

    void SaveProgressCheckpoint()
    {
        if (currentWave > BestWave)
        {
            PlayerPrefs.SetInt(PrefBestWave, currentWave);
        }

        if (score > BestScore)
        {
            PlayerPrefs.SetInt(PrefBestScore, score);
        }

        PlayerPrefs.Save();
    }

    void SaveRunResult(bool victory)
    {
        SaveProgressCheckpoint();
        PlayerPrefs.SetInt(PrefTotalRuns, TotalRuns + 1);
        PlayerPrefs.Save();
        Debug.Log($"[GameManager] Run saved. Victory={victory}, Score={score}, Wave={currentWave}");
    }

    void ShowPersistentRunStats()
    {
        if (BestScore <= 0 && BestWave <= 0)
        {
            return;
        }

        UIManager.Instance?.ShowMessage($"Best Wave {BestWave} | Best Score {BestScore}");
    }
}
