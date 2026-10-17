using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    public NetworkVariable<float> vidaActual = new NetworkVariable<float>(100f);

    // Nuevos estados
    public NetworkVariable<bool> estaMuerto = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> llegoAMeta = new NetworkVariable<bool>(false);

    [Header("Límite del Mapa")]
    public float alturaMinimaMuerte = -15f;
    public Transform puntoDeAparicion;
    private void Update()
    {
        if (!IsServer) return;

        if (transform.position.y < alturaMinimaMuerte && vidaActual.Value > 0 && !estaMuerto.Value)
        {
            RecibirDano(1000f); // Cayó al vacío
        }
    }

    public void RecibirDano(float cantidad)
    {
        if (!IsServer || estaMuerto.Value || llegoAMeta.Value) return;

        vidaActual.Value -= cantidad;

        if (vidaActual.Value <= 0)
        {
            estaMuerto.Value = true;
            ActivarEspectadorRpc(); // Pasa a ser espectador
            DeathrunManager.Instancia.RevisarEstadoPartida(); // Le avisa al cerebro que alguien murió
        }
    }

    [Rpc(SendTo.Everyone)]
    public void ActivarEspectadorRpc()
    {
        // 1. Ocultar el modelo 3D del corredor muerto para los demás
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        // 2. Apagar la colisión
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 3. Apagar el script de caminar normal (Cambiá "TuScriptDeMovimiento" por el nombre real)
        FirstPersonRunner scriptMov = GetComponent<FirstPersonRunner>();
        if (scriptMov != null) scriptMov.enabled = false;

        // 4. Frenar la caída, mantenerlo SIN gravedad (isKinematic = true) y subirlo al inicio
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // ¡Fundamental que quede en TRUE y nada lo pase a false!
        }

        // 5. Si somos el dueño de este fantasma, lo subimos al punto de inicio y prendemos el vuelo
        if (IsOwner)
        {
            if (puntoDeAparicion != null)
            {
                transform.position = puntoDeAparicion.position;
            }

            EspectadorFantasma fantasma = GetComponent<EspectadorFantasma>();
            if (fantasma != null)
            {
                fantasma.enabled = true;
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RestaurarJugadorRpc(Vector3 posicionArena)
    {
        // Devolvemos el cuerpo físico para que los demás lo vean
        GetComponent<Collider>().enabled = true;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = true;

        // 2. Solo el DUEÑO del personaje ejecuta el teletransporte
        if (IsOwner)
        {
            EspectadorFantasma fantasma = GetComponent<EspectadorFantasma>();
            if (fantasma != null) fantasma.enabled = false;

            FirstPersonRunner scriptMov = GetComponent<FirstPersonRunner>();
            if (scriptMov != null) scriptMov.enabled = true;

            // --- MAGIA PARA TELETRANSPORTAR EL RIGIDBODY ---
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;              // A. Pausamos las físicas
                transform.position = posicionArena; // B. El dueño se mueve a sí mismo
                rb.isKinematic = false;             // C. Reactivamos las físicas
            }
        }
    }
    public void RegenerarJugador()
    {
        // Solo el dueño mueve su propio cuerpo por la red
        if (IsOwner)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;                          // Pausamos las físicas
                transform.position = puntoDeAparicion.position; // Lo movemos al inicio
                rb.linearVelocity = Vector3.zero;                     // Frenamos la caída
                rb.isKinematic = false;                         // Reactivamos las físicas
            }
        }
    }

    // 3. El sensor automático: si tocamos algo peligroso, llamamos a la función
    private void OnTriggerEnter(Collider other)
    {
        // Si el cuerpo no es nuestro, no hacemos nada
        if (!IsOwner) return;

        // Si chocamos contra un objeto que tenga la etiqueta "Trampa" o "KillZone"
        if (other.CompareTag("Trampa") || other.CompareTag("KillZone"))
        {
            RegenerarJugador();
        }
    }
}