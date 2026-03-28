using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public string zoneName = "Outskirts";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        GameManager.Instance?.EnterZone(zoneName);
    }
}
