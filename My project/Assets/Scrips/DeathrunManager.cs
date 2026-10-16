using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeathrunManager : NetworkBehaviour
{
    public static DeathrunManager Instancia;

    [Header("Zonas de Batalla Final")]
    public Transform puntoAparicionSupervivientes;
    public Transform puntoAparicionActivador;

    public bool batallaFinalIniciada = false;
    private bool partidaTerminada = false;

    private void Awake()
    {
        Instancia = this;
    }

    public void RevisarEstadoPartida()
    {
        if (!IsServer || partidaTerminada) return;

        // Ahora buscamos a los dos bandos por separado
        PlayerHealth[] corredores = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        VidaActivador activador = FindFirstObjectByType<VidaActivador>();

        // --- LÓGICA DE LA BATALLA FINAL ---
        if (batallaFinalIniciada)
        {
            bool activadorVivo = (activador != null && !activador.estaMuerto.Value);
            int supervivientesVivos = 0;

            foreach (PlayerHealth corredor in corredores)
            {
                if (!corredor.estaMuerto.Value) supervivientesVivos++;
            }

            if (!activadorVivo)
                TerminarPartida("¡EL ACTIVADOR HA CAÍDO! GANAN LOS CORREDORES.");
            else if (supervivientesVivos == 0)
                TerminarPartida("¡LOS CORREDORES FUERON EXTERMINADOS! GANA EL ACTIVADOR.");

            return;
        }

        // --- LÓGICA DE LA PISTA DE TRAMPAS ---
        int corredoresEnPista = 0;
        int corredoresEnMeta = 0;

        foreach (PlayerHealth corredor in corredores)
        {
            if (!corredor.estaMuerto.Value && !corredor.llegoAMeta.Value) corredoresEnPista++;
            if (corredor.llegoAMeta.Value && !corredor.estaMuerto.Value) corredoresEnMeta++;
        }

        if (corredoresEnPista == 0)
        {
            if (corredoresEnMeta > 0)
            {
                batallaFinalIniciada = true;
                IniciarBatallaFinalRpc();
            }
            else
            {
                TerminarPartida("Todos los corredores murieron en las trampas. ¡GANA EL ACTIVADOR!");
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void IniciarBatallaFinalRpc()
    {
        // 1. Teletransportar Corredores
        PlayerHealth[] corredores = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (PlayerHealth corredor in corredores)
        {
            if (corredor.llegoAMeta.Value)
            {
                TeletransportarObjeto(corredor.gameObject, puntoAparicionSupervivientes.position);
                corredor.RestaurarJugadorRpc();
            }
        }

        // 2. Teletransportar Activador
        VidaActivador activador = FindFirstObjectByType<VidaActivador>();
        if (activador != null)
        {
            TeletransportarObjeto(activador.gameObject, puntoAparicionActivador.position);
        }
    }

    private void TeletransportarObjeto(GameObject jugador, Vector3 nuevaPosicion)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        jugador.transform.position = nuevaPosicion;
        if (cc != null) cc.enabled = true;
    }

    private void TerminarPartida(string mensajeVictoria)
    {
        partidaTerminada = true;
        Debug.Log(mensajeVictoria);
        StartCoroutine(RutinaReiniciarNivel());
    }

    private IEnumerator RutinaReiniciarNivel()
    {
        yield return new WaitForSeconds(5f);
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }
}