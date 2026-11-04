using UnityEngine;
using Unity.Netcode;

public class WeaponPickup : NetworkBehaviour
{
    private bool collected = false;

    public void RequestPickup()
    {
        Debug.Log($"[WeaponPickup] RequestPickup llamado. IsSpawned: {this.IsSpawned}, collected: {this.collected}");

       
        if (this.IsSpawned == false)
            return;

        if (this.collected == true)
            return;

        TryPickupServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TryPickupServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (this.collected == true || this.IsSpawned == false)
        {
            Debug.LogWarning($"[WeaponPickup] Rechazado: ya recogida o no spawneada (collected: {this.collected}, IsSpawned: {this.IsSpawned}).");
            return;
        }

        NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

        if (playerObject == null)
        {
            Debug.LogWarning($"[WeaponPickup] Rechazado: no se encontró el PlayerObject del cliente {clientId}.");
            return;
        }

        float distancia = Vector3.Distance(this.transform.position, playerObject.transform.position);
        if (distancia > 15f)
        {
            Debug.LogWarning($"[WeaponPickup] Rechazado por distancia en el servidor: {distancia} (cliente {clientId}).");
            return;
        }

        PlayerWeapon playerWeapon = playerObject.GetComponent<PlayerWeapon>();

        if (playerWeapon == null)
        {
            Debug.LogWarning("[WeaponPickup] Rechazado: el jugador no tiene PlayerWeapon.");
            return;
        }

        if (playerWeapon.TryCollectWeapon(clientId) == false)
        {
            Debug.LogWarning($"[WeaponPickup] Rechazado: TryCollectWeapon devolvió false (HasWeapon: {playerWeapon.HasWeapon.Value}).");
            return;
        }

        Debug.Log($"[WeaponPickup] Arma recogida por el cliente {clientId}.");

        this.collected = true;
        HidePickupClientRpc();
        this.NetworkObject.Despawn(false);
    }

    [ClientRpc]
    private void HidePickupClientRpc()
    {
        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
            rend.enabled = false;

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }
}