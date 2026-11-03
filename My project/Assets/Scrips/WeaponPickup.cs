using UnityEngine;
using Unity.Netcode;

public class WeaponPickup : NetworkBehaviour
{
    private bool collected = false;

    public void RequestPickup()
    {
        // Separamos las condiciones para evitar caracteres invisibles corruptos
        if (this.IsSpawned == false)
            return;

        if (this.collected == true)
            return;

        TryPickupServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TryPickupServerRpc(RpcParams rpcParams = default)
    {
        if (this.collected == true || this.IsSpawned == false)
            return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

        if (playerObject == null)
            return;

        if (Vector3.Distance(this.transform.position, playerObject.transform.position) > 3.5f)
            return;

        PlayerWeapon playerWeapon = playerObject.GetComponent<PlayerWeapon>();

        if (playerWeapon == null)
            return;

        if (playerWeapon.TryCollectWeapon(clientId) == false)
            return;

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