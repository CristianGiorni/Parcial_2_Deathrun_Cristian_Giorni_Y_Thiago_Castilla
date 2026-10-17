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

        // Si se cae del mapa...
        if (transform.position.y < alturaMinimaMuerte && !estaMuerto.Value)
        {
            if (!DeathrunManager.Instancia.batallaFinalIniciada)
            {
                // Fase 1: Carrera de trampas -> Lo salvamos
                RespawnearSeguro();
            }
            else
            {
                // Fase 2: Batalla Final -> Se cae de la arena y muere
                RecibirDano(1000f);
            }
        }
    }

    public void RecibirDano(float cantidad)
    {
        if (!IsServer || estaMuerto.Value) return;

        // El Activador es INMORTAL hasta que empiece la batalla final
        if (!DeathrunManager.Instancia.batallaFinalIniciada) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            estaMuerto.Value = true;
            // Le avisamos al cerebro que el jefe final cayó
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
        // Solo el Servidor tiene autoridad para reiniciar la partida
        if (!IsServer) return;

        // Si el Activador cae al vacío y toca la KillZone
        if (other.CompareTag("KillZone") || other.CompareTag("Trampa"))
        {
            ReiniciarNivelPorDerrotaActivador();
        }
    }

    public void ReiniciarNivelPorDerrotaActivador()
    {
        if (!IsServer) return;

        // 1. Limpiamos los cuerpos actuales de la red
        foreach (NetworkClient cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null && cliente.PlayerObject.IsSpawned)
            {
                cliente.PlayerObject.Despawn(true);
            }
        }

        // 2. Recargamos la escena para todos (Ganan los corredores)
        string nombreEscenaActual = SceneManager.GetActiveScene().name;
        NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaActual, LoadSceneMode.Single);
    }
}