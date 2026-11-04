using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeathrunManager : NetworkBehaviour
{
    public static DeathrunManager Instancia;

    [Header("Zonas de Inicio")]
    public Transform puntoInicioCorredores;
    public Transform puntoInicioActivador;

    [Header("Zonas de Batalla Final (Arena)")]
    public Transform puntoAparicionSupervivientes;
    public Transform puntoAparicionActivador;

    [Header("Configuración Lobby")]
    public int jugadoresRequeridos = 2;

    public NetworkVariable<ulong> idDelActivadorActual = new NetworkVariable<ulong>(999);
    public NetworkVariable<int> jugadoresListos = new NetworkVariable<int>(0);
    public NetworkVariable<bool> partidaIniciada = new NetworkVariable<bool>(false);

    public bool batallaFinalIniciada = false;
    private bool partidaTerminada = false;

    private void Awake()
    {
        Instancia = this;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void MarcarJugadorListoRpc(ulong clientId)
    {
        if (partidaIniciada.Value) return;

        jugadoresListos.Value++;

        if (jugadoresListos.Value >= jugadoresRequeridos)
        {
            partidaIniciada.Value = true;
            SortearRolesYArrancar();
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (!partidaIniciada.Value && jugadoresListos.Value > 0)
        {
            jugadoresListos.Value--;
        }
    }

    private void SortearRolesYArrancar()
    {
        if (!IsServer) return;

        var clientesConectados = NetworkManager.Singleton.ConnectedClientsIds;

        if (clientesConectados.Count > 0)
        {
            int indiceAlAzar = Random.Range(0, clientesConectados.Count);
            idDelActivadorActual.Value = clientesConectados[indiceAlAzar];
            AsignarRolesRpc(idDelActivadorActual.Value);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void AsignarRolesRpc(ulong nuevoActivadorID)
    {
        HudManager[] todosLosHuds = FindObjectsByType<HudManager>(FindObjectsSortMode.None);
        foreach (HudManager hud in todosLosHuds)
        {
            if (hud.IsOwner) hud.ActualizarRolYTeletransportar(nuevoActivadorID);
        }
    }

    public bool SoyElActivadorLocal()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return false;
        return NetworkManager.Singleton.LocalClientId == idDelActivadorActual.Value;
    }

  

    public void RevisarEstadoPartida()
    {
        
        if (!IsServer || partidaTerminada || !partidaIniciada.Value) return;

        
        PlayerHealth[] jugadores = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        int corredoresVivos = 0;
        int corredoresEnMeta = 0;
        int corredoresEnPista = 0;
        bool activadorEncontrado = false;
        bool activadorVivo = false;

        foreach (PlayerHealth p in jugadores)
        {
            if (p.OwnerClientId == idDelActivadorActual.Value)
            {
                activadorEncontrado = true;
                if (!p.estaMuerto.Value) activadorVivo = true;
                continue;
            }

            if (!p.estaMuerto.Value)
            {
                corredoresVivos++;
                
                if (p.llegoAMeta.Value) corredoresEnMeta++;
                else corredoresEnPista++;
            }
        }

        Debug.Log($"[Deathrun] Revisión -> enPista: {corredoresEnPista}, enMeta: {corredoresEnMeta}, corredoresVivos: {corredoresVivos}, activadorVivo: {activadorVivo}, batallaFinal: {batallaFinalIniciada}");

   
        if (activadorEncontrado && !activadorVivo)
        {
            TerminarPartida("¡EL ACTIVADOR HA CAÍDO! GANAN LOS CORREDORES.");
            return;
        }

       
        if (batallaFinalIniciada)
        {
            if (corredoresVivos == 0)
            {
                TerminarPartida("¡LOS CORREDORES FUERON EXTERMINADOS! GANA EL ACTIVADOR.");
            }
            return;
        }

     
        if (corredoresEnPista == 0)
        {
            if (corredoresEnMeta > 0)
            {
                
                batallaFinalIniciada = true;
                IniciarBatallaFinal(jugadores);
            }
            else
            {
             
                TerminarPartida("¡TODOS LOS CORREDORES MURIERON EN LAS TRAMPAS! GANA EL ACTIVADOR.");
            }
        }
    }

    
    private void IniciarBatallaFinal(PlayerHealth[] jugadores)
    {
      
        if (puntoAparicionActivador != null)
            TeletransportarArenaClientRpc(puntoAparicionActivador.position, SoloPara(idDelActivadorActual.Value));
        else
            Debug.LogError("DeathrunManager: falta asignar puntoAparicionActivador.");

        
        foreach (PlayerHealth c in jugadores)
        {
            if (c.OwnerClientId == idDelActivadorActual.Value) continue;
            if (c.estaMuerto.Value || !c.llegoAMeta.Value) continue;

            if (puntoAparicionSupervivientes != null)
                TeletransportarArenaClientRpc(puntoAparicionSupervivientes.position, SoloPara(c.OwnerClientId));
            else
                Debug.LogError("DeathrunManager: falta asignar puntoAparicionSupervivientes.");

            
            c.RestaurarJugadorRpc();
        }
    }

    private ClientRpcParams SoloPara(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
        };
    }

    [ClientRpc]
    private void TeletransportarArenaClientRpc(Vector3 destino, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null) return;

        GameObject miJugador = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        StartCoroutine(TeletransporteArenaSeguro(miJugador, destino));
    }

    private IEnumerator TeletransporteArenaSeguro(GameObject jugador, Vector3 destino)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        Rigidbody rb = jugador.GetComponent<Rigidbody>();
        ClientNetworkTransform cnt = jugador.GetComponent<ClientNetworkTransform>();

        if (cc != null) cc.enabled = false;
        if (cnt != null) cnt.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

       
        jugador.transform.position = destino + (Vector3.up * 1.5f);

        yield return null;

        if (cnt != null) cnt.enabled = true;
        if (cc != null) cc.enabled = true;
        if (rb != null) rb.isKinematic = false;
    }

    private void TerminarPartida(string mensajeVictoria)
    {
        partidaTerminada = true;
        Debug.Log(mensajeVictoria);
        StartCoroutine(RutinaReiniciarNivel());
    }

    private IEnumerator RutinaReiniciarNivel()
    {
        yield return new WaitForSeconds(3f);
        if (IsServer)
        {
            string nombreEscenaActual = SceneManager.GetActiveScene().name;
            NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaActual, LoadSceneMode.Single);
        }
    }
}