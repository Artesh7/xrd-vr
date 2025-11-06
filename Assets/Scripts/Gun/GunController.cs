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

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grab != null)
        {
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
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
        }

        if (desktopFire != null)
            desktopFire.action.performed -= _ => TryFire();
    }

    void Update()
    {
        if (isFiring)
            TryFire();
    }

    void OnActivated(ActivateEventArgs _) => isFiring = true;
    void OnDeactivated(DeactivateEventArgs _) => isFiring = false;

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
