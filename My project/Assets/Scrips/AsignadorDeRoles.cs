using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class AsignadorDeRoles : NetworkBehaviour
{
    [Header("Prefab Universal del Jugador")]
    [Tooltip("Arrastrá acá el Prefab que tiene el FirstPersonRunner, HudManager, y PlayerHealth. NO uses Prefabs separados.")]
    public GameObject prefabJugadorUniversal;

  
    public Transform puntoEsperaLobby;

    private void Awake()
    {
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
            NetworkManager.Singleton.OnClientConnectedCallback += CrearPersonajeParaCliente;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += EscenaCargadaParaTodos;

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

    private void EscenaCargadaParaTodos(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            CrearPersonajeParaCliente(clientId);
        }
    }

    private void CrearPersonajeParaCliente(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient cliente))
        {
            if (cliente.PlayerObject != null && cliente.PlayerObject.IsSpawned)
            {
                return;
            }
        }

        if (prefabJugadorUniversal == null || puntoEsperaLobby == null)
        {
            Debug.LogError("AsignadorDeRoles: Faltan asignar el Prefab Universal o el Punto de Espera en el Inspector.");
            return;
        }

      
        GameObject nuevoPersonaje = Instantiate(prefabJugadorUniversal, puntoEsperaLobby.position, puntoEsperaLobby.rotation);
        nuevoPersonaje.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }
}