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
        // 1. Teletransportar Activador (Esto ya te funcionaba bien porque el Host es el dueño)
        VidaActivador activador = FindFirstObjectByType<VidaActivador>();
        if (activador != null)
        {
            TeletransportarObjeto(activador.gameObject, puntoAparicionActivador.position);
        }

        // 2. Teletransportar Corredores
        // Dejamos que solo el Servidor dispare la orden para evitar desincronización de red
        if (IsServer)
        {
            PlayerHealth[] corredores = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (PlayerHealth corredor in corredores)
            {
                if (corredor.llegoAMeta.Value)
                {
                    // Le enviamos la posición de la arena para que el cliente se mueva solo
                    corredor.RestaurarJugadorRpc(puntoAparicionSupervivientes.position);
                }
            }
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
            // 1. Despawneamos los cuerpos viejos para que no queden trabados entre escenas
            foreach (NetworkClient cliente in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (cliente.PlayerObject != null && cliente.PlayerObject.IsSpawned)
                {
                 //   cliente.PlayerObject.Despawn(true);
                }
            }

            // 2. Recargamos la escena para todos los jugadores
            string nombreEscenaActual = SceneManager.GetActiveScene().name;
            NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaActual, LoadSceneMode.Single);
        }
    }
    public void VerificarEstadoPartida()
    {
        if (!IsServer) return;

        PlayerHealth[] corredores = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        int vivosEnPista = 0;
        int enMeta = 0;

        foreach (PlayerHealth c in corredores)
        {
            if (c.llegoAMeta.Value)
            {
                enMeta++;
            }
            else if (!c.estaMuerto.Value) // O la variable que uses para saber si aún no es fantasma
            {
                vivosEnPista++;
            }
        }

        // Si todavía queda al menos 1 corredor vivo corriendo en la pista, NO hacemos nada todavía
        if (vivosEnPista > 0) return;

        // Si ya no queda nadie corriendo en la pista:
        if (enMeta > 0)
        {
            // Al menos uno llegó a la meta -> Vamos todos a la Arena Final
            IniciarBatallaFinalRpc();
        }
        else
        {
            // Murieron todos y nadie llegó a la meta -> Gana el Activador y reiniciamos el nivel
            RutinaReiniciarNivel();
        }
    }
}