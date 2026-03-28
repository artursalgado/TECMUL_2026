using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f;
    public Camera interactionCamera;

    private PlayerHealth playerHealth;
    private Shooting shooting;
    private PlayerInventory inventory;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        shooting = GetComponent<Shooting>();
        inventory = GetComponent<PlayerInventory>();

        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
    }

    void Update()
    {
        if (interactionCamera == null)
        {
            UIManager.Instance?.UpdatePrompt(string.Empty);
            return;
        }

        if (Physics.Raycast(
            interactionCamera.transform.position,
            interactionCamera.transform.forward,
            out RaycastHit hit,
            interactDistance))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<LootContainer>();
            interactable ??= hit.collider.GetComponentInParent<ObjectiveInteractable>();
            interactable ??= hit.collider.GetComponentInParent<ExtractionZone>();
            if (interactable != null)
            {
                UIManager.Instance?.UpdatePrompt(interactable.GetPrompt(this));

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact(this);
                }

                return;
            }
        }

        UIManager.Instance?.UpdatePrompt(string.Empty);
    }

    public PlayerHealth PlayerHealth => playerHealth;
    public Shooting Shooting => shooting;
    public PlayerInventory Inventory => inventory;
}
