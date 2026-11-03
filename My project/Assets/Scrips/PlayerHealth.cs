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
        // 1. Apagamos los gráficos
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        // 2. Apagamos colisiones para que vuele por el mapa
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

        // 3. Lógica local para el jugador fantasma (Teletransporte)
        if (IsOwner)
        {
            // Búsqueda inteligente del punto de aparición
            if (puntoDeAparicion == null)
            {
                GameObject punto = GameObject.FindWithTag("Respawn");
                if (punto != null) puntoDeAparicion = punto.transform;
            }

            // Lo movemos a las coordenadas seguras de inicio
            if (puntoDeAparicion != null)
            {
                transform.position = puntoDeAparicion.position;
            }
            else
            {
                Debug.LogWarning("Modo Espectador: No se encontró el Tag 'Respawn'.");
            }

            // Activamos el script de vuelo libre
            EspectadorFantasma fantasma = GetComponent<EspectadorFantasma>();
            if (fantasma != null) fantasma.enabled = true;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RestaurarJugadorRpc(Vector3 posicionArena)
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

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
}