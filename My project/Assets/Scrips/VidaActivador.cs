using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class VidaActivador : NetworkBehaviour
{
    public NetworkVariable<float> vidaActual = new NetworkVariable<float>(100f);
    public NetworkVariable<bool> estaMuerto = new NetworkVariable<bool>(false);

    [Header("Configuración de Caída")]
    public float alturaMinimaMuerte = -15f;

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

        // Inmortal hasta que empiece la batalla final
        if (!DeathrunManager.Instancia.batallaFinalIniciada) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            vidaActual.Value = 0;
            estaMuerto.Value = true;
            DeathrunManager.Instancia.RevisarEstadoPartida();
        }
    }

    public void RecibirDanoPvP(float cantidad)
    {
        if (!IsServer || estaMuerto.Value) return;

        // El Activador solo recibe daño PvP si ya arrancó la batalla final
        if (!DeathrunManager.Instancia.batallaFinalIniciada) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            vidaActual.Value = 0;
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

        // El bucle de Despawn(true) fue eliminado de acá para evitar romper la red.

        string nombreEscenaActual = SceneManager.GetActiveScene().name;
        NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaActual, LoadSceneMode.Single);
    }
}