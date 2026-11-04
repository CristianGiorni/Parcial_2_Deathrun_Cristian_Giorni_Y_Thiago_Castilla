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

            // CAMBIO CLAVE: Permitimos cruzar a cualquiera cuyo ID no sea el del Activador actual
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