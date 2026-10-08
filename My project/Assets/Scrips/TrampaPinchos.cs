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

    private Vector3 posicionInicial;
    private bool isReady = true;

    private void Start()
    {
        if (grupoPinchos == null)
        {
            Debug.LogError("TrampaPinchos: No hay un GrupoPinchos asignado.");
            return;
        }

        posicionInicial = grupoPinchos.transform.position;
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

        yield return StartCoroutine(MovePinchos());

        grupoPinchos.transform.position = posicionInicial;

        yield return new WaitForSeconds(cooldownTime);

        ResetVisualClientRpc();

        isReady = true;
    }

    private IEnumerator MovePinchos()
    {
        Vector3 destino = posicionInicial + Vector3.right * distanciaMovimiento;

        while (Vector3.Distance(grupoPinchos.transform.position, destino) > 0.01f)
        {
            grupoPinchos.transform.position = Vector3.MoveTowards(
                grupoPinchos.transform.position,
                destino,
                velocidadMovimiento * Time.deltaTime
            );

            yield return null;
        }

        yield return new WaitForSeconds(0.15f);

        while (Vector3.Distance(grupoPinchos.transform.position, posicionInicial) > 0.01f)
        {
            grupoPinchos.transform.position = Vector3.MoveTowards(
                grupoPinchos.transform.position,
                posicionInicial,
                velocidadMovimiento * Time.deltaTime
            );

            yield return null;
        }
    }

    [ClientRpc]
    private void WarningClientRpc()
    {
        if (grupoPinchos == null)
            return;

        Renderer[] renderers = grupoPinchos.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.material.color = Color.red;
        }
    }

    [ClientRpc]
    private void ResetVisualClientRpc()
    {
        if (grupoPinchos == null)
            return;

        Renderer[] renderers = grupoPinchos.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.material.color = Color.white;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Un jugador fue golpeado por los pinchos. DAÑO.");
        }
    }
}