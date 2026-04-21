using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenWorldVisualBuilder : MonoBehaviour
{
    const string TargetSceneName = "Mapa_EXT01";
    const string GeneratedRootName = "Generated OpenWorld";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != TargetSceneName)
        {
            return;
        }

        // SceneBootstrapper now builds the final world; avoid duplicate runtime dressing.
        if (GameObject.Find("Prototype Scene Marker") != null || GameObject.Find("Prototype Environment") != null)
        {
            return;
        }

        if (FindFirstObjectByType<OpenWorldVisualBuilder>() != null)
        {
            return;
        }

        GameObject builderObject = new GameObject(nameof(OpenWorldVisualBuilder));
        builderObject.AddComponent<OpenWorldVisualBuilder>();
    }

    IEnumerator Start()
    {
        // Wait one frame so terrain data is ready.
        yield return null;
        BuildWorld();
    }

    void BuildWorld()
    {
        Terrain terrain = FindFirstObjectByType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning("[OpenWorldVisualBuilder] No terrain found.");
            return;
        }

        ApplyTerrainLook(terrain);
        BuildEnvironmentGeometry(terrain);
    }

    void ApplyTerrainLook(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        TerrainLayer[] currentLayers = data.terrainLayers;
        bool valid = currentLayers != null && currentLayers.Length >= 3;
        if (valid)
        {
            for (int i = 0; i < currentLayers.Length; i++)
            {
                if (currentLayers[i] == null || currentLayers[i].diffuseTexture == null)
                {
                    valid = false;
                    break;
                }
            }
        }

        if (!valid)
        {
            List<TerrainLayer> layers = new List<TerrainLayer>();
            TerrainLayer grass = CreateLayer("Grass", "Proxy Games/Stylized Nature Kit Lite/Textures/Terrain Grass.png", new Vector2(20f, 20f));
            TerrainLayer dirt = CreateLayer("Dirt", "Proxy Games/Stylized Nature Kit Lite/Textures/Terrain Dirt.png", new Vector2(20f, 20f));
            TerrainLayer rock = CreateLayer("Rock", "Proxy Games/Stylized Nature Kit Lite/Textures/Terrain Rock.png", new Vector2(26f, 26f));
            TerrainLayer sand = CreateLayer("Sand", "Proxy Games/Stylized Nature Kit Lite/Textures/Terrain Sand.png", new Vector2(28f, 28f));

            if (grass != null) layers.Add(grass);
            if (dirt != null) layers.Add(dirt);
            if (rock != null) layers.Add(rock);
            if (sand != null) layers.Add(sand);

            if (layers.Count > 0)
            {
                data.terrainLayers = layers.ToArray();
            }
        }

        PaintTerrain(terrain.terrainData);
        terrain.materialTemplate = null;
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 8f;
        terrain.basemapDistance = 2500f;
    }

    TerrainLayer CreateLayer(string layerName, string relativeAssetPath, Vector2 tileSize)
    {
        Texture2D texture = LoadTexture(relativeAssetPath);
        if (texture == null)
        {
            return null;
        }

        TerrainLayer layer = new TerrainLayer();
        layer.name = layerName;
        layer.diffuseTexture = texture;
        layer.tileSize = tileSize;
        return layer;
    }

    Texture2D LoadTexture(string relativeAssetPath)
    {
        string path = Path.Combine(Application.dataPath, relativeAssetPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        if (!texture.LoadImage(bytes, true))
        {
            DestroySafe(texture);
            return null;
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    void PaintTerrain(TerrainData data)
    {
        int layerCount = data.terrainLayers != null ? data.terrainLayers.Length : 0;
        if (layerCount == 0)
        {
            return;
        }

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        float[,,] map = new float[height, width, layerCount];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float xf = width > 1 ? (float)x / (width - 1) : 0f;
                float yf = height > 1 ? (float)y / (height - 1) : 0f;
                float h = Mathf.Clamp01(data.GetInterpolatedHeight(xf, yf) / Mathf.Max(1f, data.size.y));
                float slope = 1f - data.GetInterpolatedNormal(xf, yf).y;

                float grass = Mathf.Clamp01((1f - slope * 3f) * (1f - Mathf.Abs(h - 0.45f) * 1.8f));
                float dirt = Mathf.Clamp01((1f - Mathf.Abs(h - 0.3f) * 3.2f) * (1f - slope * 1.1f));
                float rock = Mathf.Clamp01(slope * 2.6f + h * 0.5f - 0.2f);
                float sand = Mathf.Clamp01((0.2f - h) * 6f) * (1f - slope);

                float[] weights = new float[layerCount];
                weights[0] = grass;
                if (layerCount > 1) weights[1] = dirt;
                if (layerCount > 2) weights[2] = rock;
                if (layerCount > 3) weights[3] = sand;

                float total = 0f;
                for (int i = 0; i < layerCount; i++)
                {
                    total += Mathf.Max(0f, weights[i]);
                }

                if (total < 0.0001f)
                {
                    weights[0] = 1f;
                    total = 1f;
                }

                for (int i = 0; i < layerCount; i++)
                {
                    map[y, x, i] = weights[i] / total;
                }
            }
        }

        data.SetAlphamaps(0, 0, map);
    }

    void BuildEnvironmentGeometry(Terrain terrain)
    {
        Transform existing = GameObject.Find(GeneratedRootName)?.transform;
        if (existing != null)
        {
            return;
        }

        GameObject rootObject = new GameObject(GeneratedRootName);
        Transform root = rootObject.transform;

        Transform foliageRoot = new GameObject("Foliage").transform;
        foliageRoot.SetParent(root, false);

        Transform rocksRoot = new GameObject("Rocks").transform;
        rocksRoot.SetParent(root, false);

        Transform settlementRoot = new GameObject("Settlement").transform;
        settlementRoot.SetParent(root, false);

        CreateRoads(terrain, settlementRoot);
        SpawnTrees(terrain, foliageRoot, 120);
        SpawnBushes(terrain, foliageRoot, 260);
        SpawnRocks(terrain, rocksRoot, 180);
        SpawnHouses(terrain, settlementRoot);
    }

    void CreateRoads(Terrain terrain, Transform parent)
    {
        TerrainData data = terrain.terrainData;
        Vector3 tpos = terrain.transform.position;
        float cx = tpos.x + data.size.x * 0.5f;
        float cz = tpos.z + data.size.z * 0.5f;

        CreateBox(
            parent,
            "Main Road",
            new Vector3(cx, SampleHeight(terrain, cx, cz), cz),
            new Vector3(data.size.x * 0.75f, 0.25f, 11f),
            new Color(0.18f, 0.18f, 0.19f));

        CreateBox(
            parent,
            "Cross Road",
            new Vector3(cx, SampleHeight(terrain, cx, cz), cz),
            new Vector3(11f, 0.25f, data.size.z * 0.75f),
            new Color(0.18f, 0.18f, 0.19f));
    }

    void SpawnTrees(Terrain terrain, Transform parent, int count)
    {
        SpawnScattered(
            terrain,
            parent,
            count,
            0.76f,
            (position, rotation, scale) =>
            {
                GameObject tree = new GameObject("Tree");
                tree.transform.SetPositionAndRotation(position, rotation);
                tree.transform.localScale = Vector3.one * scale;

                GameObject trunk = CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree.transform);
                trunk.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                trunk.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
                SetColor(trunk, new Color(0.31f, 0.21f, 0.12f));

                GameObject crown = CreatePrimitive(PrimitiveType.Sphere, "Crown", tree.transform);
                crown.transform.localPosition = new Vector3(0f, 3.0f, 0f);
                crown.transform.localScale = new Vector3(2.0f, 2.3f, 2.0f);
                SetColor(crown, new Color(0.21f, 0.49f, 0.22f));

                tree.transform.SetParent(parent, true);
            });
    }

    void SpawnBushes(Terrain terrain, Transform parent, int count)
    {
        SpawnScattered(
            terrain,
            parent,
            count,
            0.82f,
            (position, rotation, scale) =>
            {
                GameObject bush = CreatePrimitive(PrimitiveType.Sphere, "Bush", parent);
                bush.transform.SetPositionAndRotation(position, rotation);
                bush.transform.localScale = new Vector3(0.8f, 0.45f, 0.8f) * scale;
                SetColor(bush, new Color(0.24f, 0.56f, 0.25f));
            });
    }

    void SpawnRocks(Terrain terrain, Transform parent, int count)
    {
        SpawnScattered(
            terrain,
            parent,
            count,
            0.68f,
            (position, rotation, scale) =>
            {
                GameObject rock = CreatePrimitive(PrimitiveType.Cube, "Rock", parent);
                rock.transform.SetPositionAndRotation(position, rotation);
                rock.transform.localScale = new Vector3(
                    Random.Range(0.6f, 1.8f),
                    Random.Range(0.4f, 1.1f),
                    Random.Range(0.7f, 1.7f)) * scale;
                SetColor(rock, new Color(0.42f, 0.42f, 0.44f));
            });
    }

    void SpawnHouses(Terrain terrain, Transform parent)
    {
        TerrainData data = terrain.terrainData;
        Vector3 tpos = terrain.transform.position;
        float cx = tpos.x + data.size.x * 0.5f;
        float cz = tpos.z + data.size.z * 0.5f;

        Vector3[] offsets =
        {
            new Vector3(-38f, 0f, -22f), new Vector3(-18f, 0f, -24f), new Vector3(2f, 0f, -22f), new Vector3(24f, 0f, -26f),
            new Vector3(-42f, 0f, 20f),  new Vector3(-22f, 0f, 22f),  new Vector3(0f, 0f, 24f),   new Vector3(22f, 0f, 20f),
            new Vector3(-52f, 0f, -2f),  new Vector3(38f, 0f, -2f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 p = new Vector3(cx + offsets[i].x, 0f, cz + offsets[i].z);
            p.y = SampleHeight(terrain, p.x, p.z);
            BuildHouse(parent, p, Random.Range(0f, 360f), 1f + (i % 3) * 0.08f);
        }
    }

    void BuildHouse(Transform parent, Vector3 position, float yaw, float scale)
    {
        GameObject house = new GameObject("House");
        house.transform.SetParent(parent, true);
        house.transform.position = position;
        house.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        house.transform.localScale = Vector3.one * scale;

        GameObject body = CreatePrimitive(PrimitiveType.Cube, "Body", house.transform);
        body.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        body.transform.localScale = new Vector3(5.8f, 3f, 5f);
        SetColor(body, new Color(0.62f, 0.55f, 0.46f));

        GameObject roofLeft = CreatePrimitive(PrimitiveType.Cube, "Roof Left", house.transform);
        roofLeft.transform.localPosition = new Vector3(-1.5f, 3.5f, 0f);
        roofLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        roofLeft.transform.localScale = new Vector3(3.2f, 0.35f, 5.4f);
        SetColor(roofLeft, new Color(0.42f, 0.17f, 0.15f));

        GameObject roofRight = CreatePrimitive(PrimitiveType.Cube, "Roof Right", house.transform);
        roofRight.transform.localPosition = new Vector3(1.5f, 3.5f, 0f);
        roofRight.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        roofRight.transform.localScale = new Vector3(3.2f, 0.35f, 5.4f);
        SetColor(roofRight, new Color(0.42f, 0.17f, 0.15f));

        GameObject door = CreatePrimitive(PrimitiveType.Cube, "Door", house.transform);
        door.transform.localPosition = new Vector3(0f, 0.9f, 2.55f);
        door.transform.localScale = new Vector3(1.05f, 1.8f, 0.16f);
        SetColor(door, new Color(0.28f, 0.19f, 0.11f));

        GameObject windowLeft = CreatePrimitive(PrimitiveType.Cube, "Window Left", house.transform);
        windowLeft.transform.localPosition = new Vector3(-1.75f, 1.8f, 2.56f);
        windowLeft.transform.localScale = new Vector3(1.1f, 0.95f, 0.1f);
        SetColor(windowLeft, new Color(0.55f, 0.78f, 0.88f));

        GameObject windowRight = CreatePrimitive(PrimitiveType.Cube, "Window Right", house.transform);
        windowRight.transform.localPosition = new Vector3(1.75f, 1.8f, 2.56f);
        windowRight.transform.localScale = new Vector3(1.1f, 0.95f, 0.1f);
        SetColor(windowRight, new Color(0.55f, 0.78f, 0.88f));
    }

    void SpawnScattered(
        Terrain terrain,
        Transform parent,
        int count,
        float minNormalY,
        System.Action<Vector3, Quaternion, float> factory)
    {
        TerrainData data = terrain.terrainData;
        Vector3 tpos = terrain.transform.position;

        Random.InitState(20260419 + count);
        int placed = 0;
        int maxAttempts = count * 25;

        for (int attempt = 0; attempt < maxAttempts && placed < count; attempt++)
        {
            float x = Random.Range(8f, data.size.x - 8f);
            float z = Random.Range(8f, data.size.z - 8f);

            Vector3 world = new Vector3(tpos.x + x, 0f, tpos.z + z);
            world.y = terrain.SampleHeight(world) + tpos.y;

            float xf = data.size.x > 0f ? x / data.size.x : 0f;
            float zf = data.size.z > 0f ? z / data.size.z : 0f;
            Vector3 normal = data.GetInterpolatedNormal(xf, zf);
            if (normal.y < minNormalY)
            {
                continue;
            }

            if (Mathf.Abs(x - data.size.x * 0.5f) < 8f || Mathf.Abs(z - data.size.z * 0.5f) < 8f)
            {
                continue;
            }

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            float scale = Random.Range(0.82f, 1.25f);
            factory(world, rotation, scale);
            placed++;
        }
    }

    GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        return go;
    }

    void CreateBox(Transform parent, string name, Vector3 center, Vector3 size, Color color)
    {
        GameObject go = CreatePrimitive(PrimitiveType.Cube, name, parent);
        go.transform.position = center;
        go.transform.localScale = size;
        SetColor(go, color);
    }

    float SampleHeight(Terrain terrain, float x, float z)
    {
        Vector3 p = new Vector3(x, 0f, z);
        return terrain.SampleHeight(p) + terrain.transform.position.y;
    }

    void SetColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        mat.color = color;
        r.sharedMaterial = mat;
    }

    static void DestroySafe(Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }
}
