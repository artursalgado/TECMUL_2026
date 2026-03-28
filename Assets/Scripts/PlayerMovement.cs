using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [FormerlySerializedAs("velocidade")]
    public float walkSpeed = 5f;

    [FormerlySerializedAs("velocidadeCorrida")]
    public float sprintSpeed = 10f;

    [FormerlySerializedAs("gravidade")]
    public float gravity = -9.81f;

    [FormerlySerializedAs("alturaJump")]
    public float jumpHeight = 1.5f;

    [Header("Stealth")]
    public float crouchSpeed = 2.5f;
    public float crouchHeight = 1.2f;

    [Header("Camera")]
    [FormerlySerializedAs("cameraPrincipal")]
    public Transform playerCamera;

    [FormerlySerializedAs("sensibilidadeRato")]
    public float mouseSensitivity = 2f;

    [FormerlySerializedAs("limiteVertical")]
    public float verticalLookLimit = 80f;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float verticalRotation = 0f;
    private bool isGrounded;
    private Vector3 spawnPosition;
    private float standingHeight;
    private Vector3 standingCenter;
    private bool isCrouching;
    private float movementInputMagnitude;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        standingCenter = controller.center;
        spawnPosition = transform.position;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SnapToGround();
    }

    void Update()
    {
        LookAround();
        MovePlayer();
    }

    void LookAround()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        transform.Rotate(Vector3.up * mouseX);
    }

    void MovePlayer()
    {
        if (transform.position.y < -10f)
        {
            ResetToSpawn();
            return;
        }

        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        isCrouching = Input.GetKey(KeyCode.C);
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;

        Vector3 movement = transform.right * x + transform.forward * z;
        movementInputMagnitude = Mathf.Clamp01(new Vector2(x, z).magnitude);
        float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
        controller.height = isCrouching ? crouchHeight : standingHeight;
        controller.center = isCrouching
            ? new Vector3(standingCenter.x, crouchHeight * 0.5f, standingCenter.z)
            : standingCenter;
        controller.Move(movement * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    void SnapToGround()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 6f, Vector3.down, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            ForceSafeSpawn();
            return;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (!IsWalkableSurface(hit.collider))
            {
                continue;
            }

            Vector3 snappedPosition = hit.point + Vector3.up * 0.08f;
            controller.enabled = false;
            transform.position = snappedPosition;
            controller.enabled = true;
            spawnPosition = transform.position;
            verticalVelocity = Vector3.zero;
            return;
        }

        ForceSafeSpawn();
    }

    void ResetToSpawn()
    {
        controller.enabled = false;
        transform.position = spawnPosition + Vector3.up * 0.5f;
        controller.enabled = true;
        verticalVelocity = Vector3.zero;
    }

    void ForceSafeSpawn()
    {
        controller.enabled = false;
        transform.position = new Vector3(0f, 0.08f, -24f);
        controller.enabled = true;
        spawnPosition = transform.position;
        verticalVelocity = Vector3.zero;
    }

    bool IsWalkableSurface(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        string surfaceName = hitCollider.gameObject.name;
        return surfaceName == "Ground"
            || surfaceName == "Outer Terrain"
            || surfaceName == "Main Street"
            || surfaceName == "Cross Road";
    }

    public bool IsCrouching() => isCrouching;

    public float GetMovementInputMagnitude() => movementInputMagnitude;
}
