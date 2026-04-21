using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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
    public int zombiesPerWave = 5;
    public float timeBetweenWaves = 10f;
    public float waveMultiplier = 1.5f;
    public bool autoSpawnWaves = false;

    [Header("Game State")]
    public int currentWave = 0;
    public int score = 0;

    private int suppliesFound = 0;
    private int totalSupplyCaches = 0;
    private string activeZone = "Approach";
    private bool extractionUnlocked;

    private readonly Dictionary<string, int> zoneZombieRemaining = new Dictionary<string, int>();
    private readonly Dictionary<string, int> zoneSupplyTotals = new Dictionary<string, int>();
    private readonly Dictionary<string, int> zoneSupplyCollected = new Dictionary<string, int>();
    private readonly Dictionary<string, ObjectiveRecord> objectivesById = new Dictionary<string, ObjectiveRecord>();

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

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu" || sceneName == "Menu")
            ChangeState(GameState.MainMenu);
        else
            ChangeState(GameState.Playing);
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
                // Carrega o jogo se estiver no menu principal
                if (SceneManager.GetActiveScene().name == "MainMenu" || SceneManager.GetActiveScene().name == "Menu")
                    SceneManager.LoadScene("Mapa_EXT01");
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameState.GameOver:
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

        AddScore(10);
        UpdateObjectiveText();
        CheckExtractionUnlock();
    }

    public void GameOver(string reason = "You died.")
    {
        ChangeState(GameState.GameOver);
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
        ChangeState(GameState.GameOver);
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
}
