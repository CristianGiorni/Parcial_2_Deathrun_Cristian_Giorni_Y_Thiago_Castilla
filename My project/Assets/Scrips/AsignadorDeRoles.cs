using UnityEngine;
using Unity.Netcode;

public class AsignadorDeRoles : NetworkBehaviour
{
    [Header("Prefabs de los Jugadores")]
    public GameObject prefabActivador;
    public GameObject prefabCorredor;

    [Header("Puntos de Inicio (Largada)")]
    public Transform puntoInicioActivador;
    public Transform puntoInicioCorredores;

    public override void OnNetworkSpawn()
    {
        // Este código solo lo ejecuta el Servidor
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += AsignarRolAlConectarse;
        }
    }

    private void AsignarRolAlConectarse(ulong clientId)
    {
        GameObject jugadorInstanciado;

        if (clientId == 0) // El Host siempre es el Activador
        {
            jugadorInstanciado = Instantiate(prefabActivador, puntoInicioActivador.position, puntoInicioActivador.rotation);
        }
        else // Los demás clientes son Corredores
        {
            jugadorInstanciado = Instantiate(prefabCorredor, puntoInicioCorredores.position, puntoInicioCorredores.rotation);
        }

        // Le decimos a la red que este objeto le pertenece a ese jugador específico
        jugadorInstanciado.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AsignarRolAlConectarse;
        }
    }
}