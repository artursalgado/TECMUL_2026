using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Rendering;

public class Shooting : MonoBehaviour
{
    public enum WeaponMode
    {
        Pistol,
        Knife
    }

    public static Vector3 LastShotPosition { get; private set; }
    public static float LastShotTime { get; private set; }

    [Header("Weapon")]
    [FormerlySerializedAs("alcance")]
    public float range = 100f;

    [FormerlySerializedAs("dano")]
    public int damage = 25;

    [FormerlySerializedAs("cadenciaFogo")]
    public float fireRate = 0.2f;

    [FormerlySerializedAs("municaoMax")]
    public int maxAmmo = 30;

    [FormerlySerializedAs("tempoRecarga")]
    public float reloadTime = 2f;

    [Header("Knife")]
    public int knifeDamage = 40;
    public float knifeRange = 2.3f;
    public float knifeRate = 0.45f;
    public float knifeHitRadius = 0.32f;
    public float knifeStunDuration = 0.2f;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;

    [FormerlySerializedAs("impactoEfeito")]
    public GameObject impactEffect;

    [FormerlySerializedAs("audioDisparo")]
    public AudioSource shotAudioSource;

    [Header("Camera")]
    [FormerlySerializedAs("cam")]
    public Camera fpsCamera;

    private float nextShotTime = 0f;
    private int currentAmmo;
    private bool isReloading = false;
    private Transform weaponRoot;
    private Transform pistolRoot;
    private Transform knifeRoot;
    private Coroutine kickCoroutine;
    private PlayerInventory inventory;
    private PlayerMovement movement;
    private Crosshair cachedCrosshair;
    private Material tracerMaterial;
    private Material fallbackImpactMaterial;
    private float nextNoAmmoMessageTime;
    private float nextKnifeTime;
    private WeaponMode currentWeapon = WeaponMode.Pistol;

    void Start()
    {
        currentAmmo = maxAmmo;
        inventory = GetComponent<PlayerInventory>();
        movement = GetComponent<PlayerMovement>();
        cachedCrosshair = FindFirstObjectByType<Crosshair>();
        if (fpsCamera == null)
        {
            fpsCamera = Camera.main;
        }
        EnsureWeaponModel();
        EquipWeapon(WeaponMode.Pistol);
    }

    void Update()
    {
        if (fpsCamera == null)
        {
            fpsCamera = Camera.main;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipWeapon(WeaponMode.Pistol);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            EquipWeapon(WeaponMode.Pistol);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipWeapon(WeaponMode.Knife);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            EquipWeapon(WeaponMode.Knife);
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            EquipWeapon(currentWeapon == WeaponMode.Pistol ? WeaponMode.Knife : WeaponMode.Pistol);
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            EquipWeapon(currentWeapon == WeaponMode.Pistol ? WeaponMode.Knife : WeaponMode.Pistol);
        }
        else if (Input.GetMouseButtonDown(2))
        {
            EquipWeapon(currentWeapon == WeaponMode.Pistol ? WeaponMode.Knife : WeaponMode.Pistol);
        }
        else if (Input.mouseScrollDelta.y > 0.1f || Input.mouseScrollDelta.y < -0.1f)
        {
            EquipWeapon(currentWeapon == WeaponMode.Pistol ? WeaponMode.Knife : WeaponMode.Pistol);
        }

        if (currentWeapon == WeaponMode.Knife)
        {
            if (IsPrimaryFireHeld() && Time.time >= nextKnifeTime)
            {
                nextKnifeTime = Time.time + knifeRate;
                KnifeAttack();
            }

            return;
        }

        if (isReloading)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            if (Time.time >= nextNoAmmoMessageTime)
            {
                UIManager.Instance?.ShowMessage("Out of ammo - reloading");
                nextNoAmmoMessageTime = Time.time + 1.25f;
            }

            if (inventory == null || inventory.GetReserveAmmo() > 0)
            {
                StartCoroutine(Reload());
            }
            return;
        }

