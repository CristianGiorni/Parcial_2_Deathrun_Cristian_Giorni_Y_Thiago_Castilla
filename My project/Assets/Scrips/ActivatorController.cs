using UnityEngine;
using Unity.Netcode;

public class ActivatorController : NetworkBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    private Rigidbody rb;

    [Header("C�mara y Mirada")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;
    private float verticalLookRotation = 0f;

    [Header("Interacci�n")]
    public float interactionRange = 100f; // Qu� tan lejos llega el rayo

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            cameraTransform.gameObject.SetActive(false);
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        // Buscamos el Rigidbody al iniciar
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        Look();
        HandleInteraction();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        Move(); // Agregamos la ejecuci�n del movimiento
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
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * x + transform.forward * z).normalized;

        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Mantenemos la gravedad

        rb.linearVelocity = targetVelocity;
    }

    private void HandleInteraction()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange))
            {
                TrapTrigger trap = hit.collider.GetComponent<TrapTrigger>();

                if (trap != null)
                {
                    trap.RequestActivation();
                }
            }
        }
    }
}