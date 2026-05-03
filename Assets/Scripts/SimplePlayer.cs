using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayer : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 3.5f;
    public float velocidadeSprint = 5.5f;
    public float gravidade = -20f;

    [Header("Salto")]
    public float forcaSalto = 1.8f;

    [Header("Câmara")]
    public float sensibilidadeRato = 2f;

    private CharacterController _controller;
    private Transform _cameraTransform;
    private float _rotacaoVertical = 0f;
    private Vector3 _velocidadeQueda;
    private float _recoilOffset = 0f;
    private AudioSource audioPassos;

    public bool EmSprint => Input.GetKey(KeyCode.LeftShift) && _controller.isGrounded;

    void Start()
    {
        // Garante que o tempo do jogo não está em pausa (às vezes o Unity "encrava" no tempo 0)
        Time.timeScale = 1f;

        _controller = GetComponent<CharacterController>();
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length > 0) audioPassos = sources[0];
        
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("SimplePlayer: MainCamera não encontrada! Certifica que a câmara tem a tag 'MainCamera'.");
        }

        // Esconde e prende o rato no centro do ecrã (apenas durante o jogo)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        Debug.Log("SimplePlayer: Script Iniciado. Pronto para andar!");
    }

    void Update()
    {
        Mover();
        
        if (_cameraTransform != null)
        {
            Olhar();
        }
    }

    void Mover()
    {
        // Movimento no chão (WASD ou Setas)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float velAtual = EmSprint ? velocidadeSprint : velocidade;
        Vector3 movimento = transform.right * x + transform.forward * z;
        _controller.Move(movimento * velAtual * Time.deltaTime);

        // Som de passos
        bool emMovimento = (Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f) && _controller.isGrounded;
        if (audioPassos != null)
        {
            audioPassos.pitch = EmSprint ? 1.4f : 1f;
            if (emMovimento && !audioPassos.isPlaying) audioPassos.Play();
            else if (!emMovimento && audioPassos.isPlaying) audioPassos.Stop();
        }

        if (_controller.isGrounded && _velocidadeQueda.y < 0)
            _velocidadeQueda.y = -2f;

        // Salto
        if (Input.GetKeyDown(KeyCode.Space) && _controller.isGrounded)
            _velocidadeQueda.y = Mathf.Sqrt(forcaSalto * -2f * gravidade);

        _velocidadeQueda.y += gravidade * Time.deltaTime;
        _controller.Move(_velocidadeQueda * Time.deltaTime);
    }

    void Olhar()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadeRato;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeRato;

        // Roda o corpo do jogador para os lados (Eixo Y)
        transform.Rotate(Vector3.up * mouseX);

        // Roda a câmara para cima e para baixo (Eixo X) com limite
        _rotacaoVertical -= mouseY;
        _rotacaoVertical = Mathf.Clamp(_rotacaoVertical, -90f, 90f);
        _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, Time.deltaTime * 10f);
        _cameraTransform.localEulerAngles = Vector3.right * (_rotacaoVertical - _recoilOffset);
    }

    public void AddRecoil(float graus)
    {
        _recoilOffset = Mathf.Clamp(_recoilOffset + graus, 0f, 20f);
    }
}
