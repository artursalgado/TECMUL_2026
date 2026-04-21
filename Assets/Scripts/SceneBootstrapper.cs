using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SceneBootstrapper
{
    public const int SceneVersion = 7;

    static readonly string[] LegacyRootNames =
    {
        "Canvas",
        "HUD Canvas",
        "Crosshair Canvas",
        "GameManager",
        "UIManager",
        "GameOverScreen",
        "Player",
        "Prototype Environment",
        "Residential Block",
        "Shelter Yard",
        "Clinic",
        "Warehouse",
        "Fuel Depot",
        "Extraction Point",
        "Zombie Template",
        "Generated OpenWorld",
        "Prototype Scene Marker",
        "Terrain",
        "Stylized Dressing"
    };

    static readonly Dictionary<Color32, Material> SharedMaterialCache = new Dictionary<Color32, Material>();
    static Shader cachedSurfaceShader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BuildSceneIfNeeded()
    {
        if (!ShouldBuildActiveScene())
        {
            return;
        }

        BuildPrototypeScene();
    }

    public static bool ShouldBuildActiveScene()
    {
        return SceneManager.GetActiveScene().name == "Mapa_EXT01";
    }

    public static void BuildPrototypeScene()
    {
        Debug.Log("[Bootstrap] Phase 1: HUD & Config Menu Initialization...");
        PrepareSceneForBootstrap();
        CreateSceneMarker();
        
        // Only create UI first to show the menu
        UIManager uiManager = CreateUI();
        if (uiManager != null) {
            if (GameConfig.SkipConfigMenu) {
                // Skip the menu and build gameplay immediately
                ExecuteGameplayBuild();
                GameConfig.SkipConfigMenu = false; // Reset for next time
            } else {
                // Config menu - cria MainMenuManager se nao existir
                CreateMainMenuManagerIfNeeded();
                uiManager.ShowConfigMenu();
            }
        }
    }

    static bool _gameplayBuildExecuted = false;

    public static void ResetGameplayBuildFlag()
    {
        _gameplayBuildExecuted = false;
    }

    public static void ExecuteGameplayBuild()
    {
        if (_gameplayBuildExecuted)
        {
            Debug.Log("[Bootstrap] ExecuteGameplayBuild ja foi executado, ignorando...");
            return;
        }
        _gameplayBuildExecuted = true;

        Debug.Log("[Bootstrap] Phase 2: Generating Procedural World...");

        CreateWorld();
        GameObject player = CreatePlayer();
        
        // Setup Shooting ref for UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.shooting = player.GetComponent<Shooting>();
        }

        GameObject zombieTemplate = CreateZombieTemplate();
        GameManager gameManager = CreateGameManager(zombieTemplate, new List<Transform>());
        gameManager.autoSpawnWaves = false;

        // Apply difficulty settings
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null) ph.maxHealth = 100; // Reset for config

        CreateResidentialZone();
        CreateShelterZone();
        CreateClinicZone();
        CreateWarehouseZone();
        CreateFuelDepotZone();
        CreateExtractionPoint();
        ApplyStylizedDecoration();
        
        // Lighting adjustment
        RenderSettings.ambientIntensity = GameConfig.LightIntensity;
        RenderSettings.ambientLight = GameConfig.AmbientColor;
        
        RuntimeSceneValidator.ValidateCurrentScene();
        Debug.Log("[Bootstrap] MISSION READY.");
    }

    static void PrepareSceneForBootstrap()
    {
        Debug.Log("[Bootstrap] Clearing legacy objects...");
        SharedMaterialCache.Clear();
        Scene activeScene = SceneManager.GetActiveScene();

        for (int i = 0; i < LegacyRootNames.Length; i++)
        {
            DestroyObjectsByName(activeScene, LegacyRootNames[i]);
        }

        DestroyObjectsOfType<UIManager>();
        DestroyObjectsOfType<GameOverScreen>();
        DestroyObjectsOfType<PlayerMovement>();
        DestroyObjectsOfType<PlayerHealth>();
        DestroyObjectsOfType<PlayerInventory>();
        DestroyObjectsOfType<PlayerInteractor>();
        DestroyObjectsOfType<Shooting>();
        DestroyObjectsOfType<ZombieAI>();
        DestroyObjectsOfType<ZombieHealth>();
        DestroyObjectsOfType<LootContainer>();
        DestroyObjectsOfType<ZoneTrigger>();
        DestroyObjectsOfType<ObjectiveInteractable>();
        DestroyObjectsOfType<ExtractionZone>();
        DestroyObjectsOfType<Crosshair>();
    }

    static void DestroyObjectsByName(Scene scene, string objectName)
    {
        Transform[] allObjects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            Transform transform = allObjects[i];
            if (transform == null)
            {
                continue;
            }

            GameObject go = transform.gameObject;
            if (go.scene != scene || go.name != objectName)
            {
                continue;
            }

            DestroySafe(go);
        }
    }

    static void DestroyObjectsOfType<T>() where T : Component
    {
        T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null)
            {
                continue;
            }

            GameObject owner = component.gameObject;
            DestroySafe(owner);
        }
    }

    static void CreateSceneMarker()
    {
        Debug.Log("[Bootstrap] Creating/Updating Scene Marker...");
        PrototypeSceneMarker existingMarker = Object.FindFirstObjectByType<PrototypeSceneMarker>();
        if (existingMarker != null)
        {
            existingMarker.version = SceneVersion;
            // Removed return to allow world generation to continue
        }
        else
        {
            GameObject markerObject = new GameObject("Prototype Scene Marker");
            PrototypeSceneMarker marker = markerObject.AddComponent<PrototypeSceneMarker>();
            marker.version = SceneVersion;
        }
    }

    static void CreateMainMenuManagerIfNeeded()
    {
        if (MainMenuManager.Instance != null) return;

        Debug.Log("[Bootstrap] Creating MainMenuManager...");
        GameObject menuGO = new GameObject("MainMenuManager");
        menuGO.AddComponent<MainMenuManager>();
    }

    static void CreateWorld()
    {
        Debug.Log("[Bootstrap] Building world...");
        GameObject existingRoot = GameObject.Find("Prototype Environment");
        if (existingRoot != null)
        {
            return;
        }

        GameObject environmentRoot = new GameObject("Prototype Environment");
        CreateGround(environmentRoot.transform);
        CreateOuterTerrain(environmentRoot.transform);
        CreateDistantBackdrop(environmentRoot.transform);
        CreateRoad(environmentRoot.transform, "Main Street", new Vector3(0f, 0.02f, 0f), new Vector3(14f, 0.05f, 80f));
        CreateRoad(environmentRoot.transform, "Cross Road", new Vector3(0f, 0.02f, 4f), new Vector3(80f, 0.05f, 14f));
        CreateArenaBounds(environmentRoot.transform);
        CreateStreetLights(environmentRoot.transform);
    }

    static void CreateGround(Transform parent)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Ground";
        floor.transform.SetParent(parent);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(12f, 1f, 12f);

        Renderer floorRenderer = floor.GetComponent<Renderer>();
        if (floorRenderer != null)
        {
            SetRendererTexture(floorRenderer, new Color(0.2f, 0.24f, 0.2f), "GroundTexture");
        }
    }

    static void CreateOuterTerrain(Transform parent)
    {
        GameObject outerTerrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
        outerTerrain.name = "Outer Terrain";
        outerTerrain.transform.SetParent(parent);
        outerTerrain.transform.position = new Vector3(0f, -0.15f, 0f);
        outerTerrain.transform.localScale = new Vector3(40f, 1f, 40f);

        Renderer terrainRenderer = outerTerrain.GetComponent<Renderer>();
        if (terrainRenderer != null)
        {
            SetRendererTexture(terrainRenderer, new Color(0.29f, 0.26f, 0.18f), "GroundTexture");
        }

        CreateBoundarySlope(parent, new Vector3(0f, -1.2f, 220f), new Vector3(760f, 14f, 40f), new Vector3(8f, 0f, 0f));
        CreateBoundarySlope(parent, new Vector3(0f, -1.2f, -220f), new Vector3(760f, 14f, 40f), new Vector3(-8f, 0f, 0f));
        CreateBoundarySlope(parent, new Vector3(220f, -1.2f, 0f), new Vector3(40f, 14f, 760f), new Vector3(0f, 0f, -8f));
        CreateBoundarySlope(parent, new Vector3(-220f, -1.2f, 0f), new Vector3(40f, 14f, 760f), new Vector3(0f, 0f, 8f));
    }

    static void CreateDistantBackdrop(Transform parent)
    {
        CreateBackdropWall(parent, "North Cliffs", new Vector3(0f, 24f, 300f), new Vector3(900f, 48f, 60f), new Color(0.18f, 0.19f, 0.2f));
        CreateBackdropWall(parent, "South Cliffs", new Vector3(0f, 24f, -300f), new Vector3(900f, 48f, 60f), new Color(0.18f, 0.19f, 0.2f));
        CreateBackdropWall(parent, "East Cliffs", new Vector3(300f, 24f, 0f), new Vector3(60f, 48f, 900f), new Color(0.18f, 0.19f, 0.2f));
        CreateBackdropWall(parent, "West Cliffs", new Vector3(-300f, 24f, 0f), new Vector3(60f, 48f, 900f), new Color(0.18f, 0.19f, 0.2f));
        CreateBackdropWall(parent, "North Ridge", new Vector3(0f, 10f, 180f), new Vector3(520f, 18f, 24f), new Color(0.24f, 0.22f, 0.18f));
        CreateBackdropWall(parent, "South Ridge", new Vector3(0f, 10f, -180f), new Vector3(520f, 18f, 24f), new Color(0.24f, 0.22f, 0.18f));
    }

    static void CreateBackdropWall(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        SetRendererTexture(wall.GetComponent<Renderer>(), color, "BuildingBrickTexture");
    }

    static void CreateRoad(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = name;
        road.transform.SetParent(parent);
        road.transform.position = position;
        road.transform.localScale = scale;

        Renderer renderer = road.GetComponent<Renderer>();
        if (renderer != null)
        {
            SetRendererTexture(renderer, Color.white, "RoadTexture");
        }
    }

    static void CreateArenaBounds(Transform parent)
    {
        CreateWall(parent, "North Wall", new Vector3(0f, 2f, 96f), new Vector3(192f, 4f, 1f));
        CreateWall(parent, "South Wall", new Vector3(0f, 2f, -96f), new Vector3(192f, 4f, 1f));
        CreateWall(parent, "East Wall", new Vector3(96f, 2f, 0f), new Vector3(1f, 4f, 192f));
        CreateWall(parent, "West Wall", new Vector3(-96f, 2f, 0f), new Vector3(1f, 4f, 192f));
    }

    static void CreateStreetLights(Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(-18f, 0f, -18f),
            new Vector3(18f, 0f, -18f),
            new Vector3(-18f, 0f, 18f),
            new Vector3(18f, 0f, 18f),
            new Vector3(-32f, 0f, 4f),
            new Vector3(32f, 0f, 4f)
        };

        foreach (Vector3 position in positions)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Street Light";
            pole.transform.SetParent(parent);
            pole.transform.position = position + new Vector3(0f, 2.5f, 0f);
            pole.transform.localScale = new Vector3(0.25f, 2.5f, 0.25f);
            SetRendererColor(pole.GetComponent<Renderer>(), new Color(0.25f, 0.28f, 0.31f));

            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Lamp";
            lamp.transform.SetParent(pole.transform);
            lamp.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            lamp.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);
            SetRendererColor(lamp.GetComponent<Renderer>(), new Color(1f, 0.8f, 0.45f));
        }
    }

    static void CreateWall(Transform parent, string wallName, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        SetRendererTexture(wall.GetComponent<Renderer>(), Color.white, "BuildingBrickTexture");
    }

    static void CreateBoundarySlope(Transform parent, Vector3 position, Vector3 scale, Vector3 eulerAngles)
    {
        GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slope.name = "Boundary Slope";
        slope.transform.SetParent(parent);
        slope.transform.position = position;
        slope.transform.eulerAngles = eulerAngles;
        slope.transform.localScale = scale;
        SetRendererColor(slope.GetComponent<Renderer>(), new Color(0.23f, 0.21f, 0.17f));
    }

    static GameObject CreatePlayer()
    {
        GameObject player = FindPlayerInActiveScene();
        if (player == null)
        {
            player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.5f, -24f);
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = player.AddComponent<CharacterController>();
        }

        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        AudioSource audioSource = player.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = player.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            movement = player.AddComponent<PlayerMovement>();
        }

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null)
        {
            health = player.AddComponent<PlayerHealth>();
        }

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = player.AddComponent<PlayerInventory>();
        }

        Shooting shooting = player.GetComponent<Shooting>();
        if (shooting == null)
        {
            shooting = player.AddComponent<Shooting>();
        }

        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor == null)
        {
            interactor = player.AddComponent<PlayerInteractor>();
        }

        Camera camera = FindMainCameraInActiveScene();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.transform.SetParent(player.transform);
        camera.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        camera.transform.localRotation = Quaternion.identity;
        camera.enabled = true;

        // Weapon Visuals (FPS View)
        GameObject weaponRoot = new GameObject("FPS Weapon");
        weaponRoot.transform.SetParent(camera.transform, false);
        CreateWeaponVisuals(weaponRoot.transform, true);

        movement.playerCamera = camera.transform;
        movement.walkSpeed = 6f;
        movement.sprintSpeed = 9f;
        movement.jumpHeight = 1.1f;

        shooting.fpsCamera = camera;
        shooting.range = 120f;
        shooting.damage = 30;
        shooting.maxAmmo = 28;
        shooting.reloadTime = 1.45f;

        interactor.interactionCamera = camera;

        health.maxHealth = 100;
        health.canRegenerate = false;
        inventory.maxCarryWeight = 22f;
        inventory.ammoReserve = 56;
        inventory.medkits = 2;

        CreateCrosshair();
        return player;
    }

    static GameObject FindPlayerInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < allPlayers.Length; i++)
        {
            GameObject player = allPlayers[i];
            if (player != null && player.scene == activeScene)
            {
                return player;
            }
        }

        return null;
    }

    static Camera FindMainCameraInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null)
            {
                continue;
            }

            if (cam.gameObject.scene != activeScene)
            {
                continue;
            }

            if (cam.CompareTag("MainCamera"))
            {
                return cam;
            }
        }

        return null;
    }

    static UIManager CreateUI()
    {
        UIManager uiManager = FindComponentInActiveScene<UIManager>();
        if (uiManager == null)
        {
            GameObject uiManagerGO = new GameObject("UIManager");
            uiManager = uiManagerGO.AddComponent<UIManager>();
        }

        uiManager.EnsureRuntimeHud();

        if (FindComponentInActiveScene<GameOverScreen>() == null)
        {
            GameObject gameOverGO = new GameObject("GameOverScreen");
            gameOverGO.AddComponent<GameOverScreen>();
        }

        if (Object.FindFirstObjectByType<PauseMenu>() == null)
        {
            GameObject pauseGO = new GameObject("PauseMenu");
            pauseGO.AddComponent<PauseMenu>();
        }

        return uiManager;
    }

    static void CreateCrosshair()
    {
        if (Object.FindFirstObjectByType<Crosshair>() != null)
        {
            return;
        }

        GameObject canvasObject = GameObject.Find("Crosshair Canvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Crosshair Canvas");
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        for (int i = canvasObject.transform.childCount - 1; i >= 0; i--)
        {
            DestroySafe(canvasObject.transform.GetChild(i).gameObject);
        }

        GameObject crosshairObject = new GameObject("Crosshair");
        crosshairObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = crosshairObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(64f, 64f);

        crosshairObject.AddComponent<Crosshair>();
    }

    static GameObject CreateZombieTemplate()
    {
        GameObject zombie = new GameObject("Zombie Template");
        zombie.name = "Zombie Template";
        zombie.SetActive(false);
        zombie.transform.position = new Vector3(0f, 1f, 18f);

        CapsuleCollider collider = zombie.AddComponent<CapsuleCollider>();
        collider.height = 1.8f;
        collider.radius = 0.35f;
        collider.center = new Vector3(0f, 0.9f, 0f);

        AudioSource audioSource = zombie.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        CreateZombieVisuals(zombie.transform);

        ZombieAI zombieAI = zombie.AddComponent<ZombieAI>();
        ZombieHealth zombieHealth = zombie.AddComponent<ZombieHealth>();

        zombieAI.moveSpeed = 1.5f;
        zombieAI.detectionDistance = 13f;
        zombieAI.attackDistance = 1.25f;
        zombieAI.attackRate = 1.5f;
        zombieAI.attackDamage = 5;

        zombieHealth.maxHealth = 90;
        zombieHealth.scoreOnDeath = 50;

        return zombie;
    }

    static GameManager CreateGameManager(GameObject zombieTemplate, List<Transform> spawnPoints)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindComponentInActiveScene<GameManager>();
        }
        if (gameManager == null)
        {
            GameObject gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
        }

        gameManager.zombiePrefab = zombieTemplate;
        gameManager.spawnPoints = spawnPoints;
        return gameManager;
    }

    static T FindComponentInActiveScene<T>() where T : Component
    {
        Scene activeScene = SceneManager.GetActiveScene();
        T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null)
            {
                continue;
            }

            if (component.gameObject.scene == activeScene)
            {
                return component;
            }
        }

        return null;
    }

    static void CreateResidentialZone()
    {
        if (GameObject.Find("Residential Block") != null)
        {
            return;
        }

        GameObject zone = new GameObject("Residential Block");
        CreateZoneTrigger(zone.transform, "Residential Block", new Vector3(-22f, 1.5f, -8f), new Vector3(50f, 3f, 50f));
        CreateZoneObjective(zone.transform, "Residential Block", "res_keys", "Recover the shelter key");

        // Casas existentes com texturas melhoradas
        CreateTexturedBuilding(zone.transform, "House A", new Vector3(-32f, 0f, -8f), new Vector3(12f, 9f, 10f), new Color(0.61f, 0.52f, 0.44f), "brick");
        CreateTexturedBuilding(zone.transform, "House B", new Vector3(-16f, 0f, -8f), new Vector3(12f, 8f, 12f), new Color(0.52f, 0.57f, 0.45f), "wood");
        CreateTexturedBuilding(zone.transform, "House C", new Vector3(-24f, 0f, 8f), new Vector3(14f, 10f, 11f), new Color(0.48f, 0.46f, 0.56f), "concrete");

        // NOVAS CASAS ADICIONADAS
        CreateTexturedBuilding(zone.transform, "House D", new Vector3(-44f, 0f, -5f), new Vector3(10f, 7f, 10f), new Color(0.72f, 0.55f, 0.41f), "brick");
        CreateTexturedBuilding(zone.transform, "House E", new Vector3(-38f, 0f, 12f), new Vector3(11f, 8f, 11f), new Color(0.55f, 0.65f, 0.52f), "wood");
        CreateTexturedBuilding(zone.transform, "House F - Garage", new Vector3(-8f, 0f, 8f), new Vector3(8f, 5f, 12f), new Color(0.45f, 0.48f, 0.52f), "metal");
        CreateTexturedBuilding(zone.transform, "House G - Two Story", new Vector3(-50f, 0f, -18f), new Vector3(13f, 14f, 12f), new Color(0.68f, 0.62f, 0.48f), "brick");

        // Elementos de cenário
        CreateYardCover(zone.transform, new Vector3(-24f, 1f, -20f), new Vector3(10f, 2f, 4f));
        CreateCarWreck(zone.transform, new Vector3(-8f, 0.8f, -9f), new Color(0.32f, 0.21f, 0.19f));
        CreateCarWreck(zone.transform, new Vector3(-42f, 0.8f, 2f), new Color(0.45f, 0.38f, 0.35f));

        // LOOTS EXPANDIDOS
        CreateLootCache(zone.transform, "Pantry", "Residential Block", "Food", new Vector3(-32f, 1f, -11f), new Color(0.82f, 0.71f, 0.32f), 2, 0, 0);
        CreateLootCache(zone.transform, "Medicine Cabinet", "Residential Block", "Meds", new Vector3(-16f, 1f, -11f), new Color(0.57f, 0.88f, 0.75f), 1, 0, 20);
        CreateLootCache(zone.transform, "Backpack", "Residential Block", "Keys", new Vector3(-24f, 1f, 5f), new Color(0.64f, 0.47f, 0.31f), 1, 0, 0);

        // NOVOS SUPRIMENTOS
        CreateLootCache(zone.transform, "Kitchen Supplies", "Residential Block", "Food", new Vector3(-44f, 1f, -8f), new Color(0.92f, 0.81f, 0.42f), 3, 0, 0);
        CreateLootCache(zone.transform, "Bedroom Safe", "Residential Block", "Ammo", new Vector3(-38f, 1f, 10f), new Color(0.38f, 0.52f, 0.66f), 1, 15, 0);
        CreateLootCache(zone.transform, "Garage Toolbox", "Residential Block", "Scrap", new Vector3(-8f, 1f, 5f), new Color(0.55f, 0.48f, 0.38f), 2, 0, 0);
        CreateLootCache(zone.transform, "Attic Storage", "Residential Block", "Meds", new Vector3(-50f, 3f, -18f), new Color(0.47f, 0.78f, 0.65f), 1, 0, 30);
        CreateAmmoCrate(zone.transform, "Military Crate", "Residential Block", new Vector3(-28f, 1f, 15f), new Vector3(-30f, 1f, -5f));

        CreateObjectiveTerminal(zone.transform, "Shelter Key Rack", "Residential Block", "res_keys", "Press E to secure the shelter key", "Recovered shelter key", new Vector3(-24f, 1f, 2f), new Color(0.65f, 0.59f, 0.22f));

        // MAIS ZOMBIES
        CreateZombie(zone.transform, "Residential Block", new Vector3(-28f, 1f, -18f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Residential Block", new Vector3(-21f, 1f, -2f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Residential Block", new Vector3(-10f, 1f, -15f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Residential Block", new Vector3(-40f, 1f, -8f), ZombieAI.ZombieVariant.Crawler);
        CreateZombie(zone.transform, "Residential Block", new Vector3(-35f, 1f, 5f), ZombieAI.ZombieVariant.Screamer);
        CreateZombie(zone.transform, "Residential Block", new Vector3(-45f, 1f, 15f), ZombieAI.ZombieVariant.Walker);
    }

    static void CreateShelterZone()
    {
        if (GameObject.Find("Shelter Yard") != null)
        {
            return;
        }

        GameObject zone = new GameObject("Shelter Yard");
        CreateZoneTrigger(zone.transform, "Shelter Yard", new Vector3(0f, 1.5f, -6f), new Vector3(35f, 3f, 35f));
        CreateZoneObjective(zone.transform, "Shelter Yard", "shelter_gate", "Seal the shelter entrance");

        CreateTexturedBuilding(zone.transform, "Shelter", new Vector3(0f, 0f, -6f), new Vector3(18f, 8f, 12f), new Color(0.36f, 0.42f, 0.47f), "concrete");
        CreateTexturedBuilding(zone.transform, "Guard Tower", new Vector3(-12f, 0f, -6f), new Vector3(4f, 12f, 4f), new Color(0.42f, 0.45f, 0.48f), "metal");

        CreateObstacle(zone.transform, "Barricade Left", new Vector3(-9f, 1.5f, 4f), new Vector3(5f, 3f, 2f), new Color(0.33f, 0.25f, 0.2f));
        CreateObstacle(zone.transform, "Barricade Right", new Vector3(9f, 1.5f, 4f), new Vector3(5f, 3f, 2f), new Color(0.33f, 0.25f, 0.2f));
        CreateSandbagLine(zone.transform, new Vector3(0f, 0.6f, 8f), 8);
        CreateObjectiveTerminal(zone.transform, "Shelter Gate Console", "Shelter Yard", "shelter_gate", "Press E to seal shelter gate", "Shelter entrance secured", new Vector3(0f, 1f, 1f), new Color(0.4f, 0.58f, 0.66f), "Keys", 1, false);

        // Mais suprimentos no Shelter
        CreateLootCache(zone.transform, "Locker", "Shelter Yard", "Ammo", new Vector3(-4f, 1f, -4f), new Color(0.48f, 0.72f, 0.86f), 1, 12, 0);
        CreateLootCache(zone.transform, "Shelter Cabinet", "Shelter Yard", "Food", new Vector3(3f, 1f, -4f), new Color(0.82f, 0.71f, 0.32f), 2, 0, 0);
        CreateLootCache(zone.transform, "Emergency Stash", "Shelter Yard", "Meds", new Vector3(-12f, 3f, -6f), new Color(0.67f, 0.98f, 0.85f), 3, 0, 50);
        CreateAmmoCrate(zone.transform, "Armory", "Shelter Yard", new Vector3(8f, 1f, 2f), new Vector3(10f, 1f, 4f));

        CreateZombie(zone.transform, "Shelter Yard", new Vector3(-11f, 1f, 8f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Shelter Yard", new Vector3(12f, 1f, 9f), ZombieAI.ZombieVariant.Crawler);
        CreateZombie(zone.transform, "Shelter Yard", new Vector3(0f, 1f, 8f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Shelter Yard", new Vector3(-15f, 1f, -2f), ZombieAI.ZombieVariant.Screamer);
    }

    static void CreateClinicZone()
    {
        if (GameObject.Find("Clinic") != null)
        {
            return;
        }

        GameObject zone = new GameObject("Clinic");
        CreateZoneTrigger(zone.transform, "Clinic", new Vector3(28f, 1.5f, -10f), new Vector3(35f, 3f, 40f));
        CreateZoneObjective(zone.transform, "Clinic", "clinic_meds", "Find emergency medicine");

        CreateTexturedBuilding(zone.transform, "Clinic Building", new Vector3(28f, 0f, -10f), new Vector3(18f, 10f, 16f), new Color(0.76f, 0.78f, 0.82f), "concrete");
        CreateTexturedBuilding(zone.transform, "Pharmacy", new Vector3(42f, 0f, -5f), new Vector3(10f, 6f, 8f), new Color(0.65f, 0.72f, 0.78f), "brick");

        CreateObstacle(zone.transform, "Ambulance Bay", new Vector3(28f, 1.5f, 2f), new Vector3(10f, 3f, 4f), new Color(0.65f, 0.65f, 0.68f));
        CreateCarWreck(zone.transform, new Vector3(20f, 0.8f, 5f), new Color(0.72f, 0.72f, 0.75f));
        CreateObjectiveTerminal(zone.transform, "Medical Fridge", "Clinic", "clinic_meds", "Press E to secure medicine", "Emergency medicine recovered", new Vector3(30f, 1f, -12f), new Color(0.58f, 0.92f, 0.92f));

        // Mais suprimentos médicos
        CreateLootCache(zone.transform, "Pharmacy Shelf", "Clinic", "Meds", new Vector3(22f, 1f, -11f), new Color(0.57f, 0.88f, 0.75f), 1, 0, 25);
        CreateLootCache(zone.transform, "Treatment Room", "Clinic", "Meds", new Vector3(33f, 1f, -11f), new Color(0.57f, 0.88f, 0.75f), 1, 0, 25);
        CreateLootCache(zone.transform, "Security Box", "Clinic", "Ammo", new Vector3(28f, 1f, -3f), new Color(0.48f, 0.72f, 0.86f), 1, 10, 0);
        CreateLootCache(zone.transform, "Pharmacy Stock", "Clinic", "Meds", new Vector3(42f, 1f, -5f), new Color(0.47f, 0.78f, 0.65f), 2, 0, 40);
        CreateLootCache(zone.transform, "Doctor's Office", "Clinic", "Meds", new Vector3(38f, 1f, -15f), new Color(0.47f, 0.78f, 0.65f), 1, 0, 35);

        CreateZombie(zone.transform, "Clinic", new Vector3(18f, 1f, -6f), ZombieAI.ZombieVariant.Screamer);
        CreateZombie(zone.transform, "Clinic", new Vector3(34f, 1f, -1f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Clinic", new Vector3(38f, 1f, -18f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Clinic", new Vector3(24f, 1f, -18f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Clinic", new Vector3(45f, 1f, -2f), ZombieAI.ZombieVariant.Crawler);
    }

    static void CreateWarehouseZone()
    {
        if (GameObject.Find("Warehouse") != null)
        {
            return;
        }

        GameObject zone = new GameObject("Warehouse");
        CreateZoneTrigger(zone.transform, "Warehouse", new Vector3(-28f, 1.5f, 24f), new Vector3(50f, 3f, 50f));
        CreateZoneObjective(zone.transform, "Warehouse", "warehouse_parts", "Recover generator parts");

        // Edificio Principal
        CreateTexturedBuilding(zone.transform, "Warehouse Main", new Vector3(-28f, 0f, 24f), new Vector3(22f, 12f, 18f), new Color(0.39f, 0.42f, 0.46f), "concrete");
        
        // Contentores Extras
        CreateObstacle(zone.transform, "Container A", new Vector3(-18f, 1.2f, 28f), new Vector3(4f, 2.4f, 8f), new Color(0.7f, 0.36f, 0.24f), "MetalSheetTexture");
        CreateObstacle(zone.transform, "Container B", new Vector3(-38f, 1.2f, 21f), new Vector3(4f, 2.4f, 8f), new Color(0.2f, 0.46f, 0.57f), "MetalSheetTexture");
        CreateObstacle(zone.transform, "Container C", new Vector3(-42f, 1.2f, 30f), new Vector3(4f, 2.4f, 8f), new Color(0.25f, 0.35f, 0.25f), "MetalSheetTexture");
        
        CreateObjectiveTerminal(zone.transform, "Generator Crate", "Warehouse", "warehouse_parts", "Press E to secure generator parts", "Generator parts recovered", new Vector3(-28f, 1f, 25f), new Color(0.73f, 0.52f, 0.22f));

        // LOOTS
        CreateLootCache(zone.transform, "Tool Crate", "Warehouse", "Scrap", new Vector3(-30f, 1f, 20f), new Color(0.64f, 0.47f, 0.31f), 2, 0, 0);
        CreateLootCache(zone.transform, "Storage Shelf", "Warehouse", "Food", new Vector3(-25f, 1f, 28f), new Color(0.82f, 0.71f, 0.32f), 2, 0, 0);
        CreateLootCache(zone.transform, "Industrial Ammo", "Warehouse", "Ammo", new Vector3(-34f, 1f, 28f), new Color(0.48f, 0.72f, 0.86f), 2, 25, 0);
        CreateAmmoCrate(zone.transform, "Warehouse Armory", "Warehouse", new Vector3(-40f, 1f, 15f), new Vector3(-42f, 1f, 12f));

        // ZOMBIES
        CreateZombie(zone.transform, "Warehouse", new Vector3(-19f, 1f, 12f), ZombieAI.ZombieVariant.Tank);
        CreateZombie(zone.transform, "Warehouse", new Vector3(-32f, 1f, 14f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Warehouse", new Vector3(-36f, 1f, 31f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Warehouse", new Vector3(-20f, 1f, 34f), ZombieAI.ZombieVariant.Crawler);
        CreateZombie(zone.transform, "Warehouse", new Vector3(-45f, 1f, 25f), ZombieAI.ZombieVariant.Walker);
    }

    static void CreateFuelDepotZone()
    {
        if (GameObject.Find("Fuel Depot") != null)
        {
            return;
        }

        GameObject zone = new GameObject("Fuel Depot");
        CreateZoneTrigger(zone.transform, "Fuel Depot", new Vector3(30f, 1.5f, 24f), new Vector3(50f, 3f, 50f));
        CreateZoneObjective(zone.transform, "Fuel Depot", "fuel_power", "Restore extraction power");

        // Loja de Conveniencia
        CreateTexturedBuilding(zone.transform, "Depot Shop", new Vector3(38f, 0f, 24f), new Vector3(16f, 8f, 12f), new Color(0.56f, 0.34f, 0.29f), "brick");
        
        // Bombas e Tanques
        CreateObstacle(zone.transform, "Pump Island", new Vector3(24f, 1.5f, 24f), new Vector3(10f, 3f, 4f), new Color(0.53f, 0.54f, 0.56f), "MetalSheetTexture");
        CreateObstacle(zone.transform, "Fuel Tank A", new Vector3(26f, 2.5f, 34f), new Vector3(8f, 5f, 8f), new Color(0.29f, 0.34f, 0.37f), "MetalSheetTexture");
        CreateObstacle(zone.transform, "Fuel Tank B", new Vector3(12f, 2.5f, 38f), new Vector3(8f, 5f, 8f), new Color(0.29f, 0.34f, 0.37f), "MetalSheetTexture");

        CreateObjectiveTerminal(zone.transform, "Power Relay", "Fuel Depot", "fuel_power", "Press E to restore extraction power", "Extraction grid online", new Vector3(34f, 1f, 24f), new Color(0.86f, 0.7f, 0.2f), "Fuel", 1, true);

        // LOOTS
        CreateLootCache(zone.transform, "Cash Counter", "Fuel Depot", "Food", new Vector3(39f, 1f, 21f), new Color(0.82f, 0.71f, 0.32f), 1, 0, 0);
        CreateLootCache(zone.transform, "Service Shelf", "Fuel Depot", "Ammo", new Vector3(34f, 1f, 28f), new Color(0.48f, 0.72f, 0.86f), 2, 20, 0);
        CreateLootCache(zone.transform, "Fuel Canister", "Fuel Depot", "Fuel", new Vector3(41f, 1f, 27f), new Color(0.74f, 0.58f, 0.14f), 1, 0, 0);
        CreateLootCache(zone.transform, "Extra Fuel", "Fuel Depot", "Fuel", new Vector3(20f, 1f, 34f), new Color(0.74f, 0.58f, 0.14f), 1, 0, 0);

        // ZOMBIES
        CreateZombie(zone.transform, "Fuel Depot", new Vector3(18f, 1f, 21f), ZombieAI.ZombieVariant.Screamer);
        CreateZombie(zone.transform, "Fuel Depot", new Vector3(26f, 1f, 13f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Fuel Depot", new Vector3(41f, 1f, 33f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Fuel Depot", new Vector3(15f, 1f, 30f), ZombieAI.ZombieVariant.Walker);
    }

    static void CreateBuilding(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject building = new GameObject(name);
        building.transform.SetParent(parent);
        building.transform.position = position;

        CreateObstacle(building.transform, "Floor", position + new Vector3(0f, 0.1f, 0f), new Vector3(scale.x, 0.2f, scale.z), new Color(color.r * 0.65f, color.g * 0.65f, color.b * 0.65f), "GroundTexture");
        CreateObstacle(building.transform, "Back Wall", position + new Vector3(0f, scale.y * 0.5f, -scale.z * 0.5f), new Vector3(scale.x, scale.y, 0.4f), color, "BuildingBrickTexture");
        CreateObstacle(building.transform, "Left Wall", position + new Vector3(-scale.x * 0.5f, scale.y * 0.5f, 0f), new Vector3(0.4f, scale.y, scale.z), color, "BuildingBrickTexture");
        CreateObstacle(building.transform, "Right Wall", position + new Vector3(scale.x * 0.5f, scale.y * 0.5f, 0f), new Vector3(0.4f, scale.y, scale.z), color, "BuildingBrickTexture");
        CreateObstacle(building.transform, "Front Left", position + new Vector3(-scale.x * 0.28f, scale.y * 0.5f, scale.z * 0.5f), new Vector3(scale.x * 0.44f, scale.y, 0.4f), color, "BuildingBrickTexture");
    }

    static void CreateTexturedBuilding(Transform parent, string name, Vector3 position, Vector3 scale, Color color, string textureType)
    {
        GameObject building = new GameObject(name);
        building.transform.SetParent(parent);
        building.transform.position = position;

        // Escolhe a textura baseada no tipo
        string wallTexture = "BuildingBrickTexture";
        string floorTexture = "GroundTexture";
        Color roofColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);

        switch(textureType.ToLower())
        {
            case "brick":
                wallTexture = "BuildingBrickTexture";
                break;
            case "wood":
                wallTexture = "WoodPlanksTexture";
                roofColor = new Color(0.4f, 0.35f, 0.28f);
                break;
            case "concrete":
                wallTexture = "ConcreteTexture";
                break;
            case "metal":
                wallTexture = "MetalSheetTexture";
                roofColor = new Color(0.45f, 0.48f, 0.52f);
                break;
        }

        // Chao
        CreateObstacle(building.transform, "Floor", position + new Vector3(0f, 0.1f, 0f), new Vector3(scale.x, 0.2f, scale.z), new Color(color.r * 0.65f, color.g * 0.65f, color.b * 0.65f), floorTexture);

        // Paredes com textura
        CreateObstacle(building.transform, "Back Wall", position + new Vector3(0f, scale.y * 0.5f, -scale.z * 0.5f), new Vector3(scale.x, scale.y, 0.4f), color, wallTexture);
        CreateObstacle(building.transform, "Left Wall", position + new Vector3(-scale.x * 0.5f, scale.y * 0.5f, 0f), new Vector3(0.4f, scale.y, scale.z), color, wallTexture);
        CreateObstacle(building.transform, "Right Wall", position + new Vector3(scale.x * 0.5f, scale.y * 0.5f, 0f), new Vector3(0.4f, scale.y, scale.z), color, wallTexture);
        CreateObstacle(building.transform, "Front Left", position + new Vector3(-scale.x * 0.28f, scale.y * 0.5f, scale.z * 0.5f), new Vector3(scale.x * 0.44f, scale.y, 0.4f), color, wallTexture);
        CreateObstacle(building.transform, "Front Right", position + new Vector3(scale.x * 0.28f, scale.y * 0.5f, scale.z * 0.5f), new Vector3(scale.x * 0.44f, scale.y, 0.4f), color, wallTexture);
        CreateObstacle(building.transform, "Lintel", position + new Vector3(0f, scale.y - 1f, scale.z * 0.5f), new Vector3(scale.x * 0.16f, 2f, 0.4f), color, wallTexture);

        // Telhado
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(building.transform);
        roof.transform.position = position + new Vector3(0f, scale.y + 0.5f, 0f);
        roof.transform.localScale = new Vector3(scale.x + 1f, 1f, scale.z + 1f);
        SetRendererTexture(roof.GetComponent<Renderer>(), roofColor, "RoofTexture");

        // Janelas (decoração)
        CreateWindow(building.transform, "Window Left", position + new Vector3(-scale.x * 0.25f, scale.y * 0.6f, scale.z * 0.5f));
        CreateWindow(building.transform, "Window Right", position + new Vector3(scale.x * 0.25f, scale.y * 0.6f, scale.z * 0.5f));

        // Porta
        CreateDoor(building.transform, "Door", position + new Vector3(0f, scale.y * 0.25f, scale.z * 0.5f));
    }

    static void CreateWindow(Transform parent, string name, Vector3 position)
    {
        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = name;
        window.transform.SetParent(parent);
        window.transform.position = position;
        window.transform.localScale = new Vector3(1.5f, 1.2f, 0.2f);
        SetRendererTexture(window.GetComponent<Renderer>(), new Color(0.7f, 0.85f, 0.95f, 0.6f), "WindowTexture");
    }

    static void CreateDoor(Transform parent, string name, Vector3 position)
    {
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = name;
        door.transform.SetParent(parent);
        door.transform.position = position;
        door.transform.localScale = new Vector3(1.8f, 2.2f, 0.3f);
        SetRendererTexture(door.GetComponent<Renderer>(), new Color(0.35f, 0.28f, 0.22f), "WoodPlanksTexture");
    }

    static void CreateAmmoCrate(Transform parent, string name, string zoneName, Vector3 position, Vector3 secondaryPosition)
    {
        // Cria um conjunto de caixas de munição
        CreateLootCache(parent, name + " A", zoneName, "Ammo", position, new Color(0.28f, 0.42f, 0.28f), 1, 30, 0);
        CreateLootCache(parent, name + " B", zoneName, "Ammo", position + new Vector3(1.5f, 0f, 0.5f), new Color(0.28f, 0.42f, 0.28f), 1, 25, 0);

        // Adiciona caixas visuais extras
        GameObject crateA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crateA.name = "Ammo Box Visual";
        crateA.transform.SetParent(parent);
        crateA.transform.position = secondaryPosition;
        crateA.transform.localScale = new Vector3(0.8f, 0.5f, 1.2f);
        SetRendererTexture(crateA.GetComponent<Renderer>(), new Color(0.32f, 0.45f, 0.32f), "MetalSheetTexture");

        // Adiciona uma arma no topo
        CreateWeaponPickup(parent, "Rifle Pickup", zoneName, secondaryPosition + new Vector3(0f, 0.5f, 0f));
    }

    static void CreateWeaponPickup(Transform parent, string name, string zoneName, Vector3 position)
    {
        GameObject weapon = new GameObject(name);
        weapon.transform.SetParent(parent);
        weapon.transform.position = position;
        weapon.transform.localScale = Vector3.one;
        weapon.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
        
        CreateWeaponVisuals(weapon.transform, false);
        
        SetRendererTexture(weapon.GetComponentInChildren<Renderer>(), new Color(0.25f, 0.25f, 0.28f), "MetalSheetTexture");
    }

    static void CreateObstacle(Transform parent, string name, Vector3 position, Vector3 scale, Color color, string textureName = "")
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.SetParent(parent);
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        if (string.IsNullOrEmpty(textureName)) SetRendererTexture(obstacle.GetComponent<Renderer>(), color, "BuildingBrickTexture"); else SetRendererTexture(obstacle.GetComponent<Renderer>(), color, textureName);
    }

    static void CreateYardCover(Transform parent, Vector3 position, Vector3 scale)
    {
        CreateObstacle(parent, "Yard Cover", position, scale, new Color(0.36f, 0.25f, 0.21f));
    }

    static void CreateZombie(Transform parent, string zoneName, Vector3 position, ZombieAI.ZombieVariant variant)
    {
        GameObject zombie = new GameObject($"{zoneName} Zombie");
        zombie.name = $"{zoneName} Zombie";
        zombie.transform.SetParent(parent);
        zombie.transform.position = new Vector3(position.x, 1.05f, position.z);
        zombie.transform.localScale = Vector3.one;

        CapsuleCollider collider = zombie.AddComponent<CapsuleCollider>();
        collider.height = 1.8f;
        collider.radius = 0.35f;
        collider.center = new Vector3(0f, 0.9f, 0f);

        zombie.AddComponent<AudioSource>().playOnAwake = false;
        CreateZombieVisuals(zombie.transform);

        ZombieAI ai = zombie.AddComponent<ZombieAI>();
        ai.variant = variant;
        ai.moveSpeed = 1.55f;
        ai.detectionDistance = 12.5f;
        ai.attackDistance = 1.25f;
        ai.attackRate = 1.65f;
        ai.attackDamage = 4;

        ZombieHealth health = zombie.AddComponent<ZombieHealth>();
        health.zoneName = zoneName;
        health.maxHealth = 78;
        health.scoreOnDeath = 40;
    }

    static void CreateLootCache(
        Transform parent,
        string displayName,
        string zoneName,
        string supplyType,
        Vector3 position,
        Color color,
        int amount,
        int ammoReward,
        int healReward)
    {
        GameObject cache = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cache.name = displayName;
        cache.transform.SetParent(parent);
        cache.transform.position = position;
        cache.transform.localScale = new Vector3(1.4f, 1f, 1.4f);
        SetRendererTexture(cache.GetComponent<Renderer>(), color, "BuildingBrickTexture");

        LootContainer loot = cache.AddComponent<LootContainer>();
        loot.zoneName = zoneName;
        loot.displayName = displayName;
        loot.supplyType = supplyType;
        loot.amount = amount;
        loot.ammoReward = ammoReward;
        loot.healReward = healReward;
    }

    static void CreateZoneObjective(Transform parent, string zoneName, string objectiveId, string description)
    {
        GameManager.Instance?.RegisterObjective(zoneName, objectiveId, description);
    }

    static void CreateObjectiveTerminal(
        Transform parent,
        string name,
        string zoneName,
        string objectiveId,
        string prompt,
        string completionMessage,
        Vector3 position,
        Color color,
        string requiredResource = "",
        int requiredAmount = 0,
        bool consumesResource = false)
    {
        GameObject terminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        terminal.name = name;
        terminal.transform.SetParent(parent);
        terminal.transform.position = position;
        terminal.transform.localScale = new Vector3(1.1f, 1f, 1.1f);
        SetRendererTexture(terminal.GetComponent<Renderer>(), color, "BuildingBrickTexture");

        ObjectiveInteractable interactable = terminal.AddComponent<ObjectiveInteractable>();
        interactable.zoneName = zoneName;
        interactable.objectiveId = objectiveId;
        interactable.prompt = prompt;
        interactable.completionMessage = completionMessage;
        interactable.requiredResource = requiredResource;
        interactable.requiredAmount = requiredAmount;
        interactable.consumesResource = consumesResource;
    }

    static void CreateExtractionPoint()
    {
        if (GameObject.Find("Extraction Point") != null)
        {
            return;
        }

        GameObject extractionRoot = new GameObject("Extraction Point");
        CreateObstacle(extractionRoot.transform, "Extraction Pad", new Vector3(0f, 0.15f, -30f), new Vector3(6f, 0.3f, 6f), new Color(0.15f, 0.35f, 0.18f));
        CreateObstacle(extractionRoot.transform, "Beacon", new Vector3(0f, 2f, -30f), new Vector3(0.5f, 4f, 0.5f), new Color(0.75f, 0.85f, 0.95f));
        GameObject console = GameObject.CreatePrimitive(PrimitiveType.Cube);
        console.name = "Extraction Console";
        console.transform.SetParent(extractionRoot.transform);
        console.transform.position = new Vector3(0f, 1f, -27.5f);
        console.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        SetRendererColor(console.GetComponent<Renderer>(), new Color(0.26f, 0.5f, 0.64f));
        console.AddComponent<ExtractionZone>();
    }

    static void CreateCarWreck(Transform parent, Vector3 position, Color color)
    {
        CreateObstacle(parent, "Car Body", position, new Vector3(3.4f, 1.1f, 1.8f), color, "BuildingBrickTexture");
        CreateObstacle(parent, "Car Roof", position + new Vector3(0.2f, 0.85f, 0f), new Vector3(1.8f, 0.6f, 1.5f), new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f));
    }

    static void CreateSandbagLine(Transform parent, Vector3 startPosition, int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateObstacle(parent, $"Sandbag {i + 1}", startPosition + new Vector3(i * 1.2f - 2.4f, 0f, 0f), new Vector3(1f, 0.6f, 0.8f), new Color(0.45f, 0.39f, 0.27f));
        }
    }

    static void CreateZoneTrigger(Transform parent, string zoneName, Vector3 center, Vector3 size)
    {
        GameObject triggerObject = new GameObject($"{zoneName} Trigger");
        triggerObject.transform.SetParent(parent);
        triggerObject.transform.position = center;

        BoxCollider box = triggerObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;

        ZoneTrigger trigger = triggerObject.AddComponent<ZoneTrigger>();
        trigger.zoneName = zoneName;
    }

    static void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = GetOrCreateSharedMaterial(color, string.Empty);
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    static void SetRendererTexture(Renderer renderer, Color tint, string textureName)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = GetOrCreateSharedMaterial(tint, textureName);
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    static Material GetOrCreateSharedMaterial(Color color, string textureName)
    {
        string keyName = string.IsNullOrEmpty(textureName) ? "Plain" : textureName;
        Color32 c32 = color;
        string key = $"Mat_{keyName}_{c32.r}_{c32.g}_{c32.b}_{c32.a}";

        // Fast cache check
        foreach (var kvp in SharedMaterialCache)
        {
            if (kvp.Value != null && kvp.Value.name == key)
            {
                return kvp.Value;
            }
        }

        if (cachedSurfaceShader == null)
        {
            cachedSurfaceShader = Shader.Find("Universal Render Pipeline/Lit");
            if (cachedSurfaceShader == null) cachedSurfaceShader = Shader.Find("Standard");
        }

        if (cachedSurfaceShader == null) return null;

        Material material = new Material(cachedSurfaceShader);
        material.name = key;
        material.color = color;

        if (!string.IsNullOrEmpty(textureName))
        {
            Debug.Log($"[Bootstrap] Loading texture: {textureName}");
            Texture2D tex = Resources.Load<Texture2D>("Textures/" + textureName);
            if (tex == null) tex = Resources.Load<Texture2D>(textureName);

            if (tex != null)
            {
                material.mainTexture = tex;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", tex);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", tex);
                Debug.Log($"[Bootstrap] Successfully applied texture: {textureName}");
            }
            else
            {
                Debug.LogWarning($"[Bootstrap] FAILED to load texture: {textureName}. Check Assets/Resources/Textures/");
            }
        }

        // Use a dummy Color32 key for the dictionary since it's already defined as Dictionary<Color32, Material>
        // but we'll manually manage collisions if needed. 
        // For now, let's just stick it in with a deterministic key.
        SharedMaterialCache[new Color32(c32.r, c32.g, c32.b, (byte)(SharedMaterialCache.Count % 256))] = material;
        return material;
    }

    static void ApplyStylizedDecoration()
    {
#if UNITY_EDITOR
        ApplyTerrainLayersFromKit();
        SpawnStylizedNaturePrefabs();
#endif
    }

#if UNITY_EDITOR
    static void ApplyTerrainLayersFromKit()
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        TerrainLayer grass = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Proxy Games/Stylized Nature Kit Lite/Terrain/Grass.terrainlayer");
        TerrainLayer dirt = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Proxy Games/Stylized Nature Kit Lite/Terrain/Dirt.terrainlayer");
        TerrainLayer rock = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Proxy Games/Stylized Nature Kit Lite/Terrain/Rock.terrainlayer");
        TerrainLayer sand = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Proxy Games/Stylized Nature Kit Lite/Terrain/Sand.terrainlayer");

        List<TerrainLayer> layers = new List<TerrainLayer>();
        if (grass != null) layers.Add(grass);
        if (dirt != null) layers.Add(dirt);
        if (rock != null) layers.Add(rock);
        if (sand != null) layers.Add(sand);

        if (layers.Count > 0)
        {
            terrain.terrainData.terrainLayers = layers.ToArray();
        }

        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 10f;
        terrain.basemapDistance = 1800f;
    }

    static void SpawnStylizedNaturePrefabs()
    {
        if (GameObject.Find("Stylized Dressing") != null)
        {
            return;
        }

        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        GameObject dressingRoot = new GameObject("Stylized Dressing");

        string[] treePaths =
        {
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Trees/Spruce 1.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Trees/Spruce 2.prefab"
        };

        string[] rockPaths =
        {
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 1.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 2.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 3.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 4.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 5.prefab"
        };

        string[] foliagePaths =
        {
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Bush/Bush.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Flower/Flower.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Grass/Grass.prefab",
            "Assets/Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Mushroom/Mushrooms Patch.prefab"
        };

        PlacePrefabSet(treePaths, terrain, dressingRoot.transform, 140, 0.78f, 0.95f, 1.25f, disableColliders: true);
        PlacePrefabSet(rockPaths, terrain, dressingRoot.transform, 120, 0.68f, 0.85f, 1.35f, disableColliders: false);
        PlacePrefabSet(foliagePaths, terrain, dressingRoot.transform, 300, 0.82f, 0.7f, 1.15f, disableColliders: true);
    }

    static void PlacePrefabSet(
        string[] paths,
        Terrain terrain,
        Transform parent,
        int count,
        float minNormalY,
        float minScale,
        float maxScale,
        bool disableColliders)
    {
        if (paths == null || paths.Length == 0)
        {
            return;
        }

        List<GameObject> prefabs = new List<GameObject>();
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }

        if (prefabs.Count == 0)
        {
            return;
        }

        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        Random.InitState(9000 + count);
        int placed = 0;
        int maxAttempts = count * 20;
        for (int attempt = 0; attempt < maxAttempts && placed < count; attempt++)
        {
            float x = Random.Range(10f, data.size.x - 10f);
            float z = Random.Range(10f, data.size.z - 10f);
            float nx = data.size.x > 0f ? x / data.size.x : 0f;
            float nz = data.size.z > 0f ? z / data.size.z : 0f;
            Vector3 normal = data.GetInterpolatedNormal(nx, nz);
            if (normal.y < minNormalY)
            {
                continue;
            }

            float centerX = data.size.x * 0.5f;
            float centerZ = data.size.z * 0.5f;
            if (Mathf.Abs(x - centerX) < 8f || Mathf.Abs(z - centerZ) < 8f)
            {
                continue;
            }

            GameObject chosen = prefabs[Random.Range(0, prefabs.Count)];
            GameObject instance = PrefabUtility.InstantiatePrefab(chosen) as GameObject;
            if (instance == null)
            {
                continue;
            }

            instance.transform.SetParent(parent, true);
            float y = terrain.SampleHeight(new Vector3(terrainPos.x + x, 0f, terrainPos.z + z)) + terrainPos.y;
            instance.transform.position = new Vector3(terrainPos.x + x, y, terrainPos.z + z);
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.localScale *= Random.Range(minScale, maxScale);
            SnapInstanceToGround(instance, terrain);
            if (disableColliders)
            {
                DisableAllColliders(instance);
            }
            placed++;
        }
    }

    static void DisableAllColliders(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }

    static void SnapInstanceToGround(GameObject instance, Terrain terrain)
    {
        if (instance == null || terrain == null)
        {
            return;
        }

        Vector3 pos = instance.transform.position;
        float groundY = terrain.SampleHeight(pos) + terrain.transform.position.y;

        Bounds? bounds = null;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            if (!bounds.HasValue)
            {
                bounds = renderers[i].bounds;
            }
            else
            {
                Bounds b = bounds.Value;
                b.Encapsulate(renderers[i].bounds);
                bounds = b;
            }
        }

        float bottom = bounds.HasValue ? bounds.Value.min.y : pos.y;
        float delta = groundY - bottom;
        instance.transform.position = new Vector3(pos.x, pos.y + delta, pos.z);
    }
#endif

    static void CreateZombieVisuals(Transform root)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root, false);
        body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.92f, 0.34f);
        body.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        SetRendererTexture(body.GetComponent<Renderer>(), Color.white, "ZombieSkinTexture");
        DestroySafe(body.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root, false);
        head.transform.localPosition = new Vector3(0f, 1.6f, 0.03f);
        head.transform.localScale = new Vector3(0.34f, 0.38f, 0.34f);
        SetRendererTexture(head.GetComponent<Renderer>(), Color.white, "ZombieSkinTexture");
        DestroySafe(head.GetComponent<Collider>());

        GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftArm.name = "Left Arm";
        leftArm.transform.SetParent(root, false);
        leftArm.transform.localPosition = new Vector3(-0.42f, 1.2f, 0.35f);
        leftArm.transform.localScale = new Vector3(0.12f, 0.12f, 0.58f);
        leftArm.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        SetRendererTexture(leftArm.GetComponent<Renderer>(), Color.white, "ZombieSkinTexture");
        DestroySafe(leftArm.GetComponent<Collider>());

        GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightArm.name = "Right Arm";
        rightArm.transform.SetParent(root, false);
        rightArm.transform.localPosition = new Vector3(0.42f, 1.2f, 0.35f);
        rightArm.transform.localScale = new Vector3(0.12f, 0.12f, 0.58f);
        rightArm.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        SetRendererTexture(rightArm.GetComponent<Renderer>(), Color.white, "ZombieSkinTexture");
        DestroySafe(rightArm.GetComponent<Collider>());

        GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftLeg.name = "Left Leg";
        leftLeg.transform.SetParent(root, false);
        leftLeg.transform.localPosition = new Vector3(-0.15f, 0.34f, 0f);
        leftLeg.transform.localScale = new Vector3(0.14f, 0.66f, 0.14f);
        leftLeg.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);
        SetRendererColor(leftLeg.GetComponent<Renderer>(), new Color(0.09f, 0.11f, 0.12f));
        DestroySafe(leftLeg.GetComponent<Collider>());

        GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightLeg.name = "Right Leg";
        rightLeg.transform.SetParent(root, false);
        rightLeg.transform.localPosition = new Vector3(0.15f, 0.34f, 0f);
        rightLeg.transform.localScale = new Vector3(0.14f, 0.66f, 0.14f);
        rightLeg.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
        SetRendererColor(rightLeg.GetComponent<Renderer>(), new Color(0.09f, 0.11f, 0.12f));
        DestroySafe(rightLeg.GetComponent<Collider>());
    }

    static void CreateWeaponVisuals(Transform parent, bool isFpsView)
    {
        // Base de armas estilizada (composicao de primitivas)
        GameObject weaponBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weaponBody.name = "Body";
        weaponBody.transform.SetParent(parent, false);
        
        if (isFpsView)
        {
            weaponBody.transform.localPosition = new Vector3(0.45f, -0.35f, 0.7f);
            weaponBody.transform.localScale = new Vector3(0.12f, 0.18f, 0.45f);
        }
        else
        {
            weaponBody.transform.localPosition = Vector3.zero;
            weaponBody.transform.localScale = new Vector3(0.1f, 0.15f, 0.4f);
        }
        SetRendererTexture(weaponBody.GetComponent<Renderer>(), new Color(0.2f, 0.2f, 0.22f), "MetalSheetTexture");
        DestroySafe(weaponBody.GetComponent<Collider>());

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.SetParent(weaponBody.transform, false);
        barrel.transform.localPosition = new Vector3(0f, 0.25f, 1.2f);
        barrel.transform.localScale = new Vector3(0.35f, 1.1f, 0.35f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SetRendererTexture(barrel.GetComponent<Renderer>(), new Color(0.15f, 0.15f, 0.15f), "MetalSheetTexture");
        DestroySafe(barrel.GetComponent<Collider>());

        GameObject mag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mag.name = "Magazine";
        mag.transform.SetParent(weaponBody.transform, false);
        mag.transform.localPosition = new Vector3(0f, -0.6f, 0.2f);
        mag.transform.localScale = new Vector3(0.85f, 1.2f, 0.45f);
        SetRendererColor(mag.GetComponent<Renderer>(), new Color(0.1f, 0.1f, 0.1f));
        DestroySafe(mag.GetComponent<Collider>());

        GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.name = "Grip";
        grip.transform.SetParent(weaponBody.transform, false);
        grip.transform.localPosition = new Vector3(0f, -0.6f, -0.8f);
        grip.transform.localScale = new Vector3(0.9f, 0.8f, 0.4f);
        grip.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        SetRendererTexture(grip.GetComponent<Renderer>(), new Color(0.25f, 0.22f, 0.18f), "WoodPlanksTexture");
        DestroySafe(grip.GetComponent<Collider>());
    }

    static void DestroySafe(Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(obj);
        }
        else
        {
            Object.DestroyImmediate(obj);
        }
    }
}

