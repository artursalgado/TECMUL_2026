using UnityEngine;

public class WinZone : MonoBehaviour
{
    private GameManager gameManager;
    private HUDManager hud;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        hud = FindFirstObjectByType<HUDManager>();

        // Garante que tem trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (gameManager == null || !gameManager.portaoAberto) return;

        if (hud != null)
            hud.MostrarVitoria();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
