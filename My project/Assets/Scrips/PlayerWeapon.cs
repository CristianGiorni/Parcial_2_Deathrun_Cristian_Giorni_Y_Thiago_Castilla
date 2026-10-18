
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    public Transform cameraTransform;
    public Transform weaponHolder;
    public GameObject weaponVisualPrefab;

    public NetworkVariable<bool> HasWeapon = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private GameObject equippedVisual;

    private void Update()
    {
        if (!IsOwner || cameraTransform == null)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            TryPickupWeapon();
    }

    private void TryPickupWeapon()
    {
        if (HasWeapon.Value)
            return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            WeaponPickup pickup = hit.collider.GetComponentInParent<WeaponPickup>();

            if (pickup != null)
                pickup.RequestPickup();
        }
    }

    public bool TryCollectWeapon(ulong clientId)
    {
        if (!IsServer || HasWeapon.Value)
            return false;

        HasWeapon.Value = true;
        EquipWeaponClientRpc();

        return true;
    }

    public Transform GetMuzzlePoint()
    {
        if (equippedVisual == null)
            return null;

        Transform[] children =
            equippedVisual.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == "MuzzlePoint")
                return child;
        }

        return null;
    }

    public override void OnNetworkSpawn()
    {
        HasWeapon.OnValueChanged += OnWeaponChanged;

        if (HasWeapon.Value)
            EquipWeaponClientRpc();
    }

    public override void OnNetworkDespawn()
    {
        HasWeapon.OnValueChanged -= OnWeaponChanged;
    }

    private void OnWeaponChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            EquipWeaponClientRpc();
        }
        else if (equippedVisual != null)
        {
            Destroy(equippedVisual);
            equippedVisual = null;
        }
    }

    [ClientRpc]
    private void EquipWeaponClientRpc()
    {
        if (equippedVisual != null || weaponVisualPrefab == null)
            return;

        Transform parent;

        if (IsOwner)
            parent = weaponHolder != null ? weaponHolder : cameraTransform;
        else
            parent = transform;

        if (parent == null)
            return;

        equippedVisual = Instantiate(weaponVisualPrefab, parent);

        if (IsOwner)
        {
            equippedVisual.transform.localPosition = new Vector3(0.35f, -0.3f, 0.7f);
            equippedVisual.transform.localRotation = Quaternion.identity;
            equippedVisual.transform.localScale = new Vector3(0.15f, 0.15f, 0.6f);
        }
        else
        {
            equippedVisual.transform.localPosition = new Vector3(0.25f, 0.5f, 0.35f);
            equippedVisual.transform.localRotation = Quaternion.identity;
            equippedVisual.transform.localScale = new Vector3(0.15f, 0.15f, 0.6f);
        }

        foreach (NetworkRigidbody body in equippedVisual.GetComponentsInChildren<NetworkRigidbody>(true))
            Destroy(body);

        foreach (NetworkObject netObj in equippedVisual.GetComponentsInChildren<NetworkObject>(true))
            Destroy(netObj);

        foreach (Rigidbody body in equippedVisual.GetComponentsInChildren<Rigidbody>(true))
            Destroy(body);

        foreach (Collider col in equippedVisual.GetComponentsInChildren<Collider>(true))
            Destroy(col);

        foreach (WeaponPickup pickup in equippedVisual.GetComponentsInChildren<WeaponPickup>(true))
            Destroy(pickup);
    }
}
