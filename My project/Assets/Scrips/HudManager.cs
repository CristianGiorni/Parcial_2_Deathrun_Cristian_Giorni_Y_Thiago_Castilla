using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class HudManager : NetworkBehaviour
{
    [Header("HUD del Corredor")]
    public GameObject panelCorredor;
    public Image barraDeVidaUI;
    public TextMeshProUGUI textoVidaUI;

    [Header("HUD del Activador")]
    public GameObject panelActivador;
    public TextMeshProUGUI textoEstadoTrampas;

    private PlayerHealth saludJugadorLocal;

    public override void OnNetworkSpawn()
    {
        // Apagamos el Canvas si no es nuestra pantalla (para no ver la UI ajena)
        if (!IsOwner)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null) canvas.enabled = false;
            return;
        }

        saludJugadorLocal = GetComponent<PlayerHealth>();

        // LÓGICA DE SEPARACIÓN DE ROLES
        if (IsServer)
        {
            // Sos el Host / Activador: Apagamos la vida, encendemos el panel de control
            panelCorredor.SetActive(false);
            panelActivador.SetActive(true);

            if (textoEstadoTrampas != null)
                textoEstadoTrampas.text = "SISTEMA ONLINE\nEsperando objetivo...";
        }
        else
        {
            // Sos el Cliente / Corredor: Encendemos la vida, apagamos el panel de control
            panelCorredor.SetActive(true);
            panelActivador.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Solo el Corredor necesita actualizar la barra de vida frame a frame
        if (!IsServer && saludJugadorLocal != null)
        {
            float vidaActual = saludJugadorLocal.vidaActual.Value;

            // Actualizamos el gráfico y el texto
            if (barraDeVidaUI != null)
                barraDeVidaUI.fillAmount = vidaActual / 100f;

            if (textoVidaUI != null)
                textoVidaUI.text = Mathf.RoundToInt(vidaActual) + " / 100";
        }
    }
}