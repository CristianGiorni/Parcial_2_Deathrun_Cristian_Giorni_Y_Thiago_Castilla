using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class VidaActivador : NetworkBehaviour
{
    public NetworkVariable<float> vidaActual = new NetworkVariable<float>(100f);
    public NetworkVariable<bool> estaMuerto = new NetworkVariable<bool>(false);

    [Header("Configuración de Caída")]
    public float alturaMinimaMuerte = -15f;

    public override void OnNetworkSpawn()
    {
        estaMuerto.OnValueChanged += AlCambiarEstadoMuerte;
    }

    public override void OnNetworkDespawn()
    {
        estaMuerto.OnValueChanged -= AlCambiarEstadoMuerte;
    }

    // El prefab universal también trae VidaActivador en los corredores.
    // Solo el jugador que es el activador debe usar este componente.
    private bool SoyElActivador()
    {
        return DeathrunManager.Instancia != null &&
               OwnerClientId == DeathrunManager.Instancia.idDelActivadorActual.Value;
    }

    private void AlCambiarEstadoMuerte(bool estadoAnterior, bool estadoNuevo)
    {
        if (estadoNuevo == true)
        {
            CambiarVisibilidad(false);
        }
        else
        {
            CambiarVisibilidad(true);
        }
    }

    private void CambiarVisibilidad(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = visible;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = visible;
    }

    private void Update()
    {
        if (!IsServer || !SoyElActivador()) return;

        // Si se cae del mapa, le hacemos daño letal para que termine la partida
        if (transform.position.y < alturaMinimaMuerte && !estaMuerto.Value)
        {
            RecibirDano(1000f);
        }
    }

    public void RecibirDano(float cantidad)
    {
        if (!IsServer || !SoyElActivador() || estaMuerto.Value) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            vidaActual.Value = 0;
            estaMuerto.Value = true;
            if (DeathrunManager.Instancia != null)
                DeathrunManager.Instancia.RevisarEstadoPartida();
        }
    }

    public void RecibirDanoPvP(float cantidad)
    {
        if (!IsServer || !SoyElActivador() || estaMuerto.Value) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            vidaActual.Value = 0;
            estaMuerto.Value = true;
            if (DeathrunManager.Instancia != null)
                DeathrunManager.Instancia.RevisarEstadoPartida();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !SoyElActivador()) return;

        if (other.CompareTag("KillZone") || other.CompareTag("Trampa"))
        {
            RecibirDano(1000f); // Unificamos la muerte con el mismo sistema de daño
        }
    }

    // Mantenemos esta función por si tu script ZonaDeMuerte.cs la sigue llamando
    public void ReiniciarNivelPorDerrotaActivador()
    {
        if (!IsServer) return;
        RecibirDano(1000f);
    }
}