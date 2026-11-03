using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class VidaActivador : NetworkBehaviour
{
    public NetworkVariable<float> vidaActual = new NetworkVariable<float>(100f);
    public NetworkVariable<bool> estaMuerto = new NetworkVariable<bool>(false);

    [Header("Configuración de Caída")]
    public float alturaMinimaMuerte = -15f;

    // 1. Nos suscribimos al cambio de la variable cuando el objeto nace en la red
    public override void OnNetworkSpawn()
    {
        estaMuerto.OnValueChanged += AlCambiarEstadoMuerte;
    }

    // 2. Nos desuscribimos al destruirse para evitar errores de memoria
    public override void OnNetworkDespawn()
    {
        estaMuerto.OnValueChanged -= AlCambiarEstadoMuerte;
    }

    // 3. Este evento se dispara en TODAS las computadoras a la vez cuando muere o revive
    private void AlCambiarEstadoMuerte(bool estadoAnterior, bool estadoNuevo)
    {
        if (estadoNuevo == true) // Si acaba de morir
        {
            CambiarVisibilidad(false);
        }
        else // Si acaba de revivir (o si reinician la partida)
        {
            CambiarVisibilidad(true);
        }
    }

    // 4. Lógica para apagar/prender gráficos y colisiones
    private void CambiarVisibilidad(bool visible)
    {
        // Apagamos o prendemos todos los modelos 3D (mesh)
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }

        // Desactivamos el CharacterController para que no se mueva ni bloquee el paso
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = visible;

        // Desactivamos cualquier otro collider para que los disparos lo traspasen
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = visible;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (transform.position.y < alturaMinimaMuerte && !estaMuerto.Value)
        {
            if (!DeathrunManager.Instancia.batallaFinalIniciada)
            {
                RespawnearSeguro();
            }
            else
            {
                RecibirDano(1000f);
            }
        }
    }

    public void RecibirDano(float cantidad)
    {
        if (!IsServer || estaMuerto.Value) return;

        if (!DeathrunManager.Instancia.batallaFinalIniciada) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            vidaActual.Value = 0;
            // Al ponerse en true, dispara instantáneamente AlCambiarEstadoMuerte en todos los clientes
            estaMuerto.Value = true;
            DeathrunManager.Instancia.RevisarEstadoPartida();
        }
    }

    public void RecibirDanoPvP(float cantidad)
    {
        if (!IsServer || estaMuerto.Value) return;

        if (!DeathrunManager.Instancia.batallaFinalIniciada) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            vidaActual.Value = 0;
            // Al ponerse en true, dispara instantáneamente AlCambiarEstadoMuerte en todos los clientes
            estaMuerto.Value = true;
            DeathrunManager.Instancia.RevisarEstadoPartida();
        }
    }

    private void RespawnearSeguro()
    {
        GameObject puntoRespawn = GameObject.FindWithTag("RespawnActivador");
        if (puntoRespawn != null)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            transform.position = puntoRespawn.transform.position;

            if (cc != null) cc.enabled = true;
        }
        else
        {
            Debug.LogWarning("Falta crear un objeto con el Tag 'RespawnActivador'");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("KillZone") || other.CompareTag("Trampa"))
        {
            ReiniciarNivelPorDerrotaActivador();
        }
    }

    public void ReiniciarNivelPorDerrotaActivador()
    {
        if (!IsServer) return;

        string nombreEscenaActual = SceneManager.GetActiveScene().name;
        NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaActual, LoadSceneMode.Single);
    }
}