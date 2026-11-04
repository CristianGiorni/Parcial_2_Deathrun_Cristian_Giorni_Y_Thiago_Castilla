using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class HudManager : NetworkBehaviour
{
    [Header("HUD del Lobby (Espera)")]
    public GameObject panelLobby;
    public TextMeshProUGUI textoJugadoresListos;
    public Button botonListo;

    [Header("HUD del Corredor")]
    public GameObject panelCorredor;
    public Image barraDeVidaUI;
    public TextMeshProUGUI textoVidaUI;

    [Header("HUD del Activador")]
    public GameObject panelActivador;
    public TextMeshProUGUI textoEstadoTrampas;

    private PlayerHealth saludJugadorLocal;
    private bool soyActivador = false;
    private bool yaDioListo = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null) canvas.enabled = false;
            return;
        }

        saludJugadorLocal = GetComponent<PlayerHealth>();

        if (DeathrunManager.Instancia != null)
        {
            if (DeathrunManager.Instancia.partidaIniciada.Value)
            {
                ActualizarRolYTeletransportar(DeathrunManager.Instancia.idDelActivadorActual.Value);
            }
            else
            {
                MostrarLobby();
            }
        }
    }

    private void MostrarLobby()
    {
        // En el lobby el cursor tiene que estar libre para poder tocar el botón "Listo"
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panelLobby != null) panelLobby.SetActive(true);
        if (panelCorredor != null) panelCorredor.SetActive(false);
        if (panelActivador != null) panelActivador.SetActive(false);

        if (botonListo != null)
        {
            botonListo.onClick.RemoveAllListeners();
            botonListo.onClick.AddListener(AlPresionarListo);
        }
    }

    public void AlPresionarListo()
    {
        if (yaDioListo) return;
        yaDioListo = true;

        if (botonListo != null) botonListo.interactable = false;

        if (DeathrunManager.Instancia != null)
        {
            DeathrunManager.Instancia.MarcarJugadorListoRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    public void ActualizarRolYTeletransportar(ulong idActivadorSorteado)
    {
        if (!IsOwner) return;

        soyActivador = (NetworkManager.Singleton.LocalClientId == idActivadorSorteado);

        // Salimos del lobby: bloqueamos y ocultamos el cursor para jugar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 1. Interfaz visual
        if (panelLobby != null) panelLobby.SetActive(false);
        if (panelCorredor != null) panelCorredor.SetActive(!soyActivador);
        if (panelActivador != null) panelActivador.SetActive(soyActivador);
        if (soyActivador && textoEstadoTrampas != null) textoEstadoTrampas.text = "SISTEMA ONLINE\nEsperando objetivo...";

        // 2. Elegimos el destino
        // (PlayerHealth queda encendido para todos: el activador también usa PlayerHealth)
        Vector3 destinoFinal = soyActivador
            ? DeathrunManager.Instancia.puntoInicioActivador.position
            : DeathrunManager.Instancia.puntoInicioCorredores.position;

        // 3. Iniciamos el viaje seguro
        StartCoroutine(RutinaTeletransporteSeguro(destinoFinal));
    }

    // --- EL TRUCO PARA VENCER EL RUBBERBANDING ---
    private IEnumerator RutinaTeletransporteSeguro(Vector3 destino)
    {
        CharacterController cc = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();
        ClientNetworkTransform cnt = GetComponent<ClientNetworkTransform>();

        // 1. APAGAMOS la red, las físicas y las colisiones para que nada bloquee el viaje
        if (cc != null) cc.enabled = false;
        if (cnt != null) cnt.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 2. Nos movemos a la nueva coordenada
        transform.position = destino;

        // 3. ESPERAMOS 1 FRAME EXACTO. Esto le da tiempo al motor gráfico de registrar que estamos en el nuevo lugar
        yield return null;

        // 4. PRENDEMOS todo de vuelta
        if (cnt != null) cnt.enabled = true;
        if (cc != null) cc.enabled = true;
        if (rb != null) rb.isKinematic = false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (panelLobby != null && panelLobby.activeSelf && DeathrunManager.Instancia != null)
        {
            if (textoJugadoresListos != null)
                textoJugadoresListos.text = $"Jugadores listos: {DeathrunManager.Instancia.jugadoresListos.Value} / {DeathrunManager.Instancia.jugadoresRequeridos}";
        }

        if (!soyActivador && saludJugadorLocal != null)
        {
            float vidaActual = saludJugadorLocal.vidaActual.Value;
            if (barraDeVidaUI != null) barraDeVidaUI.fillAmount = vidaActual / 100f;
            if (textoVidaUI != null) textoVidaUI.text = Mathf.RoundToInt(vidaActual) + " / 100";
        }
    }
}