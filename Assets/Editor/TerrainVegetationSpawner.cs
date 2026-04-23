#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainVegetationSpawner : EditorWindow
{
    private Terrain terrain;
    private int seed = 42;

    // Contagens
    private int treeCount = 300;
    private int bushCount = 400;
    private int rockCount = 150;
    private int smallRockCount = 200;
    private int fernCount = 300;
    private int logStumpCount = 80;
    private int mushroomCount = 60;

    // Exclusao de agua
    private bool avoidWater = true;
    private float waterHeight = 3f;
    private float waterMargin = 5f;

    // Slope
    private float maxTreeSlope = 25f;
    private float maxRockSlope = 60f;

    private Vector2 scrollPos;
    private bool isSpawning = false;

    [MenuItem("Tools/Terrain Vegetation Spawner")]
    public static void ShowWindow()
    {
        GetWindow<TerrainVegetationSpawner>("Vegetation Spawner");
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("TERRAIN VEGETATION SPAWNER", EditorStyles.boldLabel);
        GUILayout.Label("Spawna vegetacao e rochas automaticamente", EditorStyles.miniLabel);
        EditorGUILayout.Space(10);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        seed = EditorGUILayout.IntField("Seed (aleatoriedade)", seed);

        EditorGUILayout.Space(8);
        GUILayout.Label("Contagens", EditorStyles.boldLabel);
        treeCount     = EditorGUILayout.IntSlider("Arvores grandes", treeCount, 0, 600);
        bushCount     = EditorGUILayout.IntSlider("Arbustos", bushCount, 0, 800);
        rockCount     = EditorGUILayout.IntSlider("Rochas grandes", rockCount, 0, 400);
        smallRockCount= EditorGUILayout.IntSlider("Rochas pequenas", smallRockCount, 0, 600);
        fernCount     = EditorGUILayout.IntSlider("Fetos", fernCount, 0, 600);
        logStumpCount = EditorGUILayout.IntSlider("Troncos e tocos", logStumpCount, 0, 200);
        mushroomCount = EditorGUILayout.IntSlider("Cogumelos", mushroomCount, 0, 200);

        EditorGUILayout.Space(8);
        GUILayout.Label("Opcoes", EditorStyles.boldLabel);
        avoidWater  = EditorGUILayout.Toggle("Evitar agua", avoidWater);
        if (avoidWater)
        {
            waterHeight = EditorGUILayout.FloatField("  Altura da agua (Y)", waterHeight);
            waterMargin = EditorGUILayout.FloatField("  Margem de exclusao (m)", waterMargin);
        }
        maxTreeSlope = EditorGUILayout.Slider("Inclinacao max arvores (graus)", maxTreeSlope, 5f, 60f);
        maxRockSlope = EditorGUILayout.Slider("Inclinacao max rochas (graus)", maxRockSlope, 5f, 90f);

        EditorGUILayout.Space(12);

        GUI.enabled = terrain != null && !isSpawning;
        if (GUILayout.Button("SPAWNAR VEGETACAO", GUILayout.Height(40)))
        {
            SpawnAll();
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Limpar tudo (desfaz)", GUILayout.Height(28)))
        {
            ClearAll();
        }
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    void SpawnAll()
    {
        if (terrain == null) { Debug.LogError("Seleciona um Terrain!"); return; }

        Undo.RegisterFullObjectHierarchyUndo(terrain.gameObject, "Spawn Vegetation");

        Random.InitState(seed);
        isSpawning = true;

        // Organizar em parents
        Transform vegetationParent = GetOrCreate("_Vegetation", terrain.transform.parent);
        Transform treesParent      = GetOrCreate("Trees",        vegetationParent);
        Transform bushParent       = GetOrCreate("Bushes",       vegetationParent);
        Transform rocksParent      = GetOrCreate("Rocks",        vegetationParent);
        Transform smallRocksParent = GetOrCreate("SmallRocks",   vegetationParent);
        Transform fernsParent      = GetOrCreate("Ferns",        vegetationParent);
        Transform logParent        = GetOrCreate("Logs_Stumps",  vegetationParent);
        Transform mushParent       = GetOrCreate("Mushrooms",    vegetationParent);

        // Prefabs — caminhos relativos a Assets/
        string[] treePrefabs = {
            "Tree_Packs/URP_Tree_Pack/Prefabs/URP_Tree_1.prefab",
            "Tree_Packs/URP_Tree_Pack/Prefabs/URP_Tree_2.prefab",
            "Tree_Packs/URP_Tree_Pack/Prefabs/URP_Tree_3.prefab",
            "ALP_Assets/Poplar Tree FREE/Prefabs/PoplarTree001_pr.prefab",
            "Rocks and Vegetation Pack/Prefabs/Maple Tree 1.prefab",
            "Rocks and Vegetation Pack/Prefabs/Maple Tree 2.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Trees/Spruce 1.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Trees/Spruce 2.prefab",
        };

        string[] bushPrefabs = {
            "Rocks and Vegetation Pack/Prefabs/Bush.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Bush/Bush.prefab",
        };

        string[] rockPrefabs = {
            "Rocks and Vegetation Pack/Prefabs/Rock 1.prefab",
            "Rocks and Vegetation Pack/Prefabs/Rock 2.prefab",
            "Rocks and Vegetation Pack/Prefabs/Rock 3.prefab",
            "Rocks and Vegetation Pack/Prefabs/Rock 4.prefab",
            "Rocks and Vegetation Pack/Prefabs/Rock 5.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 1.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Standard Rocks/Standard Rock 2.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Rock Cliffs/Rock Cliff 1.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Rock Cliffs/Rock Cliff 2.prefab",
        };

        string[] smallRockPrefabs = {
            "Rocks and Vegetation Pack/Prefabs/Rock 6.prefab",
            "Rocks and Vegetation Pack/Prefabs/Rock 7.prefab",
            "Rocks and Vegetation Pack/Prefabs/Rock 8.prefab",
            "Rocks and Vegetation Pack/Prefabs/Rock 9.prefab",
            "Rocks and Vegetation Pack/Prefabs/Rock 10.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Tiny Rocks/Tiny Rock 1.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Tiny Rocks/Tiny Rock 2.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Rocks/Tiny Rocks/Tiny Rock 3.prefab",
        };

        string[] fernPrefabs = {
            "Rocks and Vegetation Pack/Prefabs/Fern.prefab",
            "Rocks and Vegetation Pack/Prefabs/Fern_Tall.prefab",
            "Rocks and Vegetation Pack/Prefabs/Fern_Dry.prefab",
            "Rocks and Vegetation Pack/Prefabs/Fern_Tall_Dry.prefab",
        };

        string[] logPrefabs = {
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Log/Log.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Stump/Stump.prefab",
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Branch/Branch.prefab",
        };

        string[] mushPrefabs = {
            "Proxy Games/Stylized Nature Kit Lite/Prefabs/Foliage/Mushroom/Mushrooms Patch.prefab",
        };

        // Spawn de cada categoria
        SpawnGroup(treePrefabs,      treeCount,      treesParent,      maxTreeSlope, 0.8f,  1.4f,  true,  true);
        SpawnGroup(bushPrefabs,      bushCount,       bushParent,       maxTreeSlope, 0.6f,  1.2f,  true,  false);
        SpawnGroup(rockPrefabs,      rockCount,       rocksParent,      maxRockSlope, 0.8f,  2.0f,  false, false);
        SpawnGroup(smallRockPrefabs, smallRockCount,  smallRocksParent, maxRockSlope, 0.4f,  1.2f,  false, false);
        SpawnGroup(fernPrefabs,      fernCount,       fernsParent,      maxTreeSlope, 0.7f,  1.3f,  true,  false);
        SpawnGroup(logPrefabs,       logStumpCount,   logParent,        maxTreeSlope, 0.8f,  1.2f,  false, false);
        SpawnGroup(mushPrefabs,      mushroomCount,   mushParent,       maxTreeSlope, 0.6f,  1.0f,  true,  false);

        isSpawning = false;
        Debug.Log("Vegetacao spawned com sucesso!");
    }

    void SpawnGroup(string[] prefabPaths, int count, Transform parent,
                    float maxSlope, float minScale, float maxScale,
                    bool checkSlope, bool randomRotationY)
    {
        TerrainData td = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        List<GameObject> loadedPrefabs = new List<GameObject>();

        foreach (string path in prefabPaths)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + path);
            if (p != null) loadedPrefabs.Add(p);
        }

        if (loadedPrefabs.Count == 0)
        {
            Debug.LogWarning("Nenhum prefab encontrado para este grupo.");
            return;
        }

        int attempts = 0;
        int spawned = 0;

        while (spawned < count && attempts < count * 10)
        {
            attempts++;
            float normX = Random.value;
            float normZ = Random.value;
            float worldX = terrainPos.x + normX * td.size.x;
            float worldZ = terrainPos.z + normZ * td.size.z;
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

            // Evitar agua
            if (avoidWater && worldY < waterHeight + waterMargin) continue;

            // Verificar inclinacao
            if (checkSlope)
            {
                float slope = td.GetSteepness(normX, normZ);
                if (slope > maxSlope) continue;
            }

            // Escolher prefab aleatorio
            GameObject prefab = loadedPrefabs[Random.Range(0, loadedPrefabs.Count)];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);

            // Posicao com offset minimo do solo
            instance.transform.position = new Vector3(worldX, worldY, worldZ);

            // Rotacao aleatoria
            float rotY = randomRotationY ? Random.Range(0f, 360f) : Random.Range(0f, 360f);
            float rotX = Random.Range(-3f, 3f);
            float rotZ = Random.Range(-3f, 3f);
            instance.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);

            // Escala aleatoria
            float scale = Random.Range(minScale, maxScale);
            instance.transform.localScale = Vector3.one * scale;

            spawned++;
        }
    }

    void ClearAll()
    {
        Transform parent = terrain.transform.parent?.Find("_Vegetation");
        if (parent != null)
        {
            Undo.DestroyObjectImmediate(parent.gameObject);
            Debug.Log("Vegetacao removida.");
        }
    }

    Transform GetOrCreate(string name, Transform parent)
    {
        Transform existing = parent?.Find(name);
        if (existing != null) return existing;
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go.transform;
    }
}
#endif