        if (IsPrimaryFireHeld() && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + fireRate;
            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
        }
    }

    void EquipWeapon(WeaponMode mode)
    {
        if (currentWeapon == mode)
        {
            return;
        }

        currentWeapon = mode;
        if (currentWeapon == WeaponMode.Knife)
        {
            isReloading = false;
            UIManager.Instance?.ShowMessage("Weapon: Knife");
        }
        else
        {
            UIManager.Instance?.ShowMessage("Weapon: Pistol");
        }

        RefreshWeaponVisualMode();
    }

    void Shoot()
    {
        if (fpsCamera == null)
        {
            return;
        }

        currentAmmo--;
        PlayWeaponKick();

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (shotAudioSource != null)
        {
            shotAudioSource.Play();
        }

        Vector3 viewportPoint = new Vector3(0.5f, 0.5f, 0f);
        float spread = 0.002f + (movement != null ? movement.GetMovementInputMagnitude() * 0.01f : 0f);
        if (movement != null && movement.IsCrouching())
        {
            spread *= 0.35f;
        }

        viewportPoint.x += Random.Range(-spread, spread);
        viewportPoint.y += Random.Range(-spread, spread);

        Ray ray = fpsCamera.ViewportPointToRay(viewportPoint);
        Vector3 tracerEnd = fpsCamera.transform.position + fpsCamera.transform.forward * range;
        LastShotPosition = transform.position;
        LastShotTime = Time.time;
        if (Physics.SphereCast(ray, 0.07f, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            tracerEnd = hit.point;
            bool hitZombie = false;

            ZombieHitZone hitZone = hit.collider.GetComponent<ZombieHitZone>();
            if (hitZone != null)
            {
                hitZone.ApplyHit(damage);
                hitZombie = true;
            }
            else
            {
                ZombieHealth zombieHealth = hit.transform.GetComponentInParent<ZombieHealth>();
                if (zombieHealth != null)
                {
                    zombieHealth.TakeDamage(damage);
                    hitZombie = true;
                }
            }

            if (hitZombie)
            {
                EnsureCrosshairReference();
                if (cachedCrosshair != null)
                {
                    cachedCrosshair.FlashHit();
                }
            }

            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 1f);
            }
            else
            {
                CreateFallbackImpact(hit.point, hit.normal);
            }
        }

        CreateTracer(tracerEnd);
    }

    void KnifeAttack()
    {
        if (fpsCamera == null)
        {
            return;
        }

        Vector3 origin = fpsCamera.transform.position;
        Vector3 dir = fpsCamera.transform.forward;
        bool hitZombie = false;

        if (Physics.SphereCast(origin, knifeHitRadius, dir, out RaycastHit hit, knifeRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            ZombieHitZone hitZone = hit.collider.GetComponent<ZombieHitZone>();
            if (hitZone != null)
            {
                hitZone.ApplyHit(knifeDamage);
                hitZone.zombieHealth?.GetComponent<ZombieAI>()?.ApplyStun(knifeStunDuration);
                hitZombie = true;
            }
            else
            {
                ZombieHealth zombieHealth = hit.transform.GetComponentInParent<ZombieHealth>();
                if (zombieHealth != null)
                {
                    zombieHealth.TakeDamage(knifeDamage);
                    zombieHealth.GetComponent<ZombieAI>()?.ApplyStun(knifeStunDuration);
                    hitZombie = true;
                }
            }

            if (hitZombie)
            {
                EnsureCrosshairReference();
                cachedCrosshair?.FlashHit();
                CreateFallbackImpact(hit.point, hit.normal);
            }
        }

        PlayWeaponKick();
    }

    IEnumerator Reload()
    {
        if (isReloading)
        {
            yield break;
        }

        isReloading = true;
        UIManager.Instance?.ShowMessage("Reloading...");
        yield return new WaitForSeconds(reloadTime);
        int ammoNeeded = maxAmmo - currentAmmo;
        if (ammoNeeded > 0 && inventory != null)
        {
            int ammoToLoad = Mathf.Min(ammoNeeded, inventory.GetReserveAmmo());
            if (ammoToLoad <= 0)
            {
                UIManager.Instance?.ShowMessage("No reserve ammo");
                isReloading = false;
                yield break;
            }

            inventory.TryConsumeReserveAmmo(ammoToLoad);
            currentAmmo += ammoToLoad;
        }
        else
        {
            currentAmmo = maxAmmo;
        }

        isReloading = false;
        if (currentAmmo > 0)
        {
            UIManager.Instance?.ShowMessage("Reload complete");
        }
    }

    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
    }

    public int GetCurrentAmmo() => currentAmmo;

    public int GetMaxAmmo() => maxAmmo;

    public int GetReserveAmmo() => inventory != null ? inventory.GetReserveAmmo() : 0;

    public bool IsReloading() => isReloading;
    public bool IsUsingKnife() => currentWeapon == WeaponMode.Knife;
    public string GetWeaponLabel() => currentWeapon == WeaponMode.Knife ? "KNIFE" : "PISTOL";

    bool IsPrimaryFireHeld()
    {
        bool mouseHeld = Input.GetMouseButton(0);
        bool fireButtonHeld = false;
        try
        {
            fireButtonHeld = Input.GetButton("Fire1");
        }
        catch
        {
            fireButtonHeld = false;
        }

        return mouseHeld || fireButtonHeld;
    }

    void EnsureWeaponModel()
    {
        if (fpsCamera == null) return;

        // --- Limpa modelo anterior se existir ---
        Transform existing = fpsCamera.transform.Find("Weapon Model");
        if (existing != null)
        {
            weaponRoot  = existing;
            pistolRoot  = existing.Find("Pistol");
            knifeRoot   = existing.Find("Knife");
            if (pistolRoot == null)
            {
                pistolRoot = new GameObject("Pistol").transform;
                pistolRoot.SetParent(weaponRoot, false);
            }

            // Force upgrade old primitive pistol to asset model when available.
            if (pistolRoot.Find("WeaponAssetModel") == null)
            {
                for (int i = pistolRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = pistolRoot.GetChild(i);
                    if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject);
                }

                bool assetLoadedExisting = TryLoadWeaponAsset(pistolRoot);
                if (!assetLoadedExisting)
                {
                    CreateWeaponPart("Body",   pistolRoot, new Vector3(0f, -0.01f,  0f),   Vector3.zero,          new Vector3(0.22f, 0.15f, 0.50f), new Color(0.10f, 0.11f, 0.13f), "MetalRustyTexture");
                    CreateWeaponPart("Barrel", pistolRoot, new Vector3(0f,  0.02f,  0.33f),Vector3.zero,          new Vector3(0.05f, 0.05f, 0.30f), new Color(0.20f, 0.20f, 0.22f), "MetalRustyTexture");
                    CreateWeaponPart("Slide",  pistolRoot, new Vector3(0f,  0.06f,  0.05f),Vector3.zero,          new Vector3(0.18f, 0.07f, 0.32f), new Color(0.23f, 0.23f, 0.24f), "MetalRustyTexture");
                    CreateWeaponPart("Grip",   pistolRoot, new Vector3(0f, -0.16f, -0.08f),new Vector3(22f,0f,0f),new Vector3(0.10f, 0.22f, 0.10f), new Color(0.30f, 0.20f, 0.10f));
                }
            }

            if (knifeRoot == null) knifeRoot = CreateKnifeModel(weaponRoot);
            RefreshWeaponVisualMode();
            return;
        }

        // --- Raiz do modelo ---
        GameObject root = new GameObject("Weapon Model");
        root.transform.SetParent(fpsCamera.transform, false);
        root.transform.localPosition = new Vector3(0.18f, -0.22f, 0.38f);
        root.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);
        root.transform.localScale    = Vector3.one;
        weaponRoot = root.transform;

        // --- Pistol: tenta carregar M1911 do asset ---
        pistolRoot = new GameObject("Pistol").transform;
        pistolRoot.SetParent(weaponRoot, false);

        bool assetLoaded = TryLoadWeaponAsset(pistolRoot);
        if (!assetLoaded)
        {
            // Fallback: pistola primitiva original
            CreateWeaponPart("Body",   pistolRoot, new Vector3(0f, -0.01f,  0f),   Vector3.zero,          new Vector3(0.22f, 0.15f, 0.50f), new Color(0.10f, 0.11f, 0.13f), "MetalRustyTexture");
            CreateWeaponPart("Barrel", pistolRoot, new Vector3(0f,  0.02f,  0.33f),Vector3.zero,          new Vector3(0.05f, 0.05f, 0.30f), new Color(0.20f, 0.20f, 0.22f), "MetalRustyTexture");
            CreateWeaponPart("Slide",  pistolRoot, new Vector3(0f,  0.06f,  0.05f),Vector3.zero,          new Vector3(0.18f, 0.07f, 0.32f), new Color(0.23f, 0.23f, 0.24f), "MetalRustyTexture");
            CreateWeaponPart("Grip",   pistolRoot, new Vector3(0f, -0.16f, -0.08f),new Vector3(22f,0f,0f),new Vector3(0.10f, 0.22f, 0.10f), new Color(0.30f, 0.20f, 0.10f));
        }

        knifeRoot = CreateKnifeModel(weaponRoot);
        RefreshWeaponVisualMode();
    }

    // Carrega o prefab M1911 (ou qualquer arma do Low Poly Weapons VOL.1) e instancia como filho do parent.
    // Retorna true se conseguiu carregar.
    bool TryLoadWeaponAsset(Transform parent)
    {
        // Prefabs por ordem de preferência
        string[] candidatePaths =
        {
            "Assets/Low Poly Weapons VOL.1/Prefabs/M1911.prefab",
            "Assets/Low Poly Weapons VOL.1/Prefabs/M4_8.prefab",
            "Assets/Low Poly Weapons VOL.1/Prefabs/AK74.prefab",
            "Assets/Low Poly Weapons VOL.1/Prefabs/Uzi.prefab",
        };

        GameObject prefab = null;

#if UNITY_EDITOR
        foreach (string path in candidatePaths)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) break;
        }
        if (prefab == null)
        {
            // Fallback: procura qualquer prefab do pack por GUID
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Low Poly Weapons VOL.1/Prefabs" });
            foreach (string guid in guids)
            {
                string p = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (prefab != null) break;
            }
        }
