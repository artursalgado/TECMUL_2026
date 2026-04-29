using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public float altura = 60f;
    private Transform player;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Sempre a apontar para baixo independente de tudo
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void LateUpdate()
    {
        if (player == null) return;
        transform.position = new Vector3(player.position.x, player.position.y + altura, player.position.z);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
