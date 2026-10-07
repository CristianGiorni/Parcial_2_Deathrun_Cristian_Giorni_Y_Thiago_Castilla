using UnityEngine;
using Unity.Netcode;

public class ZonaDeMuerte : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Si el objeto que cae tiene la etiqueta "Player" y un NetworkObject
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>() != null)
        {
            // Solo el servidor tiene autoridad para destruir jugadores
            if (NetworkManager.Singleton.IsServer)
            {
                other.GetComponent<NetworkObject>().Despawn();
                Debug.Log("Un corredor cayó al vacío y fue eliminado.");
            }
        }
    }
}