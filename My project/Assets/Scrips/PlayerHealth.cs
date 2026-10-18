
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    public NetworkVariable<float> vidaActual = new NetworkVariable<float>(100f);

    public NetworkVariable<bool> estaMuerto = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> llegoAMeta = new NetworkVariable<bool>(false);

    [Header("Límite del Mapa")]
    public float alturaMinimaMuerte = -15f;
    public Transform puntoDeAparicion;

    private void Update()
    {
        if (!IsServer) return;

        if (transform.position.y < alturaMinimaMuerte && vidaActual.Value > 0 && !estaMuerto.Value)
            RecibirDano(1000f);
    }

    public void RecibirDano(float cantidad)
    {
        if (!IsServer || estaMuerto.Value || llegoAMeta.Value) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            vidaActual.Value = 0;
            estaMuerto.Value = true;
            ActivarEspectadorRpc();
            DeathrunManager.Instancia.RevisarEstadoPartida();
        }
    }

    public void RecibirDanoPvP(float cantidad)
    {
        if (!IsServer || estaMuerto.Value) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            vidaActual.Value = 0;
            estaMuerto.Value = true;
            ActivarEspectadorRpc();
            DeathrunManager.Instancia.RevisarEstadoPartida();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void ActivarEspectadorRpc()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        FirstPersonRunner scriptMov = GetComponent<FirstPersonRunner>();
        if (scriptMov != null) scriptMov.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (IsOwner)
        {
            if (puntoDeAparicion != null)
                transform.position = puntoDeAparicion.position;

            EspectadorFantasma fantasma = GetComponent<EspectadorFantasma>();
            if (fantasma != null) fantasma.enabled = true;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RestaurarJugadorRpc(Vector3 posicionArena)
    {
        GetComponent<Collider>().enabled = true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = true;

        if (IsOwner)
        {
            EspectadorFantasma fantasma = GetComponent<EspectadorFantasma>();
            if (fantasma != null) fantasma.enabled = false;

            FirstPersonRunner scriptMov = GetComponent<FirstPersonRunner>();
            if (scriptMov != null) scriptMov.enabled = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                transform.position = posicionArena;
                rb.isKinematic = false;
            }
        }
    }

    public void RegenerarJugador()
    {
        if (IsOwner)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                transform.position = puntoDeAparicion.position;
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Trampa") || other.CompareTag("KillZone"))
            RegenerarJugador();
    }
}
