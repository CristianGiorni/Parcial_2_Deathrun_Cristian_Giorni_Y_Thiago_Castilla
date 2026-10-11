using UnityEngine;
using Unity.Netcode;

public class ZonaDeGas : MonoBehaviour
{
    [Header("Daño")]
    public float danoPorSegundo = 20f; // Cuánta vida resta por segundo

    // OnTriggerStay se ejecuta constantemente MIENTRAS el jugador siga adentro del cubo
    private void OnTriggerStay(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth salud = other.GetComponent<PlayerHealth>();
            if (salud != null)
            {
                // Multiplicamos por Time.deltaTime para que el daño sea fluido por segundo
                salud.RecibirDano(danoPorSegundo * Time.deltaTime);
            }
        }
    }
}