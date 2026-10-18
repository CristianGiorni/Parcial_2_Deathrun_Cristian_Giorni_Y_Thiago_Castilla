
using UnityEngine;
using Unity.Netcode;

public class WeaponPickup : NetworkBehaviour
{
    private bool collected;

    public void RequestPickup()
    {
        if (!IsSpawned || collected)
            return;

        TryPickupServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        if (collected || !IsSpawned)
            return;

        ulong clientId = rpcParams.Receive.SenderClientId;

        NetworkObject playerObject =
            NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

        if (playerObject == null)
            return;

        if (Vector3.Distance(transform.position, playerObject.transform.position) > 3.5f)
            return;

        PlayerWeapon playerWeapon = playerObject.GetComponent<PlayerWeapon>();

        if (playerWeapon == null)
            return;

        if (!playerWeapon.TryCollectWeapon(clientId))
            return;

        collected = true;

        HidePickupClientRpc();

        NetworkObject.Despawn(false);
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
