using UnityEngine;
using Unity.Netcode;

public class ZonaDeMuerte : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

      
        if (other.CompareTag("Player"))
        {
            PlayerHealth salud = other.GetComponent<PlayerHealth>();
            if (salud != null)
            {
               
                salud.RecibirDano(1000f);
            }
        }

        
        VidaActivador vidaActivador = other.GetComponent<VidaActivador>();
        if (vidaActivador != null)
        {
            
            vidaActivador.ReiniciarNivelPorDerrotaActivador();
        }
    }
}