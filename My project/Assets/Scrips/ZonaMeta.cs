using UnityEngine;
using Unity.Netcode;

public class ZonaMeta : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth salud = other.GetComponent<PlayerHealth>();

            
            if (salud != null && salud.OwnerClientId != DeathrunManager.Instancia.idDelActivadorActual.Value && !salud.llegoAMeta.Value && !salud.estaMuerto.Value)
            {
                Debug.Log("¡Un corredor llegó a la meta!");
                salud.llegoAMeta.Value = true;

                salud.ActivarEspectadorRpc();
                DeathrunManager.Instancia.RevisarEstadoPartida();
            }
        }
    }
}