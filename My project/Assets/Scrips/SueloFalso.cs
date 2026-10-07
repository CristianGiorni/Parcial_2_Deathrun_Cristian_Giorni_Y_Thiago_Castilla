using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SueloFalso : NetworkBehaviour
{
    [Header("Métricas de la Trampa")]
    public float warningTime = 0.8f; // Tiempo que parpadea en rojo antes de caer
    public float tiempoAbierto = 2f; // Cuánto tiempo está el pozo abierto
    public float cooldownTime = 5f;  // Cuánto tarda en poder usarse de nuevo

    [Header("Referencias")]
    public GameObject pisoObjeto;    // El cubo sobre el que caminan los corredores
    private Renderer pisoRenderer;
    private Color colorOriginal;

    private bool isReady = true;

    private void Start()
    {
        if (pisoObjeto != null)
        {
            pisoRenderer = pisoObjeto.GetComponent<Renderer>();
            colorOriginal = pisoRenderer.material.color; // Guardamos el color blanco normal
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

        // 1. ADVERTENCIA (Telegraphing)
        WarningClientRpc();
        yield return new WaitForSeconds(warningTime);

        // 2. ABRIR EL PISO (Cae el corredor)
        TogglePisoClientRpc(false);
        yield return new WaitForSeconds(tiempoAbierto);

        // 3. CERRAR EL PISO (Pueden pasar los demás)
        TogglePisoClientRpc(true);

        // 4. TIEMPO DE RECARGA
        yield return new WaitForSeconds(cooldownTime);
        isReady = true;
    }

    [ClientRpc]
    private void WarningClientRpc()
    {
        // Cambiamos el piso a ROJO para avisar que se va a caer
        if (pisoRenderer != null) pisoRenderer.material.color = Color.red;
    }

    [ClientRpc]
    private void TogglePisoClientRpc(bool state)
    {
        // Apagamos o prendemos el piso en las pantallas de todos
        if (pisoObjeto != null) pisoObjeto.SetActive(state);

        // Si el piso vuelve a aparecer, le devolvemos su color original
        if (state && pisoRenderer != null) pisoRenderer.material.color = colorOriginal;
    }
}