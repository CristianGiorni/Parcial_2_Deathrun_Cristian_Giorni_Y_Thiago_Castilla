
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuRed : MonoBehaviour
{
    [Header("UI y Configuración")]
    public TMP_InputField inputIP;

    public string nombreEscenaJuego = "SampleScene";

    [Header("Configuración de Red")]
    public ushort puerto = 7777;

    
    public void IniciarComoActivador()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null)
        {
            Debug.LogError("No se encontró NetworkManager.");
            return;
        }

        UnityTransport transport =
            manager.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError("No se encontró Unity Transport.");
            return;
        }

       
        transport.SetConnectionData(
            "127.0.0.1",
            puerto,
            "0.0.0.0"
        );

        
        bool iniciado = manager.StartHost();

        if (!iniciado)
        {
            Debug.LogError("No se pudo iniciar el Host.");
            return;
        }

        Debug.Log("Host iniciado. Esperando al corredor...");

  
        manager.SceneManager.LoadScene(
            nombreEscenaJuego,
            LoadSceneMode.Single
        );
    }

 
    public void IniciarComoCorredor()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null)
        {
            Debug.LogError("No se encontró NetworkManager.");
            return;
        }

        UnityTransport transport =
            manager.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError("No se encontró Unity Transport.");
            return;
        }

        if (inputIP == null || string.IsNullOrWhiteSpace(inputIP.text))
        {
            Debug.LogError("Ingresá la IP de Radmin del Host.");
            return;
        }

        string ipDestino = inputIP.text.Trim();

     
        transport.SetConnectionData(
            ipDestino,
            puerto
        );

      
        bool conectado = manager.StartClient();

        if (!conectado)
        {
            Debug.LogError("No se pudo iniciar el Client.");
            return;
        }

        Debug.Log("Intentando conectar con: " + ipDestino);
    }
}
