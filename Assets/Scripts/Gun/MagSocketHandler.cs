using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MagSocketHandler : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    private GunController gun;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        gun    = GetComponentInParent<GunController>();

        if (socket != null)
        {
            socket.selectEntered.AddListener(OnMagInserted);
            socket.selectExited.AddListener(OnMagRemoved);
        }
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnMagInserted);
            socket.selectExited.RemoveListener(OnMagRemoved);
        }
    }

    private void OnMagInserted(SelectEnterEventArgs args)
    {
        // Lock the mag to the socket
        var t  = args.interactableObject.transform;
        if (t.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;

        // Snap to attach transform if set, else to this transform
        var attach = socket.attachTransform != null ? socket.attachTransform : transform;
        t.SetPositionAndRotation(attach.position, attach.rotation);
        t.SetParent(attach, true);

        if (gun != null) gun.SetMagInserted(true);
    }

    private void OnMagRemoved(SelectExitEventArgs args)
    {
        var t  = args.interactableObject.transform;
        if (t.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;

        t.SetParent(null, true);

        if (gun != null) gun.SetMagInserted(false);
    }
}