#else
        // Em build, tenta Resources (copia os prefabs de armas para Assets/Resources/Weapons/ se necessário)
        prefab = Resources.Load<GameObject>("Weapons/M1911");
        if (prefab == null) prefab = Resources.Load<GameObject>("Weapons/M4_8");
#endif

        if (prefab == null)
        {
            Debug.LogWarning("[Shooting] Prefab de arma do asset não encontrado. A usar modelo primitivo.");
            return false;
        }

        GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.name = "WeaponAssetModel";

        // Posição e escala FPS — a M1911 do Low Poly Weapons tem ~0.3 unidades de comprimento
        instance.transform.localPosition = new Vector3(0f, 0f, 0f);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale    = new Vector3(1f, 1f, 1f);

        // Remove todos os colliders do modelo (não devem interferir com o raycasting)
        foreach (Collider col in instance.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.Destroy(col);

        // Desativa sombras no modelo FPS (evita artefactos no ecrã)
        foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;
        }

        Debug.Log($"[Shooting] Modelo de arma carregado: {prefab.name}");
        return true;
    }

    Transform CreateKnifeModel(Transform parent)
    {
        Transform knife = new GameObject("Knife").transform;
        knife.SetParent(parent, false);
        knife.localPosition = new Vector3(0.1f, -0.06f, 0.22f);
        knife.localRotation = Quaternion.Euler(6f, 12f, -18f);
        knife.localScale = Vector3.one;

        CreateWeaponPart("Knife Blade", knife, new Vector3(0f, 0.02f, 0.17f), Vector3.zero, new Vector3(0.035f, 0.04f, 0.35f), new Color(0.75f, 0.75f, 0.78f), "MetalRustyTexture");
        CreateWeaponPart("Knife Handle", knife, new Vector3(0f, -0.03f, -0.02f), new Vector3(4f, 0f, 0f), new Vector3(0.06f, 0.08f, 0.14f), new Color(0.28f, 0.18f, 0.1f));
        return knife;
    }

    void RefreshWeaponVisualMode()
    {
        if (pistolRoot != null)
        {
            pistolRoot.gameObject.SetActive(currentWeapon == WeaponMode.Pistol);
        }

        if (knifeRoot != null)
        {
            knifeRoot.gameObject.SetActive(currentWeapon == WeaponMode.Knife);
        }
    }

    void CreateWeaponPart(string name, Transform parent, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, Color color, string textureName = "")
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localEulerAngles = localEulerAngles;
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(color, textureName);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Collider collider = part.GetComponent<Collider>();
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

    void CreateFallbackImpact(Vector3 hitPoint, Vector3 hitNormal)
    {
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.name = "Impact";
        impact.transform.position = hitPoint + hitNormal * 0.02f;
        impact.transform.localScale = Vector3.one * 0.12f;

        Renderer renderer = impact.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetFallbackImpactMaterial();
        }

        Collider collider = impact.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Destroy(impact, 0.15f);
    }

    void CreateTracer(Vector3 endPoint)
    {
        GameObject tracer = new GameObject("Bullet Tracer");
        LineRenderer line = tracer.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.SetPosition(0, fpsCamera.transform.position + fpsCamera.transform.forward * 0.4f + fpsCamera.transform.right * 0.12f - fpsCamera.transform.up * 0.06f);
        line.SetPosition(1, endPoint);
        line.widthMultiplier = 0.025f;
        line.numCapVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = GetTracerMaterial();
        line.startColor = new Color(1f, 0.95f, 0.75f, 0.95f);
        line.endColor = new Color(1f, 0.78f, 0.22f, 0.15f);
        Destroy(tracer, 0.05f);
    }

    void EnsureCrosshairReference()
    {
        if (cachedCrosshair == null)
        {
            cachedCrosshair = FindFirstObjectByType<Crosshair>();
        }
    }

    Material GetTracerMaterial()
    {
        if (tracerMaterial == null)
        {
            tracerMaterial = CreateMaterial(new Color(1f, 0.92f, 0.55f));
        }

        return tracerMaterial;
    }

    Material GetFallbackImpactMaterial()
    {
        if (fallbackImpactMaterial == null)
        {
            fallbackImpactMaterial = CreateMaterial(new Color(1f, 0.8f, 0.18f));
        }

        return fallbackImpactMaterial;
    }

    void PlayWeaponKick()
    {
        if (weaponRoot == null)
        {
            return;
        }

        if (kickCoroutine != null)
        {
            StopCoroutine(kickCoroutine);
        }

        kickCoroutine = StartCoroutine(KickRoutine());
        
        // Add screen shake
        if (UIManager.Instance != null)
        {
            // Assuming UIManager has a shake method or we can add one
            // For now, let's just add a small camera bump here
        }
    }

    IEnumerator KickRoutine()
    {
        if (weaponRoot == null || fpsCamera == null) yield break;

        Vector3 basePosition = new Vector3(0.33f, -0.28f, 0.52f);
        Quaternion baseRotation = Quaternion.Euler(6f, -18f, 0f);
        
        // More aggressive kick
        Vector3 kickPosition = basePosition + new Vector3(Random.Range(-0.02f, 0.02f), 0.02f, -0.12f);
        Quaternion kickRotation = Quaternion.Euler(Random.Range(-2f, -5f), -18f + Random.Range(-2f, 2f), Random.Range(-1f, 1f));

        float elapsed = 0f;
        float duration = 0.04f; // Faster kick
        
        // Camera recoil bump
        float camRecoilUp = 1.2f;
        float camRecoilSide = Random.Range(-0.4f, 0.4f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            weaponRoot.localPosition = Vector3.Lerp(basePosition, kickPosition, t);
            weaponRoot.localRotation = Quaternion.Slerp(baseRotation, kickRotation, t);
            
            // Camera kick
            fpsCamera.transform.localRotation *= Quaternion.Euler(-camRecoilUp * Time.deltaTime / duration, camRecoilSide * Time.deltaTime / duration, 0f);
            
            yield return null;
        }

        elapsed = 0f;
        duration = 0.12f; // Slower return
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth return using SmoothStep
            float smoothT = t * t * (3f - 2f * t);
            weaponRoot.localPosition = Vector3.Lerp(kickPosition, basePosition, smoothT);
            weaponRoot.localRotation = Quaternion.Slerp(kickRotation, baseRotation, smoothT);
            yield return null;
        }

        weaponRoot.localPosition = basePosition;
        weaponRoot.localRotation = baseRotation;
        kickCoroutine = null;
    }

}
