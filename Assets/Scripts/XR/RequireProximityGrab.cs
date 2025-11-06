using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Require the interactor to be within maxDistance to allow selection.
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class RequireProximityGrab : MonoBehaviour
{
    public float maxDistance = 0.75f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDestroy()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args == null || args.interactorObject == null) return;
        var interactorTransform = args.interactorObject.transform as Transform;
        if (interactorTransform == null) return;

            if (Vector3.Distance(interactorTransform.position, transform.position) > maxDistance)
            {
                // cancel selection immediately
                var manager = grab.interactionManager;
                if (manager != null)
                {
                    var interactorBase = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
                    if (interactorBase != null)
                    {
                        var ixrInteractor = interactorBase as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor;
                        var ixrInteractable = grab as UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable;
                        if (ixrInteractor != null && ixrInteractable != null)
                            manager.SelectExit(ixrInteractor, ixrInteractable);
                    }
                }
            }
    }
}
