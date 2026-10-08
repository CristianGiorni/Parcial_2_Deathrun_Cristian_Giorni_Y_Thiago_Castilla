using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class CuboTrampa : NetworkBehaviour
{
    [Header("Métricas de la Trampa")]
    public float warningTime = 0.8f;
    public float tiempoCaida = 2f;
    public float cooldownTime = 5f;
    public float velocidadCaida = 15f;

    [Header("Referencias")]
    public GameObject cubo;

    private Rigidbody rb;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private bool isReady = true;

    private void Start()
    {
        if (cubo == null)
        {
            Debug.LogError("CuboTrampa: No hay un cubo asignado.");
            return;
        }

        rb = cubo.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("CuboTrampa: El cubo necesita un Rigidbody.");
            return;
        }

        posicionInicial = cubo.transform.position;
        rotacionInicial = cubo.transform.rotation;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void RequestActivation()
    {
        if (!isReady)
            return;

        if (IsServer)
        {
            StartCoroutine(TrapSequence());
        }
        else
        {
            ActivateTrapServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateTrapServerRpc()
    {
        if (!isReady)
            return;

        StartCoroutine(TrapSequence());
    }

    private IEnumerator TrapSequence()
    {
        isReady = false;

        WarningClientRpc();

        yield return new WaitForSeconds(warningTime);

        ActivateCube();

        yield return new WaitForSeconds(tiempoCaida);

        ResetCube();

        yield return new WaitForSeconds(cooldownTime);

        ResetVisualClientRpc();

        isReady = true;
    }

    private void ActivateCube()
    {
        if (rb == null)
            return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.down * velocidadCaida;
    }

    private void ResetCube()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;
        rb.useGravity = false;

        cubo.transform.position = posicionInicial;
        cubo.transform.rotation = rotacionInicial;
    }

    [ClientRpc]
    private void WarningClientRpc()
    {
        if (cubo == null)
            return;

        Renderer renderer = cubo.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material.color = Color.red;
    }

    [ClientRpc]
    private void ResetVisualClientRpc()
    {
        if (cubo == null)
            return;

        Renderer renderer = cubo.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material.color = Color.white;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("El cubo golpeó a un jugador. DAÑO.");
        }
    }
}