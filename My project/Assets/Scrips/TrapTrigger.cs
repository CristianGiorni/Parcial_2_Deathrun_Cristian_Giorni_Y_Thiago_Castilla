using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class TrapTrigger : NetworkBehaviour
{
    [Header("Métricas de la Trampa")]
    public float warningTime = 0.8f; // El "Telegrafiado"
    public float tiempoAbierto = 2f; // Cuánto tiempo desaparece el piso
    public float cooldownTime = 5f;  // Recarga del botón

    [Header("Referencias")]
    public GameObject pisoObjeto;    // El cubo del piso que va a desaparecer

    private Renderer pisoRenderer;
    private Color colorOriginalPiso;
    private bool isReady = true;

    private void Start()
    {
        // Guardamos el color original del piso al iniciar
        if (pisoObjeto != null)
        {
            pisoRenderer = pisoObjeto.GetComponent<Renderer>();
            if (pisoRenderer != null) colorOriginalPiso = pisoRenderer.material.color;
        }
    }

    public void RequestActivation()
    {
        if (!isReady) return;

        if (IsServer) StartCoroutine(TrapSequence());
        else ActivateTrapServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateTrapServerRpc()
    {
        if (isReady) StartCoroutine(TrapSequence());
    }

    private IEnumerator TrapSequence()
    {
        isReady = false;

        // 1. Advertencia: Todo se pone rojo
        WarningClientRpc();
        yield return new WaitForSeconds(warningTime);

        // 2. Ejecución: Desaparece el piso
        TogglePisoClientRpc(false);
        yield return new WaitForSeconds(tiempoAbierto);

        // 3. Restauración: Vuelve a aparecer el piso
        TogglePisoClientRpc(true);

        // 4. Cooldown
        yield return new WaitForSeconds(cooldownTime);
        isReady = true;
        ResetVisualsClientRpc();
    }

    [ClientRpc]
    private void WarningClientRpc()
    {
        // El botón se pone rojo
        GetComponent<Renderer>().material.color = Color.red;
        // El piso se pone rojo
        if (pisoRenderer != null) pisoRenderer.material.color = Color.red;
    }

    [ClientRpc]
    private void TogglePisoClientRpc(bool state)
    {
        // Apaga o prende el GameObject del piso
        if (pisoObjeto != null) pisoObjeto.SetActive(state);
    }

    [ClientRpc]
    private void ResetVisualsClientRpc()
    {
        // El botón vuelve a blanco
        GetComponent<Renderer>().material.color = Color.white;
        // El piso vuelve a su color normal
        if (pisoRenderer != null) pisoRenderer.material.color = colorOriginalPiso;
    }
}