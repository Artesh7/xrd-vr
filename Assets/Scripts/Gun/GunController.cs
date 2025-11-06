using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class GunController : MonoBehaviour
{
    [Header("Refs")]
    public Transform muzzle;
    public GameObject projectilePrefab;

    [Header("Gameplay")]
    public float projectileSpeed = 30f;
    public float fireCooldown = 0.15f;
    public bool requireMag = true;
    public int magCapacity = 7;

    [Header("Optional desktop testing (non-VR)")]
    public InputActionReference desktopFire; // allows left-click to fire in Editor

    [HideInInspector] public bool magInserted;
    int ammoInMag;
    float nextFireTime;
    bool isFiring;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    // While grabbed, align gun rotation to the controller's forward
    Transform currentInteractorTransform = null;
    System.Collections.Generic.List<Renderer> currentInteractorRenderers = new System.Collections.Generic.List<Renderer>();
    System.Collections.Generic.List<Collider> currentInteractorColliders = new System.Collections.Generic.List<Collider>();
    System.Collections.Generic.List<Rigidbody> currentInteractorRigidbodies = new System.Collections.Generic.List<Rigidbody>();
    System.Collections.Generic.List<bool> currentInteractorRigidbodyOriginalKinematic = new System.Collections.Generic.List<bool>();

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grab != null)
        {
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
                grab.selectEntered.AddListener(OnSelectEntered);
                grab.selectExited.AddListener(OnSelectExited);
                // Ensure there's a stable attach transform so grabbing uses a fixed
                // grip point instead of the collision contact point (prevents
                // picking the gun from the side when it's lying on a table).
                if (grab.attachTransform == null)
                {
                    var attachGo = transform.Find("GripAttach");
                    if (attachGo == null)
                    {
                        var go = new GameObject("GripAttach");
                        go.transform.SetParent(transform, false);
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localRotation = Quaternion.identity;
                        grab.attachTransform = go.transform;
                    }
                    else
                    {
                        grab.attachTransform = attachGo;
                    }
                }
        }

        if (desktopFire != null)
            desktopFire.action.performed += _ => TryFire();
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.activated.RemoveListener(OnActivated);
            grab.deactivated.RemoveListener(OnDeactivated);
                grab.selectEntered.RemoveListener(OnSelectEntered);
                grab.selectExited.RemoveListener(OnSelectExited);
        }

        if (desktopFire != null)
            desktopFire.action.performed -= _ => TryFire();
    }

    void Update()
    {
        if (isFiring)
            TryFire();

        // If currently selected, align the gun's forward to the interactor/controller forward
        if (currentInteractorTransform != null)
        {
            if (muzzle != null)
            {
                // Rotate the gun so the muzzle's forward aligns with the interactor forward
                var rot = Quaternion.FromToRotation(muzzle.forward, currentInteractorTransform.forward);
                transform.rotation = rot * transform.rotation;
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(currentInteractorTransform.forward, currentInteractorTransform.up);
            }
        }
    }

    void OnActivated(ActivateEventArgs _) => isFiring = true;
    void OnDeactivated(DeactivateEventArgs _) => isFiring = false;

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args == null || args.interactorObject == null) return;
        currentInteractorTransform = args.interactorObject.transform as Transform;

        // Enforce proximity at selection time (if too far, cancel selection)
        try
        {
            var interactorBase = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
            if (interactorBase != null && grab != null && grab.interactionManager != null)
            {
                float dist = Vector3.Distance(interactorBase.transform.position, transform.position);
                if (dist > 0.75f) // default max pickup distance; tweak as needed or expose as field
                {
                    var ixrInteractor = interactorBase as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor;
                    var ixrInteractable = grab as UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable;
                    if (ixrInteractor != null && ixrInteractable != null)
                        grab.interactionManager.SelectExit(ixrInteractor, ixrInteractable);
                    return;
                }
            }
        }
        catch { }

        if (currentInteractorTransform != null)
        {
            if (muzzle != null)
            {
                var rot = Quaternion.FromToRotation(muzzle.forward, currentInteractorTransform.forward);
                transform.rotation = rot * transform.rotation;
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(currentInteractorTransform.forward, currentInteractorTransform.up);
            }
        }

        // Hide interactor visuals while holding the gun
        currentInteractorRenderers.Clear();
        var interactorObj = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
        if (interactorObj != null)
        {
            var rends = interactorObj.transform.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r == null) continue;
                if (r.enabled)
                {
                    currentInteractorRenderers.Add(r);
                    r.enabled = false;
                }
            }
            // Disable colliders and make rigidbodies kinematic on the controller visuals
            currentInteractorColliders.Clear();
            currentInteractorRigidbodies.Clear();
            currentInteractorRigidbodyOriginalKinematic.Clear();

            var cols = interactorObj.transform.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                if (c == null) continue;
                if (c.enabled)
                {
                    currentInteractorColliders.Add(c);
                    c.enabled = false;
                }
            }

            var rbs = interactorObj.transform.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                if (rb == null) continue;
                currentInteractorRigidbodies.Add(rb);
                currentInteractorRigidbodyOriginalKinematic.Add(rb.isKinematic);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        // Re-enable any renderers we disabled on select
        var interactorObj = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
        if (interactorObj != null)
        {
            foreach (var r in currentInteractorRenderers)
                if (r != null)
                    r.enabled = true;
        }
        currentInteractorRenderers.Clear();
        // Re-enable colliders and restore rigidbody kinematic states
        foreach (var c in currentInteractorColliders)
            if (c != null)
                c.enabled = true;
        currentInteractorColliders.Clear();

        for (int i = 0; i < currentInteractorRigidbodies.Count; ++i)
        {
            var rb = currentInteractorRigidbodies[i];
            if (rb == null) continue;
            bool origK = true;
            if (i < currentInteractorRigidbodyOriginalKinematic.Count)
                origK = currentInteractorRigidbodyOriginalKinematic[i];
            rb.isKinematic = origK;
        }
        currentInteractorRigidbodies.Clear();
        currentInteractorRigidbodyOriginalKinematic.Clear();

        currentInteractorTransform = null;
    }

    void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (requireMag && (!magInserted || ammoInMag <= 0)) return;

        Fire();
        nextFireTime = Time.time + fireCooldown;
        if (requireMag) ammoInMag--;
    }

    void Fire()
    {
        if (!muzzle || !projectilePrefab) return;

        var go = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        if (go.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = muzzle.forward * projectileSpeed;

        Debug.Log("Gun fired");
    }

    public void SetMagInserted(bool inserted)
    {
        magInserted = inserted;
        if (inserted) ammoInMag = magCapacity;
    }
    // One-time flag to avoid repeated teleport while colliders keep contacting.
    bool magazineTeleported = false;

    void OnCollisionEnter(Collision collision)
    {
        if (magazineTeleported) return;
        if (collision == null) return;

        var other = collision.collider;
        if (other == null) return;

        bool otherIsBody = IsNamedOrAncestorNamed(other.transform, "Body_M1911");
        bool otherIsMag = IsNamedOrAncestorNamed(other.transform, "Magazine");

        // Check our child colliders to find the counterpart
        var childColliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in childColliders)
        {
            if (c == null) continue;
            bool cIsBody = IsNamedOrAncestorNamed(c.transform, "Body_M1911");
            bool cIsMag = IsNamedOrAncestorNamed(c.transform, "Magazine");

            if ((otherIsBody && cIsMag) || (otherIsMag && cIsBody))
            {
                var mag = FindDescendantByName("Magazine");
                if (mag != null)
                {
                    // If the magazine is currently held by an interactor, don't force-insert it.
                    var magGrab = mag.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (magGrab != null && magGrab.isSelected) return;

                    // Set local rotation to identity so it aligns correctly inside the body.
                    mag.localRotation = Quaternion.identity;
                    mag.localPosition = new Vector3(0.103100002f, -0.00270000007f, 0f);
                    // Disable colliders and make kinematic while inserted so it
                    // doesn't immediately collide back out of the gun.
                    ConfigureInsertedMagazine(mag);
                    // Mark the magazine as inserted so firing logic can use it.
                    SetMagInserted(true);
                    magazineTeleported = true;
                    return;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Handle trigger-based setups similarly; we don't have the "otherCollider"
        // reference here, so just check whether the entering collider and any of
        // this object's child colliders match the two target names.
        if (magazineTeleported) return;
        if (other == null) return;

        // Check the incoming collider
        bool otherIsBody = IsNamedOrAncestorNamed(other.transform, "Body_M1911");
        bool otherIsMag = IsNamedOrAncestorNamed(other.transform, "Magazine");

        // Check our child colliders to see if any of them are the counterpart
        var childColliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in childColliders)
        {
            if (c == null) continue;
            bool cIsBody = IsNamedOrAncestorNamed(c.transform, "Body_M1911");
            bool cIsMag = IsNamedOrAncestorNamed(c.transform, "Magazine");

            if ((otherIsBody && cIsMag) || (otherIsMag && cIsBody))
            {
                var mag = FindDescendantByName("Magazine");
                if (mag != null)
                {
                    // If the magazine is currently held by an interactor, don't force-insert it.
                    var magGrab = mag.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (magGrab != null && magGrab.isSelected) return;

                    // Set local rotation to identity so it aligns correctly inside the body.
                    mag.localRotation = Quaternion.identity;
                    mag.localPosition = new Vector3(0.103100002f, -0.00270000007f, 0f);
                    // Disable colliders and make kinematic while inserted so it
                    // doesn't immediately collide back out of the gun.
                    ConfigureInsertedMagazine(mag);
                    // Mark the magazine as inserted so firing logic can use it.
                    SetMagInserted(true);
                    magazineTeleported = true;
                    return;
                }
            }
        }
    }

    // Helper: walk up parents to see if any ancestor (or self) matches the name.
    bool IsNamedOrAncestorNamed(Transform t, string targetName)
    {
        while (t != null)
        {
            if (t.name == targetName) return true;
            if (t == this.transform) break; // stop at this GameObject
            t = t.parent;
        }
        return false;
    }

    // Find descendant transform named exactly "Magazine" (case-sensitive).
    Transform FindDescendantByName(string name)
    {
        var children = GetComponentsInChildren<Transform>(true);
        foreach (var t in children)
            if (t.name == name)
                return t;
        return null;
    }

    // Disable magazine colliders and make its rigidbody kinematic while inserted
    void ConfigureInsertedMagazine(Transform mag)
    {
        if (mag == null) return;

        // Disable any colliders on the magazine (and its children)
        var colliders = mag.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
            c.enabled = false;

        // Make the magazine's rigidbody (if any) kinematic and zero velocities
        var rb = mag.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}
