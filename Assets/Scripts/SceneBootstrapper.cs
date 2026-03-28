using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneBootstrapper
{
    public const int SceneVersion = 5;

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
        if (Object.FindFirstObjectByType<GameManager>() != null)
        {
            return false;
        }

        return SceneManager.GetActiveScene().name == "SampleScene";
    }

    public static void BuildPrototypeScene()
    {
        CreateSceneMarker();
        CreateWorld();
        GameObject player = CreatePlayer();
        UIManager uiManager = CreateUI();

        GameObject zombieTemplate = CreateZombieTemplate();
        GameManager gameManager = CreateGameManager(zombieTemplate, new List<Transform>());
        gameManager.autoSpawnWaves = false;

        if (uiManager != null)
        {
            uiManager.shooting = player.GetComponent<Shooting>();
        }

        CreateResidentialZone();
        CreateShelterZone();
        CreateClinicZone();
        CreateWarehouseZone();
        CreateFuelDepotZone();
        CreateExtractionPoint();
    }

    static void CreateSceneMarker()
    {
        GameObject markerObject = new GameObject("Prototype Scene Marker");
        PrototypeSceneMarker marker = markerObject.AddComponent<PrototypeSceneMarker>();
        marker.version = SceneVersion;
    }

    static void CreateWorld()
    {
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
            SetRendererColor(floorRenderer, new Color(0.2f, 0.24f, 0.2f));
        }
    }

    static void CreateOuterTerrain(Transform parent)
    {
        GameObject outerTerrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
        outerTerrain.name = "Outer Terrain";
        outerTerrain.transform.SetParent(parent);
        outerTerrain.transform.position = new Vector3(0f, -0.05f, 0f);
        outerTerrain.transform.localScale = new Vector3(40f, 1f, 40f);

        Renderer terrainRenderer = outerTerrain.GetComponent<Renderer>();
        if (terrainRenderer != null)
        {
            SetRendererColor(terrainRenderer, new Color(0.29f, 0.26f, 0.18f));
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
        SetRendererColor(wall.GetComponent<Renderer>(), color);
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
            SetRendererColor(renderer, new Color(0.17f, 0.17f, 0.18f));
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
        SetRendererColor(wall.GetComponent<Renderer>(), new Color(0.35f, 0.37f, 0.4f));
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
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 3f, -24f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        AudioSource audioSource = player.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        PlayerMovement movement = player.AddComponent<PlayerMovement>();
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        PlayerInventory inventory = player.AddComponent<PlayerInventory>();
        Shooting shooting = player.AddComponent<Shooting>();
        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();

        Camera camera = Camera.main;
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

        movement.playerCamera = camera.transform;
        movement.walkSpeed = 6f;
        movement.sprintSpeed = 9f;
        movement.jumpHeight = 1.1f;

        shooting.fpsCamera = camera;
        shooting.range = 120f;
        shooting.damage = 34;
        shooting.maxAmmo = 24;
        shooting.reloadTime = 1.6f;

        interactor.interactionCamera = camera;

        health.maxHealth = 100;
        health.canRegenerate = false;
        inventory.maxCarryWeight = 22f;
        inventory.ammoReserve = 36;
        inventory.medkits = 1;

        CreateCrosshair();
        return player;
    }

    static UIManager CreateUI()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        UIManager uiManager = canvasObject.AddComponent<UIManager>();

        uiManager.healthText = CreateText("Health Text", canvasObject.transform, new Vector2(18f, -14f), new Vector2(220f, 34f), 22).GetComponent<TextMeshProUGUI>();
        uiManager.healthBar = CreateHealthBar(canvasObject.transform);
        uiManager.ammoText = CreateText("Ammo Text", canvasObject.transform, new Vector2(-18f, -14f), new Vector2(220f, 34f), 22, TextAlignmentOptions.TopRight, new Vector2(1f, 1f)).GetComponent<TextMeshProUGUI>();
        uiManager.scoreText = CreateText("Score Text", canvasObject.transform, new Vector2(18f, -78f), new Vector2(220f, 32f), 18).GetComponent<TextMeshProUGUI>();
        uiManager.suppliesText = CreateText("Supplies Text", canvasObject.transform, new Vector2(18f, -106f), new Vector2(260f, 32f), 18).GetComponent<TextMeshProUGUI>();
        uiManager.inventoryText = CreateText("Inventory Text", canvasObject.transform, new Vector2(18f, -132f), new Vector2(560f, 32f), 16).GetComponent<TextMeshProUGUI>();
        uiManager.waveText = CreateText("Pressure Text", canvasObject.transform, new Vector2(-18f, -48f), new Vector2(220f, 32f), 18, TextAlignmentOptions.TopRight, new Vector2(1f, 1f)).GetComponent<TextMeshProUGUI>();
        uiManager.objectiveText = CreateText("Objective Text", canvasObject.transform, new Vector2(18f, 18f), new Vector2(620f, 76f), 18, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f)).GetComponent<TextMeshProUGUI>();
        uiManager.promptText = CreateText("Prompt Text", canvasObject.transform, new Vector2(0f, 40f), new Vector2(480f, 36f), 18, TextAlignmentOptions.Center, new Vector2(0.5f, 0f)).GetComponent<TextMeshProUGUI>();
        uiManager.messageText = CreateText("Message Text", canvasObject.transform, new Vector2(0f, -20f), new Vector2(520f, 38f), 22, TextAlignmentOptions.Top, new Vector2(0.5f, 1f)).GetComponent<TextMeshProUGUI>();
        uiManager.damageIndicator = CreateDamageIndicator(canvasObject.transform);

        GameObject gameOverPanel = CreatePanel(canvasObject.transform);
        uiManager.gameOverPanel = gameOverPanel;
        uiManager.finalScoreText = gameOverPanel.transform.Find("Final Score Text").GetComponent<TextMeshProUGUI>();

        return uiManager;
    }

    static Slider CreateHealthBar(Transform parent)
    {
        GameObject root = new GameObject("Health Bar");
        root.transform.SetParent(parent, false);
        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -46f);
        rect.sizeDelta = new Vector2(190f, 14f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.15f, 0.18f, 1f);

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Slider slider = root.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        return slider;
    }

    static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("Game Over Panel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 200f);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.82f);

        GameObject title = CreateText("Game Over Text", panel.transform, new Vector2(0f, 42f), new Vector2(360f, 54f), 36, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));
        title.GetComponent<TextMeshProUGUI>().text = "OVERWHELMED";

        GameObject finalScore = CreateText("Final Score Text", panel.transform, new Vector2(0f, -8f), new Vector2(320f, 42f), 24, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));
        finalScore.GetComponent<TextMeshProUGUI>().text = "Final Score: 0";

        GameObject subtitle = CreateText("Survival Text", panel.transform, new Vector2(0f, -56f), new Vector2(360f, 36f), 18, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));
        subtitle.GetComponent<TextMeshProUGUI>().text = "Scavenge smarter next run.";

        panel.SetActive(false);
        return panel;
    }

    static void CreateCrosshair()
    {
        GameObject canvasObject = new GameObject("Crosshair Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        GameObject horizontal = new GameObject("Crosshair Horizontal");
        horizontal.transform.SetParent(canvasObject.transform, false);
        RectTransform hRect = horizontal.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.5f, 0.5f);
        hRect.anchorMax = new Vector2(0.5f, 0.5f);
        hRect.sizeDelta = new Vector2(14f, 2f);
        horizontal.AddComponent<Image>().color = Color.white;

        GameObject vertical = new GameObject("Crosshair Vertical");
        vertical.transform.SetParent(canvasObject.transform, false);
        RectTransform vRect = vertical.AddComponent<RectTransform>();
        vRect.anchorMin = new Vector2(0.5f, 0.5f);
        vRect.anchorMax = new Vector2(0.5f, 0.5f);
        vRect.sizeDelta = new Vector2(2f, 14f);
        vertical.AddComponent<Image>().color = Color.white;
    }

    static Image CreateDamageIndicator(Transform parent)
    {
        GameObject overlay = new GameObject("Damage Indicator");
        overlay.transform.SetParent(parent, false);
        RectTransform rect = overlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlay.AddComponent<Image>();
        image.color = new Color(0.7f, 0.08f, 0.08f, 0f);
        overlay.SetActive(false);
        return image;
    }

    static GameObject CreateText(
        string objectName,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft,
        Vector2? anchor = null)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        Vector2 anchorValue = anchor ?? new Vector2(0f, 1f);
        rect.anchorMin = anchorValue;
        rect.anchorMax = anchorValue;
        rect.pivot = anchorValue;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.text = objectName.Replace(" Text", string.Empty);

        return textObject;
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

        zombieAI.moveSpeed = 1.6f;
        zombieAI.detectionDistance = 14f;
        zombieAI.attackDistance = 1.35f;
        zombieAI.attackRate = 1.4f;
        zombieAI.attackDamage = 6;

        zombieHealth.maxHealth = 100;
        zombieHealth.scoreOnDeath = 50;

        return zombie;
    }

    static GameManager CreateGameManager(GameObject zombieTemplate, List<Transform> spawnPoints)
    {
        GameObject gameManagerObject = new GameObject("GameManager");
        GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
        gameManager.zombiePrefab = zombieTemplate;
        gameManager.spawnPoints = spawnPoints;
        return gameManager;
    }

    static void CreateResidentialZone()
    {
        GameObject zone = new GameObject("Residential Block");
        CreateZoneTrigger(zone.transform, "Residential Block", new Vector3(-22f, 1.5f, -8f), new Vector3(34f, 3f, 26f));
        CreateZoneObjective(zone.transform, "Residential Block", "res_keys", "Recover the shelter key");

        CreateBuilding(zone.transform, "House A", new Vector3(-32f, 0f, -8f), new Vector3(12f, 8f, 10f), new Color(0.61f, 0.52f, 0.44f));
        CreateBuilding(zone.transform, "House B", new Vector3(-16f, 0f, -8f), new Vector3(12f, 8f, 10f), new Color(0.52f, 0.57f, 0.45f));
        CreateBuilding(zone.transform, "House C", new Vector3(-24f, 0f, 8f), new Vector3(14f, 8f, 10f), new Color(0.48f, 0.46f, 0.56f));
        CreateYardCover(zone.transform, new Vector3(-24f, 1f, -20f), new Vector3(10f, 2f, 4f));
        CreateCarWreck(zone.transform, new Vector3(-8f, 0.8f, -9f), new Color(0.32f, 0.21f, 0.19f));

        CreateLootCache(zone.transform, "Pantry", "Residential Block", "Food", new Vector3(-32f, 1f, -11f), new Color(0.82f, 0.71f, 0.32f), 2, 0, 0);
        CreateLootCache(zone.transform, "Medicine Cabinet", "Residential Block", "Meds", new Vector3(-16f, 1f, -11f), new Color(0.57f, 0.88f, 0.75f), 1, 0, 20);
        CreateLootCache(zone.transform, "Backpack", "Residential Block", "Keys", new Vector3(-24f, 1f, 5f), new Color(0.64f, 0.47f, 0.31f), 1, 0, 0);
        CreateObjectiveTerminal(zone.transform, "Shelter Key Rack", "Residential Block", "res_keys", "Press E to secure the shelter key", "Recovered shelter key", new Vector3(-24f, 1f, 2f), new Color(0.65f, 0.59f, 0.22f));

        CreateZombie(zone.transform, "Residential Block", new Vector3(-28f, 1f, -18f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Residential Block", new Vector3(-21f, 1f, -2f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Residential Block", new Vector3(-10f, 1f, -15f), ZombieAI.ZombieVariant.Walker);
    }

    static void CreateShelterZone()
    {
        GameObject zone = new GameObject("Shelter Yard");
        CreateZoneTrigger(zone.transform, "Shelter Yard", new Vector3(0f, 1.5f, -6f), new Vector3(26f, 3f, 22f));
        CreateZoneObjective(zone.transform, "Shelter Yard", "shelter_gate", "Seal the shelter entrance");

        CreateBuilding(zone.transform, "Shelter", new Vector3(0f, 0f, -6f), new Vector3(18f, 8f, 12f), new Color(0.36f, 0.42f, 0.47f));
        CreateObstacle(zone.transform, "Barricade Left", new Vector3(-9f, 1.5f, 4f), new Vector3(5f, 3f, 2f), new Color(0.33f, 0.25f, 0.2f));
        CreateObstacle(zone.transform, "Barricade Right", new Vector3(9f, 1.5f, 4f), new Vector3(5f, 3f, 2f), new Color(0.33f, 0.25f, 0.2f));
        CreateSandbagLine(zone.transform, new Vector3(0f, 0.6f, 8f), 5);
        CreateObjectiveTerminal(zone.transform, "Shelter Gate Console", "Shelter Yard", "shelter_gate", "Press E to seal shelter gate", "Shelter entrance secured", new Vector3(0f, 1f, 1f), new Color(0.4f, 0.58f, 0.66f), "Keys", 1, false);

        CreateLootCache(zone.transform, "Locker", "Shelter Yard", "Ammo", new Vector3(-4f, 1f, -4f), new Color(0.48f, 0.72f, 0.86f), 1, 12, 0);
        CreateLootCache(zone.transform, "Shelter Cabinet", "Shelter Yard", "Food", new Vector3(3f, 1f, -4f), new Color(0.82f, 0.71f, 0.32f), 2, 0, 0);

        CreateZombie(zone.transform, "Shelter Yard", new Vector3(-11f, 1f, 8f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Shelter Yard", new Vector3(12f, 1f, 9f), ZombieAI.ZombieVariant.Crawler);
        CreateZombie(zone.transform, "Shelter Yard", new Vector3(0f, 1f, 8f), ZombieAI.ZombieVariant.Runner);
    }

    static void CreateClinicZone()
    {
        GameObject zone = new GameObject("Clinic");
        CreateZoneTrigger(zone.transform, "Clinic", new Vector3(28f, 1.5f, -10f), new Vector3(24f, 3f, 28f));
        CreateZoneObjective(zone.transform, "Clinic", "clinic_meds", "Find emergency medicine");

        CreateBuilding(zone.transform, "Clinic Building", new Vector3(28f, 0f, -10f), new Vector3(18f, 8f, 16f), new Color(0.76f, 0.78f, 0.82f));
        CreateObstacle(zone.transform, "Ambulance Bay", new Vector3(28f, 1.5f, 2f), new Vector3(10f, 3f, 4f), new Color(0.65f, 0.65f, 0.68f));
        CreateObjectiveTerminal(zone.transform, "Medical Fridge", "Clinic", "clinic_meds", "Press E to secure medicine", "Emergency medicine recovered", new Vector3(30f, 1f, -12f), new Color(0.58f, 0.92f, 0.92f));

        CreateLootCache(zone.transform, "Pharmacy Shelf", "Clinic", "Meds", new Vector3(22f, 1f, -11f), new Color(0.57f, 0.88f, 0.75f), 1, 0, 25);
        CreateLootCache(zone.transform, "Treatment Room", "Clinic", "Meds", new Vector3(33f, 1f, -11f), new Color(0.57f, 0.88f, 0.75f), 1, 0, 25);
        CreateLootCache(zone.transform, "Security Box", "Clinic", "Ammo", new Vector3(28f, 1f, -3f), new Color(0.48f, 0.72f, 0.86f), 1, 10, 0);

        CreateZombie(zone.transform, "Clinic", new Vector3(18f, 1f, -6f), ZombieAI.ZombieVariant.Screamer);
        CreateZombie(zone.transform, "Clinic", new Vector3(34f, 1f, -1f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Clinic", new Vector3(38f, 1f, -18f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Clinic", new Vector3(24f, 1f, -18f), ZombieAI.ZombieVariant.Walker);
    }

    static void CreateWarehouseZone()
    {
        GameObject zone = new GameObject("Warehouse");
        CreateZoneTrigger(zone.transform, "Warehouse", new Vector3(-28f, 1.5f, 24f), new Vector3(26f, 3f, 28f));
        CreateZoneObjective(zone.transform, "Warehouse", "warehouse_parts", "Recover generator parts");

        CreateBuilding(zone.transform, "Warehouse Building", new Vector3(-28f, 0f, 24f), new Vector3(22f, 10f, 18f), new Color(0.39f, 0.42f, 0.46f));
        CreateObstacle(zone.transform, "Container A", new Vector3(-18f, 1.2f, 28f), new Vector3(4f, 2.4f, 8f), new Color(0.7f, 0.36f, 0.24f));
        CreateObstacle(zone.transform, "Container B", new Vector3(-38f, 1.2f, 21f), new Vector3(4f, 2.4f, 8f), new Color(0.2f, 0.46f, 0.57f));
        CreateObjectiveTerminal(zone.transform, "Generator Crate", "Warehouse", "warehouse_parts", "Press E to secure generator parts", "Generator parts recovered", new Vector3(-28f, 1f, 25f), new Color(0.73f, 0.52f, 0.22f));

        CreateLootCache(zone.transform, "Tool Crate", "Warehouse", "Scrap", new Vector3(-30f, 1f, 20f), new Color(0.64f, 0.47f, 0.31f), 2, 0, 0);
        CreateLootCache(zone.transform, "Storage Shelf", "Warehouse", "Food", new Vector3(-25f, 1f, 28f), new Color(0.82f, 0.71f, 0.32f), 2, 0, 0);
        CreateLootCache(zone.transform, "Ammo Box", "Warehouse", "Ammo", new Vector3(-34f, 1f, 28f), new Color(0.48f, 0.72f, 0.86f), 1, 15, 0);

        CreateZombie(zone.transform, "Warehouse", new Vector3(-19f, 1f, 12f), ZombieAI.ZombieVariant.Tank);
        CreateZombie(zone.transform, "Warehouse", new Vector3(-32f, 1f, 14f), ZombieAI.ZombieVariant.Walker);
        CreateZombie(zone.transform, "Warehouse", new Vector3(-36f, 1f, 31f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Warehouse", new Vector3(-20f, 1f, 34f), ZombieAI.ZombieVariant.Crawler);
    }

    static void CreateFuelDepotZone()
    {
        GameObject zone = new GameObject("Fuel Depot");
        CreateZoneTrigger(zone.transform, "Fuel Depot", new Vector3(30f, 1.5f, 24f), new Vector3(28f, 3f, 28f));
        CreateZoneObjective(zone.transform, "Fuel Depot", "fuel_power", "Restore extraction power");

        CreateBuilding(zone.transform, "Depot Shop", new Vector3(38f, 0f, 24f), new Vector3(16f, 8f, 12f), new Color(0.56f, 0.34f, 0.29f));
        CreateObstacle(zone.transform, "Pump Island", new Vector3(24f, 1.5f, 24f), new Vector3(10f, 3f, 4f), new Color(0.53f, 0.54f, 0.56f));
        CreateObstacle(zone.transform, "Tank", new Vector3(26f, 1.5f, 34f), new Vector3(8f, 3f, 8f), new Color(0.29f, 0.34f, 0.37f));
        CreateObjectiveTerminal(zone.transform, "Power Relay", "Fuel Depot", "fuel_power", "Press E to restore extraction power", "Extraction grid online", new Vector3(34f, 1f, 24f), new Color(0.86f, 0.7f, 0.2f), "Fuel", 1, true);

        CreateLootCache(zone.transform, "Cash Counter", "Fuel Depot", "Food", new Vector3(39f, 1f, 21f), new Color(0.82f, 0.71f, 0.32f), 1, 0, 0);
        CreateLootCache(zone.transform, "Service Shelf", "Fuel Depot", "Ammo", new Vector3(34f, 1f, 28f), new Color(0.48f, 0.72f, 0.86f), 1, 10, 0);
        CreateLootCache(zone.transform, "Fuel Canister", "Fuel Depot", "Fuel", new Vector3(41f, 1f, 27f), new Color(0.74f, 0.58f, 0.14f), 1, 0, 0);

        CreateZombie(zone.transform, "Fuel Depot", new Vector3(18f, 1f, 21f), ZombieAI.ZombieVariant.Screamer);
        CreateZombie(zone.transform, "Fuel Depot", new Vector3(26f, 1f, 13f), ZombieAI.ZombieVariant.Runner);
        CreateZombie(zone.transform, "Fuel Depot", new Vector3(41f, 1f, 33f), ZombieAI.ZombieVariant.Walker);
    }

    static void CreateBuilding(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject building = new GameObject(name);
        building.transform.SetParent(parent);
        building.transform.position = position;

        CreateObstacle(building.transform, "Floor", position + new Vector3(0f, 0.1f, 0f), new Vector3(scale.x, 0.2f, scale.z), new Color(color.r * 0.65f, color.g * 0.65f, color.b * 0.65f));
        CreateObstacle(building.transform, "Back Wall", position + new Vector3(0f, scale.y * 0.5f, -scale.z * 0.5f), new Vector3(scale.x, scale.y, 0.4f), color);
        CreateObstacle(building.transform, "Left Wall", position + new Vector3(-scale.x * 0.5f, scale.y * 0.5f, 0f), new Vector3(0.4f, scale.y, scale.z), color);
        CreateObstacle(building.transform, "Right Wall", position + new Vector3(scale.x * 0.5f, scale.y * 0.5f, 0f), new Vector3(0.4f, scale.y, scale.z), color);
        CreateObstacle(building.transform, "Front Left", position + new Vector3(-scale.x * 0.28f, scale.y * 0.5f, scale.z * 0.5f), new Vector3(scale.x * 0.44f, scale.y, 0.4f), color);
        CreateObstacle(building.transform, "Front Right", position + new Vector3(scale.x * 0.28f, scale.y * 0.5f, scale.z * 0.5f), new Vector3(scale.x * 0.44f, scale.y, 0.4f), color);
        CreateObstacle(building.transform, "Lintel", position + new Vector3(0f, scale.y - 1f, scale.z * 0.5f), new Vector3(scale.x * 0.16f, 2f, 0.4f), color);
    }

    static void CreateObstacle(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.SetParent(parent);
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        SetRendererColor(obstacle.GetComponent<Renderer>(), color);
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
        ai.moveSpeed = 1.65f;
        ai.detectionDistance = 13f;
        ai.attackDistance = 1.35f;
        ai.attackRate = 1.5f;
        ai.attackDamage = 5;

        ZombieHealth health = zombie.AddComponent<ZombieHealth>();
        health.zoneName = zoneName;
        health.maxHealth = 85;
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
        SetRendererColor(cache.GetComponent<Renderer>(), color);

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
        SetRendererColor(terminal.GetComponent<Renderer>(), color);

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
        CreateObstacle(parent, "Car Body", position, new Vector3(3.4f, 1.1f, 1.8f), color);
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

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        renderer.sharedMaterial = material;
    }

    static void CreateZombieVisuals(Transform root)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root, false);
        body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.92f, 0.34f);
        body.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        SetRendererColor(body.GetComponent<Renderer>(), new Color(0.22f, 0.34f, 0.28f));
        Object.Destroy(body.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root, false);
        head.transform.localPosition = new Vector3(0f, 1.6f, 0.03f);
        head.transform.localScale = new Vector3(0.34f, 0.38f, 0.34f);
        SetRendererColor(head.GetComponent<Renderer>(), new Color(0.54f, 0.72f, 0.46f));
        Object.Destroy(head.GetComponent<Collider>());

        GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftArm.name = "Left Arm";
        leftArm.transform.SetParent(root, false);
        leftArm.transform.localPosition = new Vector3(-0.42f, 0.98f, 0.02f);
        leftArm.transform.localScale = new Vector3(0.12f, 0.58f, 0.12f);
        leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
        SetRendererColor(leftArm.GetComponent<Renderer>(), new Color(0.2f, 0.3f, 0.24f));
        Object.Destroy(leftArm.GetComponent<Collider>());

        GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightArm.name = "Right Arm";
        rightArm.transform.SetParent(root, false);
        rightArm.transform.localPosition = new Vector3(0.42f, 0.98f, 0.02f);
        rightArm.transform.localScale = new Vector3(0.12f, 0.58f, 0.12f);
        rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
        SetRendererColor(rightArm.GetComponent<Renderer>(), new Color(0.2f, 0.3f, 0.24f));
        Object.Destroy(rightArm.GetComponent<Collider>());

        GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftLeg.name = "Left Leg";
        leftLeg.transform.SetParent(root, false);
        leftLeg.transform.localPosition = new Vector3(-0.15f, 0.34f, 0f);
        leftLeg.transform.localScale = new Vector3(0.14f, 0.66f, 0.14f);
        leftLeg.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);
        SetRendererColor(leftLeg.GetComponent<Renderer>(), new Color(0.09f, 0.11f, 0.12f));
        Object.Destroy(leftLeg.GetComponent<Collider>());

        GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightLeg.name = "Right Leg";
        rightLeg.transform.SetParent(root, false);
        rightLeg.transform.localPosition = new Vector3(0.15f, 0.34f, 0f);
        rightLeg.transform.localScale = new Vector3(0.14f, 0.66f, 0.14f);
        rightLeg.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
        SetRendererColor(rightLeg.GetComponent<Renderer>(), new Color(0.09f, 0.11f, 0.12f));
        Object.Destroy(rightLeg.GetComponent<Collider>());
    }
}
