using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health")]
    [FormerlySerializedAs("vidaMaxima")]
    public int maxHealth = 100;

    [FormerlySerializedAs("pontosAoMorrer")]
    public int scoreOnDeath = 50;

    [Header("Zone")]
    public string zoneName = "Outskirts";

    [Header("Effects")]
    [FormerlySerializedAs("efeitoMorte")]
    public GameObject deathEffect;

    [FormerlySerializedAs("somMorte")]
    public AudioClip deathClip;

    [FormerlySerializedAs("somDano")]
    public AudioClip damageClip;

    public int currentHealth;
    private bool isDead = false;
    private AudioSource audioSource;
    private bool registered;
    private Transform healthBarRoot;
    private Image healthBarFill;
    private Camera cachedCamera;
    private float healthBarVisibleUntil;

    void Start()
    {
        maxHealth = Mathf.RoundToInt(maxHealth * GameConfig.ZombieHealthMultiplier);
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        ConfigureMainCollider();
        EnsureHumanoidVisual();
        EnsureHealthBar();
        RefreshHealthBar();
        RegisterWithGameManager();
    }

    void LateUpdate()
    {
        if (healthBarRoot == null)
        {
            return;
        }

        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera != null)
        {
            healthBarRoot.forward = cachedCamera.transform.forward;
            if (!isDead)
            {
                healthBarRoot.gameObject.SetActive(currentHealth < maxHealth && Time.time <= healthBarVisibleUntil);
            }
        }
    }

    void RegisterWithGameManager()
    {
        if (registered || !gameObject.activeInHierarchy || GameManager.Instance == null)
        {
            return;
        }

        registered = true;
        GameManager.Instance.RegisterZombie(zoneName);
    }

    public void TakeDamage(int damage)
    {
        ApplyHit(damage, false);
    }

    public void ApplyHit(int damage, bool isHeadshot)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        healthBarVisibleUntil = Time.time + 1.8f;
        RefreshHealthBar();

        if (audioSource != null && damageClip != null)
        {
            audioSource.PlayOneShot(damageClip);
        }

        if (isHeadshot)
        {
            UIManager.Instance?.ShowMessage("Headshot");
        }
        else
        {
            GetComponent<ZombieAI>()?.ApplyStun(0.12f);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        GameManager.Instance?.AddScore(scoreOnDeath);
        GameManager.Instance?.OnZombieKilled(zoneName);

        if (audioSource != null && deathClip != null)
        {
            audioSource.PlayOneShot(deathClip);
        }

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Morrer");
        }

        if (healthBarRoot != null)
        {
            healthBarRoot.gameObject.SetActive(false);
        }

        Destroy(gameObject, 1.2f);
    }

    public bool IsDead() => isDead;

    public int GetCurrentHealth() => currentHealth;

    public int GetMaxHealth() => maxHealth;

    void EnsureHumanoidVisual()
    {
        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        EnsurePart("Body", new PrimitiveSpec(PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(5f, 0f, 0f), new Vector3(0.7f, 0.92f, 0.34f), new Color(0.22f, 0.34f, 0.28f)));
        EnsurePart("Head", new PrimitiveSpec(PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0.03f), Vector3.zero, new Vector3(0.34f, 0.38f, 0.34f), new Color(0.54f, 0.72f, 0.46f)));
        EnsurePart("Left Arm", new PrimitiveSpec(PrimitiveType.Cube, new Vector3(-0.42f, 0.98f, 0.02f), new Vector3(0f, 0f, 16f), new Vector3(0.12f, 0.58f, 0.12f), new Color(0.2f, 0.3f, 0.24f)));
        EnsurePart("Right Arm", new PrimitiveSpec(PrimitiveType.Cube, new Vector3(0.42f, 0.98f, 0.02f), new Vector3(0f, 0f, -16f), new Vector3(0.12f, 0.58f, 0.12f), new Color(0.2f, 0.3f, 0.24f)));
        EnsurePart("Left Leg", new PrimitiveSpec(PrimitiveType.Cube, new Vector3(-0.15f, 0.34f, 0f), new Vector3(2f, 0f, 0f), new Vector3(0.14f, 0.66f, 0.14f), new Color(0.09f, 0.11f, 0.12f)));
        EnsurePart("Right Leg", new PrimitiveSpec(PrimitiveType.Cube, new Vector3(0.15f, 0.34f, 0f), new Vector3(-2f, 0f, 0f), new Vector3(0.14f, 0.66f, 0.14f), new Color(0.09f, 0.11f, 0.12f)));
        EnsureShadowDisc();
        EnsureHeadHitZone();
        EnsureBodyHitZone();
    }

    void EnsurePart(string partName, PrimitiveSpec spec)
    {
        Transform existingPart = transform.Find(partName);
        if (existingPart == null)
        {
            CreatePart(partName, spec);
            existingPart = transform.Find(partName);
        }

        existingPart.localPosition = spec.LocalPosition;
        existingPart.localEulerAngles = spec.LocalEulerAngles;
        existingPart.localScale = spec.LocalScale;

        Renderer renderer = existingPart.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(spec.Color, partName.Contains("Leg") ? "" : "ZombieSkinTexture");
        }
    }

    void CreatePart(string partName, PrimitiveSpec spec)
    {
        GameObject part = GameObject.CreatePrimitive(spec.Type);
        part.name = partName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = spec.LocalPosition;
        part.transform.localEulerAngles = spec.LocalEulerAngles;
        part.transform.localScale = spec.LocalScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(spec.Color, partName.Contains("Leg") ? "" : "ZombieSkinTexture");
        }

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    void EnsureHeadHitZone()
    {
        Transform hitbox = transform.Find("Head Hitbox");
        if (hitbox == null)
        {
            GameObject hitboxObject = new GameObject("Head Hitbox");
            hitboxObject.transform.SetParent(transform, false);
            hitbox = hitboxObject.transform;
        }

        hitbox.localPosition = new Vector3(0f, 1.62f, 0.06f);
        hitbox.localRotation = Quaternion.identity;

        SphereCollider sphere = hitbox.GetComponent<SphereCollider>();
        if (sphere == null)
        {
            sphere = hitbox.gameObject.AddComponent<SphereCollider>();
        }

        sphere.radius = 0.24f;
        sphere.center = Vector3.zero;

        ZombieHitZone hitZone = hitbox.GetComponent<ZombieHitZone>();
        if (hitZone == null)
        {
            hitZone = hitbox.gameObject.AddComponent<ZombieHitZone>();
        }

        hitZone.zombieHealth = this;
        hitZone.instantKill = true;
        hitZone.damageMultiplier = 2.5f;
    }

    void EnsureBodyHitZone()
    {
        ZombieHitZone hitZone = GetComponent<ZombieHitZone>();
        if (hitZone == null)
        {
            hitZone = gameObject.AddComponent<ZombieHitZone>();
        }

        hitZone.zombieHealth = this;
        hitZone.instantKill = false;
        hitZone.damageMultiplier = 1f;
    }

    void ConfigureMainCollider()
    {
        CapsuleCollider mainCollider = GetComponent<CapsuleCollider>();
        if (mainCollider == null)
        {
            return;
        }

        mainCollider.height = 1.28f;
        mainCollider.radius = 0.3f;
        mainCollider.center = new Vector3(0f, 0.68f, 0f);
    }

    void EnsureHealthBar()
    {
        Transform existing = transform.Find("Health Bar Root");
        if (existing != null)
        {
            healthBarRoot = existing;
            Transform fill = existing.Find("Canvas/Background/Fill");
            if (fill != null)
            {
                healthBarFill = fill.GetComponent<Image>();
            }
            return;
        }

        GameObject root = new GameObject("Health Bar Root");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, 2.15f, 0f);
        healthBarRoot = root.transform;

        GameObject canvasObject = new GameObject("Canvas");
        canvasObject.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(64f, 12f);
        canvasRect.localScale = Vector3.one * 0.01f;

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(60f, 8f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(backgroundObject.transform, false);
        healthBarFill = fillObject.AddComponent<Image>();
        healthBarFill.color = new Color(0.75f, 0.16f, 0.18f, 1f);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(1f, 0f);
        fillRect.sizeDelta = new Vector2(58f, -2f);
    }

    void RefreshHealthBar()
    {
        if (healthBarFill == null)
        {
            return;
        }

        float ratio = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        RectTransform fillRect = healthBarFill.rectTransform;
        fillRect.sizeDelta = new Vector2(58f * Mathf.Clamp01(ratio), -2f);
        healthBarRoot.gameObject.SetActive(!isDead && currentHealth < maxHealth && Time.time <= healthBarVisibleUntil);
    }

    void EnsureShadowDisc()
    {
        if (transform.Find("Ground Shadow") != null)
        {
            return;
        }

        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shadow.name = "Ground Shadow";
        shadow.transform.SetParent(transform, false);
        shadow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        shadow.transform.localScale = new Vector3(0.55f, 0.01f, 0.55f);

        Renderer renderer = shadow.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(new Color(0f, 0f, 0f, 0.4f));
        }

        Collider collider = shadow.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    Material CreateMaterial(Color color, string textureName = "")
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        if (!string.IsNullOrEmpty(textureName))
        {
            Texture2D tex = Resources.Load<Texture2D>("Textures/" + textureName);
            if (tex != null)
            {
                material.SetTexture("_BaseMap", tex);
                material.SetTexture("_MainTex", tex);
            }
        }
        return material;
    }

    readonly struct PrimitiveSpec
    {
        public PrimitiveType Type { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalEulerAngles { get; }
        public Vector3 LocalScale { get; }
        public Color Color { get; }

        public PrimitiveSpec(PrimitiveType type, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, Color color)
        {
            Type = type;
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
            LocalScale = localScale;
            Color = color;
        }
    }
}
