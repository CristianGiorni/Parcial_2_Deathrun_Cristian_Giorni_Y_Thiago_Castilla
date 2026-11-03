using UnityEngine;
using Unity.Netcode;
using System;
using PlayFab; // Esto ahora va a funcionar perfecto

public class PlayFabServerManager : MonoBehaviour
{
    private void Start()
    {
        // Application.isBatchMode detecta si el juego se compiló como "Dedicated Server" (sin gráficos).
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
        // Iniciamos el agente de PlayFab
        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = true;

        // Suscribimos los eventos obligatorios
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutDown;
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;

        // Le avisamos a la nube que ya cargamos el mapa y estamos listos
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();

        // Encendemos el servidor de Netcode for GameObjects
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