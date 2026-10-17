using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class AsignadorDeRoles : NetworkBehaviour
{
    public GameObject prefabActivador;
    public GameObject prefabCorredor;

    public Transform puntoInicioActivador;
    public Transform puntoInicioCorredores;

    private void Awake()
    {
        // Si al recargar la escena se creó un NetworkManager duplicado, lo destruimos
        NetworkManager[] managers = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (NetworkManager nm in managers)
        {
            if (NetworkManager.Singleton != null && nm != NetworkManager.Singleton)
            {
                Destroy(nm.gameObject);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 1. Escuchamos conexiones nuevas
            NetworkManager.Singleton.OnClientConnectedCallback += CrearPersonajeParaCliente;

            // 2. Escuchamos cuando TODOS terminan de cargar la escena tras un reinicio
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += EscenaCargadaParaTodos;

            // 3. SOLO creamos al Host acá si es la primera vez que abre la sala (1 solo conectado).
            // Si es un reinicio de partida (hay más de 1 jugador), esperamos a "EscenaCargadaParaTodos"
            // para que los clientes ya estén adentro del mapa cuando nazca el Activador.
            if (NetworkManager.Singleton.ConnectedClientsIds.Count == 1)
            {
                CrearPersonajeParaCliente(NetworkManager.ServerClientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= CrearPersonajeParaCliente;
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= EscenaCargadaParaTodos;
            }
        }
    }

    // Se dispara automáticamente solo cuando TODOS los jugadores ya cargaron la escena
    private void EscenaCargadaParaTodos(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            CrearPersonajeParaCliente(clientId);
        }
    }

    private void CrearPersonajeParaCliente(ulong clientId)
    {
        // Barrera de seguridad para no duplicar si ya tiene un cuerpo activo
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient cliente))
        {
            if (cliente.PlayerObject != null && cliente.PlayerObject.IsSpawned)
            {
                return;
            }
        }

        GameObject prefabElegido = (clientId == NetworkManager.ServerClientId) ? prefabActivador : prefabCorredor;
        Transform puntoElegido = (clientId == NetworkManager.ServerClientId) ? puntoInicioActivador : puntoInicioCorredores;

        GameObject nuevoPersonaje = Instantiate(prefabElegido, puntoElegido.position, puntoElegido.rotation);
        nuevoPersonaje.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }
}