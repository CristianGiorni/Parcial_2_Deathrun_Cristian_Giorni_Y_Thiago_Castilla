using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class TrapTrigger : NetworkBehaviour
{
    [Header("Métricas de la Trampa")]
    public float warningTime = 0.8f;
    public float tiempoAbierto = 2f;
    public float cooldownTime = 5f;

    [Header("Referencias")]
    public GameObject pisoObjeto;

    private Renderer pisoRenderer;
    private Color colorOriginalPiso;
    private bool isReady = true;

    private void Start()
    {
        if (pisoObjeto != null)
        {
            pisoRenderer = pisoObjeto.GetComponent<Renderer>();

            if (pisoRenderer != null)
                colorOriginalPiso = pisoRenderer.material.color;
        }
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
        if (isReady)
            StartCoroutine(TrapSequence());
    }

    private IEnumerator TrapSequence()
    {
        isReady = false;

        WarningClientRpc();

        yield return new WaitForSeconds(warningTime);

        TogglePisoClientRpc(false);

        yield return new WaitForSeconds(tiempoAbierto);

        TogglePisoClientRpc(true);

        yield return new WaitForSeconds(cooldownTime);

        isReady = true;

        ResetVisualsClientRpc();
    }

    [ClientRpc]
    private void WarningClientRpc()
    {
        Renderer renderer = GetComponent<Renderer>();

        if (renderer != null)
            renderer.material.color = Color.red;

        if (pisoRenderer != null)
            pisoRenderer.material.color = Color.red;
    }

    [ClientRpc]
    private void TogglePisoClientRpc(bool state)
    {
        if (pisoObjeto != null)
            pisoObjeto.SetActive(state);
    }

    [ClientRpc]
    private void ResetVisualsClientRpc()
    {
        Renderer renderer = GetComponent<Renderer>();

        if (renderer != null)
            renderer.material.color = Color.white;

        if (pisoRenderer != null)
            pisoRenderer.material.color = colorOriginalPiso;
    }
}