static class RuntimeSceneValidator
{
    public static bool ValidateCurrentScene()
    {
        if (SceneManager.GetActiveScene().name != "Mapa_EXT01")
        {
            return true;
        }

        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        ValidateSingleton<GameManager>("GameManager", errors);
        ValidateSingleton<UIManager>("UIManager", errors);
        ValidateSingleton<GameOverScreen>("GameOverScreen", errors);
        ValidateSingleton<Crosshair>("Crosshair", errors);
        ValidateMinimumNamedObjectCount("HUD Canvas", 1, errors);
        ValidateMinimumNamedObjectCount("Crosshair Canvas", 1, errors);
        ValidateNamedObjectCount("Canvas", 0, warnings);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            errors.Add("Player with tag 'Player' was not found.");
        }
        else
        {
            ValidateComponent<CharacterController>(player, "CharacterController", errors);
            ValidateComponent<PlayerMovement>(player, "PlayerMovement", errors);
            ValidateComponent<PlayerHealth>(player, "PlayerHealth", errors);
            ValidateComponent<PlayerInventory>(player, "PlayerInventory", errors);
            ValidateComponent<PlayerInteractor>(player, "PlayerInteractor", errors);
            ValidateComponent<Shooting>(player, "Shooting", errors);
        }

        ValidateMinimumCount<ObjectiveInteractable>("ObjectiveInteractable", 1, errors);
        ValidateMinimumCount<ExtractionZone>("ExtractionZone", 1, errors);
        ValidateMinimumCount<ZoneTrigger>("ZoneTrigger", 5, errors);
        ValidateMinimumCount<LootContainer>("LootContainer", 1, errors);
        ValidateMinimumCount<ZombieAI>("ZombieAI", 1, warnings);

