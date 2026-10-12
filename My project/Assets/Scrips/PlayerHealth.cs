using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    public NetworkVariable<float> vidaActual = new NetworkVariable<float>(100f);
    [Header("Límite del Mapa")]
    public float alturaMinimaMuerte = -15f; // Si cae por debajo de la altura -15, se muere

    // Agregá esta función adentro de PlayerHealth:
    private void Update()
    {
        // Solo el servidor controla si se cayó
        if (!IsServer) return;

        // Si la posición Y (altura) es menor al límite y el jugador sigue vivo
        if (transform.position.y < alturaMinimaMuerte && vidaActual.Value > 0)
        {
            Debug.Log("Jugador cayó al vacío, forzando respawn.");
            RecibirDano(1000f);
        }
    }
    [Header("Reaparici�n")]
    // Coordenadas donde va a revivir el jugador. Pod�s cambiarlas en el Inspector.
    public Vector3 puntoDeReaparicion = new Vector3(0, 2, 0);

    public void RecibirDano(float cantidad)
    {
        if (!IsServer) return; // Solo el servidor maneja la vida

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            // EN VEZ DE DESPAWN: Llamamos a la funci�n para revivirlo
            RevivirJugador();
        }
    }

    private void RevivirJugador()
    {
        // 1. Le devolvemos la vida al m�ximo
        vidaActual.Value = 100f;

        // 2. Le avisamos a todos los clientes que este jugador se tiene que teletransportar
        RespawnRpc(puntoDeReaparicion);
    }

    [Rpc(SendTo.Everyone)]
    private void RespawnRpc(Vector3 nuevaPosicion)
    {
        // Si tu personaje usa Rigidbody (F�sicas)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Frenamos cualquier ca�da libre
            rb.angularVelocity = Vector3.zero;
        }

        // Si tu personaje usa CharacterController (ATENCI�N AC�)
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            // Unity tiene un bug conocido: no pod�s teletransportar un CharacterController prendido.
            // Hay que apagarlo, moverlo y volverlo a prender.
            cc.enabled = false;
            transform.position = nuevaPosicion;
            cc.enabled = true;
        }
        else
        {
            // Si no usa CharacterController, simplemente lo movemos
            transform.position = nuevaPosicion;
        }

        Debug.Log("�El jugador revivi� en el punto de control!");
    }
}