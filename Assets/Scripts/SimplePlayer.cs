using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayer : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 5f;
    public float gravidade = -9.81f;

    [Header("Câmara")]
    public float sensibilidadeRato = 2f;
    
    private CharacterController _controller;
    private Transform _cameraTransform;
    private float _rotacaoVertical = 0f;
    private Vector3 _velocidadeQueda;

    void Start()
    {
        // Garante que o tempo do jogo não está em pausa (às vezes o Unity "encrava" no tempo 0)
        Time.timeScale = 1f;

        _controller = GetComponent<CharacterController>();
        
        // Tenta encontrar a câmara filha do jogador
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            _cameraTransform = cam.transform;
            Debug.Log("SimplePlayer: Câmara encontrada com sucesso!");
        }
        else
        {
            Debug.LogError("SimplePlayer: Nenhuma câmara encontrada dentro do Player!");
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

        Vector3 movimento = transform.right * x + transform.forward * z;
        _controller.Move(movimento * velocidade * Time.deltaTime);

        // Aplica gravidade para não flutuar
        if (_controller.isGrounded && _velocidadeQueda.y < 0)
        {
            _velocidadeQueda.y = -2f; // Mantém o jogador preso ao chão
        }

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
        _cameraTransform.localEulerAngles = Vector3.right * _rotacaoVertical;
    }
}
