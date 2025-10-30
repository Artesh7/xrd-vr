using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSocketInteractor))]
public class MagSocketHandler : MonoBehaviour
{
    private XRSocketInteractor socket;
    private GunController gun;

    [Header("Optional")]
    [Tooltip("Only accept objects with this tag (leave empty to accept any).")]
    public string requiredTag = "Magazine";

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        gun    = GetComponentInParent<GunController>();

        socket.selectEntered.AddListener(OnMagInserted);
        socket.selectExited.AddListener(OnMagRemoved);

        // If you made an AttachPoint child, set it on the socket in the Inspector.
        // (Socket Snapping uses this transform to align the mag.)
    }

    void OnDestroy()
    {
        socket.selectEntered.RemoveListener(OnMagInserted);
        socket.selectExited.RemoveListener(OnMagRemoved);
    }

    private void OnMagInserted(SelectEnterEventArgs args)
    {
        // (Optional) reject if wrong object
        if (!string.IsNullOrEmpty(requiredTag))
        {
            var comp = args.interactableObject as Component;
            if (comp != null && !comp.CompareTag(requiredTag))
            {
                // kick it back out
                if (socket.interactionManager != null)
                    socket.interactionManager.SelectExit(socket, args.interactableObject);
                return;
            }
        }

        // Lock the mag in place
        var compT = (args.interactableObject as Component)?.transform;
        if (!compT) return;

        var rb = compT.GetComponentInParent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        var attach = socket.attachTransform != null ? socket.attachTransform : transform;
        compT.SetPositionAndRotation(attach.position, attach.rotation);
        compT.SetParent(attach, true);

        if (gun) gun.SetMagInserted(true);
    }

    private void OnMagRemoved(SelectExitEventArgs args)
    {
        var compT = (args.interactableObject as Component)?.transform;
        if (!compT) return;

        var rb = compT.GetComponentInParent<Rigidbody>();
        if (rb) rb.isKinematic = false;

        compT.SetParent(null, true);

        if (gun) gun.SetMagInserted(false);
    }
}
