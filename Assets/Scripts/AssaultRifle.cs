using UnityEngine;
using System.Collections;

public class AssaultRifle : MonoBehaviour
{
    [Header("Configuração")]
    public float dano = 20f;
    public float alcance = 120f;
    public float cooldown = 0.12f;
    public int municaoMax = 30;

    [Header("Recoil")]
    public float recoilGraus = 1.5f;

    [Header("Recarga")]
    public float tempoRecarga = 2.2f;

    [HideInInspector] public int municaoAtual;

    private float tempoCooldown = 0f;
    private bool aRecarregar = false;
    private Camera cam;
    private WeaponSystem weaponSystem;
    private SimplePlayer simplePlayer;
    private HUDManager hud;

    private Transform muzzlePoint;
    private Light muzzleLight;
    private GameObject muzzleFlashSphere;
    private LineRenderer trailRenderer;
    private AudioSource audioSource;

    void Start()
    {
        municaoAtual = municaoMax;
        cam = GetComponentInParent<Camera>();
        if (cam == null) cam = Camera.main;
        weaponSystem = FindFirstObjectByType<WeaponSystem>();
        simplePlayer = FindFirstObjectByType<SimplePlayer>();
        hud = FindFirstObjectByType<HUDManager>();
        audioSource = GetComponent<AudioSource>();
        CriarVisuais();
    }

    void CriarVisuais()
    {
        GameObject mp = new GameObject("MuzzlePoint");
        mp.transform.SetParent(transform);
        mp.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        muzzlePoint = mp.transform;

        GameObject lightGO = new GameObject("MuzzleLight");
        lightGO.transform.SetParent(muzzlePoint);
        lightGO.transform.localPosition = Vector3.zero;
        muzzleLight = lightGO.AddComponent<Light>();
        muzzleLight.type = LightType.Point;
        muzzleLight.range = 5f;
        muzzleLight.intensity = 15f;
        muzzleLight.color = new Color(1f, 0.88f, 0.45f);
        muzzleLight.shadows = LightShadows.None;
        muzzleLight.enabled = false;

        muzzleFlashSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        muzzleFlashSphere.transform.SetParent(muzzlePoint);
        muzzleFlashSphere.transform.localPosition = Vector3.zero;
        muzzleFlashSphere.transform.localScale = Vector3.one * 0.06f;
        Destroy(muzzleFlashSphere.GetComponent<Collider>());
        var r = muzzleFlashSphere.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = new Color(1f, 0.95f, 0.5f);
        r.material = mat;
        muzzleFlashSphere.SetActive(false);

        GameObject trailGO = new GameObject("BulletTrail");
        trailGO.transform.SetParent(transform);
        trailRenderer = trailGO.AddComponent<LineRenderer>();
        trailRenderer.positionCount = 2;
        trailRenderer.startWidth = 0.018f;
        trailRenderer.endWidth = 0.003f;
        trailRenderer.useWorldSpace = true;
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.startColor = new Color(1f, 0.95f, 0.65f, 1f);
        trailRenderer.endColor   = new Color(1f, 0.95f, 0.65f, 0f);
        trailRenderer.enabled = false;
    }

    void Update()
    {
        if (tempoCooldown > 0) tempoCooldown -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.R)) Recarregar();
    }

    public void Disparar()
    {
        if (aRecarregar) return;
        if (tempoCooldown > 0) return;
        if (municaoAtual <= 0)
        {
            if (hud != null) hud.MostrarAviso("SEM MUNICAO  —  [R] Recarregar");
            return;
        }

        tempoCooldown = cooldown;
        municaoAtual--;

        if (weaponSystem != null)
            weaponSystem.AtualizarMunicaoHUD(municaoAtual, municaoMax);

        if (simplePlayer != null)
            simplePlayer.AddRecoil(recoilGraus);

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        Vector3 pontoFinal = ray.origin + ray.direction * alcance;

        if (Physics.Raycast(ray, out RaycastHit hit, alcance))
        {
            pontoFinal = hit.point;
            ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null)
            {
                float alturaHit = hit.point.y - zombie.transform.position.y;
                bool headshot = alturaHit > 1.5f;
                zombie.LevarDano(headshot ? dano * 4f : dano, hit.point);
                if (hud != null) hud.MostrarHitMarker();
                if (headshot && hud != null) hud.MostrarHeadshot();
            }
            StartCoroutine(MarcaDeImpacto(hit.point, hit.normal));
        }

        if (audioSource != null) audioSource.Play();
        StartCoroutine(MuzzleFlash());
        StartCoroutine(MostrarTrail(muzzlePoint.position, pontoFinal));
    }

    IEnumerator MuzzleFlash()
    {
        muzzleLight.enabled = true;
        muzzleFlashSphere.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        muzzleLight.enabled = false;
        muzzleFlashSphere.SetActive(false);
    }

    IEnumerator MostrarTrail(Vector3 inicio, Vector3 fim)
    {
        trailRenderer.SetPosition(0, inicio);
        trailRenderer.SetPosition(1, fim);
        trailRenderer.enabled = true;
        float dur = 0.08f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = 1f - (t / dur);
            trailRenderer.startColor = new Color(1f, 0.95f, 0.65f, a);
            trailRenderer.endColor   = new Color(1f, 0.95f, 0.65f, 0f);
            yield return null;
        }
        trailRenderer.enabled = false;
    }

    IEnumerator MarcaDeImpacto(Vector3 pos, Vector3 normal)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(spark.GetComponent<Collider>());
        spark.transform.position = pos + normal * 0.02f;
        spark.transform.localScale = Vector3.one * 0.04f;
        var r = spark.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.color = new Color(1f, 0.8f, 0.2f);
        r.material = m;
        float t = 0f, dur = 0.15f;
        while (t < dur)
        {
            t += Time.deltaTime;
            spark.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.01f, t / dur);
            yield return null;
        }
        Destroy(spark);
    }

    void Recarregar()
    {
        if (aRecarregar || municaoAtual == municaoMax) return;
        StartCoroutine(RecarregarCoroutine());
    }

    IEnumerator RecarregarCoroutine()
    {
        aRecarregar = true;
        if (hud != null) hud.MostrarAviso("A RECARREGAR...");

        yield return new WaitForSeconds(tempoRecarga);

        municaoAtual = municaoMax;
        aRecarregar = false;
        if (weaponSystem != null)
            weaponSystem.AtualizarMunicaoHUD(municaoAtual, municaoMax);
    }
}
