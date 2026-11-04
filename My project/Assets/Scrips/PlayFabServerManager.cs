using UnityEngine;
using Unity.Netcode;
using System;
using PlayFab; 

public class PlayFabServerManager : MonoBehaviour
{
    private void Start()
    {
       
        if (Application.isBatchMode)
        {
            Debug.Log("Iniciando en modo Servidor Dedicado. Conectando con PlayFab...");
            IniciarServidorPlayFab();
        }
        else
        {
            Debug.Log("Juego iniciado normalmente. PlayFab GSDK ignorado. Listo para Host local.");
        }
    }

    private void IniciarServidorPlayFab()
    {
       
        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = true;

       
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutDown;
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;

        
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();

      
        NetworkManager.Singleton.StartServer();
    }

    private void OnServerActive()
    {
        Debug.Log("PlayFab ha marcado el servidor como ACTIVO.");
    }

    private void OnShutDown()
    {
        Debug.Log("PlayFab ordenó apagar el servidor.");
        NetworkManager.Singleton.Shutdown();
        Application.Quit();
    }

    private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
    {
        Debug.Log($"Mantenimiento programado por PlayFab a las: {NextScheduledMaintenanceUtc}");
    }
}