        if (warnings.Count > 0)
        {
            Debug.LogWarning("[RuntimeSceneValidator] Warnings:\n- " + string.Join("\n- ", warnings));
        }

        if (errors.Count > 0)
        {
            Debug.LogError("[RuntimeSceneValidator] Validation failed:\n- " + string.Join("\n- ", errors));
            return false;
        }

        Debug.Log("[RuntimeSceneValidator] Scene validation passed.");
        return true;
    }

    static void ValidateSingleton<T>(string label, List<string> errors) where T : Object
    {
        int count = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        if (count == 0)
        {
            errors.Add($"{label} is missing.");
            return;
        }

        if (count > 1)
        {
            errors.Add($"{label} has {count} active instances (expected exactly 1).");
        }
    }

    static void ValidateComponent<T>(GameObject target, string label, List<string> errors) where T : Component
    {
        if (target.GetComponent<T>() == null)
        {
            errors.Add($"Player is missing component: {label}.");
        }
    }

    static void ValidateMinimumCount<T>(string label, int minExpected, List<string> issues) where T : Component
    {
        int count = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        if (count < minExpected)
        {
            issues.Add($"{label} count is {count} (expected at least {minExpected}).");
        }
    }

    static void ValidateNamedObjectCount(string objectName, int expected, List<string> issues)
    {
        int count = CountActiveObjectsByName(objectName);
        if (count != expected)
        {
            issues.Add($"{objectName} count is {count} (expected exactly {expected}).");
        }
    }

    static void ValidateMinimumNamedObjectCount(string objectName, int minExpected, List<string> issues)
    {
        int count = CountActiveObjectsByName(objectName);
        if (count < minExpected)
        {
            issues.Add($"{objectName} count is {count} (expected at least {minExpected}).");
        }
    }

    static int CountActiveObjectsByName(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Transform[] sceneObjects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            Transform item = sceneObjects[i];
            if (item == null)
            {
                continue;
            }

            GameObject gameObject = item.gameObject;
            if (gameObject.scene == activeScene && gameObject.name == objectName)
            {
                count++;
            }
        }

        return count;
    }
}
 
