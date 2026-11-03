
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class TrampaPinchosVertical : NetworkBehaviour
{
    [Header("Métricas")]
    public float warningTime = 0.3f;
    public float distanciaMovimiento = 5f;
    public float velocidadMovimiento = 20f;
    public float cooldownTime = 3f;

    [Header("Pinchos")]
    public GameObject grupoPinchos;

    private Vector3 posicionInicial;
    private bool isReady = true;

    private void Start()
    {
        if (grupoPinchos == null)
        {
            Debug.LogError("No hay GrupoPinchos asignado.");
            return;
        }

        posicionInicial = grupoPinchos.transform.position;
    }

    public void RequestActivation()
    {
        if (!IsSpawned) return;

        if (IsServer)
            StartCoroutineIfReady();
        else
            ActivateTrapServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ActivateTrapServerRpc()
    {
        StartCoroutineIfReady();
    }

    private void StartCoroutineIfReady()
    {
        if (!isReady) return;

        StartCoroutine(TrapSequence());
    }

    private IEnumerator TrapSequence()
    {
        isReady = false;

        WarningClientRpc();

        yield return new WaitForSeconds(warningTime);

        Vector3 destino = posicionInicial + Vector3.up * distanciaMovimiento;

        yield return MovePinchos(destino);

        yield return new WaitForSeconds(0.15f);

        yield return MovePinchos(posicionInicial);

        grupoPinchos.transform.position = posicionInicial;

        yield return new WaitForSeconds(cooldownTime);

        ResetVisualClientRpc();

        isReady = true;
    }

    private IEnumerator MovePinchos(Vector3 destino)
    {
        while (Vector3.Distance(
            grupoPinchos.transform.position, destino) > 0.01f)
        {
            grupoPinchos.transform.position = Vector3.MoveTowards(
                grupoPinchos.transform.position,
                destino,
                velocidadMovimiento * Time.deltaTime
            );

            yield return null;
        }

        grupoPinchos.transform.position = destino;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth salud = collision.gameObject.GetComponent<PlayerHealth>();

            if (salud != null)
                salud.RecibirDano(1000f);
        }
    }
}
