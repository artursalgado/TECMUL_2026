using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed   = 6f;
    public float sprintSpeed = 9f;
    public float gravity     = -9.81f;
    public float jumpHeight  = 1.1f;

    [Header("Stealth")]
    public float crouchSpeed  = 2.5f;
    public float crouchHeight = 1.2f;

    [Header("Ground Detection")]
    public LayerMask walkableLayers = ~0;

    [Header("Camera")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;

    private CharacterController _controller;
    private Vector3 _verticalVelocity;
    private float   _verticalRotation;
    private bool    _isGrounded;
    private Vector3 _spawnPosition;
    private float   _standingHeight;
    private Vector3 _standingCenter;
    private bool    _isCrouching;
    private float   _movementInputMagnitude;

    // ── Bob ──────────────────────────────────────────────────────────────
    private float _bobTimer;
    private Vector3 _bobOffset;
    private Vector3 _camBaseLocal;
    const float BobFreq   = 1.8f;
    const float BobAmount = 0.04f;

    void Start()
    {
        _controller      = GetComponent<CharacterController>();
        _standingHeight  = _controller.height;
        _standingCenter  = _controller.center;
        _spawnPosition   = transform.position;

        // Apply config
        mouseSensitivity = GameConfig.MouseSensitivity;
        if (playerCamera != null)
        {
            _camBaseLocal = playerCamera.localPosition;
            Camera cam = playerCamera.GetComponent<Camera>();
            if (cam != null) cam.fieldOfView = GameConfig.FieldOfView;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        SnapToGround();
    }

    void Update()
    {
        // Honour runtime sensitivity changes from pause/settings
        mouseSensitivity = GameConfig.MouseSensitivity;

        LookAround();
        MovePlayer();
        UpdateCameraBob();
    }

    void LookAround()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _verticalRotation -= mouseY;
        _verticalRotation  = Mathf.Clamp(_verticalRotation, -verticalLookLimit, verticalLookLimit);

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    void MovePlayer()
    {
        if (transform.position.y < -10f) { ResetToSpawn(); return; }

        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _verticalVelocity.y < 0f) _verticalVelocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        _isCrouching = Input.GetKey(KeyCode.C);
        bool sprinting = Input.GetKey(KeyCode.LeftShift) && !_isCrouching;

        Vector3 move = transform.right * x + transform.forward * z;
        _movementInputMagnitude = Mathf.Clamp01(new Vector2(x, z).magnitude);

        float speed = _isCrouching ? crouchSpeed : (sprinting ? sprintSpeed : walkSpeed);
        _controller.height = _isCrouching ? crouchHeight : _standingHeight;
        _controller.center = _isCrouching
            ? new Vector3(_standingCenter.x, crouchHeight * 0.5f, _standingCenter.z)
            : _standingCenter;
        _controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && _isGrounded)
            _verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _verticalVelocity.y += gravity * Time.deltaTime;
        _controller.Move(_verticalVelocity * Time.deltaTime);

        // Notify audio manager for footsteps
        bool isMoving = _movementInputMagnitude > 0.1f && _isGrounded;
        AudioManager.Instance?.SetPlayerMovement(isMoving, sprinting);
    }

    void UpdateCameraBob()
    {
        if (playerCamera == null) return;

        bool moving = _movementInputMagnitude > 0.1f && _isGrounded && !_isCrouching;
        if (moving)
        {
            _bobTimer += Time.deltaTime * BobFreq * (_movementInputMagnitude * 2f);
            _bobOffset = new Vector3(
                Mathf.Sin(_bobTimer * 2f) * BobAmount * 0.5f,
                Mathf.Abs(Mathf.Sin(_bobTimer)) * BobAmount,
                0f);
        }
        else
        {
            _bobTimer  = 0f;
            _bobOffset = Vector3.Lerp(_bobOffset, Vector3.zero, Time.deltaTime * 6f);
        }

        playerCamera.localPosition = _camBaseLocal + _bobOffset;
    }

    void SnapToGround()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 6f,
            Vector3.down, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) { ForceSafeSpawn(); return; }
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit h in hits)
        {
            if (!IsWalkableSurface(h.collider)) continue;
            _controller.enabled = false;
            transform.position  = h.point + Vector3.up * 0.08f;
            _controller.enabled = true;
            _spawnPosition = transform.position;
            _verticalVelocity = Vector3.zero;
            return;
        }
        ForceSafeSpawn();
    }

    void ResetToSpawn()
    {
        _controller.enabled = false;
        transform.position  = _spawnPosition + Vector3.up * 0.5f;
        _controller.enabled = true;
        _verticalVelocity   = Vector3.zero;
    }

    void ForceSafeSpawn()
    {
        _controller.enabled = false;
        transform.position  = new Vector3(0f, 0.08f, -24f);
        _controller.enabled = true;
        _spawnPosition      = transform.position;
        _verticalVelocity   = Vector3.zero;
    }

    bool IsWalkableSurface(Collider c)
    {
        if (c == null) return false;
        int layerBit = 1 << c.gameObject.layer;
        if ((walkableLayers.value & layerBit) != 0) return true;
        string n = c.gameObject.name;
        return n == "Ground" || n == "Outer Terrain" || n == "Main Street"
            || n == "Cross Road" || n.Contains("Road") || n.Contains("Terrain");
    }

    public bool  IsCrouching()                => _isCrouching;
    public float GetMovementInputMagnitude()   => _movementInputMagnitude;
}
