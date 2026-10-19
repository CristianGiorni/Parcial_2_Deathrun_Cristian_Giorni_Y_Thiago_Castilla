
using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerShooter : NetworkBehaviour
{
    public Transform cameraTransform;
    public PlayerWeapon playerWeapon;

    public float fireRate = 0.12f;
    public float range = 150f;
    public float damage = 25f;

    public Color crosshairColor = Color.white;
    public Color bulletColor = new Color(1f, 0.75f, 0.15f);
    public float tracerDuration = 0.16f;
    public float tracerWidth = 0.035f;

    private float nextFireTime;
    private Texture2D crosshairTexture;

    private void Awake()
    {
        if (playerWeapon == null)
            playerWeapon = GetComponent<PlayerWeapon>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && cameraTransform == null)
            Debug.LogError("Falta asignar Camera Transform.");
    }

    private void Update()
    {
        if (!IsOwner || cameraTransform == null || playerWeapon == null)
            return;

        if (!playerWeapon.HasWeapon.Value)
            return;

        PlayerHealth health = GetComponent<PlayerHealth>();

        if (health != null && health.estaMuerto.Value)
            return;

        bool pressed = Input.GetMouseButtonDown(0);
        bool held = Input.GetMouseButton(0);

        if (!pressed && (!held || Time.time < nextFireTime))
            return;

        nextFireTime = Time.time + fireRate;

        Transform muzzle = playerWeapon.GetMuzzlePoint();

        Vector3 tracerStart = muzzle != null
            ? muzzle.position
            : cameraTransform.position;

        ShootServerRpc(
            cameraTransform.position,
            cameraTransform.forward,
            tracerStart
        );
    }

    private void OnGUI()
    {
        if (!IsOwner || cameraTransform == null || playerWeapon == null)
            return;

        if (!playerWeapon.HasWeapon.Value)
            return;

        PlayerHealth health = GetComponent<PlayerHealth>();

        if (health != null && health.estaMuerto.Value)
            return;

        if (crosshairTexture == null)
        {
            crosshairTexture = new Texture2D(1, 1);
            crosshairTexture.SetPixel(0, 0, Color.white);
            crosshairTexture.Apply();
        }

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float gap = 4f;
        float length = 9f;
        float thickness = 2f;

        GUI.color = crosshairColor;

        GUI.DrawTexture(new Rect(centerX - length - gap, centerY - thickness * 0.5f, length, thickness), crosshairTexture);
        GUI.DrawTexture(new Rect(centerX + gap, centerY - thickness * 0.5f, length, thickness), crosshairTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - length - gap, thickness, length), crosshairTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY + gap, thickness, length), crosshairTexture);

        GUI.color = Color.white;
    }

    [ServerRpc]
    private void ShootServerRpc(
        Vector3 origin,
        Vector3 direction,
        Vector3 requestedTracerStart,
        ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        NetworkObject playerObject =
            NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(senderId);

        if (playerObject == null || playerObject != NetworkObject)
            return;

        PlayerWeapon weapon = GetComponent<PlayerWeapon>();
        PlayerHealth shooterHealth = GetComponent<PlayerHealth>();

        if (weapon == null || !weapon.HasWeapon.Value)
            return;

        if (shooterHealth != null && shooterHealth.estaMuerto.Value)
            return;

        float cameraDistance = Vector3.Distance(origin, transform.position);

        if (cameraDistance > 3.5f)
        {
            Debug.LogWarning(
                $"Disparo rechazado por distancia. Distancia cámara-raíz: {cameraDistance}"
            );
            return;
        }

        if (direction.sqrMagnitude < 0.9f || direction.sqrMagnitude > 1.1f)
            return;

        if (Vector3.Distance(requestedTracerStart, origin) > 3f)
            requestedTracerStart = origin;

        direction.Normalize();

        Vector3 endPoint = origin + direction * range;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            endPoint = hit.point;

            // Buscamos ambos posibles scripts en el objeto impactado
            PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            VidaActivador targetActivador = hit.collider.GetComponentInParent<VidaActivador>();

            // 1. Si impactamos a un corredor (y no somos nosotros mismos)
            if (targetHealth != null && targetHealth != shooterHealth)
            {
                Debug.Log($"Impacto en Corredor: {targetHealth.name}. Vida: {targetHealth.vidaActual.Value}");

                if (!targetHealth.estaMuerto.Value)
                    targetHealth.RecibirDanoPvP(damage);
            }
            // 2. Si impactamos al Activador
            else if (targetActivador != null)
            {
                Debug.Log($"Impacto en Activador: {targetActivador.name}. Vida: {targetActivador.vidaActual.Value}");

                if (!targetActivador.estaMuerto.Value)
                    targetActivador.RecibirDanoPvP(damage);
            }
            // 3. Si impactamos en una pared u otro objeto estático
            else
            {
                Debug.Log($"El disparo impactó en estructura: {hit.collider.name}");
            }

            break;
        }

        ShowTracerClientRpc(requestedTracerStart, endPoint);
    }

    [ClientRpc]
    private void ShowTracerClientRpc(Vector3 start, Vector3 end)
    {
        GameObject tracer = new GameObject("BulletTracer");
        LineRenderer line = tracer.AddComponent<LineRenderer>();

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
            line.material = new Material(shader);

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = tracerWidth;
        line.endWidth = tracerWidth * 0.2f;
        line.startColor = bulletColor;
        line.endColor = bulletColor;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        StartCoroutine(RemoveTracer(tracer, line));
    }

    private IEnumerator RemoveTracer(GameObject tracer, LineRenderer line)
    {
        yield return new WaitForSeconds(tracerDuration);

        if (line != null && line.material != null)
            Destroy(line.material);

        if (tracer != null)
            Destroy(tracer);
    }

    public override void OnNetworkDespawn()
    {
        if (crosshairTexture != null)
            Destroy(crosshairTexture);
    }
}
