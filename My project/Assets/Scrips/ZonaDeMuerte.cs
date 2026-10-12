using UnityEngine;
using Unity.Netcode;

public class ZonaDeMuerte : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Solo el servidor tiene autoridad para bajar la vida
        if (!NetworkManager.Singleton.IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth salud = other.GetComponent<PlayerHealth>();
            if (salud != null)
            {
                // Le hacemos 1000 de daño para asegurar que la vida llegue a 0 al instante
                salud.RecibirDano(1000f);
            }
        }
    }
}