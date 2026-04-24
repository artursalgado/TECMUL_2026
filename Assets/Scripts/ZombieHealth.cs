using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        // PATCH: nunca criar primitivos se existir SkinnedMeshRenderer no prefab.
        // Só configura hit zones no visual importado.
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr != null)
        {
            EnsureBodyHitZone();
            EnsureHeadHitZoneOnImportedRig();
        }
        else
        {
            // Prefab não tem skin — loga erro e NÃO cria primitivos.
            Debug.LogError(
                $"[ZombieHealth] '{gameObject.name}': SkinnedMeshRenderer não encontrado. " +
                "Verifica que o zombiePrefab aponta para ZombieMale_AAB_URP.prefab. " +
                "Primitivos visuais foram DESATIVADOS neste build.", gameObject);
        }

        EnsureShadowDisc();
        EnsureHealthBar();
        RefreshHealthBar();
        RegisterWithGameManager();
    }

    void LateUpdate()
    {
        if (healthBarRoot == null) return;

        if (cachedCamera == null) cachedCamera = Camera.main;

        if (cachedCamera != null)
        {
            healthBarRoot.forward = cachedCamera.transform.forward;
            if (!isDead)
                healthBarRoot.gameObject.SetActive(currentHealth < maxHealth && Time.time <= healthBarVisibleUntil);
        }
    }

    void RegisterWithGameManager()
    {
        if (registered || !gameObject.activeInHierarchy || GameManager.Instance == null) return;
        registered = true;
        GameManager.Instance.RegisterZombie(zoneName);
    }

    public void TakeDamage(int damage) => ApplyHit(damage, false);

    public void ApplyHit(int damage, bool isHeadshot)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        healthBarVisibleUntil = Time.time + 1.8f;
        RefreshHealthBar();

        if (audioSource != null && damageClip != null)
            audioSource.PlayOneShot(damageClip);

        if (isHeadshot)
            UIManager.Instance?.ShowMessage("Headshot");
        else
            GetComponent<ZombieAI>()?.ApplyStun(0.12f);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;

        GameManager.Instance?.AddScore(scoreOnDeath);
        GameManager.Instance?.OnZombieKilled(zoneName);

        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.SetTrigger("Morrer");

        if (healthBarRoot != null) healthBarRoot.gameObject.SetActive(false);

        Destroy(gameObject, 1.2f);
    }

    public bool IsDead() => isDead;
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;


    void EnsureHeadHitZoneOnImportedRig()
    {
        Transform rigRoot = GetComponentInChildren<Animator>(true)?.transform;
        Transform headBone = FindBoneByName(rigRoot, "head")
            ?? FindBoneByName(transform, "head")
            ?? FindBoneByName(transform, "Head_01")
            ?? FindBoneByName(transform, "Head");

        Transform hitbox = transform.Find("Head Hitbox");
        if (hitbox == null)
        {
            GameObject go = new GameObject("Head Hitbox");
            go.transform.SetParent(transform, false);
            hitbox = go.transform;
        }

        if (headBone != null)
        {
            hitbox.position = headBone.position + headBone.up * 0.08f;
            hitbox.rotation = Quaternion.identity;
            hitbox.SetParent(transform, true);
        }
        else
        {
            hitbox.localPosition = new Vector3(0f, 1.62f, 0.06f);
            hitbox.localRotation = Quaternion.identity;
            Debug.LogWarning($"[ZombieHealth] '{gameObject.name}': headBone não encontrado no rig. Head hitbox posicionado manualmente.", gameObject);
        }

        SphereCollider sphere = hitbox.GetComponent<SphereCollider>();
        if (sphere == null) sphere = hitbox.gameObject.AddComponent<SphereCollider>();
        sphere.radius = 0.2f;
        sphere.center = Vector3.zero;

        ZombieHitZone zone = hitbox.GetComponent<ZombieHitZone>();
        if (zone == null) zone = hitbox.gameObject.AddComponent<ZombieHitZone>();
        zone.zombieHealth = this;
        zone.instantKill = true;
        zone.damageMultiplier = 2.2f;
    }

    Transform FindBoneByName(Transform root, string boneName)
    {
        if (root == null || string.IsNullOrEmpty(boneName)) return null;
        if (string.Equals(root.name, boneName, System.StringComparison.OrdinalIgnoreCase)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindBoneByName(root.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }

    void EnsureBodyHitZone()
    {
        ZombieHitZone zone = GetComponent<ZombieHitZone>();
        if (zone == null) zone = gameObject.AddComponent<ZombieHitZone>();
        zone.zombieHealth = this;
        zone.instantKill = false;
        zone.damageMultiplier = 1f;
    }

    void ConfigureMainCollider()
    {
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col == null) return;
        col.height = 1.28f;
        col.radius = 0.3f;
        col.center = new Vector3(0f, 0.68f, 0f);
    }


    void EnsureHealthBar()
    {
        Transform existing = transform.Find("Health Bar Root");
        if (existing != null)
        {
            healthBarRoot = existing;
            Transform fill = existing.Find("Canvas/Background/Fill");
            if (fill != null) healthBarFill = fill.GetComponent<Image>();
            return;
        }

        GameObject root = new GameObject("Health Bar Root");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, 2.15f, 0f);
        healthBarRoot = root.transform;

        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(root.transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 10;
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(64f, 12f);
        canvasRect.localScale = Vector3.one * 0.01f;

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(60f, 8f);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        healthBarFill = fillGO.AddComponent<Image>();
        healthBarFill.color = new Color(0.75f, 0.16f, 0.18f, 1f);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(1f, 0f);
        fillRect.sizeDelta = new Vector2(58f, -2f);
    }

    void RefreshHealthBar()
    {
        if (healthBarFill == null) return;
        float ratio = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        healthBarFill.rectTransform.sizeDelta = new Vector2(58f * Mathf.Clamp01(ratio), -2f);
        healthBarRoot.gameObject.SetActive(!isDead && currentHealth < maxHealth && Time.time <= healthBarVisibleUntil);
    }

    void EnsureShadowDisc()
    {
        if (transform.Find("Ground Shadow") != null) return;

        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shadow.name = "Ground Shadow";
        shadow.transform.SetParent(transform, false);
        shadow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        shadow.transform.localScale = new Vector3(0.55f, 0.01f, 0.55f);

        Renderer r = shadow.GetComponent<Renderer>();
        if (r != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(sh);
            mat.color = new Color(0f, 0f, 0f, 0.4f);
            r.sharedMaterial = mat;
        }

        Collider col = shadow.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }
}
