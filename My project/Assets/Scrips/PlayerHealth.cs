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
        // Apagamos el cuerpo físico para que los demás no lo vean ni choquen
        GetComponent<Collider>().enabled = false;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;

        if (IsOwner)
        {
            // 1. Apagamos tu script de controles
            FirstPersonRunner scriptMov = GetComponent<FirstPersonRunner>();
            if (scriptMov != null) scriptMov.enabled = false;

            // --- 2. ACÁ AGREGÁS LA LÍNEA DEL RIGIDBODY ---
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // Congela la gravedad
                                                   // ---------------------------------------------

            // 3. Encendemos el vuelo fantasma
            EspectadorFantasma fantasma = GetComponent<EspectadorFantasma>();
            if (fantasma != null) fantasma.enabled = true;

            // 4. Lo subimos si cayó al vacío
            if (transform.position.y < 0)
            {
                transform.position = new Vector3(transform.position.x, 15f, transform.position.z);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RestaurarJugadorRpc()
    {
        // Devolvemos el cuerpo físico para la arena final
        GetComponent<Collider>().enabled = true;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = true;

        if (IsOwner)
        {
            // 1. Apagamos el vuelo fantasma
            EspectadorFantasma fantasma = GetComponent<EspectadorFantasma>();
            if (fantasma != null) fantasma.enabled = false;

            // 2. Devolvemos los controles normales
            FirstPersonRunner scriptMov = GetComponent<FirstPersonRunner>();
            if (scriptMov != null) scriptMov.enabled = true;

            // --- 3. ACÁ LE DEVOLVÉS LA GRAVEDAD AL RIGIDBODY ---
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false; // Vuelve a caer normal
                                                    // ---------------------------------------------------
        }
    }
}