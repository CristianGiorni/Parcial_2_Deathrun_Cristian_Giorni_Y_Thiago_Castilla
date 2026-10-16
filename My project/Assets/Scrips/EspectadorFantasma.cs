using UnityEngine;
using Unity.Netcode;

public class EspectadorFantasma : NetworkBehaviour
{
    [Header("Configuración de Vuelo")]
    public float velocidadVuelo = 15f;
    public float sensibilidadRaton = 2f;

    private float rotacionX = 0f;
    private Camera miCamara;

    private void Awake()
    {
        miCamara = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // 1. Rotación de la cámara (Mirar alrededor)
        float ratonX = Input.GetAxis("Mouse X") * sensibilidadRaton;
        float ratonY = Input.GetAxis("Mouse Y") * sensibilidadRaton;

        rotacionX -= ratonY;
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);

        if (miCamara != null)
        {
            miCamara.transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
        }
        transform.Rotate(Vector3.up * ratonX);

        // 2. Vuelo libre usando la dirección de la cámara
        float x = Input.GetAxis("Horizontal"); // A y D
        float z = Input.GetAxis("Vertical");   // W y S
        float y = 0f;

        if (Input.GetKey(KeyCode.E)) y = 1f; // Subida extra recta
        if (Input.GetKey(KeyCode.Q)) y = -1f; // Bajada extra recta

        // Calculamos la dirección basándonos en hacia dónde mira la CÁMARA
        Vector3 direccionAdelante = miCamara.transform.forward * z;
        Vector3 direccionLado = miCamara.transform.right * x;
        Vector3 direccionArriba = Vector3.up * y; // Para la Q y la E

        Vector3 movimiento = (direccionAdelante + direccionLado + direccionArriba) * velocidadVuelo * Time.deltaTime;
        transform.position += movimiento;
    }
}