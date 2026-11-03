using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro; // Usamos TextMeshPro para la UI
using UnityEngine.SceneManagement;

public class MenuRed : MonoBehaviour
{
    [Header("UI y Configuración")]
    public TMP_InputField inputIP;
    public string nombreEscenaJuego = "SampleScene"; // ⚠️ CAMBIÁ ESTO por el nombre exacto de tu escena de juego

    public void IniciarComoActivador()
    {
        // 1. El Activador arranca como Servidor + Jugador a la vez
        NetworkManager.Singleton.StartHost();

        // 2. SOLO el Host cambia de escena por la red. Los clientes lo seguirán automáticamente.
        NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaJuego, LoadSceneMode.Single);
    }

    public void IniciarComoCorredor()
    {
        // 1. Leemos la IP que el jugador escribió (si no puso nada, usamos localhost)
        string ipDestino = inputIP.text;
        if (string.IsNullOrEmpty(ipDestino))
        {
            ipDestino = "127.0.0.1";
        }

        // 2. Modificamos el Unity Transport para apuntar a esa IP
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ipDestino;
        }

        // 3. Iniciamos como Cliente (No cambiamos de escena, Netcode sincroniza la pantalla con el Host)
        NetworkManager.Singleton.StartClient();
    }
}