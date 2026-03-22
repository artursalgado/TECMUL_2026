using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;

    private Rigidbody rb;
    private float verticalRotation = 0f;
    private bool isGrounded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleJump();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move = move.normalized * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    void OnCollisionExit(Collision col)
    {
        if (col.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}
