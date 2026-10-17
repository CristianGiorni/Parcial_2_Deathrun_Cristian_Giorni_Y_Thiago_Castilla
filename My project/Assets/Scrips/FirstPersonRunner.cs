using UnityEngine;
using Unity.Netcode;

public class FirstPersonRunner : NetworkBehaviour
{
    public Camera camaraJugador; // Arrastrá la cámara del jugador acá en el Inspector
    public AudioListener audioListener; // Arrastrá la cámara acá también (tiene el listener)
    [Header("Movimiento (M�tricas del GDD)")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float gravityMultiplier = 2f;

    [Header("C�mara y Mirada")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;
    private float verticalLookRotation = 0f;

    private Rigidbody rb;
    private bool isGrounded;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Apagamos SOLO el lente de la cámara y el micrófono, nunca el GameObject
            if (camaraJugador != null) camaraJugador.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
        else
        {
            if (camaraJugador != null) camaraJugador.enabled = true;
            if (audioListener != null) audioListener.enabled = true;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Si no somos el due�o de este personaje, no ejecutamos los controles
        if (!IsOwner) return;

        Look();
        CheckGround();

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        Move();
        ApplyExtraGravity();
    }

    private void Look()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Rotar el cuerpo entero de izquierda a derecha
        transform.Rotate(Vector3.up * mouseX);

        // Rotar solo la c�mara arriba/abajo (limitado a 90 grados)
        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        cameraTransform.localEulerAngles = new Vector3(verticalLookRotation, 0f, 0f);
    }

    private void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * x + transform.forward * z).normalized;

        // Mantenemos la velocidad Y intacta para no cancelar la gravedad
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = targetVelocity;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Resetear Y antes de saltar
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ApplyExtraGravity()
    {
        // Hace que la ca�da sea m�s r�pida y pesada, dando esa sensaci�n de "Tigh Controls" del GDD
        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }

    private void CheckGround()
    {
        // Raycast hacia abajo desde el centro del jugador. (1.1f asume una c�psula de altura 2)
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
}