using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SueloFalso : NetworkBehaviour
{
    [Header("Métricas de la Trampa")]
    public float warningTime = 0.8f; 
    public float tiempoAbierto = 2f; 
    public float cooldownTime = 5f; 

    [Header("Referencias")]
    public GameObject pisoObjeto;    
    private Renderer pisoRenderer;
    private Color colorOriginal;

    private bool isReady = true;

    private void Start()
    {
        if (pisoObjeto != null)
        {
            pisoRenderer = pisoObjeto.GetComponent<Renderer>();
            colorOriginal = pisoRenderer.material.color;
        }
    }

    public void RequestActivation()
    {
        if (!isReady) return;

        if (IsServer) StartCoroutine(TrapSequence());
        else ActivateTrapServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ActivateTrapServerRpc()
    {
        if (isReady) StartCoroutine(TrapSequence());
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
    }

    [ClientRpc]
    private void WarningClientRpc()
    {
       
        if (pisoRenderer != null) pisoRenderer.material.color = Color.red;
    }

    [ClientRpc]
    private void TogglePisoClientRpc(bool state)
    {
        
        if (pisoObjeto != null) pisoObjeto.SetActive(state);

        
        if (state && pisoRenderer != null) pisoRenderer.material.color = colorOriginal;
    }
}