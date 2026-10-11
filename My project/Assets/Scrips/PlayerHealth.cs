using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    // Usamos NetworkVariable para que la barra de vida esté sincronizada a prueba de lag
    public NetworkVariable<float> vidaActual = new NetworkVariable<float>(100f);

    public void RecibirDano(float cantidad)
    {
        if (!IsServer) return; // Solo el servidor autoriza el daño

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            // Si la vida llega a 0, eliminamos al jugador
            GetComponent<NetworkObject>().Despawn();
        }
    }
}