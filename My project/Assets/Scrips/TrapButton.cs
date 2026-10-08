using UnityEngine;
using Unity.Netcode;

public class TrapButton : NetworkBehaviour
{
    [SerializeField] private MonoBehaviour trap;

    public void Activate()
    {
        if (trap == null)
        {
            Debug.LogError("No hay una trampa asignada al botón.");
            return;
        }

        trap.SendMessage("RequestActivation", SendMessageOptions.RequireReceiver);
    }
}