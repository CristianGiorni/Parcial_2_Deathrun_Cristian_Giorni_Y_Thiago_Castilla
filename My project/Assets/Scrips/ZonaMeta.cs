using UnityEngine;
using Unity.Netcode;

public class ZonaMeta : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Solo el servidor valida la meta

        if (other.CompareTag("Player"))
        {
            PlayerHealth salud = other.GetComponent<PlayerHealth>();

            // Verificamos que sea un Corredor (no el Activador) y que esté vivo
            if (salud != null && salud.OwnerClientId != NetworkManager.ServerClientId && !salud.llegoAMeta.Value && !salud.estaMuerto.Value)
            {
                Debug.Log("¡Un corredor llegó a la meta!");
                salud.llegoAMeta.Value = true;

                // Lo volvemos espectador temporalmente mientras espera a los demás
                salud.ActivarEspectadorRpc();

                // Avisamos al Cerebro
                DeathrunManager.Instancia.RevisarEstadoPartida();
            }
        }
    }
}