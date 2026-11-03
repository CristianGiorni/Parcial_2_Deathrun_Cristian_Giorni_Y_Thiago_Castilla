using UnityEngine;
using Unity.Netcode;

public class ZonaDeMuerte : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. SEGURO ANTI-CRASH: Si la red no arrancó o no somos el servidor, ignoramos todo
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        // 2. Si el que cayó es un Corredor (Player)
        if (other.CompareTag("Player"))
        {
            PlayerHealth salud = other.GetComponent<PlayerHealth>();
            if (salud != null)
            {
                // Le hacemos 1000 de daño para asegurar que la vida llegue a 0 al instante
                salud.RecibirDano(1000f);
            }
        }

        // 3. Si el que cayó es el Activador (Jefe)
        VidaActivador vidaActivador = other.GetComponent<VidaActivador>();
        if (vidaActivador != null)
        {
            // Ejecutamos la función que reinicia el nivel
            vidaActivador.ReiniciarNivelPorDerrotaActivador();
        }
    }
}