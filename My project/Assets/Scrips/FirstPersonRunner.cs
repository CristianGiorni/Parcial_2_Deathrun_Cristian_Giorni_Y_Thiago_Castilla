using UnityEngine;
using Unity.Netcode;

public class FirstPersonRunner : NetworkBehaviour
{
    public Camera camaraJugador;
    public AudioListener audioListener;

    [Header("Movimiento (Corredor y Activador)")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float gravityMultiplier = 2f;

    [Header("Cámara y Mirada")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;
    private float verticalLookRotation = 0f;

    [Header("Interacción (Solo Activador)")]
    public float interactionRange = 100f;

    private Rigidbody rb;
    private bool isGrounded;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
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
        if (!IsOwner) return;

        Look();
        CheckGround(); 

        bool soyActivador = false;
        if (DeathrunManager.Instancia != null)
        {
            soyActivador = DeathrunManager.Instancia.SoyElActivadorLocal();
        }

        if (soyActivador)
        {
            HandleInteraction();
        }
        else
        {
            if (Input.GetButtonDown("Jump") && isGrounded && rb != null && !rb.isKinematic)
            {
                Jump();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || rb == null) return;

        if (rb.isKinematic) return;

        Move();

        bool soyActivador = DeathrunManager.Instancia != null && DeathrunManager.Instancia.SoyElActivadorLocal();
        if (!soyActivador)
        {
            ApplyExtraGravity();
        }
    }

    private void Look()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        cameraTransform.localEulerAngles = new Vector3(verticalLookRotation, 0f, 0f);
    }

    private void Move()
    {
        if (rb.isKinematic) return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * x + transform.forward * z).normalized;
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = targetVelocity;
    }

    private void Jump()
    {
        if (rb.isKinematic) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ApplyExtraGravity()
    {
        if (rb.isKinematic) return;

        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }

 
    private void CheckGround()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    private void HandleInteraction()
    {
        if (!Input.GetButtonDown("Fire1")) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
        {
            TrapButton button = hit.collider.GetComponent<TrapButton>();
            if (button != null)
            {
                button.Activate();
            }
        }
    }
}