using UnityEngine;
using Unity.Netcode;

public class AsignadorDeRoles : NetworkBehaviour
{
    public GameObject prefabActivador;
    public GameObject prefabCorredor;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 1. Spawneamos al Host
            SpawnearPersonaje(NetworkManager.Singleton.LocalClientId);

            // 2. Escuchamos a los demás
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnearPersonaje;
        }
    }

    private void SpawnearPersonaje(ulong clientId)
    {
        // CANDADO DE SEGURIDAD: Evita que se creen clones encimados
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient clienteNetwork))
        {
            if (clienteNetwork.PlayerObject != null)
            {
                return; // Si el jugador ya tiene un cuerpo en la escena, cancelamos la creación
            }
        }

        GameObject prefabAElegir = (clientId == 0) ? prefabActivador : prefabCorredor;
        GameObject jugador = Instantiate(prefabAElegir);
        jugador.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnearPersonaje;
        }
    }
}