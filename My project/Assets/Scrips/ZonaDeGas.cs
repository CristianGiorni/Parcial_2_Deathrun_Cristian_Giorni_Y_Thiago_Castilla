using UnityEngine;
using Unity.Netcode;

public class ZonaDeGas : MonoBehaviour
{
    [Header("Daño")]
    public float danoPorSegundo = 20f; 

    
    private void OnTriggerStay(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth salud = other.GetComponent<PlayerHealth>();
            if (salud != null)
            {
              
                salud.RecibirDano(danoPorSegundo * Time.deltaTime);
            }
        }
    }
}