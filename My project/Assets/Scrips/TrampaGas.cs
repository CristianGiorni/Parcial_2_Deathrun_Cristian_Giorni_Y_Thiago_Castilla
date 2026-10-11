using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class TrampaGas : NetworkBehaviour
{
    [Header("Referencias de la Habitación")]
    public GameObject puertaEntrada;
    public GameObject puertaSalida;
    public ParticleSystem particulasGas;
    public GameObject zonaLetal;

    [Header("Tiempos (Telegrafiado y Tensión)")]
    public float tiempoDePanico = 1.5f;
    public float tiempoGasActivo = 3f;
    public float cooldownTime = 6f;

    private bool isReady = true;

    // Esta función la sigue llamando tu TrapButton.cs genérico
    public void RequestActivation()
    {
        if (!isReady) return;

        if (IsServer) StartCoroutine(SecuenciaCuboDeGas());
        else ActivarGasRpc(); // Llamamos al nuevo RPC
    }

    // NUEVA SINTAXIS: Reemplaza a [ServerRpc(RequireOwnership = false)]
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ActivarGasRpc()
    {
        if (isReady) StartCoroutine(SecuenciaCuboDeGas());
    }

    private IEnumerator SecuenciaCuboDeGas()
    {
        isReady = false;

        EstadoPuertasRpc(true);
        yield return new WaitForSeconds(tiempoDePanico);

        EstadoGasRpc(true);
        if (zonaLetal != null) zonaLetal.SetActive(true);

        yield return new WaitForSeconds(tiempoGasActivo);

        if (zonaLetal != null) zonaLetal.SetActive(false);
        EstadoGasRpc(false);
        EstadoPuertasRpc(false);

        yield return new WaitForSeconds(cooldownTime);
        isReady = true;
    }

    // NUEVA SINTAXIS: Reemplaza a [ClientRpc]
    [Rpc(SendTo.Everyone)]
    private void EstadoPuertasRpc(bool bloqueadas)
    {
        if (puertaEntrada != null) puertaEntrada.SetActive(bloqueadas);
        if (puertaSalida != null) puertaSalida.SetActive(bloqueadas);
    }

    // NUEVA SINTAXIS: Reemplaza a [ClientRpc]
    [Rpc(SendTo.Everyone)]
    private void EstadoGasRpc(bool activo)
    {
        if (particulasGas != null)
        {
            if (activo) particulasGas.Play();
            else particulasGas.Stop();
        }
    }
}