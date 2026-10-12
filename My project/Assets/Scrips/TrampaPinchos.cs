using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class TrampaPinchos : NetworkBehaviour
{
    [Header("Métricas")]
    public float warningTime = 0.3f;
    public float distanciaMovimiento = 5f;
    public float velocidadMovimiento = 20f;
    public float cooldownTime = 3f;

    [Header("Pinchos")]
    public GameObject grupoPinchos;

    private Rigidbody rb;
    private Vector3 posicionInicial;
    private bool isReady = true;

    private void Start()
    {
        if (grupoPinchos == null)
        {
            Debug.LogError("TrampaPinchos: No hay un GrupoPinchos asignado.");
            return;
        }

        rb = grupoPinchos.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("GrupoPinchos necesita un Rigidbody.");
            return;
        }

        if (grupoPinchos.GetComponent<Collider>() == null)
        {
            Debug.LogError("GrupoPinchos necesita un Box Collider.");
            return;
        }

        posicionInicial = rb.position;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void RequestActivation()
    {
        if (!IsSpawned) return;

        if (IsServer) StartCoroutineIfReady();
        else ActivateTrapServerRpc();
    }

    private void StartCoroutineIfReady()
    {
        if (!isReady) return;
        StartCoroutine(TrapSequence());
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateTrapServerRpc()
    {
        StartCoroutineIfReady();
    }

    private IEnumerator TrapSequence()
    {
        isReady = false;
        WarningClientRpc();
        yield return new WaitForSeconds(warningTime);

        Vector3 destino = posicionInicial + Vector3.right * distanciaMovimiento;

        while (Vector3.Distance(rb.position, destino) > 0.01f)
        {
            rb.MovePosition(Vector3.MoveTowards(rb.position, destino, velocidadMovimiento * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(0.15f);

        while (Vector3.Distance(rb.position, posicionInicial) > 0.01f)
        {
            rb.MovePosition(Vector3.MoveTowards(rb.position, posicionInicial, velocidadMovimiento * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        rb.position = posicionInicial;
        yield return new WaitForSeconds(cooldownTime);
        ResetVisualClientRpc();
        isReady = true;
    }

    [ClientRpc]
    private void WarningClientRpc()
    {
        if (grupoPinchos == null) return;
        foreach (Renderer r in grupoPinchos.GetComponentsInChildren<Renderer>())
            r.material.color = Color.red;
    }

    [ClientRpc]
    private void ResetVisualClientRpc()
    {
        if (grupoPinchos == null) return;
        foreach (Renderer r in grupoPinchos.GetComponentsInChildren<Renderer>())
            r.material.color = Color.white;
    }

    // --- NUEVA LÓGICA DE EMPUJE ---
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("PINCHOS: ¡Empujando al jugador al vacío!");
            NetworkObject playerNetObj = collision.gameObject.GetComponent<NetworkObject>();

            if (playerNetObj != null)
            {
                // Le damos la orden al cliente de que salga volando
                EmpujarJugadorRpc(playerNetObj.NetworkObjectId);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void EmpujarJugadorRpc(ulong playerObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerObjectId, out NetworkObject playerObj))
        {
            // Si tu personaje usa Rigidbody
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero; // Frenamos su movimiento actual

                // AHORA SOLO EMPUJA HACIA LA DERECHA. 
                // Subí un poco la fuerza a 30f para compensar que ya no hay vuelo parabólico
                Vector3 direccionVuelo = Vector3.right;
                playerRb.AddForce(direccionVuelo * 30f, ForceMode.Impulse);
            }

            // Si tu personaje usa CharacterController
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null)
            {
                // Solo lo arrastra de forma horizontal
                cc.Move(Vector3.right * 6f);
            }
        }
    }
}